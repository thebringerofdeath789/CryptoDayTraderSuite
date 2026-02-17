namespace CryptoDayTraderSuite.UI
{
    partial class DashboardControl
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.main = new System.Windows.Forms.TableLayoutPanel();
            this.topPanel = new System.Windows.Forms.Panel();
            this.lblWelcome = new System.Windows.Forms.Label();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnAccountInsights = new System.Windows.Forms.Button();
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
            ((System.ComponentModel.ISupportInitialize)(this.gridRecentTrades)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartEquity)).BeginInit();
            this.notifPanel.SuspendLayout();
            this.SuspendLayout();
            this.AutoScroll = true;
            // 
            // main
            // 
            this.main.ColumnCount = 2;
            this.main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
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
            this.main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.main.Size = new System.Drawing.Size(1000, 800);
            this.main.TabIndex = 0;
            // 
            // topPanel
            // 
            this.topPanel.Controls.Add(this.btnAccountInsights);
            this.topPanel.Controls.Add(this.btnRefresh);
            this.topPanel.Controls.Add(this.lblDataFreshness);
            this.topPanel.Controls.Add(this.lblWelcome);
            this.topPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topPanel.Location = new System.Drawing.Point(27, 27);
            this.topPanel.Name = "topPanel";
            this.topPanel.Size = new System.Drawing.Size(517, 174);
            this.topPanel.TabIndex = 0;
            // 
            // lblWelcome
            // 
            this.lblWelcome.AutoSize = true;
            this.lblWelcome.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            this.lblWelcome.Location = new System.Drawing.Point(0, 10);
            this.lblWelcome.Name = "lblWelcome";
            this.lblWelcome.Size = new System.Drawing.Size(419, 32);
            this.lblWelcome.TabIndex = 0;
            this.lblWelcome.Text = "Welcome to Crypto Day-Trader Suite";
            // 
            // btnRefresh
            // 
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.Location = new System.Drawing.Point(0, 60);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(120, 30);
            this.btnRefresh.TabIndex = 1;
            this.btnRefresh.Text = "Refresh Data";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // btnAccountInsights
            // 
            this.btnAccountInsights.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAccountInsights.Location = new System.Drawing.Point(128, 60);
            this.btnAccountInsights.Name = "btnAccountInsights";
            this.btnAccountInsights.Size = new System.Drawing.Size(150, 30);
            this.btnAccountInsights.TabIndex = 3;
            this.btnAccountInsights.Text = "Account Insights";
            this.btnAccountInsights.UseVisualStyleBackColor = true;
            this.btnAccountInsights.Click += new System.EventHandler(this.btnAccountInsights_Click);
            // 
            // lblDataFreshness
            // 
            this.lblDataFreshness.AutoSize = true;
            this.lblDataFreshness.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblDataFreshness.Location = new System.Drawing.Point(0, 98);
            this.lblDataFreshness.Name = "lblDataFreshness";
            this.lblDataFreshness.Size = new System.Drawing.Size(112, 19);
            this.lblDataFreshness.TabIndex = 2;
            this.lblDataFreshness.Text = "Data: Not loaded";
            // 
            // widgetContainer
            // 
            this.widgetContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.widgetContainer.Location = new System.Drawing.Point(550, 27);
            this.widgetContainer.Name = "widgetContainer";
            this.widgetContainer.Size = new System.Drawing.Size(423, 174);
            this.widgetContainer.TabIndex = 1;
            // 
            // summaryPanel
            // 
            this.summaryPanel.ColumnCount = 2;
            this.summaryPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.summaryPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
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
            // 
            // lblAccounts
            // 
            this.lblAccounts.AutoSize = true;
            this.lblAccounts.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblAccounts.Location = new System.Drawing.Point(3, 3);
            this.lblAccounts.Name = "lblAccounts";
            this.lblAccounts.Size = new System.Drawing.Size(100, 21);
            this.lblAccounts.TabIndex = 0;
            this.lblAccounts.Text = "Accounts: --";
            // 
            // lblAutoMode
            // 
            this.lblAutoMode.AutoSize = true;
            this.lblAutoMode.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblAutoMode.Location = new System.Drawing.Point(261, 3);
            this.lblAutoMode.Name = "lblAutoMode";
            this.lblAutoMode.Size = new System.Drawing.Size(117, 21);
            this.lblAutoMode.TabIndex = 1;
            this.lblAutoMode.Text = "Auto Mode: --";
            // 
            // lblPnL
            // 
            this.lblPnL.AutoSize = true;
            this.lblPnL.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblPnL.Location = new System.Drawing.Point(3, 33);
            this.lblPnL.Name = "lblPnL";
            this.lblPnL.Size = new System.Drawing.Size(58, 21);
            this.lblPnL.TabIndex = 2;
            this.lblPnL.Text = "PnL: --";
            // 
            // lblWinRate
            // 
            this.lblWinRate.AutoSize = true;
            this.lblWinRate.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblWinRate.Location = new System.Drawing.Point(261, 33);
            this.lblWinRate.Name = "lblWinRate";
            this.lblWinRate.Size = new System.Drawing.Size(95, 21);
            this.lblWinRate.TabIndex = 3;
            this.lblWinRate.Text = "Win Rate: --";
            // 
            // lblDrawdown
            // 
            this.lblDrawdown.AutoSize = true;
            this.lblDrawdown.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblDrawdown.Location = new System.Drawing.Point(3, 63);
            this.lblDrawdown.Name = "lblDrawdown";
            this.lblDrawdown.Size = new System.Drawing.Size(110, 21);
            this.lblDrawdown.TabIndex = 4;
            this.lblDrawdown.Text = "Drawdown: --";
            // 
            // lblSharpe
            // 
            this.lblSharpe.AutoSize = true;
            this.lblSharpe.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblSharpe.Location = new System.Drawing.Point(261, 63);
            this.lblSharpe.Name = "lblSharpe";
            this.lblSharpe.Size = new System.Drawing.Size(83, 21);
            this.lblSharpe.TabIndex = 5;
            this.lblSharpe.Text = "Sharpe: --";
            // 
            // lblTradeCount
            // 
            this.lblTradeCount.AutoSize = true;
            this.lblTradeCount.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblTradeCount.Location = new System.Drawing.Point(3, 93);
            this.lblTradeCount.Name = "lblTradeCount";
            this.lblTradeCount.Size = new System.Drawing.Size(82, 21);
            this.lblTradeCount.TabIndex = 6;
            this.lblTradeCount.Text = "Trades: --";
            // 
            // lblBest
            // 
            this.lblBest.AutoSize = true;
            this.lblBest.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblBest.Location = new System.Drawing.Point(261, 93);
            this.lblBest.Name = "lblBest";
            this.lblBest.Size = new System.Drawing.Size(115, 21);
            this.lblBest.TabIndex = 7;
            this.lblBest.Text = "Best/Worst: --";
            // 
            // tradesContainer
            // 
            this.tradesContainer.Controls.Add(this.gridRecentTrades);
            this.tradesContainer.Controls.Add(this.lblRecentTradesHeader);
            this.tradesContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tradesContainer.Location = new System.Drawing.Point(550, 207);
            this.tradesContainer.Name = "tradesContainer";
            this.main.SetRowSpan(this.tradesContainer, 2);
            this.tradesContainer.Size = new System.Drawing.Size(423, 300);
            this.tradesContainer.TabIndex = 3;
            // 
            // lblRecentTradesHeader
            // 
            this.lblRecentTradesHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRecentTradesHeader.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblRecentTradesHeader.Location = new System.Drawing.Point(0, 0);
            this.lblRecentTradesHeader.Name = "lblRecentTradesHeader";
            this.lblRecentTradesHeader.Size = new System.Drawing.Size(423, 40);
            this.lblRecentTradesHeader.TabIndex = 0;
            this.lblRecentTradesHeader.Text = "Recent Trades";
            this.lblRecentTradesHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // gridRecentTrades
            // 
            this.gridRecentTrades.AllowUserToAddRows = false;
            this.gridRecentTrades.AllowUserToDeleteRows = false;
            this.gridRecentTrades.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridRecentTrades.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridRecentTrades.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colTime,
            this.colProduct,
            this.colStrategy,
            this.colSide,
            this.colQty,
            this.colPrice,
            this.colPnL});
            this.gridRecentTrades.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridRecentTrades.Location = new System.Drawing.Point(0, 40);
            this.gridRecentTrades.MultiSelect = false;
            this.gridRecentTrades.Name = "gridRecentTrades";
            this.gridRecentTrades.ReadOnly = true;
            this.gridRecentTrades.RowHeadersVisible = false;
            this.gridRecentTrades.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridRecentTrades.Size = new System.Drawing.Size(423, 260);
            this.gridRecentTrades.TabIndex = 1;
            // 
            // colTime
            // 
            this.colTime.DataPropertyName = "AtUtc";
            this.colTime.HeaderText = "Time";
            this.colTime.Name = "colTime";
            // 
            // colProduct
            // 
            this.colProduct.DataPropertyName = "ProductId";
            this.colProduct.HeaderText = "Product";
            this.colProduct.Name = "colProduct";
            // 
            // colStrategy
            // 
            this.colStrategy.DataPropertyName = "Strategy";
            this.colStrategy.HeaderText = "Strategy";
            this.colStrategy.Name = "colStrategy";
            // 
            // colSide
            // 
            this.colSide.DataPropertyName = "Side";
            this.colSide.HeaderText = "Side";
            this.colSide.Name = "colSide";
            // 
            // colQty
            // 
            this.colQty.DataPropertyName = "Quantity";
            this.colQty.HeaderText = "Qty";
            this.colQty.Name = "colQty";
            // 
            // colPrice
            // 
            this.colPrice.DataPropertyName = "Price";
            this.colPrice.HeaderText = "Price";
            this.colPrice.Name = "colPrice";
            // 
            // colPnL
            // 
            this.colPnL.DataPropertyName = "PnL";
            this.colPnL.HeaderText = "PnL";
            this.colPnL.Name = "colPnL";
            // 
            // chartEquity
            // 
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
            // 
            // notifPanel
            // 
            this.notifPanel.Controls.Add(this.lstNotifications);
            this.notifPanel.Controls.Add(this.lblNotifHeader);
            this.notifPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.notifPanel.Location = new System.Drawing.Point(550, 513);
            this.notifPanel.Name = "notifPanel";
            this.notifPanel.Size = new System.Drawing.Size(423, 260);
            this.notifPanel.TabIndex = 5;
            // 
            // lblNotifHeader
            // 
            this.lblNotifHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblNotifHeader.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblNotifHeader.Location = new System.Drawing.Point(0, 0);
            this.lblNotifHeader.Name = "lblNotifHeader";
            this.lblNotifHeader.Size = new System.Drawing.Size(423, 30);
            this.lblNotifHeader.TabIndex = 0;
            this.lblNotifHeader.Text = "Notifications";
            // 
            // lstNotifications
            // 
            this.lstNotifications.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lstNotifications.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstNotifications.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lstNotifications.FormattingEnabled = true;
            this.lstNotifications.ItemHeight = 17;
            this.lstNotifications.Location = new System.Drawing.Point(0, 30);
            this.lstNotifications.Name = "lstNotifications";
            this.lstNotifications.Size = new System.Drawing.Size(423, 230);
            this.lstNotifications.TabIndex = 1;
            // 
            // DashboardControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.main);
            this.Name = "DashboardControl";
            this.Size = new System.Drawing.Size(1000, 800);
            this.main.ResumeLayout(false);
            this.topPanel.ResumeLayout(false);
            this.topPanel.PerformLayout();
            this.summaryPanel.ResumeLayout(false);
            this.summaryPanel.PerformLayout();
            this.tradesContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridRecentTrades)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartEquity)).EndInit();
            this.notifPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel main;
        private System.Windows.Forms.Panel topPanel;
        private System.Windows.Forms.Label lblWelcome;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnAccountInsights;
        private System.Windows.Forms.Label lblDataFreshness;
        private System.Windows.Forms.Panel widgetContainer;
        private System.Windows.Forms.TableLayoutPanel summaryPanel;
        private System.Windows.Forms.Label lblAccounts;
        private System.Windows.Forms.Label lblAutoMode;
        private System.Windows.Forms.Label lblPnL;
        private System.Windows.Forms.Label lblWinRate;
        private System.Windows.Forms.Label lblDrawdown;
        private System.Windows.Forms.Label lblSharpe;
        private System.Windows.Forms.Label lblTradeCount;
        private System.Windows.Forms.Label lblBest;
        private System.Windows.Forms.Panel tradesContainer;
        private System.Windows.Forms.Label lblRecentTradesHeader;
        private System.Windows.Forms.DataGridView gridRecentTrades;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartEquity;
        private System.Windows.Forms.Panel notifPanel;
        private System.Windows.Forms.Label lblNotifHeader;
        private System.Windows.Forms.ListBox lstNotifications;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colProduct;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStrategy;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSide;
        private System.Windows.Forms.DataGridViewTextBoxColumn colQty;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPrice;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPnL;
    }
}
