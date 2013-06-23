using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Unvell.ReoScript;

namespace CLRTypeImporting
{
	public partial class ImportInScript : Form
	{
		public ImportInScript()
		{
			InitializeComponent();
		}

		ScriptRunningMachine srm = new ScriptRunningMachine();

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			// Enable DirectAccess
			srm.WorkMode |= MachineWorkMode.AllowDirectAccess

				// NOTE: This flag is necessary to allow type importing in script
				| MachineWorkMode.AllowImportTypeInScript

				// allow that event binding in script
				| MachineWorkMode.AllowCLREventBind;

			
			// preset a script for demo
			editor.Text = @"
import System.Windows.Forms.*;
import System.Drawing.Point;

var f = new Form() {
  text: 'Form created in ReoScript', startPosition: 'CenterScreen', 
  size: { width: 300, height: 300 },
};

var link = new LinkLabel() {
  text: 'click me to close window', autoSize: true, location: { x: 75, y: 100 },
  click: function() { f.close(); },
};

f.controls.add(link);
f.showDialog();
";

			btnRun.Click += (ss, ee) => srm.Run(editor.Text);
		}
	}
}
