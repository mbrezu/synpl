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

namespace Synpl.Core
{    
    public class Token
    {
        #region Private Storage
        private int _kind;
        private string _content;
        private int _startPosition;
        private int _endPosition;
        #endregion

        #region Properties
        public string Content {
            get {
                return _content;
            }
        }

        public int EndPosition {
            get {
                return _endPosition;
            }
        }

        public int Kind {
            get {
                return _kind;
            }
        }

        public int StartPosition {
            get {
                return _startPosition;
            }
        }
        #endregion

        #region Constructor
        public Token(int kind, string content, int startPosition, int endPosition)
        {
            _kind = kind;
            _content = content;
            _startPosition = startPosition;
            _endPosition = endPosition;
        }        
        #endregion

        #region Public Methods
        public Token OffsetPositionBy(int positionOffset)
        {
            return new Token(_kind, 
                             _content, 
                             _startPosition + positionOffset, 
                             _endPosition + positionOffset);
        }
        
        public override string ToString ()
        {
            return string.Format("[Token:  Kind={0}, Content={1}, StartPosition={2}, EndPosition={3}]", 
                                 Kind, 
                                 Content, 
                                 StartPosition, 
                                 EndPosition);
        }
        
        public override bool Equals (object obj)
        {
            Token other = obj as Token;
            if (obj == null)
            {
                return false;
            }
            return _content == other._content 
                && _kind == other._kind
                    && _startPosition == other._startPosition
                    && _endPosition == other._endPosition;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        #endregion
    }
}
