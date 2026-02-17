using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Exchanges
{
	public interface IExchangeClient
	{
		string Name { get; }

		Task<List<string>> ListProductsAsync();

		Task<List<Candle>> GetCandlesAsync(string productId, int granularity, DateTime startUtc, DateTime endUtc);

		Task<Ticker> GetTickerAsync(string productId);

		Task<FeeSchedule> GetFeesAsync();

		Task<OrderResult> PlaceOrderAsync(OrderRequest order);

		Task<bool> CancelOrderAsync(string orderId);

		void SetCredentials(string apiKey, string apiSecret, string passphrase = null);
	}
}
