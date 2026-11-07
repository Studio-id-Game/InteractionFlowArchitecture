<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./../docs/PHILOSOPHY.md) |
[計算モデルとして](./../docs/COMPUTATIONAL_MODEL.md) |
[ライブラリ実装](./../docs/LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./../docs/LIBRARY_IMPLEMENTATION_DETAIL.md) |

---

# Notepad Secure

通常版と同じ Notepad.Core の Interaction / MainLoop に、パスワード認証と暗号化保存を接続するサンプルです。

```powershell
dotnet run --project InteractionFlow.Samples.Notepad.Secure
```

操作手順は [Notepad Core](../InteractionFlow.Samples.Notepad.Core/README.md#操作) と共通です。
ログイン時にパスワードを入力します。初回は確認入力を求め、既存アカウントではパスワードを検証します。
実端末ではキー入力を使い、文字を `*` で表示します。Backspace で削除できます。
入力リダイレクト時は一行をパスワードとして受け取ります。空パスワードは受け付けません。
入力・出力は UTF-8 です。

## 機能分離

- `SecureLoginOperation`: 共通ログイン Operation の認証部分を差し替えます。
- `PasswordInput`: キー入力によるマスク表示と、リダイレクトされた行入力を扱います。
- `SecureSession`: パスワードの検証、セッション鍵、ノートの暗号化・復号を扱います。
- `CredentialFiles` / `CredentialSerializer`: salt とパスワード検証用データを保存します。
- `EncryptedNoteSerializer`: Stream の入出力と暗号化形式の変換を担当します。
- `SecureNoteFiles`: Standard FilePersistence を使い、通常版とは保存先を分けます。

PBKDF2-SHA256（100,000反復、16バイト salt）で32バイトのユーザー鍵を導出します。
HMAC-SHA256 による検証値でログインを確認し、HKDF-Expand-SHA256 でファイルごとの鍵を導出します。
ノートは AES-256-GCM（12バイト nonce、16バイト tag）で暗号化し、ユーザー ID を追加認証データへ含めます。
保存ごとにファイル ID と nonce を再生成します。

暗号化ノートの形式は `[IFN2:4][file ID:16][nonce:12][tag:16][ciphertext]` です。
認証情報は `IFACCOUNT2` ヘッダー、Base64 salt、Base64 検証値の3行です。
ユーザー切り替え開始時と DI Scope 破棄時にセッション鍵をゼロクリアします。
一時的なパスワード用 char 配列、ファイル鍵、平文 byte 配列も利用後に消去します。
.NET の文字列（本文、リダイレクトされたパスワード行など）の消去は保証しません。

## 保存先と失敗時の扱い

既定の保存先は作業ディレクトリ配下の `NotepadData-v2` です。
環境変数 `INTERACTIONFLOW_NOTEPAD_ROOT` で基準ディレクトリを変更できます。

- ノート: `secure/user-alice/note-shopping.note`
- 認証情報: `secure-accounts/user-alice.account`
- Public: 各保存先の `public`（Public にもパスワードを設定します）

誤パスワードではログインせず、既存の認証情報やノートを上書きしません。
破損・アクセス不能な認証情報を新規ユーザーとして扱いません。
既存ノートがあるのに認証情報が欠落している場合も、再登録せず復元を案内します。
復号に失敗したノートは一覧で判別でき、編集による上書きはせず、確認付きの削除は可能です。

新形式は旧実装のファイルと互換ではなく、自動移行しません。
旧コードは [退避 ZIP](../archives/Notepad-before-console-system.zip) に保存しています。
このサンプルは同じ保存先の複数プロセス利用、パスワード変更・復旧、ファイル名やメタデータの秘匿を扱いません。
ファイル名の入れ替え検出や OS のアクセス権設定を提供するものでもありません。

回帰テストは [Notepad Core の検証](../InteractionFlow.Samples.Notepad.Core/README.md#検証) を参照してください。
実端末でのマスク表示・IME・Ctrl+C は手動確認が必要です。

---

<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./../docs/PHILOSOPHY.md) |
[計算モデルとして](./../docs/COMPUTATIONAL_MODEL.md) |
[ライブラリの実装](./../docs/LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./../docs/LIBRARY_IMPLEMENTATION_DETAIL.md) |
