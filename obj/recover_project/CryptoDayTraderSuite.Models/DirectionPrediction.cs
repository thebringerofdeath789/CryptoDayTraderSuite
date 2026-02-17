using System;

namespace CryptoDayTraderSuite.Models
{
	public class DirectionPrediction
	{
		public string ProductId;

		public DateTime AtUtc;

		public MarketDirection Direction;

		public decimal Probability;

		public decimal HorizonMinutes;
	}
}
