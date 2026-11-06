# InteractionFlow.Standard.Tests

Standard の公開契約と実装上の寿命管理を検証します。

`SerializationSystem/` では文字コード・BOM、失敗の伝播、Stream の位置と所有権を検証します。
契約と移行方法は [Standard SerializationSystem](../docs/SERIALIZATION.md) を参照してください。

`FileSystem/` では実ファイルを使い、保存失敗時の既存データ保持、ID と接尾辞の往復変換、
操作ごとのルート確定、Directory の準備・削除・列挙の拡張点を検証します。

- `ConsoleSystem/ConsolePortTests.cs`: DI で Port に対するテスト用実装を登録し、利用側 Interaction を検証します。
- `ConsoleSystem/ConsoleIntegrationTests.cs`: 標準実装をそのまま使用する結合テストです。
  公開の標準ストリームを設定し、入力の保留・失敗や出力を再現して、finally で元へ戻します。

内部オブジェクトの差し替えや、内部型・メソッドへのアクセスは行いません。
結合テストはプロセス共有の標準ストリーム・入力・購読ゲートを扱うため、並列実行を無効にしています。

```powershell
dotnet test InteractionFlow.Standard.Tests/InteractionFlow.Standard.Tests.csproj
```

実端末の確認項目と API の契約は [Standard Console](../docs/CONSOLE.md) を参照してください。
操作できる実行プロジェクトは [ConsoleSystem.ManualTests](../InteractionFlow.Standard.ConsoleSystem.ManualTests/README.md) です。
