namespace CryptoDayTraderSuite.UI
{
    partial class AccountsControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.topPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnEdit = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnRefreshInsights = new System.Windows.Forms.Button();
            this.btnImportCoinbase = new System.Windows.Forms.Button();
            this.gridAccounts = new System.Windows.Forms.DataGridView();
            this.pnlCoinbaseInsights = new System.Windows.Forms.TableLayoutPanel();
            this.lblCoinbaseInsightsTitle = new System.Windows.Forms.Label();
            this.lblCoinbaseInsightsSummary = new System.Windows.Forms.Label();
            this.gridHoldings = new System.Windows.Forms.DataGridView();
            this.colHoldingCurrency = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHoldingAmount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLabel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colService = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRisk = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMaxOpen = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colKeyId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.mainLayout.SuspendLayout();
            this.topPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridAccounts)).BeginInit();
            this.pnlCoinbaseInsights.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridHoldings)).BeginInit();
            this.SuspendLayout();
            // 
            // mainLayout
            // 
            this.mainLayout.ColumnCount = 1;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Controls.Add(this.topPanel, 0, 0);
            this.mainLayout.Controls.Add(this.gridAccounts, 0, 1);
            this.mainLayout.Controls.Add(this.pnlCoinbaseInsights, 0, 2);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Location = new System.Drawing.Point(0, 0);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.RowCount = 3;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 58F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 42F));
            this.mainLayout.TabIndex = 0;
            // 
            // topPanel
            // 
            this.topPanel.Controls.Add(this.btnAdd);
            this.topPanel.Controls.Add(this.btnEdit);
            this.topPanel.Controls.Add(this.btnDelete);
            this.topPanel.Controls.Add(this.btnSave);
            this.topPanel.Controls.Add(this.btnRefresh);
            this.topPanel.Controls.Add(this.btnRefreshInsights);
            this.topPanel.Controls.Add(this.btnImportCoinbase);
            this.topPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topPanel.Location = new System.Drawing.Point(3, 3);
            this.topPanel.Name = "topPanel";
            this.topPanel.Size = new System.Drawing.Size(634, 34);
            this.topPanel.TabIndex = 0;
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(3, 3);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAdd.TabIndex = 0;
            this.btnAdd.Text = "Add";
            this.btnAdd.UseVisualStyleBackColor = true;
            // 
            // btnEdit
            // 
            this.btnEdit.Location = new System.Drawing.Point(84, 3);
            this.btnEdit.Name = "btnEdit";
            this.btnEdit.Size = new System.Drawing.Size(75, 23);
            this.btnEdit.TabIndex = 1;
            this.btnEdit.Text = "Edit";
            this.btnEdit.UseVisualStyleBackColor = true;
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(165, 3);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(75, 23);
            this.btnDelete.TabIndex = 2;
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(246, 3);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(327, 3);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(75, 23);
            this.btnRefresh.TabIndex = 4;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            // 
            // btnRefreshInsights
            // 
            this.btnRefreshInsights.Location = new System.Drawing.Point(408, 3);
            this.btnRefreshInsights.Name = "btnRefreshInsights";
            this.btnRefreshInsights.Size = new System.Drawing.Size(130, 23);
            this.btnRefreshInsights.TabIndex = 5;
            this.btnRefreshInsights.Text = "Refresh Insights";
            this.btnRefreshInsights.UseVisualStyleBackColor = true;
            // 
            // btnImportCoinbase
            // 
            this.btnImportCoinbase.Location = new System.Drawing.Point(544, 3);
            this.btnImportCoinbase.Name = "btnImportCoinbase";
            this.btnImportCoinbase.Size = new System.Drawing.Size(160, 23);
            this.btnImportCoinbase.TabIndex = 6;
            this.btnImportCoinbase.Text = "Import Coinbase (Read-only)";
            this.btnImportCoinbase.UseVisualStyleBackColor = true;
            // 
            // gridAccounts
            // 
            this.gridAccounts.AllowUserToAddRows = false;
            this.gridAccounts.AllowUserToDeleteRows = false;
            this.gridAccounts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridAccounts.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colLabel,
            this.colService,
            this.colMode,
            this.colRisk,
            this.colMaxOpen,
            this.colKeyId,
            this.colEnabled});
            this.gridAccounts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridAccounts.Location = new System.Drawing.Point(3, 43);
            this.gridAccounts.Name = "gridAccounts";
            this.gridAccounts.ReadOnly = true;
            this.gridAccounts.RowHeadersVisible = false;
            this.gridAccounts.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridAccounts.Size = new System.Drawing.Size(634, 251);
            this.gridAccounts.TabIndex = 1;
            // 
            // pnlCoinbaseInsights
            // 
            this.pnlCoinbaseInsights.ColumnCount = 1;
            this.pnlCoinbaseInsights.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlCoinbaseInsights.Controls.Add(this.lblCoinbaseInsightsTitle, 0, 0);
            this.pnlCoinbaseInsights.Controls.Add(this.lblCoinbaseInsightsSummary, 0, 1);
            this.pnlCoinbaseInsights.Controls.Add(this.gridHoldings, 0, 2);
            this.pnlCoinbaseInsights.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlCoinbaseInsights.Location = new System.Drawing.Point(3, 300);
            this.pnlCoinbaseInsights.Name = "pnlCoinbaseInsights";
            this.pnlCoinbaseInsights.RowCount = 3;
            this.pnlCoinbaseInsights.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.pnlCoinbaseInsights.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this.pnlCoinbaseInsights.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlCoinbaseInsights.Size = new System.Drawing.Size(634, 177);
            this.pnlCoinbaseInsights.TabIndex = 2;
            // 
            // lblCoinbaseInsightsTitle
            // 
            this.lblCoinbaseInsightsTitle.AutoSize = true;
            this.lblCoinbaseInsightsTitle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblCoinbaseInsightsTitle.Location = new System.Drawing.Point(3, 0);
            this.lblCoinbaseInsightsTitle.Name = "lblCoinbaseInsightsTitle";
            this.lblCoinbaseInsightsTitle.Size = new System.Drawing.Size(181, 15);
            this.lblCoinbaseInsightsTitle.TabIndex = 0;
            this.lblCoinbaseInsightsTitle.Text = "Account API Insights (Selected)";
            // 
            // lblCoinbaseInsightsSummary
            // 
            this.lblCoinbaseInsightsSummary.AutoSize = true;
            this.lblCoinbaseInsightsSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCoinbaseInsightsSummary.Location = new System.Drawing.Point(3, 22);
            this.lblCoinbaseInsightsSummary.Name = "lblCoinbaseInsightsSummary";
            this.lblCoinbaseInsightsSummary.Size = new System.Drawing.Size(628, 96);
            this.lblCoinbaseInsightsSummary.TabIndex = 1;
            this.lblCoinbaseInsightsSummary.Text = "Select an account to view API/account insights.";
            // 
            // gridHoldings
            // 
            this.gridHoldings.AllowUserToAddRows = false;
            this.gridHoldings.AllowUserToDeleteRows = false;
            this.gridHoldings.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridHoldings.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colHoldingCurrency,
            this.colHoldingAmount});
            this.gridHoldings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridHoldings.Location = new System.Drawing.Point(3, 121);
            this.gridHoldings.Name = "gridHoldings";
            this.gridHoldings.ReadOnly = true;
            this.gridHoldings.RowHeadersVisible = false;
            this.gridHoldings.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridHoldings.Size = new System.Drawing.Size(628, 53);
            this.gridHoldings.TabIndex = 2;
            // 
            // colHoldingCurrency
            // 
            this.colHoldingCurrency.HeaderText = "Currency";
            this.colHoldingCurrency.Name = "colHoldingCurrency";
            this.colHoldingCurrency.ReadOnly = true;
            // 
            // colHoldingAmount
            // 
            this.colHoldingAmount.HeaderText = "Amount";
            this.colHoldingAmount.Name = "colHoldingAmount";
            this.colHoldingAmount.ReadOnly = true;
            // 
            // colLabel
            // 
            this.colLabel.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colLabel.HeaderText = "Label";
            this.colLabel.Name = "colLabel";
            this.colLabel.ReadOnly = true;
            // 
            // colService
            // 
            this.colService.HeaderText = "Service";
            this.colService.Name = "colService";
            this.colService.ReadOnly = true;
            // 
            // colMode
            // 
            this.colMode.HeaderText = "Mode";
            this.colMode.Name = "colMode";
            this.colMode.ReadOnly = true;
            // 
            // colRisk
            // 
            this.colRisk.HeaderText = "Risk %";
            this.colRisk.Name = "colRisk";
            this.colRisk.ReadOnly = true;
            // 
            // colMaxOpen
            // 
            this.colMaxOpen.HeaderText = "Max Open";
            this.colMaxOpen.Name = "colMaxOpen";
            this.colMaxOpen.ReadOnly = true;
            // 
            // colKeyId
            // 
            this.colKeyId.HeaderText = "Key Id";
            this.colKeyId.Name = "colKeyId";
            this.colKeyId.ReadOnly = true;
            // 
            // colEnabled
            // 
            this.colEnabled.HeaderText = "Enabled";
            this.colEnabled.Name = "colEnabled";
            this.colEnabled.ReadOnly = true;
            // 
            // AccountsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainLayout);
            this.Name = "AccountsControl";
            this.Size = new System.Drawing.Size(640, 480);
            this.mainLayout.ResumeLayout(false);
            this.topPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridAccounts)).EndInit();
            this.pnlCoinbaseInsights.ResumeLayout(false);
            this.pnlCoinbaseInsights.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridHoldings)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.FlowLayoutPanel topPanel;
        public System.Windows.Forms.Button btnAdd;
        public System.Windows.Forms.Button btnEdit;
        public System.Windows.Forms.Button btnDelete;
        public System.Windows.Forms.Button btnSave;
        public System.Windows.Forms.Button btnRefresh;
        public System.Windows.Forms.Button btnRefreshInsights;
        public System.Windows.Forms.Button btnImportCoinbase;
        public System.Windows.Forms.DataGridView gridAccounts;
        private System.Windows.Forms.TableLayoutPanel pnlCoinbaseInsights;
        private System.Windows.Forms.Label lblCoinbaseInsightsTitle;
        private System.Windows.Forms.Label lblCoinbaseInsightsSummary;
        private System.Windows.Forms.DataGridView gridHoldings;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHoldingCurrency;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHoldingAmount;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLabel;
        private System.Windows.Forms.DataGridViewTextBoxColumn colService;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMode;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRisk;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMaxOpen;
        private System.Windows.Forms.DataGridViewTextBoxColumn colKeyId;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colEnabled;
    }
}
