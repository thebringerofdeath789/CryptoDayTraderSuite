namespace CryptoDayTraderSuite.Brokers
{
	public sealed class BrokerCapabilities
	{
		public bool SupportsMarketEntry = true;

		public bool SupportsProtectiveExits = true;

		public bool EnforcesPrecisionRules = false;

		public string Notes;
	}
}
