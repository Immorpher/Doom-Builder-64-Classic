
#region ================== Namespaces

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Compilers;

#endregion

namespace CodeImp.DoomBuilder.Controls
{
	// This is a script tab for the global script lumps (ANIMDEFS, MAPINFO and SKYDEFS).
	// Unlike MACROS these are not part of a map. They are separate lumps in the PWAD that
	// replace the lump of the same name in engine memory. The text is loaded from the PWAD, or when the
	// PWAD does not have the lump, from the last loaded resource. Only when the text is changed,
	// the lump is added to the PWAD when the map is saved.
	internal sealed class ScriptGlobalLumpDocumentTab : ScriptDocumentTab
	{
		#region ================== Variables

		private string lumpname;

		// The text as it was loaded (or last stored). The editor control reports a change
		// for things that don't change the text, so the text is compared with this instead.
		private byte[] originaldata = new byte[0];

		#endregion

		#region ================== Properties

		public override bool ExplicitSave { get { return false; } }
		public override bool IsSaveAsRequired { get { return false; } }
		public override bool IsClosable { get { return false; } }
		public override bool IsReconfigurable { get { return false; } }
		public string LumpName { get { return lumpname; } }
		
		// Only changed when the text is really different from the original text
		public override bool IsChanged { get { return editor.IsChanged && !IsSameAsOriginal(); } }

		#endregion

		#region ================== Constructor / Disposer

		// Constructor
		public ScriptGlobalLumpDocumentTab(ScriptEditorPanel panel, string lumpname, ScriptConfiguration config) : base(panel)
		{
			this.lumpname = lumpname.ToUpperInvariant();
			this.config = config;
			editor.SetupStyles(config);

			// Load the lump data from the PWAD or the last loaded resource
			string source;
			byte[] data = TrimNulls(General.Map.GetGlobalLumpData(this.lumpname, out source));
			if(data.Length > 0)
			{
				// The editor needs the text to end with a zero
				byte[] terminated = new byte[data.Length + 1];
				Array.Copy(data, terminated, data.Length);
				editor.SetText(terminated);
				editor.ClearUndoRedo();
				originaldata = data;
				this.ToolTipText = this.lumpname + " from " + source;
			}
			else
			{
				this.ToolTipText = this.lumpname + " does not exist yet. It is added to the PWAD when you enter text.";
			}

			editor.IsChanged = false;
			SetTitle(this.lumpname);
		}

		#endregion

		#region ================== Methods

		// Implicit save: the lump is only stored when it was changed,
		// so an unchanged lump that came from a resource does not end up in the PWAD
		public override bool Save()
		{
			if(IsChanged)
			{
				byte[] text = GetEditorText();
				General.Map.SetGlobalLumpData(lumpname, text);
				originaldata = text;
			}
			editor.IsChanged = false;
			return true;
		}

		// This removes the zeros at the end of data (null becomes empty)
		private static byte[] TrimNulls(byte[] data)
		{
			if(data == null) return new byte[0];
			int length = data.Length;
			while((length > 0) && (data[length - 1] == 0)) length--;
			if(length == data.Length) return data;
			byte[] trimmed = new byte[length];
			Array.Copy(data, trimmed, length);
			return trimmed;
		}

		// This returns the text in the editor. The editor control returns the text with a zero at the end,
		// which is not part of the text, so it is removed.
		private byte[] GetEditorText()
		{
			return TrimNulls(editor.GetText());
		}

		// This compares the text in the editor with the original text
		private bool IsSameAsOriginal()
		{
			byte[] text = GetEditorText();
			if(text.Length != originaldata.Length) return false;
			for(int i = 0; i < text.Length; i++)
				if(text[i] != originaldata[i]) return false;
			return true;
		}

		#endregion
	}
}
