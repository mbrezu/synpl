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

namespace Synpl.EditorAbstraction
{
	public class TextChangedEventArgs : EventArgs
	{
		public enum OperationType { Insertion, Deletion };

		#region Private Storage
		private OperationType _operation;
		private int _start;
		private int _length;
		private string _text;
		#endregion

		#region Properties
		public int Length {
			get {
				return _length;
			}
		}

		public int Start {
			get {
				return _start;
			}
		}

		public string Text {
			get {
				return _text;
			}
		}

		public OperationType Operation {
			get {
				return _operation;
			}
		}		
		#endregion
		
		#region Constructor
		public TextChangedEventArgs(OperationType operation, int start, int length, string text)
		{
			_operation = operation;
			_start = start;
			_length = length;
			_text = text;
		}
		#endregion

        #region Public Methods
        public override string ToString ()
        {
            return string.Format("[TextChangedEventArgs: Start={1}, Text='{2}', Length={0}, Operation={3}]", 
                                 Length, 
                                 Start, 
                                 Text, 
                                 Operation);
        }

        #endregion
	}
}
