using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Unvell.ReoScript.Extensions;

namespace Unvell.ReoScript
{
	class ConsoleRunnerProgram
	{
		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine(
@"ReoScript(TM) Running Machine
Copyright(c) 2012-2013 unvell, All Rights Reserved.

Usage: ReoScript.exe <file> [-workpath|-debug|-exec]");
				return;
			}

			List<string> files = new List<string>();
			string workPath = null;
			bool debug = false;
			string initScript = null;

			for (int i = 0; i < args.Length; i++)
			{
				string arg = args[i];

				if (arg.StartsWith("-"))
				{
					string param = arg.Substring(1);

					switch (param)
					{
						case "workpath":
							workPath = args[i + 1];
							i++;
							break;

						case "debug":
							debug = true;
							break;

						case "exec":
							initScript = args[i + 1];
							i++;
							break;
					}
				}
				else
				{
					files.Add(arg);
				}
			}

			List<FileInfo> sourceFiles = new List<FileInfo>();

			foreach (string file in files)
			{
				FileInfo fi = new FileInfo(string.IsNullOrEmpty(workPath)
					? file : Path.Combine(workPath, file));

				if (!fi.Exists)
				{
					Console.WriteLine("Resource not found: " + fi.FullName);
				}
				else
				{
					sourceFiles.Add(fi);

					if (string.IsNullOrEmpty(workPath))
					{
						workPath = fi.DirectoryName;
					}
				}
			}

			if (string.IsNullOrEmpty(workPath))
			{
				workPath = Environment.CurrentDirectory;
			}

			// create SRM
			ScriptRunningMachine srm = new ScriptRunningMachine(CoreFeatures.FullFeatures);
			if (debug)
			{
				new ScriptDebugger(srm);
			}

			srm.WorkPath = workPath;
			srm.AddStdOutputListener(new BuiltinConsoleOutputListener());

			srm.SetGlobalVariable("File", new FileConstructorFunction());

			try
			{
				foreach (FileInfo file in sourceFiles)
				{
					// load main script
					srm.Run(file);
				}

				if (!string.IsNullOrEmpty(initScript))
				{
					srm.Run(initScript);
				}
			}
			catch (ReoScriptRuntimeException ex)
			{
				string str = "ReoScript Error";
				if (ex.Position != null)
				{
					str += string.Format(" at char {0} in line {1}", ex.Position.CharIndex, ex.Position.Line);
				}
				//if (ex.Position.CallStack != null)
				//{
				//  foreach (CallStackInfo csi in ex.Position.CallStack)
				//  {
				//    str += string.Format("\t{0}:{1}\n", csi.FilePath, csi.Line);
				//  }
				//}
				str += "\n\n" + ex.Message;
				Console.WriteLine(str);
			}
		}
	}
}
