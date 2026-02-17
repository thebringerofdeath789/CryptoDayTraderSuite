using System;
using System.Collections.Generic;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Backtest
{
	public class Backtester
	{
		public class Result
		{
			public int Trades { get; set; }

			public decimal PnL { get; set; }

			public decimal MaxDrawdown { get; set; }

			public decimal WinRate { get; set; }
		}

		public static Result Run(List<Candle> candles, Func<List<Candle>, int, OrderRequest> signal, decimal feeRoundTripRate, decimal startingEquity = 1000m)
		{
			Position pos = new Position
			{
				ProductId = "",
				Qty = 0m,
				AvgPrice = 0m
			};
			decimal equity = startingEquity;
			decimal peak = equity;
			decimal wins = default(decimal);
			decimal trades = default(decimal);
			decimal maxDrawdown = default(decimal);
			decimal? stopLoss = null;
			decimal? takeProfit = null;
			for (int i = 50; i < candles.Count; i++)
			{
				OrderRequest req = signal(candles, i);
				Candle bar = candles[i];
				if (req != null && pos.Qty == 0m)
				{
					decimal price = bar.Close;
					decimal qty = req.Quantity;
					pos.Qty = ((req.Side == OrderSide.Buy) ? qty : (-qty));
					pos.AvgPrice = price;
					stopLoss = req.StopLoss;
					takeProfit = req.TakeProfit;
					++trades;
					equity -= Math.Abs(qty * price) * feeRoundTripRate / 2m;
				}
				else if (pos.Qty != 0m)
				{
					bool shouldExit = false;
					decimal exitPrice = bar.Close;
					if (pos.Qty > 0m)
					{
						if (stopLoss.HasValue && bar.Low <= stopLoss.Value)
						{
							shouldExit = true;
							exitPrice = stopLoss.Value;
						}
						else if (takeProfit.HasValue && bar.High >= takeProfit.Value)
						{
							shouldExit = true;
							exitPrice = takeProfit.Value;
						}
						else if (req != null && req.Side == OrderSide.Sell)
						{
							shouldExit = true;
							exitPrice = bar.Close;
						}
					}
					else if (stopLoss.HasValue && bar.High >= stopLoss.Value)
					{
						shouldExit = true;
						exitPrice = stopLoss.Value;
					}
					else if (takeProfit.HasValue && bar.Low <= takeProfit.Value)
					{
						shouldExit = true;
						exitPrice = takeProfit.Value;
					}
					else if (req != null && req.Side == OrderSide.Buy)
					{
						shouldExit = true;
						exitPrice = bar.Close;
					}
					if (shouldExit)
					{
						decimal pnl = (exitPrice - pos.AvgPrice) * pos.Qty;
						equity += pnl;
						equity -= Math.Abs(pos.Qty * exitPrice) * feeRoundTripRate / 2m;
						if (pnl > 0m)
						{
							++wins;
						}
						pos.Qty = 0m;
						pos.AvgPrice = 0m;
						stopLoss = null;
						takeProfit = null;
					}
				}
				if (equity > peak)
				{
					peak = equity;
				}
				if (peak > 0m)
				{
					decimal dd = (peak - equity) / peak;
					if (dd > maxDrawdown)
					{
						maxDrawdown = dd;
					}
				}
			}
			if (pos.Qty != 0m)
			{
				decimal last = candles[candles.Count - 1].Close;
				decimal pnl2 = (last - pos.AvgPrice) * pos.Qty;
				equity += pnl2;
				equity -= Math.Abs(pos.Qty * last) * feeRoundTripRate / 2m;
				if (pnl2 > 0m)
				{
					++wins;
				}
				if (equity > peak)
				{
					peak = equity;
				}
				if (peak > 0m)
				{
					decimal dd2 = (peak - equity) / peak;
					if (dd2 > maxDrawdown)
					{
						maxDrawdown = dd2;
					}
				}
			}
			return new Result
			{
				Trades = (int)trades,
				PnL = equity - startingEquity,
				WinRate = ((trades > 0m) ? (wins / trades) : 0m),
				MaxDrawdown = maxDrawdown
			};
		}
	}
}
