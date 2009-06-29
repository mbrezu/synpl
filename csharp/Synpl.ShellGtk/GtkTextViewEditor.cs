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
		#endregion
		
		#region Constructor
		public GtkTextViewEditor(TextView textView)
		{
			_textView = textView;
			ConfigureEvents();
			ConfigureTags();
		}
		#endregion

		#region Implementation of IAbstractEditor
        
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
            if (inhibitTextChanged) {
                _inhibitTextChanged = true;
            }
			TextIter start = _textView.Buffer.GetIterAtOffset(position);
			TextIter end = _textView.Buffer.GetIterAtOffset(position + length);
			_textView.Buffer.Delete(ref start, ref end);
            if (inhibitTextChanged)
            {
                _inhibitTextChanged = false;
            }
		}
		
		public void InsertText(int position, string text, bool inhibitTextChanged)
		{
            if (inhibitTextChanged)
            {
                _inhibitTextChanged = true;
            }
			TextIter start = _textView.Buffer.GetIterAtOffset(position);
			_textView.Buffer.Insert(ref start, text);
            if (inhibitTextChanged)
            {
                _inhibitTextChanged = false;
            }
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

		public event EventHandler<TextChangedEventArgs> TextChanged;
		#endregion

		#region Private Helper Methods
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
			_textView.KeyPressEvent += HandleKeyPressEvent;
			_textView.KeyReleaseEvent += HandleKeyReleaseEvent;			
		}
		
		private void SaveState()
		{
			GetSelection(out _lastSelectionStart, out _lastSelectionEnd);
			_lastSelectionText = GetText(_lastSelectionStart, _lastSelectionEnd - _lastSelectionStart);
			TextIter atCursor = _textView.Buffer.GetIterAtMark(_textView.Buffer.InsertMark);
			_charAfterCursor = atCursor.Char;
			atCursor.BackwardChar();
			_charBeforeCursor = atCursor.Char;			
		}
		#endregion
				
		#region Form Event Handlers
		void HandleKeyPressEvent(object o, KeyPressEventArgs args)
		{
			SaveState();
		}

		void HandleKeyReleaseEvent(object o, KeyReleaseEventArgs args)
		{
			if (_waitForDeletionKey) {
				_waitForDeletionKey = false;
				Gdk.EventKey ev = (Gdk.EventKey)args.Args[0];
				if (ev.Key == Gdk.Key.BackSpace && !_inhibitTextChanged)
				{
                    TextChanged(this, 
                                new TextChangedEventArgs(TextChangedEventArgs.OperationType.Deletion,
                                                         _lastSelectionStart - 1,
                                                         1,
                                                         _charBeforeCursor));
				}
				else if (ev.Key == Gdk.Key.Delete && !_inhibitTextChanged)
				{
					TextChanged(this, new TextChangedEventArgs(TextChangedEventArgs.OperationType.Deletion,
					                                           _lastSelectionStart,
					                                           1,
					                                           _charAfterCursor));		
				}					
			}
			SaveState();
			GetSelection(out _lastSelectionStart, out _lastSelectionEnd);
			_lastSelectionText = GetText(_lastSelectionStart, _lastSelectionEnd - _lastSelectionStart);
			
		}

		private void HandleDeleteRange(object o, DeleteRangeArgs args)
		{
            if (_inhibitTextChanged)
            {
                return;
            }
			if (_lastSelectionEnd != _lastSelectionStart) {
                if (TextChanged != null) {      
					TextChanged(this, 
                                new TextChangedEventArgs(TextChangedEventArgs.OperationType.Deletion,
                                                         _lastSelectionStart,
                                                         _lastSelectionEnd - _lastSelectionStart,
                                                         _lastSelectionText));
                }           
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
			if (TextChanged != null) {
				TextChanged(this, 
                            new TextChangedEventArgs(TextChangedEventArgs.OperationType.Insertion,
                                                     args.Pos.Offset,
                                                     args.Length,
                                                     args.Text));
			}
		}
		#endregion
	}	
}
