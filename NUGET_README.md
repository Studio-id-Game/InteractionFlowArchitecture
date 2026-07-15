# Interaction Flow C# Package

Interaction Flow Architecture を C# で実装するためのライブラリです。
対話的なアプリケーション、ゲームループ、複雑なユーザー操作フローなどで、処理の流れと依存関係を分けて扱うための基盤を提供します。

## Packages

- `InteractionFlow.Core`
  - SystemFlow、Interaction、Context、Port などの基盤 API を提供します。
- `InteractionFlow.Standard`
  - `InteractionFlow.Core` に加えて、コンソール操作や DI ビルダーなどの標準実装を提供します。
- `InteractionFlow.Analyzers`
  - Interaction Flow Architecture の依存関係ルールを検査する Roslyn Analyzer です。

通常は `InteractionFlow.Standard` から始め、設計ルールの検査も使う場合は `InteractionFlow.Analyzers` を追加してください。

## Install

```bash
dotnet add package InteractionFlow.Standard
dotnet add package InteractionFlow.Analyzers
```

Analyzer は開発時だけ利用するため、プロジェクトファイルでは `PrivateAssets="all"` を付けることを推奨します。

```xml
<ItemGroup>
  <PackageReference Include="InteractionFlow.Standard" Version="0.4.0" />
  <PackageReference Include="InteractionFlow.Analyzers" Version="0.4.1" PrivateAssets="all" />
</ItemGroup>
```

## Architecture

Interaction Flow Architecture は、ユーザーとの相互作用を次のような要素に分けて扱います。

- SystemFlow: System 側が User への反応プロセスとして Interaction を束ねるフロー
- Interaction: システム内部の目的を達成する処理単位
- Function Port: 外部機能への抽象インターフェース
- Function External: UI、DB、外部サービスなどの実装
- Context: フロー間で受け渡される状態や文脈

依存関係は Port を境界に整理されるため、UI、保存先、外部サービスを差し替えやすく、テストしやすい構成を作れます。

## Samples

リポジトリには console sample と notepad sample が含まれています。
詳しい設計説明、図、サンプルコードは GitHub の README を参照してください。

Repository: https://github.com/Studio-id-Game/InteractionFlowArchitecture
