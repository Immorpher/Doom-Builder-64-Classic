
#region ================== Namespaces

using System;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.VisualModes;

#endregion

namespace CodeImp.DoomBuilder.Map
{
	/// <summary>
	/// Calculates the texture scrolling of Doom 64 for the 3D mode, like the game does in P_UpdateSpecials (p_spec.c):
	///  - Lines with the Scroll Right/Left/Up/Down flags scroll the textures of their FRONT side by 1 pixel per tic.
	///  - Sectors with Floor Scroll / Ceiling Scroll and a direction flag scroll their flat by 1 unit per tic
	///    (3 units per tic with the Fast Scrolling flag).
	/// The game runs at 30 tics per second and moves the textures in whole steps, so this does the same.
	/// The editor does not change the map; the result is only used as a texture coordinate offset when rendering.
	/// </summary>
	internal static class TextureScroll
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

		/// <summary>
		/// Turns the texture scrolling on or off. When off, textures are drawn without scrolling.
		/// </summary>
		public static bool Enabled
		{
			get { return enabled; }
			set { enabled = value; }
		}

		#endregion

		#region ================== Methods

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

		// Floors and ceilings: the offsets of the sector are changed by the flags of the sector
		private static bool GetPlaneOffset(VisualGeometry g, bool ceiling, out float du, out float dv)
		{
			du = 0.0f;
			dv = 0.0f;

			if((g.Sector == null) || (g.Sector.Sector == null)) return false;
			Sector s = g.Sector.Sector;

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
