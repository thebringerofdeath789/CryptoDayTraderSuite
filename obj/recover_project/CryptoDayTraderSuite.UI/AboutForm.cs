using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace CryptoDayTraderSuite.UI
{
	public class AboutForm : Form
	{
		public AboutForm()
		{
			Text = "About Crypto Day-Trading Suite";
			base.StartPosition = FormStartPosition.CenterParent;
			base.Width = 500;
			base.Height = 400;
			base.FormBorderStyle = FormBorderStyle.FixedDialog;
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			TableLayoutPanel tl = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 1,
				RowCount = 4,
				Padding = new Padding(20)
			};
			base.Controls.Add(tl);
			Label title = new Label
			{
				Text = "Crypto Day-Trading Suite",
				Font = new Font(Font.FontFamily, 16f, FontStyle.Bold),
				AutoSize = true
			};
			tl.Controls.Add(title);
			Version version = Assembly.GetExecutingAssembly().GetName().Version;
			Label details = new Label
			{
				Text = $"Version: {version}\r\nAuthor: Gregory King\r\nCreated: August 10, 2025\r\nFramework: .NET Framework 4.8.1\r\nDescription: Professional cryptocurrency trading suite supporting multiple exchanges, \r\nstrategies, and automated trading capabilities.\r\n\r\nFeatures:\r\n• Multi-exchange support (Coinbase, Kraken, Bitstamp)\r\n• Real-time market data and charting\r\n• Backtesting capabilities\r\n• Paper trading mode\r\n• Live trading with risk management\r\n• Strategy automation\r\n• Advanced technical analysis",
				AutoSize = true
			};
			tl.Controls.Add(details);
			Label copyright = new Label
			{
				Text = "Copyright © 2025 Gregory King. All rights reserved.",
				AutoSize = true
			};
			tl.Controls.Add(copyright);
			Button btnOk = new Button
			{
				Text = "OK",
				DialogResult = DialogResult.OK,
				Width = 100
			};
			FlowLayoutPanel btnPanel = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.RightToLeft,
				AutoSize = true,
				Dock = DockStyle.Bottom
			};
			btnPanel.Controls.Add(btnOk);
			tl.Controls.Add(btnPanel);
		}
	}
}
