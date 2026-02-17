namespace CryptoDayTraderSuite.Models
{
	public class ProjectionInput
	{
		public decimal StartingEquity { get; set; }

		public int TradesPerDay { get; set; }

		public decimal WinRate { get; set; }

		public decimal AvgWinR { get; set; }

		public decimal AvgLossR { get; set; }

		public decimal RiskPerTradeFraction { get; set; }

		public decimal NetFeeAndFrictionRate { get; set; }

		public int Days { get; set; }
	}
}
