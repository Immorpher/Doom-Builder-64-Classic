
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
using CodeImp.DoomBuilder.Windows;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.Geometry;
using System.Drawing;
using SlimDX;
using CodeImp.DoomBuilder.Editing;

#endregion

namespace CodeImp.DoomBuilder.VisualModes
{
	/// <summary>
	/// Provides specialized functionality for a visual (3D) Doom Builder editing mode.
	/// </summary>
	public abstract class VisualMode : EditMode
	{
		#region ================== Constants

		private const double MOVE_SPEED_MULTIPLIER = 0.001d;
		
		#endregion

		#region ================== Variables

		// 3D Mode thing
		protected Thing modething;
		
		// Graphics
		protected IRenderer3D renderer;
		private Renderer3D renderer3d;
		
		// Options
		private bool processgeometry;
		private bool processthings;

		// Mouse position captured from the outgoing ClassicMode, used to spawn
		// the camera under the cursor instead of at the player 1 start.
		private static Vector2D capturedmousemappos;
		private static bool capturedmousevalid;
		
		// Input
		private bool keyforward;
		private bool keybackward;
		private bool keyleft;
		private bool keyright;
        private bool keyup;
        private bool keydown;

		// Map
		protected VisualBlockMap blockmap;
		protected Dictionary<Thing, VisualThing> allthings;
		protected Dictionary<Sector, VisualSector> allsectors;
		protected List<VisualBlockEntry> visibleblocks;
		protected List<VisualThing> visiblethings;
		protected Dictionary<Sector, VisualSector> visiblesectors;
		protected List<VisualGeometry> visiblegeometry;
		
		// Doom 64 occlusion culling
		private struct OcclusionLine
		{
			public int start, end;		// Covered columns [start, end)
			public float s1, s2;		// Screen x (-1 .. 1) at the left and right end
			public float w1, w2;		// 1 / depth at the left and right end
			
			// 1 / depth at the center of a column
			public float GetW(int column, int columns)
			{
				float s = ((column + 0.5f) - (columns / 2)) / (columns / 2);
				float u = (s - s1) / (s2 - s1);
				if(u < 0.0f) u = 0.0f; else if(u > 1.0f) u = 1.0f;
				return w1 + (w2 - w1) * u;
			}
		}
		private List<Sidedef> cullingsidedefs = new List<Sidedef>();
		private OcclusionLine[] occlusionprojected = new OcclusionLine[0];
		private float[] occlusionbuffer;
		private bool occlusionenabled = true;
		
		// Lines that were processed by the visibility culling (kept to reuse the collection)
		private Dictionary<Linedef, Linedef> cullinglines = new Dictionary<Linedef, Linedef>(200);
		
		#endregion

		#region ================== Properties

		public bool ProcessGeometry { get { return processgeometry; } set { processgeometry = value; } }
		
		// Doom 64 occlusion culling (No Occlusion linedef flag), only applies to Doom 64 maps
		public bool OcclusionEnabled { get { return occlusionenabled; } set { occlusionenabled = value; } }
		public bool ProcessThings { get { return processthings; } set { processthings = value; } }
		public VisualBlockMap BlockMap { get { return blockmap; } }

		// Rendering
		public IRenderer3D Renderer { get { return renderer; } }
		
		#endregion

		#region ================== Constructor / Disposer

		/// <summary>
		/// Provides specialized functionality for a visual (3D) Doom Builder editing mode.
		/// </summary>
		public VisualMode()
		{
			// Initialize
			this.renderer = General.Map.Renderer3D;
			this.renderer3d = (Renderer3D)General.Map.Renderer3D;
			this.blockmap = new VisualBlockMap();
			this.allsectors = new Dictionary<Sector, VisualSector>(General.Map.Map.Sectors.Count);
			this.allthings = new Dictionary<Thing,VisualThing>(General.Map.Map.Things.Count);
			this.visibleblocks = new List<VisualBlockEntry>();
			this.visiblesectors = new Dictionary<Sector, VisualSector>(50);
			this.visiblegeometry = new List<VisualGeometry>(200);
			this.visiblethings = new List<VisualThing>(100);
			this.processgeometry = true;
			this.processthings = true;

			// Capture the mouse position from the outgoing classic mode
			if(General.Editing.Mode is ClassicMode)
			{
				ClassicMode oldmode = (ClassicMode)General.Editing.Mode;
				if(oldmode.IsMouseInside)
				{
					capturedmousemappos = oldmode.MouseMapPos;
					capturedmousevalid = true;
				}
				else
				{
					// Cursor was over a panel/toolbar/outside the display.
					capturedmousevalid = false;
				}
			}
			else
			{
				// Coming from something other than a classic mode.
				capturedmousevalid = false;
			}
		}
		
