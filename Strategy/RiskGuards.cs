namespace CryptoDayTraderSuite.Strategy
{
    public static class RiskGuards
    {
        public struct ExpectancyBreakdown
        {
            public decimal GrossEdgeR;
            public decimal FeeDragR;
            public decimal SlippageBudgetR;
            public decimal NetEdgeR;
        }

        /* AUDIT-0020 Fix: Corrected Fee calculation logic. Previous version conflated tickValue with Notional. */
        public static bool FeesKillEdge(decimal grossProfit, decimal entryNotional, decimal feeRate)
        {
            if (grossProfit <= 0m) return true;
            /* fees = Entry + Exit. Approx 2 * feeRate * Notional */
            var fees = entryNotional * feeRate * 2m; 
            var ratio = fees / grossProfit;
            /* If fees eat more than 25% of the edge, it's a bad trade */
            return ratio >= 0.25m;
        }

        [System.Obsolete("Use FeesKillEdge(grossProfit, entryNotional, feeRate).")]
        /* Legacy Overload for compatibility */
        public static bool FeesKillEdge(decimal targetTicks, decimal tickValue, decimal qty, decimal makerFee, decimal takerFee, bool takerEntry)
        {
            if (targetTicks <= 0m || tickValue <= 0m || qty <= 0m) return true;

            var grossProfit = targetTicks * tickValue * qty;
            var entryNotional = tickValue * qty;
            var feeRate = takerEntry ? (takerFee + makerFee) / 2m : (makerFee + takerFee) / 2m;
            if (feeRate < 0m) feeRate = 0m;

            return FeesKillEdge(grossProfit, entryNotional, feeRate);
        }

        public static bool SpreadTooWide(decimal spread, decimal atr, decimal maxSpreadToAtr)
        {
            if (atr <= 0m) return false;
            var r = spread / atr;
            return r > maxSpreadToAtr;
        }

        public static bool VolatilityShock(decimal atrNow, decimal atrMedian, decimal spikeFactor)
        {
            if (atrMedian <= 0m) return false;
            return atrNow >= spikeFactor * atrMedian;
        }

        public static ExpectancyBreakdown ComputeExpectancyBreakdown(decimal winRate01, decimal avgWinR, decimal avgLossR, decimal feeDragR, decimal slippageBudgetR)
        {
            var w = winRate01;
            if (w < 0m) w = 0m;
            if (w > 1m) w = 1m;

            var win = avgWinR < 0m ? 0m : avgWinR;
            var loss = avgLossR < 0m ? -avgLossR : avgLossR;
            var fees = feeDragR < 0m ? 0m : feeDragR;
            var slip = slippageBudgetR < 0m ? 0m : slippageBudgetR;

            var gross = (w * win) - ((1m - w) * loss);
            var net = gross - fees - slip;

            return new ExpectancyBreakdown
            {
                GrossEdgeR = gross,
                FeeDragR = fees,
                SlippageBudgetR = slip,
                NetEdgeR = net
            };
        }

        public static bool NetEdgeIsViable(ExpectancyBreakdown breakdown, decimal minNetEdgeR)
        {
            return breakdown.NetEdgeR > minNetEdgeR;
        }
    }
}