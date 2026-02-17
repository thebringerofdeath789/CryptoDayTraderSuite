using System;
using System.Collections.Generic;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Strategy
{
	public class VWAPTrendStrategy : IStrategy
	{
		public decimal StopPercent = 0.005m;

		public decimal TargetPercent = 0.01m;

		public string Name => "VWAP Trend";

		public int LookbackMinutes { get; set; } = 120;

		public decimal PullbackMultipleATR { get; set; } = 0.5m;

		public int AtrPeriod { get; set; } = 14;

		public bool UseChopFilter { get; set; } = true;

		public int ChopPeriod { get; set; } = 14;

		public decimal ChopThreshold { get; set; } = 61.8m;

		public StrategyResult GetSignal(List<Candle> intraday)
		{
			if (intraday == null || intraday.Count == 0)
			{
				return new StrategyResult
				{
					StrategyName = Name
				};
			}
			return GetSignal(intraday, intraday.Count - 1);
		}

		public StrategyResult GetSignal(List<Candle> intraday, int index)
		{
			StrategyResult res = new StrategyResult
			{
				StrategyName = Name
			};
			if (intraday == null || intraday.Count == 0 || index < 0 || index >= intraday.Count)
			{
				return res;
			}
			if (TrySignal(intraday, index, out var side))
			{
				decimal lastPrice = intraday[index].Close;
				res.IsSignal = true;
				res.Side = side;
				res.EntryPrice = lastPrice;
				if (side == OrderSide.Buy)
				{
					res.StopLoss = lastPrice * (1m - StopPercent);
					res.TakeProfit = lastPrice * (1m + TargetPercent);
				}
				else
				{
					res.StopLoss = lastPrice * (1m + StopPercent);
					res.TakeProfit = lastPrice * (1m - TargetPercent);
				}
				res.ConfidenceScore = 60.0;
			}
			return res;
		}

		public bool TrySignal(List<Candle> intraday, int index, out OrderSide side)
		{
			side = OrderSide.Buy;
			if (intraday == null || index < AtrPeriod + 2)
			{
				return false;
			}
			decimal vwap = Indicators.VWAP(intraday, index);
			decimal atr = Indicators.ATR(intraday, index, AtrPeriod);
			if (atr <= 0m)
			{
				return false;
			}
			Candle last = intraday[index];
			Candle prev = intraday[index - 1];
			if (LookbackMinutes > 0)
			{
				DateTime threshold = last.Time.AddMinutes(-LookbackMinutes);
				int barsInWindow = 0;
				int i = index;
				while (i >= 0 && !(intraday[i].Time < threshold))
				{
					barsInWindow++;
					i--;
				}
				if (barsInWindow < 3)
				{
					return false;
				}
			}
			if (PullbackMultipleATR > 0m)
			{
				decimal distFromVwap = Math.Abs(last.Close - vwap);
				if (distFromVwap > PullbackMultipleATR * atr)
				{
					return false;
				}
			}
			if (UseChopFilter)
			{
				decimal chop = Indicators.ChoppinessIndex(intraday, index, ChopPeriod);
				if (chop > ChopThreshold)
				{
					return false;
				}
			}
			bool upTrend = last.Close > vwap && prev.Close > vwap;
			bool downTrend = last.Close < vwap && prev.Close < vwap;
			if (upTrend)
			{
				side = OrderSide.Buy;
				return true;
			}
			if (downTrend)
			{
				side = OrderSide.Sell;
				return true;
			}
			return false;
		}
	}
}