		// Disposer
		public override void Dispose()
		{
			// Not already disposed?
			if(!isdisposed)
			{
				// Clean up
				foreach(KeyValuePair<Sector, VisualSector> s in allsectors) s.Value.Dispose();
				blockmap.Dispose();
				visiblesectors = null;
				visiblegeometry = null;
				visibleblocks = null;
				visiblethings = null;
				allsectors = null;
				allthings = null;
				blockmap = null;
				
				// Done
				base.Dispose();
			}
		}

		#endregion

		#region ================== Start / Stop

		// Mode is engaged
		public override void OnEngage()
		{
			base.OnEngage();

			// Spawn the camera under the mouse cursor position
			if(capturedmousevalid)
				PositionCameraAtMapPosition(capturedmousemappos);

			// fresh classic-mode cursor position doesn't silently reuse a stale one.
			capturedmousevalid = false;
			
			// Update the used textures
			General.Map.Data.UpdateUsedTextures();
			
			// Fill the blockmap
			FillBlockMap();
			
			// Start special input mode
			General.Interface.EnableProcessing();
			General.Interface.StartExclusiveMouseInput();
		}

		// Check position
		private bool PositionCameraAtMapPosition(Vector2D pos)
		{
			Linedef nld = General.Map.Map.NearestLinedef(pos);
			if(nld == null) return false;

			Sector s;
			if(nld.SideOfLine(pos) < 0)
				s = (nld.Front != null) ? nld.Front.Sector : null;
			else
				s = (nld.Back != null) ? nld.Back.Sector : null;

			// One-sided line?
			if(s == null)
				s = (nld.Front != null) ? nld.Front.Sector : ((nld.Back != null) ? nld.Back.Sector : null);

			if(s == null) return false;

			float floor = s.FloorHeight;
			float ceiling = s.CeilHeight;
			float z = floor + VisualCamera.THING_Z_OFFSET;

			// Don't punch through a low ceiling. Guard against broken sectors
			// where the ceiling is at or below the floor.
			float maxz = ceiling - 4f;
			if(maxz > floor && z > maxz) z = maxz;

			General.Map.VisualCamera.Position = new Vector3D(pos.x, pos.y, z);
			General.Map.VisualCamera.Sector = s;
			return true;
		}

		// Mode is disengaged
		public override void OnDisengage()
		{
			base.OnDisengage();
			
			// Dispose
			foreach(KeyValuePair<Sector, VisualSector> vs in allsectors)
				vs.Value.Dispose();

			// Dispose
			foreach(KeyValuePair<Thing, VisualThing> vt in allthings)
				vt.Value.Dispose();	
			
			// Do not leave the sector on the camera
			General.Map.VisualCamera.Sector = null;
			
			// Stop special input mode
			General.Interface.DisableProcessing();
			General.Interface.StopExclusiveMouseInput();
		}

		#endregion

		#region ================== Events

		public override bool OnUndoBegin()
		{
			renderer.SetCrosshairBusy(true);
			General.Interface.RedrawDisplay();
			return base.OnUndoBegin();
		}

		public override void OnUndoEnd()
		{
			base.OnUndoEnd();
			ResourcesReloadedPartial();
			renderer.SetCrosshairBusy(false);
		}

		public override bool OnRedoBegin()
		{
			renderer.SetCrosshairBusy(true);
			General.Interface.RedrawDisplay();
			return base.OnRedoBegin();
		}

		public override void OnRedoEnd()
		{
			base.OnRedoEnd();
			ResourcesReloadedPartial();
			renderer.SetCrosshairBusy(false);
		}

		public override void OnReloadResources()
		{
			base.OnReloadResources();
			ResourcesReloaded();
		}
		
