using System;

namespace CryptoDayTraderSuite.Models.AI
{
	public class SimpleCandle
	{
		public DateTime Time { get; set; }

		public decimal Open { get; set; }

		public decimal High { get; set; }

		public decimal Low { get; set; }

		public decimal Close { get; set; }
	}
}
