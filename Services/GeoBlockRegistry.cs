using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Services
{
    public static class GeoBlockRegistry
    {
        private sealed class DisabledServiceState
        {
            public DateTime DisabledAtUtc;
            public string Reason;
        }

        private static readonly ConcurrentDictionary<string, DisabledServiceState> _disabled = new ConcurrentDictionary<string, DisabledServiceState>(StringComparer.OrdinalIgnoreCase);

        public static bool IsDisabled(string service)
        {
            var key = NormalizeServiceKey(service);
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return _disabled.ContainsKey(key);
        }

        public static string GetDisableReason(string service)
        {
            var key = NormalizeServiceKey(service);
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            DisabledServiceState state;
            if (_disabled.TryGetValue(key, out state) && state != null)
            {
                return state.Reason ?? string.Empty;
            }

            return string.Empty;
        }

        public static List<string> GetDisabledServices()
        {
            return _disabled.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static bool TryDisable(string service, string reason)
        {
            var key = NormalizeServiceKey(service);
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            var normalizedReason = string.IsNullOrWhiteSpace(reason) ? "geo-restricted" : reason.Trim();
            var wasAdded = _disabled.TryAdd(key, new DisabledServiceState
            {
                DisabledAtUtc = DateTime.UtcNow,
                Reason = normalizedReason
            });

            if (wasAdded)
            {
                Log.Warn("[GeoBlock] Disabled service=" + key + " reason=" + normalizedReason);
            }

            return wasAdded;
        }

        public static bool TryDisableFromException(string service, Exception ex, string context = null)
        {
            if (ex == null)
            {
                return false;
            }

            var message = ex.Message ?? string.Empty;
            if (!LooksLikeGeoRestriction(message))
            {
                return false;
            }

            var prefix = string.IsNullOrWhiteSpace(context) ? "geo-restricted" : (context.Trim() + " geo-restricted");
            var reason = prefix + ": " + message;
            return TryDisable(service, reason);
        }

        public static bool LooksLikeGeoRestriction(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            var normalized = message.ToLowerInvariant();
            return normalized.Contains("http 403")
                || normalized.Contains("forbidden")
                || normalized.Contains("restricted location")
                || normalized.Contains("geo-block")
                || normalized.Contains("geoblock")
                || normalized.Contains("region is not supported")
                || normalized.Contains("not available in your region")
                || normalized.Contains("service unavailable from a restricted location");
        }

        public static string NormalizeServiceKey(string service)
        {
            if (string.IsNullOrWhiteSpace(service))
            {
                return string.Empty;
            }

            return service.Trim().Replace("_", "-").Replace(" ", "-").ToLowerInvariant();
        }
    }
}
