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
	public class ResultKexSwitchTexture : ErrorResult
	{
		#region ================== Types

		public enum Problem
		{
			WrongPartner,
			NeverReturns
		}

		#endregion

		#region ================== Variables

		private Linedef line;
		private Problem problem;
		private string texture;
		private string partnername;

		#endregion

		#region ================== Properties

		public override int Buttons { get { return 0; } }

		#endregion

		#region ================== Constructor / Destructor

		// For WrongPartner, other is the partner index and partnername the texture at that index.
		// For NeverReturns, other is the index of the first SWX texture and partnername its name.
		public ResultKexSwitchTexture(Linedef l, Problem problem, string texture, int index, int other, string partnername, string expected)
		{
			this.line = l;
			this.problem = problem;
			this.texture = texture;
			this.partnername = partnername;
			this.viewobjects.Add(l);

			if(problem == Problem.NeverReturns)
			{
				this.description = "The first \"SWX\" switch texture (\"" + partnername + "\") has odd load index " + other + ", so this repeatable switch changes into other textures " +
					"instead of returning in KEX Doom 64. Replace one more or one fewer stock texture before the \"SWX\" entries to fix this.";
			}
			else
			{
				string partnerdesc = "index " + other + ", " + ((partnername != null) ? "\"" + partnername + "\"" : "no texture");
				string fix = "Add or remove one texture before this pair in your WAD to fix this (use a new texture name).";
				if(expected != null)
					this.description = "\"" + texture + "\" has load index " + index + ", so its switch partner is " + partnerdesc + ", not \"" + expected + "\" in KEX Doom 64. " + fix;
				else
					this.description = "\"" + texture + "\" has load index " + index + ", so its switch partner is " + partnerdesc + " in KEX Doom 64. If that is not the matching texture: " + fix;
			}
		}

		#endregion

		#region ================== Methods

		// This must return the string that is displayed in the listbox
		public override string ToString()
		{
			if(problem == Problem.NeverReturns)
				return "Repeatable switch \"" + texture + "\" will not return to its original texture in KEX Doom 64";

			return "Switch texture \"" + texture + "\" is paired with " + ((partnername != null) ? "\"" + partnername + "\"" : "no texture") + " in KEX Doom 64";
		}

		// Rendering
		public override void PlotSelection(IRenderer2D renderer)
		{
			renderer.PlotLinedef(line, General.Colors.Selection);
			renderer.PlotVertex(line.Start, ColorCollection.VERTICES);
			renderer.PlotVertex(line.End, ColorCollection.VERTICES);
		}

		#endregion
	}
}
