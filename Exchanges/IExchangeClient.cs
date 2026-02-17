using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Exchanges
{
    /// <summary>
    /// Exchange client interface for market data and trading.
    /// </summary>
    public interface IExchangeClient
    {
        /// <summary>
        /// Exchange name (e.g., "Coinbase", "Kraken").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// List available products/markets.
        /// </summary>
        /// <returns>List of product symbols.</returns>
        Task<List<string>> ListProductsAsync();

        /// <summary>
        /// Get historical candles for a product.
        /// </summary>
        /// <param name="productId">Product symbol.</param>
        /// <param name="granularity">Candle interval in minutes.</param>
        /// <param name="startUtc">Start time (UTC).</param>
        /// <param name="endUtc">End time (UTC).</param>
        /// <returns>List of candles.</returns>
        Task<List<Candle>> GetCandlesAsync(string productId, int granularity, DateTime startUtc, DateTime endUtc);

        /// <summary>
        /// Get current ticker for a product.
        /// </summary>
        /// <param name="productId">Product symbol.</param>
        /// <returns>Ticker info.</returns>
        Task<Ticker> GetTickerAsync(string productId);

        /// <summary>
        /// Get current fee schedule.
        /// </summary>
        /// <returns>Fee schedule.</returns>
        Task<FeeSchedule> GetFeesAsync();

        /// <summary>
        /// Place an order asynchronously.
        /// </summary>
        /// <param name="order">Order request.</param>
        /// <returns>Order result.</returns>
        Task<OrderResult> PlaceOrderAsync(OrderRequest order);

        /// <summary>
        /// Cancel an order asynchronously.
        /// </summary>
        /// <param name="orderId">Order ID.</param>
        /// <returns>True if canceled.</returns>
        Task<bool> CancelOrderAsync(string orderId);

        /// <summary>
        /// Set API credentials for the client.
        /// </summary>
        /// <param name="apiKey">API key.</param>
        /// <param name="apiSecret">API secret.</param>
        /// <param name="passphrase">Optional passphrase.</param>
        void SetCredentials(string apiKey, string apiSecret, string passphrase = null);
    }
}
