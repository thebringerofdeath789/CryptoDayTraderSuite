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
            if (plan == null) return Task.FromResult((false, BuildFailureMessage("validation", null, "trade plan is null")));
            if (string.IsNullOrWhiteSpace(plan.Symbol)) return Task.FromResult((false, BuildFailureMessage("validation", null, "symbol is required")));
            if (plan.Qty <= 0m) return Task.FromResult((false, BuildFailureMessage("validation", null, "quantity must be greater than zero")));
            if (plan.Entry <= 0m) return Task.FromResult((false, BuildFailureMessage("validation", null, "entry must be greater than zero")));
            if (plan.Stop <= 0m || plan.Target <= 0m) return Task.FromResult((false, BuildFailureMessage("validation", null, "protective stop/target required")));
            if (plan.Direction == 0) return Task.FromResult((false, BuildFailureMessage("validation", null, "direction must be non-zero")));

            if (plan.Direction > 0)
            {
                if (!(plan.Stop < plan.Entry && plan.Entry < plan.Target))
                {
                    return Task.FromResult((false, BuildFailureMessage("validation", null, "invalid long risk geometry (expected stop < entry < target)")));
                }
            }
            else
            {
                if (!(plan.Target < plan.Entry && plan.Entry < plan.Stop))
                {
                    return Task.FromResult((false, BuildFailureMessage("validation", null, "invalid short risk geometry (expected target < entry < stop)")));
                }
            }

            return Task.FromResult((true, BuildSuccessMessage("validation", "ok")));
        }

        public async Task<(bool ok, string message)> PlaceOrderAsync(TradePlan plan)
        {
            var validation = await ValidateTradePlanAsync(plan).ConfigureAwait(false);
            if (!validation.ok) return validation;

            Log.Info("paper fill " + plan.Symbol + " " + (plan.Direction>0?"LONG":"SHORT") + " qty=" + plan.Qty + " at " + plan.Entry + " account=" + plan.AccountId);
            return (true, BuildSuccessMessage("accepted", "paper filled at " + plan.Entry));
        }
        public Task<(bool ok, string message)> CancelAllAsync(string symbol)
        {
            Log.Info("paper cancel all " + symbol);
            return System.Threading.Tasks.Task.FromResult((true, BuildSuccessMessage("canceled", "paper canceled")));
        }

        private string BuildSuccessMessage(string category, string detail)
        {
            return BrokerMessageFormatter.BuildSuccessMessage(category, detail);
        }

        private string BuildFailureMessage(string category, string detail, string fallback)
        {
            return BrokerMessageFormatter.BuildFailureMessage(category, detail, fallback);
        }
    }
}