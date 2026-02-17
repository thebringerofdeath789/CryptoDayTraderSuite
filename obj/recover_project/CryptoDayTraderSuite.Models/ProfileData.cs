using System;
using System.Collections.Generic;

namespace CryptoDayTraderSuite.Models
{
	[Serializable]
	public class ProfileData
	{
		public List<AccountInfo> Accounts = new List<AccountInfo>();

		public List<KeyInfo> Keys = new List<KeyInfo>();

		public List<int> BlockedHours = new List<int>();

		public decimal DefaultRiskPct = 0.5m;

		public string QuoteFilter = "USD";

		public int DefaultGranMinutes = 15;
	}
}
