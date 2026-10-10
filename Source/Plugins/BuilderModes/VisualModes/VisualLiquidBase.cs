#region ================== Namespaces

using System;
using System.Collections.Generic;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.VisualModes;

#endregion

namespace CodeImp.DoomBuilder.BuilderModes
{
	// 
	// The opaque bottom layer of a Doom 64 liquid floor (sector flag Liquid Effect). The game draws the flat after the
	// floor flat (floorpic + 1) here, and the floor flat itself over it as a translucent layer. This layer only exists
	// to show the effect in 3D mode: it can not be picked or selected, edits go to the floor.
	// 
	internal sealed class VisualLiquidBase : BaseVisualGeometrySector, ILiquidLayer
	{
		#region ================== Variables

		private bool active;

		#endregion

		#region ================== Properties

		// True when the sector is a liquid floor and the bottom flat was found (valid after Setup)
		public bool IsActive { get { return active; } }

		public int LiquidLayer { get { return 1; } }

		#endregion

		#region ================== Constructor / Setup

		// Constructor
		public VisualLiquidBase(BaseVisualMode mode, VisualSector vs) : base(mode, vs)
		{
			GC.SuppressFinalize(this);
		}

		// This builds the geometry. Returns false when there is no liquid layer.
		public override bool Setup()
		{
			Sector s = base.Sector.Sector;
			active = false;

			if(!TextureScroll.IsLiquid(s)) return false;
			string name = TextureScroll.GetLiquidBaseName(s);
			if(name == null) return false;

			long longname = Lump.MakeLongName(name);
			base.Texture = General.Map.Data.GetFlatImage(longname);
			if(base.Texture == null)
			{
				base.Texture = General.Map.Data.MissingTexture3D;
				setuponloadedtexture = longname;
			}
			else if(!base.Texture.IsImageLoaded)
			{
				setuponloadedtexture = longname;
			}

			int brightness = s.FloorColor.GetColor();
			base.RenderPass = RenderPass.Solid;

			WorldVertex[] verts = new WorldVertex[s.Triangles.Vertices.Count];
			for(int i = 0; i < verts.Length; i++)
			{
				verts[i].c = brightness;
				verts[i].u = s.Triangles.Vertices[i].x / 64;
				verts[i].v = -s.Triangles.Vertices[i].y / 64;
				verts[i].x = s.Triangles.Vertices[i].x;
				verts[i].y = s.Triangles.Vertices[i].y;
				verts[i].z = (float)s.FloorHeight;
			}

			base.SetVertices(verts);
			active = (verts.Length > 0);
			return active;
		}

		#endregion

		#region ================== Methods

		// Nothing to change here, the floor does that
		protected override void ChangeHeight(int amount) { }

		// This layer can not be picked
		public override bool PickFastReject(Vector3D from, Vector3D to, Vector3D dir)
		{
			return false;
		}

		public override bool PickAccurate(Vector3D from, Vector3D to, Vector3D dir, ref float u_ray)
		{
			return false;
		}

		// Return texture name
		public override string GetTextureName()
		{
			return TextureScroll.GetLiquidBaseName(this.Sector.Sector) ?? "-";
		}

		#endregion
	}
}
