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

        private sealed class HoldingViewRow
        {
            public string Currency { get; set; }
            public decimal Amount { get; set; }
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
            if (btnAdd != null) btnAdd.Visible = !insightsOnly;
            if (btnEdit != null) btnEdit.Visible = !insightsOnly;
            if (btnDelete != null) btnDelete.Visible = !insightsOnly;
            if (btnSave != null) btnSave.Visible = !insightsOnly;
            if (btnRefresh != null) btnRefresh.Visible = !insightsOnly;
        }

        private void ConfigureGrid()
        {
            gridAccounts.AutoGenerateColumns = false;
            
            // Map columns to properties on AccountProfile
            colLabel.DataPropertyName = "Label";
            colService.DataPropertyName = "Service";
            colMode.DataPropertyName = "Mode";
            colRisk.DataPropertyName = "RiskPerTradePct";
            colMaxOpen.DataPropertyName = "MaxConcurrentTrades";
            colKeyId.DataPropertyName = "KeyEntryId";
            colEnabled.DataPropertyName = "Enabled";

            // Allow editing only for checkboxes
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
            btnAdd.Click += (s, e) => AddAccount();
            btnEdit.Click += (s, e) => EditSelected();
            btnDelete.Click += (s, e) => DeleteSelected();
            btnRefresh.Click += (s, e) => LoadData();
            btnSave.Click += (s, e) => SaveChanges();
            btnImportCoinbase.Click += async (s, e) => await ImportCoinbaseReadOnlyAsync();
            btnRefreshInsights.Click += async (s, e) => await RefreshSelectedAccountInsightsAsync(forceLiveValidation: true);

            gridAccounts.DoubleClick += (s, e) => EditSelected();
            gridAccounts.CurrentCellDirtyStateChanged += GridAccounts_CurrentCellDirtyStateChanged;
            gridAccounts.SelectionChanged += async (s, e) => await RefreshSelectedAccountInsightsAsync();
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
            
            // Use SortableBindingList to support column sorting
            _bs.DataSource = new SortableBindingList<AccountProfile>(profiles);
            gridAccounts.DataSource = _bs;
            _ = RefreshSelectedAccountInsightsAsync();
        }

        private AccountProfile Selected()
        {
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

            // Convert profiles back to Info (preserves IDs and updates Enabled/formatted fields)
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
                lblCoinbaseInsightsSummary.Text = "Select an account to view API/account insights.";
                _holdingsBinding.DataSource = new List<HoldingViewRow>();
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

            lblCoinbaseInsightsSummary.Text = summaryBuilder.Length == 0
                ? "No insights available for the selected account."
                : summaryBuilder.ToString();
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
