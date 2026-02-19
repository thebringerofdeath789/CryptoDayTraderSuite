
using System;
using System.Collections.Generic;
using System.Globalization;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Strategy
{
    public class StrategyEngine
    {
        public StrategyKind Active { get; set; } /* active strategy */
        public ORBStrategy Orb { get; private set; } = new ORBStrategy(); /* orb */
        public VWAPTrendStrategy VwapTrend { get; private set; } = new VWAPTrendStrategy(); /* vwap */
        public RSIReversionStrategy RsiReversion { get; private set; } = new RSIReversionStrategy(); /* rsi */
        public DonchianStrategy Donchian { get; private set; } = new DonchianStrategy(); /* donchian */
        
        /* 
           AI Governor: Blocks trades that contradict the global market bias. 
           Updated by background service.
        */
        public MarketBias GlobalBias { get; set; } = MarketBias.Neutral;

        public void SetStrategy(string name)
        {
            if (string.IsNullOrEmpty(name)) return;

            if (name.Equals("ORB", StringComparison.OrdinalIgnoreCase) || name.Equals("ORB Strategy", StringComparison.OrdinalIgnoreCase))
                Active = StrategyKind.ORB;
            else if (name.Equals("VWAPTrend", StringComparison.OrdinalIgnoreCase) || name.Equals("VWAP Trend", StringComparison.OrdinalIgnoreCase))
                Active = StrategyKind.VWAPTrend;
            else if (name.Equals("RSIReversion", StringComparison.OrdinalIgnoreCase) || name.Equals("RSI Reversion", StringComparison.OrdinalIgnoreCase))
                Active = StrategyKind.RSIReversion;
            else if (name.Equals("Donchian", StringComparison.OrdinalIgnoreCase) || name.Equals("Donchian 20", StringComparison.OrdinalIgnoreCase) || name.Equals("DonchianStrategy", StringComparison.OrdinalIgnoreCase))
                Active = StrategyKind.Donchian;
        }

        public OrderRequest Evaluate(string productId, List<Candle> candles, FeeSchedule fees, decimal equityUsd, decimal riskFraction, decimal price, out CostBreakdown costs, int index = -1)
        {
            if (string.IsNullOrWhiteSpace(productId) || candles == null || candles.Count == 0 || fees == null)
            {
                costs = new CostBreakdown();
                return null;
            }

            /* choose strategy */
            costs = new CostBreakdown(); /* init */
            IStrategy strategy = Active == StrategyKind.ORB
                ? (IStrategy)Orb
                : (Active == StrategyKind.VWAPTrend
                    ? (IStrategy)VwapTrend
                    : (Active == StrategyKind.Donchian ? (IStrategy)Donchian : RsiReversion));
            
            int idx = index == -1 ? candles.Count - 1 : index;
            if (idx < 0 || idx >= candles.Count) return null;

            StrategyResult result;
            try
            {
                result = strategy.GetSignal(candles, idx);
            }
            catch (Exception ex)
            {
                Log.Warn("[StrategyEngine] Strategy signal generation failed; returning no-signal for " + productId
                    + " strategy=" + Active + " index=" + idx.ToString(CultureInfo.InvariantCulture)
                    + " error=" + ex.Message);
                return null;
            }
            if (!result.IsSignal) return null; /* no trade */

            /* Governor Check */
            if (GlobalBias == MarketBias.Bearish && result.Side == OrderSide.Buy) return null;
            if (GlobalBias == MarketBias.Bullish && result.Side == OrderSide.Sell) return null;

            var side = result.Side;
            var entry = result.EntryPrice;
            var stop = result.StopLoss;
            if (entry <= 0m || stop <= 0m) return null;
            var stopDistance = Math.Abs(entry - stop);
            if (stopDistance <= 0m) return null;
            if (stopDistance < 0.00000001m)
            {
                Log.Warn("[StrategyEngine] Signal skipped due to tiny stop distance for " + productId
                    + " strategy=" + Active
                    + " entry=" + entry.ToString(CultureInfo.InvariantCulture)
                    + " stop=" + stop.ToString(CultureInfo.InvariantCulture)
                    + " distance=" + stopDistance.ToString(CultureInfo.InvariantCulture));
                return null;
            }
            
            /* compute costs */
            var feeRate = fees.TakerRate; /* assume taker for entry */
            costs.FeeRateUsed = feeRate; /* set */
            /* estimate spread from bid ask will be filled in UI layer, default 0.0005 = 5 bps */
            costs.EstimatedSpreadRate = 0.0005m; /* spread */
            costs.TotalRoundTripRate = feeRate + fees.MakerRate + costs.EstimatedSpreadRate; /* approx */

            /* risk sizing */
            decimal riskPerTrade;
            try
            {
                riskPerTrade = equityUsd * riskFraction; /* $ risk */
            }
            catch (OverflowException)
            {
                return null;
            }
            if (riskPerTrade <= 0m) return null;
            
            decimal qty;
            try
            {
                qty = riskPerTrade / stopDistance; /* qty */
            }
            catch (OverflowException)
            {
                return null;
            }
            qty = Math.Round(qty, 6); if (qty <= 0m) return null; /* round */
            if (qty > 1000000m) return null;
            if (qty < 0.000001m) return null;

            if (productId.EndsWith("-USD", StringComparison.OrdinalIgnoreCase) && qty < 0.0001m) return null;

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
            }; /* order */
        }
    }
}
