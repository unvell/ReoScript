# ReoScript

ReoScript is an ECMAScript-like scripting engine for .NET applications. It is designed for embedding, so host applications can execute user-defined scripts, expose application objects to scripts, and optionally allow direct access to .NET types, members, and events.

The current migrated solution targets `.NET 10` and contains:

- `Source/ReoScript/`: the core scripting library
- `Source/ReoScriptRunner/`: a command-line runner for `.reo` files
- `Source/TestCase/`: xUnit-based language and engine tests

## Highlights

- JavaScript-like syntax with ReoScript-specific extensions
- Embed scripts into .NET applications through `ScriptRunningMachine`
- Pass host values into scripts as globals
- Register native C# functions callable from script
- Optional direct access to .NET objects and imported CLR types
- Closures, lambda expressions, JSON, modules, and timer-based async callbacks
- Extra language features such as object merge, object spread, destructuring, and tag-style syntax

## Documentation

- [English User Guide](docs/reoscript-user-guide.md)
- [Japanese User Guide](docs/reoscript-user-guide.ja.md)

## Quick Start

Build the solution:

```bash
dotnet build Source/ReoScript.sln
```

Run the CLI runner:

```bash
dotnet run --project Source/ReoScriptRunner -- sample.reo
dotnet run --project Source/ReoScriptRunner -- -e "console.log(1 + 2)"
dotnet run --project Source/ReoScriptRunner -- -console
```

Minimal embedding example:

```csharp
using unvell.ReoScript;

var srm = new ScriptRunningMachine();

srm.SetGlobalVariable("appName", "Demo");
srm.Run(@"
function add(a, b) {
  return a + b;
}
");

var result = srm.CalcExpression("add(10, 20)");
```

Minimal script example:

```javascript
var total = 0;

for (var i = 1; i <= 10; i++) {
  total += i;
}

console.log(total);
```

## Build

Build all migrated projects:

```bash
dotnet build Source/ReoScript.sln
```

Release build:

```bash
dotnet build Source/ReoScript.sln -c Release
```

## Test

Run all tests:

```bash
dotnet test Source/ReoScript.sln
```

Run the test project directly:

```bash
dotnet test Source/TestCase/TestCase.csproj
```

Run a filtered test:

```bash
dotnet test Source/TestCase/TestCase.csproj --filter "DisplayName~closure"
```

The test project covers:

- XML-driven language tests under `Source/TestCase/tests/`
- CLR interop tests
- engine-level tests for loop protection, module loading, truthy/falsy rules, async timers, and error reporting

## Repository Layout

Main migrated projects:

- `Source/ReoScript/`
  - core engine library targeting `net10.0`
  - assembly name: `unvell.ReoScript`
- `Source/ReoScriptRunner/`
  - CLI runner targeting `net10.0`
  - assembly name: `ReoScript`
- `Source/TestCase/`
  - xUnit test project targeting `net10.0`

Additional directories:

- `Samples/`: sample host applications and embedding examples
- `docs/`: end-user documentation
- `Source/ReoScript/scripts/`: embedded built-in ReoScript libraries such as `core.reo`, `array.reo`, `debug.reo`, and `number.reo`

Not yet migrated into the current `.NET 10` solution:

- `Source/ReoScriptEditor/`
- `Source/ReoScriptExtensions/`

## Language Overview

ReoScript is close to JavaScript, but it is not a drop-in JavaScript runtime. Supported and tested areas include:

- variable declarations with `var`
- `if`, `for`, `while`, `switch`, `try/catch/finally`
- functions, lambdas, lexical closures
- objects, arrays, JSON, `eval`, `typeof`, `instanceof`
- truthy/falsy conditional semantics
- modules through `import`, `import ... as`, and `importModule(...)`
- async timers via `setTimeout`, `setInterval`, and `clearInterval`

ReoScript-specific extensions include:

- object merging with `a + b`
- `new Type() { ... }` style object initialization
- shorthand properties, object spread, and destructuring support
- tag-style syntax such as `<User />`

For full syntax and usage details, use the guides in the `docs/` directory.

## Embedding Features

The main host integration surface is `ScriptRunningMachine`.

Typical host-side operations:

- execute script text with `Run(...)`
- evaluate expressions with `CalcExpression(...)`
- expose values with `SetGlobalVariable(...)`
- read globals back with `GetGlobalVariable(...)`
- import CLR types with `ImportType(...)`
- import namespaces with `ImportNamespace(...)`
- create isolated module objects with `ImportModuleFile(...)`

Feature gates are controlled through `MachineWorkMode`, including:

- `AllowDirectAccess`
- `AllowImportTypeInScript`
- `AllowCLREventBind`

For examples, see:

- `Samples/NativeFunctionExtension/`
- `Samples/DirectAccess/`
- `Samples/CLRTypeImporting/`
- `Samples/ConsoleRunner/`

## Safety and Runtime Behavior

ReoScript includes several runtime behaviors that matter for hosts:

- `MaxIterationsPerLoop` protects against runaway `for` and `while` loops
- module imports are cached
- async callback and event-handler exceptions can be observed through `ScriptError`
- `WorkPath` controls relative script import resolution

## Samples

The `Samples/` directory contains example integrations, including:

- console execution
- native function extension
- direct access to host objects
- CLR type importing
- property getter/setter integration
- script editor and UI-based demos

## License

MIT License

Copyright (c) UNVELL Inc., Jingwood 2012-2026, All Rights Reserved.
