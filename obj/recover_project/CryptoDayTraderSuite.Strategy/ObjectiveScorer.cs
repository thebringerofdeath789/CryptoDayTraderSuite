using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;

namespace CryptoDayTraderSuite.Strategy
{
	public static class ObjectiveScorer
	{
		public static double ToObjectiveUnits(string productId, double expectancyInQuote, TradeObjective objective, string targetAsset, IRateRouter router)
		{
			if (objective == TradeObjective.USDGrowth)
			{
				return expectancyInQuote;
			}
			string[] parts = productId.Split('-');
			if (parts.Length != 2)
			{
				return 0.0;
			}
			string quoteA = parts[1];
			decimal conv = router.Convert(quoteA, targetAsset, (decimal)expectancyInQuote);
			return (double)conv;
		}
	}
}
