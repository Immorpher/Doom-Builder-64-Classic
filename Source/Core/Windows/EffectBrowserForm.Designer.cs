namespace CodeImp.DoomBuilder.Windows
{
	partial class EffectBrowserForm
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.cancel = new System.Windows.Forms.Button();
			this.apply = new System.Windows.Forms.Button();
			this.tabs = new System.Windows.Forms.TabControl();
			this.tabeffects = new System.Windows.Forms.TabPage();
			this.effects = new System.Windows.Forms.ListView();
			this.colnumber = new System.Windows.Forms.ColumnHeader();
			this.colname = new System.Windows.Forms.ColumnHeader();
			this.tabs.SuspendLayout();
			this.tabeffects.SuspendLayout();
			this.SuspendLayout();
			// 
			// cancel
			// 
			this.cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancel.Location = new System.Drawing.Point(297, 459);
			this.cancel.Name = "cancel";
			this.cancel.Size = new System.Drawing.Size(112, 27);
			this.cancel.TabIndex = 2;
			this.cancel.Text = "Cancel";
			this.cancel.UseVisualStyleBackColor = true;
			this.cancel.Click += new System.EventHandler(this.cancel_Click);
			// 
			// apply
			// 
			this.apply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.apply.Location = new System.Drawing.Point(177, 459);
			this.apply.Name = "apply";
			this.apply.Size = new System.Drawing.Size(112, 27);
			this.apply.TabIndex = 1;
			this.apply.Text = "OK";
			this.apply.UseVisualStyleBackColor = true;
			this.apply.Click += new System.EventHandler(this.apply_Click);
			// 
			// tabs
			// 
			this.tabs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tabs.Controls.Add(this.tabeffects);
			this.tabs.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.tabs.ItemSize = new System.Drawing.Size(150, 19);
			this.tabs.Location = new System.Drawing.Point(10, 10);
			this.tabs.Margin = new System.Windows.Forms.Padding(1);
			this.tabs.Name = "tabs";
			this.tabs.SelectedIndex = 0;
			this.tabs.Size = new System.Drawing.Size(399, 436);
			this.tabs.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
			this.tabs.TabIndex = 0;
			// 
			// tabeffects
			// 
			this.tabeffects.Controls.Add(this.effects);
			this.tabeffects.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.tabeffects.Location = new System.Drawing.Point(4, 23);
			this.tabeffects.Name = "tabeffects";
			this.tabeffects.Padding = new System.Windows.Forms.Padding(3);
			this.tabeffects.Size = new System.Drawing.Size(391, 409);
			this.tabeffects.TabIndex = 0;
			this.tabeffects.Text = "Predefined Effects";
			this.tabeffects.UseVisualStyleBackColor = true;
			// 
			// effects
			// 
			this.effects.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colnumber,
            this.colname});
			this.effects.FullRowSelect = true;
			this.effects.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.effects.HideSelection = false;
			this.effects.Location = new System.Drawing.Point(6, 6);
			this.effects.MultiSelect = false;
			this.effects.Name = "effects";
			this.effects.Size = new System.Drawing.Size(379, 397);
			this.effects.TabIndex = 0;
			this.effects.UseCompatibleStateImageBehavior = false;
			this.effects.View = System.Windows.Forms.View.Details;
			this.effects.DoubleClick += new System.EventHandler(this.effects_DoubleClick);
			// 
			// colnumber
			// 
			this.colnumber.Text = "Effect";
			this.colnumber.Width = 70;
			// 
			// colname
			// 
			this.colname.Text = "Description";
			this.colname.Width = 280;
			// 
			// EffectBrowserForm
			// 
			this.AcceptButton = this.apply;
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.cancel;
			this.ClientSize = new System.Drawing.Size(419, 496);
			this.Controls.Add(this.tabs);
			this.Controls.Add(this.cancel);
			this.Controls.Add(this.apply);
			this.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "EffectBrowserForm";
			this.Opacity = 0;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Edit Effect";
			this.tabs.ResumeLayout(false);
			this.tabeffects.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button cancel;
		private System.Windows.Forms.Button apply;
		private System.Windows.Forms.TabControl tabs;
		private System.Windows.Forms.TabPage tabeffects;
		private System.Windows.Forms.ListView effects;
		private System.Windows.Forms.ColumnHeader colnumber;
		private System.Windows.Forms.ColumnHeader colname;
	}
}