# ReoScript ユーザーガイド

このドキュメントは、ReoScript を使ってスクリプトを書く人と、.NET アプリケーションへ組み込む人の両方に向けた利用ガイドです。内容は現在の `Source/ReoScript.sln` とテストコードに基づいています。

## 1. ReoScript とは

ReoScript は .NET アプリケーションへ組み込むための ECMAScript 風スクリプトエンジンです。文法は JavaScript に近い一方で、.NET オブジェクト直接操作、モジュール読み込み、オブジェクト結合、タグ構文など、独自拡張も含みます。

主な特徴:

- JavaScript に近い構文でスクリプトを記述できる
- `ScriptRunningMachine` から文字列、ファイル、式を実行できる
- C# 側のオブジェクトや関数をグローバル変数として公開できる
- 設定次第で .NET 型の import、プロパティ操作、イベント接続ができる
- ラムダ式、モジュール、JSON、非同期タイマー、クロージャをサポートする

## 2. セットアップ

ReoScript 本体は `Source/ReoScript/`、CLI ランナーは `Source/ReoScriptRunner/` にあります。

```bash
dotnet build Source/ReoScript.sln
```

CLI ランナーの実行例:

```bash
dotnet run --project Source/ReoScriptRunner -- sample.reo
dotnet run --project Source/ReoScriptRunner -- -e "console.log(1 + 2)"
dotnet run --project Source/ReoScriptRunner -- -console
```

`ReoScriptRunner` の主なオプション:

- `-e <script>` / `-exec <script>`: スクリプト文字列を直接実行
- `-workpath <path>`: 作業ディレクトリを設定
- `-debug`: デバッグモードを有効化
- `-console`: 実行後に対話コンソールへ入る
- `-com`: コンパイルモード。現行版では未実装

対話コンソールでは次が使えます。

- `?expr`: 式を評価して表示
- `?`: 現在のグローバルオブジェクトを表示
- `.path`: スクリプトファイルをロード
- `/quit`, `/q`: 終了

## 3. 最初のスクリプト

```javascript
var total = 0;

for (var i = 1; i <= 10; i++) {
  total += i;
}

console.log(total);
```

`Run` の戻り値は最後に評価された式の値です。

```javascript
var a = 10
var b = 20
a + b
```

このスクリプト全体の戻り値は `30` になります。

## 4. 基本文法

### 4.1 変数

```javascript
var a = 10;
var b = 20, c = a + b;
```

- 変数宣言は `var`
- グローバルスコープで宣言した `var` はグローバル変数になる
- 未設定値や存在しない値は `null` として扱われる場面が多い

### 4.2 数値・文字列・真偽値・null

```javascript
var n = 3.14;
var s = "hello";
var b = true;
var x = null;
```

### 4.3 演算子

代表的な演算子:

- 算術: `+`, `-`, `*`, `/`, `%`
- 代入: `=`, `+=`, `-=`, `*=`, `/=`
- 比較: `==`, `!=`, `===`, `!==`, `<`, `<=`, `>`, `>=`
- 論理: `&&`, `||`, `!`
- インクリメント/デクリメント: `++`, `--`
- ビット演算: `|`, `&`, `<<`, `>>`
- 三項演算子: `cond ? a : b`
- 型判定: `typeof`, `instanceof`
- プロパティ削除: `delete obj.key`

```javascript
var a = 10;
a += 5;
a++;

var label = a > 10 ? "large" : "small";
```

### 4.4 セミコロン省略

ReoScript は自動セミコロン挿入をサポートします。

```javascript
var a = 10
var b = 20
a + b
```

ただし `for (...)` ヘッダ内の区切りセミコロンは省略できません。

```javascript
for (var i = 0; i < 5; i++) {
  console.log(i);
}
```

## 5. 制御構文

### 5.1 if / else

```javascript
if (score >= 80) {
  grade = "A";
} else {
  grade = "B";
}
```

### 5.2 while

```javascript
var count = 5;
while (count) {
  console.log(count);
  count--;
}
```

