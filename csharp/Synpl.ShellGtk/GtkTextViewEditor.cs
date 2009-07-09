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
using Gtk;
using System.Collections.Generic;
using Synpl.EditorAbstraction;
using System.Text;

namespace Synpl.ShellGtk
{
	public class GtkTextViewEditor : IAbstractEditor
	{
		#region Private storage
		private TextView _textView;
		private string _lastSelectionText;
		private int _lastSelectionStart;
		private int _lastSelectionEnd;
		private string _charBeforeCursor;
		private string _charAfterCursor;
		private bool _waitForDeletionKey;
        private bool _inhibitTextChanged;
        private bool _control;
        private bool _shift;
        private bool _alt;
		#endregion
		
		#region Constructor
		public GtkTextViewEditor(TextView textView)
		{
			_textView = textView;
			ConfigureEvents();
			ConfigureTags();
            _waitForDeletionKey = false;

            _control = false;
            _shift = false;
            _alt = false;

		}
		#endregion

		#region Implementation of IAbstractEditor

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
        
        public bool Editable
        {
            get
            {
                return _textView.Editable;
            }
            set
            {
                _textView.Editable = value;
            }
        }
        
		public int Length
		{
			get 
			{
				TextIter start, end;
				_textView.Buffer.GetBounds(out start, out end);
				return end.Offset;
			}
		}
		public int CursorOffset
		{
			get 
			{
				return _textView.Buffer.GetIterAtMark(_textView.Buffer.InsertMark).Offset;
			}
			set 
			{
				_textView.Buffer.PlaceCursor(_textView.Buffer.GetIterAtOffset(value));
			}
		}

		
		public string GetText(int position, int length)
		{
			TextIter start = _textView.Buffer.GetIterAtOffset(position);
			TextIter end = _textView.Buffer.GetIterAtOffset(position + length);
			return _textView.Buffer.GetText(start, end, false);
		}
		
		public void GetSelection(out int start, out int end)
		{
			TextIter selectionStart, selectionEnd;
			_textView.Buffer.GetSelectionBounds(out selectionStart, out selectionEnd);
			start = selectionStart.Offset;
			end = selectionEnd.Offset;
		}

		public void DeleteText(int position, int length, bool inhibitTextChanged)
		{
            // Normal event handling is disabled, we do our own triggering.
            // This is because deleting text programatically doesn't use the selection.
            _inhibitTextChanged = true;
			TextIter start = _textView.Buffer.GetIterAtOffset(position);
			TextIter end = _textView.Buffer.GetIterAtOffset(position + length);
            string text = GetText(position, length);
			_textView.Buffer.Delete(ref start, ref end);
            if (!inhibitTextChanged)
            {
                OnTextChanged(TextChangedEventArgs.OperationType.Deletion,
                              position,
                              length,
                              text);
            }
            _inhibitTextChanged = false;
		}

		public void InsertText(int position, string text, bool inhibitTextChanged)
		{
            _inhibitTextChanged = true;
            // Since TextBuffer.Insert seems to act up sometimes (triggers
            // the wrong start position in the insert event), we do the triggering
            // ourselves.
			TextIter start = _textView.Buffer.GetIterAtOffset(position);
//            Console.WriteLine("InsertText: {0} {1}", position, start.Offset);
			_textView.Buffer.Insert(ref start, text);
            if (!inhibitTextChanged)
            {
                OnTextChanged(TextChangedEventArgs.OperationType.Insertion,
                              position,
                              text.Length,
                              text);
            }
            _inhibitTextChanged = false;
		}
		
		public void SetSelection(int start, int end)
		{
			TextIter startIterator = _textView.Buffer.GetIterAtOffset(start);
			TextIter endIterator = _textView.Buffer.GetIterAtOffset(end);
			_textView.Buffer.SelectRange(startIterator, endIterator);
		}
		
		public List<string> KnownFormattingHints()
		{
			List<string> result = new List<string>();
			foreach (string tagName in _textView.Buffer.TagTable.Data.Keys)
			{
				result.Add(tagName);
			}
			return result;
		}
		
