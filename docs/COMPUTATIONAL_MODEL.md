[Readme](../README.md#computational-model)

---

# 計算モデルとしての Interaction Flow Architecture <a id="computational-model-detail"></a>

## チューリングマシンという原型

計算モデルとしての Interaction Flow Architecture は、**チューリングマシン** に着想を得ています。

チューリングマシンとは、以下の要素から構成される抽象的な計算モデルです。

- 無限に伸びるテープ（データの記録領域）
- テープを読み書きするヘッド
- 現在の状態に応じて振る舞いを決める状態遷移規則

このモデルの本質は、「**読み取り → 状態遷移 → 書き込み**」という最小単位の繰り返しによって、任意の計算を表現できる点にあります。

計算モデルとしての Interaction Flow Architecture は、この構造をソフトウェアアーキテクチャに写像したものです。

---

## User と System の間にある ContextTape

この計算モデルでは、Interaction Flow Architecture における `Context` を、
`User` と `System` の間に存在する概念上の共有テープとして抽象化し、
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

一方、ライブラリの `IFlowContext` は、アーキテクチャ上の `Context` を
プログラムで扱うためのインターフェースです。
`IFlowContext` が提供する文脈は `ContextTape` の一部ですが、それを直接実装するデータ構造ではありません。

```text
ContextTape
  ├─ User 側で保持される文脈（UI 状態や User 自身の状態）
  ├─ User から System へ伝わる文脈（IOperationPort の戻り値）
  ├─ System から User へ伝わる文脈（IReactionPort の引数）
  └─ System 側で保持される文脈（IFlowContext が提供する文脈）
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
| `ContextTape` | `User` <br/> `System` | `System` <br/> `User` | Operation <br/> Reaction |
| 記録用テープ | `System` | `System` | Storage |
| その他の外部テープ | 外部環境 <br/> `System` | `System` <br/> 外部環境 | Silent External |
| Domain 内部のテープ | `System` | `System` | Domain の計算・遷移規則 |

ここでの分類は、テープの物理的な配置とは対応しません。
同じく `System` の外部にあるテープに対応する機能であっても、
記録を目的として使われる DB やファイルシステムは Storage に分類され、
連携を目的として使われる外部イベントや外部プロパティ は Silent External に分類されます。

特に Silent External は、「`User` との相互作用や記録に関与しない外部テープに対応する機能」です。
Operation、Reaction、Storage のいずれにも含まれない残余的な範囲を持ちますが、
「外部環境との連携を目的とする」という明示的な基準で定められた分類です。

また、ソフトウェアとして実装される Domain の処理は、内部に閉じた計算可能な処理であると仮定します。
この仮定のもとでは、Domain の処理も原理的にチューリングマシンのテープ、状態、遷移規則として表現できます。

なお、この計算モデルでシステムの実行を進めるには、これらのテープに加えて、
System Flow の評価を制御する `FlowState` が必要です。
`FlowState` はテープではなく、Interaction の選択を制御する有限状態として扱います。
その実現に補助的な記憶が必要な場合もありますが、具体的な構造はこのモデルでは規定しません。

また、Function はテープを読み書きする機械として、読み書きの振る舞いを実現するための
内部状態を持つことができます。Operation と Reaction では、相互作用の翻訳や加工に使う設定、
スムージングの履歴、エフェクトの進行状態などがこれに当たります。
Storage や Silent External では、外部機能の設定やキャッシュなどが含まれます。
このモデルでは、実行時に変化しうるこれらの内部状態をまとめて `FunctionState` と呼びます。

`FunctionState` はテープの内容そのものでも、System Flow の評価を制御する `FlowState` でもありません。
また、Function の固定された変換規則は Function の定義に属し、`FunctionState` には含めません。
この区別は物理的な配置ではなく計算上の役割に基づきます。Function の実装内部に置かれた記憶でも、
独立した読み書きの対象として理論上無制限に拡張する場合は、`FunctionState` ではなく、
その目的に応じた計算テープとして扱います。

---

## 計算対象となる構成状態

この計算モデルでは、Interaction による状態遷移の対象全体を **構成状態** として扱います。
構成状態には、次の要素が含まれます。

- System Flow の評価を制御する有限状態（`FlowState`）
- `ContextTape` の状態
- Function が保持する内部状態（`FunctionState`）
- Domain の状態
- 記録用テープや外部環境を含む、その他のテープの状態

時点 `n` の構成状態を `Cₙ` とすると、次のように表せます。

```text
Cₙ = (FlowStateₙ, ContextTapeₙ, FunctionStateₙ, DomainStateₙ, OtherTapesₙ)
```

`FlowStateₙ` は、System Flow の評価と Interaction の選択を制御する有限状態です。
具体的な評価位置の表現方法を規定するものではなく、
`IFlowContext` に保持される特定の値を意味するものでもありません。

`FunctionStateₙ` は、時点 `n` に存在する Function インスタンスが保持する補助的な内部状態を
まとめたものです。
個々の Function が状態を持たない場合、その Function に対応する成分は空または一定として扱えます。
Operation や Reaction の戻り値や引数は `ContextTapeₙ` に現れる文脈である一方、
それらを翻訳・加工するために Function が保持する状態は `FunctionStateₙ` に属します。
`FunctionStateₙ` も計算モデル上の集合的な表現であり、単一のライブラリ型やデータ構造を意味しません。

ここでいう構成状態は、単一のデータ構造や、ライブラリが一括して保持する値ではありません。
計算モデル上、状態遷移の前後を比較するために、分散した状態をまとめて表したものです。

以下では、Interaction を構成状態に作用する **状態遷移演算子**、
System Flow を一つ以上の Interaction から構成される **合成式** として説明します。
「演算子」と「式」は、状態遷移の単位とその組み合わせを区別するための用語であり、
チューリングマシンの構成要素との一対一の対応を主張するものではありません。

---

## 状態遷移演算子としての Interaction

この節以降では、`Cₙ` を、次の Interaction の選択と実行結果に影響する
内部状態および外部からの観測を含む、完全な構成状態として扱います。
同じ構成状態に同じ Interaction を適用した場合、遷移先は一意に定まるものと仮定します。

User の入力は `ContextTape`、外部環境から読み取る値は `OtherTapes`、
疑似乱数生成器などの内部状態は、その役割に応じて `FunctionState` や Domain の状態に含まれます。
この仮定は、実装が外部要因から独立していることを要求するものではなく、
遷移に影響する要因を構成状態の外に残さない、という計算モデル上の抽象化です。

この仮定のもとで、Interaction は構成状態に作用する、アーキテクチャ上の最小の
**状態遷移演算子** です。Interaction `I` を適用できる構成状態の集合を `C_I` とすると、
`I` を `C_I` から構成状態の集合 `C` への関数として、次のように表します。

```text
I : C_I → C
I(Cₙ) = Cₙ₊₁
```

これは、Interaction が現在の構成状態のうち必要なテープを読み取り、
必要に応じて `FunctionState` を参照または更新し、
Domain の遷移規則と Function によるテープ操作を組み合わせて、
次の構成状態へ進めることを表します。

Interaction は、Operation、Reaction、Storage、Silent External のすべてを
常に含む必要はありません。どの Function と Domain の規則を含む場合でも、
それらが参照する状態を `Cₙ` に含めることで、Interaction 全体を一つの関数として扱います。

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
評価が停止する初期状態に対して、初期の構成状態を終了時の構成状態へ進める
合成された状態遷移演算子です。評価が停止しない場合は、構成状態の遷移を継続します。

### 選択関数・状態遷移・遷移表との対応

System Flow を一つの状態遷移ずつ評価する場合、停止状態でない各構成状態に対して、
次に評価する Interaction は一意に定まります。System Flow `F` による選択を
選択関数 `σ_F`、System Flow の一段の状態遷移を `τ_F` とすると、
次のように表せます。

```text
σ_F(Cₙ) = Iₖ
Cₙ ∈ C_{Iₖ}
τ_F(Cₙ) = σ_F(Cₙ)(Cₙ) = Iₖ(Cₙ) = Cₙ₊₁
```

直列に並ぶ式では `FlowState` に応じて次の Interaction が選ばれ、
分岐や反復を含む式では、`FlowState` に加えて、System Flow から観測可能な
`ContextTape`、`FunctionState`、Domain、その他のテープの状態も選択に影響しえます。

選択に使う状態を有限個の制御状態と有限の観測記号へ正規化できる場合、
選択関数を「現在の制御状態および観測記号と、適用する Interaction の対応」として
表形式にしたものが、チューリングマシンにおける **遷移表** に相当します。
一方、構成状態全体を直接選択の入力とする一般の System Flow では、
その対応表が有限になるとは限りません。

したがって、System Flow は、記述上は Interaction の **合成式**、
逐次評価上は Interaction を選ぶ **選択関数** と、選ばれた Interaction を適用する
**状態遷移** として捉えられます。さらに、選択に使う状態と記号を有限に正規化できる場合は、
その選択関数を **遷移表** として表現できます。

---

## 合成式の評価と Context Loop

System Flow `F = I₁ ; I₂ ; I₃` を構成状態 `C₀` に対して評価すると、
構成状態は各 Interaction を通じて一意に遷移します。

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
初期状態 `C₀` が定まれば、停止状態に達するまでの構成状態の列も一意に定まります。
停止状態に達しない場合、この列は無限に続きます。
このように、System Flow の選択関数による選択と Interaction の実行を
決定的に繰り返す過程が、計算モデル上の `Context Loop` に相当します。

---

## 理想的な Interaction と状態変化

状態遷移では、`FunctionState`、Domain、記録用テープ、その他のテープの値が
実際には変化しない場合もあります。
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
`FunctionState` は独立したテープではなく、これらのテープを読み書きする Function が
翻訳や加工などの振る舞いを実現するために保持する内部状態です。

この節では、このマルチテープ構成がチューリング完全となる十分条件と、
その条件が `ContextTape`、`FunctionState`、`IFlowContext` に与える意味を示します。
これは計算モデル上の条件付きの主張であり、現在のライブラリ実装が
物理的に無限の記憶領域を提供するという意味ではありません。

### 十分条件

次の条件を仮定します。

- テープの本数は有限である
- 各テープは有限の記号集合を持ち、独立した読み取り、書き込み、位置移動ができる
- `ContextTape` 以外の少なくとも一つの計算テープは、理論上無限に拡張できる
- `FlowState` は有限個の制御状態を持つ
- System Flow の記述と、それが参照する Interaction の定義および Function の数は有限である
- `FunctionState` として扱う各内部状態は、有限個の状態のいずれかとして表現できる
- Function が無制限に拡張しうる記憶を利用する場合、その記憶は `FunctionState` に隠さず、役割に応じた計算テープとして表現できる
- System Flow の選択と、Interaction を構成する Function および Domain の状態遷移は計算可能である
- 停止状態でない各構成状態に対して、System Flow は適用可能な次の Interaction を一意に選択できる
- 各 Interaction は、必要なテープ、`FlowState`、`FunctionState` を一回のマクロ遷移として一意に更新できる

停止状態に達した場合、System Flow の評価は終了します。
到達しない場合は、一意に定まる Interaction の選択と実行を繰り返し、
構成状態の無限列を生成します。

計算能力に関して中心となる仮定は、`ContextTape` 以外に
少なくとも一つの無限拡張可能な計算テープが存在することです。
`FunctionState` に無制限な記憶を隠さず計算テープとして表現する条件は、
Function の内部状態を追加しても、モデル上の計算記憶の所在を明示できるようにするものです。
これは、個々のライブラリ実装が使用するメモリ量に上限があることを保証するものではなく、
この理論モデルの中で有限制御と計算テープを区別するための条件です。

### チューリングマシンの模倣

無限拡張可能な計算テープの一つを `InfiniteTape` とします。
任意の決定性チューリングマシン `M` に対して、そのテープ内容とヘッド位置を
`InfiniteTape`、現在の機械状態を `FlowState`、遷移表を System Flow、
遷移表の各規則を Interaction に対応させます。

| チューリングマシン | この計算モデル |
| --- | --- |
| テープとヘッド位置 | `InfiniteTape` |
| 現在の機械状態 | `FlowState` |
| 遷移表 | System Flow |
| 遷移表の一つの規則 | Interaction の定義 |
| 一回の遷移 | Interaction の実行 |
| 停止状態 | System Flow の終了条件 |

`FunctionState` は、この模倣に必要なテープ操作を実現する補助状態として使用できます。
ただし、模倣対象のテープ内容やヘッド位置を `FunctionState` に保持することは仮定せず、
それらは `InfiniteTape` 上に表現します。

チューリングマシンの遷移

```text
δ(q, a) = (q', b, direction)
```

に対して、対応する Interaction は、現在位置の記号 `a` を読み取り、`b` を書き込み、
位置を `direction` へ移動し、`FlowState` を `q'` へ更新します。
この操作の過程で補助的な `FunctionState` が更新されても構いませんが、
その更新結果も現在の構成状態から一意に定まります。
System Flow は、現在の `FlowState` と読み取った記号から、
この遷移規則に対応する Interaction を一意に選択します。

構成状態から模倣対象の機械状態と `InfiniteTape` だけを取り出し、
`ContextTape`、`FunctionState`、その他の補助状態を除外する射影を `π` とします。
模倣対象の遷移規則 `δ(q, a)` に対応する Interaction を `I_{q,a}` とすると、
模倣のために構成された各状態について、次の対応を構成できます。
ここで `Step_M` は、模倣対象のチューリングマシンの構成状態を一段進める関数です。

```text
π(τ_F(Cₙ)) = π(I_{q,a}(Cₙ)) = Step_M(π(Cₙ))
```

初期状態の射影を `M` の初期構成と一致させ、一回の Interaction が
`M` の一回の遷移を模倣するように構成します。
Interaction の実行後も上記の対応が保たれるため、
Interaction の実行回数に関する帰納法によって、任意の有限ステップ後の状態が一致します。
`M` が停止状態に達すれば System Flow も終了し、`M` が停止しなければ
System Flow も一意に定まる状態遷移を継続します。
したがって、次の条件付きの結論が得られます。

> 上記の有限性、計算可能性、決定性の条件を満たし、
> `ContextTape` 以外の少なくとも一つのテープが無限拡張可能で、
> System Flow が停止状態に達するまで（到達しない場合は無限に）
> Interaction の選択と実行を反復できるなら、
> 計算モデルとしての Interaction Flow Architecture は任意の決定性チューリングマシンを模倣でき、
> チューリング完全です。

この模倣では Silent External を必要としません。外部環境を計算能力に含める場合に、
通常のチューリングマシンとの等価性まで主張するには、その外部環境も計算可能であり、
任意の問題へ答えるオラクルとして振る舞わないことが必要です。

### ContextTape、FunctionState、IFlowContext への帰結

この模倣が依存する無限テープは、Domain 内部のテープ、
または Storage が読み書きする記録用テープです。
したがって、チューリング完全性のために `ContextTape` の無限性を仮定する必要はありません。
同様に、Operation や Reaction が保持する `FunctionState` に
無制限な計算記憶を担わせる必要もありません。

`ContextTape` は、現在の相互作用に必要な文脈を扱う有限の窓として構成できます。
長い入力は Operation を通じて計算テープへ逐次移し、長い出力は Reaction を通じて
逐次提示できます。また、過去の相互作用を完全な追記ログとして保持する必要はなく、
その結果を次の相互作用に必要な文脈へ要約できます。

射影 `π` が取り出すのは、模倣対象の機械状態に対応する `FlowState` と
`InfiniteTape` の状態だけです。`ContextTape` と `FunctionState` は、この射影には含まれません。

模倣のために構成する System Flow は、`FlowState` と `InfiniteTape` 上の現在の記号から
次の Interaction を選択します。そのため、理想的な Interaction が Reaction で終了し、
`ContextTape` を更新しても、模倣対象の状態遷移には影響しません。
Operation や Reaction の翻訳・加工に伴って `FunctionState` が更新される場合も、
その更新を含む Interaction が上記の射影関係を保つ限り、チューリングマシンの模倣と両立します。

`IFlowContext` は `ContextTape` のうち `System` 側で必要な文脈を表す実装上の投影であり、
`FunctionState` や、Domain および Storage が扱う計算状態をすべて表現する必要はありません。
具体的には、次の Interaction の選択・評価と、次の相互作用への文脈の引き継ぎに必要な情報だけを表現します。
相互作用の翻訳・加工に固有の状態は Operation や Reaction 自身が保持できるため、
その状態を `IFlowContext` に集約する必要はありません。
このことは、`IFlowContext` の契約を小さく保ち、無制限な計算記憶の責務を
Domain や Storage から移さないことの根拠になります。

ただし、これは個々の `IFlowContext` 実装が使用するメモリ量に上限があることを
数学的に保証するものではありません。ここで得られるのは、`IFlowContext` の契約に
無限テープとしての能力を要求する必要がない、という責務上の結論です。

---

## まとめ

計算モデルとしての Interaction Flow Architecture は、チューリングマシンの

- テープ
- 読み書き
- 状態遷移

という最小構造を、現代的なソフトウェア設計に再解釈したものです。

その特徴は以下に集約されます。

### テープ

- `User` と `System` の間の文脈を `ContextTape` として捉える
- Domain 内部のテープ、Storage が扱う記録用テープ、`ContextTape` を、それぞれ役割の異なる計算テープとして捉える
- Silent External を、`User` との相互作用を担わず、記録も目的としない外部テープに対応する機能として捉える

### 読み書き

- Operation と Reaction を、`ContextTape` に対する方向の異なる読み書きとして捉える
- Domain 内部を含む計算可能な処理を、テープ操作と遷移規則として統一的に捉える

### 状態遷移

- `FlowState` を、System Flow の評価と Interaction の選択を制御する有限状態として捉える
- `FunctionState` を、Function がテープの読み書きや相互作用の翻訳・加工を実現するために保持する内部状態として捉える
- Interaction を、構成状態に作用し、遷移先を一意に定める最小の状態遷移演算子（マクロ遷移）として捉える
- System Flow を、記述上は合成式、逐次評価上は選択関数と状態遷移として捉える
- 選択に使う状態と記号を有限に正規化できる場合、その選択関数を遷移表として捉える
- System Flow の評価によって構成状態を順に遷移させ、`Context Loop` を進行させる

### 計算能力

- `ContextTape` 以外の少なくとも一つの計算テープに無限性を仮定することで、任意の決定性チューリングマシンを模倣する

これにより、役割の異なるテープ、状態遷移演算子、その合成式、選択関数、式の評価を、一貫した計算モデルとして扱うことができます。

---

また、この計算モデルから、次の設計および実装上の帰結が得られます。

### Function の分類

テープの役割とアクセス方向に基づいて、Function の分類を明確にする

- テープの物理的な配置を分類基準にしない
- Operation と Reaction の対称性
- Storage と Silent External の違い
- Silent External の残余的な範囲を、外部環境との連携という明示的な基準で定めること

### `IFlowContext` を小さく保つ

`IFlowContext` を可能な限り小さく保ちながら、計算モデルを維持する

- `ContextTape` の実現に必要な状態を単一の `IFlowContext` に集約せず、複数の場所へ分散できる
  - Operation と Reaction が、相互作用に伴う入出力を翻訳・加工するための状態を保持できる
  - `ScopedFlowContext` によって一時的な文脈を重ねられる
- システムの計算能力（チューリング完全性）は `ContextTape` の無限性に依存しない
  - Storage や Domain が、計算能力に必要な計算テープを担う
- `IFlowContext` が System Flow の制御状態を持つ必要はない

### Interaction と System Flow の責務

「状態遷移」と「合成と選択」で責務を分離する

- Interaction は、構成状態に作用する計算モデル上の一つの状態遷移として実装する
- System Flow は、現在の構成状態に応じた Interaction の合成と選択に集中する
- Interaction は Reaction によって完了し、`ContextTape`（`User` との相互作用）を次の状態へ進める

---

[Readme](../README.md#computational-model) | [PageTop](#computational-model-detail)