		#endregion
		
		#region ================== Input

		// Mouse input
		public override void OnMouseInput(Vector2D delta)
		{
			base.OnMouseInput(delta);
			General.Map.VisualCamera.ProcessMouseInput(delta);
		}

		[BeginAction("moveforward", BaseAction = true)]
		public virtual void BeginMoveForward()
		{
			keyforward = true;
		}

		[EndAction("moveforward", BaseAction = true)]
		public virtual void EndMoveForward()
		{
			keyforward = false;
		}

		[BeginAction("movebackward", BaseAction = true)]
		public virtual void BeginMoveBackward()
		{
			keybackward = true;
		}

		[EndAction("movebackward", BaseAction = true)]
		public virtual void EndMoveBackward()
		{
			keybackward = false;
		}

		[BeginAction("moveleft", BaseAction = true)]
		public virtual void BeginMoveLeft()
		{
			keyleft = true;
		}

		[EndAction("moveleft", BaseAction = true)]
		public virtual void EndMoveLeft()
		{
			keyleft = false;
		}

		[BeginAction("moveright", BaseAction = true)]
		public virtual void BeginMoveRight()
		{
			keyright = true;
		}

		[EndAction("moveright", BaseAction = true)]
		public virtual void EndMoveRight()
		{
			keyright = false;
		}

        [BeginAction("moveup", BaseAction = true)]
        public virtual void BeginMoveUp()
        {
            keyup = true;
        }

        [EndAction("moveup", BaseAction = true)]
        public virtual void EndMoveUp()
        {
            keyup = false;
        }

        [BeginAction("movedown", BaseAction = true)]
        public virtual void BeginMoveDown()
        {
            keydown = true;
        }

        [EndAction("movedown", BaseAction = true)]
        public virtual void EndMoveDown()
        {
            keydown = false;
        }
		
		#endregion

		#region ================== Visibility Culling
		
		// This preforms visibility culling
		protected void DoCulling()
		{
			Dictionary<Linedef, Linedef> visiblelines = cullinglines;
			Vector2D campos2d = (Vector2D)General.Map.VisualCamera.Position;
			float viewdist = General.Settings.ViewDistance;
			
			// Empty the collections (they are reused for every frame)
			cullingsidedefs.Clear();
			visiblelines.Clear();
			visiblesectors.Clear();
			visiblegeometry.Clear();
			visiblethings.Clear();

			// Get the blocks within view range
			visibleblocks = blockmap.GetFrustumRange(renderer.Frustum2D, visibleblocks);
			
			// Fill collections with geometry and things
			foreach(VisualBlockEntry block in visibleblocks)
			{
				if(processgeometry)
				{
					// Lines
					foreach(Linedef ld in block.Lines)
					{
						// Line not already processed?
						if(!visiblelines.ContainsKey(ld))
						{
							// Add line if not added yet
							visiblelines.Add(ld, ld);

							// Which side of the line is the camera on?
							if(ld.SideOfLine(campos2d) < 0)
							{
								// Do front of line
								if(ld.Front != null) cullingsidedefs.Add(ld.Front);
							}
							else
							{
								// Do back of line
								if(ld.Back != null) cullingsidedefs.Add(ld.Back);
							}
						}
					}
				}

				if(processthings)
				{
					// Things
					foreach(Thing t in block.Things)
					{
						VisualThing vt;

						// Not filtered out?
						if(General.Map.ThingsFilter.IsThingVisible(t))
						{
							if(allthings.ContainsKey(t))
							{
								vt = allthings[t];
							}
							else
							{
								// Create new visual thing
								vt = CreateVisualThing(t);
								if(vt != null) allthings.Add(t, vt);
							}

							if(vt != null)
							{
								visiblethings.Add(vt);
							}
						}
					}
				}
			}

			if(processgeometry)
			{
				// Doom 64 occlusion: drop the sidedefs that the engine would have culled
				if(General.Map.FormatInterface.InDoom64Mode && occlusionenabled)
					ApplyDoom64Occlusion(cullingsidedefs);

				// Create the visual sectors and geometry for the remaining sidedefs
				foreach(Sidedef sd in cullingsidedefs)
					ProcessSidedefCulling(sd);

				// Find camera sector
				Linedef nld = MapSet.NearestLinedef(visiblelines.Values, campos2d);
				if(nld != null)
				{
					General.Map.VisualCamera.Sector = GetCameraSectorFromLinedef(nld);
				}
				else
				{
					// Exceptional case: no lines found in any nearby blocks!
					// This could happen in the middle of an extremely large sector and in this case
					// the above code will not have found any sectors/sidedefs for rendering.
					// Here we handle this special case with brute-force. Let's find the sector
					// the camera is in by searching the entire map and render that sector only.
					nld = General.Map.Map.NearestLinedef(campos2d);
					if(nld != null)
					{
						General.Map.VisualCamera.Sector = GetCameraSectorFromLinedef(nld);
						if(General.Map.VisualCamera.Sector != null)
						{
							foreach(Sidedef sd in General.Map.VisualCamera.Sector.Sidedefs)
							{
								float side = sd.Line.SideOfLine(campos2d);
								if(((side < 0) && sd.IsFront) ||
								   ((side > 0) && !sd.IsFront))
									ProcessSidedefCulling(sd);
							}
						}
						else
						{
							// Too far away from the map to see anything
							General.Map.VisualCamera.Sector = null;
						}
					}
					else
					{
						// Map is empty
						General.Map.VisualCamera.Sector = null;
					}
				}
			}
		}

