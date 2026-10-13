
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
using CodeImp.DoomBuilder.Windows;
using Microsoft.Win32;
using System.Diagnostics;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Types;
using CodeImp.DoomBuilder.IO;
using System.Globalization;
using System.IO;
using CodeImp.DoomBuilder.Compilers;

#endregion

namespace CodeImp.DoomBuilder.Controls
{
	internal partial class ScriptEditorPanel : UserControl
	{
		#region ================== Constants

		// The tabs of the script lumps that are not part of a map and are always shown, in this order.
		// The first tab is the SCRIPTS lump of the map.
		private static readonly string[] GLOBAL_LUMP_TABS = MapManager.GLOBAL_SCRIPT_LUMPS;

		#endregion

		#region ================== Variables

		private List<CompilerError> compilererrors;

		// Find/Replace
		private ScriptFindReplaceForm findreplaceform;
		private FindReplaceOptions findoptions;
		
		#endregion
		
		#region ================== Properties
		
		public ScriptDocumentTab ActiveTab { get { return (tabs.SelectedTab as ScriptDocumentTab); } }

		// All the document tabs
		private List<ScriptDocumentTab> AllTabs
		{
			get
			{
				List<ScriptDocumentTab> all = new List<ScriptDocumentTab>();
				foreach(TabPage page in tabs.TabPages)
					if(page is ScriptDocumentTab) all.Add((ScriptDocumentTab)page);
				return all;
			}
		}

		#endregion
		
		#region ================== Constructor
		
		// Constructor
		public ScriptEditorPanel()
		{
			InitializeComponent();
		}
		
		// This initializes the control
		public void Initialize()
		{
			// Load the script lumps
			foreach(MapLumpInfo maplumpinfo in General.Map.Config.MapLumps.Values)
			{
				// Is this a script lump?
				if(maplumpinfo.script != null)
				{
					// Load this!
					ScriptLumpDocumentTab t = new ScriptLumpDocumentTab(this, maplumpinfo.name, maplumpinfo.script);
					tabs.TabPages.Add(t);
				}
			}

			// The global script lumps always have their own tab after the SCRIPTS
			foreach(string lumpname in GLOBAL_LUMP_TABS)
				tabs.TabPages.Add(CreateGlobalLumpTab(lumpname));

			// Select the first tab
			if(tabs.TabPages.Count > 0) tabs.SelectedIndex = 0;
			
			// Return to the tab that was being edited when the script editor was closed
			RestoreLastScript();
			
			// If the map has remembered any compile errors, then show them
			ShowErrors(General.Map.Errors);
			
			// Done
			UpdateToolbar(true);
		}
		
		// This applies user preferences
		public void ApplySettings()
		{
			// Apply settings
			//int panel2size = General.Settings.ReadSetting("scriptspanel.splitter", splitter.ClientRectangle.Height - splitter.SplitterDistance);
			//splitter.SplitterDistance = splitter.ClientRectangle.Height - panel2size;
			errorlist.Columns[0].Width = General.Settings.ReadSetting("scriptspanel.errorscolumn0width", errorlist.Columns[0].Width);
			errorlist.Columns[1].Width = General.Settings.ReadSetting("scriptspanel.errorscolumn1width", errorlist.Columns[1].Width);
			errorlist.Columns[2].Width = General.Settings.ReadSetting("scriptspanel.errorscolumn2width", errorlist.Columns[2].Width);
		}
		
		// This saves user preferences
		public void SaveSettings()
		{
			//General.Settings.WriteSetting("scriptspanel.splitter", splitter.ClientRectangle.Height - splitter.SplitterDistance);
			General.Settings.WriteSetting("scriptspanel.errorscolumn0width", errorlist.Columns[0].Width);
			General.Settings.WriteSetting("scriptspanel.errorscolumn1width", errorlist.Columns[1].Width);
			General.Settings.WriteSetting("scriptspanel.errorscolumn2width", errorlist.Columns[2].Width);
		}
		
		#endregion
		
		#region ================== Methods

		// Find Next
		public void FindNext(FindReplaceOptions options)
		{
			// Save the options
			findoptions = options;
			FindNext();
		}

