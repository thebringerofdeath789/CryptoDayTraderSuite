using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Strategy
{
	public class StrategyResult
	{
		public bool IsSignal { get; set; }

		public OrderSide Side { get; set; }

		public decimal EntryPrice { get; set; }

		public decimal StopLoss { get; set; }

		public decimal TakeProfit { get; set; }

		public string StrategyName { get; set; }

		public double ConfidenceScore { get; set; }
	}
}
