namespace CryptoDayTraderSuite.Models.AI
{
	public class TradePreview
	{
		public string Strategy { get; set; }

		public string Symbol { get; set; }

		public string Side { get; set; }

		public decimal Entry { get; set; }

		public decimal Stop { get; set; }

		public decimal Target { get; set; }

		public string Rationale { get; set; }
	}
}
