using System;
using Gtk;
using Pango;
using Synpl.EditorAbstraction;
using System.Collections.Generic;

namespace Synpl.Shell
{

	public partial class MainWindow: Gtk.Window
	{	

		#region Private Storage
		private TextView _textView;
		private GtkTextViewEditor _editorWrapper;
		#endregion
		
		#region Constructor
		public MainWindow(): base (Gtk.WindowType.Toplevel)
		{
			Build();
			ConfigureTextView();
			_editorWrapper = new GtkTextViewEditor(_textView);
			_editorWrapper.TextChanged += HandleTextChanged;
			_textView.KeyReleaseEvent += HandleKeyReleaseEvent;
			ShowAll();
		}
		#endregion

		#region Editor Event Handlers
		private void HandleTextChanged(object sender, TextChangedEventArgs e)
		{
			Console.WriteLine(">>> {0}", e.Operation);
			Console.WriteLine("{0}, {1}: \"{2}\"", e.Start, e.Length, e.Text);			                 
		}
		#endregion
		
		#region Private Helper Methods
		private void ConfigureTextView()
		{
			_textView = new TextView();
			FontDescription fontDescription = new FontDescription();
			fontDescription.Family = "Bitstream Vera Sans Mono";
			fontDescription.AbsoluteSize = 15000;
			_textView.ModifyFont(fontDescription);
			Add(_textView);
		}
		#endregion
		
		#region Form Event Handlers
		protected void OnDeleteEvent (object sender, DeleteEventArgs a)
		{
			Application.Quit ();
			a.RetVal = true;
		}

		void HandleKeyReleaseEvent(object o, KeyReleaseEventArgs args)
		{
			List<FormattingHint> hints = new List<FormattingHint>();
			hints.Add(new FormattingHint(0, 3, "keyword"));
			hints.Add(new FormattingHint(4, 7, "stringLiteral"));
			hints.Add(new FormattingHint(8, 12, "comment"));
			_editorWrapper.RequestFormatting(0, _editorWrapper.Length, hints);
		}
		#endregion
	}
}