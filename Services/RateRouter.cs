using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Services
{
    public class RateRouter : IRateRouter
    {
        private readonly IExchangeProvider _provider;
        private readonly MultiVenueQuoteService _multiVenueQuoteService;
        private readonly ConcurrentDictionary<string, CachedRate> _mid = new ConcurrentDictionary<string, CachedRate>(StringComparer.OrdinalIgnoreCase);
        private readonly TimeSpan _ttl = TimeSpan.FromSeconds(15); /* AUDIT-0011 Fix: Lower TTL to 15s (was 5s in audit, but 15s is reasonable cache) */
        private readonly string[] _venues = new[] { "Coinbase", "Kraken", "Bitstamp" };

        private struct CachedRate
        {
            public decimal Rate;
            public DateTime Time;
            public string SourceVenue;
            public decimal Confidence;
        }

        public RateRouter(IExchangeProvider provider, MultiVenueQuoteService multiVenueQuoteService = null)
        {
             _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _multiVenueQuoteService = multiVenueQuoteService ?? new MultiVenueQuoteService(_provider);
        }

        public async Task<decimal> MidAsync(string baseAsset, string quoteAsset)
        {
            var pid = $"{baseAsset}-{quoteAsset}";
            
            if (_mid.TryGetValue(pid, out CachedRate c))
            {
                if ((DateTime.UtcNow - c.Time) < _ttl) return c.Rate;
            }

            try 
            {
                var parts = pid.Split('-');
                var parsedBaseAsset = parts.Length > 0 ? parts[0] : string.Empty;
                var parsedQuoteAsset = parts.Length > 1 ? parts[1] : string.Empty;

                CompositeQuote composite = null;
                if (!string.IsNullOrWhiteSpace(parsedBaseAsset) && !string.IsNullOrWhiteSpace(parsedQuoteAsset))
                {
                    composite = await _multiVenueQuoteService
                        .GetCompositeQuoteAsync(parsedBaseAsset, parsedQuoteAsset, _venues)
                        .ConfigureAwait(false);
                }

                if (composite != null && composite.Mid > 0m)
                {
                    _mid[pid] = new CachedRate
                    {
                        Rate = composite.Mid,
                        Time = DateTime.UtcNow,
                        SourceVenue = composite.BestVenue,
                        Confidence = composite.Confidence
                    };

                    Log.Debug("[RateRouter] Composite rate " + pid + "=" + composite.Mid.ToString("0.########")
                        + " venue=" + (composite.BestVenue ?? "n/a")
                        + " confidence=" + composite.Confidence.ToString("0.0000")
                        + " venues=" + (composite.Venues != null ? composite.Venues.Count : 0));

                    return composite.Mid;
                }

                /* Fallback behavior keeps legacy resilience while multi-venue coverage grows. */
                var fallbackClient = _provider.CreatePublicClient("Coinbase");
                var ticker = await fallbackClient.GetTickerAsync(pid).ConfigureAwait(false);
                if (ticker != null && ticker.Last > 0)
                {
                    var rate = ticker.Last;
                    _mid[pid] = new CachedRate { Rate = rate, Time = DateTime.UtcNow, SourceVenue = "Coinbase", Confidence = 0.25m };
                    return rate;
                }
            }
            catch (Exception ex)
            {
                Log.Warn("[RateRouter] MidAsync failed for " + pid + ": " + ex.Message);
            }
            
            return 0m;
        }

        public async Task<decimal> ConvertAsync(string fromAsset, string toAsset, decimal amount)
        {
            if (string.Equals(fromAsset, toAsset, StringComparison.OrdinalIgnoreCase)) return amount;

            /* Direct */
            var d = await MidAsync(fromAsset, toAsset).ConfigureAwait(false);
            if (d > 0m) return amount * d;
            
            /* Reverse */
            var r = await MidAsync(toAsset, fromAsset).ConfigureAwait(false);
            if (r > 0m) return amount / r;

            /* Hops via Stablecoins/Majors */
            string[] hubs = new string[] { "USD", "USDC", "USDT", "BTC" };
            foreach (var hub in hubs)
            {
                if (hub == fromAsset || hub == toAsset) continue;

                /* Try FROM -> HUB */
                var rate1 = await GetRateOrInverse(fromAsset, hub).ConfigureAwait(false);
                if (rate1 == 0) continue;

                /* Try HUB -> TO */
                var rate2 = await GetRateOrInverse(hub, toAsset).ConfigureAwait(false);
                if (rate2 == 0) continue;

                return amount * rate1 * rate2;
            }
            
            return 0m;
        }

        private async Task<decimal> GetRateOrInverse(string a, string b)
        {
            var d = await MidAsync(a, b).ConfigureAwait(false);
            if (d > 0) return d;
            var r = await MidAsync(b, a).ConfigureAwait(false);
            if (r > 0) return 1m / r;
            return 0m;
        }
    }
}
