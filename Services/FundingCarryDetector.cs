using System;
using System.Collections.Generic;
using System.Linq;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Services
{
    public class FundingCarryDetector
    {
        public List<FundingCarryOpportunity> Detect(IList<FundingRateSnapshot> snapshots, decimal minCarryBps, decimal minBasisStabilityScore)
        {
            var result = new List<FundingCarryOpportunity>();
            if (snapshots == null || snapshots.Count < 2)
            {
                return result;
            }

            var bySymbol = snapshots
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.Symbol) && !string.IsNullOrWhiteSpace(s.Venue))
                .GroupBy(s => s.Symbol, StringComparer.OrdinalIgnoreCase);

            foreach (var symbolGroup in bySymbol)
            {
                var perSymbol = symbolGroup.ToList();
                if (perSymbol.Count < 2) continue;

                var stability = ComputeBasisStability(perSymbol);
                var highestFunding = perSymbol.OrderByDescending(s => s.FundingRateBps).First();
                var lowestFunding = perSymbol.OrderBy(s => s.FundingRateBps).First();

                var expectedCarry = highestFunding.FundingRateBps - lowestFunding.FundingRateBps;
                var opportunity = new FundingCarryOpportunity
                {
                    Symbol = symbolGroup.Key,
                    LongVenue = lowestFunding.Venue,
                    ShortVenue = highestFunding.Venue,
                    ExpectedCarryBps = Math.Round(expectedCarry, 4),
                    BasisStabilityScore = Math.Round(stability, 4),
                    ComputedAtUtc = DateTime.UtcNow
                };

                if (expectedCarry < minCarryBps)
                {
                    opportunity.IsExecutable = false;
                    opportunity.RejectReason = "fees-kill";
                    result.Add(opportunity);
                    continue;
                }

                if (stability < minBasisStabilityScore)
                {
                    opportunity.IsExecutable = false;
                    opportunity.RejectReason = "latency-risk";
                    result.Add(opportunity);
                    continue;
                }

                opportunity.IsExecutable = true;
                opportunity.RejectReason = string.Empty;
                result.Add(opportunity);
            }

            return result
                .OrderByDescending(o => o.IsExecutable)
                .ThenByDescending(o => o.ExpectedCarryBps)
                .ToList();
        }

        private static decimal ComputeBasisStability(IList<FundingRateSnapshot> snapshots)
        {
            if (snapshots == null || snapshots.Count == 0) return 0m;

            var basisValues = snapshots.Select(s => s.BasisBps).ToList();
            var mean = basisValues.Average();
            var avgDeviation = basisValues.Average(v => Math.Abs(v - mean));

            if (avgDeviation <= 0m) return 1m;

            var normalized = 1m - Math.Min(1m, avgDeviation / 50m);
            return Math.Max(0m, normalized);
        }
    }
}
