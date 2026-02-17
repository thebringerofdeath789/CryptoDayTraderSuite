/* File: Util/CueBanner.cs */
/* Author: Gregory King */
/* Date: 2025-08-10 */
/* Description: winforms cue banner text for textbox on .net 4.8 */
/* Functions: SetCue */

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CryptoDayTraderSuite.Util
{
	public static class CueBanner
	{
		/* em_setcuebanner message id */
		private const int EM_SETCUEBANNER = 0x1501; /* set cue */

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam); /* p-invoke */

		public static void SetCue(TextBox tb, string text)
		{
			/* guard */
			if (tb == null || tb.IsDisposed) return; /* no textbox */
			/* 1 = show even when focused on newer os versions, ignored if not supported */
			SendMessage(tb.Handle, EM_SETCUEBANNER, new IntPtr(1), text ?? ""); /* set */
		}
	}
}
