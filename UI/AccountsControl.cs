using System;
using System.Collections.Generic;
using System.Linq;
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

            if (btnImportCoinbase != null)
            {
                btnImportCoinbase.Enabled = _coinbaseReadOnlyImportService != null;
            }
            LoadData();
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

            gridAccounts.DoubleClick += (s, e) => EditSelected();
            gridAccounts.CurrentCellDirtyStateChanged += GridAccounts_CurrentCellDirtyStateChanged;
            gridAccounts.SelectionChanged += (s, e) => RefreshSelectedAccountInsights();
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
            RefreshSelectedAccountInsights();
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
            RefreshSelectedAccountInsights();
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
                RefreshSelectedAccountInsights();

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

        private void RefreshSelectedAccountInsights()
        {
            if (lblCoinbaseInsightsSummary == null || _coinbaseReadOnlyImportService == null)
            {
                return;
            }

            var selected = Selected();
            if (selected == null)
            {
                lblCoinbaseInsightsSummary.Text = "Select an account to view Coinbase imported metrics.";
                _holdingsBinding.DataSource = new List<HoldingViewRow>();
                return;
            }

            var service = (selected.Service ?? string.Empty).Trim();
            if (service.IndexOf("coinbase", StringComparison.OrdinalIgnoreCase) < 0)
            {
                lblCoinbaseInsightsSummary.Text = "Selected account is not Coinbase.";
                _holdingsBinding.DataSource = new List<HoldingViewRow>();
                return;
            }

            var snapshot = _coinbaseReadOnlyImportService.GetLatestSnapshot(selected.KeyEntryId);
            if (snapshot == null)
            {
                lblCoinbaseInsightsSummary.Text = "No Coinbase snapshot found for this account yet. Use import or save the Coinbase key to auto-import.";
                _holdingsBinding.DataSource = new List<HoldingViewRow>();
                return;
            }

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

            lblCoinbaseInsightsSummary.Text =
                "Account: " + (selected.Label ?? "(unnamed)") + "\n"
                + "Imported: " + snapshot.ImportedUtc.ToString("u") + "\n"
                + "Snapshot: " + freshnessText + "\n"
                + "Products: " + snapshot.ProductCount + " | Non-zero balances: " + snapshot.NonZeroBalanceCount + "\n"
                + "Total holdings (" + quoteLabel + "): " + FormatAmount(totalInQuote)
                + " | Excluded: " + snapshot.TotalBalanceExcludedCount + "\n"
                + "Fills: " + snapshot.TotalFillCount + " | New imports: " + snapshot.ImportedTradeCount + "\n"
                + "Fees paid: " + FormatAmount(snapshot.TotalFeesPaid) + " | Net profit est: " + FormatAmount(snapshot.NetProfitEstimate) + "\n"
                + "Maker: " + (snapshot.MakerRate * 100m).ToString("0.####") + "% | Taker: " + (snapshot.TakerRate * 100m).ToString("0.####") + "%";

            var holdings = snapshot.Holdings == null
                ? new List<HoldingViewRow>()
                : snapshot.Holdings
                    .OrderByDescending(h => h.Amount)
                    .Select(h => new HoldingViewRow { Currency = h.Currency, Amount = h.Amount })
                    .ToList();
            _holdingsBinding.DataSource = holdings;
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
