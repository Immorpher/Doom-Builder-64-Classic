
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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.IO;
using System.IO;

#endregion

namespace CodeImp.DoomBuilder.Data
{
	public sealed class SpriteImage : ImageData
	{
		#region ================== Variables

		protected int offsetx;
		protected int offsety;
		
		#endregion

		#region ================== Properties

		public int OffsetX { get { return offsetx; } }
		public int OffsetY { get { return offsety; } }
		
		#endregion
		
		#region ================== Constructor / Disposer

		// Constructor
		internal SpriteImage(string name)
		{
			// Initialize
			SetName(name);

			// We have no destructor
			GC.SuppressFinalize(this);
		}

		#endregion

		#region ================== Methods

		private static readonly object lumpreadlock = new object();
		private static uint[] crctable;

		// CRC32 as used by PNG chunks
		private static uint PngCrc(byte[] data, int offset, int length)
		{
			if(crctable == null)
			{
				uint[] t = new uint[256];
				for(uint n = 0; n < 256; n++)
				{
					uint c = n;
					for(int k = 0; k < 8; k++)
						c = ((c & 1) != 0) ? (0xEDB88320u ^ (c >> 1)) : (c >> 1);
					t[n] = c;
				}
				crctable = t;
			}

			uint crc = 0xFFFFFFFFu;
			for(int i = offset; i < offset + length; i++)
				crc = crctable[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
			return crc ^ 0xFFFFFFFFu;
		}

		// This replaces the PLTE chunk of an indexed PNG (in place) with the given
		// palette and fixes the chunk CRC. Returns false when the data is not a PNG
		// with a palette chunk, in which case the data is left untouched.
		private static bool ReplacePngPalette(byte[] png, Playpal pal)
		{
			// PNG signature
			if(png.Length < 20 || png[0] != 0x89 || png[1] != 0x50 || png[2] != 0x4E || png[3] != 0x47)
				return false;

			int pos = 8;
			while(pos + 12 <= png.Length)
			{
				int len = (png[pos] << 24) | (png[pos + 1] << 16) | (png[pos + 2] << 8) | png[pos + 3];
				if(len < 0 || pos + 12 + len > png.Length) return false;

				char t0 = (char)png[pos + 4], t1 = (char)png[pos + 5], t2 = (char)png[pos + 6], t3 = (char)png[pos + 7];
				if(t0 == 'P' && t1 == 'L' && t2 == 'T' && t3 == 'E')
				{
					int count = Math.Min(len / 3, 256);
					for(int i = 0; i < count; i++)
					{
						png[pos + 8 + i * 3] = pal[i].r;
						png[pos + 9 + i * 3] = pal[i].g;
						png[pos + 10 + i * 3] = pal[i].b;
					}

					uint crc = PngCrc(png, pos + 4, len + 4);
					png[pos + 8 + len] = (byte)(crc >> 24);
					png[pos + 9 + len] = (byte)(crc >> 16);
					png[pos + 10 + len] = (byte)(crc >> 8);
					png[pos + 11 + len] = (byte)crc;
					return true;
				}

				// PLTE must come before the image data
				if(t0 == 'I' && t1 == 'D' && t2 == 'A' && t3 == 'T') return false;

				pos += 12 + len;
			}

			return false;
		}

		// This loads the image
		protected override void LocalLoadImage()
		{
			Stream lumpdata;
			MemoryStream mem;
			IImageReader reader;
			byte[] membytes;

			// Leave when already loaded
			if(this.IsImageLoaded) return;

			lock(this)
			{
				// Get the lump data stream
				lumpdata = General.Map.Data.GetSpriteData(Name);
				if(lumpdata != null)
				{
					// Copy lump data to memory. Different sprite images (such as palette variants)
					// can share the same lump stream and may be loaded by different threads at the
					// same time, so only one of them can read from a lump stream at a time.
					lock(lumpreadlock)
					{
						lumpdata.Seek(0, SeekOrigin.Begin);
						membytes = new byte[(int)lumpdata.Length];
						lumpdata.Read(membytes, 0, (int)lumpdata.Length);
					}
					// Doom 64 palette swap (Nightmare Imp, Spectre, Player 2-4, etc.):
					// sprites are indexed PNGs, so swap in the alternate palette lump
					// directly. This is exact, unlike guessing indexes from RGB values.
					palettebaked = false;
					if(PalIndex > 0 && General.Map != null && General.Map.Data != null)
					{
						Playpal altpal = General.Map.Data.GetThingPaletteByIndex(PalIndex);
						if(altpal != null) palettebaked = ReplacePngPalette(membytes, altpal);
					}

					mem = new MemoryStream(membytes);
					mem.Seek(0, SeekOrigin.Begin);

					// Get a reader for the data
					reader = ImageDataFormat.GetImageReader(mem, ImageDataFormat.DOOMPICTURE, General.Map.Data.Palette);
					if(reader is UnknownImageReader)
					{
						// Data is in an unknown format!
						General.ErrorLogger.Add(ErrorType.Error, "Sprite lump '" + Name + "' data format could not be read. Does this lump contain valid picture data at all?");
						bitmap = null;
					}
					else
					{
						// Read data as bitmap
						mem.Seek(0, SeekOrigin.Begin);
						if(bitmap != null) bitmap.Dispose();
						bitmap = reader.ReadAsBitmap(mem, out offsetx, out offsety);
					}
					
					// Done
					mem.Dispose();

					if(bitmap != null)
					{
						// Get width and height from image
						width = bitmap.Size.Width;
						height = bitmap.Size.Height;
						scale.x = 1.0f;
						scale.y = 1.0f;
						
						// Make offset corrections if the offset was not given
						if((offsetx == int.MinValue) || (offsety == int.MinValue))
						{
							offsetx = (int)((width * scale.x) * 0.5f);
							offsety = (int)(height * scale.y);
						}
					}
					else
					{
						loadfailed = true;
					}
				}
				else
				{
					// Missing a patch lump!
					General.ErrorLogger.Add(ErrorType.Error, "Missing sprite lump '" + Name + "'. Forgot to include required resources?");
				}

				// Pass on to base
				base.LocalLoadImage();
			}
		}

		#endregion
	}
}
