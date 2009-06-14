
using System;
using Gtk;
using System.Collections.Generic;

namespace Synpl.EditorAbstraction
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
		
		public void DeleteText(int position, int length)
		{
			TextIter start = _textView.Buffer.GetIterAtOffset(position);
			TextIter end = _textView.Buffer.GetIterAtOffset(position + length);
			_textView.Buffer.Delete(ref start, ref end);
		}
		
		public void InsertText(int position, string text)
		{
			TextIter start = _textView.Buffer.GetIterAtOffset(position);
			_textView.Buffer.Insert(ref start, text);
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
				if (ev.Key == Gdk.Key.BackSpace) 
				{
					Console.WriteLine("deleted by backspace");
					TextChanged(this, new TextChangedEventArgs(TextChangedEventArgs.OperationType.Deletion,
					                                           _lastSelectionStart,
					                                           1,
					                                           _charBeforeCursor));		
				}
				else if (ev.Key == Gdk.Key.Delete)
				{
					Console.WriteLine("delete by delete");
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
			if (TextChanged != null) {		
				if (_lastSelectionEnd != _lastSelectionStart) {
					TextChanged(this, new TextChangedEventArgs(TextChangedEventArgs.OperationType.Deletion,
					                                           _lastSelectionStart,
					                                           _lastSelectionEnd - _lastSelectionStart,
					                                           _lastSelectionText));
				}
				else
				{
					_waitForDeletionKey = true;
				}
			}			
		}

		private void HandleInsertText(object o, InsertTextArgs args)
		{
			if (TextChanged != null) {
				TextChanged(this, new TextChangedEventArgs(TextChangedEventArgs.OperationType.Insertion,
				                                           args.Pos.Offset,
				                                           args.Length,
				                                           args.Text));
			}						
		}
		#endregion
	}	
}
