using System;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Services
{
    public sealed class ExecutionCostAssumptions
    {
        public string ExecutionMode { get; set; }
        public decimal RoundTripFeeBps { get; set; }
        public decimal SlippageBps { get; set; }
        public decimal RoundTripTotalBps { get; set; }
        public decimal FeeTierAdjustmentBps { get; set; }
        public decimal RebateBps { get; set; }
    }

    public class ExecutionCostModelService
    {
        private const decimal DefaultMakerRate = 0.004m;
        private const decimal DefaultTakerRate = 0.006m;
        private const decimal DefaultMakerSlipBps = 3m;
        private const decimal DefaultTakerSlipBps = 7m;

        public ExecutionCostAssumptions Build(string venue, FeeSchedule feeSchedule)
        {
            var mode = ResolveExecutionMode();
            var fees = feeSchedule ?? new FeeSchedule { MakerRate = DefaultMakerRate, TakerRate = DefaultTakerRate };

            var makerRate = ClampNonNegative(fees.MakerRate > 0m ? fees.MakerRate : DefaultMakerRate);
            var takerRate = ClampNonNegative(fees.TakerRate > 0m ? fees.TakerRate : DefaultTakerRate);

            var feeTierAdjBps = GetEnvDecimal("CDTS_FEE_TIER_ADJ_BPS", 0m);
            var rebateBps = GetEnvDecimal("CDTS_FEE_REBATE_BPS", 0m);
            var venueAdjBps = GetEnvDecimal("CDTS_FEE_EXTRA_BPS_" + NormalizeVenueKey(venue), 0m);

            decimal rawRoundTripFeeBps;
            if (string.Equals(mode, "maker-preferred", StringComparison.OrdinalIgnoreCase))
            {
                rawRoundTripFeeBps = RateToBps(makerRate + takerRate);
            }
            else
            {
                rawRoundTripFeeBps = RateToBps(takerRate + takerRate);
            }

            var adjustedRoundTripFeeBps = rawRoundTripFeeBps + feeTierAdjBps + venueAdjBps - rebateBps;
            if (adjustedRoundTripFeeBps < 0m) adjustedRoundTripFeeBps = 0m;

            var defaultSlip = string.Equals(mode, "maker-preferred", StringComparison.OrdinalIgnoreCase)
                ? DefaultMakerSlipBps
                : DefaultTakerSlipBps;

            var slippageBps = GetEnvDecimal("CDTS_SLIPPAGE_BASE_BPS", defaultSlip);
            var venueSlipAdjBps = GetEnvDecimal("CDTS_SLIPPAGE_EXTRA_BPS_" + NormalizeVenueKey(venue), 0m);
            slippageBps = ClampNonNegative(slippageBps + venueSlipAdjBps);

            return new ExecutionCostAssumptions
            {
                ExecutionMode = mode,
                RoundTripFeeBps = Math.Round(adjustedRoundTripFeeBps, 4),
                SlippageBps = Math.Round(slippageBps, 4),
                RoundTripTotalBps = Math.Round(adjustedRoundTripFeeBps + slippageBps, 4),
                FeeTierAdjustmentBps = Math.Round(feeTierAdjBps + venueAdjBps, 4),
                RebateBps = Math.Round(rebateBps, 4)
            };
        }

        private string ResolveExecutionMode()
        {
            var raw = (Environment.GetEnvironmentVariable("CDTS_EXECUTION_MODE") ?? string.Empty).Trim().ToLowerInvariant();
            if (raw == "taker-only" || raw == "taker") return "taker-only";
            return "maker-preferred";
        }

        private decimal GetEnvDecimal(string name, decimal fallback)
        {
            var raw = Environment.GetEnvironmentVariable(name);
            decimal parsed;
            if (!string.IsNullOrWhiteSpace(raw)
                && decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private static decimal ClampNonNegative(decimal value)
        {
            return value < 0m ? 0m : value;
        }

        private static decimal RateToBps(decimal rate)
        {
            return rate * 10000m;
        }

        private static string NormalizeVenueKey(string venue)
        {
            return (venue ?? string.Empty)
                .Trim()
                .ToUpperInvariant()
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("_", string.Empty);
        }
    }
}
