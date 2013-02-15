///////////////////////////////////////////////////////////////////////////////
// 
// ReoScript
// 
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
// PURPOSE.
//
// License: GNU Lesser General Public License (LGPLv3)
//
// Jing, Lu (lujing@unvell.com)
//
// Copyright (C) unvell.com, 2013. All Rights Reserved
//
// PID: 0D4F1DD2-93B5-4359-9FAA-AB697EEEB9DD
//
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Unvell.ReoScript.Editor
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new ReoScriptEditor());
		}
	}
}
