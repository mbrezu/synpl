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

namespace Synpl.EditorAbstraction
{
	public class FormattingHint 
	{
		#region Private Storage
		private int _start;
		private int _end;
		private string _hint;
		#endregion
		
		#region Properties
		public int Start {
			get {
				return _start;
			}
		}
        public int End {
            get {
                return _end;
            }
        }       
		public string Hint {
			get {
				return _hint;
			}
		}
		#endregion
		
		#region Constructor
		public FormattingHint(int start, int end, string hint)
		{
			_start = start;
			_end = end;
			_hint = hint;
		}
		#endregion

        #region Public Methods
        public override string ToString ()
        {
            return string.Format("[FormattingHint: Start={0}, End={1}, Hint={2}]", Start, End, Hint);
        }
        #endregion
	}
}
