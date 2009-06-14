
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
	}
}
