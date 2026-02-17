using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Themes;

namespace CryptoDayTraderSuite.UI
{
	public class GovernorWidget : UserControl
	{
		private AIGovernor _governor;

		private IContainer components = null;

		private Label lblHeader;

		private Label lblBias;

		private Label lblReason;

		private Label lblStatus;

		public GovernorWidget()
		{
			InitializeComponent();
			Theme.Apply(this);
			lblBias.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
		}

		public void Configure(AIGovernor governor)
		{
			_governor = governor;
			if (_governor != null)
			{
				_governor.BiasUpdated += OnBiasUpdated;
				_governor.StatusChanged += OnStatusChanged;
			}
		}

		private void OnBiasUpdated(MarketBias bias, string reason)
		{
			if (base.InvokeRequired)
			{
				Invoke((Action)delegate
				{
					OnBiasUpdated(bias, reason);
				});
				return;
			}
			lblBias.Text = bias.ToString().ToUpper();
			lblReason.Text = reason;
			switch (bias)
			{
			case MarketBias.Bullish:
				lblBias.ForeColor = Color.LightGreen;
				break;
			case MarketBias.Bearish:
				lblBias.ForeColor = Color.LightCoral;
				break;
			default:
				lblBias.ForeColor = Color.Silver;
				break;
			}
		}

		private void OnStatusChanged(string status)
		{
			if (base.InvokeRequired)
			{
				Invoke((Action)delegate
				{
					OnStatusChanged(status);
				});
			}
			else
			{
				lblStatus.Text = status;
			}
		}

		protected override void OnHandleDestroyed(EventArgs e)
		{
			if (_governor != null)
			{
				_governor.BiasUpdated -= OnBiasUpdated;
				_governor.StatusChanged -= OnStatusChanged;
			}
			base.OnHandleDestroyed(e);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			this.lblHeader = new System.Windows.Forms.Label();
			this.lblBias = new System.Windows.Forms.Label();
			this.lblReason = new System.Windows.Forms.Label();
			this.lblStatus = new System.Windows.Forms.Label();
			base.SuspendLayout();
			this.lblHeader.AutoSize = true;
			this.lblHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			this.lblHeader.ForeColor = System.Drawing.Color.Gray;
			this.lblHeader.Location = new System.Drawing.Point(4, 4);
			this.lblHeader.Name = "lblHeader";
			this.lblHeader.Size = new System.Drawing.Size(84, 13);
			this.lblHeader.TabIndex = 0;
			this.lblHeader.Text = "AI MARKET BIAS";
			this.lblBias.AutoSize = true;
			this.lblBias.Font = new System.Drawing.Font("Segoe UI", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
			this.lblBias.ForeColor = System.Drawing.Color.White;
			this.lblBias.Location = new System.Drawing.Point(3, 20);
			this.lblBias.Name = "lblBias";
			this.lblBias.Size = new System.Drawing.Size(81, 21);
			this.lblBias.TabIndex = 1;
			this.lblBias.Text = "PENDING";
			this.lblReason.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			this.lblReason.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			this.lblReason.ForeColor = System.Drawing.Color.Silver;
			this.lblReason.Location = new System.Drawing.Point(4, 50);
			this.lblReason.Name = "lblReason";
			this.lblReason.Size = new System.Drawing.Size(192, 90);
			this.lblReason.TabIndex = 2;
			this.lblReason.Text = "Waiting for analysis...";
			this.lblStatus.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
			this.lblStatus.AutoSize = true;
			this.lblStatus.ForeColor = System.Drawing.Color.DimGray;
			this.lblStatus.Location = new System.Drawing.Point(4, 145);
			this.lblStatus.Name = "lblStatus";
			this.lblStatus.Size = new System.Drawing.Size(73, 13);
			this.lblStatus.TabIndex = 3;
			this.lblStatus.Text = "Disconnected";
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(30, 32, 38);
			base.Controls.Add(this.lblStatus);
			base.Controls.Add(this.lblReason);
			base.Controls.Add(this.lblBias);
			base.Controls.Add(this.lblHeader);
			base.Name = "GovernorWidget";
			base.Size = new System.Drawing.Size(200, 160);
			base.ResumeLayout(false);
			base.PerformLayout();
		}
	}
}
