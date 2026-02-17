using System;
using System.Collections.Generic;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Strategy
{
	public class ORBStrategy : IStrategy
	{
		public string Name => "ORB 15m";

		public TimeSpan OpeningRange { get; set; } = TimeSpan.FromMinutes(15.0);

		public decimal BufferATRMultiples { get; set; } = 0.25m;

		public decimal StopATRMultiples { get; set; } = 0.75m;

		public decimal TakeProfitATRMultiples { get; set; } = 1.5m;

		public int AtrPeriod { get; set; } = 14;

		public StrategyResult GetSignal(List<Candle> dayCandles)
		{
			if (dayCandles == null || dayCandles.Count == 0)
			{
				return new StrategyResult
				{
					StrategyName = Name
				};
			}
			return GetSignal(dayCandles, dayCandles.Count - 1);
		}

		public StrategyResult GetSignal(List<Candle> dayCandles, int index)
		{
			StrategyResult res = new StrategyResult
			{
				StrategyName = Name
			};
			if (dayCandles == null || dayCandles.Count == 0 || index < 0 || index >= dayCandles.Count)
			{
				return res;
			}
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
			side = OrderSide.Buy;
			entry = (stop = (takeProfit = 0m));
			if (dayCandles == null || index < 1 || index >= dayCandles.Count)
			{
				return false;
			}
			decimal atr = Indicators.ATR(dayCandles, index, AtrPeriod);
			if (atr <= 0m)
			{
				return false;
			}
			DateTime sessionDate = dayCandles[index].Time.Date;
			decimal orHigh = decimal.MinValue;
			decimal orLow = decimal.MaxValue;
			bool rangeEstablished = false;
			int i = index;
			while (i >= 0 && dayCandles[i].Time >= sessionDate)
			{
				i--;
			}
			int sessionStartIndex = i + 1;
			for (int j = sessionStartIndex; j <= index; j++)
			{
				Candle c = dayCandles[j];
				TimeSpan timeIntoSession = c.Time - sessionDate;
				if (timeIntoSession <= OpeningRange)
				{
					if (c.High > orHigh)
					{
						orHigh = c.High;
					}
					if (c.Low < orLow)
					{
						orLow = c.Low;
					}
					rangeEstablished = true;
					continue;
				}
				break;
			}
			if (!rangeEstablished || orHigh <= 0m || orLow >= decimal.MaxValue || orHigh <= orLow)
			{
				return false;
			}
			if (dayCandles[index].Time - sessionDate <= OpeningRange)
			{
				return false;
			}
			Candle last = dayCandles[index];
			if (last.Close > orHigh + BufferATRMultiples * atr)
			{
				side = OrderSide.Buy;
				entry = last.Close;
				stop = last.Close - StopATRMultiples * atr;
				takeProfit = last.Close + TakeProfitATRMultiples * atr;
				return true;
			}
			if (last.Close < orLow - BufferATRMultiples * atr)
			{
				side = OrderSide.Sell;
				entry = last.Close;
				stop = last.Close + StopATRMultiples * atr;
				takeProfit = last.Close - TakeProfitATRMultiples * atr;
				return true;
			}
			return false;
		}
	}
}
