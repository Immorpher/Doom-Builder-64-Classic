
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
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.VisualModes;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Data;

#endregion

namespace CodeImp.DoomBuilder.BuilderModes
{
	internal class BaseVisualThing : VisualThing, IVisualEventReceiver
	{
		#region ================== Constants
		
		#endregion
		
		#region ================== Variables

		protected BaseVisualMode mode;
		
		private ThingTypeInfo info;
		private bool isloaded;
		private ImageData sprite;
		private ImageData[] sprites;	// All rotations of the sprite (a single one when it has no rotations)
		private float cageradius2;
		private Vector2D pos2d;
		private Vector3D boxp1;
		private Vector3D boxp2;
		
		// Undo/redo
		private int undoticket;

		// If this is set to true, the thing will be rebuilt after the action is performed.
		protected bool changed;

		// Dragging (the thing is carried along with the camera view)
		private bool thingdragging;
		private int dragarmedpress = -1;	// Number of the edit button press that the drag start data is for
		private float dragstartanglexy;
		private float dragstartanglez;
		private Vector3D dragstartcampos;
		private Vector3D dragstartcamvec;
		private float dragdistance;
		private float draglastanglexy;	// View angle at the previous drag update
		private float dragangle;		// How far the view has turned since the thing was grabbed
		private Vector2D dragcenter;	// Middle of all the dragged things when the drag started
		private List<DragItem> dragitems;

		// Vertex alpha (0-255) for things that are spawned in by a special
		private const int SPAWNED_THING_ALPHA = 0x80;

		// How far the view must turn (radians) with the select button held before a thing is dragged
		private const float DRAG_ANGLE_TOLERANCE = 0.06f;

		#endregion

		#region ================== Types

		// A thing that is being carried around with the camera
		private class DragItem
		{
			public BaseVisualThing visual;
			public Vector2D startxy;	// Map position when the drag started
			public float startz;		// World height of the thing base when the drag started
			public float startangle;	// Angle of the thing when the drag started
		}

		#endregion
		
		#region ================== Properties

		public bool Changed { get { return changed; } set { changed |= value; } }

		// World height of the base of the thing
		internal float WorldBaseZ { get { return boxp1.z; } }
		
		#endregion
		
		#region ================== Constructor / Setup
		
		// Constructor
		public BaseVisualThing(BaseVisualMode mode, Thing t) : base(t)
		{
			this.mode = mode;
			
			Rebuild();
			
			// We have no destructor
			GC.SuppressFinalize(this);
		}
		
