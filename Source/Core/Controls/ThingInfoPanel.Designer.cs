namespace CodeImp.DoomBuilder.Controls
{
	partial class ThingInfoPanel
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
			System.Windows.Forms.Label label5;
			System.Windows.Forms.Label label4;
			System.Windows.Forms.Label label3;
			System.Windows.Forms.Label label2;
			System.Windows.Forms.Label label1;
			this.infopanel = new System.Windows.Forms.GroupBox();
			this.angle = new System.Windows.Forms.Label();
			this.tag = new System.Windows.Forms.Label();
			this.position = new System.Windows.Forms.Label();
			this.action = new System.Windows.Forms.Label();
			this.type = new System.Windows.Forms.Label();
			this.spritepanel = new System.Windows.Forms.GroupBox();
			this.spritename = new System.Windows.Forms.Label();
			this.spritetex = new System.Windows.Forms.Panel();
			label5 = new System.Windows.Forms.Label();
			label4 = new System.Windows.Forms.Label();
			label3 = new System.Windows.Forms.Label();
			label2 = new System.Windows.Forms.Label();
			label1 = new System.Windows.Forms.Label();
			this.infopanel.SuspendLayout();
			this.spritepanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// label5
			// 
			label5.AutoSize = true;
			label5.Location = new System.Drawing.Point(165, 77);
			label5.Name = "label5";
			label5.Size = new System.Drawing.Size(38, 14);
			label5.TabIndex = 8;
			label5.Text = "Angle:";
			// 
			// label4
			// 
			label4.AutoSize = true;
			label4.Location = new System.Drawing.Point(30, 77);
			label4.Name = "label4";
			label4.Size = new System.Drawing.Size(28, 14);
			label4.TabIndex = 4;
			label4.Text = "Tag:";
			// 
			// label3
			// 
			label3.AutoSize = true;
			label3.Location = new System.Drawing.Point(11, 58);
			label3.Name = "label3";
			label3.Size = new System.Drawing.Size(47, 14);
			label3.TabIndex = 3;
			label3.Text = "Position:";
			// 
			// label2
			// 
			label2.AutoSize = true;
			label2.Location = new System.Drawing.Point(17, 39);
			label2.Name = "label2";
			label2.Size = new System.Drawing.Size(41, 14);
			label2.TabIndex = 2;
			label2.Text = "Action:";
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.Location = new System.Drawing.Point(24, 19);
			label1.Name = "label1";
			label1.Size = new System.Drawing.Size(34, 14);
			label1.TabIndex = 0;
			label1.Text = "Type:";
			// 
			// infopanel
			// 
			this.infopanel.Controls.Add(this.angle);
			this.infopanel.Controls.Add(label5);
			this.infopanel.Controls.Add(this.tag);
			this.infopanel.Controls.Add(this.position);
			this.infopanel.Controls.Add(this.action);
			this.infopanel.Controls.Add(label4);
			this.infopanel.Controls.Add(label3);
			this.infopanel.Controls.Add(label2);
			this.infopanel.Controls.Add(this.type);
			this.infopanel.Controls.Add(label1);
			this.infopanel.Location = new System.Drawing.Point(0, 0);
			this.infopanel.Name = "infopanel";
			this.infopanel.Size = new System.Drawing.Size(473, 100);
			this.infopanel.TabIndex = 4;
			this.infopanel.TabStop = false;
			this.infopanel.Text = " Thing ";
			// 
			// angle
			// 
			this.angle.AutoSize = true;
			this.angle.Location = new System.Drawing.Point(206, 77);
			this.angle.Name = "angle";
			this.angle.Size = new System.Drawing.Size(13, 14);
			this.angle.TabIndex = 11;
			this.angle.Text = "0";
			// 
			// tag
			// 
			this.tag.AutoSize = true;
			this.tag.Location = new System.Drawing.Point(61, 77);
			this.tag.Name = "tag";
			this.tag.Size = new System.Drawing.Size(13, 14);
			this.tag.TabIndex = 7;
			this.tag.Text = "0";
			// 
			// position
			// 
			this.position.AutoSize = true;
			this.position.Location = new System.Drawing.Point(61, 58);
			this.position.Name = "position";
			this.position.Size = new System.Drawing.Size(91, 14);
			this.position.TabIndex = 6;
			this.position.Text = "1024, 1024, 1024";
			// 
			// action
			// 
			this.action.AutoEllipsis = true;
			this.action.Location = new System.Drawing.Point(61, 39);
			this.action.Name = "action";
			this.action.Size = new System.Drawing.Size(210, 14);
			this.action.TabIndex = 5;
			this.action.Text = "0 - Spawn a Blue Poopie and Ammo";
			// 
			// type
			// 
			this.type.AutoSize = true;
			this.type.Location = new System.Drawing.Point(61, 19);
			this.type.Name = "type";
			this.type.Size = new System.Drawing.Size(99, 14);
			this.type.TabIndex = 1;
			this.type.Text = "0 - Big Brown Pimp";
			// 
			// spritepanel
			// 
			this.spritepanel.Controls.Add(this.spritename);
			this.spritepanel.Controls.Add(this.spritetex);
			this.spritepanel.Location = new System.Drawing.Point(479, 0);
			this.spritepanel.Name = "spritepanel";
			this.spritepanel.Size = new System.Drawing.Size(107, 100);
			this.spritepanel.TabIndex = 5;
			this.spritepanel.TabStop = false;
			this.spritepanel.Text = " Sprite ";
			// 
			// spritename
			// 
			this.spritename.Location = new System.Drawing.Point(11, 80);
			this.spritename.Name = "spritename";
			this.spritename.Size = new System.Drawing.Size(84, 13);
			this.spritename.TabIndex = 1;
			this.spritename.Text = "BROWNHUG";
			this.spritename.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// spritetex
			// 
			this.spritetex.BackColor = System.Drawing.SystemColors.AppWorkspace;
			this.spritetex.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.spritetex.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.spritetex.Location = new System.Drawing.Point(19, 19);
			this.spritetex.Name = "spritetex";
			this.spritetex.Size = new System.Drawing.Size(68, 60);
			this.spritetex.TabIndex = 0;
			// 
			// ThingInfoPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.spritepanel);
			this.Controls.Add(this.infopanel);
			this.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.MaximumSize = new System.Drawing.Size(10000, 100);
			this.MinimumSize = new System.Drawing.Size(100, 100);
			this.Name = "ThingInfoPanel";
			this.Size = new System.Drawing.Size(650, 100);
			this.infopanel.ResumeLayout(false);
			this.infopanel.PerformLayout();
			this.spritepanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox spritepanel;
		private System.Windows.Forms.Label spritename;
		private System.Windows.Forms.Panel spritetex;
		private System.Windows.Forms.Label angle;
		private System.Windows.Forms.Label tag;
		private System.Windows.Forms.Label position;
		private System.Windows.Forms.Label action;
		private System.Windows.Forms.Label type;
		private System.Windows.Forms.GroupBox infopanel;

	}
}
