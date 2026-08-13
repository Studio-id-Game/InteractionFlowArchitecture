<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./../docs/PHILOSOPHY.md) |
[計算モデルとして](./../docs/COMPUTATIONAL_MODEL.md) |
[ライブラリ実装](./../docs/LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./../docs/LIBRARY_IMPLEMENTATION_DETAIL.md) |

---

# Notepad

`Notepad.Core` の System Flow を、Console と通常のファイル保存で実行するサンプルです。

- `Program` で Console、Storage、Persistence、シリアライザを DI 登録します。
- `NotepadDataSimpleSerializer` は Note のタイトルと本文を単純なテキスト形式へ変換します。
- Core の Interaction と `MainLoop` を変えずに、通常版の外部実装を構成しています。

Flow の設計と、実行環境の実装を DI で分ける基本例です。

---

<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./../docs/PHILOSOPHY.md) |
[計算モデルとして](./../docs/COMPUTATIONAL_MODEL.md) |
[ライブラリの実装](./../docs/LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./../docs/LIBRARY_IMPLEMENTATION_DETAIL.md) |
