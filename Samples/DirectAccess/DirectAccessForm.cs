using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Unvell.ReoScript;

namespace DirectAccess
{
	public partial class DirectAccessForm : Form
	{
		ScriptRunningMachine srm = new ScriptRunningMachine();

		public DirectAccessForm()
		{
			InitializeComponent();

			srm.WorkMode |= MachineWorkMode.AllowDirectAccess;

			srm.SetGlobalVariable("user", new User());
		}

		private void button1_Click(object sender, EventArgs e)
		{
			srm.Run(textBox1.Text);
		}

	}

public class User 
{
	private string nickname = "no name";

	public string Nickname
	{
		get { return nickname; }
		set { nickname = value; }
	}

	private int age = 30;

	public int Age
	{
		get { return age; }
		set { age = value; }
	}

	public void Hello()
	{
		MessageBox.Show(string.Format("Hello {0}!", nickname));
	}
}
}
