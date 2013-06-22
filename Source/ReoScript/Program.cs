///////////////////////////////////////////////////////////////////////////////
// 
// ReoScript 
// http://www.unvell.com/ReoScript
// 
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
// PURPOSE.
//
// GNU Lesser General Public License (LGPLv3)
//
// lujing@unvell.com
// Copyright (C) unvell, 2012-2013. All Rights Reserved
//
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Unvell.ReoScript
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			
			new MachineConsole(args).Run();
		}
	}
}
