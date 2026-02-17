/* file: UI/MainForm_LogHook.cs */
using System;
using System.Windows.Forms;

namespace CryptoDayTraderSuite
{
	public partial class MainForm : Form
	{
		/* optional: cache a log textbox if you want, but we keep the original Log() in MainForm.cs */
		private TextBox _txtLog; /* found at runtime */

		public void OnShown_Log(object sender, EventArgs e)
		{
			try
			{
				_txtLog = FindField<TextBox>("txtLog"); /* uses MainForm_Helpers.cs */
			}
			catch
			{
				/* ignore if not present */
			}
		}

		/* if you ever need thread-safe logging from this partial, call LogSafe(...) internally.
           it defers to the existing private Log(string) defined in MainForm.cs */
		private void LogSafe(string message)
		{
			if (this.InvokeRequired)
			{
				this.BeginInvoke(new Action<string>(LogSafe), message); /* hop to UI thread */
				return;
			}

			/* use the original Log(string) from MainForm.cs so there's only one method named Log */
			Log(message);
		}
	}
}
