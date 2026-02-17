
using System;
using System.Collections.Generic;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Strategy
{
    public class VWAPTrendStrategy : IStrategy
    {
        public string Name => "VWAP Trend";
        public int LookbackMinutes { get; set; } = 120;
        public decimal PullbackMultipleATR { get; set; } = 0.5m;
        public int AtrPeriod { get; set; } = 14;
        
        /* Chop-Block Filter */
        public bool UseChopFilter { get; set; } = true;
        public int ChopPeriod { get; set; } = 14;
        public decimal ChopThreshold { get; set; } = 61.8m;

        /* standard risk params for vwap trend following */
        public decimal StopPercent = 0.005m; /* 0.5% */
        public decimal TargetPercent = 0.01m; /* 1.0% */

        public StrategyResult GetSignal(List<Candle> intraday)
        {
            if (intraday == null || intraday.Count == 0)
                return new StrategyResult { StrategyName = Name };
            return GetSignal(intraday, intraday.Count - 1);
        }

        public StrategyResult GetSignal(List<Candle> intraday, int index)
        {
             var res = new StrategyResult { StrategyName = Name };
               if (intraday == null || intraday.Count == 0 || index < 0 || index >= intraday.Count) return res;
             if (TrySignal(intraday, index, out var side))
             {
                 var lastPrice = intraday[index].Close;
                 res.IsSignal = true;
                 res.Side = side;
                 res.EntryPrice = lastPrice;
                 
                 if (side == OrderSide.Buy)
                 {
                     res.StopLoss = lastPrice * (1 - StopPercent);
                     res.TakeProfit = lastPrice * (1 + TargetPercent);
                 }
                 else
                 {
                     res.StopLoss = lastPrice * (1 + StopPercent);
                     res.TakeProfit = lastPrice * (1 - TargetPercent);
                 }
                 res.ConfidenceScore = 60.0;
             }
             return res;
        }

        public bool TrySignal(List<Candle> intraday, int index, out OrderSide side)
        {
            side = OrderSide.Buy; /* default */
            if (intraday == null || index < AtrPeriod + 2) return false; 
            
            var vwap = Indicators.VWAP(intraday, index); 
            var atr = Indicators.ATR(intraday, index, AtrPeriod); if (atr <= 0m) return false; 
            
            var last = intraday[index]; 
            var prev = intraday[index - 1]; // Was -2 in original code? Original said Count - 2. So index-1.

            if (LookbackMinutes > 0)
            {
                var threshold = last.Time.AddMinutes(-LookbackMinutes);
                var barsInWindow = 0;
                for (int i = index; i >= 0; i--)
                {
                    if (intraday[i].Time < threshold) break;
                    barsInWindow++;
                }
                if (barsInWindow < 3) return false;
            }

            if (PullbackMultipleATR > 0m)
            {
                var distFromVwap = Math.Abs(last.Close - vwap);
                if (distFromVwap > PullbackMultipleATR * atr) return false;
            }

            /* Chop-Block: Filter out ranging markets */
            if (UseChopFilter)
            {
                var chop = Indicators.ChoppinessIndex(intraday, index, ChopPeriod);
                if (chop > ChopThreshold) return false; /* Market is ranging/choppy, avoid vwap continuation */
            }

            /* trend criteria */
            var upTrend = last.Close > vwap && prev.Close > vwap; 
            var downTrend = last.Close < vwap && prev.Close < vwap; 
            
            if (upTrend) { side = OrderSide.Buy; return true; } 
            if (downTrend) { side = OrderSide.Sell; return true; } 
            return false; /* no signal */
        }
    }
}
