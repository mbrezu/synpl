// Synpl - a "structured editor".
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
using Synpl.Core;
using Synpl.EditorAbstraction;
using System.Collections.Generic;
using System.Text;
using Synpl.Parser.Sexp;

namespace Synpl.Shell
{
    // TODO: Shell should not depend on SexpParser. Need to find a way to plug in the parser in.
    // TODO: Write a ConsistencyCheck function that compares the current tree with the one 
    // resulting from a complete reparse after each operation. (after text changed too).
    // If there is a failure ConsistencyCheck will dump it to console (expected/actual format).
    public class Shell
    {

        #region Constants
        readonly private static int MaxColumn = 40;
        #endregion
        
        #region Private Storage
        private IAbstractEditor _editor;
        private TextWithChanges _text;
        private ParseTree _parseTree;
        private CowList<ParseTree> _selectedTreeStack;
        private Queue<Synpl.EditorAbstraction.Key> _chordBuffer;
        private string _structureModeAction;
        private bool _inStructureMode;
        #endregion

        #region Constructor
        public Shell(IAbstractEditor editor)
        {
            _editor = editor;
            _editor.TextChanged += HandleTextChanged;
            _text = new TextWithChanges(_editor);
            _selectedTreeStack = new CowList<ParseTree>();
            _chordBuffer = new Queue<Synpl.EditorAbstraction.Key>();
            _structureModeAction = "m";
            _inStructureMode = false;
        }
        #endregion

        #region Public Methods
        public bool HandleKey(Synpl.EditorAbstraction.Key key)
        {
//            Console.WriteLine("Shell key handling: {0}", key.ToString());
            _chordBuffer.Enqueue(key);
            while (_chordBuffer.Count > 5)
            {
                _chordBuffer.Dequeue();
            }
            DebugDisplayChordBuffer();
            if (TypedChord("C-a"))
            {
                _editor.MoveToStartOfLine();
                return true;
            }
            else if (TypedChord("Tab"))
            {
                Indent();
                return true;
            }
            else if (TypedChord("C-e"))
            {
                _editor.MoveToEndOfLine();
                return true;
            }
            else if (TypedChord("C-x C-t") && !InStructureMode())
            { 
                EnterStructureMode();
                return true;
            }
            else if (TypedChord("C-g") && InStructureMode())
            {
                ExitStructureMode();
                return true;
            }
            else if (TypedChord("C-n"))
            {
                _editor.MoveForwardLines(1);
                return true;
            }
            else if (TypedChord("C-p"))
            {
                _editor.MoveForwardLines(-1);
                return true;
            }
            else if (TypedChord("C-b"))
            {
                _editor.MoveForwardChars(-1);
                return true;
            }
            else if (TypedChord("C-f"))
            {
                _editor.MoveForwardChars(1);
                return true;
            }
            if (InStructureMode())
            {
                if (TypedChord("q"))
                {
                    ExitStructureMode();
                    return true;
                }
                else if (TypedChord("i"))
                {
                    Indent();
                    return true;
                }
                else if (TypedChord("p"))
                {
                    ExtendToParent();
                    return true;
                }
                else if (TypedChord("l"))
                {
                    LastSelection();
                    return true;
                }
                else if (TypedChord("m"))
                {
                    _structureModeAction = "m";
                    return true;
                }
                else if (TypedChord("r"))
                {
                    _structureModeAction = "r";
                    return true;
                }
                else if (TypedChord("x"))
                {
                    _structureModeAction = "x";
                    return true;
                }
                else if (TypedChord("t"))
                {
                    _structureModeAction = "t";
                    return true;
                }
                else if (TypedChord("u"))
                {
                    switch (_structureModeAction)
                    {
                    case "m":
                        SelectPreviousSibling();
                        return true;
                    case "t":
                        MoveUp();
                        UpdateFormatting();
                        ConsistencyCheck();
                        return true;
                    }
                }
                else if (TypedChord("d"))
                {
                    switch (_structureModeAction)
                    {
                    case "m":
                        SelectNextSibling();
                        return true;
                    case "t":
                        MoveDown();
                        UpdateFormatting();
                        ConsistencyCheck();
                        return true;
                    }
                }
                else if (TypedChord("c"))
                {
                    SelectFirstChild();
                    return true;
                }
                else if (TypedChord("y"))
                {
                    _parseTree = PrettyPrint();
                    return true;
                }
            }
            return false;
        }
        
