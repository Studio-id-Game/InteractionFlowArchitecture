# InteractionFlow Agent Guide

このファイルは、Codex を含む自動編集エージェント向けの作業指針です。
リポジトリの設計意図を崩さず、変更を小さく安全に進めるための基準をまとめます。
また、`## トラブルシュート` を参照して、同種の問題が発生、または事前に予想される場合は、該当する確認手順や既知の解決策を優先して試してください。

## まず見るもの

- 作業の前提
  - `docs/processes/README.md`
  - `.editorconfig`
  - `Directory.Build.props`
  - `global.json`

- リポジトリの基本情報
  - `README.md`
  - `docs/RoleOfMainProjects.md`

- 設計思想に関連するタスクの場合
  - `docs/PHILOSOPHY.md`

- SystemFlowBuilder に関連するタスクの場合
  - `docs/SYSTEM_FLOW_BUILDER.md`

- 実装に関連するタスクの場合
  - `docs/LIBRARY_IMPLEMENTATION.md`
  - 変更対象に近い `*.csproj` と該当ソース

## このリポジトリの基本方針

- `InteractionFlow.Core` は概念と契約を置く層です。外部実装や UI 依存を入れないでください。
- `InteractionFlow.Standard` は再利用しやすい標準実装を置く層です。汎用性を優先してください。
- `InteractionFlow.Samples.*` は動作例と検証用です。実験的な変更はまずここで試してください。
- `InteractionFlow.Analyzers` は設計ルールの検証を担います。依存関係の規約変更はここも確認してください。

## 編集時の優先順位

1. 既存の命名、配置、責務分割に合わせる
2. 層の境界を跨ぐ変更を避ける
3. 変更は小さく分ける
4. 必要なら docs を同時に更新する

## 実装の注意

- `Core` から `Standard` や `Samples` への依存を作らない
- `SystemFlow` と `Interaction` の責務を混ぜない
- `ExternalPort` と `External` の境界を保つ
- 新しい概念を追加するときは、既存の `Entities` / `Builders` / `Interactions` / `SystemFlows` のどこに属するかを先に決める

## Interaction Flow の用語を確認するとき

- `Context` は、`Context Loop` のある時点における、User と System が共有する「現在」を表す
- `Context Loop` は、Interaction によって `Context` が移り変わり続ける時間的な過程であり、User と System の関係の歴史を表す
- `IFlowContext` は、`Context` のうち System 側で扱う文脈を提供する実装上の投影であり、`Context` または `Context Loop` そのものではない
- 一つの `IFlowContext` インスタンスを継続利用することは、`Context Loop` を実現する代表的な構成であり、両者の同一性を意味しない
- 文書間の矛盾を判断するときは、対象が概念と実装のどちらか、状態と時間的な過程のどちらかを分けて確認する
- 表現、包含、実現、寄与の関係を同一性として扱わず、比較する原文の主語と対象が一致していることを確認してから指摘する

## 図を参照するとき

- 問題解決のために `docs/img` 配下の図を参照する場合は、まず同名の `.context.md` を読む
- `.context.md` だけでは判断できない場合に、対応する `.svg` や `docs/img/src` 配下の `.drawio` を確認する
- `.drawio` の内容を変更した場合は `docs/processes/drawio-img-refresh.md` に従って `.context.md` と `.svg` を更新する

## 変更後に確認すること

- 変更後は、影響範囲の再ビルドと再テストを実行する
- ビルド時の定型フロー:
  - SDK 参照やユーザープロファイル配下のアクセス制限で止まりやすいため、`dotnet build InteractionFlow.slnx` は最初から昇格付きで実行する
- テスト時の定型フロー:
  - SDK 参照やユーザープロファイル配下のアクセス制限で止まりやすいため、`dotnet test InteractionFlow.slnx` は最初から昇格付きで実行する
- 個別に確認したい場合は `InteractionFlow.Analyzers.Tests/InteractionFlow.Analyzers.Tests.csproj` を対象に `dotnet test` を実行する
- 挙動に関わるなら関連サンプルの実行確認
- アーキテクチャや役割の変更があるなら `README.md` と `docs/` を更新
- `docs/img/src` の `.drawio` を更新した場合は `docs/processes/drawio-img-refresh.md` に従って関連成果物を更新

## 迷ったとき

- まずは `Core` に抽象を追加できるかを考える
- 実装の差し替えが必要なら `ExternalPort` を追加する
- サンプル都合の変更なら `Samples` 側に閉じ込める
