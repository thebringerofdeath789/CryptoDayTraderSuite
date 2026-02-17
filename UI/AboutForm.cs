/* File: UI/AboutForm.cs */
/* Author: Gregory King */
/* Date: 2025-08-10 */
/* Description: About dialog with program information */

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;

namespace CryptoDayTraderSuite.UI
{
    public class AboutForm : Form
    {
        public AboutForm()
        {
            this.Text = "About Crypto Day-Trading Suite";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Width = 500;
            this.Height = 400;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var tl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(20)
            };
            this.Controls.Add(tl);

            var title = new Label
            {
                Text = "Crypto Day-Trading Suite",
                Font = new Font(this.Font.FontFamily, 16, FontStyle.Bold),
                AutoSize = true
            };
            tl.Controls.Add(title);

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var details = new Label
            {
                Text = $@"Version: {version}
Author: Gregory King
Created: August 10, 2025
Framework: .NET Framework 4.8.1
Description: Professional cryptocurrency trading suite supporting multiple exchanges, 
strategies, and automated trading capabilities.

Features:
• Multi-exchange support (Coinbase, Kraken, Bitstamp)
• Real-time market data and charting
• Backtesting capabilities
• Paper trading mode
• Live trading with risk management
• Strategy automation
• Advanced technical analysis",
                AutoSize = true
            };
            tl.Controls.Add(details);

            var copyright = new Label
            {
                Text = "Copyright © 2025 Gregory King. All rights reserved.",
                AutoSize = true
            };
            tl.Controls.Add(copyright);

            var btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Width = 100
            };
            var btnPanel = new FlowLayoutPanel
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