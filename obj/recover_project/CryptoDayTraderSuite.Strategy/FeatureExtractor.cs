using System;
using System.Collections.Generic;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Strategy
{
	public static class FeatureExtractor
	{
		public static bool TryComputeFeatures(List<Candle> candles, out Dictionary<string, decimal> features, int smaShort = 5, int smaLong = 20, int rsiPeriod = 14, int vwapPeriod = 20)
		{
			features = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
			if (candles == null || candles.Count < 2)
			{
				return false;
			}
			if (smaShort <= 0 || smaLong <= 0 || rsiPeriod <= 0 || vwapPeriod <= 0)
			{
				return false;
			}
			int minRequired = Math.Max(Math.Max(smaLong, vwapPeriod), rsiPeriod + 1);
			if (candles.Count < minRequired)
			{
				return false;
			}
			int n = candles.Count;
			decimal last = candles[n - 1].Close;
			decimal prev = candles[n - 2].Close;
			features["ret_1"] = SafeDiv(last - prev, prev);
			features["sma_short"] = Indicators.SMA(candles, n - 1, smaShort);
			features["sma_long"] = Indicators.SMA(candles, n - 1, smaLong);
			features["sma_gap"] = SafeDiv(features["sma_short"] - features["sma_long"], features["sma_long"]);
			features["rsi"] = Indicators.RSI(candles, n - 1, rsiPeriod);
			features["vwap_gap"] = VWAPGap(candles, n - 1, vwapPeriod);
			return true;
		}

		public static Dictionary<string, decimal> ComputeFeatures(List<Candle> candles, int smaShort = 5, int smaLong = 20, int rsiPeriod = 14, int vwapPeriod = 20)
		{
			Dictionary<string, decimal> f;
			return TryComputeFeatures(candles, out f, smaShort, smaLong, rsiPeriod, vwapPeriod) ? f : new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
		}

		private static decimal SafeDiv(decimal a, decimal b)
		{
			return (b == 0m) ? 0m : (a / b);
		}

		private static decimal VWAPGap(List<Candle> c, int index, int win)
		{
			int n = c.Count;
			if (n < win || index < 0 || index >= n)
			{
				return 0m;
			}
			decimal pv = default(decimal);
			decimal vol = default(decimal);
			int start = Math.Max(0, index - win + 1);
			for (int i = start; i <= index; i++)
			{
				decimal hlc3 = (c[i].High + c[i].Low + c[i].Close) / 3m;
				pv += hlc3 * c[i].Volume;
				vol += c[i].Volume;
			}
			if (vol == 0m)
			{
				return 0m;
			}
			decimal vwap = pv / vol;
			decimal last = c[index].Close;
			if (vwap == 0m)
			{
				return 0m;
			}
			return (last - vwap) / vwap;
		}
	}
}
