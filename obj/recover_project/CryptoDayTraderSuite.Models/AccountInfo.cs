using System;

namespace CryptoDayTraderSuite.Models
{
	[Serializable]
	public class AccountInfo
	{
		public string Id;

		public string Broker;

		public string DisplayName;

		public string DefaultQuote;

		public bool Paper;

		public decimal MaxOrderPct;

		public DateTime CreatedUtc = DateTime.UtcNow;

		public bool Enabled = true;

		public DateTime UpdatedUtc = DateTime.UtcNow;

		public AccountMode Mode = AccountMode.Live;

		public decimal RiskPerTradePct = 1m;

		public int MaxConcurrentTrades = 3;

		public string KeyEntryId;

		public string Label
		{
			get
			{
				return DisplayName;
			}
			set
			{
				DisplayName = value;
			}
		}

		public string Service
		{
			get
			{
				return Broker;
			}
			set
			{
				Broker = value;
			}
		}
	}
}
