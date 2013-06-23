namespace CLRTypeImporting
{
	partial class ImportInCSharp
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
			this.sandboxGroup = new System.Windows.Forms.GroupBox();
			this.txtScript = new System.Windows.Forms.TextBox();
			this.btnRun = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// sandboxGroup
			// 
			this.sandboxGroup.Location = new System.Drawing.Point(31, 37);
			this.sandboxGroup.Name = "sandboxGroup";
			this.sandboxGroup.Size = new System.Drawing.Size(290, 229);
			this.sandboxGroup.TabIndex = 0;
			this.sandboxGroup.TabStop = false;
			this.sandboxGroup.Text = "Sandbox";
			// 
			// txtScript
			// 
			this.txtScript.Location = new System.Drawing.Point(363, 75);
			this.txtScript.Multiline = true;
			this.txtScript.Name = "txtScript";
			this.txtScript.Size = new System.Drawing.Size(305, 128);
			this.txtScript.TabIndex = 1;
			// 
			// btnRun
			// 
			this.btnRun.Location = new System.Drawing.Point(469, 234);
			this.btnRun.Name = "btnRun";
			this.btnRun.Size = new System.Drawing.Size(104, 32);
			this.btnRun.TabIndex = 2;
			this.btnRun.Text = "Run";
			this.btnRun.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(362, 42);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(289, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "Execute the following script will add a LinkLabel in sandbox.";
			// 
			// ImportInCSharp
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(706, 299);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.btnRun);
			this.Controls.Add(this.txtScript);
			this.Controls.Add(this.sandboxGroup);
			this.Name = "ImportInCSharp";
			this.Text = "Importing Type by Programming";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox sandboxGroup;
		private System.Windows.Forms.TextBox txtScript;
		private System.Windows.Forms.Button btnRun;
		private System.Windows.Forms.Label label1;
	}
}

