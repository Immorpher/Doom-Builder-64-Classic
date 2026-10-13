namespace CodeImp.DoomBuilder.AutomapMode
{
	partial class MenusForm
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
			if(disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.typeseparator = new System.Windows.Forms.ToolStripSeparator();
			this.typelabel = new System.Windows.Forms.ToolStripLabel();
			this.automaptype = new System.Windows.Forms.ToolStripComboBox();
			this.showhiddenlines = new System.Windows.Forms.ToolStripButton();
			this.showsecretsectors = new System.Windows.Forms.ToolStripButton();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// toolStrip1
			// 
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showhiddenlines,
            this.showsecretsectors,
            this.typeseparator,
            this.typelabel,
            this.automaptype});
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(731, 25);
			this.toolStrip1.TabIndex = 0;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// typeseparator
			// 
			this.typeseparator.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.typeseparator.Name = "typeseparator";
			this.typeseparator.Size = new System.Drawing.Size(6, 25);
			// 
			// typelabel
			// 
			this.typelabel.Name = "typelabel";
			this.typelabel.Size = new System.Drawing.Size(34, 22);
			this.typelabel.Text = "Type:";
			// 
			// automaptype
			// 
			this.automaptype.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.automaptype.Items.AddRange(new object[] {
            "Lines",
            "Floors"});
			this.automaptype.Name = "automaptype";
			this.automaptype.Size = new System.Drawing.Size(80, 25);
			this.automaptype.SelectedIndexChanged += new System.EventHandler(this.automaptype_SelectedIndexChanged);
			// 
			// showhiddenlines
			// 
			this.showhiddenlines.CheckOnClick = true;
			this.showhiddenlines.Image = global::CodeImp.DoomBuilder.AutomapMode.Properties.Resources.ShowHiddenLines;
			this.showhiddenlines.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.showhiddenlines.Margin = new System.Windows.Forms.Padding(0, 1, 2, 2);
			this.showhiddenlines.Name = "showhiddenlines";
			this.showhiddenlines.Size = new System.Drawing.Size(88, 22);
			this.showhiddenlines.Text = "Show hidden";
			this.showhiddenlines.CheckedChanged += new System.EventHandler(this.showhiddenlines_CheckedChanged);
			// 
			// showsecretsectors
			// 
			this.showsecretsectors.CheckOnClick = true;
			this.showsecretsectors.Image = global::CodeImp.DoomBuilder.AutomapMode.Properties.Resources.ShowSecrets;
			this.showsecretsectors.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.showsecretsectors.Margin = new System.Windows.Forms.Padding(0, 1, 2, 2);
			this.showsecretsectors.Name = "showsecretsectors";
			this.showsecretsectors.Size = new System.Drawing.Size(95, 22);
			this.showsecretsectors.Text = "Show secrets";
			this.showsecretsectors.CheckedChanged += new System.EventHandler(this.showsecretsectors_CheckedChanged);
			// 
			// MenusForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.toolStrip1);
			this.Name = "MenusForm";
			this.Size = new System.Drawing.Size(731, 65);
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton showhiddenlines;
		private System.Windows.Forms.ToolStripButton showsecretsectors;
		private System.Windows.Forms.ToolStripSeparator typeseparator;
		private System.Windows.Forms.ToolStripLabel typelabel;
		private System.Windows.Forms.ToolStripComboBox automaptype;
	}
}