		// Find Next with saved options
		public void FindNext()
		{
			if(!string.IsNullOrEmpty(findoptions.FindText) && (ActiveTab != null))
			{
				if(!ActiveTab.FindNext(findoptions))
				{
					General.MainWindow.DisplayStatus(StatusType.Warning, "Can't find any occurence of \"" + findoptions.FindText + "\".");
				}
			}
			else
			{
				General.MessageBeep(MessageBeepType.Default);
			}
		}
		
		// Replace if possible
		public void Replace(FindReplaceOptions options)
		{
			if(!string.IsNullOrEmpty(findoptions.FindText) && (options.ReplaceWith != null) && (ActiveTab != null))
			{
				if(string.Compare(ActiveTab.GetSelectedText(), options.FindText, !options.CaseSensitive) == 0)
				{
					// Replace selection
					ActiveTab.ReplaceSelection(options.ReplaceWith);
				}
			}
			else
			{
				General.MessageBeep(MessageBeepType.Default);
			}
		}
		
		// Replace all
		public void ReplaceAll(FindReplaceOptions options)
		{
			int replacements = 0;
			findoptions = options;
			if(!string.IsNullOrEmpty(findoptions.FindText) && (options.ReplaceWith != null) && (ActiveTab != null))
			{
				int firstfindpos = -1;
				int lastpos = -1;
				bool firstreplace = true;
				bool wrappedaround = false;
				int selectionstart = Math.Min(ActiveTab.SelectionStart, ActiveTab.SelectionEnd);
				
				// Continue finding and replacing until nothing more found
				while(ActiveTab.FindNext(findoptions))
				{
					int curpos = Math.Min(ActiveTab.SelectionStart, ActiveTab.SelectionEnd);
					if(curpos <= lastpos)
						wrappedaround = true;
					
					if(firstreplace)
					{
						// Remember where we started replacing
						firstfindpos = curpos;
					}
					else if(wrappedaround)
					{
						// Make sure we don't go past our start point, or we could be in an endless loop
						if(curpos >= firstfindpos)
							break;
					}
					
					Replace(findoptions);
					replacements++;
					firstreplace = false;

					lastpos = curpos;
				}

				// Restore selection
				ActiveTab.SelectionStart = selectionstart;
				ActiveTab.SelectionEnd = selectionstart;
				
				// Show result
				if(replacements == 0)
					General.MainWindow.DisplayStatus(StatusType.Warning, "Can't find any occurence of \"" + findoptions.FindText + "\".");
				else
					General.MainWindow.DisplayStatus(StatusType.Info, "Replaced " + replacements + " occurences of \"" + findoptions.FindText + "\" with \"" + findoptions.ReplaceWith + "\".");
			}
			else
			{
				General.MessageBeep(MessageBeepType.Default);
			}
		}
		
		// This closed the Find & Replace subwindow
		public void CloseFindReplace(bool closing)
		{
			if(findreplaceform != null)
			{
				if(!closing) findreplaceform.Close();
				findreplaceform = null;
			}
		}

		// This opens the Find & Replace subwindow
		public void OpenFindAndReplace()
		{
			if(findreplaceform == null)
				findreplaceform = new ScriptFindReplaceForm();

			try
			{
				if(findreplaceform.Visible)
					findreplaceform.Focus();
				else
					findreplaceform.Show(this.ParentForm);

				if(ActiveTab.SelectionEnd != ActiveTab.SelectionStart)
					findreplaceform.SetFindText(ActiveTab.GetSelectedText());
			}
			catch(Exception)
			{
				// If we can't pop up the find/replace form right now, thats just too bad.
			}
		}

		// This refreshes all settings
		public void RefreshSettings()
		{
			foreach(ScriptDocumentTab t in AllTabs)
			{
				t.RefreshSettings();
			}
		}

		// This clears all error marks and hides the errors list
		public void ClearErrors()
		{
			// Hide list
			splitter.Panel2Collapsed = true;
			errorlist.Items.Clear();

			// Clear marks
			foreach(ScriptDocumentTab t in AllTabs)
			{
				t.ClearMarks();
			}
		}
		
