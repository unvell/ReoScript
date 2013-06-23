using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Unvell.ReoScript;

namespace NativeFunctionExtension
{
	public partial class Form1 : Form
	{
		ScriptRunningMachine srm = new ScriptRunningMachine();

		public Form1()
		{
			InitializeComponent();

			// create a function and add into global object
			//
			//  ctx is context of current execution
			//  owner is object which the function belonging to
			//  args is arguments (can be null or an empty array)

			srm["myfunc"] = new NativeFunctionObject("myfunc", (ctx, owner, args) =>
			{
				MessageBox.Show("called from script!");

				// all function should returns a value (null for nothing)
				return null;
			});
		}

		private void button1_Click(object sender, EventArgs e)
		{
			// run script from textbox
			srm.Run(textBox1.Text);
		}

		private void button2_Click(object sender, EventArgs e)
		{
			// call function by specified name
			srm.InvokeFunctionIfExisted("myfunc");
		}
	}
}
