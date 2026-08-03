<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../../README.md) |
[Philosophy](./../PHILOSOPHY.md) |
[計算モデルとして](./../COMPUTATIONAL_MODEL.md) |
[ライブラリ実装](./../LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./../LIBRARY_IMPLEMENTATION_DETAIL.md) |

---

# packages-YYYY.MM.DD

InteractionFlow の NuGet パッケージ公開リリースです。

## 公開パッケージ

| Package | Version | 概要 |
|---|---:|---|
| **[InteractionFlow.Core](https://www.nuget.org/packages/InteractionFlow.Core)** | `x.y.z` | 更新内容 |
| **[InteractionFlow.Standard](https://www.nuget.org/packages/InteractionFlow.Standard)** | `x.y.z` | 更新内容 |
| **[InteractionFlow.Analyzers](https://www.nuget.org/packages/InteractionFlow.Analyzers)** | `x.y.z` | 更新内容 |

## 主な変更

- 変更点を記載
- 変更点を記載
- 変更点を記載

## 互換性

- `InteractionFlow.Core`: `netstandard2.1`
- `InteractionFlow.Standard`: `netstandard2.1`
- `InteractionFlow.Analyzers`: `netstandard2.0`

## 導入

通常は `InteractionFlow.Standard` から導入してください。
（`InteractionFlow.Standard` は `InteractionFlow.Core` に依存し、NuGetによって自動で解決されます。）

```bash
dotnet add package InteractionFlow.Standard
```

設計ルールの検査も利用する場合は Analyzer を追加してください。

```bash
dotnet add package InteractionFlow.Analyzers
```

Analyzer は開発時のみ利用するため、`.csproj` では `PrivateAssets="all"` の指定を推奨します。

```xml
<PackageReference Include="InteractionFlow.Analyzers" Version="x.y.z" PrivateAssets="all" />
```

## 確認項目

- [ ] `InteractionFlow.Core` の NuGet ページを確認
- [ ] `InteractionFlow.Standard` の NuGet ページを確認
- [ ] `InteractionFlow.Analyzers` の NuGet ページを確認
- [ ] GitHub Actions の `Package Install Check` ワークフローを手動で実行
- [ ] `Package Install Check`: 参照バージョンが最新かつ Release Note と一致しているかを確認
- [ ] `Package Install Check`: `InteractionFlowArchitecture001` を無視してビルドに問題がないかを確認
- [ ] `Package Install Check`: Analyzer が `InteractionFlowArchitecture001` を検出するかを確認

## 補足

このリリースタグはパッケージ公開バッチを表します。
各パッケージのバージョンは `Directory.Build.props` の `CoreVersion`、`StandardVersion`、`AnalyzerVersion` で個別に管理します。
このプロジェクトは現在 0.x 系です。1.0 に到達するまでは公開 API が変更される可能性があります。

---

<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../../README.md) |
[Philosophy](./../PHILOSOPHY.md) |
[計算モデルとして](./../COMPUTATIONAL_MODEL.md) |
[ライブラリの実装](./../LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./../LIBRARY_IMPLEMENTATION_DETAIL.md) |
