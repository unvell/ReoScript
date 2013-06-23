using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Unvell.ReoScript;

namespace PropertyGetterAndSetter
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
		}

		ScriptRunningMachine srm = new ScriptRunningMachine();

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			// Attach a property to global object using 'ExternalProperty'.
			//
			// ExternalProperty class provides a chance to do something, The delegate method 
			// defined in ExternalProperty will be fired when the property be getted or setted
			// in script running.
			//
			srm.SetGlobalVariable("percent", new ExternalProperty(

				// property getter
				//
				// this will be called when property value is required in script.
				() => { return trackValue.Value; },

				// property setter
				//
				// this will be called when a property value will be setted in script.
				(v) => { trackValue.Value = ScriptRunningMachine.GetIntValue(v); }

				));
		}

		private void btnGet_Click(object sender, EventArgs e)
		{
			labResult.Text = string.Format("Value is {0}", srm.Run("return percent;"));
		}

		private void trackValue_Scroll(object sender, EventArgs e)
		{
			labNativeValue.Text = "Current: " + Convert.ToString(trackValue.Value);
		}

		private void buttonSetRandomly_Click(object sender, EventArgs e)
		{
			object returnValue = srm.Run("percent = parseInt(Math.random() * 100);");
			labelSetTo.Text = "Set to " + Convert.ToString(returnValue);
		}
	}
}
