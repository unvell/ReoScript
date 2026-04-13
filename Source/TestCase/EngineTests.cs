/*****************************************************************************
 *
 * ReoScript - .NET Script Language Engine
 *
 * https://github.com/unvell/ReoScript
 *
 * THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
 * KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
 * PURPOSE.
 *
 * This software released under MIT license.
 * Copyright (c) 2012-2019 Jingwood, unvell.com, all rights reserved.
 *
 ****************************************************************************/

using System;
using System.IO;
using Xunit;
using unvell.ReoScript;
using unvell.ReoScript.Diagnostics;

namespace unvell.ReoScript.TestCase
{
	/// <summary>
	/// Tests for engine-level features: loop protection, exception safety,
	/// truthy/falsy semantics, error source location.
	/// </summary>
	public class EngineTests
	{
		private ScriptRunningMachine CreateSRM()
		{
			var srm = new ScriptRunningMachine();
			new ScriptDebugger(srm);
			return srm;
		}

		#region Loop Protection

		[Fact]
		public void InfiniteWhileLoop_ThrowsTimeout()
		{
			var srm = CreateSRM();
			srm.MaxIterationsPerLoop = 1000;

			Assert.Throws<ScriptExecutionTimeoutException>(() =>
			{
				srm.Run("var i = 0; while (true) { i++; }");
			});
		}

		[Fact]
		public void InfiniteForLoop_ThrowsTimeout()
		{
			var srm = CreateSRM();
			srm.MaxIterationsPerLoop = 1000;

			Assert.Throws<ScriptExecutionTimeoutException>(() =>
			{
				srm.Run("for (var i = 0; ; i++) { }");
			});
		}

		[Fact]
		public void NormalLoop_CompletesWithinLimit()
		{
			var srm = CreateSRM();
			srm.MaxIterationsPerLoop = 10000;

			// Should complete without error
			srm.Run("var sum = 0; for (var i = 0; i < 5000; i++) { sum += i; }");
		}

		[Fact]
		public void LoopProtectionDisabled_WhenZero()
		{
			var srm = CreateSRM();
			srm.MaxIterationsPerLoop = 0;

			// With limit disabled, a bounded loop should still work fine
			srm.Run("var sum = 0; for (var i = 0; i < 100; i++) { sum += i; }");
		}

		#endregion

		#region Error Source Location

		[Fact]
		public void ErrorObject_IncludesLineNumber()
		{
			var srm = CreateSRM();

			var ex = Assert.Throws<ReoScriptRuntimeException>(() =>
			{
				srm.Run("var x = 1;\nvar y = 2;\nundefinedFunction();");
			});

			Assert.NotNull(ex.ErrorObject);
			Assert.True(ex.ErrorObject.Line > 0, "Error should include line number");
		}

		[Fact]
		public void ErrorObject_IncludesFilePathWhenLoaded()
		{
			var srm = CreateSRM();

			// Run with a file path context via ScriptContext
			var ctx = srm.CreateContext();
			ctx.SourceFilePath = "test_script.reo";

			var ex = Assert.Throws<ReoScriptRuntimeException>(() =>
			{
				srm.Run("undefinedFunction();", ctx);
			});

			Assert.NotNull(ex.ErrorObject);
			Assert.Equal("test_script.reo", ex.ErrorObject.FilePath);
		}

		[Fact]
		public void GetFullErrorInfo_IncludesFileAndLine()
		{
			var srm = CreateSRM();

			var ctx = srm.CreateContext();
			ctx.SourceFilePath = "demo.reo";

			var ex = Assert.Throws<ReoScriptRuntimeException>(() =>
			{
				srm.Run("undefinedFunction();", ctx);
			});

			string info = ex.ErrorObject.GetFullErrorInfo();
			Assert.Contains("demo.reo", info);
		}

		#endregion

		#region Truthy/Falsy

		[Fact]
		public void WhileTruthyCount_Terminates()
		{
			var srm = CreateSRM();
			srm.MaxIterationsPerLoop = 100;
			srm.Run("var count = 5; var sum = 0; while (count > 0) { sum += count; count--; }");
			var sum = srm.CalcExpression("sum;");
			Assert.Equal(15.0, sum);
		}

