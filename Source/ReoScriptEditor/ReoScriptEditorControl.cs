/*****************************************************************************
 * 
 * ReoScript - .NET Script Language Engine
 * 
 * http://www.unvell.com/ReoScript
 *
 * THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
 * KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
 * PURPOSE.
 *
 * This software released under LGPLv3 license.
 * Author: Jing Lu <dujid0@gmail.com>
 * 
 * Copyright (c) 2012-2013 unvell.com, all rights reserved.
 * 
 ****************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace Unvell.ReoScript.Editor
{
	public partial class ReoScriptEditorControl : UserControl
	{
		public ReoScriptEditorControl()
		{
			InitializeComponent();
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ScriptRunningMachine Srm { get; set; }

		public FastColoredTextBox Fctb
		{
			get { return fctb; }
			set { fctb = value; }
		}

		public override string Text
		{
			get
			{
				return fctb.Text;
			}
			set
			{
				fctb.Text = value;
			}
		}

		protected override void OnTextChanged(EventArgs e)
		{
			base.OnTextChanged(e);

			if (TextChanged != null)
			{
				TextChanged(this, e);
			}
		}

		public event EventHandler TextChanged;
	}
}
