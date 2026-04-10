# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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

ANTLR runtime is vendored at [Source/ReoScript/Ref/Antlr3.Runtime.dll](Source/ReoScript/Ref/Antlr3.Runtime.dll) (referenced directly, no NuGet needed). All other dependencies (xUnit) are restored via NuGet automatically.

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

Two test suites run:
1. **Language tests** — XML files in [Source/TestCase/tests/](Source/TestCase/tests/) (e.g. `001_core.xml`, `021_fun.xml`). Each XML file contains `<test-case>` blocks of ReoScript source that call `debug.assert()`.
2. **CLR interop tests** — C# test methods in [Source/TestCase/CLRTestCases.cs](Source/TestCase/CLRTestCases.cs), discovered via `[TestSuite]` / `[TestCase]` attributes.

## Architecture

### Pipeline

1. **Lexing/Parsing** — ANTLR 3 generated lexer/parser. The authoritative grammar is [Source/ReoScript/ReoScript.g](Source/ReoScript/ReoScript.g) (recovered from a 2015 backup); regenerate `ReoScriptLexer.cs` / `ReoScriptParser.cs` from it with ANTLR 3 if syntax changes. The big generated files at the project root — [Source/ReoScript/ReoScriptLexer.cs](Source/ReoScript/ReoScriptLexer.cs) (~5.4k LOC) and [Source/ReoScript/ReoScriptParser.cs](Source/ReoScript/ReoScriptParser.cs) (~12.9k LOC) — are the ANTLR output. The much smaller files under [Source/ReoScript/Core/Grammar/](Source/ReoScript/Core/Grammar/) are **`partial class` extensions** of those generated classes (hand-written constants and helpers like `HIDDEN`, `MAX_TOKENS`, `REPLACED_TREE`), not duplicates — keeping them in separate files means re-running ANTLR doesn't clobber the hand-written parts.
2. **Pre-interpret pass** — constant folding, syntax-error promotion, function-info collection. See `FunctionInfo`/`VariableInfo` under [Source/ReoScript/Core/Reflection/](Source/ReoScript/Core/Reflection/).
3. **Tree walking** — [Source/ReoScript/ScriptRunningMachine.cs](Source/ReoScript/ScriptRunningMachine.cs) (~9k LOC) is the heart of the project. It contains the `ScriptRunningMachine` (SRM) class plus most built-in object types (`ObjectValue`, wrapper types, `BreakNode`/`ContinueNode` sentinels, etc.). New AST node types and runtime semantics generally land here.

`ScriptContext` represents an execution scope; SRM exposes `Run`, `CalcExpression`, and `CreateContext` for host integration.

### Closure semantics

Closures use lexical scoping. Each `FunctionObject` has a `CapturedScope` field set once at creation time (not at call time). This means escaped closures, independent closure instances, and mutable shared state all work correctly, matching JavaScript's closure behavior.

### Core types split out of `ScriptRunningMachine.cs`

[Source/ReoScript/Core/](Source/ReoScript/Core/) is an in-progress refactor pulling individual node/statement classes out of the monolithic `ScriptRunningMachine.cs`:

- `Core/Node/` — primitive value sentinels (`InfinityValue`, `MinusInfinityValue`, `NaNValue`, `ReturnNode`).
- `Core/Statement/` — `FunctionDefineNode`, `VariableDefineNode`, `StaticFunctionScope`, `MemberScopeModifier`, `ISyntaxTreeReturn` (the marker interface implemented by every value/node that flows through the interpreter).
- `Core/Reflection/` — `FunctionInfo`, `VariableInfo` (compile-time metadata).

[Source/ReoScript/AnonymousFunctionDefineNode.cs](Source/ReoScript/AnonymousFunctionDefineNode.cs) lives at the project root for the same reason. When adding new node types, prefer the `Core/` layout over adding to the root.

### JIT compiler prototype

A JIT compiler prototype (`ReoScriptCompiler`, `CompilerContext`) exists in `ScriptRunningMachine.cs` behind `#if REOSCRIPT_JIT`. It uses `System.Reflection.Emit` to generate IL. This is not yet functional and will be rewritten for .NET 10.

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
