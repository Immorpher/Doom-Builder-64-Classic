
#region ================== Namespaces

using System;
using System.Collections.Generic;

#endregion

namespace CodeImp.DoomBuilder.Map
{
	/// <summary>
	/// Simulates the Doom 64 light sector specials the same way the game does (p_lights.c, p_spec.c):
	///   1   = Light Blinks (randomly)           T_LightFlash
	///   2   = Light Strobe (0.5 sec)            T_StrobeFlash
	///   3   = Light Strobe (1 sec)              T_StrobeFlash
	///   8   = Light Glows (normal)              T_Glow
	///   9   = Light Glows (slow)                T_Glow
	///   11  = Light Glows (randomly)            T_Glow
	///   17  = Light Flickers (randomly)         T_FireFlicker
	///   202 = Bright Light Strobe (0.1 sec)     T_StrobeFlash
	///   204 = Light Strobe (0.3 sec)            T_StrobeFlash
	///   205 = Sequence Starter                  T_SequenceGlow
	///   206 = Light Strobe (3 sec)              T_StrobeFlash
	///   208 = Bright Light Strobe (0.2 sec)     T_StrobeFlash
	/// A thinker is run 30 times per second (the game's tick rate) and changes a per-sector "light level"
	/// that is ADDED to the texture color when rendering (G_CC_D64COMB07 in the Doom 64 RE source).
	///
	/// The editor does not keep any of this state in the map; it is purely for display in the 3D mode.
	/// </summary>
	internal static class SectorGlow
	{
		#region ================== Constants

		// Game tick rate (the game runs at a maximum of 30 fps)
		public const int TICS_PER_SECOND = 30;

		// Maximum number of tics that are simulated in one update. When the editor was not
		// updating for a longer time (window minimized, etc.) the lights just continue from here.
		private const int MAX_CATCHUP_TICS = 60;

		// Sector flag "Sync Specials" (MS_SYNCSPECIALS)
		private const string FLAG_SYNC_SPECIALS = "8";

		// From p_spec.h
		private const int GLOWSPEED = 2;
		private const int STROBEBRIGHT = 3;
		private const int STROBEBRIGHT2 = 1;
		private const int FASTDARK = 15;
		private const int SLOWDARK = 30;
		private const int SEQUENCELIGHTMAX = 48;

		// Light levels are 0..255 on the N64 (PRIM_LOD_FRAC is 8 bits)
		private const float LIGHT_SCALE = 1.0f / 255.0f;

		#endregion

		#region ================== Thinkers

		// Thinkers that share a function in the game also share a family. Sectors with "Sync Specials"
		// copy the light level of the first thinker of their family (P_CombineLightSpecials).
		private enum Family
		{
			None,
			Flash,
			Strobe,
			Glow,
			Flicker,
			Sequence
		}

		private abstract class Thinker
		{
			public Sector sector;
			public int effect;
			public int lightlevel = 0;
			public bool dead = false;

			public abstract void Tick();
		}

		// Port of T_LightFlash (special 1)
		private sealed class FlashThinker : Thinker
		{
			private int count;

			public FlashThinker(Sector s, int effect)
			{
				this.sector = s;
				this.effect = effect;
				this.count = (P_Random() & 63) + 1;
			}

			public override void Tick()
			{
				if(--count != 0) return;

				if(lightlevel == 32)
				{
					lightlevel = 0;
					count = (P_Random() & 7) + 1;
				}
				else
				{
					lightlevel = 32;
					count = (P_Random() & 32) + 1;
				}
			}
		}

		// Port of T_StrobeFlash (specials 2, 3, 202, 204, 206, 208)
		private sealed class StrobeThinker : Thinker
		{
			private int count;
			private int maxlight;
			private int darktime;
			private int brighttime;

			public StrobeThinker(Sector s, int effect)
			{
				this.sector = s;
				this.effect = effect;

				switch(effect)
				{
					case 2: // P_SpawnStrobeFlash(sector, FASTDARK)
						SetupNormal(FASTDARK);
						break;
					case 3: // P_SpawnStrobeFlash(sector, SLOWDARK)
						SetupNormal(SLOWDARK);
						break;
					case 204: // P_SpawnStrobeFlash(sector, 7)
						SetupNormal(7);
						break;
					case 206: // P_SpawnStrobeFlash(sector, 90)
						SetupNormal(90);
						break;
					case 202: // P_SpawnStrobeAltFlash(sector, 3)
						SetupAlt(3);
						break;
					case 208: // P_SpawnStrobeAltFlash(sector, 6)
						SetupAlt(6);
						break;
				}
			}