### 5.3 for

```javascript
var sum = 0;
for (var i = 0; i < 10; i++) {
  sum += i;
}
```

### 5.4 switch

```javascript
switch (kind) {
  case "error":
    console.log("error");
    break;
  case "warn":
    console.log("warn");
    break;
  default:
    console.log("info");
    break;
}
```

### 5.5 for-in

`for ... in` は対象によって振る舞いが異なります。

- オブジェクト: プロパティ名を列挙
- 配列: 要素を列挙

```javascript
for (key in obj) {
  console.log(key);
}

for (item in arr) {
  console.log(item);
}
```

JavaScript のように配列インデックスを得る構文ではない点に注意してください。

## 6. 関数とクロージャ

### 6.1 関数宣言

```javascript
function add(a, b) {
  return a + b;
}
```

関数宣言は呼び出しより後ろに書いても使えます。

```javascript
var x = add(10, 20);

function add(a, b) {
  return a + b;
}
```

### 6.2 関数式

```javascript
var add = function(a, b) {
  return a + b;
};
```

### 6.3 ラムダ式

```javascript
var add = (a, b) => a + b;
var square = x => x * x;

var total = ((a, b) => a + b)(2, 3);
```

ブロック本体も使えます。

```javascript
var classify = n => {
  if (n >= 0) return "positive";
  return "negative";
};
```

### 6.4 クロージャ

ReoScript の関数はレキシカルスコープを持ちます。外側関数のローカル変数を内側関数が捕捉できます。

```javascript
function makeCounter() {
  var count = 0;
  return function() {
    count = count + 1;
    return count;
  };
}

var c1 = makeCounter();
var c2 = makeCounter();
```

`c1` と `c2` は独立した状態を保持します。

### 6.5 this / call / apply

関数には `call` と `apply` があり、呼び出し時の `this` を差し替えられます。

```javascript
function bracketMe() {
  return "[" + this + "]";
}

bracketMe.call("abc");
```

## 7. オブジェクト

### 7.1 オブジェクトリテラル

```javascript
var user = {
  name: "alice",
  age: 20
};
```

アクセス方法:

```javascript
user.name
user["name"]
```

### 7.2 プロパティ追加・削除

```javascript
user.email = "a@example.com";
delete user.email;
```

### 7.3 コンストラクタと new

```javascript
function User(name) {
  this.name = name;
}

var u = new User("alice");
```

`new` の後ろに初期化ブロックを付ける独自構文も使えます。

```javascript
function User() {
  this.role = "guest";
}

var u = new User() {
  name: "alice",
  active: true
};
```

### 7.4 オブジェクト結合

ReoScript にはオブジェクト同士を `+` で結合する拡張があります。

```javascript
var a = { x: 10 };
var b = { y: 20 };
var c = a + b;
```

### 7.5 省略プロパティ・スプレッド・分割代入

```javascript
var a = 1, b = 2;
var obj = { a, b };

var merged = { ...obj, c: 3 };

var { a, c } = merged;
```

## 8. 配列

### 8.1 基本

```javascript
var arr = [1, 2, 3];
arr.push(4);
arr[0] = 10;
```

### 8.2 主なメソッド

標準の配列操作に加えて、組み込みスクリプトでいくつかの補助メソッドが定義されています。

- `push`
- `splice`
- `indexOf`
- `join`
- `concat`
- `map`
- `reduce`
- `where`
- `first`
- `last`
- `equals`
- `each`

```javascript
var arr = [1, 2, 3, 4, 5];
var even = arr.where(n => Math.floor(n / 2) == n / 2);
var doubled = arr.map(n => n * 2);
var total = arr.reduce((a, c) => a + c, 0);
```

## 9. 組み込みオブジェクトと関数

### 9.1 console

`console` が有効な環境では次が使えます。

- `console.read()`
- `console.readline()`
- `console.write(value)`
- `console.log(value)`

### 9.2 Math

主な定数と関数:

