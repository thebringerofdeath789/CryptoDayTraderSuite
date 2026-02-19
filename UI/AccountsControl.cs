using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.UI
{
    public partial class AccountsControl : UserControl
    {
        private const int SnapshotFreshnessHours = 24;
        private BindingSource _bs = new BindingSource();
        private IAccountService _service;
        private IKeyService _keyService;
        private IHistoryService _historyService;
        private CoinbaseReadOnlyImportService _coinbaseReadOnlyImportService;
        private IExchangeProvider _exchangeProvider;
        private AccountBuyingPowerService _accountBuyingPowerService;
        private BindingSource _holdingsBinding = new BindingSource();

        private bool _insightsOnlyMode;
        private bool _wiringInitialized;
        private ComboBox _cmbInsightsAccount;
        private Label _lblInsightsAccount;
        private TabControl _insightsTabs;
        private TextBox _txtInsightsOverview;
        private DataGridView _gridTradeHistory;
        private DataGridView _gridStrategyStats;
        private DataGridView _gridBestTrades;
        private Label _lblProfitability;
        private Label _lblWinLoss;

        private sealed class HoldingViewRow
        {
            public string Currency { get; set; }
            public decimal Amount { get; set; }
        }

        private sealed class TradeHistoryRow
        {
            public DateTime AtUtc { get; set; }
            public string Exchange { get; set; }
            public string ProductId { get; set; }
            public string Strategy { get; set; }
            public string Side { get; set; }
            public decimal Quantity { get; set; }
            public decimal? FillPrice { get; set; }
            public decimal? PnL { get; set; }
        }

        private sealed class StrategyStatsRow
        {
            public string Strategy { get; set; }
            public int Trades { get; set; }
            public int Wins { get; set; }
            public int Losses { get; set; }
            public decimal WinRatePct { get; set; }
            public decimal NetPnl { get; set; }
            public decimal AvgPnl { get; set; }
        }

        public AccountsControl()
        {
            InitializeComponent();
            ConfigureGrid();
            WireEvents();
        }

        public void Initialize(IAccountService service, IKeyService keyService = null, IHistoryService historyService = null)
        {
            _service = service;
            _keyService = keyService;
            _historyService = historyService;
            _coinbaseReadOnlyImportService = (_service != null && _keyService != null)
                ? new CoinbaseReadOnlyImportService(_keyService, _service, _historyService)
                : null;
            _exchangeProvider = _keyService != null ? new ExchangeProvider(_keyService) : null;
            _accountBuyingPowerService = _keyService != null ? new AccountBuyingPowerService(_keyService) : null;

            if (btnImportCoinbase != null)
            {
                btnImportCoinbase.Enabled = _coinbaseReadOnlyImportService != null;
            }
            LoadData();
        }

        public void SetInsightsOnlyMode(bool insightsOnly)
        {
            _insightsOnlyMode = insightsOnly;

            if (btnAdd != null) btnAdd.Visible = !insightsOnly;
            if (btnEdit != null) btnEdit.Visible = !insightsOnly;
            if (btnDelete != null) btnDelete.Visible = !insightsOnly;
            if (btnSave != null) btnSave.Visible = !insightsOnly;
            if (btnRefresh != null) btnRefresh.Visible = !insightsOnly;

            if (insightsOnly)
            {
                EnsureInsightsOnlyLayout();
                _ = RefreshSelectedAccountInsightsAsync();
            }
        }

        private void ConfigureGrid()
        {
            gridAccounts.AutoGenerateColumns = false;

            colLabel.DataPropertyName = "Label";
            colService.DataPropertyName = "Service";
            colMode.DataPropertyName = "Mode";
            colRisk.DataPropertyName = "RiskPerTradePct";
            colMaxOpen.DataPropertyName = "MaxConcurrentTrades";
            colKeyId.DataPropertyName = "KeyEntryId";
            colEnabled.DataPropertyName = "Enabled";

            gridAccounts.ReadOnly = false;
            foreach (DataGridViewColumn col in gridAccounts.Columns)
            {
                col.ReadOnly = true;
            }
            colEnabled.ReadOnly = false;

            gridAccounts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            colLabel.FillWeight = 180;
            colService.FillWeight = 95;
            colMode.FillWeight = 75;
            colRisk.FillWeight = 70;
            colMaxOpen.FillWeight = 85;
            colKeyId.FillWeight = 200;
            colEnabled.FillWeight = 60;

            gridAccounts.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            gridAccounts.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            gridAccounts.RowTemplate.Height = 26;

            if (gridHoldings != null)
            {
                gridHoldings.AutoGenerateColumns = false;
                colHoldingCurrency.DataPropertyName = "Currency";
                colHoldingAmount.DataPropertyName = "Amount";
                gridHoldings.ReadOnly = true;
                gridHoldings.RowHeadersVisible = false;
                gridHoldings.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                gridHoldings.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                colHoldingCurrency.FillWeight = 40;
                colHoldingAmount.FillWeight = 60;
                gridHoldings.DataSource = _holdingsBinding;
            }
        }

        private void WireEvents()
        {
            if (_wiringInitialized) return;
            _wiringInitialized = true;

            btnAdd.Click += (s, e) => AddAccount();
            btnEdit.Click += (s, e) => EditSelected();
            btnDelete.Click += (s, e) => DeleteSelected();
            btnRefresh.Click += (s, e) => LoadData();
            btnSave.Click += (s, e) => SaveChanges();
            btnImportCoinbase.Click += async (s, e) => await ImportCoinbaseReadOnlyAsync();
            btnRefreshInsights.Click += async (s, e) => await RefreshSelectedAccountInsightsAsync(forceLiveValidation: true);

            gridAccounts.DoubleClick += (s, e) => EditSelected();
            gridAccounts.CurrentCellDirtyStateChanged += GridAccounts_CurrentCellDirtyStateChanged;
            gridAccounts.SelectionChanged += async (s, e) =>
            {
                if (!_insightsOnlyMode)
                {
                    await RefreshSelectedAccountInsightsAsync();
                }
            };
        }

        private void GridAccounts_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (gridAccounts.IsCurrentCellDirty)
            {
                gridAccounts.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        public void LoadData()
        {
            if (_service == null) return;
            var accountInfos = _service.GetAll();
            var profiles = accountInfos.Select(info => (AccountProfile)info).ToList();

            _bs.DataSource = new SortableBindingList<AccountProfile>(profiles);
            gridAccounts.DataSource = _bs;

            if (_insightsOnlyMode)
            {
                EnsureInsightsOnlyLayout();
                PopulateInsightsPicker(profiles);
            }

            _ = RefreshSelectedAccountInsightsAsync();
        }

        private AccountProfile Selected()
        {
            if (_insightsOnlyMode && _cmbInsightsAccount != null)
            {
                return _cmbInsightsAccount.SelectedItem as AccountProfile;
            }

            return gridAccounts.CurrentRow != null ? gridAccounts.CurrentRow.DataBoundItem as AccountProfile : null;
        }

        private void AddAccount()
        {
            if (_service == null) return;
            var dlg = new AccountEditDialog(null, _service, _keyService);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                LoadData();
        }

        private void EditSelected()
        {
            if (_service == null) return;
            var cur = Selected();
            if (cur == null) return;

            var dlg = new AccountEditDialog(cur.Id, _service, _keyService);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                LoadData();
        }

        private void DeleteSelected()
        {
            if (_service == null) return;
            var cur = Selected();
            if (cur == null) return;

            if (MessageBox.Show("Delete account " + cur.Label + "?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _service.Delete(cur.Id);
                LoadData();
            }
        }

        private void SaveChanges()
        {
            if (_service == null) return;
            var data = _bs.DataSource as SortableBindingList<AccountProfile>;
            if (data == null) return;

            var accountInfos = data.Select(profile => (AccountInfo)profile).ToList();
            _service.ReplaceAll(accountInfos);

            MessageBox.Show("Accounts saved.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _ = RefreshSelectedAccountInsightsAsync();
        }

        private async Task ImportCoinbaseReadOnlyAsync()
        {
            if (_coinbaseReadOnlyImportService == null)
            {
                MessageBox.Show("Account/key services are not initialized.", "Import Coinbase", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnImportCoinbase.Enabled = false;
            var originalText = btnImportCoinbase.Text;
            btnImportCoinbase.Text = "Importing...";

            try
            {
                var result = await _coinbaseReadOnlyImportService.ValidateAndImportAsync().ConfigureAwait(true);
                LoadData();
                await RefreshSelectedAccountInsightsAsync().ConfigureAwait(true);

                var message = "Coinbase read-only validation succeeded.\n\n"
                    + "Key: " + result.KeyId + "\n"
                    + "Products: " + result.ProductCount + "\n"
                    + "Non-zero balances: " + result.NonZeroBalanceCount + "\n"
                    + "Holdings total (" + ResolveQuoteDisplayLabel(result.TotalBalanceQuoteCurrency) + "): " + FormatAmount(result.TotalBalanceInQuote) + "\n"
                    + "Holdings excluded (non-" + ResolveQuoteDisplayLabel(result.TotalBalanceQuoteCurrency) + "): " + result.TotalBalanceExcludedCount + "\n"
                    + "Fills returned: " + result.TotalFillCount + "\n"
                    + "New trades imported: " + result.ImportedTradeCount + "\n"
                    + "Fees paid (fills): " + FormatAmount(result.TotalFeesPaid) + "\n"
                    + "Net profit estimate: " + FormatAmount(result.NetProfitEstimate) + "\n"
                    + "Maker fee: " + (result.MakerRate * 100m).ToString("0.####") + "%\n"
                    + "Taker fee: " + (result.TakerRate * 100m).ToString("0.####") + "%\n"
                    + "Accounts imported: " + result.AccountsImported + "\n\n"
                    + "No trading actions were performed.";

                MessageBox.Show(message, "Coinbase Validation / Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Coinbase read-only validation/import failed: " + ex.Message, "Import Coinbase", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnImportCoinbase.Text = originalText;
                btnImportCoinbase.Enabled = _coinbaseReadOnlyImportService != null;
            }
        }

        private async Task RefreshSelectedAccountInsightsAsync(bool forceLiveValidation = false)
        {
            if (lblCoinbaseInsightsSummary == null)
            {
                return;
            }

            var selected = Selected();
            if (selected == null)
            {
                SetOverviewText("Select an account to view API/account insights.");
                _holdingsBinding.DataSource = new List<HoldingViewRow>();
                RefreshPerformanceTabs(new List<TradeRecord>());
                return;
            }

            var service = (selected.Service ?? string.Empty).Trim();
            var isCoinbase = service.IndexOf("coinbase", StringComparison.OrdinalIgnoreCase) >= 0;

            var summaryBuilder = new StringBuilder();

            if (isCoinbase)
            {
                var snapshot = _coinbaseReadOnlyImportService == null ? null : _coinbaseReadOnlyImportService.GetLatestSnapshot(selected.KeyEntryId);
                if (snapshot == null)
                {
                    summaryBuilder.AppendLine("No Coinbase snapshot found for this account yet. Use import or save the Coinbase key to auto-import.");
                    _holdingsBinding.DataSource = new List<HoldingViewRow>();
                }
                else
                {
                    var quoteCurrency = string.IsNullOrWhiteSpace(snapshot.TotalBalanceQuoteCurrency)
                        ? ResolveDefaultQuoteForDisplay(selected)
                        : snapshot.TotalBalanceQuoteCurrency;
                    var quoteLabel = ResolveQuoteDisplayLabel(quoteCurrency);
                    var totalInQuote = snapshot.TotalBalanceInQuote != 0m ? snapshot.TotalBalanceInQuote : snapshot.TotalBalance;
                    var importedAge = DateTime.UtcNow - snapshot.ImportedUtc;
                    var isStale = importedAge.TotalHours >= SnapshotFreshnessHours;
                    var freshnessText = isStale
                        ? "STALE (" + ((int)Math.Floor(importedAge.TotalHours)).ToString() + "h old)"
                        : "Fresh (" + ((int)Math.Floor(importedAge.TotalHours)).ToString() + "h old)";

                    summaryBuilder.AppendLine("Imported Snapshot:");
                    summaryBuilder.AppendLine("Account: " + (selected.Label ?? "(unnamed)"));
                    summaryBuilder.AppendLine("Imported: " + snapshot.ImportedUtc.ToString("u"));
                    summaryBuilder.AppendLine("Snapshot: " + freshnessText);
                    summaryBuilder.AppendLine("Products: " + snapshot.ProductCount + " | Non-zero balances: " + snapshot.NonZeroBalanceCount);
                    summaryBuilder.AppendLine("Total holdings (" + quoteLabel + "): " + FormatAmount(totalInQuote) + " | Excluded: " + snapshot.TotalBalanceExcludedCount);
                    summaryBuilder.AppendLine("Fills: " + snapshot.TotalFillCount + " | New imports: " + snapshot.ImportedTradeCount);
                    summaryBuilder.AppendLine("Fees paid: " + FormatAmount(snapshot.TotalFeesPaid) + " | Net profit est: " + FormatAmount(snapshot.NetProfitEstimate));
                    summaryBuilder.Append("Maker: " + (snapshot.MakerRate * 100m).ToString("0.####") + "% | Taker: " + (snapshot.TakerRate * 100m).ToString("0.####") + "%");

                    var holdings = snapshot.Holdings == null
                        ? new List<HoldingViewRow>()
                        : snapshot.Holdings
                            .OrderByDescending(h => h.Amount)
                            .Select(h => new HoldingViewRow { Currency = h.Currency, Amount = h.Amount })
                            .ToList();
                    _holdingsBinding.DataSource = holdings;
                }
            }

            if (!isCoinbase)
            {
                _holdingsBinding.DataSource = new List<HoldingViewRow>();
            }

            var shouldRunLive = forceLiveValidation || !isCoinbase;
            if (shouldRunLive)
            {
                if (summaryBuilder.Length > 0)
                {
                    summaryBuilder.AppendLine();
                    summaryBuilder.AppendLine();
                }

                var liveSummary = await BuildLiveApiValidationSummaryAsync(selected).ConfigureAwait(true);
                summaryBuilder.Append(liveSummary);
            }

            SetOverviewText(summaryBuilder.Length == 0
                ? "No insights available for the selected account."
                : summaryBuilder.ToString());

            RefreshPerformanceTabs(GetTradesForInsights(selected));
        }

        private void EnsureInsightsOnlyLayout()
        {
            if (mainLayout == null || topPanel == null || pnlCoinbaseInsights == null) return;

            if (gridAccounts != null)
            {
                gridAccounts.Visible = false;
            }

            if (mainLayout.RowStyles.Count >= 3)
            {
                mainLayout.RowStyles[1] = new RowStyle(SizeType.Absolute, 0F);
                mainLayout.RowStyles[2] = new RowStyle(SizeType.Percent, 100F);
            }

            if (_lblInsightsAccount == null)
            {
                _lblInsightsAccount = new Label
                {
                    AutoSize = true,
                    Text = "Account:",
                    Margin = new Padding(3, 8, 3, 0)
                };
            }

            if (_cmbInsightsAccount == null)
            {
                _cmbInsightsAccount = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Width = 280,
                    Margin = new Padding(3, 3, 8, 3)
                };
                _cmbInsightsAccount.Format += InsightsAccountFormat;
                _cmbInsightsAccount.SelectedIndexChanged += async (s, e) => await RefreshSelectedAccountInsightsAsync();
            }

            topPanel.Controls.Clear();
            topPanel.Controls.Add(_lblInsightsAccount);
            topPanel.Controls.Add(_cmbInsightsAccount);
            if (btnRefreshInsights != null)
            {
                btnRefreshInsights.Visible = true;
                btnRefreshInsights.Text = "Refresh Insights";
                topPanel.Controls.Add(btnRefreshInsights);
            }
            if (btnImportCoinbase != null)
            {
                btnImportCoinbase.Visible = true;
                topPanel.Controls.Add(btnImportCoinbase);
            }

            BuildInsightsTabs();
        }

        private void BuildInsightsTabs()
        {
            if (_insightsTabs != null) return;

            pnlCoinbaseInsights.Controls.Clear();
            pnlCoinbaseInsights.RowStyles.Clear();
            pnlCoinbaseInsights.ColumnStyles.Clear();
            pnlCoinbaseInsights.RowCount = 1;
            pnlCoinbaseInsights.ColumnCount = 1;
            pnlCoinbaseInsights.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            pnlCoinbaseInsights.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            _insightsTabs = new TabControl { Dock = DockStyle.Fill };

            var tabOverview = new TabPage("Overview");
            _txtInsightsOverview = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill
            };
            tabOverview.Controls.Add(_txtInsightsOverview);

            var tabHoldings = new TabPage("Holdings");
            if (gridHoldings.Parent != null)
            {
                gridHoldings.Parent.Controls.Remove(gridHoldings);
            }
            gridHoldings.Dock = DockStyle.Fill;
            tabHoldings.Controls.Add(gridHoldings);

            var tabHistory = new TabPage("Trade History");
            _gridTradeHistory = BuildReadOnlyGrid();
            _gridTradeHistory.AutoGenerateColumns = false;
            _gridTradeHistory.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Time (UTC)", DataPropertyName = "AtUtc", FillWeight = 130 });
            _gridTradeHistory.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Exchange", DataPropertyName = "Exchange", FillWeight = 90 });
            _gridTradeHistory.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Product", DataPropertyName = "ProductId", FillWeight = 95 });
            _gridTradeHistory.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Strategy", DataPropertyName = "Strategy", FillWeight = 90 });
            _gridTradeHistory.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Side", DataPropertyName = "Side", FillWeight = 60 });
            _gridTradeHistory.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Qty", DataPropertyName = "Quantity", FillWeight = 70 });
            _gridTradeHistory.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Fill", DataPropertyName = "FillPrice", FillWeight = 75 });
            _gridTradeHistory.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PnL", DataPropertyName = "PnL", FillWeight = 70 });
            tabHistory.Controls.Add(_gridTradeHistory);

            var tabProfitability = new TabPage("Profitability");
            _lblProfitability = new Label { Dock = DockStyle.Fill, AutoSize = false, Text = "No profitability data.", Padding = new Padding(8) };
            tabProfitability.Controls.Add(_lblProfitability);

            var tabWinLoss = new TabPage("Win vs Loss");
            _lblWinLoss = new Label { Dock = DockStyle.Fill, AutoSize = false, Text = "No win/loss data.", Padding = new Padding(8) };
            tabWinLoss.Controls.Add(_lblWinLoss);

            var tabBestStrategies = new TabPage("Best Strategies");
            _gridStrategyStats = BuildReadOnlyGrid();
            _gridStrategyStats.AutoGenerateColumns = false;
            _gridStrategyStats.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Strategy", DataPropertyName = "Strategy", FillWeight = 120 });
            _gridStrategyStats.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trades", DataPropertyName = "Trades", FillWeight = 60 });
            _gridStrategyStats.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Wins", DataPropertyName = "Wins", FillWeight = 60 });
            _gridStrategyStats.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Losses", DataPropertyName = "Losses", FillWeight = 60 });
            _gridStrategyStats.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Win %", DataPropertyName = "WinRatePct", FillWeight = 70 });
            _gridStrategyStats.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Net PnL", DataPropertyName = "NetPnl", FillWeight = 85 });
            _gridStrategyStats.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Avg PnL", DataPropertyName = "AvgPnl", FillWeight = 80 });
            tabBestStrategies.Controls.Add(_gridStrategyStats);

            var tabBestTrades = new TabPage("Best Trades");
            _gridBestTrades = BuildReadOnlyGrid();
            _gridBestTrades.AutoGenerateColumns = false;
            _gridBestTrades.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Time (UTC)", DataPropertyName = "AtUtc", FillWeight = 130 });
            _gridBestTrades.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Product", DataPropertyName = "ProductId", FillWeight = 100 });
            _gridBestTrades.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Strategy", DataPropertyName = "Strategy", FillWeight = 90 });
            _gridBestTrades.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Side", DataPropertyName = "Side", FillWeight = 60 });
            _gridBestTrades.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PnL", DataPropertyName = "PnL", FillWeight = 70 });
            tabBestTrades.Controls.Add(_gridBestTrades);

            _insightsTabs.TabPages.Add(tabOverview);
            _insightsTabs.TabPages.Add(tabHoldings);
            _insightsTabs.TabPages.Add(tabHistory);
            _insightsTabs.TabPages.Add(tabProfitability);
            _insightsTabs.TabPages.Add(tabWinLoss);
            _insightsTabs.TabPages.Add(tabBestStrategies);
            _insightsTabs.TabPages.Add(tabBestTrades);

            pnlCoinbaseInsights.Controls.Add(_insightsTabs, 0, 0);
        }

        private DataGridView BuildReadOnlyGrid()
        {
            var grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            return grid;
        }

        private void PopulateInsightsPicker(List<AccountProfile> profiles = null)
        {
            if (_cmbInsightsAccount == null) return;

            var allProfiles = profiles;
            if (allProfiles == null)
            {
                var source = _bs.DataSource as SortableBindingList<AccountProfile>;
                allProfiles = source == null ? new List<AccountProfile>() : source.ToList();
            }

            var selectedId = (_cmbInsightsAccount.SelectedItem as AccountProfile)?.Id;
            _cmbInsightsAccount.Items.Clear();

            foreach (var profile in allProfiles)
            {
                _cmbInsightsAccount.Items.Add(profile);
            }

            if (_cmbInsightsAccount.Items.Count == 0)
            {
                return;
            }

            var idx = -1;
            if (!string.IsNullOrWhiteSpace(selectedId))
            {
                for (int i = 0; i < _cmbInsightsAccount.Items.Count; i++)
                {
                    var item = _cmbInsightsAccount.Items[i] as AccountProfile;
                    if (item != null && string.Equals(item.Id, selectedId, StringComparison.OrdinalIgnoreCase))
                    {
                        idx = i;
                        break;
                    }
                }
            }

            _cmbInsightsAccount.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private void InsightsAccountFormat(object sender, ListControlConvertEventArgs e)
        {
            var profile = e.ListItem as AccountProfile;
            if (profile == null) return;

            var label = string.IsNullOrWhiteSpace(profile.Label) ? "(unnamed)" : profile.Label;
            var service = string.IsNullOrWhiteSpace(profile.Service) ? "(service?)" : profile.Service;
            e.Value = label + " [" + service + "]";
        }

        private void SetOverviewText(string text)
        {
            if (_txtInsightsOverview != null)
            {
                _txtInsightsOverview.Text = text ?? string.Empty;
                return;
            }

            lblCoinbaseInsightsSummary.Text = text ?? string.Empty;
        }

        private List<TradeRecord> GetTradesForInsights(AccountProfile selected)
        {
            if (_historyService == null)
            {
                return new List<TradeRecord>();
            }

            var trades = _historyService.LoadTrades() ?? new List<TradeRecord>();
            if (selected == null)
            {
                return trades;
            }

            var selectedFamily = ToServiceFamily(selected.Service);
            if (string.IsNullOrWhiteSpace(selectedFamily))
            {
                return trades;
            }

            var filtered = trades.Where(t => string.Equals(ToServiceFamily(t.Exchange), selectedFamily, StringComparison.OrdinalIgnoreCase)).ToList();
            return filtered.Count > 0 ? filtered : trades;
        }

        private void RefreshPerformanceTabs(List<TradeRecord> trades)
        {
            var safe = trades ?? new List<TradeRecord>();
            var executed = safe.Where(t => t != null && t.Executed).ToList();
            var pnlKnown = executed.Where(t => t.PnL.HasValue).ToList();

            if (_gridTradeHistory != null)
            {
                var rows = executed
                    .OrderByDescending(t => t.AtUtc)
                    .Take(250)
                    .Select(t => new TradeHistoryRow
                    {
                        AtUtc = t.AtUtc,
                        Exchange = t.Exchange,
                        ProductId = t.ProductId,
                        Strategy = t.Strategy,
                        Side = t.Side,
                        Quantity = t.Quantity,
                        FillPrice = t.FillPrice,
                        PnL = t.PnL
                    })
                    .ToList();
                _gridTradeHistory.DataSource = rows;
            }

            var wins = pnlKnown.Count(t => t.PnL.GetValueOrDefault() > 0m);
            var losses = pnlKnown.Count(t => t.PnL.GetValueOrDefault() < 0m);
            var flat = pnlKnown.Count - wins - losses;
            var netPnl = pnlKnown.Sum(t => t.PnL.GetValueOrDefault());
            var avgPnl = pnlKnown.Count > 0 ? netPnl / pnlKnown.Count : 0m;
            var best = pnlKnown.Count > 0 ? pnlKnown.Max(t => t.PnL.GetValueOrDefault()) : 0m;
            var worst = pnlKnown.Count > 0 ? pnlKnown.Min(t => t.PnL.GetValueOrDefault()) : 0m;
            var winRate = pnlKnown.Count > 0 ? (wins * 100m) / pnlKnown.Count : 0m;

            if (_lblProfitability != null)
            {
                _lblProfitability.Text =
                    "Executed trades: " + executed.Count + "\n"
                    + "PnL-known trades: " + pnlKnown.Count + "\n"
                    + "Net PnL: " + FormatAmount(netPnl) + "\n"
                    + "Average PnL: " + FormatAmount(avgPnl) + "\n"
                    + "Best trade: " + FormatAmount(best) + "\n"
                    + "Worst trade: " + FormatAmount(worst);
            }

            if (_lblWinLoss != null)
            {
                _lblWinLoss.Text =
                    "Wins: " + wins + "\n"
                    + "Losses: " + losses + "\n"
                    + "Flat: " + flat + "\n"
                    + "Win Rate: " + winRate.ToString("0.##") + "%";
            }

            if (_gridStrategyStats != null)
            {
                var strategyRows = pnlKnown
                    .GroupBy(t => string.IsNullOrWhiteSpace(t.Strategy) ? "(unknown)" : t.Strategy)
                    .Select(g =>
                    {
                        var strategyWins = g.Count(x => x.PnL.GetValueOrDefault() > 0m);
                        var strategyLosses = g.Count(x => x.PnL.GetValueOrDefault() < 0m);
                        var strategyNet = g.Sum(x => x.PnL.GetValueOrDefault());
                        return new StrategyStatsRow
                        {
                            Strategy = g.Key,
                            Trades = g.Count(),
                            Wins = strategyWins,
                            Losses = strategyLosses,
                            WinRatePct = g.Count() > 0 ? (strategyWins * 100m) / g.Count() : 0m,
                            NetPnl = strategyNet,
                            AvgPnl = g.Count() > 0 ? strategyNet / g.Count() : 0m
                        };
                    })
                    .OrderByDescending(r => r.NetPnl)
                    .ToList();
                _gridStrategyStats.DataSource = strategyRows;
            }

            if (_gridBestTrades != null)
            {
                var bestRows = pnlKnown
                    .OrderByDescending(t => t.PnL.GetValueOrDefault())
                    .Take(20)
                    .Select(t => new TradeHistoryRow
                    {
                        AtUtc = t.AtUtc,
                        ProductId = t.ProductId,
                        Strategy = t.Strategy,
                        Side = t.Side,
                        PnL = t.PnL
                    })
                    .ToList();
                _gridBestTrades.DataSource = bestRows;
            }
        }

        private string ToServiceFamily(string service)
        {
            var s = string.IsNullOrWhiteSpace(service) ? string.Empty : service.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            if (s.StartsWith("BINANCE")) return "BINANCE";
            if (s.StartsWith("COINBASE")) return "COINBASE";
            if (s.StartsWith("BYBIT")) return "BYBIT";
            if (s.StartsWith("OKX")) return "OKX";
            if (s.StartsWith("KRAKEN")) return "KRAKEN";
            if (s.StartsWith("BITSTAMP")) return "BITSTAMP";
            return s;
        }

        private async Task<string> BuildLiveApiValidationSummaryAsync(AccountProfile selected)
        {
            var lines = new List<string>();
            var service = (selected == null ? string.Empty : (selected.Service ?? string.Empty)).Trim();
            lines.Add("Live API Validation:");
            lines.Add("Service: " + service);
            lines.Add("Account: " + (selected == null ? "(none)" : (selected.Label ?? "(unnamed)")));

            if (_exchangeProvider == null)
            {
                lines.Add("Validation unavailable: exchange provider is not initialized.");
                return string.Join("\n", lines);
            }

            try
            {
                var client = _exchangeProvider.CreatePublicClient(service);
                var products = await client.ListProductsAsync().ConfigureAwait(true);
                var symbol = ResolveProbeSymbol(products, selected == null ? string.Empty : selected.DefaultQuote);
                var ticker = await client.GetTickerAsync(symbol).ConfigureAwait(true);

                lines.Add("Public API: PASS");
                lines.Add("Products: " + (products == null ? 0 : products.Count));
                lines.Add("Probe Symbol: " + symbol + " | Last: " + (ticker == null ? "0" : ticker.Last.ToString("0.########")));
            }
            catch (Exception ex)
            {
                lines.Add("Public API: FAIL - " + ex.Message);
            }

            try
            {
                var authClient = _exchangeProvider.CreateAuthenticatedClient(service);
                var fees = await authClient.GetFeesAsync().ConfigureAwait(true);
                lines.Add("Private API Auth: PASS");
                lines.Add("Fees: Maker " + ((fees == null ? 0m : fees.MakerRate) * 100m).ToString("0.####") + "% | Taker " + ((fees == null ? 0m : fees.TakerRate) * 100m).ToString("0.####") + "%");
            }
            catch (Exception ex)
            {
                lines.Add("Private API Auth: FAIL - " + ex.Message);
            }

            try
            {
                if (_accountBuyingPowerService == null)
                {
                    lines.Add("Buying Power: unavailable (service not initialized)");
                }
                else
                {
                    var account = selected == null ? null : (AccountInfo)selected;
                    var buyingPower = await _accountBuyingPowerService.ResolveAsync(account, 0m).ConfigureAwait(true);
                    if (buyingPower.UsedLiveBalance)
                    {
                        lines.Add("Buying Power: " + buyingPower.EquityToUse.ToString("0.########") + " " + (buyingPower.QuoteCurrency ?? "USD") + " (live)");
                    }
                    else
                    {
                        lines.Add("Buying Power: not available from live balances (" + (buyingPower.Note ?? "unknown") + ")");
                    }
                }
            }
            catch (Exception ex)
            {
                lines.Add("Buying Power: FAIL - " + ex.Message);
            }

            lines.Add("Validated UTC: " + DateTime.UtcNow.ToString("u"));
            return string.Join("\n", lines);
        }

        private string ResolveProbeSymbol(List<string> products, string preferredQuote)
        {
            if (products == null || products.Count == 0)
            {
                return "BTC-USD";
            }

            var quote = string.IsNullOrWhiteSpace(preferredQuote) ? "USD" : preferredQuote.Trim().ToUpperInvariant();
            for (int i = 0; i < products.Count; i++)
            {
                var symbol = products[i] ?? string.Empty;
                var upper = symbol.ToUpperInvariant();
                if (upper.EndsWith("/" + quote) || upper.EndsWith("-" + quote))
                {
                    return symbol;
                }
            }

            for (int i = 0; i < products.Count; i++)
            {
                var symbol = products[i] ?? string.Empty;
                if (symbol.ToUpperInvariant().EndsWith("/USD") || symbol.ToUpperInvariant().EndsWith("-USD"))
                {
                    return symbol;
                }
            }

            return products[0];
        }

        private string ResolveDefaultQuoteForDisplay(AccountProfile selected)
        {
            if (selected != null && !string.IsNullOrWhiteSpace(selected.DefaultQuote))
            {
                return selected.DefaultQuote;
            }

            return "USD";
        }

        private string ResolveQuoteDisplayLabel(string quote)
        {
            var normalized = string.IsNullOrWhiteSpace(quote) ? "USD" : quote.Trim().ToUpperInvariant();
            if (normalized == "USD" || normalized == "USDC" || normalized == "USDT" || normalized == "USDP" || normalized == "FDUSD" || normalized == "TUSD")
            {
                return "USD-family";
            }

            return normalized;
        }

        private string FormatAmount(decimal value)
        {
            return value.ToString("0.########");
        }
    }
}
