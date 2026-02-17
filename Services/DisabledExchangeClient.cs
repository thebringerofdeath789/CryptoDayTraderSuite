using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Services
{
    public class DisabledExchangeClient : IExchangeClient
    {
        private readonly string _service;
        private readonly string _reason;

        public DisabledExchangeClient(string service, string reason)
        {
            _service = string.IsNullOrWhiteSpace(service) ? "Unknown" : service;
            _reason = string.IsNullOrWhiteSpace(reason) ? "geo-restricted" : reason;
        }

        public string Name => _service;

        public void SetCredentials(string apiKey, string apiSecret, string passphrase = null)
        {
        }

        public Task<List<string>> ListProductsAsync()
        {
            return Task.FromResult(new List<string>());
        }

        public Task<List<Candle>> GetCandlesAsync(string productId, int granularity, DateTime startUtc, DateTime endUtc)
        {
            return Task.FromResult(new List<Candle>());
        }

        public Task<Ticker> GetTickerAsync(string productId)
        {
            return Task.FromResult(new Ticker { Time = DateTime.UtcNow });
        }

        public Task<FeeSchedule> GetFeesAsync()
        {
            return Task.FromResult(new FeeSchedule
            {
                MakerRate = 0m,
                TakerRate = 0m,
                Notes = "service-disabled: " + _reason
            });
        }

        public Task<OrderResult> PlaceOrderAsync(OrderRequest order)
        {
            return Task.FromResult(new OrderResult
            {
                OrderId = string.Empty,
                Accepted = false,
                Filled = false,
                FilledQty = 0m,
                AvgFillPrice = 0m,
                Message = "service-disabled: " + _reason
            });
        }

        public Task<bool> CancelOrderAsync(string orderId)
        {
            return Task.FromResult(false);
        }

        public Task<List<OpenOrder>> GetOpenOrdersAsync(string productId = null)
        {
            return Task.FromResult(new List<OpenOrder>());
        }

        public Task<Dictionary<string, decimal>> GetBalancesAsync()
        {
            return Task.FromResult(new Dictionary<string, decimal>());
        }
    }
}
