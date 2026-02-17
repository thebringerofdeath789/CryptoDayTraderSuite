using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Backtest;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Strategy;

namespace CryptoDayTraderSuite.Services
{
	public class BacktestService
	{
		private readonly IExchangeProvider _exchangeProvider;

		private readonly StrategyEngine _engine;

		public BacktestService(IExchangeProvider exchangeProvider)
		{
			_exchangeProvider = exchangeProvider ?? throw new ArgumentNullException("exchangeProvider");
			_engine = new StrategyEngine();
		}

		public async Task<BacktestResultWrapper> RunBacktestAsync(string exchange, string product, string strategy, decimal riskPercent, decimal equity, FeeSchedule fees, MarketBias biasOverride = MarketBias.Neutral)
		{
			try
			{
				IExchangeClient client = _exchangeProvider.CreatePublicClient(exchange);
				DateTime end = DateTime.UtcNow;
				List<Candle> candles = await client.GetCandlesAsync(startUtc: end.AddDays(-7.0), productId: product, granularity: 1, endUtc: end);
				if (candles == null || candles.Count < 100)
				{
					return new BacktestResultWrapper
					{
						Error = "Not enough candles (found " + (candles?.Count ?? 0) + ")"
					};
				}
				_engine.SetStrategy(strategy);
				_engine.GlobalBias = biasOverride;
				decimal riskFrac = riskPercent / 100m;
				Func<List<Candle>, int, OrderRequest> signal = delegate(List<Candle> list, int idx)
				{
					decimal close = list[idx].Close;
					CostBreakdown costs;
					return _engine.Evaluate(product, list, fees, equity, riskFrac, close, out costs, idx);
				};
				decimal friction = fees.MakerRate + fees.TakerRate + 0.0005m;
				Backtester.Result res = Backtester.Run(candles, signal, friction);
				return new BacktestResultWrapper
				{
					RunResult = res,
					Candles = candles
				};
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				return new BacktestResultWrapper
				{
					Error = ex2.Message
				};
			}
		}
	}
}
