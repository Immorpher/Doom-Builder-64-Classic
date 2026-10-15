
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
    internal partial class ActionBrowserForm : DelayedForm
	{
		// Variables
		private int selectedaction;
		
		// Properties
		public int SelectedAction { get { return selectedaction; } }
		
		// Constructor
		public ActionBrowserForm(int action)
		{
			TreeNode cn, n;
			
			// Initialize
			InitializeComponent();

			// Go for all predefined categories
			foreach(LinedefActionCategory ac in General.Map.Config.ActionCategories)
			{
				// Empty category names will not be created
				// (those actions will go in the root of the tree)
				if(ac.Title.Length > 0)
				{
					// Create category
					cn = actions.Nodes.Add(ac.Title);
					foreach(LinedefActionInfo ai in ac.Actions)
					{
						// Create action
						n = cn.Nodes.Add(ai.Title);
						n.Tag = ai;

						// This is the given action?
						if(ai.Index == action)
						{
							// Select this and expand the category
							cn.Expand();
							actions.SelectedNode = n;
							n.EnsureVisible();
						}
					}
				}
				else
				{
					// Put actions in the tree root
					foreach(LinedefActionInfo ai in ac.Actions)
					{
						// Create action
						n = actions.Nodes.Add(ai.Title);
						n.Tag = ai;
					}
				}
			}
		}
		
		// This browses for an action
		// Returns the new action or the same action when cancelled
		public static int BrowseAction(IWin32Window owner, int action)
		{
			ActionBrowserForm f = new ActionBrowserForm(action);
            if (f.ShowDialog(owner) == DialogResult.OK)
                action = f.SelectedAction;
            else
                action = 0; // villsa
			f.Dispose();
			return action;
		}
		
		// OK clicked
		private void apply_Click(object sender, EventArgs e)
		{
			// Presume no result
			selectedaction = 0;
			
			// Predefined action?
			if(tabs.SelectedTab == tabactions)
			{
				// Action node selected?
				if((actions.SelectedNode != null) && (actions.SelectedNode.Tag is LinedefActionInfo))
				{
					// Our result
					selectedaction = (actions.SelectedNode.Tag as LinedefActionInfo).Index;
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

		// Double clicking on item
		private void actions_DoubleClick(object sender, EventArgs e)
		{
			// Action node selected?
			if((actions.SelectedNode != null) && (actions.SelectedNode.Tag is LinedefActionInfo))
			{
				if(apply.Enabled) apply_Click(this, EventArgs.Empty);
			}
		}

        private void ActionBrowserForm_Load(object sender, EventArgs e) // villsa
        {
            if (General.Map.FormatInterface.InDoom64Mode)
                this.prefixespanel.Hide();
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {

        }
	}
}
