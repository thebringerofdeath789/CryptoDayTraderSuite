using System;
using System.Collections.Generic;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Services
{
	public class ExchangeProvider : IExchangeProvider
	{
		private readonly IKeyService _keyService;

		private readonly Dictionary<string, (string hash, IExchangeClient client)> _clientCache = new Dictionary<string, (string, IExchangeClient)>();

		public ExchangeProvider(IKeyService keyService)
		{
			_keyService = keyService;
		}

		public IExchangeClient CreateAuthenticatedClient(string brokerName)
		{
			if (string.IsNullOrEmpty(brokerName))
			{
				brokerName = "Coinbase";
			}
			Log.Info("[Connection] CreateAuthenticatedClient requested for " + brokerName, "CreateAuthenticatedClient", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\ExchangeProvider.cs", 32);
			string activeKeyId = _keyService.GetActiveId(brokerName);
			if (string.IsNullOrEmpty(activeKeyId))
			{
				throw new InvalidOperationException("No active API key found for " + brokerName + ". Please set one in the API Keys tab.");
			}
			KeyInfo keyEntry = _keyService.Get(activeKeyId);
			if (keyEntry == null)
			{
				throw new InvalidOperationException("Active API key " + activeKeyId + " not found in the registry.");
			}
			string k = keyEntry.ApiKey ?? "";
			string s = keyEntry.Secret ?? "";
			string p = keyEntry.Passphrase ?? "";
			string stateHash = activeKeyId + "|" + k + "|" + s + "|" + p;
			if (_clientCache.TryGetValue(activeKeyId, out (string, IExchangeClient) cached) && cached.Item1 == stateHash)
			{
				Log.Debug("[Connection] Using cached authenticated client for " + brokerName + " (" + activeKeyId + ")", "CreateAuthenticatedClient", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\ExchangeProvider.cs", 57);
				return cached.Item2;
			}
			string apiKey = _keyService.Unprotect(k);
			string apiSecret = _keyService.Unprotect(s);
			string passphrase = _keyService.Unprotect(p);
			IExchangeClient client = Factory(brokerName, apiKey, apiSecret, passphrase);
			Log.Info("[Connection] Authenticated client created for " + brokerName + " (" + activeKeyId + ")", "CreateAuthenticatedClient", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\ExchangeProvider.cs", 68);
			_clientCache[activeKeyId] = (stateHash, client);
			return client;
		}

		public IExchangeClient CreatePublicClient(string brokerName)
		{
			if (string.IsNullOrEmpty(brokerName))
			{
				brokerName = "Coinbase";
			}
			Log.Info("[Connection] Public client created for " + brokerName, "CreatePublicClient", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\ExchangeProvider.cs", 78);
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
			case "kraken":
				client = new KrakenClient(key, secret);
				break;
			case "bitstamp":
				client = new BitstampClient(key, secret, pass);
				break;
			default:
				throw new NotSupportedException("The exchange " + brokerName + " is not supported.");
			}
			return new ResilientExchangeClient(client);
		}
	}
}
