using System;
using System.Drawing;
using System.Windows.Forms;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Themes;

namespace CryptoDayTraderSuite.UI
{
    public partial class GovernorWidget : UserControl
    {
        private AIGovernor _governor;

        public GovernorWidget()
        {
            InitializeComponent();
            Theme.Apply(this);
            // Re-apply specific font sizes if Theme overwrote them too aggressively
            lblBias.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
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
            if (InvokeRequired) { Invoke(new Action(() => OnBiasUpdated(bias, reason))); return; }
            
            lblBias.Text = bias.ToString().ToUpper();
            lblReason.Text = reason;

            switch (bias)
            {
                case MarketBias.Bullish: lblBias.ForeColor = Color.LightGreen; break;
                case MarketBias.Bearish: lblBias.ForeColor = Color.LightCoral; break;
                default: lblBias.ForeColor = Color.Silver; break;
            }
        }

        private void OnStatusChanged(string status)
        {
            if (InvokeRequired) { Invoke(new Action(() => OnStatusChanged(status))); return; }
            lblStatus.Text = status;
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
    }
}