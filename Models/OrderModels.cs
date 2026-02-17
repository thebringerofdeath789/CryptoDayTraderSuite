
using System;

namespace CryptoDayTraderSuite.Models
{
    public class OrderRequest
    {
        public string ProductId { get; set; } /* pair like BTC-USD or XBTUSD */
        public OrderSide Side { get; set; } /* side */
        public OrderType Type { get; set; } /* type */
        public decimal Quantity { get; set; } /* qty */
        public decimal? Price { get; set; } /* limit price */
        public decimal? StopLoss { get; set; } /* optional protective stop */
        public decimal? TakeProfit { get; set; } /* optional target */
        public TimeInForce Tif { get; set; } /* tif */
        public string ClientOrderId { get; set; } /* client id */
    }

    public class OrderResult
    {
        public string OrderId { get; set; } /* order id */
        public bool Accepted { get; set; } /* accepted */
        public bool Filled { get; set; } /* filled */
        public decimal FilledQty { get; set; } /* fill qty */
        public decimal AvgFillPrice { get; set; } /* avg price */
        public string Message { get; set; } /* message */
    }

    public class OpenOrder
    {
        public string OrderId { get; set; }
        public string ProductId { get; set; }
        public OrderSide Side { get; set; }
        public OrderType Type { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal FilledQty { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string Status { get; set; }
    }

    public class Position
    {
        public string ProductId { get; set; } /* product */
        public decimal Qty { get; set; } /* qty */
        public decimal AvgPrice { get; set; } /* avg */
        public decimal UnrealizedPnL(decimal mark) { return (mark - AvgPrice) * Qty; } /* calc */
    }

    public class SymbolConstraints
    {
        public string Symbol { get; set; }
        public decimal MinQty { get; set; }
        public decimal MaxQty { get; set; }
        public decimal StepSize { get; set; }
        public decimal MinNotional { get; set; }
        public decimal PriceTickSize { get; set; }
        public string Source { get; set; }
    }
}
