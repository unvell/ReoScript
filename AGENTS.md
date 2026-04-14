# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## Project Overview

ReoScript is an ECMAScript-like script language interpreter for .NET applications, allowing host applications to embed scripting and to expose .NET classes/objects/events directly to scripts. It is a tree-walking interpreter built on an ANTLR 3 grammar — there is no bytecode/JIT stage.

The library targets **.NET 10** and is cross-platform (no `System.Windows.Forms` dependency in the core). Build with `dotnet build`.

## Solution Layout

[Source/ReoScript.sln](Source/ReoScript.sln) contains three migrated projects:

| Project | Target | Purpose |
|---|---|---|
| [Source/ReoScript/](Source/ReoScript/) | `net10.0` (library) | The interpreter library (`unvell.ReoScript.dll`). Root namespace `unvell.ReoScript`. |
| [Source/ReoScriptRunner/](Source/ReoScriptRunner/) | `net10.0` (exe) | Command-line runner that executes `.reo` files. |
| [Source/TestCase/](Source/TestCase/) | `net10.0` (xUnit) | Test suite — xUnit adapter over XML-defined language tests + C# CLR-interop tests. |

Not yet migrated (still targeting .NET Framework 3.5, excluded from solution):

| Project | Purpose |
|---|---|
| [Source/ReoScriptEditor/](Source/ReoScriptEditor/) | WinForms script editor (FastColoredTextBox-based). Needs `net10.0-windows`. |
| [Source/ReoScriptExtensions/](Source/ReoScriptExtensions/) | Optional extensions (File I/O, Graphics). Needs `System.Drawing` / `net10.0-windows`. |

Sample integrations live under [Samples/](Samples/) with their own [ReoScriptSamples.sln](Samples/ReoScriptSamples.sln).

## Building

```bash
# Build everything:
dotnet build Source/ReoScript.sln

# Release build:
dotnet build Source/ReoScript.sln -c Release
```

The project has zero external dependencies beyond the .NET SDK (xUnit is restored via NuGet for tests only).

## Running Tests

Tests use **xUnit** with a thin adapter ([Source/TestCase/XmlTestAdapter.cs](Source/TestCase/XmlTestAdapter.cs)) that loads the existing XML test suites as `[Theory]` test data.

```bash
# Run all tests:
dotnet test Source/ReoScript.sln

# Run with verbose output:
dotnet test Source/TestCase/TestCase.csproj -v normal

# Filter by test name (xUnit filter):
dotnet test Source/TestCase/TestCase.csproj --filter "DisplayName~closure"
```

Three test suites run:
1. **Language tests** — XML files in [Source/TestCase/tests/](Source/TestCase/tests/) (e.g. `001_core.xml`, `021_fun.xml`). Each XML file contains `<test-case>` blocks of ReoScript source that call `debug.assert()`.
2. **CLR interop tests** — Standard xUnit `[Fact]` methods in [Source/TestCase/CLRTestCases.cs](Source/TestCase/CLRTestCases.cs) testing .NET direct-access, property/method access, and IDictionary support.
3. **Engine tests** — Standard xUnit `[Fact]` methods in [Source/TestCase/EngineTests.cs](Source/TestCase/EngineTests.cs) testing loop protection, truthy/falsy, error reporting, and module import.

Note: `070-001 setInterval` is a known flaky test (timing-dependent busy-wait).

## Architecture

### Pipeline

