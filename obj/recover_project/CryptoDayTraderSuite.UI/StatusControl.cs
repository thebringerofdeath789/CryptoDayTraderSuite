using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;

namespace CryptoDayTraderSuite.UI
{
	public class StatusControl : UserControl
	{
		private IContainer components = null;

		private TableLayoutPanel tlMain;

		private Label lblTitle;

		private Label lblPnL;

		private Label lblWinRate;

		private Label lblDrawdown;

		private Label lblProj100;

		private Label lblProj1000;

		private Button btnRefresh;

		private IHistoryService _historyService;

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
			this.tlMain = new System.Windows.Forms.TableLayoutPanel();
			this.lblTitle = new System.Windows.Forms.Label();
			this.lblPnL = new System.Windows.Forms.Label();
			this.lblWinRate = new System.Windows.Forms.Label();
			this.lblDrawdown = new System.Windows.Forms.Label();
			this.lblProj100 = new System.Windows.Forms.Label();
			this.lblProj1000 = new System.Windows.Forms.Label();
			this.btnRefresh = new System.Windows.Forms.Button();
			this.tlMain.SuspendLayout();
			base.SuspendLayout();
			this.tlMain.ColumnCount = 1;
			this.tlMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.tlMain.Controls.Add(this.lblTitle, 0, 0);
			this.tlMain.Controls.Add(this.btnRefresh, 0, 1);
			this.tlMain.Controls.Add(this.lblPnL, 0, 2);
			this.tlMain.Controls.Add(this.lblWinRate, 0, 3);
			this.tlMain.Controls.Add(this.lblDrawdown, 0, 4);
			this.tlMain.Controls.Add(this.lblProj100, 0, 5);
			this.tlMain.Controls.Add(this.lblProj1000, 0, 6);
			this.tlMain.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tlMain.Name = "tlMain";
			this.tlMain.Padding = new System.Windows.Forms.Padding(24);
			this.tlMain.RowCount = 8;
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.lblTitle.AutoSize = true;
			this.lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
			this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 16f, System.Drawing.FontStyle.Bold);
			this.lblTitle.Padding = new System.Windows.Forms.Padding(0, 0, 0, 12);
			this.lblTitle.Text = "Status / Performance";
			this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.lblPnL.AutoSize = true;
			this.lblPnL.Font = new System.Drawing.Font("Segoe UI", 12f);
			this.lblPnL.Text = "PnL: --";
			this.lblWinRate.AutoSize = true;
			this.lblWinRate.Font = new System.Drawing.Font("Segoe UI", 12f);
			this.lblWinRate.Text = "Win Rate: --";
			this.lblDrawdown.AutoSize = true;
			this.lblDrawdown.Font = new System.Drawing.Font("Segoe UI", 12f);
			this.lblDrawdown.Text = "Max Drawdown: --";
			this.lblProj100.AutoSize = true;
			this.lblProj100.Font = new System.Drawing.Font("Segoe UI", 12f);
			this.lblProj100.Text = "$100 projection: --";
			this.lblProj1000.AutoSize = true;
			this.lblProj1000.Font = new System.Drawing.Font("Segoe UI", 12f);
			this.lblProj1000.Text = "$1000 projection: --";
			this.btnRefresh.Dock = System.Windows.Forms.DockStyle.Top;
			this.btnRefresh.Height = 36;
			this.btnRefresh.Text = "Refresh";
			this.btnRefresh.Click += new System.EventHandler(BtnRefresh_Click);
			base.Controls.Add(this.tlMain);
			base.Name = "StatusControl";
			base.Size = new System.Drawing.Size(600, 400);
			this.tlMain.ResumeLayout(false);
			this.tlMain.PerformLayout();
			base.ResumeLayout(false);
		}

		public StatusControl()
		{
			InitializeComponent();
		}

		public void Initialize(IHistoryService historyService)
		{
			_historyService = historyService;
			LoadData();
		}

		private void BtnRefresh_Click(object sender, EventArgs e)
		{
			LoadData();
		}

		private void LoadData()
		{
			if (_historyService == null)
			{
				return;
			}
			List<TradeRecord> trades = _historyService.LoadTrades() ?? new List<TradeRecord>();
			decimal pnl = default(decimal);
			decimal wins = default(decimal);
			decimal losses = default(decimal);
			decimal maxDrawdown = default(decimal);
			decimal equity = default(decimal);
			decimal peak = default(decimal);
			decimal trough = default(decimal);
			List<decimal> eqCurve = new List<decimal>();
			foreach (TradeRecord t in trades.OrderBy((TradeRecord x) => x.AtUtc))
			{
				if (t.PnL.HasValue)
				{
					pnl += t.PnL.Value;
				}
				equity += t.PnL.GetValueOrDefault();
				eqCurve.Add(equity);
				if (t.PnL.HasValue)
				{
					if (t.PnL.Value > 0m)
					{
						++wins;
					}
					else if (t.PnL.Value < 0m)
					{
						++losses;
					}
				}
			}
			peak = default(decimal);
			trough = default(decimal);
			maxDrawdown = default(decimal);
			foreach (decimal eq in eqCurve)
			{
				if (eq > peak)
				{
					peak = eq;
					trough = eq;
				}
				if (eq < trough)
				{
					trough = eq;
				}
				decimal dd = peak - trough;
				if (dd > maxDrawdown)
				{
					maxDrawdown = dd;
				}
			}
			decimal total = wins + losses;
			decimal winRate = ((total > 0m) ? (wins / total * 100m) : 0m);
			lblPnL.Text = $"PnL: {pnl:C2}";
			lblWinRate.Text = $"Win Rate: {winRate:0.0}%";
			lblDrawdown.Text = $"Max Drawdown: {maxDrawdown:C2}";
			decimal avgWinR = 1.1m;
			decimal avgLossR = 1.0m;
			decimal riskFrac = 0.02m;
			decimal tradesPerDay = 10m;
			decimal netFee = 0.001m;
			if (trades.Any())
			{
				List<decimal> winPnls = (from tradeRecord in trades
					where tradeRecord.PnL.HasValue && tradeRecord.PnL.Value > 0m
					select tradeRecord.PnL.Value).ToList();
				List<decimal> lossPnls = (from tradeRecord in trades
					where tradeRecord.PnL.HasValue && tradeRecord.PnL.Value < 0m
					select Math.Abs(tradeRecord.PnL.Value)).ToList();
				avgWinR = (winPnls.Any() ? winPnls.Average() : avgWinR);
				avgLossR = (lossPnls.Any() ? lossPnls.Average() : avgLossR);
			}
			ProjectionInput projInput = new ProjectionInput
			{
				StartingEquity = 100m,
				TradesPerDay = (int)tradesPerDay,
				WinRate = ((total > 0m) ? (winRate / 100m) : 0.52m),
				AvgWinR = avgWinR,
				AvgLossR = avgLossR,
				RiskPerTradeFraction = riskFrac,
				NetFeeAndFrictionRate = netFee,
				Days = 20
			};
			ProjectionResult proj100 = Projections.Compute(projInput);
			lblProj100.Text = $"$100 projection: {proj100.EndingEquity:0.00} (daily {proj100.DailyExpectedReturnPct:0.00}%)";
			projInput.StartingEquity = 1000m;
			ProjectionResult proj1000 = Projections.Compute(projInput);
			lblProj1000.Text = $"$1000 projection: {proj1000.EndingEquity:0.00}";
		}
	}
}
