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
        // TODO: Selection of multiple nodes (to handle extend selection up/down).
        // TODO: Try keyboard accelerators (mode for structured editing).
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
        private CowList<ParseTree> _selectedTreeStack;
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
            _selectedTreeStack = new CowList<ParseTree>();
			ShowAll();
		}

        private void CompleteReparse()
        {
            CowList<Token> tokens = 
                SexpParser.GetInstance(SexpParser.ParseType.Global).
                    TokenizerFunc(_text.GetCurrentSlice(0,
                                                        _text.GetActualLength()));
//            Console.WriteLine(">>> tokens: {0}", tokens);
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
//                Console.WriteLine("Tokens left in the stream, incomplete parse.");
            }
            // HACK: Maybe validate slice should be called by the parser?
            _text.ValidateSlice(_parseTree.StartPosition, _parseTree.EndPosition);
//            Console.WriteLine(">>> New parse tree:");
//            Console.WriteLine(">>> TWC is: {0}", _text.TestRender());
//            Console.WriteLine(">>> Code tree is:{0}{1}", 
//                              Environment.NewLine,
//                              _parseTree.ToStringAsTree());            
//            Console.WriteLine(">>> Old code is '{0}'.", _parseTree.ToStringAsCode(true));
//            Console.WriteLine(">>> New code is '{0}'.", _parseTree.ToStringAsCode(false));
        }
        
        private void TryParsing(TextChangedEventArgs e)
        {
            if (_parseTree == null 
                || _parseTree.EndPosition <= e.Start)
            {
//                Console.WriteLine("Complete Reparse.");
                _text.SetText("");
                string text = _editor.GetText(0, _editor.Length);
                int pos = 0;
                foreach (char ch in text)
                {
                    _text.InsertChar(ch, pos);
                    pos ++;
                }
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
//                    Console.WriteLine(">>> After insert:");
                    break;
                case TextChangedEventArgs.OperationType.Deletion:
//                    Console.WriteLine(">>> TWC is: {0}", _text.TestRender());
                    for (int i = 0; i < e.Length; i++)
                    {
                        _parseTree = _parseTree.CharDeletedAt(e.Start);
                    }
//                    Console.WriteLine(">>> After delete:");
                    break;
                default:
                    Console.WriteLine("Unknown text change operation.");
                    break;
                }
//                Console.WriteLine(">>> Code tree is:{0}{1}", 
//                                  Environment.NewLine,
//                                  _parseTree.ToStringAsTree());
//                Console.WriteLine(">>> TWC is: {1}: {0}", 
//                                  _text.TestRender(),
//                                  _text.GetHashCode());
//                Console.WriteLine(">>> Old code is '{0}'.", _parseTree.ToStringAsCode(true));
//                Console.WriteLine(">>> New code is '{0}'.", _parseTree.ToStringAsCode(false));
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


        private void ListParseTrees(ParseTree pt, CowList<ParseTree> acc)
        {
            acc.Add(pt);
            foreach (ParseTree tree in pt.SubTrees)
            {
                ListParseTrees(tree, acc);
            }
        }

        // TODO: Optimization: shouldn't recolor all text when the text changes.
        private void UpdateFormatting()
        {
            IList<TextChange> changes = _text.Changes;
            List<FormattingHint> hints = new List<FormattingHint>();
            List<int> deletePositions = new List<int>();
//            Console.WriteLine(">>> Formatting hints:");
            foreach(TextChange change in changes)
            {
                if (!change.IsDeletion)
                {
                    FormattingHint hint = new FormattingHint(change.Position, 
                                                             change.Position + 1, 
                                                             "addedText");
                    hints.Add(hint);
//                    Console.WriteLine(hint);
                }
                else
                {
                    deletePositions.Add(_text.ConvertOldPositionToActual(change.Position));
                }
            }
            Dictionary<ParseTree, int> treesToHighlight = new Dictionary<ParseTree, int>();
            foreach (int pos in deletePositions)
            {
                // FIXME: Ambiguity: the deleted character may indeed
                // belong to the tree pt below (result of HasEndPosition)
                // or to its parent. Posible resolution: check if the old version
                // of pt parses.
                ParseTree pt = _parseTree.HasEndPosition(pos);
                if (pt == null)
                {
                    CowList<ParseTree> path = _parseTree.GetPathForPosition(pos);
                    if (path.Count > 0)
                    {
                        pt = path.Last;
                    }
                    else
                    {
                        pt = _parseTree;
                    }
                }
                if (!treesToHighlight.ContainsKey(pt))
                {
                    treesToHighlight.Add(pt, 1);
                }
            }
            foreach (ParseTree pt in treesToHighlight.Keys)
            {
                FormattingHint hint = new FormattingHint(pt.StartPosition,
                                                         pt.EndPosition,
                                                         "brokenByDelete");
//                Console.WriteLine(hint);
                hints.Add(hint);
            }
            _editor.RequestFormatting(0, _editor.Length, hints);
        }
		#endregion

		#region Editor Event Handlers
		private void HandleTextChanged(object sender, TextChangedEventArgs e)
		{
            Console.WriteLine("************************");
            Console.WriteLine("TWC before: {0}", _text.TestRender());
            UpdateTextWithChanges(e);
            TryParsing(e);
            UpdateFormatting();
			Console.WriteLine(">>> {0}", e.Operation);
			Console.WriteLine("{0}, {1}: \"{2}\"", e.Start, e.Length, e.Text);
            Console.WriteLine("TWC after: {0}", _text.TestRender());
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

        private void ValidateSelectionStack()
        {
            int selStart, selEnd;
            _editor.GetSelection(out selStart, out selEnd);
            if (_selectedTreeStack.Count > 0)
            {
                if (selStart != _selectedTreeStack.Last.StartPosition
                    || selEnd != _selectedTreeStack.Last.EndPosition)
                {
                    _selectedTreeStack.Clear();
                }
            }
        }

        private void UpdateSelection(ParseTree newRoot, CowList<int> path)
        {
            if (newRoot != null)
            {
                _selectedTreeStack.Clear();
                _parseTree = newRoot;
                _selectedTreeStack.Add(_parseTree.GetNodeAtPath(path));
                _editor.SetSelection(_selectedTreeStack.Last.StartPosition,
                                     _selectedTreeStack.Last.EndPosition);                
            }
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
            int offset = _editor.CursorOffset;
            int line, column;
            _editor.OffsetToLineColumn(offset, out line, out column);
            int selStart, selEnd;
            _editor.GetSelection(out selStart, out selEnd);
            label1.Text = String.Format("Position: {0} {1} {2}; Selection: {3}-{4}", 
                                        offset, 
                                        line, 
                                        column,
                                        selStart,
                                        selEnd);
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
       
        protected virtual void OnCopyActionActivated(object sender, System.EventArgs e)
        {
            _clipboard = GetSelectedText();
//            Console.WriteLine(_clipboard);
        }
       
        protected virtual void OnCutActionActivated(object sender, System.EventArgs e)
        {
            _clipboard = GetSelectedText();
            int selStart, selEnd;
            _editor.GetSelection(out selStart, out selEnd);
            _editor.DeleteText(selStart, selEnd - selStart, true);
        }

        protected virtual void OnPasteActionActivated(object sender, System.EventArgs e)
        {
            _editor.InsertText(_editor.CursorOffset, _clipboard, true);
        }

        protected virtual void OnExtendToParentActionActivated(object sender, System.EventArgs e)
        {
            if (_parseTree == null)
            {
                return;
            }
            ValidateSelectionStack();
            int selStart, selEnd;
            _editor.GetSelection(out selStart, out selEnd);
            if (_selectedTreeStack.Count == 0 || !_selectedTreeStack.Last.Contains(selStart))
            {
                _selectedTreeStack.Clear();
                CowList<ParseTree> pts = _parseTree.GetPathForPosition(selStart);
                if (pts.Count == 0)
                {
                    return;
                }
                _selectedTreeStack.Add(pts.Last);
            }
            else
            {
                if (_selectedTreeStack.Last.Parent != null)
                {
                    _selectedTreeStack.Add(_selectedTreeStack.Last.Parent);
                }
            }
            _editor.SetSelection(_selectedTreeStack.Last.StartPosition, 
                                 _selectedTreeStack.Last.EndPosition);
        }

        protected virtual void OnRestrictChildActionActivated (object sender, System.EventArgs e)
        {
            ValidateSelectionStack();
            if (_selectedTreeStack.Count >= 2)
            {
                _selectedTreeStack.RemoveAt(_selectedTreeStack.Count - 1);
                _editor.SetSelection(_selectedTreeStack.Last.StartPosition, 
                                     _selectedTreeStack.Last.EndPosition);
            }            
        }

        protected virtual void OnInsert1ActionActivated (object sender, System.EventArgs e)
        {
            _parseTree = null;
            _editor.DeleteText(0, _editor.Length, false);
            _editor.InsertText(0, "(map fun ((1 2) (3 (4)) (5 6)))", false);
            _selectedTreeStack.Clear();
        }

        protected virtual void OnInsert2ActionActivated (object sender, System.EventArgs e)
        {
            _parseTree = null;
            _editor.DeleteText(0, _editor.Length, false);
            _editor.InsertText(0, @"
(map (lambda (x) (* x x))
     '(1 2 3 4 5))", false);
            _selectedTreeStack.Clear();
        }

        protected virtual void OnSelectPreviousSiblingActionActivated(object sender,
                                                                      System.EventArgs e)
        {
            if (_selectedTreeStack.Count > 0)
            {
                ParseTree prev = _selectedTreeStack.Last.GetPreviousSibling();
                if (prev != null)
                {
                    _selectedTreeStack.Add(prev);
                    _editor.SetSelection(_selectedTreeStack.Last.StartPosition,
                                         _selectedTreeStack.Last.EndPosition);
                }
            }
        }

        protected virtual void OnSelectNextSiblingActionActivated(object sender,
                                                                  System.EventArgs e)
        {
            if (_selectedTreeStack.Count > 0)
            {
                ParseTree next = _selectedTreeStack.Last.GetNextSibling();
                if (next != null)
                {
                    _selectedTreeStack.Add(next);
                    _editor.SetSelection(_selectedTreeStack.Last.StartPosition,
                                         _selectedTreeStack.Last.EndPosition);
                }
            }
        }

        protected virtual void OnMoveUpAction1Activated(object sender,
                                                        System.EventArgs e)
        {
            if (_parseTree == null)
            {
                return;
            }
            if (_selectedTreeStack.Count == 0)
            {
                return;
            }
            CowList<int> path = _selectedTreeStack.Last.GetPath();
            if (path.Count > 0)
            {
                if (path.Last > 0)
                {
                    path.Last --;
                }
            }
            ParseTree newRoot = _selectedTreeStack.Last.MoveUp();
            UpdateSelection(newRoot, path);
        }

        protected virtual void OnMoveDownAction1Activated(object sender,
                                                          System.EventArgs e)
        {
            if (_parseTree == null)
            {
                return;
            }
            if (_selectedTreeStack.Count == 0)
            {
                return;
            }
            CowList<int> path = _selectedTreeStack.Last.GetPath();
            if (path.Count > 0)
            {
                if (path.Last < _selectedTreeStack.Last.Parent.SubTrees.Count - 1)
                {
                    path.Last ++;
                }
            }
            ParseTree newRoot = _selectedTreeStack.Last.MoveDown();
            UpdateSelection(newRoot, path);
        }

        protected virtual void OnIndentActionActivated (object sender, System.EventArgs e)
        {
            if (_parseTree == null)
            {
                return;
            }
            int currentOffset = _editor.CursorOffset;
            int currentLine, currentColumn;
            _editor.OffsetToLineColumn(currentOffset, out currentLine, out currentColumn);
            if (currentLine == 0)
            {
                return;
            }
            Console.WriteLine("!!! Indenting:");
            Console.WriteLine("current: {0} {1} {2}", currentOffset, currentLine, currentColumn);
            int lineStartOffset = _editor.LineColumnToOffset(currentLine, 0);
            Console.WriteLine("line start:{0}", lineStartOffset);
            ParseTree lineStarter = _parseTree.GetFirstNodeAfter(lineStartOffset);
            if (lineStarter == null)
            {
                return;
            }
            ParseTree lastSibling = lineStarter.GetLastSiblingBefore(lineStartOffset);
            int indentOffset;
            if (lastSibling != null)
            {
                Console.WriteLine("indent by sibling");
                indentOffset = lastSibling.StartPosition;
            }
            else if (lineStarter.Parent != null)
            {
                Console.WriteLine("indent by parent");
                indentOffset = lineStarter.Parent.StartPosition + 2;
            }
            else
            {
                return;
            }
            int indentLine, indentColumn;
            _editor.OffsetToLineColumn(indentOffset, out indentLine, out indentColumn);
            Console.WriteLine("desired: {0} {1} {2}", indentOffset, indentLine, indentColumn);
            int lineStarterLine, lineStarterColumn;
            _editor.OffsetToLineColumn(lineStarter.StartPosition, 
                                       out lineStarterLine,
                                       out lineStarterColumn);
            Console.WriteLine("found: {0} {1} {2}", lineStarter.StartPosition, lineStarterLine, lineStarterColumn);
            if (lineStarterColumn < indentColumn)                
            {
                Console.WriteLine("adding");
                _editor.InsertText(lineStartOffset, 
                                   new String(' ', indentColumn - lineStarterColumn), 
                                   false);
            }
            else if (lineStarterColumn > indentColumn)
            {
                Console.WriteLine("deleting");
                _editor.DeleteText(lineStartOffset, lineStarterColumn - indentColumn, false);
            }
        }
        
		#endregion

    }
}