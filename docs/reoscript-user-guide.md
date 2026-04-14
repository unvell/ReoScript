# ReoScript User Guide

This guide covers both sides of ReoScript usage: writing ReoScript code and embedding the engine into a .NET application. The content is based on the current `Source/ReoScript.sln` and the test suite in this repository.

## 1. What ReoScript Is

ReoScript is an ECMAScript-like scripting engine designed to be embedded into .NET applications. Its syntax is close to JavaScript, but it also includes ReoScript-specific extensions such as direct .NET access, module loading, object merging, and tag-style syntax.

Main characteristics:

- JavaScript-like syntax for application scripting
- Execute script text, files, and expressions through `ScriptRunningMachine`
- Expose C# objects and functions as global values
- Optionally allow .NET type import, property access, and event binding
- Support for lambda expressions, modules, JSON, timers, and closures

## 2. Setup

The core library is in `Source/ReoScript/` and the CLI runner is in `Source/ReoScriptRunner/`.

```bash
dotnet build Source/ReoScript.sln
```

Examples for the CLI runner:

```bash
dotnet run --project Source/ReoScriptRunner -- sample.reo
dotnet run --project Source/ReoScriptRunner -- -e "console.log(1 + 2)"
dotnet run --project Source/ReoScriptRunner -- -console
```

Main `ReoScriptRunner` options:

- `-e <script>` / `-exec <script>`: execute a script string directly
- `-workpath <path>`: set the working directory
- `-debug`: enable debug mode
- `-console`: enter the interactive console after execution
- `-com`: compile mode, not implemented in the current version

Interactive console commands:

- `?expr`: evaluate and print an expression
- `?`: print the current global object
- `.path`: load a script file
- `/quit`, `/q`: exit

## 3. Your First Script

```javascript
var total = 0;

for (var i = 1; i <= 10; i++) {
  total += i;
}

console.log(total);
```

`Run` returns the value of the last evaluated expression.

```javascript
var a = 10
var b = 20
a + b
```

The result of the whole script is `30`.

## 4. Basic Syntax

### 4.1 Variables

```javascript
var a = 10;
var b = 20, c = a + b;
```

- Variable declarations use `var`
- A `var` declared in global scope becomes a global variable
- Missing or unset values are often treated as `null`

### 4.2 Numbers, Strings, Booleans, null

```javascript
var n = 3.14;
var s = "hello";
var b = true;
var x = null;
```

### 4.3 Operators

Common operators:

- Arithmetic: `+`, `-`, `*`, `/`, `%`
- Assignment: `=`, `+=`, `-=`, `*=`, `/=`
- Comparison: `==`, `!=`, `===`, `!==`, `<`, `<=`, `>`, `>=`
- Logical: `&&`, `||`, `!`
- Increment/decrement: `++`, `--`
- Bitwise: `|`, `&`, `<<`, `>>`
- Ternary: `cond ? a : b`
- Type checks: `typeof`, `instanceof`
- Property deletion: `delete obj.key`

```javascript
var a = 10;
a += 5;
a++;

var label = a > 10 ? "large" : "small";
```

### 4.4 Optional Semicolons

ReoScript supports automatic semicolon insertion.

```javascript
var a = 10
var b = 20
a + b
```

However, semicolons inside a `for (...)` header are still required.

```javascript
for (var i = 0; i < 5; i++) {
  console.log(i);
}
```

## 5. Control Flow

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

`for ... in` behaves differently depending on the target:

- Objects: iterates property names
- Arrays: iterates element values

```javascript
for (key in obj) {
  console.log(key);
}

for (item in arr) {
  console.log(item);
}
```

This is different from JavaScript, where array iteration usually gives indices in `for...in`.

## 6. Functions and Closures

### 6.1 Function Declarations

```javascript
function add(a, b) {
  return a + b;
}
```

Function declarations are hoisted and can be used before their definition.

```javascript
var x = add(10, 20);

function add(a, b) {
  return a + b;
}
```

### 6.2 Function Expressions

```javascript
var add = function(a, b) {
  return a + b;
};
```

### 6.3 Lambda Expressions

```javascript
var add = (a, b) => a + b;
var square = x => x * x;

var total = ((a, b) => a + b)(2, 3);
```

