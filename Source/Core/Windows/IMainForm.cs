
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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Drawing;
using System.ComponentModel;
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.Map;
using SlimDX.Direct3D9;
using SlimDX;
using CodeImp.DoomBuilder.Geometry;
using System.Drawing.Imaging;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.Controls;

#endregion

namespace CodeImp.DoomBuilder.Windows
{
	public interface IMainForm : IWin32Window
	{
		// Properties
		bool AltState { get; }
		bool CtrlState { get; }
		bool ShiftState { get; }
		bool MouseInDisplay { get; }
		bool AutoMerge { get; }
		bool SnapToGrid { get; }
		bool MouseExclusive { get; }
		MouseButtons MouseButtons { get; }
		bool IsActiveWindow { get; }
		RenderTargetControl Display { get; }
		
		// Methods
		void DisplayReady();
		void DisplayStatus(StatusType type, string message);
		void DisplayStatus(StatusInfo newstatus);
		void RedrawDisplay();
		DialogResult ShowEditVertices(ICollection<Vertex> vertices);
		DialogResult ShowEditLinedefs(ICollection<Linedef> lines);
		DialogResult ShowEditSectors(ICollection<Sector> sectors);
		DialogResult ShowEditThings(ICollection<Thing> things);
		void ShowLinedefInfo(Linedef l);
		void ShowSectorInfo(Sector s);
		void ShowThingInfo(Thing t);
		void ShowVertexInfo(Vertex v);
		void HideInfo();
		void RefreshInfo();
		void UpdateCoordinates(Vector2D coords);
		bool Focus();
		void EnableProcessing();
		void DisableProcessing();
		void StartExclusiveMouseInput();
		void StopExclusiveMouseInput();
		void BreakExclusiveMouseInput();
		void ResumeExclusiveMouseInput();
		void SetCursor(Cursor cursor);
		void MessageBeep(MessageBeepType type);

		// 
		// This moves the focus to the editing display.
		// 
		bool FocusDisplay();

		// 
		// This browses the lindef types
		// 
		// Returns the new action or the same action when cancelled</returns>
		int BrowseLinedefActions(IWin32Window owner, int initialvalue);
		
		// 
		// This browses sector effects
		// 
		// Returns the new effect or the same effect when cancelled</returns>
		int BrowseSectorEffect(IWin32Window owner, int initialvalue);

		// 
		// This browses for a texture
		// 
		// Returns the new texture name or the same texture name when cancelled</returns>
		string BrowseTexture(IWin32Window owner, string initialvalue);

		// 
		// This browses for a flat
		// 
		// Returns the new flat name or the same flat name when cancelled</returns>
		string BrowseFlat(IWin32Window owner, string initialvalue);

		// 
		// THis browses for a thing type
		// 
		// Returns the new thing type or the same thing type when cancelled</returns>
		int BrowseThingType(IWin32Window owner, int initialvalue);
		
		// 
		// This adds a menu to the Doom Builder menu strip.
		// 
		// NOTE: When the Tag property of menu items is set with a string, this changes the
		// tag to a fully qualified action name by prefixing it with the assembly name.
		// 
		void AddMenu(ToolStripMenuItem menu);
		
		// 
		// This adds a menu or menu item to the Doom Builder menu strip in a specific location.
		// 
		// NOTE: When the Tag property of menu items is set with a string, this changes the
		// tag to a fully qualified action name by prefixing it with the assembly name.
		// 
		// 
		void AddMenu(ToolStripMenuItem menu, MenuSection section);
		
		// 
		// This removes a menu from the Doom Builder menu strip.
		// 
		void RemoveMenu(ToolStripMenuItem menu);
		
		// 
		// This method invokes the action specified on the Tag property of the given menu item.
		// 
		// <param name="sender">Menu item with Tag property set to the name of an action
		// that you want to invoke.</param>
		// <param name="e">Unused.</param>
		void InvokeTaggedAction(object sender, EventArgs e);
		
		// 
		// This adds a custom button to the toolbar.
		// 
		void AddButton(ToolStripItem button);

		// 
		// This adds a custom button to a specific section in the toolbar. Note that the visibility of the button will be controlled by the user's preferences of that section! ToolbarSection.Modes places the button on the vertical editing modes toolbar on the left.
		// 
		void AddButton(ToolStripItem button, ToolbarSection section);

		// 
		// This removes a custom button from the toolbar.
		// 
		void RemoveButton(ToolStripItem button);
		
		// 
		// This adds a docker to the side panel.
		// 
		void AddDocker(Docker d);
		
		// 
		// This removes a docker from the side panel.
		// 
		bool RemoveDocker(Docker d);
		
		// 
		// Selects a docker in the side panel.
		// 
		bool SelectDocker(Docker d);
		
		// 
		// This selected the previously selected docker in the side panel.
		// 
		void SelectPreviousDocker();
	}
}
