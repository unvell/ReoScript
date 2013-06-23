using Unvell.ReoScript.Editor;
namespace GameRS
{
	partial class MainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.battleground = new GameRS.Battleground();
			this.panel2 = new System.Windows.Forms.Panel();
			this.editor = new Unvell.ReoScript.Editor.ReoScriptEditorControl();
			this.splitter1 = new System.Windows.Forms.Splitter();
			this.panel3 = new System.Windows.Forms.Panel();
			this.labFps = new System.Windows.Forms.Label();
			this.btnStart = new System.Windows.Forms.Button();
			this.panel2.SuspendLayout();
			this.panel3.SuspendLayout();
			this.SuspendLayout();
			// 
			// battleground
			// 
			this.battleground.BackColor = System.Drawing.Color.White;
			this.battleground.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.battleground.CurrentFps = 8;
			this.battleground.Dock = System.Windows.Forms.DockStyle.Fill;
			this.battleground.Location = new System.Drawing.Point(0, 55);
			this.battleground.Name = "battleground";
			this.battleground.Size = new System.Drawing.Size(512, 688);
			this.battleground.Srm = null;
			this.battleground.TabIndex = 0;
			// 
			// panel2
			// 
			this.panel2.Controls.Add(this.editor);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Right;
			this.panel2.Location = new System.Drawing.Point(516, 55);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(621, 688);
			this.panel2.TabIndex = 1;
			// 
			// editor
			// 
			this.editor.AutoSize = true;
			this.editor.Dock = System.Windows.Forms.DockStyle.Fill;
			this.editor.Location = new System.Drawing.Point(0, 0);
			this.editor.Name = "editor";
			this.editor.Size = new System.Drawing.Size(621, 688);
			this.editor.TabIndex = 0;
			// 
			// splitter1
			// 
			this.splitter1.Dock = System.Windows.Forms.DockStyle.Right;
			this.splitter1.Location = new System.Drawing.Point(512, 55);
			this.splitter1.Name = "splitter1";
			this.splitter1.Size = new System.Drawing.Size(4, 688);
			this.splitter1.TabIndex = 2;
			this.splitter1.TabStop = false;
			// 
			// panel3
			// 
			this.panel3.Controls.Add(this.labFps);
			this.panel3.Controls.Add(this.btnStart);
			this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel3.Location = new System.Drawing.Point(0, 0);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(1137, 55);
			this.panel3.TabIndex = 3;
			// 
			// labFps
			// 
			this.labFps.AutoSize = true;
			this.labFps.Location = new System.Drawing.Point(141, 21);
			this.labFps.Name = "labFps";
			this.labFps.Size = new System.Drawing.Size(91, 13);
			this.labFps.TabIndex = 2;
			this.labFps.Text = "Current FPS: 0";
			// 
			// btnStart
			// 
			this.btnStart.Location = new System.Drawing.Point(12, 13);
			this.btnStart.Name = "btnStart";
			this.btnStart.Size = new System.Drawing.Size(104, 31);
			this.btnStart.TabIndex = 0;
			this.btnStart.Text = "Start";
			this.btnStart.UseVisualStyleBackColor = true;
			this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1137, 743);
			this.Controls.Add(this.battleground);
			this.Controls.Add(this.splitter1);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.panel3);
			this.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "MainForm";
			this.Text = "Board Sample";
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.panel3.ResumeLayout(false);
			this.panel3.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private Battleground battleground;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Splitter splitter1;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Button btnStart;
		private System.Windows.Forms.Label labFps;
		private ReoScriptEditorControl editor;
	}
}

