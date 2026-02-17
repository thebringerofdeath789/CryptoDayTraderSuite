using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Services
{
    public class MultiVenueQuoteService
    {
        private readonly IExchangeProvider _provider;
        private readonly VenueHealthService _venueHealthService;
        private readonly TimeSpan _staleThreshold;

        public MultiVenueQuoteService(IExchangeProvider provider, VenueHealthService venueHealthService = null, TimeSpan? staleThreshold = null)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _venueHealthService = venueHealthService;
            _staleThreshold = staleThreshold ?? TimeSpan.FromSeconds(20);
        }

        public async Task<CompositeQuote> GetCompositeQuoteAsync(string baseAsset, string quoteAsset, IEnumerable<string> venues)
        {
            if (string.IsNullOrWhiteSpace(baseAsset) || string.IsNullOrWhiteSpace(quoteAsset))
            {
                throw new ArgumentException("Base/quote asset is required.");
            }

            var normalizedBase = baseAsset.Trim().ToUpperInvariant();
            var normalizedQuote = quoteAsset.Trim().ToUpperInvariant();
            var symbol = normalizedBase + "-" + normalizedQuote;

            var venueList = (venues ?? Enumerable.Empty<string>())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (venueList.Count == 0)
            {
                venueList.Add("Coinbase");
            }

            if (_venueHealthService != null)
            {
                venueList = venueList
                    .Where(v => !_venueHealthService.IsVenueDisabled(v))
                    .ToList();
            }

            venueList = venueList
                .Where(v => !GeoBlockRegistry.IsDisabled(v))
                .ToList();

            if (venueList.Count == 0)
            {
                return new CompositeQuote
                {
                    Symbol = symbol,
                    Mid = 0m,
                    ComputedAtUtc = DateTime.UtcNow,
                    BestVenue = string.Empty,
                    IsStale = true,
                    Confidence = 0m,
                    Venues = new List<VenueQuoteSnapshot>()
                };
            }

            var tasks = venueList.Select(v => FetchVenueSnapshotSafeAsync(v, normalizedBase, normalizedQuote)).ToArray();
            var snapshots = await Task.WhenAll(tasks).ConfigureAwait(false);
            var valid = snapshots.Where(s => s != null && s.Mid > 0m).ToList();

            if (valid.Count == 0)
            {
                return new CompositeQuote
                {
                    Symbol = symbol,
                    Mid = 0m,
                    ComputedAtUtc = DateTime.UtcNow,
                    BestVenue = string.Empty,
                    IsStale = true,
                    Confidence = 0m,
                    Venues = snapshots.Where(s => s != null).ToList()
                };
            }

            var best = valid
                .Where(s => !s.IsStale)
                .OrderBy(s => s.RoundTripMs)
                .ThenByDescending(s => s.QuoteTimeUtc)
                .FirstOrDefault() ?? valid.OrderBy(s => s.RoundTripMs).First();

            var midpoint = valid.Average(s => s.Mid);
            var staleCount = valid.Count(s => s.IsStale);
            var staleRatio = valid.Count > 0 ? (decimal)staleCount / valid.Count : 1m;
            var confidence = Math.Max(0m, 1m - staleRatio);
            confidence = Math.Min(1m, confidence * Math.Min(1m, (decimal)valid.Count / 3m));

            return new CompositeQuote
            {
                Symbol = symbol,
                Mid = midpoint,
                ComputedAtUtc = DateTime.UtcNow,
                BestVenue = best.Venue,
                IsStale = staleCount == valid.Count,
                Confidence = Math.Round(confidence, 4),
                Venues = valid
            };
        }

        private async Task<VenueQuoteSnapshot> FetchVenueSnapshotSafeAsync(string venue, string baseAsset, string quoteAsset)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var client = _provider.CreatePublicClient(venue);
                if (client == null)
                {
                    return null;
                }

                var productCandidates = GetProductCandidates(baseAsset, quoteAsset, venue);
                foreach (var productId in productCandidates)
                {
                    try
                    {
                        var ticker = await client.GetTickerAsync(productId).ConfigureAwait(false);
                        if (ticker == null)
                        {
                            continue;
                        }

                        var bid = ticker.Bid;
                        var ask = ticker.Ask;
                        var last = ticker.Last;
                        var mid = 0m;

                        if (bid > 0m && ask > 0m)
                        {
                            mid = (bid + ask) / 2m;
                        }
                        else if (last > 0m)
                        {
                            mid = last;
                        }

                        if (mid <= 0m)
                        {
                            continue;
                        }

                        var quoteTime = ticker.Time == default(DateTime) ? DateTime.UtcNow : ticker.Time.ToUniversalTime();
                        var received = DateTime.UtcNow;
                        var snapshot = new VenueQuoteSnapshot
                        {
                            Venue = venue,
                            Symbol = productId,
                            Bid = bid,
                            Ask = ask,
                            Mid = mid,
                            QuoteTimeUtc = quoteTime,
                            ReceivedTimeUtc = received,
                            RoundTripMs = (int)sw.ElapsedMilliseconds,
                            IsStale = (received - quoteTime) > _staleThreshold,
                            Source = client.Name
                        };

                        if (_venueHealthService != null)
                        {
                            _venueHealthService.RecordQuote(snapshot, false);
                        }

                        return snapshot;
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("[MultiVenueQuoteService] Ticker failed for " + venue + " " + productId + ": " + ex.Message);
                        TryAutoDisableVenueOnGeoRestriction(venue, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn("[MultiVenueQuoteService] Venue fetch failed for " + venue + ": " + ex.Message);
                TryAutoDisableVenueOnGeoRestriction(venue, ex);
            }
            finally
            {
                sw.Stop();
            }

            if (_venueHealthService != null)
            {
                _venueHealthService.RecordQuote(new VenueQuoteSnapshot
                {
                    Venue = venue,
                    Symbol = baseAsset + "-" + quoteAsset,
                    Bid = 0m,
                    Ask = 0m,
                    Mid = 0m,
                    QuoteTimeUtc = DateTime.UtcNow,
                    ReceivedTimeUtc = DateTime.UtcNow,
                    RoundTripMs = (int)Math.Max(0, sw.ElapsedMilliseconds),
                    IsStale = true,
                    Source = venue
                }, true);
            }

            return null;
        }

        public List<VenueHealthSnapshot> GetVenueHealthSnapshots()
        {
            if (_venueHealthService == null)
            {
                return new List<VenueHealthSnapshot>();
            }

            return _venueHealthService.BuildSnapshots();
        }

        private List<string> GetProductCandidates(string baseAsset, string quoteAsset, string venue)
        {
            var candidates = new List<string>
            {
                baseAsset + "-" + quoteAsset,
                baseAsset + "/" + quoteAsset,
                baseAsset + quoteAsset
            };

            if (string.Equals(baseAsset, "BTC", StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add("XBT-" + quoteAsset);
                candidates.Add("XBT/" + quoteAsset);
                candidates.Add("XBT" + quoteAsset);
            }

            if (string.Equals(venue, "Kraken", StringComparison.OrdinalIgnoreCase))
            {
                candidates.Insert(0, baseAsset + "/" + quoteAsset);
                if (string.Equals(baseAsset, "BTC", StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Insert(0, "XBT/USD");
                }
            }

            if (string.Equals(venue, "Bitstamp", StringComparison.OrdinalIgnoreCase))
            {
                candidates.Insert(0, baseAsset + quoteAsset);
            }

            return candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private void TryAutoDisableVenueOnGeoRestriction(string venue, Exception ex)
        {
            if (ex == null || string.IsNullOrWhiteSpace(venue))
            {
                return;
            }

            if (!GeoBlockRegistry.TryDisableFromException(venue, ex, "quote-fetch"))
            {
                return;
            }

            var reason = GeoBlockRegistry.GetDisableReason(venue);
            if (_venueHealthService != null)
            {
                _venueHealthService.TryDisableVenue(venue, string.IsNullOrWhiteSpace(reason) ? "geo-restricted" : reason);
            }
        }
    }
}
