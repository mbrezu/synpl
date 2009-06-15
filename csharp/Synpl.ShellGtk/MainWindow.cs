// Synpl - a "structured editor" plugin for Gedit.
// Copyright (C) 2009  Miron Brezuleanu <mbrezu@gmail.com>

// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License along
// with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

using System;
using Gtk;
using Pango;
using Synpl.EditorAbstraction;
using System.Collections.Generic;

namespace Synpl.ShellGtk
{

	public partial class MainWindow: Gtk.Window
	{	

		#region Private Storage
		private TextView _textView;
		private GtkTextViewEditor _editorWrapper;
		#endregion
		
		#region Constructor
		public MainWindow(): base (Gtk.WindowType.Toplevel)
		{
			Build();
			ConfigureTextView();
			_editorWrapper = new GtkTextViewEditor(_textView);
			_editorWrapper.TextChanged += HandleTextChanged;
			_textView.KeyReleaseEvent += HandleKeyReleaseEvent;
			ShowAll();
		}
		#endregion

		#region Editor Event Handlers
		private void HandleTextChanged(object sender, TextChangedEventArgs e)
		{
			Console.WriteLine(">>> {0}", e.Operation);
			Console.WriteLine("{0}, {1}: \"{2}\"", e.Start, e.Length, e.Text);			                 
		}
		#endregion
		
		#region Private Helper Methods
		private void ConfigureTextView()
		{
			_textView = new TextView();
			FontDescription fontDescription = new FontDescription();
			fontDescription.Family = "Bitstream Vera Sans Mono";
			fontDescription.AbsoluteSize = 15000;
			_textView.ModifyFont(fontDescription);
			Add(_textView);
		}
		#endregion
		
		#region Form Event Handlers
		protected void OnDeleteEvent (object sender, DeleteEventArgs a)
		{
			Application.Quit ();
			a.RetVal = true;
		}

		void HandleKeyReleaseEvent(object o, KeyReleaseEventArgs args)
		{
			List<FormattingHint> hints = new List<FormattingHint>();
			hints.Add(new FormattingHint(0, 3, "keyword"));
			hints.Add(new FormattingHint(4, 7, "stringLiteral"));
			hints.Add(new FormattingHint(8, 12, "comment"));
			_editorWrapper.RequestFormatting(0, _editorWrapper.Length, hints);
		}
		#endregion
	}
}