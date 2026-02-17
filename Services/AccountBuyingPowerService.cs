using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Services
{
    public sealed class AccountBuyingPowerResolution
    {
        public decimal EquityToUse;
        public bool UsedLiveBalance;
        public string QuoteCurrency;
        public string Source;
        public string Note;
    }

    public sealed class AccountBuyingPowerService
    {
        private sealed class CacheEntry
        {
            public AccountBuyingPowerResolution Resolution;
            public DateTime ExpiresUtc;
        }

        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
        private readonly object _cacheLock = new object();
        private readonly Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly IKeyService _keyService;

        public AccountBuyingPowerService(IKeyService keyService)
        {
            _keyService = keyService;
        }

        public async Task<AccountBuyingPowerResolution> ResolveAsync(AccountInfo account, decimal manualFallback)
        {
            if (account == null)
            {
                return ManualFallback(manualFallback, "account unavailable");
            }

            if (_keyService == null)
            {
                return ManualFallback(manualFallback, "key service unavailable");
            }

            if (account.Mode == AccountMode.Paper)
            {
                return ManualFallback(manualFallback, "paper account");
            }

            if (string.IsNullOrWhiteSpace(account.KeyEntryId))
            {
                return ManualFallback(manualFallback, "missing key id");
            }

            var cacheKey = BuildCacheKey(account);
            var cached = TryGetCached(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var key = _keyService.Get(account.KeyEntryId);
            if (key == null)
            {
                return ManualFallback(manualFallback, "key not found");
            }

            try
            {
                var balances = await GetBalancesAsync(account, key).ConfigureAwait(false);
                var quoteCandidates = BuildQuoteCandidates(account.DefaultQuote);
                foreach (var quote in quoteCandidates)
                {
                    decimal value;
                    if (!balances.TryGetValue(quote, out value))
                    {
                        continue;
                    }

                    if (value <= 0m)
                    {
                        continue;
                    }

                    var live = new AccountBuyingPowerResolution
                    {
                        EquityToUse = value,
                        UsedLiveBalance = true,
                        QuoteCurrency = quote,
                        Source = NormalizeService(account.Service),
                        Note = "live quote balance"
                    };
                    Cache(cacheKey, live);
                    return live;
                }

                var missing = ManualFallback(manualFallback, "quote balance unavailable");
                missing.Source = NormalizeService(account.Service);
                missing.QuoteCurrency = quoteCandidates.FirstOrDefault() ?? "USD";
                Cache(cacheKey, missing);
                return missing;
            }
            catch (Exception ex)
            {
                var fallback = ManualFallback(manualFallback, "balance lookup failed: " + ex.Message);
                fallback.Source = NormalizeService(account.Service);
                Cache(cacheKey, fallback);
                return fallback;
            }
        }

        private AccountBuyingPowerResolution TryGetCached(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                return null;
            }

            lock (_cacheLock)
            {
                CacheEntry entry;
                if (!_cache.TryGetValue(cacheKey, out entry) || entry == null)
                {
                    return null;
                }

                if (DateTime.UtcNow > entry.ExpiresUtc)
                {
                    _cache.Remove(cacheKey);
                    return null;
                }

                return entry.Resolution;
            }
        }

        private void Cache(string cacheKey, AccountBuyingPowerResolution resolution)
        {
            if (string.IsNullOrWhiteSpace(cacheKey) || resolution == null)
            {
                return;
            }

            lock (_cacheLock)
            {
                _cache[cacheKey] = new CacheEntry
                {
                    Resolution = resolution,
                    ExpiresUtc = DateTime.UtcNow.Add(CacheTtl)
                };
            }
        }

        private string BuildCacheKey(AccountInfo account)
        {
            return (account.Id ?? string.Empty) + "|"
                + (account.KeyEntryId ?? string.Empty) + "|"
                + NormalizeService(account.Service) + "|"
                + (account.DefaultQuote ?? string.Empty);
        }

        private static AccountBuyingPowerResolution ManualFallback(decimal manualFallback, string reason)
        {
            return new AccountBuyingPowerResolution
            {
                EquityToUse = manualFallback,
                UsedLiveBalance = false,
                QuoteCurrency = "USD",
                Source = "manual",
                Note = reason
            };
        }

        private async Task<Dictionary<string, decimal>> GetBalancesAsync(AccountInfo account, KeyInfo key)
        {
            var service = NormalizeService(account.Service);
            switch (service)
            {
                case "coinbase":
                    return await GetCoinbaseBalancesAsync(key).ConfigureAwait(false);
                case "kraken":
                    return await GetKrakenBalancesAsync(key).ConfigureAwait(false);
                case "bitstamp":
                    return await GetBitstampBalancesAsync(key).ConfigureAwait(false);
                case "bybit":
                    return await GetBybitBalancesAsync(account, key).ConfigureAwait(false);
                case "okx":
                    return await GetOkxBalancesAsync(account, key).ConfigureAwait(false);
                default:
                    throw new NotSupportedException("Buying-power auto-detection is not implemented for service '" + service + "'.");
            }
        }

        private async Task<Dictionary<string, decimal>> GetCoinbaseBalancesAsync(KeyInfo key)
        {
            var apiKey = SafeUnprotect(key.ApiKey);
            var apiSecret = SafeUnprotect(!string.IsNullOrWhiteSpace(key.ApiSecretBase64) ? key.ApiSecretBase64 : key.Secret);
            var passphrase = SafeUnprotect(key.Passphrase);
            var keyName = key.ApiKeyName ?? string.Empty;
            var pem = SafeUnprotect(key.ECPrivateKeyPem);

            CoinbaseCredentialNormalizer.NormalizeCoinbaseAdvancedInputs(ref apiKey, ref apiSecret, ref keyName, ref pem);
            var client = new CoinbaseExchangeClient(keyName, pem, passphrase);
            return await client.GetBalancesAsync().ConfigureAwait(false);
        }

        private async Task<Dictionary<string, decimal>> GetKrakenBalancesAsync(KeyInfo key)
        {
            var apiKey = SafeUnprotect(key.ApiKey);
            var apiSecret = SafeUnprotect(!string.IsNullOrWhiteSpace(key.ApiSecretBase64) ? key.ApiSecretBase64 : key.Secret);
            var client = new KrakenClient(apiKey, apiSecret);
            return await client.GetBalancesAsync().ConfigureAwait(false);
        }

        private async Task<Dictionary<string, decimal>> GetBitstampBalancesAsync(KeyInfo key)
        {
            var apiKey = SafeUnprotect(key.ApiKey);
            var apiSecret = SafeUnprotect(!string.IsNullOrWhiteSpace(key.ApiSecretBase64) ? key.ApiSecretBase64 : key.Secret);
            var customerId = SafeUnprotect(key.Passphrase);
            var client = new BitstampClient(apiKey, apiSecret, customerId);
            return await client.GetBalancesAsync().ConfigureAwait(false);
        }

        private async Task<Dictionary<string, decimal>> GetBybitBalancesAsync(AccountInfo account, KeyInfo key)
        {
            var apiKey = SafeUnprotect(key.ApiKey);
            var apiSecret = SafeUnprotect(!string.IsNullOrWhiteSpace(key.ApiSecretBase64) ? key.ApiSecretBase64 : key.Secret);
            var rawService = (account == null ? string.Empty : (account.Service ?? string.Empty)).Trim().ToLowerInvariant();
            var restBaseUrl = rawService == "bybit-global" || rawService == "bybit_global" || rawService == "bybit global"
                ? "https://api.bybit.com"
                : null;
            var client = new BybitClient(apiKey, apiSecret, restBaseUrl);
            return await client.GetBalancesAsync().ConfigureAwait(false);
        }

        private async Task<Dictionary<string, decimal>> GetOkxBalancesAsync(AccountInfo account, KeyInfo key)
        {
            var apiKey = SafeUnprotect(key.ApiKey);
            var apiSecret = SafeUnprotect(!string.IsNullOrWhiteSpace(key.ApiSecretBase64) ? key.ApiSecretBase64 : key.Secret);
            var passphrase = SafeUnprotect(key.Passphrase);
            var rawService = (account == null ? string.Empty : (account.Service ?? string.Empty)).Trim().ToLowerInvariant();
            var restBaseUrl = rawService == "okx-global" || rawService == "okx_global" || rawService == "okx global"
                ? "https://www.okx.com"
                : null;
            var client = new OkxClient(apiKey, apiSecret, passphrase, restBaseUrl);
            return await client.GetBalancesAsync().ConfigureAwait(false);
        }

        private string SafeUnprotect(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            try
            {
                return _keyService.Unprotect(value);
            }
            catch
            {
                return value;
            }
        }

        private static List<string> BuildQuoteCandidates(string preferredQuote)
        {
            var list = new List<string>();
            var normalized = string.IsNullOrWhiteSpace(preferredQuote) ? "USD" : preferredQuote.Trim().ToUpperInvariant();
            list.Add(normalized);
            if (!list.Contains("USD")) list.Add("USD");
            if (!list.Contains("USDC")) list.Add("USDC");
            if (!list.Contains("USDT")) list.Add("USDT");
            return list;
        }

        private static string NormalizeService(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "coinbase";
            var normalized = value.Trim().ToLowerInvariant();

            if (normalized == "coinbase" || normalized == "coinbase-advanced" || normalized == "coinbase_exchange" || normalized == "coinbase-exchange" || normalized == "coinbase advanced" || normalized == "coinbase exchange")
            {
                return "coinbase";
            }

            if (normalized == "kraken")
            {
                return "kraken";
            }

            if (normalized == "bitstamp")
            {
                return "bitstamp";
            }

            if (normalized == "bybit" || normalized == "bybit-global" || normalized == "bybit_global" || normalized == "bybit global")
            {
                return "bybit";
            }

            if (normalized == "okx" || normalized == "okx-global" || normalized == "okx_global" || normalized == "okx global")
            {
                return "okx";
            }

            return normalized;
        }
    }
}