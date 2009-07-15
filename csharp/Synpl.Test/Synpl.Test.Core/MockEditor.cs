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
using Synpl.EditorAbstraction;
using System.Collections.Generic;

namespace Synpl.Test.Core
{
    // TODO: Add unit tests.
    public class MockEditor : IAbstractEditor
    {
        #region Constructor
        public MockEditor()
        {
        }
        #endregion

        #region Public Methods
        public void SimulateKeyStroke(Synpl.EditorAbstraction.Key keystroke)
        {
            OnKeyStroke(new KeyStrokeEventArgs(keystroke));
        }
        #endregion

        #region IAbstractEditor Implementation
        
        #region Navigation
        // TODO: MoveForwardLines, MoveForwardChars, MoveToStartOfLine,
        // MoveToEndOfLine, LastColumnOnLine, OffsetStartLine should become        
        // extension methods for IAbstractEditor (a "mixin")
        public int MoveForwardLines(int howMany)
        {
            int result = 0;
            bool forward = howMany > 0;
            int lastLine, lastColumn;
            OffsetToLineColumn(Length, out lastLine, out lastColumn);
            while (howMany != 0)
            {
                int currentLine, currentColumn;
                OffsetToLineColumn(CursorOffset, out currentLine, out currentColumn);
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
                int lastColumnOnNewLine = LastColumnOnLine(newLine);
                if (newColumn > lastColumnOnNewLine)
                {
                    newColumn = lastColumnOnNewLine;
                }
                CursorOffset = LineColumnToOffset(newLine, newColumn);
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

        public int MoveForwardChars(int howMany)
        {
            int result = 0;
            bool forward = howMany > 0;
            while (howMany != 0)
            {
                if (forward && CursorOffset >= Length - 1)
                {
                    break;
                }
                else if (!forward && CursorOffset == 0)
                {
                    break;
                }
                if (forward) 
                {
                    CursorOffset ++;
                    howMany --;
                }
                else
                {
                    CursorOffset --;
                    howMany ++;
                }
                result ++;
            }
            return result;
        }

        public bool MoveToStartOfLine()
        {
            int line, column;
            OffsetToLineColumn(CursorOffset, out line, out column);
            bool result = CursorOffset != OffsetStartLine(line);
            CursorOffset = OffsetStartLine(line);
            return result;
        }
        
        public bool MoveToEndOfLine()
        {
            int line, column;
            OffsetToLineColumn(CursorOffset, out line, out column);
            column = LastColumnOnLine(line);
            bool result = CursorOffset != LineColumnToOffset(line, column);
            CursorOffset = LineColumnToOffset(line, column);
            return result;
        }
        #endregion
        
        #region Coordinate Conversion
        public int CursorOffset 
        { 
            get
            {
                return _currentPosition;
            }
            set
            {
                _currentPosition = Math.Min(value, _text.Length);
            }
        }
        
        public void GetSelection(out int start, out int end)
        {
            start = _selectionStart;
            end = _selectionEnd;
        }
        
        public void SetSelection(int start, int end)
        {
            _selectionStart = Math.Min(start, _text.Length);
            _selectionEnd = Math.Min(end, _text.Length);
        }
        
        public int Length 
        { 
            get 
            {
                return _text.Length;
            }            
        }

        public int LineColumnToOffset(int line, int column)
        {
            int lineCounter = 0;
            int i;
            for (i = 0; i < _text.Length && lineCounter < line; i++)
            {
                if (_text[i] == '\n')
                {
                    lineCounter ++;
                }
            }
            if (i == _text.Length)
            {
                return i;
            }
            int columnCounter = 0;
            for (; i < _text.Length && columnCounter < column; i++)
            {
                columnCounter ++;
            }
            return i;
        }
        
        public void OffsetToLineColumn(int offset, out int line, out int column)
        {
            line = 0; column = 0;
            for (int i = 0; i < Math.Min(offset, _text.Length); i++)
            {
                if (_text[i] == '\n')
                {
                    line ++;
                    column = 0;
                }
                else
                {
                    column ++;
                }
            }
        }
        
        #endregion
        
        #region Text Manipulation
        public void InsertText(int position, string text, bool inhibitTextChanged)
        {
            _text = _text.Substring(0, position) + text + _text.Substring(position);
            if (!inhibitTextChanged)
            {
                OnTextChanged(new TextChangedEventArgs(TextChangedEventArgs.OperationType.Insertion,
                                                       position,
                                                       text.Length,
                                                       text));
            }
        }
        
        public void DeleteText(int position, int length, bool inhibitTextChanged)
        {
            string deletedText = GetText(position, length);
            _text = _text.Substring(0, position) + _text.Substring(position + length);
            if (!inhibitTextChanged)
            {
                OnTextChanged(new TextChangedEventArgs(TextChangedEventArgs.OperationType.Deletion,
                                                       position,
                                                       length,
                                                       deletedText));
            }
        }
        
        public string GetText(int position, int length)
        {
            return _text.Substring(position, length);
        }
        #endregion
        
        #region Text Changes
        public event EventHandler<TextChangedEventArgs> TextChanged;
        #endregion
        
        #region Formatting
        public void RequestFormatting(int start, int end, List<FormattingHint> hints)
        {
        }
        
        public List<string> KnownFormattingHints()
        {
            return new List<string>();
        }
        #endregion

        #region Keyboard events
        public event EventHandler<KeyStrokeEventArgs> KeyStroke;
        public bool Editable 
        { 
            get { return true; }
            set {} 
        }
        #endregion
        
        #endregion

        #region Private Storage
        string _text;
        int _currentPosition;
        int _selectionStart;
        int _selectionEnd;
        #endregion

        #region Private Helper Methods
        private void OnTextChanged(TextChangedEventArgs e)
        {
            if (TextChanged != null)
            {
                TextChanged(this, e);
            }
        }

        private void OnKeyStroke(KeyStrokeEventArgs e)
        {
            if (KeyStroke != null)
            {
                KeyStroke(this, e);
            }
        }

        private int LastColumnOnLine(int line)
        {
            int offset = OffsetStartLine(line + 1) - 1;
            if (offset > Length - 1)
            {
                offset = Length - 1;
            }
            int dummyLine, column;
            OffsetToLineColumn(offset, out dummyLine, out column);
            return column;
        }

        private int OffsetStartLine(int line)
        {
            return LineColumnToOffset(line, 0);
        }
        #endregion
    }
}
