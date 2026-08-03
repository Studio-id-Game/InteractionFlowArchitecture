<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./../docs/PHILOSOPHY.md) |
[計算モデルとして](./../docs/COMPUTATIONAL_MODEL.md) |
[ライブラリ実装](./../docs/LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./../docs/LIBRARY_IMPLEMENTATION_DETAIL.md) |

---

# Notepad Core

Notepad の Interaction と System Flow を、UI や具体的な保存形式から分離して定義する中核プロジェクトです。

- `NotepadContext` がログイン User と現在の Note を、次の相互作用に必要な Context として保持します。
- Login、一覧、作成、編集、削除を個別の Interaction とし、`MainLoop` がそれらをユーザー操作の流れとして合成します。
- Storage Port と Persistence Port により、ノート操作からファイルやシリアライズの具体的な実装を分離します。

通常版と Secure 版は、このプロジェクトの Flow を共有し、DI で実装を選びます。

---

<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./../docs/PHILOSOPHY.md) |
[計算モデルとして](./../docs/COMPUTATIONAL_MODEL.md) |
[ライブラリの実装](./../docs/LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./../docs/LIBRARY_IMPLEMENTATION_DETAIL.md) |
