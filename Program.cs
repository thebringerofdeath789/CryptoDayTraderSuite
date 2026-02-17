using System;
using System.Windows.Forms;
using System.Collections.Generic;
using CryptoDayTraderSuite.Util;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Services.Messaging;
using CryptoDayTraderSuite.Services.Messaging.Events;
using CryptoDayTraderSuite.Strategy;

namespace CryptoDayTraderSuite
{
    static class Program
    {
        /* entry point */
        [STAThread]
        static void Main()
        {
            /* enable visual styles */
            Application.EnableVisualStyles(); 
            Application.SetCompatibleTextRenderingDefault(false); 
            
            // AUDIT-0001: Global Exception Handling
            // AUDIT-0010: Initialize Logger
            Log.Init(LogLevel.Debug);
            
            Application.ThreadException += (s, e) =>
            {
                Log.Error("Global Thread Error", e.Exception);
                MessageBox.Show("A critical error occurred: " + e.Exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                Log.Error("Global Domain Error", ex);
                MessageBox.Show("A fatal error occurred. Application will terminate.\n" + (ex?.Message ?? "Unknown"), "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            // DI Composition Root
            // Phase 9 Services (Keys First)
            var eventBus = new EventBus();
            Log.OnLine += line =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(line)) eventBus.Publish(new LogEvent(line));
                }
                catch { }
            };
            var keyService = new KeyService();
            var accountService = new AccountService();
            var timeFilterService = new TimeFilterService();
            var profileService = new ProfileService(accountService, keyService, timeFilterService);
            var autoModeProfileService = new AutoModeProfileService();
            var historyService = new HistoryService();

            var exchangeProvider = new ExchangeProvider(keyService);
            var executionCostModelService = new ExecutionCostModelService();
            var venueHealthService = new VenueHealthService();
            var multiVenueQuoteService = new MultiVenueQuoteService(exchangeProvider, venueHealthService);
            var spreadDivergenceDetector = new SpreadDivergenceDetector(multiVenueQuoteService);
            var fundingCarryDetector = new FundingCarryDetector();
            var strategyExchangePolicyService = new StrategyExchangePolicyService();
            var executionVenueScorer = new ExecutionVenueScorer();
            var smartOrderRouter = new SmartOrderRouter(executionVenueScorer);
            var rateRouter = new RateRouter(exchangeProvider, multiVenueQuoteService);
            var backtestService = new BacktestService(exchangeProvider, executionCostModelService);
            var chromeSidecar = new ChromeSidecar();
            chromeSidecar.SetLaunchChromeHidden(CryptoDayTraderSuite.Properties.Settings.Default.SidecarLaunchHidden);
            var strategyEngine = new StrategyEngine();
            
            // Start AI Governor
            var aiGovernor = new AIGovernor(chromeSidecar, strategyEngine, exchangeProvider, eventBus);
            aiGovernor.Start();
            
            // For AutoPlanner, we need a public client.
            var publicClient = exchangeProvider.CreatePublicClient("Coinbase");
            var strategies = new List<IStrategy> 
            { 
                new ORBStrategy(), 
                new VWAPTrendStrategy(), 
                new RSIReversionStrategy(),
                new DonchianStrategy()
            };
            var autoPlanner = new AutoPlannerService(
                publicClient,
                strategies,
                chromeSidecar,
                strategyEngine,
                multiVenueQuoteService,
                venueHealthService,
                spreadDivergenceDetector,
                smartOrderRouter,
                fundingCarryDetector,
                strategyExchangePolicyService,
                executionCostModelService);

            var form = new MainForm(profileService);
            form.InitializeDependencies(autoPlanner, exchangeProvider, backtestService, eventBus, accountService, keyService, historyService, autoModeProfileService, chromeSidecar, strategyEngine, rateRouter, aiGovernor);

            try
            {
                Application.Run(form);
            }
            finally
            {
                try { aiGovernor.Stop(); } catch { }
                try { chromeSidecar.Dispose(); } catch { }
                try { Log.Shutdown(); } catch { }
            }
        }
    }
}
