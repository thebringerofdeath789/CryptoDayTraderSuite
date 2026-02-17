using System;
using System.Collections.Generic;

namespace CryptoDayTraderSuite.Models
{
	[Serializable]
	public class AutoModeProfile
	{
		public string ProfileId;

		public string Name;

		public string AccountId;

		public bool Enabled = true;

		public string PairScope = "Selected";

		public List<string> SelectedPairs = new List<string>();

		public int IntervalMinutes = 5;

		public int MaxTradesPerCycle = 3;

		public int CooldownMinutes = 30;

		public decimal DailyRiskStopPct = 3m;

		public DateTime CreatedUtc = DateTime.UtcNow;

		public DateTime UpdatedUtc = DateTime.UtcNow;
	}
}
