# Hello Door

最小の Context Loop を表す Console サンプルです。

- `DoorState` が現在の開閉状態を Context として保持します。
- `OperateDoor` は User の操作を受け、Reaction によって状態更新と観測可能な反応を行います。
- `DoorSystemFlow` はこの一つの Interaction を繰り返し、Context が次の反応を変える流れを表します。

Interaction Flow を初めて読むときの入口です。
