
#region ================== Namespaces

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CodeImp.DoomBuilder.Config;

#endregion

namespace CodeImp.DoomBuilder.IO
{
	// Support for "WADs within a WAD", as used by the Doom 64 IWAD:
	// every map is stored as a lump named MAPxx, and the data of that lump
	// is a complete WAD file which holds exactly one map (marker + map lumps).
	internal static class NestedWad
	{
		#region ================== Constants

		// Lump names of nested map WADs must start with this
		public const string MAP_PREFIX = "MAP";

		// WAD header size, and lump table entry size
		private const int HEADER_SIZE = 12;
		private const int ENTRY_SIZE = 16;

		// Alignment of lump data in written WAD files (matches the Doom 64 IWAD)
		private const int ALIGNMENT = 4;

		#endregion

		#region ================== Detection

		// This checks if the lump name is eligible to be a nested map WAD
		public static bool HasMapPrefix(string lumpname)
		{
			return (lumpname != null) && lumpname.ToUpperInvariant().StartsWith(MAP_PREFIX);
		}

		// This checks if the lump data is a valid WAD file and returns the lump names
		// inside it (in order). Returns null when the lump is not a valid WAD.
		public static List<string> ReadLumpNames(Lump lump)
		{
			string type;
			return ReadLumpNames(lump, out type);
		}

		// Same as above, but also returns the WAD type (IWAD or PWAD)
		public static List<string> ReadLumpNames(Lump lump, out string type)
		{
			type = null;

			// Must be big enough to have a header
			if((lump == null) || (lump.Length < HEADER_SIZE)) return null;

			try
			{
				// Read the header
				lump.Stream.Seek(0, SeekOrigin.Begin);
				BinaryReader reader = new BinaryReader(lump.Stream, WAD.ENCODING);
				string wadtype = WAD.ENCODING.GetString(reader.ReadBytes(4));
				if((wadtype != WAD.TYPE_IWAD) && (wadtype != WAD.TYPE_PWAD)) return null;
				int numlumps = reader.ReadInt32();
				int tableoffset = reader.ReadInt32();

				// Validate the header against the size of the lump
				if((numlumps < 1) || (numlumps > 65535)) return null;
				if(tableoffset < HEADER_SIZE) return null;
				if(((long)tableoffset + ((long)numlumps * ENTRY_SIZE)) > (long)lump.Length) return null;

				// Read the lump table
				List<string> names = new List<string>(numlumps);
				lump.Stream.Seek(tableoffset, SeekOrigin.Begin);
				for(int i = 0; i < numlumps; i++)
				{
					reader.ReadInt32();		// offset
					reader.ReadInt32();		// length
					byte[] fixedname = reader.ReadBytes(8);
					if(fixedname.Length < 8) return null;
					names.Add(Lump.MakeNormalName(fixedname, WAD.ENCODING));
				}

				type = wadtype;
				return names;
			}
			catch(Exception)
			{
				// Not readable as a WAD
				return null;
			}
		}

		// This finds the marker lump of the (first) map in a list of lump names, using the
		// same rules as the "Open Map" dialog: a lump that is not a known map lump, followed by
		// enough recognized map lumps to satisfy all the required ones.
		// Returns null when no map is found.
		public static string FindMapMarker(List<string> lumpnames, Configuration cfg)
		{
			if((lumpnames == null) || (cfg == null)) return null;

			IDictionary maplumpnames = cfg.ReadSetting("maplumpnames", new Hashtable());

			// Count how many required lumps we have to find
			int lumpsrequired = 0;
			foreach(DictionaryEntry ml in maplumpnames)
			{
				// Ignore the map header (it will not be found because the name is different)
				if(ml.Key.ToString() != MapManager.CONFIG_MAP_HEADER)
				{
					if(cfg.ReadSetting("maplumpnames." + ml.Key + ".required", false)) lumpsrequired++;
				}
			}

			// Go for all the lumps
			for(int scanindex = 0; scanindex < (lumpnames.Count - 1); scanindex++)
			{
				// Make sure this lump is not part of the map
				if(!maplumpnames.Contains(lumpnames[scanindex]))
				{
					int lumpsfound = 0;
					int checkoffset = 1;

					// Continue while still within bounds and lumps are still recognized
					while(((scanindex + checkoffset) < lumpnames.Count) &&
						  maplumpnames.Contains(lumpnames[scanindex + checkoffset]))
					{
						// Count the lump when it is marked as required
						string lumpname = lumpnames[scanindex + checkoffset];
						if(cfg.ReadSetting("maplumpnames." + lumpname + ".required", false)) lumpsfound++;
						checkoffset++;
					}

					// Map found?
					if(lumpsfound >= lumpsrequired) return lumpnames[scanindex];
				}
			}

			return null;
		}

