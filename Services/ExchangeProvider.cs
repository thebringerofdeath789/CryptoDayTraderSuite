using System;
using System.Collections.Generic;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Services
{
    public interface IExchangeProvider
    {
        IExchangeClient CreateAuthenticatedClient(string brokerName);
        IExchangeClient CreatePublicClient(string brokerName);
    }

    public class ExchangeProvider : IExchangeProvider
    {
        private readonly IKeyService _keyService;

        /* Cache hash of encrypted credentials -> instantiated client to avoid repeated DPAPI decryption */
        private readonly Dictionary<string, (string hash, IExchangeClient client)> _clientCache 
            = new Dictionary<string, (string, IExchangeClient)>();

        public ExchangeProvider(IKeyService keyService)
        {
            _keyService = keyService;
        }

        public IExchangeClient CreateAuthenticatedClient(string brokerName)
        {
            if (string.IsNullOrEmpty(brokerName)) brokerName = "Coinbase";
            var rawBrokerName = brokerName;
            brokerName = ExchangeServiceNameNormalizer.NormalizeBrokerName(brokerName);
            if (GeoBlockRegistry.IsDisabled(brokerName))
            {
                var reason = GeoBlockRegistry.GetDisableReason(brokerName);
                Log.Warn("[Connection] Authenticated client blocked by geo-disable for " + brokerName + " reason=" + reason);
                return new DisabledExchangeClient(brokerName, reason);
            }
            Log.Info($"[Connection] CreateAuthenticatedClient requested for {brokerName}");

            var activeKeyId = _keyService.GetActiveId(brokerName);
            if (string.IsNullOrEmpty(activeKeyId) && !string.Equals(rawBrokerName, brokerName, StringComparison.OrdinalIgnoreCase))
            {
                activeKeyId = _keyService.GetActiveId(rawBrokerName);
            }
            if (string.IsNullOrEmpty(activeKeyId))
            {
                throw new InvalidOperationException($"No active API key found for {brokerName}. Please set one in the API Keys tab.");
            }

            var keyEntry = _keyService.Get(activeKeyId);
            if (keyEntry == null)
            {
                throw new InvalidOperationException($"Active API key {activeKeyId} not found in the registry.");
            }
            
            // Generate a state hash of the encrypted blobs
            var k = keyEntry.ApiKey ?? "";
            var s = keyEntry.Secret ?? "";
            var p = keyEntry.Passphrase ?? "";
            var stateHash = $"{activeKeyId}|{k}|{s}|{p}";

            // Check cache
            if (_clientCache.TryGetValue(activeKeyId, out var cached))
            {
                if (cached.hash == stateHash)
                {
                    Log.Debug($"[Connection] Using cached authenticated client for {brokerName} ({activeKeyId})");
                    return cached.client;
                }
            }

            // Fallback: Decrypt and Instantiate
            string apiKey = _keyService.Unprotect(k);
            string apiSecret = _keyService.Unprotect(s);
            string passphrase = _keyService.Unprotect(p);

            var client = Factory(brokerName, apiKey, apiSecret, passphrase);
            Log.Info($"[Connection] Authenticated client created for {brokerName} ({activeKeyId})");
            
            // Update cache
            _clientCache[activeKeyId] = (stateHash, client);
            return client;
        }

        public IExchangeClient CreatePublicClient(string brokerName)
        {
            if (string.IsNullOrEmpty(brokerName)) brokerName = "Coinbase";
            brokerName = ExchangeServiceNameNormalizer.NormalizeBrokerName(brokerName);
            if (GeoBlockRegistry.IsDisabled(brokerName))
            {
                var reason = GeoBlockRegistry.GetDisableReason(brokerName);
                Log.Warn("[Connection] Public client blocked by geo-disable for " + brokerName + " reason=" + reason);
                return new DisabledExchangeClient(brokerName, reason);
            }
            Log.Info($"[Connection] Public client created for {brokerName}");
            return Factory(brokerName, null, null, null);
        }

        private IExchangeClient Factory(string brokerName, string key, string secret, string pass)
        {
            IExchangeClient client;
            switch (brokerName.ToLowerInvariant())
            {
                case "coinbase":
                    client = new CoinbaseExchangeClient(key, secret, pass);
                    break;
                case "binance":
                    client = new BinanceClient(key, secret);
                    break;
                case "binance-us":
                    client = new BinanceClient(key, secret, "https://api.binance.us");
                    break;
                case "binance-global":
                    client = new BinanceClient(key, secret, "https://api.binance.com");
                    break;
                case "bybit":
                    client = new BybitClient(key, secret);
                    break;
                case "bybit-global":
                    client = new BybitClient(key, secret, "https://api.bybit.com");
                    break;
                case "okx":
                    client = new OkxClient(key, secret, pass);
                    break;
                case "okx-global":
                    client = new OkxClient(key, secret, pass, "https://www.okx.com");
                    break;
                case "kraken":
                    client = new KrakenClient(key, secret);
                    break;
                case "bitstamp":
                    client = new BitstampClient(key, secret, pass);
                    break;
                default:
                    throw new NotSupportedException($"The exchange {brokerName} is not supported.");
            }

            // Wrap in resilience policy (Retries on Network Error)
            return new ResilientExchangeClient(client, serviceKey: brokerName);
        }
    }
}
