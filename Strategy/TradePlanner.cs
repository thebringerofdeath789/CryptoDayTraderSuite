/* File: Strategy/TradePlanner.cs */
/* Author: Gregory King */
/* Date: 2025-08-10 */
/* Description: generate trade plans from strategies, apply governance, keep planned list */
/* Types: TradePlanner, GovernanceRules */

using System;
using System.Collections.Generic;
using System.Linq;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Strategy
{
    public class GovernanceRules
    {
        public HashSet<string> DisabledStrategies = new HashSet<string>(StringComparer.OrdinalIgnoreCase); /* disable list */
        public HashSet<string> DisabledProducts = new HashSet<string>(StringComparer.OrdinalIgnoreCase); /* symbols */
        public TimeSpan? NoTradeBefore = null; /* time window */
        public TimeSpan? NoTradeAfter = null; /* time window */
        public decimal MaxRiskFractionPerTrade = 0.02m; /* 2% */
        public decimal MinEdge = 0.0005m; /* 5 bps min modeled edge */
        public bool DisableAfterLosingStreak = true; /* flag */
        public int LosingStreakThreshold = 4; /* n */
        public Dictionary<string, int> StrategyLosingStreak = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); /* state */

        public bool IsTimeBlocked(DateTime utcNow)
        {
            if (!NoTradeBefore.HasValue && !NoTradeAfter.HasValue) return false;
            var tod = utcNow.TimeOfDay;
            if (NoTradeBefore.HasValue && NoTradeAfter.HasValue)
            {
                var start = NoTradeBefore.Value;
                var end = NoTradeAfter.Value;

                if (start <= end)
                {
                    return tod < start || tod > end;
                }

                return tod < start && tod > end;
            }

            if (NoTradeBefore.HasValue && tod < NoTradeBefore.Value) return true;
            if (NoTradeAfter.HasValue && tod > NoTradeAfter.Value) return true;
            return false;
        }
    }

    public class TradePlanner
    {
        public readonly List<TradeRecord> Planned = new List<TradeRecord>(); /* list */
        public GovernanceRules Rules = new GovernanceRules(); /* rules */

        public void Clear() { Planned.Clear(); } /* clear */

        public void AddCandidate(TradeRecord t)
        {
            t.Enabled = ApplyRules(t); /* rule */
            Planned.Add(t);
        }

        public void ReapplyAll()
        {
            for (int i = 0; i < Planned.Count; i++) Planned[i].Enabled = ApplyRules(Planned[i]); /* reapply */
        }

        public IEnumerable<TradeRecord> Enabled() { return Planned.Where(p => p.Enabled); } /* enabled */

        private bool ApplyRules(TradeRecord t)
        {
            if (Rules.DisabledStrategies.Contains(t.Strategy)) return false;
            if (Rules.DisabledProducts.Contains(t.ProductId)) return false;
            if (Rules.IsTimeBlocked(DateTime.UtcNow)) return false;
            if (Math.Abs(t.EstEdge) < Rules.MinEdge) return false;
            int ls; if (Rules.StrategyLosingStreak.TryGetValue(t.Strategy, out ls) && Rules.DisableAfterLosingStreak && ls >= Rules.LosingStreakThreshold) return false;
            return true;
        }
    }
}