		// This builds the thing geometry. Returns false when nothing was created.
		public virtual bool Setup()
		{
			int sectorcolor = 0;
			
			// Must have a width and height!
			if((info.Radius < 0.1f) || (info.Height < 0.1f)) return false;

			// Find the sector in which the thing resides
			Thing.DetermineSector(mode.BlockMap);

			if((sprite != null) && (sprites != null))
			{
				if(Thing.Sector != null)
				{
					// Use sector brightness for color shading
					//byte brightness = (byte)General.Clamp(Thing.Sector.Brightness, 0, 255);
					//sectorcolor = new PixelColor(255, brightness, brightness, brightness);

                    // villsa 9/11/11 (builder64) cameras/triggers are rendered full bright
                    if (Thing.Type == 0 || Thing.Type == 89)
                        sectorcolor = -1;
                    else
                        sectorcolor = Thing.Sector.ThingColor.GetColor();
				}

                // Things with the nightmare flag are shaded green (#00FF00), keeping the sector light level.
                if (Thing.IsFlagSet("4096") && (Thing.Type != 0) && (Thing.Type != 89))
                {
                    sectorcolor = (sectorcolor & 0x0000FF00) | unchecked((int)0xFF000000);
                }

                // Things that are spawned in by a special (flag 32) are drawn semi-transparent, so they
                // can be told apart from things that are there from the start. Camera/trigger icons are
                // rendered fullbright (the vertex color is ignored), so they are left alone.
                RenderPass = RenderPass.Mask;
                if (Thing.IsFlagSet("32") && (Thing.Type != 0) && (Thing.Type != 89))
                {
                    sectorcolor = (sectorcolor & 0x00FFFFFF) | (SPAWNED_THING_ALPHA << 24);
                    RenderPass = RenderPass.Alpha;
                }

                // villsa 9/11/11 (builder64) render camera/trigger icon
                if (Thing.Type == 0 || Thing.Type == 89)
                {
                    if (Thing.Type == 89)
                    {
                        base.Texture = General.Map.Data.ThingTrigger;
                    }
                    else
                    {
                        base.Texture = General.Map.Data.ThingCamera;
                    }

                    // Determine sprite size
                    float radius = Math.Min(info.Radius, info.Height / 2f);
                    float height = Math.Min(info.Radius * 2f, info.Height);

                    // Make vertices
                    WorldVertex[] verts = new WorldVertex[6];
                    verts[0] = new WorldVertex(-radius, 0.0f, 0.0f, sectorcolor, 0.0f, 1.0f);
                    verts[1] = new WorldVertex(-radius, 0.0f, height, sectorcolor, 0.0f, 0.0f);
                    verts[2] = new WorldVertex(+radius, 0.0f, height, sectorcolor, 1.0f, 0.0f);
                    verts[3] = verts[0];
                    verts[4] = verts[2];
                    verts[5] = new WorldVertex(+radius, 0.0f, 0.0f, sectorcolor, 1.0f, 1.0f);
                    SetVertices(verts);
                }
                else
                {
                    // Check if all the textures (all sprite rotations) are loaded
                    isloaded = true;
                    bool alldone = true;
                    foreach (ImageData s in sprites)
                    {
                        if (!s.IsImageLoaded) s.LoadImage();
                        isloaded &= s.IsImageLoaded;
                        alldone &= (s.IsImageLoaded || s.LoadFailed);
                    }

                    // The sprites to make geometry for, and which of them are mirrored
                    ImageData[] usesprites = sprites;
                    SpriteFrameInfo[] frames = info.SpriteFrame;

                    // When some of the rotations cannot be loaded at all, show the first one that
                    // did load without rotations, instead of leaving the thing without a sprite
                    if (!isloaded && alldone)
                    {
                        foreach (ImageData s in sprites)
                        {
                            if (s.IsImageLoaded)
                            {
                                usesprites = new ImageData[] { s };
                                frames = new SpriteFrameInfo[1];
                                isloaded = true;
                                break;
                            }
                        }
                    }

                    if (isloaded)
                    {
                        // Make the geometry for each sprite rotation
                        WorldVertex[][] allverts = new WorldVertex[usesprites.Length][];

                        for (int i = 0; i < usesprites.Length; i++)
                        {
                            ImageData frametex = usesprites[i];
                            bool mirror = ((i < frames.Length) && frames[i].Mirror);
                            float offsetx = 0.0f;
                            float offsety = 0.0f;

                            // Determine sprite size and offset
                            float radius = frametex.ScaledWidth * 0.5f;
                            float height = frametex.ScaledHeight;
                            if (frametex is SpriteImage)
                            {
                                offsetx = (frametex as SpriteImage).OffsetX - radius;
                                offsety = (frametex as SpriteImage).OffsetY - height;
                            }

                            // A mirrored sprite is also mirrored around the thing origin
                            float ul = 0.0f;
                            float ur = 1.0f;
                            if (mirror)
                            {
                                offsetx = -offsetx;
                                ul = 1.0f;
                                ur = 0.0f;
                            }

                            // Make vertices
                            WorldVertex[] verts = new WorldVertex[6];
                            verts[0] = new WorldVertex(-radius + offsetx, 0.0f, 0.0f + offsety, sectorcolor, ul, 1.0f);
                            verts[1] = new WorldVertex(-radius + offsetx, 0.0f, height + offsety, sectorcolor, ul, 0.0f);
                            verts[2] = new WorldVertex(+radius + offsetx, 0.0f, height + offsety, sectorcolor, ur, 0.0f);
                            verts[3] = verts[0];
                            verts[4] = verts[2];
                            verts[5] = new WorldVertex(+radius + offsetx, 0.0f, 0.0f + offsety, sectorcolor, ur, 1.0f);
                            allverts[i] = verts;
                        }

                        SetFrameTextures(usesprites);
                        SetFrameVertices(allverts);
                    }
                    else
                    {
                        base.Texture = General.Map.Data.Hourglass3D;

                        // Determine sprite size
                        float radius = Math.Min(info.Radius, info.Height / 2f);
                        float height = Math.Min(info.Radius * 2f, info.Height);

                        // Make vertices
                        WorldVertex[] verts = new WorldVertex[6];
                        verts[0] = new WorldVertex(-radius, 0.0f, 0.0f, sectorcolor, 0.0f, 1.0f);
                        verts[1] = new WorldVertex(-radius, 0.0f, height, sectorcolor, 0.0f, 0.0f);
                        verts[2] = new WorldVertex(+radius, 0.0f, height, sectorcolor, 1.0f, 0.0f);
                        verts[3] = verts[0];
                        verts[4] = verts[2];
                        verts[5] = new WorldVertex(+radius, 0.0f, 0.0f, sectorcolor, 1.0f, 1.0f);
                        SetVertices(verts);
                    }
                }
			}
			
			// Determine position
			Vector3D pos = Thing.Position;
			if(info.AbsoluteZ)
			{
				// Absolute Z position
				pos.z = Thing.Position.z;
			}
			else if(info.Hangs)
			{
				// Hang from ceiling
				if(Thing.Sector != null) pos.z = Thing.Sector.CeilHeight - info.Height;
				if(Thing.Position.z > 0) pos.z -= Thing.Position.z;
				
				// Check if below floor
				if((Thing.Sector != null) && (pos.z < Thing.Sector.FloorHeight))
				{
					// Put thing on the floor
					pos.z = Thing.Sector.FloorHeight;
				}
			}
			else
			{
				// Stand on floor
				if(Thing.Sector != null) pos.z = Thing.Sector.FloorHeight;
				if(Thing.Position.z > 0) pos.z += Thing.Position.z;
				
				// Check if above ceiling
				if((Thing.Sector != null) && ((pos.z + info.Height) > Thing.Sector.CeilHeight))
				{
					// Put thing against ceiling
					pos.z = Thing.Sector.CeilHeight - info.Height;
				}
			}
			
			// Apply settings
			SetPosition(pos);
			SetCageSize(info.Radius, info.Height);
			SetCageColor(Thing.Color);

			// Keep info for object picking
			cageradius2 = info.Radius * Angle2D.SQRT2;
			cageradius2 = cageradius2 * cageradius2;
			pos2d = pos;
			boxp1 = new Vector3D(pos.x - info.Radius, pos.y - info.Radius, pos.z);
			boxp2 = new Vector3D(pos.x + info.Radius, pos.y + info.Radius, pos.z + info.Height);
			
			// Done
			changed = false;
			return true;
		}
		
