///////////////////////////////////////////////////////////////////////////////
// 
// ReoScript
// 
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
// PURPOSE.
//
// License: GNU Lesser General Public License (LGPLv3)
//
// Email: dujid0@gmail.com
//
// Copyright (C) UNVELL.com, 2013. All Rights Reserved
//
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Unvell.ReoScript.Editor.Properties;

namespace Unvell.ReoScript.Editor
{
	public partial class ReoScriptEditor : Form, IStdOutputListener
	{
		public ReoScriptEditor()
		{
			InitializeComponent();

			newToolStripButton.Click += (s, e) => NewFile();
			newToolStripMenuItem.Click += (s, e) => NewFile();

			openToolStripButton.Click += (s, e) => LoadFile();
			openToolStripMenuItem.Click += (s, e) => LoadFile();

			saveToolStripButton.Click += (s, e) => SaveFile();
			saveToolStripMenuItem.Click += (s, e) => SaveFile();

			runToolStripButton.Click += (s, e) => RunScript();
			runToolStripMenuItem.Click += (s, e) => RunScript();

			cutToolStripButton.Click += (s, e) => editor.Fctb.Cut();
			cutToolStripMenuItem.Click += (s, e) => editor.Fctb.Cut();

			copyToolStripButton.Click += (s, e) => editor.Fctb.Copy();
			copyToolStripMenuItem.Click += (s, e) => editor.Fctb.Copy();

			pasteToolStripMenuItem.Click += (s, e) => editor.Fctb.Paste();
			pasteToolStripDropDownButton.Click += (s, e) => editor.Fctb.Paste();

			undoToolStripButton.Click += (s, e) => editor.Fctb.Undo();
			undoToolStripMenuItem.Click += (s, e) => editor.Fctb.Undo();

			redoToolStripButton.Click += (s, e) => editor.Fctb.Redo();
			redoToolStripMenuItem.Click += (s, e) => editor.Fctb.Redo();

			using (StreamReader sr = new StreamReader(new MemoryStream(Resources.default_rs)))
			{
				editor.Text = sr.ReadToEnd();
			}

			console.LineReceived += (s, e) =>
			{
				Log("\n");

				if (!string.IsNullOrEmpty(e.Text))
				{
					try
					{
						object val = srm.CalcExpression(e.Text);
						LogValue(val);
					}
					catch (Exception ex)
					{
						Log("error: " + ex.Message);
					}
				}
			};

			timer.Tick += (s, e) =>
			{
				bool running = srm == null ? false : srm.IsRunning;

				runToolStripButton.Enabled =
					runToolStripMenuItem.Enabled =
					!running;

				stopToolStripButton.Enabled =
					stopToolStripMenuItem.Enabled =
					!runToolStripMenuItem.Enabled;

				if (!running)
				{
					timer.Enabled = false;
				}
			};

			stopToolStripButton.Click += (s, e) => ForceStop();
			stopToolStripMenuItem.Click += (s, e) => ForceStop();


			//srm.EnableDirectAccess = true;
			//srm.IgnoreCLRExceptions = true;

			//srm.SetGlobalVariable("obj", new ClassA());
		}

	

		private void ForceStop()
		{
			if (srm != null) srm.ForceStop();
		}

		private void NewFile()
		{
			Script = string.Empty;
			srm.ResetContext();
		}

		public void LoadFile()
		{
			using (OpenFileDialog ofd = new OpenFileDialog())
			{
				ofd.Filter = "ReoScript(*.rs)|*.rs|All Files(*.*)|*.*";
				if (ofd.ShowDialog() == DialogResult.OK)
				{
					using (StreamReader sr = new StreamReader(new FileStream(ofd.FileName, FileMode.Open)))
					{
						editor.Text = sr.ReadToEnd();
					}
				}
			}
		}

