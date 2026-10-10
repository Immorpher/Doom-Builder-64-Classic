
#region ================== Namespaces

using System;
using System.Collections.Generic;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.VisualModes;

#endregion

namespace CodeImp.DoomBuilder.Map
{
	// 
	// Calculates the texture scrolling of Doom 64 for the 3D mode, like the game does in P_UpdateSpecials (p_spec.c):
	//  - Lines with the Scroll Right/Left/Up/Down flags scroll the textures of their FRONT side by 1 pixel per tic.
	//  - Sectors with Floor Scroll / Ceiling Scroll and a direction flag scroll their flat by 1 unit per tic
	//    (3 units per tic with the Fast Scrolling flag).
	// The game runs at 30 tics per second and moves the textures in whole steps, so this does the same.
	// The editor does not change the map; the result is only used as a texture coordinate offset when rendering.
	// 
	// 
	// Implemented by the visual geometry that makes up a Doom 64 liquid floor.
	// 
	public interface ILiquidLayer
	{
		// 0 = not a liquid layer, 1 = opaque bottom layer, 2 = translucent top layer.
		int LiquidLayer { get; }
	}

	public static class TextureScroll
	{
		#region ================== Constants

		// Game tick rate (the game runs at a maximum of 30 fps)
		private const int TICS_PER_SECOND = 30;

		// Wall offsets wrap at 127 pixels (SCROLLLIMIT), flats are 64x64
		private const int WALL_WRAP = 128;
		private const int FLAT_WRAP = 64;

		// Line flags
		private const string FLAG_LINE_SCROLL_RIGHT = "131072";
		private const string FLAG_LINE_SCROLL_LEFT = "262144";
		private const string FLAG_LINE_SCROLL_UP = "524288";
		private const string FLAG_LINE_SCROLL_DOWN = "1048576";

		// Sector flags
		private const string FLAG_SECTOR_LIQUID = "4";
		private const string FLAG_SECTOR_SCROLL_FAST = "16";
		private const string FLAG_SECTOR_SCROLL_CEILING = "1024";
		private const string FLAG_SECTOR_SCROLL_FLOOR = "2048";
		private const string FLAG_SECTOR_SCROLL_LEFT = "4096";
		private const string FLAG_SECTOR_SCROLL_RIGHT = "8192";
		private const string FLAG_SECTOR_SCROLL_UP = "16384";
		private const string FLAG_SECTOR_SCROLL_DOWN = "32768";

		#endregion

		#region ================== Variables

		private static long tic = 0;
		private static bool enabled = true;

		#endregion

		#region ================== Properties

		// 
		// Turns the texture scrolling on or off. When off, textures are drawn without scrolling.
		// 
		public static bool Enabled
		{
			get { return enabled; }
			set { enabled = value; }
		}

		#endregion

		#region ================== Methods

		// The current game tic (as of the last Update)
		public static long Tic { get { return tic; } }

		// This returns the game tic of this moment, without changing the tic that is used for rendering
		public static long GetCurrentTic()
		{
			return (long)(General.stopwatch.Elapsed.TotalMilliseconds * TICS_PER_SECOND / 1000.0);
		}

		// This updates the current game tic. Call this once per rendered frame.
		public static void Update()
		{
			tic = (long)(General.stopwatch.Elapsed.TotalMilliseconds * TICS_PER_SECOND / 1000.0);
		}

		// This returns value modulo wrap as a positive number
		private static int Wrap(long value, int wrap)
		{
			long r = value % wrap;
			if(r < 0) r += wrap;
			return (int)r;
		}

		// This returns the texture coordinate offset (to add to the u and v of the vertices) for a geometry,
		// and the height offset (to add to the z of the vertices, only used by switches).
		// Returns false when the geometry does not scroll.
		public static bool GetOffset(VisualGeometry g, out float du, out float dv, out float dz)
		{
			du = 0.0f;
			dv = 0.0f;
			dz = 0.0f;

			if(!enabled || (g == null) || (General.Map == null) || !General.Map.FormatInterface.InDoom64Mode) return false;

			// Layers of a liquid floor
			ILiquidLayer liquid = g as ILiquidLayer;
			if((liquid != null) && (liquid.LiquidLayer != 0)) return GetLiquidOffset(g, liquid.LiquidLayer == 2, out du, out dv);

			switch(g.ScrollKind)
			{
				case ScrollSurface.Wall: return GetWallOffset(g, out du, out dv);
				case ScrollSurface.Floor: return GetPlaneOffset(g, false, out du, out dv);
				case ScrollSurface.Ceiling: return GetPlaneOffset(g, true, out du, out dv);
				case ScrollSurface.Switch: return GetSwitchOffset(g, out dz);
				default: return false;
			}
		}

