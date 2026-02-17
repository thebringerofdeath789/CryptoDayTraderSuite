using System;

namespace CryptoDayTraderSuite.Models
{
	public class Ticker
	{
		public decimal Bid { get; set; }

		public decimal Ask { get; set; }

		public decimal Last { get; set; }

		public DateTime Time { get; set; }
	}
}