		// 
		// Doom 64 occlusion culling. The Doom 64 engine keeps a buffer of 320 screen columns. Walls that are
		// solid (one-sided, or two-sided with no opening) mark the columns they cover as occluded, and
		// everything behind them in those columns is not rendered. A line with the "No Occlusion" (1024) or the
		// "Render Mid-Texture" (512) flag never occludes (see R_AddLine in r_phase1.c), which is what makes
		// the illusion of bridges possible.
		// 
		// The engine walks the BSP front to back. We have no BSP, so instead of relying on order, we first
		// build the nearest occluder depth per column and then keep every sidedef that is not strictly behind
		// it in all of its columns. This only removes sidedefs that are certainly hidden; sidedefs that
		// cannot be projected (behind the camera, edge-on) are kept.
		// 
		private void ApplyDoom64Occlusion(List<Sidedef> sidedefs)
		{
			const int COLUMNS = 320;
			const float NEARCLIP = 8.0f;		// R_AddLine clips at y = 8
			const float TOLERANCE = 1.002f;		// Slack so a wall does not occlude itself (or coplanar walls)
			
			int count = sidedefs.Count;
			if(count == 0) return;
			
			Vector3D cam = General.Map.VisualCamera.Position;
			float sin = (float)Math.Sin(General.Map.VisualCamera.AngleXY);
			float cos = (float)Math.Cos(General.Map.VisualCamera.AngleXY);
			float tanhalf = (float)Math.Tan(Angle2D.DegToRad((float)General.Settings.VisualFOV) * 0.5f);
			
			// Reuse buffers
			if(occlusionbuffer == null) occlusionbuffer = new float[COLUMNS];
			Array.Clear(occlusionbuffer, 0, COLUMNS);
			if(occlusionprojected.Length < count) occlusionprojected = new OcclusionLine[count + count / 2];
			OcclusionLine[] lines = occlusionprojected;
			
			// Project all sidedefs to screen columns. We keep 1/depth (w) because it is linear in screen space.
			for(int i = 0; i < count; i++)
			{
				Sidedef sd = sidedefs[i];
				OcclusionLine ol = new OcclusionLine();
				
				Vector2D p1 = sd.Line.Start.Position;
				Vector2D p2 = sd.Line.End.Position;
				float dx1 = p1.x - cam.x, dy1 = p1.y - cam.y;
				float dx2 = p2.x - cam.x, dy2 = p2.y - cam.y;
				float x1 = dx1 * sin - dy1 * cos, y1 = dx1 * cos + dy1 * sin;
				float x2 = dx2 * sin - dy2 * cos, y2 = dx2 * cos + dy2 * sin;
				
				if((y1 >= NEARCLIP) || (y2 >= NEARCLIP))
				{
					// Clip against the near line
					if(y1 < NEARCLIP)
					{
						float u = (NEARCLIP - y1) / (y2 - y1);
						x1 += (x2 - x1) * u; y1 = NEARCLIP;
					}
					else if(y2 < NEARCLIP)
					{
						float u = (NEARCLIP - y2) / (y1 - y2);
						x2 += (x1 - x2) * u; y2 = NEARCLIP;
					}
					
					// Screen x, relative to half the view width
					float s1 = x1 / y1 / tanhalf;
					float s2 = x2 / y2 / tanhalf;
					float w1 = 1.0f / y1, w2 = 1.0f / y2;
					if(s1 > s2)
					{
						float t = s1; s1 = s2; s2 = t;
						t = w1; w1 = w2; w2 = t;
					}
					
					// Same truncation as the engine
					int start = (int)Math.Floor(s1 * (COLUMNS / 2) + (COLUMNS / 2));
					int end = (int)Math.Floor(s2 * (COLUMNS / 2) + (COLUMNS / 2));
					if(start < 0) start = 0;
					if(end > COLUMNS) end = COLUMNS;
					if((start < end) && ((s2 - s1) > 0.00001f))
					{
						ol.start = start; ol.end = end;
						ol.s1 = s1; ol.s2 = s2;
						ol.w1 = w1; ol.w2 = w2;
					}
				}
				
				lines[i] = ol;
			}
			
			// Build the occluder buffer
			for(int i = 0; i < count; i++)
			{
				OcclusionLine ol = lines[i];
				if(ol.end <= ol.start) continue;
				if(!IsDoom64Occluder(sidedefs[i])) continue;
				
				for(int c = ol.start; c < ol.end; c++)
				{
					float w = ol.GetW(c, COLUMNS);
					if(w > occlusionbuffer[c]) occlusionbuffer[c] = w;
				}
			}
			
			// Remove sidedefs that are completely behind occluders
			int keep = 0;
			for(int i = 0; i < count; i++)
			{
				OcclusionLine ol = lines[i];
				bool visible = (ol.end <= ol.start);
				for(int c = ol.start; (c < ol.end) && !visible; c++)
				{
					if((ol.GetW(c, COLUMNS) * TOLERANCE) >= occlusionbuffer[c])
						visible = true;
				}
				
				if(visible) sidedefs[keep++] = sidedefs[i];
			}
			sidedefs.RemoveRange(keep, count - keep);
		}
		
