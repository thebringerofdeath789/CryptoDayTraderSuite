/* File: MainForm.cs */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Strategy;
using CryptoDayTraderSuite.Util;
using CryptoDayTraderSuite.UI;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Services.Messaging;
using CryptoDayTraderSuite.Services.Messaging.Events;

namespace CryptoDayTraderSuite
{
 public partial class MainForm : Form
 {
 /* Injected Dependencies */
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

        /* State */
        private IExchangeClient _client = null;
        private StrategyEngine _engine = new StrategyEngine();
        private FeeSchedule _fees = new FeeSchedule { MakerRate = 0.0040m, TakerRate = 0.0060m, Notes = "default" };
        
        /* UI Components */
        private SidebarControl _sidebar;
        private TableLayoutPanel _shellLayout;
        private Panel _contentPanel;
        private ToolStripStatusLabel _statusLabel;
        private TradingControl _tradingControl;
        private AutoModeControl _autoModeControl;
        private Dictionary<string, Control> _views = new Dictionary<string, Control>();
        private readonly List<string> _logBuffer = new List<string>();
        private string _lastLogMessage;

        /* Public Accessors for Menu Integration */
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

           
            /* Default fallbacks for Designer or legacy startup (replaced by InitializeDependencies) */
            if (_keyService == null) _keyService = new KeyService();
            if (_exchangeProvider == null) _exchangeProvider = new ExchangeProvider(_keyService);
            if (_accountService == null) _accountService = new AccountService();
            
            if (cmbExchange.Items.Count > 0) cmbExchange.SelectedIndex = 0;
            if (cmbStrategy.Items.Count > 0) cmbStrategy.SelectedIndex = 0;

            try
            {
                LoadCoinbaseCdpKeys();
            }
            catch (Exception ex)
            {
                Util.Log.Warn("[MainForm] Failed to load Coinbase CDP keys at startup: " + ex.Message);
            }
            
            /* Defer Build until dependencies likely ready, or do basic build */
            BuildModernLayout();
            Log("MainForm constructed");
        }

