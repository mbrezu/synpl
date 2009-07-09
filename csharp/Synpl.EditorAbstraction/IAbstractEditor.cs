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

namespace Synpl.EditorAbstraction
{	
	public interface IAbstractEditor
	{
        #region Navigation
        int MoveForwardLines(int howMany);
        int MoveForwardChars(int howMany);
        bool MoveToStartOfLine();
        bool MoveToEndOfLine();
        #endregion
        
		#region Coordinate Conversion
		int CursorOffset { get; set; }
		void GetSelection(out int start, out int end);
		void SetSelection(int start, int end);		
		int Length { get; }
        int LineColumnToOffset(int line, int column);
        void OffsetToLineColumn(int offset, out int line, out int column);
		#endregion
		
		#region Text Manipulation
		void InsertText(int position, string text, bool inhibitTextChanged);
		void DeleteText(int position, int length, bool inhibitTextChanged);	
		string GetText(int position, int length);
		#endregion
		
		#region Text Changes
		event EventHandler<TextChangedEventArgs> TextChanged;
		#endregion
		
		#region Formatting
		void RequestFormatting(int start, int end, List<FormattingHint> hints);
		List<string> KnownFormattingHints();
		#endregion

        #region Keyboard events
        event EventHandler<KeyStrokeEventArgs> KeyStroke;
        bool Editable { get; set; }
        #endregion
	}
}
