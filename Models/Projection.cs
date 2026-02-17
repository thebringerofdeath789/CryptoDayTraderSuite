
using System;

namespace CryptoDayTraderSuite.Models
{
    public class ProjectionInput
    {
        public decimal StartingEquity { get; set; } /* start */
        public int TradesPerDay { get; set; } /* count */
        public decimal WinRate { get; set; } /* 0..1 */
        public decimal AvgWinR { get; set; } /* in R */
        public decimal AvgLossR { get; set; } /* in R positive number */
        public decimal RiskPerTradeFraction { get; set; } /* 0..1 */
        public decimal NetFeeAndFrictionRate { get; set; } /* 0..1 roundtrip */
        public int Days { get; set; } /* days */
    }

    public class ProjectionResult
    {
        public decimal EndingEquity { get; set; } /* end */
        public decimal DailyExpectedReturnPct { get; set; } /* daily */
        public decimal TotalReturnPct { get; set; } /* total */
    }

    public static class Projections
    {
        /* compute deterministic projection from basic expectancy */
        public static ProjectionResult Compute(ProjectionInput p)
        {
            /* guard sane values */
            if (p.TradesPerDay <= 0 || p.Days <= 0) return new ProjectionResult { EndingEquity = p.StartingEquity, DailyExpectedReturnPct = 0m, TotalReturnPct = 0m }; /* no trades */
            var expectancyR = p.WinRate * p.AvgWinR - (1m - p.WinRate) * p.AvgLossR; /* expectancy in R */
            var netExpectancyR = expectancyR - p.NetFeeAndFrictionRate; /* subtract roundtrip friction as R approx */
            var dailyGrowth = 1m + (netExpectancyR * p.RiskPerTradeFraction * p.TradesPerDay); /* compounding per day */
            if (dailyGrowth < 0.5m) dailyGrowth = 0.5m; /* clamp */
            if (dailyGrowth > 1.5m) dailyGrowth = 1.5m; /* clamp */
            var ending = p.StartingEquity; /* init */
            for (int d = 0; d < p.Days; d++) ending *= dailyGrowth; /* compound */
            return new ProjectionResult
            {
                EndingEquity = ending, /* end */
                DailyExpectedReturnPct = (dailyGrowth - 1m) * 100m, /* daily % */
                TotalReturnPct = (ending / p.StartingEquity - 1m) * 100m /* total % */
            };
        }
    }
}
