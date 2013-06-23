using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unvell.ReoScript;

namespace ConsoleRunner
{
	class Program
	{
		static void Main(string[] args)
		{

			// Create ScriptRunningMachine console and run it
			new MachineConsole(args).Run();

/*

ReoScript Machine Console Help

/<system command>       submit system command.
  quit | q              quit from console.
  help | h              show this topic.

?[experssion]           calculate value of an expression and output return value.
                        show all varaibles in current global object if
                        expression be ignored.

<statement>;						run ReoScript statement.

*/

		}
	}
}
