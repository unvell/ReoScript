using Unvell.ReoScript.Editor;
namespace CLRTypeImporting
{
	partial class ImportInScript
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
			this.label1 = new System.Windows.Forms.Label();
			this.btnRun = new System.Windows.Forms.Button();
			this.editor = new Unvell.ReoScript.Editor.ReoScriptEditorControl();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(22, 26);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(362, 13);
			this.label1.TabIndex = 7;
			this.label1.Text = "Execute the following script will create a windows form and put a LinkLabel.";
			// 
			// btnRun
			// 
			this.btnRun.Location = new System.Drawing.Point(679, 57);
			this.btnRun.Name = "btnRun";
			this.btnRun.Size = new System.Drawing.Size(104, 32);
			this.btnRun.TabIndex = 6;
			this.btnRun.Text = "Run";
			this.btnRun.UseVisualStyleBackColor = true;
			// 
			// editor
			// 
			this.editor.AutoSize = true;
			this.editor.Location = new System.Drawing.Point(25, 57);
			this.editor.Name = "editor";
			this.editor.Size = new System.Drawing.Size(633, 338);
			this.editor.TabIndex = 8;
			// 
			// ImportInScript
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(814, 421);
			this.Controls.Add(this.editor);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.btnRun);
			this.Name = "ImportInScript";
			this.Text = "Import Type In Script";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button btnRun;
		private ReoScriptEditorControl editor;
	}
}