		// Disposing
		public override void Dispose()
		{
			if(!IsDisposed)
			{
				if(sprites != null)
				{
					foreach(ImageData s in sprites)
						if(s != null) s.RemoveReference();
					sprites = null;
				}
				sprite = null;
			}
			
			base.Dispose();
		}
		
		#endregion
		
		#region ================== Methods
		
		// This forces to rebuild the whole thing
		public void Rebuild()
		{
			// Find thing information
			info = General.Map.Data.GetThingInfo(Thing.Type);

            // Find the sprite textures (one for each sprite rotation)
            ImageData[] oldsprites = sprites;
            sprite = null;
            sprites = null;
            if(info.Sprite.Length > 0)
            {
                SpriteFrameInfo[] frames = info.SpriteFrame;
                sprites = new ImageData[frames.Length];
                for(int i = 0; i < frames.Length; i++)
                {
                    sprites[i] = General.Map.Data.GetSpriteImage(frames[i].Sprite, info.PalIndex);
                    if(sprites[i] != null) sprites[i].AddReference();
                }

                // The first one is the one used for everything that doesn't care about rotations
                sprite = sprites[0];
            }

            // Release the previous references
            if(oldsprites != null)
            {
                foreach(ImageData s in oldsprites)
                    if(s != null) s.RemoveReference();
            }

            // Setup visual thing
            Setup();
		}
		
		// This updates the thing when needed
		public override void Update()
		{
			if(!isloaded && (sprites != null))
			{
				// Rebuild sprite geometry when all sprites are loaded
				bool allloaded = true;
				bool anyloaded = false;
				foreach(ImageData s in sprites)
				{
					if(s.IsImageLoaded) anyloaded = true;
					else if(!s.LoadFailed) allloaded = false;
				}

				if(allloaded && anyloaded) Setup();
			}
			
			// Let the base update
			base.Update();
		}

		// This performs a fast test in object picking
		public override bool PickFastReject(Vector3D from, Vector3D to, Vector3D dir)
		{
			float distance2 = Line2D.GetDistanceToLineSq(from, to, pos2d, false);
			return (distance2 <= cageradius2);
		}

