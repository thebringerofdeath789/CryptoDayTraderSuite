/* File: MainForm.Designer.cs */
/* Author: Gregory King */
/* Date: 2025-08-10 */
/* Description: designer code for main form without PlaceholderText property for .net 4.8 */
/* Functions: InitializeComponent, Dispose */

namespace CryptoDayTraderSuite
{
	partial class MainForm
	{
		private System.ComponentModel.IContainer components = null;
		private System.Windows.Forms.ComboBox cmbExchange;
		private System.Windows.Forms.ComboBox cmbProduct;
		private System.Windows.Forms.Button btnLoadProducts;
		private System.Windows.Forms.Button btnBacktest;
		private System.Windows.Forms.Button btnPaper;
		private System.Windows.Forms.Button btnLive;
		private System.Windows.Forms.TextBox txtLog;
		private System.Windows.Forms.NumericUpDown numRisk;
		private System.Windows.Forms.NumericUpDown numEquity;
		private System.Windows.Forms.ComboBox cmbStrategy;
		private System.Windows.Forms.Button btnFees;
		private System.Windows.Forms.TextBox txtApiKey;
		private System.Windows.Forms.TextBox txtSecret;
		private System.Windows.Forms.TextBox txtExtra;
		private System.Windows.Forms.Button btnSaveKeys;
		private System.Windows.Forms.Label lblProj100;
		private System.Windows.Forms.Label lblProj1000;

		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null)) { components.Dispose(); } /* dispose */
			base.Dispose(disposing); /* base */
		}

		private void InitializeComponent()
		{
			this.cmbExchange = new System.Windows.Forms.ComboBox();
			this.cmbProduct = new System.Windows.Forms.ComboBox();
			this.btnLoadProducts = new System.Windows.Forms.Button();
			this.btnBacktest = new System.Windows.Forms.Button();
			this.btnPaper = new System.Windows.Forms.Button();
			this.btnLive = new System.Windows.Forms.Button();
			this.txtLog = new System.Windows.Forms.TextBox();
			this.numRisk = new System.Windows.Forms.NumericUpDown();
			this.numEquity = new System.Windows.Forms.NumericUpDown();
			this.cmbStrategy = new System.Windows.Forms.ComboBox();
			this.btnFees = new System.Windows.Forms.Button();
			this.txtApiKey = new System.Windows.Forms.TextBox();
			this.txtSecret = new System.Windows.Forms.TextBox();
			this.txtExtra = new System.Windows.Forms.TextBox();
			this.btnSaveKeys = new System.Windows.Forms.Button();
			this.lblProj100 = new System.Windows.Forms.Label();
			this.lblProj1000 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.numRisk)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numEquity)).BeginInit();
			this.SuspendLayout();
			/* cmbExchange */
			this.cmbExchange.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbExchange.FormattingEnabled = true;
			this.cmbExchange.Items.AddRange(new object[] { "Coinbase", "Kraken", "Bitstamp", "Binance-US", "Binance-Global", "Bybit-Global", "OKX-Global" });
			this.cmbExchange.Location = new System.Drawing.Point(12, 12);
			this.cmbExchange.Name = "cmbExchange";
			this.cmbExchange.Size = new System.Drawing.Size(180, 21);
			this.cmbExchange.TabIndex = 0;
			/* cmbProduct */
			this.cmbProduct.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbProduct.FormattingEnabled = true;
			this.cmbProduct.Location = new System.Drawing.Point(198, 12);
			this.cmbProduct.Name = "cmbProduct";
			this.cmbProduct.Size = new System.Drawing.Size(180, 21);
			this.cmbProduct.TabIndex = 1;
			/* btnLoadProducts */
			this.btnLoadProducts.Location = new System.Drawing.Point(384, 12);
			this.btnLoadProducts.Name = "btnLoadProducts";
			this.btnLoadProducts.Size = new System.Drawing.Size(120, 23);
			this.btnLoadProducts.TabIndex = 2;
			this.btnLoadProducts.Text = "Load Products";
			this.btnLoadProducts.UseVisualStyleBackColor = true;
			this.btnLoadProducts.Click += new System.EventHandler(this.btnLoadProducts_Click);
			/* btnBacktest */
			this.btnBacktest.Location = new System.Drawing.Point(12, 135);
			this.btnBacktest.Name = "btnBacktest";
			this.btnBacktest.Size = new System.Drawing.Size(120, 23);
			this.btnBacktest.TabIndex = 3;
			this.btnBacktest.Text = "Backtest";
			this.btnBacktest.UseVisualStyleBackColor = true;
			this.btnBacktest.Click += new System.EventHandler(this.btnBacktest_Click);
			/* btnPaper */
			this.btnPaper.Location = new System.Drawing.Point(138, 135);
			this.btnPaper.Name = "btnPaper";
			this.btnPaper.Size = new System.Drawing.Size(120, 23);
			this.btnPaper.TabIndex = 4;
			this.btnPaper.Text = "Start Paper";
			this.btnPaper.UseVisualStyleBackColor = true;
			this.btnPaper.Click += new System.EventHandler(this.btnPaper_Click);
			/* btnLive */
			this.btnLive.Location = new System.Drawing.Point(264, 135);
			this.btnLive.Name = "btnLive";
			this.btnLive.Size = new System.Drawing.Size(120, 23);
			this.btnLive.TabIndex = 5;
			this.btnLive.Text = "Start Live";
			this.btnLive.UseVisualStyleBackColor = true;
			this.btnLive.Click += new System.EventHandler(this.btnLive_Click);
			/* txtLog */
			this.txtLog.Location = new System.Drawing.Point(12, 164);
			this.txtLog.Multiline = true;
			this.txtLog.Name = "txtLog";
			this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.txtLog.Size = new System.Drawing.Size(760, 285);
			this.txtLog.TabIndex = 6;
			/* numRisk */
			this.numRisk.DecimalPlaces = 2;
			this.numRisk.Location = new System.Drawing.Point(12, 105);
			this.numRisk.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
			this.numRisk.Name = "numRisk";
			this.numRisk.Size = new System.Drawing.Size(120, 20);
			this.numRisk.TabIndex = 7;
			this.numRisk.Value = new decimal(new int[] { 1, 0, 0, 0 });
			/* numEquity */
			this.numEquity.DecimalPlaces = 2;
			this.numEquity.Location = new System.Drawing.Point(138, 105);
			this.numEquity.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
			this.numEquity.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
			this.numEquity.Name = "numEquity";
			this.numEquity.Size = new System.Drawing.Size(120, 20);
			this.numEquity.TabIndex = 8;
			this.numEquity.Value = new decimal(new int[] { 100, 0, 0, 0 });
			/* cmbStrategy */
			this.cmbStrategy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbStrategy.FormattingEnabled = true;
			this.cmbStrategy.Items.AddRange(new object[] { "ORB", "VWAPTrend" });
			this.cmbStrategy.Location = new System.Drawing.Point(12, 72);
			this.cmbStrategy.Name = "cmbStrategy";
			this.cmbStrategy.Size = new System.Drawing.Size(120, 21);
			this.cmbStrategy.TabIndex = 9;
			/* btnFees */
			this.btnFees.Location = new System.Drawing.Point(510, 12);
			this.btnFees.Name = "btnFees";
			this.btnFees.Size = new System.Drawing.Size(120, 23);
			this.btnFees.TabIndex = 10;
			this.btnFees.Text = "Get Fees";
			this.btnFees.UseVisualStyleBackColor = true;
			this.btnFees.Click += new System.EventHandler(this.btnFees_Click);
			/* txtApiKey */
			this.txtApiKey.Location = new System.Drawing.Point(198, 72);
			this.txtApiKey.Name = "txtApiKey";
			this.txtApiKey.Size = new System.Drawing.Size(180, 20);
			this.txtApiKey.TabIndex = 11;
			/* txtSecret */
			this.txtSecret.Location = new System.Drawing.Point(384, 72);
			this.txtSecret.Name = "txtSecret";
			this.txtSecret.Size = new System.Drawing.Size(180, 20);
			this.txtSecret.TabIndex = 12;
			/* txtExtra */
			this.txtExtra.Location = new System.Drawing.Point(570, 72);
			this.txtExtra.Name = "txtExtra";
			this.txtExtra.Size = new System.Drawing.Size(180, 20);
			this.txtExtra.TabIndex = 13;
			/* btnSaveKeys */
			this.btnSaveKeys.Location = new System.Drawing.Point(636, 12);
			this.btnSaveKeys.Name = "btnSaveKeys";
			this.btnSaveKeys.Size = new System.Drawing.Size(136, 23);
			this.btnSaveKeys.TabIndex = 14;
			this.btnSaveKeys.Text = "Save Keys";
			this.btnSaveKeys.UseVisualStyleBackColor = true;
			this.btnSaveKeys.Click += new System.EventHandler(this.btnSaveKeys_Click);
			/* lblProj100 */
			this.lblProj100.AutoSize = true;
			this.lblProj100.Location = new System.Drawing.Point(270, 108);
			this.lblProj100.Name = "lblProj100";
			this.lblProj100.Size = new System.Drawing.Size(108, 13);
			this.lblProj100.TabIndex = 15;
			this.lblProj100.Text = "$100 projection: n/a";
			/* lblProj1000 */
			this.lblProj1000.AutoSize = true;
			this.lblProj1000.Location = new System.Drawing.Point(420, 108);
			this.lblProj1000.Name = "lblProj1000";
			this.lblProj1000.Size = new System.Drawing.Size(114, 13);
			this.lblProj1000.TabIndex = 16;
			this.lblProj1000.Text = "$1000 projection: n/a";
			/* MainForm */
			this.ClientSize = new System.Drawing.Size(784, 461);
			this.Controls.Add(this.lblProj1000);
			this.Controls.Add(this.lblProj100);
			this.Controls.Add(this.btnSaveKeys);
			this.Controls.Add(this.txtExtra);
			this.Controls.Add(this.txtSecret);
			this.Controls.Add(this.txtApiKey);
			this.Controls.Add(this.btnFees);
			this.Controls.Add(this.cmbStrategy);
			this.Controls.Add(this.numEquity);
			this.Controls.Add(this.numRisk);
			this.Controls.Add(this.txtLog);
			this.Controls.Add(this.btnLive);
			this.Controls.Add(this.btnPaper);
			this.Controls.Add(this.btnBacktest);
			this.Controls.Add(this.btnLoadProducts);
			this.Controls.Add(this.cmbProduct);
			this.Controls.Add(this.cmbExchange);
			this.Name = "MainForm";
			this.Text = "Crypto Day-Trading Suite";
			this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
			((System.ComponentModel.ISupportInitialize)(this.numRisk)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numEquity)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();
		}
	}
}
