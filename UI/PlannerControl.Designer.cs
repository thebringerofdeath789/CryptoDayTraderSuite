namespace CryptoDayTraderSuite.UI
{
    partial class PlannerControl
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
            this.components = new System.ComponentModel.Container();
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.headerBar = new System.Windows.Forms.FlowLayoutPanel();
            this.lblAccount = new System.Windows.Forms.Label();
            this.cmbAccount = new System.Windows.Forms.ComboBox();
            this.lblRunProduct = new System.Windows.Forms.Label();
            this.cmbRunProduct = new System.Windows.Forms.ComboBox();
            this.lblGran = new System.Windows.Forms.Label();
            this.cmbGran = new System.Windows.Forms.ComboBox();
            this.lblLookback = new System.Windows.Forms.Label();
            this.numLookback = new System.Windows.Forms.NumericUpDown();
            this.lblEquity = new System.Windows.Forms.Label();
            this.numEquity = new System.Windows.Forms.NumericUpDown();
            this.mainSplit = new System.Windows.Forms.SplitContainer();
            this.leftPanel = new System.Windows.Forms.Panel();
            this.topBar = new System.Windows.Forms.FlowLayoutPanel();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnAdd = new System.Windows.Forms.Button();
            this.lblProduct = new System.Windows.Forms.Label();
            this.cmbFilterProduct = new System.Windows.Forms.ComboBox();
            this.lblStrategy = new System.Windows.Forms.Label();
            this.cmbFilterStrategy = new System.Windows.Forms.ComboBox();
            this.rightLayout = new System.Windows.Forms.TableLayoutPanel();
            this.actionBar = new System.Windows.Forms.FlowLayoutPanel();
            this.btnScan = new System.Windows.Forms.Button();
            this.btnPropose = new System.Windows.Forms.Button();
            this.btnProposeAll = new System.Windows.Forms.Button();
            this.btnExecute = new System.Windows.Forms.Button();
            this.lblPlannerStatus = new System.Windows.Forms.Label();
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabPlanned = new System.Windows.Forms.TabPage();
            this.gridPlanned = new System.Windows.Forms.DataGridView();
            this.colEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colExchange = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colProduct = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStrategy = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSide = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colQty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEstEdge = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNotes = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ctxMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.miEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.miDelete = new System.Windows.Forms.ToolStripMenuItem();
            this.tabPredictions = new System.Windows.Forms.TabPage();
            this.gridPreds = new System.Windows.Forms.DataGridView();
            this.colPredProduct = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPredTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPredHorizon = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPredDir = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPredProb = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPredExpRet = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPredExpVol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPredKnown = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPredRDir = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPredRRet = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.mainLayout.SuspendLayout();
            this.headerBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numLookback)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numEquity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.mainSplit)).BeginInit();
            this.mainSplit.Panel1.SuspendLayout();
            this.mainSplit.Panel2.SuspendLayout();
            this.mainSplit.SuspendLayout();
            this.leftPanel.SuspendLayout();
            this.topBar.SuspendLayout();
            this.rightLayout.SuspendLayout();
            this.actionBar.SuspendLayout();
            this.tabMain.SuspendLayout();
            this.tabPlanned.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridPlanned)).BeginInit();
            this.ctxMenu.SuspendLayout();
            this.tabPredictions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridPreds)).BeginInit();
            this.SuspendLayout();
            // 
            // mainLayout
            // 
            this.mainLayout.ColumnCount = 1;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Controls.Add(this.headerBar, 0, 0);
            this.mainLayout.Controls.Add(this.mainSplit, 0, 1);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Location = new System.Drawing.Point(0, 0);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.RowCount = 2;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Size = new System.Drawing.Size(1200, 800);
            this.mainLayout.TabIndex = 0;
            // 
            // headerBar
            // 
            this.headerBar.AutoSize = true;
            this.headerBar.Controls.Add(this.lblAccount);
            this.headerBar.Controls.Add(this.cmbAccount);
            this.headerBar.Controls.Add(this.lblRunProduct);
            this.headerBar.Controls.Add(this.cmbRunProduct);
            this.headerBar.Controls.Add(this.lblGran);
            this.headerBar.Controls.Add(this.cmbGran);
            this.headerBar.Controls.Add(this.lblLookback);
            this.headerBar.Controls.Add(this.numLookback);
            this.headerBar.Controls.Add(this.lblEquity);
            this.headerBar.Controls.Add(this.numEquity);
            this.headerBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headerBar.Location = new System.Drawing.Point(3, 3);
            this.headerBar.Name = "headerBar";
            this.headerBar.Padding = new System.Windows.Forms.Padding(8);
            this.headerBar.Size = new System.Drawing.Size(1194, 40);
            this.headerBar.TabIndex = 0;
            this.headerBar.WrapContents = true;
            // 
            // lblAccount
            // 
            this.lblAccount.AutoSize = true;
            this.lblAccount.Location = new System.Drawing.Point(11, 14);
            this.lblAccount.Margin = new System.Windows.Forms.Padding(3, 6, 0, 0);
            this.lblAccount.Name = "lblAccount";
            this.lblAccount.Size = new System.Drawing.Size(50, 13);
            this.lblAccount.TabIndex = 0;
            this.lblAccount.Text = "Account:";
            // 
            // cmbAccount
            // 
            this.cmbAccount.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAccount.FormattingEnabled = true;
            this.cmbAccount.Location = new System.Drawing.Point(64, 11);
            this.cmbAccount.Name = "cmbAccount";
            this.cmbAccount.Size = new System.Drawing.Size(180, 21);
            this.cmbAccount.TabIndex = 1;
            // 
            // lblRunProduct
            // 
            this.lblRunProduct.AutoSize = true;
            this.lblRunProduct.Location = new System.Drawing.Point(259, 14);
            this.lblRunProduct.Margin = new System.Windows.Forms.Padding(12, 6, 0, 0);
            this.lblRunProduct.Name = "lblRunProduct";
            this.lblRunProduct.Size = new System.Drawing.Size(44, 13);
            this.lblRunProduct.TabIndex = 2;
            this.lblRunProduct.Text = "Symbol:";
            // 
            // cmbRunProduct
            // 
            this.cmbRunProduct.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbRunProduct.FormattingEnabled = true;
            this.cmbRunProduct.Location = new System.Drawing.Point(306, 11);
            this.cmbRunProduct.Name = "cmbRunProduct";
            this.cmbRunProduct.Size = new System.Drawing.Size(160, 21);
            this.cmbRunProduct.TabIndex = 3;
            // 
            // lblGran
            // 
            this.lblGran.AutoSize = true;
            this.lblGran.Location = new System.Drawing.Point(481, 14);
            this.lblGran.Margin = new System.Windows.Forms.Padding(12, 6, 0, 0);
            this.lblGran.Name = "lblGran";
            this.lblGran.Size = new System.Drawing.Size(37, 13);
            this.lblGran.TabIndex = 4;
            this.lblGran.Text = "Gran:";
            // 
            // cmbGran
            // 
            this.cmbGran.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbGran.FormattingEnabled = true;
            this.cmbGran.Items.AddRange(new object[] {
            "1",
            "5",
            "15",
            "30",
            "60",
            "240"});
            this.cmbGran.Location = new System.Drawing.Point(521, 11);
            this.cmbGran.Name = "cmbGran";
            this.cmbGran.Size = new System.Drawing.Size(70, 21);
            this.cmbGran.TabIndex = 5;
            // 
            // lblLookback
            // 
            this.lblLookback.AutoSize = true;
            this.lblLookback.Location = new System.Drawing.Point(606, 14);
            this.lblLookback.Margin = new System.Windows.Forms.Padding(12, 6, 0, 0);
            this.lblLookback.Name = "lblLookback";
            this.lblLookback.Size = new System.Drawing.Size(76, 13);
            this.lblLookback.TabIndex = 6;
            this.lblLookback.Text = "Lookback (d):";
            // 
            // numLookback
            // 
            this.numLookback.Location = new System.Drawing.Point(685, 11);
            this.numLookback.Maximum = new decimal(new int[] {
            120,
            0,
            0,
            0});
            this.numLookback.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numLookback.Name = "numLookback";
            this.numLookback.Size = new System.Drawing.Size(70, 20);
            this.numLookback.TabIndex = 7;
            this.numLookback.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // lblEquity
            // 
            this.lblEquity.AutoSize = true;
            this.lblEquity.Location = new System.Drawing.Point(770, 14);
            this.lblEquity.Margin = new System.Windows.Forms.Padding(12, 6, 0, 0);
            this.lblEquity.Name = "lblEquity";
            this.lblEquity.Size = new System.Drawing.Size(42, 13);
            this.lblEquity.TabIndex = 8;
            this.lblEquity.Text = "Equity:";
            // 
            // numEquity
            // 
            this.numEquity.Location = new System.Drawing.Point(815, 11);
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
            this.numEquity.Size = new System.Drawing.Size(90, 20);
            this.numEquity.TabIndex = 9;
            this.numEquity.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // mainSplit
            // 
            this.mainSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainSplit.Location = new System.Drawing.Point(3, 49);
            this.mainSplit.Name = "mainSplit";
            // 
            // mainSplit.Panel1
            // 
            this.mainSplit.Panel1.Controls.Add(this.leftPanel);
            // 
            // mainSplit.Panel2
            // 
            this.mainSplit.Panel2.Controls.Add(this.rightLayout);
            this.mainSplit.Size = new System.Drawing.Size(1194, 748);
            this.mainSplit.SplitterDistance = 320;
            this.mainSplit.TabIndex = 1;
            // 
            // leftPanel
            // 
            this.leftPanel.Controls.Add(this.topBar);
            this.leftPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftPanel.Location = new System.Drawing.Point(0, 0);
            this.leftPanel.Name = "leftPanel";
            this.leftPanel.Padding = new System.Windows.Forms.Padding(8);
            this.leftPanel.Size = new System.Drawing.Size(320, 748);
            this.leftPanel.TabIndex = 0;
            // 
            // topBar
            // 
            this.topBar.AutoScroll = true;
            this.topBar.Controls.Add(this.btnRefresh);
            this.topBar.Controls.Add(this.btnSave);
            this.topBar.Controls.Add(this.btnAdd);
            this.topBar.Controls.Add(this.lblProduct);
            this.topBar.Controls.Add(this.cmbFilterProduct);
            this.topBar.Controls.Add(this.lblStrategy);
            this.topBar.Controls.Add(this.cmbFilterStrategy);
            this.topBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topBar.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.topBar.Location = new System.Drawing.Point(8, 8);
            this.topBar.Name = "topBar";
            this.topBar.Size = new System.Drawing.Size(304, 732);
            this.topBar.TabIndex = 0;
            this.topBar.WrapContents = false;
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(3, 3);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(140, 26);
            this.btnRefresh.TabIndex = 0;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(3, 35);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(140, 26);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(3, 67);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(140, 26);
            this.btnAdd.TabIndex = 2;
            this.btnAdd.Text = "Add Trade";
            this.btnAdd.UseVisualStyleBackColor = true;
            // 
            // lblProduct
            // 
            this.lblProduct.AutoSize = true;
            this.lblProduct.Location = new System.Drawing.Point(3, 104);
            this.lblProduct.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
            this.lblProduct.Name = "lblProduct";
            this.lblProduct.Size = new System.Drawing.Size(68, 13);
            this.lblProduct.TabIndex = 3;
            this.lblProduct.Text = "Filter product";
            // 
            // cmbFilterProduct
            // 
            this.cmbFilterProduct.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFilterProduct.FormattingEnabled = true;
            this.cmbFilterProduct.Location = new System.Drawing.Point(3, 120);
            this.cmbFilterProduct.Name = "cmbFilterProduct";
            this.cmbFilterProduct.Size = new System.Drawing.Size(220, 21);
            this.cmbFilterProduct.TabIndex = 4;
            // 
            // lblStrategy
            // 
            this.lblStrategy.AutoSize = true;
            this.lblStrategy.Location = new System.Drawing.Point(3, 152);
            this.lblStrategy.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
            this.lblStrategy.Name = "lblStrategy";
            this.lblStrategy.Size = new System.Drawing.Size(66, 13);
            this.lblStrategy.TabIndex = 5;
            this.lblStrategy.Text = "Filter strategy";
            // 
            // cmbFilterStrategy
            // 
            this.cmbFilterStrategy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFilterStrategy.FormattingEnabled = true;
            this.cmbFilterStrategy.Location = new System.Drawing.Point(3, 168);
            this.cmbFilterStrategy.Name = "cmbFilterStrategy";
            this.cmbFilterStrategy.Size = new System.Drawing.Size(220, 21);
            this.cmbFilterStrategy.TabIndex = 6;
            // 
            // rightLayout
            // 
            this.rightLayout.ColumnCount = 1;
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Controls.Add(this.actionBar, 0, 0);
            this.rightLayout.Controls.Add(this.tabMain, 0, 1);
            this.rightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightLayout.Location = new System.Drawing.Point(0, 0);
            this.rightLayout.Name = "rightLayout";
            this.rightLayout.RowCount = 2;
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Size = new System.Drawing.Size(870, 748);
            this.rightLayout.TabIndex = 0;
            // 
            // actionBar
            // 
            this.actionBar.AutoSize = true;
            this.actionBar.Controls.Add(this.btnScan);
            this.actionBar.Controls.Add(this.btnPropose);
            this.actionBar.Controls.Add(this.btnProposeAll);
            this.actionBar.Controls.Add(this.btnExecute);
            this.actionBar.Controls.Add(this.lblPlannerStatus);
            this.actionBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionBar.Location = new System.Drawing.Point(3, 3);
            this.actionBar.Name = "actionBar";
            this.actionBar.Padding = new System.Windows.Forms.Padding(4);
            this.actionBar.Size = new System.Drawing.Size(864, 35);
            this.actionBar.TabIndex = 0;
            // 
            // btnScan
            // 
            this.btnScan.Location = new System.Drawing.Point(7, 7);
            this.btnScan.Name = "btnScan";
            this.btnScan.Size = new System.Drawing.Size(90, 23);
            this.btnScan.TabIndex = 0;
            this.btnScan.Text = "Scan";
            this.btnScan.UseVisualStyleBackColor = true;
            // 
            // btnPropose
            // 
            this.btnPropose.Location = new System.Drawing.Point(103, 7);
            this.btnPropose.Name = "btnPropose";
            this.btnPropose.Size = new System.Drawing.Size(90, 23);
            this.btnPropose.TabIndex = 1;
            this.btnPropose.Text = "Propose";
            this.btnPropose.UseVisualStyleBackColor = true;
            // 
            // btnProposeAll
            // 
            this.btnProposeAll.Location = new System.Drawing.Point(199, 7);
            this.btnProposeAll.Name = "btnProposeAll";
            this.btnProposeAll.Size = new System.Drawing.Size(90, 23);
            this.btnProposeAll.TabIndex = 2;
            this.btnProposeAll.Text = "Propose All";
            this.btnProposeAll.UseVisualStyleBackColor = true;
            // 
            // btnExecute
            // 
            this.btnExecute.Location = new System.Drawing.Point(295, 7);
            this.btnExecute.Name = "btnExecute";
            this.btnExecute.Size = new System.Drawing.Size(90, 23);
            this.btnExecute.TabIndex = 3;
            this.btnExecute.Text = "Execute";
            this.btnExecute.UseVisualStyleBackColor = true;
            // 
            // lblPlannerStatus
            // 
            this.lblPlannerStatus.AutoSize = true;
            this.lblPlannerStatus.ForeColor = System.Drawing.Color.DimGray;
            this.lblPlannerStatus.Location = new System.Drawing.Point(391, 12);
            this.lblPlannerStatus.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
            this.lblPlannerStatus.Name = "lblPlannerStatus";
            this.lblPlannerStatus.Size = new System.Drawing.Size(98, 13);
            this.lblPlannerStatus.TabIndex = 4;
            this.lblPlannerStatus.Text = "Status: Not started";
            // 
            // tabMain
            // 
            this.tabMain.Controls.Add(this.tabPlanned);
            this.tabMain.Controls.Add(this.tabPredictions);
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.Location = new System.Drawing.Point(3, 44);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(864, 701);
            this.tabMain.TabIndex = 1;
            // 
            // tabPlanned
            // 
            this.tabPlanned.Controls.Add(this.gridPlanned);
            this.tabPlanned.Location = new System.Drawing.Point(4, 22);
            this.tabPlanned.Name = "tabPlanned";
            this.tabPlanned.Padding = new System.Windows.Forms.Padding(3);
            this.tabPlanned.Size = new System.Drawing.Size(856, 675);
            this.tabPlanned.TabIndex = 0;
            this.tabPlanned.Text = "Planned Trades";
            this.tabPlanned.UseVisualStyleBackColor = true;
            // 
            // gridPlanned
            // 
            this.gridPlanned.AllowUserToAddRows = false;
            this.gridPlanned.AllowUserToDeleteRows = false;
            this.gridPlanned.AutoGenerateColumns = false;
            this.gridPlanned.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridPlanned.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridPlanned.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colEnabled,
            this.colExchange,
            this.colProduct,
            this.colStrategy,
            this.colSide,
            this.colQty,
            this.colPrice,
            this.colEstEdge,
            this.colNotes});
            this.gridPlanned.ContextMenuStrip = this.ctxMenu;
            this.gridPlanned.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridPlanned.Location = new System.Drawing.Point(3, 3);
            this.gridPlanned.MultiSelect = true;
            this.gridPlanned.Name = "gridPlanned";
            this.gridPlanned.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridPlanned.Size = new System.Drawing.Size(850, 669);
            this.gridPlanned.TabIndex = 0;
            // 
            // colEnabled
            // 
            this.colEnabled.DataPropertyName = "Enabled";
            this.colEnabled.HeaderText = "Enabled";
            this.colEnabled.Name = "colEnabled";
            // 
            // colExchange
            // 
            this.colExchange.DataPropertyName = "Exchange";
            this.colExchange.HeaderText = "Exchange";
            this.colExchange.Name = "colExchange";
            this.colExchange.ReadOnly = true;
            // 
            // colProduct
            // 
            this.colProduct.DataPropertyName = "ProductId";
            this.colProduct.HeaderText = "Product";
            this.colProduct.Name = "colProduct";
            this.colProduct.ReadOnly = true;
            // 
            // colStrategy
            // 
            this.colStrategy.DataPropertyName = "Strategy";
            this.colStrategy.HeaderText = "Strategy";
            this.colStrategy.Name = "colStrategy";
            this.colStrategy.ReadOnly = true;
            // 
            // colSide
            // 
            this.colSide.DataPropertyName = "Side";
            this.colSide.HeaderText = "Side";
            this.colSide.Name = "colSide";
            this.colSide.ReadOnly = true;
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
            // colEstEdge
            // 
            this.colEstEdge.DataPropertyName = "EstEdge";
            this.colEstEdge.HeaderText = "Est. Edge";
            this.colEstEdge.Name = "colEstEdge";
            this.colEstEdge.ReadOnly = true;
            // 
            // colNotes
            // 
            this.colNotes.DataPropertyName = "Notes";
            this.colNotes.HeaderText = "Notes";
            this.colNotes.Name = "colNotes";
            // 
            // ctxMenu
            // 
            this.ctxMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miEdit,
            this.miDelete});
            this.ctxMenu.Name = "ctxMenu";
            this.ctxMenu.Size = new System.Drawing.Size(108, 48);
            // 
            // miEdit
            // 
            this.miEdit.Name = "miEdit";
            this.miEdit.Size = new System.Drawing.Size(107, 22);
            this.miEdit.Text = "Edit";
            // 
            // miDelete
            // 
            this.miDelete.Name = "miDelete";
            this.miDelete.Size = new System.Drawing.Size(107, 22);
            this.miDelete.Text = "Delete";
            // 
            // tabPredictions
            // 
            this.tabPredictions.Controls.Add(this.gridPreds);
            this.tabPredictions.Location = new System.Drawing.Point(4, 22);
            this.tabPredictions.Name = "tabPredictions";
            this.tabPredictions.Padding = new System.Windows.Forms.Padding(3);
            this.tabPredictions.Size = new System.Drawing.Size(856, 675);
            this.tabPredictions.TabIndex = 1;
            this.tabPredictions.Text = "Predictions";
            this.tabPredictions.UseVisualStyleBackColor = true;
            // 
            // gridPreds
            // 
            this.gridPreds.AllowUserToAddRows = false;
            this.gridPreds.AllowUserToDeleteRows = false;
            this.gridPreds.AutoGenerateColumns = false;
            this.gridPreds.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridPreds.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridPreds.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colPredProduct,
            this.colPredTime,
            this.colPredHorizon,
            this.colPredDir,
            this.colPredProb,
            this.colPredExpRet,
            this.colPredExpVol,
            this.colPredKnown,
            this.colPredRDir,
            this.colPredRRet});
            this.gridPreds.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridPreds.Location = new System.Drawing.Point(3, 3);
            this.gridPreds.MultiSelect = true;
            this.gridPreds.Name = "gridPreds";
            this.gridPreds.ReadOnly = true;
            this.gridPreds.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridPreds.Size = new System.Drawing.Size(850, 669);
            this.gridPreds.TabIndex = 0;
            // 
            // colPredProduct
            // 
            this.colPredProduct.DataPropertyName = "ProductId";
            this.colPredProduct.HeaderText = "Product";
            this.colPredProduct.Name = "colPredProduct";
            this.colPredProduct.ReadOnly = true;
            // 
            // colPredTime
            // 
            this.colPredTime.DataPropertyName = "AtUtc";
            this.colPredTime.HeaderText = "Time";
            this.colPredTime.Name = "colPredTime";
            this.colPredTime.ReadOnly = true;
            // 
            // colPredHorizon
            // 
            this.colPredHorizon.DataPropertyName = "HorizonMinutes";
            this.colPredHorizon.HeaderText = "Horizon";
            this.colPredHorizon.Name = "colPredHorizon";
            this.colPredHorizon.ReadOnly = true;
            // 
            // colPredDir
            // 
            this.colPredDir.DataPropertyName = "Direction";
            this.colPredDir.HeaderText = "Dir";
            this.colPredDir.Name = "colPredDir";
            this.colPredDir.ReadOnly = true;
            // 
            // colPredProb
            // 
            this.colPredProb.DataPropertyName = "Probability";
            this.colPredProb.HeaderText = "Prob";
            this.colPredProb.Name = "colPredProb";
            this.colPredProb.ReadOnly = true;
            // 
            // colPredExpRet
            // 
            this.colPredExpRet.DataPropertyName = "ExpectedReturn";
            this.colPredExpRet.HeaderText = "ExpRet";
            this.colPredExpRet.Name = "colPredExpRet";
            this.colPredExpRet.ReadOnly = true;
            // 
            // colPredExpVol
            // 
            this.colPredExpVol.DataPropertyName = "ExpectedVol";
            this.colPredExpVol.HeaderText = "ExpVol";
            this.colPredExpVol.Name = "colPredExpVol";
            this.colPredExpVol.ReadOnly = true;
            // 
            // colPredKnown
            // 
            this.colPredKnown.DataPropertyName = "RealizedKnown";
            this.colPredKnown.HeaderText = "Known";
            this.colPredKnown.Name = "colPredKnown";
            this.colPredKnown.ReadOnly = true;
            // 
            // colPredRDir
            // 
            this.colPredRDir.DataPropertyName = "RealizedDirection";
            this.colPredRDir.HeaderText = "RDir";
            this.colPredRDir.Name = "colPredRDir";
            this.colPredRDir.ReadOnly = true;
            // 
            // colPredRRet
            // 
            this.colPredRRet.DataPropertyName = "RealizedReturn";
            this.colPredRRet.HeaderText = "RRet";
            this.colPredRRet.Name = "colPredRRet";
            this.colPredRRet.ReadOnly = true;
            // 
            // PlannerControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainLayout);
            this.Name = "PlannerControl";
            this.Size = new System.Drawing.Size(1200, 800);
            this.mainLayout.ResumeLayout(false);
            this.mainLayout.PerformLayout();
            this.headerBar.ResumeLayout(false);
            this.headerBar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numLookback)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numEquity)).EndInit();
            this.mainSplit.Panel1.ResumeLayout(false);
            this.mainSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.mainSplit)).EndInit();
            this.mainSplit.ResumeLayout(false);
            this.leftPanel.ResumeLayout(false);
            this.topBar.ResumeLayout(false);
            this.topBar.PerformLayout();
            this.rightLayout.ResumeLayout(false);
            this.rightLayout.PerformLayout();
            this.actionBar.ResumeLayout(false);
            this.tabMain.ResumeLayout(false);
            this.tabPlanned.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridPlanned)).EndInit();
            this.ctxMenu.ResumeLayout(false);
            this.tabPredictions.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridPreds)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.FlowLayoutPanel headerBar;
        private System.Windows.Forms.Label lblAccount;
        private System.Windows.Forms.ComboBox cmbAccount;
        private System.Windows.Forms.Label lblRunProduct;
        private System.Windows.Forms.ComboBox cmbRunProduct;
        private System.Windows.Forms.Label lblGran;
        private System.Windows.Forms.ComboBox cmbGran;
        private System.Windows.Forms.Label lblLookback;
        private System.Windows.Forms.NumericUpDown numLookback;
        private System.Windows.Forms.Label lblEquity;
        private System.Windows.Forms.NumericUpDown numEquity;
        private System.Windows.Forms.SplitContainer mainSplit;
        private System.Windows.Forms.Panel leftPanel;
        private System.Windows.Forms.FlowLayoutPanel topBar;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Label lblProduct;
        private System.Windows.Forms.ComboBox cmbFilterProduct;
        private System.Windows.Forms.Label lblStrategy;
        private System.Windows.Forms.ComboBox cmbFilterStrategy;
        private System.Windows.Forms.TableLayoutPanel rightLayout;
        private System.Windows.Forms.FlowLayoutPanel actionBar;
        private System.Windows.Forms.Button btnScan;
        private System.Windows.Forms.Button btnPropose;
        private System.Windows.Forms.Button btnProposeAll;
        private System.Windows.Forms.Button btnExecute;
        private System.Windows.Forms.Label lblPlannerStatus;
        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabPlanned;
        private System.Windows.Forms.DataGridView gridPlanned;
        private System.Windows.Forms.TabPage tabPredictions;
        private System.Windows.Forms.DataGridView gridPreds;
        private System.Windows.Forms.ContextMenuStrip ctxMenu;
        private System.Windows.Forms.ToolStripMenuItem miEdit;
        private System.Windows.Forms.ToolStripMenuItem miDelete;

        private System.Windows.Forms.DataGridViewCheckBoxColumn colEnabled;
        private System.Windows.Forms.DataGridViewTextBoxColumn colExchange;
        private System.Windows.Forms.DataGridViewTextBoxColumn colProduct;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStrategy;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSide;
        private System.Windows.Forms.DataGridViewTextBoxColumn colQty;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPrice;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEstEdge;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNotes;

        private System.Windows.Forms.DataGridViewTextBoxColumn colPredProduct;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPredTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPredHorizon;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPredDir;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPredProb;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPredExpRet;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPredExpVol;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPredKnown;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPredRDir;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPredRRet;
    }
}
