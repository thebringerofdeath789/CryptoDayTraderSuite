namespace CryptoDayTraderSuite.Models
{
	public static class Projections
	{
		public static ProjectionResult Compute(ProjectionInput p)
		{
			if (p.TradesPerDay <= 0 || p.Days <= 0)
			{
				return new ProjectionResult
				{
					EndingEquity = p.StartingEquity,
					DailyExpectedReturnPct = 0m,
					TotalReturnPct = 0m
				};
			}
			decimal expectancyR = p.WinRate * p.AvgWinR - (1m - p.WinRate) * p.AvgLossR;
			decimal netExpectancyR = expectancyR - p.NetFeeAndFrictionRate;
			decimal dailyGrowth = 1m + netExpectancyR * p.RiskPerTradeFraction * (decimal)p.TradesPerDay;
			if (dailyGrowth < 0.5m)
			{
				dailyGrowth = 0.5m;
			}
			if (dailyGrowth > 1.5m)
			{
				dailyGrowth = 1.5m;
			}
			decimal ending = p.StartingEquity;
			for (int d = 0; d < p.Days; d++)
			{
				ending *= dailyGrowth;
			}
			return new ProjectionResult
			{
				EndingEquity = ending,
				DailyExpectedReturnPct = (dailyGrowth - 1m) * 100m,
				TotalReturnPct = (ending / p.StartingEquity - 1m) * 100m
			};
		}
	}
}
