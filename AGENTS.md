# InteractionFlow Agent Guide

このファイルは、Codex を含む自動編集エージェント向けの作業指針です。
リポジトリの設計意図を崩さず、変更を小さく安全に進めるための基準をまとめます。
また、`## トラブルシュート` を参照して、同種の問題が発生、または事前に予想される場合は、該当する確認手順や既知の解決策を優先して試してください。

## まず見るもの

- `README.md`
- `.editorconfig`
- `Directory.Build.props`
- `global.json`
- `docs/RoleOfMainProjects.md`
- `docs/SystemFlowBuilder.md`
- `docs/processes/README.md`
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

## 繰り返し同じ問題が発生する時

- リポジトリ固有の問題が再発し、その原因と有効な解決手順を確認できた場合は、`## トラブルシュート` への追記を検討する
- 一度だけ発生した問題、原因を特定できていない問題、一般的な開発知識で解決できる問題は追記しない
- 追記する前に、既存の項目と重複していないか確認する
- トラブルシュートには、症状・確認できた原因・解決手順・確認コマンドを簡潔に残す
- 推測を含む場合は、確定事項として記述せず、未確認であることを明記する
- 一時的な回避策の場合は、その旨と恒久対応が必要かどうかを書く
- 環境依存の問題は、OS・SDK・権限・パスなど、確認できた再発条件を書く
- AGENTS.md の変更は、本来のタスクと関係のある最小限の差分に留める
- 解決しない場合や、原因に確信を持てない場合はユーザーに報告する


## トラブルシュート

### タイトル

- 症状:
- 発生条件:
- 原因:
- 対応:
- 確認:
- 備考:
