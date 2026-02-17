using System;

namespace CryptoDayTraderSuite.Brokers
{
    internal static class BrokerMessageFormatter
    {
        public static string BuildSuccessMessage(string category, string detail)
        {
            var normalizedCategory = string.IsNullOrWhiteSpace(category) ? "ok" : category.Trim().ToLowerInvariant();
            var body = string.IsNullOrWhiteSpace(detail) ? "ok" : detail.Trim();
            if (body.Length > 220)
            {
                body = body.Substring(0, 220) + "...";
            }

            return normalizedCategory + ": " + body;
        }

        public static string BuildFailureMessage(string category, string detail, string fallback)
        {
            var normalizedCategory = string.IsNullOrWhiteSpace(category) ? "error" : category.Trim().ToLowerInvariant();
            var body = string.IsNullOrWhiteSpace(detail) ? (fallback ?? "error") : detail.Trim();
            if (body.Length > 220)
            {
                body = body.Substring(0, 220) + "...";
            }

            return normalizedCategory + ": " + body;
        }
    }
}
