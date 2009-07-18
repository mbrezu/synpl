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
using System.Collections.Generic;
using System.Text;
using Synpl.EditorAbstraction;

namespace Synpl.Core
{
    public abstract class ParseTree
    {
        #region Private Storage
        private int _startPosition;
        private int _endPosition;
        private CowList<ParseTree>  _subTrees;
        private Parser _parser;
        private ParseTree _parent;
        private TextWithChanges _text;
        private string _label;
        #endregion

        #region Properties
        public int EndPosition {
            get {
                return _endPosition;
            }
        }

        public ParseTree Parent {
            get {
                return _parent;
            }
            set
            {
                _parent = value;
            }
        }

        public Parser Parser {
            get {
                return _parser;
            }
        }

        public int StartPosition {
            get {
                return _startPosition;
            }
        }

        public CowList<ParseTree> SubTrees {
            get {
                return _subTrees;
            }
        }

        public TextWithChanges Text {
            get {
                return _text;
            }
        }

        public string Label {
            get {
                return _label;
            }
        }

        #endregion

        #region Constructor
        public ParseTree(int startPosition, 
                         int endPosition, 
                         CowList<ParseTree> subTrees, 
                         Parser parser,
                         ParseTree parent,
                         TextWithChanges text,
                         string label)
        {
            _startPosition = startPosition;
            _endPosition = endPosition;
            _subTrees = subTrees;
            _parser = parser;
            _parent = parent;
            _text = text;
            _label = label;
            foreach (ParseTree node in _subTrees)
            {
                node._parent = this;
            }
        }
        #endregion

        #region Public Methods
        public virtual string ToStringAsLabel()
        {
            return _label;
        }

        public virtual string ToStringAsTree()
        {
            return ToStringAsTree("");
        }

        public override string ToString ()
        {
            return string.Format("[ParseTree: StartPosition={0}, EndPosition={1}, SubTrees={2}]", 
                                 StartPosition, 
                                 EndPosition, 
                                 SubTrees.Count);
        }

        /// <summary>
        /// Returns the path from the root to the current node as a list of integers.
        /// </summary>
        /// <returns>
        /// A <see cref="List"/> list of integers representing the path.
        ///
        /// An empty list means the node is the root.
        /// 
        /// An [1] list means the node is the second (counting from 0) subtree of the root.
        /// 
        /// An [1, 0] list means the node is the first subtree
        /// of the second subtree of the root.
        /// </returns>
        public CowList<int> GetPath()
        {
            int index = GetIndexInParent();
            if (index == -1)
            {
                return new CowList<int>();
            }
            else
            {
                CowList<int> result = _parent.GetPath();
                result.Add(index);
                return result;
            }
        }

        public ParseTree GetNodeAtPath(CowList<int> path)
        {
            if (path.Count == 0)
            {
                return this;
            }
            int head = path[0];
            if (head >= 0 && head < _subTrees.Count)
            {
                return _subTrees[head].GetNodeAtPath(path.Tail);
            }
            throw new ArgumentException("Invalid path.");
        }

        public bool Contains(int position)
        {
            return position >= _startPosition && position < _endPosition;
        }

        public ParseTree HasEndPosition(int position)
        {
            if (position == _endPosition)
            {
                return this;
            }
            foreach (ParseTree tree in _subTrees)
            {
                ParseTree pt = tree.HasEndPosition(position);
                if (pt != null)
                {
                    return pt;
                }
            }
            return null;
        }

        public CowList<ParseTree> GetPathForPosition(int position)
        {            
            if (!Contains(position)) {
                return new CowList<ParseTree>();
            }
            CowList<ParseTree> result = new CowList<ParseTree>();
            result.Add(this);
            foreach (ParseTree subTree in _subTrees)
            {
                if (position < subTree.StartPosition)
                {
                    break;
                }
                if (subTree.Contains(position))
                {
                    result.AddRange(subTree.GetPathForPosition(position));
                    break;
                }
            }
            return result;
        }

        public int GetIndexInParent()
        {
            if (_parent == null)
            {
                return -1;
            }
            for (int i = 0; i < _parent._subTrees.Count; i++)
            {
                if (_parent._subTrees[i] == this)
                {
                    return i;
                }
            }
            throw new InvalidOperationException("Node not found in parent.");
        }

        public ParseTree GetPreviousSibling()
        {
            int index = GetIndexInParent();
            if (index < 1)
            {
                return null;
            }
            return _parent._subTrees[index - 1];
        }

        public ParseTree GetNextSibling()
        {
            int index = GetIndexInParent();
            if (index == -1 || index == _parent._subTrees.Count - 1)
            {
                return null;
            }
            return _parent._subTrees[index + 1];
        }

