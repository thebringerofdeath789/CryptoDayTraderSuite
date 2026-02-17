using System;

namespace CryptoDayTraderSuite.Models
{
	public class TradeRecord
	{
		public string Exchange { get; set; }

		public string ProductId { get; set; }

		public DateTime AtUtc { get; set; }

		public string Strategy { get; set; }

		public string Side { get; set; }

		public decimal Quantity { get; set; }

		public decimal Price { get; set; }

		public decimal EstEdge { get; set; }

		public bool Executed { get; set; }

		public decimal? FillPrice { get; set; }

		public decimal? PnL { get; set; }

		public string Notes { get; set; }

		public bool Enabled { get; set; }
	}
}
