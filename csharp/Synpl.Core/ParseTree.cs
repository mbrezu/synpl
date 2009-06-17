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

namespace Synpl.Core
{
    public abstract class ParseTree
    {
        #region Private Storage
        private int _startPosition;
        private int _endPosition;
        private List<ParseTree>  _subTrees;
        private Parser _parser;
        private ParseTree _parent;
        private TextWithChanges _text;
        private int _id;
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

        public List<ParseTree> SubTrees {
            get {
                return _subTrees;
            }
        }

        public TextWithChanges Text {
            get {
                return _text;
            }
        }

        public int Id {
            get {
                return _id;
            }
        }
        #endregion

        #region Constructor
        public ParseTree(int startPosition, 
                         int endPosition, 
                         List<ParseTree> subTrees, 
                         Parser parser,
                         ParseTree parent,
                         TextWithChanges text)
        {
            _startPosition = startPosition;
            _endPosition = endPosition;
            _subTrees = subTrees;
            _parser = parser;
            _parent = parent;
            _text = text;
            _id = parser.NewId();
        }
        #endregion

        #region Public Abstract Methods
        public abstract string ToStringAsLabel();
        #endregion

        #region Public Abstract Methods
        public string ToStringAsTree()
        {
            return ToStringAsTree("");
        }

        public override string ToString ()
        {
            return string.Format("[ParseTree: StartPosition={0}, EndPosition={1}, SubTrees={2}, Parent={3}, Parser={4}, Text={5}]", 
                                 StartPosition, 
                                 EndPosition, 
                                 SubTrees, 
                                 Parent, 
                                 Parser, 
                                 Text);
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
        public List<int> GetPath()
        {
            int index = GetIndexInParent();
            if (index == -1)
            {
                return new List<int>();
            }
            else
            {
                List<int> result = _parent.GetPath();
                result.Add(index);
                return result;
            }
        }

        public ParseTree GetNodeAtPath(List<int> path)
        {
            if (path.Count == 0)
            {
                return this;
            }
            int head = path[0];
            if (head > 0 && head < _subTrees.Count)
            {
                return _subTrees[head].GetNodeAtPath(path.GetRange(1, path.Count - 1));
            }
            throw new ArgumentException("Invalid path.");
        }

        public bool Contains(int position)
        {
            return position >= _startPosition && position < _endPosition;
        }

        public List<ParseTree> GetPathForPosition(int position)
        {            
            if (!Contains(position)) {
                return new List<ParseTree>();
            }
            List<ParseTree> result = new List<ParseTree>();
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
                if (_parent._subTrees[i].Id == _id)
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
            _text.RemoveSliceWithChanges(_startPosition, _endPosition);
            TextWithChanges previousTwcSlice = _text.GetSliceWithChanges(previous._startPosition,
                                                                         previous._endPosition);
            _text.RemoveSliceWithChanges(previous._startPosition, previous._endPosition);
            
            _text.InsertSliceWithChanges(previous._startPosition, thisTwcSlice);
            int adjustedSelfStartPosition = _startPosition +
                thisTwcSlice.GetActualLength() -
                    previousTwcSlice.GetActualLength();
            _text.InsertSliceWithChanges(adjustedSelfStartPosition, previousTwcSlice);
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
                                             previous._startPosition + thisTwcSlice.GetActualLength());
                _text.RemoveSliceWithChanges(adjustedSelfStartPosition,
                                             _startPosition + previousTwcSlice.GetActualLength());
                _text.InsertSliceWithChanges(previous._startPosition, previousTwcSlice);
                _text.InsertSliceWithChanges(_startPosition, thisTwcSlice);
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

        public List<CharWithPosition> ToStringAsCode(bool useOldVersion)
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

        public ParseTree GetRoot()
        {
            if (_parent == null)
            {
                return this;
            }
            return _parent.GetRoot();
        }

        public ParseTree CharInsertedAt(char ch, int position)
        {
            List<ParseTree> path = GetPathForPosition(position);
            ParseTree nodeAffected = null;
            if (path.Count == 0)
            {
                nodeAffected = this;
            }
            else
            {
                nodeAffected = path[path.Count - 1];
            }
            nodeAffected._text.InsertChar(ch, position);
            nodeAffected._endPosition ++;
            nodeAffected.OffsetSuccessorsPositionBy(1);
            return nodeAffected.ReparseAndValidateRecursively();
        }

        // The first node that doesn't parse after a delete should be
        // marked with squigglies or similar marks to show that something
        // is missing there and the editor parser doesn't use the visible
        // text.
        public ParseTree CharDeletedAt(int position)
        {
            List<ParseTree> path = GetPathForPosition(position);
            ParseTree nodeAffected = null;
            if (path.Count == 0)
            {
                nodeAffected = this;
            }
            else
            {
                nodeAffected = path[path.Count - 1];
            }
            nodeAffected._text.DeleteChar(position);
            nodeAffected._endPosition--;
            nodeAffected.OffsetSuccessorsPositionBy(-1);
            return nodeAffected.ReparseAndValidateRecursively();
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
            List<CharWithPosition> code = ToStringAsCode(useOldVersion);
            List<Token> tokens = _parser.TokenizerFunc(code);
            ParseTree reparsedSelf;
            List<Token> tokensRest;
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
        #endregion
    }
}
