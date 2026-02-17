namespace CryptoDayTraderSuite.UI
{
    partial class AutoModeControl
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
            this.tableLayout = new System.Windows.Forms.TableLayoutPanel();
            this.headerPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.lblAccount = new System.Windows.Forms.Label();
            this.cmbAccount = new System.Windows.Forms.ComboBox();
            this.lblProfile = new System.Windows.Forms.Label();
            this.cmbProfile = new System.Windows.Forms.ComboBox();
            this.chkProfileEnabled = new System.Windows.Forms.CheckBox();
            this.lblProfileEvery = new System.Windows.Forms.Label();
            this.numProfileInterval = new System.Windows.Forms.NumericUpDown();
            this.btnProfileSave = new System.Windows.Forms.Button();
            this.btnProfileDelete = new System.Windows.Forms.Button();
            this.chkProfileAllPairs = new System.Windows.Forms.CheckBox();
            this.chkAutoRun = new System.Windows.Forms.CheckBox();
            this.lblAutoEvery = new System.Windows.Forms.Label();
            this.numAutoInterval = new System.Windows.Forms.NumericUpDown();
            this.chkLiveArm = new System.Windows.Forms.CheckBox();
            this.btnKillSwitch = new System.Windows.Forms.Button();
            this.lblAutoStatus = new System.Windows.Forms.Label();
            this.mainSplit = new System.Windows.Forms.SplitContainer();
            this.topPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.lblProduct = new System.Windows.Forms.Label();
            this.cmbProduct = new System.Windows.Forms.ComboBox();
            this.lblGran = new System.Windows.Forms.Label();
            this.cmbGran = new System.Windows.Forms.ComboBox();
            this.lblLookback = new System.Windows.Forms.Label();
            this.numLookback = new System.Windows.Forms.NumericUpDown();
            this.lblEquity = new System.Windows.Forms.Label();
            this.numEquity = new System.Windows.Forms.NumericUpDown();
            this.chkAutoPropose = new System.Windows.Forms.CheckBox();
            this.lblMaxTradesPerCycle = new System.Windows.Forms.Label();
            this.numMaxTradesPerCycle = new System.Windows.Forms.NumericUpDown();
            this.lblCooldownMinutes = new System.Windows.Forms.Label();
            this.numCooldownMinutes = new System.Windows.Forms.NumericUpDown();
            this.lblDailyRiskStopPct = new System.Windows.Forms.Label();
            this.numDailyRiskStopPct = new System.Windows.Forms.NumericUpDown();
            this.rightLayout = new System.Windows.Forms.TableLayoutPanel();
            this.actionPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnScan = new System.Windows.Forms.Button();
            this.btnPropose = new System.Windows.Forms.Button();
            this.btnExecute = new System.Windows.Forms.Button();
            this.grid = new System.Windows.Forms.DataGridView();
            this.colStrategy = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSymbol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colGran = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colExpectancy = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colWinRate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAvgWin = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAvgLoss = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSharpe = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSamples = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.footerPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblProfileSummary = new System.Windows.Forms.Label();
            this.lblTelemetrySummary = new System.Windows.Forms.Label();
            this.tableLayout.SuspendLayout();
            this.headerPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numProfileInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAutoInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.mainSplit)).BeginInit();
            this.mainSplit.Panel1.SuspendLayout();
            this.mainSplit.Panel2.SuspendLayout();
            this.mainSplit.SuspendLayout();
            this.topPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numLookback)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numEquity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxTradesPerCycle)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCooldownMinutes)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDailyRiskStopPct)).BeginInit();
            this.rightLayout.SuspendLayout();
            this.actionPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.footerPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayout
            // 
            this.tableLayout.ColumnCount = 1;
            this.tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayout.Controls.Add(this.headerPanel, 0, 0);
            this.tableLayout.Controls.Add(this.mainSplit, 0, 1);
            this.tableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayout.Location = new System.Drawing.Point(0, 0);
            this.tableLayout.Name = "tableLayout";
            this.tableLayout.RowCount = 2;
            this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayout.Size = new System.Drawing.Size(1200, 800);
            this.tableLayout.TabIndex = 0;
            // 
            // headerPanel
            // 
            this.headerPanel.AutoSize = true;
            this.headerPanel.Controls.Add(this.lblAccount);
            this.headerPanel.Controls.Add(this.cmbAccount);
            this.headerPanel.Controls.Add(this.lblProfile);
            this.headerPanel.Controls.Add(this.cmbProfile);
            this.headerPanel.Controls.Add(this.chkProfileEnabled);
            this.headerPanel.Controls.Add(this.lblProfileEvery);
            this.headerPanel.Controls.Add(this.numProfileInterval);
            this.headerPanel.Controls.Add(this.btnProfileSave);
            this.headerPanel.Controls.Add(this.btnProfileDelete);
            this.headerPanel.Controls.Add(this.chkProfileAllPairs);
            this.headerPanel.Controls.Add(this.chkAutoRun);
            this.headerPanel.Controls.Add(this.lblAutoEvery);
            this.headerPanel.Controls.Add(this.numAutoInterval);
            this.headerPanel.Controls.Add(this.chkLiveArm);
            this.headerPanel.Controls.Add(this.btnKillSwitch);
            this.headerPanel.Controls.Add(this.lblAutoStatus);
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headerPanel.Location = new System.Drawing.Point(3, 3);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Padding = new System.Windows.Forms.Padding(8);
            this.headerPanel.Size = new System.Drawing.Size(1194, 44);
            this.headerPanel.TabIndex = 0;
            this.headerPanel.WrapContents = true;
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
            // lblProfile
            // 
            this.lblProfile.AutoSize = true;
            this.lblProfile.Location = new System.Drawing.Point(259, 14);
            this.lblProfile.Margin = new System.Windows.Forms.Padding(12, 6, 0, 0);
            this.lblProfile.Name = "lblProfile";
            this.lblProfile.Size = new System.Drawing.Size(39, 13);
            this.lblProfile.TabIndex = 2;
            this.lblProfile.Text = "Profile:";
            // 
            // cmbProfile
            // 
            this.cmbProfile.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbProfile.FormattingEnabled = true;
            this.cmbProfile.Location = new System.Drawing.Point(301, 11);
            this.cmbProfile.Name = "cmbProfile";
            this.cmbProfile.Size = new System.Drawing.Size(170, 21);
            this.cmbProfile.TabIndex = 3;
            // 
            // chkProfileEnabled
            // 
            this.chkProfileEnabled.AutoSize = true;
            this.chkProfileEnabled.Checked = true;
            this.chkProfileEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkProfileEnabled.Location = new System.Drawing.Point(477, 14);
            this.chkProfileEnabled.Name = "chkProfileEnabled";
            this.chkProfileEnabled.Size = new System.Drawing.Size(65, 17);
            this.chkProfileEnabled.TabIndex = 4;
            this.chkProfileEnabled.Text = "Enabled";
            this.chkProfileEnabled.UseVisualStyleBackColor = true;
            // 
            // lblProfileEvery
            // 
            this.lblProfileEvery.AutoSize = true;
            this.lblProfileEvery.Location = new System.Drawing.Point(548, 14);
            this.lblProfileEvery.Margin = new System.Windows.Forms.Padding(3, 6, 0, 0);
            this.lblProfileEvery.Name = "lblProfileEvery";
            this.lblProfileEvery.Size = new System.Drawing.Size(68, 13);
            this.lblProfileEvery.TabIndex = 5;
            this.lblProfileEvery.Text = "Profile Every:";
            // 
            // numProfileInterval
            // 
            this.numProfileInterval.Location = new System.Drawing.Point(619, 11);
            this.numProfileInterval.Maximum = new decimal(new int[] {
            240,
            0,
            0,
            0});
            this.numProfileInterval.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numProfileInterval.Name = "numProfileInterval";
            this.numProfileInterval.Size = new System.Drawing.Size(50, 20);
            this.numProfileInterval.TabIndex = 6;
            this.numProfileInterval.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // btnProfileSave
            // 
            this.btnProfileSave.Location = new System.Drawing.Point(675, 11);
            this.btnProfileSave.Name = "btnProfileSave";
            this.btnProfileSave.Size = new System.Drawing.Size(86, 23);
            this.btnProfileSave.TabIndex = 7;
            this.btnProfileSave.Text = "Save Profile";
            this.btnProfileSave.UseVisualStyleBackColor = true;
            // 
            // btnProfileDelete
            // 
            this.btnProfileDelete.Location = new System.Drawing.Point(767, 11);
            this.btnProfileDelete.Name = "btnProfileDelete";
            this.btnProfileDelete.Size = new System.Drawing.Size(88, 23);
            this.btnProfileDelete.TabIndex = 8;
            this.btnProfileDelete.Text = "Delete Profile";
            this.btnProfileDelete.UseVisualStyleBackColor = true;
            // 
            // chkProfileAllPairs
            // 
            this.chkProfileAllPairs.AutoSize = true;
            this.chkProfileAllPairs.Location = new System.Drawing.Point(861, 14);
            this.chkProfileAllPairs.Name = "chkProfileAllPairs";
            this.chkProfileAllPairs.Size = new System.Drawing.Size(66, 17);
            this.chkProfileAllPairs.TabIndex = 9;
            this.chkProfileAllPairs.Text = "All Pairs";
            this.chkProfileAllPairs.UseVisualStyleBackColor = true;
            // 
            // chkAutoRun
            // 
            this.chkAutoRun.AutoSize = true;
            this.chkAutoRun.Location = new System.Drawing.Point(933, 14);
            this.chkAutoRun.Name = "chkAutoRun";
            this.chkAutoRun.Size = new System.Drawing.Size(71, 17);
            this.chkAutoRun.TabIndex = 10;
            this.chkAutoRun.Text = "Auto Run";
            this.chkAutoRun.UseVisualStyleBackColor = true;
            // 
            // lblAutoEvery
            // 
            this.lblAutoEvery.AutoSize = true;
            this.lblAutoEvery.Location = new System.Drawing.Point(1010, 14);
            this.lblAutoEvery.Margin = new System.Windows.Forms.Padding(3, 6, 0, 0);
            this.lblAutoEvery.Name = "lblAutoEvery";
            this.lblAutoEvery.Size = new System.Drawing.Size(38, 13);
            this.lblAutoEvery.TabIndex = 11;
            this.lblAutoEvery.Text = "Every:";
            // 
            // numAutoInterval
            // 
            this.numAutoInterval.Location = new System.Drawing.Point(1051, 11);
            this.numAutoInterval.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.numAutoInterval.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numAutoInterval.Name = "numAutoInterval";
            this.numAutoInterval.Size = new System.Drawing.Size(50, 20);
            this.numAutoInterval.TabIndex = 12;
            this.numAutoInterval.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // chkLiveArm
            // 
            this.chkLiveArm.AutoSize = true;
            this.chkLiveArm.Location = new System.Drawing.Point(1107, 14);
            this.chkLiveArm.Name = "chkLiveArm";
            this.chkLiveArm.Size = new System.Drawing.Size(68, 17);
            this.chkLiveArm.TabIndex = 13;
            this.chkLiveArm.Text = "Live Arm";
            this.chkLiveArm.UseVisualStyleBackColor = true;
            // 
            // btnKillSwitch
            // 
            this.btnKillSwitch.BackColor = System.Drawing.Color.MistyRose;
            this.btnKillSwitch.Location = new System.Drawing.Point(1181, 11);
            this.btnKillSwitch.Name = "btnKillSwitch";
            this.btnKillSwitch.Size = new System.Drawing.Size(78, 23);
            this.btnKillSwitch.TabIndex = 14;
            this.btnKillSwitch.Text = "Kill Switch";
            this.btnKillSwitch.UseVisualStyleBackColor = false;
            // 
            // lblAutoStatus
            // 
            this.lblAutoStatus.AutoSize = true;
            this.lblAutoStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAutoStatus.Location = new System.Drawing.Point(1265, 14);
            this.lblAutoStatus.Margin = new System.Windows.Forms.Padding(3, 6, 0, 0);
            this.lblAutoStatus.Name = "lblAutoStatus";
            this.lblAutoStatus.Size = new System.Drawing.Size(72, 13);
            this.lblAutoStatus.TabIndex = 15;
            this.lblAutoStatus.Text = "Status: OFF";
            // 
            // mainSplit
            // 
            this.mainSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainSplit.Location = new System.Drawing.Point(3, 53);
            this.mainSplit.Name = "mainSplit";
            // 
            // mainSplit.Panel1
            // 
            this.mainSplit.Panel1.Controls.Add(this.topPanel);
            // 
            // mainSplit.Panel2
            // 
            this.mainSplit.Panel2.Controls.Add(this.rightLayout);
            this.mainSplit.Size = new System.Drawing.Size(1194, 744);
            this.mainSplit.SplitterDistance = 360;
            this.mainSplit.TabIndex = 1;
            // 
            // topPanel
            // 
            this.topPanel.AutoScroll = true;
            this.topPanel.Controls.Add(this.lblProduct);
            this.topPanel.Controls.Add(this.cmbProduct);
            this.topPanel.Controls.Add(this.lblGran);
            this.topPanel.Controls.Add(this.cmbGran);
            this.topPanel.Controls.Add(this.lblLookback);
            this.topPanel.Controls.Add(this.numLookback);
            this.topPanel.Controls.Add(this.lblEquity);
            this.topPanel.Controls.Add(this.numEquity);
            this.topPanel.Controls.Add(this.chkAutoPropose);
            this.topPanel.Controls.Add(this.lblMaxTradesPerCycle);
            this.topPanel.Controls.Add(this.numMaxTradesPerCycle);
            this.topPanel.Controls.Add(this.lblCooldownMinutes);
            this.topPanel.Controls.Add(this.numCooldownMinutes);
            this.topPanel.Controls.Add(this.lblDailyRiskStopPct);
            this.topPanel.Controls.Add(this.numDailyRiskStopPct);
            this.topPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.topPanel.Location = new System.Drawing.Point(0, 0);
            this.topPanel.Name = "topPanel";
            this.topPanel.Padding = new System.Windows.Forms.Padding(10);
            this.topPanel.Size = new System.Drawing.Size(360, 744);
            this.topPanel.TabIndex = 0;
            this.topPanel.WrapContents = false;
            // 
            // lblProduct
            // 
            this.lblProduct.AutoSize = true;
            this.lblProduct.Location = new System.Drawing.Point(13, 13);
            this.lblProduct.Name = "lblProduct";
            this.lblProduct.Size = new System.Drawing.Size(47, 13);
            this.lblProduct.TabIndex = 0;
            this.lblProduct.Text = "Product:";
            // 
            // cmbProduct
            // 
            this.cmbProduct.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbProduct.FormattingEnabled = true;
            this.cmbProduct.Location = new System.Drawing.Point(13, 29);
            this.cmbProduct.Name = "cmbProduct";
            this.cmbProduct.Size = new System.Drawing.Size(320, 21);
            this.cmbProduct.TabIndex = 1;
            // 
            // lblGran
            // 
            this.lblGran.AutoSize = true;
            this.lblGran.Location = new System.Drawing.Point(13, 58);
            this.lblGran.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
            this.lblGran.Name = "lblGran";
            this.lblGran.Size = new System.Drawing.Size(57, 13);
            this.lblGran.TabIndex = 2;
            this.lblGran.Text = "Gran (min):";
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
            this.cmbGran.Location = new System.Drawing.Point(13, 74);
            this.cmbGran.Name = "cmbGran";
            this.cmbGran.Size = new System.Drawing.Size(120, 21);
            this.cmbGran.TabIndex = 3;
            // 
            // lblLookback
            // 
            this.lblLookback.AutoSize = true;
            this.lblLookback.Location = new System.Drawing.Point(13, 103);
            this.lblLookback.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
            this.lblLookback.Name = "lblLookback";
            this.lblLookback.Size = new System.Drawing.Size(87, 13);
            this.lblLookback.TabIndex = 4;
            this.lblLookback.Text = "Lookback (days):";
            // 
            // numLookback
            // 
            this.numLookback.Location = new System.Drawing.Point(13, 119);
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
            this.numLookback.Size = new System.Drawing.Size(120, 20);
            this.numLookback.TabIndex = 5;
            this.numLookback.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // lblEquity
            // 
            this.lblEquity.AutoSize = true;
            this.lblEquity.Location = new System.Drawing.Point(13, 147);
            this.lblEquity.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
            this.lblEquity.Name = "lblEquity";
            this.lblEquity.Size = new System.Drawing.Size(53, 13);
            this.lblEquity.TabIndex = 6;
            this.lblEquity.Text = "Equity ($):";
            // 
            // numEquity
            // 
            this.numEquity.Location = new System.Drawing.Point(13, 163);
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
            this.numEquity.TabIndex = 7;
            this.numEquity.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // chkAutoPropose
            // 
            this.chkAutoPropose.AutoSize = true;
            this.chkAutoPropose.Checked = true;
            this.chkAutoPropose.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoPropose.Location = new System.Drawing.Point(13, 191);
            this.chkAutoPropose.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
            this.chkAutoPropose.Name = "chkAutoPropose";
            this.chkAutoPropose.Size = new System.Drawing.Size(97, 17);
            this.chkAutoPropose.TabIndex = 8;
            this.chkAutoPropose.Text = "Auto Propose";
            this.chkAutoPropose.UseVisualStyleBackColor = true;
            // 
            // lblMaxTradesPerCycle
            // 
            this.lblMaxTradesPerCycle.AutoSize = true;
            this.lblMaxTradesPerCycle.Location = new System.Drawing.Point(13, 216);
            this.lblMaxTradesPerCycle.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
            this.lblMaxTradesPerCycle.Name = "lblMaxTradesPerCycle";
            this.lblMaxTradesPerCycle.Size = new System.Drawing.Size(90, 13);
            this.lblMaxTradesPerCycle.TabIndex = 9;
            this.lblMaxTradesPerCycle.Text = "Max trades/cycle:";
            // 
            // numMaxTradesPerCycle
            // 
            this.numMaxTradesPerCycle.Location = new System.Drawing.Point(13, 232);
            this.numMaxTradesPerCycle.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.numMaxTradesPerCycle.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numMaxTradesPerCycle.Name = "numMaxTradesPerCycle";
            this.numMaxTradesPerCycle.Size = new System.Drawing.Size(120, 20);
            this.numMaxTradesPerCycle.TabIndex = 10;
            this.numMaxTradesPerCycle.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // lblCooldownMinutes
            // 
            this.lblCooldownMinutes.AutoSize = true;
            this.lblCooldownMinutes.Location = new System.Drawing.Point(13, 260);
            this.lblCooldownMinutes.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
            this.lblCooldownMinutes.Name = "lblCooldownMinutes";
            this.lblCooldownMinutes.Size = new System.Drawing.Size(88, 13);
            this.lblCooldownMinutes.TabIndex = 11;
            this.lblCooldownMinutes.Text = "Cooldown (min):";
            // 
            // numCooldownMinutes
            // 
            this.numCooldownMinutes.Location = new System.Drawing.Point(13, 276);
            this.numCooldownMinutes.Maximum = new decimal(new int[] {
            240,
            0,
            0,
            0});
            this.numCooldownMinutes.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numCooldownMinutes.Name = "numCooldownMinutes";
            this.numCooldownMinutes.Size = new System.Drawing.Size(120, 20);
            this.numCooldownMinutes.TabIndex = 12;
            this.numCooldownMinutes.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // lblDailyRiskStopPct
            // 
            this.lblDailyRiskStopPct.AutoSize = true;
            this.lblDailyRiskStopPct.Location = new System.Drawing.Point(13, 304);
            this.lblDailyRiskStopPct.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
            this.lblDailyRiskStopPct.Name = "lblDailyRiskStopPct";
            this.lblDailyRiskStopPct.Size = new System.Drawing.Size(68, 13);
            this.lblDailyRiskStopPct.TabIndex = 13;
            this.lblDailyRiskStopPct.Text = "Daily Risk %:";
            // 
            // numDailyRiskStopPct
            // 
            this.numDailyRiskStopPct.DecimalPlaces = 1;
            this.numDailyRiskStopPct.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.numDailyRiskStopPct.Location = new System.Drawing.Point(13, 320);
            this.numDailyRiskStopPct.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numDailyRiskStopPct.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numDailyRiskStopPct.Name = "numDailyRiskStopPct";
            this.numDailyRiskStopPct.Size = new System.Drawing.Size(120, 20);
            this.numDailyRiskStopPct.TabIndex = 14;
            this.numDailyRiskStopPct.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // rightLayout
            // 
            this.rightLayout.ColumnCount = 1;
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Controls.Add(this.actionPanel, 0, 0);
            this.rightLayout.Controls.Add(this.grid, 0, 1);
            this.rightLayout.Controls.Add(this.footerPanel, 0, 2);
            this.rightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightLayout.Location = new System.Drawing.Point(0, 0);
            this.rightLayout.Name = "rightLayout";
            this.rightLayout.RowCount = 3;
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.rightLayout.Size = new System.Drawing.Size(830, 744);
            this.rightLayout.TabIndex = 0;
            // 
            // actionPanel
            // 
            this.actionPanel.AutoSize = true;
            this.actionPanel.Controls.Add(this.btnScan);
            this.actionPanel.Controls.Add(this.btnPropose);
            this.actionPanel.Controls.Add(this.btnExecute);
            this.actionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionPanel.Location = new System.Drawing.Point(3, 3);
            this.actionPanel.Name = "actionPanel";
            this.actionPanel.Padding = new System.Windows.Forms.Padding(4);
            this.actionPanel.Size = new System.Drawing.Size(824, 35);
            this.actionPanel.TabIndex = 0;
            // 
            // btnScan
            // 
            this.btnScan.Location = new System.Drawing.Point(7, 7);
            this.btnScan.Name = "btnScan";
            this.btnScan.Size = new System.Drawing.Size(75, 23);
            this.btnScan.TabIndex = 0;
            this.btnScan.Text = "Scan";
            this.btnScan.UseVisualStyleBackColor = true;
            // 
            // btnPropose
            // 
            this.btnPropose.Location = new System.Drawing.Point(88, 7);
            this.btnPropose.Name = "btnPropose";
            this.btnPropose.Size = new System.Drawing.Size(75, 23);
            this.btnPropose.TabIndex = 1;
            this.btnPropose.Text = "Propose";
            this.btnPropose.UseVisualStyleBackColor = true;
            // 
            // btnExecute
            // 
            this.btnExecute.Location = new System.Drawing.Point(169, 7);
            this.btnExecute.Name = "btnExecute";
            this.btnExecute.Size = new System.Drawing.Size(75, 23);
            this.btnExecute.TabIndex = 2;
            this.btnExecute.Text = "Execute";
            this.btnExecute.UseVisualStyleBackColor = true;
            // 
            // grid
            // 
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colStrategy,
            this.colSymbol,
            this.colGran,
            this.colExpectancy,
            this.colWinRate,
            this.colAvgWin,
            this.colAvgLoss,
            this.colSharpe,
            this.colSamples});
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.Location = new System.Drawing.Point(3, 44);
            this.grid.MultiSelect = true;
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(824, 647);
            this.grid.TabIndex = 1;
            // 
            // colStrategy
            // 
            this.colStrategy.DataPropertyName = "Strategy";
            this.colStrategy.FillWeight = 180F;
            this.colStrategy.HeaderText = "Strategy";
            this.colStrategy.Name = "colStrategy";
            this.colStrategy.ReadOnly = true;
            // 
            // colSymbol
            // 
            this.colSymbol.DataPropertyName = "Symbol";
            this.colSymbol.HeaderText = "Symbol";
            this.colSymbol.Name = "colSymbol";
            this.colSymbol.ReadOnly = true;
            // 
            // colGran
            // 
            this.colGran.DataPropertyName = "GranMinutes";
            this.colGran.HeaderText = "Granularity";
            this.colGran.Name = "colGran";
            this.colGran.ReadOnly = true;
            // 
            // colExpectancy
            // 
            this.colExpectancy.DataPropertyName = "Expectancy";
            this.colExpectancy.HeaderText = "Expectancy";
            this.colExpectancy.Name = "colExpectancy";
            this.colExpectancy.ReadOnly = true;
            // 
            // colWinRate
            // 
            this.colWinRate.DataPropertyName = "WinRate";
            this.colWinRate.HeaderText = "Win%";
            this.colWinRate.Name = "colWinRate";
            this.colWinRate.ReadOnly = true;
            // 
            // colAvgWin
            // 
            this.colAvgWin.DataPropertyName = "AvgWin";
            this.colAvgWin.HeaderText = "AvgWin";
            this.colAvgWin.Name = "colAvgWin";
            this.colAvgWin.ReadOnly = true;
            // 
            // colAvgLoss
            // 
            this.colAvgLoss.DataPropertyName = "AvgLoss";
            this.colAvgLoss.HeaderText = "AvgLoss";
            this.colAvgLoss.Name = "colAvgLoss";
            this.colAvgLoss.ReadOnly = true;
            // 
            // colSharpe
            // 
            this.colSharpe.DataPropertyName = "SharpeApprox";
            this.colSharpe.HeaderText = "Sharpe";
            this.colSharpe.Name = "colSharpe";
            this.colSharpe.ReadOnly = true;
            // 
            // colSamples
            // 
            this.colSamples.DataPropertyName = "Samples";
            this.colSamples.HeaderText = "Samples";
            this.colSamples.Name = "colSamples";
            this.colSamples.ReadOnly = true;
            // 
            // footerPanel
            // 
            this.footerPanel.AutoSize = true;
            this.footerPanel.ColumnCount = 1;
            this.footerPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.footerPanel.Controls.Add(this.lblProfileSummary, 0, 0);
            this.footerPanel.Controls.Add(this.lblTelemetrySummary, 0, 1);
            this.footerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.footerPanel.Location = new System.Drawing.Point(3, 697);
            this.footerPanel.Name = "footerPanel";
            this.footerPanel.RowCount = 2;
            this.footerPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.footerPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.footerPanel.Size = new System.Drawing.Size(824, 44);
            this.footerPanel.TabIndex = 2;
            // 
            // lblProfileSummary
            // 
            this.lblProfileSummary.AutoSize = true;
            this.lblProfileSummary.Location = new System.Drawing.Point(3, 0);
            this.lblProfileSummary.Name = "lblProfileSummary";
            this.lblProfileSummary.Size = new System.Drawing.Size(117, 13);
            this.lblProfileSummary.TabIndex = 0;
            this.lblProfileSummary.Text = "Profile: (not selected)";
            // 
            // lblTelemetrySummary
            // 
            this.lblTelemetrySummary.AutoSize = true;
            this.lblTelemetrySummary.ForeColor = System.Drawing.Color.DimGray;
            this.lblTelemetrySummary.Location = new System.Drawing.Point(3, 13);
            this.lblTelemetrySummary.Name = "lblTelemetrySummary";
            this.lblTelemetrySummary.Size = new System.Drawing.Size(142, 13);
            this.lblTelemetrySummary.TabIndex = 1;
            this.lblTelemetrySummary.Text = "Telemetry: no cycle reports";
            // 
            // AutoModeControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayout);
            this.Name = "AutoModeControl";
            this.Size = new System.Drawing.Size(1200, 800);
            this.tableLayout.ResumeLayout(false);
            this.tableLayout.PerformLayout();
            this.headerPanel.ResumeLayout(false);
            this.headerPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numProfileInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAutoInterval)).EndInit();
            this.mainSplit.Panel1.ResumeLayout(false);
            this.mainSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.mainSplit)).EndInit();
            this.mainSplit.ResumeLayout(false);
            this.topPanel.ResumeLayout(false);
            this.topPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numLookback)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numEquity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxTradesPerCycle)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCooldownMinutes)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDailyRiskStopPct)).EndInit();
            this.rightLayout.ResumeLayout(false);
            this.rightLayout.PerformLayout();
            this.actionPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.footerPanel.ResumeLayout(false);
            this.footerPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayout;
        private System.Windows.Forms.FlowLayoutPanel headerPanel;
        private System.Windows.Forms.Label lblAccount;
        private System.Windows.Forms.ComboBox cmbAccount;
        private System.Windows.Forms.Label lblProfile;
        private System.Windows.Forms.ComboBox cmbProfile;
        private System.Windows.Forms.CheckBox chkProfileEnabled;
        private System.Windows.Forms.Label lblProfileEvery;
        private System.Windows.Forms.NumericUpDown numProfileInterval;
        private System.Windows.Forms.Button btnProfileSave;
        private System.Windows.Forms.Button btnProfileDelete;
        private System.Windows.Forms.CheckBox chkProfileAllPairs;
        private System.Windows.Forms.CheckBox chkAutoRun;
        private System.Windows.Forms.Label lblAutoEvery;
        private System.Windows.Forms.NumericUpDown numAutoInterval;
        private System.Windows.Forms.CheckBox chkLiveArm;
        private System.Windows.Forms.Button btnKillSwitch;
        private System.Windows.Forms.Label lblAutoStatus;
        private System.Windows.Forms.SplitContainer mainSplit;
        private System.Windows.Forms.FlowLayoutPanel topPanel;
        private System.Windows.Forms.Label lblProduct;
        private System.Windows.Forms.ComboBox cmbProduct;
        private System.Windows.Forms.Label lblGran;
        private System.Windows.Forms.ComboBox cmbGran;
        private System.Windows.Forms.Label lblLookback;
        private System.Windows.Forms.NumericUpDown numLookback;
        private System.Windows.Forms.Label lblEquity;
        private System.Windows.Forms.NumericUpDown numEquity;
        private System.Windows.Forms.CheckBox chkAutoPropose;
        private System.Windows.Forms.Label lblMaxTradesPerCycle;
        private System.Windows.Forms.NumericUpDown numMaxTradesPerCycle;
        private System.Windows.Forms.Label lblCooldownMinutes;
        private System.Windows.Forms.NumericUpDown numCooldownMinutes;
        private System.Windows.Forms.Label lblDailyRiskStopPct;
        private System.Windows.Forms.NumericUpDown numDailyRiskStopPct;
        private System.Windows.Forms.TableLayoutPanel rightLayout;
        private System.Windows.Forms.FlowLayoutPanel actionPanel;
        private System.Windows.Forms.Button btnScan;
        private System.Windows.Forms.Button btnPropose;
        private System.Windows.Forms.Button btnExecute;
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStrategy;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSymbol;
        private System.Windows.Forms.DataGridViewTextBoxColumn colGran;
        private System.Windows.Forms.DataGridViewTextBoxColumn colExpectancy;
        private System.Windows.Forms.DataGridViewTextBoxColumn colWinRate;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAvgWin;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAvgLoss;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSharpe;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSamples;
        private System.Windows.Forms.TableLayoutPanel footerPanel;
        private System.Windows.Forms.Label lblProfileSummary;
        private System.Windows.Forms.Label lblTelemetrySummary;
    }
}
