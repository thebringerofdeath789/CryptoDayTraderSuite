using System;
using System.Collections.Generic;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Strategy
{
    public class RSIReversionStrategy : IStrategy
    {
        public string Name => "RSIReversion";
        
        public int RsiPeriod { get; set; } = 14;
        public int SmaPeriod { get; set; } = 20;
        public int AtrPeriod { get; set; } = 14;
        public decimal OversoldLevel { get; set; } = 30;
        public decimal OverboughtLevel { get; set; } = 70;
        public decimal StopAtrMulti { get; set; } = 2.0m;

        public StrategyResult GetSignal(List<Candle> candles)
        {
            if (candles == null || candles.Count == 0)
                return new StrategyResult { StrategyName = Name, IsSignal = false, ConfidenceScore = 0 };
            return GetSignal(candles, candles.Count - 1);
        }

        public StrategyResult GetSignal(List<Candle> candles, int index)
        {
            var result = new StrategyResult
            {
                StrategyName = Name,
                IsSignal = false,
                ConfidenceScore = 0
            };

            if (candles == null || candles.Count == 0 || index < 0 || index >= candles.Count) return result;

            // Ensure specific history requirements
            if (index < Math.Max(SmaPeriod, RsiPeriod + 1)) return result;

            decimal rsi = Indicators.RSI(candles, index, RsiPeriod);
            decimal atr = Indicators.ATR(candles, index, AtrPeriod);
            decimal sma = Indicators.SMA(candles, index, SmaPeriod);
            decimal close = candles[index].Close;

            if (rsi < OversoldLevel) /* Oversold -> Long */
            {
                result.IsSignal = true;
                result.Side = OrderSide.Buy;
                result.EntryPrice = close;
                result.StopLoss = close - (StopAtrMulti * atr);
                result.TakeProfit = sma; /* Mean Reversion Target */
                
                /* Calculate dynamic confidence based on RSI depth (0-100 scale) */
                result.ConfidenceScore = (double)(Math.Max(0, OversoldLevel - rsi) * (100m / OversoldLevel));
                if (result.ConfidenceScore > 100d) result.ConfidenceScore = 100d;
            }
            else if (rsi > OverboughtLevel) /* Overbought -> Short */
            {
                result.IsSignal = true;
                result.Side = OrderSide.Sell;
                result.EntryPrice = close;
                result.StopLoss = close + (StopAtrMulti * atr);
                result.TakeProfit = sma; /* Mean Reversion Target */

                /* Calculate dynamic confidence based on RSI height (0-100 scale) */
                result.ConfidenceScore = (double)(Math.Max(0, rsi - OverboughtLevel) * (100m / (100m - OverboughtLevel)));
                if (result.ConfidenceScore > 100d) result.ConfidenceScore = 100d;
            }

            return result;
        }
    }
}