		// This checks if a camera-facing sidedef marks the columns it covers as occluded (R_AddLine)
		private static bool IsDoom64Occluder(Sidedef sd)
		{
			Linedef ld = sd.Line;
			if(ld.IsFlagSet("1024") || ld.IsFlagSet("512")) return false;
			
			Sidedef other = sd.Other;
			if((other == null) || (other.Sector == null)) return true;
			
			Sector front = sd.Sector;
			Sector back = other.Sector;
			return (back.CeilHeight <= front.FloorHeight) ||
				   (back.FloorHeight >= front.CeilHeight) ||
				   (back.FloorHeight == back.CeilHeight);
		}
		
		// This finds and adds visible sectors
		private void ProcessSidedefCulling(Sidedef sd)
		{
			VisualSector vs;
			
			// Find the visualsector and make it if needed
			if(allsectors.ContainsKey(sd.Sector))
			{
				// Take existing visualsector
				vs = allsectors[sd.Sector];
			}
			else
			{
				// Make new visualsector
				vs = CreateVisualSector(sd.Sector);
				if(vs != null) allsectors.Add(sd.Sector, vs);
			}
			
			if(vs != null)
			{
				// Add to visible sectors if not added yet
				if(!visiblesectors.ContainsKey(sd.Sector))
				{
					visiblesectors.Add(sd.Sector, vs);
					visiblegeometry.AddRange(vs.FixedGeometry);
				}
				
				// Add sidedef geometry
				visiblegeometry.AddRange(vs.GetSidedefGeometry(sd));
			}
		}

		// This returns the camera sector from linedef
		private Sector GetCameraSectorFromLinedef(Linedef ld)
		{
			if(ld.SideOfLine(General.Map.VisualCamera.Position) < 0)
			{
				if(ld.Front != null)
					return ld.Front.Sector;
				else
					return null;
			}
			else
			{
				if(ld.Back != null)
					return ld.Back.Sector;
				else
					return null;
			}
		}
		
