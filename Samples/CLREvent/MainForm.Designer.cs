namespace Event
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
			this.btnAttachRun = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label1 = new System.Windows.Forms.Label();
			this.txtAttachScript = new System.Windows.Forms.TextBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.link = new System.Windows.Forms.LinkLabel();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.label2 = new System.Windows.Forms.Label();
			this.txtDetachScript = new System.Windows.Forms.TextBox();
			this.btnDetachRun = new System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnAttachRun
			// 
			this.btnAttachRun.Location = new System.Drawing.Point(102, 207);
			this.btnAttachRun.Name = "btnAttachRun";
			this.btnAttachRun.Size = new System.Drawing.Size(93, 25);
			this.btnAttachRun.TabIndex = 1;
			this.btnAttachRun.Text = "Run";
			this.btnAttachRun.UseVisualStyleBackColor = true;
			this.btnAttachRun.Click += new System.EventHandler(this.btnRun_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.txtAttachScript);
			this.groupBox1.Controls.Add(this.btnAttachRun);
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(314, 246);
			this.groupBox1.TabIndex = 2;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "1. Run and attach event";
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(12, 28);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(290, 31);
			this.label1.TabIndex = 3;
			this.label1.Text = "link is predefined object associated with LinkLabel in C#.";
			// 
			// txtAttachScript
			// 
			this.txtAttachScript.Location = new System.Drawing.Point(15, 65);
			this.txtAttachScript.Multiline = true;
			this.txtAttachScript.Name = "txtAttachScript";
			this.txtAttachScript.Size = new System.Drawing.Size(278, 129);
			this.txtAttachScript.TabIndex = 2;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.link);
			this.groupBox2.Location = new System.Drawing.Point(340, 13);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(164, 245);
			this.groupBox2.TabIndex = 3;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "2. Fire event";
			// 
			// link
			// 
			this.link.AutoSize = true;
			this.link.Location = new System.Drawing.Point(47, 113);
			this.link.Name = "link";
			this.link.Size = new System.Drawing.Size(63, 14);
			this.link.TabIndex = 0;
			this.link.TabStop = true;
			this.link.Text = "Click Me";
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.label2);
			this.groupBox3.Controls.Add(this.txtDetachScript);
			this.groupBox3.Controls.Add(this.btnDetachRun);
			this.groupBox3.Location = new System.Drawing.Point(518, 13);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(293, 245);
			this.groupBox3.TabIndex = 4;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "3. Try remove event";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(13, 27);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(264, 31);
			this.label2.TabIndex = 3;
			this.label2.Text = "set link.click to null will detach event.";
			// 
			// txtDetachScript
			// 
			this.txtDetachScript.Location = new System.Drawing.Point(16, 64);
			this.txtDetachScript.Multiline = true;
			this.txtDetachScript.Name = "txtDetachScript";
			this.txtDetachScript.Size = new System.Drawing.Size(261, 129);
			this.txtDetachScript.TabIndex = 4;
			// 
			// btnDetachRun
			// 
			this.btnDetachRun.Location = new System.Drawing.Point(96, 206);
			this.btnDetachRun.Name = "btnDetachRun";
			this.btnDetachRun.Size = new System.Drawing.Size(93, 25);
			this.btnDetachRun.TabIndex = 3;
			this.btnDetachRun.Text = "Run";
			this.btnDetachRun.UseVisualStyleBackColor = true;
			this.btnDetachRun.Click += new System.EventHandler(this.btnDetachRun_Click);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(841, 274);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Event Binding Demo";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.LinkLabel link;
		private System.Windows.Forms.Button btnAttachRun;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox txtAttachScript;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox txtDetachScript;
		private System.Windows.Forms.Button btnDetachRun;

	}
}