			private void SetupNormal(int dark)
			{
				brighttime = STROBEBRIGHT;
				maxlight = 16;
				darktime = dark;
				count = (P_Random() & 7) + 1;
			}

			private void SetupAlt(int dark)
			{
				brighttime = STROBEBRIGHT2;
				maxlight = 127;
				darktime = dark;
				count = 1;
			}

			public override void Tick()
			{
				if(--count != 0) return;

				if(lightlevel == 0)
				{
					lightlevel = maxlight;
					count = brighttime;
				}
				else
				{
					lightlevel = 0;
					count = darktime;
				}
			}
		}

		// Port of T_Glow (specials 8, 9, 11)
		private sealed class GlowThinker : Thinker
		{
			private int count = 2;
			private int direction = 1;
			private int minlight = 0;
			private int maxlight;

			public GlowThinker(Sector s, int effect)
			{
				this.sector = s;
				this.effect = effect;

				// P_SpawnGlowingLight
				if(effect == 8) maxlight = 32;
				else maxlight = 48;
			}

			public override void Tick()
			{
				if(--count != 0) return;
				count = 2;

				switch(direction)
				{
					case -1: // DOWN
						lightlevel -= GLOWSPEED;
						if(lightlevel < minlight)
						{
							lightlevel = minlight;
							if(effect == 11) maxlight = (P_Random() & 31) + 17;
							direction = 1;
						}
						break;

					case 1: // UP
						lightlevel += GLOWSPEED;
						if(maxlight < lightlevel)
						{
							lightlevel = maxlight;
							if(effect == 11) minlight = (P_Random() & 15);
							direction = -1;
						}
						break;
				}
			}
		}

		// Port of T_FireFlicker (special 17)
		private sealed class FlickerThinker : Thinker
		{
			private int count = 3;

			public FlickerThinker(Sector s, int effect)
			{
				this.sector = s;
				this.effect = effect;
			}

			public override void Tick()
			{
				if(--count != 0) return;

				lightlevel = (P_Random() & 31);
				count = 3;
			}
		}

		// Port of T_SequenceGlow (special 205)
		// The starter waits for its head sector to light up, glows up and down, and while glowing up it passes
		// the light on to the neighbouring sectors (the next sector of the sequence is the one behind a line
		// with the tag + 1 that has no special). Those followers glow once and then stop (their special is
		// temporarily set by the game, the map in the editor is never changed).
		private sealed class SequenceThinker : Thinker
		{
			private int count = 1;
			private int start;
			private Sector head;

			public SequenceThinker(Sector s, int effect, Sector head)
			{
				this.sector = s;
				this.effect = effect;
				this.head = head;
				this.start = (head == null) ? 1 : 0;
			}

			public override void Tick()
			{
				if(--count != 0) return;
				count = 1;

				switch(start)
				{
					case -1: // DOWN
						lightlevel -= GLOWSPEED;
						if(lightlevel > 0) return;
						lightlevel = 0;

						if(head == null)
						{
							// The game sets the special of the sector back to 0 here
							dead = true;
							return;
						}

						start = 0;
						break;

					case 0: // CHECK
						if(LevelOf(head) == 0) return;
						start = 1;
						break;

					case 1: // UP
						lightlevel += GLOWSPEED;
						if(lightlevel < (SEQUENCELIGHTMAX + 1))
						{
							if(lightlevel != 8) return;
							PassOnLight(this);
						}
						else
						{
							lightlevel = SEQUENCELIGHTMAX;
							start = -1;
						}
						break;
				}
			}
		}

		#endregion

		#region ================== Variables

		// The thinkers the game would have spawned for the sectors in the map, by sector
		private static readonly Dictionary<Sector, Thinker> natives = new Dictionary<Sector, Thinker>();

		// Sequence followers that are currently glowing (sectors that got their special set by a sequence)
		private static readonly List<Thinker> dynamics = new List<Thinker>();

		// All running thinkers in the order they run (sector order, followers last) and by sector
		private static readonly List<Thinker> order = new List<Thinker>();
		private static readonly Dictionary<Sector, Thinker> running = new Dictionary<Sector, Thinker>();

		// Sectors with "Sync Specials" and the thinker they follow
		private static readonly Dictionary<Sector, Thinker> synced = new Dictionary<Sector, Thinker>();

