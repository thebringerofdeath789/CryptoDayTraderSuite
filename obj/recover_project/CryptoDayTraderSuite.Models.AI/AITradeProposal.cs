namespace CryptoDayTraderSuite.Models.AI
{
	public class AITradeProposal
	{
		public bool Approve { get; set; }

		public string Symbol { get; set; }

		public string Side { get; set; }

		public decimal? Entry { get; set; }

		public decimal? Stop { get; set; }

		public decimal? Target { get; set; }

		public string StrategyHint { get; set; }

		public string Reason { get; set; }

		public decimal Confidence { get; set; }
	}
}
