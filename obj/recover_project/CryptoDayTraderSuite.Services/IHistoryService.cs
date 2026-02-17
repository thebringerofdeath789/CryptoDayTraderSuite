using System.Collections.Generic;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Services
{
	public interface IHistoryService
	{
		void SavePrediction(PredictionRecord r);

		void SaveTrade(TradeRecord t);

		List<PredictionRecord> LoadPredictions();

		void SavePlannedTrades(IEnumerable<TradeRecord> trades);

		List<TradeRecord> LoadPlannedTrades();

		List<TradeRecord> LoadTrades();
	}
}
