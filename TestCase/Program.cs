///////////////////////////////////////////////////////////////////////////////
// 
// ReoScript 
// 
// HP: http://www.unvell.com/ReoScript
// 
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
// PURPOSE.
//
// License: GNU Lesser General Public License (LGPLv3)
//
// Email: lujing@unvell.com
//
// Copyright (C) unvell, 2012-2013. All Rights Reserved
//
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Unvell.ReoScript.Editor;
using Unvell.ReoScript;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;

namespace Unvell.ReoScript.TestCase
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static int Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			bool hasErrors = new TestCaseRunner().Run(args.Length > 0 ? args[0] : null);
			
			//using (ReoScriptEditor editor = new ReoScriptEditor())
			//{
			//  using (StreamReader sr = new StreamReader(new FileStream("scripts/winform.rs", FileMode.Open)))
			//  {
			//    editor.Srm.WorkMode |= MachineWorkMode.AllowDirectAccess 
			//      | MachineWorkMode.AllowImportTypeInScript | MachineWorkMode.AllowCLREventBind;

			//    editor.Script = sr.ReadToEnd();
			//  }

			//  Application.Run(editor);
			//}
			return hasErrors ? 1 : 0;
		}
	}

	class TestCaseFailureException : Exception{
		public TestCaseFailureException(string msg) :
			base(msg) { }
	}
	
	class TestCaseRunner
	{
		public TestCaseRunner()
		{
		}

		private static readonly XmlSerializer xmlSuiteSerializer = new XmlSerializer(typeof(XmlTestSuite));

		public bool Run(string testCaseId)
		{
			bool hasErrors = false;

			ScriptRunningMachine srm = new ScriptRunningMachine();
			ScriptDebugger debugMonitor = new ScriptDebugger(srm);

			int testCases = 0, success = 0, failed = 0;

			foreach (string filename in Directory.GetFiles("tests"))
			{
				XmlTestSuite suite = xmlSuiteSerializer.Deserialize(File.OpenRead(filename)) as XmlTestSuite;

				if (suite != null)
				{
					testCases += suite.TestCases.Count;
				}

				DateTime dt;

				suite.TestCases.ForEach(t =>
				{
					string caseId = string.Format("{0,3}-{1,3}", suite.Id, t.Id);

					if (t.Disabled || string.IsNullOrEmpty(t.Script)
						|| (!string.IsNullOrEmpty(testCaseId) && testCaseId!= caseId)) 
						return;

					srm.Reset();

					Console.Write("[{0,6} {1,-10}] {2,-30} : ", caseId, suite.Name, t.Name);

					dt = DateTime.Now;
	
					try
					{
						srm.Run(t.Script);

						long elapsed = (DateTime.Now - dt).Milliseconds;
						
						success++;
						Console.WriteLine("{0,5} ms.", elapsed);
					}
					catch (Exception ex)
					{
						failed++;
						Console.WriteLine(string.IsNullOrEmpty(ex.Message) ? "failed"
							: "failed: " + ex.Message);
						hasErrors = true;
					}
				});

				//Console.WriteLine();
			}

			Console.WriteLine("\n    {0,3} test cases, {1} successed, {2} failed, {3} skipped",
				testCases, success, failed, (testCases - success - failed));
			Console.WriteLine("  {0,5} objects created.\n", debugMonitor.TotalObjectCreated);

			return hasErrors;
		}
	}

	[XmlRoot("test-suite")]
	public class XmlTestSuite
	{
		[XmlAttribute("id")]
		public string Id { get; set; }

		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlElement("test-case")]
		public List<XmlTestCase> TestCases { get; set; }
	}

	public class XmlTestCase
	{
		[XmlAttribute("id")]
		public string Id { get; set; }

		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlElement("script")]
		public string Script { get; set; }

		[XmlAttribute("disabled")]
		public bool Disabled { get; set; }

	}
}