        public string GetStatus()
        {
            int offset = _editor.CursorOffset;
            int line, column;
            _editor.OffsetToLineColumn(offset, out line, out column);
            int selStart, selEnd;
            _editor.GetSelection(out selStart, out selEnd);
            return String.Format("Position: {0} {1} {2}; Selection: {3}-{4}", 
                                 offset, 
                                 line, 
                                 column,
                                 selStart,
                                 selEnd);
        }
        
        public bool TypedChord(string chordStr)
        {
            string[] keysStr = chordStr.Split(' ');
            if (keysStr.Length > _chordBuffer.Count)
            {
                return false;
            }
            List<Synpl.EditorAbstraction.Key> chord = new List<Synpl.EditorAbstraction.Key>();
            foreach (string key in keysStr)
            {
                chord.Add(new Synpl.EditorAbstraction.Key(key));
            }
            Synpl.EditorAbstraction.Key[] queue = _chordBuffer.ToArray();
            int queueIdx = queue.Length - 1;
            int chordIdx = chord.Count - 1;
            while (chordIdx >= 0)
            {
//                Console.WriteLine("{0} ?= {1}", chord[chordIdx], queue[queueIdx]);
//                Console.WriteLine(chord[chordIdx].ToString() != queue[queueIdx].ToString());
                if (chord[chordIdx].ToString() != queue[queueIdx].ToString())
                {
                    return false;
                }
                chordIdx --;
                queueIdx --;
            }
            return true;
        }
        
        public void ExtendToParent()
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
            SelectInEditor();
        }

        public void LastSelection()
        {
            ValidateSelectionStack();
            if (_selectedTreeStack.Count >= 2)
            {
                _selectedTreeStack.RemoveAt(_selectedTreeStack.Count - 1);
                SelectInEditor();
            }            
        }

        public void SelectPreviousSibling()
        {
            if (_selectedTreeStack.Count > 0)
            {
                ParseTree prev = _selectedTreeStack.Last.GetPreviousSibling();
                if (prev != null)
                {
                    _selectedTreeStack.Add(prev);
                    SelectInEditor();
                }
            }
        }

        public void SelectNextSibling()
        {
            if (_selectedTreeStack.Count > 0)
            {
                ParseTree next = _selectedTreeStack.Last.GetNextSibling();
                if (next != null)
                {
                    _selectedTreeStack.Add(next);
                    SelectInEditor();
                }
            }
        }

        public void SelectFirstChild()
        {
            if (_selectedTreeStack.Count > 0)
            {
                ParseTree top = _selectedTreeStack.Last;
                if (top.SubTrees.Count > 0)
                {
                    _selectedTreeStack.Add(top.SubTrees[0]);
                    SelectInEditor();
                }
            }
        }

        public void MoveUp()
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

        public void MoveDown()
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

        public void Indent()
        {
            if (_parseTree == null)
            {
                return;
            }
            CowList<int> pathToCursor;
            int cursorOffset;
            GetEditorPositionAsPath(out pathToCursor, out cursorOffset);
            ParseTree newParseTree = _parseTree.Indent(_editor.CursorOffset, Shell.MaxColumn, _editor);
            // If newParseTree is _parseTree, then the indend didn't do anything
            // and we don't need to cancel the selection and update the stored parse tree.
            if (newParseTree != _parseTree)
            {                
                // We need to clear the selected tree stack as the trees on the stack are no longer
                // valid.
                // TODO: store trees on the stack as paths to nodes and do not invalidate on indent?
                _parseTree = newParseTree;
                _selectedTreeStack.Clear();
            }
            UpdateFormatting();
            ConsistencyCheck();
            SetEditorPositionFromPath(pathToCursor, cursorOffset);
        }