		public void RequestFormatting(int start, int end, List<FormattingHint> hints)
		{
			TextIter startIter = _textView.Buffer.GetIterAtOffset(start);
			TextIter endIter = _textView.Buffer.GetIterAtOffset(end);
			_textView.Buffer.RemoveAllTags(startIter, endIter);
			foreach (FormattingHint hint in hints)
			{
				TextIter startHint = _textView.Buffer.GetIterAtOffset(hint.Start);
				TextIter endHint = _textView.Buffer.GetIterAtOffset(hint.End);
				_textView.Buffer.ApplyTag(hint.Hint, startHint, endHint);
			}
		}

        public int LineColumnToOffset(int line, int column)
        {
            TextIter iter = _textView.Buffer.GetIterAtLineOffset(line, column);
            return iter.Offset;
        }

        public void OffsetToLineColumn(int offset, out int line, out int column)
        {
            TextIter iter = _textView.Buffer.GetIterAtOffset(offset);
            line = iter.Line;
            column = iter.LineOffset;
        }

		public event EventHandler<TextChangedEventArgs> TextChanged;

        public event EventHandler<KeyStrokeEventArgs> KeyStroke;
		#endregion

		#region Private Helper Methods
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
        
		private void ConfigureTags()
		{
			// These values should be read from a configuration file.
            TextTag addedTextTag = new TextTagWorkaround("addedText", _textView.Buffer.TagTable);
            addedTextTag.Background = "#44ff44";
            TextTag deleteTextTag = new TextTagWorkaround("brokenByDelete", _textView.Buffer.TagTable);
            deleteTextTag.Underline = Pango.Underline.Error;
			TextTag keywordTag = new TextTagWorkaround("keyword", _textView.Buffer.TagTable);
			keywordTag.Foreground = "#ff0000";
			TextTag commentTag = new TextTagWorkaround("comment", _textView.Buffer.TagTable);
			commentTag.Foreground = "#888888";
			TextTag identifierTag = new TextTagWorkaround("identifier", _textView.Buffer.TagTable);
			identifierTag.Foreground = "#2222ff";
			TextTag numericLiteralTag = new TextTagWorkaround("numericLiteral", _textView.Buffer.TagTable);
			numericLiteralTag.Foreground = "#aa8822";
			TextTag stringLiteralTag = new TextTagWorkaround("stringLiteral", _textView.Buffer.TagTable);
			stringLiteralTag.Foreground = "#22aa88";
		}
		
		private void ConfigureEvents()
		{
			_textView.Buffer.InsertText += HandleInsertText;
			_textView.Buffer.DeleteRange += HandleDeleteRange;
            _textView.Buffer.MarkSet += HandleMarkSet;
			_textView.KeyPressEvent += HandleKeyPressEvent;
			_textView.KeyReleaseEvent += HandleKeyReleaseEvent;
		}

		private void SaveState()
		{
            if (!_waitForDeletionKey)
            {
                GetSelection(out _lastSelectionStart, out _lastSelectionEnd);
    			_lastSelectionText = GetText(_lastSelectionStart, _lastSelectionEnd - _lastSelectionStart);
    			TextIter atCursor = _textView.Buffer.GetIterAtMark(_textView.Buffer.InsertMark);
    			_charAfterCursor = atCursor.Char;
    			atCursor.BackwardChar();
    			_charBeforeCursor = atCursor.Char;
//                Console.WriteLine("save state");
//                Console.WriteLine("sel: {0}-{1}, chars: '{2}' | '{3}'",
//                                  _lastSelectionStart,
//                                  _lastSelectionEnd,
//                                  _charBeforeCursor,
//                                  _charAfterCursor);
            }
		}

        private void OnTextChanged(TextChangedEventArgs.OperationType operation,
                                   int start,
                                   int length,
                                   string text)
        {
            if (TextChanged != null)
            {
                TextChanged(this,
                            new TextChangedEventArgs(operation, start, length, text));
            }
        }

        private void OnKeyStroke(Synpl.EditorAbstraction.Key keycode)
        {
            if (KeyStroke != null)
            {
                KeyStroke(this,
                          new KeyStrokeEventArgs(keycode));
            }
        }
		#endregion
				
