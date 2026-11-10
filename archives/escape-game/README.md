# EscapeGameのアーカイブ

保存日: 2026-11-11。完了または後続設計へ置き換えられた検討・移行記録です。
型名、契約、テスト件数、計測値は各記録時点のものです。現行実装の指示には使いません。

| 記録 | アーカイブした理由 |
| --- | --- |
| [利用側改善プラン](./ESCAPE_GAME_APPLICATION_REFACTOR_PLAN.md) | 改修済みで、その後に契約・配置も変更された |
| [AccessとContextの再分類](./ESCAPE_GAME_ACCESS_CONTRACT_REVIEW_PLAN.md) | 再分類・契約分割が完了した |
| [命名整理](./ESCAPE_GAME_NAMING_PLAN.md) | 改名を実施済みで、旧名称の対応表になっている |
| [責務整理の旧プラン](./ESCAPE_GAME_RESPONSIBILITY_NEXT_PLAN.md) | 旧方式の撤回と後続実装への置換を記録している |
| [実装簡素化プラン](./ESCAPE_GAME_SIMPLIFICATION_PLAN.md) | 採否判断と実装が完了し、現在は改修の経緯を伝える資料になっている |
| [過去の改修・検証記録](./IMPLEMENTATION_HISTORY.md) | README・現行ガイドから過去の結果と移行記録を分離した |

現行の案内:

- [サンプルREADME](../../InteractionFlow.Samples.EscapeGame/README.md)
- [ゲーム要件と初版設計](../../docs/specs/TURN_BASED_ESCAPE_GAME.md)
- [現在の配置と依存関係](../../docs/design-notes/ESCAPE_GAME_NAMESPACE_LAYOUT.md)
- [現在の状態共有・同期契約](../../docs/design-notes/ESCAPE_GAME_GAMEPLAY_RULES.md)
- [テストの点検結果](../../docs/design-notes/ESCAPE_GAME_TEST_COVERAGE.md)

旧実装へのリンクで対象が現存しないものは、参照先のパスを履歴として記載します。
ライブラリ全体の性能計測資料や、現在も参照する仕様・契約文書は移動していません。
