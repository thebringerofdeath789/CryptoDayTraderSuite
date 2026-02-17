using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Brokers
{
	public class CoinbaseExchangeBroker : IBroker
	{
		private readonly IKeyService _keyService;

		private readonly IAccountService _accountService;

		public string Service => "coinbase-exchange";

		public CoinbaseExchangeBroker(IKeyService keyService, IAccountService accountService)
		{
			_keyService = keyService ?? throw new ArgumentNullException("keyService");
			_accountService = accountService ?? throw new ArgumentNullException("accountService");
		}

		public BrokerCapabilities GetCapabilities()
		{
			return new BrokerCapabilities
			{
				SupportsMarketEntry = true,
				SupportsProtectiveExits = false,
				EnforcesPrecisionRules = true,
				Notes = "Native bracket/OCO exits are unavailable; requires local protective watchdog in Auto Mode."
			};
		}

		public (bool ok, string message) ValidateTradePlan(TradePlan plan)
		{
			if (plan == null)
			{
				return (ok: false, message: "trade plan is null");
			}
			if (string.IsNullOrWhiteSpace(plan.Symbol))
			{
				return (ok: false, message: "symbol is required");
			}
			if (plan.Qty <= 0m)
			{
				return (ok: false, message: "invalid quantity");
			}
			if (Math.Round(plan.Qty, 6) != plan.Qty)
			{
				return (ok: false, message: "quantity precision exceeds 6 decimals");
			}
			if (plan.Entry <= 0m)
			{
				return (ok: false, message: "entry must be greater than zero");
			}
			if (plan.Stop <= 0m || plan.Target <= 0m)
			{
				return (ok: false, message: "protective stop/target required");
			}
			return (ok: true, message: "ok");
		}

		public Task<(bool ok, string message)> PlaceOrderAsync(TradePlan plan)
		{
			try
			{
				(bool, string) validation = ValidateTradePlan(plan);
				if (!validation.Item1)
				{
					return Task.FromResult((false, validation.Item2));
				}
				string keyId = ((!string.IsNullOrWhiteSpace(plan.AccountId)) ? _accountService.Get(plan.AccountId) : null)?.KeyEntryId;
				if (string.IsNullOrWhiteSpace(keyId))
				{
					keyId = _keyService.GetActiveId(Service);
				}
				if (string.IsNullOrWhiteSpace(keyId))
				{
					return Task.FromResult((false, "no active coinbase-exchange key selected"));
				}
				KeyInfo keyEntry = _keyService.Get(keyId);
				if (keyEntry == null)
				{
					return Task.FromResult((false, "coinbase-exchange key not found"));
				}
				string apiKey = UnprotectOrRaw((!string.IsNullOrEmpty(keyEntry.ApiKey)) ? keyEntry.ApiKey : keyEntry.Data["ApiKey"]);
				string apiSecret = UnprotectOrRaw((!string.IsNullOrEmpty(keyEntry.ApiSecretBase64)) ? keyEntry.ApiSecretBase64 : ((!string.IsNullOrEmpty(keyEntry.Secret)) ? keyEntry.Secret : keyEntry.Data["ApiSecretBase64"]));
				string passphrase = UnprotectOrRaw((!string.IsNullOrEmpty(keyEntry.Passphrase)) ? keyEntry.Passphrase : keyEntry.Data["Passphrase"]);
				if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
				{
					return Task.FromResult((false, "incomplete coinbase credentials"));
				}
				CoinbaseExchangeClient client = new CoinbaseExchangeClient(apiKey, apiSecret, passphrase);
				OrderRequest order = new OrderRequest
				{
					ProductId = (plan.Symbol ?? string.Empty).Replace("/", "-"),
					Side = ((plan.Direction <= 0) ? OrderSide.Sell : OrderSide.Buy),
					Type = OrderType.Market,
					Quantity = plan.Qty,
					Tif = TimeInForce.GTC,
					ClientOrderId = "cdts-" + Guid.NewGuid().ToString("N")
				};
				return PlaceWithClientAsync(client, order);
			}
			catch (Exception ex)
			{
				Log.Error("coinbase place failed: " + ex.Message, ex, "PlaceOrderAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Brokers\\CoinbaseExchangeBroker.cs", 87);
				return Task.FromResult((false, ex.Message));
			}
		}

		private async Task<(bool ok, string message)> PlaceWithClientAsync(CoinbaseExchangeClient client, OrderRequest order)
		{
			OrderResult result = await client.PlaceOrderAsync(order);
			if (result == null)
			{
				return (ok: false, message: "empty order result");
			}
			if (!result.Accepted)
			{
				return (ok: false, message: string.IsNullOrWhiteSpace(result.Message) ? "order rejected" : result.Message);
			}
			return (ok: true, message: "accepted order=" + (result.OrderId ?? "(unknown)"));
		}

		private string UnprotectOrRaw(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return string.Empty;
			}
			string decrypted = _keyService.Unprotect(value);
			if (!string.IsNullOrWhiteSpace(decrypted))
			{
				return decrypted;
			}
			return value;
		}

		public async Task<(bool ok, string message)> CancelAllAsync(string symbol)
		{
			try
			{
				string active = _keyService.GetActiveId("coinbase-exchange");
				if (string.IsNullOrEmpty(active))
				{
					return (ok: false, message: "no active coinbase-exchange key selected");
				}
				KeyInfo e = _keyService.Get(active);
				if (e == null)
				{
					return (ok: false, message: "active coinbase-exchange key not found");
				}
				string apiKey = _keyService.Unprotect((!string.IsNullOrEmpty(e.ApiKey)) ? e.ApiKey : e.Data["ApiKey"]);
				string apiSecret = _keyService.Unprotect((!string.IsNullOrEmpty(e.ApiSecretBase64)) ? e.ApiSecretBase64 : ((!string.IsNullOrEmpty(e.Secret)) ? e.Secret : e.Data["ApiSecretBase64"]));
				string passphrase = _keyService.Unprotect((!string.IsNullOrEmpty(e.Passphrase)) ? e.Passphrase : e.Data["Passphrase"]);
				if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
				{
					return (ok: false, message: "incomplete coinbase credentials");
				}
				CoinbaseExchangeClient cli = new CoinbaseExchangeClient(apiKey, apiSecret, passphrase);
				List<Dictionary<string, object>> openOrders = await cli.GetOpenOrdersAsync();
				if (openOrders == null)
				{
					return (ok: false, message: "failed to fetch open orders");
				}
				int canceled = 0;
				foreach (Dictionary<string, object> order in openOrders)
				{
					if (order == null || !order.ContainsKey("id"))
					{
						continue;
					}
					string productId = ((!order.ContainsKey("product_id")) ? "" : ((order["product_id"] == null) ? "" : order["product_id"].ToString()));
					if (!string.IsNullOrWhiteSpace(symbol))
					{
						string normalized = symbol.Replace("/", "-");
						if (!string.Equals(productId, normalized, StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}
					}
					if (await cli.CancelOrderAsync(order["id"].ToString()))
					{
						canceled++;
					}
				}
				return (ok: true, message: "canceled=" + canceled);
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Log.Error("coinbase cancel failed: " + ex2.Message, ex2, "CancelAllAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Brokers\\CoinbaseExchangeBroker.cs", 146);
				return (ok: false, message: ex2.Message);
			}
		}
	}
}
