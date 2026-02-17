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
                        Source = ExchangeServiceNameNormalizer.NormalizeFamilyKey(account.Service, "coinbase"),
                        Note = "live quote balance"
                    };
                    Cache(cacheKey, live);
                    return live;
                }

                var missing = ManualFallback(manualFallback, "quote balance unavailable");
                missing.Source = ExchangeServiceNameNormalizer.NormalizeFamilyKey(account.Service, "coinbase");
                missing.QuoteCurrency = quoteCandidates.FirstOrDefault() ?? "USD";
                Cache(cacheKey, missing);
                return missing;
            }
            catch (Exception ex)
            {
                var fallback = ManualFallback(manualFallback, "balance lookup failed: " + ex.Message);
                fallback.Source = ExchangeServiceNameNormalizer.NormalizeFamilyKey(account.Service, "coinbase");
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
                + ExchangeServiceNameNormalizer.NormalizeFamilyKey(account.Service, "coinbase") + "|"
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
            var service = ExchangeServiceNameNormalizer.NormalizeFamilyKey(account.Service, "coinbase");
            switch (service)
            {
                case "coinbase":
                    return await GetCoinbaseBalancesAsync(key).ConfigureAwait(false);
                case "binance":
                    return await GetBinanceBalancesAsync(account, key).ConfigureAwait(false);
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
            var inner = new CoinbaseExchangeClient(keyName, pem, passphrase);
            var client = new ResilientExchangeClient(inner, serviceKey: "coinbase");
            return await client.GetBalancesAsync().ConfigureAwait(false);
        }

        private async Task<Dictionary<string, decimal>> GetBinanceBalancesAsync(AccountInfo account, KeyInfo key)
        {
            var apiKey = SafeUnprotect(key.ApiKey);
            var apiSecret = SafeUnprotect(!string.IsNullOrWhiteSpace(key.ApiSecretBase64) ? key.ApiSecretBase64 : key.Secret);
            var isGlobal = ExchangeServiceNameNormalizer.IsBinanceGlobalAlias(account == null ? string.Empty : account.Service);
            var restBaseUrl = isGlobal
                ? "https://api.binance.com"
                : "https://api.binance.us";
            var inner = new BinanceClient(apiKey, apiSecret, restBaseUrl);
            var client = new ResilientExchangeClient(inner, serviceKey: isGlobal ? "binance-global" : "binance-us");
            return await client.GetBalancesAsync().ConfigureAwait(false);
        }

        private async Task<Dictionary<string, decimal>> GetKrakenBalancesAsync(KeyInfo key)
        {
            var apiKey = SafeUnprotect(key.ApiKey);
            var apiSecret = SafeUnprotect(!string.IsNullOrWhiteSpace(key.ApiSecretBase64) ? key.ApiSecretBase64 : key.Secret);
            var inner = new KrakenClient(apiKey, apiSecret);
            var client = new ResilientExchangeClient(inner, serviceKey: "kraken");
            return await client.GetBalancesAsync().ConfigureAwait(false);
        }

        private async Task<Dictionary<string, decimal>> GetBitstampBalancesAsync(KeyInfo key)
        {
            var apiKey = SafeUnprotect(key.ApiKey);
            var apiSecret = SafeUnprotect(!string.IsNullOrWhiteSpace(key.ApiSecretBase64) ? key.ApiSecretBase64 : key.Secret);
            var customerId = SafeUnprotect(key.Passphrase);
            var inner = new BitstampClient(apiKey, apiSecret, customerId);
            var client = new ResilientExchangeClient(inner, serviceKey: "bitstamp");
            return await client.GetBalancesAsync().ConfigureAwait(false);
        }

        private async Task<Dictionary<string, decimal>> GetBybitBalancesAsync(AccountInfo account, KeyInfo key)
        {
            var apiKey = SafeUnprotect(key.ApiKey);
            var apiSecret = SafeUnprotect(!string.IsNullOrWhiteSpace(key.ApiSecretBase64) ? key.ApiSecretBase64 : key.Secret);
            var isGlobal = ExchangeServiceNameNormalizer.IsBybitGlobalAlias(account == null ? string.Empty : account.Service);
            var restBaseUrl = isGlobal
                ? "https://api.bybit.com"
                : null;
            var inner = new BybitClient(apiKey, apiSecret, restBaseUrl);
            var client = new ResilientExchangeClient(inner, serviceKey: isGlobal ? "bybit-global" : "bybit");
            return await client.GetBalancesAsync().ConfigureAwait(false);
        }

        private async Task<Dictionary<string, decimal>> GetOkxBalancesAsync(AccountInfo account, KeyInfo key)
        {
            var apiKey = SafeUnprotect(key.ApiKey);
            var apiSecret = SafeUnprotect(!string.IsNullOrWhiteSpace(key.ApiSecretBase64) ? key.ApiSecretBase64 : key.Secret);
            var passphrase = SafeUnprotect(key.Passphrase);
            var isGlobal = ExchangeServiceNameNormalizer.IsOkxGlobalAlias(account == null ? string.Empty : account.Service);
            var restBaseUrl = isGlobal
                ? "https://www.okx.com"
                : null;
            var inner = new OkxClient(apiKey, apiSecret, passphrase, restBaseUrl);
            var client = new ResilientExchangeClient(inner, serviceKey: isGlobal ? "okx-global" : "okx");
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

    }
}