		// This performs an accurate test for object picking
		public override bool PickAccurate(Vector3D from, Vector3D to, Vector3D dir, ref float u_ray)
		{
			Vector3D delta = to - from;
			float tfar = float.MaxValue;
			float tnear = float.MinValue;
			
			// Ray-Box intersection code
			// See http://www.masm32.com/board/index.php?topic=9941.0
			
			// Check X slab
			if(delta.x == 0.0f)
			{
				if(from.x > boxp2.x || from.x < boxp1.x)
				{
					// Ray is parallel to the planes & outside slab
					return false;
				}
			}
			else
			{
				float tmp = 1.0f / delta.x;
				float t1 = (boxp1.x - from.x) * tmp;
				float t2 = (boxp2.x - from.x) * tmp;
				if(t1 > t2) General.Swap(ref t1, ref t2);
				if(t1 > tnear) tnear = t1;
				if(t2 < tfar) tfar = t2;
				if(tnear > tfar || tfar < 0.0f)
				{
					// Ray missed box or box is behind ray
					return false;
				}
			}
			
			// Check Y slab
			if(delta.y == 0.0f)
			{
				if(from.y > boxp2.y || from.y < boxp1.y)
				{
					// Ray is parallel to the planes & outside slab
					return false;
				}
			}
			else
			{
				float tmp = 1.0f / delta.y;
				float t1 = (boxp1.y - from.y) * tmp;
				float t2 = (boxp2.y - from.y) * tmp;
				if(t1 > t2) General.Swap(ref t1, ref t2);
				if(t1 > tnear) tnear = t1;
				if(t2 < tfar) tfar = t2;
				if(tnear > tfar || tfar < 0.0f)
				{
					// Ray missed box or box is behind ray
					return false;
				}
			}
			
			// Check Z slab
			if(delta.z == 0.0f)
			{
				if(from.z > boxp2.z || from.z < boxp1.z)
				{
					// Ray is parallel to the planes & outside slab
					return false;
				}
			}
			else
			{
				float tmp = 1.0f / delta.z;
				float t1 = (boxp1.z - from.z) * tmp;
				float t2 = (boxp2.z - from.z) * tmp;
				if(t1 > t2) General.Swap(ref t1, ref t2);
				if(t1 > tnear) tnear = t1;
				if(t2 < tfar) tfar = t2;
				if(tnear > tfar || tfar < 0.0f)
				{
					// Ray missed box or box is behind ray
					return false;
				}
			}
			
			// Set interpolation point
			u_ray = (tnear > 0.0f) ? tnear : tfar;
			return true;
		}
		
		#endregion

		#region ================== Events

		// Unused
		public virtual void OnEditBegin() { }
		public virtual void OnChangeTargetBrightness(bool up) { }
		public virtual void OnChangeTextureOffset(int horizontal, int vertical) { }
		public virtual void OnSelectTexture() { }
		public virtual void OnCopyTexture() { }
		public virtual void OnPasteTexture() { }
        public virtual void OnCopyLight() { }   // villsa
        public virtual void OnPasteLight() { }  // villsa
		public virtual void OnCopyTextureOffsets() { }
		public virtual void OnPasteTextureOffsets() { }
		public virtual void OnTextureAlign(bool alignx, bool aligny) { }
		public virtual void OnToggleUpperUnpegged() { }
		public virtual void OnToggleLowerUnpegged() { }
		public virtual void OnResetTextureOffset() { }
		public virtual void OnProcess(double deltatime) { }
		public virtual void OnTextureFloodfill() { }
		public virtual void OnInsert() { }
		public virtual void ApplyTexture(string texture) { }
		public virtual void ApplyUpperUnpegged(bool set) { }
		public virtual void ApplyLowerUnpegged(bool set) { }
		
		// Return texture name
		public virtual string GetTextureName() { return ""; }

		// Delete the thing
		public virtual void OnDelete()
		{
			mode.CreateUndo("Delete thing");
			mode.SetActionResult("Deleted thing.");
			Thing.Dispose();
			mode.ThingsWereDeleted();
		}

		// Select button pressed
		public virtual void OnSelectBegin() { }

		// Select or deselect
		public virtual void OnSelectEnd()
		{
			if(this.selected)
			{
				this.selected = false;
				mode.RemoveSelectedObject(this);
			}
			else
			{
				this.selected = true;
				mode.AddSelectedObject(this);
			}
		}

