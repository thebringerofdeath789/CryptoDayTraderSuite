using System;
using System.Collections.Generic;

namespace CryptoDayTraderSuite.Strategy
{
	public class PredictionModel
	{
		public Dictionary<string, decimal> _wDir = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

		public decimal _bDir = default(decimal);

		public Dictionary<string, decimal> _wRet = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

		public decimal _bRet = default(decimal);

		public Dictionary<string, decimal> _wVol = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

		public decimal _bVol = default(decimal);

		public decimal LearningRate = 0.05m;

		public decimal Regularization = 0.0001m;

		public void CopyFrom(PredictionModel other)
		{
			if (other != null)
			{
				_wDir = new Dictionary<string, decimal>(other._wDir, StringComparer.OrdinalIgnoreCase);
				_bDir = other._bDir;
				_wRet = new Dictionary<string, decimal>(other._wRet, StringComparer.OrdinalIgnoreCase);
				_bRet = other._bRet;
				_wVol = new Dictionary<string, decimal>(other._wVol, StringComparer.OrdinalIgnoreCase);
				_bVol = other._bVol;
				LearningRate = other.LearningRate;
				Regularization = other.Regularization;
			}
		}

		public decimal ScoreUpProbability(Dictionary<string, decimal> f)
		{
			decimal z = _bDir;
			foreach (KeyValuePair<string, decimal> kv in f)
			{
				_wDir.TryGetValue(kv.Key, out var w);
				z += w * kv.Value;
			}
			return Sigmoid(z);
		}

		public decimal ScoreReturn(Dictionary<string, decimal> f)
		{
			decimal y = _bRet;
			foreach (KeyValuePair<string, decimal> kv in f)
			{
				_wRet.TryGetValue(kv.Key, out var w);
				y += w * kv.Value;
			}
			return y;
		}

		public decimal ScoreVolatility(Dictionary<string, decimal> f)
		{
			decimal y = _bVol;
			foreach (KeyValuePair<string, decimal> kv in f)
			{
				_wVol.TryGetValue(kv.Key, out var w);
				y += w * kv.Value;
			}
			return Math.Abs(y);
		}

		public void Update(Dictionary<string, decimal> f, int realizedDir, decimal realizedRet)
		{
			decimal lr = LearningRate;
			decimal reg = Regularization;
			decimal p = ScoreUpProbability(f);
			decimal y = ((realizedDir == 1) ? 1m : 0m);
			decimal grad = p - y;
			_bDir -= lr * (grad + reg * _bDir);
			foreach (KeyValuePair<string, decimal> kv in f)
			{
				_wDir.TryGetValue(kv.Key, out var w);
				w -= lr * (grad * kv.Value + reg * w);
				_wDir[kv.Key] = w;
			}
			decimal predRet = ScoreReturn(f);
			decimal errR = predRet - realizedRet;
			_bRet -= lr * (errR + reg * _bRet);
			foreach (KeyValuePair<string, decimal> kv2 in f)
			{
				_wRet.TryGetValue(kv2.Key, out var w2);
				w2 -= lr * (errR * kv2.Value + reg * w2);
				_wRet[kv2.Key] = w2;
			}
			decimal predVol = ScoreVolatility(f);
			decimal errV = predVol - Math.Abs(realizedRet);
			_bVol -= lr * (errV + reg * _bVol);
			foreach (KeyValuePair<string, decimal> kv3 in f)
			{
				_wVol.TryGetValue(kv3.Key, out var w3);
				w3 -= lr * (errV * kv3.Value + reg * w3);
				_wVol[kv3.Key] = w3;
			}
		}

		private static decimal Sigmoid(decimal z)
		{
			double dz = (double)z;
			if (dz > 60.0)
			{
				return 1m;
			}
			if (dz < -60.0)
			{
				return 0m;
			}
			double s = 1.0 / (1.0 + Math.Exp(0.0 - dz));
			return (decimal)s;
		}
	}
}
