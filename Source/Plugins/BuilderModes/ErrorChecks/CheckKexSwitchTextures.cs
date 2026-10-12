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
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.Types;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Data;
using System.Threading;

#endregion

namespace CodeImp.DoomBuilder.BuilderModes
{
	// Doom 64 only. Predicts the KEX Doom 64 problem where a switch texture is paired with the wrong
	// texture, or where a repeatable switch never returns to its original texture. See KexSwitchRules.
	[ErrorChecker("Check KEX Doom 64 switch textures", true, 40)]
	public class CheckKexSwitchTextures : ErrorChecker
	{
		#region ================== Constants

		private const int PROGRESS_STEP = 1000;
		private const int SWITCH_PART_MASK = 0x6000;	// ML_SWITCHX02 | ML_SWITCHX04
		private const int SWITCH_UPPER = 0x2000;
		private const int SWITCH_LOWER = 0x4000;
		private const int ACTIVATE_REPEAT = 0x8000;		// MLU_REPEAT

		#endregion

		#region ================== Constructor / Destructor

		public CheckKexSwitchTextures()
		{
			// Total progress is done when all lines are checked
			SetTotalProgress(General.Map.Map.Linedefs.Count / PROGRESS_STEP);
		}

		#endregion

		#region ================== Methods

		// This runs the check
		public override void Run()
		{
			int progress = 0;
			int stepprogress = 0;

			// Only for Doom 64 maps
			if(!General.Map.FormatInterface.InDoom64Mode) { SkipProgress(); return; }

			// The texture list as the game will build it
			List<string> merged = General.Map.Data.GetKexTextureOrder();
			if(merged == null) { SkipProgress(); return; }
			int anchor = KexSwitchRules.FindAnchor(merged);
			if(anchor < 0) { SkipProgress(); return; }	// Reported when the resources are loaded

			foreach(Linedef l in General.Map.Map.Linedefs)
			{
				// The game only toggles the front texture of a linedef that has an action
				int part = l.SwitchMask & SWITCH_PART_MASK;
				if((part != 0) && (l.Action != 0) && (l.Front != null))
				{
					string name;
					if(part == SWITCH_UPPER) name = l.Front.HighTexture;
					else if(part == SWITCH_LOWER) name = l.Front.LowTexture;
					else name = l.Front.MiddleTexture;

					if((name != null) && (name.Length > 0) && (name[0] != '-'))
					{
						int index = KexSwitchRules.FindIndex(merged, name);
						if(index >= 0) CheckSwitch(l, name, index, merged, anchor);
					}
				}

				// Handle thread interruption
				try { Thread.Sleep(0); }
				catch(ThreadInterruptedException) { return; }

				// We are making progress!
				if((++progress / PROGRESS_STEP) > stepprogress)
				{
					stepprogress = (progress / PROGRESS_STEP);
					AddProgress(1);
				}
			}
		}

		// Nothing to check: complete the progress bar
		private void SkipProgress()
		{
			if(TotalProgress > 0) AddProgress(TotalProgress);
		}

		private void CheckSwitch(Linedef l, string name, int index, List<string> merged, int anchor)
		{
			bool repeatable = (l.Activate & ACTIVATE_REPEAT) != 0;

			// Repeatable switches restore their texture without the SWX offset, so the first SWX texture must be on an even index
			if(repeatable && ((anchor & 1) != 0))
			{
				SubmitResult(new ResultKexSwitchTexture(l, ResultKexSwitchTexture.Problem.NeverReturns, name, index, anchor, merged[anchor], null));
				return;
			}

			// The texture the game changes this switch into
			int partner = KexSwitchRules.Partner(index, anchor);
			string partnername = ((partner >= 0) && (partner < merged.Count)) ? merged[partner] : null;
			if((partnername == null) || !KexSwitchRules.LooksPaired(name, partnername))
			{
				SubmitResult(new ResultKexSwitchTexture(l, ResultKexSwitchTexture.Problem.WrongPartner, name, index, partner, partnername, KexSwitchRules.ExpectedPartnerName(name)));
			}
		}

		#endregion
	}
}