Block bodies are also supported.

```javascript
var classify = n => {
  if (n >= 0) return "positive";
  return "negative";
};
```

### 6.4 Closures

ReoScript uses lexical scoping. Inner functions can capture local variables from outer functions.

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

`c1` and `c2` keep independent captured state.

### 6.5 this / call / apply

Functions provide `call` and `apply`, so you can override the call-time `this`.

```javascript
function bracketMe() {
  return "[" + this + "]";
}

bracketMe.call("abc");
```

## 7. Objects

### 7.1 Object Literals

```javascript
var user = {
  name: "alice",
  age: 20
};
```

Property access:

```javascript
user.name
user["name"]
```

### 7.2 Adding and Removing Properties

```javascript
user.email = "a@example.com";
delete user.email;
```

### 7.3 Constructors and new

```javascript
function User(name) {
  this.name = name;
}

var u = new User("alice");
```

ReoScript also supports an extended initializer form after `new`.

```javascript
function User() {
  this.role = "guest";
}

var u = new User() {
  name: "alice",
  active: true
};
```

### 7.4 Object Merging

ReoScript supports object merging through `+`.

```javascript
var a = { x: 10 };
var b = { y: 20 };
var c = a + b;
```

### 7.5 Shorthand Properties, Spread, Destructuring

```javascript
var a = 1, b = 2;
var obj = { a, b };

var merged = { ...obj, c: 3 };

var { a, c } = merged;
```

## 8. Arrays

### 8.1 Basics

```javascript
var arr = [1, 2, 3];
arr.push(4);
arr[0] = 10;
```

### 8.2 Common Methods

Besides the usual array behavior, several helper methods are added by the built-in scripts.

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

## 9. Built-in Objects and Functions

### 9.1 console

When `console` is available in the host, these methods are exposed:

- `console.read()`
- `console.readline()`
- `console.write(value)`
- `console.log(value)`

### 9.2 Math

Common constants and functions:

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

Both `JSON.parse` and `JSON.stringify` support converter callbacks.

```javascript
var obj = JSON.parse(src, (key, value) => value);
var str = JSON.stringify(obj, (key, value) => String(value));
```

### 9.5 eval

```javascript
var value = eval("1 + 2");
```

`eval` runs in the current scope.

### 9.6 parseInt

```javascript
var n = parseInt("FF", 16);
```

### 9.7 debug

The `debug` object is available when you attach `new ScriptDebugger(srm)`.

```javascript
debug.assert(1 + 1 == 2);
var sw = debug.Stopwatch.startNew();
```

## 10. Truthy and Falsy Rules

ReoScript evaluates conditions using truthy / falsy conversion.

Falsy values:

- `null`
- `false`
- `0`
- `NaN`
- `""`

Truthy values:

- non-zero numbers
- non-empty strings
- objects
- arrays
- functions

```javascript
var name = input || "guest";
var enabled = config && config.flag;
```

`&&` and `||` return actual values, not only booleans.

## 11. Exception Handling

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

`catch` can also be written without a variable.

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

For `Error` objects, `message` is available.

## 12. Modules and import

### 12.1 Legacy import

```javascript
import "common.reo";
```

This executes the imported script in global scope.

### 12.2 import as

```javascript
import "math.reo" as math;

var result = math.add(10, 3);
```

This loads the script into an isolated scope and binds it to the given name.

### 12.3 importModule

```javascript
var math = importModule("math.reo");
var result = math.add(3, 4);
```

Properties:

- module scope is isolated from the global scope
- module-level functions and variables become properties of the returned object
- a module file is cached and normally executes only once

## 13. Async Features

Timer functions:

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

Exceptions raised inside event handlers or async callbacks can be handled on the host side through the `ScriptError` event.

## 14. Embedding in .NET

### 14.1 Minimal Usage

```csharp
using unvell.ReoScript;

var srm = new ScriptRunningMachine();
srm.Run("var x = 10; var y = 20;");
var result = srm.CalcExpression("x + y");
```

### 14.2 Passing Global Variables

```csharp
using unvell.ReoScript;

var srm = new ScriptRunningMachine();
srm.SetGlobalVariable("appName", "Demo");
srm.SetGlobalVariable("version", 1);

srm.Run("console.log(appName);");
```

