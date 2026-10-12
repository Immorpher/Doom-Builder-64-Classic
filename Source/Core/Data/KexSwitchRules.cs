#region ================== Copyright

/*
 * KEX Doom 64 switch texture rules.
 *
 * The KEX Doom 64 Remaster toggles a switch's texture by flipping the lowest bit of the texture's
 * position relative to the first texture whose name starts with "SWX" (swx_start), but restores the
 * texture with a plain "index ^ 1" that ignores swx_start. The two only agree when swx_start is even,
 * so repeatable switches only come back to their original texture then. Each switch pair also has to
 * start on an even index in the final merged texture list.
 *
 * The merged texture list is built by the game when it loads: every stock texture whose name is not
 * used by a loaded WAD (names compared case-insensitively) in stock order, followed by all textures of
 * each loaded WAD in WAD order. This class reproduces that, so the editor can predict the problem.
 */

#endregion

#region ================== Namespaces

using System;
using System.Collections.Generic;

#endregion

namespace CodeImp.DoomBuilder.Data
{
	public static class KexSwitchRules
	{
		#region ================== Constants

		public const string ANCHOR_PREFIX = "SWX";

		#endregion

		#region ================== Merged list

		// Merges texture name lists, the first list being the stock (IWAD) textures
		public static List<string> Merge(List<List<string>> sources)
		{
			List<string> merged = new List<string>();
			bool first = true;
			foreach(List<string> source in sources)
			{
				if(first)
				{
					merged.AddRange(source);
					first = false;
					continue;
				}

				HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach(string name in source) used.Add(name);
				merged.RemoveAll(delegate(string existing) { return used.Contains(existing); });
				merged.AddRange(source);
			}
			return merged;
		}

		// Index of the first texture that starts with SWX (case-sensitive, like the game), or -1
		public static int FindAnchor(List<string> merged)
		{
			for(int i = 0; i < merged.Count; i++)
				if(merged[i].StartsWith(ANCHOR_PREFIX, StringComparison.Ordinal)) return i;
			return -1;
		}

		// Index of the last texture with this name (case-insensitive), or -1
		public static int FindIndex(List<string> merged, string name)
		{
			for(int i = merged.Count - 1; i >= 0; i--)
				if(string.Equals(merged[i], name, StringComparison.OrdinalIgnoreCase)) return i;
			return -1;
		}

		#endregion

		#region ================== Switch math

		// The texture index the game switches to when the switch is used
		public static int Partner(int index, int anchor)
		{
			return anchor + ((index - anchor) ^ 1);
		}

		// The texture index the game restores after a repeatable switch resets
		public static int Restored(int pressedindex)
		{
			return pressedindex ^ 1;
		}

		// True when two names look like the two halves of a switch (the same name apart from one character)
		public static bool LooksPaired(string a, string b)
		{
			if((a == null) || (b == null) || (a.Length != b.Length)) return false;
			int diff = 0;
			for(int i = 0; i < a.Length; i++)
				if(char.ToUpperInvariant(a[i]) != char.ToUpperInvariant(b[i])) diff++;
			return diff <= 1;
		}

		// For names ending in A or B this returns the other half, otherwise null
		public static string ExpectedPartnerName(string name)
		{
			if(string.IsNullOrEmpty(name)) return null;
			char last = name[name.Length - 1];
			char other;
			if(last == 'A') other = 'B';
			else if(last == 'B') other = 'A';
			else if(last == 'a') other = 'b';
			else if(last == 'b') other = 'a';
			else return null;
			return name.Substring(0, name.Length - 1) + other;
		}

		#endregion

		#region ================== Stock switches

		// Checks whether the stock SWX switches still work after the WADs were merged over the stock textures.
		// Returns warning messages (empty when everything is fine).
		public static List<string> CheckStockSwitches(List<string> stock, List<string> merged)
		{
			List<string> messages = new List<string>();

			int stockanchor = FindAnchor(stock);
			if(stockanchor < 0) return messages;	// Not a Doom 64 IWAD
			int s = FindAnchor(merged);

			// No (capitalized) SWX texture is left
			if(s < 0)
			{
				messages.Add("No texture starting with \"SWX\" (capital letters) is left after your WAD replaced the stock switch textures. " +
					"Switches will not work correctly in KEX Doom 64. Name your replacement switch textures in capital letters.");
				return messages;
			}

			// The first SWX texture is on an odd index
			if((s & 1) != 0)
			{
				string anchorname = merged[s];
				if(string.Equals(anchorname, stock[stockanchor], StringComparison.OrdinalIgnoreCase) && (s != stockanchor))
				{
					messages.Add("Your WAD replaces an odd number of stock textures that load before the \"SWX\" switch textures, which breaks the stock switches in KEX Doom 64. " +
						"Replace one more or one fewer stock texture before the \"SWX\" entries to fix this.");
				}
				else
				{
					messages.Add("The first \"SWX\" switch texture (\"" + anchorname + "\") has an odd index (" + s + "), which breaks the stock switches in KEX Doom 64. " +
						"Add or remove one texture (with a new name) before it in your WAD to fix this.");
				}
				return messages;
			}

			// Check that every stock switch pair is still a pair
			int bad = 0;
			string firstbad = null;
			for(int i = stockanchor; i + 1 < stock.Count; i += 2)
			{
				if(!stock[i].StartsWith(ANCHOR_PREFIX, StringComparison.Ordinal) ||
				   !stock[i + 1].StartsWith(ANCHOR_PREFIX, StringComparison.Ordinal)) break;

				int ia = FindIndex(merged, stock[i]);
				int ib = FindIndex(merged, stock[i + 1]);
				if((ia < 0) || (ib < 0)) continue;

				int p = Partner(ia, s);
				if(p != ib)
				{
					bad++;
					if(firstbad == null)
					{
						string shown = ((p >= 0) && (p < merged.Count)) ? merged[p] : "nothing";
						firstbad = "Stock switch \"" + stock[i] + "\"/\"" + stock[i + 1] + "\" is misaligned in KEX Doom 64 and would show \"" + shown + "\" instead of \"" + stock[i + 1] + "\".";
					}
				}
			}

			if(bad > 0)
			{
				messages.Add(firstbad + (bad > 1 ? " (" + (bad - 1) + " more stock switches are affected.)" : "") +
					" Replace both halves of a stock switch or neither, and keep replaced pairs starting on an even index.");
			}

			return messages;
		}

		#endregion
	}
}
