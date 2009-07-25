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

namespace Synpl.Core
{
    public class TextChange : IComparable
    {
        #region Private Storage
        private int _position;
        private bool _isDeletion;
        #endregion

        #region Properties
        public bool IsDeletion {
            get {
                return _isDeletion;
            }
        }

        public int Position {
            get {
                return _position;
            }
        }
        #endregion
        
        #region Constructor
        public TextChange(int position, bool isDeletion)
        {
            _position = position;
            _isDeletion = isDeletion;
        }
        #endregion

        #region Public Methods
        public TextChange Moved(int offset)
        {
            return new TextChange(_position + offset, _isDeletion);
        }

        public bool IsBetween(int start, int end)
        {
            return _position >= start && _position < end;
        }
        #endregion

        #region IComparable Implementation
        public int CompareTo(object other)
        {
            TextChange tcOther = other as TextChange;
            if (tcOther == null) {
                return 0;
            }
            return _position - tcOther.Position;
        }
        #endregion
    }
}
