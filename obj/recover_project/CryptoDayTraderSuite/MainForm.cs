using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using CryptoDayTraderSuite.Backtest;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Services.Messaging;
using CryptoDayTraderSuite.Services.Messaging.Events;
using CryptoDayTraderSuite.Strategy;
using CryptoDayTraderSuite.Themes;
using CryptoDayTraderSuite.UI;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite
{
	public class MainForm : Form
	{
		private AutoPlannerService _autoPlanner;

		private IExchangeProvider _exchangeProvider;

		private BacktestService _backtestService;

		private IEventBus _eventBus;

		private IAccountService _accountService;

		private IKeyService _keyService;

		private IProfileService _profileService;

		private IAutoModeProfileService _autoModeProfileService;

		private IHistoryService _historyService;

		private ChromeSidecar _sidecar;

		private AIGovernor _governor;

		private IRateRouter _rateRouter;

		private IExchangeClient _client = null;

		private StrategyEngine _engine = new StrategyEngine();

		private FeeSchedule _fees = new FeeSchedule
		{
			MakerRate = 0.0040m,
			TakerRate = 0.0060m,
			Notes = "default"
		};

		private SidebarControl _sidebar;

		private TableLayoutPanel _shellLayout;

		private Panel _contentPanel;

		private ToolStripStatusLabel _statusLabel;

		private TradingControl _tradingControl;

		private AutoModeControl _autoModeControl;

		private Dictionary<string, Control> _views = new Dictionary<string, Control>();

		private readonly List<string> _logBuffer = new List<string>();

		private string _lastLogMessage;

		private IContainer components = null;

		private ComboBox cmbExchange;

		private ComboBox cmbProduct;

		private Button btnLoadProducts;

		private Button btnBacktest;

		private Button btnPaper;

		private Button btnLive;

		private TextBox txtLog;

		private NumericUpDown numRisk;

		private NumericUpDown numEquity;

		private ComboBox cmbStrategy;

		private Button btnFees;

		private TextBox txtApiKey;

		private TextBox txtSecret;

		private TextBox txtExtra;

		private Button btnSaveKeys;

		private Label lblProj100;

		private Label lblProj1000;

		private readonly PredictionEngine _predict = new PredictionEngine(new PredictionConfig());

		private readonly TradePlanner _planner = new TradePlanner();

		private PlannerForm _plannerForm = null;

		private TextBox _txtLog;

		private TableLayoutPanel _tl;

		private FlowLayoutPanel _top;

		private SplitContainer _split;

		private Control _chartArea;

		private bool _isFull = false;

		private FormWindowState _prevState;

		private FormBorderStyle _prevBorder;

		private Rectangle _prevBounds;

		private ToolTip _tt;

		public AutoPlannerService AutoPlanner => _autoPlanner;

		public IExchangeProvider ExchangeProvider => _exchangeProvider;

		public IAccountService AccountService => _accountService;

		public IKeyService KeyService => _keyService;

		public IProfileService ProfileService => _profileService;

		public IHistoryService HistoryService => _historyService;

		public MainForm(IProfileService profileService)
		{
			InitializeComponent();
			_profileService = profileService;
			if (_keyService == null)
			{
				_keyService = new KeyService();
			}
			if (_exchangeProvider == null)
			{
				_exchangeProvider = new ExchangeProvider(_keyService);
			}
			if (_accountService == null)
			{
				_accountService = new AccountService();
			}
			if (cmbExchange.Items.Count > 0)
			{
				cmbExchange.SelectedIndex = 0;
			}
			if (cmbStrategy.Items.Count > 0)
			{
				cmbStrategy.SelectedIndex = 0;
			}
			try
			{
				LoadCoinbaseCdpKeys();
			}
			catch
			{
			}
			BuildModernLayout();
			Log("MainForm constructed");
		}

		public void InitializeDependencies(AutoPlannerService planner, IExchangeProvider provider, BacktestService backtester, IEventBus eventBus, IAccountService accountService, IKeyService keyService, IHistoryService historyService, IAutoModeProfileService autoModeProfileService, ChromeSidecar sidecar, StrategyEngine engine, IRateRouter rateRouter, AIGovernor governor)
		{
			_autoPlanner = planner;
			_exchangeProvider = provider;
			_backtestService = backtester;
			_eventBus = eventBus;
			_accountService = accountService;
			_keyService = keyService;
			_historyService = historyService;
			_autoModeProfileService = autoModeProfileService;
			_sidecar = sidecar;
			_engine = engine;
			_rateRouter = rateRouter;
			_governor = governor;
			if (_sidecar != null)
			{
				Task.Run(async delegate
				{
					if (await _sidecar.ConnectAsync())
					{
						Log("Chrome Sidecar Connected");
					}
					else
					{
						Log("Chrome Sidecar: Not Found (Run Chrome with --remote-debugging-port=9222)");
					}
				});
			}
			if (_eventBus != null)
			{
				_eventBus.Subscribe<LogEvent>(OnLogEvent);
			}
			_views.Clear();
			_autoModeControl = null;
			base.Controls.Clear();
			BuildModernLayout();
		}

		private void OnLogEvent(LogEvent evt)
		{
			if (base.InvokeRequired)
			{
				BeginInvoke(new Action<LogEvent>(OnLogEvent), evt);
			}
			else if (!string.IsNullOrWhiteSpace(evt.Message) && !string.Equals(_lastLogMessage, evt.Message, StringComparison.Ordinal))
			{
				_lastLogMessage = evt.Message;
				string line = evt.Timestamp.ToString("HH:mm:ss") + " " + evt.Message;
				_logBuffer.Add(line);
				if (_logBuffer.Count > 500)
				{
					_logBuffer.RemoveAt(0);
				}
				if (_statusLabel != null)
				{
					_statusLabel.Text = line;
				}
				if (_tradingControl != null)
				{
					_tradingControl.Log(line);
				}
			}
		}

		private void Log(string s)
		{
			if (_eventBus != null)
			{
				_eventBus.Publish(new LogEvent(s));
			}
			else if (_tradingControl != null)
			{
				_tradingControl.Log(s);
			}
		}

		private void LoadCoinbaseCdpKeys()
		{
			if (_keyService == null)
			{
				return;
			}
			try
			{
				string cdpFile = Path.Combine(Application.StartupPath, "cdp_api_key.json");
				if (File.Exists(cdpFile))
				{
					string json = File.ReadAllText(cdpFile);
					Dictionary<string, object> cdpData = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
					if (cdpData != null && cdpData.ContainsKey("name") && cdpData.ContainsKey("privateKey"))
					{
						KeyInfo keyInfo = new KeyInfo
						{
							Broker = "Coinbase",
							Label = "CDP Auto-loaded",
							ApiKey = cdpData["name"].ToString(),
							Secret = cdpData["privateKey"].ToString(),
							Passphrase = "",
							CreatedUtc = DateTime.UtcNow,
							Enabled = true
						};
						_keyService.Upsert(keyInfo);
						_keyService.SetActive("Coinbase", "CDP Auto-loaded");
						Log("loaded coinbase CDP key");
					}
				}
			}
			catch (Exception ex)
			{
				Log("error loading CDP key: " + ex.Message);
			}
		}

		private IExchangeClient BuildClient()
		{
			string exch = "Coinbase";
			if (_tradingControl != null)
			{
				exch = _tradingControl.Exchange ?? "Coinbase";
			}
			else if (cmbExchange.SelectedItem != null)
			{
				exch = cmbExchange.SelectedItem.ToString();
			}
			return _exchangeProvider.CreateAuthenticatedClient(exch);
		}

		private async void btnLoadProducts_Click(object sender, EventArgs e)
		{
			try
			{
				string exch = "Coinbase";
				if (_tradingControl != null)
				{
					exch = _tradingControl.Exchange ?? "Coinbase";
				}
				_client = _exchangeProvider.CreatePublicClient(exch);
				List<string> usdPairs = (await _client.ListProductsAsync()).Where((string x) => x.Contains("/USD") || x.Contains("-USD")).ToList();
				if (_tradingControl != null)
				{
					_tradingControl.SetProducts(usdPairs);
				}
				Log("loaded products: " + usdPairs.Count);
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Log("error loading products " + ex2.Message);
			}
		}

		private async Task<List<Candle>> Load1mCandles(string productId, DateTime startUtc, DateTime endUtc)
		{
			if (_client == null)
			{
				_client = _exchangeProvider.CreatePublicClient("Coinbase");
			}
			return await _client.GetCandlesAsync(productId, 1, startUtc, endUtc);
		}

		private async void btnFees_Click(object sender, EventArgs e)
		{
			try
			{
				_client = BuildClient();
				_fees = await _client.GetFeesAsync();
				Log("fees maker " + (_fees.MakerRate * 100m).ToString("0.###") + "% taker " + (_fees.TakerRate * 100m).ToString("0.###") + "% " + _fees.Notes);
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Log("error getting fees " + ex2.Message);
			}
		}

		private async void btnBacktest_Click(object sender, EventArgs e)
		{
			try
			{
				if (_backtestService == null)
				{
					Log("BacktestService not initialized");
					return;
				}
				string product = ((_tradingControl != null) ? _tradingControl.Product : null) ?? "BTC-USD";
				string strat = ((_tradingControl != null) ? _tradingControl.Strategy : null) ?? "ORB";
				decimal risk = ((_tradingControl != null) ? _tradingControl.Risk : 1m);
				decimal equity = ((_tradingControl != null) ? _tradingControl.Equity : 1000m);
				string exchange = ((_tradingControl != null) ? _tradingControl.Exchange : "Coinbase") ?? "Coinbase";
				Log("starting backtest...");
				BacktestResultWrapper res = await _backtestService.RunBacktestAsync(exchange, product, strat, risk, equity, _fees);
				if (res.Error != null)
				{
					Log("Backtest Error: " + res.Error);
					return;
				}
				if (_tradingControl != null && res.Candles != null)
				{
					_tradingControl.SetCandles(res.Candles);
				}
				if (res.Candles != null && res.Candles.Count > 0)
				{
					LearnFromCandles(product, res.Candles);
					await AnalyzeAndPlanAsync(product, res.Candles);
				}
				Backtester.Result r = res.RunResult;
				Log("backtest " + product + " trades " + r.Trades + " pnl $" + r.PnL.ToString("0.00") + " win " + (r.WinRate * 100m).ToString("0.0") + "% mdd " + (r.MaxDrawdown * 100m).ToString("0.0") + "%");
				UpdateProjections();
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Log("backtest fatal error " + ex2.Message);
			}
		}

		private void UpdateProjections()
		{
			decimal riskFrac = ((_tradingControl != null) ? _tradingControl.Risk : 1m) / 100m;
			decimal roundtrip = _fees.MakerRate + _fees.TakerRate + 0.0005m;
			ProjectionInput p = new ProjectionInput
			{
				StartingEquity = 100m,
				TradesPerDay = 10,
				WinRate = 0.52m,
				AvgWinR = 1.1m,
				AvgLossR = 1.0m,
				RiskPerTradeFraction = riskFrac,
				NetFeeAndFrictionRate = roundtrip,
				Days = 20
			};
			ProjectionResult r = Projections.Compute(p);
			string s100 = " projection: " + r.EndingEquity.ToString("0.00") + " daily " + r.DailyExpectedReturnPct.ToString("0.00") + "%";
			p.StartingEquity = 1000m;
			ProjectionResult r2 = Projections.Compute(p);
			string s1000 = " projection: " + r2.EndingEquity.ToString("0.00");
			if (_tradingControl != null)
			{
				_tradingControl.SetProjections(s100, s1000);
			}
		}

		private async void btnPaper_Click(object sender, EventArgs e)
		{
			try
			{
				_client = BuildClient();
				string product = ((_tradingControl != null) ? _tradingControl.Product : null) ?? "BTC-USD";
				string strat = ((_tradingControl != null) ? _tradingControl.Strategy : null) ?? "ORB";
				DateTime end = DateTime.UtcNow;
				DateTime start = end.AddHours(-8.0);
				List<Candle> candles = await _client.GetCandlesAsync(product, 1, start, end);
				if (_tradingControl != null)
				{
					_tradingControl.SetCandles(candles);
				}
				if (candles != null && candles.Count > 0)
				{
					await AnalyzeAndPlanAsync(product, candles);
				}
				_engine.SetStrategy(strat);
				decimal riskFrac = ((_tradingControl != null) ? _tradingControl.Risk : 1m) / 100m;
				decimal equity = ((_tradingControl != null) ? _tradingControl.Equity : 1000m);
				if (candles == null || candles.Count == 0)
				{
					Log("No candles");
					return;
				}
				decimal price = candles.Last().Close;
				CostBreakdown cb;
				OrderRequest order = _engine.Evaluate(product, candles, _fees, equity, riskFrac, price, out cb);
				if (order == null)
				{
					Log("no paper signal");
					return;
				}
				Log("paper trade " + order.Side.ToString() + " " + order.Quantity + " " + product);
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Log("paper error " + ex2.Message);
			}
		}

		private async void btnLive_Click(object sender, EventArgs e)
		{
			try
			{
				_client = BuildClient();
				string product = ((_tradingControl != null) ? _tradingControl.Product : null) ?? "BTC-USD";
				string strat = ((_tradingControl != null) ? _tradingControl.Strategy : null) ?? "ORB";
				DateTime end = DateTime.UtcNow;
				DateTime start = end.AddHours(-8.0);
				List<Candle> candles = await _client.GetCandlesAsync(product, 1, start, end);
				if (_tradingControl != null)
				{
					_tradingControl.SetCandles(candles);
				}
				if (candles != null && candles.Count > 0)
				{
					await AnalyzeAndPlanAsync(product, candles);
				}
				_engine.SetStrategy(strat);
				decimal riskFrac = ((_tradingControl != null) ? _tradingControl.Risk : 1m) / 100m;
				decimal equity = ((_tradingControl != null) ? _tradingControl.Equity : 1000m);
				Ticker ticker = await _client.GetTickerAsync(product);
				CostBreakdown cb;
				OrderRequest order = _engine.Evaluate(product, candles, _fees, equity, riskFrac, ticker.Last, out cb);
				if (order == null)
				{
					Log("no live signal");
					return;
				}
				if (!order.StopLoss.HasValue || !order.TakeProfit.HasValue || order.StopLoss.Value <= 0m || order.TakeProfit.Value <= 0m)
				{
					Log("live order blocked: protective stop/target missing");
					return;
				}
				OrderResult res = await _client.PlaceOrderAsync(order);
				Log("live order " + res.OrderId + " " + res.Message);
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Log("live error " + ex2.Message);
			}
		}

		private void btnSaveKeys_Click(object sender, EventArgs e)
		{
			try
			{
				if (_keyService == null)
				{
					MessageBox.Show("Key service unavailable.");
					return;
				}
				string broker = ((cmbExchange != null && cmbExchange.SelectedItem != null) ? cmbExchange.SelectedItem.ToString() : "Coinbase");
				string label = "Manual";
				KeyInfo keyInfo = new KeyInfo
				{
					Broker = broker,
					Label = label,
					ApiKey = ((txtApiKey != null) ? txtApiKey.Text.Trim() : string.Empty),
					Secret = ((txtSecret != null) ? txtSecret.Text.Trim() : string.Empty),
					Passphrase = ((txtExtra != null) ? txtExtra.Text.Trim() : string.Empty),
					CreatedUtc = DateTime.UtcNow,
					Enabled = true,
					Active = true,
					Service = broker
				};
				_keyService.Upsert(keyInfo);
				_keyService.SetActive(broker, label);
				Log("keys saved for " + broker + " (" + label + ")");
			}
			catch (Exception ex)
			{
				Log("error saving keys " + ex.Message);
			}
		}

		private void BuildModernLayout()
		{
			base.Controls.Clear();
			Theme.Apply(this);
			_sidebar = new SidebarControl();
			_sidebar.Dock = DockStyle.Fill;
			_sidebar.NavigationSelected += OnNavigationSelected;
			if (_governor != null)
			{
				_sidebar.Configure(_governor);
			}
			_shellLayout = new TableLayoutPanel();
			_shellLayout.Dock = DockStyle.Fill;
			_shellLayout.ColumnCount = 2;
			_shellLayout.RowCount = 1;
			_shellLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, _sidebar.Width));
			_shellLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
			_shellLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
			_contentPanel = new Panel();
			_contentPanel.Dock = DockStyle.Fill;
			_contentPanel.BackColor = Theme.ContentBg;
			_shellLayout.Controls.Add(_sidebar, 0, 0);
			_shellLayout.Controls.Add(_contentPanel, 1, 0);
			_sidebar.SizeChanged -= Sidebar_SizeChanged;
			_sidebar.SizeChanged += Sidebar_SizeChanged;
			Sidebar_SizeChanged(_sidebar, EventArgs.Empty);
			StatusStrip statusStrip = new StatusStrip();
			statusStrip.Dock = DockStyle.Bottom;
			statusStrip.BackColor = Theme.PanelBg;
			statusStrip.ForeColor = Theme.TextMuted;
			_statusLabel = new ToolStripStatusLabel();
			_statusLabel.Text = "Ready";
			_statusLabel.Spring = true;
			_statusLabel.TextAlign = ContentAlignment.MiddleLeft;
			statusStrip.Items.Add(_statusLabel);
			base.Controls.Add(statusStrip);
			base.Controls.Add(_shellLayout);
			OnNavigationSelected("Dashboard");
			EnsureAutoModeControlInitialized();
		}

		private void Sidebar_SizeChanged(object sender, EventArgs e)
		{
			if (_shellLayout != null && _shellLayout.ColumnStyles.Count >= 1 && _sidebar != null)
			{
				int width = _sidebar.Width;
				if (width < 48)
				{
					width = 48;
				}
				_shellLayout.ColumnStyles[0].Width = width;
			}
		}

		public void NavigateTo(string page)
		{
			_sidebar?.SetActivePage(page);
			OnNavigationSelected(page);
		}

		private void OnNavigationSelected(string page)
		{
			Control view = null;
			if (_views.ContainsKey(page))
			{
				view = _views[page];
			}
			else
			{
				view = CreateView(page);
				if (view != null)
				{
					_views[page] = view;
				}
			}
			_contentPanel.Controls.Clear();
			if (view != null)
			{
				view.Dock = DockStyle.Fill;
				_contentPanel.Controls.Add(view);
			}
		}

		private Control CreateView(string page)
		{
			try
			{
				switch (page)
				{
				case "Dashboard":
				{
					DashboardControl db = new DashboardControl();
					if (_accountService != null)
					{
						db.Initialize(_accountService, _historyService ?? new HistoryService(), _governor);
					}
					Theme.Apply(db);
					return db;
				}
				case "Trading":
					_tradingControl = new TradingControl();
					_tradingControl.LoadProductsClicked += btnLoadProducts_Click;
					_tradingControl.FeesClicked += btnFees_Click;
					_tradingControl.BacktestClicked += btnBacktest_Click;
					_tradingControl.PaperClicked += btnPaper_Click;
					_tradingControl.LiveClicked += btnLive_Click;
					_tradingControl.SetExchanges(new string[3] { "Coinbase", "Kraken", "Bitstamp" });
					_tradingControl.SetStrategies(new string[4] { "ORB", "VWAPTrend", "RSIReversion", "Donchian 20" });
					if (_logBuffer.Count > 0)
					{
						foreach (string line in _logBuffer)
						{
							_tradingControl.Log(line);
						}
					}
					Theme.Apply(_tradingControl);
					return _tradingControl;
				case "Planner":
				{
					PlannerControl pc = new PlannerControl();
					pc.Initialize(_historyService ?? new HistoryService(), _autoPlanner, (_exchangeProvider != null) ? _exchangeProvider.CreatePublicClient("Coinbase") : null, _accountService, _keyService);
					Theme.Apply(pc);
					return pc;
				}
				case "Auto":
					EnsureAutoModeControlInitialized();
					return _autoModeControl;
				case "Accounts":
					return CreateSetupCenterView("Accounts", includeStrategyButton: false);
				case "Keys":
					return CreateSetupCenterView("API Keys", includeStrategyButton: false);
				case "Settings":
					return CreateSetupCenterView("Accounts", includeStrategyButton: true);
				case "Profiles":
				{
					ProfileManagerControl pm = new ProfileManagerControl();
					pm.Initialize(_keyService, _accountService, _profileService);
					ApplyThemeToControl(pm);
					return pm;
				}
				default:
					return null;
				}
			}
			catch (Exception ex)
			{
				Log("Error creating view " + page + ": " + ex.Message);
				return new Label
				{
					Text = "Error: " + ex.Message
				};
			}
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			LoadAI();
			if (_sidebar == null || _contentPanel == null)
			{
				BuildModernLayout();
			}
			MinimumSize = new Size(900, 600);
			base.KeyPreview = true;
			base.KeyDown += delegate(object s, KeyEventArgs a)
			{
				if (a.KeyCode == Keys.F11)
				{
					ToggleFullScreen();
				}
			};
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			base.OnFormClosing(e);
			if (_eventBus != null)
			{
				_eventBus.Unsubscribe<LogEvent>(OnLogEvent);
			}
			SaveAI();
		}

		private void ApplyThemeToControl(Control c)
		{
			c.BackColor = Theme.ContentBg;
			c.ForeColor = Theme.Text;
			foreach (Control child in c.Controls)
			{
				ApplyThemeToControlRecursively(child);
			}
		}

		private void EnsureAutoModeControlInitialized()
		{
			if (_autoModeControl == null || _autoModeControl.IsDisposed)
			{
				_autoModeControl = new AutoModeControl();
				if (_autoPlanner != null && _exchangeProvider != null)
				{
					IExchangeClient pubClient = _exchangeProvider.CreatePublicClient("Coinbase");
					_autoModeControl.Initialize(_autoPlanner, pubClient, _accountService, _keyService, _autoModeProfileService, _historyService ?? new HistoryService());
				}
				Theme.Apply(_autoModeControl);
				_views["Auto"] = _autoModeControl;
			}
		}

		private void ApplyThemeToControlRecursively(Control c)
		{
			if (c is Button b)
			{
				b.FlatStyle = FlatStyle.Flat;
				b.FlatAppearance.BorderColor = Theme.PanelBg;
				b.BackColor = Theme.PanelBg;
				b.ForeColor = Theme.Text;
				return;
			}
			if (c is TextBox t)
			{
				t.BackColor = Theme.PanelBg;
				t.ForeColor = Theme.Text;
				t.BorderStyle = BorderStyle.FixedSingle;
				return;
			}
			c.BackColor = Theme.PanelBg;
			c.ForeColor = Theme.Text;
			if (!c.HasChildren)
			{
				return;
			}
			foreach (Control child in c.Controls)
			{
				ApplyThemeToControlRecursively(child);
			}
		}

		private Control CreateSetupCenterView(string initialTab, bool includeStrategyButton)
		{
			FlowLayoutPanel settingsPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.TopDown,
				AutoScroll = true,
				WrapContents = false
			};
			settingsPanel.BackColor = Theme.ContentBg;
			Label lblSetup = new Label
			{
				Text = "Setup Center",
				Font = new Font("Segoe UI", 14f, FontStyle.Bold),
				AutoSize = true,
				ForeColor = Theme.Text,
				Margin = new Padding(10)
			};
			settingsPanel.Controls.Add(lblSetup);
			TabControl setupTabs = new TabControl
			{
				Width = 920,
				Height = 520,
				Margin = new Padding(10)
			};
			TabPage tabAccounts = new TabPage("Accounts");
			TabPage tabKeys = new TabPage("API Keys");
			TabPage tabProfiles = new TabPage("Profiles");
			AccountsControl setupAccounts = new AccountsControl();
			if (_accountService != null)
			{
				setupAccounts.Initialize(_accountService, _keyService);
			}
			setupAccounts.Dock = DockStyle.Fill;
			tabAccounts.Controls.Add(setupAccounts);
			KeysControl setupKeys = new KeysControl();
			if (_keyService != null)
			{
				setupKeys.Initialize(_keyService);
			}
			setupKeys.Dock = DockStyle.Fill;
			tabKeys.Controls.Add(setupKeys);
			ProfilesControl setupProfiles = new ProfilesControl();
			if (_profileService != null)
			{
				setupProfiles.Initialize(_profileService);
			}
			setupProfiles.Dock = DockStyle.Fill;
			tabProfiles.Controls.Add(setupProfiles);
			setupTabs.TabPages.Add(tabAccounts);
			setupTabs.TabPages.Add(tabKeys);
			setupTabs.TabPages.Add(tabProfiles);
			if (string.Equals(initialTab ?? string.Empty, "API Keys", StringComparison.OrdinalIgnoreCase))
			{
				setupTabs.SelectedTab = tabKeys;
			}
			else if (string.Equals(initialTab ?? string.Empty, "Profiles", StringComparison.OrdinalIgnoreCase))
			{
				setupTabs.SelectedTab = tabProfiles;
			}
			else
			{
				setupTabs.SelectedTab = tabAccounts;
			}
			settingsPanel.Controls.Add(setupTabs);
			if (includeStrategyButton)
			{
				Button btnConfig = new Button
				{
					Text = "Configure Strategies",
					Width = 220,
					Height = 40,
					Margin = new Padding(20)
				};
				btnConfig.Click += delegate
				{
					if (_engine != null)
					{
						using (StrategyConfigDialog strategyConfigDialog = new StrategyConfigDialog(_engine))
						{
							strategyConfigDialog.ShowDialog();
							return;
						}
					}
					MessageBox.Show("Strategy Engine not initialized.");
				};
				settingsPanel.Controls.Add(btnConfig);
			}
			ApplyThemeToControl(settingsPanel);
			return settingsPanel;
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
			this.cmbExchange = new System.Windows.Forms.ComboBox();
			this.cmbProduct = new System.Windows.Forms.ComboBox();
			this.btnLoadProducts = new System.Windows.Forms.Button();
			this.btnBacktest = new System.Windows.Forms.Button();
			this.btnPaper = new System.Windows.Forms.Button();
			this.btnLive = new System.Windows.Forms.Button();
			this.txtLog = new System.Windows.Forms.TextBox();
			this.numRisk = new System.Windows.Forms.NumericUpDown();
			this.numEquity = new System.Windows.Forms.NumericUpDown();
			this.cmbStrategy = new System.Windows.Forms.ComboBox();
			this.btnFees = new System.Windows.Forms.Button();
			this.txtApiKey = new System.Windows.Forms.TextBox();
			this.txtSecret = new System.Windows.Forms.TextBox();
			this.txtExtra = new System.Windows.Forms.TextBox();
			this.btnSaveKeys = new System.Windows.Forms.Button();
			this.lblProj100 = new System.Windows.Forms.Label();
			this.lblProj1000 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)this.numRisk).BeginInit();
			((System.ComponentModel.ISupportInitialize)this.numEquity).BeginInit();
			base.SuspendLayout();
			this.cmbExchange.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbExchange.FormattingEnabled = true;
			this.cmbExchange.Items.AddRange(new object[3] { "Coinbase", "Kraken", "Bitstamp" });
			this.cmbExchange.Location = new System.Drawing.Point(12, 12);
			this.cmbExchange.Name = "cmbExchange";
			this.cmbExchange.Size = new System.Drawing.Size(180, 21);
			this.cmbExchange.TabIndex = 0;
			this.cmbProduct.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbProduct.FormattingEnabled = true;
			this.cmbProduct.Location = new System.Drawing.Point(198, 12);
			this.cmbProduct.Name = "cmbProduct";
			this.cmbProduct.Size = new System.Drawing.Size(180, 21);
			this.cmbProduct.TabIndex = 1;
			this.btnLoadProducts.Location = new System.Drawing.Point(384, 12);
			this.btnLoadProducts.Name = "btnLoadProducts";
			this.btnLoadProducts.Size = new System.Drawing.Size(120, 23);
			this.btnLoadProducts.TabIndex = 2;
			this.btnLoadProducts.Text = "Load Products";
			this.btnLoadProducts.UseVisualStyleBackColor = true;
			this.btnLoadProducts.Click += new System.EventHandler(btnLoadProducts_Click);
			this.btnBacktest.Location = new System.Drawing.Point(12, 135);
			this.btnBacktest.Name = "btnBacktest";
			this.btnBacktest.Size = new System.Drawing.Size(120, 23);
			this.btnBacktest.TabIndex = 3;
			this.btnBacktest.Text = "Backtest";
			this.btnBacktest.UseVisualStyleBackColor = true;
			this.btnBacktest.Click += new System.EventHandler(btnBacktest_Click);
			this.btnPaper.Location = new System.Drawing.Point(138, 135);
			this.btnPaper.Name = "btnPaper";
			this.btnPaper.Size = new System.Drawing.Size(120, 23);
			this.btnPaper.TabIndex = 4;
			this.btnPaper.Text = "Start Paper";
			this.btnPaper.UseVisualStyleBackColor = true;
			this.btnPaper.Click += new System.EventHandler(btnPaper_Click);
			this.btnLive.Location = new System.Drawing.Point(264, 135);
			this.btnLive.Name = "btnLive";
			this.btnLive.Size = new System.Drawing.Size(120, 23);
			this.btnLive.TabIndex = 5;
			this.btnLive.Text = "Start Live";
			this.btnLive.UseVisualStyleBackColor = true;
			this.btnLive.Click += new System.EventHandler(btnLive_Click);
			this.txtLog.Location = new System.Drawing.Point(12, 164);
			this.txtLog.Multiline = true;
			this.txtLog.Name = "txtLog";
			this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.txtLog.Size = new System.Drawing.Size(760, 285);
			this.txtLog.TabIndex = 6;
			this.numRisk.DecimalPlaces = 2;
			this.numRisk.Location = new System.Drawing.Point(12, 105);
			this.numRisk.Maximum = new decimal(new int[4] { 100, 0, 0, 0 });
			this.numRisk.Name = "numRisk";
			this.numRisk.Size = new System.Drawing.Size(120, 20);
			this.numRisk.TabIndex = 7;
			this.numRisk.Value = new decimal(new int[4] { 1, 0, 0, 0 });
			this.numEquity.DecimalPlaces = 2;
			this.numEquity.Location = new System.Drawing.Point(138, 105);
			this.numEquity.Maximum = new decimal(new int[4] { 1000000, 0, 0, 0 });
			this.numEquity.Minimum = new decimal(new int[4] { 10, 0, 0, 0 });
			this.numEquity.Name = "numEquity";
			this.numEquity.Size = new System.Drawing.Size(120, 20);
			this.numEquity.TabIndex = 8;
			this.numEquity.Value = new decimal(new int[4] { 100, 0, 0, 0 });
			this.cmbStrategy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbStrategy.FormattingEnabled = true;
			this.cmbStrategy.Items.AddRange(new object[2] { "ORB", "VWAPTrend" });
			this.cmbStrategy.Location = new System.Drawing.Point(12, 72);
			this.cmbStrategy.Name = "cmbStrategy";
			this.cmbStrategy.Size = new System.Drawing.Size(120, 21);
			this.cmbStrategy.TabIndex = 9;
			this.btnFees.Location = new System.Drawing.Point(510, 12);
			this.btnFees.Name = "btnFees";
			this.btnFees.Size = new System.Drawing.Size(120, 23);
			this.btnFees.TabIndex = 10;
			this.btnFees.Text = "Get Fees";
			this.btnFees.UseVisualStyleBackColor = true;
			this.btnFees.Click += new System.EventHandler(btnFees_Click);
			this.txtApiKey.Location = new System.Drawing.Point(198, 72);
			this.txtApiKey.Name = "txtApiKey";
			this.txtApiKey.Size = new System.Drawing.Size(180, 20);
			this.txtApiKey.TabIndex = 11;
			this.txtSecret.Location = new System.Drawing.Point(384, 72);
			this.txtSecret.Name = "txtSecret";
			this.txtSecret.Size = new System.Drawing.Size(180, 20);
			this.txtSecret.TabIndex = 12;
			this.txtExtra.Location = new System.Drawing.Point(570, 72);
			this.txtExtra.Name = "txtExtra";
			this.txtExtra.Size = new System.Drawing.Size(180, 20);
			this.txtExtra.TabIndex = 13;
			this.btnSaveKeys.Location = new System.Drawing.Point(636, 12);
			this.btnSaveKeys.Name = "btnSaveKeys";
			this.btnSaveKeys.Size = new System.Drawing.Size(136, 23);
			this.btnSaveKeys.TabIndex = 14;
			this.btnSaveKeys.Text = "Save Keys";
			this.btnSaveKeys.UseVisualStyleBackColor = true;
			this.btnSaveKeys.Click += new System.EventHandler(btnSaveKeys_Click);
			this.lblProj100.AutoSize = true;
			this.lblProj100.Location = new System.Drawing.Point(270, 108);
			this.lblProj100.Name = "lblProj100";
			this.lblProj100.Size = new System.Drawing.Size(108, 13);
			this.lblProj100.TabIndex = 15;
			this.lblProj100.Text = "$100 projection: n/a";
			this.lblProj1000.AutoSize = true;
			this.lblProj1000.Location = new System.Drawing.Point(420, 108);
			this.lblProj1000.Name = "lblProj1000";
			this.lblProj1000.Size = new System.Drawing.Size(114, 13);
			this.lblProj1000.TabIndex = 16;
			this.lblProj1000.Text = "$1000 projection: n/a";
			base.ClientSize = new System.Drawing.Size(784, 461);
			base.Controls.Add(this.lblProj1000);
			base.Controls.Add(this.lblProj100);
			base.Controls.Add(this.btnSaveKeys);
			base.Controls.Add(this.txtExtra);
			base.Controls.Add(this.txtSecret);
			base.Controls.Add(this.txtApiKey);
			base.Controls.Add(this.btnFees);
			base.Controls.Add(this.cmbStrategy);
			base.Controls.Add(this.numEquity);
			base.Controls.Add(this.numRisk);
			base.Controls.Add(this.txtLog);
			base.Controls.Add(this.btnLive);
			base.Controls.Add(this.btnPaper);
			base.Controls.Add(this.btnBacktest);
			base.Controls.Add(this.btnLoadProducts);
			base.Controls.Add(this.cmbProduct);
			base.Controls.Add(this.cmbExchange);
			base.Name = "MainForm";
			this.Text = "Crypto Day-Trading Suite";
			base.WindowState = System.Windows.Forms.FormWindowState.Maximized;
			((System.ComponentModel.ISupportInitialize)this.numRisk).EndInit();
			((System.ComponentModel.ISupportInitialize)this.numEquity).EndInit();
			base.ResumeLayout(false);
			base.PerformLayout();
		}

		private Task AnalyzeAndPlanAsync(string product, List<Candle> candles)
		{
			if (candles == null || candles.Count < 30 || string.IsNullOrWhiteSpace(product))
			{
				return Task.CompletedTask;
			}
			if (!FeatureExtractor.TryComputeFeatures(candles, out var f))
			{
				return Task.CompletedTask;
			}
			DirectionPrediction dir = _predict.PredictDirection(product, f, 5m);
			MagnitudePrediction mag = _predict.PredictMagnitude(product, f, 5m);
			PredictionRecord rec = new PredictionRecord
			{
				ProductId = product,
				AtUtc = DateTime.UtcNow,
				HorizonMinutes = 5m,
				Direction = (int)dir.Direction,
				Probability = dir.Probability,
				ExpectedReturn = mag.ExpectedReturn,
				ExpectedVol = mag.ExpectedVol,
				RealizedKnown = false,
				RealizedDirection = 0,
				RealizedReturn = 0m
			};
			if (_historyService != null)
			{
				_historyService.SavePrediction(rec);
			}
			_planner.Clear();
			FeeSchedule fees = _fees;
			decimal friction = fees.MakerRate + fees.TakerRate + 0.0005m;
			if (cmbStrategy.Items.Contains("ORB"))
			{
				decimal edge = mag.ExpectedReturn * dir.Probability - friction;
				string side = ((edge >= 0m) ? "buy" : "sell");
				_planner.AddCandidate(new TradeRecord
				{
					Exchange = ((_client != null) ? _client.Name : "n/a"),
					ProductId = product,
					AtUtc = DateTime.UtcNow,
					Strategy = "ORB",
					Side = side,
					Quantity = 0.0m,
					Price = candles[candles.Count - 1].Close,
					EstEdge = Math.Abs(edge),
					Executed = false,
					Notes = "auto from prediction"
				});
			}
			if (cmbStrategy.Items.Contains("VWAPTrend"))
			{
				decimal edge2 = -mag.ExpectedReturn * (1m - dir.Probability) - friction;
				string side2 = ((edge2 >= 0m) ? "sell" : "buy");
				_planner.AddCandidate(new TradeRecord
				{
					Exchange = ((_client != null) ? _client.Name : "n/a"),
					ProductId = product,
					AtUtc = DateTime.UtcNow,
					Strategy = "VWAPTrend",
					Side = side2,
					Quantity = 0.0m,
					Price = candles[candles.Count - 1].Close,
					EstEdge = Math.Abs(edge2),
					Executed = false,
					Notes = "auto from prediction"
				});
			}
			if (_plannerForm != null && !_plannerForm.IsDisposed)
			{
				_planner.ReapplyAll();
				_plannerForm.SetData(_planner.Planned.ToList(), (_historyService != null) ? _historyService.LoadPredictions() : new List<PredictionRecord>());
			}
			return Task.CompletedTask;
		}

		private void LearnFromCandles(string product, List<Candle> candles)
		{
			if (candles == null || candles.Count < 40 || string.IsNullOrWhiteSpace(product))
			{
				return;
			}
			int start = 20;
			int end = candles.Count - 2;
			for (int i = start; i <= end; i++)
			{
				try
				{
					List<Candle> window = candles.GetRange(0, i + 1);
					if (FeatureExtractor.TryComputeFeatures(window, out var f))
					{
						decimal curr = candles[i].Close;
						decimal next = candles[i + 1].Close;
						if (!(curr <= 0m))
						{
							decimal ret = (next - curr) / curr;
							int dir = ((ret > 0m) ? 1 : (-1));
							_predict.Learn(product, f, dir, ret);
						}
					}
				}
				catch
				{
				}
			}
		}

		public void LoadAI()
		{
			try
			{
				string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CryptoDayTraderSuite");
				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
				string path = Path.Combine(dir, "prediction_model.json");
				if (File.Exists(path))
				{
					_predict.LoadState(File.ReadAllText(path));
					Log("AI model loaded.");
				}
			}
			catch (Exception ex)
			{
				Log("AI load failed: " + ex.Message);
			}
		}

		public void SaveAI()
		{
			try
			{
				string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CryptoDayTraderSuite");
				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
				string path = Path.Combine(dir, "prediction_model.json");
				File.WriteAllText(path, _predict.SerializeState());
				Log("AI model saved.");
			}
			catch (Exception ex)
			{
				Log("AI save failed: " + ex.Message);
			}
		}

		private void btnPlanner_Click(object sender, EventArgs e)
		{
			NavigateTo("Planner");
		}

		public void OpenPlanner()
		{
			NavigateTo("Planner");
		}

		private T FindField<T>(string name) where T : class
		{
			try
			{
				FieldInfo fi = GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (fi == null)
				{
					return null;
				}
				return fi.GetValue(this) as T;
			}
			catch
			{
				return null;
			}
		}

		public void OnShown_Hooks(object sender, EventArgs e)
		{
			try
			{
				MainFormExtraButtons.Add(this);
			}
			catch
			{
			}
		}

		public void OnShown_Log(object sender, EventArgs e)
		{
			try
			{
				_txtLog = FindField<TextBox>("txtLog");
			}
			catch
			{
			}
		}

		private void LogSafe(string message)
		{
			if (base.InvokeRequired)
			{
				BeginInvoke(new Action<string>(LogSafe), message);
			}
			else
			{
				Log(message);
			}
		}

		private Control FindTradeHost()
		{
			TabControl tc = FindFirstTab(this);
			if (tc != null && tc.TabPages.Count > 0)
			{
				return tc.SelectedTab ?? tc.TabPages[0];
			}
			return this;
		}

		private TabControl FindFirstTab(Control root)
		{
			if (root == null)
			{
				return null;
			}
			if (root is TabControl)
			{
				return (TabControl)root;
			}
			foreach (Control c in root.Controls)
			{
				TabControl r = FindFirstTab(c);
				if (r != null)
				{
					return r;
				}
			}
			return null;
		}

		private Control FindChartLike(Control root)
		{
			Control found = FindByTypeName(this, "Chart");
			if (found != null)
			{
				return found;
			}
			Panel pnl = new Panel();
			pnl.Name = "autoChartArea";
			pnl.BackColor = Color.FromArgb(32, 34, 40);
			return pnl;
		}

		private Control FindByTypeName(Control root, string typeNameContains)
		{
			if (root == null)
			{
				return null;
			}
			foreach (Control c in root.Controls)
			{
				string t = c.GetType().Name;
				if (t.IndexOf(typeNameContains, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return c;
				}
				Control r = FindByTypeName(c, typeNameContains);
				if (r != null)
				{
					return r;
				}
			}
			return null;
		}

		private TextBox FindLogBox()
		{
			TextBox tb = FindMultilineTextBox(this);
			if (tb != null)
			{
				return tb;
			}
			tb = new TextBox();
			tb.Multiline = true;
			tb.ScrollBars = ScrollBars.Vertical;
			tb.Name = "autoLog";
			tb.Font = new Font("Consolas", 9f, FontStyle.Regular);
			return tb;
		}

		private TextBox FindMultilineTextBox(Control root)
		{
			if (root == null)
			{
				return null;
			}
			foreach (Control c in root.Controls)
			{
				if (c is TextBox t && t.Multiline)
				{
					return t;
				}
				TextBox r = FindMultilineTextBox(c);
				if (r != null)
				{
					return r;
				}
			}
			return null;
		}

		private void BuildResponsiveLayout()
		{
			Control host = FindTradeHost();
			if (host == null)
			{
				host = this;
			}
			_chartArea = FindChartLike(host);
			TextBox logBox = FindLogBox();
			_tl = new TableLayoutPanel();
			_tl.Dock = DockStyle.Fill;
			_tl.ColumnCount = 1;
			_tl.RowCount = 2;
			_tl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			_tl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
			_top = new FlowLayoutPanel();
			_top.Dock = DockStyle.Top;
			_top.WrapContents = true;
			_top.AutoSize = true;
			_top.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			_top.Padding = new Padding(6, 6, 6, 6);
			int h = 24;
			Action<Control> addTop = delegate(Control c)
			{
				if (c != null)
				{
					c.Height = h;
					_top.Controls.Add(c);
				}
			};
			addTop(cmbExchange);
			addTop(cmbProduct);
			addTop(btnLoadProducts);
			addTop(btnFees);
			addTop(cmbStrategy);
			addTop(numRisk);
			addTop(numEquity);
			addTop(btnBacktest);
			addTop(btnPaper);
			addTop(btnLive);
			if (lblProj100 != null)
			{
				_top.Controls.Add(lblProj100);
			}
			if (lblProj1000 != null)
			{
				_top.Controls.Add(lblProj1000);
			}
			_split = new SplitContainer();
			_split.Dock = DockStyle.Fill;
			_split.Orientation = Orientation.Horizontal;
			_split.SplitterWidth = 6;
			_split.Panel1MinSize = 100;
			_split.Panel2MinSize = 80;
			_split.SizeChanged += delegate
			{
				SetSafeSplitterDistance();
			};
			base.Shown += delegate
			{
				SetSafeSplitterDistance();
			};
			_chartArea.Parent = _split.Panel1;
			_chartArea.Dock = DockStyle.Fill;
			logBox.Parent = _split.Panel2;
			logBox.Dock = DockStyle.Fill;
			host.Controls.Clear();
			host.Controls.Add(_tl);
			_tl.Controls.Add(_top, 0, 0);
			_tl.Controls.Add(_split, 0, 1);
			BeginInvoke(new Action(SetSafeSplitterDistance));
		}

		private void SetSafeSplitterDistance()
		{
			try
			{
				if (_split == null)
				{
					return;
				}
				int total = ((_split.Orientation == Orientation.Horizontal) ? _split.Height : _split.Width);
				if (total > 0)
				{
					int min = _split.Panel1MinSize;
					int max = Math.Max(min, total - _split.Panel2MinSize - _split.SplitterWidth);
					int desired = Math.Max(min, Math.Min(max, (int)((double)total * 0.65)));
					if (_split.SplitterDistance != desired)
					{
						_split.SplitterDistance = desired;
					}
				}
			}
			catch
			{
			}
		}

		private void ToggleFullScreen()
		{
			if (!_isFull)
			{
				_prevState = base.WindowState;
				_prevBorder = base.FormBorderStyle;
				_prevBounds = base.Bounds;
				base.FormBorderStyle = FormBorderStyle.None;
				base.WindowState = FormWindowState.Maximized;
				_isFull = true;
			}
			else
			{
				base.FormBorderStyle = _prevBorder;
				base.WindowState = _prevState;
				base.Bounds = _prevBounds;
				_isFull = false;
			}
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			Theme.Apply(this);
		}

		public void OnShown_Tooltips(object sender, EventArgs e)
		{
			try
			{
				BuildTooltips();
			}
			catch
			{
			}
		}

		private void BuildTooltips()
		{
			if (_tt == null)
			{
				_tt = new ToolTip();
				_tt.AutoPopDelay = 8000;
				_tt.InitialDelay = 500;
				_tt.ReshowDelay = 200;
				_tt.ShowAlways = true;
			}
			tip("btnLoadProducts", "Fetch tradable products from the selected exchange.");
			tip("btnFees", "Query current trading fees for the selected account/exchange.");
			tip("btnBacktest", "Run strategy backtests on historical data.");
			tip("btnPaper", "Start paper trading with no real orders placed.");
			tip("btnLive", "Start live trading using the active API keys and account.");
			tip("cmbExchange", "Select an exchange/broker (e.g., Coinbase, Kraken, Bitstamp).");
			tip("cmbProduct", "Select the market/pair to trade (e.g., BTC-USD).");
			tip("cmbGranularity", "Candle interval for charts and analysis (minutes).");
			tip("numQty", "Order size in base units or quote currency depending on mode.");
			tip("numRisk", "Percent of equity to risk per trade.");
			tip("cmbOrderType", "Choose between market, limit, and other supported order types.");
			tip("btnKeys", "Open API Key Manager to add, edit, or delete keys.");
			tip("btnAccounts", "Manage linked accounts and their trading settings.");
			tip("btnSaveKeys", "Save entered API key details securely to your profile.");
			tip("txtApiKeyId", "For Coinbase: Enter CDP API key name (organizations/.../apiKeys/...). For others: API key ID.");
			tip("txtApiSecret", "For Coinbase: Enter CDP private key (-----BEGIN EC PRIVATE KEY-----...). For others: API secret.");
			tip("txtApiPassphrase", "Enter the API passphrase (Coinbase legacy) or customer ID (Bitstamp). Not used for Coinbase CDP format.");
			tip("btnAutoMode", "Open Auto Mode to scan markets and propose trades by risk.");
			tip("btnPropose", "Build proposed trades from current strategy and settings.");
			tip("btnExecute", "Place the currently proposed trades on the selected account.");
			tip("chart1", "Price chart with overlays and indicators.");
			tip("btnIndicators", "Toggle indicators and overlays (VWAP, MA, ORB, etc.).");
			tip("txtLog", "Execution log. Use the Logs menu to open logs folder.");
			tip("btnStatus", "Open status page to compare projected vs actual PnL.");
			void tip(string fieldName, string text)
			{
				Control c = FindField<Control>(fieldName);
				if (c != null)
				{
					_tt.SetToolTip(c, text);
				}
			}
		}
	}
}
