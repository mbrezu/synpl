
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
		public string Hint {
			get {
				return _hint;
			}
		}
		public int End {
			get {
				return _end;
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
	}
}
