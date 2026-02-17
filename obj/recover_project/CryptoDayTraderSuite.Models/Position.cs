namespace CryptoDayTraderSuite.Models
{
	public class Position
	{
		public string ProductId { get; set; }

		public decimal Qty { get; set; }

		public decimal AvgPrice { get; set; }

		public decimal UnrealizedPnL(decimal mark)
		{
			return (mark - AvgPrice) * Qty;
		}
	}
}
