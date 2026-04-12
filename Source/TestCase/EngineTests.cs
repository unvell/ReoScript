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
	}
}
