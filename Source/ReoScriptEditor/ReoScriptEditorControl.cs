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
	}
}