		#endregion

		#region ================== Object Picking

		// This picks an object from the scene
		public VisualPickResult PickObject(Vector3D from, Vector3D to)
		{
			VisualPickResult result = new VisualPickResult();
			Line2D ray2d = new Line2D(from, to);
			Vector3D delta = to - from;
			
			// Setup no result
			result.picked = null;
			result.hitpos = new Vector3D();
			result.u_ray = 1.0f;
			
			// Find all blocks we are intersecting
			List<VisualBlockEntry> blocks = blockmap.GetLineBlocks(from, to);
			
			// Make collections
			Dictionary<Linedef, Linedef> lines = new Dictionary<Linedef, Linedef>(blocks.Count * 10);
			Dictionary<Sector, VisualSector> sectors = new Dictionary<Sector, VisualSector>(blocks.Count * 10);
			List<IVisualPickable> pickables = new List<IVisualPickable>(blocks.Count * 10);
			
			// Add geometry from the camera sector
			if((General.Map.VisualCamera.Sector != null) && allsectors.ContainsKey(General.Map.VisualCamera.Sector))
			{
				VisualSector vs = allsectors[General.Map.VisualCamera.Sector];
				sectors.Add(General.Map.VisualCamera.Sector, vs);
				foreach(VisualGeometry g in vs.FixedGeometry) pickables.Add(g);
			}
			
			// Go for all lines to see which ones we intersect
			// We will collect geometry from the sectors and sidedefs
			foreach(VisualBlockEntry b in blocks)
			{
				foreach(Linedef ld in b.Lines)
				{
					// Make sure we don't test a line twice
					if(!lines.ContainsKey(ld))
					{
						lines.Add(ld, ld);
						
						// Intersecting?
						float u;
						if(ld.Line.GetIntersection(ray2d, out u))
						{
							// Check on which side we are
							float side = ld.SideOfLine(ray2d.v1);
							
							// Calculate intersection point
							Vector3D intersect = from + delta * u;
							
							// We must add the sectors of both sides of the line
							// If we wouldn't, then aiming at a sector that is just within range
							// could result in an incorrect hit (because the far line of the
							// sector may not be included in this loop)
							if(ld.Front != null)
							{
								// Find the visualsector
								if(allsectors.ContainsKey(ld.Front.Sector))
								{
									VisualSector vs = allsectors[ld.Front.Sector];
									
									// Add sector if not already added
									if(!sectors.ContainsKey(ld.Front.Sector))
									{
										sectors.Add(ld.Front.Sector, vs);
										foreach(VisualGeometry g in vs.FixedGeometry)
										{
											// Must have content
											if(g.Triangles > 0)
												pickables.Add(g);
										}
									}
									
									// Add sidedef if on the front side
									if(side < 0.0f)
									{
										List<VisualGeometry> sidedefgeo = vs.GetSidedefGeometry(ld.Front);
										foreach(VisualGeometry g in sidedefgeo)
										{
											// Must have content
											if(g.Triangles > 0)
											{
												g.SetPickResults(intersect, u);
												pickables.Add(g);
											}
										}
									}
								}
							}
							
							// Add back side also
							if(ld.Back != null)
							{
								// Find the visualsector
								if(allsectors.ContainsKey(ld.Back.Sector))
								{
									VisualSector vs = allsectors[ld.Back.Sector];

									// Add sector if not already added
									if(!sectors.ContainsKey(ld.Back.Sector))
									{
										sectors.Add(ld.Back.Sector, vs);
										foreach(VisualGeometry g in vs.FixedGeometry)
										{
											// Must have content
											if(g.Triangles > 0)
												pickables.Add(g);
										}
									}

									// Add sidedef if on the front side
									if(side > 0.0f)
									{
										List<VisualGeometry> sidedefgeo = vs.GetSidedefGeometry(ld.Back);
										foreach(VisualGeometry g in sidedefgeo)
										{
											// Must have content
											if(g.Triangles > 0)
											{
												g.SetPickResults(intersect, u);
												pickables.Add(g);
											}
										}
									}
								}
							}
						}
					}
				}
			}
			
			// Add all the visible things
			foreach(VisualThing vt in visiblethings)
				pickables.Add(vt);
			
			// Now we have a list of potential geometry that lies along the trace line.
			// We still don't know what geometry actually hits, but we ruled out that which doesn't get even close.
			// This is still too much for accurate intersection testing, so we do a fast reject pass first.
			Vector3D direction = to - from;
			direction = direction.GetNormal();
			List<IVisualPickable> potentialpicks = new List<IVisualPickable>(pickables.Count);
			foreach(IVisualPickable p in pickables)
			{
				if(p.PickFastReject(from, to, direction)) potentialpicks.Add(p);
			}
			
			// Now we do an accurate intersection test for all resulting geometry
			// We keep only the closest hit!
			foreach(IVisualPickable p in potentialpicks)
			{
				float u = result.u_ray;
				if(p.PickAccurate(from, to, direction, ref u))
				{
					// Closer than previous find?
					if((u > 0.0f) && (u < result.u_ray))
					{
						result.u_ray = u;
						result.picked = p;
					}
				}
			}
			
			// Setup final result
			result.hitpos = from + to * result.u_ray;
			
			// Done
			return result;
		}
		
