using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;

using System.Windows.Forms.DataVisualization.Charting;
using CryptoDayTraderSuite.Themes;

/* AutoPlanner alias removed */

namespace CryptoDayTraderSuite.UI
{
    public partial class DashboardControl : UserControl
    {
        private GovernorWidget _govWidget;
        private List<TradeRecord> _recentTrades;
        private List<string> _notifications;
        private IAccountService _accountService;
        private IHistoryService _historyService;

        // Event to bubble navigation requests up to the main shell
        public event Action<string> NavigationRequest;

        private void RaiseNavigationRequest(string destination)
        {
            NavigationRequest?.Invoke(destination);
        }

        public DashboardControl()
        {
            InitializeComponent();
            Theme.Apply(this);
            // Re-apply styles after Theme.Apply might have been overwritten or specific overrides needed
            lblWelcome.ForeColor = Theme.Text;
            btnRefresh.ForeColor = Theme.Text;
            btnRefresh.BackColor = Theme.ContentBg;
            btnRefresh.FlatAppearance.BorderColor = Theme.PanelBg;
            btnAccountInsights.ForeColor = Theme.Text;
            btnAccountInsights.BackColor = Theme.ContentBg;
            btnAccountInsights.FlatAppearance.BorderColor = Theme.PanelBg;
            SetFreshness("Not loaded", false, true);
        }

        public void Initialize(IAccountService accountService, IHistoryService historyService, AIGovernor governor = null)
        {
            _accountService = accountService;
            _historyService = historyService;
            
            // Re-instantiate widget if needed, or better, add it to the container in Designer if possible.
            // Since UserControls can be tricky in Designer if they are in the same project and not built,
            // we will add it dynamically here, but the CONTAINER is now essentially designed.
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
            SetFreshness("Refreshing...", false, true);
            LoadData();
        }

        private void btnAccountInsights_Click(object sender, EventArgs e)
        {
            RaiseNavigationRequest("Insights");
        }

        /* 
           BuildUi() Removed - Logic migrated to InitializeComponent in .Designer.cs 
           to comply with UI Development Standards.
        */

        private void LoadData()
        {
            if (_accountService == null)
            {
                SetFreshness("Unavailable: account service missing", true, false);
                return;
            }

            // Accounts summary
            var accounts = _accountService != null ? _accountService.GetAll() : new List<AccountInfo>();
            var enabled = accounts.Count(a => a.Enabled);
            var total = accounts.Count;
            lblAccounts.Text = $"Accounts: {enabled} enabled / {total} total";

            // Auto mode status
            lblAutoMode.Text = enabled > 0 ? "Auto Mode: Ready" : "Auto Mode: No enabled accounts";

            // Performance summary (last 30 days)
            var trades = _historyService != null ? _historyService.LoadTrades() : new List<TradeRecord>();
            if (trades != null)
            {
                _recentTrades = trades.Where(t => t.AtUtc > DateTime.UtcNow.AddDays(-30)).OrderByDescending(t => t.AtUtc).ToList();
                decimal pnl = 0m, wins = 0, losses = 0, maxDrawdown = 0m, equity = 0m, peak = 0m, trough = 0m;
                var eqCurvePoints = new List<Tuple<DateTime, decimal>>();
                var returns = new List<decimal>();
                decimal? best = null, worst = null;
                
                var sortedTrades = _recentTrades.OrderBy(x => x.AtUtc).ToList();

                foreach (var t in sortedTrades)
                {
                    if (t.PnL.HasValue)
                    {
                        pnl += t.PnL.Value;
                        returns.Add(t.PnL.Value);
                        if (!best.HasValue || t.PnL.Value > best.Value) best = t.PnL.Value;
                        if (!worst.HasValue || t.PnL.Value < worst.Value) worst = t.PnL.Value;
                        
                        if (t.PnL.Value > 0) wins++;
                        else if (t.PnL.Value < 0) losses++;
                    }
                    equity += t.PnL ?? 0m;
                    eqCurvePoints.Add(new Tuple<DateTime, decimal>(t.AtUtc, equity));
                }

                peak = 0m; trough = 0m; maxDrawdown = 0m;
                foreach (var pt in eqCurvePoints)
                {
                    var val = pt.Item2;
                    if (val > peak) { peak = val; trough = val; }
                    if (val < trough) trough = val;
                    var dd = peak - trough;
                    if (dd > maxDrawdown) maxDrawdown = dd;
                }
                var totalTrades = wins + losses;
                var winRate = totalTrades > 0 ? (wins / totalTrades) * 100m : 0m;

                double sharpe = 0;
                if (returns.Count > 1)
                {
                    var mean = (double)returns.Average();
                    var std = Math.Sqrt(returns.Select(x => Math.Pow((double)x - mean, 2)).Sum() / (returns.Count - 1));
                    if (std > 0) sharpe = mean / std * Math.Sqrt(252);
                }

                lblPnL.Text = $"PnL (30d): {pnl:C2}";
                lblWinRate.Text = $"Win Rate: {winRate:0.0}%";
                lblDrawdown.Text = $"Drawdown: {maxDrawdown:C2}";
                lblSharpe.Text = $"Sharpe: {(sharpe == 0 ? "--" : sharpe.ToString("0.00"))}";
                lblTradeCount.Text = $"Trades: {totalTrades}";
                lblBest.Text = $"Best: {(best?.ToString("0.00") ?? "--")} / Worst: {(worst?.ToString("0.00") ?? "--")}";

                // Recent trades grid (last 10)
                gridRecentTrades.DataSource = _recentTrades.Take(10).Select(t => new
                {
                    AtUtc = t.AtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    t.ProductId,
                    t.Strategy,
                    t.Side,
                    t.Quantity,
                    t.Price,
                    PnL = t.PnL.HasValue ? t.PnL.Value.ToString("0.00") : ""
                }).ToList();

                // Equity curve chart
                if (chartEquity != null)
                {
                    var series = chartEquity.Series["Equity"];
                    series.Points.Clear();
                    
                    foreach (var pt in eqCurvePoints)
                    {
                        series.Points.AddXY(pt.Item1.ToLocalTime(), pt.Item2);
                    }
                    
                    chartEquity.ChartAreas["Equity"].RecalculateAxesScale();
                }

                // Notifications
                _notifications = new List<string>();
                if (total == 0) _notifications.Add("No accounts configured.");
                if (enabled == 0) _notifications.Add("Enable accounts to trade.");
                if (!_recentTrades.Any()) _notifications.Add("No recent trades.");
                lstNotifications.DataSource = _notifications;

                SetFreshness($"Updated {DateTime.Now:HH:mm:ss} · trades(30d): {_recentTrades.Count} · notes: {_notifications.Count}", false, false);
            }
            else
            {
                SetFreshness($"Updated {DateTime.Now:HH:mm:ss} · no trade history", false, true);
            }
        }

        private void SetFreshness(string message)
        {
            SetFreshness(message, false, false);
        }

        private void SetFreshness(string message, bool warn, bool neutral)
        {
            if (lblDataFreshness == null) return;
            lblDataFreshness.Text = "Data: " + message;
            if (neutral)
            {
                lblDataFreshness.ForeColor = Color.DimGray;
            }
            else
            {
                lblDataFreshness.ForeColor = warn ? Color.DarkOrange : Color.DarkGreen;
            }
        }


    }
}
