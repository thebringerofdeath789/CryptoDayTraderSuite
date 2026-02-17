
using System;
using System.Collections.Generic;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Strategy
{
    public class ORBStrategy : IStrategy
    {
        public string Name => "ORB 15m";
        public TimeSpan OpeningRange { get; set; } = TimeSpan.FromMinutes(15);
        public decimal BufferATRMultiples { get; set; } = 0.25m;
        public decimal StopATRMultiples { get; set; } = 0.75m;
        public decimal TakeProfitATRMultiples { get; set; } = 1.5m;
        public int AtrPeriod { get; set; } = 14;

        public StrategyResult GetSignal(List<Candle> dayCandles)
        {
            if (dayCandles == null || dayCandles.Count == 0)
                return new StrategyResult { StrategyName = Name };
            return GetSignal(dayCandles, dayCandles.Count - 1);
        }

        public StrategyResult GetSignal(List<Candle> dayCandles, int index)
        {
            var res = new StrategyResult { StrategyName = Name };
            if (dayCandles == null || dayCandles.Count == 0 || index < 0 || index >= dayCandles.Count) return res;
            if (TrySignal(dayCandles, index, out var side, out var entry, out var stop, out var tp))
            {
                res.IsSignal = true;
                res.Side = side;
                res.EntryPrice = entry;
                res.StopLoss = stop;
                res.TakeProfit = tp;
                res.ConfidenceScore = 80.0;
            }
            return res;
        }

        public bool TrySignal(List<Candle> dayCandles, int index, out OrderSide side, out decimal entry, out decimal stop, out decimal takeProfit)
        {
            side = OrderSide.Buy; entry = stop = takeProfit = 0m; /* init */
            if (dayCandles == null || index < 1 || index >= dayCandles.Count) return false; 
            
            var atr = Indicators.ATR(dayCandles, index, AtrPeriod); if (atr <= 0m) return false; 
            
            /* AUDIT-0017 Fix: Explicit session open check (00:00 UTC). */
            /* Do not rely on dayCandles[0] being the start. Find the start of the current day. */
            
            var sessionDate = dayCandles[index].Time.Date; /* 00:00 UTC of the signal candle */
            var orHigh = decimal.MinValue; 
            var orLow = decimal.MaxValue; 
            bool rangeEstablished = false;

            /* Scan backwards to find today's data or just scan relative to session start if we can find it efficiently. */
            /* Since list is time-ordered, we finding the first candle >= sessionDate. */
            /* Optimization: Since we are usually iterating sequentially, creating a cache would be better, but local loop is safer for statelessness. */
            
            int i = index;
            while (i >= 0 && dayCandles[i].Time >= sessionDate)
            {
                i--;
            }
            // i is now the index BEFORE the session start, or -1. So start at i+1.
            int sessionStartIndex = i + 1;

            // Scan form session start for OpeningRange duration
            for (int j = sessionStartIndex; j <= index; j++)
            {
                var c = dayCandles[j];
                var timeIntoSession = c.Time - sessionDate;
                
                if (timeIntoSession <= OpeningRange)
                {
                    if (c.High > orHigh) orHigh = c.High; 
                    if (c.Low < orLow) orLow = c.Low;
                    rangeEstablished = true;
                }
                else
                {
                    // passed opening range
                    break;
                }
            }

            if (!rangeEstablished || orHigh <= 0m || orLow >= decimal.MaxValue || orHigh <= orLow) return false; /* invalid or no data in range */
            
            /* If we are still INSIDE the opening range, do not signal yet? ORB usually implies waiting for the range to close. */
            if ((dayCandles[index].Time - sessionDate) <= OpeningRange) return false;

            /* breakout check on the CURRENT candle (index) */
            var last = dayCandles[index]; 
            if (last.Close > orHigh + BufferATRMultiples * atr)
            {
                side = OrderSide.Buy; entry = last.Close; stop = last.Close - StopATRMultiples * atr; takeProfit = last.Close + TakeProfitATRMultiples * atr; return true; 
            }
            if (last.Close < orLow - BufferATRMultiples * atr)
            {
                side = OrderSide.Sell; entry = last.Close; stop = last.Close + StopATRMultiples * atr; takeProfit = last.Close - TakeProfitATRMultiples * atr; return true; 
            }
            return false; /* no signal */
        }
    }
}
