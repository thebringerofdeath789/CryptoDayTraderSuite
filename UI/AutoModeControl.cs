using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using CryptoDayTraderSuite.Brokers;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Properties;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.UI
{
	public partial class AutoModeControl : UserControl
	{
		private sealed class ExecutionOutcome
		{
			public int OkCount;

			public int FailCount;

			public int SkippedCooldown;

			public int SkippedOpenCap;

			public int SkippedRisk;

			public int SkippedValidation;

			public string ToSummary()
			{
				return $"ok={OkCount}, fail={FailCount}, cooldown={SkippedCooldown}, openCap={SkippedOpenCap}, riskStop={SkippedRisk}, validation={SkippedValidation}";
			}
		}

		private sealed class PaperOpenPosition
		{
			public string AccountId;

			public string Symbol;

			public string Strategy;

			public int Direction;

			public decimal Qty;

			public decimal Entry;

			public decimal Stop;

			public decimal Target;

			public DateTime OpenedUtc;

			public string ScopeKey;
		}

		private sealed class ProfileCycleTelemetry
		{
			public string ProfileId;

			public string ProfileName;

			public string AccountId;

			public string AccountLabel;

			public string Service;

			public string Mode;

			public string PairScope;

			public int ExpectedSymbolCount;

			public int SymbolCount;

			public int ScanRowCount;

			public int ProposedCount;

			public int MaxTradesPerCycle;

			public int CooldownMinutes;

			public decimal DailyRiskStopPct;

			public string Status;

			public string Reason;

			public int Executed;

			public int Failed;

			public int SkippedCooldown;

			public int SkippedOpenCap;

			public int SkippedRisk;

			public int SkippedValidation;

			public int NoSignalCount;

			public int AiVetoCount;

			public int BiasBlockedCount;

			public string GuardrailScopeKey;

			public decimal DailyRiskUsedAfter;

			public DateTime StartedUtc;

			public DateTime EndedUtc;
		}

		private sealed class AutoCycleTelemetry
		{
			public string CycleId;

			public DateTime StartedUtc;

			public DateTime EndedUtc;

			public int EnabledProfileCount;

			public int ProcessedProfileCount;

			public int ExecutedProfiles;

			public int FailedProfiles;

			public string Summary;

			public bool MatrixHasSelectedScopeProfile;

			public bool MatrixHasAllScopeProfile;

			public bool MatrixPairConfigurationConsistent;

			public bool MatrixHasGuardrailValues;

			public bool MatrixGuardrailScopesIsolated;

			public bool MatrixIndependentGuardrailsObserved;

			public bool MatrixFailureContainmentObserved;

			public bool MatrixFailureDoesNotHaltCycle;

			public bool MatrixIsolationObserved;

			public bool MatrixMinimumProfileCoverage;

			public string MatrixCoverageNote;

			public string MatrixStatus;

			public bool GateNoSignalObserved;

			public bool GateAiVetoObserved;

			public bool GateRiskVetoObserved;

			public bool GateSuccessObserved;

			public string GateStatus;

			public int CycleErrorCount;

			public string CycleErrorMessage;

			public string RoutingChosenVenues;

			public string RoutingAlternateVenues;

			public string RoutingExecutionModes;

			public int RoutingUnavailableCount;

			public int PolicyHealthBlockedCount;

			public int RegimeBlockedCount;

			public int CircuitBreakerObservedCount;

			public Dictionary<string, int> RejectReasonCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

			public List<ProfileCycleTelemetry> Profiles = new List<ProfileCycleTelemetry>();
		}

		private sealed class ProposalBatchResult
		{
			public List<TradePlan> Plans = new List<TradePlan>();

			public Dictionary<string, int> ReasonCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

			public List<string> ChosenVenues = new List<string>();

			public List<string> AlternateVenues = new List<string>();

			public List<string> ExecutionModes = new List<string>();

			public int RoutingUnavailableCount;

			public int PolicyHealthBlockedCount;

			public int RegimeBlockedCount;

			public int CircuitBreakerObservedCount;
		}

		private sealed class AutoProfileComboItem
		{
			public string Id;

			public string Name;

			public override string ToString()
			{
				return Name ?? "(unnamed profile)";
			}
		}

		private static readonly string[] PopularPairs = new string[10] { "BTC-USD", "ETH-USD", "SOL-USD", "XRP-USD", "ADA-USD", "DOGE-USD", "AVAX-USD", "LINK-USD", "LTC-USD", "BCH-USD" };

		private static readonly string[] CertificationRejectCategories = new string[6] { "fees-kill", "slippage-kill", "routing-unavailable", "no-signal", "ai-veto", "bias-blocked" };

		private const int DefaultAutoAllPairsMaxSymbols = 12;

		private const int AutoScanPerSymbolDelayMs = 120;

		private List<ProjectionRow> _last;

		private List<AccountInfo> _accounts;

		private List<TradePlan> _queued;

		private Label _lblPairs;

		private CheckedListBox _lstPairs;

		private FlowLayoutPanel _pairActions;

		private Button _btnPairsAll;

		private Button _btnPairsTop5;

		private Button _btnPairsClear;

		private AutoPlannerService _planner;

		private IExchangeClient _client;

		private IAccountService _accountService;

		private IKeyService _keyService;

		private AccountBuyingPowerService _buyingPowerService;

		private IHistoryService _historyService;

		private IAutoModeProfileService _autoModeProfileService;

		private List<AutoModeProfile> _autoProfiles = new List<AutoModeProfile>();

		private bool _isApplyingProfile;

		private Timer _autoTimer;

		private bool _autoCycleRunning;

		private bool _autoStopRequested;

		private bool _suppressAutoRunToggleHandler;

		private bool _restoreAutoRunOnInitialize;

		private readonly Dictionary<string, DateTime> _symbolCooldownUtc = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

		private readonly Dictionary<string, int> _sessionOpenPositionsByAccount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		private readonly Dictionary<string, DateTime> _profileLastRunUtc = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

		private List<string> _productUniverseCache = new List<string>();

		private DateTime _dailyRiskDateUtc = DateTime.UtcNow.Date;

		private readonly Dictionary<string, decimal> _dailyRiskUsedByScope = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

		private readonly List<PaperOpenPosition> _paperOpenPositions = new List<PaperOpenPosition>();

		private readonly Dictionary<string, DateTime> _paperPositionPlanRefreshUtc = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

		private string _lastRoutingSummaryText = "Routing: n/a";

		private string _lastVenueHealthSummaryText = "Venue Health: n/a";


		public AutoModeControl()
		{
			InitializeComponent();
			Dock = DockStyle.Fill;
			BuildPairSelector();
			if (btnScan != null)
			{
				btnScan.Click += delegate
				{
					DoScan();
				};
			}
			if (btnPropose != null)
			{
				btnPropose.Click += delegate
				{
					DoPropose();
				};
			}
			if (btnExecute != null)
			{
				btnExecute.Click += delegate
				{
					DoExecute();
				};
			}
			if (chkAutoRun != null)
			{
				chkAutoRun.CheckedChanged += async delegate
				{
					await OnAutoRunChangedAsync();
				};
			}
			if (btnKillSwitch != null)
			{
				btnKillSwitch.Click += delegate
				{
					StopAutoRun("Kill switch pressed");
				};
			}
			if (numAutoInterval != null)
			{
				numAutoInterval.ValueChanged += delegate
				{
					UpdateTimerInterval();
				};
			}
			if (btnProfileSave != null)
			{
				btnProfileSave.Click += delegate
				{
					SaveCurrentProfile();
				};
			}
			if (btnProfileDelete != null)
			{
				btnProfileDelete.Click += delegate
				{
					DeleteCurrentProfile();
				};
			}
			if (cmbProfile != null)
			{
				cmbProfile.SelectedIndexChanged += delegate
				{
					ApplySelectedProfile();
				};
			}
			if (cmbAccount != null)
			{
				cmbAccount.SelectedIndexChanged += delegate
				{
					UpdateSelectedProfileSummary();
				};
			}
			if (numProfileInterval != null)
			{
				numProfileInterval.ValueChanged += delegate
				{
					UpdateSelectedProfileSummary();
				};
			}
			if (chkProfileEnabled != null)
			{
				chkProfileEnabled.CheckedChanged += delegate
				{
					UpdateSelectedProfileSummary();
				};
			}
			if (chkProfileAllPairs != null)
			{
				chkProfileAllPairs.CheckedChanged += delegate
				{
					if (!_isApplyingProfile)
					{
						if (chkProfileAllPairs.Checked)
						{
							SelectAllPairs();
						}
						UpdateSelectedProfileSummary();
					}
				};
			}
			if (cmbGran != null && cmbGran.Items.Count > 0 && cmbGran.SelectedIndex < 0)
			{
				cmbGran.SelectedIndex = 2;
			}
			_restoreAutoRunOnInitialize = LoadAutoRunPreference();
			if (chkAutoRun != null)
			{
				_suppressAutoRunToggleHandler = true;
				try
				{
					chkAutoRun.Checked = false;
				}
				finally
				{
					_suppressAutoRunToggleHandler = false;
				}
			}
			if (numAutoInterval != null && numAutoInterval.Value < 1m)
			{
				numAutoInterval.Value = 5m;
			}
			if (numProfileInterval != null && numProfileInterval.Value < 1m)
			{
				numProfileInterval.Value = 5m;
			}
			if (numMaxTradesPerCycle != null && numMaxTradesPerCycle.Value < 1m)
			{
				numMaxTradesPerCycle.Value = 3m;
			}
			if (numCooldownMinutes != null && numCooldownMinutes.Value < 1m)
			{
				numCooldownMinutes.Value = 30m;
			}
			if (numDailyRiskStopPct != null && numDailyRiskStopPct.Value < 1m)
			{
				numDailyRiskStopPct.Value = 3m;
			}
			InitializeAutoTimer();
			UpdateAutoStatus("Auto is OFF");
			UpdateSelectedProfileSummary();
			RefreshLatestTelemetrySummary();
			base.Disposed += delegate
			{
				if (_autoTimer != null)
				{
					_autoTimer.Stop();
					_autoTimer.Dispose();
				}
			};
		}

		public void Initialize(AutoPlannerService planner, IExchangeClient client, IAccountService accountService, IKeyService keyService)
		{
			Initialize(planner, client, accountService, keyService, null, null);
		}

		public void Initialize(AutoPlannerService planner, IExchangeClient client, IAccountService accountService, IKeyService keyService, IAutoModeProfileService autoModeProfileService)
		{
			Initialize(planner, client, accountService, keyService, autoModeProfileService, null);
		}

		public void Initialize(AutoPlannerService planner, IExchangeClient client, IAccountService accountService, IKeyService keyService, IAutoModeProfileService autoModeProfileService, IHistoryService historyService)
		{
			_planner = planner;
			_client = client;
			_accountService = accountService;
			_keyService = keyService;
			_buyingPowerService = _keyService == null ? null : new AccountBuyingPowerService(_keyService);
			_autoModeProfileService = autoModeProfileService;
			_historyService = historyService;
			if (_client != null)
			{
				LoadProducts();
			}
			LoadAccounts();
			LoadAutoProfiles();
			RestoreAutoRunPreferenceIfNeeded();
		}

		private void LoadAccounts()
		{
			if (_accountService == null)
			{
				return;
			}
			_accounts = (from accountInfo in _accountService.GetAll()
				where accountInfo.Enabled
				select accountInfo).ToList();
			if (cmbAccount == null)
			{
				return;
			}
			cmbAccount.Items.Clear();
			foreach (AccountInfo a in _accounts)
			{
				cmbAccount.Items.Add(a.Label + " [" + a.Service + "]");
			}
			if (cmbAccount.Items.Count > 0)
			{
				cmbAccount.SelectedIndex = 0;
			}
			if (!_isApplyingProfile)
			{
				LoadAutoProfiles();
			}
		}

		private async void LoadProducts()
		{
			try
			{
				List<string> prods = ((_client == null) ? (await new CoinbasePublicClient().GetProductsAsync()) : (await _client.ListProductsAsync()));
				List<string> rankedPairs = RankPairs(prods);
				if (rankedPairs == null || rankedPairs.Count == 0)
				{
					rankedPairs = PopularPairs.ToList();
					Log.Warn("[AutoMode] Product list was empty; using fallback popular pairs.", "LoadProducts", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 298);
				}
				_productUniverseCache = rankedPairs.Where((string value) => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
				if (cmbProduct == null)
				{
					return;
				}
				cmbProduct.Items.Clear();
				foreach (string p in rankedPairs)
				{
					cmbProduct.Items.Add(p);
				}
				if (cmbProduct.Items.Count > 0)
				{
					cmbProduct.SelectedIndex = 0;
				}
				PopulatePairList(rankedPairs);
				ApplySelectedProfilePairScope();
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Log.Error("[AutoMode] Failed to load products", ex2, "LoadProducts", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 319);
				if (_productUniverseCache == null || _productUniverseCache.Count == 0)
				{
					_productUniverseCache = PopularPairs.ToList();
				}
				if (!base.IsDisposed)
				{
					MessageBox.Show("Failed to load products: " + ex2.Message, "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
			}
		}

		private async void DoScan()
		{
			await RunScanAsync(interactive: true, allowAutoPropose: true);
		}

		private async Task<List<ProjectionRow>> RunScanAsync(bool interactive, bool allowAutoPropose)
		{
			List<string> symbols = GetSelectedPairs();
			if (symbols.Count == 0)
			{
				UpdateAutoStatus("Scan skipped: no pairs selected", warn: true);
				return new List<ProjectionRow>();
			}
			if (!int.TryParse(cmbGran.SelectedItem?.ToString(), out var gran))
			{
				gran = 60;
			}
			if (btnScan != null)
			{
				btnScan.Enabled = false;
			}
			try
			{
				if (_planner == null)
				{
					throw new InvalidOperationException("AutoPlannerService not initialized");
				}
				List<ProjectionRow> rows = (_last = await ScanSymbolsAsync(symbols, gran, (int)numLookback.Value));
				grid.DataSource = rows;
				if (rows.Count == 0)
				{
					UpdateAutoStatus("Scan complete: no candidate rows", warn: true);
				}
				else
				{
					UpdateAutoStatus($"Scan complete: {rows.Count} candidate row(s) across {symbols.Count} pair(s)");
					if (allowAutoPropose && chkAutoPropose != null && chkAutoPropose.Checked)
					{
						await ProposeBestAsync(fromAutoScan: true, interactive);
					}
				}
				return rows;
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Log.Error("[AutoMode] Scan error", ex2, "RunScanAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 371);
				if (interactive)
				{
					MessageBox.Show("Scan error: " + ex2.Message);
				}
				UpdateAutoStatus("Scan error: " + ex2.Message, warn: true);
				return new List<ProjectionRow>();
			}
			finally
			{
				if (!base.IsDisposed)
				{
					btnScan.Enabled = true;
				}
			}
		}

		private async void DoPropose()
		{
			try
			{
				await ProposeBestAsync(fromAutoScan: false, interactive: true);
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Log.Error("[AutoMode] DoPropose unhandled error", ex2, "DoPropose", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 390);
				if (!base.IsDisposed)
				{
					MessageBox.Show("Proposal error: " + ex2.Message, "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
			}
		}

		private async Task ProposeBestAsync(bool fromAutoScan, bool interactive)
		{
			if (_last == null || _last.Count == 0 || cmbAccount == null || cmbAccount.SelectedIndex < 0)
			{
				return;
			}
			if (_accounts == null || _accounts.Count == 0)
			{
				if (interactive && !fromAutoScan && !base.IsDisposed)
				{
					MessageBox.Show("No enabled accounts available. Configure an account in Settings.", "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
				UpdateAutoStatus("Propose skipped: no enabled accounts", warn: true);
			}
			else
			{
				if (cmbAccount.SelectedIndex >= _accounts.Count)
				{
					return;
				}
				AccountInfo acc = _accounts[cmbAccount.SelectedIndex];
				if (!int.TryParse(cmbGran.SelectedItem?.ToString(), out var gran))
				{
					gran = 60;
				}
				decimal manualEquity = numEquity == null ? 1000m : numEquity.Value;
				decimal resolvedEquity = await ResolveEquityForAccountAsync(acc, manualEquity, "manual propose");
				ProposalBatchResult proposeBatch = await ProposeForAccountAsync(acc, _last, gran, resolvedEquity);
				List<TradePlan> plans = (_queued = ((proposeBatch != null) ? (proposeBatch.Plans ?? new List<TradePlan>()) : new List<TradePlan>()));
				int scanGroupsCount = (from r in _last
					where !string.IsNullOrWhiteSpace(r.Symbol)
					select r.Symbol).Distinct(StringComparer.OrdinalIgnoreCase).Count();
				int proposedSymbols = (from p in plans
					where !string.IsNullOrWhiteSpace(p.Symbol)
					select p.Symbol).Distinct(StringComparer.OrdinalIgnoreCase).Count();
				if (_queued.Count > 0)
				{
					SetComboProduct(_queued[0].Symbol);
					string msg = string.Join(Environment.NewLine, _queued.Select((TradePlan p) => $"{p.Symbol} | {p.Strategy} {p.Direction} {p.Qty} @ {p.Entry} (Note: {p.Note})"));
					string summary = $"Proposed {_queued.Count} trade(s) across {proposedSymbols}/{scanGroupsCount} scanned symbol(s).";
					UpdateAutoStatus(summary);
					Log.Info("[AutoMode] " + summary, "ProposeBestAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 440);
					Log.Info("[AutoMode] Proposed plans detail: " + msg.Replace(Environment.NewLine, " | "), "ProposeBestAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 441);
				}
				else
				{
					UpdateAutoStatus($"No valid trades proposed across {scanGroupsCount} symbol(s)", warn: true);
				}
			}
		}

		private async Task<ProposalBatchResult> ProposeForAccountAsync(AccountInfo acc, List<ProjectionRow> rows, int gran, decimal equity)
		{
			ProposalBatchResult batch = new ProposalBatchResult();
			if (acc == null || rows == null || rows.Count == 0)
			{
				return batch;
			}
			if (_planner == null)
			{
				throw new InvalidOperationException("AutoPlannerService not initialized");
			}
			var scanGroups = (from g in rows.Where((ProjectionRow r) => !string.IsNullOrWhiteSpace(r.Symbol)).GroupBy((ProjectionRow r) => r.Symbol, StringComparer.OrdinalIgnoreCase)
				select new
				{
					Symbol = g.Key,
					Rows = g.OrderByDescending((ProjectionRow r) => r.Expectancy).ToList(),
					TopExpectancy = g.Max((ProjectionRow r) => r.Expectancy)
				} into g
				orderby g.TopExpectancy descending
				select g).ToList();
			if (scanGroups.Count == 0)
			{
				return batch;
			}
			List<TradePlan> allPlans = new List<TradePlan>();
			foreach (var group in scanGroups)
			{
				AutoPlannerService.ProposalDiagnostics diag = await _planner.ProposeWithDiagnosticsAsync(acc.Id, group.Symbol, gran, equity, acc.RiskPerTradePct, group.Rows);
				List<TradePlan> plans = ((diag != null) ? (diag.Plans ?? new List<TradePlan>()) : new List<TradePlan>());
				if (plans.Count > 0)
				{
					allPlans.AddRange(plans);
				}
				string reason = ((diag == null || string.IsNullOrWhiteSpace(diag.ReasonCode)) ? "unknown" : diag.ReasonCode);
				if (!batch.ReasonCounts.ContainsKey(reason))
				{
					batch.ReasonCounts[reason] = 0;
				}
				batch.ReasonCounts[reason] = batch.ReasonCounts[reason] + 1;
			}
			batch.Plans = allPlans;
			return batch;
		}

		private Dictionary<string, int> CreateRejectCategoryCounter()
		{
			Dictionary<string, int> counter = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			foreach (string category in CertificationRejectCategories)
			{
				counter[category] = 0;
			}
			return counter;
		}

		private void AccumulateRejectCategoryCounts(Dictionary<string, int> target, Dictionary<string, int> source)
		{
			if (target == null || source == null)
			{
				return;
			}
			foreach (string category in CertificationRejectCategories)
			{
				if (!source.TryGetValue(category, out var count) || count <= 0)
				{
					continue;
				}
				if (!target.ContainsKey(category))
				{
					target[category] = 0;
				}
				target[category] += count;
			}
		}

		private string FormatObservedRejectCategoryCounts(Dictionary<string, int> counts)
		{
			if (counts == null)
			{
				return string.Empty;
			}

			List<string> observed = new List<string>();
			foreach (string category in CertificationRejectCategories)
			{
				if (!counts.TryGetValue(category, out var count) || count <= 0)
				{
					continue;
				}
				observed.Add(category + "=" + count);
			}

			return string.Join(", ", observed);
		}

		private async void DoExecute()
		{
			await ExecuteQueuedAsync(interactive: true, fromAutoCycle: false);
		}

		private async Task ExecuteQueuedAsync(bool interactive, bool fromAutoCycle)
		{
			if (_queued == null || _queued.Count == 0)
			{
				UpdateAutoStatus("Execute skipped: nothing queued", warn: true);
				return;
			}
			if (cmbAccount == null || cmbAccount.SelectedIndex < 0 || _accounts == null || cmbAccount.SelectedIndex >= _accounts.Count)
			{
				UpdateAutoStatus("Execute skipped: no account selected", warn: true);
				return;
			}
			if (_keyService == null)
			{
				if (interactive)
				{
					MessageBox.Show("Key service unavailable.", "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
				UpdateAutoStatus("Execute failed: key service unavailable", warn: true);
				return;
			}
			AccountInfo acc = _accounts[cmbAccount.SelectedIndex];
			if (acc.Mode != AccountMode.Paper && (chkLiveArm == null || !chkLiveArm.Checked))
			{
				UpdateAutoStatus("Live account selected but Live Arm is OFF; execution skipped", warn: true);
				if (interactive)
				{
					MessageBox.Show("Live account requires 'Live Arm' enabled for non-interactive execution.", "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
				return;
			}
			if (acc.Mode != AccountMode.Paper)
			{
				string keyId = acc.KeyEntryId;
				if (string.IsNullOrEmpty(keyId) || _keyService.Get(keyId) == null)
				{
					if (interactive)
					{
						MessageBox.Show("No valid API key for selected account.", "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					}
					UpdateAutoStatus("Execute failed: missing API key", warn: true);
					return;
				}
			}
			IBroker broker = BrokerFactory.GetBroker(acc.Service, acc.Mode, _keyService, _accountService);
			if (broker == null)
			{
				if (interactive)
				{
					MessageBox.Show("Unsupported broker: " + acc.Service, "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
				UpdateAutoStatus("Execute failed: unsupported broker", warn: true);
				return;
			}
			string manualCapabilityBlock = GetCapabilityBlockReason(acc, broker);
			if (!string.IsNullOrWhiteSpace(manualCapabilityBlock))
			{
				if (interactive)
				{
					MessageBox.Show("Execution blocked: " + manualCapabilityBlock, "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
				UpdateAutoStatus("Execute blocked: " + manualCapabilityBlock, warn: true);
				return;
			}
			if (btnExecute != null)
			{
				btnExecute.Enabled = false;
			}
			try
			{
				decimal manualEquity = numEquity == null ? 1000m : numEquity.Value;
				decimal resolvedEquity = await ResolveEquityForAccountAsync(acc, manualEquity, "manual execute");
				string summary = (await ExecutePlansForAccountAsync(acc, broker, _queued.Where((TradePlan p) => p.AccountId == acc.Id).ToList(), resolvedEquity, (numMaxTradesPerCycle != null) ? ((int)numMaxTradesPerCycle.Value) : 3, (numCooldownMinutes != null) ? ((int)numCooldownMinutes.Value) : 30, (numDailyRiskStopPct != null) ? numDailyRiskStopPct.Value : 3m, interactive, fromAutoCycle)).ToSummary();
				UpdateAutoStatus(summary, summary.IndexOf("fail=0", StringComparison.OrdinalIgnoreCase) < 0);
				Log.Info("[AutoMode] " + summary, "ExecuteQueuedAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 565);
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Log.Error("[AutoMode] Execution error", ex2, "ExecuteQueuedAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 569);
				UpdateAutoStatus("Execution error: " + ex2.Message, warn: true);
				if (interactive)
				{
					MessageBox.Show("Execution error: " + ex2.Message, "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
			}
			finally
			{
				if (!base.IsDisposed)
				{
					btnExecute.Enabled = true;
				}
			}
		}

		private void InitializeAutoTimer()
		{
			_autoTimer = new Timer();
			_autoTimer.Interval = GetAutoIntervalMs();
			_autoTimer.Tick += async delegate
			{
				await RunAutoCycleAsync();
			};
		}

		private async Task OnAutoRunChangedAsync()
		{
			if (chkAutoRun != null && !_suppressAutoRunToggleHandler)
			{
				SaveAutoRunPreference(chkAutoRun.Checked);
				if (chkAutoRun.Checked)
				{
					_autoStopRequested = false;
					UpdateTimerInterval();
					_autoTimer.Start();
					UpdateAutoStatus($"Auto is ON (every {GetAutoIntervalMinutes()} min)");
					Log.Info("[AutoMode] Auto-run enabled.", "OnAutoRunChangedAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 600);
					await RunAutoCycleAsync();
				}
				else
				{
					StopAutoRun("Auto run disabled");
				}
			}
		}

		private void StopAutoRun(string reason)
		{
			_autoStopRequested = true;
			bool timerWasRunning = _autoTimer != null && _autoTimer.Enabled;
			bool checkWasOn = chkAutoRun != null && chkAutoRun.Checked;
			if (_autoTimer != null)
			{
				_autoTimer.Stop();
			}
			if (chkAutoRun != null && chkAutoRun.Checked)
			{
				_suppressAutoRunToggleHandler = true;
				try
				{
					chkAutoRun.Checked = false;
				}
				finally
				{
					_suppressAutoRunToggleHandler = false;
				}
			}
			UpdateAutoStatus("Auto is OFF: " + reason, warn: true);
			if (timerWasRunning || checkWasOn || _autoCycleRunning)
			{
				Log.Warn("[AutoMode] " + reason, "StopAutoRun", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 633);
			}
			else
			{
				Log.Info("[AutoMode] Auto already OFF: " + reason, "StopAutoRun", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 637);
			}
			SaveAutoRunPreference(enabled: false);
		}

		private bool LoadAutoRunPreference()
		{
			try
			{
				return Settings.Default.AutoModeAutoRunEnabled;
			}
			catch (Exception ex)
			{
				Log.Warn("[AutoMode] Failed to load Auto Run preference: " + ex.Message, "LoadAutoRunPreference", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 651);
				return false;
			}
		}

		private void SaveAutoRunPreference(bool enabled)
		{
			try
			{
				Settings.Default.AutoModeAutoRunEnabled = enabled;
				Settings.Default.Save();
			}
			catch (Exception ex)
			{
				Log.Warn("[AutoMode] Failed to save Auto Run preference: " + ex.Message, "SaveAutoRunPreference", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 665);
			}
		}

		private void RestoreAutoRunPreferenceIfNeeded()
		{
			if (_restoreAutoRunOnInitialize && chkAutoRun != null && !chkAutoRun.Checked)
			{
				_restoreAutoRunOnInitialize = false;
				chkAutoRun.Checked = true;
				Log.Info("[AutoMode] Restored sticky Auto Run preference (ON).", "RestoreAutoRunPreferenceIfNeeded", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 676);
			}
		}

		private async Task RunAutoCycleAsync()
		{
			if (_autoCycleRunning)
			{
				return;
			}
			_autoCycleRunning = true;
			AutoCycleTelemetry cycleTelemetry = null;
			try
			{
				UpdateAutoStatus("Auto cycle running...");
				List<AutoModeProfile> enabledProfiles = (_autoProfiles ?? new List<AutoModeProfile>()).Where((AutoModeProfile p) => p.Enabled && !string.IsNullOrWhiteSpace(p.AccountId)).ToList();
				await EnsureProductUniverseAsync();
				if (enabledProfiles.Count == 0)
				{
					List<ProjectionRow> rows = await RunScanAsync(interactive: false, allowAutoPropose: false);
					if (_autoStopRequested)
					{
						UpdateAutoStatus("Auto cycle stopped by kill switch", warn: true);
						return;
					}
					if (rows == null || rows.Count == 0)
					{
						UpdateAutoStatus("Auto cycle complete: no scan candidates", warn: true);
						return;
					}
					await ProposeBestAsync(fromAutoScan: true, interactive: false);
					if (_autoStopRequested)
					{
						UpdateAutoStatus("Auto cycle stopped by kill switch", warn: true);
					}
					else
					{
						await ExecuteQueuedAsync(interactive: false, fromAutoCycle: true);
					}
					return;
				}
				List<string> cycleStats = new List<string>();
				Dictionary<string, int> cycleRejectReasonCounts = CreateRejectCategoryCounter();
				cycleTelemetry = new AutoCycleTelemetry
				{
					CycleId = Guid.NewGuid().ToString("N"),
					StartedUtc = DateTime.UtcNow,
					EnabledProfileCount = enabledProfiles.Count
				};
				bool anyExecuted = false;
				foreach (AutoModeProfile profile in enabledProfiles)
				{
					if (_autoStopRequested)
					{
						cycleStats.Add("stopped(kill-switch)");
						break;
					}
					DateTime profileStartedUtc = DateTime.UtcNow;
					ProfileCycleTelemetry profileTelemetry = new ProfileCycleTelemetry
					{
						ProfileId = profile.ProfileId,
						ProfileName = profile.Name,
						AccountId = profile.AccountId,
						GuardrailScopeKey = "profile:" + profile.ProfileId,
						StartedUtc = profileStartedUtc
					};
					try
					{
						if (!ShouldRunProfile(profile))
						{
							cycleStats.Add(profile.Name + ": skipped(interval)");
							profileTelemetry.Status = "skipped";
							profileTelemetry.Reason = "interval";
							profileTelemetry.EndedUtc = DateTime.UtcNow;
							cycleTelemetry.Profiles.Add(profileTelemetry);
							continue;
						}
						AccountInfo account = (_accounts ?? new List<AccountInfo>()).FirstOrDefault((AccountInfo a) => string.Equals(a.Id, profile.AccountId, StringComparison.OrdinalIgnoreCase) && a.Enabled);
						if (account == null)
						{
							cycleStats.Add(profile.Name + ": skipped(account)");
							profileTelemetry.Status = "skipped";
							profileTelemetry.Reason = "account";
							profileTelemetry.EndedUtc = DateTime.UtcNow;
							cycleTelemetry.Profiles.Add(profileTelemetry);
							continue;
						}
						profileTelemetry.AccountLabel = account.Label;
						profileTelemetry.Service = account.Service;
						profileTelemetry.Mode = account.Mode.ToString();
						profileTelemetry.PairScope = profile.PairScope;
						profileTelemetry.MaxTradesPerCycle = profile.MaxTradesPerCycle;
						profileTelemetry.CooldownMinutes = profile.CooldownMinutes;
						profileTelemetry.DailyRiskStopPct = profile.DailyRiskStopPct;
						List<string> symbols = ApplyAutoCycleSymbolLimits(symbols: ResolveProfileSymbols(profile), profile: profile);
						profileTelemetry.ExpectedSymbolCount = (string.Equals(profile.PairScope, "All", StringComparison.OrdinalIgnoreCase) ? (-1) : (profile.SelectedPairs ?? new List<string>()).Where((string s) => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).Count());
						if (profileTelemetry.ExpectedSymbolCount <= 0 && !string.Equals(profile.PairScope, "All", StringComparison.OrdinalIgnoreCase))
						{
							profileTelemetry.ExpectedSymbolCount = symbols.Count;
						}
						profileTelemetry.SymbolCount = symbols.Count;
						if (symbols.Count == 0)
						{
							cycleStats.Add(profile.Name + ": skipped(pairs)");
							profileTelemetry.Status = "skipped";
							profileTelemetry.Reason = "pairs";
							Log.Warn("[AutoMode] Profile skipped due to empty symbol universe: " + (profile.Name ?? "(unnamed)"), "RunAutoCycleAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 793);
							profileTelemetry.EndedUtc = DateTime.UtcNow;
							cycleTelemetry.Profiles.Add(profileTelemetry);
							continue;
						}
						int gran = ParseGranularityMinutes();
						int lookbackDays = ((numLookback != null) ? ((int)numLookback.Value) : 30);
						List<ProjectionRow> rows2 = await ScanSymbolsAsync(symbols, gran, lookbackDays);
						profileTelemetry.ScanRowCount = rows2.Count;
						if (rows2.Count == 0)
						{
							cycleStats.Add(profile.Name + ": no-scan-rows");
							MarkProfileRun(profile);
							profileTelemetry.Status = "skipped";
							profileTelemetry.Reason = "no-scan-rows";
							profileTelemetry.EndedUtc = DateTime.UtcNow;
							cycleTelemetry.Profiles.Add(profileTelemetry);
							continue;
						}
						decimal profileManualEquity = numEquity == null ? 1000m : numEquity.Value;
						decimal profileResolvedEquity = await ResolveEquityForAccountAsync(account, profileManualEquity, "auto cycle propose");
						ProposalBatchResult proposeBatch = await ProposeForAccountAsync(account, rows2, gran, profileResolvedEquity);
						List<TradePlan> plans = ((proposeBatch != null) ? (proposeBatch.Plans ?? new List<TradePlan>()) : new List<TradePlan>());
						int noSignalCount = 0;
						int aiVetoCount = 0;
						int biasBlockedCount = 0;
						if (proposeBatch != null && proposeBatch.ReasonCounts != null)
						{
							proposeBatch.ReasonCounts.TryGetValue("no-signal", out noSignalCount);
							proposeBatch.ReasonCounts.TryGetValue("ai-veto", out aiVetoCount);
							proposeBatch.ReasonCounts.TryGetValue("bias-blocked", out biasBlockedCount);
							AccumulateRejectCategoryCounts(cycleRejectReasonCounts, proposeBatch.ReasonCounts);
						}
						profileTelemetry.ProposedCount = plans.Count;
						profileTelemetry.NoSignalCount = noSignalCount;
						profileTelemetry.AiVetoCount = aiVetoCount;
						profileTelemetry.BiasBlockedCount = biasBlockedCount;
						if (plans.Count == 0)
						{
							cycleStats.Add(profile.Name + ": no-plans");
							MarkProfileRun(profile);
							profileTelemetry.Status = "skipped";
							profileTelemetry.Reason = "no-plans";
							profileTelemetry.EndedUtc = DateTime.UtcNow;
							cycleTelemetry.Profiles.Add(profileTelemetry);
							continue;
						}
						IBroker broker = BrokerFactory.GetBroker(account.Service, account.Mode, _keyService, _accountService);
						if (broker == null)
						{
							cycleStats.Add(profile.Name + ": broker-unsupported");
							MarkProfileRun(profile);
							profileTelemetry.Status = "blocked";
							profileTelemetry.Reason = "broker-unsupported";
							profileTelemetry.EndedUtc = DateTime.UtcNow;
							cycleTelemetry.Profiles.Add(profileTelemetry);
							continue;
						}
						string profileCapabilityBlock = GetCapabilityBlockReason(account, broker);
						if (!string.IsNullOrWhiteSpace(profileCapabilityBlock))
						{
							cycleStats.Add(profile.Name + ": blocked(" + profileCapabilityBlock + ")");
							MarkProfileRun(profile);
							profileTelemetry.Status = "blocked";
							profileTelemetry.Reason = profileCapabilityBlock;
							profileTelemetry.EndedUtc = DateTime.UtcNow;
							cycleTelemetry.Profiles.Add(profileTelemetry);
							continue;
						}
						if (account.Mode != AccountMode.Paper && (chkLiveArm == null || !chkLiveArm.Checked))
						{
							cycleStats.Add(profile.Name + ": live-not-armed");
							MarkProfileRun(profile);
							profileTelemetry.Status = "blocked";
							profileTelemetry.Reason = "live-not-armed";
							profileTelemetry.EndedUtc = DateTime.UtcNow;
							cycleTelemetry.Profiles.Add(profileTelemetry);
							continue;
						}
						string profileGuardrailScopeKey = profileTelemetry.GuardrailScopeKey;
						ExecutionOutcome execOutcome = await ExecutePlansForAccountAsync(account, broker, plans, profileResolvedEquity, profile.MaxTradesPerCycle, profile.CooldownMinutes, profile.DailyRiskStopPct, interactive: false, fromAutoCycle: true, profileGuardrailScopeKey);
						string execSummary = execOutcome.ToSummary();
						profileTelemetry.Executed = execOutcome.OkCount;
						profileTelemetry.Failed = execOutcome.FailCount;
						profileTelemetry.SkippedCooldown = execOutcome.SkippedCooldown;
						profileTelemetry.SkippedOpenCap = execOutcome.SkippedOpenCap;
						profileTelemetry.SkippedRisk = execOutcome.SkippedRisk;
						profileTelemetry.SkippedValidation = execOutcome.SkippedValidation;
						profileTelemetry.DailyRiskUsedAfter = GetDailyRiskUsed(profileGuardrailScopeKey);
						profileTelemetry.Status = ((execOutcome.OkCount > 0) ? "executed" : "completed");
						profileTelemetry.Reason = "ok";
						profileTelemetry.EndedUtc = DateTime.UtcNow;
						cycleTelemetry.Profiles.Add(profileTelemetry);
						if (proposeBatch != null && proposeBatch.ReasonCounts != null)
						{
							string observedProfileRejects = FormatObservedRejectCategoryCounts(proposeBatch.ReasonCounts);
							if (!string.IsNullOrWhiteSpace(observedProfileRejects))
							{
								Log.Info("[AutoMode][RejectEvidence] profile=" + (profile.Name ?? profile.ProfileId ?? "(unknown)") + " observed=" + observedProfileRejects, "RunAutoCycleAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 911);
							}
						}
						if (execOutcome.OkCount > 0)
						{
							anyExecuted = true;
						}
						cycleStats.Add(profile.Name + ": " + execSummary);
						MarkProfileRun(profile);
					}
					catch (Exception ex)
					{
						profileTelemetry.Status = "error";
						profileTelemetry.Reason = ex.Message;
						profileTelemetry.EndedUtc = DateTime.UtcNow;
						cycleTelemetry.Profiles.Add(profileTelemetry);
						cycleStats.Add(profile.Name + ": error(" + ex.Message + ")");
						Log.Error("[AutoMode] Profile cycle error: " + (profile.ProfileId ?? "(no-id)"), ex, "RunAutoCycleAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 918);
						MarkProfileRun(profile);
					}
				}
				string summary = "Auto cycle complete: " + string.Join(" | ", cycleStats);
				UpdateAutoStatus(summary, !anyExecuted);
				Log.Info("[AutoMode] " + summary, "RunAutoCycleAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 925);
				cycleTelemetry.RejectReasonCounts = cycleRejectReasonCounts;
				string observedCycleRejects = FormatObservedRejectCategoryCounts(cycleRejectReasonCounts);
				if (!string.IsNullOrWhiteSpace(observedCycleRejects))
				{
					Log.Info("[AutoMode][RejectEvidence] cycle=" + (cycleTelemetry.CycleId ?? "(unknown)") + " observed=" + observedCycleRejects, "RunAutoCycleAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 929);
				}
				cycleTelemetry.EndedUtc = DateTime.UtcNow;
				cycleTelemetry.Summary = summary;
				cycleTelemetry.ProcessedProfileCount = cycleTelemetry.Profiles.Count;
				cycleTelemetry.ExecutedProfiles = cycleTelemetry.Profiles.Count((ProfileCycleTelemetry p) => string.Equals(p.Status, "executed", StringComparison.OrdinalIgnoreCase));
				cycleTelemetry.FailedProfiles = cycleTelemetry.Profiles.Count((ProfileCycleTelemetry p) => string.Equals(p.Status, "blocked", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "error", StringComparison.OrdinalIgnoreCase));
				EvaluateReliabilityGates(cycleTelemetry);
				EvaluateMatrixStatus(cycleTelemetry);
				WriteCycleTelemetry(cycleTelemetry);
			}
			catch (Exception ex2)
			{
				Exception ex3 = ex2;
				Log.Error("[AutoMode] Auto cycle error", ex3, "RunAutoCycleAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 940);
				UpdateAutoStatus("Auto cycle error: " + ex3.Message, warn: true);
				if (cycleTelemetry != null)
				{
					cycleTelemetry.CycleErrorCount = 1;
					cycleTelemetry.CycleErrorMessage = ex3.Message;
					cycleTelemetry.EndedUtc = DateTime.UtcNow;
					cycleTelemetry.ProcessedProfileCount = (cycleTelemetry.Profiles ?? new List<ProfileCycleTelemetry>()).Count;
					cycleTelemetry.ExecutedProfiles = (cycleTelemetry.Profiles ?? new List<ProfileCycleTelemetry>()).Count((ProfileCycleTelemetry p) => string.Equals(p.Status, "executed", StringComparison.OrdinalIgnoreCase));
					cycleTelemetry.FailedProfiles = (cycleTelemetry.Profiles ?? new List<ProfileCycleTelemetry>()).Count((ProfileCycleTelemetry p) => string.Equals(p.Status, "blocked", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "error", StringComparison.OrdinalIgnoreCase));
					cycleTelemetry.Summary = "Auto cycle failed: " + ex3.Message;
					EvaluateReliabilityGates(cycleTelemetry);
					EvaluateMatrixStatus(cycleTelemetry);
					WriteCycleTelemetry(cycleTelemetry);
				}
			}
			finally
			{
				_autoCycleRunning = false;
			}
		}

		private void UpdateTimerInterval()
		{
			if (_autoTimer != null)
			{
				_autoTimer.Interval = GetAutoIntervalMs();
			}
		}

		private int GetAutoIntervalMinutes()
		{
			if (numAutoInterval == null)
			{
				return 5;
			}
			int minutes = (int)numAutoInterval.Value;
			return (minutes < 1) ? 1 : minutes;
		}

		private int GetAutoIntervalMs()
		{
			return GetAutoIntervalMinutes() * 60 * 1000;
		}

		private void UpdateAutoStatus(string message, bool warn = false)
		{
			if (lblAutoStatus != null)
			{
				lblAutoStatus.Text = "Status: " + message;
				lblAutoStatus.ForeColor = (warn ? Color.DarkOrange : Color.DarkGreen);
			}
		}

		private void ResetDailyRiskIfNeeded()
		{
			DateTime utcDate = DateTime.UtcNow.Date;
			if (_dailyRiskDateUtc != utcDate)
			{
				_dailyRiskDateUtc = utcDate;
				_dailyRiskUsedByScope.Clear();
			}
		}

		private bool IsOnCooldown(string scopeKey, string symbol, int cooldownMinutes)
		{
			if (string.IsNullOrWhiteSpace(symbol) || cooldownMinutes <= 0)
			{
				return false;
			}
			string key = BuildCooldownKey(scopeKey, symbol);
			if (!_symbolCooldownUtc.TryGetValue(key, out var lastUtc))
			{
				return false;
			}
			return DateTime.UtcNow - lastUtc < TimeSpan.FromMinutes(cooldownMinutes);
		}

		private void MarkCooldown(string scopeKey, string symbol)
		{
			if (!string.IsNullOrWhiteSpace(symbol))
			{
				string key = BuildCooldownKey(scopeKey, symbol);
				_symbolCooldownUtc[key] = DateTime.UtcNow;
			}
		}

		private string BuildCooldownKey(string scopeKey, string symbol)
		{
			return NormalizeGuardrailScopeKey(scopeKey, null) + "|" + (symbol ?? string.Empty).Trim().ToUpperInvariant();
		}

		private string NormalizeGuardrailScopeKey(string scopeKey, AccountInfo account)
		{
			if (!string.IsNullOrWhiteSpace(scopeKey))
			{
				return scopeKey.Trim();
			}
			if (account != null && !string.IsNullOrWhiteSpace(account.Id))
			{
				return "account:" + account.Id;
			}
			return "global";
		}

		private decimal GetDailyRiskUsed(string scopeKey)
		{
			if (string.IsNullOrWhiteSpace(scopeKey))
			{
				return 0m;
			}
			decimal used;
			return _dailyRiskUsedByScope.TryGetValue(scopeKey, out used) ? used : 0m;
		}

		private void SetDailyRiskUsed(string scopeKey, decimal value)
		{
			if (!string.IsNullOrWhiteSpace(scopeKey))
			{
				_dailyRiskUsedByScope[scopeKey] = ((value < 0m) ? 0m : value);
			}
		}

		private int GetSessionOpenCount(string accountId)
		{
			if (string.IsNullOrWhiteSpace(accountId))
			{
				return 0;
			}
			int count;
			return _sessionOpenPositionsByAccount.TryGetValue(accountId, out count) ? count : 0;
		}

		private int GetPersistedOpenCount(AccountInfo acc)
		{
			if (acc == null || _historyService == null)
			{
				return 0;
			}
			try
			{
				List<TradeRecord> trades = _historyService.LoadTrades() ?? new List<TradeRecord>();
				if (trades.Count == 0)
				{
					return 0;
				}
				DateTime cutoffUtc = DateTime.UtcNow.AddHours(-GetPersistedOpenLookbackHours());
				List<TradeRecord> exchangeTrades = trades.Where((TradeRecord t) => t != null && t.Executed && t.AtUtc >= cutoffUtc && string.Equals(t.Exchange, acc.Service, StringComparison.OrdinalIgnoreCase)).ToList();
				if (exchangeTrades.Count == 0)
				{
					return 0;
				}
				string accountTag = "acct:" + (acc.Id ?? string.Empty);
				IEnumerable<TradeRecord> accountScoped = exchangeTrades.Where((TradeRecord t) => !string.IsNullOrWhiteSpace(t.Notes) && t.Notes.IndexOf(accountTag, StringComparison.OrdinalIgnoreCase) >= 0);
				int taggedForThisAccount = ComputeOpenCountFromTradeSet(accountScoped);
				if (taggedForThisAccount > 0)
				{
					return taggedForThisAccount;
				}
				if (exchangeTrades.Any((TradeRecord t) => !string.IsNullOrWhiteSpace(t.Notes) && t.Notes.IndexOf("acct:", StringComparison.OrdinalIgnoreCase) >= 0))
				{
					return 0;
				}
				return ComputeOpenCountFromTradeSet(exchangeTrades);
			}
			catch (Exception ex)
			{
				Log.Warn("[AutoMode] Failed to load persisted open count: " + ex.Message, "GetPersistedOpenCount", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1082);
				return 0;
			}
		}

		private int GetPersistedOpenLookbackHours()
		{
			string raw = Environment.GetEnvironmentVariable("CDTS_AUTOMODE_PERSISTED_OPEN_LOOKBACK_HOURS");
			if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw.Trim(), out var parsed) && parsed >= 1 && parsed <= 168)
			{
				return parsed;
			}
			return 24;
		}

		private int ComputeOpenCountFromTradeSet(IEnumerable<TradeRecord> trades)
		{
			List<TradeRecord> set = (trades ?? new List<TradeRecord>()).Where((TradeRecord t) => t?.Executed ?? false).ToList();
			if (set.Count == 0)
			{
				return 0;
			}
			int opens = set.Count((TradeRecord t) => !t.PnL.HasValue);
			int closes = set.Count((TradeRecord t) => t.PnL.HasValue && !string.IsNullOrWhiteSpace(t.Notes) && t.Notes.IndexOf("[close:", StringComparison.OrdinalIgnoreCase) >= 0);
			int openCount = opens - closes;
			return (openCount >= 0) ? openCount : 0;
		}

		private async Task EvaluatePaperProtectiveExitsForAccountAsync(AccountInfo account, IBroker broker, string scopeKey)
		{
			if (account == null || broker == null || _historyService == null)
			{
				return;
			}
			List<PaperOpenPosition> positions = _paperOpenPositions.Where((PaperOpenPosition p) => p != null && string.Equals(p.AccountId, account.Id, StringComparison.OrdinalIgnoreCase)).ToList();
			foreach (PaperOpenPosition pos in positions)
			{
				try
				{
					string symbolDash = (pos.Symbol ?? string.Empty).Replace("/", "-");
					if (string.IsNullOrWhiteSpace(symbolDash))
					{
						continue;
					}
					decimal last = ((_client == null) ? (await new CoinbasePublicClient().GetTickerMidAsync(symbolDash)) : ((await _client.GetTickerAsync(symbolDash))?.Last ?? 0m));
					if (last <= 0m)
					{
						continue;
					}
					bool isLong = pos.Direction > 0;
					bool targetHit = (isLong ? (last >= pos.Target) : (last <= pos.Target));
					bool stopHit = (isLong ? (last <= pos.Stop) : (last >= pos.Stop));
					if (targetHit || stopHit)
					{
						decimal pnl = (isLong ? ((last - pos.Entry) * pos.Qty) : ((pos.Entry - last) * pos.Qty));
						string closeReason = (targetHit ? "target" : "stop");
						TradePlan closePlan = new TradePlan
						{
							AccountId = account.Id,
							Symbol = pos.Symbol,
							Strategy = pos.Strategy,
							Direction = ((!isLong) ? 1 : (-1)),
							Entry = last,
							Stop = pos.Stop,
							Target = pos.Target,
							Qty = pos.Qty,
							Note = $"Protective exit [{closeReason}] [close:{symbolDash}]"
						};
						(bool ok, string message) closeResult = await broker.PlaceOrderAsync(closePlan);
						if (!closeResult.ok)
						{
							Log.Warn("[AutoMode] Protective close failed for " + symbolDash + ": " + closeResult.message, "EvaluatePaperProtectiveExitsForAccountAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1155);
							continue;
						}
						_historyService.SaveTrade(new TradeRecord
						{
							Exchange = account.Service,
							ProductId = symbolDash,
							AtUtc = DateTime.UtcNow,
							Strategy = pos.Strategy,
							Side = (isLong ? "Sell" : "Buy"),
							Quantity = pos.Qty,
							Price = last,
							EstEdge = 0m,
							Executed = true,
							FillPrice = last,
							PnL = pnl,
							Notes = $"Protective exit [{closeReason}] [close:{symbolDash}] [acct:{account.Id ?? string.Empty}] [scope:{scopeKey ?? pos.ScopeKey ?? string.Empty}]",
							Enabled = true
						});
						_paperOpenPositions.Remove(pos);
						SetSessionOpenCount(account.Id, Math.Max(0, GetSessionOpenCount(account.Id) - 1));
						Log.Info($"[AutoMode] Local protective exit {closeReason} for {symbolDash} @ {last:0.########} pnl={pnl:0.00}", "EvaluatePaperProtectiveExitsForAccountAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1183);
					}
				}
				catch (Exception ex)
				{
					Log.Warn("[AutoMode] Paper protective exit check failed: " + ex.Message, "EvaluatePaperProtectiveExitsForAccountAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1187);
				}
			}
		}

		private int GetPaperPositionReplanMinutes()
		{
			string raw = Environment.GetEnvironmentVariable("CDTS_AUTOMODE_POSITION_REPLAN_MINUTES");
			if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw.Trim(), out var parsed) && parsed > 0)
			{
				return parsed;
			}
			return 3;
		}

		private string BuildPaperPositionRefreshKey(PaperOpenPosition pos)
		{
			if (pos == null)
			{
				return string.Empty;
			}
			string account = (pos.AccountId ?? string.Empty).Trim();
			string symbol = ((pos.Symbol ?? string.Empty).Replace("/", "-")).Trim();
			string direction = pos.Direction.ToString();
			return account + "|" + symbol + "|" + direction;
		}

		private bool ShouldRefreshPaperPositionPlan(PaperOpenPosition pos)
		{
			string key = BuildPaperPositionRefreshKey(pos);
			if (string.IsNullOrWhiteSpace(key))
			{
				return false;
			}
			int minMinutes = GetPaperPositionReplanMinutes();
			if (minMinutes <= 0)
			{
				return true;
			}
			if (!_paperPositionPlanRefreshUtc.TryGetValue(key, out var lastUtc))
			{
				return true;
			}
			return DateTime.UtcNow - lastUtc >= TimeSpan.FromMinutes(minMinutes);
		}

		private void MarkPaperPositionRefreshed(PaperOpenPosition pos)
		{
			string key = BuildPaperPositionRefreshKey(pos);
			if (!string.IsNullOrWhiteSpace(key))
			{
				_paperPositionPlanRefreshUtc[key] = DateTime.UtcNow;
			}
		}

		private async Task RefreshPaperPositionPlansForAccountAsync(AccountInfo account, string scopeKey)
		{
			if (account == null || _planner == null)
			{
				return;
			}
			List<PaperOpenPosition> positions = _paperOpenPositions.Where((PaperOpenPosition p) => p != null && string.Equals(p.AccountId, account.Id, StringComparison.OrdinalIgnoreCase)).ToList();
			if (positions.Count == 0)
			{
				return;
			}
			int gran = ParseGranularityMinutes();
			int lookbackMins = ((numLookback != null) ? ((int)numLookback.Value) : 30) * 1440;
			decimal manualEquity = (numEquity == null) ? 1000m : numEquity.Value;
			decimal equity = await ResolveEquityForAccountAsync(account, manualEquity, "paper-position-replan");
			foreach (PaperOpenPosition pos in positions)
			{
				if (_autoStopRequested)
				{
					break;
				}
				if (!ShouldRefreshPaperPositionPlan(pos))
				{
					continue;
				}
				try
				{
					string symbolDash = (pos.Symbol ?? string.Empty).Replace("/", "-");
					if (string.IsNullOrWhiteSpace(symbolDash))
					{
						continue;
					}
					List<ProjectionRow> rows = await _planner.ProjectAsync(symbolDash, gran, lookbackMins, 0.006m, 0.004m);
					if (rows == null || rows.Count == 0)
					{
						MarkPaperPositionRefreshed(pos);
						continue;
					}
					AutoPlannerService.ProposalDiagnostics diag = await _planner.ProposeWithDiagnosticsAsync(account.Id, symbolDash, gran, equity, account.RiskPerTradePct, rows);
					List<TradePlan> plans = ((diag != null) ? (diag.Plans ?? new List<TradePlan>()) : new List<TradePlan>());
					TradePlan refreshPlan = plans.FirstOrDefault((TradePlan p) => p != null && p.Direction == pos.Direction);
					if (refreshPlan == null)
					{
						MarkPaperPositionRefreshed(pos);
						continue;
					}
					decimal oldStop = pos.Stop;
					decimal oldTarget = pos.Target;
					bool isLong = pos.Direction > 0;
					if (isLong)
					{
						if (refreshPlan.Stop > pos.Stop)
						{
							pos.Stop = refreshPlan.Stop;
						}
						if (refreshPlan.Target > pos.Target)
						{
							pos.Target = refreshPlan.Target;
						}
					}
					else
					{
						if (refreshPlan.Stop < pos.Stop)
						{
							pos.Stop = refreshPlan.Stop;
						}
						if (refreshPlan.Target < pos.Target)
						{
							pos.Target = refreshPlan.Target;
						}
					}
					MarkPaperPositionRefreshed(pos);
					if (oldStop != pos.Stop || oldTarget != pos.Target)
					{
						if (_historyService != null)
						{
							_historyService.SaveTrade(new TradeRecord
							{
								Exchange = account.Service,
								ProductId = symbolDash,
								AtUtc = DateTime.UtcNow,
								Strategy = pos.Strategy,
								Side = (isLong ? "Buy" : "Sell"),
								Quantity = pos.Qty,
								Price = pos.Entry,
								EstEdge = 0m,
								Executed = true,
								FillPrice = null,
								PnL = null,
								Notes = string.Format("Adaptive position refresh [acct:{0}] [scope:{1}] [stop:{2:0.########}->{3:0.########}] [target:{4:0.########}->{5:0.########}]", account.Id ?? string.Empty, scopeKey ?? pos.ScopeKey ?? string.Empty, oldStop, pos.Stop, oldTarget, pos.Target),
								Enabled = true
							});
						}
						Log.Info(string.Format("[AutoMode] Adaptive refresh for {0}: stop {1:0.########}->{2:0.########}, target {3:0.########}->{4:0.########}", symbolDash, oldStop, pos.Stop, oldTarget, pos.Target), "RefreshPaperPositionPlansForAccountAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1310);
					}
				}
				catch (Exception ex)
				{
					Log.Warn("[AutoMode] Paper position refresh failed: " + ex.Message, "RefreshPaperPositionPlansForAccountAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1315);
				}
			}
		}

		private bool UpsertPaperOpenPosition(AccountInfo account, TradePlan plan, string scopeKey)
		{
			if (account == null || plan == null)
			{
				return false;
			}
			PaperOpenPosition existing = _paperOpenPositions.FirstOrDefault((PaperOpenPosition p) => p != null
				&& string.Equals(p.AccountId, account.Id, StringComparison.OrdinalIgnoreCase)
				&& string.Equals((p.Symbol ?? string.Empty).Replace("/", "-"), (plan.Symbol ?? string.Empty).Replace("/", "-"), StringComparison.OrdinalIgnoreCase)
				&& p.Direction == plan.Direction);
			if (existing == null)
			{
				_paperOpenPositions.Add(new PaperOpenPosition
				{
					AccountId = account.Id,
					Symbol = plan.Symbol,
					Strategy = plan.Strategy,
					Direction = plan.Direction,
					Qty = plan.Qty,
					Entry = plan.Entry,
					Stop = plan.Stop,
					Target = plan.Target,
					OpenedUtc = DateTime.UtcNow,
					ScopeKey = scopeKey
				});
				return true;
			}
			decimal existingQty = existing.Qty;
			decimal incomingQty = plan.Qty;
			decimal newQty = existingQty + incomingQty;
			if (newQty > 0m)
			{
				existing.Entry = ((existing.Entry * existingQty) + (plan.Entry * incomingQty)) / newQty;
				existing.Qty = newQty;
			}
			if (plan.Direction > 0)
			{
				existing.Stop = Math.Max(existing.Stop, plan.Stop);
				existing.Target = Math.Max(existing.Target, plan.Target);
			}
			else
			{
				existing.Stop = Math.Min(existing.Stop, plan.Stop);
				existing.Target = Math.Min(existing.Target, plan.Target);
			}
			existing.Strategy = string.IsNullOrWhiteSpace(plan.Strategy) ? existing.Strategy : plan.Strategy;
			existing.ScopeKey = scopeKey;
			Log.Info(string.Format("[AutoMode] Merged open position {0}: qty {1:0.########}+{2:0.########}={3:0.########}", (plan.Symbol ?? string.Empty).Replace("/", "-"), existingQty, incomingQty, existing.Qty), "UpsertPaperOpenPosition", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1360);
			return false;
		}

		private void SetSessionOpenCount(string accountId, int count)
		{
			if (!string.IsNullOrWhiteSpace(accountId))
			{
				_sessionOpenPositionsByAccount[accountId] = Math.Max(0, count);
			}
		}

		private async Task<List<ProjectionRow>> ScanSymbolsAsync(List<string> symbols, int granMinutes, int lookbackDays)
		{
			List<ProjectionRow> result = new List<ProjectionRow>();
			if (_planner == null || symbols == null || symbols.Count == 0)
			{
				return result;
			}
			int lookbackMins = lookbackDays * 1440;
			int scanDelayMs = AutoScanPerSymbolDelayMs;
			foreach (string symbol in symbols)
			{
				try
				{
					List<ProjectionRow> symbolRows = await _planner.ProjectAsync(symbol, granMinutes, lookbackMins, 0.006m, 0.004m);
					if (symbolRows != null && symbolRows.Count > 0)
					{
						result.AddRange(symbolRows);
					}
					if (scanDelayMs > AutoScanPerSymbolDelayMs)
					{
						scanDelayMs = Math.Max(AutoScanPerSymbolDelayMs, scanDelayMs - 120);
					}
				}
				catch (Exception ex)
				{
					Log.Warn("[AutoMode] Scan symbol failed: " + symbol + " | " + ex.Message, "ScanSymbolsAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1216);
					if (IsRateLimitError(ex))
					{
						scanDelayMs = Math.Min(3000, scanDelayMs + 600);
						Log.Warn("[AutoMode] Rate-limit backoff increased to " + scanDelayMs + "ms after 429.", "ScanSymbolsAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1219);
					}
				}
				await Task.Delay(scanDelayMs);
			}
			return result.OrderByDescending((ProjectionRow r) => r.Expectancy).ToList();
		}

		private bool IsRateLimitError(Exception ex)
		{
			if (ex == null || string.IsNullOrWhiteSpace(ex.Message))
			{
				return false;
			}
			string msg = ex.Message.ToLowerInvariant();
			return msg.Contains("429") || msg.Contains("too many requests");
		}

		private List<string> ApplyAutoCycleSymbolLimits(AutoModeProfile profile, List<string> symbols)
		{
			List<string> ordered = (symbols ?? new List<string>()).Where((string s) => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			if (ordered.Count == 0)
			{
				return ordered;
			}
			if (profile == null || !string.Equals((profile.PairScope ?? string.Empty).Trim(), "All", StringComparison.OrdinalIgnoreCase))
			{
				return ordered;
			}
			int maxAllScopeSymbols = GetAutoAllScopeMaxSymbols();
			if (maxAllScopeSymbols <= 0 || ordered.Count <= maxAllScopeSymbols)
			{
				return ordered;
			}
			List<string> limited = ordered.Take(maxAllScopeSymbols).ToList();
			Log.Warn(string.Format("[AutoMode] Limiting All-scope symbols for profile '{0}' from {1} to {2} to reduce public API rate-limit pressure.", profile.Name ?? profile.ProfileId ?? "(unknown)", ordered.Count, limited.Count), "ApplyAutoCycleSymbolLimits", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1252);
			return limited;
		}

		private int GetAutoAllScopeMaxSymbols()
		{
			string raw = Environment.GetEnvironmentVariable("CDTS_AUTOMODE_MAX_SYMBOLS");
			if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw.Trim(), out var parsed) && parsed > 0)
			{
				return parsed;
			}
			return 12;
		}

		private async Task<ExecutionOutcome> ExecutePlansForAccountAsync(AccountInfo acc, IBroker broker, List<TradePlan> plans, decimal equity, int maxTradesPerCycle, int cooldownMinutes, decimal dailyRiskStopPct, bool interactive, bool fromAutoCycle, string guardrailScopeKey = null)
		{
			ResetDailyRiskIfNeeded();
			await RefreshPaperPositionPlansForAccountAsync(acc, guardrailScopeKey);
			await EvaluatePaperProtectiveExitsForAccountAsync(acc, broker, guardrailScopeKey);

			ExecutionOutcome outcome = new ExecutionOutcome();
			string scopeKey = NormalizeGuardrailScopeKey(guardrailScopeKey, acc);

			if (maxTradesPerCycle < 1) maxTradesPerCycle = 1;
			if (cooldownMinutes < 1) cooldownMinutes = 1;
			if (dailyRiskStopPct < 0.1m) dailyRiskStopPct = 0.1m;

			decimal dailyRiskCap = equity * (dailyRiskStopPct / 100m);
			decimal dailyRiskUsed = GetDailyRiskUsed(scopeKey);

			int sessionOpen = GetSessionOpenCount(acc.Id);
			int persistedOpen = GetPersistedOpenCount(acc);
			int openCount = Math.Max(sessionOpen, persistedOpen);

			foreach (TradePlan p in plans ?? new List<TradePlan>())
			{
				if (_autoStopRequested) break;
				if (p == null || p.AccountId != acc.Id) continue;
				if (outcome.OkCount >= maxTradesPerCycle) break;

				string normalizedSymbol = (p.Symbol ?? string.Empty).Replace("/", "-");
				if (cooldownMinutes > 0 && IsOnCooldown(scopeKey, normalizedSymbol, cooldownMinutes))
				{
					outcome.SkippedCooldown++;
					continue;
				}

				if (acc.MaxConcurrentTrades > 0 && openCount >= acc.MaxConcurrentTrades)
				{
					outcome.SkippedOpenCap++;
					continue;
				}

				decimal planRisk = Math.Abs(p.Entry - p.Stop) * p.Qty;
				if (planRisk < 0m) planRisk = 0m;

				if (dailyRiskCap > 0m && dailyRiskUsed + planRisk > dailyRiskCap)
				{
					outcome.SkippedRisk++;
					continue;
				}

				(bool ok, string message) validation = await broker.ValidateTradePlanAsync(p);
				if (!validation.ok)
				{
					outcome.SkippedValidation++;
					Log.Warn("[AutoMode] Plan blocked by broker validation: " + validation.message + " | symbol=" + p.Symbol + " | account=" + acc.Id, "ExecutePlansForAccountAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1336);
					continue;
				}

				(bool ok, string message) r = await broker.PlaceOrderAsync(p);
				if (r.ok)
				{
					outcome.OkCount++;
					if (planRisk > 0m)
					{
						dailyRiskUsed += planRisk;
						SetDailyRiskUsed(scopeKey, dailyRiskUsed);
					}

					MarkCooldown(scopeKey, normalizedSymbol);
					bool createdNewPosition = UpsertPaperOpenPosition(acc, p, scopeKey);
					if (createdNewPosition) openCount++;

					if (interactive)
					{
						Log.Info("[AutoMode] Execute ok: " + r.message, "ExecutePlansForAccountAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1352);
					}
				}
				else
				{
					outcome.FailCount++;
					if (interactive && !fromAutoCycle)
					{
						Log.Warn("[AutoMode] Execute err: " + r.message, "ExecutePlansForAccountAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1372);
					}
				}

				if (_historyService != null)
				{
					try
					{
						string planSymbol = (p.Symbol ?? string.Empty).Replace("/", "-");
						TradeRecord rec = new TradeRecord
						{
							Exchange = acc.Service,
							ProductId = planSymbol,
							AtUtc = DateTime.UtcNow,
							Strategy = p.Strategy,
							Side = ((p.Direction > 0) ? "Buy" : "Sell"),
							Quantity = p.Qty,
							Price = p.Entry,
							EstEdge = ((!(p.Entry > 0m)) ? 0m : ((p.Direction > 0) ? ((p.Target - p.Entry) / p.Entry) : ((p.Entry - p.Target) / p.Entry))),
							Executed = r.ok,
							FillPrice = (r.ok ? new decimal?(p.Entry) : ((decimal?)null)),
							PnL = null,
							Notes = string.Format("{0} [acct:{1}] [mode:{2}] [scope:{3}] [result:{4}]", p.Note ?? string.Empty, acc.Id ?? string.Empty, acc.Mode, scopeKey, r.ok ? "ok" : ("err:" + (r.message ?? string.Empty))),
							Enabled = true
						};
						_historyService.SaveTrade(rec);
					}
					catch (Exception ex)
					{
						Exception ex2 = ex;
						Log.Warn("[AutoMode] Failed to persist execution record: " + ex2.Message, "ExecutePlansForAccountAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1405);
					}
				}
			}

			SetSessionOpenCount(acc.Id, openCount);
			return outcome;
		}

		private async Task<decimal> ResolveEquityForAccountAsync(AccountInfo account, decimal manualFallback, string scope)
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
				Log.Info("[AutoMode] " + scope + " using live buying power " + resolution.EquityToUse.ToString("0.########") + " " + (resolution.QuoteCurrency ?? "USD") + " for account " + (account.Label ?? account.Id));
				return resolution.EquityToUse;
			}

			Log.Warn("[AutoMode] " + scope + " using manual equity fallback " + manualFallback.ToString("0.########") + " (" + (resolution.Note ?? "no reason") + ")");
			return manualFallback;
		}

		private int ParseGranularityMinutes()
		{
			if (cmbGran == null)
			{
				return 60;
			}
			if (!int.TryParse(cmbGran.SelectedItem?.ToString(), out var gran))
			{
				gran = 60;
			}
			return gran;
		}

		private bool ShouldRunProfile(AutoModeProfile profile)
		{
			if (profile == null)
			{
				return false;
			}
			if (!_profileLastRunUtc.TryGetValue(profile.ProfileId, out var lastRun))
			{
				return true;
			}
			int interval = ((profile.IntervalMinutes < 1) ? 1 : profile.IntervalMinutes);
			return DateTime.UtcNow - lastRun >= TimeSpan.FromMinutes(interval);
		}

		private void MarkProfileRun(AutoModeProfile profile)
		{
			if (profile != null && !string.IsNullOrWhiteSpace(profile.ProfileId))
			{
				_profileLastRunUtc[profile.ProfileId] = DateTime.UtcNow;
			}
		}

		private List<string> ResolveProfileSymbols(AutoModeProfile profile)
		{
			if (profile == null)
			{
				return new List<string>();
			}
			string scope = (profile.PairScope ?? string.Empty).Trim();
			if (string.Equals(scope, "All", StringComparison.OrdinalIgnoreCase) || string.Equals(scope, "AllPairs", StringComparison.OrdinalIgnoreCase) || string.Equals(scope, "Any", StringComparison.OrdinalIgnoreCase))
			{
				return GetRuntimeProductUniverse();
			}
			List<string> selected = (profile.SelectedPairs ?? new List<string>()).Where((string s) => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			if (selected.Count == 0)
			{
				selected = GetSelectedPairs();
			}
			if (selected.Count == 0)
			{
				selected = GetRuntimeProductUniverse();
				if (selected.Count > 0)
				{
					Log.Warn("[AutoMode] Profile selected scope resolved zero symbols; falling back to runtime universe for " + (profile.Name ?? "(unnamed)"), "ResolveProfileSymbols", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1462);
				}
			}
			return selected;
		}

		private async Task EnsureProductUniverseAsync()
		{
			if (_productUniverseCache != null && _productUniverseCache.Count > 0)
			{
				return;
			}
			try
			{
				Task<List<string>> fetchTask = ((_client != null) ? _client.ListProductsAsync() : new CoinbasePublicClient().GetProductsAsync());
				if (await Task.WhenAny(fetchTask, Task.Delay(TimeSpan.FromSeconds(3.0))) != fetchTask)
				{
					throw new TimeoutException("Timed out loading product universe.");
				}
				List<string> ranked = RankPairs(await fetchTask);
				if (ranked == null || ranked.Count == 0)
				{
					ranked = PopularPairs.ToList();
				}
				_productUniverseCache = ranked.Where((string p) => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			}
			catch (Exception ex)
			{
				if (_productUniverseCache == null || _productUniverseCache.Count == 0)
				{
					_productUniverseCache = PopularPairs.ToList();
				}
				Log.Warn("[AutoMode] Failed to refresh runtime product universe: " + ex.Message, "EnsureProductUniverseAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1508);
			}
		}

		private List<string> GetRuntimeProductUniverse()
		{
			List<string> all = new List<string>();
			if (_lstPairs != null)
			{
				foreach (object item in _lstPairs.Items)
				{
					string pair = item as string;
					if (!string.IsNullOrWhiteSpace(pair))
					{
						all.Add(pair);
					}
				}
			}
			if (all.Count == 0 && cmbProduct != null && cmbProduct.Items.Count > 0)
			{
				foreach (object item2 in cmbProduct.Items)
				{
					string pair2 = item2 as string;
					if (!string.IsNullOrWhiteSpace(pair2))
					{
						all.Add(pair2);
					}
				}
			}
			if (all.Count == 0 && _productUniverseCache != null && _productUniverseCache.Count > 0)
			{
				all.AddRange(_productUniverseCache);
			}
			if (all.Count == 0)
			{
				all.AddRange(PopularPairs);
			}
			return all.Where((string p) => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}

		private void BuildPairSelector()
		{
			if (topPanel == null || _lstPairs != null)
			{
				return;
			}
			_lblPairs = new Label
			{
				Text = "Pairs:",
				AutoSize = true,
				Margin = new Padding(12, 6, 0, 0)
			};
			_lstPairs = new CheckedListBox
			{
				Name = "lstPairs",
				CheckOnClick = true,
				Width = 180,
				Height = 90,
				SelectionMode = SelectionMode.One,
				Margin = new Padding(6, 3, 8, 3)
			};
			_lstPairs.ItemCheck += delegate(object s, ItemCheckEventArgs e)
			{
				if (e.NewValue == CheckState.Checked)
				{
					string text = _lstPairs.Items[e.Index] as string;
					if (!string.IsNullOrWhiteSpace(text))
					{
						SetComboProduct(text);
					}
				}
			};
			_pairActions = new FlowLayoutPanel
			{
				AutoSize = true,
				WrapContents = false,
				Margin = new Padding(0, 3, 8, 3)
			};
			_btnPairsAll = new Button
			{
				Text = "All",
				Width = 52,
				Height = 24,
				Margin = new Padding(0, 0, 4, 0)
			};
			_btnPairsAll.Click += delegate
			{
				SelectAllPairs();
			};
			_btnPairsTop5 = new Button
			{
				Text = "Top 5",
				Width = 60,
				Height = 24,
				Margin = new Padding(0, 0, 4, 0)
			};
			_btnPairsTop5.Click += delegate
			{
				SelectTopPopularPairs(5);
			};
			_btnPairsClear = new Button
			{
				Text = "Clear",
				Width = 58,
				Height = 24,
				Margin = new Padding(0, 0, 0, 0)
			};
			_btnPairsClear.Click += delegate
			{
				ClearPairSelection();
			};
			_pairActions.Controls.Add(_btnPairsAll);
			_pairActions.Controls.Add(_btnPairsTop5);
			_pairActions.Controls.Add(_btnPairsClear);
			topPanel.Controls.Add(_lblPairs);
			topPanel.Controls.Add(_lstPairs);
			topPanel.Controls.Add(_pairActions);
		}

		private void LoadAutoProfiles(string preferProfileId = null)
		{
			if (_autoModeProfileService == null)
			{
				_autoProfiles = new List<AutoModeProfile>();
				if (cmbProfile != null)
				{
					cmbProfile.Items.Clear();
					cmbProfile.Enabled = false;
				}
				if (btnProfileSave != null)
				{
					btnProfileSave.Enabled = false;
				}
				if (btnProfileDelete != null)
				{
					btnProfileDelete.Enabled = false;
				}
				if (chkProfileAllPairs != null)
				{
					chkProfileAllPairs.Enabled = false;
				}
				if (chkProfileEnabled != null)
				{
					chkProfileEnabled.Enabled = false;
				}
				if (numProfileInterval != null)
				{
					numProfileInterval.Enabled = false;
				}
				UpdateSelectedProfileSummary();
				return;
			}
			_autoProfiles = _autoModeProfileService.GetAll().OrderBy((AutoModeProfile p) => p.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase).ToList();
			if (cmbProfile == null)
			{
				return;
			}
			string currentId = preferProfileId ?? GetSelectedProfileId();
			_isApplyingProfile = true;
			try
			{
				cmbProfile.Items.Clear();
				foreach (AutoModeProfile profile in _autoProfiles)
				{
					cmbProfile.Items.Add(new AutoProfileComboItem
					{
						Id = profile.ProfileId,
						Name = profile.Name
					});
				}
				cmbProfile.Enabled = true;
				if (btnProfileSave != null)
				{
					btnProfileSave.Enabled = true;
				}
				if (btnProfileDelete != null)
				{
					btnProfileDelete.Enabled = cmbProfile.Items.Count > 0;
				}
				if (chkProfileAllPairs != null)
				{
					chkProfileAllPairs.Enabled = true;
				}
				if (chkProfileEnabled != null)
				{
					chkProfileEnabled.Enabled = true;
				}
				if (numProfileInterval != null)
				{
					numProfileInterval.Enabled = true;
				}
				if (cmbProfile.Items.Count == 0)
				{
					UpdateSelectedProfileSummary();
					return;
				}
				int idx = 0;
				if (!string.IsNullOrWhiteSpace(currentId))
				{
					for (int i = 0; i < cmbProfile.Items.Count; i++)
					{
						if (cmbProfile.Items[i] is AutoProfileComboItem item && string.Equals(item.Id, currentId, StringComparison.OrdinalIgnoreCase))
						{
							idx = i;
							break;
						}
					}
				}
				cmbProfile.SelectedIndex = idx;
			}
			finally
			{
				_isApplyingProfile = false;
			}
			ApplySelectedProfile();
			UpdateSelectedProfileSummary();
		}

		private string GetSelectedProfileId()
		{
			if (cmbProfile == null)
			{
				return null;
			}
			return (!(cmbProfile.SelectedItem is AutoProfileComboItem item)) ? null : item.Id;
		}

		private AutoModeProfile GetSelectedProfile()
		{
			string id = GetSelectedProfileId();
			if (string.IsNullOrWhiteSpace(id))
			{
				return null;
			}
			return _autoProfiles.FirstOrDefault((AutoModeProfile p) => string.Equals(p.ProfileId, id, StringComparison.OrdinalIgnoreCase));
		}

		private void SaveCurrentProfile()
		{
			if (_autoModeProfileService == null)
			{
				UpdateAutoStatus("Profile save unavailable: profile service not configured", warn: true);
				return;
			}
			if (_accounts == null || _accounts.Count == 0 || cmbAccount == null || cmbAccount.SelectedIndex < 0 || cmbAccount.SelectedIndex >= _accounts.Count)
			{
				MessageBox.Show("Select an account before saving a profile.", "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			AccountInfo account = _accounts[cmbAccount.SelectedIndex];
			AutoModeProfile profile = GetSelectedProfile();
			bool isNew = profile == null;
			if (profile == null)
			{
				profile = new AutoModeProfile
				{
					ProfileId = Guid.NewGuid().ToString("N"),
					CreatedUtc = DateTime.UtcNow
				};
			}
			profile.Name = string.Format("{0} [{1}] {2}", account.Label ?? "Account", account.Service ?? "Service", DateTime.Now.ToString("HH:mm"));
			profile.AccountId = account.Id;
			profile.Enabled = chkProfileEnabled == null || chkProfileEnabled.Checked;
			profile.PairScope = ((chkProfileAllPairs != null && chkProfileAllPairs.Checked) ? "All" : "Selected");
			profile.SelectedPairs = GetSelectedPairsForProfile();
			profile.IntervalMinutes = (int)((numProfileInterval != null) ? numProfileInterval.Value : ((decimal)GetAutoIntervalMinutes()));
			profile.MaxTradesPerCycle = (int)((numMaxTradesPerCycle != null) ? numMaxTradesPerCycle.Value : 3m);
			profile.CooldownMinutes = (int)((numCooldownMinutes != null) ? numCooldownMinutes.Value : 30m);
			profile.DailyRiskStopPct = ((numDailyRiskStopPct != null) ? numDailyRiskStopPct.Value : 3m);
			profile.UpdatedUtc = DateTime.UtcNow;
			_autoModeProfileService.Upsert(profile);
			LoadAutoProfiles(profile.ProfileId);
			UpdateAutoStatus((isNew ? "Profile created: " : "Profile updated: ") + profile.Name);
			Log.Info("[AutoMode] Profile saved: " + profile.Name, "SaveCurrentProfile", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1753);
			UpdateSelectedProfileSummary();
		}

		private void DeleteCurrentProfile()
		{
			if (_autoModeProfileService != null)
			{
				AutoModeProfile profile = GetSelectedProfile();
				if (profile == null)
				{
					UpdateAutoStatus("No profile selected to delete", warn: true);
				}
				else if (MessageBox.Show("Delete profile '" + profile.Name + "'?", "Auto Mode", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				{
					_autoModeProfileService.Delete(profile.ProfileId);
					LoadAutoProfiles();
					UpdateAutoStatus("Profile deleted");
					Log.Info("[AutoMode] Profile deleted: " + profile.Name, "DeleteCurrentProfile", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1775);
					UpdateSelectedProfileSummary();
				}
			}
		}

		private void ApplySelectedProfile()
		{
			if (_isApplyingProfile)
			{
				return;
			}
			AutoModeProfile profile = GetSelectedProfile();
			if (profile == null)
			{
				return;
			}
			_isApplyingProfile = true;
			try
			{
				if (_accounts != null && cmbAccount != null)
				{
					int idx = _accounts.FindIndex((AccountInfo a) => string.Equals(a.Id, profile.AccountId, StringComparison.OrdinalIgnoreCase));
					if (idx >= 0 && idx < cmbAccount.Items.Count)
					{
						cmbAccount.SelectedIndex = idx;
					}
				}
                UpdateTimerInterval();

				if (numProfileInterval != null)
				{
					SetNumeric(numProfileInterval, profile.IntervalMinutes);
				}
				if (numMaxTradesPerCycle != null)
				{
					SetNumeric(numMaxTradesPerCycle, profile.MaxTradesPerCycle);
				}
				if (numCooldownMinutes != null)
				{
					SetNumeric(numCooldownMinutes, profile.CooldownMinutes);
				}
				if (numDailyRiskStopPct != null)
				{
					SetNumeric(numDailyRiskStopPct, profile.DailyRiskStopPct);
				}
				if (chkProfileEnabled != null)
				{
					chkProfileEnabled.Checked = profile.Enabled;
				}
				if (chkProfileAllPairs != null)
				{
					chkProfileAllPairs.Checked = string.Equals(profile.PairScope, "All", StringComparison.OrdinalIgnoreCase);
				}
				ApplySelectedProfilePairScope();
				UpdateAutoStatus("Profile loaded: " + (profile.Name ?? profile.ProfileId));
				UpdateSelectedProfileSummary();
			}
			finally
			{
				_isApplyingProfile = false;
			}
		}

		private void ApplySelectedProfilePairScope()
		{
			AutoModeProfile profile = GetSelectedProfile();
			if (_lstPairs == null || _lstPairs.Items.Count == 0)
			{
				return;
			}
			if (profile == null)
			{
				if (chkProfileAllPairs != null && chkProfileAllPairs.Checked)
				{
					SelectAllPairs();
				}
				return;
			}
			if (string.Equals(profile.PairScope, "All", StringComparison.OrdinalIgnoreCase))
			{
				SelectAllPairs();
				return;
			}
			ClearPairSelection();
			HashSet<string> set = new HashSet<string>(profile.SelectedPairs ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < _lstPairs.Items.Count; i++)
			{
				string pair = _lstPairs.Items[i] as string;
				if (!string.IsNullOrWhiteSpace(pair) && set.Contains(pair))
				{
					_lstPairs.SetItemChecked(i, value: true);
				}
			}
			if (_lstPairs.CheckedItems.Count == 0 && cmbProduct != null && cmbProduct.SelectedItem != null)
			{
				SetComboProduct(cmbProduct.SelectedItem.ToString());
			}
		}

		private List<string> GetSelectedPairsForProfile()
		{
			List<string> selected = new List<string>();
			if (_lstPairs != null)
			{
				foreach (object item in _lstPairs.CheckedItems)
				{
					string pair = item as string;
					if (!string.IsNullOrWhiteSpace(pair))
					{
						selected.Add(pair);
					}
				}
			}
			if (selected.Count == 0 && cmbProduct != null && cmbProduct.SelectedItem != null)
			{
				selected.Add(cmbProduct.SelectedItem.ToString());
			}
			return selected.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}

		private static void SetNumeric(NumericUpDown control, decimal value)
		{
			if (control != null)
			{
				if (value < control.Minimum)
				{
					value = control.Minimum;
				}
				if (value > control.Maximum)
				{
					value = control.Maximum;
				}
				control.Value = value;
			}
		}

		private void UpdateSelectedProfileSummary()
		{
			if (lblProfileSummary == null)
			{
				return;
			}
			AutoModeProfile profile = GetSelectedProfile();
			if (profile == null)
			{
				lblProfileSummary.Text = "Profile: (not selected)";
				lblProfileSummary.ForeColor = Color.DimGray;
				return;
			}
			AccountInfo account = (_accounts ?? new List<AccountInfo>()).FirstOrDefault((AccountInfo a) => string.Equals(a.Id, profile.AccountId, StringComparison.OrdinalIgnoreCase));
			bool effectiveEnabled = ((chkProfileEnabled != null) ? chkProfileEnabled.Checked : profile.Enabled);
			int effectiveInterval = ((numProfileInterval != null) ? ((int)numProfileInterval.Value) : profile.IntervalMinutes);
			int pairCount = GetProfilePairCount(profile);
			string service = ((account != null) ? (account.Service ?? "unknown") : "missing");
			string mode = ((account != null) ? account.Mode.ToString() : "missing");
			string accountLabel = ((account != null) ? (account.Label ?? "(unnamed)") : "(missing account)");
			lblProfileSummary.Text = $"Profile: {accountLabel} | {service}/{mode} | {pairCount} pair(s) | every {effectiveInterval}m | max {profile.MaxTradesPerCycle}/cycle | cd {profile.CooldownMinutes}m | risk {profile.DailyRiskStopPct:0.0}%";
			lblProfileSummary.ForeColor = ((!effectiveEnabled || account == null || !account.Enabled) ? Color.DarkOrange : Color.DarkGreen);
		}

		private int GetProfilePairCount(AutoModeProfile profile)
		{
			if (profile == null)
			{
				return 0;
			}
			if (string.Equals(profile.PairScope, "All", StringComparison.OrdinalIgnoreCase))
			{
				return (_lstPairs != null) ? _lstPairs.Items.Count : 0;
			}
			return (profile.SelectedPairs ?? new List<string>()).Where((string s) => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).Count();
		}

		private string GetCapabilityBlockReason(AccountInfo account, IBroker broker)
		{
			if (account == null)
			{
				return "account not found";
			}
			if (broker == null)
			{
				return "unsupported broker adapter";
			}
			BrokerCapabilities caps = broker.GetCapabilities();
			if (caps == null)
			{
				return "broker capabilities unavailable";
			}
			if (!caps.SupportsMarketEntry)
			{
				return "market entry not supported";
			}
			if (account.Mode != AccountMode.Paper)
			{
				if (_keyService == null)
				{
					return "key service unavailable";
				}
				if (string.IsNullOrWhiteSpace(account.KeyEntryId) || _keyService.Get(account.KeyEntryId) == null)
				{
					return "missing or invalid API key";
				}
				if (!caps.SupportsProtectiveExits && !SupportsLocalProtectiveWatchdog(account, broker))
				{
					return (!string.IsNullOrWhiteSpace(caps.Notes)) ? ("protective exits unsupported: " + caps.Notes) : "protective exits unsupported";
				}
			}
			return null;
		}

		private bool SupportsLocalProtectiveWatchdog(AccountInfo account, IBroker broker)
		{
			if (account == null || broker == null)
			{
				return false;
			}
			if (account.Mode == AccountMode.Paper)
			{
				return true;
			}
			if (_client == null)
			{
				return false;
			}
			return string.Equals(broker.Service, "coinbase-advanced", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(broker.Service, "coinbase-exchange", StringComparison.OrdinalIgnoreCase);
		}

		private void WriteCycleTelemetry(AutoCycleTelemetry cycleTelemetry)
		{
			if (cycleTelemetry == null)
			{
				return;
			}
			try
			{
				string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CryptoDayTraderSuite", "automode", "cycle_reports");
				if (!Directory.Exists(root))
				{
					Directory.CreateDirectory(root);
				}
				string safeCycleId = (string.IsNullOrWhiteSpace(cycleTelemetry.CycleId) ? Guid.NewGuid().ToString("N") : cycleTelemetry.CycleId);
				string filename = string.Format("cycle_{0}_{1}.json", DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff"), safeCycleId.Substring(0, Math.Min(8, safeCycleId.Length)));
				string fullPath = Path.Combine(root, filename);
				JavaScriptSerializer serializer = new JavaScriptSerializer();
				string json = serializer.Serialize(cycleTelemetry);
				File.WriteAllText(fullPath, json);
				Log.Info("[AutoMode] Cycle telemetry written: " + fullPath, "WriteCycleTelemetry", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 2001);
				RefreshLatestTelemetrySummary();
			}
			catch (Exception ex)
			{
				Log.Warn("[AutoMode] Failed to write cycle telemetry: " + ex.Message, "WriteCycleTelemetry", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 2006);
			}
		}

		private string GetCycleReportsDirectory()
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CryptoDayTraderSuite", "automode", "cycle_reports");
		}

		private void RefreshLatestTelemetrySummary()
		{
			if (lblTelemetrySummary == null)
			{
				return;
			}
			try
			{
				string root = GetCycleReportsDirectory();
				if (!Directory.Exists(root))
				{
					lblTelemetrySummary.Text = "Telemetry: no cycle reports";
					lblTelemetrySummary.ForeColor = Color.DimGray;
					return;
				}
				FileInfo latest = (from f in new DirectoryInfo(root).GetFiles("cycle_*.json")
					orderby f.LastWriteTimeUtc descending
					select f).FirstOrDefault();
				if (latest == null)
				{
					lblTelemetrySummary.Text = "Telemetry: no cycle reports";
					lblTelemetrySummary.ForeColor = Color.DimGray;
					return;
				}
				string json = File.ReadAllText(latest.FullName);
				JavaScriptSerializer serializer = new JavaScriptSerializer();
				AutoCycleTelemetry telemetry = serializer.Deserialize<AutoCycleTelemetry>(json);
				if (telemetry == null)
				{
					lblTelemetrySummary.Text = "Telemetry: latest report unreadable";
					lblTelemetrySummary.ForeColor = Color.DarkOrange;
					return;
				}
				lblTelemetrySummary.Text = string.Format("Telemetry: {0} | profiles {1}/{2} | executed {3} | blocked {4} | gates {5} (no-signal:{6}, ai-veto:{7}, risk-veto:{8}, success:{9}) | matrix {10} (guardrails:{11}, containment:{12}, coverage:{13}) | {14}", latest.Name, telemetry.ProcessedProfileCount, telemetry.EnabledProfileCount, telemetry.ExecutedProfiles, telemetry.FailedProfiles, string.IsNullOrWhiteSpace(telemetry.GateStatus) ? "n/a" : telemetry.GateStatus, telemetry.GateNoSignalObserved ? "obs" : "na", telemetry.GateAiVetoObserved ? "obs" : "na", telemetry.GateRiskVetoObserved ? "obs" : "na", telemetry.GateSuccessObserved ? "obs" : "na", string.IsNullOrWhiteSpace(telemetry.MatrixStatus) ? "n/a" : telemetry.MatrixStatus, telemetry.MatrixIndependentGuardrailsObserved ? "obs" : "na", telemetry.MatrixFailureContainmentObserved ? "obs" : "na", telemetry.MatrixMinimumProfileCoverage ? "obs" : "na", (telemetry.EndedUtc == default(DateTime)) ? "pending" : telemetry.EndedUtc.ToLocalTime().ToString("HH:mm:ss"));
				lblTelemetrySummary.ForeColor = ((telemetry.FailedProfiles > 0 || !string.Equals(telemetry.MatrixStatus, "PASS", StringComparison.OrdinalIgnoreCase) || !string.Equals(telemetry.GateStatus, "PASS", StringComparison.OrdinalIgnoreCase)) ? Color.DarkOrange : Color.DarkGreen);
			}
			catch (Exception ex)
			{
				lblTelemetrySummary.Text = "Telemetry: read failed";
				lblTelemetrySummary.ForeColor = Color.DarkOrange;
				Log.Warn("[AutoMode] Telemetry summary refresh failed: " + ex.Message, "RefreshLatestTelemetrySummary", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 2079);
			}
		}

		private void EvaluateMatrixStatus(AutoCycleTelemetry cycleTelemetry)
		{
			if (cycleTelemetry == null)
			{
				return;
			}
			List<ProfileCycleTelemetry> profiles = cycleTelemetry.Profiles ?? new List<ProfileCycleTelemetry>();
			cycleTelemetry.MatrixHasSelectedScopeProfile = profiles.Any((ProfileCycleTelemetry p) => string.Equals(p.PairScope, "Selected", StringComparison.OrdinalIgnoreCase));
			cycleTelemetry.MatrixHasAllScopeProfile = profiles.Any((ProfileCycleTelemetry p) => string.Equals(p.PairScope, "All", StringComparison.OrdinalIgnoreCase));
			cycleTelemetry.MatrixPairConfigurationConsistent = profiles.All(delegate(ProfileCycleTelemetry p)
			{
				if (string.Equals(p.PairScope, "All", StringComparison.OrdinalIgnoreCase))
				{
					return p.SymbolCount > 0;
				}
				return (p.ExpectedSymbolCount <= 0) ? (p.SymbolCount > 0) : (p.SymbolCount == p.ExpectedSymbolCount);
			});
			cycleTelemetry.MatrixHasGuardrailValues = profiles.All((ProfileCycleTelemetry p) => p.MaxTradesPerCycle > 0 && p.CooldownMinutes > 0 && p.DailyRiskStopPct > 0m);
			List<ProfileCycleTelemetry> profilesWithIds = profiles.Where((ProfileCycleTelemetry p) => !string.IsNullOrWhiteSpace(p.ProfileId)).ToList();
			cycleTelemetry.MatrixGuardrailScopesIsolated = profilesWithIds.Count != 0 && profilesWithIds.Select((ProfileCycleTelemetry p) => p.GuardrailScopeKey ?? string.Empty).Distinct(StringComparer.OrdinalIgnoreCase).Count() == profilesWithIds.Select((ProfileCycleTelemetry p) => p.ProfileId).Distinct(StringComparer.OrdinalIgnoreCase).Count() && profilesWithIds.All((ProfileCycleTelemetry p) => !string.IsNullOrWhiteSpace(p.GuardrailScopeKey));
			cycleTelemetry.MatrixIndependentGuardrailsObserved = cycleTelemetry.MatrixHasGuardrailValues && cycleTelemetry.MatrixGuardrailScopesIsolated && profilesWithIds.Count > 0;
			bool hasBlocked = profiles.Any((ProfileCycleTelemetry p) => string.Equals(p.Status, "blocked", StringComparison.OrdinalIgnoreCase));
			bool hasErrored = profiles.Any((ProfileCycleTelemetry p) => string.Equals(p.Status, "error", StringComparison.OrdinalIgnoreCase));
			bool hasFailedOrders = profiles.Any((ProfileCycleTelemetry p) => p.Failed > 0);
			bool hasOtherCompleted = profiles.Any((ProfileCycleTelemetry p) => !string.Equals(p.Status, "blocked", StringComparison.OrdinalIgnoreCase) && !string.Equals(p.Status, "error", StringComparison.OrdinalIgnoreCase) && (string.Equals(p.Status, "completed", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "executed", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "skipped", StringComparison.OrdinalIgnoreCase)));
			bool hasFailures = hasBlocked || hasErrored || hasFailedOrders;
			cycleTelemetry.MatrixFailureContainmentObserved = hasFailures && cycleTelemetry.ProcessedProfileCount >= cycleTelemetry.EnabledProfileCount && hasOtherCompleted;
cycleTelemetry.MatrixFailureDoesNotHaltCycle = !hasFailures || cycleTelemetry.MatrixFailureContainmentObserved;
cycleTelemetry.MatrixIsolationObserved = (!hasBlocked || hasOtherCompleted) && cycleTelemetry.MatrixFailureDoesNotHaltCycle;
int requiredProfiles = 2;
cycleTelemetry.MatrixMinimumProfileCoverage = cycleTelemetry.EnabledProfileCount >= requiredProfiles && cycleTelemetry.ProcessedProfileCount >= requiredProfiles;
cycleTelemetry.MatrixCoverageNote = (cycleTelemetry.MatrixMinimumProfileCoverage ? string.Empty : string.Format("insufficient profile coverage: processed={0}, enabled={1}, required>={2}", cycleTelemetry.ProcessedProfileCount, cycleTelemetry.EnabledProfileCount, requiredProfiles));
bool pass = cycleTelemetry.MatrixPairConfigurationConsistent && cycleTelemetry.MatrixIndependentGuardrailsObserved && cycleTelemetry.MatrixMinimumProfileCoverage && cycleTelemetry.MatrixIsolationObserved;
cycleTelemetry.MatrixStatus = (pass ? "PASS" : (cycleTelemetry.MatrixMinimumProfileCoverage ? "PARTIAL" : "INSUFFICIENT"));
		}

		private void EvaluateReliabilityGates(AutoCycleTelemetry cycleTelemetry)
		{
			if (cycleTelemetry != null)
			{
				List<ProfileCycleTelemetry> profiles = cycleTelemetry.Profiles ?? new List<ProfileCycleTelemetry>();
				cycleTelemetry.GateNoSignalObserved = profiles.Any((ProfileCycleTelemetry p) => p.NoSignalCount > 0);
				cycleTelemetry.GateAiVetoObserved = profiles.Any((ProfileCycleTelemetry p) => p.AiVetoCount > 0);
				cycleTelemetry.GateRiskVetoObserved = profiles.Any((ProfileCycleTelemetry p) => p.SkippedRisk > 0);
				cycleTelemetry.GateSuccessObserved = profiles.Any((ProfileCycleTelemetry p) => p.Executed > 0);
				bool pass = cycleTelemetry.GateNoSignalObserved && cycleTelemetry.GateAiVetoObserved && cycleTelemetry.GateRiskVetoObserved && cycleTelemetry.GateSuccessObserved;
				cycleTelemetry.GateStatus = (pass ? "PASS" : "PARTIAL");
			}
		}

		private List<string> RankPairs(IEnumerable<string> products)
		{
			List<string> all = (products ?? new List<string>())
				.Where((string p) => IsRankableUsdPair(p))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
			List<string> popular = PopularPairs.Where((string p) => all.Any((string x) => string.Equals(x, p, StringComparison.OrdinalIgnoreCase))).ToList();
			List<string> others = (from p in all
				where !popular.Any((string x) => string.Equals(x, p, StringComparison.OrdinalIgnoreCase))
				orderby p
				select p).ToList();
			return popular.Concat(others).ToList();
		}

		private bool IsRankableUsdPair(string pair)
		{
			if (string.IsNullOrWhiteSpace(pair))
			{
				return false;
			}

			string normalized = pair.Trim().ToUpperInvariant().Replace("/", "-");
			if (!normalized.EndsWith("-USD", StringComparison.OrdinalIgnoreCase) || normalized.Length <= 4)
			{
				return false;
			}

			string baseAsset = normalized.Substring(0, normalized.Length - 4);
			if (string.IsNullOrWhiteSpace(baseAsset))
			{
				return false;
			}

			return baseAsset.Any(char.IsLetter);
		}

		private void PopulatePairList(List<string> rankedPairs)
		{
			if (_lstPairs == null)
			{
				return;
			}
			_lstPairs.Items.Clear();
			foreach (string pair in rankedPairs)
			{
				_lstPairs.Items.Add(pair, PopularPairs.Contains(pair, StringComparer.OrdinalIgnoreCase));
			}
		}

		private void SelectAllPairs()
		{
			if (_lstPairs != null && _lstPairs.Items.Count != 0)
			{
				for (int i = 0; i < _lstPairs.Items.Count; i++)
				{
					_lstPairs.SetItemChecked(i, value: true);
				}
				UpdateSelectedProfileSummary();
			}
		}

		private void SelectTopPopularPairs(int count)
		{
			if (_lstPairs == null || _lstPairs.Items.Count == 0)
			{
				return;
			}
			ClearPairSelection();
			int selected = 0;
			for (int i = 0; i < _lstPairs.Items.Count; i++)
			{
				if (selected >= count)
				{
					break;
				}
				string pair = _lstPairs.Items[i] as string;
				if (!string.IsNullOrWhiteSpace(pair) && PopularPairs.Contains(pair, StringComparer.OrdinalIgnoreCase))
				{
					_lstPairs.SetItemChecked(i, value: true);
					selected++;
				}
			}
		}

		private void ClearPairSelection()
		{
			if (_lstPairs != null && _lstPairs.Items.Count != 0)
			{
				for (int i = 0; i < _lstPairs.Items.Count; i++)
				{
					_lstPairs.SetItemChecked(i, value: false);
				}
				UpdateSelectedProfileSummary();
			}
		}

		private List<string> GetSelectedPairs()
		{
			if (chkProfileAllPairs != null && chkProfileAllPairs.Checked && _lstPairs != null && _lstPairs.Items.Count > 0)
			{
				List<string> all = new List<string>();
				foreach (object item in _lstPairs.Items)
				{
					string pair = item as string;
					if (!string.IsNullOrWhiteSpace(pair))
					{
						all.Add(pair);
					}
				}
				if (all.Count > 0)
				{
					return all.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
				}
			}
			List<string> selected = new List<string>();
			if (_lstPairs != null)
			{
				foreach (object item2 in _lstPairs.CheckedItems)
				{
					string pair2 = item2 as string;
					if (!string.IsNullOrWhiteSpace(pair2))
					{
						selected.Add(pair2);
					}
				}
			}
			if (selected.Count == 0 && cmbProduct != null && cmbProduct.SelectedItem != null)
			{
				selected.Add(cmbProduct.SelectedItem.ToString());
			}
			return selected.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}

		private void SetComboProduct(string symbol)
		{
			if (cmbProduct == null || string.IsNullOrWhiteSpace(symbol))
			{
				return;
			}
			for (int i = 0; i < cmbProduct.Items.Count; i++)
			{
				string item = cmbProduct.Items[i] as string;
				if (string.Equals(item, symbol, StringComparison.OrdinalIgnoreCase))
				{
					cmbProduct.SelectedIndex = i;
					break;
				}
			}
		}

	}
}



