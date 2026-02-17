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

			public string MatrixStatus;

			public bool GateNoSignalObserved;

			public bool GateAiVetoObserved;

			public bool GateRiskVetoObserved;

			public bool GateSuccessObserved;

			public string GateStatus;

			public int CycleErrorCount;

			public string CycleErrorMessage;

			public List<ProfileCycleTelemetry> Profiles = new List<ProfileCycleTelemetry>();
		}

		private sealed class ProposalBatchResult
		{
			public List<TradePlan> Plans = new List<TradePlan>();

			public Dictionary<string, int> ReasonCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
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

		private IContainer components = null;

		private TableLayoutPanel tableLayout;

		private FlowLayoutPanel headerPanel;

		private Label lblAccount;

		private ComboBox cmbAccount;

		private Label lblProfile;

		private ComboBox cmbProfile;

		private CheckBox chkProfileEnabled;

		private Label lblProfileEvery;

		private NumericUpDown numProfileInterval;

		private Button btnProfileSave;

		private Button btnProfileDelete;

		private CheckBox chkProfileAllPairs;

		private CheckBox chkAutoRun;

		private Label lblAutoEvery;

		private NumericUpDown numAutoInterval;

		private CheckBox chkLiveArm;

		private Button btnKillSwitch;

		private Label lblAutoStatus;

		private SplitContainer mainSplit;

		private FlowLayoutPanel topPanel;

		private Label lblProduct;

		private ComboBox cmbProduct;

		private Label lblGran;

		private ComboBox cmbGran;

		private Label lblLookback;

		private NumericUpDown numLookback;

		private Label lblEquity;

		private NumericUpDown numEquity;

		private CheckBox chkAutoPropose;

		private Label lblMaxTradesPerCycle;

		private NumericUpDown numMaxTradesPerCycle;

		private Label lblCooldownMinutes;

		private NumericUpDown numCooldownMinutes;

		private Label lblDailyRiskStopPct;

		private NumericUpDown numDailyRiskStopPct;

		private TableLayoutPanel rightLayout;

		private FlowLayoutPanel actionPanel;

		private Button btnScan;

		private Button btnPropose;

		private Button btnExecute;

		private DataGridView grid;

		private DataGridViewTextBoxColumn colStrategy;

		private DataGridViewTextBoxColumn colSymbol;

		private DataGridViewTextBoxColumn colGran;

		private DataGridViewTextBoxColumn colExpectancy;

		private DataGridViewTextBoxColumn colWinRate;

		private DataGridViewTextBoxColumn colAvgWin;

		private DataGridViewTextBoxColumn colAvgLoss;

		private DataGridViewTextBoxColumn colSharpe;

		private DataGridViewTextBoxColumn colSamples;

		private TableLayoutPanel footerPanel;

		private Label lblProfileSummary;

		private Label lblTelemetrySummary;

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
			_accounts = (from a in _accountService.GetAll()
				where a.Enabled
				select a).ToList();
			if (cmbAccount == null)
			{
				return;
			}
			cmbAccount.Items.Clear();
			foreach (AccountInfo account in _accounts)
			{
				cmbAccount.Items.Add(account.Label + " [" + account.Service + "]");
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
				ProposalBatchResult proposeBatch = await ProposeForAccountAsync(acc, _last, gran, numEquity.Value);
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
				string summary = (await ExecutePlansForAccountAsync(acc, broker, _queued.Where((TradePlan p) => p.AccountId == acc.Id).ToList(), (numEquity != null) ? numEquity.Value : 1000m, (numMaxTradesPerCycle != null) ? ((int)numMaxTradesPerCycle.Value) : 3, (numCooldownMinutes != null) ? ((int)numCooldownMinutes.Value) : 30, (numDailyRiskStopPct != null) ? numDailyRiskStopPct.Value : 3m, interactive, fromAutoCycle)).ToSummary();
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
			bool flag = _autoTimer != null && _autoTimer.Enabled;
			bool flag2 = chkAutoRun != null && chkAutoRun.Checked;
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
			if (flag || flag2 || _autoCycleRunning)
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
						ProposalBatchResult proposeBatch = await ProposeForAccountAsync(account, rows2, gran, (numEquity != null) ? numEquity.Value : 1000m);
						List<TradePlan> plans = ((proposeBatch != null) ? (proposeBatch.Plans ?? new List<TradePlan>()) : new List<TradePlan>());
						int noSignalCount = 0;
						int aiVetoCount = 0;
						int biasBlockedCount = 0;
						if (proposeBatch != null && proposeBatch.ReasonCounts != null)
						{
							proposeBatch.ReasonCounts.TryGetValue("no-signal", out noSignalCount);
							proposeBatch.ReasonCounts.TryGetValue("ai-veto", out aiVetoCount);
							proposeBatch.ReasonCounts.TryGetValue("bias-blocked", out biasBlockedCount);
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
						ExecutionOutcome execOutcome = await ExecutePlansForAccountAsync(account, broker, plans, (numEquity != null) ? numEquity.Value : 1000m, profile.MaxTradesPerCycle, profile.CooldownMinutes, profile.DailyRiskStopPct, interactive: false, fromAutoCycle: true, profileGuardrailScopeKey);
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
			int num = (int)numAutoInterval.Value;
			return (num < 1) ? 1 : num;
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
			DateTime date = DateTime.UtcNow.Date;
			if (_dailyRiskDateUtc != date)
			{
				_dailyRiskDateUtc = date;
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
			if (!_symbolCooldownUtc.TryGetValue(key, out var value))
			{
				return false;
			}
			return DateTime.UtcNow - value < TimeSpan.FromMinutes(cooldownMinutes);
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
			decimal value;
			return _dailyRiskUsedByScope.TryGetValue(scopeKey, out value) ? value : 0m;
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
			int value;
			return _sessionOpenPositionsByAccount.TryGetValue(accountId, out value) ? value : 0;
		}

		private int GetPersistedOpenCount(AccountInfo acc)
		{
			if (acc == null || _historyService == null)
			{
				return 0;
			}
			try
			{
				List<TradeRecord> list = _historyService.LoadTrades() ?? new List<TradeRecord>();
				if (list.Count == 0)
				{
					return 0;
				}
				List<TradeRecord> list2 = list.Where((TradeRecord t) => t != null && t.Executed && string.Equals(t.Exchange, acc.Service, StringComparison.OrdinalIgnoreCase)).ToList();
				if (list2.Count == 0)
				{
					return 0;
				}
				string accountTag = "acct:" + (acc.Id ?? string.Empty);
				IEnumerable<TradeRecord> trades = list2.Where((TradeRecord t) => !string.IsNullOrWhiteSpace(t.Notes) && t.Notes.IndexOf(accountTag, StringComparison.OrdinalIgnoreCase) >= 0);
				int num = ComputeOpenCountFromTradeSet(trades);
				if (num > 0)
				{
					return num;
				}
				if (list2.Any((TradeRecord t) => !string.IsNullOrWhiteSpace(t.Notes) && t.Notes.IndexOf("acct:", StringComparison.OrdinalIgnoreCase) >= 0))
				{
					return 0;
				}
				return ComputeOpenCountFromTradeSet(list2);
			}
			catch (Exception ex)
			{
				Log.Warn("[AutoMode] Failed to load persisted open count: " + ex.Message, "GetPersistedOpenCount", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1082);
				return 0;
			}
		}

		private int ComputeOpenCountFromTradeSet(IEnumerable<TradeRecord> trades)
		{
			List<TradeRecord> list = (trades ?? new List<TradeRecord>()).Where((TradeRecord t) => t?.Executed ?? false).ToList();
			if (list.Count == 0)
			{
				return 0;
			}
			int num = list.Count((TradeRecord t) => !t.PnL.HasValue);
			int num2 = list.Count((TradeRecord t) => t.PnL.HasValue && !string.IsNullOrWhiteSpace(t.Notes) && t.Notes.IndexOf("[close:", StringComparison.OrdinalIgnoreCase) >= 0);
			int num3 = num - num2;
			return (num3 >= 0) ? num3 : 0;
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
			foreach (string symbol in symbols)
			{
				try
				{
					List<ProjectionRow> symbolRows = await _planner.ProjectAsync(symbol, granMinutes, lookbackMins, 0.006m, 0.004m);
					if (symbolRows != null && symbolRows.Count > 0)
					{
						result.AddRange(symbolRows);
					}
				}
				catch (Exception ex)
				{
					Log.Warn("[AutoMode] Scan symbol failed: " + symbol + " | " + ex.Message, "ScanSymbolsAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1216);
				}
				await Task.Delay(120);
			}
			return result.OrderByDescending((ProjectionRow r) => r.Expectancy).ToList();
		}

		private List<string> ApplyAutoCycleSymbolLimits(AutoModeProfile profile, List<string> symbols)
		{
			List<string> list = (symbols ?? new List<string>()).Where((string s) => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			if (list.Count == 0)
			{
				return list;
			}
			if (profile == null || !string.Equals((profile.PairScope ?? string.Empty).Trim(), "All", StringComparison.OrdinalIgnoreCase))
			{
				return list;
			}
			int autoAllScopeMaxSymbols = GetAutoAllScopeMaxSymbols();
			if (autoAllScopeMaxSymbols <= 0 || list.Count <= autoAllScopeMaxSymbols)
			{
				return list;
			}
			List<string> list2 = list.Take(autoAllScopeMaxSymbols).ToList();
			Log.Warn(string.Format("[AutoMode] Limiting All-scope symbols for profile '{0}' from {1} to {2} to reduce public API rate-limit pressure.", profile.Name ?? profile.ProfileId ?? "(unknown)", list.Count, list2.Count), "ApplyAutoCycleSymbolLimits", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1252);
			return list2;
		}

		private int GetAutoAllScopeMaxSymbols()
		{
			string environmentVariable = Environment.GetEnvironmentVariable("CDTS_AUTOMODE_MAX_SYMBOLS");
			if (!string.IsNullOrWhiteSpace(environmentVariable) && int.TryParse(environmentVariable.Trim(), out var result) && result > 0)
			{
				return result;
			}
			return 12;
		}

		private async Task<ExecutionOutcome> ExecutePlansForAccountAsync(AccountInfo acc, IBroker broker, List<TradePlan> plans, decimal equity, int maxTradesPerCycle, int cooldownMinutes, decimal dailyRiskStopPct, bool interactive, bool fromAutoCycle, string guardrailScopeKey = null)
		{
			ResetDailyRiskIfNeeded();
			await EvaluatePaperProtectiveExitsForAccountAsync(acc, broker, guardrailScopeKey);
			ExecutionOutcome outcome = new ExecutionOutcome();
			string scopeKey = NormalizeGuardrailScopeKey(guardrailScopeKey, acc);
			if (maxTradesPerCycle < 1)
			{
				maxTradesPerCycle = 1;
			}
			if (cooldownMinutes < 1)
			{
				cooldownMinutes = 1;
			}
			if (dailyRiskStopPct < 0.1m)
			{
				dailyRiskStopPct = 0.1m;
			}
			decimal dailyRiskCap = equity * (dailyRiskStopPct / 100m);
			decimal dailyRiskUsed = GetDailyRiskUsed(scopeKey);
			int sessionOpen = GetSessionOpenCount(acc.Id);
			int persistedOpen = GetPersistedOpenCount(acc);
			int openCount = Math.Max(sessionOpen, persistedOpen);
			foreach (TradePlan p in plans ?? new List<TradePlan>())
			{
				if (_autoStopRequested)
				{
					break;
				}
				if (p == null || p.AccountId != acc.Id)
				{
					continue;
				}
				if (outcome.OkCount >= maxTradesPerCycle)
				{
					break;
				}
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
				if (planRisk < 0m)
				{
					planRisk = default(decimal);
				}
				if (dailyRiskCap > 0m && dailyRiskUsed + planRisk > dailyRiskCap)
				{
					outcome.SkippedRisk++;
					continue;
				}
				(bool ok, string message) validation = broker.ValidateTradePlan(p);
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
					openCount++;
					if (planRisk > 0m)
					{
						dailyRiskUsed += planRisk;
						SetDailyRiskUsed(scopeKey, dailyRiskUsed);
					}
					MarkCooldown(scopeKey, normalizedSymbol);
					if (interactive)
					{
						Log.Info("[AutoMode] Execute ok: " + r.message, "ExecutePlansForAccountAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1352);
					}
					_paperOpenPositions.Add(new PaperOpenPosition
					{
						AccountId = acc.Id,
						Symbol = p.Symbol,
						Strategy = p.Strategy,
						Direction = p.Direction,
						Qty = p.Qty,
						Entry = p.Entry,
						Stop = p.Stop,
						Target = p.Target,
						OpenedUtc = DateTime.UtcNow,
						ScopeKey = scopeKey
					});
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

		private int ParseGranularityMinutes()
		{
			if (cmbGran == null)
			{
				return 60;
			}
			if (!int.TryParse(cmbGran.SelectedItem?.ToString(), out var result))
			{
				result = 60;
			}
			return result;
		}

		private bool ShouldRunProfile(AutoModeProfile profile)
		{
			if (profile == null)
			{
				return false;
			}
			if (!_profileLastRunUtc.TryGetValue(profile.ProfileId, out var value))
			{
				return true;
			}
			int num = ((profile.IntervalMinutes < 1) ? 1 : profile.IntervalMinutes);
			return DateTime.UtcNow - value >= TimeSpan.FromMinutes(num);
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
			string a = (profile.PairScope ?? string.Empty).Trim();
			if (string.Equals(a, "All", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "AllPairs", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "Any", StringComparison.OrdinalIgnoreCase))
			{
				return GetRuntimeProductUniverse();
			}
			List<string> list = (profile.SelectedPairs ?? new List<string>()).Where((string s) => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			if (list.Count == 0)
			{
				list = GetSelectedPairs();
			}
			if (list.Count == 0)
			{
				list = GetRuntimeProductUniverse();
				if (list.Count > 0)
				{
					Log.Warn("[AutoMode] Profile selected scope resolved zero symbols; falling back to runtime universe for " + (profile.Name ?? "(unnamed)"), "ResolveProfileSymbols", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1462);
				}
			}
			return list;
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
			List<string> list = new List<string>();
			if (_lstPairs != null)
			{
				foreach (object item in _lstPairs.Items)
				{
					string text = item as string;
					if (!string.IsNullOrWhiteSpace(text))
					{
						list.Add(text);
					}
				}
			}
			if (list.Count == 0 && cmbProduct != null && cmbProduct.Items.Count > 0)
			{
				foreach (object item2 in cmbProduct.Items)
				{
					string text2 = item2 as string;
					if (!string.IsNullOrWhiteSpace(text2))
					{
						list.Add(text2);
					}
				}
			}
			if (list.Count == 0 && _productUniverseCache != null && _productUniverseCache.Count > 0)
			{
				list.AddRange(_productUniverseCache);
			}
			if (list.Count == 0)
			{
				list.AddRange(PopularPairs);
			}
			return list.Where((string p) => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
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
			string text = preferProfileId ?? GetSelectedProfileId();
			_isApplyingProfile = true;
			try
			{
				cmbProfile.Items.Clear();
				foreach (AutoModeProfile autoProfile in _autoProfiles)
				{
					cmbProfile.Items.Add(new AutoProfileComboItem
					{
						Id = autoProfile.ProfileId,
						Name = autoProfile.Name
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
				int selectedIndex = 0;
				if (!string.IsNullOrWhiteSpace(text))
				{
					for (int num = 0; num < cmbProfile.Items.Count; num++)
					{
						if (cmbProfile.Items[num] is AutoProfileComboItem autoProfileComboItem && string.Equals(autoProfileComboItem.Id, text, StringComparison.OrdinalIgnoreCase))
						{
							selectedIndex = num;
							break;
						}
					}
				}
				cmbProfile.SelectedIndex = selectedIndex;
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
			return (!(cmbProfile.SelectedItem is AutoProfileComboItem autoProfileComboItem)) ? null : autoProfileComboItem.Id;
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
			AccountInfo accountInfo = _accounts[cmbAccount.SelectedIndex];
			AutoModeProfile autoModeProfile = GetSelectedProfile();
			bool flag = autoModeProfile == null;
			if (autoModeProfile == null)
			{
				autoModeProfile = new AutoModeProfile
				{
					ProfileId = Guid.NewGuid().ToString("N"),
					CreatedUtc = DateTime.UtcNow
				};
			}
			autoModeProfile.Name = string.Format("{0} [{1}] {2}", accountInfo.Label ?? "Account", accountInfo.Service ?? "Service", DateTime.Now.ToString("HH:mm"));
			autoModeProfile.AccountId = accountInfo.Id;
			autoModeProfile.Enabled = chkProfileEnabled == null || chkProfileEnabled.Checked;
			autoModeProfile.PairScope = ((chkProfileAllPairs != null && chkProfileAllPairs.Checked) ? "All" : "Selected");
			autoModeProfile.SelectedPairs = GetSelectedPairsForProfile();
			autoModeProfile.IntervalMinutes = (int)((numProfileInterval != null) ? numProfileInterval.Value : ((decimal)GetAutoIntervalMinutes()));
			autoModeProfile.MaxTradesPerCycle = (int)((numMaxTradesPerCycle != null) ? numMaxTradesPerCycle.Value : 3m);
			autoModeProfile.CooldownMinutes = (int)((numCooldownMinutes != null) ? numCooldownMinutes.Value : 30m);
			autoModeProfile.DailyRiskStopPct = ((numDailyRiskStopPct != null) ? numDailyRiskStopPct.Value : 3m);
			autoModeProfile.UpdatedUtc = DateTime.UtcNow;
			_autoModeProfileService.Upsert(autoModeProfile);
			LoadAutoProfiles(autoModeProfile.ProfileId);
			UpdateAutoStatus((flag ? "Profile created: " : "Profile updated: ") + autoModeProfile.Name);
			Log.Info("[AutoMode] Profile saved: " + autoModeProfile.Name, "SaveCurrentProfile", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1753);
			UpdateSelectedProfileSummary();
		}

		private void DeleteCurrentProfile()
		{
			if (_autoModeProfileService != null)
			{
				AutoModeProfile selectedProfile = GetSelectedProfile();
				if (selectedProfile == null)
				{
					UpdateAutoStatus("No profile selected to delete", warn: true);
				}
				else if (MessageBox.Show("Delete profile '" + selectedProfile.Name + "'?", "Auto Mode", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				{
					_autoModeProfileService.Delete(selectedProfile.ProfileId);
					LoadAutoProfiles();
					UpdateAutoStatus("Profile deleted");
					Log.Info("[AutoMode] Profile deleted: " + selectedProfile.Name, "DeleteCurrentProfile", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 1775);
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
					int num = _accounts.FindIndex((AccountInfo a) => string.Equals(a.Id, profile.AccountId, StringComparison.OrdinalIgnoreCase));
					if (num >= 0 && num < cmbAccount.Items.Count)
					{
						cmbAccount.SelectedIndex = num;
					}
				}
				if (numAutoInterval != null)
				{
					SetNumeric(numAutoInterval, profile.IntervalMinutes);
				}
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
			AutoModeProfile selectedProfile = GetSelectedProfile();
			if (_lstPairs == null || _lstPairs.Items.Count == 0)
			{
				return;
			}
			if (selectedProfile == null)
			{
				if (chkProfileAllPairs != null && chkProfileAllPairs.Checked)
				{
					SelectAllPairs();
				}
				return;
			}
			if (string.Equals(selectedProfile.PairScope, "All", StringComparison.OrdinalIgnoreCase))
			{
				SelectAllPairs();
				return;
			}
			ClearPairSelection();
			HashSet<string> hashSet = new HashSet<string>(selectedProfile.SelectedPairs ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < _lstPairs.Items.Count; i++)
			{
				string text = _lstPairs.Items[i] as string;
				if (!string.IsNullOrWhiteSpace(text) && hashSet.Contains(text))
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
			List<string> list = new List<string>();
			if (_lstPairs != null)
			{
				foreach (object checkedItem in _lstPairs.CheckedItems)
				{
					string text = checkedItem as string;
					if (!string.IsNullOrWhiteSpace(text))
					{
						list.Add(text);
					}
				}
			}
			if (list.Count == 0 && cmbProduct != null && cmbProduct.SelectedItem != null)
			{
				list.Add(cmbProduct.SelectedItem.ToString());
			}
			return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
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
			AccountInfo accountInfo = (_accounts ?? new List<AccountInfo>()).FirstOrDefault((AccountInfo a) => string.Equals(a.Id, profile.AccountId, StringComparison.OrdinalIgnoreCase));
			bool flag = ((chkProfileEnabled != null) ? chkProfileEnabled.Checked : profile.Enabled);
			int num = ((numProfileInterval != null) ? ((int)numProfileInterval.Value) : profile.IntervalMinutes);
			int profilePairCount = GetProfilePairCount(profile);
			string text = ((accountInfo != null) ? (accountInfo.Service ?? "unknown") : "missing");
			string text2 = ((accountInfo != null) ? accountInfo.Mode.ToString() : "missing");
			string text3 = ((accountInfo != null) ? (accountInfo.Label ?? "(unnamed)") : "(missing account)");
			lblProfileSummary.Text = $"Profile: {text3} | {text}/{text2} | {profilePairCount} pair(s) | every {num}m | max {profile.MaxTradesPerCycle}/cycle | cd {profile.CooldownMinutes}m | risk {profile.DailyRiskStopPct:0.0}%";
			lblProfileSummary.ForeColor = ((!flag || accountInfo == null || !accountInfo.Enabled) ? Color.DarkOrange : Color.DarkGreen);
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
			BrokerCapabilities capabilities = broker.GetCapabilities();
			if (capabilities == null)
			{
				return "broker capabilities unavailable";
			}
			if (!capabilities.SupportsMarketEntry)
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
				if (!capabilities.SupportsProtectiveExits && !SupportsLocalProtectiveWatchdog(account, broker))
				{
					return (!string.IsNullOrWhiteSpace(capabilities.Notes)) ? ("protective exits unsupported: " + capabilities.Notes) : "protective exits unsupported";
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
			return string.Equals(broker.Service, "coinbase-exchange", StringComparison.OrdinalIgnoreCase);
		}

		private void WriteCycleTelemetry(AutoCycleTelemetry cycleTelemetry)
		{
			if (cycleTelemetry == null)
			{
				return;
			}
			try
			{
				string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CryptoDayTraderSuite", "automode", "cycle_reports");
				if (!Directory.Exists(text))
				{
					Directory.CreateDirectory(text);
				}
				string text2 = (string.IsNullOrWhiteSpace(cycleTelemetry.CycleId) ? Guid.NewGuid().ToString("N") : cycleTelemetry.CycleId);
				string path = string.Format("cycle_{0}_{1}.json", DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff"), text2.Substring(0, Math.Min(8, text2.Length)));
				string text3 = Path.Combine(text, path);
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				string contents = javaScriptSerializer.Serialize(cycleTelemetry);
				File.WriteAllText(text3, contents);
				Log.Info("[AutoMode] Cycle telemetry written: " + text3, "WriteCycleTelemetry", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\AutoModeControl.cs", 2001);
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
				string cycleReportsDirectory = GetCycleReportsDirectory();
				if (!Directory.Exists(cycleReportsDirectory))
				{
					lblTelemetrySummary.Text = "Telemetry: no cycle reports";
					lblTelemetrySummary.ForeColor = Color.DimGray;
					return;
				}
				FileInfo fileInfo = (from f in new DirectoryInfo(cycleReportsDirectory).GetFiles("cycle_*.json")
					orderby f.LastWriteTimeUtc descending
					select f).FirstOrDefault();
				if (fileInfo == null)
				{
					lblTelemetrySummary.Text = "Telemetry: no cycle reports";
					lblTelemetrySummary.ForeColor = Color.DimGray;
					return;
				}
				string input = File.ReadAllText(fileInfo.FullName);
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				AutoCycleTelemetry autoCycleTelemetry = javaScriptSerializer.Deserialize<AutoCycleTelemetry>(input);
				if (autoCycleTelemetry == null)
				{
					lblTelemetrySummary.Text = "Telemetry: latest report unreadable";
					lblTelemetrySummary.ForeColor = Color.DarkOrange;
					return;
				}
				lblTelemetrySummary.Text = string.Format("Telemetry: {0} | profiles {1}/{2} | executed {3} | blocked {4} | gates {5} (no-signal:{6}, ai-veto:{7}, risk-veto:{8}, success:{9}) | matrix {10} (guardrails:{11}, containment:{12}) | {13}", fileInfo.Name, autoCycleTelemetry.ProcessedProfileCount, autoCycleTelemetry.EnabledProfileCount, autoCycleTelemetry.ExecutedProfiles, autoCycleTelemetry.FailedProfiles, string.IsNullOrWhiteSpace(autoCycleTelemetry.GateStatus) ? "n/a" : autoCycleTelemetry.GateStatus, autoCycleTelemetry.GateNoSignalObserved ? "obs" : "na", autoCycleTelemetry.GateAiVetoObserved ? "obs" : "na", autoCycleTelemetry.GateRiskVetoObserved ? "obs" : "na", autoCycleTelemetry.GateSuccessObserved ? "obs" : "na", string.IsNullOrWhiteSpace(autoCycleTelemetry.MatrixStatus) ? "n/a" : autoCycleTelemetry.MatrixStatus, autoCycleTelemetry.MatrixIndependentGuardrailsObserved ? "obs" : "na", autoCycleTelemetry.MatrixFailureContainmentObserved ? "obs" : "na", (autoCycleTelemetry.EndedUtc == default(DateTime)) ? "pending" : autoCycleTelemetry.EndedUtc.ToLocalTime().ToString("HH:mm:ss"));
				lblTelemetrySummary.ForeColor = ((autoCycleTelemetry.FailedProfiles > 0 || !string.Equals(autoCycleTelemetry.MatrixStatus, "PASS", StringComparison.OrdinalIgnoreCase) || !string.Equals(autoCycleTelemetry.GateStatus, "PASS", StringComparison.OrdinalIgnoreCase)) ? Color.DarkOrange : Color.DarkGreen);
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
			List<ProfileCycleTelemetry> source = cycleTelemetry.Profiles ?? new List<ProfileCycleTelemetry>();
			cycleTelemetry.MatrixHasSelectedScopeProfile = source.Any((ProfileCycleTelemetry p) => string.Equals(p.PairScope, "Selected", StringComparison.OrdinalIgnoreCase));
			cycleTelemetry.MatrixHasAllScopeProfile = source.Any((ProfileCycleTelemetry p) => string.Equals(p.PairScope, "All", StringComparison.OrdinalIgnoreCase));
			cycleTelemetry.MatrixPairConfigurationConsistent = source.All(delegate(ProfileCycleTelemetry p)
			{
				if (string.Equals(p.PairScope, "All", StringComparison.OrdinalIgnoreCase))
				{
					return p.SymbolCount > 0;
				}
				return (p.ExpectedSymbolCount <= 0) ? (p.SymbolCount > 0) : (p.SymbolCount == p.ExpectedSymbolCount);
			});
			cycleTelemetry.MatrixHasGuardrailValues = source.All((ProfileCycleTelemetry p) => p.MaxTradesPerCycle > 0 && p.CooldownMinutes > 0 && p.DailyRiskStopPct > 0m);
			List<ProfileCycleTelemetry> list = source.Where((ProfileCycleTelemetry p) => !string.IsNullOrWhiteSpace(p.ProfileId)).ToList();
			cycleTelemetry.MatrixGuardrailScopesIsolated = list.Count != 0 && list.Select((ProfileCycleTelemetry p) => p.GuardrailScopeKey ?? string.Empty).Distinct(StringComparer.OrdinalIgnoreCase).Count() == list.Select((ProfileCycleTelemetry p) => p.ProfileId).Distinct(StringComparer.OrdinalIgnoreCase).Count() && list.All((ProfileCycleTelemetry p) => !string.IsNullOrWhiteSpace(p.GuardrailScopeKey));
			cycleTelemetry.MatrixIndependentGuardrailsObserved = cycleTelemetry.MatrixHasGuardrailValues && cycleTelemetry.MatrixGuardrailScopesIsolated && list.Count > 0;
			bool flag = source.Any((ProfileCycleTelemetry p) => string.Equals(p.Status, "blocked", StringComparison.OrdinalIgnoreCase));
			bool flag2 = source.Any((ProfileCycleTelemetry p) => string.Equals(p.Status, "error", StringComparison.OrdinalIgnoreCase));
			bool flag3 = source.Any((ProfileCycleTelemetry p) => p.Failed > 0);
			bool flag4 = source.Any((ProfileCycleTelemetry p) => !string.Equals(p.Status, "blocked", StringComparison.OrdinalIgnoreCase) && !string.Equals(p.Status, "error", StringComparison.OrdinalIgnoreCase) && (string.Equals(p.Status, "completed", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "executed", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "skipped", StringComparison.OrdinalIgnoreCase)));
			bool flag5 = flag || flag2 || flag3;
			cycleTelemetry.MatrixFailureContainmentObserved = flag5 && cycleTelemetry.ProcessedProfileCount >= cycleTelemetry.EnabledProfileCount && flag4;
			cycleTelemetry.MatrixFailureDoesNotHaltCycle = !flag5 || cycleTelemetry.MatrixFailureContainmentObserved;
			cycleTelemetry.MatrixIsolationObserved = (!flag || flag4) && cycleTelemetry.MatrixFailureDoesNotHaltCycle;
			bool flag6 = cycleTelemetry.MatrixPairConfigurationConsistent && cycleTelemetry.MatrixIndependentGuardrailsObserved && cycleTelemetry.MatrixIsolationObserved;
			cycleTelemetry.MatrixStatus = (flag6 ? "PASS" : "PARTIAL");
		}

		private void EvaluateReliabilityGates(AutoCycleTelemetry cycleTelemetry)
		{
			if (cycleTelemetry != null)
			{
				List<ProfileCycleTelemetry> source = cycleTelemetry.Profiles ?? new List<ProfileCycleTelemetry>();
				cycleTelemetry.GateNoSignalObserved = source.Any((ProfileCycleTelemetry p) => p.NoSignalCount > 0);
				cycleTelemetry.GateAiVetoObserved = source.Any((ProfileCycleTelemetry p) => p.AiVetoCount > 0);
				cycleTelemetry.GateRiskVetoObserved = source.Any((ProfileCycleTelemetry p) => p.SkippedRisk > 0);
				cycleTelemetry.GateSuccessObserved = source.Any((ProfileCycleTelemetry p) => p.Executed > 0);
				bool flag = cycleTelemetry.GateNoSignalObserved && cycleTelemetry.GateAiVetoObserved && cycleTelemetry.GateRiskVetoObserved && cycleTelemetry.GateSuccessObserved;
				cycleTelemetry.GateStatus = (flag ? "PASS" : "PARTIAL");
			}
		}

		private List<string> RankPairs(IEnumerable<string> products)
		{
			List<string> all = (products ?? new List<string>()).Where((string p) => !string.IsNullOrWhiteSpace(p) && (p.EndsWith("-USD", StringComparison.OrdinalIgnoreCase) || p.EndsWith("/USD", StringComparison.OrdinalIgnoreCase))).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			List<string> popular = PopularPairs.Where((string p) => all.Any((string x) => string.Equals(x, p, StringComparison.OrdinalIgnoreCase))).ToList();
			List<string> second = (from p in all
				where !popular.Any((string x) => string.Equals(x, p, StringComparison.OrdinalIgnoreCase))
				orderby p
				select p).ToList();
			return popular.Concat(second).ToList();
		}

		private void PopulatePairList(List<string> rankedPairs)
		{
			if (_lstPairs == null)
			{
				return;
			}
			_lstPairs.Items.Clear();
			foreach (string rankedPair in rankedPairs)
			{
				_lstPairs.Items.Add(rankedPair, PopularPairs.Contains(rankedPair, StringComparer.OrdinalIgnoreCase));
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
			int num = 0;
			for (int i = 0; i < _lstPairs.Items.Count; i++)
			{
				if (num >= count)
				{
					break;
				}
				string value = _lstPairs.Items[i] as string;
				if (!string.IsNullOrWhiteSpace(value) && PopularPairs.Contains(value, StringComparer.OrdinalIgnoreCase))
				{
					_lstPairs.SetItemChecked(i, value: true);
					num++;
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
				List<string> list = new List<string>();
				foreach (object item in _lstPairs.Items)
				{
					string text = item as string;
					if (!string.IsNullOrWhiteSpace(text))
					{
						list.Add(text);
					}
				}
				if (list.Count > 0)
				{
					return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
				}
			}
			List<string> list2 = new List<string>();
			if (_lstPairs != null)
			{
				foreach (object checkedItem in _lstPairs.CheckedItems)
				{
					string text2 = checkedItem as string;
					if (!string.IsNullOrWhiteSpace(text2))
					{
						list2.Add(text2);
					}
				}
			}
			if (list2.Count == 0 && cmbProduct != null && cmbProduct.SelectedItem != null)
			{
				list2.Add(cmbProduct.SelectedItem.ToString());
			}
			return list2.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}

		private void SetComboProduct(string symbol)
		{
			if (cmbProduct == null || string.IsNullOrWhiteSpace(symbol))
			{
				return;
			}
			for (int i = 0; i < cmbProduct.Items.Count; i++)
			{
				string a = cmbProduct.Items[i] as string;
				if (string.Equals(a, symbol, StringComparison.OrdinalIgnoreCase))
				{
					cmbProduct.SelectedIndex = i;
					break;
				}
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

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
			((System.ComponentModel.ISupportInitialize)this.numProfileInterval).BeginInit();
			((System.ComponentModel.ISupportInitialize)this.numAutoInterval).BeginInit();
			((System.ComponentModel.ISupportInitialize)this.mainSplit).BeginInit();
			this.mainSplit.Panel1.SuspendLayout();
			this.mainSplit.Panel2.SuspendLayout();
			this.mainSplit.SuspendLayout();
			this.topPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)this.numLookback).BeginInit();
			((System.ComponentModel.ISupportInitialize)this.numEquity).BeginInit();
			((System.ComponentModel.ISupportInitialize)this.numMaxTradesPerCycle).BeginInit();
			((System.ComponentModel.ISupportInitialize)this.numCooldownMinutes).BeginInit();
			((System.ComponentModel.ISupportInitialize)this.numDailyRiskStopPct).BeginInit();
			this.rightLayout.SuspendLayout();
			this.actionPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)this.grid).BeginInit();
			this.footerPanel.SuspendLayout();
			base.SuspendLayout();
			this.tableLayout.ColumnCount = 1;
			this.tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.tableLayout.Controls.Add(this.headerPanel, 0, 0);
			this.tableLayout.Controls.Add(this.mainSplit, 0, 1);
			this.tableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayout.Location = new System.Drawing.Point(0, 0);
			this.tableLayout.Name = "tableLayout";
			this.tableLayout.RowCount = 2;
			this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.tableLayout.Size = new System.Drawing.Size(1200, 800);
			this.tableLayout.TabIndex = 0;
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
			this.lblAccount.AutoSize = true;
			this.lblAccount.Location = new System.Drawing.Point(11, 14);
			this.lblAccount.Margin = new System.Windows.Forms.Padding(3, 6, 0, 0);
			this.lblAccount.Name = "lblAccount";
			this.lblAccount.Size = new System.Drawing.Size(50, 13);
			this.lblAccount.TabIndex = 0;
			this.lblAccount.Text = "Account:";
			this.cmbAccount.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbAccount.FormattingEnabled = true;
			this.cmbAccount.Location = new System.Drawing.Point(64, 11);
			this.cmbAccount.Name = "cmbAccount";
			this.cmbAccount.Size = new System.Drawing.Size(180, 21);
			this.cmbAccount.TabIndex = 1;
			this.lblProfile.AutoSize = true;
			this.lblProfile.Location = new System.Drawing.Point(259, 14);
			this.lblProfile.Margin = new System.Windows.Forms.Padding(12, 6, 0, 0);
			this.lblProfile.Name = "lblProfile";
			this.lblProfile.Size = new System.Drawing.Size(39, 13);
			this.lblProfile.TabIndex = 2;
			this.lblProfile.Text = "Profile:";
			this.cmbProfile.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbProfile.FormattingEnabled = true;
			this.cmbProfile.Location = new System.Drawing.Point(301, 11);
			this.cmbProfile.Name = "cmbProfile";
			this.cmbProfile.Size = new System.Drawing.Size(170, 21);
			this.cmbProfile.TabIndex = 3;
			this.chkProfileEnabled.AutoSize = true;
			this.chkProfileEnabled.Checked = true;
			this.chkProfileEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chkProfileEnabled.Location = new System.Drawing.Point(477, 14);
			this.chkProfileEnabled.Name = "chkProfileEnabled";
			this.chkProfileEnabled.Size = new System.Drawing.Size(65, 17);
			this.chkProfileEnabled.TabIndex = 4;
			this.chkProfileEnabled.Text = "Enabled";
			this.chkProfileEnabled.UseVisualStyleBackColor = true;
			this.lblProfileEvery.AutoSize = true;
			this.lblProfileEvery.Location = new System.Drawing.Point(548, 14);
			this.lblProfileEvery.Margin = new System.Windows.Forms.Padding(3, 6, 0, 0);
			this.lblProfileEvery.Name = "lblProfileEvery";
			this.lblProfileEvery.Size = new System.Drawing.Size(68, 13);
			this.lblProfileEvery.TabIndex = 5;
			this.lblProfileEvery.Text = "Profile Every:";
			this.numProfileInterval.Location = new System.Drawing.Point(619, 11);
			this.numProfileInterval.Maximum = new decimal(new int[4] { 240, 0, 0, 0 });
			this.numProfileInterval.Minimum = new decimal(new int[4] { 1, 0, 0, 0 });
			this.numProfileInterval.Name = "numProfileInterval";
			this.numProfileInterval.Size = new System.Drawing.Size(50, 20);
			this.numProfileInterval.TabIndex = 6;
			this.numProfileInterval.Value = new decimal(new int[4] { 5, 0, 0, 0 });
			this.btnProfileSave.Location = new System.Drawing.Point(675, 11);
			this.btnProfileSave.Name = "btnProfileSave";
			this.btnProfileSave.Size = new System.Drawing.Size(86, 23);
			this.btnProfileSave.TabIndex = 7;
			this.btnProfileSave.Text = "Save Profile";
			this.btnProfileSave.UseVisualStyleBackColor = true;
			this.btnProfileDelete.Location = new System.Drawing.Point(767, 11);
			this.btnProfileDelete.Name = "btnProfileDelete";
			this.btnProfileDelete.Size = new System.Drawing.Size(88, 23);
			this.btnProfileDelete.TabIndex = 8;
			this.btnProfileDelete.Text = "Delete Profile";
			this.btnProfileDelete.UseVisualStyleBackColor = true;
			this.chkProfileAllPairs.AutoSize = true;
			this.chkProfileAllPairs.Location = new System.Drawing.Point(861, 14);
			this.chkProfileAllPairs.Name = "chkProfileAllPairs";
			this.chkProfileAllPairs.Size = new System.Drawing.Size(66, 17);
			this.chkProfileAllPairs.TabIndex = 9;
			this.chkProfileAllPairs.Text = "All Pairs";
			this.chkProfileAllPairs.UseVisualStyleBackColor = true;
			this.chkAutoRun.AutoSize = true;
			this.chkAutoRun.Location = new System.Drawing.Point(933, 14);
			this.chkAutoRun.Name = "chkAutoRun";
			this.chkAutoRun.Size = new System.Drawing.Size(71, 17);
			this.chkAutoRun.TabIndex = 10;
			this.chkAutoRun.Text = "Auto Run";
			this.chkAutoRun.UseVisualStyleBackColor = true;
			this.lblAutoEvery.AutoSize = true;
			this.lblAutoEvery.Location = new System.Drawing.Point(1010, 14);
			this.lblAutoEvery.Margin = new System.Windows.Forms.Padding(3, 6, 0, 0);
			this.lblAutoEvery.Name = "lblAutoEvery";
			this.lblAutoEvery.Size = new System.Drawing.Size(38, 13);
			this.lblAutoEvery.TabIndex = 11;
			this.lblAutoEvery.Text = "Every:";
			this.numAutoInterval.Location = new System.Drawing.Point(1051, 11);
			this.numAutoInterval.Maximum = new decimal(new int[4] { 60, 0, 0, 0 });
			this.numAutoInterval.Minimum = new decimal(new int[4] { 1, 0, 0, 0 });
			this.numAutoInterval.Name = "numAutoInterval";
			this.numAutoInterval.Size = new System.Drawing.Size(50, 20);
			this.numAutoInterval.TabIndex = 12;
			this.numAutoInterval.Value = new decimal(new int[4] { 5, 0, 0, 0 });
			this.chkLiveArm.AutoSize = true;
			this.chkLiveArm.Location = new System.Drawing.Point(1107, 14);
			this.chkLiveArm.Name = "chkLiveArm";
			this.chkLiveArm.Size = new System.Drawing.Size(68, 17);
			this.chkLiveArm.TabIndex = 13;
			this.chkLiveArm.Text = "Live Arm";
			this.chkLiveArm.UseVisualStyleBackColor = true;
			this.btnKillSwitch.BackColor = System.Drawing.Color.MistyRose;
			this.btnKillSwitch.Location = new System.Drawing.Point(1181, 11);
			this.btnKillSwitch.Name = "btnKillSwitch";
			this.btnKillSwitch.Size = new System.Drawing.Size(78, 23);
			this.btnKillSwitch.TabIndex = 14;
			this.btnKillSwitch.Text = "Kill Switch";
			this.btnKillSwitch.UseVisualStyleBackColor = false;
			this.lblAutoStatus.AutoSize = true;
			this.lblAutoStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
			this.lblAutoStatus.Location = new System.Drawing.Point(1265, 14);
			this.lblAutoStatus.Margin = new System.Windows.Forms.Padding(3, 6, 0, 0);
			this.lblAutoStatus.Name = "lblAutoStatus";
			this.lblAutoStatus.Size = new System.Drawing.Size(72, 13);
			this.lblAutoStatus.TabIndex = 15;
			this.lblAutoStatus.Text = "Status: OFF";
			this.mainSplit.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mainSplit.Location = new System.Drawing.Point(3, 53);
			this.mainSplit.Name = "mainSplit";
			this.mainSplit.Panel1.Controls.Add(this.topPanel);
			this.mainSplit.Panel2.Controls.Add(this.rightLayout);
			this.mainSplit.Size = new System.Drawing.Size(1194, 744);
			this.mainSplit.SplitterDistance = 360;
			this.mainSplit.TabIndex = 1;
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
			this.lblProduct.AutoSize = true;
			this.lblProduct.Location = new System.Drawing.Point(13, 13);
			this.lblProduct.Name = "lblProduct";
			this.lblProduct.Size = new System.Drawing.Size(47, 13);
			this.lblProduct.TabIndex = 0;
			this.lblProduct.Text = "Product:";
			this.cmbProduct.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbProduct.FormattingEnabled = true;
			this.cmbProduct.Location = new System.Drawing.Point(13, 29);
			this.cmbProduct.Name = "cmbProduct";
			this.cmbProduct.Size = new System.Drawing.Size(320, 21);
			this.cmbProduct.TabIndex = 1;
			this.lblGran.AutoSize = true;
			this.lblGran.Location = new System.Drawing.Point(13, 58);
			this.lblGran.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
			this.lblGran.Name = "lblGran";
			this.lblGran.Size = new System.Drawing.Size(57, 13);
			this.lblGran.TabIndex = 2;
			this.lblGran.Text = "Gran (min):";
			this.cmbGran.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbGran.FormattingEnabled = true;
			this.cmbGran.Items.AddRange(new object[6] { "1", "5", "15", "30", "60", "240" });
			this.cmbGran.Location = new System.Drawing.Point(13, 74);
			this.cmbGran.Name = "cmbGran";
			this.cmbGran.Size = new System.Drawing.Size(120, 21);
			this.cmbGran.TabIndex = 3;
			this.lblLookback.AutoSize = true;
			this.lblLookback.Location = new System.Drawing.Point(13, 103);
			this.lblLookback.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
			this.lblLookback.Name = "lblLookback";
			this.lblLookback.Size = new System.Drawing.Size(87, 13);
			this.lblLookback.TabIndex = 4;
			this.lblLookback.Text = "Lookback (days):";
			this.numLookback.Location = new System.Drawing.Point(13, 119);
			this.numLookback.Maximum = new decimal(new int[4] { 120, 0, 0, 0 });
			this.numLookback.Minimum = new decimal(new int[4] { 5, 0, 0, 0 });
			this.numLookback.Name = "numLookback";
			this.numLookback.Size = new System.Drawing.Size(120, 20);
			this.numLookback.TabIndex = 5;
			this.numLookback.Value = new decimal(new int[4] { 30, 0, 0, 0 });
			this.lblEquity.AutoSize = true;
			this.lblEquity.Location = new System.Drawing.Point(13, 147);
			this.lblEquity.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
			this.lblEquity.Name = "lblEquity";
			this.lblEquity.Size = new System.Drawing.Size(53, 13);
			this.lblEquity.TabIndex = 6;
			this.lblEquity.Text = "Equity ($):";
			this.numEquity.Location = new System.Drawing.Point(13, 163);
			this.numEquity.Maximum = new decimal(new int[4] { 1000000, 0, 0, 0 });
			this.numEquity.Minimum = new decimal(new int[4] { 10, 0, 0, 0 });
			this.numEquity.Name = "numEquity";
			this.numEquity.Size = new System.Drawing.Size(120, 20);
			this.numEquity.TabIndex = 7;
			this.numEquity.Value = new decimal(new int[4] { 1000, 0, 0, 0 });
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
			this.lblMaxTradesPerCycle.AutoSize = true;
			this.lblMaxTradesPerCycle.Location = new System.Drawing.Point(13, 216);
			this.lblMaxTradesPerCycle.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
			this.lblMaxTradesPerCycle.Name = "lblMaxTradesPerCycle";
			this.lblMaxTradesPerCycle.Size = new System.Drawing.Size(90, 13);
			this.lblMaxTradesPerCycle.TabIndex = 9;
			this.lblMaxTradesPerCycle.Text = "Max trades/cycle:";
			this.numMaxTradesPerCycle.Location = new System.Drawing.Point(13, 232);
			this.numMaxTradesPerCycle.Maximum = new decimal(new int[4] { 20, 0, 0, 0 });
			this.numMaxTradesPerCycle.Minimum = new decimal(new int[4] { 1, 0, 0, 0 });
			this.numMaxTradesPerCycle.Name = "numMaxTradesPerCycle";
			this.numMaxTradesPerCycle.Size = new System.Drawing.Size(120, 20);
			this.numMaxTradesPerCycle.TabIndex = 10;
			this.numMaxTradesPerCycle.Value = new decimal(new int[4] { 3, 0, 0, 0 });
			this.lblCooldownMinutes.AutoSize = true;
			this.lblCooldownMinutes.Location = new System.Drawing.Point(13, 260);
			this.lblCooldownMinutes.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
			this.lblCooldownMinutes.Name = "lblCooldownMinutes";
			this.lblCooldownMinutes.Size = new System.Drawing.Size(88, 13);
			this.lblCooldownMinutes.TabIndex = 11;
			this.lblCooldownMinutes.Text = "Cooldown (min):";
			this.numCooldownMinutes.Location = new System.Drawing.Point(13, 276);
			this.numCooldownMinutes.Maximum = new decimal(new int[4] { 240, 0, 0, 0 });
			this.numCooldownMinutes.Minimum = new decimal(new int[4] { 1, 0, 0, 0 });
			this.numCooldownMinutes.Name = "numCooldownMinutes";
			this.numCooldownMinutes.Size = new System.Drawing.Size(120, 20);
			this.numCooldownMinutes.TabIndex = 12;
			this.numCooldownMinutes.Value = new decimal(new int[4] { 30, 0, 0, 0 });
			this.lblDailyRiskStopPct.AutoSize = true;
			this.lblDailyRiskStopPct.Location = new System.Drawing.Point(13, 304);
			this.lblDailyRiskStopPct.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
			this.lblDailyRiskStopPct.Name = "lblDailyRiskStopPct";
			this.lblDailyRiskStopPct.Size = new System.Drawing.Size(68, 13);
			this.lblDailyRiskStopPct.TabIndex = 13;
			this.lblDailyRiskStopPct.Text = "Daily Risk %:";
			this.numDailyRiskStopPct.DecimalPlaces = 1;
			this.numDailyRiskStopPct.Increment = new decimal(new int[4] { 5, 0, 0, 65536 });
			this.numDailyRiskStopPct.Location = new System.Drawing.Point(13, 320);
			this.numDailyRiskStopPct.Maximum = new decimal(new int[4] { 50, 0, 0, 0 });
			this.numDailyRiskStopPct.Minimum = new decimal(new int[4] { 1, 0, 0, 0 });
			this.numDailyRiskStopPct.Name = "numDailyRiskStopPct";
			this.numDailyRiskStopPct.Size = new System.Drawing.Size(120, 20);
			this.numDailyRiskStopPct.TabIndex = 14;
			this.numDailyRiskStopPct.Value = new decimal(new int[4] { 3, 0, 0, 0 });
			this.rightLayout.ColumnCount = 1;
			this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.rightLayout.Controls.Add(this.actionPanel, 0, 0);
			this.rightLayout.Controls.Add(this.grid, 0, 1);
			this.rightLayout.Controls.Add(this.footerPanel, 0, 2);
			this.rightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
			this.rightLayout.Location = new System.Drawing.Point(0, 0);
			this.rightLayout.Name = "rightLayout";
			this.rightLayout.RowCount = 3;
			this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.rightLayout.Size = new System.Drawing.Size(830, 744);
			this.rightLayout.TabIndex = 0;
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
			this.btnScan.Location = new System.Drawing.Point(7, 7);
			this.btnScan.Name = "btnScan";
			this.btnScan.Size = new System.Drawing.Size(75, 23);
			this.btnScan.TabIndex = 0;
			this.btnScan.Text = "Scan";
			this.btnScan.UseVisualStyleBackColor = true;
			this.btnPropose.Location = new System.Drawing.Point(88, 7);
			this.btnPropose.Name = "btnPropose";
			this.btnPropose.Size = new System.Drawing.Size(75, 23);
			this.btnPropose.TabIndex = 1;
			this.btnPropose.Text = "Propose";
			this.btnPropose.UseVisualStyleBackColor = true;
			this.btnExecute.Location = new System.Drawing.Point(169, 7);
			this.btnExecute.Name = "btnExecute";
			this.btnExecute.Size = new System.Drawing.Size(75, 23);
			this.btnExecute.TabIndex = 2;
			this.btnExecute.Text = "Execute";
			this.btnExecute.UseVisualStyleBackColor = true;
			this.grid.AllowUserToAddRows = false;
			this.grid.AllowUserToDeleteRows = false;
			this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.grid.Columns.AddRange(this.colStrategy, this.colSymbol, this.colGran, this.colExpectancy, this.colWinRate, this.colAvgWin, this.colAvgLoss, this.colSharpe, this.colSamples);
			this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.grid.Location = new System.Drawing.Point(3, 44);
			this.grid.Name = "grid";
			this.grid.ReadOnly = true;
			this.grid.RowHeadersVisible = false;
			this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.grid.Size = new System.Drawing.Size(824, 647);
			this.grid.TabIndex = 1;
			this.colStrategy.DataPropertyName = "Strategy";
			this.colStrategy.FillWeight = 180f;
			this.colStrategy.HeaderText = "Strategy";
			this.colStrategy.Name = "colStrategy";
			this.colStrategy.ReadOnly = true;
			this.colSymbol.DataPropertyName = "Symbol";
			this.colSymbol.HeaderText = "Symbol";
			this.colSymbol.Name = "colSymbol";
			this.colSymbol.ReadOnly = true;
			this.colGran.DataPropertyName = "GranMinutes";
			this.colGran.HeaderText = "Granularity";
			this.colGran.Name = "colGran";
			this.colGran.ReadOnly = true;
			this.colExpectancy.DataPropertyName = "Expectancy";
			this.colExpectancy.HeaderText = "Expectancy";
			this.colExpectancy.Name = "colExpectancy";
			this.colExpectancy.ReadOnly = true;
			this.colWinRate.DataPropertyName = "WinRate";
			this.colWinRate.HeaderText = "Win%";
			this.colWinRate.Name = "colWinRate";
			this.colWinRate.ReadOnly = true;
			this.colAvgWin.DataPropertyName = "AvgWin";
			this.colAvgWin.HeaderText = "AvgWin";
			this.colAvgWin.Name = "colAvgWin";
			this.colAvgWin.ReadOnly = true;
			this.colAvgLoss.DataPropertyName = "AvgLoss";
			this.colAvgLoss.HeaderText = "AvgLoss";
			this.colAvgLoss.Name = "colAvgLoss";
			this.colAvgLoss.ReadOnly = true;
			this.colSharpe.DataPropertyName = "SharpeApprox";
			this.colSharpe.HeaderText = "Sharpe";
			this.colSharpe.Name = "colSharpe";
			this.colSharpe.ReadOnly = true;
			this.colSamples.DataPropertyName = "Samples";
			this.colSamples.HeaderText = "Samples";
			this.colSamples.Name = "colSamples";
			this.colSamples.ReadOnly = true;
			this.footerPanel.AutoSize = true;
			this.footerPanel.ColumnCount = 1;
			this.footerPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
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
			this.lblProfileSummary.AutoSize = true;
			this.lblProfileSummary.Location = new System.Drawing.Point(3, 0);
			this.lblProfileSummary.Name = "lblProfileSummary";
			this.lblProfileSummary.Size = new System.Drawing.Size(117, 13);
			this.lblProfileSummary.TabIndex = 0;
			this.lblProfileSummary.Text = "Profile: (not selected)";
			this.lblTelemetrySummary.AutoSize = true;
			this.lblTelemetrySummary.ForeColor = System.Drawing.Color.DimGray;
			this.lblTelemetrySummary.Location = new System.Drawing.Point(3, 13);
			this.lblTelemetrySummary.Name = "lblTelemetrySummary";
			this.lblTelemetrySummary.Size = new System.Drawing.Size(142, 13);
			this.lblTelemetrySummary.TabIndex = 1;
			this.lblTelemetrySummary.Text = "Telemetry: no cycle reports";
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(this.tableLayout);
			base.Name = "AutoModeControl";
			base.Size = new System.Drawing.Size(1200, 800);
			this.tableLayout.ResumeLayout(false);
			this.tableLayout.PerformLayout();
			this.headerPanel.ResumeLayout(false);
			this.headerPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)this.numProfileInterval).EndInit();
			((System.ComponentModel.ISupportInitialize)this.numAutoInterval).EndInit();
			this.mainSplit.Panel1.ResumeLayout(false);
			this.mainSplit.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)this.mainSplit).EndInit();
			this.mainSplit.ResumeLayout(false);
			this.topPanel.ResumeLayout(false);
			this.topPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)this.numLookback).EndInit();
			((System.ComponentModel.ISupportInitialize)this.numEquity).EndInit();
			((System.ComponentModel.ISupportInitialize)this.numMaxTradesPerCycle).EndInit();
			((System.ComponentModel.ISupportInitialize)this.numCooldownMinutes).EndInit();
			((System.ComponentModel.ISupportInitialize)this.numDailyRiskStopPct).EndInit();
			this.rightLayout.ResumeLayout(false);
			this.rightLayout.PerformLayout();
			this.actionPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)this.grid).EndInit();
			this.footerPanel.ResumeLayout(false);
			this.footerPanel.PerformLayout();
			base.ResumeLayout(false);
		}
	}
}