        public void ReplaceText(string newText)
        {
            _parseTree = null;
            _editor.DeleteText(0, _editor.Length, false);
            _editor.InsertText(0, newText, false);
            _selectedTreeStack.Clear();
        }

        public ParseTree PrettyPrint()
        {
            if (_selectedTreeStack.Count == 0)
            {
                return _parseTree;
            }

            CowList<int> path = _selectedTreeStack.Last.GetPath();
            Console.WriteLine("path of pped node: {0}", path.ToString());
            _parseTree = _selectedTreeStack.Last.PrettyPrint(Shell.MaxColumn, _editor);
            _selectedTreeStack.Clear();
            ParseTree previouslySelected = _parseTree.GetNodeAtPath(path);
            if (previouslySelected != null)
            {
                _selectedTreeStack.Add(previouslySelected);
                SelectInEditor();
            }
            Console.WriteLine(">>> Code tree is:{0}{1}", 
                              Environment.NewLine,
                              _parseTree.ToStringAsTree());            
            UpdateFormatting();
            ConsistencyCheck();
            return _parseTree;
        }
        #endregion

        #region Private Helper Methods
        private void DebugDisplayChordBuffer()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var key in _chordBuffer.ToArray())
            {
                sb.AppendFormat("{0} ", key.ToString());
            }
