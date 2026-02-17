using System.Threading.Tasks;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Brokers
{
    public class PaperBroker : IBroker
    {
        public string Service { get { return "paper"; } }

        public BrokerCapabilities GetCapabilities()
        {
            return new BrokerCapabilities
            {
                SupportsMarketEntry = true,
                SupportsProtectiveExits = true,
                EnforcesPrecisionRules = false,
                Notes = "Paper broker simulates fills and accepts validated plans."
            };
        }

        public Task<(bool ok, string message)> ValidateTradePlanAsync(TradePlan plan)
        {
            if (plan == null) return Task.FromResult((false, "trade plan is null"));
            if (string.IsNullOrWhiteSpace(plan.Symbol)) return Task.FromResult((false, "symbol is required"));
            if (plan.Qty <= 0m) return Task.FromResult((false, "quantity must be greater than zero"));
            if (plan.Entry <= 0m) return Task.FromResult((false, "entry must be greater than zero"));
            if (plan.Stop <= 0m || plan.Target <= 0m) return Task.FromResult((false, "protective stop/target required"));
            if (plan.Direction == 0) return Task.FromResult((false, "direction must be non-zero"));

            if (plan.Direction > 0)
            {
                if (!(plan.Stop < plan.Entry && plan.Entry < plan.Target))
                {
                    return Task.FromResult((false, "invalid long risk geometry (expected stop < entry < target)"));
                }
            }
            else
            {
                if (!(plan.Target < plan.Entry && plan.Entry < plan.Stop))
                {
                    return Task.FromResult((false, "invalid short risk geometry (expected target < entry < stop)"));
                }
            }

            return Task.FromResult((true, "ok"));
        }

        public async Task<(bool ok, string message)> PlaceOrderAsync(TradePlan plan)
        {
            var validation = await ValidateTradePlanAsync(plan).ConfigureAwait(false);
            if (!validation.ok) return (false, validation.message);

            Log.Info("paper fill " + plan.Symbol + " " + (plan.Direction>0?"LONG":"SHORT") + " qty=" + plan.Qty + " at " + plan.Entry + " account=" + plan.AccountId);
            return (true, "paper filled at " + plan.Entry);
        }
        public Task<(bool ok, string message)> CancelAllAsync(string symbol)
        {
            Log.Info("paper cancel all " + symbol);
            return System.Threading.Tasks.Task.FromResult((true, "paper canceled"));
        }
    }
}