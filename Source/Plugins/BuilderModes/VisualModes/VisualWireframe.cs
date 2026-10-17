#region ================== Namespaces

using System;
using System.Collections.Generic;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;

#endregion

namespace CodeImp.DoomBuilder.BuilderModes
{
	// Builds the wireframe that is drawn over geometry that the Doom 64 engine does not draw (and that is
	// therefore only kept for picking): BLANK walls, upper walls behind a sky and sky floors / ceilings.
	// The lines use the colors and the 1 pixel thickness of the linedefs in the 2D map view.
	internal static class VisualWireframe
	{
		// How opaque the lines are (0-255)
		private const int ALPHA = 0x40;

		// The color of a linedef as in the 2D view: single-sided lines in the linedef color, double-sided lines
		// in the same color faded by the double-sided alpha (blended over the background to stay opaque in 3D)
		// The alpha makes the lines see-through.
		public static int GetLinedefColor(Linedef l)
		{
			PixelColor c = General.Colors.Linedefs;
			if(l.Back != null)
			{
				PixelColor bg = General.Colors.Background;
				int a = General.Settings.DoubleSidedAlphaByte;
				c = new PixelColor(255,
					(byte)((c.r * a + bg.r * (255 - a)) / 255),
					(byte)((c.g * a + bg.g * (255 - a)) / 255),
					(byte)((c.b * a + bg.b * (255 - a)) / 255));
			}

			c.a = (byte)ALPHA;
			return c.ToInt();
		}

		// Adds a single line
		private static void AddLine(List<WorldVertex> lines, float x1, float y1, float z1, float x2, float y2, float z2, int color)
		{
			lines.Add(new WorldVertex(x1, y1, z1, color, 0f, 0f));
			lines.Add(new WorldVertex(x2, y2, z2, color, 0f, 0f));
		}

		// Adds a rectangular frame along a linedef between two heights
		public static void AddWallFrame(List<WorldVertex> lines, Linedef l, float bottom, float top)
		{
			int c = GetLinedefColor(l);
			float x1 = l.Start.Position.x;
			float y1 = l.Start.Position.y;
			float x2 = l.End.Position.x;
			float y2 = l.End.Position.y;

			AddLine(lines, x1, y1, bottom, x2, y2, bottom, c);	// Bottom
			AddLine(lines, x1, y1, top, x2, y2, top, c);		// Top
			AddLine(lines, x1, y1, bottom, x1, y1, top, c);		// Start side
			AddLine(lines, x2, y2, bottom, x2, y2, top, c);		// End side
		}

		// Traces the outline of a sector at a height, along its linedefs
		public static void AddSectorOutline(List<WorldVertex> lines, Sector s, float z)
		{
			foreach(Sidedef sd in s.Sidedefs)
			{
				Linedef l = sd.Line;
				AddLine(lines, l.Start.Position.x, l.Start.Position.y, z, l.End.Position.x, l.End.Position.y, z, GetLinedefColor(l));
			}
		}
	}
}
