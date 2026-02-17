using System;
using CryptoDayTraderSuite.Brokers;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;

namespace CryptoDayTraderSuite.UI
{
	public static class BrokerFactory
	{
		public static IBroker GetBroker(string service, AccountMode mode, IKeyService keyService, IAccountService accountService)
		{
			if (string.IsNullOrEmpty(service))
			{
				throw new ArgumentNullException("service");
			}
			if (mode == AccountMode.Paper || service.Equals("paper", StringComparison.OrdinalIgnoreCase))
			{
				return new PaperBroker();
			}
			string text = service.ToLowerInvariant();
			string text2 = text;
			if (text2 == "coinbase-exchange")
			{
				return new CoinbaseExchangeBroker(keyService, accountService);
			}
			return null;
		}
	}
}
