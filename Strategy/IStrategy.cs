using System.Collections.Generic;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Strategy
{
    public class StrategyResult
    {
        public bool IsSignal { get; set; }
        public OrderSide Side { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal StopLoss { get; set; }
        public decimal TakeProfit { get; set; }
        public string StrategyName { get; set; }
        public double ConfidenceScore { get; set; } /* Added for Planner scoring */
    }

    public interface IStrategy
    {
        string Name { get; }
        StrategyResult GetSignal(List<Candle> candles); /* Legacy: Defaults to last candle */
        StrategyResult GetSignal(List<Candle> candles, int index); /* Optimization: Analyze at specific index */
    }
}
