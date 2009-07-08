// // Synpl - a "structured editor" plugin for Gedit.
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
        public Key(string keycode)
        {
            _keycode = keycode;
        }
        
        public Key(string keycode, bool shift, bool control, bool alt)
        {
            _keycode = keycode;
            _shift = shift;
            _control = control;
            _alt = alt;
        }
        #endregion

        #region Public Constructor
        public override string ToString ()
        {
            return string.Format("[Key: Alt={0}, Control={1}, Shift={2}, KeyCode={3}]", 
                                 Alt, 
                                 Control, 
                                 Shift, 
                                 KeyCode);
        }

        #endregion
    }
}
