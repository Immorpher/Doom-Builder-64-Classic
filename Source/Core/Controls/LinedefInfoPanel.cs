
#region ================== Copyright (c) 2007 Pascal vd Heiden

/*
 * Copyright (c) 2007 Pascal vd Heiden, www.codeimp.com
 * This program is released under GNU General Public License
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 */

#endregion

#region ================== Namespaces

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Types;
using CodeImp.DoomBuilder.IO;

#endregion

namespace CodeImp.DoomBuilder.Controls
{
	internal partial class LinedefInfoPanel : UserControl
	{
		
		// Constructor
		public LinedefInfoPanel()
		{
			// Initialize
			InitializeComponent();

			// Doom 64 has no action arguments, so this panel is narrower
			infopanel.Width = infopanel.Width - 190;
		}
		
		// This shows the info
		public void ShowInfo(Linedef l)
		{
			bool upperunpegged, lowerunpegged;
			string peggedness;
			
			// Move panels
			frontpanel.Left = infopanel.Left + infopanel.Width + infopanel.Margin.Right + frontpanel.Margin.Left;
			backpanel.Left = frontpanel.Left + frontpanel.Width + frontpanel.Margin.Right + backpanel.Margin.Left;
			
			// Get line action information
			LinedefActionInfo act = General.Map.Config.GetLinedefActionInfo(l.Action);
			
			// Determine peggedness
			upperunpegged = l.IsFlagSet(General.Map.Config.UpperUnpeggedFlag);
			lowerunpegged = l.IsFlagSet(General.Map.Config.LowerUnpeggedFlag);
			if(upperunpegged && lowerunpegged)
				peggedness = "Upper & Lower";
			else if(upperunpegged)
				peggedness = "Upper";
			else if(lowerunpegged)
				peggedness = "Lower";
			else
				peggedness = "None";
			
			// Linedef info
			infopanel.Text = " Linedef " + l.Index + " ";
			action.Text = act.ToString();
			length.Text = l.Length.ToString("0.##");
			angle.Text = l.AngleDeg.ToString() + "\u00B0";
			tag.Text = l.Tag.ToString();
			unpegged.Text = peggedness;
			
			// Front side available?
			if(l.Front != null)
			{
				// Show sidedef info
				frontpanel.Text = " Front Sidedef " + l.Front.Index + " ";
				frontsector.Text = " Sector " + l.Front.Sector.Index;
				frontsector.Visible = true;
				frontoffset.Text = l.Front.OffsetX + ", " + l.Front.OffsetY;
				fronthighname.Text = l.Front.HighTexture;
				frontmidname.Text = l.Front.MiddleTexture;
				frontlowname.Text = l.Front.LowTexture;
				DisplaySidedefTexture(fronthightex, l.Front.HighTexture, l.Front.HighRequired());
				DisplaySidedefTexture(frontmidtex, l.Front.MiddleTexture, l.Front.MiddleRequired());
				DisplaySidedefTexture(frontlowtex, l.Front.LowTexture, l.Front.LowRequired());
				frontoffsetlabel.Enabled = true;
				frontoffset.Enabled = true;
				frontpanel.Enabled = true;
			}
			else
			{
				// Show no info
				frontpanel.Text = " Front Sidedef ";
				frontsector.Text = "";
				frontsector.Visible = false;
				frontoffsetlabel.Enabled = false;
				frontoffset.Enabled = false;
				frontpanel.Enabled = false;
				frontoffset.Text = "--, --";
				fronthighname.Text = "";
				frontmidname.Text = "";
				frontlowname.Text = "";
				fronthightex.BackgroundImage = null;
				frontmidtex.BackgroundImage = null;
				frontlowtex.BackgroundImage = null;
			}

			// Back size available?
			if(l.Back != null)
			{
				// Show sidedef info
				backpanel.Text = " Back Sidedef " + l.Back.Index + " ";
				backsector.Text = " Sector " + l.Back.Sector.Index;
				backsector.Visible = true;
				backoffset.Text = l.Back.OffsetX + ", " + l.Back.OffsetY;
				backhighname.Text = l.Back.HighTexture;
				backmidname.Text = l.Back.MiddleTexture;
				backlowname.Text = l.Back.LowTexture;
				DisplaySidedefTexture(backhightex, l.Back.HighTexture, l.Back.HighRequired());
				DisplaySidedefTexture(backmidtex, l.Back.MiddleTexture, l.Back.MiddleRequired());
				DisplaySidedefTexture(backlowtex, l.Back.LowTexture, l.Back.LowRequired());
				backoffsetlabel.Enabled = true;
				backoffset.Enabled = true;
				backpanel.Enabled = true;
			}
			else
			{
				// Show no info
				backpanel.Text = " Back Sidedef ";
				backsector.Text = "";
				backsector.Visible = false;
				backoffsetlabel.Enabled = false;
				backoffset.Enabled = false;
				backpanel.Enabled = false;
				backoffset.Text = "--, --";
				backhighname.Text = "";
				backmidname.Text = "";
				backlowname.Text = "";
				backhightex.BackgroundImage = null;
				backmidtex.BackgroundImage = null;
				backlowtex.BackgroundImage = null;
			}
			
			// Position labels
			frontsector.Left = frontlowtex.Right - frontsector.Width;
			backsector.Left = backlowtex.Right - backsector.Width;

			// Show the whole thing
			this.Show();
			this.Update();
		}

		// When visible changed
		protected override void OnVisibleChanged(EventArgs e)
		{
			// Hiding panels
			if(!this.Visible)
			{
				fronthightex.BackgroundImage = null;
				frontmidtex.BackgroundImage = null;
				frontlowtex.BackgroundImage = null;
				backhightex.BackgroundImage = null;
				backmidtex.BackgroundImage = null;
				backlowtex.BackgroundImage = null;
			}

			// Call base
			base.OnVisibleChanged(e);
		}

		// This shows a sidedef texture in a panel
		private void DisplaySidedefTexture(Panel panel, string name, bool required)
		{
			// Check if name is a "none" texture
			if((name.Length < 1) || (name[0] == '-'))
			{
				// Determine image to show
				if(required)
					panel.BackgroundImage = CodeImp.DoomBuilder.Properties.Resources.MissingTexture;
				else
					panel.BackgroundImage = null;
			}
			else
			{
				// Set the image
				panel.BackgroundImage = General.Map.Data.GetTextureImage(name).GetPreview();
			}
			
			// Image not null?
			if(panel.BackgroundImage != null)
			{
				// Small enough to fit in panel?
				if((panel.BackgroundImage.Size.Width < panel.ClientRectangle.Width) &&
				   (panel.BackgroundImage.Size.Height < panel.ClientRectangle.Height))
				{
					// Display centered
					panel.BackgroundImageLayout = ImageLayout.Center;
				}
				else
				{
					// Display zoomed
					panel.BackgroundImageLayout = ImageLayout.Zoom;
				}
			}
		}
	}
}
