# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ReoScript is an ECMAScript-like script language interpreter for .NET applications, allowing host applications to embed scripting and to expose .NET classes/objects/events directly to scripts. It is a tree-walking interpreter built on an ANTLR 3 grammar — there is no bytecode/JIT stage.

The library targets **.NET Framework 3.5** and references `System.Windows.Forms`. It is a legacy project; build with classic MSBuild (Visual Studio 2019 or `msbuild.exe`), not modern `dotnet build`.

## Solution Layout

[Source/ReoScript.sln](Source/ReoScript.sln) contains five projects:

| Project | Purpose |
|---|---|
| [Source/ReoScript/](Source/ReoScript/) | The interpreter library (`unvell.ReoScript.dll`). Root namespace `unvell.ReoScript`. |
| [Source/ReoScriptEditor/](Source/ReoScriptEditor/) | WinForms script editor (FastColoredTextBox-based) — recently changed from a library to a Windows application (commit `6e836b2`). |
| [Source/ReoScriptRunner/](Source/ReoScriptRunner/) | Command-line runner that executes `.reo` files. |
| [Source/ReoScriptExtensions/](Source/ReoScriptExtensions/) | Optional script-callable extensions (File I/O, Graphics). |
| [Source/TestCase/](Source/TestCase/) | Test runner — drives XML-defined language tests plus C# CLR-interop tests. |

Sample integrations live under [Samples/](Samples/) with their own [ReoScriptSamples.sln](Samples/ReoScriptSamples.sln) (CLREvent, CLRTypeImporting, DirectAccess, GameRS, NativeFunctionExtension, etc.).

## Building

```bash
# From Source/, build everything (Release):
msbuild ReoScript.sln /p:Configuration=Release

# Debug build:
msbuild ReoScript.sln /p:Configuration=Debug
```

The Release configuration of `ReoScript.csproj` defines `EXTERNAL_GETTER_SETTER` — a feature flag that toggles host-defined property getters/setters.

ANTLR runtime is vendored at [Source/ReoScript/Ref/Antlr3.Runtime.dll](Source/ReoScript/Ref/Antlr3.Runtime.dll); no NuGet restore is needed.

## Running Tests

Tests are run by the `TestCase` executable, not a unit-test framework. Two suites run back-to-back:

1. **Language tests** — XML files in [Source/TestCase/tests/](Source/TestCase/tests/) (e.g. `001_core.xml`, `021_fun.xml`, `070_async.xml`). Each XML file contains tagged `<test>` blocks of ReoScript source plus expected results.
2. **CLR interop tests** — C# test methods in [Source/TestCase/CLRTestCases.cs](Source/TestCase/CLRTestCases.cs).

```bash
# Convenience script (runs full Release build then all tests with default tags):
cd Source && run-test-cases.bat

# Or after building, run directly:
cd Source/TestCase/bin/Release
Unvell.ReoScript.TestCase.exe -etpfmc        # run all tests; -et<tag> enables a tag
Unvell.ReoScript.TestCase.exe 021_fun        # run a single XML test file by id
```

The CLI argument format (see [Source/TestCase/Program.cs](Source/TestCase/Program.cs)):
- Positional arg = test file id filter (e.g. `021_fun` matches `021_fun.xml`).
- `-et<tag>` enables a tag — pass once per tag. The default `-etpfmc` enables tags `p`, `f`, `m`, `c`. Tests without an enabled tag are skipped.

The process exits with code `1` on any failure, `0` if everything passes — `run-test-cases.bat` keys off this.

## Architecture

### Pipeline

