using System;
using System.Collections.Generic;

namespace CryptoDayTraderSuite.Services
{
    public sealed class ExchangeCredentialPolicy
    {
        public static readonly ExchangeCredentialPolicy GenericApiKeySecret =
            new ExchangeCredentialPolicy(true, true, false, false, false, "API Key + API Secret");

        private static readonly Dictionary<string, ExchangeCredentialPolicy> Policies =
            new Dictionary<string, ExchangeCredentialPolicy>(StringComparer.OrdinalIgnoreCase)
            {
                { "paper", new ExchangeCredentialPolicy(false, false, false, false, false, "No API credentials required") },
                { "coinbase-advanced", new ExchangeCredentialPolicy(false, false, false, true, true, "API Key Name + EC Private Key (PEM)") },
                { "coinbase-exchange", new ExchangeCredentialPolicy(false, false, false, true, true, "API Key Name + EC Private Key (PEM)") },
                { "binance", new ExchangeCredentialPolicy(true, true, false, false, false, "API Key + API Secret") },
                { "binance-us", new ExchangeCredentialPolicy(true, true, false, false, false, "API Key + API Secret") },
                { "binance-global", new ExchangeCredentialPolicy(true, true, false, false, false, "API Key + API Secret") },
                { "bybit", new ExchangeCredentialPolicy(true, true, false, false, false, "API Key + API Secret") },
                { "bybit-global", new ExchangeCredentialPolicy(true, true, false, false, false, "API Key + API Secret") },
                { "okx", new ExchangeCredentialPolicy(true, true, true, false, false, "API Key + API Secret + Passphrase") },
                { "okx-global", new ExchangeCredentialPolicy(true, true, true, false, false, "API Key + API Secret + Passphrase") },
                { "kraken", new ExchangeCredentialPolicy(true, true, false, false, false, "API Key + API Secret") },
                { "bitstamp", new ExchangeCredentialPolicy(true, true, false, false, false, "API Key + API Secret") }
            };

        public ExchangeCredentialPolicy(bool requiresApiKey, bool requiresApiSecret, bool requiresPassphrase, bool requiresApiKeyName, bool requiresEcPrivateKeyPem, string requiredSummary)
        {
            RequiresApiKey = requiresApiKey;
            RequiresApiSecret = requiresApiSecret;
            RequiresPassphrase = requiresPassphrase;
            RequiresApiKeyName = requiresApiKeyName;
            RequiresEcPrivateKeyPem = requiresEcPrivateKeyPem;
            RequiredSummary = requiredSummary ?? string.Empty;
        }

        public bool RequiresApiKey { get; private set; }

        public bool RequiresApiSecret { get; private set; }

        public bool RequiresPassphrase { get; private set; }

        public bool RequiresApiKeyName { get; private set; }

        public bool RequiresEcPrivateKeyPem { get; private set; }

        public string RequiredSummary { get; private set; }

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
