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

namespace Synpl.ShellGtk
{
	public partial class MainWindow: Gtk.Window
	{
        // TODO: Mock editor for testing.
        // TODO: Selection by trees and subtrees.
        // TODO: Moveup/movedown trees in the editor.
        // TODO: Pretty printing? :-) This needs some design work.
        // TODO: Indent (first tree on current line at the same indentation
        // as the previous node, or slightly indented with regard to the parent
        // if there is no previous node.
        
        // TODO: Common shell code (independent of the UI) should be extracted to
        // a Synpl.Shell assembly/namespace so it's not duplicated across shells.

		#region Private Storage
        private IAbstractEditor _editor;
        private TextWithChanges _text;
        private ParseTree _parseTree;
        // This toy clipboard isn't intended for serious use, I just want to be able
        // to test the functionality of the GtkTextViewEditor class
        private string _clipboard = String.Empty;
		#endregion
		
		#region Constructor
		public MainWindow(): base (Gtk.WindowType.Toplevel)
		{
			Build();
			ConfigureTextView();
			_editor = new GtkTextViewEditor(txtEditor);
			_editor.TextChanged += HandleTextChanged;
			txtEditor.KeyReleaseEvent += HandleKeyReleaseEvent;
            _text = new TextWithChanges(_editor);
			ShowAll();
		}

        private void CompleteReparse()
        {
            CowList<Token> tokens = 
                SexpParser.GetInstance(SexpParser.ParseType.Global).
                    TokenizerFunc(_text.GetCurrentSlice(0,
                                                        _text.GetActualLength()));
            Console.WriteLine(">>> tokens: {0}", tokens);
            CowList<Token> remainingTokens = null;
            try 
            {
                SexpParser.GetInstance(SexpParser.ParseType.Global).
                    ParserFunc(tokens,
                               _text,
                               out _parseTree,
                               out remainingTokens);
            }
            catch (ParseException ex)
            {
                Console.WriteLine("Error while parsing: {0}.", ex.Message);
                return;
            }
            if (remainingTokens != null && remainingTokens.Count > 0)
            {
                Console.WriteLine("Tokens left in the stream, incomplete parse.");
            }
            // HACK: Maybe validate slice should be called by the parser?
            _text.ValidateSlice(_parseTree.StartPosition, _parseTree.EndPosition);
            Console.WriteLine(">>> New parse tree:");
            Console.WriteLine(">>> TWC is: {0}", _text.TestRender());
            Console.WriteLine(">>> Code tree is:{0}{1}", 
                              Environment.NewLine,
                              _parseTree.ToStringAsTree());            
            Console.WriteLine(">>> Old code is '{0}'.", _parseTree.ToStringAsCode(true));
            Console.WriteLine(">>> New code is '{0}'.", _parseTree.ToStringAsCode(false));
        }
        
        private void TryParsing(TextChangedEventArgs e)
        {
            if (_parseTree == null 
                || _parseTree.EndPosition <= e.Start)
            {
                Console.WriteLine("Complete Reparse.");
                _text.SetText(_editor.GetText(0, _editor.Length));
                CompleteReparse();
            }
            else
            {
                // TODO: Add multiple char deletion and insertion to parse nodes and TWC.
                // Also add unit tests for them.
                switch (e.Operation)
                {
                case TextChangedEventArgs.OperationType.Insertion:
                    for (int i = 0; i < e.Length; i++)
                    {
                        _parseTree = _parseTree.CharInsertedAt(e.Text[i], e.Start + i);
                    }
                    Console.WriteLine(">>> After insert:");
                    break;
                case TextChangedEventArgs.OperationType.Deletion:
                    Console.WriteLine(">>> TWC is: {0}", _text.TestRender());
                    for (int i = 0; i < e.Length; i++)
                    {
                        _parseTree = _parseTree.CharDeletedAt(e.Start);
                    }
                    Console.WriteLine(">>> After delete:");
                    break;
                default:
                    Console.WriteLine("Unknown text change operation.");
                    break;
                }
                Console.WriteLine(">>> Code tree is:{0}{1}", 
                                  Environment.NewLine,
                                  _parseTree.ToStringAsTree());
                Console.WriteLine(">>> TWC is: {1}: {0}", 
                                  _text.TestRender(),
                                  _text.GetHashCode());
                Console.WriteLine(">>> Old code is '{0}'.", _parseTree.ToStringAsCode(true));
                Console.WriteLine(">>> New code is '{0}'.", _parseTree.ToStringAsCode(false));
            }
        }

        private void UpdateTextWithChanges(TextChangedEventArgs e)
        {
//            switch (e.Operation)
//            {
//            case TextChangedEventArgs.OperationType.Insertion:
//                Console.WriteLine(">>> Inserting...");
//                Console.WriteLine(e);
//                int insertAt = e.Start;
//                foreach (char ch in e.Text)
//                {
//                    _text.InsertChar(ch, insertAt);
//                    insertAt ++;
//                }
//                Console.WriteLine(">>> TWC is now {0}", _text.TestRender());
//                break;
//            case TextChangedEventArgs.OperationType.Deletion:
//                Console.WriteLine(">>> Deleting...");
//                Console.WriteLine(e);
//                for (int i = 0; i < e.Text.Length; i++)
//                {                    
//                    _text.DeleteChar(e.Start);
//                }
//                Console.WriteLine(">>> TWC is now {0}", _text.TestRender());
//                break;
//            default:
//                Console.WriteLine("Unknown text change operation.");
//                break;
//            }
        }
		#endregion

		#region Editor Event Handlers
		private void HandleTextChanged(object sender, TextChangedEventArgs e)
		{
            UpdateTextWithChanges(e);
            TryParsing(e);
//			Console.WriteLine(">>> {0}", e.Operation);
//			Console.WriteLine("{0}, {1}: \"{2}\"", e.Start, e.Length, e.Text);
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

        private string GetSelectedText()
        {
            int selStart, selEnd;
            _editor.GetSelection(out selStart, out selEnd);
            return _editor.GetText(selStart, selEnd - selStart);
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
            // TODO: Extend this to get some syntax highlighting without parsing?
//			List<FormattingHint> hints = new List<FormattingHint>();
//			hints.Add(new FormattingHint(0, 3, "keyword"));
//			hints.Add(new FormattingHint(4, 7, "stringLiteral"));
//			hints.Add(new FormattingHint(8, 12, "comment"));
//			_editor.RequestFormatting(0, _editor.Length, hints);
		}

        protected virtual void OnExitActionActivated (object sender, System.EventArgs e)
        {
            Application.Quit();
        }
       
        protected virtual void OnCopyActionActivated (object sender, System.EventArgs e)
        {
            _clipboard = GetSelectedText();
//            Console.WriteLine(_clipboard);
        }
       
        protected virtual void OnCutActionActivated (object sender, System.EventArgs e)
        {
            _clipboard = GetSelectedText();
            int selStart, selEnd;
            _editor.GetSelection(out selStart, out selEnd);
            _editor.DeleteText(selStart, selEnd - selStart, true);
        }

        protected virtual void OnPasteActionActivated (object sender, System.EventArgs e)
        {
            _editor.InsertText(_editor.CursorOffset, _clipboard, true);
        }
        
		#endregion

    
    }
}