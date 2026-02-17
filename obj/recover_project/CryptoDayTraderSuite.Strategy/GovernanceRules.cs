using System;
using System.Collections.Generic;

namespace CryptoDayTraderSuite.Strategy
{
	public class GovernanceRules
	{
		public HashSet<string> DisabledStrategies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		public HashSet<string> DisabledProducts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		public TimeSpan? NoTradeBefore = null;

		public TimeSpan? NoTradeAfter = null;

		public decimal MaxRiskFractionPerTrade = 0.02m;

		public decimal MinEdge = 0.0005m;

		public bool DisableAfterLosingStreak = true;

		public int LosingStreakThreshold = 4;

		public Dictionary<string, int> StrategyLosingStreak = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		public bool IsTimeBlocked(DateTime utcNow)
		{
			if (!NoTradeBefore.HasValue && !NoTradeAfter.HasValue)
			{
				return false;
			}
			TimeSpan tod = utcNow.TimeOfDay;
			if (NoTradeBefore.HasValue && tod < NoTradeBefore.Value)
			{
				return true;
			}
			if (NoTradeAfter.HasValue && tod > NoTradeAfter.Value)
			{
				return true;
			}
			return false;
		}
	}
}