		#region Form Event Handlers
        void HandleMarkSet(object o, MarkSetArgs args)
        {
//            Console.WriteLine("markSet");
            SaveState();
        }

 		void HandleKeyPressEvent(object o, KeyPressEventArgs args)
		{
			SaveState();
            UpdateShiftControAlt(args.Event.Key, true);
		}

		void HandleKeyReleaseEvent(object o, KeyReleaseEventArgs args)
		{
            UpdateShiftControAlt(args.Event.Key, false);
            if (args.Event.Key >= Gdk.Key.F1 && args.Event.Key <= Gdk.Key.F10)
            {
                OnKeyStroke(new Synpl.EditorAbstraction.Key(args.Event.Key.ToString(), 
                                                            _shift, 
                                                            _control, 
                                                            _alt));
            }
            else if (args.Event.Key >= Gdk.Key.a && args.Event.Key <= Gdk.Key.z)
            {
                OnKeyStroke(new Synpl.EditorAbstraction.Key(args.Event.Key.ToString().ToLower(), 
                                                            _shift, 
                                                            _control, 
                                                            _alt));
            }
            else if (args.Event.Key >= Gdk.Key.A && args.Event.Key <= Gdk.Key.Z)
            {
                OnKeyStroke(new Synpl.EditorAbstraction.Key(args.Event.Key.ToString().ToLower(), 
                                                            _shift, 
                                                            _control, 
                                                            _alt));
            }
            else if (args.Event.Key >= Gdk.Key.Key_0 && args.Event.Key <= Gdk.Key.Key_9)
            {
                OnKeyStroke(new Synpl.EditorAbstraction.Key(args.Event.Key.ToString().Substring(4), 
                                                            _shift, 
                                                            _control, 
                                                            _alt));
            }
			if (_waitForDeletionKey) 
            {
				_waitForDeletionKey = false;
				Gdk.EventKey ev = (Gdk.EventKey)args.Args[0];
				if (ev.Key == Gdk.Key.BackSpace && !_inhibitTextChanged)
				{
                    OnTextChanged(TextChangedEventArgs.OperationType.Deletion,  
                                  _lastSelectionStart - 1,
                                  1,
                                  _charBeforeCursor);
				}
				else if (ev.Key == Gdk.Key.Delete && !_inhibitTextChanged)
				{
					OnTextChanged(TextChangedEventArgs.OperationType.Deletion,
                                   _lastSelectionStart,
                                  1,
                                  _charAfterCursor);
				}
			}
			SaveState();
			GetSelection(out _lastSelectionStart, out _lastSelectionEnd);
			_lastSelectionText = GetText(_lastSelectionStart,
                                         _lastSelectionEnd - _lastSelectionStart);
		}

		private void HandleDeleteRange(object o, DeleteRangeArgs args)
		{
            if (_inhibitTextChanged)
            {
                return;
            }
			if (_lastSelectionEnd != _lastSelectionStart)
            {
                OnTextChanged(TextChangedEventArgs.OperationType.Deletion,
                              _lastSelectionStart,
                              _lastSelectionEnd - _lastSelectionStart,
                              _lastSelectionText);
			}
			else
			{
				_waitForDeletionKey = true;
			}
		}

		private void HandleInsertText(object o, InsertTextArgs args)
		{
            if (_inhibitTextChanged)
            {
                return;
            }
            OnTextChanged(TextChangedEventArgs.OperationType.Insertion,
                          args.Pos.Offset - 1,
                          args.Length,
                          args.Text);
		}

        private void UpdateShiftControAlt(Gdk.Key key, bool state)
        {
            if (key == Gdk.Key.Shift_L || key == Gdk.Key.Shift_R)
            {
                _shift = state;
            }
            if (key == Gdk.Key.Control_L || key == Gdk.Key.Control_R)
            {
                _control = state;
            }
            if (key == Gdk.Key.Alt_L || key == Gdk.Key.Alt_R)
            {
                _alt = state;
            }
        }

		#endregion
	}	
}