		// This shows the errors panel with the given errors
		// Also updates the scripts with markers for the given errors
		public void ShowErrors(IEnumerable<CompilerError> errors)
		{
			// Copy list
			if(errors != null)
				compilererrors = new List<CompilerError>(errors);
			else
				compilererrors = new List<CompilerError>();
			
			// Fill list
			errorlist.BeginUpdate();
			errorlist.Items.Clear();
			int listindex = 1;
			foreach(CompilerError e in compilererrors)
			{
				ListViewItem ei = new ListViewItem(listindex.ToString());
				ei.ImageIndex = 0;
				ei.SubItems.Add(e.description);
				if(e.filename.StartsWith("?"))
					ei.SubItems.Add(e.filename.Replace("?", "") + " (line " + e.linenumber.ToString() + ")");
				else
					ei.SubItems.Add(Path.GetFileName(e.filename) + " (line " + e.linenumber.ToString() + ")");
				ei.Tag = e;
				errorlist.Items.Add(ei);
				listindex++;
			}
			errorlist.EndUpdate();
			
			// Show marks on scripts
			foreach(ScriptDocumentTab t in AllTabs)
			{
				t.MarkScriptErrors(compilererrors);
			}
			
			// Show/hide panel
			splitter.Panel2Collapsed = (errorlist.Items.Count == 0);
		}
		
		// This returns a key that identifies a tab, to find it again later (or null when it can't be identified)
		private static string GetTabKey(ScriptDocumentTab t)
		{
			if(t is ScriptLumpDocumentTab) return "lump:" + t.Text;
			if(t is ScriptGlobalLumpDocumentTab) return "global:" + (t as ScriptGlobalLumpDocumentTab).LumpName;
			return null;
		}
		
		// This selects the tab that was being edited when the script editor was last closed
		private void RestoreLastScript()
		{
			string tabkey = General.Map.Options.ScriptEditorTab;
			if(tabkey.Length == 0) return;

			foreach(TabPage page in tabs.TabPages)
			{
				ScriptDocumentTab t = (page as ScriptDocumentTab);
				if((t != null) && (GetTabKey(t) == tabkey))
				{
					tabs.SelectedTab = t;
					break;
				}
			}
		}
		
		// This remembers which tab the script editor is showing, to show that again when it is opened again
		public void WriteOpenTabToConfiguration()
		{
			ScriptDocumentTab selected = (tabs.SelectedTab as ScriptDocumentTab);
			General.Map.Options.ScriptEditorTab = (selected != null) ? (GetTabKey(selected) ?? "") : "";
		}
		
		// This returns true when any of the scripts are changed
		public bool CheckImplicitChanges()
		{
			bool changes = false;
			foreach(ScriptDocumentTab t in AllTabs)
			{
				if(t.IsChanged) changes = true;
			}
			return changes;
		}
		
		// This forces the focus to the script editor
		public void ForceFocus()
		{
			ScriptDocumentTab t = ActiveTab;
			tabs.Focus();
			if(t != null) t.Focus();
		}
		
		// This does an implicit save on all documents that use implicit saving
		// Call this to save the lumps before disposing the panel!
		public void ImplicitSave()
		{
			// Save all scripts
			foreach(ScriptDocumentTab t in AllTabs)
			{
				t.Save();
			}
			
			UpdateToolbar(false);
		}
		
		// This updates the toolbar for the current status
		private void UpdateToolbar(bool focuseditor)
		{
			int numscriptsopen = tabs.TabPages.Count;
			ScriptDocumentTab t = null;
			
			// Get current script, if any are open
			if(numscriptsopen > 0)
				t = ActiveTab;
			
			// Enable/disable buttons
			buttoncompile.Enabled = (t != null) && (t.Config.Compiler != null);
			buttonkeywordhelp.Enabled = (t != null) && !string.IsNullOrEmpty(t.Config.KeywordHelp);
			buttonundo.Enabled = (t != null);
			buttonredo.Enabled = (t != null);
			buttoncopy.Enabled = (t != null);
			buttoncut.Enabled = (t != null);
			buttonpaste.Enabled = (t != null);
			
			if(t != null)
			{
				// Focus to script editor
				if(focuseditor) ForceFocus();
			}
		}