1. **Lexing/Parsing** — Hand-written lexer and recursive descent parser under [Source/ReoScript/Core/Syntax/](Source/ReoScript/Core/Syntax/). No external parser-generator dependency. The original ANTLR 3 grammar is preserved at [Source/ReoScript/ReoScript.g](Source/ReoScript/ReoScript.g) for reference.
   - `NodeType.cs` — integer constants for all token/node types (values match the former ANTLR constants so existing dispatch tables work unchanged).
   - `Token.cs` / `Lexer.cs` — tokenizer producing `Token` structs.
   - `Parser.cs` — `ReoScriptHandwrittenParser` with precedence-climbing expression parsing. Produces `SyntaxNode` AST.
   - `SyntaxNode.cs` — base AST node class (replaces ANTLR's `CommonTree`). Also contains `ConstValueNode` and `ReplacedSyntaxNode`.
2. **Pre-interpret pass** — constant folding, syntax-error promotion, function-info collection. See `FunctionInfo`/`VariableInfo` under [Source/ReoScript/Core/Reflection/](Source/ReoScript/Core/Reflection/).
3. **Tree walking** — [Source/ReoScript/ScriptRunningMachine.cs](Source/ReoScript/ScriptRunningMachine.cs) is the heart of the project. It contains the `ScriptRunningMachine` (SRM) class plus most built-in object types (`ObjectValue`, wrapper types, `BreakNode`/`ContinueNode` sentinels, etc.). New AST node types and runtime semantics generally land here.

`ScriptContext` represents an execution scope; SRM exposes `Run`, `CalcExpression`, and `CreateContext` for host integration.

### Closure semantics

Closures use lexical scoping. Each `FunctionObject` has a `CapturedScope` field set once at creation time (not at call time). This means escaped closures, independent closure instances, and mutable shared state all work correctly, matching JavaScript's closure behavior.

### Core types

[Source/ReoScript/Core/](Source/ReoScript/Core/) organizes individual node/statement classes:

- `Core/Syntax/` — lexer, parser, AST node types, token constants.
- `Core/Node/` — primitive value sentinels (`InfinityValue`, `MinusInfinityValue`, `NaNValue`, `ReturnNode`).
- `Core/Statement/` — `FunctionDefineNode`, `VariableDefineNode`, `StaticFunctionScope`, `MemberScopeModifier`, `ISyntaxTreeReturn` (the marker interface implemented by every value/node that flows through the interpreter).
- `Core/Reflection/` — `FunctionInfo`, `VariableInfo` (compile-time metadata).

[Source/ReoScript/AnonymousFunctionDefineNode.cs](Source/ReoScript/AnonymousFunctionDefineNode.cs) lives at the project root for historical reasons. When adding new node types, prefer the `Core/` layout over adding to the root.

### JIT compiler prototype

A JIT compiler prototype exists under [Source/ReoScript/Compiler/](Source/ReoScript/Compiler/). It uses `System.Reflection.Emit` to generate IL from the `SyntaxNode` AST, with automatic fallback to tree-walking for unsupported node types. The AST is designed with JIT in mind — `SyntaxNode` is a public typed base class supporting the Visitor pattern for future compilation backends.

### Truthy/falsy semantics

Condition expressions (`if`, `while`, `for`, `? :`, `&&`, `||`, `!`) use truthy/falsy conversion:
- **Falsy**: `null`, `false`, `0`, `NaN`, `""` (empty string)
- **Truthy**: everything else (non-zero numbers, non-empty strings, objects, arrays, functions)

`&&` and `||` return the actual value (short-circuit), enabling patterns like `var x = obj || "default"`.

### Loop protection

`ScriptRunningMachine.MaxIterationsPerLoop` (default: 10,000,000) limits iterations per `for`/`while` loop. Exceeding the limit throws `ScriptExecutionTimeoutException`. Set to `0` to disable. This prevents user scripts from freezing the host application.

### Error reporting

`ErrorObject` includes `FilePath`, `Line`, and `CharIndex`. `GetFullErrorInfo()` produces formatted output like `demo.reo:3:5 - error message` with a full call stack. Scripts can access `error.file`, `error.line`, `error.message`, and `error.stack`.

### Event handler exception safety

Script exceptions in CLR event handlers and async callbacks (`setTimeout`/`setInterval`) are caught and routed to the `ScriptRunningMachine.ScriptError` event instead of crashing the host. Subscribe to `ScriptError` to log or display errors.

### Module import

`importModule("path/to/file.reo")` loads a script file in an isolated scope and returns a module object. Module-level functions and variables become properties of the returned object. Results are cached (each file executes at most once). Closures inside the module correctly resolve module-level variables.

```javascript
var anim = importModule("animation/animate.xb");
anim.fadeIn(element);
```

The traditional `import "file.reo"` (which executes in global scope) remains available for backward compatibility. From C# host code, use `srm.ImportModuleFile(fullPath)`.

### Host-integration surface

- **Direct .NET access** — gated by `MachineWorkMode` flags (`AllowDirectAccess`, `AllowImportTypeInScript`, `AllowCLREventBind`). Scripts can `import` .NET namespaces and call methods/bind events on real CLR objects.
- **Native function extension** — host code registers C# delegates as script-callable functions on the SRM or on script objects.
- **Built-in scripts** — [Source/ReoScript/scripts/](Source/ReoScript/scripts/) (`core.reo`, `array.reo`, `array_ext.reo`, `number.reo`, `debug.reo`) are embedded resources loaded via `Assembly.GetManifestResourceStream`.

### File extension

`.reo` is the canonical script extension (changed from `.rs` in v1.3.1, see [Source/ChangeLog.txt](Source/ChangeLog.txt)).

## Editing notes

- The grammar supports an HTML-tag literal syntax (`tag`, `tagAttr`, `tagTemplateDefine` rules in [Source/ReoScript/ReoScript.g](Source/ReoScript/ReoScript.g)) — `template<TypeName>(args) <tag/>` declares a named, parameterized tag template. This was added before JSX was widespread; it predates React's release. Tag-related token IDs (`TAG`, `TAG_ATTR`, `TAG_NAME`, `TEMPLATE_DEFINE`, `TEMPLATE_TAG`) are still emitted by the parser.
- The csproj files use SDK-style format with `EnableDefaultItems=false` for the core library. New `.cs` files are picked up by the glob pattern `**\*.cs`.
- There is no formatter/linter configured; follow the surrounding tab-indented, K&R-brace style used in `ScriptRunningMachine.cs`.
