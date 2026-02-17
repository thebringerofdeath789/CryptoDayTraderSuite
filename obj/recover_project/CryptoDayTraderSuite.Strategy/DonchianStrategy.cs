using System.Collections.Generic;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Strategy
{
	public class DonchianStrategy : IStrategy
	{
		public int Lookback = 20;

		public decimal AtrMult = 2.0m;

		public decimal MinVolRatio = 1.2m;

		public int AtrPeriod = 14;

		public string Name => "Donchian 20";

		public StrategyResult GetSignal(List<Candle> candles)
		{
			if (candles == null || candles.Count == 0)
			{
				return new StrategyResult
				{
					StrategyName = Name
				};
			}
			return GetSignal(candles, candles.Count - 1);
		}

		public StrategyResult GetSignal(List<Candle> candles, int index)
		{
			StrategyResult res = new StrategyResult
			{
				StrategyName = Name
			};
			if (candles == null || candles.Count == 0 || index < 0 || index >= candles.Count)
			{
				return res;
			}
			if (TrySignal(candles, index, out var side, out var entry, out var stop, out var tp))
			{
				res.IsSignal = true;
				res.Side = side;
				res.EntryPrice = entry;
				res.StopLoss = stop;
				res.TakeProfit = tp;
				res.ConfidenceScore = 70.0;
			}
			return res;
		}

		private bool TrySignal(List<Candle> candles, int index, out OrderSide side, out decimal entry, out decimal stop, out decimal takeProfit)
		{
			side = OrderSide.Buy;
			entry = (stop = (takeProfit = 0m));
			if (candles == null || candles.Count == 0 || index < Lookback + 1 || index >= candles.Count)
			{
				return false;
			}
			decimal atr = Indicators.ATR(candles, index, AtrPeriod);
			if (atr <= 0m)
			{
				return false;
			}
			decimal volSum = default(decimal);
			int volCnt = 0;
			for (int k = 0; k < 20; k++)
			{
				if (index - k >= 0)
				{
					volSum += candles[index - k].Volume;
					volCnt++;
				}
			}
			decimal avgVol = ((volCnt > 0) ? (volSum / (decimal)volCnt) : 0m);
			if (avgVol > 0m && candles[index].Volume / avgVol < MinVolRatio)
			{
				return false;
			}
			decimal hh = decimal.MinValue;
			decimal ll = decimal.MaxValue;
			for (int i = 1; i <= Lookback; i++)
			{
				Candle past = candles[index - i];
				if (past.High > hh)
				{
					hh = past.High;
				}
				if (past.Low < ll)
				{
					ll = past.Low;
				}
			}
			decimal close = candles[index].Close;
			if (close > hh)
			{
				side = OrderSide.Buy;
				entry = close;
				stop = close - AtrMult * atr;
				takeProfit = close + AtrMult * atr;
				return true;
			}
			if (close < ll)
			{
				side = OrderSide.Sell;
				entry = close;
				stop = close + AtrMult * atr;
				takeProfit = close - AtrMult * atr;
				return true;
			}
			return false;
		}
	}
}
