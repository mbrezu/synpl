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
using Synpl.Core;
using Synpl.Parser.Sexp;
using System.Text;
using Synpl.Shell;

namespace Synpl.ShellGtk
{
	public partial class MainWindow: Gtk.Window
	{
        // TODO: Mock editor for testing.
        // TODO: Selection of multiple nodes (to handle extend selection up/down).
        // TODO: Try keyboard accelerators (mode for structured editing).
        // TODO: Pretty printing? :-) This needs some design work.

        // TODO: Common shell code (independent of the UI) should be extracted to
        // a Synpl.Shell assembly/namespace so it's not duplicated across shells.

		#region Private Storage
        // This toy clipboard isn't intended for serious use, I just want to be able
        // to test the functionality of the GtkTextViewEditor class
        private Synpl.Shell.Shell _shell;
		#endregion
		
		#region Constructor
		public MainWindow(): base (Gtk.WindowType.Toplevel)
		{
			Build();
			ConfigureTextView();
            _shell = new Synpl.Shell.Shell(new GtkTextViewEditor(txtEditor));
			txtEditor.KeyReleaseEvent += HandleKeyReleaseEvent;
			ShowAll();
		}

		#endregion

		#region Private Helper Methods
        
		private void ConfigureTextView()
		{
			FontDescription fontDescription = new FontDescription();
			fontDescription.Family = "Bitstream Vera Sans Mono";
			fontDescription.AbsoluteSize = 15000;
			txtEditor.ModifyFont(fontDescription);
		}
		#endregion
		
		#region Form Event Handlers
		protected void OnDeleteEvent(object sender, DeleteEventArgs a)
		{
			Application.Quit ();
			a.RetVal = true;
		}

		void HandleKeyReleaseEvent(object o, KeyReleaseEventArgs args)
		{
            label1.Text = _shell.GetStatus();
            label1.Justify = Justification.Left;
            // TODO: Extend this to get some syntax highlighting without parsing?
//			List<FormattingHint> hints = new List<FormattingHint>();
//			hints.Add(new FormattingHint(0, 3, "keyword"));
//			hints.Add(new FormattingHint(4, 7, "stringLiteral"));
//			hints.Add(new FormattingHint(8, 12, "comment"));
//			_editor.RequestFormatting(0, _editor.Length, hints);
		}
        
        protected virtual void OnExitActionActivated(object sender, System.EventArgs e)
        {
            Application.Quit();
        }
       
        protected virtual void OnExtendToParentActionActivated(object sender, System.EventArgs e)
        {
            _shell.ExtendToParent();
        }

        protected virtual void OnRestrictChildActionActivated (object sender, System.EventArgs e)
        {
            _shell.LastSelection();
        }

        protected virtual void OnInsert1ActionActivated (object sender, System.EventArgs e)
        {
            _shell.ReplaceText("(map fun ((1 2) (3 (4)) (5 6)))");
        }

        protected virtual void OnInsert2ActionActivated (object sender, System.EventArgs e)
        {
            _shell.ReplaceText(@"
(map (lambda (x) (* x x))
     '(1 2 3 4 5))");
        }

        protected virtual void OnInsert3ActionActivated (object sender, System.EventArgs e)
        {
            _shell.ReplaceText(@"
(
    '(some stuff)
    '(some other stuff)
    '(1 2 3 4 5))");
        }

        protected virtual void OnSelectPreviousSiblingActionActivated(object sender,
                                                                      System.EventArgs e)
        {
            _shell.SelectPreviousSibling();
        }

        protected virtual void OnSelectNextSiblingActionActivated(object sender,
                                                                  System.EventArgs e)
        {
            _shell.SelectNextSibling();
        }

        protected virtual void OnMoveUpAction1Activated(object sender,
                                                        System.EventArgs e)
        {
            _shell.MoveUp();
        }

        protected virtual void OnMoveDownAction1Activated(object sender,
                                                          System.EventArgs e)
        {
            _shell.MoveDown();
        }

        protected virtual void OnIndentActionActivated (object sender, System.EventArgs e)
        {
            _shell.Indent();
        }

		#endregion

    }
}