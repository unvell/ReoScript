namespace PropertyGetterAndSetter
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
			this.btnGet = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.trackValue = new System.Windows.Forms.TrackBar();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.labNativeValue = new System.Windows.Forms.Label();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.labResult = new System.Windows.Forms.Label();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.labelSetTo = new System.Windows.Forms.Label();
			this.buttonSetRandomly = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.trackValue)).BeginInit();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnGet
			// 
			this.btnGet.Location = new System.Drawing.Point(22, 36);
			this.btnGet.Name = "btnGet";
			this.btnGet.Size = new System.Drawing.Size(133, 28);
			this.btnGet.TabIndex = 0;
			this.btnGet.Text = "Get";
			this.btnGet.UseVisualStyleBackColor = true;
			this.btnGet.Click += new System.EventHandler(this.btnGet_Click);
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(22, 22);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(699, 34);
			this.label2.TabIndex = 1;
			this.label2.Text = "There is a property \'percent\' has been added into global object using ExternalPro" +
					"perty. ";
			// 
			// trackValue
			// 
			this.trackValue.Location = new System.Drawing.Point(16, 36);
			this.trackValue.Maximum = 100;
			this.trackValue.Name = "trackValue";
			this.trackValue.Size = new System.Drawing.Size(217, 45);
			this.trackValue.TabIndex = 3;
			this.trackValue.TickFrequency = 10;
			this.trackValue.Value = 50;
			this.trackValue.ValueChanged += new System.EventHandler(this.trackValue_Scroll);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.labNativeValue);
			this.groupBox1.Controls.Add(this.trackValue);
			this.groupBox1.Location = new System.Drawing.Point(40, 80);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(252, 117);
			this.groupBox1.TabIndex = 4;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "1. Set value in C#";
			// 
			// labNativeValue
			// 
			this.labNativeValue.AutoSize = true;
			this.labNativeValue.Location = new System.Drawing.Point(39, 84);
			this.labNativeValue.Name = "labNativeValue";
			this.labNativeValue.Size = new System.Drawing.Size(61, 12);
			this.labNativeValue.TabIndex = 4;
			this.labNativeValue.Text = "Current: 50";
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.labResult);
			this.groupBox2.Controls.Add(this.btnGet);
			this.groupBox2.Location = new System.Drawing.Point(309, 80);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(178, 117);
			this.groupBox2.TabIndex = 5;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "2. Get value in script";
			// 
			// labResult
			// 
			this.labResult.AutoSize = true;
			this.labResult.Location = new System.Drawing.Point(20, 84);
			this.labResult.Name = "labResult";
			this.labResult.Size = new System.Drawing.Size(51, 12);
			this.labResult.TabIndex = 4;
			this.labResult.Text = "Value is ";
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.labelSetTo);
			this.groupBox3.Controls.Add(this.buttonSetRandomly);
			this.groupBox3.Location = new System.Drawing.Point(504, 80);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(178, 117);
			this.groupBox3.TabIndex = 6;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "3. Set value in script";
			// 
			// labelSetTo
			// 
			this.labelSetTo.AutoSize = true;
			this.labelSetTo.Location = new System.Drawing.Point(20, 84);
			this.labelSetTo.Name = "labelSetTo";
			this.labelSetTo.Size = new System.Drawing.Size(36, 12);
			this.labelSetTo.TabIndex = 4;
			this.labelSetTo.Text = "Set to";
			// 
			// buttonSetRandomly
			// 
			this.buttonSetRandomly.Location = new System.Drawing.Point(22, 36);
			this.buttonSetRandomly.Name = "buttonSetRandomly";
			this.buttonSetRandomly.Size = new System.Drawing.Size(133, 28);
			this.buttonSetRandomly.TabIndex = 0;
			this.buttonSetRandomly.Text = "Set Randomly";
			this.buttonSetRandomly.UseVisualStyleBackColor = true;
			this.buttonSetRandomly.Click += new System.EventHandler(this.buttonSetRandomly_Click);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(729, 217);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.label2);
			this.Name = "MainForm";
			this.Text = "ExternalProperty Sample";
			((System.ComponentModel.ISupportInitialize)(this.trackValue)).EndInit();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button btnGet;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TrackBar trackValue;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label labNativeValue;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label labResult;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.Label labelSetTo;
		private System.Windows.Forms.Button buttonSetRandomly;
	}
}

