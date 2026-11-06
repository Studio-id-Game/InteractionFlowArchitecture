# Standard FileSystem

`InteractionFlow.Standard.FileSystem.Externals` は、
Core の `IPersistencePort<TId, TValue>` をファイル・ディレクトリへ接続する実装基底を提供します。
Storage は値の所有、Serializer は形式変換、Persistence は実際の I/O を担当します。

## 公開 API

利用側は `IPersistencePort<TId, TValue>` またはそれを継承するアプリケーションの Port に依存します。
FileSystem 固有の特殊化 Port と公開 DirectoryUtility は廃止しました。
ID とパスの変換は派生実装向けの protected メソッドです。

| 型 | 主な責務 |
| --- | --- |
| FilePersistence | 単一ファイルの保存・読込・削除・存在確認・列挙 |
| DirectoryPersistence | パスと実行順序を揃え、内容処理と列挙を派生側へ委譲 |
| FileSystemPath（internal） | 確定済みルートからのパス検証・対象種類の確認 |

新しい共通基底、内部デバイス抽象、テスト用の差し替えコンストラクターは導入していません。

## 保存先の取得

両基底の既定実装は次のとおりです。

```csharp
public virtual string GetCurrentRootPath() => Environment.CurrentDirectory;
```

相対パスも返せます。各操作では、次の処理を一度だけ実行します。

```csharp
var rootPath = Path.GetFullPath(GetCurrentRootPath());
```

取得直後に絶対パス化し、検証・I/O・一時ファイルの後始末まで同じルートを使います。
途中で CurrentDirectory が変わっても、その操作の保存先は変わりません。
操作間の変更は次の操作へ反映されます。ID とキャッシュの対応は利用側で管理してください。
無効な戻り値や取得失敗は操作の失敗 Result です。

失敗 Result を渡した Save は、ルート取得・Serializer・拡張点を呼ばず、元の失敗を返します。
Load の oldValue は再利用のための別契約なので、失敗 Result でも読み込みを続けます。

## FilePersistence

### 派生例

```csharp
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Standard.FileSystem.Externals;
using System.IO;

public sealed class NoteFiles(ISerializerPort<Stream, string> serializer)
    : FilePersistence<string, string>(serializer, ".data.json")
{
    public override string GetCurrentRootPath() => "notes";

    protected override string GetRelativePathWithoutExtension(string id) => id;

    protected override string GetIdFromRelativePathWithoutExtension(string relativePath)
        => relativePath.Replace('\\', '/');
}
```

この例の ID `group/note.v2` は、現在の `notes` を基準に `group/note.v2.data.json` へ対応します。
Serializer は実際の保存形式に合わせて別途実装します。
ID の変換は往復可能かつ保存先が一意になるよう、派生側で定義してください。

### 接尾辞

コンストラクターの extension は読み取り専用の `Extension` へ保持します。既定は `.bin` です。
空文字、単一拡張子、複合拡張子を許可し、指定した文字列全体を付加・除去します。
大文字小文字の比較は Ordinal です。`.data.json` と `.DATA.JSON` は別の接尾辞として扱います。
ファイル名から複合拡張子を自動推測せず、ID 本体のドットは保持します。
空文字以外はドットで開始し、パス区切り・ワイルドカード・無効な文字・末尾のドットや空白を拒否します。

### 保存の手順と保証

1. 入力 Result を確認する。
2. ルートを確定し、ID から対象パスを解決・検証する。
3. 親ディレクトリを作成する。
4. 同じ親に固有名の一時ファイルを CreateNew で開く。
5. Serializer の成功を確認し、Flush とストリーム破棄を完了する。
6. 新規対象には File.Move、既存ファイルには File.Replace で配置する。

`netstandard2.1` では上書き指定の File.Move を使用できないため、まず新規配置を試み、
IOException の場合は対象がファイルであることを確認して Replace します。
保存先を先に削除する処理や、失敗後にコピー上書きするフォールバックはありません。
配置の競合や置換失敗は失敗 Result とし、無条件に再試行しません。

変換失敗・部分書き込み・Flush・Close の失敗では、既存ファイルを切り詰めません。
新規保存の失敗でも、不完全な正式ファイルを公開しません。
置換成功後に成功 Result を返します。
読み書きのストリームは Persistence が所有します。Serializer は閉じたり、所有権を取得したりしません。
Serialize の戻り値は成否確認に使用し、別のストリームへの差し替えには使用しません。

一時ファイルの最終名には `.interactionflow-save-` 接頭辞を予約します。
この接頭辞で始まる保存名は大文字小文字を区別せず拒否し、列挙からも除外します。
一時ファイルはこの実装が作成できたものだけを失敗時に削除します。
変換・配置の失敗と一時ファイル削除の失敗が重なった場合は、両方を AggregateException に保持します。
過去の中断で残った一時ファイルを名前だけで自動削除する処理はありません。

親ディレクトリの作成はロールバックしません。
複数プロセス間の更新順序、停電後の永続性、任意のファイルシステム上の原子性は保証しません。
ファイルの置き換えには親ディレクトリへの作成・置換権限と一時保存の追加容量も必要です。
Windows のローカル実ファイルで新規・更新・ロック・競合・失敗時の保持を検証しています。
ACL やメタデータの保持、他の OS・ネットワーク共有は使用する環境で別途確認してください。

## DirectoryPersistence

ID は階層を含む相対ディレクトリパスへ変換できます。
次の内容処理・列挙を abstract として派生側が実装します。

