using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CLRTypeImporting
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			new ImportInCSharp().ShowDialog();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			new ImportInScript().ShowDialog();
		}
	}
}
