using System;
using System.Collections.Generic;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Backtest
{
    public class Backtester
    {
        public class Result
        {
            public int Trades { get; set; } /* trades */
            public decimal PnL { get; set; } /* pnl */
            public decimal MaxDrawdown { get; set; } /* mdd */
            public decimal WinRate { get; set; } /* win */
        }

        public static Result Run(List<Candle> candles, Func<List<Candle>, int, OrderRequest> signal, decimal feeRoundTripRate, decimal startingEquity = 1000m)
        {
            var pos = new Position { ProductId = "", Qty = 0m, AvgPrice = 0m };
            decimal equity = startingEquity, peak = equity, wins = 0m, trades = 0m, maxDrawdown = 0m;
            decimal? stopLoss = null;
            decimal? takeProfit = null;

            for (int i = 50; i < candles.Count; i++)
            {
                var req = signal(candles, i);
                var bar = candles[i];

                if (req != null && pos.Qty == 0m)
                {
                    var price = bar.Close;
                    var qty = req.Quantity;
                    pos.Qty = req.Side == OrderSide.Buy ? qty : -qty;
                    pos.AvgPrice = price;
                    stopLoss = req.StopLoss;
                    takeProfit = req.TakeProfit;
                    trades++;
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
                    else
                    {
                        if (stopLoss.HasValue && bar.High >= stopLoss.Value)
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
                    }

                    if (shouldExit)
                    {
                        var pnl = (exitPrice - pos.AvgPrice) * pos.Qty;
                        equity += pnl;
                        equity -= Math.Abs(pos.Qty * exitPrice) * feeRoundTripRate / 2m;
                        if (pnl > 0m) wins++;
                        pos.Qty = 0m;
                        pos.AvgPrice = 0m;
                        stopLoss = null;
                        takeProfit = null;
                    }
                }

                if (equity > peak) peak = equity;
                if (peak > 0m)
                {
                    var dd = (peak - equity) / peak;
                    if (dd > maxDrawdown) maxDrawdown = dd;
                }
            }

            if (pos.Qty != 0m)
            {
                var last = candles[candles.Count - 1].Close;
                var pnl = (last - pos.AvgPrice) * pos.Qty;
                equity += pnl;
                equity -= Math.Abs(pos.Qty * last) * feeRoundTripRate / 2m;
                if (pnl > 0m) wins++;
                if (equity > peak) peak = equity;
                if (peak > 0m)
                {
                    var dd = (peak - equity) / peak;
                    if (dd > maxDrawdown) maxDrawdown = dd;
                }
            }

            return new Result
            {
                Trades = (int)trades,
                PnL = equity - startingEquity,
                WinRate = trades > 0m ? wins / trades : 0m,
                MaxDrawdown = maxDrawdown
            };
        }
    }
}
