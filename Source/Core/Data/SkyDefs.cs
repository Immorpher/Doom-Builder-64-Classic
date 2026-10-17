#region ================== Copyright

/*
 * Doom 64 sky definitions.
 *
 * A flat is a sky when the engine does not draw it as a plane: the stock Doom 64 engine treats every flat
 * from F_SKYA onward as a sky (see P_LoadSectors in DOOM64-RE), and the KEX Doom 64 Remaster defines the
 * skies in the SKYDEFS lump with "sky F_SKYx { ... }" blocks. This class reads those block names.
 */

#endregion

#region ================== Namespaces

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#endregion

namespace CodeImp.DoomBuilder.Data
{
	public static class SkyDefs
	{
		// Flats with this prefix are always skies (F_SKYA .. F_SKYK in the stock IWAD)
		public const string STOCK_PREFIX = "F_SKY";

		private static readonly Regex commentsregex = new Regex(@"//[^\r\n]*|/\*.*?\*/", RegexOptions.Singleline | RegexOptions.Compiled);
		private static readonly Regex skyregex = new Regex(@"(?<![\w])sky\s+""?([^\s""{}]+)""?\s*\{", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		// Returns the names of the skies defined in SKYDEFS text
		public static List<string> ParseNames(string text)
		{
			List<string> names = new List<string>();
			if(string.IsNullOrEmpty(text)) return names;

			text = commentsregex.Replace(text, " ");
			foreach(Match m in skyregex.Matches(text))
				names.Add(m.Groups[1].Value);
			return names;
		}

		// True when the name is a stock sky flat
		public static bool IsStockSky(string name)
		{
			return !string.IsNullOrEmpty(name) && name.StartsWith(STOCK_PREFIX, StringComparison.OrdinalIgnoreCase);
		}
	}
}
