
namespace CryptoDayTraderSuite.Models
{
    public class FeeSchedule
    {
        public decimal MakerRate { get; set; } /* maker fee rate like 0.0040 for 0.40% */
        public decimal TakerRate { get; set; } /* taker fee rate like 0.0060 for 0.60% */
        public string Notes { get; set; } /* notes */
    }

    public class CostBreakdown
    {
        public decimal EstimatedSpreadRate { get; set; } /* estimated spread as fraction */
        public decimal FeeRateUsed { get; set; } /* maker or taker applied */
        public decimal TotalRoundTripRate { get; set; } /* approx maker+taker+spread */
    }
}