		// This checks if the lump is a nested WAD (named MAPxx) that holds a map.
		// Returns the name of the map marker inside the nested WAD, or null when it is not a map.
		public static string FindMapMarker(Lump lump, Configuration cfg)
		{
			if((lump == null) || !HasMapPrefix(lump.Name)) return null;
			return FindMapMarker(ReadLumpNames(lump), cfg);
		}

		// This finds a lump by name in a WAD that is a nested map WAD.
		// Returns null when not found or when the lump is not a nested map.
		public static Lump FindNestedMapLump(WAD wad, string lumpname, Configuration cfg, out string markername)
		{
			markername = null;
			if(!HasMapPrefix(lumpname)) return null;

			Lump lump = wad.FindLump(lumpname);
			if(lump == null) return null;

			markername = FindMapMarker(lump, cfg);
			if(markername == null) return null;
			return lump;
		}

		#endregion

		#region ================== Extract

		// This copies the data of a lump to a file on disk
		public static void ExtractLump(Lump lump, string filename)
		{
			byte[] data = lump.Stream.ReadAllBytes();
			File.WriteAllBytes(filename, data);
		}

		#endregion

		#region ================== Write

		// This builds a WAD file in memory from all the lumps of the given WAD.
		// The WAD type is the type as it will be written in the header.
		public static byte[] Serialize(WAD source, string type)
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter writer = new BinaryWriter(ms, WAD.ENCODING);
			List<Lump> lumps = source.Lumps;
			int[] offsets = new int[lumps.Count];

			// Header (table offset is written later)
			writer.Write(WAD.ENCODING.GetBytes(type));
			writer.Write(lumps.Count);
			writer.Write((int)0);

			// Lump data
			for(int i = 0; i < lumps.Count; i++)
			{
				Align(ms, writer);
				offsets[i] = (int)ms.Position;
				if(lumps[i].Length > 0) writer.Write(lumps[i].Stream.ReadAllBytes());
			}

			// Lump table
			Align(ms, writer);
			int tableoffset = (int)ms.Position;
			for(int i = 0; i < lumps.Count; i++)
			{
				writer.Write(offsets[i]);
				writer.Write(lumps[i].Length);
				writer.Write(lumps[i].FixedName, 0, 8);
			}

			// Write the table offset in the header
			ms.Seek(8, SeekOrigin.Begin);
			writer.Write(tableoffset);
			writer.Flush();

			return ms.ToArray();
		}

		// This writes zeros until the position is aligned
		private static void Align(MemoryStream ms, BinaryWriter writer)
		{
			while((ms.Position % ALIGNMENT) != 0) writer.Write((byte)0);
		}