		public void SaveFile()
		{
			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Filter = "ReoScript(*.rs)|*.rs|All Files(*.*)|*.*";
				if (sfd.ShowDialog() == DialogResult.OK)
				{
					using (StreamWriter sw = new StreamWriter(new FileStream(sfd.FileName, FileMode.Create)))
					{
						sw.Write(editor.Text);
					}
				}
			}
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private ScriptRunningMachine srm = new ScriptRunningMachine();

		public ScriptRunningMachine Srm
		{
			get { return srm; }
			set { srm = value;
			SetMachineSwitches();
			}
		}

		private Timer timer = new Timer()
		{
			Interval = 100,
		};

		public void RunScript()
		{
			if (!srm.HasStdOutputListener(this))
			{
				srm.AddStdOutputListener(this);
			}

			try
			{
				object v = srm.Run(editor.Text);
				timer.Enabled = true;
				//LogValue(v);
			}
			catch (Exception ex)
			{
				Log("error: " + ex.Message);
			}
		}

		void LogValue(object v)
		{
			if (v is AbstractFunctionValue)
			{
				Log(v.ToString());
			}
			else if (v is ObjectValue)
			{
				Log(((ObjectValue)v).DumpObject());
			}
			else if (v is bool)
			{
				Log(((bool)v) ? "true" : "false");
			}
			else
			{
				Log(v == null ? "undefined" : v.ToString());
			}
		}

		void Log(string log)
		{
			console.OutLine(log);
		}

		#region IStdOutputListener Members

		public void Write(string text)
		{
			Log(text);
		}

		public void WriteLine(string line)
		{
			Log(line);
		}

		public void Write(byte[] buf)
		{
			Log(Encoding.ASCII.GetString(buf));
		}

		#endregion

		public string Script
		{
			get
			{
				return editor.Text;
			}
			set
			{
				editor.Text = value;
			}
		}

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (AboutBox about = new AboutBox())
			{
				about.ShowDialog();
			}
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			GetMachineSwitches();

			enableDirectAccessToolStripMenuItem.Click += (ss, ee) => SetMachineSwitches();
			enableImportNamespacesAndClassesToolStripMenuItem.Click += (ss, ee) => SetMachineSwitches();
			enableAutoImportDependencyTypeToolStripMenuItem.Click += (ss, ee) => SetMachineSwitches();
			enableEventBindingToolStripMenuItem.Click += (ss, ee) => SetMachineSwitches();
		}

		private void GetMachineSwitches()
		{
			enableDirectAccessToolStripMenuItem.Checked =
				(srm.WorkMode & MachineWorkMode.AllowDirectAccess) == MachineWorkMode.AllowDirectAccess;
			enableImportNamespacesAndClassesToolStripMenuItem.Checked =
				(srm.WorkMode & MachineWorkMode.AllowImportTypeInScript) == MachineWorkMode.AllowImportTypeInScript;
			enableAutoImportDependencyTypeToolStripMenuItem.Checked =
				(srm.WorkMode & MachineWorkMode.AutoImportRelationType) == MachineWorkMode.AutoImportRelationType;
			enableEventBindingToolStripMenuItem.Checked =
				(srm.WorkMode & MachineWorkMode.AllowCLREventBind) == MachineWorkMode.AllowCLREventBind;
		}

		private void SetMachineSwitches()
		{
			MachineWorkMode mode = srm.WorkMode;

			if (enableDirectAccessToolStripMenuItem.Checked)
			{
				mode |= MachineWorkMode.AllowDirectAccess;
			}
			if (enableImportNamespacesAndClassesToolStripMenuItem.Checked)
			{
				mode |= MachineWorkMode.AllowImportTypeInScript;
			}
			if (enableAutoImportDependencyTypeToolStripMenuItem.Checked)
			{
				mode |= MachineWorkMode.AutoImportRelationType;
			}
			if (enableEventBindingToolStripMenuItem.Checked)
			{
				mode |= MachineWorkMode.AllowCLREventBind;
			}

			srm.WorkMode = mode;
		}
	}

}
