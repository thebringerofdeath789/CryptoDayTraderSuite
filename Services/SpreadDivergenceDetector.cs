using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Services
{
    public class SpreadDivergenceDetector
    {
        private readonly MultiVenueQuoteService _quoteService;
        private readonly decimal _defaultFeeBps;
        private readonly decimal _defaultSlippageBps;

        public SpreadDivergenceDetector(MultiVenueQuoteService quoteService, decimal defaultFeeBps = 8m, decimal defaultSlippageBps = 5m)
        {
            _quoteService = quoteService ?? throw new ArgumentNullException(nameof(quoteService));
            _defaultFeeBps = Math.Max(0m, defaultFeeBps);
            _defaultSlippageBps = Math.Max(0m, defaultSlippageBps);
        }

        public async Task<List<SpreadDivergenceOpportunity>> DetectAsync(string baseAsset, string quoteAsset, IEnumerable<string> venues, decimal minNetEdgeBps)
        {
            var composite = await _quoteService.GetCompositeQuoteAsync(baseAsset, quoteAsset, venues).ConfigureAwait(false);
            var result = new List<SpreadDivergenceOpportunity>();
            var snapshots = composite != null && composite.Venues != null
                ? composite.Venues.Where(v => v != null).ToList()
                : new List<VenueQuoteSnapshot>();

            if (snapshots.Count < 2)
            {
                result.Add(new SpreadDivergenceOpportunity
                {
                    Symbol = baseAsset + "-" + quoteAsset,
                    RejectReason = "insufficient-depth",
                    IsExecutable = false,
                    ComputedAtUtc = DateTime.UtcNow
                });
                return result;
            }

            for (var i = 0; i < snapshots.Count; i++)
            {
                for (var j = 0; j < snapshots.Count; j++)
                {
                    if (i == j) continue;

                    var buyVenue = snapshots[i];
                    var sellVenue = snapshots[j];
                    var opportunity = BuildOpportunity(baseAsset, quoteAsset, buyVenue, sellVenue, minNetEdgeBps);
                    result.Add(opportunity);
                }
            }

            return result
                .OrderByDescending(o => o.IsExecutable)
                .ThenByDescending(o => o.NetEdgeBps)
                .ToList();
        }

        private SpreadDivergenceOpportunity BuildOpportunity(string baseAsset, string quoteAsset, VenueQuoteSnapshot buyVenue, VenueQuoteSnapshot sellVenue, decimal minNetEdgeBps)
        {
            var symbol = baseAsset + "-" + quoteAsset;
            var buy = buyVenue.Ask > 0m ? buyVenue.Ask : buyVenue.Mid;
            var sell = sellVenue.Bid > 0m ? sellVenue.Bid : sellVenue.Mid;

            var opportunity = new SpreadDivergenceOpportunity
            {
                Symbol = symbol,
                BuyVenue = buyVenue.Venue,
                SellVenue = sellVenue.Venue,
                BuyPrice = buy,
                SellPrice = sell,
                EstimatedFeeBps = _defaultFeeBps,
                EstimatedSlippageBps = _defaultSlippageBps,
                ComputedAtUtc = DateTime.UtcNow
            };

            if (buy <= 0m || sell <= 0m)
            {
                opportunity.RejectReason = "insufficient-depth";
                opportunity.IsExecutable = false;
                return opportunity;
            }

            if (buyVenue.IsStale || sellVenue.IsStale)
            {
                opportunity.RejectReason = "stale-quote";
                opportunity.IsExecutable = false;
                return opportunity;
            }

            if (buyVenue.RoundTripMs > 2000 || sellVenue.RoundTripMs > 2000)
            {
                opportunity.RejectReason = "latency-risk";
                opportunity.IsExecutable = false;
                return opportunity;
            }

            var grossSpreadBps = ((sell - buy) / buy) * 10000m;
            var netEdgeBps = grossSpreadBps - _defaultFeeBps - _defaultSlippageBps;

            opportunity.GrossSpreadBps = Math.Round(grossSpreadBps, 4);
            opportunity.NetEdgeBps = Math.Round(netEdgeBps, 4);

            if (grossSpreadBps <= 0m)
            {
                opportunity.RejectReason = "insufficient-depth";
                opportunity.IsExecutable = false;
                return opportunity;
            }

            if (netEdgeBps <= 0m)
            {
                opportunity.RejectReason = "fees-kill";
                opportunity.IsExecutable = false;
                return opportunity;
            }

            if (netEdgeBps < minNetEdgeBps)
            {
                opportunity.RejectReason = "slippage-kill";
                opportunity.IsExecutable = false;
                return opportunity;
            }

            opportunity.RejectReason = string.Empty;
            opportunity.IsExecutable = true;
            return opportunity;
        }
    }
}