        public void InitializeDependencies(
            AutoPlannerService planner, 
            IExchangeProvider provider, 
            BacktestService backtester,
            IEventBus eventBus,
            IAccountService accountService,
            IKeyService keyService,
            IHistoryService historyService,
            IAutoModeProfileService autoModeProfileService,
            ChromeSidecar sidecar,
            StrategyEngine engine,
            IRateRouter rateRouter,
            AIGovernor governor)
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
                Log("Chrome Sidecar connection is managed by governor/runtime workflow.");
            }

            /* Re-subscribe loggers using EventBus */
            if (_eventBus != null)
            {
                _eventBus.Subscribe<LogEvent>(OnLogEvent);
            }
 
            /* Rebuild layout with new dependencies */
            // Clear view cache so they are recreated with new services
            _views.Clear();
            _autoModeControl = null;
            this.Controls.Clear();
            BuildModernLayout();
        }

        private void OnLogEvent(LogEvent evt)
        {
            /* Marshall to UI thread */
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<LogEvent>(OnLogEvent), evt);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(evt.Message)) return;
            if (string.Equals(_lastLogMessage, evt.Message, StringComparison.Ordinal)) return;
            _lastLogMessage = evt.Message;

            var line = evt.Timestamp.ToString("HH:mm:ss") + " " + evt.Message;
            _logBuffer.Add(line);
            if (_logBuffer.Count > 500) _logBuffer.RemoveAt(0);

            // 1. Update Global Status Bar (Always Visible)
            if (_statusLabel != null)
            {
                _statusLabel.Text = line;
            }

            // 2. Update Trading Control (If Active/Cached)
            if (_tradingControl != null)
            {
                _tradingControl.Log(line);
            }
        }

 private void Log(string s) 
 { 
 /* Legacy local log method, now forwards to EventBus if available, or direct UI */
 if (_eventBus != null)
 {
 _eventBus.Publish(new LogEvent(s));
 }
 else
 {
 // Fallback direct update
 if (_tradingControl != null) _tradingControl.Log(s);
 }
 }

 private void LoadCoinbaseCdpKeys()
 {
     if (_keyService == null) return;
     try
     {
         var cdpFile = Path.Combine(Application.StartupPath, "cdp_api_key.json");
         if (File.Exists(cdpFile))
         {
             var json = File.ReadAllText(cdpFile);
             var cdpData = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
             if (cdpData != null && cdpData.ContainsKey("name") && cdpData.ContainsKey("privateKey"))
             {
                 var keyInfo = new KeyInfo { 
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
     catch (Exception ex) { Log("error loading CDP key: " + ex.Message); }
 }

 /* 
 REFACTOR: Use ExchangeProvider for client creation 
 */
 private IExchangeClient BuildClient()
 {
 string exch = "Coinbase";
 if (_tradingControl != null) exch = _tradingControl.Exchange ?? "Coinbase";
 else if (cmbExchange.SelectedItem != null) exch = cmbExchange.SelectedItem.ToString();
 
 return _exchangeProvider.CreateAuthenticatedClient(exch);
 }

 private async void btnLoadProducts_Click(object sender, EventArgs e)
 {
                try
                {
                    string exch = "Coinbase";
                    if (_tradingControl != null) exch = _tradingControl.Exchange ?? "Coinbase";
                    // Use Provider
                    _client = _exchangeProvider.CreatePublicClient(exch);

                    var list = await _client.ListProductsAsync();
                    var usdPairs = list.Where(x => x.Contains("/USD") || x.Contains("-USD")).ToList();
                    if (_tradingControl != null) _tradingControl.SetProducts(usdPairs);
                    Log("loaded products: " + usdPairs.Count);
                }
                catch (Exception ex) { Log("error loading products " + ex.Message); }
            }

            private async Task<List<Candle>> Load1mCandles(string productId, DateTime startUtc, DateTime endUtc)
            {
                /* If no client, create public one default */
                if (_client == null) _client = _exchangeProvider.CreatePublicClient("Coinbase");
                var c = await _client.GetCandlesAsync(productId, 1, startUtc, endUtc);
                return c;
            }

 private async void btnFees_Click(object sender, EventArgs e)
 {
 try
 {
 _client = BuildClient();
 _fees = await _client.GetFeesAsync();
                    Log("fees maker " + (_fees.MakerRate * 100m).ToString("0.###") + "% taker " + (_fees.TakerRate * 100m).ToString("0.###") + "% " + _fees.Notes);
                }
                catch (Exception ex) { Log("error getting fees " + ex.Message); }
            }

            /* 
               REFACTOR: Use BacktestService 
            */
            private async void btnBacktest_Click(object sender, EventArgs e)
            {
                try
                {
                    if (_backtestService == null) { Log("BacktestService not initialized"); return; }

                    var product = (_tradingControl != null ? _tradingControl.Product : null) ?? "BTC-USD";
                    var strat = (_tradingControl != null ? _tradingControl.Strategy : null) ?? "ORB";
                    var risk = (_tradingControl != null ? _tradingControl.Risk : 1m);
                    var equity = (_tradingControl != null ? _tradingControl.Equity : 1000m);
                    var exchange = (_tradingControl != null ? _tradingControl.Exchange : "Coinbase") ?? "Coinbase";

                    Log("starting backtest...");
                    var res = await _backtestService.RunBacktestAsync(exchange, product, strat, risk, equity, _fees);
                    
                    if (res.Error != null) { Log("Backtest Error: " + res.Error); return; }

                    if (_tradingControl != null && res.Candles != null) _tradingControl.SetCandles(res.Candles);
                    if (res.Candles != null && res.Candles.Count > 0)
                    {
                        LearnFromCandles(product, res.Candles);
                        await AnalyzeAndPlanAsync(product, res.Candles);
                    }
                    
                    var r = res.RunResult;
                    Log("backtest " + product + " trades " + r.Trades + " pnl $" + r.PnL.ToString("0.00") + " win " + (r.WinRate * 100m).ToString("0.0") + "% mdd " + (r.MaxDrawdown * 100m).ToString("0.0") + "%");

                    UpdateProjections();
                }
                catch (Exception ex) { Log("backtest fatal error " + ex.Message); }
            }

            /* Legacy Projection UI Logic */
            private void UpdateProjections()
            {
                var riskFrac = ((_tradingControl != null ? _tradingControl.Risk : 1m)) / 100m;
                var roundtrip = _fees.MakerRate + _fees.TakerRate + 0.0005m;
                var p = new ProjectionInput { StartingEquity = 100m, TradesPerDay = 10, WinRate = 0.52m, AvgWinR = 1.1m, AvgLossR = 1.0m, RiskPerTradeFraction = riskFrac, NetFeeAndFrictionRate = roundtrip, Days = 20 };
                var r = Projections.Compute(p);
                var s100 = " projection: " + r.EndingEquity.ToString("0.00") + " daily " + r.DailyExpectedReturnPct.ToString("0.00") + "%";
                p.StartingEquity = 1000m; var r2 = Projections.Compute(p); 
                var s1000 = " projection: " + r2.EndingEquity.ToString("0.00");
                if (_tradingControl != null) _tradingControl.SetProjections(s100, s1000);
            }

            private async void btnPaper_Click(object sender, EventArgs e)
            {
                // Keep legacy logic for now, StrategyEngine decoupled internally
                try
                {
                    _client = BuildClient();
                    var product = (_tradingControl != null ? _tradingControl.Product : null) ?? "BTC-USD";
                    var strat = (_tradingControl != null ? _tradingControl.Strategy : null) ?? "ORB";
                    var end = DateTime.UtcNow; var start = end.AddHours(-8);
                    
                    // AUDIT: Load1mCandles might not exist, use client directly
                    var candles = await _client.GetCandlesAsync(product, 1, start, end);
                    if (_tradingControl != null) _tradingControl.SetCandles(candles);
                    if (candles != null && candles.Count > 0) await AnalyzeAndPlanAsync(product, candles);

                    _engine.SetStrategy(strat);
                    var riskFrac = ((_tradingControl != null ? _tradingControl.Risk : 1m)) / 100m; 
                    var equity = (_tradingControl != null ? _tradingControl.Equity : 1000m);
                    CostBreakdown cb;
                    
                    if (candles == null || candles.Count == 0) { Log("No candles"); return; }

                    var price = candles.Last().Close;
                    var order = _engine.Evaluate(product, candles, _fees, equity, riskFrac, price, out cb);
                    if (order == null) { Log("no paper signal"); return; }
                    Log("paper trade " + order.Side + " " + order.Quantity + " " + product);
                }
                catch (Exception ex) { Log("paper error " + ex.Message); }
            }

            private async void btnLive_Click(object sender, EventArgs e)
            {
                try
                {
                    _client = BuildClient();
                    var product = (_tradingControl != null ? _tradingControl.Product : null) ?? "BTC-USD";
                    var strat = (_tradingControl != null ? _tradingControl.Strategy : null) ?? "ORB";
                    var end = DateTime.UtcNow; var start = end.AddHours(-8);

                    var candles = await _client.GetCandlesAsync(product, 1, start, end);
                    if (_tradingControl != null) _tradingControl.SetCandles(candles);
                    if (candles != null && candles.Count > 0) await AnalyzeAndPlanAsync(product, candles);

                    _engine.SetStrategy(strat);
                    var riskFrac = ((_tradingControl != null ? _tradingControl.Risk : 1m)) / 100m; 
                    var equity = (_tradingControl != null ? _tradingControl.Equity : 1000m);

                    var ticker = await _client.GetTickerAsync(product);
                    CostBreakdown cb;
                    var order = _engine.Evaluate(product, candles, _fees, equity, riskFrac, ticker.Last, out cb);
                    if (order == null) { Log("no live signal"); return; }
                    if (!order.StopLoss.HasValue || !order.TakeProfit.HasValue || order.StopLoss.Value <= 0m || order.TakeProfit.Value <= 0m)
                    {
                        Log("live order blocked: protective stop/target missing");
                        return;
                    }
                    var res = await _client.PlaceOrderAsync(order);
                    Log("live order " + res.OrderId + " " + res.Message);
                }
                catch (Exception ex) { Log("live error " + ex.Message); }
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

                    var broker = (cmbExchange != null && cmbExchange.SelectedItem != null)
                        ? cmbExchange.SelectedItem.ToString()
                        : "Coinbase";
                    var label = "Manual";

                    var keyInfo = new KeyInfo
                    {
                        Broker = broker,
                        Label = label,
                        ApiKey = txtApiKey != null ? txtApiKey.Text.Trim() : string.Empty,
                        Secret = txtSecret != null ? txtSecret.Text.Trim() : string.Empty,
                        Passphrase = txtExtra != null ? txtExtra.Text.Trim() : string.Empty,
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
            this.Controls.Clear();
            CryptoDayTraderSuite.Themes.Theme.Apply(this);

            /* 1. Sidebar */
            _sidebar = new SidebarControl();
            _sidebar.Dock = DockStyle.Fill;
            _sidebar.NavigationSelected += OnNavigationSelected;
            
            /* Wire AI Status */
            if (_governor != null)
            {
                _sidebar.Configure(_governor);
            }

            /* 2. Shell Layout (Sidebar + Content) */
            _shellLayout = new TableLayoutPanel();
            _shellLayout.Dock = DockStyle.Fill;
            _shellLayout.ColumnCount = 2;
            _shellLayout.RowCount = 1;
            _shellLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, _sidebar.Width));
            _shellLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _shellLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _contentPanel = new Panel();
            _contentPanel.Dock = DockStyle.Fill;
            _contentPanel.BackColor = CryptoDayTraderSuite.Themes.Theme.ContentBg;

            _shellLayout.Controls.Add(_sidebar, 0, 0);
            _shellLayout.Controls.Add(_contentPanel, 1, 0);

            _sidebar.SizeChanged -= Sidebar_SizeChanged;
            _sidebar.SizeChanged += Sidebar_SizeChanged;
            Sidebar_SizeChanged(_sidebar, EventArgs.Empty);
            
            /* 3. Status Bar */
            var statusStrip = new StatusStrip();
            statusStrip.Dock = DockStyle.Bottom;
            statusStrip.BackColor = CryptoDayTraderSuite.Themes.Theme.PanelBg;
            statusStrip.ForeColor = CryptoDayTraderSuite.Themes.Theme.TextMuted;
            
            _statusLabel = new ToolStripStatusLabel();
            _statusLabel.Text = "Ready";
            _statusLabel.Spring = true;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusStrip.Items.Add(_statusLabel);
            
            this.Controls.Add(statusStrip);
            this.Controls.Add(_shellLayout);
            
            /* Load Default */
            OnNavigationSelected("Dashboard");
            EnsureAutoModeControlInitialized();
        }

        private void Sidebar_SizeChanged(object sender, EventArgs e)
        {
            if (_shellLayout == null || _shellLayout.ColumnStyles.Count < 1 || _sidebar == null) return;
            var width = _sidebar.Width;
            if (width < 48) width = 48;
            _shellLayout.ColumnStyles[0].Width = width;
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
                if (view != null) _views[page] = view;
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
                switch(page)
                {
                    case "Dashboard":
                        var db = new DashboardControl();
                        db.NavigationRequest += destination => NavigateTo(destination);
                        if (_accountService != null) 
                            db.Initialize(_accountService, _historyService ?? new HistoryService(), _governor);
                        CryptoDayTraderSuite.Themes.Theme.Apply(db);
                        return db;

                    case "Trading":
                        _tradingControl = new TradingControl();
                        /* Wire Legacy Events */
                        _tradingControl.LoadProductsClicked += btnLoadProducts_Click;
                        _tradingControl.FeesClicked += btnFees_Click;
                        _tradingControl.BacktestClicked += btnBacktest_Click;
                        _tradingControl.PaperClicked += btnPaper_Click;
                        _tradingControl.LiveClicked += btnLive_Click;
                        
                        /* Populate Lists */
                        _tradingControl.SetExchanges(new []
                        {
                            "Coinbase",
                            "Kraken",
                            "Bitstamp",
                            "Binance-US",
                            "Binance-Global",
                            "Bybit-Global",
                            "OKX-Global"
                        });
                        _tradingControl.SetStrategies(new [] { "ORB", "VWAPTrend", "RSIReversion", "Donchian 20" });

                        if (_logBuffer.Count > 0)
                        {
                            foreach (var line in _logBuffer) _tradingControl.Log(line);
                        }

                        CryptoDayTraderSuite.Themes.Theme.Apply(_tradingControl);
                        return _tradingControl;

                    case "Planner":
                        var pc = new PlannerControl();
                        pc.Initialize(
                            _historyService ?? new HistoryService(),
                            _autoPlanner,
                            _exchangeProvider != null ? _exchangeProvider.CreatePublicClient("Coinbase") : null,
                            _accountService,
                            _keyService);
                        CryptoDayTraderSuite.Themes.Theme.Apply(pc);
                        return pc;

                    case "Auto":
                        EnsureAutoModeControlInitialized();
                        return _autoModeControl;

                    case "Accounts":
                        return CreateAccountsView();

                    case "Insights":
                        return CreateInsightsView();

                    case "Keys":
                        return CreateKeysView();
                         
                    case "Settings":
                        return CreateSettingsView();

                    case "Profiles":
                        var pm = new ProfileManagerControl();
                        pm.Initialize(_keyService, _accountService, _profileService);
                        // Fix for Theme.Apply overloading issue
                        ApplyThemeToControl(pm);
                        return pm;
                         
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                Log("Error creating view " + page + ": " + ex.Message);
                return new Label { Text = "Error: " + ex.Message };
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
 this.MinimumSize = new System.Drawing.Size(900, 600);
 this.KeyPreview = true;
 this.KeyDown += (s, a) => { if (a.KeyCode == Keys.F11) ToggleFullScreen(); };
 }

 protected override void OnFormClosing(FormClosingEventArgs e)
 {
 base.OnFormClosing(e);
 if (_eventBus != null) _eventBus.Unsubscribe<LogEvent>(OnLogEvent);
 SaveAI();
 }
 
        private void ApplyThemeToControl(Control c)
        {
            c.BackColor = CryptoDayTraderSuite.Themes.Theme.ContentBg;
            c.ForeColor = CryptoDayTraderSuite.Themes.Theme.Text;
            foreach (Control child in c.Controls) ApplyThemeToControlRecursively(child);
        }

        private void EnsureAutoModeControlInitialized()
        {
            if (_autoModeControl != null && !_autoModeControl.IsDisposed)
            {
                return;
            }

            _autoModeControl = new AutoModeControl();
            if (_autoPlanner != null && _exchangeProvider != null)
            {
                var pubClient = _exchangeProvider.CreatePublicClient("Coinbase");
                _autoModeControl.Initialize(
                    _autoPlanner,
                    pubClient,
                    _accountService,
                    _keyService,
                    _autoModeProfileService,
                    _historyService ?? new HistoryService());
            }

            CryptoDayTraderSuite.Themes.Theme.Apply(_autoModeControl);
            _views["Auto"] = _autoModeControl;
        }

        private void ApplyThemeToControlRecursively(Control c)
        {
            // Simple recursive applier since Theme.Apply only takes Form/UserControl
             if (c is Button b) 
            { 
                b.FlatStyle = FlatStyle.Flat; 
                b.FlatAppearance.BorderColor = CryptoDayTraderSuite.Themes.Theme.PanelBg; 
                b.BackColor = CryptoDayTraderSuite.Themes.Theme.PanelBg; 
                b.ForeColor = CryptoDayTraderSuite.Themes.Theme.Text; 
            }
            else if (c is TextBox t) { t.BackColor = CryptoDayTraderSuite.Themes.Theme.PanelBg; t.ForeColor = CryptoDayTraderSuite.Themes.Theme.Text; t.BorderStyle = BorderStyle.FixedSingle; }
            else {
                 c.BackColor = CryptoDayTraderSuite.Themes.Theme.PanelBg;
                 c.ForeColor = CryptoDayTraderSuite.Themes.Theme.Text;
                 if (c.HasChildren) foreach (Control child in c.Controls) ApplyThemeToControlRecursively(child);
            }
        }

        private Control CreateAccountsView()
        {
            var accounts = new AccountsControl();
            if (_accountService != null)
            {
                accounts.Initialize(_accountService, _keyService, _historyService);
            }
            accounts.Dock = DockStyle.Fill;
            CryptoDayTraderSuite.Themes.Theme.Apply(accounts);
            return accounts;
        }

        private Control CreateInsightsView()
        {
            var insights = new AccountsControl();
            if (_accountService != null)
            {
                insights.Initialize(_accountService, _keyService, _historyService);
            }
            insights.SetInsightsOnlyMode(true);
            insights.Dock = DockStyle.Fill;
            CryptoDayTraderSuite.Themes.Theme.Apply(insights);
            return insights;
        }

        private Control CreateKeysView()
        {
            var keys = new KeysControl();
            if (_keyService != null)
            {
                keys.Initialize(_keyService, _accountService, _historyService);
            }
            keys.Dock = DockStyle.Fill;
            CryptoDayTraderSuite.Themes.Theme.Apply(keys);
            return keys;
        }

        private Control CreateSettingsView()
        {
            var settingsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoScroll = true, WrapContents = false };
            settingsPanel.BackColor = CryptoDayTraderSuite.Themes.Theme.ContentBg;

            var lblSetup = new Label
            {
                Text = "Settings",
                Font = new System.Drawing.Font("Segoe UI", 14, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                ForeColor = CryptoDayTraderSuite.Themes.Theme.Text,
                Margin = new Padding(10)
            };
            settingsPanel.Controls.Add(lblSetup);

            var profileHost = new Panel
            {
                Width = 920,
                Height = 360,
                Margin = new Padding(10)
            };

            var setupProfiles = new ProfilesControl();
            if (_profileService != null)
            {
                setupProfiles.Initialize(_profileService);
            }
            setupProfiles.Dock = DockStyle.Fill;
            profileHost.Controls.Add(setupProfiles);
            settingsPanel.Controls.Add(profileHost);

            var sidecarPanel = new FlowLayoutPanel
            {
                Width = 920,
                Height = 72,
                Margin = new Padding(20, 0, 20, 0),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            var chkSidecarLaunchHidden = new CheckBox
            {
                Text = "Launch Sidecar Hidden",
                AutoSize = true,
                Checked = CryptoDayTraderSuite.Properties.Settings.Default.SidecarLaunchHidden,
                Margin = new Padding(4, 12, 14, 0)
            };

            var btnSidecarVisibility = new Button
            {
                Width = 130,
                Height = 28,
                Margin = new Padding(0, 8, 8, 0)
            };

            var lblSidecarWindowStatus = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 14, 0, 0),
                Text = "Sidecar Window: Unknown"
            };

            var sidecarUiSync = false;

            Action refreshSidecarButtons = () =>
            {
                sidecarUiSync = true;
                if (_sidecar == null)
                {
                    chkSidecarLaunchHidden.Enabled = false;
                    btnSidecarVisibility.Enabled = false;
                    btnSidecarVisibility.Text = "Sidecar N/A";
                    lblSidecarWindowStatus.Text = "Sidecar Window: Not managed";
                    sidecarUiSync = false;
                    return;
                }

                chkSidecarLaunchHidden.Enabled = true;
                chkSidecarLaunchHidden.Checked = _sidecar.LaunchChromeHidden;
                btnSidecarVisibility.Enabled = true;
                var isVisible = _sidecar.IsManagedChromeVisible();
                btnSidecarVisibility.Text = isVisible ? "Hide Sidecar" : "Show Sidecar";
                lblSidecarWindowStatus.Text = "Sidecar Window: " + (isVisible ? "Visible" : "Hidden");
                sidecarUiSync = false;
            };

            chkSidecarLaunchHidden.CheckedChanged += (s, ev) =>
            {
                if (sidecarUiSync)
                {
                    return;
                }

                var hidden = chkSidecarLaunchHidden.Checked;
                if (_sidecar != null)
                {
                    _sidecar.SetLaunchChromeHidden(hidden);
                    if (hidden)
                    {
                        _sidecar.SetManagedChromeVisible(false);
                    }
                }

                CryptoDayTraderSuite.Properties.Settings.Default.SidecarLaunchHidden = hidden;
                CryptoDayTraderSuite.Properties.Settings.Default.Save();
                refreshSidecarButtons();
            };

            btnSidecarVisibility.Click += (s, ev) =>
            {
                if (_sidecar == null)
                {
                    refreshSidecarButtons();
                    return;
                }

                var visible = _sidecar.IsManagedChromeVisible();
                _sidecar.SetManagedChromeVisible(!visible);
                refreshSidecarButtons();
            };

            sidecarPanel.Controls.Add(chkSidecarLaunchHidden);
            sidecarPanel.Controls.Add(btnSidecarVisibility);
            sidecarPanel.Controls.Add(lblSidecarWindowStatus);
            settingsPanel.Controls.Add(sidecarPanel);
            refreshSidecarButtons();

            var btnConfig = new Button { Text = "Configure Strategies", Width = 220, Height = 40, Margin = new Padding(20) };
            btnConfig.Click += (s, ev) => {
                if (_engine != null)
                {
                    using(var dlg = new StrategyConfigDialog(_engine))
                    {
                        dlg.ShowDialog();
                    }
                }
                else MessageBox.Show("Strategy Engine not initialized.");
            };
            settingsPanel.Controls.Add(btnConfig);

            ApplyThemeToControl(settingsPanel);
            return settingsPanel;
        }
 }
}
