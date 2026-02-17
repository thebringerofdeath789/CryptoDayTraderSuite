using System.Threading.Tasks;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Brokers
{
    public sealed class BrokerCapabilities
    {
        public bool SupportsMarketEntry = true;
        public bool SupportsProtectiveExits = true;
        public bool EnforcesPrecisionRules = false;
        public string Notes;
    }

    /// <summary>
    /// Broker interface for placing and managing orders.
    /// </summary>
    public interface IBroker
    {
        /// <summary>
        /// The service name (e.g., "coinbase-exchange").
        /// </summary>
        string Service { get; }

        /// <summary>
        /// Broker capability contract used for pre-trade validation and diagnostics.
        /// </summary>
        BrokerCapabilities GetCapabilities();

        /// <summary>
        /// Validate a plan asynchronously against broker-side constraints before order placement.
        /// </summary>
        /// <param name="plan">Trade plan to validate.</param>
        /// <returns>Tuple (ok, message) indicating validation status and details.</returns>
        Task<(bool ok, string message)> ValidateTradePlanAsync(TradePlan plan);

        /// <summary>
        /// Place an order asynchronously.
        /// </summary>
        /// <param name="plan">Trade plan to execute.</param>
        /// <returns>Tuple (ok, message) indicating success and details.</returns>
        Task<(bool ok, string message)> PlaceOrderAsync(TradePlan plan);

        /// <summary>
        /// Cancel all open orders for a symbol asynchronously.
        /// </summary>
        /// <param name="symbol">Symbol to cancel orders for.</param>
        /// <returns>Tuple (ok, message) indicating success and details.</returns>
        Task<(bool ok, string message)> CancelAllAsync(string symbol);
    }
}