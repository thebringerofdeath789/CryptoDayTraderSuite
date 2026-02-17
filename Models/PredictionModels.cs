/* File: Models/PredictionModels.cs */
/* Author: Gregory King */
/* Date: 2025-08-10 */
/* Description: prediction and analytics models */
/* Types: DirectionPrediction, MagnitudePrediction, PredictionRecord, TradeRecord */

using System;

namespace CryptoDayTraderSuite.Models
{
    public enum MarketDirection { Down = -1, Flat = 0, Up = 1 } /* direction */

    public class DirectionPrediction
    {
        public string ProductId; /* symbol */
        public DateTime AtUtc; /* when predicted */
        public MarketDirection Direction; /* up/down/flat */
        public decimal Probability; /* probability of predicted direction 0..1 */
        public decimal HorizonMinutes; /* minutes ahead */
    }

    public class MagnitudePrediction
    {
        public string ProductId; /* symbol */
        public DateTime AtUtc; /* when predicted */
        public decimal ExpectedReturn; /* expected fractional return over horizon, signed */
        public decimal ExpectedVol; /* expected absolute move */
        public decimal HorizonMinutes; /* minutes */
    }

    public class PredictionRecord
    {
        public string ProductId { get; set; }
        public DateTime AtUtc { get; set; }
        public decimal HorizonMinutes { get; set; }
        public int Direction { get; set; }
        public decimal Probability { get; set; }
        public decimal ExpectedReturn { get; set; }
        public decimal ExpectedVol { get; set; }
        public bool RealizedKnown { get; set; }
        public int RealizedDirection { get; set; }
        public decimal RealizedReturn { get; set; }
    }

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