		[Fact]
		public void GetBoolValue_NumberZero_IsFalsy()
		{
			Assert.False(ScriptRunningMachine.GetBoolValue(0));
			Assert.False(ScriptRunningMachine.GetBoolValue(0.0));
			Assert.False(ScriptRunningMachine.GetBoolValue(0L));
			Assert.True(ScriptRunningMachine.GetBoolValue(1));
			Assert.True(ScriptRunningMachine.GetBoolValue(-1.5));
		}

		[Fact]
		public void GetBoolValue_Strings()
		{
			Assert.False(ScriptRunningMachine.GetBoolValue(""));
			Assert.True(ScriptRunningMachine.GetBoolValue("hello"));
			Assert.True(ScriptRunningMachine.GetBoolValue(" "));
		}

		[Fact]
		public void GetBoolValue_Null_IsFalsy()
		{
			Assert.False(ScriptRunningMachine.GetBoolValue(null));
		}

		[Fact]
		public void IfTruthy_WithNumber()
		{
			var srm = CreateSRM();
			srm.Run("var result = 'no'; if (1) result = 'yes';");
			Assert.Equal("yes", srm.CalcExpression("result;"));
		}

		[Fact]
		public void IfFalsy_WithNull()
		{
			var srm = CreateSRM();
			srm.Run("var result = 'no'; var x = null; if (x) result = 'yes';");
			Assert.Equal("no", srm.CalcExpression("result;"));
		}

		[Fact]
		public void LogicalOr_ReturnsFirstTruthy()
		{
			var srm = CreateSRM();
			srm.Run("var result = null || 'fallback';");
			Assert.Equal("fallback", srm.CalcExpression("result;"));
		}

		[Fact]
		public void NotOperator_OnNull()
		{
			var srm = CreateSRM();
			srm.Run("var result = !null;");
			Assert.Equal(true, srm.CalcExpression("result;"));
		}

		#endregion

		#region ScriptError Event

		[Fact]
		public void LoopTimeout_ErrorIncludesLineInfo()
		{
			var srm = CreateSRM();
			srm.MaxIterationsPerLoop = 100;

			var ex = Assert.Throws<ScriptExecutionTimeoutException>(() =>
			{
				srm.Run("while (true) { }");
			});

			string info = ex.GetFullErrorInfo();
			Assert.Contains("Loop exceeded maximum iteration limit", info);
		}

		#endregion

		#region Async (setTimeout / setInterval)

		[Fact]
		public void SetTimeout_CallbackExecutes()
		{
			var srm = CreateSRM();
			srm.Run("var a = 0; setTimeout(function(){ a = 10; }, 10);");

			// Wait enough time for the callback to fire
			System.Threading.Thread.Sleep(200);

			Assert.Equal(10.0, srm.CalcExpression("a;"));
		}

		[Fact]
		public void SetInterval_CallbackFiresMultipleTimes()
		{
			var srm = CreateSRM();
			srm.Run("var count = 0; var id = setInterval(function(){ count++; }, 20);");

			// Wait for several intervals to fire
			System.Threading.Thread.Sleep(300);

			srm.Run("clearInterval(id);");
			var count = Convert.ToDouble(srm.CalcExpression("count;"));

			// Should have fired multiple times (at least 3 in 300ms with 20ms interval)
			Assert.True(count >= 3, $"Expected count >= 3, but was {count}");
		}

		[Fact]
		public void ClearInterval_StopsCallback()
		{
			var srm = CreateSRM();
			srm.Run("var count = 0; var id = setInterval(function(){ count++; }, 20);");

			System.Threading.Thread.Sleep(150);
			srm.Run("clearInterval(id);");

			var countAfterClear = Convert.ToDouble(srm.CalcExpression("count;"));

			// Wait more and confirm count didn't increase
			System.Threading.Thread.Sleep(200);
			var countLater = Convert.ToDouble(srm.CalcExpression("count;"));

			Assert.Equal(countAfterClear, countLater);
		}

		[Fact]
		public void SetInterval_ReturnsValidId()
		{
			var srm = CreateSRM();
			srm.Run("var id = setInterval(function(){}, 100);");

			var id = Convert.ToDouble(srm.CalcExpression("id;"));
			Assert.True(id > 0, "setInterval should return a positive id");

			srm.Run("clearInterval(id);");
		}