		// Mouse moves
		public virtual void OnMouseMove(MouseEventArgs e)
		{
			if(thingdragging)
			{
				// Keep carrying the thing with the view
				UpdateDrag();
			}
			else if(General.Actions.CheckActionActive(General.ThisAssembly, "visualedit"))
			{
				if(dragarmedpress != mode.EditPressCount)
				{
					// The edit button was just pressed on this thing: remember where the view was
					dragarmedpress = mode.EditPressCount;
					dragstartanglexy = General.Map.VisualCamera.AngleXY;
					dragstartanglez = General.Map.VisualCamera.AngleZ;
					dragstartcampos = General.Map.VisualCamera.Position;
					dragstartcamvec = General.Map.VisualCamera.Target - General.Map.VisualCamera.Position;

					// Distance from the camera to the middle of the thing. The thing is carried
					// at this distance in front of the camera while dragging.
					Vector3D center = new Vector3D(pos2d.x, pos2d.y, boxp1.z + info.Height * 0.5f);
					dragdistance = Math.Max((center - dragstartcampos).GetLength(), 1.0f);
				}
				else
				{
					// Start dragging when the view is turned far enough with the edit button held
					float deltaxy = Math.Abs(Angle2D.Normalized(General.Map.VisualCamera.AngleXY - dragstartanglexy + Angle2D.PI) - Angle2D.PI);
					float deltaz = Math.Abs(General.Map.VisualCamera.AngleZ - dragstartanglez);
					if((deltaxy + deltaz) > DRAG_ANGLE_TOLERANCE) BeginDrag();
				}
			}
		}

		// This is called when the edit button is released after dragging
		internal void EndDrag()
		{
			thingdragging = false;
			dragitems = null;
		}

		// This starts dragging the thing (and the other selected things, when this one is selected)
		private void BeginDrag()
		{
			mode.PreAction(UndoGroup.None);
			mode.CreateUndo("Drag thing");

			// This thing is always dragged, the others only when this one is part of the selection
			dragitems = new List<DragItem>();
			AddDragItem(this);
			if(this.selected)
			{
				foreach(Thing t in mode.GetSelectedThings())
				{
					if((t != this.Thing) && mode.VisualThingExists(t))
						AddDragItem(mode.GetVisualThing(t) as BaseVisualThing);
				}
			}

			// Let the mode know, so releasing the edit button ends the drag instead of opening the dialog
			mode.SetDraggingThing(this);
			// The things turn as a group around the middle (average position) of all of them
			dragcenter = new Vector2D(0.0f, 0.0f);
			foreach(DragItem item in dragitems)
				dragcenter += item.startxy;
			dragcenter /= (float)dragitems.Count;

			draglastanglexy = dragstartanglexy;
			dragangle = 0.0f;
			thingdragging = true;
			UpdateDrag();
		}

		// This remembers where a thing was when the drag started
		private void AddDragItem(BaseVisualThing vt)
		{
			if(vt == null) return;

			DragItem item = new DragItem();
			item.visual = vt;
			item.startxy = new Vector2D(vt.Thing.Position.x, vt.Thing.Position.y);
			item.startz = vt.boxp1.z;
			item.startangle = vt.Thing.Angle;
			dragitems.Add(item);
		}

		// This moves the dragged things along with where the camera is aiming
		private void UpdateDrag()
		{
			// How far the point in front of the camera has moved since the select button was pressed
			Vector3D campos = General.Map.VisualCamera.Position;
			Vector3D camvec = General.Map.VisualCamera.Target - campos;
			Vector3D delta = (campos + camvec * dragdistance) - (dragstartcampos + dragstartcamvec * dragdistance);

			// Turn the things by as much as the view has turned (keeps the angle between the view and the thing)
			float camangle = General.Map.VisualCamera.AngleXY;
			dragangle += Angle2D.Normalized(camangle - draglastanglexy + Angle2D.PI) - Angle2D.PI;
			draglastanglexy = camangle;

			// Move all the things by that amount
			bool moved = false;
			float sin = (float)Math.Sin(dragangle);
			float cos = (float)Math.Cos(dragangle);
			foreach(DragItem item in dragitems)
			{
				// Turn the position around the middle of the group, then move it along with the view
				Vector2D rel = item.startxy - dragcenter;
				float turnedx = dragcenter.x + (rel.x * cos) - (rel.y * sin);
				float turnedy = dragcenter.y + (rel.x * sin) + (rel.y * cos);
				Vector3D newpos = new Vector3D(turnedx + delta.x, turnedy + delta.y, item.startz + delta.z);
				if(item.visual.MoveToWorldPosition(newpos)) moved = true;
				if(item.visual.RotateTo(item.startangle + dragangle)) moved = true;
			}

			if(moved)
			{
				mode.ShowTargetInfo();
				General.Interface.DisplayStatus(StatusType.Action, "Dragging thing at " + Thing.Position.x + ", " + Thing.Position.y + ", height " + Thing.Position.z + ", angle " + Thing.AngleDoom + ".");
			}
		}