		#endregion

		#region ================== Processing
		
		/// <summary>
		/// This disposes all resources. Needed geometry will be rebuild automatically.
		/// </summary>
		protected virtual void ResourcesReloaded()
		{
			// Dispose
			foreach(KeyValuePair<Sector, VisualSector> vs in allsectors)
				vs.Value.Dispose();
				
			foreach(KeyValuePair<Thing, VisualThing> vt in allthings)
				vt.Value.Dispose();
				
			// Clear collections
			allsectors.Clear();
			allthings.Clear();
			visiblesectors.Clear();
			visibleblocks.Clear();
			visiblegeometry.Clear();
			visiblethings.Clear();
			
			// Make new blockmap
			FillBlockMap();

			// Visibility culling (this re-creates the needed resources)
			DoCulling();
		}

		/// <summary>
		/// This disposes orphaned resources and resources on changed geometry.
		/// This usually happens when geometry is changed by undo, redo, cut or paste actions
		/// and uses the marks to check what needs to be reloaded.
		/// </summary>
		protected virtual void ResourcesReloadedPartial()
		{
			Dictionary<Sector, VisualSector> newsectors = new Dictionary<Sector,VisualSector>(allsectors.Count);
			
			// Neighbour sectors must be updated as well
			foreach(Sector s in General.Map.Map.Sectors)
			{
				if(s.Marked)
				{
					foreach(Sidedef sd in s.Sidedefs)
						if(sd.Other != null) sd.Other.Marked = true;
				}
			}
			
			// Go for all sidedefs to mark sectors that need updating
			foreach(Sidedef sd in General.Map.Map.Sidedefs)
				if(sd.Marked) sd.Sector.Marked = true;
			
			// Go for all vertices to mark linedefs that need updating
			foreach(Vertex v in General.Map.Map.Vertices)
			{
				if(v.Marked)
				{
					foreach(Linedef ld in v.Linedefs)
						ld.Marked = true;
				}
			}
			
			// Go for all linedefs to mark sectors that need updating
			foreach(Linedef ld in General.Map.Map.Linedefs)
			{
				if(ld.Marked)
				{
					if(ld.Front != null) ld.Front.Sector.Marked = true;
					if(ld.Back != null) ld.Back.Sector.Marked = true;
				}
			}
			
			// Dispose if source was disposed or marked
			foreach(KeyValuePair<Sector, VisualSector> vs in allsectors)
			{
				if(vs.Key.IsDisposed || vs.Key.Marked)
					vs.Value.Dispose();
				else
					newsectors.Add(vs.Key, vs.Value);
			}
			
			// Things depend on the sector they are in and because we can't
			// easily determine which ones changed, we dispose all things
			foreach(KeyValuePair<Thing, VisualThing> vt in allthings)
				vt.Value.Dispose();
			
			// Apply new lists
			allsectors = newsectors;
			allthings = new Dictionary<Thing, VisualThing>(allthings.Count);
			
			// Clear visibility collections
			visiblesectors.Clear();
			visibleblocks.Clear();
			visiblegeometry.Clear();
			visiblethings.Clear();
			
			// Make new blockmap
			FillBlockMap();
			
			// Visibility culling (this re-creates the needed resources)
			DoCulling();
		}
		
