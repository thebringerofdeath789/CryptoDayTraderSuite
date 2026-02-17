
using System;
using System.Collections.Generic;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Strategy
{
    public static class Indicators
    {
        public static decimal TrueRange(Candle current, Candle prev)
        {
            /* True Range (TR) = Max(H-L, Abs(H-Cp), Abs(L-Cp)) */
            var hl = current.High - current.Low;
            var hcp = Math.Abs(current.High - prev.Close);
            var lcp = Math.Abs(current.Low - prev.Close);
            return Math.Max(hl, Math.Max(hcp, lcp));
        }

        public static decimal ATR(List<Candle> candles, int index, int period)
        {
            /* average true range ending at index */
            if (candles == null || candles.Count == 0 || period <= 0 || index < period || index >= candles.Count) return 0m; /* need history */
            
            decimal sum = 0m; 
            for (int i = 0; i < period; i++)
            {
                var currIdx = index - i;
                /* For the first candle (index 0), TR is just H-L, but loop starts at period so index >= period > 0 */
                var prevIdx = currIdx - 1;
                
                sum += TrueRange(candles[currIdx], candles[prevIdx]);
            }
            return sum / period; 
        }

        public static decimal ChoppinessIndex(List<Candle> candles, int index, int period)
        {
            /* 
             * CHOP = 100 * Log10( Sum(TR, n) / ( Max(H,n) - Min(L,n) ) ) / Log10(n) 
             * Values > 61.8 indicate consolidation (Chop)
             * Values < 38.2 indicate trend
             */
            if (candles == null || candles.Count == 0 || period <= 1 || index < period || index >= candles.Count) return 50m; /* insufficient data, neutral */

            decimal sumTr = 0m;
            decimal maxHigh = decimal.MinValue;
            decimal minLow = decimal.MaxValue;

            for (int i = 0; i < period; i++)
            {
                var idx = index - i;
                var c = candles[idx];
                
                /* Calculate TR for this bar */
                /* For TR of idx, we need idx-1 */
                if (idx > 0)
                {
                    sumTr += TrueRange(c, candles[idx - 1]);
                }
                else
                {
                    /* Fallback for index 0 if it happens: simple range */
                    sumTr += (c.High - c.Low);
                }

                if (c.High > maxHigh) maxHigh = c.High;
                if (c.Low < minLow) minLow = c.Low;
            }

            var range = maxHigh - minLow;
            if (range == 0m) return 50m; /* avoid div/0, flat line is neutral/choppy? usually 0 range is weird */

            /* using Math.Log10 (requires double) */
            var ratio = (double)(sumTr / range);
            if (ratio <= 0d) return 50m;

            var denom = Math.Log10(period);
            if (denom == 0d || double.IsNaN(denom) || double.IsInfinity(denom)) return 50m;

            double chop = Math.Log10(ratio) / denom;
            if (double.IsNaN(chop) || double.IsInfinity(chop)) return 50m;

            var score = (decimal)(chop * 100.0);
            if (score < 0m) score = 0m;
            if (score > 100m) score = 100m;
            return score;
        }
        
        // Overload for backward compatibility/legacy calls (if any remain, though we should update all)
        public static decimal ATR(List<Candle> candles, int period) => ATR(candles, candles.Count - 1, period);

        public static decimal VWAP(List<Candle> candles, int index)
        {
            /* Intraday VWAP: Cumulative from start of the day (Session Reset) up to index.
             * Optimization: Iterate backwards and stop when the date changes.
             * This reduces complexity from O(TotalHistory) to O(BarsPerDay).
             */
            if (index < 0 || index >= candles.Count) return 0m;

            decimal pv = 0m; 
            decimal v = 0m; 

            // Assuming candles are sorted by time (which they must be for this to work elegantly)
            // Even if not perfectly sorted, checking Date difference is the standard "Session Break" logic.
            DateTime sessionDate = candles[index].Time.Date;
            
            for (int i = index; i >= 0; i--)
            {
                var c = candles[i];
                // Stop if we crossed into the previous day
                if (c.Time.Date != sessionDate) break;

                pv += c.Close * c.Volume; 
                v += c.Volume; 
            }
            
            if (v <= 0m) return 0m; 
            return pv / v;
        }

        public static decimal VWAP(List<Candle> candles) => VWAP(candles, candles.Count - 1);

        public static decimal SMA(List<Candle> candles, int index, int period)
        {
            if (index < period - 1) return 0m; 
            decimal sum = 0m; 
            for (int i = 0; i < period; i++) 
                sum += candles[index - i].Close; 
            return sum / period; 
        }

        public static decimal SMA(List<Candle> candles, int period) => SMA(candles, candles.Count - 1, period);

        public static decimal RSI(List<Candle> candles, int index, int period)
        {
            if (index < period) return 50m; // Insufficient history

            decimal sumGain = 0m;
            decimal sumLoss = 0m;

            for (int i = 0; i < period; i++)
            {
                var currIdx = index - i;
                if (currIdx <= 0) return 50m;

                var current = candles[currIdx];
                var prev = candles[currIdx - 1];
                var change = current.Close - prev.Close;

                if (change > 0)
                    sumGain += change;
                else
                    sumLoss += Math.Abs(change);
            }

            decimal avgGain = sumGain / period;
            decimal avgLoss = sumLoss / period;

            if (avgLoss == 0m)
            {
                if (avgGain == 0m) return 50m; // No change
                return 100m; // All gains
            }

            decimal rs = avgGain / avgLoss;
            return 100m - (100m / (1m + rs));
        }

        public static decimal RSI(List<Candle> candles, int period) => RSI(candles, candles.Count - 1, period);
    }
}
