namespace CryptoDayTraderSuite.UI
{
    partial class AccountEditDialog
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblLabel = new System.Windows.Forms.Label();
            this.txtLabel = new System.Windows.Forms.TextBox();
            this.lblService = new System.Windows.Forms.Label();
            this.cmbService = new System.Windows.Forms.ComboBox();
            this.lblMode = new System.Windows.Forms.Label();
            this.cmbMode = new System.Windows.Forms.ComboBox();
            this.lblRisk = new System.Windows.Forms.Label();
            this.numRisk = new System.Windows.Forms.NumericUpDown();
            this.lblMax = new System.Windows.Forms.Label();
            this.numMax = new System.Windows.Forms.NumericUpDown();
            this.lblKeyId = new System.Windows.Forms.Label();
            this.txtKeyId = new System.Windows.Forms.TextBox();
            this.lblExistingKey = new System.Windows.Forms.Label();
            this.cmbExistingKey = new System.Windows.Forms.ComboBox();
            this.lblCredHeader = new System.Windows.Forms.Label();
            this.lblKeyLabel = new System.Windows.Forms.Label();
            this.txtKeyLabel = new System.Windows.Forms.TextBox();
            this.lblApiKey = new System.Windows.Forms.Label();
            this.txtApiKey = new System.Windows.Forms.TextBox();
            this.pnlApiKeyImport = new System.Windows.Forms.TableLayoutPanel();
            this.btnImportCoinbaseJson = new System.Windows.Forms.Button();
            this.lblSecret = new System.Windows.Forms.Label();
            this.txtSecret = new System.Windows.Forms.TextBox();
            this.lblPassphrase = new System.Windows.Forms.Label();
            this.txtPassphrase = new System.Windows.Forms.TextBox();
            this.lblApiKeyName = new System.Windows.Forms.Label();
            this.txtApiKeyName = new System.Windows.Forms.TextBox();
            this.lblPem = new System.Windows.Forms.Label();
            this.txtPem = new System.Windows.Forms.TextBox();
            this.actionPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.mainLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numRisk)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMax)).BeginInit();
            this.actionPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainLayout
            // 
            this.mainLayout.AutoScroll = true;
            this.mainLayout.ColumnCount = 2;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 148F));
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Controls.Add(this.lblLabel, 0, 0);
            this.mainLayout.Controls.Add(this.txtLabel, 1, 0);
            this.mainLayout.Controls.Add(this.lblService, 0, 1);
            this.mainLayout.Controls.Add(this.cmbService, 1, 1);
            this.mainLayout.Controls.Add(this.lblMode, 0, 2);
            this.mainLayout.Controls.Add(this.cmbMode, 1, 2);
            this.mainLayout.Controls.Add(this.lblRisk, 0, 3);
            this.mainLayout.Controls.Add(this.numRisk, 1, 3);
            this.mainLayout.Controls.Add(this.lblMax, 0, 4);
            this.mainLayout.Controls.Add(this.numMax, 1, 4);
            this.mainLayout.Controls.Add(this.lblKeyId, 0, 5);
            this.mainLayout.Controls.Add(this.txtKeyId, 1, 5);
            this.mainLayout.Controls.Add(this.lblExistingKey, 0, 6);
            this.mainLayout.Controls.Add(this.cmbExistingKey, 1, 6);
            this.mainLayout.Controls.Add(this.lblCredHeader, 0, 7);
            this.mainLayout.Controls.Add(this.lblKeyLabel, 0, 8);
            this.mainLayout.Controls.Add(this.txtKeyLabel, 1, 8);
            this.mainLayout.Controls.Add(this.lblApiKey, 0, 9);
            this.mainLayout.Controls.Add(this.pnlApiKeyImport, 1, 9);
            this.mainLayout.Controls.Add(this.lblSecret, 0, 10);
            this.mainLayout.Controls.Add(this.txtSecret, 1, 10);
            this.mainLayout.Controls.Add(this.lblPassphrase, 0, 11);
            this.mainLayout.Controls.Add(this.txtPassphrase, 1, 11);
            this.mainLayout.Controls.Add(this.lblApiKeyName, 0, 12);
            this.mainLayout.Controls.Add(this.txtApiKeyName, 1, 12);
            this.mainLayout.Controls.Add(this.lblPem, 0, 13);
            this.mainLayout.Controls.Add(this.txtPem, 1, 13);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Location = new System.Drawing.Point(0, 0);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.Padding = new System.Windows.Forms.Padding(14);
            this.mainLayout.RowCount = 14;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainLayout.Size = new System.Drawing.Size(704, 525);
            this.mainLayout.TabIndex = 0;
            // 
            // lblLabel
            // 
            this.lblLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblLabel.AutoSize = true;
            this.lblLabel.Location = new System.Drawing.Point(17, 17);
            this.lblLabel.Name = "lblLabel";
            this.lblLabel.Size = new System.Drawing.Size(33, 13);
            this.lblLabel.TabIndex = 0;
            this.lblLabel.Text = "Label";
            // 
            // txtLabel
            // 
            this.txtLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))));
            this.txtLabel.Location = new System.Drawing.Point(165, 14);
            this.txtLabel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
            this.txtLabel.Name = "txtLabel";
            this.txtLabel.Size = new System.Drawing.Size(522, 20);
            this.txtLabel.TabIndex = 1;
            // 
            // lblService
            // 
            this.lblService.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblService.AutoSize = true;
            this.lblService.Location = new System.Drawing.Point(17, 48);
            this.lblService.Name = "lblService";
            this.lblService.Size = new System.Drawing.Size(43, 13);
            this.lblService.TabIndex = 2;
            this.lblService.Text = "Service";
            // 
            // cmbService
            // 
            this.cmbService.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbService.FormattingEnabled = true;
            this.cmbService.Items.AddRange(new object[] {
            "paper",
            "coinbase-advanced",
            "binance",
            "binance-us",
            "binance-global",
            "bybit",
            "bybit-global",
            "okx",
            "okx-global",
            "kraken",
            "bitstamp"});
            this.cmbService.Location = new System.Drawing.Point(165, 45);
            this.cmbService.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
            this.cmbService.Name = "cmbService";
            this.cmbService.Size = new System.Drawing.Size(260, 21);
            this.cmbService.TabIndex = 3;
            this.cmbService.SelectedIndexChanged += new System.EventHandler(this.cmbService_SelectedIndexChanged);
            // 
            // lblMode
            // 
            this.lblMode.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblMode.AutoSize = true;
            this.lblMode.Location = new System.Drawing.Point(17, 77);
            this.lblMode.Name = "lblMode";
            this.lblMode.Size = new System.Drawing.Size(34, 13);
            this.lblMode.TabIndex = 4;
            this.lblMode.Text = "Mode";
            // 
            // cmbMode
            // 
            this.cmbMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMode.FormattingEnabled = true;
            this.cmbMode.Items.AddRange(new object[] {
            "Paper",
            "Live"});
            this.cmbMode.Location = new System.Drawing.Point(165, 74);
            this.cmbMode.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
            this.cmbMode.Name = "cmbMode";
            this.cmbMode.Size = new System.Drawing.Size(260, 21);
            this.cmbMode.TabIndex = 5;
            // 
            // lblRisk
            // 
            this.lblRisk.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblRisk.AutoSize = true;
            this.lblRisk.Location = new System.Drawing.Point(17, 106);
            this.lblRisk.Name = "lblRisk";
            this.lblRisk.Size = new System.Drawing.Size(79, 13);
            this.lblRisk.TabIndex = 6;
            this.lblRisk.Text = "Risk per trade %";
            // 
            // numRisk
            // 
            this.numRisk.DecimalPlaces = 2;
            this.numRisk.Location = new System.Drawing.Point(165, 103);
            this.numRisk.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
            this.numRisk.Name = "numRisk";
            this.numRisk.Size = new System.Drawing.Size(120, 20);
            this.numRisk.TabIndex = 7;
            this.numRisk.Value = new decimal(new int[] {
            50,
            0,
            0,
            131072});
            // 
            // lblMax
            // 
            this.lblMax.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblMax.AutoSize = true;
            this.lblMax.Location = new System.Drawing.Point(17, 135);
            this.lblMax.Name = "lblMax";
            this.lblMax.Size = new System.Drawing.Size(77, 13);
            this.lblMax.TabIndex = 8;
            this.lblMax.Text = "Max concurrent";
            // 
            // numMax
            // 
            this.numMax.Location = new System.Drawing.Point(165, 132);
            this.numMax.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
            this.numMax.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numMax.Name = "numMax";
            this.numMax.Size = new System.Drawing.Size(120, 20);
            this.numMax.TabIndex = 9;
            this.numMax.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // lblKeyId
            // 
            this.lblKeyId.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblKeyId.AutoSize = true;
            this.lblKeyId.Location = new System.Drawing.Point(17, 164);
            this.lblKeyId.Name = "lblKeyId";
            this.lblKeyId.Size = new System.Drawing.Size(59, 13);
            this.lblKeyId.TabIndex = 10;
            this.lblKeyId.Text = "Key Entry Id";
            // 
            // txtKeyId
            // 
            this.txtKeyId.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))));
            this.txtKeyId.Location = new System.Drawing.Point(165, 161);
            this.txtKeyId.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
            this.txtKeyId.Name = "txtKeyId";
            this.txtKeyId.Size = new System.Drawing.Size(522, 20);
            this.txtKeyId.TabIndex = 11;
            // 
            // lblExistingKey
            // 
            this.lblExistingKey.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblExistingKey.AutoSize = true;
            this.lblExistingKey.Location = new System.Drawing.Point(17, 194);
            this.lblExistingKey.Name = "lblExistingKey";
            this.lblExistingKey.Size = new System.Drawing.Size(70, 13);
            this.lblExistingKey.TabIndex = 12;
            this.lblExistingKey.Text = "Existing API Key";
            // 
            // cmbExistingKey
            // 
            this.cmbExistingKey.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbExistingKey.FormattingEnabled = true;
            this.cmbExistingKey.Location = new System.Drawing.Point(165, 190);
            this.cmbExistingKey.Margin = new System.Windows.Forms.Padding(3, 3, 3, 10);
            this.cmbExistingKey.Name = "cmbExistingKey";
            this.cmbExistingKey.Size = new System.Drawing.Size(522, 21);
            this.cmbExistingKey.TabIndex = 13;
            this.cmbExistingKey.SelectedIndexChanged += new System.EventHandler(this.cmbExistingKey_SelectedIndexChanged);
            // 
            // lblCredHeader
            // 
            this.lblCredHeader.AutoSize = true;
            this.mainLayout.SetColumnSpan(this.lblCredHeader, 2);
            this.lblCredHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCredHeader.Location = new System.Drawing.Point(17, 236);
            this.lblCredHeader.Margin = new System.Windows.Forms.Padding(3, 16, 3, 8);
            this.lblCredHeader.Name = "lblCredHeader";
            this.lblCredHeader.Size = new System.Drawing.Size(87, 15);
            this.lblCredHeader.TabIndex = 14;
            this.lblCredHeader.Text = "API Credentials";
            // 
            // lblKeyLabel
            // 
            this.lblKeyLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblKeyLabel.AutoSize = true;
            this.lblKeyLabel.Location = new System.Drawing.Point(17, 264);
            this.lblKeyLabel.Name = "lblKeyLabel";
            this.lblKeyLabel.Size = new System.Drawing.Size(56, 13);
            this.lblKeyLabel.TabIndex = 15;
            this.lblKeyLabel.Text = "Key Label";
            // 
            // txtKeyLabel
            // 
            this.txtKeyLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))));
            this.txtKeyLabel.Location = new System.Drawing.Point(165, 261);
            this.txtKeyLabel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
            this.txtKeyLabel.Name = "txtKeyLabel";
            this.txtKeyLabel.Size = new System.Drawing.Size(522, 20);
            this.txtKeyLabel.TabIndex = 16;
            // 
            // lblApiKey
            // 
            this.lblApiKey.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblApiKey.AutoSize = true;
            this.lblApiKey.Location = new System.Drawing.Point(17, 293);
            this.lblApiKey.Name = "lblApiKey";
            this.lblApiKey.Size = new System.Drawing.Size(43, 13);
            this.lblApiKey.TabIndex = 17;
            this.lblApiKey.Text = "API Key";
            // 
            // pnlApiKeyImport
            // 
            this.pnlApiKeyImport.ColumnCount = 2;
            this.pnlApiKeyImport.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlApiKeyImport.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.pnlApiKeyImport.Controls.Add(this.txtApiKey, 0, 0);
            this.pnlApiKeyImport.Controls.Add(this.btnImportCoinbaseJson, 1, 0);
            this.pnlApiKeyImport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlApiKeyImport.Location = new System.Drawing.Point(165, 287);
            this.pnlApiKeyImport.Margin = new System.Windows.Forms.Padding(0);
            this.pnlApiKeyImport.Name = "pnlApiKeyImport";
            this.pnlApiKeyImport.RowCount = 1;
            this.pnlApiKeyImport.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlApiKeyImport.Size = new System.Drawing.Size(522, 29);
            this.pnlApiKeyImport.TabIndex = 18;
            // 
            // txtApiKey
            // 
            this.txtApiKey.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))));
            this.txtApiKey.Location = new System.Drawing.Point(3, 3);
            this.txtApiKey.Margin = new System.Windows.Forms.Padding(3, 3, 8, 8);
            this.txtApiKey.Name = "txtApiKey";
            this.txtApiKey.Size = new System.Drawing.Size(426, 20);
            this.txtApiKey.TabIndex = 0;
            // 
            // btnImportCoinbaseJson
            // 
            this.btnImportCoinbaseJson.AutoSize = true;
            this.btnImportCoinbaseJson.Location = new System.Drawing.Point(437, 3);
            this.btnImportCoinbaseJson.Margin = new System.Windows.Forms.Padding(0, 3, 3, 8);
            this.btnImportCoinbaseJson.Name = "btnImportCoinbaseJson";
            this.btnImportCoinbaseJson.Size = new System.Drawing.Size(82, 23);
            this.btnImportCoinbaseJson.TabIndex = 1;
            this.btnImportCoinbaseJson.Text = "Import JSON...";
            this.btnImportCoinbaseJson.UseVisualStyleBackColor = true;
            this.btnImportCoinbaseJson.Click += new System.EventHandler(this.btnImportCoinbaseJson_Click);
            // 
            // lblSecret
            // 
            this.lblSecret.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSecret.AutoSize = true;
            this.lblSecret.Location = new System.Drawing.Point(17, 322);
            this.lblSecret.Name = "lblSecret";
            this.lblSecret.Size = new System.Drawing.Size(89, 13);
            this.lblSecret.TabIndex = 19;
            this.lblSecret.Text = "API Secret (base64)";
            // 
            // txtSecret
            // 
            this.txtSecret.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))));
            this.txtSecret.Location = new System.Drawing.Point(165, 319);
            this.txtSecret.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
            this.txtSecret.Name = "txtSecret";
            this.txtSecret.Size = new System.Drawing.Size(522, 20);
            this.txtSecret.TabIndex = 20;
            // 
            // lblPassphrase
            // 
            this.lblPassphrase.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblPassphrase.AutoSize = true;
            this.lblPassphrase.Location = new System.Drawing.Point(17, 351);
            this.lblPassphrase.Name = "lblPassphrase";
            this.lblPassphrase.Size = new System.Drawing.Size(61, 13);
            this.lblPassphrase.TabIndex = 21;
            this.lblPassphrase.Text = "Passphrase";
            // 
            // txtPassphrase
            // 
            this.txtPassphrase.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))));
            this.txtPassphrase.Location = new System.Drawing.Point(165, 348);
            this.txtPassphrase.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
            this.txtPassphrase.Name = "txtPassphrase";
            this.txtPassphrase.Size = new System.Drawing.Size(522, 20);
            this.txtPassphrase.TabIndex = 22;
            // 
            // lblApiKeyName
            // 
            this.lblApiKeyName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblApiKeyName.AutoSize = true;
            this.lblApiKeyName.Location = new System.Drawing.Point(17, 380);
            this.lblApiKeyName.Name = "lblApiKeyName";
            this.lblApiKeyName.Size = new System.Drawing.Size(70, 13);
            this.lblApiKeyName.TabIndex = 23;
            this.lblApiKeyName.Text = "API Key Name";
            // 
            // txtApiKeyName
            // 
            this.txtApiKeyName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))));
            this.txtApiKeyName.Location = new System.Drawing.Point(165, 377);
            this.txtApiKeyName.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
            this.txtApiKeyName.Name = "txtApiKeyName";
            this.txtApiKeyName.Size = new System.Drawing.Size(522, 20);
            this.txtApiKeyName.TabIndex = 24;
            // 
            // lblPem
            // 
            this.lblPem.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblPem.AutoSize = true;
            this.lblPem.Location = new System.Drawing.Point(17, 426);
            this.lblPem.Name = "lblPem";
            this.lblPem.Size = new System.Drawing.Size(78, 13);
            this.lblPem.TabIndex = 25;
            this.lblPem.Text = "EC Private Key";
            // 
            // txtPem
            // 
            this.txtPem.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right))));
            this.txtPem.Location = new System.Drawing.Point(165, 406);
            this.txtPem.AcceptsReturn = true;
            this.txtPem.Margin = new System.Windows.Forms.Padding(3, 3, 3, 10);
            this.txtPem.Multiline = true;
            this.txtPem.Name = "txtPem";
            this.txtPem.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtPem.Size = new System.Drawing.Size(522, 90);
            this.txtPem.TabIndex = 26;
            this.txtPem.WordWrap = false;
            // 
            // actionPanel
            // 
            this.actionPanel.Controls.Add(this.btnCancel);
            this.actionPanel.Controls.Add(this.btnSave);
            this.actionPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.actionPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.actionPanel.Location = new System.Drawing.Point(0, 525);
            this.actionPanel.Name = "actionPanel";
            this.actionPanel.Padding = new System.Windows.Forms.Padding(12, 10, 12, 10);
            this.actionPanel.Size = new System.Drawing.Size(704, 52);
            this.actionPanel.TabIndex = 1;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(606, 13);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(74, 28);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(526, 13);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(74, 28);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // AccountEditDialog
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(704, 577);
            this.Controls.Add(this.mainLayout);
            this.Controls.Add(this.actionPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AccountEditDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Account";
            this.mainLayout.ResumeLayout(false);
            this.mainLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numRisk)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMax)).EndInit();
            this.actionPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.Label lblLabel;
        private System.Windows.Forms.Label lblService;
        private System.Windows.Forms.Label lblMode;
        private System.Windows.Forms.Label lblRisk;
        private System.Windows.Forms.Label lblMax;
        private System.Windows.Forms.Label lblCredHeader;
        private System.Windows.Forms.FlowLayoutPanel actionPanel;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.TextBox txtLabel;
        private System.Windows.Forms.ComboBox cmbService;
        private System.Windows.Forms.ComboBox cmbMode;
        private System.Windows.Forms.NumericUpDown numRisk;
        private System.Windows.Forms.NumericUpDown numMax;
        private System.Windows.Forms.TextBox txtKeyId;
        private System.Windows.Forms.Label lblExistingKey;
        private System.Windows.Forms.ComboBox cmbExistingKey;
        private System.Windows.Forms.TextBox txtKeyLabel;
        private System.Windows.Forms.TextBox txtApiKey;
        private System.Windows.Forms.TextBox txtSecret;
        private System.Windows.Forms.TextBox txtPassphrase;
        private System.Windows.Forms.TextBox txtApiKeyName;
        private System.Windows.Forms.TextBox txtPem;
        private System.Windows.Forms.Label lblKeyId;
        private System.Windows.Forms.Label lblKeyLabel;
        private System.Windows.Forms.Label lblApiKey;
        private System.Windows.Forms.Label lblSecret;
        private System.Windows.Forms.Label lblPassphrase;
        private System.Windows.Forms.Label lblApiKeyName;
        private System.Windows.Forms.Label lblPem;
        private System.Windows.Forms.TableLayoutPanel pnlApiKeyImport;
        private System.Windows.Forms.Button btnImportCoinbaseJson;
    }
}