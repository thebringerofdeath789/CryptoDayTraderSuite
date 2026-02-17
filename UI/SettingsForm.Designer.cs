namespace CryptoDayTraderSuite.UI
{
    partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

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
            this.SuspendLayout();

            /* tabsMain */
            this.tabsMain.Controls.Add(this.tabGeneral);
            this.tabsMain.Controls.Add(this.tabTrading);
            this.tabsMain.Controls.Add(this.tabData);
            this.tabsMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabsMain.Name = "tabsMain";

            /* tabGeneral */
            this.tabGeneral.Controls.Add(this.lblGeneral);
            this.tabGeneral.Name = "tabGeneral";
            this.tabGeneral.Text = "General";
            this.tabGeneral.Padding = new System.Windows.Forms.Padding(3);

            /* lblGeneral */
            this.lblGeneral.AutoSize = true;
            this.lblGeneral.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblGeneral.Padding = new System.Windows.Forms.Padding(8);
            this.lblGeneral.Text = "General application preferences.";

            /* tabTrading */
            this.tabTrading.Controls.Add(this.lblTrading);
            this.tabTrading.Name = "tabTrading";
            this.tabTrading.Text = "Trading";
            this.tabTrading.Padding = new System.Windows.Forms.Padding(3);

            /* lblTrading */
            this.lblTrading.AutoSize = true;
            this.lblTrading.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTrading.Padding = new System.Windows.Forms.Padding(8);
            this.lblTrading.Text = "Trading defaults and risk settings.";

            /* tabData */
            this.tabData.Controls.Add(this.lblData);
            this.tabData.Name = "tabData";
            this.tabData.Text = "Data";
            this.tabData.Padding = new System.Windows.Forms.Padding(3);

            /* lblData */
            this.lblData.AutoSize = true;
            this.lblData.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblData.Padding = new System.Windows.Forms.Padding(8);
            this.lblData.Text = "Data sources and cache settings.";

            /* SettingsForm */
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.tabsMain);
            this.Name = "SettingsForm";
            this.Text = "Settings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;

            this.tabsMain.ResumeLayout(false);
            this.tabGeneral.ResumeLayout(false);
            this.tabGeneral.PerformLayout();
            this.tabTrading.ResumeLayout(false);
            this.tabTrading.PerformLayout();
            this.tabData.ResumeLayout(false);
            this.tabData.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl tabsMain;
        private System.Windows.Forms.TabPage tabGeneral;
        private System.Windows.Forms.Label lblGeneral;
        private System.Windows.Forms.TabPage tabTrading;
        private System.Windows.Forms.Label lblTrading;
        private System.Windows.Forms.TabPage tabData;
        private System.Windows.Forms.Label lblData;
    }
}