1. **Lexing/Parsing** — ANTLR 3 generated lexer/parser. The authoritative grammar is [Source/ReoScript/ReoScript.g](Source/ReoScript/ReoScript.g) (recovered from a 2015 backup); regenerate `ReoScriptLexer.cs` / `ReoScriptParser.cs` from it with ANTLR 3 if syntax changes. The big generated files at the project root — [Source/ReoScript/ReoScriptLexer.cs](Source/ReoScript/ReoScriptLexer.cs) (~5.4k LOC) and [Source/ReoScript/ReoScriptParser.cs](Source/ReoScript/ReoScriptParser.cs) (~12.9k LOC) — are the ANTLR output. The much smaller files under [Source/ReoScript/Core/Grammar/](Source/ReoScript/Core/Grammar/) are **`partial class` extensions** of those generated classes (hand-written constants and helpers like `HIDDEN`, `MAX_TOKENS`, `REPLACED_TREE`), not duplicates — keeping them in separate files means re-running ANTLR doesn't clobber the hand-written parts.
2. **Pre-interpret pass** — constant folding, syntax-error promotion, function-info collection. See `FunctionInfo`/`VariableInfo` under [Source/ReoScript/Core/Reflection/](Source/ReoScript/Core/Reflection/).
3. **Tree walking** — [Source/ReoScript/ScriptRunningMachine.cs](Source/ReoScript/ScriptRunningMachine.cs) (~9k LOC) is the heart of the project. It contains the `ScriptRunningMachine` (SRM) class plus most built-in object types (`ObjectValue`, wrapper types, `BreakNode`/`ContinueNode` sentinels, etc.). New AST node types and runtime semantics generally land here.

`ScriptContext` represents an execution scope; SRM exposes `Run`, `CalcExpression`, and `CreateContext` for host integration.

### Core types split out of `ScriptRunningMachine.cs`

[Source/ReoScript/Core/](Source/ReoScript/Core/) is an in-progress refactor pulling individual node/statement classes out of the monolithic `ScriptRunningMachine.cs`:

- `Core/Node/` — primitive value sentinels (`InfinityValue`, `MinusInfinityValue`, `NaNValue`, `ReturnNode`).
- `Core/Statement/` — `FunctionDefineNode`, `VariableDefineNode`, `StaticFunctionScope`, `MemberScopeModifier`, `ISyntaxTreeReturn` (the marker interface implemented by every value/node that flows through the interpreter).
- `Core/Reflection/` — `FunctionInfo`, `VariableInfo` (compile-time metadata).

[Source/ReoScript/AnonymousFunctionDefineNode.cs](Source/ReoScript/AnonymousFunctionDefineNode.cs) lives at the project root for the same reason. When adding new node types, prefer the `Core/` layout over adding to the root.

### Host-integration surface

- **Direct .NET access** — gated by `MachineWorkMode` flags (`AllowDirectAccess`, `AllowImportTypeInScript`, `AllowCLREventBind`). Scripts can `import` .NET namespaces and call methods/bind events on real CLR objects.
- **Native function extension** — host code registers C# delegates as script-callable functions on the SRM or on script objects.
- **Built-in scripts** — [Source/ReoScript/scripts/](Source/ReoScript/scripts/) (`core.reo`, `array.reo`, `array_ext.reo`, `number.reo`, `debug.reo`) are loaded as embedded resources to define prototype methods that are easier to express in ReoScript than C#.

### File extension

`.reo` is the canonical script extension (changed from `.rs` in v1.3.1, see [Source/ChangeLog.txt](Source/ChangeLog.txt)).

## Editing notes

- Recent commit `c268849 rename namespace` reorganized namespaces — the codebase currently mixes `unvell.ReoScript`, `unvell.ReoScript.Core`, `unvell.ReoScript.Core.Statement`, `unvell.ReoScript.Runtime`, `unvell.ReoScript.Parsers`, `unvell.ReoScript.Reflection`. Check `using` directives at the top of `ScriptRunningMachine.cs` before adding new types.
- The grammar supports an HTML-tag literal syntax (`tag`, `tagAttr`, `tagTemplateDefine` rules in [Source/ReoScript/ReoScript.g](Source/ReoScript/ReoScript.g)) — `template<TypeName>(args) <tag/>` declares a named, parameterized tag template. This was added before JSX was widespread; it predates React's release. Tag-related token IDs (`TAG`, `TAG_ATTR`, `TAG_NAME`, `TEMPLATE_DEFINE`, `TEMPLATE_TAG`) are still emitted by the parser.
- The csproj is hand-maintained MSBuild XML — when adding a `.cs` file, remember to add a `<Compile Include=...>` entry.
- There is no formatter/linter configured; follow the surrounding tab-indented, K&R-brace style used in `ScriptRunningMachine.cs`.
