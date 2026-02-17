using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CryptoDayTraderSuite.Util
{
	public static class CueBanner
	{
		private const int EM_SETCUEBANNER = 5377;

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

		public static void SetCue(TextBox tb, string text)
		{
			if (tb != null && !tb.IsDisposed)
			{
				SendMessage(tb.Handle, 5377, new IntPtr(1), text ?? "");
			}
		}
	}
}
