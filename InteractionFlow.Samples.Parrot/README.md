# Parrot

複数の Interaction を選択・合成する Console サンプルです。

- `InitializeApplication` と `SelectAndRunSample` が、初期化とサンプル選択・実行を別の System Flow として表します。
- `ScopedFlowContext` と `RefEntity` により、選択中のサンプルや終了状態をその実行範囲に限定します。
- Console の状態、Cancellation、Storage、ネストした Interaction を組み合わせ、同じ Flow 構造で振る舞いを切り替えます。

Context の局所化と、System Flow によるユーザー体験の合成を読むためのサンプルです。
