# Standard SerializationSystem

`InteractionFlow.Standard.SerializationSystem.Externals` は形式変換の実装基底を提供します。
ファイルの配置や保存の確定は FileSystem の責務です。

- `StreamSerializer<TValue>`: Stream と値の相互変換を派生実装へ委ね、依存ノードを提供します。
- `TextSerializer<TValue>`: 文字列と値の相互変換から Stream 変換を提供します。

## 文字コード

`Encoding` の既定値は `new UTF8Encoding(false, true)` 相当です。
BOM を出力せず、不正なバイト列や不正なサロゲートは失敗 Result になります。
別の文字コード、BOM 出力、置換フォールバックが必要なら Encoding を override します。
出力時のプリアンブルの扱いは StreamWriter に従います。

`DetectEncodingFromByteOrderMarks` は既定で true です。
読み取り時に BOM があれば、その文字コードを優先します。
指定した Encoding に固定したい場合は false に override します。
BOM によって文字コードが切り替わった場合は、StreamReader が選択した Encoding の
フォールバック設定に従います。

## TextSerializer の Stream 変換

Result API で成功時の処理を連結し、失敗を伝播します。

- Serialize は書き込み先の失敗を先に確認し、失敗なら文字列変換を呼びません。
- 文字列版 Serialize には inputValue をそのまま渡し、refText には空文字を渡します。
  inputValue の失敗をどう扱うかは派生実装が決めます。
- Deserialize は入力の失敗・読み取り失敗時には文字列変換を呼びません。
  refValue の失敗は変換先の新規作成などに使えるよう、派生実装へそのまま渡します。
- Reader / Writer による Stream 操作の例外だけを失敗 Result に変換します。
  文字列版の変換例外は捕捉しません。例外を Result 化する責務は、その派生実装にあります。

Stream は借りて利用し、成功・失敗のいずれでも所有権を取得しません。
現在位置から読み書きし、読み取りは末尾まで行います。
巻き戻し、位置の復元、書き込み後の切り詰めは行いません。
書き込み途中の失敗に対するロールバックは保証しません。
書き込み成功時は Flush を終え、渡された Stream を返します。

## 移行と検証

`DefaultRefText` は削除しました。既存の Stream 内容を参照するなどの特殊な変換は、
Stream 版 Serialize を override して実装します。
既定の出力は BOM 付き UTF-8 から BOM なし UTF-8 に変更しています。
従来と同じ文字コード設定が必要な場合は Encoding を `Encoding.UTF8` に override します。

専用テストは `InteractionFlow.Standard.Tests/SerializationSystem` にあります。
文字コード、BOM 検出、失敗時の処理順序、例外の Result 化、Stream の位置と所有権を検証します。