//            Console.WriteLine("chord buffer: {0}", sb.ToString());
        }

        private ParseTree ReparseAll(bool useOldText)
        {
            CowList<Token> tokens = null;
            if (!useOldText)
            {
                tokens = SexpParser.GetInstance(SexpParser.ParseType.Global).
                    TokenizerFunc(_text.GetCurrentSlice(0,
                                                        _text.GetActualLength()));
            }
            else
            {
                tokens = SexpParser.GetInstance(SexpParser.ParseType.Global).
                    TokenizerFunc(_text.GetOldSlice(0,
                                                        _text.GetActualLength()));
            }
//            Console.WriteLine(">>> tokens: {0}", tokens);
            CowList<Token> remainingTokens = null;
            ParseTree parseTree;
            try 
            {
                SexpParser.GetInstance(SexpParser.ParseType.Global).
                    ParserFunc(tokens,
                               _text,
                               out parseTree,
                               out remainingTokens);
            }
            catch (ParseException ex)
            {
                Console.WriteLine("Error while parsing: {0}.", ex.Message);
                return null;
            }
            if (remainingTokens != null && remainingTokens.Count > 0)
            {
//                Console.WriteLine("Tokens left in the stream, incomplete parse.");
                return null;
            }
            return parseTree;
        }
            
        private void CompleteReparse()
        {
            _parseTree = ReparseAll(false);
            if (_parseTree == null)
            {
                return;
            }
            // HACK: Maybe validate slice should be called by the parser?
            _text.ValidateSlice(_parseTree.StartPosition, _parseTree.EndPosition);
//            Console.WriteLine(">>> New parse tree:");
//            Console.WriteLine(">>> TWC is: {0}", _text.TestRender());
            Console.WriteLine(">>> Code tree is:{0}{1}", 
                              Environment.NewLine,
                              _parseTree.ToStringAsTree());            
//            Console.WriteLine(">>> Old code is '{0}'.", _parseTree.ToStringAsCode(true));
//            Console.WriteLine(">>> New code is '{0}'.", _parseTree.ToStringAsCode(false));
        }        

        private void TryParsing(TextChangedEventArgs e)
        {
            if (_parseTree == null 
                || _parseTree.EndPosition <= e.Start
                || _parseTree.StartPosition >= e.Start)
            {
                Console.WriteLine("Complete Reparse.");
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
//                    Console.WriteLine("Unknown text change operation.");
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

        // TODO: Optimization: shouldn't recolor all text when the text changes.
        private void UpdateFormatting()
        {
            Console.WriteLine(">>> Update Formatting:");
            IList<TextChange> changes = _text.Changes;
            List<FormattingHint> hints = new List<FormattingHint>();
            List<int> deletePositions = new List<int>();
//            Console.WriteLine(">>> Formatting hints:");
            foreach(TextChange change in changes)
            {
                if (!change.IsDeletion)
                {
                    int actualPosition = 
                        _text.ConvertOldPositionToActual(change.Position);
                    FormattingHint hint = new FormattingHint(actualPosition,
                                                             actualPosition + 1,
                                                             "addedText");
                    hints.Add(hint);
                    Console.WriteLine(hint);
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
                Console.WriteLine(hint);
                hints.Add(hint);
            }
            _editor.RequestFormatting(0, _editor.Length, hints);
        }

        private bool InStructureMode()
        {
            return _inStructureMode;
        }

        private void EnterStructureMode()
        {
            _inStructureMode = true;
        }

        private void ExitStructureMode()
        {
            _inStructureMode = false;
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
                SelectInEditor();
            }
        }

        private void SelectInEditor()
        {
            _editor.SetSelection(_selectedTreeStack.Last.StartPosition,
                                 _selectedTreeStack.Last.EndPosition);                
        }

        // This function is used to save the current position of the cursor in a
        // way that's restorable after a pretty print or transform.
        // It stores the path to the node the cursor is on, along with the offset of the
        // cursor position relative to the start position of that node.
        //
        // Obviously, this works only for transformations that rearrange text, not for transformations
        // that change the parse tree structure. Transformations of the tree structure must be re-applied
        // on the <path> vector before calling SetEditorPositionAsPath.
        private void GetEditorPositionAsPath(out CowList<int> path, out int offset)
        {
            path = null;
            offset = 0;
            if (_parseTree == null)
            {
                return;
            }            
            CowList<ParseTree> pathToPosition = _parseTree.GetPathForPosition(_editor.CursorOffset);
            if (pathToPosition.Count == 0)
            {
                return;
            }
            ParseTree currentTree = pathToPosition.Last;
            path = currentTree.GetPath();
            offset = _editor.CursorOffset - currentTree.StartPosition;
        }

        // Restores a position saved by GetEditorPositionAsPath
        private void SetEditorPositionFromPath(CowList<int> path, int offset)
        {
            if (path == null || _parseTree == null)
            {
                return;
            }
            ParseTree currentNode = _parseTree.GetNodeAtPath(path);
            if (currentNode != null)
            {
                _editor.CursorOffset = currentNode.StartPosition + offset;
            }
        }

        private void ConsistencyCheck()
        {
//            ParseTree completeReparse = ReparseAll(true);
//            if (completeReparse == null && _parseTree != null)
//            {
//                Console.WriteLine(">>> Consistency check: complete reparse is null, _parseTree isn't null.");
//                throw new InvalidOperationException("Synpl Consistency Check.");
//            }
//            if (completeReparse != null && _parseTree == null)
//            {
//                Console.WriteLine(">>> Consistency check: complete reparse isn't null, _parseTree is null.");
//                throw new InvalidOperationException("Synpl Consistency Check.");
//            }
//            if (completeReparse == null || _parseTree == null)
//            {
//                return;
//            }
//            string crTree = completeReparse.ToStringAsTree();
//            string ptTree = _parseTree.ToStringAsTree();
//            if (crTree != ptTree)
//            {
//                Console.WriteLine(">>> Consistency check: different trees:");
//                Console.WriteLine(">>> Expected (complete reparse):");
//                Console.WriteLine(crTree);
//                Console.WriteLine(">>> Actual (current tree):");
//                Console.WriteLine(ptTree);
//                throw new InvalidOperationException("Synpl Consistency Check.");
//            }
        }
        #endregion

        #region Editor Event Handlers
        private void HandleTextChanged(object sender, TextChangedEventArgs e)
        {
            TryParsing(e);
            UpdateFormatting();
            ConsistencyCheck();
        }
        
        #endregion

    }
}
