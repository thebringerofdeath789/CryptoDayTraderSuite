using System;
using System.Collections.Generic;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Strategy
{
	public class PredictionEngine
	{
		private readonly PredictionModel _model = new PredictionModel();

		private readonly RollingStats _dirStats;

		private readonly RollingStats _retStats;

		private readonly object _sync = new object();

		private readonly decimal _startLr;

		private readonly decimal _startReg;

		public PredictionEngine(PredictionConfig config = null)
		{
			if (config == null)
			{
				config = new PredictionConfig();
			}
			_startLr = config.LearningRate;
			_startReg = config.L2Regularization;
			_model.LearningRate = _startLr;
			_model.Regularization = _startReg;
			_dirStats = new RollingStats(config.RollingEventsWindow);
			_retStats = new RollingStats(config.RollingEventsWindow);
		}

		public DirectionPrediction PredictDirection(string productId, Dictionary<string, decimal> f, decimal horizonMinutes)
		{
			lock (_sync)
			{
				if (f == null || f.Count == 0)
				{
					return new DirectionPrediction
					{
						ProductId = productId,
						AtUtc = DateTime.UtcNow,
						Direction = MarketDirection.Flat,
						Probability = 0.5m,
						HorizonMinutes = horizonMinutes
					};
				}
				decimal p = _model.ScoreUpProbability(f);
				MarketDirection dir = ((p > 0.55m) ? MarketDirection.Up : ((p < 0.45m) ? MarketDirection.Down : MarketDirection.Flat));
				return new DirectionPrediction
				{
					ProductId = productId,
					AtUtc = DateTime.UtcNow,
					Direction = dir,
					Probability = p,
					HorizonMinutes = horizonMinutes
				};
			}
		}

		public MagnitudePrediction PredictMagnitude(string productId, Dictionary<string, decimal> f, decimal horizonMinutes)
		{
			lock (_sync)
			{
				if (f == null || f.Count == 0)
				{
					return new MagnitudePrediction
					{
						ProductId = productId,
						AtUtc = DateTime.UtcNow,
						ExpectedReturn = default(decimal),
						ExpectedVol = default(decimal),
						HorizonMinutes = horizonMinutes
					};
				}
				decimal mu = _model.ScoreReturn(f);
				decimal vol = _model.ScoreVolatility(f);
				return new MagnitudePrediction
				{
					ProductId = productId,
					AtUtc = DateTime.UtcNow,
					ExpectedReturn = mu,
					ExpectedVol = vol,
					HorizonMinutes = horizonMinutes
				};
			}
		}

		public void Learn(string productId, Dictionary<string, decimal> f, int realizedDir, decimal realizedRet)
		{
			lock (_sync)
			{
				if (f != null && f.Count != 0)
				{
					_model.Update(f, realizedDir, realizedRet);
					decimal pUp = _model.ScoreUpProbability(f);
					decimal y = ((realizedDir == 1) ? 1m : 0m);
					decimal brier = (pUp - y) * (pUp - y);
					_dirStats.Add((double)brier);
					_retStats.Add((double)(realizedRet * realizedRet));
				}
			}
		}

		public decimal CurrentBrier()
		{
			lock (_sync)
			{
				return (decimal)_dirStats.Mean();
			}
		}

		public decimal CurrentRetMSE()
		{
			lock (_sync)
			{
				return (decimal)_retStats.Mean();
			}
		}

		public decimal ReliabilityWeight()
		{
			decimal brier = CurrentBrier();
			if (brier <= 0m)
			{
				return 1m;
			}
			decimal w = 1m / (1m + 10m * brier);
			if (w < 0m)
			{
				w = default(decimal);
			}
			if (w > 1m)
			{
				w = 1m;
			}
			return w;
		}

		public string SerializeState()
		{
			lock (_sync)
			{
				return UtilCompat.JsonSerialize(_model);
			}
		}

		public void LoadState(string json)
		{
			lock (_sync)
			{
				PredictionModel loaded = UtilCompat.JsonDeserialize<PredictionModel>(json);
				if (loaded != null)
				{
					_model.CopyFrom(loaded);
					_model.LearningRate = _startLr;
					_model.Regularization = _startReg;
				}
			}
		}
	}
}