- `Math.PI`
- `Math.E`
- `Math.LN2`
- `Math.LN10`
- `Math.min(...)`
- `Math.max(...)`
- `Math.floor(...)`
- `Math.sqrt(...)`
- `Math.atan2(...)`

### 9.3 Date

```javascript
var now = Date.now();
var d = new Date();
var ms = d.getTime();
```

### 9.4 JSON

```javascript
var obj = JSON.parse("{name:'apple',count:10}");
var str = JSON.stringify(obj);
```

`JSON.parse` と `JSON.stringify` は変換関数も受け取れます。

```javascript
var obj = JSON.parse(src, (key, value) => value);
var str = JSON.stringify(obj, (key, value) => String(value));
```

### 9.5 eval

```javascript
var value = eval("1 + 2");
```

`eval` は現在のスコープ内で評価されます。

### 9.6 parseInt

```javascript
var n = parseInt("FF", 16);
```

### 9.7 debug

`debug` オブジェクトは `new ScriptDebugger(srm)` を付けた場合に利用できます。

```javascript
debug.assert(1 + 1 == 2);
var sw = debug.Stopwatch.startNew();
```

## 10. 真偽値変換

ReoScript の条件式は truthy / falsy で評価されます。

Falsy:

- `null`
- `false`
- `0`
- `NaN`
- `""`

Truthy:

- 非 0 数値
- 非空文字列
- オブジェクト
- 配列
- 関数

```javascript
var name = input || "guest";
var enabled = config && config.flag;
```

`&&` と `||` は真偽値ではなく実際の値を返します。

## 11. 例外処理

### 11.1 try / catch / finally

```javascript
try {
  dangerous();
} catch (e) {
  console.log(e.message);
} finally {
  console.log("cleanup");
}
```

`catch` は変数名なしでも書けます。

```javascript
try {
  dangerous();
} catch {
  console.log("error");
}
```

### 11.2 throw

```javascript
throw new Error("something wrong");
throw 10;
```

`Error` オブジェクトでは `message` を参照できます。

## 12. モジュールと import

### 12.1 旧来の import

```javascript
import "common.reo";
```

これは読み込んだスクリプトをグローバルスコープで実行します。

### 12.2 import as

```javascript
import "math.reo" as math;

var result = math.add(10, 3);
```

こちらは別スコープでロードし、指定名へ束縛します。

### 12.3 importModule

```javascript
var math = importModule("math.reo");
var result = math.add(3, 4);
```

特徴:

- モジュールスコープはグローバルから分離される
- モジュール内の関数・変数が戻り値オブジェクトに公開される
- 同じファイルはキャッシュされ、原則 1 回だけ実行される

## 13. 非同期処理

タイマー関数:

- `setTimeout(callback, milliseconds)`
- `setInterval(callback, milliseconds)`
- `clearInterval(id)`

```javascript
var count = 0;
var id = setInterval(function() {
  count++;
  if (count >= 5) {
    clearInterval(id);
  }
}, 100);
```

イベントハンドラや非同期コールバック内の例外は、ホスト側で `ScriptError` イベントを購読して処理できます。

## 14. .NET への組み込み

### 14.1 最小構成

```csharp
using unvell.ReoScript;

var srm = new ScriptRunningMachine();
srm.Run("var x = 10; var y = 20;");
var result = srm.CalcExpression("x + y");
```

### 14.2 グローバル変数を渡す

```csharp
using unvell.ReoScript;

var srm = new ScriptRunningMachine();
srm.SetGlobalVariable("appName", "Demo");
srm.SetGlobalVariable("version", 1);

srm.Run("console.log(appName);");
```

インデクサでも設定できます。

```csharp
srm["answer"] = 42;
```

### 14.3 ネイティブ関数を公開する

```csharp
using unvell.ReoScript;

var srm = new ScriptRunningMachine();
srm["hello"] = new NativeFunctionObject("hello", (ctx, owner, args) =>
{
	return "Hello " + (args.Length > 0 ? args[0] : "world");
});
```

### 14.4 .NET オブジェクトを直接操作させる