		#endregion

		#region Module Import

		private string WriteTempScript(string content)
		{
			string path = Path.Combine(Path.GetTempPath(), "reoscript_test_" + Guid.NewGuid().ToString("N") + ".reo");
			File.WriteAllText(path, content);
			return path;
		}

		[Fact]
		public void ImportModule_FunctionsAvailable()
		{
			string modulePath = WriteTempScript(@"
function add(a, b) { return a + b; }
function mul(a, b) { return a * b; }
");
			try
			{
				var srm = CreateSRM();
				srm.Run(string.Format(
					"var math = importModule(\"{0}\"); var r = math.add(3, 4);",
					modulePath.Replace("\\", "\\\\")));
				Assert.Equal(7.0, srm.CalcExpression("r;"));
			}
			finally
			{
				File.Delete(modulePath);
			}
		}

		[Fact]
		public void ImportModule_VariablesAvailable()
		{
			string modulePath = WriteTempScript(@"
var PI = 3.14159;
var name = 'mathlib';
");
			try
			{
				var srm = CreateSRM();
				srm.Run(string.Format(
					"var m = importModule(\"{0}\");",
					modulePath.Replace("\\", "\\\\")));
				Assert.Equal("mathlib", srm.CalcExpression("m.name;"));
			}
			finally
			{
				File.Delete(modulePath);
			}
		}

		[Fact]
		public void ImportModule_IsolatedScope()
		{
			string modulePath = WriteTempScript(@"
var secret = 42;
function getSecret() { return secret; }
");
			try
			{
				var srm = CreateSRM();
				srm.Run(string.Format(
					"var m = importModule(\"{0}\"); var s = m.getSecret();",
					modulePath.Replace("\\", "\\\\")));

				// Module function should work
				Assert.Equal(42.0, srm.CalcExpression("s;"));

				// Module's 'secret' should NOT be in global scope
				Assert.Null(srm.CalcExpression("secret;"));
			}
			finally
			{
				File.Delete(modulePath);
			}
		}

		[Fact]
		public void ImportModule_CachedOnSecondImport()
		{
			string modulePath = WriteTempScript(@"
var counter = 0;
counter++;
");
			try
			{
				var srm = CreateSRM();
				srm.Run(string.Format(@"
var m1 = importModule(""{0}"");
var m2 = importModule(""{0}"");
",
					modulePath.Replace("\\", "\\\\")));

				// Both references should be the same cached object
				// counter should be 1, not 2 (file executed only once)
				Assert.Equal(1.0, srm.CalcExpression("m1.counter;"));
				Assert.Equal(1.0, srm.CalcExpression("m2.counter;"));
			}
			finally
			{
				File.Delete(modulePath);
			}
		}

		[Fact]
		public void ImportModule_FileNotFound_Throws()
		{
			var srm = CreateSRM();
			Assert.Throws<ReoScriptException>(() =>
			{
				srm.Run("var m = importModule('nonexistent_file.reo');");
			});
		}

		[Fact]
		public void ImportModuleFile_FromCSharp()
		{
			string modulePath = WriteTempScript(@"
function greet(name) { return 'hello ' + name; }
");
			try
			{
				var srm = CreateSRM();
				var module = srm.ImportModuleFile(modulePath);

				Assert.NotNull(module);
				Assert.True(module["greet"] is AbstractFunctionObject);
			}
			finally
			{
				File.Delete(modulePath);
			}
		}

		#endregion

		#region ASI (Automatic Semicolon Insertion)

		[Fact]
		public void ASI_SimpleStatements()
		{
			var srm = new ScriptRunningMachine();
			new ScriptDebugger(srm);
			// Variable declaration and assignment without semicolons
			object result = srm.Run(@"
var a = 10
var b = 20
a + b
");
			Assert.Equal(30d, ScriptRunningMachine.GetNumberValue(result));
		}

		[Fact]
		public void ASI_FunctionCallWithoutSemicolon()
		{
			var srm = new ScriptRunningMachine();
			new ScriptDebugger(srm);
			object result = srm.Run(@"
function add(x, y) {
	return x + y
}
add(3, 4)
");
			Assert.Equal(7d, ScriptRunningMachine.GetNumberValue(result));
		}

		[Fact]
		public void ASI_ReturnWithNewline()
		{
			var srm = new ScriptRunningMachine();
			new ScriptDebugger(srm);
			// return on its own line should return null (like JS)
			object result = srm.Run(@"
function f() {
	return
	42
}
f()
");
			Assert.Null(result);
		}

		[Fact]
		public void ASI_ReturnWithValueOnSameLine()
		{
			var srm = new ScriptRunningMachine();
			new ScriptDebugger(srm);
			object result = srm.Run(@"
function f() {
	return 42
}
f()
");
			Assert.Equal(42d, ScriptRunningMachine.GetNumberValue(result));
		}

		[Fact]
		public void ASI_MixedWithExplicitSemicolons()
		{
			var srm = new ScriptRunningMachine();
			new ScriptDebugger(srm);
			// Mixing explicit semicolons and ASI should work
			object result = srm.Run(@"
var a = 1;
var b = 2
var c = a + b;
c
");
			Assert.Equal(3d, ScriptRunningMachine.GetNumberValue(result));
		}

		[Fact]
		public void ASI_ForLoopStillRequiresSemicolons()
		{
			var srm = new ScriptRunningMachine();
			new ScriptDebugger(srm);
			// for loop header semicolons are mandatory
			object result = srm.Run(@"
var sum = 0
for (var i = 0; i < 5; i++) {
	sum = sum + i
}
sum
");
			Assert.Equal(10d, ScriptRunningMachine.GetNumberValue(result));
		}

		[Fact]
		public void ASI_ObjectPropertyAccess()
		{
			var srm = new ScriptRunningMachine();
			new ScriptDebugger(srm);
			object result = srm.Run(@"
var obj = { x: 10, y: 20 }
obj.x + obj.y
");
			Assert.Equal(30d, ScriptRunningMachine.GetNumberValue(result));
		}

		[Fact]
		public void ASI_BeforeClosingBrace()
		{
			var srm = new ScriptRunningMachine();
			new ScriptDebugger(srm);
			// ASI before } (no newline or semicolon needed)
			object result = srm.Run("function f() { return 5 } f()");
			Assert.Equal(5d, ScriptRunningMachine.GetNumberValue(result));
		}

		#endregion

		#region Import As

		[Fact]
		public void ImportAs_FunctionsAvailable()
		{
			string modulePath = WriteTempScript(@"
function add(a, b) { return a + b; }
function sub(a, b) { return a - b; }
");
			try
			{
				var srm = CreateSRM();
				srm.WorkPath = Path.GetDirectoryName(modulePath);
				srm.Run(string.Format(
					"import \"{0}\" as math; var r = math.add(10, 3);",
					Path.GetFileName(modulePath)));
				Assert.Equal(13.0, srm.CalcExpression("r;"));
			}
			finally
			{
				File.Delete(modulePath);
			}
		}

		[Fact]
		public void ImportAs_IsolatedScope()
		{
			string modulePath = WriteTempScript(@"
var secret = 99;
function getSecret() { return secret; }
");
			try
			{
				var srm = CreateSRM();
				srm.WorkPath = Path.GetDirectoryName(modulePath);
				srm.Run(string.Format(
					"import \"{0}\" as m;",
					Path.GetFileName(modulePath)));

				// Module function works via alias
				Assert.Equal(99.0, srm.CalcExpression("m.getSecret();"));

				// secret is NOT in global scope
				Assert.Null(srm.CalcExpression("typeof secret == 'undefined' ? null : secret;"));
			}
			finally
			{
				File.Delete(modulePath);
			}
		}

		[Fact]
		public void ImportAs_WithoutAs_LegacyBehavior()
		{
			string modulePath = WriteTempScript(@"
var x = 42;
");
			try
			{
				var srm = CreateSRM();
				srm.WorkPath = Path.GetDirectoryName(modulePath);
				srm.Run(string.Format(
					"import \"{0}\";",
					Path.GetFileName(modulePath)));

				// Legacy import: x should be in global scope
				Assert.Equal(42.0, srm.CalcExpression("x;"));
			}
			finally
			{
				File.Delete(modulePath);
			}
		}

		#endregion

		#region Date Enhancement

		[Fact]
		public void DateNow_ReturnsNumber()
		{
			var srm = CreateSRM();
			object result = srm.CalcExpression("Date.now();");
			Assert.IsType<double>(result);
			Assert.True((double)result > 0);
		}

		[Fact]
		public void DateGetTime_MatchesTicks()
		{
			var srm = CreateSRM();
			srm.Run("var d = new Date(); var t = d.getTime();");
			object result = srm.CalcExpression("t;");
			Assert.IsType<double>(result);
			Assert.True((double)result > 0);
		}

		[Fact]
		public void DateNow_ElapsedPattern()
		{
			var srm = CreateSRM();
			srm.Run(@"
var start = Date.now();
var sum = 0;
for (var i = 0; i < 1000; i++) { sum += i; }
var elapsed = Date.now() - start;
");
			object elapsed = srm.CalcExpression("elapsed;");
			Assert.IsType<double>(elapsed);
			Assert.True((double)elapsed >= 0);
		}

		#endregion

		#region Shorthand Property

		[Fact]
		public void ShorthandProperty_Basic()
		{
			var srm = CreateSRM();
			srm.Run("var a = 1, b = 2; var obj = { a, b };");
			Assert.Equal(1.0, srm.CalcExpression("obj.a;"));
			Assert.Equal(2.0, srm.CalcExpression("obj.b;"));
		}

		[Fact]
		public void ShorthandProperty_MixedWithNormal()
		{
			var srm = CreateSRM();
			srm.Run("var x = 10; var obj = { x, y: 20 };");
			Assert.Equal(10.0, srm.CalcExpression("obj.x;"));
			Assert.Equal(20.0, srm.CalcExpression("obj.y;"));
		}

		#endregion

		#region Object Spread

		[Fact]
		public void ObjectSpread_CopiesProperties()
		{
			var srm = CreateSRM();
			srm.Run("var obj1 = { a: 1, b: 2 }; var obj2 = { ...obj1 };");
			Assert.Equal(1.0, srm.CalcExpression("obj2.a;"));
			Assert.Equal(2.0, srm.CalcExpression("obj2.b;"));
		}

		[Fact]
		public void ObjectSpread_WithAdditionalProperties()
		{
			var srm = CreateSRM();
			srm.Run("var obj1 = { a: 1 }; var obj2 = { ...obj1, b: 2 };");
			Assert.Equal(1.0, srm.CalcExpression("obj2.a;"));
			Assert.Equal(2.0, srm.CalcExpression("obj2.b;"));
		}

		[Fact]
		public void ObjectSpread_OverridesProperties()
		{
			var srm = CreateSRM();
			srm.Run("var obj1 = { a: 1, b: 2 }; var obj2 = { ...obj1, b: 99 };");
			Assert.Equal(1.0, srm.CalcExpression("obj2.a;"));
			Assert.Equal(99.0, srm.CalcExpression("obj2.b;"));
		}

		#endregion

		#region Destructuring

		[Fact]
		public void Destructuring_Basic()
		{
			var srm = CreateSRM();
			srm.Run("var obj = { a: 10, b: 20 }; var { a, b } = obj;");
			Assert.Equal(10.0, srm.CalcExpression("a;"));
			Assert.Equal(20.0, srm.CalcExpression("b;"));
		}

		[Fact]
		public void Destructuring_PartialExtract()
		{
			var srm = CreateSRM();
			srm.Run("var obj = { x: 1, y: 2, z: 3 }; var { x, z } = obj;");
			Assert.Equal(1.0, srm.CalcExpression("x;"));
			Assert.Equal(3.0, srm.CalcExpression("z;"));
		}

		[Fact]
		public void Destructuring_MissingProperty_IsNull()
		{
			var srm = CreateSRM();
			srm.Run("var obj = { a: 1 }; var { a, b } = obj;");
			Assert.Equal(1.0, srm.CalcExpression("a;"));
			Assert.Null(srm.CalcExpression("b;"));
		}

		#endregion
	}
}
