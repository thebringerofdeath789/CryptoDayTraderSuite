using System;
using System.Collections.Generic;
using System.Linq;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Services
{
    public class ExecutionVenueScorer
    {
        public List<VenueExecutionScore> Score(string symbol, IList<VenueQuoteSnapshot> quotes, IList<VenueHealthSnapshot> health, decimal expectedGrossEdgeBps, decimal feeBps, decimal slippageBps)
        {
            var results = new List<VenueExecutionScore>();
            if (quotes == null || quotes.Count == 0)
            {
                return results;
            }

            var healthByVenue = (health ?? new List<VenueHealthSnapshot>())
                .Where(h => h != null && !string.IsNullOrWhiteSpace(h.Venue))
                .GroupBy(h => h.Venue, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var quote in quotes.Where(q => q != null))
            {
                var healthScore = 0.5d;
                VenueHealthSnapshot healthSnapshot;
                if (healthByVenue.TryGetValue(quote.Venue ?? string.Empty, out healthSnapshot))
                {
                    healthScore = healthSnapshot.HealthScore;
                }

                var latencyPenaltyBps = (decimal)Math.Min(30d, Math.Max(0d, quote.RoundTripMs / 100d));
                var healthPenaltyBps = (decimal)Math.Max(0d, (1d - healthScore) * 25d);
                var net = expectedGrossEdgeBps - feeBps - slippageBps - latencyPenaltyBps - healthPenaltyBps;

                var score = new VenueExecutionScore
                {
                    Venue = quote.Venue,
                    Symbol = symbol,
                    ExpectedNetEdgeBps = Math.Round(net, 4),
                    FeeDragBps = feeBps,
                    SlippageBudgetBps = slippageBps,
                    HealthScore = Math.Round(healthScore, 4),
                    LatencyMs = Math.Max(0, quote.RoundTripMs),
                    IsEligible = false,
                    RejectReason = string.Empty
                };

                if (quote.IsStale)
                {
                    score.IsEligible = false;
                    score.RejectReason = "stale-quote";
                }
                else if (quote.RoundTripMs > 2000)
                {
                    score.IsEligible = false;
                    score.RejectReason = "latency-risk";
                }
                else if (net <= 0m)
                {
                    score.IsEligible = false;
                    score.RejectReason = "fees-kill";
                }
                else
                {
                    score.IsEligible = true;
                }

                results.Add(score);
            }

            return results
                .OrderByDescending(r => r.IsEligible)
                .ThenByDescending(r => r.ExpectedNetEdgeBps)
                .ThenByDescending(r => r.HealthScore)
                .ThenBy(r => r.LatencyMs)
                .ToList();
        }
    }
}
