using System;
using System.Drawing;
using System.Windows.Forms;
using CryptoDayTraderSuite.Strategy;
using CryptoDayTraderSuite.Themes;

namespace CryptoDayTraderSuite.UI
{
    public partial class StrategyConfigControl : UserControl
    {
        private StrategyEngine _engine;
        private ComboBox cmbStrategy;
        private PropertyGrid propertyGrid;
        private Label lblHeader;

        public StrategyConfigControl()
        {
            InitializeComponent();
        }

        public void Initialize(StrategyEngine engine)
        {
            _engine = engine;
            Theme.Apply(this);
            ApplyTheme();

            cmbStrategy.Items.Clear();
            cmbStrategy.Items.Add("ORB Strategy");
            cmbStrategy.Items.Add("VWAP Trend");
            cmbStrategy.Items.Add("RSI Reversion");
            cmbStrategy.Items.Add("Donchian 20");
            cmbStrategy.SelectedIndex = 0;
        }

        private void ApplyTheme()
        {
            this.BackColor = Theme.ContentBg;
            lblHeader.ForeColor = Theme.Text;
            
            cmbStrategy.BackColor = Theme.PanelBg;
            cmbStrategy.ForeColor = Theme.Text;
            cmbStrategy.FlatStyle = FlatStyle.Flat;

            propertyGrid.ViewBackColor = Theme.ContentBg;
            propertyGrid.ViewForeColor = Theme.Text;
            propertyGrid.LineColor = Theme.PanelBg;
            propertyGrid.CategoryForeColor = Theme.Accent;
            propertyGrid.HelpBackColor = Theme.PanelBg;
            propertyGrid.HelpForeColor = Theme.TextMuted;
            propertyGrid.BackColor = Theme.ContentBg;
        }

        private void cmbStrategy_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_engine == null || cmbStrategy.SelectedItem == null) return;

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
    }
}