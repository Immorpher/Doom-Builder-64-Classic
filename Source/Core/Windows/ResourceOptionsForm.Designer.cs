namespace CodeImp.DoomBuilder.Windows
{
	partial class ResourceOptionsForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ResourceOptionsForm));
			this.label6 = new System.Windows.Forms.Label();
			this.strictpatches = new System.Windows.Forms.CheckBox();
			this.browsewad = new System.Windows.Forms.Button();
			this.wadlocation = new System.Windows.Forms.TextBox();
			this.cancel = new System.Windows.Forms.Button();
			this.apply = new System.Windows.Forms.Button();
			this.wadfiledialog = new System.Windows.Forms.OpenFileDialog();
			this.notfortesting = new System.Windows.Forms.CheckBox();
			label1 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.Location = new System.Drawing.Point(12, 12);
			label1.Name = "label1";
			label1.Size = new System.Drawing.Size(104, 14);
			label1.TabIndex = 0;
			label1.Text = "WAD File Resource:";
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(12, 92);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(362, 58);
			this.label6.TabIndex = 8;
			this.label6.Text = resources.GetString("label6.Text");
			// 
			// strictpatches
			// 
			this.strictpatches.AutoSize = true;
			this.strictpatches.Location = new System.Drawing.Point(12, 64);
			this.strictpatches.Name = "strictpatches";
			this.strictpatches.Size = new System.Drawing.Size(298, 18);
			this.strictpatches.TabIndex = 2;
			this.strictpatches.Text = "Strictly load patches between P_START and P_END only";
			this.strictpatches.UseVisualStyleBackColor = true;
			// 
			// browsewad
			// 
			this.browsewad.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.browsewad.Image = global::CodeImp.DoomBuilder.Properties.Resources.Folder;
			this.browsewad.Location = new System.Drawing.Point(346, 28);
			this.browsewad.Name = "browsewad";
			this.browsewad.Padding = new System.Windows.Forms.Padding(0, 0, 1, 3);
			this.browsewad.Size = new System.Drawing.Size(28, 23);
			this.browsewad.TabIndex = 1;
			this.browsewad.Text = " ";
			this.browsewad.UseVisualStyleBackColor = true;
			this.browsewad.Click += new System.EventHandler(this.browsewad_Click);
			// 
			// wadlocation
			// 
			this.wadlocation.Location = new System.Drawing.Point(12, 29);
			this.wadlocation.Name = "wadlocation";
			this.wadlocation.ReadOnly = true;
			this.wadlocation.Size = new System.Drawing.Size(328, 20);
			this.wadlocation.TabIndex = 0;
			// 
			// cancel
			// 
			this.cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancel.Location = new System.Drawing.Point(262, 186);
			this.cancel.Name = "cancel";
			this.cancel.Size = new System.Drawing.Size(112, 25);
			this.cancel.TabIndex = 5;
			this.cancel.Text = "Cancel";
			this.cancel.UseVisualStyleBackColor = true;
			this.cancel.Click += new System.EventHandler(this.cancel_Click);
			// 
			// apply
			// 
			this.apply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.apply.Location = new System.Drawing.Point(144, 186);
			this.apply.Name = "apply";
			this.apply.Size = new System.Drawing.Size(112, 25);
			this.apply.TabIndex = 4;
			this.apply.Text = "OK";
			this.apply.UseVisualStyleBackColor = true;
			this.apply.Click += new System.EventHandler(this.apply_Click);
			// 
			// wadfiledialog
			// 
			this.wadfiledialog.Filter = "Doom WAD Files (*.wad)|*.wad";
			this.wadfiledialog.Title = "Browse WAD File";
			// 
			// notfortesting
			// 
			this.notfortesting.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.notfortesting.AutoSize = true;
			this.notfortesting.Location = new System.Drawing.Point(12, 156);
			this.notfortesting.Name = "notfortesting";
			this.notfortesting.Size = new System.Drawing.Size(249, 18);
			this.notfortesting.TabIndex = 3;
			this.notfortesting.Text = "Exclude this resource from testing parameters";
			this.notfortesting.UseVisualStyleBackColor = true;
			// 
			// ResourceOptionsForm
			// 
			this.AcceptButton = this.apply;
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.cancel;
			this.ClientSize = new System.Drawing.Size(386, 224);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.strictpatches);
			this.Controls.Add(this.browsewad);
			this.Controls.Add(this.wadlocation);
			this.Controls.Add(label1);
			this.Controls.Add(this.notfortesting);
			this.Controls.Add(this.cancel);
			this.Controls.Add(this.apply);
			this.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ResourceOptionsForm";
			this.Opacity = 0;
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Resource Options";
			this.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.ResourceOptionsForm_HelpRequested);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button cancel;
		private System.Windows.Forms.Button apply;
		private System.Windows.Forms.TextBox wadlocation;
		private System.Windows.Forms.Button browsewad;
		private System.Windows.Forms.OpenFileDialog wadfiledialog;
		private System.Windows.Forms.CheckBox strictpatches;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.CheckBox notfortesting;
	}
}