
using System;
using System.Collections.Generic;

namespace CryptoDayTraderSuite.Models
{
    public class Candle
    {
        public DateTime Time { get; set; } /* candle time */
        public decimal Open { get; set; } /* o */
        public decimal High { get; set; } /* h */
        public decimal Low { get; set; } /* l */
        public decimal Close { get; set; } /* c */
        public decimal Volume { get; set; } /* v */
    }

    public class Ticker
    {
        public decimal Bid { get; set; } /* bid */
        public decimal Ask { get; set; } /* ask */
        public decimal Last { get; set; } /* last */
        public DateTime Time { get; set; } /* time */
    }

    public class VenueQuoteSnapshot
    {
        public string Venue { get; set; }
        public string Symbol { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public decimal Mid { get; set; }
        public DateTime QuoteTimeUtc { get; set; }
        public DateTime ReceivedTimeUtc { get; set; }
        public int RoundTripMs { get; set; }
        public bool IsStale { get; set; }
        public string Source { get; set; }
    }

    public class CompositeQuote
    {
        public string Symbol { get; set; }
        public decimal Mid { get; set; }
        public DateTime ComputedAtUtc { get; set; }
        public string BestVenue { get; set; }
        public bool IsStale { get; set; }
        public decimal Confidence { get; set; }
        public List<VenueQuoteSnapshot> Venues { get; set; }
    }

    public class VenueHealthSnapshot
    {
        public string Venue { get; set; }
        public DateTime ComputedAtUtc { get; set; }
        public int TotalSamples { get; set; }
        public int SuccessSamples { get; set; }
        public int ErrorSamples { get; set; }
        public int StaleSamples { get; set; }
        public double SuccessRatio { get; set; }
        public double StaleRatio { get; set; }
        public double AvgRoundTripMs { get; set; }
        public double HealthScore { get; set; }
        public bool IsDegraded { get; set; }
        public bool CircuitBreakerOpen { get; set; }
        public string CircuitBreakerReason { get; set; }
        public DateTime? CircuitBreakerOpenedAtUtc { get; set; }
        public DateTime? CircuitBreakerReenableAtUtc { get; set; }
    }

    public class SpreadDivergenceOpportunity
    {
        public string Symbol { get; set; }
        public string BuyVenue { get; set; }
        public string SellVenue { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public decimal GrossSpreadBps { get; set; }
        public decimal NetEdgeBps { get; set; }
        public decimal EstimatedFeeBps { get; set; }
        public decimal EstimatedSlippageBps { get; set; }
        public string RejectReason { get; set; }
        public bool IsExecutable { get; set; }
        public DateTime ComputedAtUtc { get; set; }
    }

    public class FundingRateSnapshot
    {
        public string Venue { get; set; }
        public string Symbol { get; set; }
        public decimal FundingRateBps { get; set; }
        public decimal BasisBps { get; set; }
        public DateTime TimestampUtc { get; set; }
    }

    public class FundingCarryOpportunity
    {
        public string Symbol { get; set; }
        public string LongVenue { get; set; }
        public string ShortVenue { get; set; }
        public decimal ExpectedCarryBps { get; set; }
        public decimal BasisStabilityScore { get; set; }
        public string RejectReason { get; set; }
        public bool IsExecutable { get; set; }
        public DateTime ComputedAtUtc { get; set; }
    }

    public class VenueExecutionScore
    {
        public string Venue { get; set; }
        public string Symbol { get; set; }
        public decimal ExpectedNetEdgeBps { get; set; }
        public decimal FeeDragBps { get; set; }
        public decimal SlippageBudgetBps { get; set; }
        public double HealthScore { get; set; }
        public int LatencyMs { get; set; }
        public bool IsEligible { get; set; }
        public string RejectReason { get; set; }
    }

    public class RoutingDecision
    {
        public string Symbol { get; set; }
        public string ChosenVenue { get; set; }
        public string FallbackVenue { get; set; }
        public decimal ChosenScoreBps { get; set; }
        public List<VenueExecutionScore> RankedVenues { get; set; }
        public string Reason { get; set; }
        public DateTime ComputedAtUtc { get; set; }
    }
}
