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

		public decimal OversoldLevel { get; set; } = 30m;

		public decimal OverboughtLevel { get; set; } = 70m;

		public decimal StopAtrMulti { get; set; } = 2.0m;

		public StrategyResult GetSignal(List<Candle> candles)
		{
			if (candles == null || candles.Count == 0)
			{
				return new StrategyResult
				{
					StrategyName = Name,
					IsSignal = false,
					ConfidenceScore = 0.0
				};
			}
			return GetSignal(candles, candles.Count - 1);
		}

		public StrategyResult GetSignal(List<Candle> candles, int index)
		{
			StrategyResult result = new StrategyResult
			{
				StrategyName = Name,
				IsSignal = false,
				ConfidenceScore = 0.0
			};
			if (candles == null || candles.Count == 0 || index < 0 || index >= candles.Count)
			{
				return result;
			}
			if (index < Math.Max(SmaPeriod, RsiPeriod + 1))
			{
				return result;
			}
			decimal rsi = Indicators.RSI(candles, index, RsiPeriod);
			decimal atr = Indicators.ATR(candles, index, AtrPeriod);
			decimal sma = Indicators.SMA(candles, index, SmaPeriod);
			decimal close = candles[index].Close;
			if (rsi < OversoldLevel)
			{
				result.IsSignal = true;
				result.Side = OrderSide.Buy;
				result.EntryPrice = close;
				result.StopLoss = close - StopAtrMulti * atr;
				result.TakeProfit = sma;
				result.ConfidenceScore = (double)(Math.Max(0m, OversoldLevel - rsi) * (100m / OversoldLevel));
				if (result.ConfidenceScore > 100.0)
				{
					result.ConfidenceScore = 100.0;
				}
			}
			else if (rsi > OverboughtLevel)
			{
				result.IsSignal = true;
				result.Side = OrderSide.Sell;
				result.EntryPrice = close;
				result.StopLoss = close + StopAtrMulti * atr;
				result.TakeProfit = sma;
				result.ConfidenceScore = (double)(Math.Max(0m, rsi - OverboughtLevel) * (100m / (100m - OverboughtLevel)));
				if (result.ConfidenceScore > 100.0)
				{
					result.ConfidenceScore = 100.0;
				}
			}
			return result;
		}
	}
}
