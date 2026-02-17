using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Backtest;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Strategy;

namespace CryptoDayTraderSuite.Services
{
    public class BacktestResultWrapper
    {
        public Backtester.Result RunResult { get; set; }
        public List<Candle> Candles { get; set; }
        public string Error { get; set; }
    }

    public class BacktestService
    {
        private readonly IExchangeProvider _exchangeProvider;
        private readonly StrategyEngine _engine;
        private readonly ExecutionCostModelService _executionCostModelService;

        public BacktestService(IExchangeProvider exchangeProvider, ExecutionCostModelService executionCostModelService = null)
        {
            _exchangeProvider = exchangeProvider ?? throw new ArgumentNullException(nameof(exchangeProvider));
            _engine = new StrategyEngine();
            _executionCostModelService = executionCostModelService;
        }

        public async Task<BacktestResultWrapper> RunBacktestAsync(
            string exchange,
            string product,
            string strategy,
            decimal riskPercent,
            decimal equity,
            FeeSchedule fees,
            MarketBias biasOverride = MarketBias.Neutral)
        {
            try
            {
                // Use public client for data fetching to avoid needing API keys for backtests
                // Note: Some exchanges might require auth for specific history requests, but standard is Public.
                var client = _exchangeProvider.CreatePublicClient(exchange);
                var end = DateTime.UtcNow;
                var start = end.AddDays(-7);

                // IExchangeClient.GetCandlesAsync takes granularity in MINUTES (int).
                // We want 1 minute candles.
                var candles = await client.GetCandlesAsync(product, 1, start, end);
                
                if (candles == null || candles.Count < 100) 
                    return new BacktestResultWrapper { Error = "Not enough candles (found " + (candles?.Count ?? 0) + ")" };

                // Configure Strategy
                // DONE: Decoupled string mapping to StrategyEngine.SetStrategy
                _engine.SetStrategy(strategy);
                
                // Set Bias (Simulation Mode)
                _engine.GlobalBias = biasOverride;

                var riskFrac = riskPercent / 100m;

                Func<List<Candle>, int, OrderRequest> signal = (list, idx) =>
                {
                    var price = list[idx].Close;
                    CostBreakdown cb;
                    return _engine.Evaluate(product, list, fees, equity, riskFrac, price, out cb, idx);
                };

                var costModel = _executionCostModelService;
                if (costModel == null)
                {
                    costModel = new ExecutionCostModelService();
                }

                var cost = costModel.Build(exchange, fees);
                var friction = cost.RoundTripTotalBps / 10000m;
                var res = Backtester.Run(candles, signal, friction);

                return new BacktestResultWrapper 
                { 
                    RunResult = res, 
                    Candles = candles 
                };
            }
            catch (Exception ex)
            {
                return new BacktestResultWrapper { Error = ex.Message };
            }
        }
    }
}