		// Walls: the offsets of the front sidedef are changed by the flags of the line
		private static bool GetWallOffset(VisualGeometry g, out float du, out float dv)
		{
			du = 0.0f;
			dv = 0.0f;

			Sidedef sd = g.Sidedef;
			ImageData tex = g.Texture;
			if((sd == null) || (sd.Line == null) || !sd.IsFront || (tex == null)) return false;

			Linedef line = sd.Line;
			int dirx = 0;
			int diry = 0;
			if(line.IsFlagSet(FLAG_LINE_SCROLL_RIGHT)) dirx = 1;
			else if(line.IsFlagSet(FLAG_LINE_SCROLL_LEFT)) dirx = -1;
			if(line.IsFlagSet(FLAG_LINE_SCROLL_UP)) diry = 1;
			else if(line.IsFlagSet(FLAG_LINE_SCROLL_DOWN)) diry = -1;
			if((dirx == 0) && (diry == 0)) return false;

			// Offsets are in pixels, the same way as the sidedef offsets
			bool scaled = General.Map.Config.ScaledTextureOffsets && !tex.WorldPanning;
			float scalex = scaled ? tex.Scale.x : 1.0f;
			float scaley = scaled ? tex.Scale.y : 1.0f;
			float width = tex.ScaledWidth;
			float height = tex.ScaledHeight;
			if((width < 1.0f) || (height < 1.0f)) return false;

			du = (Wrap(dirx * tic, WALL_WRAP) * scalex) / width;
			dv = (Wrap(diry * tic, WALL_WRAP) * scaley) / height;
			return true;
		}

		// Switches: the game places a switch using the vertical offset of the front sidedef (rowoffset), which
		// the Scroll Up / Scroll Down flags change, so the switch moves up and down with the wall. It does not
		// move sideways. The offset wraps at 127 like the one of the sidedef (the geometry was built with the
		// offset that is set in the map, so only the difference is added).
		private static bool GetSwitchOffset(VisualGeometry g, out float dz)
		{
			dz = 0.0f;

			Sidedef sd = g.Sidedef;
			if((sd == null) || (sd.Line == null) || !sd.IsFront) return false;

			Linedef line = sd.Line;
			int diry = 0;
			if(line.IsFlagSet(FLAG_LINE_SCROLL_UP)) diry = 1;
			else if(line.IsFlagSet(FLAG_LINE_SCROLL_DOWN)) diry = -1;
			if(diry == 0) return false;

			int start = sd.OffsetY;
			dz = Wrap(start + (diry * tic), WALL_WRAP) - start;
			return true;
		}

		// This returns true when the floor (or ceiling) flat of the sector moves in the game, so the 2D view has to animate it
		public static bool IsScrolling(Sector s, bool ceiling)
		{
			float du, dv;
			if(!ceiling && IsLiquid(s)) return enabled;
			return GetSectorPlaneOffset(s, ceiling, out du, out dv);
		}

		// This returns true when the sector is a Doom 64 liquid floor (Liquid Effect flag)
		public static bool IsLiquid(Sector s)
		{
			return (s != null) && (General.Map != null) && General.Map.FormatInterface.InDoom64Mode && s.IsFlagSet(FLAG_SECTOR_LIQUID);
		}

		// The game draws a liquid floor with the flat after the floor flat (floorpic + 1) as the opaque bottom layer.
		// This finds the name of that flat using the texture index list of the game configuration (the order of the
		// textures in the game). Returns null when there is none.
		public static string GetLiquidBaseName(Sector s)
		{
			if((s == null) || (General.Map == null)) return null;
			List<TextureIndexInfo> list = General.Map.Config.D64TextureIndex;
			int index = -1;
			for(int i = 0; i < list.Count; i++)
			{
				if(string.Equals(list[i].Title, s.FloorTexture, StringComparison.OrdinalIgnoreCase))
				{
					index = list[i].Index;
					break;
				}
			}
			if(index < 0) return null;

			for(int i = 0; i < list.Count; i++)
			{
				if(list[i].Index == index + 1)
				{
					string name = list[i].Title;
					if(string.IsNullOrEmpty(name) || (name == "-")) return null;
					return name;
				}
			}
			return null;
		}

