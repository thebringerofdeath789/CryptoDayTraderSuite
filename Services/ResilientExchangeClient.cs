using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Services
{
    /// <summary>
    /// A decorator for IExchangeClient that adds retry logic with exponential backoff.
    /// </summary>
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
            // Be careful retrying orders - only if we are sure it wasn't placed.
            // For safety in this simpler implementation, we might NOT retry PlaceOrder to avoid duplicates,
            // OR we retry only on connection failures where the request definitely didn't leave.
            // Given the risk of verifying execution, we will skip retry for PlaceOrderAsync 
            // unless we can verify state. For now, let's pass specific "false" to retry.
            // Actually, network glitch could mean it posted. Safest is NO RETRY on PlaceOrder.
            return _inner.PlaceOrderAsync(order);
        }

        public Task<bool> CancelOrderAsync(string orderId)
        {
            // Cancelling is idempotent-ish. If it's already done, it fails safe.
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

                    // Exponential Backoff: 500ms, 1000ms, 2000ms...
                    int delay = _baseDelayMs * (int)Math.Pow(2, attempt - 1);
                    Log.Error($"[Resilience] {Name} Attempt {attempt} failed: {ex.Message}. Retrying in {delay}ms...");
                    await Task.Delay(delay);
                }
            }
        }

        private bool IsTransient(Exception ex)
        {
            // Handle common HTTP timeouts/connectivity issues
            if (ex is System.Net.WebException) return true;
            if (ex is System.Net.Http.HttpRequestException) return true;
            if (ex is TimeoutException) return true;
            var msg = ex != null && ex.Message != null ? ex.Message.ToLowerInvariant() : string.Empty;
            if (msg.Contains("429") || msg.Contains("too many requests")) return true;
            
            // Assume 500/502/503/504 in message logic if strictly parsing, 
            // but for now, base types are the main catch.
            return false;
        }
    }
}