		// This creates the tab for a global script lump (ANIMDEFS, MAPINFO or SKYDEFS)
		private ScriptGlobalLumpDocumentTab CreateGlobalLumpTab(string lumpname)
		{
			// Find the script configuration for highlighting
			ScriptConfiguration config;
			if(!General.ScriptConfigs.TryGetValue("doom64_" + lumpname.ToLowerInvariant() + ".cfg", out config))
				config = new ScriptConfiguration();
			
			return new ScriptGlobalLumpDocumentTab(this, lumpname, config);
		}
		
		#endregion
		
		#region ================== Events

		// Called when the window that contains this panel closes
		public void OnClose()
		{
			// Close the sub windows now
			if(findreplaceform != null) findreplaceform.Dispose();
		}

		// Keyword help requested
		private void buttonkeywordhelp_Click(object sender, EventArgs e)
		{
			// Get script
			ScriptDocumentTab t = ActiveTab;
			t.LaunchKeywordHelp();
		}

		// A tab is selected
		private void tabs_Selecting(object sender, TabControlCancelEventArgs e)
		{
			UpdateToolbar(true);
		}
		
		// Compile Script clicked
		private void buttoncompile_Click(object sender, EventArgs e)
		{
			// First save all implicit scripts to the temporary wad file
			ImplicitSave();
			
			// Get script
			ScriptDocumentTab t = ActiveTab;

			// Compile now
			General.MainWindow.DisplayStatus(StatusType.Busy, "Compiling script " + t.Text + "...");
			Cursor.Current = Cursors.WaitCursor;
			t.Compile();

			// Show warning
			if((compilererrors != null) && (compilererrors.Count > 0))
				General.MainWindow.DisplayStatus(StatusType.Warning, compilererrors.Count.ToString() + " errors while compiling " + t.Text + "!");
			else
				General.MainWindow.DisplayStatus(StatusType.Info, "Script " + t.Text + " compiled without errors.");

			Cursor.Current = Cursors.Default;
			UpdateToolbar(true);
		}
		
		// Undo clicked
		private void buttonundo_Click(object sender, EventArgs e)
		{
			ScriptDocumentTab t = ActiveTab;
			t.Undo();
			UpdateToolbar(true);
		}
		
		// Redo clicked
		private void buttonredo_Click(object sender, EventArgs e)
		{
			ScriptDocumentTab t = ActiveTab;
			t.Redo();
			UpdateToolbar(true);
		}
		
		// Cut clicked
		private void buttoncut_Click(object sender, EventArgs e)
		{
			ScriptDocumentTab t = ActiveTab;
			t.Cut();
			UpdateToolbar(true);
		}
		
		// Copy clicked
		private void buttoncopy_Click(object sender, EventArgs e)
		{
			ScriptDocumentTab t = ActiveTab;
			t.Copy();
			UpdateToolbar(true);
		}

		// Paste clicked
		private void buttonpaste_Click(object sender, EventArgs e)
		{
			ScriptDocumentTab t = ActiveTab;
			t.Paste();
			UpdateToolbar(true);
		}
		
		// Mouse released on tabs
		private void tabs_MouseUp(object sender, MouseEventArgs e)
		{
			ForceFocus();
		}
		
		// User double-clicks and error in the list
		private void errorlist_ItemActivate(object sender, EventArgs e)
		{
			// Anything selection?
			if(errorlist.SelectedItems.Count > 0)
			{
				// Get the compiler error
				CompilerError err = (CompilerError)errorlist.SelectedItems[0].Tag;
				
				// Show the tab with the script that matches
				foreach(ScriptDocumentTab t in AllTabs)
				{
					if(t.VerifyErrorForScript(err))
					{
						tabs.SelectedTab = t;
						t.MoveToLine(err.linenumber);
						break;
					}
				}

				ForceFocus();
			}
		}
		
		#endregion
	}
}
