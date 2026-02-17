using System;

namespace CryptoDayTraderSuite.Brokers
{
    internal static class BrokerPrecision
    {
        private const decimal MinimumTolerance = 0.0000000001m;

        public static decimal AlignDownToStep(decimal quantity, decimal stepSize)
        {
            if (quantity <= 0m || stepSize <= 0m)
            {
                return 0m;
            }

            var steps = Math.Floor(quantity / stepSize);
            var aligned = stepSize * steps;
            if (aligned < 0m)
            {
                return 0m;
            }

            return aligned;
        }

        public static bool IsAlignedToStep(decimal value, decimal stepSize)
        {
            if (value <= 0m || stepSize <= 0m)
            {
                return false;
            }

            var aligned = AlignDownToStep(value, stepSize);
            var tolerance = GetStepAlignmentTolerance(stepSize);
            return Math.Abs(value - aligned) <= tolerance;
        }

        private static decimal GetStepAlignmentTolerance(decimal stepSize)
        {
            if (stepSize <= 0m)
            {
                return 0m;
            }

            var precision = GetDecimalPrecision(stepSize);
            var scaledTolerance = stepSize / Pow10(precision + 2);
            return scaledTolerance > MinimumTolerance ? scaledTolerance : MinimumTolerance;
        }

        private static int GetDecimalPrecision(decimal value)
        {
            var bits = decimal.GetBits(value);
            return (bits[3] >> 16) & 0x7F;
        }

        private static decimal Pow10(int exponent)
        {
            if (exponent <= 0)
            {
                return 1m;
            }

            var result = 1m;
            for (var i = 0; i < exponent; i++)
            {
                result *= 10m;
            }

            return result;
        }
    }
}