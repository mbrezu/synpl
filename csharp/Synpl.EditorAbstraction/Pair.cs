// Synpl - a "structured editor".
// Copyright (C) 2009  Miron Brezuleanu <mbrezu@gmail.com>
// 
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
// 

using System;

namespace Synpl.EditorAbstraction
{
    public class Pair<T, U>
    {
        private T _first;        
        private U _second;

        public T First
        {
            get
            {
                return _first;
            }
        }

        public U Second
        {
            get
            {
                return _second;
            }
        }
        
        public Pair(T first, U second) {
            _first = first;
            _second = second;
        }

        public override string ToString ()
        {
            return string.Format("[Pair: First={0}, Second={1}]", First, Second);
        }

        public override int GetHashCode ()
        {
            return ToString().GetHashCode();
        }

        public override bool Equals (object obj)
        {
            Pair<T, U> other = obj as Pair<T, U>;
            if (other == null)
            {
                return false;
            }
            return ToString() == other.ToString();
        }

    }
}
