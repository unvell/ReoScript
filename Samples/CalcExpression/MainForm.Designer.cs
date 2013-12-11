namespace CalcExpression
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
			this.label1 = new System.Windows.Forms.Label();
			this.txtExpr1 = new System.Windows.Forms.TextBox();
			this.btnExpr1 = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label7 = new System.Windows.Forms.Label();
			this.txtResult1 = new System.Windows.Forms.TextBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.label8 = new System.Windows.Forms.Label();
			this.txtVarC = new System.Windows.Forms.TextBox();
			this.label6 = new System.Windows.Forms.Label();
			this.txtResult2 = new System.Windows.Forms.TextBox();
			this.txtVarB = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.txtVarA = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.btnCalc2 = new System.Windows.Forms.Button();
			this.txtExpr2 = new System.Windows.Forms.TextBox();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(20, 38);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(266, 14);
			this.label1.TabIndex = 0;
			this.label1.Text = "Calculate expression: (ex. 1 + 2 * 3)";
			// 
			// txtExpr1
			// 
			this.txtExpr1.Location = new System.Drawing.Point(23, 68);
			this.txtExpr1.Name = "txtExpr1";
			this.txtExpr1.Size = new System.Drawing.Size(325, 22);
			this.txtExpr1.TabIndex = 1;
			this.txtExpr1.Text = "1 + 2 * 3";
			// 
			// btnExpr1
			// 
			this.btnExpr1.Location = new System.Drawing.Point(98, 112);
			this.btnExpr1.Name = "btnExpr1";
			this.btnExpr1.Size = new System.Drawing.Size(175, 30);
			this.btnExpr1.TabIndex = 2;
			this.btnExpr1.Text = "Calc Expression";
			this.btnExpr1.UseVisualStyleBackColor = true;
			this.btnExpr1.Click += new System.EventHandler(this.btnExpr1_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.label7);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.btnExpr1);
			this.groupBox1.Controls.Add(this.txtResult1);
			this.groupBox1.Controls.Add(this.txtExpr1);
			this.groupBox1.Location = new System.Drawing.Point(24, 20);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(377, 229);
			this.groupBox1.TabIndex = 4;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Expression Calc";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(20, 160);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(56, 14);
			this.label7.TabIndex = 0;
			this.label7.Text = "Result:";
			// 
			// txtResult1
			// 
			this.txtResult1.Location = new System.Drawing.Point(23, 183);
			this.txtResult1.Name = "txtResult1";
			this.txtResult1.Size = new System.Drawing.Size(325, 22);
			this.txtResult1.TabIndex = 1;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.label8);
			this.groupBox2.Controls.Add(this.txtVarC);
			this.groupBox2.Controls.Add(this.label6);
			this.groupBox2.Controls.Add(this.txtResult2);
			this.groupBox2.Controls.Add(this.txtVarB);
			this.groupBox2.Controls.Add(this.label5);
			this.groupBox2.Controls.Add(this.txtVarA);
			this.groupBox2.Controls.Add(this.label4);
			this.groupBox2.Controls.Add(this.label3);
			this.groupBox2.Controls.Add(this.label2);
			this.groupBox2.Controls.Add(this.btnCalc2);
			this.groupBox2.Controls.Add(this.txtExpr2);
			this.groupBox2.Location = new System.Drawing.Point(450, 20);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(377, 321);
			this.groupBox2.TabIndex = 5;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Using variables in expression";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(16, 257);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(56, 14);
			this.label8.TabIndex = 0;
			this.label8.Text = "Result:";
			// 
			// txtVarC
			// 
			this.txtVarC.Location = new System.Drawing.Point(191, 94);
			this.txtVarC.Name = "txtVarC";
			this.txtVarC.Size = new System.Drawing.Size(116, 22);
			this.txtVarC.TabIndex = 6;
			this.txtVarC.Text = "3";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(157, 98);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(14, 14);
			this.label6.TabIndex = 5;
			this.label6.Text = "c";
			// 
			// txtResult2
			// 
			this.txtResult2.Location = new System.Drawing.Point(20, 280);
			this.txtResult2.Name = "txtResult2";
			this.txtResult2.Size = new System.Drawing.Size(325, 22);
			this.txtResult2.TabIndex = 1;
			// 
			// txtVarB
			// 
			this.txtVarB.Location = new System.Drawing.Point(191, 66);
			this.txtVarB.Name = "txtVarB";
			this.txtVarB.Size = new System.Drawing.Size(116, 22);
			this.txtVarB.TabIndex = 6;
			this.txtVarB.Text = "5";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(157, 70);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(14, 14);
			this.label5.TabIndex = 5;
			this.label5.Text = "b";
			// 
			// txtVarA
			// 
			this.txtVarA.Location = new System.Drawing.Point(190, 38);
			this.txtVarA.Name = "txtVarA";
			this.txtVarA.Size = new System.Drawing.Size(116, 22);
			this.txtVarA.TabIndex = 4;
			this.txtVarA.Text = "2";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(156, 42);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(14, 14);
			this.label4.TabIndex = 3;
			this.label4.Text = "a";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(20, 38);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(105, 14);
			this.label3.TabIndex = 0;
			this.label3.Text = "Set Variables:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(16, 143);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(301, 14);
			this.label2.TabIndex = 0;
			this.label2.Text = "Expression that contains variables above: ";
			// 
			// btnCalc2
			// 
			this.btnCalc2.Location = new System.Drawing.Point(99, 216);
			this.btnCalc2.Name = "btnCalc2";
			this.btnCalc2.Size = new System.Drawing.Size(175, 30);
			this.btnCalc2.TabIndex = 2;
			this.btnCalc2.Text = "Calc Expression";
			this.btnCalc2.UseVisualStyleBackColor = true;
			this.btnCalc2.Click += new System.EventHandler(this.btnCalc2_Click);
			// 
			// txtExpr2
			// 
			this.txtExpr2.Location = new System.Drawing.Point(19, 165);
			this.txtExpr2.Name = "txtExpr2";
			this.txtExpr2.Size = new System.Drawing.Size(325, 22);
			this.txtExpr2.TabIndex = 1;
			this.txtExpr2.Text = "a + (b - c) * 2";
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(863, 370);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "MainForm";
			this.Padding = new System.Windows.Forms.Padding(5);
			this.Text = "Calculate Expression Sample";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox txtExpr1;
		private System.Windows.Forms.Button btnExpr1;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.TextBox txtVarC;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.TextBox txtVarB;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox txtVarA;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button btnCalc2;
		private System.Windows.Forms.TextBox txtExpr2;
		private System.Windows.Forms.TextBox txtResult1;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.TextBox txtResult2;
	}
}

