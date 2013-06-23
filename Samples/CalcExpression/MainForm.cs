using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Unvell.ReoScript;

namespace CalcExpression
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
		}

		ScriptRunningMachine srm1 = new ScriptRunningMachine();

		private void btnExpr1_Click(object sender, EventArgs e)
		{
			txtResult1.Text = (Convert.ToString(srm1.CalcExpression(txtExpr1.Text)));
		}

		ScriptRunningMachine srm2 = new ScriptRunningMachine();

		private void btnCalc2_Click(object sender, EventArgs e)
		{
			SetVarible(srm2, "a", txtVarA.Text);
			SetVarible(srm2, "b", txtVarB.Text);
			SetVarible(srm2, "c", txtVarC.Text);

			txtResult2.Text = (Convert.ToString(srm2.CalcExpression(txtExpr2.Text)));
		}

		private static void SetVarible(ScriptRunningMachine srm, string identifier, string str)
		{
			double variable = 0;
			if (double.TryParse(str, out variable))
				srm.SetGlobalVariable(identifier, variable);
			else
				srm.SetGlobalVariable(identifier, str);
		}
	}
}
