
using System;
using System.Collections.Generic;

namespace Synpl.EditorAbstraction
{	
	public interface IAbstractEditor
	{
		#region Navigation
		int CursorOffset { get; set; }
		void GetSelection(out int start, out int end);
		void SetSelection(int start, int end);		
		int Length { get; }	
		#endregion
		
		#region Text Manipulation
		void InsertText(int position, string text);
		void DeleteText(int position, int length);	
		string GetText(int position, int length);
		#endregion
		
		#region Text Changes
		event EventHandler<TextChangedEventArgs> TextChanged;
		#endregion
		
		#region Formatting
		void RequestFormatting(int start, int end, List<FormattingHint> hints);
		List<string> KnownFormattingHints();
		#endregion
	}
}
