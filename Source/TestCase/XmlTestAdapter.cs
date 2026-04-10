using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Xunit;
using unvell.ReoScript;
using unvell.ReoScript.Diagnostics;

namespace unvell.ReoScript.TestCase
{
	/// <summary>
	/// xUnit adapter that loads the existing XML test suites as [Theory] test data.
	/// Each XML test-case becomes an individual xUnit test.
	/// </summary>
	public class XmlLanguageTests
	{
		public static IEnumerable<object[]> GetTestCases()
		{
			string testsDir = Path.Combine(AppContext.BaseDirectory, "tests");
			if (!Directory.Exists(testsDir)) yield break;

			var serializer = new XmlSerializer(typeof(XmlTestSuite));

			foreach (string file in Directory.GetFiles(testsDir, "*.xml").OrderBy(f => f))
			{
				XmlTestSuite suite;
				using (var stream = File.OpenRead(file))
				{
					suite = serializer.Deserialize(stream) as XmlTestSuite;
				}

				if (suite?.TestCases == null) continue;

				foreach (var tc in suite.TestCases)
				{
					if (tc.Disabled) continue;

					var testCode = tc.Script;
					if (string.IsNullOrEmpty(testCode)) testCode = tc.TestCode;
					if (string.IsNullOrEmpty(testCode)) continue;

					string label = string.Format("[{0}-{1} {2}] {3}",
						suite.Id?.PadLeft(3, '0'),
						tc.Id?.PadLeft(3, '0'),
						suite.Name ?? "",
						tc.Name ?? "").Trim();

					yield return new object[] { label, testCode };
				}
			}
		}

		[Theory]
		[MemberData(nameof(GetTestCases))]
		public void RunXmlTestCase(string label, string script)
		{
			var srm = new ScriptRunningMachine();
			// ScriptDebugger installs the 'debug' global object (debug.assert, etc.)
			var debugger = new ScriptDebugger(srm);
			srm.Run(script);
		}
	}

	/// <summary>
	/// xUnit adapter for the CLR interop test cases defined in CLRTestCases.cs.
	/// Uses reflection to discover methods with [TestCaseAttribute].
	/// </summary>
	public class ClrInteropTests
	{
		public static IEnumerable<object[]> GetTestCases()
		{
			foreach (Type type in typeof(ClrInteropTests).Assembly.GetTypes())
			{
				var suiteAttrs = type.GetCustomAttributes(typeof(TestSuiteAttribute), true)
					as TestSuiteAttribute[];
				if (suiteAttrs == null || suiteAttrs.Length == 0) continue;

				string suiteName = suiteAttrs[0].Name;
				if (string.IsNullOrEmpty(suiteName)) suiteName = type.Name;

				foreach (var method in type.GetMethods())
				{
					var caseAttrs = method.GetCustomAttributes(typeof(TestCaseAttribute), false)
						as TestCaseAttribute[];
					if (caseAttrs == null || caseAttrs.Length == 0 || caseAttrs[0].Disabled) continue;

					string caseName = caseAttrs[0].Desc;
					if (string.IsNullOrEmpty(caseName)) caseName = method.Name;

					string label = string.Format("[{0}] {1}", suiteName, caseName);

					yield return new object[] { label, type.FullName, method.Name, caseAttrs[0].WorkMode };
				}
			}
		}

		[Theory]
		[MemberData(nameof(GetTestCases))]
		public void RunClrTestCase(string label, string typeName, string methodName, MachineWorkMode workMode)
		{
			Type type = typeof(ClrInteropTests).Assembly.GetType(typeName);
			object instance = Activator.CreateInstance(type);

			if (instance is ReoScriptTestSuite testSuite)
			{
				var srm = new ScriptRunningMachine();
				srm.WorkMode = workMode;
				var debugger = new ScriptDebugger(srm);
				testSuite.SRM = srm;
			}

			var method = type.GetMethod(methodName);
			method.Invoke(instance, null);
		}
	}
}
