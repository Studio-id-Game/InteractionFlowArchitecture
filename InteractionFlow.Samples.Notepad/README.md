# Notepad

`Notepad.Core` の System Flow を、Console と通常のファイル保存で実行するサンプルです。

- `Program` で Console、Storage、Persistence、シリアライザを DI 登録します。
- `NotepadDataSimpleSerializer` は Note のタイトルと本文を単純なテキスト形式へ変換します。
- Core の Interaction と `MainLoop` を変えずに、通常版の外部実装を構成しています。

Flow の設計と、実行環境の実装を DI で分ける基本例です。
