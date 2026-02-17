namespace CryptoDayTraderSuite.UI
{
    partial class StatusControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

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
            this.SuspendLayout();

            /* tlMain */
            this.tlMain.ColumnCount = 1;
            this.tlMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
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
            this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize)); // Title
            this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize)); // Btn
            this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize)); // PnL
            this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize)); // WinRate
            this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize)); // Drawdown
            this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize)); // P100
            this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize)); // P1000
            this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

            /* lblTitle */
            this.lblTitle.AutoSize = true;
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Padding = new System.Windows.Forms.Padding(0, 0, 0, 12);
            this.lblTitle.Text = "Status / Performance";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            /* Config Labels */
            this.lblPnL.AutoSize = true;
            this.lblPnL.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblPnL.Text = "PnL: --";

            this.lblWinRate.AutoSize = true;
            this.lblWinRate.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblWinRate.Text = "Win Rate: --";

            this.lblDrawdown.AutoSize = true;
            this.lblDrawdown.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblDrawdown.Text = "Max Drawdown: --";

            this.lblProj100.AutoSize = true;
            this.lblProj100.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblProj100.Text = "$100 projection: --";

            this.lblProj1000.AutoSize = true;
            this.lblProj1000.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblProj1000.Text = "$1000 projection: --";

            /* btnRefresh */
            this.btnRefresh.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnRefresh.Height = 36;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.Click += new System.EventHandler(this.BtnRefresh_Click);

            /* this */
            this.Controls.Add(this.tlMain);
            this.Name = "StatusControl";
            this.Size = new System.Drawing.Size(600, 400);

            this.tlMain.ResumeLayout(false);
            this.tlMain.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tlMain;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblPnL;
        private System.Windows.Forms.Label lblWinRate;
        private System.Windows.Forms.Label lblDrawdown;
        private System.Windows.Forms.Label lblProj100;
        private System.Windows.Forms.Label lblProj1000;
        private System.Windows.Forms.Button btnRefresh;
    }
}