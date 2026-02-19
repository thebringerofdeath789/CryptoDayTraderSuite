using System.Threading.Tasks;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;

namespace CryptoDayTraderSuite.Strategy
{
    public static class ObjectiveScorer
    {
        public static async Task<double> ToObjectiveUnitsAsync(string productId, double expectancyInQuote, TradeObjective objective, string targetAsset, IRateRouter router)
        {
            if (objective == TradeObjective.USDGrowth) return expectancyInQuote;
            var parts = productId.Split('-');
            if (parts.Length != 2) return 0.0;
            var quoteA = parts[1];
            var conv = await router.ConvertAsync(quoteA, targetAsset, (decimal)expectancyInQuote).ConfigureAwait(false);
            return (double)conv;
        }
    }
}