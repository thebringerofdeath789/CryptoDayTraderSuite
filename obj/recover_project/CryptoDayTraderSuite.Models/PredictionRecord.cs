using System;

namespace CryptoDayTraderSuite.Models
{
	public class PredictionRecord
	{
		public string ProductId { get; set; }

		public DateTime AtUtc { get; set; }

		public decimal HorizonMinutes { get; set; }

		public int Direction { get; set; }

		public decimal Probability { get; set; }

		public decimal ExpectedReturn { get; set; }

		public decimal ExpectedVol { get; set; }

		public bool RealizedKnown { get; set; }

		public int RealizedDirection { get; set; }

		public decimal RealizedReturn { get; set; }
	}
}
