using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Unvell.ReoScript;

namespace Event
{
	public partial class MainForm : Form
	{
		ScriptRunningMachine srm = new ScriptRunningMachine();

		public MainForm()
		{
			InitializeComponent();

			srm.WorkMode |=
				  // Enable DirectAccess 
				  MachineWorkMode.AllowDirectAccess

				// Ignore exceptions in CLR calling (by default)
				| MachineWorkMode.IgnoreCLRExceptions

				// Enable CLR Event Binding
				| MachineWorkMode.AllowCLREventBind;

			txtAttachScript.Text = @"
link.click = function() {
  alert('Link Clicked!');
};
";

			txtDetachScript.Text = @"link.click = null;";

			// add C# object link into script context
			srm.SetGlobalVariable("link", link);
		}

		private void btnRun_Click(object sender, EventArgs e)
		{
			srm.Run(txtAttachScript.Text);
			
			// test whether event is attached
			srm.Run("if(link.click!=null) alert('event attached.');");
		}

		private void btnDetachRun_Click(object sender, EventArgs e)
		{
			srm.Run(txtDetachScript.Text);

			// test whether event is detached
			srm.Run("if(link.click==null) alert('event detached.');");
		}
	}
}
