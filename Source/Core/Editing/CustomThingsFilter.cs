
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
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Config;

#endregion

namespace CodeImp.DoomBuilder.Editing
{
	public class CustomThingsFilter : ThingsFilter
	{
		#region ================== Variables

		#endregion

		#region ================== Properties

		public new string Name { get { return name; } set { name = value; } }
		public new string CategoryName { get { return categoryname; } set { categoryname = value; } }
		public new int ThingType { get { return thingtype; } set { thingtype = value; } }
		public new ICollection<string> RequiredFields { get { return requiredfields; } }
		public new ICollection<string> ForbiddenFields { get { return forbiddenfields; } }

		#endregion

		#region ================== Constructor / Disposer

		// Constructor for a new filter
		public CustomThingsFilter()
		{
			// Initialize
			requiredfields = new List<string>();
			forbiddenfields = new List<string>();
			categoryname = "";
			thingtype = -1;
			name = "Unnamed filter";

			// We have no destructor
			GC.SuppressFinalize(this);
		}

		// Disposer
		public new virtual void Dispose()
		{
			base.Dispose();
		}

		#endregion

		#region ================== Methods

		#endregion
	}
}
