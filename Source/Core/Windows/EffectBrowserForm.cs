
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
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Controls;

#endregion

namespace CodeImp.DoomBuilder.Windows
{
	internal partial class EffectBrowserForm : DelayedForm
	{
		// Variables
		private int selectedeffect;
		
		// Properties
		public int SelectedEffect { get { return selectedeffect; } }
		
		// Constructor
		public EffectBrowserForm(int effect)
		{
			ListViewItem n;
			
			// Initialize
			InitializeComponent();

			// Go for all predefined effects
			foreach(SectorEffectInfo si in General.Map.Config.SortedSectorEffects)
			{
				// Create effect
				n = effects.Items.Add(si.Index.ToString());
				n.SubItems.Add(si.Title);
				n.Tag = si;
				if(si.Index == effect) n.Selected = true;
			}
			
		}
		
		// This browses for an effect
		// Returns the new effect or the same effect when cancelled
		public static int BrowseEffect(IWin32Window owner, int effect)
		{
			EffectBrowserForm f = new EffectBrowserForm(effect);
			if(f.ShowDialog(owner) == DialogResult.OK) effect = f.SelectedEffect;
			f.Dispose();
			return effect;
		}
		
		// OK clicked
		private void apply_Click(object sender, EventArgs e)
		{
			// Presume no result
			selectedeffect = 0;
			
			// Predefined action?
			if(tabs.SelectedTab == tabeffects)
			{
				// Effect selected?
				if((effects.SelectedItems.Count > 0) && (effects.SelectedItems[0].Tag is SectorEffectInfo))
				{
					// Our result
					selectedeffect = (effects.SelectedItems[0].Tag as SectorEffectInfo).Index;
				}
			}
			// Done
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		// Cancel clicked
		private void cancel_Click(object sender, EventArgs e)
		{
			// Leave
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}
		
		// Double-clicked on item
		private void effects_DoubleClick(object sender, EventArgs e)
		{
			// Effect selected?
			if((effects.SelectedItems.Count > 0) && (effects.SelectedItems[0].Tag is SectorEffectInfo))
			{
				if(apply.Enabled) apply_Click(this, EventArgs.Empty);
			}
		}
	}
}