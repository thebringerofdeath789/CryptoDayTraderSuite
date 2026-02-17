using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Themes;

namespace CryptoDayTraderSuite.UI
{
	public class DashboardControl : UserControl
	{
		private GovernorWidget _govWidget;

		private List<TradeRecord> _recentTrades;

		private List<string> _notifications;

		private IAccountService _accountService;

		private IHistoryService _historyService;

		private IContainer components = null;

		private TableLayoutPanel main;

		private Panel topPanel;

		private Label lblWelcome;

		private Button btnRefresh;

		private Label lblDataFreshness;

		private Panel widgetContainer;

		private TableLayoutPanel summaryPanel;

		private Label lblAccounts;

		private Label lblAutoMode;

		private Label lblPnL;

		private Label lblWinRate;

		private Label lblDrawdown;

		private Label lblSharpe;

		private Label lblTradeCount;

		private Label lblBest;

		private Panel tradesContainer;

		private Label lblRecentTradesHeader;

		private DataGridView gridRecentTrades;

		private Chart chartEquity;

		private Panel notifPanel;

		private Label lblNotifHeader;

		private ListBox lstNotifications;

		private DataGridViewTextBoxColumn colTime;

		private DataGridViewTextBoxColumn colProduct;

		private DataGridViewTextBoxColumn colStrategy;

		private DataGridViewTextBoxColumn colSide;

		private DataGridViewTextBoxColumn colQty;

		private DataGridViewTextBoxColumn colPrice;

		private DataGridViewTextBoxColumn colPnL;

		public event Action<string> NavigationRequest;

		private void RaiseNavigationRequest(string destination)
		{
			this.NavigationRequest?.Invoke(destination);
		}

		public DashboardControl()
		{
			InitializeComponent();
			Theme.Apply(this);
			lblWelcome.ForeColor = Theme.Text;
			btnRefresh.ForeColor = Theme.Text;
			btnRefresh.BackColor = Theme.ContentBg;
			btnRefresh.FlatAppearance.BorderColor = Theme.PanelBg;
			SetFreshness("Not loaded", warn: false, neutral: true);
		}

		public void Initialize(IAccountService accountService, IHistoryService historyService, AIGovernor governor = null)
		{
			_accountService = accountService;
			_historyService = historyService;
			if (_govWidget == null)
			{
				_govWidget = new GovernorWidget();
				_govWidget.Dock = DockStyle.Left;
				widgetContainer.Controls.Add(_govWidget);
			}
			if (governor != null)
			{
				_govWidget.Configure(governor);
			}
			LoadData();
		}

		private void btnRefresh_Click(object sender, EventArgs e)
		{
			SetFreshness("Refreshing...", warn: false, neutral: true);
			LoadData();
		}