        public ParseTree MoveUp()
        {
            ParseTree previous = GetPreviousSibling();
            if (previous == null)
            {
                return null;
            }
            
            TextWithChanges thisTwcSlice = _text.GetSliceWithChanges(_startPosition, _endPosition);
            _text.RemoveSliceWithChanges(_startPosition, _endPosition, true);
            TextWithChanges previousTwcSlice = _text.GetSliceWithChanges(previous._startPosition,
                                                                         previous._endPosition);
            _text.RemoveSliceWithChanges(previous._startPosition, previous._endPosition, true);
            
            _text.InsertSliceWithChanges(previous._startPosition, thisTwcSlice, true);
            int adjustedSelfStartPosition = _startPosition +
                thisTwcSlice.GetActualLength() -
                    previousTwcSlice.GetActualLength();
            _text.InsertSliceWithChanges(adjustedSelfStartPosition, previousTwcSlice, true);
            // Use old version because we don't want extra unparsed
            // characters in any of the parent's children (including the
            // two being swapped) to break the parse.
            ParseTree reparsedRoot = _parent.TryReparse(true);

            if (reparsedRoot != null)
            {
                return reparsedRoot;
            }
            else
            {
                // If the reparse is not successful, we need to swap back
                // the nodes - moving up or down should not break the parse.
                // 
                // CAUTION: this piece of code is not tested with the sexp
                // parser, because swapping elements in a list is always
                // parsable. We need some kind of MiniPascal or similar to
                // test it.
                _text.RemoveSliceWithChanges(previous._startPosition,
                                             previous._startPosition + thisTwcSlice.GetActualLength(),
                                             true);
                _text.RemoveSliceWithChanges(adjustedSelfStartPosition,
                                             _startPosition + previousTwcSlice.GetActualLength(),
                                             true);
                _text.InsertSliceWithChanges(previous._startPosition, previousTwcSlice, true);
                _text.InsertSliceWithChanges(_startPosition, thisTwcSlice, true);
                return null;
            }
        }

        public ParseTree MoveDown()
        {
            ParseTree next = GetNextSibling();
            if (next == null)
            {
                return null;
            }
            return next.MoveUp();
        }

        public CowList<CharWithPosition> RepresentAsCode(bool useOldVersion)
        {
            if (useOldVersion)
            {
                return _text.GetOldSlice(_startPosition, _endPosition);
            }
            else
            {
                return _text.GetCurrentSlice(_startPosition, _endPosition);
            }
        }

        public string ToStringAsCode(bool useOldVersion)
        {
            return CharListToString(RepresentAsCode(useOldVersion));
        }

        public ParseTree GetRoot()
        {
            if (_parent == null)
            {
                return this;
            }
            return _parent.GetRoot();
        }

        public ParseTree GetNodeOrThis(int position)
        {
            CowList<ParseTree> path = GetPathForPosition(position);
            ParseTree nodeAffected = null;
            if (path.Count == 0)
            {
                nodeAffected = this;
            }
            else
            {
                nodeAffected = path[path.Count - 1];
            }
            return nodeAffected;
        }

        // This function is to be used when the parse tree inserts a character.
        // It doesn't trigger a reparse.
        public void InsertCharAt(char ch, int position)
        {
            ParseTree nodeAffected = GetNodeOrThis(position);
            nodeAffected._text.InsertChar(ch, position, true);
            nodeAffected._endPosition ++;
            nodeAffected.OffsetSubtreesPositionBy(position, 1);
            nodeAffected.OffsetSuccessorsPositionBy(1);
        }

        // This function is to be used when the parse tree is notified of a character
        // insertion in the editor.
        public ParseTree CharInsertedAt(char ch, int position)
        {
            ParseTree nodeAffected = GetNodeOrThis(position);
            nodeAffected._text.InsertChar(ch, position);
            nodeAffected._endPosition ++;
            nodeAffected.OffsetSubtreesPositionBy(position, 1);
            nodeAffected.OffsetSuccessorsPositionBy(1);
            return nodeAffected.ReparseAndValidateRecursively();
        }

        // This function is to be used when the parse tree deletes a character.
        // It doesn't trigger a reparse.
        public void DeleteCharAt(int position)
        {
            ParseTree nodeAffected = GetNodeOrThis(position);
            nodeAffected._text.DeleteChar(position, true);
            nodeAffected._endPosition--;
            nodeAffected.OffsetSubtreesPositionBy(position, -1);
            nodeAffected.OffsetSuccessorsPositionBy(-1);
        }

        // This function is to be used when the parse tree is notified of a character
        // deletion in the editor.
        public ParseTree CharDeletedAt(int position)
        {
            ParseTree nodeAffected = GetNodeOrThis(position);
            nodeAffected._text.DeleteChar(position);
            nodeAffected._endPosition--;
            nodeAffected.OffsetSubtreesPositionBy(position, -1);
            nodeAffected.OffsetSuccessorsPositionBy(-1);
            return nodeAffected.ReparseAndValidateRecursively();
        }
        
