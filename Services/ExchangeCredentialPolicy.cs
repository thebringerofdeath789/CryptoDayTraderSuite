using System;
using System.Collections.Generic;

namespace CryptoDayTraderSuite.Services
{
    public sealed class ExchangeCredentialPolicy
    {
        public static readonly ExchangeCredentialPolicy GenericApiKeySecret =
            new ExchangeCredentialPolicy(
                true,
                true,
                false,
                false,
                false,
                "API Key + API Secret",
                "API Key=<exchange key>; API Secret=<exchange secret>");

        private static readonly Dictionary<string, ExchangeCredentialPolicy> Policies =
            new Dictionary<string, ExchangeCredentialPolicy>(StringComparer.OrdinalIgnoreCase)
            {
                { "paper", new ExchangeCredentialPolicy(false, false, false, false, false, "No API credentials required", "No credentials are required for Paper mode") },
                { "coinbase-advanced", new ExchangeCredentialPolicy(false, false, false, true, true, "API Key Name + EC Private Key (PEM)", "API Key Name=organizations/{orgId}/apiKeys/{keyId}; EC Private Key (PEM)=-----BEGIN EC PRIVATE KEY-----...") },
                { "coinbase-exchange", new ExchangeCredentialPolicy(false, false, false, true, true, "API Key Name + EC Private Key (PEM)", "API Key Name=organizations/{orgId}/apiKeys/{keyId}; EC Private Key (PEM)=-----BEGIN EC PRIVATE KEY-----...") },
                { "binance", new ExchangeCredentialPolicy(true, true, false, false, false, "API Key + API Secret", "API Key=<binance key>; API Secret=<binance secret>") },
                { "binance-us", new ExchangeCredentialPolicy(true, true, false, false, false, "API Key + API Secret", "API Key=<binance us key>; API Secret=<binance us secret>") },
                { "binance-global", new ExchangeCredentialPolicy(true, true, false, false, false, "API Key + API Secret", "API Key=<binance global key>; API Secret=<binance global secret>") },
                { "bybit", new ExchangeCredentialPolicy(true, true, false, false, false, "API Key + API Secret", "API Key=<bybit key>; API Secret=<bybit secret>") },
                { "bybit-global", new ExchangeCredentialPolicy(true, true, false, false, false, "API Key + API Secret", "API Key=<bybit global key>; API Secret=<bybit global secret>") },
                { "okx", new ExchangeCredentialPolicy(true, true, true, false, false, "API Key + API Secret + Passphrase", "API Key=<okx key>; API Secret=<okx secret>; Passphrase=<okx passphrase>") },
                { "okx-global", new ExchangeCredentialPolicy(true, true, true, false, false, "API Key + API Secret + Passphrase", "API Key=<okx global key>; API Secret=<okx global secret>; Passphrase=<okx passphrase>") },
                { "kraken", new ExchangeCredentialPolicy(true, true, false, false, false, "API Key + API Secret", "API Key=<kraken key>; API Secret=<kraken secret>") },
                { "bitstamp", new ExchangeCredentialPolicy(true, true, false, false, false, "API Key + API Secret", "API Key=<bitstamp key>; API Secret=<bitstamp secret>") }
            };

        public ExchangeCredentialPolicy(bool requiresApiKey, bool requiresApiSecret, bool requiresPassphrase, bool requiresApiKeyName, bool requiresEcPrivateKeyPem, string requiredSummary, string templateSummary)
        {
            RequiresApiKey = requiresApiKey;
            RequiresApiSecret = requiresApiSecret;
            RequiresPassphrase = requiresPassphrase;
            RequiresApiKeyName = requiresApiKeyName;
            RequiresEcPrivateKeyPem = requiresEcPrivateKeyPem;
            RequiredSummary = requiredSummary ?? string.Empty;
            TemplateSummary = templateSummary ?? string.Empty;
        }

        public bool RequiresApiKey { get; private set; }

        public bool RequiresApiSecret { get; private set; }

        public bool RequiresPassphrase { get; private set; }

        public bool RequiresApiKeyName { get; private set; }

        public bool RequiresEcPrivateKeyPem { get; private set; }

        public string RequiredSummary { get; private set; }

        public string TemplateSummary { get; private set; }

        public bool IsPaper
        {
            get { return !RequiresApiKey && !RequiresApiSecret && !RequiresPassphrase && !RequiresApiKeyName && !RequiresEcPrivateKeyPem; }
        }

        public static ExchangeCredentialPolicy ForService(string service)
        {
            if (string.IsNullOrWhiteSpace(service)) return GenericApiKeySecret;

            ExchangeCredentialPolicy policy;
            if (Policies.TryGetValue(service.Trim(), out policy))
            {
                return policy;
            }

            return GenericApiKeySecret;
        }
    }
}
