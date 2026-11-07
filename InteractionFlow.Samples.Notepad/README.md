<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./../docs/PHILOSOPHY.md) |
[計算モデルとして](./../docs/COMPUTATIONAL_MODEL.md) |
[ライブラリ実装](./../docs/LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./../docs/LIBRARY_IMPLEMENTATION_DETAIL.md) |

---

# Notepad

新しい Standard ConsoleSystem と FilePersistence を使った、ユーザー別の Console メモ帳です。
ログイン、一覧、作成、編集、削除、ユーザー切り替えを提供します。

```powershell
dotnet run --project InteractionFlow.Samples.Notepad
```

入力・出力は UTF-8 です。操作手順と中断・保存の規則は
[Notepad Core](../InteractionFlow.Samples.Notepad.Core/README.md#操作) を参照してください。

## 実装

Program が共通の Interaction 群、ConsoleLoginOperation、NotepadDataFilePersistence、
NotepadDataSimpleSerializer を DI 登録します。
Serializer は先頭行をタイトル、残りを本文として、UTF-8 テキストに変換します。
Stream の開閉とファイルの置換は Standard の Persistence が担当し、Serializer は借りた Stream を閉じません。

既定の保存先は作業ディレクトリ配下の `NotepadData-v2/plain` です。
環境変数 `INTERACTIONFLOW_NOTEPAD_ROOT` を設定すると、そのディレクトリ配下の `plain` を使います。
例: `plain/user-alice/note-shopping.note`。Public は `plain/public` を使います。

通常版のユーザー分けは整理のためのもので、認証やデータの暗号化は行いません。
同時に複数のプロセスから同じ保存先を編集する用途は扱いません。
旧保存先・旧ファイル形式は読み書きせず、自動移行もしません。
旧ソースは [退避 ZIP](../archives/Notepad-before-console-system.zip) にあります。

同じ Flow で認証と暗号化を差し替える例は [Secure 版](../InteractionFlow.Samples.Notepad.Secure/README.md) を参照してください。

---

<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./../docs/PHILOSOPHY.md) |
[計算モデルとして](./../docs/COMPUTATIONAL_MODEL.md) |
[ライブラリの実装](./../docs/LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./../docs/LIBRARY_IMPLEMENTATION_DETAIL.md) |