        public IEnumerable<ParseTree>AllNodes()
        {
            Stack<ParseTree> stack = new Stack<ParseTree>();
            stack.Push(this);
            while (stack.Count > 0)
            {
                ParseTree top = stack.Pop();
                yield return top;
                for (int i = top.SubTrees.Count - 1; i >= 0; i--)
                {
                    stack.Push(top.SubTrees[i]);
                }
            }
        }
        
        public ParseTree GetFirstNodeAfter(int position)
        {
            foreach (ParseTree tree in AllNodes())
            {
                if (tree.StartPosition >= position)
                {
                    return tree;
                }
            }
            return null;
        }

        public ParseTree GetLastNodeBefore(int position)
        {
            ParseTree candidate = null;
            foreach (ParseTree tree in AllNodes())
            {
                if (tree.StartPosition >= position)
                {
                    break;
                }
                if (tree.EndPosition <= position)
                {
                    candidate = tree;
                }
            }
            return candidate;
        }

        public ParseTree GetLastSiblingBefore(int position)
        {
            if (_parent == null)
            {
                return null;
            }
            ParseTree candidate = null;
            foreach (ParseTree tree in _parent.SubTrees)
            {
                if (tree.StartPosition >= position)
                {
                    break;
                }
                if (tree.EndPosition <= position)
                {
                    candidate = tree;
                }
            }
            return candidate;
        }

        public ParseTree GetFirstSiblingAfter(int position)
        {
            if (_parent == null)
            {
                return null;
            }
            foreach (ParseTree tree in _parent.SubTrees)
            {
                if (tree.StartPosition >= position)
                {
                    return tree;
                }
            }
            return null;
        }

        public bool HasUnparsedChanges()
        {
            return ToStringAsCode(true) != ToStringAsCode(false);
        }

        // TODO: Add unit test.
        //
        // The unit tests for pretty printing and indenting should check the behaviour in
        // the presence of unparsed text changes.
        //
        // TODO: if we have unparsed text changes, try to pretty print the subtrees. This
        // is a 'best-effort' approach, as opposed to just doing nothing if we have 
        // unparsed text changes.
        //
        // TODO: Pretty printing should preserve the position of the cursor (relative to the
        // inner most node.
        public ParseTree PrettyPrint(int maxColumn, IAbstractEditor editor)
        {
            if (HasUnparsedChanges())
            {
                return GetRoot();
            }
            int line, column;
            editor.OffsetToLineColumn(StartPosition, out line, out column);
            string prettyPrint = ToStringAsPrettyPrint(column, maxColumn);
            int length = EndPosition - StartPosition;
            for (int i = 0; i < length; i++)
            {
                DeleteCharAt(StartPosition);
            }
            Console.WriteLine("pp: {0}", prettyPrint);
            for (int i = 0; i < prettyPrint.Length; i++)
            {
                InsertCharAt(prettyPrint[i], StartPosition + i);
            }
            return ReparseAndValidateRecursively();
        }

        // TODO: Add unit test.
        public virtual string ToStringAsPrettyPrint(int indentLevel, int maxColumn)
        {
            return ToStringAsCode(false);
        }

        // TODO: Add unit test.
        public virtual ParseTree Indent(int position, IAbstractEditor editor)
        {
            int currentLine, currentColumn;
            editor.OffsetToLineColumn(position, out currentLine, out currentColumn);
            if (currentLine == 0)
            {
                return GetRoot();
            }
            Console.WriteLine("!!! Indenting:");
            Console.WriteLine("current: {0} {1} {2}", position, currentLine, currentColumn);
            int lineStartOffset = editor.LineColumnToOffset(currentLine, 0);
            int testLine, testColumn;
            editor.OffsetToLineColumn(lineStartOffset, out testLine, out testColumn);
            Console.WriteLine("line start:{0}", lineStartOffset);
            ParseTree lineStarter = GetFirstNodeAfter(lineStartOffset);
            if (lineStarter == null)
            {
                return GetRoot();
            }
            ParseTree lastSibling = lineStarter.GetLastSiblingBefore(lineStartOffset);
            int desiredIndentColumn;
            if (lastSibling != null)
            {
                int indentLine, indentOffset;
                Console.WriteLine("indent by sibling");
                indentOffset = lastSibling.StartPosition;
                editor.OffsetToLineColumn(indentOffset, out indentLine, out desiredIndentColumn);
            }
            else if (lineStarter.Parent != null)
            {
                Console.WriteLine("indent by parent");
                int indentLine, indentOffset;
                indentOffset = lineStarter.Parent.StartPosition;
                editor.OffsetToLineColumn(indentOffset, out indentLine, out desiredIndentColumn);
                desiredIndentColumn += 2;
            }
            else
            {
                return GetRoot();
            }
            Console.WriteLine("desired column: {0}", desiredIndentColumn);
            int lineStarterLine, lineStarterColumn;
            editor.OffsetToLineColumn(lineStarter.StartPosition, 
                                       out lineStarterLine,
                                       out lineStarterColumn);
            Console.WriteLine("found: {0} {1} {2}", 
                              lineStarter.StartPosition, 
                              lineStarterLine, 
                              lineStarterColumn);
            if (lineStarterColumn < desiredIndentColumn)                
            {
                Console.WriteLine("adding {0}", lineStartOffset);
                for (int i = 0; i < desiredIndentColumn - lineStarterColumn; i++)
                {
                    InsertCharAt(' ', lineStartOffset);
                }
                return ReparseAndValidateRecursively();
            }
            else if (lineStarterColumn > desiredIndentColumn)
            {
                Console.WriteLine("deleting");
                for (int i = 0; i <  lineStarterColumn - desiredIndentColumn; i++)
                {
                    DeleteCharAt(lineStartOffset);
                }
                return ReparseAndValidateRecursively();
            }
            return GetRoot();
        }
        #endregion

