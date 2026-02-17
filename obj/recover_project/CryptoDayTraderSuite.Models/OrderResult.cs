namespace CryptoDayTraderSuite.Models
{
	public class OrderResult
	{
		public string OrderId { get; set; }

		public bool Accepted { get; set; }

		public bool Filled { get; set; }

		public decimal FilledQty { get; set; }

		public decimal AvgFillPrice { get; set; }

		public string Message { get; set; }
	}
}
