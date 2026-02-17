using System;
using System.Collections.Generic;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Strategy
{
	public class StrategyEngine
	{
		public StrategyKind Active { get; set; }

		public ORBStrategy Orb { get; private set; } = new ORBStrategy();

		public VWAPTrendStrategy VwapTrend { get; private set; } = new VWAPTrendStrategy();

		public RSIReversionStrategy RsiReversion { get; private set; } = new RSIReversionStrategy();

		public DonchianStrategy Donchian { get; private set; } = new DonchianStrategy();

		public MarketBias GlobalBias { get; set; } = MarketBias.Neutral;

		public void SetStrategy(string name)
		{
			if (!string.IsNullOrEmpty(name))
			{
				if (name.Equals("ORB", StringComparison.OrdinalIgnoreCase) || name.Equals("ORB Strategy", StringComparison.OrdinalIgnoreCase))
				{
					Active = StrategyKind.ORB;
				}
				else if (name.Equals("VWAPTrend", StringComparison.OrdinalIgnoreCase) || name.Equals("VWAP Trend", StringComparison.OrdinalIgnoreCase))
				{
					Active = StrategyKind.VWAPTrend;
				}
				else if (name.Equals("RSIReversion", StringComparison.OrdinalIgnoreCase) || name.Equals("RSI Reversion", StringComparison.OrdinalIgnoreCase))
				{
					Active = StrategyKind.RSIReversion;
				}
				else if (name.Equals("Donchian", StringComparison.OrdinalIgnoreCase) || name.Equals("Donchian 20", StringComparison.OrdinalIgnoreCase) || name.Equals("DonchianStrategy", StringComparison.OrdinalIgnoreCase))
				{
					Active = StrategyKind.Donchian;
				}
			}
		}

		public OrderRequest Evaluate(string productId, List<Candle> candles, FeeSchedule fees, decimal equityUsd, decimal riskFraction, decimal price, out CostBreakdown costs, int index = -1)
		{
			if (string.IsNullOrWhiteSpace(productId) || candles == null || candles.Count == 0 || fees == null)
			{
				costs = new CostBreakdown();
				return null;
			}
			costs = new CostBreakdown();
			IStrategy strategy;
			if (Active != StrategyKind.ORB)
			{
				if (Active != StrategyKind.VWAPTrend)
				{
					if (Active != StrategyKind.Donchian)
					{
						IStrategy rsiReversion = RsiReversion;
						strategy = rsiReversion;
					}
					else
					{
						IStrategy rsiReversion = Donchian;
						strategy = rsiReversion;
					}
				}
				else
				{
					IStrategy rsiReversion = VwapTrend;
					strategy = rsiReversion;
				}
			}
			else
			{
				IStrategy rsiReversion = Orb;
				strategy = rsiReversion;
			}
			IStrategy strategy2 = strategy;
			int idx = ((index == -1) ? (candles.Count - 1) : index);
			if (idx < 0 || idx >= candles.Count)
			{
				return null;
			}
			StrategyResult result = strategy2.GetSignal(candles, idx);
			if (!result.IsSignal)
			{
				return null;
			}
			if (GlobalBias == MarketBias.Bearish && result.Side == OrderSide.Buy)
			{
				return null;
			}
			if (GlobalBias == MarketBias.Bullish && result.Side == OrderSide.Sell)
			{
				return null;
			}
			OrderSide side = result.Side;
			decimal entry = result.EntryPrice;
			decimal stop = result.StopLoss;
			if (entry <= 0m || stop <= 0m)
			{
				return null;
			}
			decimal stopDistance = Math.Abs(entry - stop);
			if (stopDistance <= 0m)
			{
				return null;
			}
			decimal feeRate = fees.TakerRate;
			costs.FeeRateUsed = feeRate;
			costs.EstimatedSpreadRate = 0.0005m;
			costs.TotalRoundTripRate = feeRate + fees.MakerRate + costs.EstimatedSpreadRate;
			decimal riskPerTrade = equityUsd * riskFraction;
			if (riskPerTrade <= 0m)
			{
				return null;
			}
			decimal qty = riskPerTrade / stopDistance;
			qty = Math.Round(qty, 6);
			if (qty <= 0m)
			{
				return null;
			}
			if (qty > 1000000m)
			{
				return null;
			}
			if (qty < 0.000001m)
			{
				return null;
			}
			if (productId.EndsWith("-USD", StringComparison.OrdinalIgnoreCase) && qty < 0.0001m)
			{
				return null;
			}
			return new OrderRequest
			{
				ProductId = productId,
				Side = side,
				Type = OrderType.Market,
				Quantity = qty,
				StopLoss = result.StopLoss,
				TakeProfit = result.TakeProfit,
				Tif = TimeInForce.GTC,
				Price = null,
				ClientOrderId = "cdts-" + Guid.NewGuid().ToString("N")
			};
		}
	}
}
