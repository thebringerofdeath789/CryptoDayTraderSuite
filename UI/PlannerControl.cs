using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Brokers;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.UI
{
    public partial class PlannerControl : UserControl
    {
        private IHistoryService _historyService;
        private AutoPlannerService _planner;
        private IExchangeClient _client;
        private IAccountService _accountService;
        private IKeyService _keyService;
        private AccountBuyingPowerService _buyingPowerService;

        private List<TradeRecord> _planned = new List<TradeRecord>();
        private List<PredictionRecord> _preds = new List<PredictionRecord>();
        private List<AccountInfo> _accounts = new List<AccountInfo>();
        private List<ProjectionRow> _scanRows = new List<ProjectionRow>();
        private List<TradePlan> _queuedPlans = new List<TradePlan>();

        private sealed class PlannerProposalDiagnosticsSummary
        {
            public readonly Dictionary<string, int> ReasonCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            public readonly HashSet<string> ChosenVenues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public readonly HashSet<string> AlternateVenues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public readonly HashSet<string> ExecutionModes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public int PolicyHealthBlockedCount;
            public int RegimeBlockedCount;
            public int CircuitBreakerObservedCount;
            public int RoutingUnavailableCount;
        }

        public PlannerControl()
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
            UpdatePlannerStatus("Ready");
            
            // Wire events
            if (btnRefresh != null) btnRefresh.Click += (s, e) => LoadData();
            if (btnSave != null) btnSave.Click += (s, e) => SaveData();
            if (btnAdd != null) btnAdd.Click += (s, e) => AddTrade();
            if (btnScan != null) btnScan.Click += (s, e) => DoScan();
            if (btnPropose != null) btnPropose.Click += (s, e) => DoPropose();
            if (btnProposeAll != null) btnProposeAll.Click += (s, e) => DoProposeAll();
            if (btnExecute != null) btnExecute.Click += (s, e) => DoExecute();
            
            if (cmbFilterProduct != null) cmbFilterProduct.SelectedIndexChanged += (s, e) => ApplyFilters();
            if (cmbFilterStrategy != null) cmbFilterStrategy.SelectedIndexChanged += (s, e) => ApplyFilters();
            
            if (gridPlanned != null)
            {
                gridPlanned.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditSelectedTrade(); };
                gridPlanned.CurrentCellDirtyStateChanged += (s, e) =>
                {
                    if (gridPlanned.IsCurrentCellDirty)
                    {
                        gridPlanned.CommitEdit(DataGridViewDataErrorContexts.Commit);
                    }
                };
            }
                
            // Context menu wiring
            if (miEdit != null) miEdit.Click += (s, e) => EditSelectedTrade();
            if (miDelete != null) miDelete.Click += (s, e) => DeleteSelectedTrade();

            if (cmbGran != null && cmbGran.Items.Count > 0 && cmbGran.SelectedIndex < 0) cmbGran.SelectedIndex = 2;
            if (numLookback != null && numLookback.Value < 5) numLookback.Value = 30;
            if (numEquity != null && numEquity.Value < 10) numEquity.Value = 1000;
        }

        public void Initialize(IHistoryService historyService)
        {
            Initialize(historyService, null, null, null, null);
        }

        public void Initialize(IHistoryService historyService, AutoPlannerService planner, IExchangeClient client, IAccountService accountService, IKeyService keyService)
        {
            _historyService = historyService;
            _planner = planner;
            _client = client;
            _accountService = accountService;
            _keyService = keyService;
            _buyingPowerService = _keyService == null ? null : new AccountBuyingPowerService(_keyService);

            LoadAccounts();
            LoadProducts();
            LoadData();
        }

        private void LoadAccounts()
        {
            if (_accountService == null || cmbAccount == null) return;
            _accounts = _accountService.GetAll().Where(a => a.Enabled).ToList();

            cmbAccount.Items.Clear();
            foreach (var account in _accounts)
            {
                cmbAccount.Items.Add($"{account.Label} [{account.Service}]");
            }

            if (cmbAccount.Items.Count > 0 && cmbAccount.SelectedIndex < 0)
            {
                cmbAccount.SelectedIndex = 0;
            }
        }

        private async void LoadProducts()
        {
            if (cmbRunProduct == null) return;

            try
            {
                List<string> products;
                if (_client != null) products = await _client.ListProductsAsync();
                else products = await new CoinbasePublicClient().GetProductsAsync();

                cmbRunProduct.Items.Clear();
                foreach (var product in products.Where(p => p.Contains("USD")))
                {
                    cmbRunProduct.Items.Add(product);
                }

                if (cmbRunProduct.Items.Count > 0 && cmbRunProduct.SelectedIndex < 0)
                {
                    cmbRunProduct.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Log.Error("[Planner] Failed to load products", ex);
            }
        }

        private void LoadData()
        {
            if (_historyService == null) return;

            _planned = _historyService.LoadPlannedTrades() ?? new List<TradeRecord>();
            _preds = _historyService.LoadPredictions() ?? new List<PredictionRecord>();

            // Populate filters
            var products = _planned.Select(p => p.ProductId).Distinct().OrderBy(x => x).ToList();
            
            string currentProd = cmbFilterProduct.SelectedItem as string;
            cmbFilterProduct.Items.Clear();
            cmbFilterProduct.Items.Add("All");
            cmbFilterProduct.Items.AddRange(products.ToArray());
            if (currentProd != null && cmbFilterProduct.Items.Contains(currentProd))
                cmbFilterProduct.SelectedItem = currentProd;
            else
                cmbFilterProduct.SelectedIndex = 0;

            var strategies = _planned.Select(p => p.Strategy).Distinct().OrderBy(x => x).ToList();
            string currentStrat = cmbFilterStrategy.SelectedItem as string;
            cmbFilterStrategy.Items.Clear();
            cmbFilterStrategy.Items.Add("All");
            cmbFilterStrategy.Items.AddRange(strategies.ToArray());
            if (currentStrat != null && cmbFilterStrategy.Items.Contains(currentStrat))
                cmbFilterStrategy.SelectedItem = currentStrat;
            else
                cmbFilterStrategy.SelectedIndex = 0;

            ApplyFilters();

            gridPreds.Rows.Clear();
            foreach (var r in _preds.OrderByDescending(x => x.AtUtc).Take(500))
            {
                gridPreds.Rows.Add(r.ProductId, r.AtUtc.ToLocalTime(), r.HorizonMinutes, r.Direction, r.Probability, r.ExpectedReturn, r.ExpectedVol, r.RealizedKnown, r.RealizedDirection, r.RealizedReturn);
            }

            UpdatePlannerStatus($"Loaded {_planned.Count} planned trade(s), {_preds.Count} prediction row(s)");
        }
        
        private List<TradeRecord> GetFilteredPlanned()
        {
            string prod = cmbFilterProduct.SelectedItem?.ToString();
            string strat = cmbFilterStrategy.SelectedItem?.ToString();
            return _planned.Where(p =>
                (prod == "All" || string.IsNullOrEmpty(prod) || p.ProductId == prod) &&
                (strat == "All" || string.IsNullOrEmpty(strat) || p.Strategy == strat)).OrderByDescending(x => x.AtUtc).ToList();
        }

        private void ApplyFilters()
        {
            if (_planned == null) return;
            
            var filtered = GetFilteredPlanned();
            gridPlanned.Rows.Clear();
            foreach (var p in filtered)
            {
                var rowIndex = gridPlanned.Rows.Add(p.Enabled, p.Exchange, p.ProductId, p.Strategy, p.Side, p.Quantity, p.Price, p.EstEdge, p.Notes);
                gridPlanned.Rows[rowIndex].Tag = p;
            }
        }

        private void SaveData()
        {
            if (_historyService == null) return;

            var filtered = GetFilteredPlanned();

            if (gridPlanned.Rows.Count != filtered.Count)
            {
                // Simple safeguard: If filter mismatches grid rows, abort save to avoid corruption
                UpdatePlannerStatus("Save aborted: filter mismatch; data reloaded", true);
                LoadData();
                return;
            }

            for (int i = 0; i < gridPlanned.Rows.Count; i++)
            {
                var row = gridPlanned.Rows[i];
                var p = row.Tag as TradeRecord;
                if (p == null)
                {
                    if (i < 0 || i >= filtered.Count) continue;
                    p = filtered[i];
                }
                p.Enabled = Convert.ToBoolean(row.Cells[0].Value ?? false);
                p.Quantity = Convert.ToDecimal(row.Cells[5].Value ?? 0m);
                p.Price = Convert.ToDecimal(row.Cells[6].Value ?? 0m);
                p.Notes = row.Cells[8].Value?.ToString();
            }
            
            _historyService.SavePlannedTrades(_planned);
            UpdatePlannerStatus("Planner updated");
        }

        private void AddTrade()
        {
            var dlg = new TradeEditDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _planned.Add(dlg.Result);
                SaveData();
                LoadData();
            }
        }

        private void EditSelectedTrade()
        {
            if (gridPlanned.SelectedRows.Count == 0) return;
            var row = gridPlanned.SelectedRows[0];
            var rec = row.Tag as TradeRecord;
            if (rec == null)
            {
                var idx = row.Index;
                var filtered = GetFilteredPlanned();
                if (idx < 0 || idx >= filtered.Count) return;
                rec = filtered[idx];
            }
            
            var dlg = new TradeEditDialog(rec);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                // Update the original record in _planned
                // Since rec is a reference from _planned (via LINQ), updating it updates the list reference.
                // However, let's just create a new one to be safe if TradeRecord is struct (it's class though).
                // Actually assuming TradeRecord is a class.
                
                rec.Enabled = dlg.Result.Enabled;
                rec.Quantity = dlg.Result.Quantity;
                rec.Price = dlg.Result.Price;
                rec.Notes = dlg.Result.Notes;
                
                SaveData();
                LoadData();
            }
        }

        private void DeleteSelectedTrade()
        {
            if (gridPlanned.SelectedRows.Count == 0) return;
            var records = gridPlanned.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.Tag as TradeRecord)
                .Where(r => r != null)
                .Distinct()
                .ToList();

            if (records.Count == 0)
            {
                var filtered = GetFilteredPlanned();
                records = gridPlanned.SelectedRows
                    .Cast<DataGridViewRow>()
                    .Select(r => r.Index)
                    .Where(i => i >= 0 && i < filtered.Count)
                    .Distinct()
                    .Select(i => filtered[i])
                    .ToList();
            }

            if (records.Count == 0) return;

            var prompt = records.Count == 1
                ? $"Delete planned trade for {records[0].ProductId} ({records[0].Strategy})?"
                : $"Delete {records.Count} selected planned trade(s)?";

            if (MessageBox.Show(prompt, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                foreach (var rec in records)
                {
                    _planned.Remove(rec);
                }

                SaveData();
                LoadData();
                UpdatePlannerStatus($"Deleted {records.Count} planned trade(s)");
            }
        }

        private int AddProposedPlans(IEnumerable<TradePlan> plans, string exchange, out int duplicates)
        {
            duplicates = 0;
            var added = 0;
            if (plans == null) return added;

            foreach (var plan in plans)
            {
                if (_planned.Any(p => p.ProductId == plan.Symbol && p.Strategy == plan.Strategy && !p.Executed))
                {
                    duplicates++;
                    continue;
                }

                var rec = new TradeRecord
                {
                    Enabled = true,
                    Exchange = exchange,
                    ProductId = (plan.Symbol ?? string.Empty).Replace("/", "-"),
                    Strategy = plan.Strategy,
                    Side = plan.Direction > 0 ? "Buy" : "Sell",
                    Quantity = plan.Qty,
                    Price = plan.Entry,
                    EstEdge = CalculateEdge(plan),
                    AtUtc = DateTime.UtcNow,
                    Executed = false,
                    Notes = plan.Note
                };

                _planned.Add(rec);
                added++;
            }

            return added;
        }

        private async void DoScan()
        {
            if (_planner == null)
            {
                UpdatePlannerStatus("Scan unavailable: planner service missing", true);
                return;
            }

            if (cmbRunProduct == null || cmbRunProduct.SelectedItem == null)
            {
                UpdatePlannerStatus("Scan blocked: select a product first", true);
                return;
            }

            int gran;
            if (!int.TryParse(cmbGran.SelectedItem?.ToString(), out gran)) gran = 15;
            int lookbackMins = (int)numLookback.Value * 1440;
            string symbol = cmbRunProduct.SelectedItem.ToString();

            btnScan.Enabled = false;
            try
            {
                _scanRows = await _planner.ProjectAsync(symbol, gran, lookbackMins, 0.006m, 0.004m);
                if (_scanRows == null || _scanRows.Count == 0)
                {
                    UpdatePlannerStatus("Scan complete: no projection rows", true);
                    return;
                }

                // Populate Scan Results Grid (reuse Predictions grid for now or create new one)
                gridPreds.Rows.Clear();
                foreach (var r in _scanRows.OrderByDescending(x => x.Expectancy))
                {
                    gridPreds.Rows.Add(r.Symbol, DateTime.UtcNow.ToLocalTime(), r.GranMinutes, r.Expectancy >= 0 ? "Bul" : "Bea", r.WinRate, r.Expectancy, r.Samples, 0, 0, 0);
                }

                var best = _scanRows.OrderByDescending(r => r.Expectancy).First();
                UpdatePlannerStatus($"Scan complete: {symbol} top {best.Strategy} (Exp {best.Expectancy:0.00}, Win {best.WinRate:0.0}%)");
            }
            catch (Exception ex)
            {
                Log.Error("[Planner] Scan failed", ex);
                MessageBox.Show("Scan failed: " + ex.Message, "Planner", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdatePlannerStatus("Scan failed: " + ex.Message, true);
            }
            finally
            {
                btnScan.Enabled = true;
            }
        }

        private async void DoPropose()
        {
            var proposeDisabled = false;
            try
            {
                if (_planner == null)
                {
                    UpdatePlannerStatus("Propose unavailable: planner service missing", true);
                    return;
                }
                if (_scanRows == null || _scanRows.Count == 0)
                {
                    UpdatePlannerStatus("Propose blocked: run Scan first", true);
                    return;
                }
                if (_accounts == null || cmbAccount == null || cmbAccount.SelectedIndex < 0 || cmbAccount.SelectedIndex >= _accounts.Count)
                {
                    UpdatePlannerStatus("Propose blocked: select an account first", true);
                    return;
                }
                if (cmbRunProduct == null || cmbRunProduct.SelectedItem == null)
                {
                    UpdatePlannerStatus("Propose blocked: select a product first", true);
                    return;
                }

                var account = _accounts[cmbAccount.SelectedIndex];
                int gran;
                if (!int.TryParse(cmbGran.SelectedItem?.ToString(), out gran)) gran = 15;
                string symbol = cmbRunProduct.SelectedItem.ToString();

                btnPropose.Enabled = false;
                proposeDisabled = true;

                // Pass _scanRows to ProposeAsync
                // The service will filter/pick best strategy based on expectancy
                var manualEquity = numEquity == null ? 1000m : numEquity.Value;
                var resolvedEquity = await ResolveEquityForPlanningAsync(account, manualEquity, "Propose");
                var diagnostics = await _planner.ProposeWithDiagnosticsAsync(account.Id, symbol, gran, resolvedEquity, account.RiskPerTradePct, _scanRows);
                var plans = diagnostics != null ? (diagnostics.Plans ?? new List<TradePlan>()) : new List<TradePlan>();
                _queuedPlans = plans ?? new List<TradePlan>();
                var summary = BuildProposalDiagnosticsSummary(diagnostics, plans);
                
                if (plans == null || plans.Count == 0)
                {
                    // Check logs for specific reason
                    UpdatePlannerStatus("Propose complete: no active signal (No Signal/Risk Guard/AI Veto) | " + FormatProposalDiagnosticsSummary(summary), true);
                    return;
                }

                int duplicates;
                int added = AddProposedPlans(plans, account.Service, out duplicates);

                if (added > 0)
                {
                    _historyService?.SavePlannedTrades(_planned);
                    ApplyFilters(); // Refresh planned grid
                    UpdatePlannerStatus($"Propose complete: {added} added, {duplicates} duplicate(s); {_queuedPlans.Count} queued for execute | {FormatProposalDiagnosticsSummary(summary)}");
                }
                else
                {
                    UpdatePlannerStatus($"Propose complete: 0 added (duplicates); {_queuedPlans.Count} queued for execute | {FormatProposalDiagnosticsSummary(summary)}", true);
                }
            }
            catch (Exception ex)
            {
                Log.Error("[Planner] Propose failed", ex);
                MessageBox.Show("Propose failed: " + ex.Message, "Planner", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdatePlannerStatus("Propose failed: " + ex.Message, true);
            }
            finally
            {
                if (proposeDisabled) btnPropose.Enabled = true;
            }
        }

        private async void DoProposeAll()
        {
            var disabled = false;
            try
            {
                if (_planner == null)
                {
                    UpdatePlannerStatus("Propose All unavailable: planner service missing", true);
                    return;
                }

                if (_accounts == null || cmbAccount == null || cmbAccount.SelectedIndex < 0 || cmbAccount.SelectedIndex >= _accounts.Count)
                {
                    UpdatePlannerStatus("Propose All blocked: select an account first", true);
                    return;
                }

                int gran;
                if (!int.TryParse(cmbGran.SelectedItem?.ToString(), out gran)) gran = 15;
                int lookbackMins = (int)numLookback.Value * 1440;

                var symbols = cmbRunProduct.Items
                    .Cast<object>()
                    .Select(item => item == null ? string.Empty : item.ToString())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (symbols.Count == 0)
                {
                    UpdatePlannerStatus("Propose All blocked: no symbols available", true);
                    return;
                }

                if (symbols.Count > 25)
                {
                    var proceed = MessageBox.Show($"Propose across {symbols.Count} symbols? This may take a while.", "Planner", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (proceed != DialogResult.Yes)
                    {
                        UpdatePlannerStatus("Propose All cancelled", true);
                        return;
                    }
                }

                btnPropose.Enabled = false;
                if (btnProposeAll != null) btnProposeAll.Enabled = false;
                disabled = true;

                var account = _accounts[cmbAccount.SelectedIndex];
                var manualEquity = numEquity == null ? 1000m : numEquity.Value;
                var resolvedEquity = await ResolveEquityForPlanningAsync(account, manualEquity, "Propose All");
                var allQueued = new List<TradePlan>();
                var aggregateSummary = new PlannerProposalDiagnosticsSummary();

                int scanned = 0;
                int proposedSymbols = 0;
                int added = 0;
                int duplicates = 0;

                foreach (var symbol in symbols)
                {
                    var rows = await _planner.ProjectAsync(symbol, gran, lookbackMins, 0.006m, 0.004m);
                    scanned++;
                    if (rows == null || rows.Count == 0)
                    {
                        IncrementReasonCount(aggregateSummary, "no-projection");
                        continue;
                    }

                    var diagnostics = await _planner.ProposeWithDiagnosticsAsync(account.Id, symbol, gran, resolvedEquity, account.RiskPerTradePct, rows);
                    var plans = diagnostics != null ? (diagnostics.Plans ?? new List<TradePlan>()) : new List<TradePlan>();
                    MergeProposalDiagnosticsSummary(aggregateSummary, BuildProposalDiagnosticsSummary(diagnostics, plans));
                    if (plans == null || plans.Count == 0)
                    {
                        continue;
                    }

                    proposedSymbols++;
                    allQueued.AddRange(plans);

                    int localDuplicates;
                    added += AddProposedPlans(plans, account.Service, out localDuplicates);
                    duplicates += localDuplicates;
                }

                _queuedPlans = allQueued;

                if (added > 0)
                {
                    _historyService?.SavePlannedTrades(_planned);
                    ApplyFilters();
                }

                if (_queuedPlans.Count == 0)
                {
                    UpdatePlannerStatus($"Propose All complete: no queued plans across {scanned} symbol(s) | {FormatProposalDiagnosticsSummary(aggregateSummary)}", true);
                    return;
                }

                UpdatePlannerStatus($"Propose All complete: {_queuedPlans.Count} queued across {proposedSymbols}/{scanned} symbol(s); {added} added, {duplicates} duplicate(s) | {FormatProposalDiagnosticsSummary(aggregateSummary)}");
            }
            catch (Exception ex)
            {
                Log.Error("[Planner] Propose All failed", ex);
                MessageBox.Show("Propose All failed: " + ex.Message, "Planner", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdatePlannerStatus("Propose All failed: " + ex.Message, true);
            }
            finally
            {
                if (disabled)
                {
                    btnPropose.Enabled = true;
                    if (btnProposeAll != null) btnProposeAll.Enabled = true;
                }
            }
        }

        private async void DoExecute()
        {
            var executeDisabled = false;
            try
            {
                if (_queuedPlans == null || _queuedPlans.Count == 0)
                {
                    UpdatePlannerStatus("Execute blocked: no queued plans (run Propose first)", true);
                    return;
                }

                if (_accounts == null || cmbAccount == null || cmbAccount.SelectedIndex < 0 || cmbAccount.SelectedIndex >= _accounts.Count)
                {
                    UpdatePlannerStatus("Execute blocked: select an account first", true);
                    return;
                }

                var account = _accounts[cmbAccount.SelectedIndex];
                var broker = BrokerFactory.GetBroker(account.Service, account.Mode, _keyService, _accountService);
                if (broker == null)
                {
                    MessageBox.Show("Unsupported broker: " + account.Service, "Planner", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var caps = broker.GetCapabilities();
                if (caps == null)
                {
                    MessageBox.Show("Broker capabilities unavailable.", "Planner", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!caps.SupportsMarketEntry)
                {
                    MessageBox.Show("Selected broker does not support market entry.", "Planner", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (account.Mode != AccountMode.Paper && !caps.SupportsProtectiveExits)
                {
                    var reason = !string.IsNullOrWhiteSpace(caps.Notes)
                        ? caps.Notes
                        : "Protective exits are required for live execution.";
                    MessageBox.Show("Execution blocked: " + reason, "Planner", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                btnExecute.Enabled = false;
                executeDisabled = true;

                foreach (var plan in _queuedPlans.Where(p => p.AccountId == account.Id))
                {
                    var validation = await broker.ValidateTradePlanAsync(plan);
                    if (!validation.ok)
                    {
                        Log.Warn("[Planner] Plan blocked by broker validation: " + validation.message + " | symbol=" + (plan.Symbol ?? "") + " | account=" + (account.Id ?? ""));
                        continue;
                    }

                    var result = await broker.PlaceOrderAsync(plan);
                    if (result.ok)
                    {
                        var planned = _planned.LastOrDefault(t => t.ProductId == (plan.Symbol ?? string.Empty).Replace("/", "-")
                                                              && t.Strategy == plan.Strategy
                                                              && !t.Executed);
                        if (planned != null)
                        {
                            planned.Executed = true;
                            planned.FillPrice = plan.Entry;
                        }
                    }
                    else
                    {
                        Log.Warn("[Planner] Execute failed: " + result.message);
                    }
                }

                _historyService?.SavePlannedTrades(_planned);
                ApplyFilters();
                UpdatePlannerStatus("Execution cycle complete");
            }
            catch (Exception ex)
            {
                Log.Error("[Planner] Execute failed", ex);
                MessageBox.Show("Execute failed: " + ex.Message, "Planner", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdatePlannerStatus("Execute failed: " + ex.Message, true);
            }
            finally
            {
                if (executeDisabled) btnExecute.Enabled = true;
            }
        }

        private void UpdatePlannerStatus(string message, bool warn = false)
        {
            if (lblPlannerStatus == null) return;

            var suffix = DateTime.Now.ToString("HH:mm:ss");
            lblPlannerStatus.Text = "Status: " + message + " · " + suffix;
            lblPlannerStatus.ForeColor = warn ? Color.DarkOrange : Color.DarkGreen;
        }

        private async Task<decimal> ResolveEquityForPlanningAsync(AccountInfo account, decimal manualFallback, string scope)
        {
            if (_buyingPowerService == null || account == null)
            {
                return manualFallback;
            }

            var resolution = await _buyingPowerService.ResolveAsync(account, manualFallback).ConfigureAwait(true);
            if (resolution == null)
            {
                return manualFallback;
            }

            if (resolution.UsedLiveBalance)
            {
                Log.Info("[Planner] " + scope + " using live buying power " + resolution.EquityToUse.ToString("0.########") + " " + (resolution.QuoteCurrency ?? "USD") + " for account " + (account.Label ?? account.Id));
                return resolution.EquityToUse;
            }

            Log.Warn("[Planner] " + scope + " using manual equity fallback " + manualFallback.ToString("0.########") + " (" + (resolution.Note ?? "no reason") + ")");
            return manualFallback;
        }

        private decimal CalculateEdge(TradePlan plan)
        {
            if (plan == null || plan.Entry <= 0m) return 0m;
            if (plan.Direction > 0) return (plan.Target - plan.Entry) / plan.Entry;
            return (plan.Entry - plan.Target) / plan.Entry;
        }

        private PlannerProposalDiagnosticsSummary BuildProposalDiagnosticsSummary(AutoPlannerService.ProposalDiagnostics diagnostics, IEnumerable<TradePlan> plans)
        {
            var summary = new PlannerProposalDiagnosticsSummary();
            if (diagnostics != null && !string.IsNullOrWhiteSpace(diagnostics.ReasonCode))
            {
                IncrementReasonCount(summary, diagnostics.ReasonCode);
            }

            foreach (var plan in plans ?? Enumerable.Empty<TradePlan>())
            {
                var note = plan == null ? string.Empty : (plan.Note ?? string.Empty);

                var chosen = ExtractBracketTagValue(note, "Route");
                if (!string.IsNullOrWhiteSpace(chosen)) summary.ChosenVenues.Add(chosen.Trim());

                var alternate = ExtractBracketTagValue(note, "Alt");
                if (!string.IsNullOrWhiteSpace(alternate)) summary.AlternateVenues.Add(alternate.Trim());

                var execMode = ExtractBracketTagValue(note, "ExecMode");
                if (!string.IsNullOrWhiteSpace(execMode)) summary.ExecutionModes.Add(execMode.Trim());

                var policyReason = ExtractBracketTagValue(note, "PolicyReason");
                if (!string.IsNullOrWhiteSpace(policyReason)) summary.PolicyHealthBlockedCount++;

                var regime = ExtractBracketTagValue(note, "Regime");
                if (!string.IsNullOrWhiteSpace(regime) && !string.Equals(regime, "normal", StringComparison.OrdinalIgnoreCase)) summary.RegimeBlockedCount++;

                var routeUnavailable = string.Equals(diagnostics != null ? diagnostics.ReasonCode : string.Empty, "routing-unavailable", StringComparison.OrdinalIgnoreCase);
                if (routeUnavailable) summary.RoutingUnavailableCount++;
            }

            if (diagnostics != null)
            {
                var reasonCode = diagnostics.ReasonCode ?? string.Empty;
                if (reasonCode.IndexOf("policy", StringComparison.OrdinalIgnoreCase) >= 0) summary.PolicyHealthBlockedCount++;
                if (reasonCode.IndexOf("regime", StringComparison.OrdinalIgnoreCase) >= 0) summary.RegimeBlockedCount++;
                if (reasonCode.IndexOf("circuit", StringComparison.OrdinalIgnoreCase) >= 0) summary.CircuitBreakerObservedCount++;
                if (string.Equals(reasonCode, "routing-unavailable", StringComparison.OrdinalIgnoreCase)) summary.RoutingUnavailableCount++;
            }

            return summary;
        }

        private void MergeProposalDiagnosticsSummary(PlannerProposalDiagnosticsSummary target, PlannerProposalDiagnosticsSummary source)
        {
            if (target == null || source == null) return;

            foreach (var key in source.ReasonCounts.Keys)
            {
                var value = source.ReasonCounts[key];
                if (target.ReasonCounts.ContainsKey(key)) target.ReasonCounts[key] += value;
                else target.ReasonCounts[key] = value;
            }

            foreach (var value in source.ChosenVenues) target.ChosenVenues.Add(value);
            foreach (var value in source.AlternateVenues) target.AlternateVenues.Add(value);
            foreach (var value in source.ExecutionModes) target.ExecutionModes.Add(value);

            target.PolicyHealthBlockedCount += source.PolicyHealthBlockedCount;
            target.RegimeBlockedCount += source.RegimeBlockedCount;
            target.CircuitBreakerObservedCount += source.CircuitBreakerObservedCount;
            target.RoutingUnavailableCount += source.RoutingUnavailableCount;
        }

        private string FormatProposalDiagnosticsSummary(PlannerProposalDiagnosticsSummary summary)
        {
            if (summary == null) return "routing=n/a | venueHealth policy=0 regime=0 circuit=0 route-unavail=0";

            var route = summary.ChosenVenues.Count > 0 ? string.Join(",", summary.ChosenVenues.Take(2)) : "n/a";
            var alt = summary.AlternateVenues.Count > 0 ? string.Join(",", summary.AlternateVenues.Take(2)) : "n/a";
            var exec = summary.ExecutionModes.Count > 0 ? string.Join(",", summary.ExecutionModes.Take(2)) : "n/a";

            var topReason = "none";
            if (summary.ReasonCounts.Count > 0)
            {
                var pair = summary.ReasonCounts.OrderByDescending(kv => kv.Value).First();
                topReason = pair.Key + "=" + pair.Value;
            }

            return "routing chosen=" + route + " alt=" + alt + " exec=" + exec
                + " | venueHealth policy=" + summary.PolicyHealthBlockedCount + " regime=" + summary.RegimeBlockedCount + " circuit=" + summary.CircuitBreakerObservedCount + " route-unavail=" + summary.RoutingUnavailableCount
                + " | reject=" + topReason;
        }

        private void IncrementReasonCount(PlannerProposalDiagnosticsSummary summary, string reasonCode)
        {
            if (summary == null || string.IsNullOrWhiteSpace(reasonCode)) return;

            var key = reasonCode.Trim();
            if (summary.ReasonCounts.ContainsKey(key)) summary.ReasonCounts[key]++;
            else summary.ReasonCounts[key] = 1;
        }

        private string ExtractBracketTagValue(string text, string key)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(key)) return string.Empty;

            var token = "[" + key + "=";
            var start = text.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return string.Empty;

            start += token.Length;
            var end = text.IndexOf(']', start);
            if (end <= start) return string.Empty;

            return text.Substring(start, end - start).Trim();
        }
    }
}
