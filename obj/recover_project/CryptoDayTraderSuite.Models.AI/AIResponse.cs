namespace CryptoDayTraderSuite.Models.AI
{
	public class AIResponse
	{
		public string Bias { get; set; }

		public bool Approve { get; set; }

		public string Reason { get; set; }

		public decimal Confidence { get; set; }

		public decimal? SuggestedLimit { get; set; }
	}
}