The indexer works as well.

```csharp
srm["answer"] = 42;
```

### 14.3 Exposing Native Functions

```csharp
using unvell.ReoScript;

var srm = new ScriptRunningMachine();
srm["hello"] = new NativeFunctionObject("hello", (ctx, owner, args) =>
{
	return "Hello " + (args.Length > 0 ? args[0] : "world");
});
```

### 14.4 Direct Access to .NET Objects

```csharp
using unvell.ReoScript;

var srm = new ScriptRunningMachine();
srm.WorkMode |= MachineWorkMode.AllowDirectAccess;
srm.SetGlobalVariable("user", new User());
```

Script side:

```javascript
user.Nickname = "alice";
user.Hello();
```

### 14.5 Importing Types from C#

```csharp
srm.WorkMode |= MachineWorkMode.AllowDirectAccess;
srm.ImportType(typeof(System.Windows.Forms.LinkLabel));
```

### 14.6 Allowing Type Import from Script

```csharp
srm.WorkMode |= MachineWorkMode.AllowDirectAccess
	| MachineWorkMode.AllowImportTypeInScript
	| MachineWorkMode.AllowCLREventBind;
```

Script side:

```javascript
import System.Windows.Forms.*;
import System.Drawing.Point;

var f = new Form() {
  text: "Form created in ReoScript"
};
```

### 14.7 Event Binding

With `AllowCLREventBind`, script functions can be attached to .NET events.

```javascript
var link = new LinkLabel() {
  text: "click me",
  click: function() {
    f.close();
  }
};
```

### 14.8 Debug Support

```csharp
using unvell.ReoScript;
using unvell.ReoScript.Diagnostics;

var srm = new ScriptRunningMachine();
var debugger = new ScriptDebugger(srm);
```

That enables `debug.assert` and related helpers.

## 15. Execution Control and Safety

### 15.1 Loop Protection

You can limit loop iterations to guard against infinite loops.

```csharp
srm.MaxIterationsPerLoop = 10_000_000;
```

- default value: `10_000_000`
- set `0` to disable the limit
- exceeding the limit throws `ScriptExecutionTimeoutException`

### 15.2 Working Path

Relative imports use `WorkPath` as their base path.

```csharp
srm.WorkPath = @"C:\scripts";
```

## 16. Advanced ReoScript-Specific Syntax

### 16.1 Tag Literals

ReoScript includes an HTML-like tag syntax for object construction.

```javascript
function User() { }

var usr = <User />;
```

### 16.2 Template Tag Definitions

The grammar also includes template-style declarations such as:

```javascript
template<UserCard>(name, age) <User />;
```

This syntax is ReoScript-specific and is not the same thing as JavaScript or JSX. If you plan to use it, confirm the expected behavior in the target host application.

## 17. Differences from JavaScript

ReoScript is ECMAScript-like, not a drop-in JavaScript runtime. Important differences include:

- `for ... in` over arrays yields elements rather than indices
- `undefined` often behaves close to `null`
- there are tests where `NaN == NaN` is true, unlike JavaScript
- `debug` is not guaranteed to exist unless the debugger helper is attached
- object merge with `a + b`, `new Type() { ... }`, and tag syntax are ReoScript-specific extensions

When porting JavaScript code, validate behavior in ReoScript rather than assuming browser or Node.js semantics.

## 18. Common Usage Patterns

### 18.1 Configuration Scripts

```javascript
var config = {
  title: "My App",
  retryCount: 3,
  endpoints: ["a", "b", "c"]
};
```

### 18.2 Rule Evaluation

```javascript
function canPurchase(user, total) {
  return user != null && user.active && total > 0;
}
```

### 18.3 Extending Host UI or APIs

```javascript
import System.Windows.Forms.*;

var button = new Button() {
  text: "Run",
  click: function() {
    console.log("clicked");
  }
};
```

## 19. Useful References in This Repository

- CLI runner: `Source/ReoScriptRunner/`
- embedding samples: `Samples/`
- language tests: `Source/TestCase/tests/`
- core implementation: `Source/ReoScript/ScriptRunningMachine.cs`

If you need more examples, start with `Source/TestCase/tests/` and `Samples/`. They are the most concrete reference for supported behavior.
