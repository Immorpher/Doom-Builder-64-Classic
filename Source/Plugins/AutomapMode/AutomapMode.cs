
#region ================== Copyright (c) 2016 Boris Iwanski

/*
 * Copyright (c) 2016 Boris Iwanski https://github.com/biwa/automapmode
 * Additions by MaxED (GZDoom Builder)
 * Doom 64 support by the Doom Builder 64 Enhanced authors
 * Ported to Doom Builder 64 II
 *
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
using System.Drawing;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Editing;

#endregion

namespace CodeImp.DoomBuilder.AutomapMode
{
	[EditMode(DisplayName = "Automap Mode",
			  SwitchAction = "automapmode",		// Action name used to switch to this mode
			  ButtonImage = "automap.png",		// Image resource name for the button
			  ButtonOrder = int.MinValue + 503,	// Position of the button (lower is more to the left)
			  ButtonGroup = "000_editing",
			  UseByDefault = true)]

	public class AutomapMode : ClassicMode
	{
		#region ================== Enums

		internal enum ColorPreset
		{
			DOOM,
			HEXEN,
			STRIFE,
			DOOM64,
		}

		#endregion

		#region ================== Variables

		private CustomPresentation automappresentation;
		private List<Linedef> validlinedefs;
		private HashSet<Sector> secretsectors;
		private HashSet<Sector> hiddensectors;

		// Highlighted item
		private Linedef highlighted;

		// UI
		private MenusForm menusform;

		// Colors
		private PixelColor ColorSingleSided;
		private PixelColor ColorSecret;
		private PixelColor ColorSpecial;
		private PixelColor ColorFloorDiff;
		private PixelColor ColorCeilDiff;
		private PixelColor ColorMatchingHeight;
		private PixelColor ColorHiddenFlag;
		private PixelColor ColorInvisible;
		private PixelColor ColorBackground;

		#endregion

		#region ================== Properties

		public override object HighlightedObject { get { return highlighted; } }

		#endregion

		#region ================== Constructor / Disposer

		public AutomapMode()
		{
			// Create and setup menu
			menusform = new MenusForm();
			menusform.ShowHiddenLines = General.Settings.ReadPluginSetting("automapmode.showhiddenlines", false);
			menusform.ShowSecretSectors = General.Settings.ReadPluginSetting("automapmode.showsecretsectors", false);
			menusform.ColorPreset = (ColorPreset)General.Settings.ReadPluginSetting("automapmode.colorpreset", (int)ColorPreset.DOOM);

			// Doom 64 maps default to the Doom 64 preset the first time this map is opened
			if(General.Map != null && General.Map.FormatInterface.InDoom64Mode &&
			   General.Settings.ReadPluginSetting("automapmode.colorpreset64set", 0) == 0)
			{
				menusform.ColorPreset = ColorPreset.DOOM64;
				General.Settings.WritePluginSetting("automapmode.colorpreset64set", 1);
			}

			// Handle events
			menusform.OnShowHiddenLinesChanged += delegate
			{
				UpdateValidLinedefs();
				General.Interface.RedrawDisplay();
			};

			menusform.OnShowSecretSectorsChanged += delegate { General.Interface.RedrawDisplay(); };

			menusform.OnColorPresetChanged += delegate
			{
				ApplyColorPreset(menusform.ColorPreset);
				General.Interface.RedrawDisplay();
			};

			// Apply color preset
			ApplyColorPreset(menusform.ColorPreset);
		}

		#endregion

		#region ================== Methods

		// This highlights a new item
		private void Highlight(Linedef l)
		{
			// Update display
			if(renderer.StartPlotter(false))
			{
				// Undraw previous highlight
				if((highlighted != null) && !highlighted.IsDisposed)
				{
					PixelColor c = LinedefIsValid(highlighted) ? DetermineLinedefColor(highlighted) : PixelColor.Transparent;
					renderer.PlotLine(highlighted.Start.Position, highlighted.End.Position, c);
				}

				// Set new highlight
				highlighted = l;

				// Render highlighted item
				if((highlighted != null) && !highlighted.IsDisposed && LinedefIsValid(highlighted))
				{
					renderer.PlotLine(highlighted.Start.Position, highlighted.End.Position, General.Colors.Highlight);
				}

				// Done
				renderer.Finish();
				renderer.Present();
			}

			// Show highlight info
			if((highlighted != null) && !highlighted.IsDisposed)
				General.Interface.ShowLinedefInfo(highlighted);
			else
				General.Interface.HideInfo();
		}

		// This rebuilds the list of linedefs that are visible on the automap
		internal void UpdateValidLinedefs()
		{
			validlinedefs = new List<Linedef>();
			if(General.Map == null) return;

			foreach(Linedef ld in General.Map.Map.Linedefs)
				if(LinedefIsValid(ld)) validlinedefs.Add(ld);
		}

		// This rebuilds the secret / hidden sector lookups
		internal void UpdateSectorFlags()
		{
			secretsectors = new HashSet<Sector>();
			hiddensectors = new HashSet<Sector>();
			if(General.Map == null) return;

			foreach(Sector s in General.Map.Map.Sectors)
			{
				if(SectorIsSecret(s)) secretsectors.Add(s);
				if(SectorIsHidden(s)) hiddensectors.Add(s);
			}
		}

		private PixelColor DetermineLinedefColor(Linedef ld)
		{
			if(menusform.ShowSecretSectors && secretsectors != null &&
			   ((ld.Front != null && secretsectors.Contains(ld.Front.Sector)) ||
				(ld.Back != null && secretsectors.Contains(ld.Back.Sector))))
				return ColorSecret;

			if(ld.IsFlagSet(BuilderPlug.Me.HiddenFlag)) return ColorHiddenFlag;
			if(LinedefIsInHiddenSector(ld)) return ColorInvisible;
			if(ld.Back == null || ld.Front == null || ld.IsFlagSet(BuilderPlug.Me.SecretFlag)) return ColorSingleSided;

			// Doom 64: two-sided lines with an action are drawn in the special color
			if(LinedefIsSpecial(ld)) return ColorSpecial;
			if(ld.Front.Sector.FloorHeight != ld.Back.Sector.FloorHeight) return ColorFloorDiff;
			if(ld.Front.Sector.CeilHeight != ld.Back.Sector.CeilHeight) return ColorCeilDiff;

			// Two-sided with matching floor and ceiling heights
			return ColorMatchingHeight;
		}

		private bool LinedefIsValid(Linedef ld)
		{
			// Ctrl temporarily inverts the "show hidden lines" toggle
			if(menusform.ShowHiddenLines ^ General.Interface.CtrlState) return true;

			// "Hide on Automap" linedef flag always wins
			if(ld.IsFlagSet(BuilderPlug.Me.HiddenFlag)) return false;

			// Doom 64: sectors can be hidden from the automap as a whole
			if(LinedefIsInHiddenSector(ld)) return false;

			// Doom 64: "Show on Automap" forces the line to be drawn
			if(BuilderPlug.Me.AlwaysShowFlag != null && ld.IsFlagSet(BuilderPlug.Me.AlwaysShowFlag)) return true;

			// One-sided lines and lines drawn as one-sided are always shown
			if(ld.Back == null || ld.Front == null || ld.IsFlagSet(BuilderPlug.Me.SecretFlag)) return true;

			// Doom 64: lines with an action are shown unless "Hide Special on Automap" is set
			if(LinedefIsSpecial(ld)) return true;

			// In Doom 64 every remaining two-sided line is drawn
			if(General.Map.FormatInterface.InDoom64Mode) return true;

			// Two-sided lines are only shown when there's a height difference
			if(ld.Front.Sector.FloorHeight != ld.Back.Sector.FloorHeight ||
			   ld.Front.Sector.CeilHeight != ld.Back.Sector.CeilHeight) return true;

			return false;
		}

		// Doom 64: a line with an action is drawn as a special line unless
		// the "Hide Special on Automap" flag (33554432) is set
		private bool LinedefIsSpecial(Linedef ld)
		{
			if(!General.Map.FormatInterface.InDoom64Mode) return false;
			if(ld.Action == 0) return false;
			return !ld.IsFlagSet(BuilderPlug.Me.HideSpecialFlag);
		}

		// A line is hidden when every sector it borders is flagged "Hide on Automap"
		private bool LinedefIsInHiddenSector(Linedef ld)
		{
			if(hiddensectors == null || hiddensectors.Count == 0) return false;

			bool hashiddenside = false;

			if(ld.Front != null)
			{
				if(!hiddensectors.Contains(ld.Front.Sector)) return false;
				hashiddenside = true;
			}

			if(ld.Back != null)
			{
				if(!hiddensectors.Contains(ld.Back.Sector)) return false;
				hashiddenside = true;
			}

			return hashiddenside;
		}

		private static bool SectorIsSecret(Sector s)
		{
			// Doom 64 stores "secret" as a sector flag instead of a sector effect
			if(General.Map.FormatInterface.InDoom64Mode) return s.IsFlagSet("32");

			// Everything else: sector effect 9
			return (s.Effect == 9);
		}

		private static bool SectorIsHidden(Sector s)
		{
			// Doom 64 only: sector flag 512 = "Hide on Automap"
			if(General.Map.FormatInterface.InDoom64Mode) return s.IsFlagSet("512");
			return false;
		}

		private void ApplyColorPreset(ColorPreset preset)
		{
			switch(preset)
			{
				case ColorPreset.DOOM:
					ColorSingleSided = new PixelColor(255, 252, 0, 0);
					ColorSecret = new PixelColor(255, 255, 0, 255);
					ColorFloorDiff = new PixelColor(255, 188, 120, 72);
					ColorCeilDiff = new PixelColor(255, 252, 252, 0);
					ColorHiddenFlag = new PixelColor(255, 192, 192, 192);
					ColorInvisible = new PixelColor(255, 128, 128, 128);
					ColorMatchingHeight = new PixelColor(255, 108, 108, 108);
					ColorBackground = new PixelColor(255, 0, 0, 0);
					break;

				case ColorPreset.HEXEN:
					ColorSingleSided = new PixelColor(255, 89, 64, 27);
					ColorSecret = new PixelColor(255, 255, 0, 255);
					ColorFloorDiff = new PixelColor(255, 208, 176, 133);
					ColorCeilDiff = new PixelColor(255, 103, 59, 31);
					ColorHiddenFlag = new PixelColor(255, 192, 192, 192);
					ColorInvisible = new PixelColor(255, 108, 108, 108);
					ColorMatchingHeight = new PixelColor(255, 108, 108, 108);
					ColorBackground = new PixelColor(255, 163, 129, 84);
					break;

				case ColorPreset.STRIFE:
					ColorSingleSided = new PixelColor(255, 199, 195, 195);
					ColorSecret = new PixelColor(255, 255, 0, 255);
					ColorFloorDiff = new PixelColor(255, 55, 59, 91);
					ColorCeilDiff = new PixelColor(255, 108, 108, 108);
					ColorHiddenFlag = new PixelColor(255, 0, 87, 130);
					ColorInvisible = new PixelColor(255, 192, 192, 192);
					ColorMatchingHeight = new PixelColor(255, 112, 112, 160);
					ColorBackground = new PixelColor(255, 0, 0, 0);
					break;

				case ColorPreset.DOOM64:
					// Doom 64 automap palette
					ColorSingleSided = new PixelColor(255, 0xE5, 0x00, 0x00);		// #e50000
					ColorSecret = new PixelColor(255, 255, 0, 255);
					ColorFloorDiff = new PixelColor(255, 0xC0, 0x80, 0x43);			// #c08043 (two-sided)
					ColorCeilDiff = new PixelColor(255, 0xC0, 0x80, 0x43);			// #c08043 (two-sided)
					ColorMatchingHeight = new PixelColor(255, 0xC0, 0x80, 0x43);	// #c08043 (two-sided)
					ColorSpecial = new PixelColor(255, 0xFF, 0xFF, 0x00);			// #ffff00
					ColorHiddenFlag = new PixelColor(255, 192, 192, 192);
					ColorInvisible = new PixelColor(255, 128, 128, 128);
					ColorBackground = new PixelColor(255, 0, 0, 0);
					break;
			}
		}

		#endregion

		#region ================== Events

		// Cancel mode
		public override void OnCancel()
		{
			base.OnCancel();

			// Return to this mode
			General.Editing.ChangeMode(new AutomapMode());
		}

		// Mode engages
		public override void OnEngage()
		{
			base.OnEngage();

			// Automap presentation without the surfaces
			automappresentation = new CustomPresentation();
			automappresentation.AddLayer(new PresentLayer(RendererLayer.Overlay, BlendingMode.Mask));
			automappresentation.AddLayer(new PresentLayer(RendererLayer.Grid, BlendingMode.Mask));
			automappresentation.AddLayer(new PresentLayer(RendererLayer.Geometry, BlendingMode.Alpha, 1f, true));
			renderer.SetPresentation(automappresentation);

			UpdateSectorFlags();
			UpdateValidLinedefs();

			// Show UI
			menusform.Register();
		}

		// Mode disengages
		public override void OnDisengage()
		{
			base.OnDisengage();

			// Store settings
			General.Settings.WritePluginSetting("automapmode.showhiddenlines", menusform.ShowHiddenLines);
			General.Settings.WritePluginSetting("automapmode.showsecretsectors", menusform.ShowSecretSectors);
			General.Settings.WritePluginSetting("automapmode.colorpreset", (int)menusform.ColorPreset);

			// Hide UI
			menusform.Unregister();

			// Hide highlight info
			General.Interface.HideInfo();
		}

		public override void OnUndoEnd()
		{
			UpdateSectorFlags();
			UpdateValidLinedefs();

			base.OnUndoEnd();
		}

		public override void OnRedoEnd()
		{
			UpdateSectorFlags();
			UpdateValidLinedefs();

			base.OnRedoEnd();
		}

		// This redraws the display
		public override void OnRedrawDisplay()
		{
			renderer.RedrawSurface();

			// Render lines
			if(renderer.StartPlotter(true))
			{
				foreach(Linedef ld in General.Map.Map.Linedefs)
				{
					if(LinedefIsValid(ld))
						renderer.PlotLine(ld.Start.Position, ld.End.Position, DetermineLinedefColor(ld));
				}

				if((highlighted != null) && !highlighted.IsDisposed && LinedefIsValid(highlighted))
				{
					renderer.PlotLine(highlighted.Start.Position, highlighted.End.Position, General.Colors.Highlight);
				}

				renderer.Finish();
			}

			// Render automap background
			if(renderer.StartOverlay(true))
			{
				RectangleF screenrect = new RectangleF(0, 0, General.Interface.Display.Width, General.Interface.Display.Height);
				renderer.RenderRectangleFilled(screenrect, ColorBackground, false);
				renderer.Finish();
			}

			renderer.Present();
		}

		protected override void OnSelectEnd()
		{
			// Item highlighted?
			if((highlighted != null) && !highlighted.IsDisposed)
			{
				General.Map.UndoRedo.CreateUndo("Toggle \"Shown as 1-sided on automap\" linedef flag");

				// Toggle flag
				highlighted.SetFlag(BuilderPlug.Me.SecretFlag, !highlighted.IsFlagSet(BuilderPlug.Me.SecretFlag));
				UpdateValidLinedefs();
				General.Interface.RedrawDisplay();
			}

			base.OnSelectEnd();
		}

		protected override void OnEditEnd()
		{
			// Item highlighted?
			if((highlighted != null) && !highlighted.IsDisposed)
			{
				General.Map.UndoRedo.CreateUndo("Toggle \"Not shown on automap\" linedef flag");

				// Toggle flag
				highlighted.SetFlag(BuilderPlug.Me.HiddenFlag, !highlighted.IsFlagSet(BuilderPlug.Me.HiddenFlag));
				UpdateValidLinedefs();
				General.Interface.RedrawDisplay();
			}

			base.OnEditEnd();
		}

		// Mouse moves
		public override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			// Not holding any buttons?
			if(e.Button == MouseButtons.None)
			{
				// Find the nearest linedef within highlight range
				Linedef l = MapSet.NearestLinedefRange(validlinedefs, mousemappos, BuilderPlug.Me.HighlightRange / renderer.Scale);

				// Highlight if not the same
				if(l != highlighted) Highlight(l);
			}
		}

		// Mouse leaves
		public override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);

			// Highlight nothing
			Highlight(null);
		}

		public override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			if(e.Control)
			{
				UpdateValidLinedefs();
				General.Interface.RedrawDisplay();
			}
		}

		public override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);

			if(!e.Control)
			{
				UpdateValidLinedefs();
				General.Interface.RedrawDisplay();
			}
		}

		#endregion
	}
}
