# Interaction Flow Architecture for C#

Interaction Flow Architecture を C# / .NET で実装するためのライブラリです。
対話的な UI、ゲームループ、エージェントなどを、User と System の間で続く相互作用と文脈の循環として設計します。

> Interaction shapes Context, and Context shapes Interaction.

## インストール

通常は、Core API と標準実装を利用できる `InteractionFlow.Standard` から始めます。
設計ルールを継続的に検査する場合は `InteractionFlow.Analyzers` も追加してください。

```bash
dotnet add package InteractionFlow.Standard --version 0.5.0
dotnet add package InteractionFlow.Analyzers --version 0.5.0
```

プロジェクトファイルでは、Analyzer を実行時の依存関係に含めないよう
`PrivateAssets="all"` を指定することを推奨します。

```xml
<ItemGroup>
  <PackageReference Include="InteractionFlow.Standard" Version="0.5.0" />
  <PackageReference Include="InteractionFlow.Analyzers" Version="0.5.0" PrivateAssets="all" />
</ItemGroup>
```

アーキテクチャ概念だけを使い、標準実装を必要としない場合は、
`InteractionFlow.Core` を直接インストールできます。

```bash
dotnet add package InteractionFlow.Core --version 0.5.0
```

`InteractionFlow.Core` と `InteractionFlow.Standard` の Target Framework は `netstandard2.1`、
`InteractionFlow.Analyzers` は `netstandard2.0` です。

## パッケージ

- `InteractionFlow.Core`
  - アーキテクチャ概念を、副作用や特定の利用形態に依存しない構造と振る舞いとして定義します。
  - `IFlowContext`、`Interaction`、`SystemFlow`、Function Port などの基盤 API を提供します。
- `InteractionFlow.Standard`
  - Core API を現実のユースケースで扱いやすくする標準パッケージです。
  - System Flow Builder、Console、ファイルシステム、Serialization などの API と実装を提供します。
- `InteractionFlow.Analyzers`
  - namespace に基づくレイヤー間依存と、依存ノードの宣言規則を検査する Roslyn Analyzer です。

## アーキテクチャ

Interaction Flow Architecture は、相互作用によって Context が更新され、
新しい Context が次の相互作用を形作る時間的な過程を `Context Loop` として捉えます。

```text
Context -> Interaction -> next Context -> next Interaction -> ...
```

- `Context`: User と System が共有する、現在の相互作用に関する状態や状況
- `Context Loop`: Context が移り変わり続ける過程と、User と System の関係の歴史
- `System Flow`: 一つ以上の Interaction を通じて、System が User との関係を構築する単位
- `Interaction`: System が内部の目的を達成するための相互作用の単位
- `Function Port`: Interaction から見える機能の契約
- `Function External`: Function Port を UI、DB、ファイルシステム、外部サービスなどへ接続する実装

Function は目的に応じて分類します。

- `Operation`: User による操作や入力を受け取る
- `Reaction`: User が観測できる System の反応を提供する
- `Storage`: Context の文脈的な意味とは独立してデータを記録する
- `Silent External`: User との相互作用や記録を目的とせず、外部環境と連携する

主な依存方向は次のとおりです。Interaction は具体的な実行環境ではなく Function Port に依存するため、
UI や保存先などを差し替えられます。

```text
SystemFlow -> Interaction -> Function Port <- Function External
```

`IFlowContext` は、概念上の Context のうち System 側で扱う文脈を提供する実装上の投影です。
Context や Context Loop そのものを表す型ではありません。一つの `IFlowContext` インスタンスを継続利用することは、
Context Loop を実現する代表的な構成です。

## Analyzer の有効化

Analyzer は既定では診断を行いません。プロジェクトの `.editorconfig` で有効にします。

```ini
[*.cs]
interactionflow_enabled = True
interactionflow_mode = Error
```

`interactionflow_mode` を省略した場合の重大度は `Warning` です。
外部 namespace を追加で許可する場合は、カンマ区切りで指定できます。

```ini
interactionflow_allowed_roots = Microsoft, YourCompany
```

## はじめる

最小構成の [HelloDoor サンプル](https://github.com/Studio-id-Game/InteractionFlowArchitecture/tree/main/InteractionFlow.Samples.HelloDoor) では、
Operation と Reaction を Console 実装へ接続し、Interaction を System Flow Builder で組み立て、
`IFlowContext` とともに SystemFlow を実行する一連の流れを確認できます。

用途別のサンプルもリポジトリに含まれています。

- [HelloDoor.Lock](https://github.com/Studio-id-Game/InteractionFlowArchitecture/tree/main/InteractionFlow.Samples.HelloDoor.Lock): Context と操作語彙の拡張
- [Parrot](https://github.com/Studio-id-Game/InteractionFlowArchitecture/tree/main/InteractionFlow.Samples.Parrot): 複数の Interaction と SystemFlow の選択・合成
- [Notepad.Core](https://github.com/Studio-id-Game/InteractionFlowArchitecture/tree/main/InteractionFlow.Samples.Notepad.Core): ユーザー体験と実行環境の分離
- [Notepad](https://github.com/Studio-id-Game/InteractionFlowArchitecture/tree/main/InteractionFlow.Samples.Notepad): Console とファイル保存への接続
- [Notepad.Secure](https://github.com/Studio-id-Game/InteractionFlowArchitecture/tree/main/InteractionFlow.Samples.Notepad.Secure): Port 実装と Interaction の差し替え

## ドキュメント

- [README / Getting Started](https://github.com/Studio-id-Game/InteractionFlowArchitecture#readme)
- [Philosophy](https://github.com/Studio-id-Game/InteractionFlowArchitecture/blob/main/docs/PHILOSOPHY.md)
- [計算モデルとして](https://github.com/Studio-id-Game/InteractionFlowArchitecture/blob/main/docs/COMPUTATIONAL_MODEL.md)
- [ライブラリの実装](https://github.com/Studio-id-Game/InteractionFlowArchitecture/blob/main/docs/LIBRARY_IMPLEMENTATION.md)
- [ライブラリ実装の詳細](https://github.com/Studio-id-Game/InteractionFlowArchitecture/blob/main/docs/LIBRARY_IMPLEMENTATION_DETAIL.md)
- [System Flow Builder の設計](https://github.com/Studio-id-Game/InteractionFlowArchitecture/blob/main/docs/SYSTEM_FLOW_BUILDER.md)
- [Analyzer の詳細](https://github.com/Studio-id-Game/InteractionFlowArchitecture/blob/main/InteractionFlow.Analyzers/README.md)

Repository: [Studio-id-Game/InteractionFlowArchitecture](https://github.com/Studio-id-Game/InteractionFlowArchitecture)
