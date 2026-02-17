using System;

namespace CryptoDayTraderSuite.Services
{
    public static class ExchangeServiceNameNormalizer
    {
        public static string NormalizeBrokerName(string brokerName)
        {
            if (string.IsNullOrWhiteSpace(brokerName)) return "Coinbase";

            var key = brokerName.Trim().ToLowerInvariant();
            switch (key)
            {
                case "coinbase advanced":
                case "coinbase-advanced":
                case "coinbase_advanced":
                case "coinbaseadvanced":
                case "coinbase exchange":
                case "coinbase-exchange":
                case "coinbase_exchange":
                case "coinbase":
                    return "Coinbase";
                case "kraken":
                    return "Kraken";
                case "bitstamp":
                    return "Bitstamp";
                case "binance":
                    return "Binance";
                case "binance us":
                case "binance_us":
                case "binance-us":
                    return "Binance-US";
                case "binance global":
                case "binance_global":
                case "binance-global":
                    return "Binance-Global";
                case "bybit":
                    return "Bybit";
                case "bybit global":
                case "bybit_global":
                case "bybit-global":
                    return "Bybit-Global";
                case "okx":
                    return "OKX";
                case "okx global":
                case "okx_global":
                case "okx-global":
                    return "OKX-Global";
                default:
                    return brokerName.Trim();
            }
        }

        public static string NormalizeFamilyKey(string serviceName, string defaultValue = "")
        {
            var canonical = NormalizeBrokerName(serviceName);
            if (string.IsNullOrWhiteSpace(canonical))
            {
                return defaultValue ?? string.Empty;
            }

            switch (canonical.ToLowerInvariant())
            {
                case "coinbase":
                    return "coinbase";
                case "binance":
                case "binance-us":
                case "binance-global":
                    return "binance";
                case "bybit":
                case "bybit-global":
                    return "bybit";
                case "okx":
                case "okx-global":
                    return "okx";
                case "kraken":
                    return "kraken";
                case "bitstamp":
                    return "bitstamp";
                default:
                    return canonical.Trim().ToLowerInvariant();
            }
        }

        public static string NormalizeAuditServiceName(string serviceName)
        {
            var family = NormalizeFamilyKey(serviceName, string.Empty);
            switch (family)
            {
                case "coinbase":
                    return "Coinbase";
                case "binance":
                    return "Binance";
                case "bybit":
                    return "Bybit";
                case "okx":
                    return "OKX";
                case "kraken":
                    return "Kraken";
                case "bitstamp":
                    return "Bitstamp";
                default:
                    return (serviceName ?? string.Empty).Trim();
            }
        }

        public static bool IsBinanceGlobalAlias(string serviceName)
        {
            return string.Equals(NormalizeBrokerName(serviceName), "Binance-Global", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsBybitGlobalAlias(string serviceName)
        {
            return string.Equals(NormalizeBrokerName(serviceName), "Bybit-Global", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsOkxGlobalAlias(string serviceName)
        {
            return string.Equals(NormalizeBrokerName(serviceName), "OKX-Global", StringComparison.OrdinalIgnoreCase);
        }
    }
}