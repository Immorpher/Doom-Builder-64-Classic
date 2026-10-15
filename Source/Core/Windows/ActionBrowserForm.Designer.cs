namespace CodeImp.DoomBuilder.Windows
{
	partial class ActionBrowserForm
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
            System.Windows.Forms.Label label1;
            System.Windows.Forms.Label label2;
            System.Windows.Forms.Label label3;
            System.Windows.Forms.Label label4;
            System.Windows.Forms.Label label5;
            System.Windows.Forms.Label label6;
            this.cancel = new System.Windows.Forms.Button();
            this.apply = new System.Windows.Forms.Button();
            this.actions = new System.Windows.Forms.TreeView();
            this.tabs = new System.Windows.Forms.TabControl();
            this.tabactions = new System.Windows.Forms.TabPage();
            this.prefixespanel = new System.Windows.Forms.Panel();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            this.tabs.SuspendLayout();
            this.tabactions.SuspendLayout();
            this.prefixespanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(27, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(60, 14);
            label1.TabIndex = 21;
            label1.Text = "S = Switch";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(140, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(77, 14);
            label2.TabIndex = 22;
            label2.Text = "W = Walk over";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(269, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(63, 14);
            label3.TabIndex = 23;
            label3.Text = "G = Gunfire";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(27, 16);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(49, 14);
            label4.TabIndex = 24;
            label4.Text = "D = Door";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(140, 16);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(80, 14);
            label5.TabIndex = 25;
            label5.Text = "R = Repeatable";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(269, 16);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(74, 14);
            label6.TabIndex = 26;
            label6.Text = "1 = Once only";
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
            // actions
            // 
            this.actions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.actions.HideSelection = false;
            this.actions.Location = new System.Drawing.Point(6, 52);
            this.actions.Name = "actions";
            this.actions.Size = new System.Drawing.Size(379, 351);
            this.actions.TabIndex = 0;
            this.actions.DoubleClick += new System.EventHandler(this.actions_DoubleClick);
            // 
            // tabs
            // 
            this.tabs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tabs.Controls.Add(this.tabactions);
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
            // tabactions
            // 
            this.tabactions.Controls.Add(this.actions);
            this.tabactions.Controls.Add(this.prefixespanel);
            this.tabactions.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabactions.Location = new System.Drawing.Point(4, 23);
            this.tabactions.Name = "tabactions";
            this.tabactions.Padding = new System.Windows.Forms.Padding(3);
            this.tabactions.Size = new System.Drawing.Size(391, 409);
            this.tabactions.TabIndex = 0;
            this.tabactions.Text = "Predefined Actions";
            this.tabactions.UseVisualStyleBackColor = true;
            // 
            // prefixespanel
            // 
            this.prefixespanel.Controls.Add(label6);
            this.prefixespanel.Controls.Add(label5);
            this.prefixespanel.Controls.Add(label4);
            this.prefixespanel.Controls.Add(label3);
            this.prefixespanel.Controls.Add(label2);
            this.prefixespanel.Controls.Add(label1);
            this.prefixespanel.Location = new System.Drawing.Point(7, 12);
            this.prefixespanel.Name = "prefixespanel";
            this.prefixespanel.Size = new System.Drawing.Size(378, 34);
            this.prefixespanel.TabIndex = 27;
            // 
            // ActionBrowserForm
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
            this.Name = "ActionBrowserForm";
            this.Opacity = 0;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Edit Action";
            this.Load += new System.EventHandler(this.ActionBrowserForm_Load);
            this.tabs.ResumeLayout(false);
            this.tabactions.ResumeLayout(false);
            this.prefixespanel.ResumeLayout(false);
            this.prefixespanel.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button cancel;
		private System.Windows.Forms.Button apply;
		private System.Windows.Forms.TreeView actions;
		private System.Windows.Forms.TabControl tabs;
		private System.Windows.Forms.TabPage tabactions;
        private System.Windows.Forms.Panel prefixespanel;
	}
}