		// The resulting light levels
		private static readonly Dictionary<Sector, int> levels = new Dictionary<Sector, int>();

		private static readonly List<Sector> removelist = new List<Sector>();
		private static readonly Random random = new Random();
		private static long lasttic = -1;
		private static bool enabled = true;

		#endregion

		#region ================== Properties

		/// <summary>
		/// Turns the light effects on or off. When off, all sectors are drawn without the additive glow.
		/// </summary>
		public static bool Enabled
		{
			get { return enabled; }
			set { enabled = value; }
		}

		#endregion

		#region ================== Methods

		// Port of P_Random
		private static int P_Random()
		{
			return random.Next(256);
		}

		// This clears all state
		public static void Reset()
		{
			natives.Clear();
			dynamics.Clear();
			order.Clear();
			running.Clear();
			synced.Clear();
			levels.Clear();
			lasttic = -1;
		}

		// This returns the family of thinker that a sector effect spawns
		private static Family GetFamily(int effect)
		{
			switch(effect)
			{
				case 1: return Family.Flash;
				case 2:
				case 3:
				case 202:
				case 204:
				case 206:
				case 208: return Family.Strobe;
				case 8:
				case 9:
				case 11: return Family.Glow;
				case 17: return Family.Flicker;
				case 205: return Family.Sequence;
				default: return Family.None;
			}
		}

		// This returns true when the effect is one of the light effects
		public static bool IsLightEffect(int effect)
		{
			return GetFamily(effect) != Family.None;
		}

		// This returns true when the sector has a light effect that changes over time (Doom 64 only), so
		// the 2D view has to keep redrawing it
		public static bool IsAnimated(Sector s)
		{
			return enabled && (s != null) && (General.Map != null) && General.Map.FormatInterface.InDoom64Mode && (GetFamily(s.Effect) != Family.None);
		}

		// This returns the additive glow for a sector as a 0..1 value to add to the texture color.
		// Returns 0 for all sectors that do not glow.
		public static float GetGlow(Sector s)
		{
			int level;
			if(!enabled || (s == null)) return 0.0f;
			if(levels.TryGetValue(s, out level)) return level * LIGHT_SCALE;
			return 0.0f;
		}

		// This returns the current light level of a sector (sectors without a light special are 0)
		private static int LevelOf(Sector s)
		{
			Thinker t;
			if(s == null) return 0;
			if(running.TryGetValue(s, out t)) return t.lightlevel;
			if(synced.TryGetValue(s, out t) && (t != null)) return t.lightlevel;
			return 0;
		}

		// This creates the thinker that the game spawns for a sector (P_AddSectorSpecial), or null for none
		private static Thinker SpawnThinker(Sector s)
		{
			int effect = s.Effect;
			switch(GetFamily(effect))
			{
				case Family.Flash: return new FlashThinker(s, effect);
				case Family.Strobe: return new StrobeThinker(s, effect);
				case Family.Glow: return new GlowThinker(s, effect);
				case Family.Flicker: return new FlickerThinker(s, effect);
				case Family.Sequence: return SpawnSequence(s);
				default: return null;
			}
		}

		// Port of P_SpawnSequenceLight for a sector that has the sequence starter special
		private static Thinker SpawnSequence(Sector s)
		{
			// The head sector is the sector at the front of a line of this sector that has the same tag.
			// Like in the game, when no such line is found the front sector of the last line is used.
			List<Linedef> lines = GetLines(s);
			Sector head = null;
			foreach(Linedef l in lines)
			{
				head = (l.Front != null) ? l.Front.Sector : null;
				if((head != null) && (head != s) && (head.Tag == s.Tag)) break;
			}

			if(head == null) return null;
			return new SequenceThinker(s, s.Effect, head);
		}

		// This returns the lines of a sector in the order of the line indices (like the game's sector line list)
		private static List<Linedef> GetLines(Sector s)
		{
			List<Linedef> lines = new List<Linedef>();
			foreach(Sidedef sd in s.Sidedefs)
			{
				if((sd.Line != null) && !lines.Contains(sd.Line)) lines.Add(sd.Line);
			}
			lines.Sort(delegate(Linedef a, Linedef b) { return a.Index.CompareTo(b.Index); });
			return lines;
		}

