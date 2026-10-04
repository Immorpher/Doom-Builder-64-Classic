
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
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Data;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Collections.Specialized;

#endregion

namespace CodeImp.DoomBuilder.Config
{
	internal sealed class ResourceTextureSet : TextureSet, IFilledTextureSet
	{
		#region ================== Constants
		
		#endregion

		#region ================== Variables

		// Matching textures and flats
		private Dictionary<long, ImageData> textures;
		private Dictionary<long, ImageData> flats;
		private DataLocation location;

		// Doom 64 texture hashes (hash -> name) of the textures and flats in this set
		private Dictionary<uint, string> texturehashes;
		private Dictionary<uint, string> flathashes;

		#endregion

		#region ================== Properties
		
		public ICollection<ImageData> Textures { get { return textures.Values; } }
		public ICollection<ImageData> Flats { get { return flats.Values; } }
		public DataLocation Location { get { return location; } }
		
		#endregion

		#region ================== Constructor / Destructor

		// New texture set constructor
		public ResourceTextureSet(string name, DataLocation location)
		{
			this.name = name;
			this.location = location;
			this.textures = new Dictionary<long, ImageData>();
			this.flats = new Dictionary<long, ImageData>();
			this.texturehashes = new Dictionary<uint, string>();
			this.flathashes = new Dictionary<uint, string>();
		}
		
		#endregion

		#region ================== Methods
		
		// Add a texture
		internal void AddTexture(ImageData image)
		{
			CheckHashDuplicate(texturehashes, "Texture", image);
			textures[image.LongName] = image;
		}

		// Add a flat
		internal void AddFlat(ImageData image)
		{
			CheckHashDuplicate(flathashes, "Flat", image);
			flats[image.LongName] = image;
		}

		// Doom 64 maps reference textures by a 16 bit name hash, so two images are
		// duplicates when their hashes match (which includes identical names, but also
		// different names that collide). Warn when this image's hash is already taken.
		private void CheckHashDuplicate(Dictionary<uint, string> hashes, string kind, ImageData image)
		{
			uint hash = WADReader.GetTextureNameHash(image.Name);
			string existing;
			if(hashes.TryGetValue(hash, out existing))
			{
				if(string.Equals(existing, image.Name, StringComparison.OrdinalIgnoreCase))
					General.ErrorLogger.Add(ErrorType.Warning, kind + " \"" + image.Name + "\" is double defined (hash " + hash + ") in resource \"" + this.Location.location + "\".");
				else
					General.ErrorLogger.Add(ErrorType.Warning, kind + " \"" + image.Name + "\" has the same hash (" + hash + ") as " + kind.ToLower() + " \"" + existing + "\" in resource \"" + this.Location.location + "\". Maps cannot tell them apart.");
			}
			hashes[hash] = image.Name;
		}

		// Check if this set has a texture
		internal bool TextureExists(ImageData image)
		{
			return textures.ContainsKey(image.LongName);
		}

		// Check if this set has a flat
		internal bool FlatExists(ImageData image)
		{
			return flats.ContainsKey(image.LongName);
		}

		// Mix the textures and flats
		internal void MixTexturesAndFlats()
		{
			// Make a copy of the flats only
			Dictionary<long, ImageData> flatsonly = new Dictionary<long, ImageData>(flats);
			
			// Add textures to flats
			foreach(KeyValuePair<long, ImageData> t in textures)
			{
				if(!flats.ContainsKey(t.Key))
					flats.Add(t.Key, t.Value);
			}
			
			// Add flats to textures
			foreach(KeyValuePair<long, ImageData> f in flatsonly)
			{
				if(!textures.ContainsKey(f.Key))
					textures.Add(f.Key, f.Value);
			}
		}
		
		#endregion
	}
}
