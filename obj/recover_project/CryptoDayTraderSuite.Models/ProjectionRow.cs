namespace CryptoDayTraderSuite.Models
{
	public class ProjectionRow
	{
		public string Strategy { get; set; }

		public string Symbol { get; set; }

		public int GranMinutes { get; set; }

		public double Expectancy { get; set; }

		public double WinRate { get; set; }

		public double AvgWin { get; set; }

		public double AvgLoss { get; set; }

		public double SharpeApprox { get; set; }

		public int Samples { get; set; }
	}
}
