<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./../docs/PHILOSOPHY.md) |
[計算モデルとして](./../docs/COMPUTATIONAL_MODEL.md) |
[ライブラリ実装](./../docs/LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./../docs/LIBRARY_IMPLEMENTATION_DETAIL.md) |

---

# Hello Door Lock

`HelloDoor` にロック状態と Lock / Unlock 操作を加えたサンプルです。

- DI で Lock 対応の Operation / Reaction を登録し、既存の `Interaction` と `SystemFlow` を再利用します。
- `DoorState`、`DoorLockState`、`RefEntry<DoorLockCommand>` を Context として構成することで、追加した文脈とその役割を構造的に確認できます。
- Context は意味論的なオブジェクトとして統合・分離できます。System Flow 全体で共有する文脈は `Program` で構成し、相互作用だけで必要な一時的文脈は `ScopedContext` として重ねて使い捨てられます。

この例では、既存の Operation の戻り値型を変えずに、Context を通じて操作の語彙を拡張しています。

---

<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./../docs/PHILOSOPHY.md) |
[計算モデルとして](./../docs/COMPUTATIONAL_MODEL.md) |
[ライブラリの実装](./../docs/LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./../docs/LIBRARY_IMPLEMENTATION_DETAIL.md) |
