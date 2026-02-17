using System;

namespace CryptoDayTraderSuite.Strategy
{
	public static class RiskGuards
	{
		public static bool FeesKillEdge(decimal grossProfit, decimal entryNotional, decimal feeRate)
		{
			if (grossProfit <= 0m)
			{
				return true;
			}
			decimal fees = entryNotional * feeRate * 2m;
			decimal ratio = fees / grossProfit;
			return ratio >= 0.25m;
		}

		[Obsolete("Use FeesKillEdge(grossProfit, entryNotional, feeRate).")]
		public static bool FeesKillEdge(decimal targetTicks, decimal tickValue, decimal qty, decimal makerFee, decimal takerFee, bool takerEntry)
		{
			if (targetTicks <= 0m || tickValue <= 0m || qty <= 0m)
			{
				return true;
			}
			decimal grossProfit = targetTicks * tickValue * qty;
			decimal entryNotional = tickValue * qty;
			decimal feeRate = (takerEntry ? ((takerFee + makerFee) / 2m) : ((makerFee + takerFee) / 2m));
			if (feeRate < 0m)
			{
				feeRate = default(decimal);
			}
			return FeesKillEdge(grossProfit, entryNotional, feeRate);
		}

		public static bool SpreadTooWide(decimal spread, decimal atr, decimal maxSpreadToAtr)
		{
			if (atr <= 0m)
			{
				return false;
			}
			decimal r = spread / atr;
			return r > maxSpreadToAtr;
		}

		public static bool VolatilityShock(decimal atrNow, decimal atrMedian, decimal spikeFactor)
		{
			if (atrMedian <= 0m)
			{
				return false;
			}
			return atrNow >= spikeFactor * atrMedian;
		}
	}
}