		private void LoadData()
		{
			if (_accountService == null)
			{
				SetFreshness("Unavailable: account service missing", warn: true, neutral: false);
				return;
			}
			List<AccountInfo> accounts = ((_accountService != null) ? _accountService.GetAll() : new List<AccountInfo>());
			int enabled = accounts.Count((AccountInfo a) => a.Enabled);
			int total = accounts.Count;
			lblAccounts.Text = $"Accounts: {enabled} enabled / {total} total";
			lblAutoMode.Text = ((enabled > 0) ? "Auto Mode: Ready" : "Auto Mode: No enabled accounts");
			List<TradeRecord> trades = ((_historyService != null) ? _historyService.LoadTrades() : new List<TradeRecord>());
			if (trades != null)
			{
				_recentTrades = (from tradeRecord in trades
					where tradeRecord.AtUtc > DateTime.UtcNow.AddDays(-30.0)
					orderby tradeRecord.AtUtc descending
					select tradeRecord).ToList();
				decimal pnl = default(decimal);
				decimal wins = default(decimal);
				decimal losses = default(decimal);
				decimal maxDrawdown = default(decimal);
				decimal equity = default(decimal);
				decimal peak = default(decimal);
				decimal trough = default(decimal);
				List<Tuple<DateTime, decimal>> eqCurvePoints = new List<Tuple<DateTime, decimal>>();
				List<decimal> returns = new List<decimal>();
				decimal? best = null;
				decimal? worst = null;
				List<TradeRecord> sortedTrades = _recentTrades.OrderBy((TradeRecord x) => x.AtUtc).ToList();
				foreach (TradeRecord t in sortedTrades)
				{
					if (t.PnL.HasValue)
					{
						pnl += t.PnL.Value;
						returns.Add(t.PnL.Value);
						if (!best.HasValue || t.PnL.Value > best.Value)
						{
							best = t.PnL.Value;
						}
						if (!worst.HasValue || t.PnL.Value < worst.Value)
						{
							worst = t.PnL.Value;
						}
						if (t.PnL.Value > 0m)
						{
							++wins;
						}
						else if (t.PnL.Value < 0m)
						{
							++losses;
						}
					}
					equity += t.PnL.GetValueOrDefault();
					eqCurvePoints.Add(new Tuple<DateTime, decimal>(t.AtUtc, equity));
				}
				peak = default(decimal);
				trough = default(decimal);
				maxDrawdown = default(decimal);
				foreach (Tuple<DateTime, decimal> pt in eqCurvePoints)
				{
					decimal val = pt.Item2;
					if (val > peak)
					{
						peak = val;
						trough = val;
					}
					if (val < trough)
					{
						trough = val;
					}
					decimal dd = peak - trough;
					if (dd > maxDrawdown)
					{
						maxDrawdown = dd;
					}
				}
				decimal totalTrades = wins + losses;
				decimal winRate = ((totalTrades > 0m) ? (wins / totalTrades * 100m) : 0m);
				double sharpe = 0.0;
				if (returns.Count > 1)
				{
					double mean = (double)returns.Average();
					double std = Math.Sqrt(returns.Select((decimal x) => Math.Pow((double)x - mean, 2.0)).Sum() / (double)(returns.Count - 1));
					if (std > 0.0)
					{
						sharpe = mean / std * Math.Sqrt(252.0);
					}
				}
				lblPnL.Text = $"PnL (30d): {pnl:C2}";
				lblWinRate.Text = $"Win Rate: {winRate:0.0}%";
				lblDrawdown.Text = $"Drawdown: {maxDrawdown:C2}";
				lblSharpe.Text = "Sharpe: " + ((sharpe == 0.0) ? "--" : sharpe.ToString("0.00"));
				lblTradeCount.Text = $"Trades: {totalTrades}";
				lblBest.Text = "Best: " + (best?.ToString("0.00") ?? "--") + " / Worst: " + (worst?.ToString("0.00") ?? "--");
				gridRecentTrades.DataSource = (from tradeRecord in _recentTrades.Take(10)
					select new
					{
						AtUtc = tradeRecord.AtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
						ProductId = tradeRecord.ProductId,
						Strategy = tradeRecord.Strategy,
						Side = tradeRecord.Side,
						Quantity = tradeRecord.Quantity,
						Price = tradeRecord.Price,
						PnL = (tradeRecord.PnL.HasValue ? tradeRecord.PnL.Value.ToString("0.00") : "")
					}).ToList();
				if (chartEquity != null)
				{
					Series series = chartEquity.Series["Equity"];
					series.Points.Clear();
					foreach (Tuple<DateTime, decimal> pt2 in eqCurvePoints)
					{
						series.Points.AddXY(pt2.Item1.ToLocalTime(), pt2.Item2);
					}
					chartEquity.ChartAreas["Equity"].RecalculateAxesScale();
				}
				_notifications = new List<string>();
				if (total == 0)
				{
					_notifications.Add("No accounts configured.");
				}
				if (enabled == 0)
				{
					_notifications.Add("Enable accounts to trade.");
				}
				if (!_recentTrades.Any())
				{
					_notifications.Add("No recent trades.");
				}
				lstNotifications.DataSource = _notifications;
				SetFreshness($"Updated {DateTime.Now:HH:mm:ss} · trades(30d): {_recentTrades.Count} · notes: {_notifications.Count}", warn: false, neutral: false);
			}
			else
			{
				SetFreshness($"Updated {DateTime.Now:HH:mm:ss} · no trade history", warn: false, neutral: true);
			}
		}

