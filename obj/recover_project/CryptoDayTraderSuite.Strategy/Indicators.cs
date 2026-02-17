using System;
using System.Collections.Generic;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Strategy
{
	public static class Indicators
	{
		public static decimal TrueRange(Candle current, Candle prev)
		{
			decimal hl = current.High - current.Low;
			decimal hcp = Math.Abs(current.High - prev.Close);
			decimal lcp = Math.Abs(current.Low - prev.Close);
			return Math.Max(hl, Math.Max(hcp, lcp));
		}

		public static decimal ATR(List<Candle> candles, int index, int period)
		{
			if (candles == null || candles.Count == 0 || period <= 0 || index < period || index >= candles.Count)
			{
				return 0m;
			}
			decimal sum = default(decimal);
			for (int i = 0; i < period; i++)
			{
				int currIdx = index - i;
				int prevIdx = currIdx - 1;
				sum += TrueRange(candles[currIdx], candles[prevIdx]);
			}
			return sum / (decimal)period;
		}

		public static decimal ChoppinessIndex(List<Candle> candles, int index, int period)
		{
			if (candles == null || candles.Count == 0 || period <= 1 || index < period || index >= candles.Count)
			{
				return 50m;
			}
			decimal sumTr = default(decimal);
			decimal maxHigh = decimal.MinValue;
			decimal minLow = decimal.MaxValue;
			for (int i = 0; i < period; i++)
			{
				int idx = index - i;
				Candle c = candles[idx];
				if (idx > 0)
				{
					sumTr += TrueRange(c, candles[idx - 1]);
				}
				else
				{
					sumTr += c.High - c.Low;
				}
				if (c.High > maxHigh)
				{
					maxHigh = c.High;
				}
				if (c.Low < minLow)
				{
					minLow = c.Low;
				}
			}
			decimal range = maxHigh - minLow;
			if (range == 0m)
			{
				return 50m;
			}
			double ratio = (double)(sumTr / range);
			if (ratio <= 0.0)
			{
				return 50m;
			}
			double denom = Math.Log10(period);
			if (denom == 0.0 || double.IsNaN(denom) || double.IsInfinity(denom))
			{
				return 50m;
			}
			double chop = Math.Log10(ratio) / denom;
			if (double.IsNaN(chop) || double.IsInfinity(chop))
			{
				return 50m;
			}
			decimal score = (decimal)(chop * 100.0);
			if (score < 0m)
			{
				score = default(decimal);
			}
			if (score > 100m)
			{
				score = 100m;
			}
			return score;
		}

		public static decimal ATR(List<Candle> candles, int period)
		{
			return ATR(candles, candles.Count - 1, period);
		}

		public static decimal VWAP(List<Candle> candles, int index)
		{
			if (index < 0 || index >= candles.Count)
			{
				return 0m;
			}
			decimal pv = default(decimal);
			decimal v = default(decimal);
			DateTime sessionDate = candles[index].Time.Date;
			for (int i = index; i >= 0; i--)
			{
				Candle c = candles[i];
				if (c.Time.Date != sessionDate)
				{
					break;
				}
				pv += c.Close * c.Volume;
				v += c.Volume;
			}
			if (v <= 0m)
			{
				return 0m;
			}
			return pv / v;
		}

		public static decimal VWAP(List<Candle> candles)
		{
			return VWAP(candles, candles.Count - 1);
		}

		public static decimal SMA(List<Candle> candles, int index, int period)
		{
			if (index < period - 1)
			{
				return 0m;
			}
			decimal sum = default(decimal);
			for (int i = 0; i < period; i++)
			{
				sum += candles[index - i].Close;
			}
			return sum / (decimal)period;
		}

		public static decimal SMA(List<Candle> candles, int period)
		{
			return SMA(candles, candles.Count - 1, period);
		}

		public static decimal RSI(List<Candle> candles, int index, int period)
		{
			if (index < period)
			{
				return 50m;
			}
			decimal sumGain = default(decimal);
			decimal sumLoss = default(decimal);
			for (int i = 0; i < period; i++)
			{
				int currIdx = index - i;
				if (currIdx <= 0)
				{
					return 50m;
				}
				Candle current = candles[currIdx];
				Candle prev = candles[currIdx - 1];
				decimal change = current.Close - prev.Close;
				if (change > 0m)
				{
					sumGain += change;
				}
				else
				{
					sumLoss += Math.Abs(change);
				}
			}
			decimal avgGain = sumGain / (decimal)period;
			decimal avgLoss = sumLoss / (decimal)period;
			if (avgLoss == 0m)
			{
				if (avgGain == 0m)
				{
					return 50m;
				}
				return 100m;
			}
			decimal rs = avgGain / avgLoss;
			return 100m - 100m / (1m + rs);
		}

		public static decimal RSI(List<Candle> candles, int period)
		{
			return RSI(candles, candles.Count - 1, period);
		}
	}
}
