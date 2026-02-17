using System;
using System.Collections.Generic;
using System.Linq;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Services
{
    public sealed class StrategyExchangePolicyDecision
    {
        public bool IsAllowed { get; set; }
        public string RejectCode { get; set; }
        public string RejectReason { get; set; }
        public string PolicyId { get; set; }
        public string PolicyRationale { get; set; }
    }

    public class StrategyExchangePolicyService
    {
        private readonly Dictionary<string, HashSet<string>> _strategyVenueAllowList;

        public StrategyExchangePolicyService()
        {
            _strategyVenueAllowList = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "vwaptrend", NewVenueSet("Coinbase", "Kraken", "Bitstamp", "Binance", "Bybit", "OKX") },
                { "orb", NewVenueSet("Coinbase", "Kraken", "Bitstamp", "Binance", "Bybit", "OKX") },
                { "rsireversion", NewVenueSet("Coinbase", "Kraken", "Bitstamp", "Binance", "Bybit", "OKX") },
                { "donchian", NewVenueSet("Coinbase", "Kraken", "Bitstamp", "Binance", "Bybit", "OKX") },
                { "fundingcarry", NewVenueSet("Binance", "Bybit", "OKX") },
                { "crossexchangespreaddivergence", NewVenueSet("Coinbase", "Kraken", "Bitstamp", "Binance", "Bybit", "OKX") }
            };
        }

        public StrategyExchangePolicyDecision Evaluate(string strategyName, string venue, OrderSide side, MarketBias globalBias, IList<VenueHealthSnapshot> venueHealth)
        {
            var normalizedStrategy = NormalizeStrategy(strategyName);
            var normalizedVenue = NormalizeVenue(venue);

            if (globalBias == MarketBias.Bearish && side == OrderSide.Buy)
            {
                return Reject("regime-mismatch", strategyName, normalizedVenue, "Global bias is Bearish and candidate side is Buy.");
            }

            if (globalBias == MarketBias.Bullish && side == OrderSide.Sell)
            {
                return Reject("regime-mismatch", strategyName, normalizedVenue, "Global bias is Bullish and candidate side is Sell.");
            }

            if (string.IsNullOrWhiteSpace(normalizedVenue))
            {
                return Reject("policy-venue-unknown", strategyName, normalizedVenue, "No routed execution venue was available for policy evaluation.");
            }

            HashSet<string> allowSet;
            if (_strategyVenueAllowList.TryGetValue(normalizedStrategy, out allowSet) && allowSet != null && allowSet.Count > 0)
            {
                if (!allowSet.Contains(normalizedVenue))
                {
                    return Reject("policy-matrix-blocked", strategyName, normalizedVenue, "Strategy is not permitted on routed venue by runtime policy matrix.");
                }
            }

            var health = FindVenueHealth(venueHealth, normalizedVenue);
            if (health != null)
            {
                if (health.CircuitBreakerOpen)
                {
                    return Reject("policy-health-blocked", strategyName, normalizedVenue, "Venue circuit breaker is open.");
                }

                if (health.IsDegraded)
                {
                    return Reject("policy-health-blocked", strategyName, normalizedVenue, "Venue health is degraded for execution policy.");
                }
            }

            return new StrategyExchangePolicyDecision
            {
                IsAllowed = true,
                RejectCode = string.Empty,
                RejectReason = string.Empty,
                PolicyId = BuildPolicyId(normalizedStrategy, normalizedVenue),
                PolicyRationale = "strategy-venue matrix allows routed venue; regime and health checks passed"
            };
        }

        private static HashSet<string> NewVenueSet(params string[] venues)
        {
            return new HashSet<string>(venues ?? new string[0], StringComparer.OrdinalIgnoreCase);
        }

        private static VenueHealthSnapshot FindVenueHealth(IList<VenueHealthSnapshot> venueHealth, string venue)
        {
            if (venueHealth == null || venueHealth.Count == 0 || string.IsNullOrWhiteSpace(venue))
            {
                return null;
            }

            for (int i = 0; i < venueHealth.Count; i++)
            {
                var candidate = venueHealth[i];
                if (candidate == null || string.IsNullOrWhiteSpace(candidate.Venue)) continue;
                if (string.Equals(NormalizeVenue(candidate.Venue), venue, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return null;
        }

        private StrategyExchangePolicyDecision Reject(string code, string strategyName, string venue, string reason)
        {
            var normalizedStrategy = NormalizeStrategy(strategyName);
            return new StrategyExchangePolicyDecision
            {
                IsAllowed = false,
                RejectCode = code ?? "policy-matrix-blocked",
                RejectReason = reason ?? "Policy rejected candidate.",
                PolicyId = BuildPolicyId(normalizedStrategy, venue),
                PolicyRationale = reason ?? "policy-reject"
            };
        }

        private static string BuildPolicyId(string normalizedStrategy, string normalizedVenue)
        {
            return (normalizedStrategy ?? "unknown-strategy") + "@" + (normalizedVenue ?? "unknown-venue");
        }

        private static string NormalizeStrategy(string strategyName)
        {
            var value = (strategyName ?? string.Empty).Trim().ToLowerInvariant();
            value = value.Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty);

            if (value.Contains("vwap")) return "vwaptrend";
            if (value.Contains("orb")) return "orb";
            if (value.Contains("rsi")) return "rsireversion";
            if (value.Contains("donchian")) return "donchian";
            if (value.Contains("funding") && value.Contains("carry")) return "fundingcarry";
            if (value.Contains("spread") && value.Contains("divergence")) return "crossexchangespreaddivergence";

            return value;
        }

        private static string NormalizeVenue(string venue)
        {
            var value = (venue ?? string.Empty).Trim();
            if (value.Equals("Coinbase Advanced", StringComparison.OrdinalIgnoreCase) || value.Equals("Coinbase Exchange", StringComparison.OrdinalIgnoreCase))
            {
                return "Coinbase";
            }

            return value;
        }
    }
}