```csharp
protected abstract string GetRelativeDirectoryPath(TDirectoryId id);
protected abstract Task<Result> SaveDirectory(string path, TDirectoryId id, Result<TValue> value);
protected abstract Task<Result<TValue>> LoadDirectory(string path, TDirectoryId id, Result<TValue> oldValue);
protected abstract Task<Result<TDirectoryId[]>> GetDirectoryIds(string rootPath);
```

各拡張点には、その操作で確定・検証した絶対パスを渡します。ルートを取り直す必要はありません。
派生実装が別の ID のパスも必要とする場合は、確定済みルートを受け取る各基底の
`GetDirectoryPath(rootPath, id)` / `GetFilePath(rootPath, id)` を利用できます。

### 準備と削除

```csharp
protected virtual Task<Result> EnsureDirectory(string path, TDirectoryId id);
protected virtual Task<Result> DeleteDirectory(string path, TDirectoryId id);
```

Save は毎回 EnsureDirectory を呼び、成功した場合だけ SaveDirectory へ進みます。
既定の準備は Directory.CreateDirectory です。既存でも呼ばれ、初回限定ではありません。
派生側は子ディレクトリや初期ファイルを繰り返し整えられるように実装してください。
準備に失敗した場合は、保存へ進みません。
Load・Exists・GetAllIds からは準備処理を呼びません。

Delete は未作成対象でも拡張点へ委譲します。既定は再帰削除で、明確な不存在は成功です。
親と子がそれぞれ保存対象でも、親の既定削除は子を含めて削除します。
派生側の失敗 Result や例外を、基底が不存在と推測して成功へ変更することはありません。

複数ファイルの更新方法、部分失敗への対処、読込中に再利用する値の変更は派生側の責務です。
EnsureDirectory の変更や保存途中の変更を、基底がロールバックする契約はありません。

### 列挙

GetAllIds は非 virtual の共通境界です。ルートが存在するときだけ GetDirectoryIds へ委譲します。
ルートが未作成なら空配列を返します。派生側の探索中の例外を空配列に変更しません。
保存対象の識別と探索範囲は派生側が決めます。識別用ファイルは標準の必須条件ではありません。
保存対象でない中間・内部ディレクトリは除外し、同じ ID は重複させずに返してください。
親・子の両方が保存対象なら両方を返せます。順序は保証しません。

## 共通の失敗処理とパス

Result と例外の境界は [Result API](./RESULT.md) に従います。
Serializer と Result を返す Directory 拡張点の例外は、呼び出し側で Result 化しません。
既定の Directory 拡張点は、自身が担当する Directory 操作の例外を Result 化します。
File の保存・読込は Serializer の例外が伝播する場合も Stream を破棄します。
保存では所有する一時ファイルを清掃し、清掃にも失敗した場合は例外を集約して送出します。

| 操作 | 不存在の場合 | 作成の副作用 |
| --- | --- | --- |
| Save | File は親を作成、Directory は準備へ委譲 | 成功入力のみ許可 |
| Load | 失敗 Result | なし |
| Exists | 失敗 Result | なし |
| Delete | 既定実装では明確な不存在なら成功 | なし |
| GetAllIds | ルート不存在と確認できた場合だけ空配列 | なし |

Exists は File.GetAttributes によって対象の種類も確認します。
File.Exists / Directory.Exists の false だけで、権限や I/O の失敗を不存在へ置き換えません。
利用できないドライブ・共有を空の保存先として扱わないよう、
ルートや親が見つからない場合はボリュームルートの利用可否も確認します。
存在確認は次の実操作の成功保証ではありません。

ID からのパスは絶対パスやルート外への脱出を拒否し、ルート自身を保存・削除対象にしません。
パス検査は文字列の正規化と配下判定であり、リンクを解決した実体の隔離は保証しません。
GetAllIds が探索基準としてルートを使用することは許可します。

## 移行

| 以前の API | 新しい API / 対応 |
| --- | --- |
| IFilePersistencePort / IDirectoryPersistencePort | Core の IPersistencePort、またはアプリケーションの Port |
| RootPath override | public override GetCurrentRootPath() |
| Extension override | FilePersistence のコンストラクターへ接尾辞を渡す |
| public GetFileName / GetFileId | protected GetRelativePathWithoutExtension / GetIdFromRelativePathWithoutExtension |
| GetDirectoryName / GetDirectoryId | protected GetRelativeDirectoryPath。逆変換は列挙実装が必要に応じ定義 |
| GetAllIds override（Directory） | protected GetDirectoryIds(rootPath) |
| GetFilePath(id) / GetDirectoryPath(id) | protected GetFilePath(rootPath, id) / GetDirectoryPath(rootPath, id) |
| 公開 DirectoryUtility / CreateDirectories ラッパー | 基底のパス解決と、準備拡張点または標準 Directory API |

旧 API のエイリアスは提供しません。Core と SerializationSystem の型の契約は維持し、
名前空間をそれぞれ `InteractionFlow.Core.ExternalPorts`、
`InteractionFlow.Standard.SerializationSystem.Externals` に整理しています。
既存 Notepad 系 Samples の移行・再実装は別作業です。

## 検証

自動テストは `InteractionFlow.Standard.Tests/FileSystem` にあります。
標準実装は所有する専用一時ディレクトリで検証し、変換失敗は Serializer Port の実装で再現します。
内部オブジェクトの差し替えは使用しません。Windows 固有のテストは他の OS では明示的にスキップします。
