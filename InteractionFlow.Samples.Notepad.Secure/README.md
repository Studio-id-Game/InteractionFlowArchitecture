# Notepad Secure

`Notepad.Core` のユーザー体験を保ったまま、ログインと保存処理を安全な実装へ差し替えるサンプルです。

- `LoginSecure` と `EnterPassword` が、通常の Login を拡張してパスワード入力とユーザー鍵の準備を加えます。
- PBKDF2、HKDF、AES-GCM を用いる `SecureManagerPbkdf2` と暗号化シリアライザを DI 登録します。
- `SecretBuffer` と `UserSecureData` は、鍵素材を利用後にゼロクリアする責務を持ちます。

同じ Core の Interaction／System Flow に対し、Port 実装と一部の Interaction を差し替えてセキュリティ要件を統合する例です。
