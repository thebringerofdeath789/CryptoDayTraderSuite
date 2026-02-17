using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace CryptoDayTraderSuite.UI
{
	public class SettingsForm : Form
	{
		private IContainer components = null;

		private TabControl tabsMain;

		private TabPage tabGeneral;

		private Label lblGeneral;

		private TabPage tabTrading;

		private Label lblTrading;

		private TabPage tabData;

		private Label lblData;

		public SettingsForm()
		{
			InitializeComponent();
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
			this.tabsMain = new System.Windows.Forms.TabControl();
			this.tabGeneral = new System.Windows.Forms.TabPage();
			this.lblGeneral = new System.Windows.Forms.Label();
			this.tabTrading = new System.Windows.Forms.TabPage();
			this.lblTrading = new System.Windows.Forms.Label();
			this.tabData = new System.Windows.Forms.TabPage();
			this.lblData = new System.Windows.Forms.Label();
			this.tabsMain.SuspendLayout();
			this.tabGeneral.SuspendLayout();
			this.tabTrading.SuspendLayout();
			this.tabData.SuspendLayout();
			base.SuspendLayout();
			this.tabsMain.Controls.Add(this.tabGeneral);
			this.tabsMain.Controls.Add(this.tabTrading);
			this.tabsMain.Controls.Add(this.tabData);
			this.tabsMain.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabsMain.Name = "tabsMain";
			this.tabGeneral.Controls.Add(this.lblGeneral);
			this.tabGeneral.Name = "tabGeneral";
			this.tabGeneral.Text = "General";
			this.tabGeneral.Padding = new System.Windows.Forms.Padding(3);
			this.lblGeneral.AutoSize = true;
			this.lblGeneral.Dock = System.Windows.Forms.DockStyle.Top;
			this.lblGeneral.Padding = new System.Windows.Forms.Padding(8);
			this.lblGeneral.Text = "General application preferences.";
			this.tabTrading.Controls.Add(this.lblTrading);
			this.tabTrading.Name = "tabTrading";
			this.tabTrading.Text = "Trading";
			this.tabTrading.Padding = new System.Windows.Forms.Padding(3);
			this.lblTrading.AutoSize = true;
			this.lblTrading.Dock = System.Windows.Forms.DockStyle.Top;
			this.lblTrading.Padding = new System.Windows.Forms.Padding(8);
			this.lblTrading.Text = "Trading defaults and risk settings.";
			this.tabData.Controls.Add(this.lblData);
			this.tabData.Name = "tabData";
			this.tabData.Text = "Data";
			this.tabData.Padding = new System.Windows.Forms.Padding(3);
			this.lblData.AutoSize = true;
			this.lblData.Dock = System.Windows.Forms.DockStyle.Top;
			this.lblData.Padding = new System.Windows.Forms.Padding(8);
			this.lblData.Text = "Data sources and cache settings.";
			base.ClientSize = new System.Drawing.Size(800, 600);
			base.Controls.Add(this.tabsMain);
			base.Name = "SettingsForm";
			this.Text = "Settings";
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.tabsMain.ResumeLayout(false);
			this.tabGeneral.ResumeLayout(false);
			this.tabGeneral.PerformLayout();
			this.tabTrading.ResumeLayout(false);
			this.tabTrading.PerformLayout();
			this.tabData.ResumeLayout(false);
			this.tabData.PerformLayout();
			base.ResumeLayout(false);
		}
	}
}
