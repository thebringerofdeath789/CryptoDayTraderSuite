using CryptoDayTraderSuite.Exchanges;

namespace CryptoDayTraderSuite.Services
{
	public interface IExchangeProvider
	{
		IExchangeClient CreateAuthenticatedClient(string brokerName);

		IExchangeClient CreatePublicClient(string brokerName);
	}
}
