using System;

namespace CryptoDayTraderSuite.Models
{

    public class TradePlan
    {
        public string PlanId { get; set; }
        public string Strategy { get; set; }
        public string Symbol { get; set; }
        public int GranMinutes { get; set; }
        public int Direction { get; set; }
        public decimal Entry { get; set; }
        public decimal Stop { get; set; }
        public decimal Target { get; set; }
        public decimal Qty { get; set; }
        public string Note { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string AccountId { get; set; }
    }

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