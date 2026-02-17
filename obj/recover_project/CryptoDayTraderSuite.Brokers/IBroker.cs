using System.Threading.Tasks;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Brokers
{
	public interface IBroker
	{
		string Service { get; }

		BrokerCapabilities GetCapabilities();

		(bool ok, string message) ValidateTradePlan(TradePlan plan);

		Task<(bool ok, string message)> PlaceOrderAsync(TradePlan plan);

		Task<(bool ok, string message)> CancelAllAsync(string symbol);
	}
}