		/// <summary>
		/// Implement this to create an instance of your VisualSector implementation.
		/// </summary>
		protected abstract VisualSector CreateVisualSector(Sector s);

		/// <summary>
		/// Implement this to create an instance of your VisualThing implementation.
		/// </summary>
		protected abstract VisualThing CreateVisualThing(Thing t);
		
		/// <summary>
		/// This returns the VisualSector for the given Sector.
		/// </summary>
		public VisualSector GetVisualSector(Sector s) { return allsectors[s]; }
		
		/// <summary>
		/// This returns the VisualThing for the given Thing.
		/// </summary>
		public VisualThing GetVisualThing(Thing t) { return allthings[t]; }

		/// <summary>
		/// Returns True when a VisualSector has been created for the specified Sector.
		/// </summary>
		public bool VisualSectorExists(Sector s) { return allsectors.ContainsKey(s); }

		/// <summary>
		/// Returns True when a VisualThing has been created for the specified Thing.
		/// </summary>
		public bool VisualThingExists(Thing t) { return allthings.ContainsKey(t); }

		/// <summary>
		/// This is called when the blockmap needs to be refilled, because it was invalidated.
		/// This usually happens when geometry is changed by undo, redo, cut or paste actions.
		/// Lines and Things are added to the block map by the base implementation.
		/// </summary>
		protected virtual void FillBlockMap()
		{
			if(blockmap != null) blockmap.Dispose();
			blockmap = new VisualBlockMap();
			
			blockmap.AddLinedefsSet(General.Map.Map.Linedefs);
			blockmap.AddThingsSet(General.Map.Map.Things);
			blockmap.AddSectorsSet(General.Map.Map.Sectors);
		}
		
		/// <summary>
		/// While this mode is active, this is called continuously to process whatever needs processing.
		/// </summary>
		public override void OnProcess(double deltatime)
		{
			double multiplier;
			
			base.OnProcess(deltatime);
			
			// Camera vectors
			Vector3D camvec = Vector3D.FromAngleXYZ(General.Map.VisualCamera.AngleXY, General.Map.VisualCamera.AngleZ);
			Vector3D camvecstrafe = Vector3D.FromAngleXY(General.Map.VisualCamera.AngleXY + Angle2D.PIHALF);
			Vector3D cammovemul = General.Map.VisualCamera.MoveMultiplier;
			Vector3D camdeltapos = new Vector3D();
            Vector3D upvec = new Vector3D(0.0f, 0.0f, 1.0f);

			// Move the camera
			if(General.Interface.ShiftState) multiplier = MOVE_SPEED_MULTIPLIER * 2.0f; else multiplier = MOVE_SPEED_MULTIPLIER;
			if(keyforward) camdeltapos += camvec * cammovemul * (float)((double)General.Settings.MoveSpeed * multiplier * deltatime);
			if(keybackward) camdeltapos -= camvec * cammovemul * (float)((double)General.Settings.MoveSpeed * multiplier * deltatime);
			if(keyleft) camdeltapos -= camvecstrafe * cammovemul * (float)((double)General.Settings.MoveSpeed * multiplier * deltatime);
			if(keyright) camdeltapos += camvecstrafe * cammovemul * (float)((double)General.Settings.MoveSpeed * multiplier * deltatime);
            if(keyup) camdeltapos += upvec * cammovemul * (float)((double)General.Settings.MoveSpeed * multiplier * deltatime);
            if(keydown) camdeltapos += -upvec * cammovemul * (float)((double)General.Settings.MoveSpeed * multiplier * deltatime);
			
			// Move the camera
			General.Map.VisualCamera.ProcessMovement(camdeltapos);
			
			// Apply new camera matrices
			renderer.PositionAndLookAt(General.Map.VisualCamera.Position, General.Map.VisualCamera.Target);
			
			// Visibility culling
			DoCulling();
			
			// Update labels in main window
			General.MainWindow.UpdateCoordinates((Vector2D)General.Map.VisualCamera.Position);
			
			// Now redraw
			General.Interface.RedrawDisplay();
		}
		
		#endregion

		#region ================== Actions

		#endregion
	}
}
