using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Unvell.ReoScript;
using Unvell.ReoScript.Reflection;

namespace GetScriptInfo
{
	class Program
	{
		/// <summary>
		/// This example shows how to get the information about functions and variables that 
		/// is written in script from .NET program.
		/// 
		/// ReoScript provides the ability to get functions and variables information after 
		/// script is compiling. 
		/// 
		/// To use this feature you have to use the following namespaces:
		/// 
		/// - Unvell.ReoScript
		/// - Unvell.ReoScript.Reflection
		/// 
		/// The script's instruction information returned in a tree-style:
		/// 
		///     CompiledScript
		///         +- Functions
		///             +- Functions
		///                 +- Functions
		///                 +- Variables
		///             +- Variables
		///         +- Variables
		/// </summary>
		/// <param name="args"></param>
		static void Main(string[] args)
		{
			string script = @"

				function normal() {
					return 'normal function';
				}

				function outer(p1, p2, p3, p4) {
					
					function inner(key, value) { 
            var local = 10;

						function inner2(param) {
            }
					}

					return function(a, b) { return a + b; } ();
				}

				var af = function(x, y) { return x * y; };

				var result = af(2, 5);
		 		
			";

			ScriptRunningMachine srm = new ScriptRunningMachine();
			CompiledScript cs = srm.Compile(script, true);

			Console.WriteLine("Functions: ");
			Console.WriteLine();

			foreach (FunctionInfo fi in cs.DeclaredFunctions)
			{
				IterateFunction(fi, 0);
			}

			Console.WriteLine("Global Variables: ");
			Console.WriteLine();

			foreach (VariableInfo vi in cs.DeclaredVariables)
			{
				PrintOutLine("Variable Name", vi.Name, 0);
				PrintOutLine("  Has Initial Value", vi.HasInitialValue.ToString(), 0);
				PrintOutLine("  Position", vi.Line + ":" + vi.CharIndex, 0);
				Console.WriteLine();
			}


#if DEBUG
			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();
#endif
		}

		private static void IterateFunction(FunctionInfo fi, int indents)
		{
			PrintOutLine("Function Name", fi.Name, indents);
			PrintOutLine("  Is Anonymous", fi.IsAnonymous.ToString(), indents);
			PrintOutLine("  Is Nested", fi.IsInner.ToString(), indents);
			PrintOutLine("  Position", fi.Line + ":" + fi.CharIndex, indents);
			PrintOutLine("  Arguments", string.Join(", ", fi.Args), indents);
			PrintOutLine("  Local Variables ", string.Join(", ",
			fi.DeclaredLocalVariables.Select(vi => vi.Name).ToArray()), indents);

			Console.WriteLine();

			indents += 4;
			foreach (FunctionInfo innerFun in fi.DeclaredInnerFunctions)
			{
				IterateFunction(innerFun, indents);
			}
			indents -= 4;
		}

		private static void PrintOutLine(string key, string value, int indents)
		{
			Console.WriteLine(string.Format("{0,-" + indents + "} {1,-20}: {2}", " ", key, value));
		}
	}
}
