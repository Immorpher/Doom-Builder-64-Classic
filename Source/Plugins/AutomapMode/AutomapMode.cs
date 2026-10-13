
#region ================== Copyright (c) 2016 Boris Iwanski

/*
 * Copyright (c) 2016 Boris Iwanski https://github.com/biwa/automapmode
 * Additions by MaxED (GZDoom Builder)
 * Doom 64 support by the Doom Builder 64 Enhanced authors
 * Ported to Doom Builder 64 Classic
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
using System.Reflection;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Geometry;
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

		// How the automap is drawn
		internal enum AutomapType
		{
			LINES,		// Only the lines
			FLOORS,		// The floor textures (tinted by the floor colors) below the lines, like the Doom 64 textured automap
		}

		#endregion

		#region ================== Variables

		private CustomPresentation automappresentation;
		private List<Linedef> validlinedefs;
		private HashSet<Sector> secretsectors;

		// The view mode and full brightness setting that were active before this mode started, so they can be restored
		private ViewMode previousviewmode;
		private bool previousfullbrightness;
		private bool previouslighteffects;
		private bool viewmodesaved;

		// True while this mode changes the view mode itself (the redraws that this causes are ignored)
		private bool changingview;

		// Highlighted item
		private Linedef highlighted;
		private Sector highlightedsector;	// Floors type: the sector under the mouse

		// UI
		private MenusForm menusform;

		// Colors
		// Doom 64 automap palette
		private static readonly PixelColor ColorSingleSided = new PixelColor(255, 0xE5, 0x00, 0x00);	// #e50000
		private static readonly PixelColor ColorSecret = new PixelColor(255, 255, 0, 255);
		private static readonly PixelColor ColorTwoSided = new PixelColor(255, 0xC0, 0x80, 0x43);		// #c08043
		private static readonly PixelColor ColorSpecial = new PixelColor(255, 0xFF, 0xFF, 0x00);		// #ffff00
		private static readonly PixelColor ColorHiddenFlag = new PixelColor(255, 192, 192, 192);
		private static readonly PixelColor ColorBackground = new PixelColor(255, 0, 0, 0);

		// Sector flag "Hide on Automap" (the floor is not drawn on the textured automap)
		private const string SECTOR_HIDE_FLAG = "512";

		// Thing flag "Count Secret on Pickup"
		private const string THING_SECRET_FLAG = "256";

		// Radius of the thing triangles of the Doom 64 automap, in map units
		private const float THING_TRIANGLE_RADIUS = 32.0f;

		#endregion

		#region ================== Properties

		public override object HighlightedObject { get { if(menusform.Type == AutomapType.FLOORS) return highlightedsector; else return highlighted; } }

		#endregion

		#region ================== Constructor / Disposer

		public AutomapMode()
		{
			// Create and setup menu
			menusform = new MenusForm();
			menusform.ShowHiddenLines = General.Settings.ReadPluginSetting("automapmode.showhiddenlines", false);
			menusform.ShowSecretSectors = General.Settings.ReadPluginSetting("automapmode.showsecretsectors", false);
			// The type list is stored by index (the old "automapmode.colorpreset" settings are not used anymore)
			int typeindex = General.Settings.ReadPluginSetting("automapmode.type", (int)AutomapType.LINES);
			if((typeindex < 0) || (typeindex > (int)AutomapType.FLOORS)) typeindex = (int)AutomapType.LINES;
			menusform.Type = (AutomapType)typeindex;

			// Handle events
			menusform.OnShowHiddenLinesChanged += delegate
			{
				UpdateValidLinedefs();
				General.Interface.RedrawDisplay();
			};

			menusform.OnShowSecretSectorsChanged += delegate { General.Interface.RedrawDisplay(); };

			menusform.OnTypeChanged += delegate
			{
				// Store right away, so a new instance of this mode (after Cancel) starts with the same type
				General.Settings.WritePluginSetting("automapmode.type", (int)menusform.Type);

				// Not engaged yet (or anymore): the type is applied when the mode engages
				if(!viewmodesaved) return;

				highlighted = null;
				highlightedsector = null;
				General.Interface.HideInfo();

				ApplyType();
				General.Interface.RedrawDisplay();
			};
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

		// This returns the corners of the triangle that the Doom 64 automap draws for a thing: the tip points in the
		// direction the thing is facing, the other corners are at 225 and 135 degrees from there (AM_DrawThings)
		private static Vector2D[] ThingTriangle(Thing t)
		{
			float[] offsets = new float[] { 0.0f, Angle2D.PI * 1.25f, Angle2D.PI * 0.75f };
			Vector2D[] corners = new Vector2D[3];
			for(int i = 0; i < 3; i++)
			{
				float a = t.Angle + offsets[i];
				corners[i] = new Vector2D(t.Position.x + (float)Math.Cos(a) * THING_TRIANGLE_RADIUS,
										  t.Position.y + (float)Math.Sin(a) * THING_TRIANGLE_RADIUS);
			}
			return corners;
		}

		// Floors type: this plots the outline of a sector
		private void PlotSectorOutline(Sector s, PixelColor c)
		{
			foreach(Sidedef sd in s.Sidedefs)
				renderer.PlotLine(sd.Line.Start.Position, sd.Line.End.Position, c);
		}

		// Floors type: this highlights a sector
		private void HighlightSector(Sector s)
		{
			highlightedsector = s;

			// The plotter only contains the highlight in this type
			if(renderer.StartPlotter(true))
			{
				if((highlightedsector != null) && !highlightedsector.IsDisposed)
					PlotSectorOutline(highlightedsector, General.Colors.Highlight);

				renderer.Finish();
				renderer.Present();
			}

			if((highlightedsector != null) && !highlightedsector.IsDisposed)
				General.Interface.ShowSectorInfo(highlightedsector);
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

		// This rebuilds the secret sector lookup
		internal void UpdateSectorFlags()
		{
			secretsectors = new HashSet<Sector>();
			if(General.Map == null) return;

			foreach(Sector s in General.Map.Map.Sectors)
			{
				if(SectorIsSecret(s)) secretsectors.Add(s);
			}
		}

		private PixelColor DetermineLinedefColor(Linedef ld)
		{
			if(menusform.ShowSecretSectors && secretsectors != null &&
			   ((ld.Front != null && secretsectors.Contains(ld.Front.Sector)) ||
				(ld.Back != null && secretsectors.Contains(ld.Back.Sector))))
				return ColorSecret;

			if(ld.IsFlagSet(BuilderPlug.Me.HiddenFlag)) return ColorHiddenFlag;
			if(ld.Back == null || ld.Front == null || ld.IsFlagSet(BuilderPlug.Me.SecretFlag)) return ColorSingleSided;

			// Two-sided lines with an action are drawn in the special color
			if(LinedefIsSpecial(ld)) return ColorSpecial;

			// All other two-sided lines have the same color
			return ColorTwoSided;
		}

		private bool LinedefIsValid(Linedef ld)
		{
			// Ctrl temporarily inverts the "show hidden lines" toggle
			if(menusform.ShowHiddenLines ^ General.Interface.CtrlState) return true;

			// "Hide on Automap" linedef flag always wins
			if(ld.IsFlagSet(BuilderPlug.Me.HiddenFlag)) return false;

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

		private static bool SectorIsSecret(Sector s)
		{
			// Doom 64 stores "secret" as a sector flag instead of a sector effect
			if(General.Map.FormatInterface.InDoom64Mode) return s.IsFlagSet("32");

			// Everything else: sector effect 9
			return (s.Effect == 9);
		}

		// This applies the selected automap type: which layers are shown and the view mode that draws the floors
		private void ApplyType()
		{
			// Presentation (no grid layer, the game's automap has none). The floors are drawn on the surface layer (and the hidden ones are covered on the things layer).
			automappresentation = new CustomPresentation();
			automappresentation.AddLayer(new PresentLayer(RendererLayer.Overlay, BlendingMode.Mask));
			if(menusform.Type == AutomapType.FLOORS)
			{
				automappresentation.AddLayer(new PresentLayer(RendererLayer.Surface, BlendingMode.Mask));
				automappresentation.AddLayer(new PresentLayer(RendererLayer.Things, BlendingMode.Alpha, 1f, false));
			}
			automappresentation.AddLayer(new PresentLayer(RendererLayer.Geometry, BlendingMode.Alpha, 1f, true));
			renderer.SetPresentation(automappresentation);

			EnsureViewMode();
		}

		// The renderer settings that are not exposed to plugins
		private static PropertyInfo FullBrightnessProperty()
		{
			Type t = typeof(General).Assembly.GetType("CodeImp.DoomBuilder.Rendering.Renderer");
			return (t != null) ? t.GetProperty("FullBrightness", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) : null;
		}

		private static PropertyInfo LightEffectsProperty()
		{
			Type t = typeof(General).Assembly.GetType("CodeImp.DoomBuilder.Map.SectorGlow");
			return (t != null) ? t.GetProperty("Enabled", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) : null;
		}

		// Sector light effects (flicker, strobe, glow, ...). The game's automap does not show them.
		private static bool GetLightEffects()
		{
			try
			{
				PropertyInfo pi = LightEffectsProperty();
				if(pi != null) return (bool)pi.GetValue(null, null);
			}
			catch(Exception) { }
			return true;
		}

		private static void SetLightEffects(bool enabled)
		{
			try
			{
				PropertyInfo pi = LightEffectsProperty();
				if(pi != null) pi.SetValue(null, enabled, null);
			}
			catch(Exception) { }
		}

		private static bool GetFullBrightness()
		{
			try
			{
				PropertyInfo pi = FullBrightnessProperty();
				if(pi != null) return (bool)pi.GetValue(null, null);
			}
			catch(Exception) { }
			return false;
		}

		// This makes sure the renderer uses the view mode that belongs to the automap type. Lines only needs the
		// normal view mode. Floors needs the floor textures view mode, with the floor colors (full brightness
		// would replace the floor colors by the sector brightness). Returns true when something was changed.
		private bool EnsureViewMode()
		{
			if((General.Map == null) || changingview) return false;

			bool floors = (menusform.Type == AutomapType.FLOORS);
			ViewMode wanted = floors ? ViewMode.FloorTextures : ViewMode.Normal;
			bool changed = false;

			changingview = true;
			try
			{
				if(floors && GetFullBrightness())
				{
					// This toggles the setting, updates the sectors and redraws
					General.Actions.InvokeAction("builder_togglefullbrightness");
					changed = true;
				}

				if(General.Map.Renderer2D.ViewMode != wanted)
				{
					ApplyViewMode(wanted);
					changed = true;
				}
			}
			finally
			{
				changingview = false;
			}

			return changed;
		}

		// This changes the view mode of the renderer
		private void ApplyViewMode(ViewMode mode)
		{
			switch(mode)
			{
				case ViewMode.FloorTextures: ViewModeFloors(); break;
				case ViewMode.CeilingTextures: ViewModeCeilings(); break;
				case ViewMode.FloorColor: ViewModeFloorColor(); break;
				case ViewMode.CeilingColor: ViewModeCeilingColor(); break;
				case ViewMode.ThingColor: ViewModeThingColor(); break;
				default: ViewModeNormal(); break;
			}
		}

		// This covers the floors that are not drawn on the automap with the background color (and draws the solid secret
		// thing triangles on the same layer). The floors that are covered are the
		// sectors with the "Hide on Automap" flag (unless "Show hidden" is on, Ctrl inverts it like for the
		// lines) and the sectors without a floor texture.
		private void RenderHiddenFloors()
		{
			if(!renderer.StartThings(true)) return;

			bool showhidden = menusform.ShowHiddenLines ^ General.Interface.CtrlState;

			List<FlatVertex> verts = new List<FlatVertex>();
			int color = ColorBackground.ToInt();

			foreach(Sector s in General.Map.Map.Sectors)
			{
				if(s.IsDisposed || (s.Triangles == null)) continue;
				if((showhidden || !s.IsFlagSet(SECTOR_HIDE_FLAG)) && (s.FloorTexture != "-")) continue;

				foreach(Vector2D v in s.Triangles.Vertices)
				{
					FlatVertex fv = new FlatVertex();
					fv.x = v.x;
					fv.y = v.y;
					fv.z = 1.0f;
					fv.c = color;
					verts.Add(fv);
				}
			}

			// Show secrets: solid triangles for the things that count as a secret
			if(menusform.ShowSecretSectors)
			{
				int secretcolor = ColorSecret.ToInt();
				foreach(Thing t in General.Map.Map.Things)
				{
					if(!t.IsFlagSet(THING_SECRET_FLAG)) continue;

					foreach(Vector2D v in ThingTriangle(t))
					{
						FlatVertex fv = new FlatVertex();
						fv.x = v.x;
						fv.y = v.y;
						fv.z = 1.0f;
						fv.c = secretcolor;
						verts.Add(fv);
					}
				}
			}

			if(verts.Count > 0) renderer.RenderGeometry(verts.ToArray(), null, true);

			renderer.Finish();
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

			UpdateSectorFlags();
			UpdateValidLinedefs();

			// Remember the view mode, so it can be restored when leaving this mode
			previousviewmode = General.Map.Renderer2D.ViewMode;
			previousfullbrightness = GetFullBrightness();
			previouslighteffects = GetLightEffects();
			viewmodesaved = true;

			// No sector light effects on the automap (they stay in the other modes)
			SetLightEffects(false);

			// Automap presentation and view mode of the selected type
			ApplyType();

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
			General.Settings.WritePluginSetting("automapmode.type", (int)menusform.Type);

			// Restore the view mode and the full brightness setting
			if(viewmodesaved && (General.Map != null))
			{
				viewmodesaved = false;
				SetLightEffects(previouslighteffects);
				changingview = true;
				try
				{
					if(GetFullBrightness() != previousfullbrightness)
						General.Actions.InvokeAction("builder_togglefullbrightness");

					if(General.Map.Renderer2D.ViewMode != previousviewmode)
						ApplyViewMode(previousviewmode);
				}
				finally
				{
					changingview = false;
				}
			}

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
			// Ignore the redraws that are caused by changing the view mode here, and
			// make sure the view mode is right (it can be changed from the toolbar)
			if(changingview || !viewmodesaved) return;
			EnsureViewMode();

			// Floors: the textured sectors, tinted by the floor colors
			renderer.RedrawSurface();

			// Floors: cover the sectors that are hidden on the automap
			if(menusform.Type == AutomapType.FLOORS) RenderHiddenFloors();

			// Render lines (not in the Floors type, which only shows the highlighted sector)
			if(renderer.StartPlotter(true))
			{
				if(menusform.Type == AutomapType.FLOORS)
				{
					if((highlightedsector != null) && !highlightedsector.IsDisposed)
						PlotSectorOutline(highlightedsector, General.Colors.Highlight);
				}
				else
				{
					foreach(Linedef ld in General.Map.Map.Linedefs)
					{
						if(LinedefIsValid(ld))
							renderer.PlotLine(ld.Start.Position, ld.End.Position, DetermineLinedefColor(ld));
					}

					// Show secrets: lined triangles for the things that count as a secret
					if(menusform.ShowSecretSectors)
					{
						foreach(Thing t in General.Map.Map.Things)
						{
							if(!t.IsFlagSet(THING_SECRET_FLAG)) continue;

							Vector2D[] c = ThingTriangle(t);
							renderer.PlotLine(c[0], c[1], ColorSecret);
							renderer.PlotLine(c[1], c[2], ColorSecret);
							renderer.PlotLine(c[2], c[0], ColorSecret);
						}
					}

					if((highlighted != null) && !highlighted.IsDisposed && LinedefIsValid(highlighted))
					{
						renderer.PlotLine(highlighted.Start.Position, highlighted.End.Position, General.Colors.Highlight);
					}
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
			// Floors type: nothing to toggle with the left mouse button
			if(menusform.Type == AutomapType.FLOORS)
			{
				base.OnSelectEnd();
				return;
			}

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
			// Floors type: toggle the "Hide on Automap" sector flag
			if(menusform.Type == AutomapType.FLOORS)
			{
				if((highlightedsector != null) && !highlightedsector.IsDisposed)
				{
					General.Map.UndoRedo.CreateUndo("Toggle \"Hide on Automap\" sector flag");
					highlightedsector.SetFlag(SECTOR_HIDE_FLAG, !highlightedsector.IsFlagSet(SECTOR_HIDE_FLAG));
					General.Interface.RedrawDisplay();
				}

				base.OnEditEnd();
				return;
			}

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
				// Floors type: highlight the sector under the mouse
				if(menusform.Type == AutomapType.FLOORS)
				{
					Sector sec = null;
					Linedef nl = General.Map.Map.NearestLinedef(mousemappos);
					if(nl != null)
					{
						Sidedef sd = (nl.SideOfLine(mousemappos) > 0) ? nl.Back : nl.Front;
						if(sd != null) sec = sd.Sector;
					}

					if(sec != highlightedsector) HighlightSector(sec);
					return;
				}

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
			if(menusform.Type == AutomapType.FLOORS)
				HighlightSector(null);
			else
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
