using System;
using System.Collections.Generic;

namespace CryptoDayTraderSuite.Models.AI
{
    public class MarketContext
    {
        public string Symbol { get; set; }
        public string Interval { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal VWAP { get; set; }
        public decimal RSI { get; set; }
        public decimal ATR { get; set; }
        public List<SimpleCandle> RecentStructure { get; set; }
        public DateTime TimestampUtc { get; set; }
    }

    public class SimpleCandle
    {
        public DateTime Time { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
    }

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

    public class AIResponse
    {
        /* Standardized JSON output we expect from the LLM */
        public string Bias { get; set; } /* "Bullish", "Bearish", "Neutral" */
        public bool Approve { get; set; }
        public string Reason { get; set; }
        public decimal Confidence { get; set; } /* 0.0 to 1.0 */
        public decimal? SuggestedLimit { get; set; }
    }

    public class AITradeProposal
    {
        public bool Approve { get; set; }
        public string Symbol { get; set; }
        public string Side { get; set; }
        public decimal? Entry { get; set; }
        public decimal? Stop { get; set; }
        public decimal? Target { get; set; }
        public string StrategyHint { get; set; }
        public string Reason { get; set; }
        public decimal Confidence { get; set; }
    }
}
