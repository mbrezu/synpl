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
    public class CharWithPosition
    {
        #region Private Storage
        private int _position;
        private char _char;
        #endregion

        #region Properties
        public char Char {
            get {
                return _char;
            }
        }

        public int Position {
            get {
                return _position;
            }
        }
        #endregion

        #region Constructor
        public CharWithPosition(int position, char ch)
        {
            _position = position;
            _char = ch;
        }
        #endregion

        #region Public Methods
        public override string ToString ()
        {
            return string.Format("new CharWithPosition({1}, '{0}')", Char, Position);
        }

        public override bool Equals (object obj)
        {
            CharWithPosition other = obj as CharWithPosition;
            if (other == null)
            {
                return false;
            }
            return _position == other._position && _char == other._char;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        #endregion
    }
}
