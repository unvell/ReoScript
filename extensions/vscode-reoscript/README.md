# ReoScript Syntax for VS Code

ReoScript の `.reo` ファイル向けの VS Code 拡張です。  
JavaScript 風の基本構文に加えて、ReoScript 独自の tag literal と `template<Type>(...) <Tag />` 構文をハイライトします。

## 含まれるもの

- `.reo` の言語登録
- `//` と `/* */` コメント設定
- ReoScript 用 TextMate grammar
- JS と tag 混在を扱うための JSX ベースのハイライト
- `template`, `importModule`, `debug`, access modifier の追加スコープ

## 使い方

1. VS Code でこのフォルダを開く
2. `extensions/vscode-reoscript` を拡張として開発実行する
3. `F5` で Extension Development Host を起動する
4. `.reo` ファイルを開いてハイライトを確認する

拡張だけを単独で開く場合は、このフォルダを VS Code ワークスペースとして開いてください。

## ファイル構成

- `package.json`: 拡張 manifest
- `language-configuration.json`: コメント、括弧、ペア設定
- `syntaxes/reoscript.tmLanguage.json`: ハイライトルール
- `samples/demo.reo`: 動作確認用サンプル

## 実装メモ

- ベースは VS Code 組み込みの `source.js.jsx` grammar です
- ReoScript 独自の `template<...>` 定義ヘッダを先にマッチさせています
- tag literal は JSX に近いため、既存の JSX ハイライトを再利用しています

## 既知の制約

- ReoScript の tag 属性値は `unaryExpression` まで許容されますが、ハイライトは JSX ベースなので一部の独自ケースは完全一致しません
- 旧拡張子 `.rs` は Rust と衝突するため、この拡張では関連付けていません

必要なら次の段階で semantic token provider や injection grammar を追加して、tag 属性値の精度を上げられます。
