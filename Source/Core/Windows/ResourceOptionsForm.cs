
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
using CodeImp.DoomBuilder.Map;
using System.IO;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.Controls;
using CodeImp.DoomBuilder.IO;

#endregion

namespace CodeImp.DoomBuilder.Windows
{
	internal partial class ResourceOptionsForm : DelayedForm
	{
		// Variables
		private DataLocation res;
		
		// Properties
		public DataLocation ResourceLocation { get { return res; } }
		
		// Constructor
		public ResourceOptionsForm(DataLocation settings, string caption)
		{
			// Initialize
			InitializeComponent();

			// Set caption
			this.Text = caption;
			
			// Apply settings from ResourceLocation
			this.res = settings;
			if(res.type == DataLocation.RESOURCE_WAD)
			{
				wadlocation.Text = res.location;
			}
			
			// Checkbox
			notfortesting.Checked = res.notfortesting;
		}
		
		// OK clicked
		private void apply_Click(object sender, EventArgs e)
		{
			// Check if file is specified
			if((wadlocation.Text.Length == 0) ||
			   (!File.Exists(wadlocation.Text)))
			{
				// No valid wad file specified
				MessageBox.Show(this, "Please select a valid WAD File resource.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			else
			{
				// Apply settings
				res.type = DataLocation.RESOURCE_WAD;
				res.location = wadlocation.Text;
				res.option1 = false;
				res.option2 = false;
				res.notfortesting = notfortesting.Checked;

				// Done
				this.DialogResult = DialogResult.OK;
				this.Close();
			}
		}
		
		// Cancel clicked
		private void cancel_Click(object sender, EventArgs e)
		{
			// Just hide
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		// Browse WAD File clicked
		private void browsewad_Click(object sender, EventArgs e)
		{
			// Browse for WAD File
			if(wadfiledialog.ShowDialog(this) == DialogResult.OK)
			{
				// Use this file
				wadlocation.Text = wadfiledialog.FileName;
			}
		}

		// Help
		private void ResourceOptionsForm_HelpRequested(object sender, HelpEventArgs hlpevent)
		{
			General.ShowHelp("w_resourceoptions.html");
			hlpevent.Handled = true;
		}
	}
}