		private void SetFreshness(string message)
		{
			SetFreshness(message, warn: false, neutral: false);
		}

		private void SetFreshness(string message, bool warn, bool neutral)
		{
			if (lblDataFreshness != null)
			{
				lblDataFreshness.Text = "Data: " + message;
				if (neutral)
				{
					lblDataFreshness.ForeColor = Color.DimGray;
				}
				else
				{
					lblDataFreshness.ForeColor = (warn ? Color.DarkOrange : Color.DarkGreen);
				}
			}
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
			System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
			System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
			this.main = new System.Windows.Forms.TableLayoutPanel();
			this.topPanel = new System.Windows.Forms.Panel();
			this.lblWelcome = new System.Windows.Forms.Label();
			this.btnRefresh = new System.Windows.Forms.Button();
			this.lblDataFreshness = new System.Windows.Forms.Label();
			this.widgetContainer = new System.Windows.Forms.Panel();
			this.summaryPanel = new System.Windows.Forms.TableLayoutPanel();
			this.lblAccounts = new System.Windows.Forms.Label();
			this.lblAutoMode = new System.Windows.Forms.Label();
			this.lblPnL = new System.Windows.Forms.Label();
			this.lblWinRate = new System.Windows.Forms.Label();
			this.lblDrawdown = new System.Windows.Forms.Label();
			this.lblSharpe = new System.Windows.Forms.Label();
			this.lblTradeCount = new System.Windows.Forms.Label();
			this.lblBest = new System.Windows.Forms.Label();
			this.tradesContainer = new System.Windows.Forms.Panel();
			this.lblRecentTradesHeader = new System.Windows.Forms.Label();
			this.gridRecentTrades = new System.Windows.Forms.DataGridView();
			this.colTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colProduct = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colStrategy = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colSide = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colQty = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colPnL = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.chartEquity = new System.Windows.Forms.DataVisualization.Charting.Chart();
			this.notifPanel = new System.Windows.Forms.Panel();
			this.lblNotifHeader = new System.Windows.Forms.Label();
			this.lstNotifications = new System.Windows.Forms.ListBox();
			this.main.SuspendLayout();
			this.topPanel.SuspendLayout();
			this.summaryPanel.SuspendLayout();
			this.tradesContainer.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)this.gridRecentTrades).BeginInit();
			((System.ComponentModel.ISupportInitialize)this.chartEquity).BeginInit();
			this.notifPanel.SuspendLayout();
			base.SuspendLayout();
			this.AutoScroll = true;
			this.main.ColumnCount = 2;
			this.main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55f));
			this.main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45f));
			this.main.Controls.Add(this.topPanel, 0, 0);
			this.main.Controls.Add(this.widgetContainer, 1, 0);
			this.main.Controls.Add(this.summaryPanel, 0, 1);
			this.main.Controls.Add(this.tradesContainer, 1, 1);
			this.main.Controls.Add(this.chartEquity, 0, 3);
			this.main.Controls.Add(this.notifPanel, 1, 3);
			this.main.Dock = System.Windows.Forms.DockStyle.Fill;
			this.main.Location = new System.Drawing.Point(0, 0);
			this.main.Name = "main";
			this.main.Padding = new System.Windows.Forms.Padding(24);
			this.main.RowCount = 4;
			this.main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 180f));
			this.main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 60f));
			this.main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40f));
			this.main.Size = new System.Drawing.Size(1000, 800);
			this.main.TabIndex = 0;
			this.topPanel.Controls.Add(this.btnRefresh);
			this.topPanel.Controls.Add(this.lblDataFreshness);
			this.topPanel.Controls.Add(this.lblWelcome);
			this.topPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.topPanel.Location = new System.Drawing.Point(27, 27);
			this.topPanel.Name = "topPanel";
			this.topPanel.Size = new System.Drawing.Size(517, 174);
			this.topPanel.TabIndex = 0;
			this.lblWelcome.AutoSize = true;
			this.lblWelcome.Font = new System.Drawing.Font("Segoe UI", 18f, System.Drawing.FontStyle.Bold);
			this.lblWelcome.Location = new System.Drawing.Point(0, 10);
			this.lblWelcome.Name = "lblWelcome";
			this.lblWelcome.Size = new System.Drawing.Size(419, 32);
			this.lblWelcome.TabIndex = 0;
			this.lblWelcome.Text = "Welcome to Crypto Day-Trader Suite";
			this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnRefresh.Location = new System.Drawing.Point(0, 60);
			this.btnRefresh.Name = "btnRefresh";
			this.btnRefresh.Size = new System.Drawing.Size(120, 30);
			this.btnRefresh.TabIndex = 1;
			this.btnRefresh.Text = "Refresh Data";
			this.btnRefresh.UseVisualStyleBackColor = true;
			this.btnRefresh.Click += new System.EventHandler(btnRefresh_Click);
			this.lblDataFreshness.AutoSize = true;
			this.lblDataFreshness.Font = new System.Drawing.Font("Segoe UI", 10f);
			this.lblDataFreshness.Location = new System.Drawing.Point(0, 98);
			this.lblDataFreshness.Name = "lblDataFreshness";
			this.lblDataFreshness.Size = new System.Drawing.Size(112, 19);
			this.lblDataFreshness.TabIndex = 2;
			this.lblDataFreshness.Text = "Data: Not loaded";
			this.widgetContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.widgetContainer.Location = new System.Drawing.Point(550, 27);
			this.widgetContainer.Name = "widgetContainer";
			this.widgetContainer.Size = new System.Drawing.Size(423, 174);
			this.widgetContainer.TabIndex = 1;
			this.summaryPanel.ColumnCount = 2;
			this.summaryPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50f));
			this.summaryPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50f));
			this.summaryPanel.Controls.Add(this.lblAccounts, 0, 0);
			this.summaryPanel.Controls.Add(this.lblAutoMode, 1, 0);
			this.summaryPanel.Controls.Add(this.lblPnL, 0, 1);
			this.summaryPanel.Controls.Add(this.lblWinRate, 1, 1);
			this.summaryPanel.Controls.Add(this.lblDrawdown, 0, 2);
			this.summaryPanel.Controls.Add(this.lblSharpe, 1, 2);
			this.summaryPanel.Controls.Add(this.lblTradeCount, 0, 3);
			this.summaryPanel.Controls.Add(this.lblBest, 1, 3);
			this.summaryPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.summaryPanel.Location = new System.Drawing.Point(27, 207);
			this.summaryPanel.Name = "summaryPanel";
			this.summaryPanel.RowCount = 4;
			this.main.SetRowSpan(this.summaryPanel, 2);
			this.summaryPanel.Size = new System.Drawing.Size(517, 300);
			this.summaryPanel.TabIndex = 2;
			this.lblAccounts.AutoSize = true;
			this.lblAccounts.Font = new System.Drawing.Font("Segoe UI", 12f);
			this.lblAccounts.Location = new System.Drawing.Point(3, 3);
			this.lblAccounts.Name = "lblAccounts";
			this.lblAccounts.Size = new System.Drawing.Size(100, 21);
			this.lblAccounts.TabIndex = 0;
			this.lblAccounts.Text = "Accounts: --";
			this.lblAutoMode.AutoSize = true;
			this.lblAutoMode.Font = new System.Drawing.Font("Segoe UI", 12f);
			this.lblAutoMode.Location = new System.Drawing.Point(261, 3);
			this.lblAutoMode.Name = "lblAutoMode";
			this.lblAutoMode.Size = new System.Drawing.Size(117, 21);
			this.lblAutoMode.TabIndex = 1;
			this.lblAutoMode.Text = "Auto Mode: --";
			this.lblPnL.AutoSize = true;
			this.lblPnL.Font = new System.Drawing.Font("Segoe UI", 12f);
			this.lblPnL.Location = new System.Drawing.Point(3, 33);
			this.lblPnL.Name = "lblPnL";
			this.lblPnL.Size = new System.Drawing.Size(58, 21);
			this.lblPnL.TabIndex = 2;
			this.lblPnL.Text = "PnL: --";
			this.lblWinRate.AutoSize = true;
			this.lblWinRate.Font = new System.Drawing.Font("Segoe UI", 12f);
			this.lblWinRate.Location = new System.Drawing.Point(261, 33);
			this.lblWinRate.Name = "lblWinRate";
			this.lblWinRate.Size = new System.Drawing.Size(95, 21);
			this.lblWinRate.TabIndex = 3;
			this.lblWinRate.Text = "Win Rate: --";
			this.lblDrawdown.AutoSize = true;
			this.lblDrawdown.Font = new System.Drawing.Font("Segoe UI", 12f);
			this.lblDrawdown.Location = new System.Drawing.Point(3, 63);
			this.lblDrawdown.Name = "lblDrawdown";
			this.lblDrawdown.Size = new System.Drawing.Size(110, 21);
			this.lblDrawdown.TabIndex = 4;
			this.lblDrawdown.Text = "Drawdown: --";
			this.lblSharpe.AutoSize = true;
			this.lblSharpe.Font = new System.Drawing.Font("Segoe UI", 12f);
			this.lblSharpe.Location = new System.Drawing.Point(261, 63);
			this.lblSharpe.Name = "lblSharpe";
			this.lblSharpe.Size = new System.Drawing.Size(83, 21);
			this.lblSharpe.TabIndex = 5;
			this.lblSharpe.Text = "Sharpe: --";
			this.lblTradeCount.AutoSize = true;
			this.lblTradeCount.Font = new System.Drawing.Font("Segoe UI", 12f);
			this.lblTradeCount.Location = new System.Drawing.Point(3, 93);
			this.lblTradeCount.Name = "lblTradeCount";
			this.lblTradeCount.Size = new System.Drawing.Size(82, 21);
			this.lblTradeCount.TabIndex = 6;
			this.lblTradeCount.Text = "Trades: --";
			this.lblBest.AutoSize = true;
			this.lblBest.Font = new System.Drawing.Font("Segoe UI", 12f);
			this.lblBest.Location = new System.Drawing.Point(261, 93);
			this.lblBest.Name = "lblBest";
			this.lblBest.Size = new System.Drawing.Size(115, 21);
			this.lblBest.TabIndex = 7;
			this.lblBest.Text = "Best/Worst: --";
			this.tradesContainer.Controls.Add(this.gridRecentTrades);
			this.tradesContainer.Controls.Add(this.lblRecentTradesHeader);
			this.tradesContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tradesContainer.Location = new System.Drawing.Point(550, 207);
			this.tradesContainer.Name = "tradesContainer";
			this.main.SetRowSpan(this.tradesContainer, 2);
			this.tradesContainer.Size = new System.Drawing.Size(423, 300);
			this.tradesContainer.TabIndex = 3;
			this.lblRecentTradesHeader.Dock = System.Windows.Forms.DockStyle.Top;
			this.lblRecentTradesHeader.Font = new System.Drawing.Font("Segoe UI", 14f, System.Drawing.FontStyle.Bold);
			this.lblRecentTradesHeader.Location = new System.Drawing.Point(0, 0);
			this.lblRecentTradesHeader.Name = "lblRecentTradesHeader";
			this.lblRecentTradesHeader.Size = new System.Drawing.Size(423, 40);
			this.lblRecentTradesHeader.TabIndex = 0;
			this.lblRecentTradesHeader.Text = "Recent Trades";
			this.lblRecentTradesHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.gridRecentTrades.AllowUserToAddRows = false;
			this.gridRecentTrades.AllowUserToDeleteRows = false;
			this.gridRecentTrades.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.gridRecentTrades.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.gridRecentTrades.Columns.AddRange(this.colTime, this.colProduct, this.colStrategy, this.colSide, this.colQty, this.colPrice, this.colPnL);
			this.gridRecentTrades.Dock = System.Windows.Forms.DockStyle.Fill;
			this.gridRecentTrades.Location = new System.Drawing.Point(0, 40);
			this.gridRecentTrades.MultiSelect = false;
			this.gridRecentTrades.Name = "gridRecentTrades";
			this.gridRecentTrades.ReadOnly = true;
			this.gridRecentTrades.RowHeadersVisible = false;
			this.gridRecentTrades.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.gridRecentTrades.Size = new System.Drawing.Size(423, 260);
			this.gridRecentTrades.TabIndex = 1;
			this.colTime.DataPropertyName = "AtUtc";
			this.colTime.HeaderText = "Time";
			this.colTime.Name = "colTime";
			this.colProduct.DataPropertyName = "ProductId";
			this.colProduct.HeaderText = "Product";
			this.colProduct.Name = "colProduct";
			this.colStrategy.DataPropertyName = "Strategy";
			this.colStrategy.HeaderText = "Strategy";
			this.colStrategy.Name = "colStrategy";
			this.colSide.DataPropertyName = "Side";
			this.colSide.HeaderText = "Side";
			this.colSide.Name = "colSide";
			this.colQty.DataPropertyName = "Quantity";
			this.colQty.HeaderText = "Qty";
			this.colQty.Name = "colQty";
			this.colPrice.DataPropertyName = "Price";
			this.colPrice.HeaderText = "Price";
			this.colPrice.Name = "colPrice";
			this.colPnL.DataPropertyName = "PnL";
			this.colPnL.HeaderText = "PnL";
			this.colPnL.Name = "colPnL";
			chartArea1.Name = "Equity";
			this.chartEquity.ChartAreas.Add(chartArea1);
			this.chartEquity.Dock = System.Windows.Forms.DockStyle.Fill;
			this.chartEquity.Location = new System.Drawing.Point(27, 513);
			this.chartEquity.Name = "chartEquity";
			series1.ChartArea = "Equity";
			series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
			series1.Name = "Equity";
			this.chartEquity.Series.Add(series1);
			this.chartEquity.Size = new System.Drawing.Size(517, 260);
			this.chartEquity.TabIndex = 4;
			this.chartEquity.Text = "Equity Curve";
			this.chartEquity.BackColor = System.Drawing.Color.Transparent;
			this.notifPanel.Controls.Add(this.lstNotifications);
			this.notifPanel.Controls.Add(this.lblNotifHeader);
			this.notifPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.notifPanel.Location = new System.Drawing.Point(550, 513);
			this.notifPanel.Name = "notifPanel";
			this.notifPanel.Size = new System.Drawing.Size(423, 260);
			this.notifPanel.TabIndex = 5;
			this.lblNotifHeader.Dock = System.Windows.Forms.DockStyle.Top;
			this.lblNotifHeader.Font = new System.Drawing.Font("Segoe UI", 12f, System.Drawing.FontStyle.Bold);
			this.lblNotifHeader.Location = new System.Drawing.Point(0, 0);
			this.lblNotifHeader.Name = "lblNotifHeader";
			this.lblNotifHeader.Size = new System.Drawing.Size(423, 30);
			this.lblNotifHeader.TabIndex = 0;
			this.lblNotifHeader.Text = "Notifications";
			this.lstNotifications.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.lstNotifications.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lstNotifications.Font = new System.Drawing.Font("Segoe UI", 10f);
			this.lstNotifications.FormattingEnabled = true;
			this.lstNotifications.ItemHeight = 17;
			this.lstNotifications.Location = new System.Drawing.Point(0, 30);
			this.lstNotifications.Name = "lstNotifications";
			this.lstNotifications.Size = new System.Drawing.Size(423, 230);
			this.lstNotifications.TabIndex = 1;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(this.main);
			base.Name = "DashboardControl";
			base.Size = new System.Drawing.Size(1000, 800);
			this.main.ResumeLayout(false);
			this.topPanel.ResumeLayout(false);
			this.topPanel.PerformLayout();
			this.summaryPanel.ResumeLayout(false);
			this.summaryPanel.PerformLayout();
			this.tradesContainer.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)this.gridRecentTrades).EndInit();
			((System.ComponentModel.ISupportInitialize)this.chartEquity).EndInit();
			this.notifPanel.ResumeLayout(false);
			base.ResumeLayout(false);
		}
	}
}
