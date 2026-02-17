using System;
using System.Collections.Generic;

namespace CryptoDayTraderSuite.Models.AI
{
	public class MarketContext
	{
		public string Symbol { get; set; }

		public string Interval { get; set; }

		public decimal CurrentPrice { get; set; }

		public decimal VWAP { get; set; }

		public decimal RSI { get; set; }

		public decimal ATR { get; set; }

		public List<SimpleCandle> RecentStructure { get; set; }

		public DateTime TimestampUtc { get; set; }
	}
}
