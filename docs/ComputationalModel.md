[Readme](../README.md#computational-model)

---

# 計算モデルとしての Interaction Flow アーキテクチャ <a id="computational-model-detail"></a>

## チューリングマシンという原型

計算モデルとしての Interaction Flow アーキテクチャは、**チューリングマシン** に着想を得ています。

チューリングマシンとは、以下の要素から構成される抽象的な計算モデルです。

- 無限に伸びるテープ（データの記録領域）
- テープを読み書きするヘッド
- 現在の状態に応じて振る舞いを決める状態遷移規則

このモデルの本質は、「**読み取り → 状態遷移 → 書き込み**」という最小単位の繰り返しによって、任意の計算を表現できる点にあります。

計算モデルとしての Interaction Flow アーキテクチャは、この構造をソフトウェアアーキテクチャに写像したものです。

---

## User と System の間にある ContextTape

この計算モデルでは、`User` と `System` の間に存在する概念上の共有テープを
`ContextTape` と呼びます。

`ContextTape` は、現在の相互作用に関する状態や状況と、
`User` と `System` の間で与えられる文脈を表します。

`User` と `System` は、このテープを直接操作するのではなく、
Operation と Reaction という方向の異なる機械を通じて読み書きします。

```text
User
  ├─ Operation ── write ──> ContextTape ── read ──> System
  └─ Reaction  <── read ─── ContextTape <── write ─ System
```

- **Operation**

  `User` が `ContextTape` へ書き、`System` が読み取るための機械

- **Reaction**

  `System` が `ContextTape` へ書き、`User` が読み取るための機械

`ContextTape` は計算モデル上の概念であり、特定のデータ構造や単一の共有メモリを意味しません。
文脈は、UI、`User` の認識、入出力、`System` 内部などに分散して存在します。

ライブラリの `IFlowContext` は、そのうち `System` が次の Interaction のために保持する文脈を表現したものです。

```text
ContextTape
  ├─ User 側で保持される文脈（UI 状態や User 自身の状態）
  ├─ User → System の文脈（IOperationPort の戻り値）
  ├─ System → User の文脈（IReactionPort の引数）
  └─ System 側で保持される文脈（IFlowContext）
```

---

## システム全体のテープ構成と Function の役割

チューリングマシンの視点に立つと、システム全体の構成には、`ContextTape` に加えて、
記録用テープ、その他の外部テープ、Domain 内部のテープが含まれます。
これらのテープは、アーキテクチャ上の意味と目的によって分類されています。

チューリングマシンとしてのシステムの機能は、
「どのテープに対して、どのような読み書きを行うか」によって、以下のように整理できます。

| テープの役割 | 書き手 | 読み手 | 対応する機能または遷移規則 |
| --- | --- | --- | --- |
| `ContextTape` | `User` | `System` | Operation |
| `ContextTape` | `System` | `User` | Reaction |
| 記録用テープ | `System` | `System` | Storage |
| その他の外部テープ | `System` または外部環境 | 外部環境または `System` | Silent External |
| Domain 内部のテープ | `System` 内部 | `System` 内部 | Domain の計算・遷移規則 |

ここでの分類は、テープの物理的な配置とは対応しません。
同じく `System` の外部にあるテープに対応する機能であっても、
記録を目的として使われる DB やファイルシステムは Storage に分類され、
連携を目的として使われる外部イベントや外部プロパティ は Silent External に分類されます。

特に Silent External は、「`User` との相互作用や記録に関与しない外部テープに対応する機能」です。
この分類は、実質的に Operation、Reaction、Storage のいずれにも含まれない機能を受け入れるために導かれた、意図的な「その他」の分類です。

また、ソフトウェアとして実装される Domain の処理は、内部に閉じた計算可能な処理であると仮定します。
この仮定のもとでは、Domain の処理も原理的にチューリングマシンのテープ、状態、遷移規則として表現できます。

なお、この計算モデルでシステムの実行を進めるには、これらのテープに加えて、
System Flow の評価を制御する `FlowState` が必要です。
`FlowState` はテープではなく、Interaction の選択を制御する有限状態として扱います。
その実現に補助的な記憶が必要な場合もありますが、具体的な構造はこのモデルでは規定しません。

---

## 計算対象となる構成状態

この計算モデルでは、Interaction による状態遷移の対象全体を **構成状態** として扱います。
構成状態には、次の要素が含まれます。

- System Flow の評価位置
- `ContextTape` の状態
- Domain の状態
- 記録用テープや外部環境を含む、その他のテープの状態

時点 `n` の構成状態を `Cₙ` とすると、次のように表せます。

```text
Cₙ = (FlowStateₙ, ContextTapeₙ, DomainStateₙ, OtherTapesₙ)
```

`FlowStateₙ` は、System Flow のどこを評価しているか、次に評価されうる Interaction は
どれかを表す計算モデル上の状態です。`IFlowContext` に保持される特定の値を意味しません。

ここでいう構成状態は、単一のデータ構造や、ライブラリが一括して保持する値ではありません。
計算モデル上、状態遷移の前後を比較するために、分散した状態をまとめて表したものです。

以下では、Interaction を構成状態に作用する **状態遷移演算子**、
System Flow を一つ以上の Interaction から構成される **合成式** として説明します。
「演算子」と「式」は、状態遷移の単位とその組み合わせを区別するための用語であり、
チューリングマシンの構成要素との一対一の対応を主張するものではありません。

---

## 状態遷移演算子としての Interaction

この計算モデルにおいて、Interaction は構成状態に作用する、アーキテクチャ上の最小の
**状態遷移演算子** です。Interaction `I` による遷移は、次のように表せます。

```text
Cₙ ──I──> Cₙ₊₁
```

これは、Interaction が現在の構成状態のうち必要なテープを読み取り、
Domain の遷移規則と必要な Function を組み合わせて、
次の構成状態へ進めることを表します。

Interaction は、Operation、Reaction、Storage、Silent External のすべてを
常に含む必要はありません。また、この表記は計算モデル上の抽象であり、
Interaction の実装に純粋性や決定性が保証されることを意味しません。

Interaction の実行は、チューリングマシンにおける「遷移規則の適用」に相当します。
ただし、Interaction は単独の Function や一回のテープ操作ではなく、
必要なテープ操作と Domain の遷移規則を一つの状態遷移としてまとめた演算子です。

したがって、計算モデル上、Interaction の定義は一つの遷移規則、
その実行は一回の状態遷移として捉えられます。
ただし、一つの Interaction は内部に複数のテープ操作や Domain の計算を含みうるため、
チューリングマシンの原始的な遷移をまとめた **マクロ遷移** に相当します。

---

## 合成式としての System Flow

System Flow は、一つ以上の Interaction から構成される **状態遷移の合成式** です。
Interaction `I₁`、`I₂`、`I₃` を順に実行する System Flow `F` は、
概念上、次のように表せます。

```text
F = I₁ ; I₂ ; I₃
```

ここで `;` は Interaction の実行順序を表します。System Flow に分岐や反復が含まれる場合も、
現在の構成状態に応じて、どの Interaction をどの順序で評価するかを定める式として捉えます。

System Flow は、システム全体で発生するすべての処理を表す式ではありません。
`Context Loop` の一環として `System` が実行する Interaction の組み合わせを、
一つの評価可能な式として表します。

System Flow の定義は合成式であり、その式が表す計算上の意味は、
初期の構成状態を終了時の構成状態へ進める、合成された状態遷移演算子です。

### 遷移関数・遷移表との対応

System Flow を一つの状態遷移ずつ評価する場合、System Flow は現在の構成状態に応じて、
次に評価する Interaction を選択します。System Flow `F` による選択を
遷移関数 `δ_F` とすると、次のように表せます。

```text
δ_F(Cₙ) = Iₖ
Cₙ ──Iₖ──> Cₙ₊₁
```

直列に並ぶ式では評価位置に応じて次の Interaction が選ばれ、
分岐や反復を含む式では、評価位置に加えて `ContextTape`、Domain、
その他のテープの状態も選択に影響します。

この遷移関数を「現在の構成状態と適用する Interaction の対応」として
表形式に正規化したものが、チューリングマシンにおける **遷移表** に相当します。

したがって、System Flow は、記述上は Interaction の **合成式**、
逐次評価上は Interaction を選択する **遷移関数**、
その遷移関数を表形式に正規化した場合は **遷移表** として捉えられます。

---

## 合成式の評価と Context Loop

System Flow `F = I₁ ; I₂ ; I₃` を構成状態 `C₀` に対して評価すると、
構成状態は各 Interaction を通じて順に遷移します。

```text
C₀ ──I₁──> C₁ ──I₂──> C₂ ──I₃──> C₃
```

構造と実行の関係は、次のように整理できます。

```text
System Flow: Interaction の合成と選択を定める式
  └─ Interaction: 構成状態に作用する状態遷移演算子
       └─ Function と Domain の遷移規則: テープの読み書きと内部計算
```

System Flow は Interaction の合成式として `Context Loop` を表現し、
その評価によって構成状態を順に遷移させます。
各 Interaction の評価結果が、次の Interaction の入力となります。
このように、System Flow の遷移関数による選択と Interaction の実行を繰り返す過程が、
計算モデル上の `Context Loop` に相当します。

---

## 理想的な Interaction と状態変化

状態遷移では、Domain、記録用テープ、その他のテープの値が実際には変化しない場合もあります。
一方、計算モデル上の理想的な Interaction は、必ず Reaction で終了します。
理想的な Reaction は `System` から `User` へ向けた `ContextTape` への書き込みを行うため、
理想が守られている場合、Interaction の評価によって `ContextTape` が更新され、
構成状態は必ず変化します。

たとえば、すでに開いているドアに対して「ドアはすでに開いている」と反応する場合、
ドアの状態や `IFlowContext` の値は変化しないことがあります。
それでも、`User` の操作に対して `System` が判断し、その Reaction を `User` が
観測することで、`User` と `System` の間に新たな相互作用が成立します。
この関係の変化を `ContextTape` の状態遷移として扱うため、構成状態は変化します。

```text
現在の構成状態から必要なテープを読み取る
    ↓
Interaction を評価する
    ↓
Reaction によって ContextTape へ書き込む
    ↓
ContextTape が次の状態へ進む
```

この状態遷移は、過去の相互作用を物理的な追記ログとして保持することを要求しません。
Reaction の前後で `User` と `System` の関係が異なることを、
計算モデル上の `ContextTape` の状態遷移として扱います。

現在のライブラリは、Interaction の終了を `ReactionEnd` として表現しますが、
Reaction が `User` に向けた書き込みを実際に行い、それが `User` に観測されたことまでは保証しません。
この節で述べる理想的な Interaction は計算モデル上の規範であり、
実装では設計、Port の実装、Analyzer、コードレビューによって維持します。

---

## マルチテープ構成とチューリング完全性

ここまでに説明したテープは、役割ごとに分離されたマルチテープ構成として捉えられます。
`ContextTape` は `User` と `System` の相互作用を担い、Domain 内部のテープや記録用テープは、
`System` 内部の計算や永続化を担います。

この節では、このマルチテープ構成がチューリング完全となる十分条件と、
その条件が `ContextTape` および `IFlowContext` に与える意味を示します。
これは計算モデル上の条件付きの主張であり、現在のライブラリ実装が
物理的に無限の記憶領域を提供するという意味ではありません。

### 十分条件

次の条件を仮定します。

- テープの本数は有限である
- 各テープは有限の記号集合を持ち、独立した読み取り、書き込み、位置移動ができる
- `ContextTape` 以外の少なくとも一つの計算テープは、理論上無限に拡張できる
- `FlowState` は有限個の制御状態を持つ
- System Flow は、現在の構成状態から次の Interaction を選択し、停止状態まで評価を反復できる
- Interaction は、必要なテープと `FlowState` を一回のマクロ遷移として更新できる

System Flow と Interaction に関する条件は、前段で説明した遷移関数と状態遷移演算子の役割を、
停止状態まで反復できる形で明示したものです。計算能力に関して新たに置く中心的な仮定は、
`ContextTape` 以外に少なくとも一つの無限拡張可能な計算テープが存在することです。

### チューリングマシンの模倣

無限拡張可能な計算テープの一つを `InfiniteTape` とし、
任意のチューリングマシンのテープ内容とヘッド位置を表現します。
チューリングマシンの状態を `FlowState`、遷移表を System Flow、
遷移表の各規則を Interaction に対応させます。

| チューリングマシン | この計算モデル |
| --- | --- |
| テープとヘッド位置 | `InfiniteTape` |
| 現在の機械状態 | `FlowState` |
| 遷移表 | System Flow |
| 遷移表の一つの規則 | Interaction の定義 |
| 一回の遷移 | Interaction の実行 |
| 停止状態 | System Flow の終了条件 |

チューリングマシンの遷移

```text
δ(q, a) = (q', b, direction)
```

に対して、対応する Interaction は、現在位置の記号 `a` を読み取り、`b` を書き込み、
位置を `direction` へ移動し、`FlowState` を `q'` へ更新します。
System Flow は、現在の `FlowState` と読み取った記号から、この Interaction を選択します。

構成状態から模倣対象の機械状態と `InfiniteTape` だけを取り出す射影を `π` とすると、
各 Interaction `I` について、次の対応を構成できます。

```text
π(I(Cₙ)) = δ(π(Cₙ))
```

初期状態が一致し、一回の Interaction が一回の遷移を模倣するため、
Interaction の実行回数に関する帰納法によって、任意の有限ステップ後の状態が一致します。
したがって、次の条件付きの結論が得られます。

> 有限本の独立したテープを持ち、`ContextTape` 以外の少なくとも一つが無限拡張可能で、
> System Flow が Interaction の選択と実行を停止状態まで反復できるなら、
> 計算モデルとしての Interaction Flow アーキテクチャは任意のチューリングマシンを模倣でき、
> チューリング完全です。

この模倣では Silent External を必要としません。外部環境を計算能力に含める場合に、
通常のチューリングマシンとの等価性まで主張するには、その外部環境も計算可能であり、
任意の問題へ答えるオラクルとして振る舞わないことが必要です。

### ContextTape と IFlowContext への帰結

この模倣が依存する無限テープは、Domain 内部のテープ、
または Storage が読み書きする記録用テープです。
したがって、チューリング完全性のために `ContextTape` の無限性を仮定する必要はありません。

`ContextTape` は、現在の相互作用に必要な文脈を扱う有限の窓として構成できます。
長い入力は Operation を通じて計算テープへ逐次移し、長い出力は Reaction を通じて
逐次提示できます。また、過去の相互作用を完全な追記ログとして保持する必要はなく、
その結果を次の相互作用に必要な文脈へ要約できます。

Reaction による `ContextTape` の更新は、模倣対象の計算状態を取り出す射影 `π` から
除外されます。そのため、理想的な Interaction が Reaction で終了して
`ContextTape` を更新することと、`ContextTape` 以外の計算テープ上で
チューリングマシンを模倣することは両立します。

`IFlowContext` は `ContextTape` のうち `System` 側で必要な文脈を表す実装上の投影であり、
Domain や Storage の計算状態をすべて保持する必要はありません。
具体的には、現在の Operation の解釈、次の Interaction の選択と評価、
適切な Reaction の生成、次の相互作用への文脈の引き継ぎに必要な情報だけを保持します。
このことは、`IFlowContext` の契約を小さく保ち、無制限な計算記憶の責務を
Domain や Storage から移さないことの根拠になります。

ただし、これは個々の `IFlowContext` 実装が使用するメモリ量に上限があることを
数学的に保証するものではありません。ここで得られるのは、`IFlowContext` の契約に
無限テープとしての能力を要求する必要がない、という責務上の結論です。

---

## まとめ

計算モデルとしての Interaction Flow アーキテクチャは、チューリングマシンの

- テープ
- 読み書き
- 状態遷移

という最小構造を、現代的なソフトウェア設計に再解釈したものです。

その特徴は以下に集約されます。

- `User` と `System` の間の文脈を `ContextTape` として捉える
- Operation と Reaction を、`ContextTape` に対する方向の異なる読み書きとして捉える
- Domain 内部のテープ、Storage が扱う記録用テープ、`ContextTape` などを、役割の異なるテープとして捉える
- Domain 内部を含む計算可能な処理を、テープ操作と遷移規則として統一的に捉える
- Interaction を、構成状態に作用するアーキテクチャ上の最小の状態遷移演算子であり、マクロ遷移として捉える
- System Flow を、記述上は合成式、逐次評価上は遷移関数として捉える
- System Flow の遷移関数を表形式に正規化したものを、遷移表として捉える
- System Flow の評価によって構成状態を順に遷移させ、`Context Loop` を進行させる
- `ContextTape` 以外の少なくとも一つの計算テープに無限性を仮定することで、任意のチューリングマシンを模倣する
- チューリング完全性を `ContextTape` の無限性に依存させず、`IFlowContext` の契約を小さく保つ

これにより、役割の異なるテープ、状態遷移演算子、その合成式、遷移関数、式の評価を
一貫した計算モデルとして扱うことができます。

---

[Readme](../README.md#computational-model) | [PageTop](#computational-model-detail)