```csharp
using unvell.ReoScript;

var srm = new ScriptRunningMachine();
srm.WorkMode |= MachineWorkMode.AllowDirectAccess;
srm.SetGlobalVariable("user", new User());
```

スクリプト側:

```javascript
user.Nickname = "alice";
user.Hello();
```

### 14.5 型 import を C# 側から許可する

```csharp
srm.WorkMode |= MachineWorkMode.AllowDirectAccess;
srm.ImportType(typeof(System.Windows.Forms.LinkLabel));
```

### 14.6 型 import をスクリプト側から許可する

```csharp
srm.WorkMode |= MachineWorkMode.AllowDirectAccess
	| MachineWorkMode.AllowImportTypeInScript
	| MachineWorkMode.AllowCLREventBind;
```

スクリプト側:

```javascript
import System.Windows.Forms.*;
import System.Drawing.Point;

var f = new Form() {
  text: "Form created in ReoScript"
};
```

### 14.7 イベント連携

`AllowCLREventBind` を有効にすると、.NET イベントへ関数を割り当てられます。

```javascript
var link = new LinkLabel() {
  text: "click me",
  click: function() {
    f.close();
  }
};
```

### 14.8 デバッグ支援

```csharp
using unvell.ReoScript;
using unvell.ReoScript.Diagnostics;

var srm = new ScriptRunningMachine();
var debugger = new ScriptDebugger(srm);
```

これで `debug.assert` などが使えます。

## 15. 実行制御と安全性

### 15.1 ループ回数制限

無限ループ対策として、1 ループあたりの最大反復回数を設定できます。

```csharp
srm.MaxIterationsPerLoop = 10_000_000;
```

- 既定値は `10_000_000`
- `0` を指定すると無効化
- 超過時は `ScriptExecutionTimeoutException`

### 15.2 作業パス

相対パス import の基準は `WorkPath` です。

```csharp
srm.WorkPath = @"C:\scripts";
```

## 16. 高度な独自構文

### 16.1 タグリテラル

HTML 風タグをオブジェクト生成へ使う独自構文があります。

```javascript
function User() { }

var usr = <User />;
```

### 16.2 テンプレートタグ定義

文法上は次のようなテンプレート定義構文があります。

```javascript
template<UserCard>(name, age) <User />;
```

この系統は ReoScript 独自拡張で、一般的な JavaScript / JSX とは別物です。利用時は対象ホスト側の実装も確認してください。

## 17. JavaScript との差分と注意点

ReoScript は JavaScript 互換ではなく ECMAScript 風言語です。特に次を意識してください。

- `for ... in` で配列を回すとインデックスではなく要素が得られる
- `undefined` は `null` と近い扱いになる場面がある
- `NaN == NaN` が真になるテストケースがある。JavaScript と同じ挙動ではない
- `debug` オブジェクトは標準では常に存在するわけではない
- オブジェクト結合 `a + b`、`new Type() { ... }`、タグ構文など独自拡張がある

移植時は JavaScript の常識をそのまま当てはめず、必ず ReoScript 上で動作確認してください。

## 18. 典型的な使い方

### 18.1 設定スクリプトとして使う

```javascript
var config = {
  title: "My App",
  retryCount: 3,
  endpoints: ["a", "b", "c"]
};
```

### 18.2 ルール記述として使う

```javascript
function canPurchase(user, total) {
  return user != null && user.active && total > 0;
}
```

### 18.3 UI やホスト API の拡張として使う

```javascript
import System.Windows.Forms.*;

var button = new Button() {
  text: "Run",
  click: function() {
    console.log("clicked");
  }
};
```

## 19. 参考になるソース

- CLI ランナー: `Source/ReoScriptRunner/`
- 組み込みサンプル: `Samples/`
- 言語テスト: `Source/TestCase/tests/`
- コア実装: `Source/ReoScript/ScriptRunningMachine.cs`

利用例を増やしたい場合は、まず `Source/TestCase/tests/` の XML テストと `Samples/` のサンプルを読むのが最短です。