		// This writes a nested map WAD (as the data of a lump) into the WAD file 'targetfile'.
		//
		// - When the target file contains a lump named 'oldname' that is a nested WAD, that lump is
		//   replaced (and renamed to 'newname') and the WAD type of the existing nested WAD is kept.
		// - When the target file does not exist, or has no lump 'oldname', the lump is added at the end.
		// - When the target file has a lump 'oldname' that is NOT a nested WAD, an IOException is thrown
		//   and the target file is not touched.
		//
		// 'nestedwad' is the complete data of the nested WAD, 'defaulttype' is the type (IWAD/PWAD)
		// that is used when there is no existing nested WAD to take the type from.
		// The target file is rewritten completely, all other lumps and the type of the target
		// WAD are preserved.
		public static void WriteMapLump(string targetfile, string oldname, string newname, byte[] nestedwad, string defaulttype)
		{
			string tempfile = targetfile + ".nestedtmp";
			long longold = Lump.MakeLongName(oldname);

			// No existing file?
			if(!File.Exists(targetfile) || (new FileInfo(targetfile).Length < HEADER_SIZE))
			{
				// Create a brand new WAD with just this lump
				SetType(nestedwad, defaulttype);
				List<byte[]> newnames = new List<byte[]>();
				List<byte[]> newdata = new List<byte[]>();
				newnames.Add(Lump.MakeFixedName(newname, WAD.ENCODING));
				newdata.Add(nestedwad);
				WriteWadFile(tempfile, WAD.TYPE_PWAD, newnames, newdata);
				if(File.Exists(targetfile)) File.Delete(targetfile);
				File.Move(tempfile, targetfile);
				return;
			}

			string sourcetype;
			List<byte[]> names = new List<byte[]>();
			List<byte[]> datas = new List<byte[]>();

			// Read the entire existing WAD
			using(FileStream fs = new FileStream(targetfile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				BinaryReader reader = new BinaryReader(fs, WAD.ENCODING);
				sourcetype = WAD.ENCODING.GetString(reader.ReadBytes(4));
				int numlumps = reader.ReadInt32();
				int tableoffset = reader.ReadInt32();
				if((sourcetype != WAD.TYPE_IWAD) && (sourcetype != WAD.TYPE_PWAD)) throw new IOException("The target file is not a valid WAD file.");
				if((numlumps < 0) || (tableoffset < HEADER_SIZE) || (((long)tableoffset + ((long)numlumps * ENTRY_SIZE)) > fs.Length))
					throw new IOException("The target file has an invalid lump table.");

				// Read the lump table
				int[] offsets = new int[numlumps];
				int[] lengths = new int[numlumps];
				fs.Seek(tableoffset, SeekOrigin.Begin);
				for(int i = 0; i < numlumps; i++)
				{
					offsets[i] = reader.ReadInt32();
					lengths[i] = reader.ReadInt32();
					names.Add(reader.ReadBytes(8));
				}

				// Find the lump to replace
				int replaceindex = -1;
				for(int i = 0; i < numlumps; i++)
				{
					if(Lump.MakeLongName(Lump.MakeNormalName(names[i], WAD.ENCODING)) == longold)
					{
						replaceindex = i;
						break;
					}
				}

				// Read all the lump data
				for(int i = 0; i < numlumps; i++)
				{
					if(i == replaceindex)
					{
						// Check that the existing lump really is a nested WAD, and keep its type
						byte[] existing = ReadBytes(fs, offsets[i], lengths[i]);
						string existingtype = (existing.Length >= 4) ? WAD.ENCODING.GetString(existing, 0, 4) : "";
						if((existingtype != WAD.TYPE_IWAD) && (existingtype != WAD.TYPE_PWAD))
							throw new IOException("The target file already has a lump named " + oldname + " which is not a nested map WAD.");
						SetType(nestedwad, existingtype);
						datas.Add(nestedwad);
						names[i] = Lump.MakeFixedName(newname, WAD.ENCODING);
					}
					else
					{
						datas.Add(ReadBytes(fs, offsets[i], lengths[i]));
					}
				}

				// Not found? Then add it at the end
				if(replaceindex == -1)
				{
					SetType(nestedwad, defaulttype);
					names.Add(Lump.MakeFixedName(newname, WAD.ENCODING));
					datas.Add(nestedwad);
				}
			}

			// Write the new WAD next to the old one, then swap them
			WriteWadFile(tempfile, sourcetype, names, datas);
			File.Delete(targetfile);
			File.Move(tempfile, targetfile);
		}

		// This reads a block of bytes from a stream
		private static byte[] ReadBytes(Stream s, int offset, int length)
		{
			if((length <= 0) || (offset < 0) || (((long)offset + length) > s.Length)) return new byte[0];
			byte[] data = new byte[length];
			s.Seek(offset, SeekOrigin.Begin);
			int read = 0;
			while(read < length)
			{
				int n = s.Read(data, read, length - read);
				if(n <= 0) break;
				read += n;
			}
			return data;
		}

		// This sets the WAD type in the header of WAD data
		private static void SetType(byte[] wad, string type)
		{
			byte[] t = WAD.ENCODING.GetBytes(type);
			for(int i = 0; i < 4; i++) wad[i] = t[i];
		}

		// This writes a WAD file with the given lump names (fixed 8 byte names) and data
		private static void WriteWadFile(string filename, string type, List<byte[]> names, List<byte[]> datas)
		{
			using(FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				BinaryWriter writer = new BinaryWriter(fs, WAD.ENCODING);
				int[] offsets = new int[names.Count];

				// Header
				writer.Write(WAD.ENCODING.GetBytes(type));
				writer.Write(names.Count);
				writer.Write((int)0);

				// Lump data
				for(int i = 0; i < names.Count; i++)
				{
					while((fs.Position % ALIGNMENT) != 0) writer.Write((byte)0);
					offsets[i] = (int)fs.Position;
					if(datas[i].Length > 0) writer.Write(datas[i]);
				}

				// Lump table
				while((fs.Position % ALIGNMENT) != 0) writer.Write((byte)0);
				int tableoffset = (int)fs.Position;
				for(int i = 0; i < names.Count; i++)
				{
					writer.Write(offsets[i]);
					writer.Write(datas[i].Length);
					writer.Write(names[i], 0, 8);
				}

				// Table offset in header
				fs.Seek(8, SeekOrigin.Begin);
				writer.Write(tableoffset);
				writer.Flush();
			}
		}

		#endregion
	}
}
