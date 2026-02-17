using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Services
{
	public class ResilientExchangeClient : IExchangeClient
	{
		private readonly IExchangeClient _inner;

		private readonly int _maxRetries;

		private readonly int _baseDelayMs;

		public string Name => _inner.Name;

		public ResilientExchangeClient(IExchangeClient inner, int maxRetries = 3, int baseDelayMs = 500)
		{
			_inner = inner;
			_maxRetries = maxRetries;
			_baseDelayMs = baseDelayMs;
		}

		public void SetCredentials(string apiKey, string apiSecret, string passphrase = null)
		{
			_inner.SetCredentials(apiKey, apiSecret, passphrase);
		}

		public Task<List<string>> ListProductsAsync()
		{
			return ExecuteWithRetry(() => _inner.ListProductsAsync());
		}

		public Task<List<Candle>> GetCandlesAsync(string productId, int granularity, DateTime startUtc, DateTime endUtc)
		{
			return ExecuteWithRetry(() => _inner.GetCandlesAsync(productId, granularity, startUtc, endUtc));
		}

		public Task<Ticker> GetTickerAsync(string productId)
		{
			return ExecuteWithRetry(() => _inner.GetTickerAsync(productId));
		}

		public Task<FeeSchedule> GetFeesAsync()
		{
			return ExecuteWithRetry(() => _inner.GetFeesAsync());
		}

		public Task<OrderResult> PlaceOrderAsync(OrderRequest order)
		{
			return _inner.PlaceOrderAsync(order);
		}

		public Task<bool> CancelOrderAsync(string orderId)
		{
			return ExecuteWithRetry(() => _inner.CancelOrderAsync(orderId));
		}

		private async Task<T> ExecuteWithRetry<T>(Func<Task<T>> action)
		{
			int attempt = 0;
			while (true)
			{
				try
				{
					attempt++;
					return await action();
				}
				catch (Exception ex)
				{
					if (attempt > _maxRetries || !IsTransient(ex))
					{
						throw;
					}
					int delay = _baseDelayMs * (int)Math.Pow(2.0, attempt - 1);
					Log.Error($"[Resilience] {Name} Attempt {attempt} failed: {ex.Message}. Retrying in {delay}ms...", null, "ExecuteWithRetry", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\ResilientExchangeClient.cs", 89);
					await Task.Delay(delay);
				}
			}
		}

		private bool IsTransient(Exception ex)
		{
			if (ex is WebException)
			{
				return true;
			}
			if (ex is HttpRequestException)
			{
				return true;
			}
			if (ex is TimeoutException)
			{
				return true;
			}
			return false;
		}
	}
}
