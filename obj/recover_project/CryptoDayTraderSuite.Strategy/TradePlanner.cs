using System;
using System.Collections.Generic;
using System.Linq;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Strategy
{
	public class TradePlanner
	{
		public readonly List<TradeRecord> Planned = new List<TradeRecord>();

		public GovernanceRules Rules = new GovernanceRules();

		public void Clear()
		{
			Planned.Clear();
		}

		public void AddCandidate(TradeRecord t)
		{
			t.Enabled = ApplyRules(t);
			Planned.Add(t);
		}

		public void ReapplyAll()
		{
			for (int i = 0; i < Planned.Count; i++)
			{
				Planned[i].Enabled = ApplyRules(Planned[i]);
			}
		}

		public IEnumerable<TradeRecord> Enabled()
		{
			return Planned.Where((TradeRecord p) => p.Enabled);
		}

		private bool ApplyRules(TradeRecord t)
		{
			if (Rules.DisabledStrategies.Contains(t.Strategy))
			{
				return false;
			}
			if (Rules.DisabledProducts.Contains(t.ProductId))
			{
				return false;
			}
			if (Rules.IsTimeBlocked(DateTime.UtcNow))
			{
				return false;
			}
			if (Math.Abs(t.EstEdge) < Rules.MinEdge)
			{
				return false;
			}
			if (Rules.StrategyLosingStreak.TryGetValue(t.Strategy, out var ls) && Rules.DisableAfterLosingStreak && ls >= Rules.LosingStreakThreshold)
			{
				return false;
			}
			return true;
		}
	}
}
