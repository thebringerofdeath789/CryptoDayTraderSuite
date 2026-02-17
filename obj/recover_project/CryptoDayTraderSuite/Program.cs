using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Services.Messaging;
using CryptoDayTraderSuite.Services.Messaging.Events;
using CryptoDayTraderSuite.Strategy;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite
{
	internal static class Program
	{
		[STAThread]
		private static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(defaultValue: false);
			Log.Init(LogLevel.Debug);
			Application.ThreadException += delegate(object s, ThreadExceptionEventArgs e)
			{
				Log.Error("Global Thread Error", e.Exception, "Main", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Program.cs", 28);
				MessageBox.Show("A critical error occurred: " + e.Exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			};
			AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
			{
				Exception ex = e.ExceptionObject as Exception;
				Log.Error("Global Domain Error", ex, "Main", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Program.cs", 35);
				MessageBox.Show("A fatal error occurred. Application will terminate.\n" + (ex?.Message ?? "Unknown"), "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			};
			EventBus eventBus = new EventBus();
			Log.OnLine += delegate(string line)
			{
				try
				{
					if (!string.IsNullOrWhiteSpace(line))
					{
						eventBus.Publish(new LogEvent(line));
					}
				}
				catch
				{
				}
			};
			KeyService keyService = new KeyService();
			AccountService accountService = new AccountService();
			TimeFilterService timeFilterService = new TimeFilterService();
			ProfileService profileService = new ProfileService(accountService, keyService, timeFilterService);
			AutoModeProfileService autoModeProfileService = new AutoModeProfileService();
			HistoryService historyService = new HistoryService();
			ExchangeProvider exchangeProvider = new ExchangeProvider(keyService);
			RateRouter rateRouter = new RateRouter(exchangeProvider);
			BacktestService backtestService = new BacktestService(exchangeProvider);
			ChromeSidecar chromeSidecar = new ChromeSidecar();
			StrategyEngine strategyEngine = new StrategyEngine();
			AIGovernor aiGovernor = new AIGovernor(chromeSidecar, strategyEngine, exchangeProvider, eventBus);
			aiGovernor.Start();
			IExchangeClient publicClient = exchangeProvider.CreatePublicClient("Coinbase");
			List<IStrategy> strategies = new List<IStrategy>
			{
				new ORBStrategy(),
				new VWAPTrendStrategy(),
				new RSIReversionStrategy(),
				new DonchianStrategy()
			};
			AutoPlannerService autoPlanner = new AutoPlannerService(publicClient, strategies, chromeSidecar, strategyEngine);
			MainForm form = new MainForm(profileService);
			form.InitializeDependencies(autoPlanner, exchangeProvider, backtestService, eventBus, accountService, keyService, historyService, autoModeProfileService, chromeSidecar, strategyEngine, rateRouter, aiGovernor);
			try
			{
				Application.Run(form);
			}
			finally
			{
				try
				{
					aiGovernor.Stop();
				}
				catch
				{
				}
				try
				{
					chromeSidecar.Dispose();
				}
				catch
				{
				}
			}
		}
	}
}
