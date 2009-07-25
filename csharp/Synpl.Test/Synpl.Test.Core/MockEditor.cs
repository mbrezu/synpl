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
            _text = String.Empty;
            _currentPosition = 0;
            _selectionStart = 0;
            _selectionEnd = 0;
        }
        #endregion

        #region Public Methods (Simulation of User Interaction)
        public void SimulateStartSelecting()
        {
            _selectionStart = _currentPosition;
            _selectionEnd = _currentPosition;
        }

        public void SimulateMoveUp(bool keepSelection)
        {
            this.MoveForwardLines(-1);
            HandleSelection(keepSelection);
        }

        public void SimulateMoveDown(bool keepSelection)
        {
            this.MoveForwardLines(1);
            HandleSelection(keepSelection);
        }

        public void SimulateMoveLeft(bool keepSelection)
        {
            this.MoveForwardChars(-1);
            HandleSelection(keepSelection);
        }

        public void SimulateMoveRight(bool keepSelection)
        {
            this.MoveForwardChars(1);
            HandleSelection(keepSelection);
        }

        public void SimulateInsertText(string text)
        {
            InsertText(_currentPosition, text, false);
            _currentPosition += text.Length;
            HandleSelection(false);
        }

        public void SimulateDelKeyStroke()
        {
            if (_selectionEnd != _selectionStart)
            {
                int startDelete = Math.Min(_selectionEnd, _selectionStart);
                int numberOfChars = Math.Abs(_selectionEnd - _selectionStart);
                DeleteText(startDelete, numberOfChars, false);
                _currentPosition = startDelete;
                HandleSelection(false);
            }            
            else
            {
                if (_currentPosition < _text.Length)
                {
                    DeleteText(_currentPosition, 1, false);
                    HandleSelection(false);
                }
            }
        }

        public void SimulateBackspaceKeyStroke()
        {
            if (_currentPosition > 0)
            {
                _currentPosition --;
                DeleteText(_currentPosition, 1, false);
                HandleSelection(false);
            }
        }
        #endregion

        #region IAbstractEditor Implementation
        
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
            _currentPosition = _selectionEnd;
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
            for (; i < _text.Length && columnCounter < column && _text[i] != '\n'; i++)
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

        private void HandleSelection(bool keepSelection)
        {
            if (keepSelection)
            {
                _selectionEnd = _currentPosition;
            }
            else
            {
                SimulateStartSelecting();
            }
        }

        #endregion
    }
}
