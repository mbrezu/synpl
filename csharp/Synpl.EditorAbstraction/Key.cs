// // Synpl - a "structured editor".
// // Copyright (C) 2009  Miron Brezuleanu <mbrezu@gmail.com>
// 
// // This program is free software; you can redistribute it and/or modify
// // it under the terms of the GNU General Public License as published by
// // the Free Software Foundation; either version 2 of the License, or
// // (at your option) any later version.
// 
// // This program is distributed in the hope that it will be useful,
// // but WITHOUT ANY WARRANTY; without even the implied warranty of
// // MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// // GNU General Public License for more details.
// 
// // You should have received a copy of the GNU General Public License along
// // with this program; if not, write to the Free Software Foundation, Inc.,
// // 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

using System;
using System.Text;

namespace Synpl.EditorAbstraction
{
    public class Key
    {
        #region Private Storage
        private string _keycode = null;
        private bool _shift = false;
        private bool _control = false;
        private bool _alt = false;
        #endregion

        #region Properties
        public bool Alt {
            get {
                return _alt;
            }
        }

        public bool Control {
            get {
                return _control;
            }
        }

        public string KeyCode {
            get {
                return _keycode;
            }
        }

        public bool Shift {
            get {
                return _shift;
            }
        }
        #endregion

        #region Constructor
        public Key(string keycode, bool shift, bool control, bool alt)
        {
            _keycode = keycode;
            _shift = shift;
            _control = control;
            _alt = alt;
        }

        public Key(string emacsKeyStr)
        {
            int parseOffset = 0;
            while (parseOffset < emacsKeyStr.Length)
            {
                if (parseOffset + 2 <= emacsKeyStr.Length 
                    && emacsKeyStr.Substring(parseOffset, 2) == "C-")
                {
                    parseOffset += 2;
                    _control = true;
                }
                else if (parseOffset + 2 <= emacsKeyStr.Length 
                         && emacsKeyStr.Substring(parseOffset, 2) == "S-")
                {
                    parseOffset += 2;
                    _shift = true;
                }
                else if (parseOffset + 2 <= emacsKeyStr.Length 
                         && emacsKeyStr.Substring(parseOffset, 2) == "A-")
                {
                    parseOffset += 2;
                    _alt = true;
                }
                else
                {
                    _keycode = emacsKeyStr.Substring(parseOffset);
                    parseOffset = emacsKeyStr.Length;
                }
            }
        }
        #endregion

        #region Public Constructor
        public override string ToString ()
        {
            StringBuilder sb = new StringBuilder();
            if (_control)
            {
                sb.Append("C-");
            }
            if (_alt)
            {
                sb.Append("A-");
            }
            if (_shift)
            {
                sb.Append("S-");
            }
            sb.Append(_keycode);
            return sb.ToString();
        }

        public override bool Equals (object obj)
        {
            Key other = obj as Key;
            if (other == null)
            {
                return false;
            }
            return _keycode.ToLower() == other._keycode.ToLower()
                   && _alt == other._alt
                   && _control == other._control
                   && _shift == other._shift;
        }

        public override int GetHashCode ()
        {
            return ToString().GetHashCode();
        }
        #endregion
    }
}
