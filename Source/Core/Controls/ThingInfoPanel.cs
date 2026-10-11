
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
using CodeImp.DoomBuilder.Geometry;
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
	internal partial class ThingInfoPanel : UserControl
	{
		// Constructor
		public ThingInfoPanel()
		{
			// Initialize
			InitializeComponent();

			// Doom 64 has no action arguments, so this panel is narrower
			infopanel.Width = infopanel.Width - 190;
		}

		// This shows the info
		public void ShowInfo(Thing t)
		{
			ThingTypeInfo ti;
			LinedefActionInfo act = null;
			string actioninfo = "";
			string zinfo;
			float zvalue;

			// Move panel
			spritepanel.Left = infopanel.Left + infopanel.Width + infopanel.Margin.Right + spritepanel.Margin.Left;
			
			// Lookup thing info
			ti = General.Map.Data.GetThingInfo(t.Type);

			// Get thing action information
			if(General.Map.Config.LinedefActions.ContainsKey(t.Action))
			{
				act = General.Map.Config.LinedefActions[t.Action];
				actioninfo = act.ToString();
			}
			else if(t.Action == 0)
				actioninfo = t.Action.ToString() + " - None";
			else
				actioninfo = t.Action.ToString() + " - Unknown";
			
			// Determine z info to show
			t.DetermineSector();
			if(ti.AbsoluteZ)
			{
				zvalue = t.Position.z;
				zinfo = zvalue.ToString();
			}
			else
			{
				if(t.Sector != null)
				{
					// Hangs from ceiling?
					if(ti.Hangs)
					{
						zvalue = (float)t.Sector.CeilHeight + t.Position.z;
						zinfo = zvalue.ToString();
					}
					else
					{
						zvalue = (float)t.Sector.FloorHeight + t.Position.z;
						zinfo = zvalue.ToString();
					}
				}
				else
				{
					zvalue = t.Position.z;
					if(zvalue >= 0.0f) zinfo = "+" + zvalue.ToString(); else zinfo = zvalue.ToString();
				}
			}

			// Thing info
			infopanel.Text = " Thing " + t.Index + " ";
			type.Text = t.Type + " - " + ti.Title;
			action.Text = actioninfo;
			position.Text = t.Position.x.ToString() + ", " + t.Position.y.ToString() + ", " + zinfo;
			tag.Text = t.Tag.ToString();
			angle.Text = Angle2D.RealToDoom(t.Angle).ToString() + "\u00B0";
			
			// Sprite
            if (ti.Title == "Camera") // villsa
            {
                General.DisplayZoomedImage(spritetex, General.Map.Data.ThingCamera.GetBitmap());
                spritename.Text = "";
            }
            else if (ti.Title == "Trigger") // villsa 9/11/11
            {
                General.DisplayZoomedImage(spritetex, General.Map.Data.ThingTrigger.GetBitmap());
                spritename.Text = "";
            }
            else if (ti.Sprite.ToLowerInvariant().StartsWith(DataManager.INTERNAL_PREFIX) && (ti.Sprite.Length > DataManager.INTERNAL_PREFIX.Length))
            {
                spritename.Text = "";
                General.DisplayZoomedImage(spritetex, General.Map.Data.GetSpriteImage(ti.Sprite).GetBitmap());
            }
            else if ((ti.Sprite.Length <= 8) && (ti.Sprite.Length > 0))
            {
                spritename.Text = ti.Sprite;
                General.DisplayZoomedImage(spritetex, General.Map.Data.GetSpriteImage(ti.Sprite, ti.PalIndex).GetPreview());
            }
            else
            {
                spritename.Text = "";
                spritetex.BackgroundImage = null;
            }
			
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
				spritetex.BackgroundImage = null;
			}

			// Call base
			base.OnVisibleChanged(e);
		}
	}
}
