using System.Collections.Generic;
using CryptoDayTraderSuite.Backtest;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Services
{
	public class BacktestResultWrapper
	{
		public Backtester.Result RunResult { get; set; }

		public List<Candle> Candles { get; set; }

		public string Error { get; set; }
	}
}
