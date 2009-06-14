
using System;
using Gtk;

namespace Synpl.EditorAbstraction
{
	internal class TextTagWorkaround : TextTag
	{		
		public TextTagWorkaround(string name, TextTagTable table) : base(name)
		{
			table.Add(this);
		}
	}
}
