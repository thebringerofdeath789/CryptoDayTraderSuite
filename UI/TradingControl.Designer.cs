namespace CryptoDayTraderSuite.UI
{
    partial class TradingControl
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
            this.pnlTop = new System.Windows.Forms.FlowLayoutPanel();
            this.btnBacktest = new System.Windows.Forms.Button();
            this.btnPaper = new System.Windows.Forms.Button();
            this.btnLive = new System.Windows.Forms.Button();
            this.lblProj100 = new System.Windows.Forms.Label();
            this.lblProj1000 = new System.Windows.Forms.Label();
            this.lblTradeStatus = new System.Windows.Forms.Label();
            this.mainSplit = new System.Windows.Forms.SplitContainer();
            this.configPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.lblExchange = new System.Windows.Forms.Label();
            this.cmbExchange = new System.Windows.Forms.ComboBox();
            this.lblProduct = new System.Windows.Forms.Label();
            this.cmbProduct = new System.Windows.Forms.ComboBox();
            this.btnLoadProducts = new System.Windows.Forms.Button();
            this.btnFees = new System.Windows.Forms.Button();
            this.lblStrategy = new System.Windows.Forms.Label();
            this.cmbStrategy = new System.Windows.Forms.ComboBox();
            this.lblRisk = new System.Windows.Forms.Label();
            this.numRisk = new System.Windows.Forms.NumericUpDown();
            this.lblEquity = new System.Windows.Forms.Label();
            this.numEquity = new System.Windows.Forms.NumericUpDown();
            this.rightLayout = new System.Windows.Forms.TableLayoutPanel();
            this.chartHost = new System.Windows.Forms.Panel();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.tlMain.SuspendLayout();
            this.pnlTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mainSplit)).BeginInit();
            this.mainSplit.Panel1.SuspendLayout();
            this.mainSplit.Panel2.SuspendLayout();
            this.mainSplit.SuspendLayout();
            this.configPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numRisk)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numEquity)).BeginInit();
            this.rightLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // tlMain
            // 
            this.tlMain.ColumnCount = 1;
            this.tlMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlMain.Controls.Add(this.pnlTop, 0, 0);
            this.tlMain.Controls.Add(this.mainSplit, 0, 1);
            this.tlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlMain.Location = new System.Drawing.Point(0, 0);
            this.tlMain.Name = "tlMain";
            this.tlMain.RowCount = 2;
            this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlMain.Size = new System.Drawing.Size(900, 600);
            this.tlMain.TabIndex = 0;
            // 
            // pnlTop
            // 
            this.pnlTop.AutoSize = true;
            this.pnlTop.Controls.Add(this.btnBacktest);
            this.pnlTop.Controls.Add(this.btnPaper);
            this.pnlTop.Controls.Add(this.btnLive);
            this.pnlTop.Controls.Add(this.lblProj100);
            this.pnlTop.Controls.Add(this.lblProj1000);
            this.pnlTop.Controls.Add(this.lblTradeStatus);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTop.Location = new System.Drawing.Point(3, 3);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Padding = new System.Windows.Forms.Padding(8);
            this.pnlTop.Size = new System.Drawing.Size(894, 42);
            this.pnlTop.TabIndex = 0;
            this.pnlTop.WrapContents = true;
            // 
            // btnBacktest
            // 
            this.btnBacktest.Location = new System.Drawing.Point(11, 11);
            this.btnBacktest.Name = "btnBacktest";
            this.btnBacktest.Size = new System.Drawing.Size(96, 23);
            this.btnBacktest.TabIndex = 0;
            this.btnBacktest.Text = "Backtest";
            this.btnBacktest.UseVisualStyleBackColor = true;
            this.btnBacktest.Click += new System.EventHandler(this.BtnBacktest_Click);
            // 
            // btnPaper
            // 
            this.btnPaper.Location = new System.Drawing.Point(113, 11);
            this.btnPaper.Name = "btnPaper";
            this.btnPaper.Size = new System.Drawing.Size(96, 23);
            this.btnPaper.TabIndex = 1;
            this.btnPaper.Text = "Paper Trade";
            this.btnPaper.UseVisualStyleBackColor = true;
            this.btnPaper.Click += new System.EventHandler(this.BtnPaper_Click);
            // 
            // btnLive
            // 
            this.btnLive.Location = new System.Drawing.Point(215, 11);
            this.btnLive.Name = "btnLive";
            this.btnLive.Size = new System.Drawing.Size(96, 23);
            this.btnLive.TabIndex = 2;
            this.btnLive.Text = "Live Trade";
            this.btnLive.UseVisualStyleBackColor = true;
            this.btnLive.Click += new System.EventHandler(this.BtnLive_Click);
            // 
            // lblProj100
            // 
            this.lblProj100.AutoSize = true;
            this.lblProj100.Location = new System.Drawing.Point(320, 16);
            this.lblProj100.Margin = new System.Windows.Forms.Padding(6, 8, 3, 0);
            this.lblProj100.Name = "lblProj100";
            this.lblProj100.Size = new System.Drawing.Size(61, 13);
            this.lblProj100.TabIndex = 3;
            this.lblProj100.Text = "projection: -";
            // 
            // lblProj1000
            // 
            this.lblProj1000.AutoSize = true;
            this.lblProj1000.Location = new System.Drawing.Point(390, 16);
            this.lblProj1000.Margin = new System.Windows.Forms.Padding(6, 8, 3, 0);
            this.lblProj1000.Name = "lblProj1000";
            this.lblProj1000.Size = new System.Drawing.Size(61, 13);
            this.lblProj1000.TabIndex = 4;
            this.lblProj1000.Text = "projection: -";
            // 
            // lblTradeStatus
            // 
            this.lblTradeStatus.AutoSize = true;
            this.lblTradeStatus.Location = new System.Drawing.Point(460, 16);
            this.lblTradeStatus.Margin = new System.Windows.Forms.Padding(6, 8, 3, 0);
            this.lblTradeStatus.Name = "lblTradeStatus";
            this.lblTradeStatus.Size = new System.Drawing.Size(80, 13);
            this.lblTradeStatus.TabIndex = 5;
            this.lblTradeStatus.Text = "Status: Ready";
            // 
            // mainSplit
            // 
            this.mainSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainSplit.Location = new System.Drawing.Point(3, 51);
            this.mainSplit.Name = "mainSplit";
            // 
            // mainSplit.Panel1
            // 
            this.mainSplit.Panel1.Controls.Add(this.configPanel);
            // 
            // mainSplit.Panel2
            // 
            this.mainSplit.Panel2.Controls.Add(this.rightLayout);
            this.mainSplit.Size = new System.Drawing.Size(894, 546);
            this.mainSplit.SplitterDistance = 260;
            this.mainSplit.TabIndex = 1;
            // 
            // configPanel
            // 
            this.configPanel.AutoScroll = true;
            this.configPanel.Controls.Add(this.lblExchange);
            this.configPanel.Controls.Add(this.cmbExchange);
            this.configPanel.Controls.Add(this.lblProduct);
            this.configPanel.Controls.Add(this.cmbProduct);
            this.configPanel.Controls.Add(this.btnLoadProducts);
            this.configPanel.Controls.Add(this.btnFees);
            this.configPanel.Controls.Add(this.lblStrategy);
            this.configPanel.Controls.Add(this.cmbStrategy);
            this.configPanel.Controls.Add(this.lblRisk);
            this.configPanel.Controls.Add(this.numRisk);
            this.configPanel.Controls.Add(this.lblEquity);
            this.configPanel.Controls.Add(this.numEquity);
            this.configPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.configPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.configPanel.Location = new System.Drawing.Point(0, 0);
            this.configPanel.Name = "configPanel";
            this.configPanel.Padding = new System.Windows.Forms.Padding(10);
            this.configPanel.Size = new System.Drawing.Size(260, 546);
            this.configPanel.TabIndex = 0;
            this.configPanel.WrapContents = false;
            // 
            // lblExchange
            // 
            this.lblExchange.AutoSize = true;
            this.lblExchange.Location = new System.Drawing.Point(13, 13);
            this.lblExchange.Name = "lblExchange";
            this.lblExchange.Size = new System.Drawing.Size(55, 13);
            this.lblExchange.TabIndex = 0;
            this.lblExchange.Text = "Exchange";
            // 
            // cmbExchange
            // 
            this.cmbExchange.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbExchange.FormattingEnabled = true;
            this.cmbExchange.Location = new System.Drawing.Point(13, 29);
            this.cmbExchange.Name = "cmbExchange";
            this.cmbExchange.Size = new System.Drawing.Size(220, 21);
            this.cmbExchange.TabIndex = 1;
            // 
            // lblProduct
            // 
            this.lblProduct.AutoSize = true;
            this.lblProduct.Location = new System.Drawing.Point(13, 58);
            this.lblProduct.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
            this.lblProduct.Name = "lblProduct";
            this.lblProduct.Size = new System.Drawing.Size(44, 13);
            this.lblProduct.TabIndex = 2;
            this.lblProduct.Text = "Product";
            // 
            // cmbProduct
            // 
            this.cmbProduct.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbProduct.FormattingEnabled = true;
            this.cmbProduct.Location = new System.Drawing.Point(13, 74);
            this.cmbProduct.Name = "cmbProduct";
            this.cmbProduct.Size = new System.Drawing.Size(220, 21);
            this.cmbProduct.TabIndex = 3;
            // 
            // btnLoadProducts
            // 
            this.btnLoadProducts.Location = new System.Drawing.Point(13, 104);
            this.btnLoadProducts.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
            this.btnLoadProducts.Name = "btnLoadProducts";
            this.btnLoadProducts.Size = new System.Drawing.Size(140, 24);
            this.btnLoadProducts.TabIndex = 4;
            this.btnLoadProducts.Text = "Load Products";
            this.btnLoadProducts.UseVisualStyleBackColor = true;
            this.btnLoadProducts.Click += new System.EventHandler(this.BtnLoadProducts_Click);
            // 
            // btnFees
            // 
            this.btnFees.Location = new System.Drawing.Point(13, 131);
            this.btnFees.Name = "btnFees";
            this.btnFees.Size = new System.Drawing.Size(140, 24);
            this.btnFees.TabIndex = 5;
            this.btnFees.Text = "Get Fees";
            this.btnFees.UseVisualStyleBackColor = true;
            this.btnFees.Click += new System.EventHandler(this.BtnFees_Click);
            // 
            // lblStrategy
            // 
            this.lblStrategy.AutoSize = true;
            this.lblStrategy.Location = new System.Drawing.Point(13, 163);
            this.lblStrategy.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
            this.lblStrategy.Name = "lblStrategy";
            this.lblStrategy.Size = new System.Drawing.Size(46, 13);
            this.lblStrategy.TabIndex = 6;
            this.lblStrategy.Text = "Strategy";
            // 
            // cmbStrategy
            // 
            this.cmbStrategy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbStrategy.FormattingEnabled = true;
            this.cmbStrategy.Location = new System.Drawing.Point(13, 179);
            this.cmbStrategy.Name = "cmbStrategy";
            this.cmbStrategy.Size = new System.Drawing.Size(220, 21);
            this.cmbStrategy.TabIndex = 7;
            // 
            // lblRisk
            // 
            this.lblRisk.AutoSize = true;
            this.lblRisk.Location = new System.Drawing.Point(13, 208);
            this.lblRisk.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
            this.lblRisk.Name = "lblRisk";
            this.lblRisk.Size = new System.Drawing.Size(40, 13);
            this.lblRisk.TabIndex = 8;
            this.lblRisk.Text = "Risk %";
            // 
            // numRisk
            // 
            this.numRisk.Location = new System.Drawing.Point(13, 224);
            this.numRisk.Maximum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numRisk.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numRisk.Name = "numRisk";
            this.numRisk.Size = new System.Drawing.Size(120, 20);
            this.numRisk.TabIndex = 9;
            this.numRisk.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // lblEquity
            // 
            this.lblEquity.AutoSize = true;
            this.lblEquity.Location = new System.Drawing.Point(13, 252);
            this.lblEquity.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
            this.lblEquity.Name = "lblEquity";
            this.lblEquity.Size = new System.Drawing.Size(43, 13);
            this.lblEquity.TabIndex = 10;
            this.lblEquity.Text = "Equity $";
            // 
            // numEquity
            // 
            this.numEquity.Location = new System.Drawing.Point(13, 268);
            this.numEquity.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.numEquity.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numEquity.Name = "numEquity";
            this.numEquity.Size = new System.Drawing.Size(120, 20);
            this.numEquity.TabIndex = 11;
            this.numEquity.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // rightLayout
            // 
            this.rightLayout.ColumnCount = 1;
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Controls.Add(this.chartHost, 0, 0);
            this.rightLayout.Controls.Add(this.txtLog, 0, 1);
            this.rightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightLayout.Location = new System.Drawing.Point(0, 0);
            this.rightLayout.Name = "rightLayout";
            this.rightLayout.RowCount = 2;
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 75F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.rightLayout.Size = new System.Drawing.Size(630, 546);
            this.rightLayout.TabIndex = 0;
            // 
            // chartHost
            // 
            this.chartHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartHost.Location = new System.Drawing.Point(3, 3);
            this.chartHost.Name = "chartHost";
            this.chartHost.Size = new System.Drawing.Size(624, 403);
            this.chartHost.TabIndex = 0;
            // 
            // txtLog
            // 
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtLog.Location = new System.Drawing.Point(3, 412);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(624, 131);
            this.txtLog.TabIndex = 1;
            this.txtLog.WordWrap = false;
            // 
            // TradingControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tlMain);
            this.Name = "TradingControl";
            this.Size = new System.Drawing.Size(900, 600);
            this.tlMain.ResumeLayout(false);
            this.tlMain.PerformLayout();
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.mainSplit.Panel1.ResumeLayout(false);
            this.mainSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.mainSplit)).EndInit();
            this.mainSplit.ResumeLayout(false);
            this.configPanel.ResumeLayout(false);
            this.configPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numRisk)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numEquity)).EndInit();
            this.rightLayout.ResumeLayout(false);
            this.rightLayout.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tlMain;
        private System.Windows.Forms.FlowLayoutPanel pnlTop;
        private System.Windows.Forms.Button btnBacktest;
        private System.Windows.Forms.Button btnPaper;
        private System.Windows.Forms.Button btnLive;
        private System.Windows.Forms.Label lblProj100;
        private System.Windows.Forms.Label lblProj1000;
        private System.Windows.Forms.Label lblTradeStatus;
        private System.Windows.Forms.SplitContainer mainSplit;
        private System.Windows.Forms.FlowLayoutPanel configPanel;
        private System.Windows.Forms.Label lblExchange;
        private System.Windows.Forms.ComboBox cmbExchange;
        private System.Windows.Forms.Label lblProduct;
        private System.Windows.Forms.ComboBox cmbProduct;
        private System.Windows.Forms.Button btnLoadProducts;
        private System.Windows.Forms.Button btnFees;
        private System.Windows.Forms.Label lblStrategy;
        private System.Windows.Forms.ComboBox cmbStrategy;
        private System.Windows.Forms.Label lblRisk;
        private System.Windows.Forms.NumericUpDown numRisk;
        private System.Windows.Forms.Label lblEquity;
        private System.Windows.Forms.NumericUpDown numEquity;
        private System.Windows.Forms.TableLayoutPanel rightLayout;
        private System.Windows.Forms.Panel chartHost;
        private System.Windows.Forms.TextBox txtLog;
    }
}