		// This moves the thing so that its base is at the given world position.
		// The height is converted to the height offset the thing stores. Returns true
		// when the thing has moved. A thing is never moved out of the sectors.
		private bool MoveToWorldPosition(Vector3D world)
		{
			Vector2D newxy = new Vector2D((float)Math.Round(world.x), (float)Math.Round(world.y));
			Sector s = FindSectorAt(newxy);
			if(s == null) return false;

			float zoffset = Thing.Position.z;
			if(General.Map.FormatInterface.HasThingHeight)
			{
				if(info.AbsoluteZ)
				{
					zoffset = world.z;
				}
				else
				{
					// Keep the thing between the floor and the ceiling
					float maxoffset = Math.Max(0.0f, (float)(s.CeilHeight - s.FloorHeight) - info.Height);
					if(info.Hangs)
						zoffset = (s.CeilHeight - info.Height) - world.z;
					else
						zoffset = world.z - s.FloorHeight;

					zoffset = Math.Max(0.0f, Math.Min(zoffset, maxoffset));
				}

				zoffset = (float)Math.Round(zoffset);
			}

			// Nothing changed?
			if((newxy.x == Thing.Position.x) && (newxy.y == Thing.Position.y) && (zoffset == Thing.Position.z))
				return false;

			// Move the thing, also in the blockmap
			Vector2D oldxy = Thing.Position;
			mode.BlockMap.RemoveThing(Thing, oldxy);
			Thing.Move(new Vector3D(newxy.x, newxy.y, zoffset));
			mode.BlockMap.AddThing(Thing);

			// Rebuild the visual thing at the new position
			Setup();
			return true;
		}

		// This turns the thing to the given angle (in whole degrees, like the map format stores it).
		// Returns true when the angle has changed.
		private bool RotateTo(float angle)
		{
			float newangle = Angle2D.DoomToReal(Angle2D.RealToDoom(Angle2D.Normalized(angle)));
			if(Angle2D.RealToDoom(newangle) == Thing.AngleDoom) return false;

			Thing.Rotate(newangle);
			return true;
		}

		// This finds the sector at the given position
		private Sector FindSectorAt(Vector2D p)
		{
			VisualBlockMap blockmap = mode.BlockMap;
			foreach(Sector s in blockmap.GetBlock(blockmap.GetBlockCoordinates(p)).Sectors)
			{
				if(s.Intersect(p)) return s;
			}

			return null;
		}
		
		// Copy properties
		public virtual void OnCopyProperties()
		{
			BuilderPlug.Me.CopiedThingProps = new ThingProperties(Thing);
			mode.SetActionResult("Copied thing properties.");
		}
		
		// Paste properties
		public virtual void OnPasteProperties()
		{
			if(BuilderPlug.Me.CopiedThingProps != null)
			{
				mode.CreateUndo("Paste thing properties");
				mode.SetActionResult("Pasted thing properties.");
				BuilderPlug.Me.CopiedThingProps.Apply(Thing);
				Thing.UpdateConfiguration();
				this.Rebuild();
				mode.ShowTargetInfo();
			}
		}
		
		// Edit button released
		public virtual void OnEditEnd()
		{
			// Not using any modifier buttons
			if(!General.Interface.ShiftState && !General.Interface.CtrlState && !General.Interface.AltState)
			{
				if(General.Interface.IsActiveWindow)
				{
					List<Thing> things = mode.GetSelectedThings();
					DialogResult result = General.Interface.ShowEditThings(things);
					if(result == DialogResult.OK)
					{
						foreach(Thing t in things)
						{
							VisualThing vt = mode.GetVisualThing(t);
							if(vt != null)
								(vt as BaseVisualThing).Changed = true;
						}
					}
				}
			}
		}
		
		// Raise/lower thing
		public virtual void OnChangeTargetHeight(int amount)
		{
			if(General.Map.FormatInterface.HasThingHeight)
			{
				if((General.Map.UndoRedo.NextUndo == null) || (General.Map.UndoRedo.NextUndo.TicketID != undoticket))
					undoticket = mode.CreateUndo("Change thing height");

				Thing.Move(Thing.Position + new Vector3D(0.0f, 0.0f, (float)amount));

				mode.SetActionResult("Changed thing height to " + Thing.Position.z + ".");

				this.Changed = true;
			}
		}
		
		#endregion
	}
}
