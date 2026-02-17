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
        private readonly string _serviceKey;

        public string Name => _inner.Name;

        public ResilientExchangeClient(IExchangeClient inner, int maxRetries = 3, int baseDelayMs = 500, string serviceKey = null)
        {
            _inner = inner;
            _maxRetries = maxRetries;
            _baseDelayMs = baseDelayMs;
            _serviceKey = string.IsNullOrWhiteSpace(serviceKey) ? _inner.Name : serviceKey;
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

        public async Task<OrderResult> PlaceOrderAsync(OrderRequest order)
        {
            try
            {
                return await _inner.PlaceOrderAsync(order).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (GeoBlockRegistry.TryDisableFromException(_serviceKey, ex, "place-order"))
                {
                    return new OrderResult
                    {
                        OrderId = string.Empty,
                        Accepted = false,
                        Filled = false,
                        FilledQty = 0m,
                        AvgFillPrice = 0m,
                        Message = "service-disabled: " + GeoBlockRegistry.GetDisableReason(_serviceKey)
                    };
                }

                throw;
            }
        }

        public Task<bool> CancelOrderAsync(string orderId)
        {
            // Cancelling is idempotent-ish. If it's already done, it fails safe.
            return ExecuteWithRetry(() => _inner.CancelOrderAsync(orderId));
        }

        public Task<List<OpenOrder>> GetOpenOrdersAsync(string productId = null)
        {
            return ExecuteWithRetry(() => _inner.GetOpenOrdersAsync(productId));
        }

        public Task<Dictionary<string, decimal>> GetBalancesAsync()
        {
            return ExecuteWithRetry(() => _inner.GetBalancesAsync());
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
                    if (GeoBlockRegistry.TryDisableFromException(_serviceKey, ex, "resilient-call"))
                    {
                        return CreateGeoDisabledResult<T>();
                    }

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

        private T CreateGeoDisabledResult<T>()
        {
            var type = typeof(T);
            if (type == typeof(List<string>))
            {
                return (T)(object)new List<string>();
            }

            if (type == typeof(List<Candle>))
            {
                return (T)(object)new List<Candle>();
            }

            if (type == typeof(List<OpenOrder>))
            {
                return (T)(object)new List<OpenOrder>();
            }

            if (type == typeof(Ticker))
            {
                return (T)(object)new Ticker { Time = DateTime.UtcNow };
            }

            if (type == typeof(FeeSchedule))
            {
                return (T)(object)new FeeSchedule
                {
                    MakerRate = 0m,
                    TakerRate = 0m,
                    Notes = "service-disabled: " + GeoBlockRegistry.GetDisableReason(_serviceKey)
                };
            }

            if (type == typeof(bool))
            {
                return (T)(object)false;
            }

            if (type == typeof(Dictionary<string, decimal>))
            {
                return (T)(object)new Dictionary<string, decimal>();
            }

            if (type == typeof(OrderResult))
            {
                return (T)(object)new OrderResult
                {
                    OrderId = string.Empty,
                    Accepted = false,
                    Filled = false,
                    FilledQty = 0m,
                    AvgFillPrice = 0m,
                    Message = "service-disabled: " + GeoBlockRegistry.GetDisableReason(_serviceKey)
                };
            }

            return default(T);
        }
    }
}
