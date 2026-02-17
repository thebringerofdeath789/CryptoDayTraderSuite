using System;
using System.Windows.Forms;
using CryptoDayTraderSuite.Strategy;
using CryptoDayTraderSuite.Themes;

namespace CryptoDayTraderSuite.UI
{
    public partial class StrategyConfigDialog : Form
    {
        private readonly StrategyEngine _engine;

        public StrategyConfigDialog(StrategyEngine engine)
        {
            InitializeComponent();
            DialogTheme.Apply(this);
            // PropertyGrid specific styling
            propertyGrid.ViewBackColor = Theme.ContentBg;
            propertyGrid.ViewForeColor = Theme.Text;
            propertyGrid.LineColor = Theme.PanelBg;
            propertyGrid.CategoryForeColor = Theme.Accent;
            propertyGrid.HelpBackColor = Theme.PanelBg;
            propertyGrid.HelpForeColor = Theme.TextMuted;

            _engine = engine;

            cmbStrategy.Items.Add("ORB Strategy");
            cmbStrategy.Items.Add("VWAP Trend");
            cmbStrategy.Items.Add("RSI Reversion");
            cmbStrategy.Items.Add("Donchian 20");
            cmbStrategy.SelectedIndex = 0;
        }

        private void cmbStrategy_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_engine == null) return;

            var sel = cmbStrategy.SelectedItem.ToString();
            if (sel == "ORB Strategy")
            {
                propertyGrid.SelectedObject = _engine.Orb;
            }
            else if (sel == "VWAP Trend")
            {
                propertyGrid.SelectedObject = _engine.VwapTrend;
            }
            else if (sel == "RSI Reversion")
            {
                propertyGrid.SelectedObject = _engine.RsiReversion;
            }
            else if (sel == "Donchian 20")
            {
                propertyGrid.SelectedObject = _engine.Donchian;
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}