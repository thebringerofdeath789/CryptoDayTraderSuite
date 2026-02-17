using System;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;

namespace CryptoDayTraderSuite.Brokers
{
    public static class BrokerFactory
    {
        public static IBroker GetBroker(string service, AccountMode mode, IKeyService keyService, IAccountService accountService)
        {
            if (string.IsNullOrEmpty(service))
                throw new ArgumentNullException(nameof(service));

            if (mode == AccountMode.Paper || service.Equals("paper", StringComparison.OrdinalIgnoreCase))
                return new PaperBroker();

            switch (service.ToLowerInvariant())
            {
                case "coinbase-exchange":
                case "coinbase-advanced":
                    return new CoinbaseExchangeBroker(keyService, accountService);
                case "binance":
                case "binance-us":
                case "binance-global":
                    return new BinanceBroker(keyService, accountService);
                case "bybit":
                case "bybit-global":
                    return new BybitBroker(keyService, accountService);
                case "okx":
                case "okx-global":
                    return new OkxBroker(keyService, accountService);
                default:
                    return null;
            }
        }
    }
}