        #region Private Helper Methods

        private ParseTree ReparseAndValidateRecursively()
        {
            ParseTree reparsedNode = TryReparse(false);
            if (reparsedNode != null)
            {
                reparsedNode.AdjustCoordinates(this);
                reparsedNode._text.ValidateSlice(reparsedNode._startPosition,
                                                 reparsedNode._endPosition);
                return reparsedNode.GetRoot();
            }
            else
            {
                if (_parent != null)
                {
                    return _parent.ReparseAndValidateRecursively();
                }
                else
                {
                    return GetRoot();
                }
            }
        }

        private ParseTree TryReparse(bool useOldVersion)
        {
            ParseTree result = null;
            try
            {
                result = Reparse(useOldVersion);
            }
            catch (ParseException)
            {
                return null;
            }
            return result;
        }

        private ParseTree Reparse(bool useOldVersion)
        {
            CowList<CharWithPosition> code = RepresentAsCode(useOldVersion);
            CowList<Token> tokens = _parser.TokenizerFunc(code);
            ParseTree reparsedSelf;
            CowList<Token> tokensRest;
            _parser.ParserFunc(tokens, _text, out reparsedSelf, out tokensRest);
            if (tokensRest.Count > 0)
            {
                throw new ParseException("Extra tokens in stream.");
            }
            int index = GetIndexInParent();
            if (index != -1)
            {
                _parent.SubTrees[index] = reparsedSelf;
                reparsedSelf._parent = _parent;
                _parent = null;
            }
            reparsedSelf.AdjustCoordinates(this);
            return reparsedSelf.GetRoot();
        }

        private void AdjustCoordinates(ParseTree oldNode)
        {
            int offset = _endPosition - oldNode._endPosition;
            if (offset != 0)
            {
                OffsetSuccessorsPositionBy(offset);
            }
        }

        private void OffsetPositionBy(int offset)
        {
            _startPosition += offset;
            _endPosition += offset;
            foreach (ParseTree node in _subTrees)
            {
                node.OffsetPositionBy(offset);
            }
        }

        private void OffsetSubtreesPositionBy(int position, int offset)
        {
            foreach (ParseTree subtree in _subTrees)
            {
                if (subtree.StartPosition >= position)
                {
                    subtree.OffsetPositionBy(offset);
                }
            }
        }

        private void OffsetSuccessorsPositionBy(int offset)
        {
            int index = GetIndexInParent();
            if (index == -1)
            {
                return;
            }            
            for (int i = index + 1; i < _parent._subTrees.Count; i++)
            {
                _parent.SubTrees[i].OffsetPositionBy(offset);
            }
            _parent._endPosition += offset;
            _parent.OffsetSuccessorsPositionBy(offset);
        }        

        private string ToStringAsTree(string indent)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(String.Format("{0}({1},{2}): {3}\n", 
                                    indent,
                                    _startPosition,
                                    _endPosition,
                                    ToStringAsLabel()));
            foreach (ParseTree node in _subTrees)
            {
                sb.Append(node.ToStringAsTree(indent  + "  "));
            }
            return sb.ToString();
        }
        
        private static string CharListToString(CowList<CharWithPosition> chars)
        {
            StringBuilder sb = new StringBuilder();
            foreach (CharWithPosition ch in chars)
            {
                sb.Append(ch.Char);
            }
            return sb.ToString();                
        }
        #endregion
    }
}
