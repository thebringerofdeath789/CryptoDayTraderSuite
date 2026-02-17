using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Services
{
    public class VenueHealthService
    {
        private sealed class VenueCounters
        {
            public int TotalSamples;
            public int SuccessSamples;
            public int ErrorSamples;
            public int StaleSamples;
            public long TotalRoundTripMs;
            public int ConsecutiveErrorSamples;
            public int ConsecutiveStaleSamples;
            public int ConsecutiveLatencyBreaches;
            public DateTime? CircuitOpenedAtUtc;
            public DateTime? CircuitReenableAtUtc;
            public string CircuitReason;
            public int ConsecutiveCircuitOpens;
        }

        private sealed class DisabledVenueState
        {
            public DateTime DisabledAtUtc;
            public string Reason;
        }

        private readonly ConcurrentDictionary<string, VenueCounters> _counters = new ConcurrentDictionary<string, VenueCounters>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, DisabledVenueState> _disabledVenues = new ConcurrentDictionary<string, DisabledVenueState>(StringComparer.OrdinalIgnoreCase);
        private readonly TimeSpan _circuitOpenDuration = TimeSpan.FromSeconds(90);
        private readonly TimeSpan _maxCircuitOpenDuration = TimeSpan.FromMinutes(8);
        private readonly int _maxConsecutiveErrors = 3;
        private readonly int _maxConsecutiveStale = 4;
        private readonly int _maxConsecutiveLatencyBreaches = 4;
        private readonly int _latencyBreachMs = 1800;

        public void RecordQuote(VenueQuoteSnapshot quote, bool hadError)
        {
            if (quote == null || string.IsNullOrWhiteSpace(quote.Venue))
            {
                return;
            }

            var counters = _counters.GetOrAdd(quote.Venue, _ => new VenueCounters());
            lock (counters)
            {
                TryResetCircuitIfElapsed(quote.Venue, counters, DateTime.UtcNow);
                counters.TotalSamples++;
                if (hadError)
                {
                    counters.ErrorSamples++;
                    counters.ConsecutiveErrorSamples++;
                    counters.ConsecutiveStaleSamples = 0;
                    counters.ConsecutiveLatencyBreaches = 0;
                    TryOpenCircuit(quote.Venue, counters, DateTime.UtcNow, "api-failure-streak");
                }
                else
                {
                    counters.SuccessSamples++;
                    counters.TotalRoundTripMs += Math.Max(0, quote.RoundTripMs);
                    counters.ConsecutiveErrorSamples = 0;

                    if (quote.IsStale)
                    {
                        counters.StaleSamples++;
                        counters.ConsecutiveStaleSamples++;
                        TryOpenCircuit(quote.Venue, counters, DateTime.UtcNow, "stale-quote-streak");
                    }
                    else
                    {
                        counters.ConsecutiveStaleSamples = 0;
                    }

                    if (quote.RoundTripMs >= _latencyBreachMs)
                    {
                        counters.ConsecutiveLatencyBreaches++;
                        TryOpenCircuit(quote.Venue, counters, DateTime.UtcNow, "latency-breach-streak");
                    }
                    else
                    {
                        counters.ConsecutiveLatencyBreaches = 0;
                    }

                    if (!quote.IsStale && quote.RoundTripMs > 0 && quote.RoundTripMs < _latencyBreachMs && counters.ConsecutiveCircuitOpens > 0)
                    {
                        counters.ConsecutiveCircuitOpens--;
                    }
                }
            }
        }

        public bool IsCircuitOpen(string venue, DateTime utcNow)
        {
            if (string.IsNullOrWhiteSpace(venue)) return false;
            if (IsVenueDisabled(venue)) return true;
            VenueCounters counters;
            if (!_counters.TryGetValue(venue, out counters)) return false;

            lock (counters)
            {
                TryResetCircuitIfElapsed(venue, counters, utcNow);
                return counters.CircuitReenableAtUtc.HasValue && counters.CircuitReenableAtUtc.Value > utcNow;
            }
        }

        public bool HasAnyTradableVenue(IEnumerable<string> venues, DateTime utcNow)
        {
            if (venues == null) return true;

            foreach (var venue in venues)
            {
                if (string.IsNullOrWhiteSpace(venue)) continue;
                if (!IsCircuitOpen(venue, utcNow)) return true;
            }

            return false;
        }

        public bool IsVenueDisabled(string venue)
        {
            if (string.IsNullOrWhiteSpace(venue))
            {
                return false;
            }

            var key = venue.Trim();
            return _disabledVenues.ContainsKey(key) || GeoBlockRegistry.IsDisabled(key);
        }

        public bool TryDisableVenue(string venue, string reason)
        {
            if (string.IsNullOrWhiteSpace(venue))
            {
                return false;
            }

            var normalizedVenue = venue.Trim();
            var disabled = _disabledVenues.TryAdd(normalizedVenue, new DisabledVenueState
            {
                DisabledAtUtc = DateTime.UtcNow,
                Reason = string.IsNullOrWhiteSpace(reason) ? "disabled" : reason.Trim()
            });

            if (disabled)
            {
                Log.Warn("[VenueHealth] Venue disabled venue=" + normalizedVenue + " reason=" + (reason ?? "disabled"));
            }

            GeoBlockRegistry.TryDisable(normalizedVenue, reason);

            return disabled;
        }

        public List<VenueHealthSnapshot> BuildSnapshots()
        {
            var now = DateTime.UtcNow;
            var snapshots = new List<VenueHealthSnapshot>();

            var venues = new HashSet<string>(_counters.Keys, StringComparer.OrdinalIgnoreCase);
            foreach (var disabledVenue in _disabledVenues.Keys)
            {
                venues.Add(disabledVenue);
            }

            foreach (var venue in venues)
            {
                VenueCounters counters;
                _counters.TryGetValue(venue, out counters);
                DisabledVenueState disabledState;
                _disabledVenues.TryGetValue(venue, out disabledState);
                VenueHealthSnapshot snapshot;

                if (disabledState != null)
                {
                    snapshot = new VenueHealthSnapshot
                    {
                        Venue = venue,
                        ComputedAtUtc = now,
                        TotalSamples = counters != null ? counters.TotalSamples : 0,
                        SuccessSamples = counters != null ? counters.SuccessSamples : 0,
                        ErrorSamples = counters != null ? counters.ErrorSamples : 0,
                        StaleSamples = counters != null ? counters.StaleSamples : 0,
                        SuccessRatio = 0d,
                        StaleRatio = 1d,
                        AvgRoundTripMs = 0d,
                        HealthScore = 0d,
                        IsDegraded = true,
                        CircuitBreakerOpen = true,
                        CircuitBreakerReason = disabledState.Reason,
                        CircuitBreakerOpenedAtUtc = disabledState.DisabledAtUtc,
                        CircuitBreakerReenableAtUtc = null
                    };

                    snapshots.Add(snapshot);
                    continue;
                }

                if (counters == null)
                {
                    continue;
                }

                lock (counters)
                {
                    TryResetCircuitIfElapsed(venue, counters, now);
                    var total = Math.Max(1, counters.TotalSamples);
                    var successRatio = (double)counters.SuccessSamples / total;
                    var staleRatio = (double)counters.StaleSamples / total;
                    var avgRtt = counters.SuccessSamples > 0
                        ? (double)counters.TotalRoundTripMs / counters.SuccessSamples
                        : 0d;

                    var health = Math.Max(0d, Math.Min(1d,
                        (successRatio * 0.65d)
                        + ((1d - Math.Min(1d, staleRatio)) * 0.25d)
                        + ((1d - Math.Min(1d, avgRtt / 2000d)) * 0.10d)));

                    snapshot = new VenueHealthSnapshot
                    {
                        Venue = venue,
                        ComputedAtUtc = now,
                        TotalSamples = counters.TotalSamples,
                        SuccessSamples = counters.SuccessSamples,
                        ErrorSamples = counters.ErrorSamples,
                        StaleSamples = counters.StaleSamples,
                        SuccessRatio = Math.Round(successRatio, 4),
                        StaleRatio = Math.Round(staleRatio, 4),
                        AvgRoundTripMs = Math.Round(avgRtt, 2),
                        HealthScore = Math.Round(health, 4),
                        IsDegraded = health < 0.55d,
                        CircuitBreakerOpen = counters.CircuitReenableAtUtc.HasValue && counters.CircuitReenableAtUtc.Value > now,
                        CircuitBreakerReason = counters.CircuitReason,
                        CircuitBreakerOpenedAtUtc = counters.CircuitOpenedAtUtc,
                        CircuitBreakerReenableAtUtc = counters.CircuitReenableAtUtc
                    };
                }

                snapshots.Add(snapshot);
            }

            return snapshots.OrderByDescending(s => s.HealthScore).ThenBy(s => s.Venue).ToList();
        }

        private void TryOpenCircuit(string venue, VenueCounters counters, DateTime utcNow, string reason)
        {
            if (counters == null) return;

            if (counters.ConsecutiveErrorSamples >= _maxConsecutiveErrors
                || counters.ConsecutiveStaleSamples >= _maxConsecutiveStale
                || counters.ConsecutiveLatencyBreaches >= _maxConsecutiveLatencyBreaches)
            {
                var wasOpen = counters.CircuitReenableAtUtc.HasValue && counters.CircuitReenableAtUtc.Value > utcNow;
                if (!wasOpen)
                {
                    counters.ConsecutiveCircuitOpens = Math.Min(counters.ConsecutiveCircuitOpens + 1, 5);
                }

                var backoffMultiplier = (int)Math.Pow(2d, Math.Max(0, counters.ConsecutiveCircuitOpens - 1));
                var dynamicOpenDuration = TimeSpan.FromTicks(Math.Min(_maxCircuitOpenDuration.Ticks, _circuitOpenDuration.Ticks * backoffMultiplier));
                counters.CircuitOpenedAtUtc = utcNow;
                counters.CircuitReenableAtUtc = utcNow.Add(dynamicOpenDuration);
                counters.CircuitReason = reason;

                if (!wasOpen)
                {
                    Log.Warn("[VenueHealth] Circuit opened venue=" + (venue ?? "n/a")
                        + " reason=" + reason
                        + " backoffSeconds=" + ((int)dynamicOpenDuration.TotalSeconds).ToString()
                        + " openCount=" + counters.ConsecutiveCircuitOpens.ToString()
                        + " reenableUtc=" + counters.CircuitReenableAtUtc.Value.ToString("o"));
                }
            }
        }

        private void TryResetCircuitIfElapsed(string venue, VenueCounters counters, DateTime utcNow)
        {
            if (counters == null) return;
            if (!counters.CircuitReenableAtUtc.HasValue) return;
            if (counters.CircuitReenableAtUtc.Value > utcNow) return;

            var priorReason = counters.CircuitReason;

            counters.CircuitOpenedAtUtc = null;
            counters.CircuitReenableAtUtc = null;
            counters.CircuitReason = null;
            counters.ConsecutiveErrorSamples = 0;
            counters.ConsecutiveStaleSamples = 0;
            counters.ConsecutiveLatencyBreaches = 0;

            Log.Info("[VenueHealth] Circuit re-enabled venue=" + (venue ?? "n/a")
                + " priorReason=" + (priorReason ?? "n/a")
                + " consecutiveOpens=" + counters.ConsecutiveCircuitOpens.ToString());
        }

    }
}
