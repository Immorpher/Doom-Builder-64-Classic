
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
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;

#endregion

namespace CodeImp.DoomBuilder.Compilers
{
	internal sealed class BlamCompiler : Compiler
	{
		#region ================== Constants
		
		private const string BLAM_ERROR_FILE = "blam_err.txt";
		
		#endregion
		
		#region ================== Variables
		
		#endregion
		
		#region ================== Constructor
		
		// Constructor
		public BlamCompiler(CompilerInfo info) : base(info)
		{
		}

		// Disposer
		public override void Dispose()
		{
			// Not already disposed?
			if(!isdisposed)
			{
				// Clean up

				// Done
				base.Dispose();
			}
		}
		
		#endregion
		
		#region ================== Methods
		
		// This checks if compiled MACROS lump data contains at least one real (non-empty) action.
		// Layout: UInt16 macro count, UInt16 action count, then for each macro a UInt16 action count
		// followed by that many actions plus one 'dummy' action, each being 3 Int16 values (batch, tag, type).
		public static bool HasMacroActions(byte[] data)
		{
			if((data == null) || (data.Length <= 4)) return false;
			
			int count = BitConverter.ToUInt16(data, 0);
			int pos = 4;
			
			for(int i = 0; i < count; i++)
			{
				if((pos + 2) > data.Length) return false;
				int setcount = BitConverter.ToUInt16(data, pos);
				pos += 2;
				
				for(int j = 0; j < setcount + 1; j++)
				{
					if((pos + 6) > data.Length) return false;
					short type = BitConverter.ToInt16(data, pos + 4);
					if((j < setcount) && (type != 0)) return true;
					pos += 6;
				}
			}
			
			return false;
		}
		
		// This decompiles compiled MACROS lump data into BLAM script source text.
		// Returns null when the decompiler failed to produce any output.
		public byte[] Decompile(byte[] macrodata)
		{
			string macrofile = General.MakeTempFilename(tempdir.FullName, "tmp");
			File.WriteAllBytes(macrofile, macrodata);
			
			// BLAM writes its output next to the input file, as <inputfile>_decompiled.txt
			string resultfile = macrofile + "_decompiled.txt";
			
			ProcessStartInfo processinfo = new ProcessStartInfo();
			string dummyoutput = Path.GetFileName(General.MakeTempFilename(tempdir.FullName, "tmp"));
			processinfo.Arguments = "\"" + Path.GetFileName(macrofile) + "\" \"" + dummyoutput + "\" -d";
			processinfo.FileName = Path.Combine(this.tempdir.FullName, info.ProgramFile);
			processinfo.CreateNoWindow = false;
			processinfo.ErrorDialog = false;
			processinfo.UseShellExecute = true;
			processinfo.WindowStyle = ProcessWindowStyle.Hidden;
			processinfo.WorkingDirectory = this.tempdir.FullName;
			
			General.WriteLogLine("Running decompiler...");
			General.WriteLogLine("Program:    " + processinfo.FileName);
			General.WriteLogLine("Arguments:  " + processinfo.Arguments);
			
			Process process = Process.Start(processinfo);
			if(process == null) return null;
			process.WaitForExit();
			General.WriteLogLine("Decompiler process has finished.");
			
			if(!File.Exists(resultfile)) return null;
			return RemoveCommonInclude(File.ReadAllBytes(resultfile));
		}
		
		// BLAM starts its decompiled output with an include of common.txt, which we don't want
		// in the script. This removes that line (and the blank line that follows it).
		private static byte[] RemoveCommonInclude(byte[] data)
		{
			Encoding encoding = Encoding.GetEncoding(1252);
			string[] lines = encoding.GetString(data).Replace("\r\n", "\n").Split('\n');
			List<string> result = new List<string>(lines.Length);
			
			for(int i = 0; i < lines.Length; i++)
			{
				if(lines[i].Trim().Replace(" ", "").ToLowerInvariant() == "#include\"common.txt\"")
				{
					// Skip the blank line after it, too
					if(((i + 1) < lines.Length) && (lines[i + 1].Trim().Length == 0)) i++;
					continue;
				}
				result.Add(lines[i]);
			}
			
			return encoding.GetBytes(string.Join("\r\n", result.ToArray()));
		}
		
		// This runs the compiler
		public override bool Run()
		{
			ProcessStartInfo processinfo;
			Process process;
			TimeSpan deltatime;
			int line = 0;
			string sourcedir = Path.GetDirectoryName(sourcefile);
			
			// Create parameters
			string args = this.parameters;
			args = args.Replace("%FI", inputfile);
			args = args.Replace("%FO", outputfile);
			args = args.Replace("%FS", sourcefile);
			args = args.Replace("%PT", this.tempdir.FullName);
			args = args.Replace("%PS", sourcedir);
			
			// Setup process info
			processinfo = new ProcessStartInfo();
			processinfo.Arguments = args;
			processinfo.FileName = Path.Combine(this.tempdir.FullName, info.ProgramFile);
			processinfo.CreateNoWindow = false;
			processinfo.ErrorDialog = false;
			processinfo.UseShellExecute = true;
			processinfo.WindowStyle = ProcessWindowStyle.Hidden;
			processinfo.WorkingDirectory = this.workingdir;
			
			// Output info
			General.WriteLogLine("Running compiler...");
			General.WriteLogLine("Program:    " + processinfo.FileName);
			General.WriteLogLine("Arguments:  " + processinfo.Arguments);
			
			try
			{
				// Start the compiler
				process = Process.Start(processinfo);
			}
			catch(Exception e)
			{
				// Unable to start the compiler
				General.ShowErrorMessage("Unable to start the compiler (" + info.Name + "). " + e.GetType().Name + ": " + e.Message, MessageBoxButtons.OK);
				return false;
			}
			
			// Wait for compiler to complete
			process.WaitForExit();
			deltatime = TimeSpan.FromTicks(process.ExitTime.Ticks - process.StartTime.Ticks);
			General.WriteLogLine("Compiler process has finished.");
			General.WriteLogLine("Compile time: " + deltatime.TotalSeconds.ToString("########0.00") + " seconds");
			
			// Now find the error file
			string errfile = Path.Combine(this.workingdir, BLAM_ERROR_FILE);
			if(File.Exists(errfile))
			{
				try
				{
					// Read all lines
					string[] errlines = File.ReadAllLines(errfile);
					while(line < errlines.Length)
					{
						// Check line
						string linestr = errlines[line];
                        CompilerError err = new CompilerError();

                        err.filename = inputfile;
                        err.description = linestr;
                        ReportError(err);
						
						// Next line
						line++;
					}
				}
				catch(Exception e)
				{
					// Error reading errors (ironic, isn't it)
					ReportError(new CompilerError("Failed to retrieve compiler error report. " + e.GetType().Name + ": " + e.Message));
				}
			}
			
			return true;
		}
		
		#endregion
	}
}

