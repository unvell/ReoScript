using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Unvell.ReoScript.Editor;
using System.IO;
using ScriptEditor.Properties;

namespace ScriptEditor
{
	public partial class DemoForm : Form
	{
		public DemoForm()
		{
			InitializeComponent();
		}

		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			ShowScript(Resources.car);
		}

		private void ShowScript(byte[] buf)
		{
			ShowScript(buf, false);
		}

		private void ShowScript(byte[] buf, bool directAccess)
		{
			using (ReoScriptEditor editor = new ReoScriptEditor())
			{
				using (StreamReader sr = new StreamReader(new MemoryStream(buf)))
				{
					if (directAccess)
					{
						editor.Srm.WorkMode |= Unvell.ReoScript.MachineWorkMode.AllowDirectAccess
							| Unvell.ReoScript.MachineWorkMode.AllowCLREventBind
							| Unvell.ReoScript.MachineWorkMode.AllowImportTypeInScript;
					}
					editor.Script = sr.ReadToEnd();
				}
				editor.ShowDialog();
			}
		}

		private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			using (ReoScriptEditor editor = new ReoScriptEditor())
			{
				editor.ShowDialog();
			}
		}

		private void DemoForm_Load(object sender, EventArgs e)
		{
			//linkLabel5_LinkClicked(null, null);
			//Close();
		}

		private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			ShowScript(Resources.helloworld);
		}

		private void linkLabel5_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			ShowScript(Resources.winform, true);
		}

		private void linkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			ShowScript(Resources.lambda, true);
		}
	}
}
