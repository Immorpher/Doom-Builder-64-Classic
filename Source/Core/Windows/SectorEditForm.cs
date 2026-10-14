
#region ================== Copyright (c) 2007 Pascal vd Heiden

/*
 * Copyright (c) 2007 Pascal vd Heiden, www.codeimp.com
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.IO;
using System.IO;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.Controls;
using CodeImp.DoomBuilder.Rendering; // villsa

#endregion

namespace CodeImp.DoomBuilder.Windows
{
	internal partial class SectorEditForm : DelayedForm
	{
        // Variables
        private ICollection<Sector> sectors;
        private PixelColor color;
        private PixelColor[] initialcolor;
        private int[] initialtags;   // styd: remembers the tag of each color on load
        private string[] initialindex;   // styd: remembers the displayed physical LIGHTS index of each color on load
        private List<float>[] intensitysteps;   // styd: sequence of clicks +/- per slot (0=Ceiling, 1=Top, 2=Thing, 3=Lower, 4=Floor)
        private PixelColor[] expectedcolor;   // styd: expected color if only following the +/- clicks, without manual adjustment
        private const float LIGHTINCVALUE = 0.235f;
        private const float LIGHTDECVALUE = -0.1825f;

        // Constructor
        public SectorEditForm()
		{
			// Initialize
			InitializeComponent();

            color = new PixelColor(255, 255, 255, 255);
            initialcolor = new PixelColor[Sector.NUM_COLORS];
            initialtags = new int[Sector.NUM_COLORS];   // styd
            initialindex = new string[Sector.NUM_COLORS];   // styd

            // styd
            intensitysteps = new List<float>[Sector.NUM_COLORS];
            for (int i = 0; i < Sector.NUM_COLORS; i++)
                intensitysteps[i] = new List<float>();

            expectedcolor = new PixelColor[Sector.NUM_COLORS];   // styd

            // Fill effects list
            effect.AddInfo(General.Map.Config.SortedSectorEffects.ToArray());

            // villsa
            if (General.Map.FormatInterface.InDoom64Mode)
            {
                // Fill flags list
                foreach (KeyValuePair<string, string> lf in General.Map.Config.SectorFlags)
                    flags.Add(lf.Value, lf.Key);

                brightness.Hide();
                label9.Hide();
                groupeffect.Height = 64;
            }
			
			// Fill universal fields list
			fieldslist.ListFixedFields(General.Map.Config.SectorFields);

			// Initialize image selectors
			floortex.Initialize();
			ceilingtex.Initialize();

			// Set steps for brightness field
			brightness.StepValues = General.Map.Config.BrightnessLevels;

			// Custom fields?
			bool hascustomfields = General.Map.FormatInterface.HasCustomFields;
			customgroup.Visible = hascustomfields;

            // villsa
            bool hascolors = General.Map.FormatInterface.InDoom64Mode;
            groupBox1.Visible = hascolors;
            groupBox3.Visible = hascolors;

			// Initialize custom fields editor
			fieldslist.Setup("sector");

			// Arrange the single-screen layout.
			int gap = 6;
			groupeffect.Top = groupfloorceiling.Bottom + gap;
			int rightbottom = groupeffect.Bottom;
			if(hascolors)
			{
				groupBox1.Top = groupBox3.Top = rightbottom + gap;
				rightbottom = groupBox1.Bottom;
			}
			if(hascustomfields)
			{
				customgroup.Top = rightbottom + gap;
				rightbottom = customgroup.Bottom;
			}
			groupaction.Top = rightbottom - groupaction.Height;
			settingsgroup.Height = Math.Max(groupaction.Top - gap - settingsgroup.Top, 100);
			flags.Height = settingsgroup.Height - flags.Top - 8;
			this.ClientSize = new Size(this.ClientSize.Width, rightbottom + 10 + cancel.Height + 10);
			cancel.Top = apply.Top = this.ClientSize.Height - 10 - cancel.Height;
		}
		
		// This sets up the form to edit the given sectors
		public void Setup(ICollection<Sector> sectors)
		{
			Sector sc;
			
			// Keep this list
			this.sectors = sectors;
			if(sectors.Count > 1) this.Text = "Edit Sectors (" + sectors.Count + ")";
			else if(sectors.Count == 1) this.Text = "Edit Sector (Index: " + General.GetByIndex(sectors, 0).Index + ")";

			////////////////////////////////////////////////////////////////////////
			// Set all options to the first sector properties
			////////////////////////////////////////////////////////////////////////

			// Get first sector
			sc = General.GetByIndex(sectors, 0);

            if (General.Map.FormatInterface.InDoom64Mode)
            {
                // villsa - Flags
                foreach (CheckBox c in flags.Checkboxes)
                    if (sc.Flags.ContainsKey(c.Tag.ToString())) c.Checked = sc.Flags[c.Tag.ToString()];

                ceilingcolor.Color = initialcolor[0] = sc.CeilColor.color;
                topcolor.Color = initialcolor[1] = sc.TopColor.color;
                thingcolor.Color = initialcolor[2] = sc.ThingColor.color;
                lowercolor.Color = initialcolor[3] = sc.LowerColor.color;
                floorcolor.Color = initialcolor[4] = sc.FloorColor.color;

                // styd: load tags by color
                ceilingcolortag.Text = (initialtags[0] = sc.CeilColor.tag).ToString();
                topcolortag.Text = (initialtags[1] = sc.TopColor.tag).ToString();
                thingcolortag.Text = (initialtags[2] = sc.ThingColor.tag).ToString();
                lowercolortag.Text = (initialtags[3] = sc.LowerColor.tag).ToString();
                floorcolortag.Text = (initialtags[4] = sc.FloorColor.tag).ToString();

                // styd: load the physical LIGHTS index by color, matching DEX Editor's
                // "Light NNN" field
                ceilingcolor.IndexText = initialindex[0] = Lights.GetDisplayIndex(sc.CeilColor);
                topcolor.IndexText = initialindex[1] = Lights.GetDisplayIndex(sc.TopColor);
                thingcolor.IndexText = initialindex[2] = Lights.GetDisplayIndex(sc.ThingColor);
                lowercolor.IndexText = initialindex[3] = Lights.GetDisplayIndex(sc.LowerColor);
                floorcolor.IndexText = initialindex[4] = Lights.GetDisplayIndex(sc.FloorColor);

                // styd: start from scratch each time the window is opened
                foreach (List<float> l in intensitysteps) l.Clear();
                for (int i = 0; i < Sector.NUM_COLORS; i++)
                    expectedcolor[i] = initialcolor[i];   // styd
            }

			// Effects
			effect.Value = sc.Effect;
			brightness.Text = sc.Brightness.ToString();

			// Floor/ceiling
			floorheight.Text = sc.FloorHeight.ToString();
			ceilingheight.Text = sc.CeilHeight.ToString();
			floortex.TextureName = sc.FloorTexture;
			ceilingtex.TextureName = sc.CeilTexture;

			// Action
			tag.Text = sc.Tag.ToString();

			// Custom fields
			fieldslist.SetValues(sc.Fields, true);
			
			////////////////////////////////////////////////////////////////////////
			// Now go for all sectors and change the options when a setting is different
			////////////////////////////////////////////////////////////////////////

			// Go for all sectors
			foreach(Sector s in sectors)
			{
                // Flags
                foreach (CheckBox c in flags.Checkboxes)
                {
                    if (s.Flags.ContainsKey(c.Tag.ToString()))
                    {
                        if (s.Flags[c.Tag.ToString()] != c.Checked)
                        {
                            c.ThreeState = true;
                            c.CheckState = CheckState.Indeterminate;
                        }
                    }
                }

				// Effects
				if(s.Effect != effect.Value) effect.Empty = true;
				if(s.Brightness.ToString() != brightness.Text) brightness.Text = "";

				// Floor/Ceiling
				if(s.FloorHeight.ToString() != floorheight.Text) floorheight.Text = "";
				if(s.CeilHeight.ToString() != ceilingheight.Text) ceilingheight.Text = "";
				if(s.FloorTexture != floortex.TextureName) floortex.TextureName = "";
				if(s.CeilTexture != ceilingtex.TextureName) ceilingtex.TextureName = "";

				// Action
				if(s.Tag.ToString() != tag.Text) tag.Text = "";

				// Custom fields
				fieldslist.SetValues(s.Fields, false);
			}

			// Show sector height
			UpdateSectorHeight();
		}

		// This updates the sector height field
		private void UpdateSectorHeight()
		{
			bool showheight = true;
			int delta = 0;
			Sector first = null;
			
			// Check all selected sectors
			foreach(Sector s in sectors)
			{
				if(first == null)
				{
					// First sector in list
					delta = s.CeilHeight - s.FloorHeight;
					showheight = true;
					first = s;
				}
				else
				{
					if(delta != (s.CeilHeight - s.FloorHeight))
					{
						// We can't show heights because the delta
						// heights for the sectors is different
						showheight = false;
						break;
					}
				}
			}

			if(showheight)
			{
				int fh = floorheight.GetResult(first.FloorHeight);
				int ch = ceilingheight.GetResult(first.CeilHeight);
				int height = ch - fh;
				sectorheight.Text = height.ToString();
				sectorheight.Visible = true;
				sectorheightlabel.Visible = true;
			}
			else
			{
				sectorheight.Visible = false;
				sectorheightlabel.Visible = false;
			}
		}

        // styd: applies color (absolute OR relative via intensitysteps) + tag + physical
        // index to a slot, preserving isDirect for untouched slots, and allows combining
        // +/- clicks AND manual retouching in the same edit
        private void ApplyColorSlot(Lights current, ColorControlSector colorctrl, ButtonsNumericTextbox tagctrl,
            PixelColor initcolor, int inittag, string initindex,
            List<float> steps, PixelColor expected, Action<Lights> setter)
        {
            int newtag = tagctrl.GetResult(inittag);
            bool tagchanged = (newtag != inittag);
            bool colorchanged = (initcolor.ToColor() != colorctrl.Color.ToColor());
            bool hasintensitysteps = (steps.Count > 0);
            string newindextext = colorctrl.IndexText.Trim();
            bool indexchanged = (newindextext != initindex);

            // styd: Did we follow the exact sequence of clicks, without any manual retouching afterwards?
            bool followedstepsonly = hasintensitysteps && (colorctrl.Color.ToColor() == expected.ToColor());

            if (!colorchanged && !tagchanged && !indexchanged)
                return;

            Lights light = current;   // share of values ​​specific to this sector

            if (followedstepsonly)
            {
                // styd: replays the sequence related to the specific color of this sector
                foreach (float step in steps)
                    light.SetIntensity(step);
            }
            else if (colorchanged)
            {
                // styd: explicit final color (direct pick, or retouching after +/- clicks)
                light.color = colorctrl.Color;
            }

            if (tagchanged)
            {
                light.tag = (short)General.Clamp(newtag, General.Map.FormatInterface.MinTag, General.Map.FormatInterface.MaxTag);
                if (light.tag != 0) light.isDirect = false;
            }

            // styd: manual physical LIGHTS index editing, reproducing DEX Editor's "Light
            // NNN" field. See AddLightGetIndex() in Doom64MapSetIO.cs for how a requested
            // index >= 256 is honored on save (only actually shared if the RGB+tag also
            // match another entry at that same original position - typing a number here
            // does not by itself copy the color from that slot).
            if (indexchanged)
            {
                int newindex;
                if (newindextext.Length == 0)
                {
                    // Cleared - stop requesting a specific shared slot, fall back to the
                    // automatic dedup rules in AddLightGetIndex() at save time
                    light.hasOriginalIndex = false;
                }
                else if (int.TryParse(newindextext, out newindex))
                {
                    if (newindex >= 256)
                    {
                        light.hasOriginalIndex = true;
                        light.originalIndex = newindex - 256;
                    }
                    else
                    {
                        // Direct grayscale index, exactly like DEX: the number itself is
                        // the gray value
                        byte g = (byte)General.Clamp(newindex, 0, 255);
                        light.color = new PixelColor(255, g, g, g);
                        light.tag = 0;
                        light.isDirect = true;
                        light.hasOriginalIndex = false;
                    }
                }
            }

            setter(light);
        }

        // OK clicked
        private void apply_Click(object sender, EventArgs e)
		{
			string undodesc = "sector";
			
			// Verify the tag
			if((tag.GetResult(0) < General.Map.FormatInterface.MinTag) || (tag.GetResult(0) > General.Map.FormatInterface.MaxTag))
			{
				General.ShowWarningMessage("Sector tag must be between " + General.Map.FormatInterface.MinTag + " and " + General.Map.FormatInterface.MaxTag + ".", MessageBoxButtons.OK);
				return;
			}

			// Verify the effect
			if((effect.Value < General.Map.FormatInterface.MinEffect) || (effect.Value > General.Map.FormatInterface.MaxEffect))
			{
				General.ShowWarningMessage("Sector effect must be between " + General.Map.FormatInterface.MinEffect + " and " + General.Map.FormatInterface.MaxEffect + ".", MessageBoxButtons.OK);
				return;
			}

			// Verify the brightness
			if((brightness.GetResult(0) < General.Map.FormatInterface.MinBrightness) || (brightness.GetResult(0) > General.Map.FormatInterface.MaxBrightness))
			{
				General.ShowWarningMessage("Sector brightness must be between " + General.Map.FormatInterface.MinBrightness + " and " + General.Map.FormatInterface.MaxBrightness + ".", MessageBoxButtons.OK);
				return;
			}
			
			// Make undo
			if(sectors.Count > 1) undodesc = sectors.Count + " sectors";
			General.Map.UndoRedo.CreateUndo("Edit " + undodesc);

			// Go for all sectors
			foreach(Sector s in sectors)
			{
                // villsa - Apply all flags
                if (General.Map.FormatInterface.InDoom64Mode)
                {
                    // flags
                    foreach (CheckBox c in flags.Checkboxes)
                    {
                        if (c.CheckState == CheckState.Checked) s.SetFlag(c.Tag.ToString(), true);
                        else if (c.CheckState == CheckState.Unchecked) s.SetFlag(c.Tag.ToString(), false);
                    }

                    //
                    // color lights + tags
                    //

                    ApplyColorSlot(s.CeilColor, ceilingcolor, ceilingcolortag, initialcolor[0], initialtags[0], initialindex[0], intensitysteps[0], expectedcolor[0], v => s.CeilColor = v);
                    ApplyColorSlot(s.TopColor, topcolor, topcolortag, initialcolor[1], initialtags[1], initialindex[1], intensitysteps[1], expectedcolor[1], v => s.TopColor = v);
                    ApplyColorSlot(s.ThingColor, thingcolor, thingcolortag, initialcolor[2], initialtags[2], initialindex[2], intensitysteps[2], expectedcolor[2], v => s.ThingColor = v);
                    ApplyColorSlot(s.LowerColor, lowercolor, lowercolortag, initialcolor[3], initialtags[3], initialindex[3], intensitysteps[3], expectedcolor[3], v => s.LowerColor = v);
                    ApplyColorSlot(s.FloorColor, floorcolor, floorcolortag, initialcolor[4], initialtags[4], initialindex[4], intensitysteps[4], expectedcolor[4], v => s.FloorColor = v);
                }

				// Effects
				if(!effect.Empty) s.Effect = effect.Value;
				s.Brightness = General.Clamp(brightness.GetResult(s.Brightness), General.Map.FormatInterface.MinBrightness, General.Map.FormatInterface.MaxBrightness);

				// Floor/Ceiling
				s.FloorHeight = floorheight.GetResult(s.FloorHeight);
				s.CeilHeight = ceilingheight.GetResult(s.CeilHeight);
				s.SetFloorTexture(floortex.GetResult(s.FloorTexture));
				s.SetCeilTexture(ceilingtex.GetResult(s.CeilTexture));

				// Action
				s.Tag = General.Clamp(tag.GetResult(s.Tag), General.Map.FormatInterface.MinTag, General.Map.FormatInterface.MaxTag);

				// Custom fields
				fieldslist.Apply(s.Fields);
			}
			
			// Update the used textures
			General.Map.Data.UpdateUsedTextures();
			
			// Done
			General.Map.IsChanged = true;
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		// Cancel clicked
		private void cancel_Click(object sender, EventArgs e)
		{
			// Be gone
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		// This finds a new (unused) tag
		private void newtag_Click(object sender, EventArgs e)
		{
			tag.Text = General.Map.Map.GetNewTag().ToString();
		}

        // This returns the lights (color slots) of a sector by slot index
        // (0=Ceiling, 1=Top, 2=Thing, 3=Lower, 4=Floor)
        private static Lights GetSlotLight(Sector s, int slot)
        {
            switch(slot)
            {
                case 0: return s.CeilColor;
                case 1: return s.TopColor;
                case 2: return s.ThingColor;
                case 3: return s.LowerColor;
                default: return s.FloorColor;
            }
        }

        // This finds a new tag for a light slot that is not used by any other light
        // (in other sectors, in the other slots of the sectors being edited, or in the
        // other light tag boxes of this window), nor by any regular sector/linedef/thing tag
        private int GetNewLightTag(int slot)
        {
            ButtonsNumericTextbox[] tagboxes = new ButtonsNumericTextbox[] { ceilingcolortag, topcolortag, thingcolortag, lowercolortag, floorcolortag };
            List<int> usedlighttags = new List<int>();
            HashSet<Sector> editing = new HashSet<Sector>(sectors);

            // Lights of the sectors that are not being edited
            foreach(Sector s in General.Map.Map.Sectors)
            {
                if(editing.Contains(s)) continue;
                for(int i = 0; i < Sector.NUM_COLORS; i++)
                    usedlighttags.Add(GetSlotLight(s, i).tag);
            }

            // Lights of the sectors being edited (except the slot we are replacing)
            for(int i = 0; i < Sector.NUM_COLORS; i++)
            {
                if(i == slot) continue;

                int boxtag = tagboxes[i].GetResult(initialtags[i]);
                if(boxtag != initialtags[i])
                {
                    // This tag will be applied to all selected sectors
                    usedlighttags.Add(boxtag);
                }
                else
                {
                    // Untouched, so the sectors keep their own tags
                    foreach(Sector s in sectors)
                        usedlighttags.Add(GetSlotLight(s, i).tag);
                }
            }

            return General.Map.Map.GetNewTag(usedlighttags);
        }

        // styd: same "find a new unused tag" behavior as the sector's own New Tag button,
        // applied per color (also avoiding the tags used by other lights)
        private void ceilingcolornewtag_Click(object sender, EventArgs e)
        {
            ceilingcolortag.Text = GetNewLightTag(0).ToString();
        }

        private void topcolornewtag_Click(object sender, EventArgs e)
        {
            topcolortag.Text = GetNewLightTag(1).ToString();
        }

        private void thingcolornewtag_Click(object sender, EventArgs e)
        {
            thingcolortag.Text = GetNewLightTag(2).ToString();
        }

        private void lowercolornewtag_Click(object sender, EventArgs e)
        {
            lowercolortag.Text = GetNewLightTag(3).ToString();
        }

        private void floorcolornewtag_Click(object sender, EventArgs e)
        {
            floorcolortag.Text = GetNewLightTag(4).ToString();
        }

        // Browse Effect clicked
        private void browseeffect_Click(object sender, EventArgs e)
		{
			effect.Value = EffectBrowserForm.BrowseEffect(this, effect.Value);
		}

		// Ceiling height changes
		private void ceilingheight_TextChanged(object sender, EventArgs e)
		{
			UpdateSectorHeight();
		}

		// Floor height changes
		private void floorheight_TextChanged(object sender, EventArgs e)
		{
			UpdateSectorHeight();
		}

		// Help
		private void SectorEditForm_HelpRequested(object sender, HelpEventArgs hlpevent)
		{
			General.ShowHelp("w_sectoredit.html");
			hlpevent.Handled = true;
		}

        // Ceiling +
        private void button1_Click(object sender, EventArgs e)
        {
            Lights light = new Lights(ceilingcolor.Color.r, ceilingcolor.Color.g, ceilingcolor.Color.b, 0);
            light.SetIntensity(LIGHTINCVALUE);
            ceilingcolor.Color = light.color;
            intensitysteps[0].Add(LIGHTINCVALUE); // styd
            expectedcolor[0] = light.color;   // styd
        }

        // Top +
        private void button4_Click(object sender, EventArgs e)
        {
            Lights light = new Lights(topcolor.Color.r, topcolor.Color.g, topcolor.Color.b, 0);
            light.SetIntensity(LIGHTINCVALUE);
            topcolor.Color = light.color;
            intensitysteps[1].Add(LIGHTINCVALUE); // styd
            expectedcolor[1] = light.color;   // styd
        }

        // Thing +
        private void button16_Click(object sender, EventArgs e)
        {
            Lights light = new Lights(thingcolor.Color.r, thingcolor.Color.g, thingcolor.Color.b, 0);
            light.SetIntensity(LIGHTINCVALUE);
            thingcolor.Color = light.color;
            intensitysteps[2].Add(LIGHTINCVALUE); // styd
            expectedcolor[2] = light.color;   // styd
        }

        // Lower +
        private void button18_Click(object sender, EventArgs e)
        {
            Lights light = new Lights(lowercolor.Color.r, lowercolor.Color.g, lowercolor.Color.b, 0);
            light.SetIntensity(LIGHTINCVALUE);
            lowercolor.Color = light.color;
            intensitysteps[3].Add(LIGHTINCVALUE); // styd
            expectedcolor[3] = light.color;   // styd
        }

        // Floor +
        private void button20_Click(object sender, EventArgs e)
        {
            Lights light = new Lights(floorcolor.Color.r, floorcolor.Color.g, floorcolor.Color.b, 0);
            light.SetIntensity(LIGHTINCVALUE);
            floorcolor.Color = light.color;
            intensitysteps[4].Add(LIGHTINCVALUE); // styd
            expectedcolor[4] = light.color;   // styd
        }

        // Ceiling -
        private void button2_Click(object sender, EventArgs e)
        {
            Lights light = new Lights(ceilingcolor.Color.r, ceilingcolor.Color.g, ceilingcolor.Color.b, 0);
            light.SetIntensity(LIGHTDECVALUE);
            ceilingcolor.Color = light.color;
            intensitysteps[0].Add(LIGHTDECVALUE); // styd
            expectedcolor[0] = light.color;   // styd
        }

        // Top -
        private void button3_Click(object sender, EventArgs e)
        {
            Lights light = new Lights(topcolor.Color.r, topcolor.Color.g, topcolor.Color.b, 0);
            light.SetIntensity(LIGHTDECVALUE);
            topcolor.Color = light.color;
            intensitysteps[1].Add(LIGHTDECVALUE); // styd
            expectedcolor[1] = light.color;   // styd
        }

        // Thing -
        private void button5_Click(object sender, EventArgs e)
        {
            Lights light = new Lights(thingcolor.Color.r, thingcolor.Color.g, thingcolor.Color.b, 0);
            light.SetIntensity(LIGHTDECVALUE);
            thingcolor.Color = light.color;
            intensitysteps[2].Add(LIGHTDECVALUE); // styd
            expectedcolor[2] = light.color;   // styd
        }

        // Lower -
        private void button17_Click(object sender, EventArgs e)
        {
            Lights light = new Lights(lowercolor.Color.r, lowercolor.Color.g, lowercolor.Color.b, 0);
            light.SetIntensity(LIGHTDECVALUE);
            lowercolor.Color = light.color;
            intensitysteps[3].Add(LIGHTDECVALUE); // styd
            expectedcolor[3] = light.color;   // styd
        }

        // Floor -
        private void button19_Click(object sender, EventArgs e)
        {
            Lights light = new Lights(floorcolor.Color.r, floorcolor.Color.g, floorcolor.Color.b, 0);
            light.SetIntensity(LIGHTDECVALUE);
            floorcolor.Color = light.color;
            intensitysteps[4].Add(LIGHTDECVALUE); // styd
            expectedcolor[4] = light.color;   // styd
        }
    }
}
