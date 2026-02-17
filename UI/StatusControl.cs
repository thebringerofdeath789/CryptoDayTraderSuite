using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;

namespace CryptoDayTraderSuite.UI
{
    public partial class StatusControl : UserControl
    {
        private IHistoryService _historyService;

        public StatusControl()
        {
            InitializeComponent();
        }

        public void Initialize(IHistoryService historyService)
        {
            _historyService = historyService;
            LoadData();
        }

        private void BtnRefresh_Click(object sender, EventArgs e) => LoadData();

        private void LoadData()
        {
            if (_historyService == null) return;
            // Load trade history and compute stats
            var trades = _historyService.LoadTrades() ?? new List<TradeRecord>();
            decimal pnl = 0m, wins = 0, losses = 0, maxDrawdown = 0m, equity = 0m, peak = 0m, trough = 0m;
            var eqCurve = new List<decimal>();
            foreach (var t in trades.OrderBy(x => x.AtUtc))
            {
                if (t.PnL.HasValue)
                    pnl += t.PnL.Value;
                equity += t.PnL ?? 0m;
                eqCurve.Add(equity);
                if (t.PnL.HasValue)
                {
                    if (t.PnL.Value > 0) wins++;
                    else if (t.PnL.Value < 0) losses++;
                }
            }
            // Compute max drawdown
            peak = 0m; trough = 0m; maxDrawdown = 0m;
            foreach (var eq in eqCurve)
            {
                if (eq > peak) { peak = eq; trough = eq; }
                if (eq < trough) trough = eq;
                var dd = peak - trough;
                if (dd > maxDrawdown) maxDrawdown = dd;
            }
            var total = wins + losses;
            var winRate = total > 0 ? (wins / total) * 100m : 0m;

            lblPnL.Text = $"PnL: {pnl:C2}";
            lblWinRate.Text = $"Win Rate: {winRate:0.0}%";
            lblDrawdown.Text = $"Max Drawdown: {maxDrawdown:C2}";

            // Projections (using last known win rate and avg PnL)
            decimal avgWinR = 1.1m, avgLossR = 1.0m, riskFrac = 0.02m, tradesPerDay = 10, netFee = 0.001m;
            if (trades.Any())
            {
                var winPnls = trades.Where(t => t.PnL.HasValue && t.PnL.Value > 0).Select(t => t.PnL.Value).ToList();
                var lossPnls = trades.Where(t => t.PnL.HasValue && t.PnL.Value < 0).Select(t => Math.Abs(t.PnL.Value)).ToList();
                avgWinR = winPnls.Any() ? winPnls.Average() : avgWinR;
                avgLossR = lossPnls.Any() ? lossPnls.Average() : avgLossR;
            }
            var projInput = new ProjectionInput
            {
                StartingEquity = 100m,
                TradesPerDay = (int)tradesPerDay,
                WinRate = total > 0 ? (decimal)winRate / 100m : 0.52m,
                AvgWinR = avgWinR,
                AvgLossR = avgLossR,
                RiskPerTradeFraction = riskFrac,
                NetFeeAndFrictionRate = netFee,
                Days = 20
            };
            var proj100 = Projections.Compute(projInput);
            lblProj100.Text = $"$100 projection: {proj100.EndingEquity:0.00} (daily {proj100.DailyExpectedReturnPct:0.00}%)";
            projInput.StartingEquity = 1000m;
            var proj1000 = Projections.Compute(projInput);
            lblProj1000.Text = $"$1000 projection: {proj1000.EndingEquity:0.00}";
        }
    }
}
