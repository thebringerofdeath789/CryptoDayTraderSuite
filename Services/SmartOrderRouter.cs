using System;
using System.Collections.Generic;
using System.Linq;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Services
{
    public class SmartOrderRouter
    {
        private readonly ExecutionVenueScorer _scorer;

        public SmartOrderRouter(ExecutionVenueScorer scorer)
        {
            _scorer = scorer ?? throw new ArgumentNullException(nameof(scorer));
        }

        public RoutingDecision Route(string symbol, IList<VenueQuoteSnapshot> quotes, IList<VenueHealthSnapshot> health, decimal expectedGrossEdgeBps, decimal feeBps, decimal slippageBps)
        {
            var ranked = _scorer.Score(symbol, quotes, health, expectedGrossEdgeBps, feeBps, slippageBps);

            var decision = new RoutingDecision
            {
                Symbol = symbol,
                RankedVenues = ranked,
                ComputedAtUtc = DateTime.UtcNow,
                Reason = "no-eligible-venue"
            };

            if (ranked == null || ranked.Count == 0)
            {
                return decision;
            }

            var eligible = ranked.Where(r => r.IsEligible).ToList();
            if (eligible.Count == 0)
            {
                decision.Reason = ranked[0].RejectReason;
                return decision;
            }

            var primary = eligible[0];
            var fallback = eligible.Skip(1).FirstOrDefault();

            decision.ChosenVenue = primary.Venue;
            decision.ChosenScoreBps = primary.ExpectedNetEdgeBps;
            decision.FallbackVenue = fallback != null ? fallback.Venue : string.Empty;
            decision.Reason = fallback != null ? "primary-with-fallback" : "primary-only";

            return decision;
        }
    }
}
