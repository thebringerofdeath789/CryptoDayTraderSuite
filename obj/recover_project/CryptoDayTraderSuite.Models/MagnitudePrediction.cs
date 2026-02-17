using System;

namespace CryptoDayTraderSuite.Models
{
	public class MagnitudePrediction
	{
		public string ProductId;

		public DateTime AtUtc;

		public decimal ExpectedReturn;

		public decimal ExpectedVol;

		public decimal HorizonMinutes;
	}
}
