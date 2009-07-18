// Synpl - a "structured editor" plugin for Gedit.
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
    // TODO: These need to be tested in TestMockEditor.cs.
    public static class AbstractEditorOperations
    {        
        public static int MoveForwardLines(this IAbstractEditor _this, int howMany)
        {
            int result = 0;
            bool forward = howMany > 0;
            int lastLine, lastColumn;
            _this.OffsetToLineColumn(_this.Length, out lastLine, out lastColumn);
            while (howMany != 0)
            {
                int currentLine, currentColumn;
                _this.OffsetToLineColumn(_this.CursorOffset, out currentLine, out currentColumn);
                if (forward && currentLine == lastLine)
                {
                    break;
                }
                else if (!forward && currentLine == 0)
                {
                    break;
                }
                int newLine;
                if (forward)
                {
                    newLine = currentLine + 1;
                }
                else
                {
                    newLine = currentLine - 1;
                }
                int newColumn = currentColumn;
                int lastColumnOnNewLine = _this.LastColumnOnLine(newLine);
                if (newColumn > lastColumnOnNewLine)
                {
                    newColumn = lastColumnOnNewLine;
                }
                _this.CursorOffset = _this.LineColumnToOffset(newLine, newColumn);
                if (forward) 
                {
                    howMany --;
                }
                else
                {
                    howMany ++;
                }
                result ++;                    
            }
            return result;
        }

        public static int MoveForwardChars(this IAbstractEditor _this, int howMany)
        {
            int result = 0;
            bool forward = howMany > 0;
            while (howMany != 0)
            {
                if (forward && _this.CursorOffset >= _this.Length - 1)
                {
                    break;
                }
                else if (!forward && _this.CursorOffset == 0)
                {
                    break;
                }
                if (forward) 
                {
                    _this.CursorOffset ++;
                    howMany --;
                }
                else
                {
                    _this.CursorOffset --;
                    howMany ++;
                }
                result ++;
            }
            return result;
        }

        public static bool MoveToStartOfLine(this IAbstractEditor _this)
        {
            int line, column;
            _this.OffsetToLineColumn(_this.CursorOffset, out line, out column);
            bool result = _this.CursorOffset != _this.OffsetStartLine(line);
            _this.CursorOffset = _this.OffsetStartLine(line);
            return result;
        }
        
        public static bool MoveToEndOfLine(this IAbstractEditor _this)
        {
            int line, column;
            _this.OffsetToLineColumn(_this.CursorOffset, out line, out column);
            column = _this.LastColumnOnLine(line);
            bool result = _this.CursorOffset != _this.LineColumnToOffset(line, column);
            _this.CursorOffset = _this.LineColumnToOffset(line, column);
            return result;
        }
        
        public static int LastColumnOnLine(this IAbstractEditor _this, int line)
        {
            int lastLine, lastColumn;
            _this.OffsetToLineColumn(_this.Length, out lastLine, out lastColumn);
            // This is a hack workaround for a Gtk bug (LineColumnToOffset returns
            // a bogus result if the line/column are out of range).
            if (line == lastLine)
            {
                return lastColumn;
            }
            if (line > lastLine)
            {
                throw new ArgumentOutOfRangeException("Line is out of range.");
            }
            int offset = _this.OffsetStartLine(line + 1) - 1;
            if (offset > _this.Length - 1)
            {
                offset = _this.Length - 1;
            }
            int dummyLine, column;
            _this.OffsetToLineColumn(offset, out dummyLine, out column);
            return column;
        }

        public static int OffsetStartLine(this IAbstractEditor _this, int line)
        {
            return _this.LineColumnToOffset(line, 0);
        }       
        
    }
}