		// Liquid floors: both layers scroll all the time. Without the Floor Scroll flag the game moves them by half
		// a unit per tic (scrollfrac): the bottom layer along x, the top layer along y (the game
		// swaps and negates its offsets for the top layer). With the Floor Scroll flag the sector offsets are used instead.
		private static bool GetLiquidOffset(VisualGeometry g, bool top, out float du, out float dv)
		{
			du = 0.0f;
			dv = 0.0f;

			if((g.Sector == null) || (g.Sector.Sector == null)) return false;
			return GetSectorLiquidOffset(g.Sector.Sector, top, out du, out dv);
		}

		// Same as above for a sector, used by the 2D view (top = translucent floor layer, otherwise bottom layer)
		public static bool GetSectorLiquidOffset(Sector s, bool top, out float du, out float dv)
		{
			du = 0.0f;
			dv = 0.0f;

			if(!enabled || (s == null) || (General.Map == null) || !General.Map.FormatInterface.InDoom64Mode) return false;

			int xo = 0;
			int yo = 0;
			if(s.IsFlagSet(FLAG_SECTOR_SCROLL_FLOOR))
			{
				int dirx = 0;
				int diry = 0;
				if(s.IsFlagSet(FLAG_SECTOR_SCROLL_LEFT)) dirx = 1;
				else if(s.IsFlagSet(FLAG_SECTOR_SCROLL_RIGHT)) dirx = -1;
				if(s.IsFlagSet(FLAG_SECTOR_SCROLL_UP)) diry = -1;
				else if(s.IsFlagSet(FLAG_SECTOR_SCROLL_DOWN)) diry = 1;
				int speed = s.IsFlagSet(FLAG_SECTOR_SCROLL_FAST) ? 3 : 1;
				xo = Wrap(dirx * speed * tic, FLAT_WRAP);
				yo = Wrap(diry * speed * tic, FLAT_WRAP);
			}
			else
			{
				xo = Wrap(tic / 2, FLAT_WRAP);
			}

			// The top layer uses (-yoffset, xoffset)
			int px = top ? -yo : xo;
			int py = top ? xo : yo;
			du = Wrap(px, FLAT_WRAP) / 64.0f;
			dv = -Wrap(py, FLAT_WRAP) / 64.0f;
			return true;
		}

		// Floors and ceilings: the offsets of the sector are changed by the flags of the sector
		private static bool GetPlaneOffset(VisualGeometry g, bool ceiling, out float du, out float dv)
		{
			du = 0.0f;
			dv = 0.0f;

			if((g.Sector == null) || (g.Sector.Sector == null)) return false;
			return GetSectorPlaneOffset(g.Sector.Sector, ceiling, out du, out dv);
		}

		// Same as above for a sector, used by the 2D view
		public static bool GetSectorPlaneOffset(Sector s, bool ceiling, out float du, out float dv)
		{
			du = 0.0f;
			dv = 0.0f;

			if(!enabled || (s == null) || (General.Map == null) || !General.Map.FormatInterface.InDoom64Mode) return false;

			// The sector must scroll this plane
			if(!s.IsFlagSet(ceiling ? FLAG_SECTOR_SCROLL_CEILING : FLAG_SECTOR_SCROLL_FLOOR)) return false;

			int dirx = 0;
			int diry = 0;
			if(s.IsFlagSet(FLAG_SECTOR_SCROLL_LEFT)) dirx = 1;
			else if(s.IsFlagSet(FLAG_SECTOR_SCROLL_RIGHT)) dirx = -1;
			if(s.IsFlagSet(FLAG_SECTOR_SCROLL_UP)) diry = -1;
			else if(s.IsFlagSet(FLAG_SECTOR_SCROLL_DOWN)) diry = 1;
			if((dirx == 0) && (diry == 0)) return false;

			int speed = s.IsFlagSet(FLAG_SECTOR_SCROLL_FAST) ? 3 : 1;

			// The game adds the offset to the x and y of the vertices (64 units = 1 texture), and the
			// texture v coordinate runs the other way than the y of the map
			du = Wrap(dirx * speed * tic, FLAT_WRAP) / 64.0f;
			dv = -Wrap(diry * speed * tic, FLAT_WRAP) / 64.0f;
			return true;
		}

		#endregion
	}
}
