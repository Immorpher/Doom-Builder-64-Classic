
#region ================== Copyright (c) 2016 Boris Iwanski

/*
 * Copyright (c) 2016 Boris Iwanski https://github.com/biwa/automapmode
 * Ported to Doom Builder 64 II
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

using CodeImp.DoomBuilder.Plugins;

#endregion

namespace CodeImp.DoomBuilder.AutomapMode
{
	//
	// MANDATORY: The plug!
	// This is an important class to the Doom Builder core. Every plugin must
	// have exactly 1 class that inherits from Plug. When the plugin is loaded,
	// this class is instantiated and used to receive events from the core.
	//

	public class BuilderPlug : Plug
	{
		#region ================== Variables

		private float highlightrange;

		// Static instance. We can't use a real static class, because BuilderPlug must
		// be instantiated by the core, so we keep a static reference.
		private static BuilderPlug me;

		#endregion

		#region ================== Properties

		public float HighlightRange { get { return highlightrange; } }

		// Linedef flag that makes a two-sided line render as one-sided on the automap.
		// Doom 64 II's game configurations define this as "secretflag" (32 for Doom 64).
		public string SecretFlag
		{
			get
			{
				string f = General.Map.Config.SecretFlag;
				if(!string.IsNullOrEmpty(f) && f != "0") return f;
				return IsUDMF ? "secret" : "32";
			}
		}

		// Linedef flag that hides a line from the automap entirely.
		// Doom 64 II's game configurations define this as "invisibleflag" (128 for Doom 64).
		public string HiddenFlag
		{
			get
			{
				string f = General.Map.Config.InvisibleFlag;
				if(!string.IsNullOrEmpty(f) && f != "0") return f;
				return IsUDMF ? "dontdraw" : "128";
			}
		}

		// Doom 64 only: linedef flag 256 = "Show on Automap". Returns null when unsupported.
		public string AlwaysShowFlag
		{
			get { return General.Map.FormatInterface.InDoom64Mode ? "256" : null; }
		}

		// Doom 64 only: linedef flag 33554432 = "Hide Special on Automap"
		public string HideSpecialFlag
		{
			get { return "33554432"; }
		}

		private static bool IsUDMF
		{
			get { return General.Map.Config.FormatInterface == "UniversalMapSetIO"; }
		}

		// Static property to access the BuilderPlug
		public static BuilderPlug Me { get { return me; } }

		#endregion

		#region ================== Methods

		// This event is called when the plugin is initialized
		public override void OnInitialize()
		{
			base.OnInitialize();

			General.Actions.BindMethods(this);

			// Keep a static reference
			me = this;

			LoadSettings();
		}

		public override void OnMapOpenEnd()
		{
			AutomapMode mode = General.Editing.Mode as AutomapMode;
			if(mode != null)
			{
				mode.UpdateSectorFlags();
				mode.UpdateValidLinedefs();
			}

			base.OnMapOpenEnd();
		}

		public override void OnMapNewEnd()
		{
			AutomapMode mode = General.Editing.Mode as AutomapMode;
			if(mode != null)
			{
				mode.UpdateSectorFlags();
				mode.UpdateValidLinedefs();
			}

			base.OnMapNewEnd();
		}

		// This is called when the plugin is terminated
		public override void Dispose()
		{
			base.Dispose();

			// This must be called to remove bound methods for actions.
			General.Actions.UnbindMethods(this);
		}

		private void LoadSettings()
		{
			// Reuse the BuilderModes highlight range so highlighting feels consistent
			highlightrange = General.Settings.ReadPluginSetting("plugins.buildermodes", "highlightrange", 20);
		}

		#endregion
	}
}
