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
	public partial class ImportInCSharp : Form
	{
		public ImportInCSharp()
		{
			InitializeComponent();
		}

		ScriptRunningMachine srm = new ScriptRunningMachine();

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			// enable DirectAccess
			srm.WorkMode |= MachineWorkMode.AllowDirectAccess;

			// import type LinkLabel
			srm.ImportType(typeof(LinkLabel));

			// add sandbox instance as global variable
			srm.SetGlobalVariable("sandbox", sandboxGroup);


			txtScript.Text = @"
sandbox.controls.add(
  new LinkLabel(){ text: 'Link added!', dock: 'Fill' }
);
";
			btnRun.Click += (ss, ee) => { srm.Run(txtScript.Text); };
		}
	}
}
