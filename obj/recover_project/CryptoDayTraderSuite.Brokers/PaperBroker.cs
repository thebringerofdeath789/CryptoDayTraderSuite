using System.Threading.Tasks;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Brokers
{
	public class PaperBroker : IBroker
	{
		public string Service => "paper";

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
				return (ok: false, message: "quantity must be greater than zero");
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
			(bool, string) validation = ValidateTradePlan(plan);
			if (!validation.Item1)
			{
				return Task.FromResult((false, validation.Item2));
			}
			Log.Info("paper fill " + plan.Symbol + " " + ((plan.Direction > 0) ? "LONG" : "SHORT") + " qty=" + plan.Qty + " at " + plan.Entry + " account=" + plan.AccountId, "PlaceOrderAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Brokers\\PaperBroker.cs", 37);
			return Task.FromResult((true, "paper filled at " + plan.Entry));
		}

		public Task<(bool ok, string message)> CancelAllAsync(string symbol)
		{
			Log.Info("paper cancel all " + symbol, "CancelAllAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Brokers\\PaperBroker.cs", 42);
			return Task.FromResult((true, "paper canceled"));
		}
	}
}