		// Part of T_SequenceGlow: lets the next sectors of the sequence start glowing
		private static void PassOnLight(Thinker from)
		{
			Sector s = from.sector;
			foreach(Linedef l in GetLines(s))
			{
				// Only the lines that have this sector at the front have it as 'backsector' for the next sector
				if((l.Front == null) || (l.Back == null) || (l.Front.Sector != s)) continue;

				Sector next = l.Back.Sector;
				if((next == null) || (next == s) || next.IsDisposed) continue;

				// The next sector must have no special (of its own, or from another sequence) and the next tag
				if((next.Effect != 0) || running.ContainsKey(next)) continue;
				if(next.Tag != (s.Tag + 1)) continue;

				Thinker t = new SequenceThinker(next, from.effect, null);
				dynamics.Add(t);
				order.Add(t);
				running[next] = t;
			}
		}

		// This advances the simulation to the current time. Call this once per rendered frame.
		public static void Update()
		{
			if(!enabled || (General.Map == null) || (General.Map.Map == null)) return;

			// Only the Doom 64 map format has these effects
			if(!General.Map.FormatInterface.InDoom64Mode)
			{
				if((levels.Count > 0) || (natives.Count > 0)) Reset();
				return;
			}

			// Which tic are we at now?
			long tic = (long)(General.stopwatch.Elapsed.TotalMilliseconds * TICS_PER_SECOND / 1000.0);
			if(lasttic < 0) lasttic = tic;
			long steps = tic - lasttic;
			if(steps <= 0) return;
			lasttic = tic;
			if(steps > MAX_CATCHUP_TICS) steps = MAX_CATCHUP_TICS;

			// Keep the thinkers up to date with the sector effects in the map, in sector order like the game
			// spawns them. Sectors with "Sync Specials" follow the first thinker of their family.
			Dictionary<Family, Thinker> firstoffamily = new Dictionary<Family, Thinker>();
			List<Sector> syncsectors = new List<Sector>();
			order.Clear();
			running.Clear();
			synced.Clear();
			foreach(Sector s in General.Map.Map.Sectors)
			{
				Family family = GetFamily(s.Effect);
				if(family == Family.None) continue;

				if(s.IsFlagSet(FLAG_SYNC_SPECIALS))
				{
					// The game can only sync with these, the sequence starter does nothing in this case
					if(family != Family.Sequence) syncsectors.Add(s);
					continue;
				}

				Thinker t;
				if(!natives.TryGetValue(s, out t) || (t == null) || (t.effect != s.Effect))
				{
					t = SpawnThinker(s);
					natives[s] = t;
				}

				if(t == null) continue;
				order.Add(t);
				running[s] = t;
				if(!firstoffamily.ContainsKey(family)) firstoffamily[family] = t;
			}

			// Remove the thinkers of sectors that no longer have a (unsynced) light effect
			removelist.Clear();
			foreach(Sector s in natives.Keys)
			{
				if(s.IsDisposed || !running.ContainsKey(s)) removelist.Add(s);
			}
			foreach(Sector s in removelist) natives.Remove(s);

			// The sequence followers go after the thinkers of the map, like they were spawned later
			for(int i = dynamics.Count - 1; i >= 0; i--)
			{
				Thinker d = dynamics[i];
				if(d.dead || d.sector.IsDisposed || (d.sector.Effect != 0)) dynamics.RemoveAt(i);
			}
			foreach(Thinker d in dynamics)
			{
				order.Add(d);
				running[d.sector] = d;
			}

			// The sectors that follow another thinker
			foreach(Sector s in syncsectors)
			{
				Thinker combiner;
				firstoffamily.TryGetValue(GetFamily(s.Effect), out combiner);
				synced[s] = combiner;
			}

			// Run the thinkers (followers that are spawned while running are added to the end of the list)
			for(long step = 0; step < steps; step++)
			{
				for(int i = 0; i < order.Count; i++)
				{
					Thinker t = order[i];
					if(t.dead) continue;
					t.Tick();

					// A follower that finished is free to be started again right away, like in the game
					if(t.dead) running.Remove(t.sector);
				}
			}

			// Publish the resulting light levels
			levels.Clear();
			foreach(Thinker t in order)
			{
				if(!t.dead) levels[t.sector] = t.lightlevel;
			}
			foreach(KeyValuePair<Sector, Thinker> kv in synced)
			{
				levels[kv.Key] = (kv.Value != null) ? kv.Value.lightlevel : 0;
			}
		}

		#endregion
	}
}
