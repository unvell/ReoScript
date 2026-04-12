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

}
