namespace CryptoDayTraderSuite.Models
{
	public class OrderRequest
	{
		public string ProductId { get; set; }

		public OrderSide Side { get; set; }

		public OrderType Type { get; set; }

		public decimal Quantity { get; set; }

		public decimal? Price { get; set; }

		public decimal? StopLoss { get; set; }

		public decimal? TakeProfit { get; set; }

		public TimeInForce Tif { get; set; }

		public string ClientOrderId { get; set; }
	}
}
