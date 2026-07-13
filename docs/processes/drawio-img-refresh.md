# Draw.io Image Refresh Process

このドキュメントは、`docs/img/src` 以下の `.drawio` ファイルを更新したときに、対応する `.context.md` と `.svg` を更新するための作業定義である。

## 対象

対象は `docs/img/src` 以下にある、内容が更新されている `<filename>.drawio` ファイルである。

出力先は次の通り。

- SVG: `docs/img/<filename>.svg`
- Context: `docs/img/<filename>.context.md`

## 手順

### 1. 更新対象を確認する

`docs/img/src` 以下の `.drawio` ファイルのうち、前回状態から内容が変わっているものを対象にする。

Git 管理下であれば、次の観点で確認する。

- `git status --short -- docs/img/src`
- `git diff -- docs/img/src/*.drawio`

Git の比較元がない場合は、ファイル更新時刻、既存の `.svg` / `.context.md` との更新時刻、または作業依頼の対象指定に従う。

### 2. Version チェックを行う

図の内容が更新されている場合、図中の version 表記も更新されていることを簡単に確認する。

確認観点:

- `.drawio` 内に `version` またはそれに相当するラベルがあるか
- 図の内容差分がある場合、その version ラベルにも差分があるか
- version ラベルが存在しない図では、存在しないことを作業結果に明記する

Git 管理下で確認できる場合は、変更前後の `.drawio` から version ラベルを比較する。厳密なセマンティックバージョン検証までは不要である。

図の内容が更新されているのに version が据え置きに見える場合は、この時点でリフレッシュ作業を中止し、`.context.md` と `.svg` は更新しない。作業結果では、version が据え置きであるため中止したことをユーザーに警告する。

### 3. Context を圧縮・更新する

対象の `.drawio` から `docs/img/<filename>.context.md` を作成または更新する。

目的は、図を直接参照できない状況でも同等の意味を再利用できるコンテキストにすることである。

作成時の方針:

- 図タイトル、version、主要コンテナ、Layer、Block、外部要素、状態、フロー、凡例を保持する
- `C1`、`P1` などのラベル付きフローは原則すべて残す
- 色、矢印種別、線種、形状などが意味を持つ場合は凡例として残す
- 既存の `.context.md` がある場合は、全面再生成ではなく差分更新を基本にする
- 既存の `.context.md` に含まれる内容のうち、図の更新後も意味が変わらない部分はなるべく変更しない
- 図から要素そのものが削除された場合は、対応する `.context.md` 上の要素説明も削除または更新する
- 図上の要素が変わらない場合は、冗長な表現の圧縮にとどめ、図上の意味・粒度・推定事項を失わせない
- 明示 edge ではなく矢印図形や座標から推定した関係は、推定であることを書く
- 冗長な説明は圧縮してよいが、図上の主要要素は落とさない

Codex で作業する場合は、個人スキル `drawio-context` を使う。

### 4. SVG を出力する

対象の `.drawio` を `docs/img/<filename>.svg` にレンダリングする。

ローカルに draw.io Desktop がある場合の例:

```powershell
& 'C:\Program Files\draw.io\draw.io.exe' --export --format svg --embed-svg-fonts=false --output 'docs\img\<filename>.svg' 'docs\img\src\<filename>.drawio'
```

`--embed-svg-fonts=false` は、フォントデータやテキストフォールバック画像を SVG に埋め込まないための指定である。編集用のダイアグラム情報も SVG に含めないため、`--embed-diagram` は指定しない。

出力後、次を確認する。

- `.svg` ファイルが存在する
- ファイルサイズが 0 ではない
- 先頭が SVG/XML として読める

### 5. 最終確認

対象ごとに、次のファイルが揃っていることを確認する。

- `docs/img/src/<filename>.drawio`
- `docs/img/<filename>.svg`
- `docs/img/<filename>.context.md`

作業結果には、処理した対象ファイル、Version チェック結果、生成・更新した `.svg` と `.context.md` を報告する。

## 注意

この作業定義はプロジェクト固有の運用である。draw.io 図を Markdown コンテキストへ翻訳する一般的な方法は、個人スキル `drawio-context` 側に置く。
