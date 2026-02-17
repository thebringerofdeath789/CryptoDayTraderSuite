using System.Collections.Generic;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Strategy
{
	public interface IStrategy
	{
		string Name { get; }

		StrategyResult GetSignal(List<Candle> candles);

		StrategyResult GetSignal(List<Candle> candles, int index);
	}
}
