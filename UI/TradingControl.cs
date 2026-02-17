using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.UI
{
    public partial class TradingControl : UserControl
    {
#if NETFRAMEWORK
        private Control chartDisplay;
#endif

        public event EventHandler LoadProductsClicked;
        public event EventHandler FeesClicked;
        public event EventHandler BacktestClicked;
        public event EventHandler PaperClicked;
        public event EventHandler LiveClicked;

        public string Exchange => cmbExchange.SelectedItem?.ToString();
        public string Product => cmbProduct.SelectedItem?.ToString();
        public string Strategy => cmbStrategy.SelectedItem?.ToString();
        public decimal Risk => numRisk.Value;
        public decimal Equity => numEquity.Value;

        public TradingControl()
        {
            InitializeComponent();
            if (cmbExchange != null)
            {
                cmbExchange.SelectedIndexChanged += CmbExchange_SelectedIndexChanged;
            }
            InitializeChart();
            SetStatus("Ready", false, true);
        }

        private void InitializeChart()
        {
#if NETFRAMEWORK
            var chart = new System.Windows.Forms.DataVisualization.Charting.Chart 
            { 
                Dock = DockStyle.Fill, 
                BackColor = Color.FromArgb(32, 34, 40) 
            };
            var area = new System.Windows.Forms.DataVisualization.Charting.ChartArea("Main");
            area.BackColor = Color.FromArgb(32, 34, 40);
            area.AxisX.LabelStyle.ForeColor = Color.WhiteSmoke;
            area.AxisY.LabelStyle.ForeColor = Color.WhiteSmoke;
            area.AxisX.LineColor = Color.Gray;
            area.AxisY.LineColor = Color.Gray;
            area.AxisX.MajorGrid.LineColor = Color.FromArgb(64,64,64);
            area.AxisY.MajorGrid.LineColor = Color.FromArgb(64,64,64);
            chart.ChartAreas.Add(area);

            var series = new System.Windows.Forms.DataVisualization.Charting.Series("Candles") 
            { 
                ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Candlestick 
            };
            series["PriceUpColor"] = "Green";
            series["PriceDownColor"] = "Red";
            chart.Series.Add(series);

            chartDisplay = chart;
            if (this.chartHost != null)
            {
                this.chartHost.Controls.Clear();
                this.chartHost.Controls.Add(chartDisplay);
            }
#else
            var chartPanel = new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.FromArgb(32, 34, 40) };
            if (this.chartHost != null)
            {
                this.chartHost.Controls.Clear();
                this.chartHost.Controls.Add(chartPanel);
            }
#endif
        }

        public void Log(string msg) { txtLog.AppendText(DateTime.Now.ToString("HH:mm:ss") + " " + msg + Environment.NewLine); }
        
        public void SetExchanges(IEnumerable<string> exchanges)
        {
            cmbExchange.Items.Clear();
            foreach (var e in exchanges) cmbExchange.Items.Add(e);
            if (cmbExchange.Items.Count > 0) cmbExchange.SelectedIndex = 0;
            var hint = GetExchangeRoutingHint(Exchange);
            if (!string.IsNullOrEmpty(hint))
            {
                SetStatus(hint, false, true);
                return;
            }

            SetStatus($"Exchange list updated ({cmbExchange.Items.Count})");
        }

        public void SetStrategies(IEnumerable<string> strategies)
        {
            cmbStrategy.Items.Clear();
            foreach (var s in strategies) cmbStrategy.Items.Add(s);
            if (cmbStrategy.Items.Count > 0) cmbStrategy.SelectedIndex = 0;
            SetStatus($"Strategy list updated ({cmbStrategy.Items.Count})");
        }

        public void SetProducts(IEnumerable<string> products)
        {
            cmbProduct.Items.Clear();
            foreach (var p in products) cmbProduct.Items.Add(p);
            if (cmbProduct.Items.Count > 0) cmbProduct.SelectedIndex = 0;
            SetStatus($"Product list updated ({cmbProduct.Items.Count})");
        }
        
        public void SetProjections(string p100, string p1000)
        {
            lblProj100.Text = p100;
            lblProj1000.Text = p1000;
            SetStatus("Projection updated");
        }

        public void SetCandles(List<Candle> candles)
        {
#if NETFRAMEWORK
            var chart = chartDisplay as System.Windows.Forms.DataVisualization.Charting.Chart;
            if (chart != null)
            {
                var s = chart.Series["Candles"];
                s.Points.Clear();
                foreach (var c in candles)
                {
                    // High, Low, Open, Close
                    int i = s.Points.AddXY(c.Time.ToLocalTime(), (double)c.High);
                    s.Points[i].YValues[1] = (double)c.Low;
                    s.Points[i].YValues[2] = (double)c.Open;
                    s.Points[i].YValues[3] = (double)c.Close;
                }
                chart.ChartAreas[0].RecalculateAxesScale();
                SetStatus($"Chart updated ({candles.Count} candles)");
            }
#endif
        }

        public void SetStatus(string message)
        {
            SetStatus(message, false, false);
        }

        public void SetStatus(string message, bool warn, bool neutral)
        {
            if (lblTradeStatus == null) return;
            lblTradeStatus.Text = "Status: " + message + " · " + DateTime.Now.ToString("HH:mm:ss");
            if (neutral)
            {
                lblTradeStatus.ForeColor = Color.DimGray;
            }
            else
            {
                lblTradeStatus.ForeColor = warn ? Color.DarkOrange : Color.DarkGreen;
            }
        }

        /* Event Handlers linked in Designer */
        private void BtnLoadProducts_Click(object sender, EventArgs e)
        {
            SetStatus("Loading products", false, true);
            LoadProductsClicked?.Invoke(this, EventArgs.Empty);
        }

        private void BtnFees_Click(object sender, EventArgs e)
        {
            SetStatus("Loading fees", false, true);
            FeesClicked?.Invoke(this, EventArgs.Empty);
        }

        private void BtnBacktest_Click(object sender, EventArgs e)
        {
            SetStatus("Running backtest", false, true);
            BacktestClicked?.Invoke(this, EventArgs.Empty);
        }

        private void BtnPaper_Click(object sender, EventArgs e)
        {
            SetStatus("Submitting paper trade", false, true);
            PaperClicked?.Invoke(this, EventArgs.Empty);
        }

        private void BtnLive_Click(object sender, EventArgs e)
        {
            SetStatus("Submitting live trade", false, true);
            LiveClicked?.Invoke(this, EventArgs.Empty);
        }

        private void CmbExchange_SelectedIndexChanged(object sender, EventArgs e)
        {
            var hint = GetExchangeRoutingHint(Exchange);
            if (!string.IsNullOrEmpty(hint))
            {
                SetStatus(hint, false, true);
            }
        }

        private static string GetExchangeRoutingHint(string exchange)
        {
            var normalized = (exchange ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized == "binance-us") return "Routing: Binance US endpoint (api.binance.us).";
            if (normalized == "binance-global") return "Routing: Binance Global endpoint (api.binance.com).";
            if (normalized == "bybit-global") return "Routing: Bybit Global endpoint (api.bybit.com).";
            if (normalized == "okx-global") return "Routing: OKX Global endpoint (www.okx.com).";
            return null;
        }
    }
}
