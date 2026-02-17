/* File: Strategy/PredictionEngine.cs */
/* Author: Gregory King */
/* Date: 2025-08-10 */
/* Description: online prediction model that learns from outcomes and ranks strategies */
/* Types: PredictionEngine, PredictionModel, RollingStats */

using System;
using System.Collections.Generic;
using System.IO;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Strategy
{
    public class PredictionEngine
    {
        private readonly PredictionModel _model = new PredictionModel(); /* online model */
        private readonly RollingStats _dirStats; /* brier score window */
        private readonly RollingStats _retStats; /* mse on returns */
        private readonly object _sync = new object(); /* lock */

        // Store config to enforce it after load
        private readonly decimal _startLr;
        private readonly decimal _startReg;

        public PredictionEngine(PredictionConfig config = null)
        {
            if (config == null) config = new PredictionConfig();

            /* Phase 9: Configurable ML */
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
                    return new DirectionPrediction { ProductId = productId, AtUtc = DateTime.UtcNow, Direction = MarketDirection.Flat, Probability = 0.5m, HorizonMinutes = horizonMinutes };

                var p = _model.ScoreUpProbability(f); /* prob up */
                var dir = p > 0.55m ? MarketDirection.Up : (p < 0.45m ? MarketDirection.Down : MarketDirection.Flat); /* bucket */
                return new DirectionPrediction { ProductId = productId, AtUtc = DateTime.UtcNow, Direction = dir, Probability = p, HorizonMinutes = horizonMinutes };
            }
        }

        public MagnitudePrediction PredictMagnitude(string productId, Dictionary<string, decimal> f, decimal horizonMinutes)
        {
            lock (_sync)
            {
                if (f == null || f.Count == 0)
                    return new MagnitudePrediction { ProductId = productId, AtUtc = DateTime.UtcNow, ExpectedReturn = 0m, ExpectedVol = 0m, HorizonMinutes = horizonMinutes };

                var mu = _model.ScoreReturn(f); /* expected return */
                var vol = _model.ScoreVolatility(f); /* expected abs move */
                return new MagnitudePrediction { ProductId = productId, AtUtc = DateTime.UtcNow, ExpectedReturn = mu, ExpectedVol = vol, HorizonMinutes = horizonMinutes };
            }
        }

        public void Learn(string productId, Dictionary<string, decimal> f, int realizedDir, decimal realizedRet)
        {
            lock (_sync)
            {
                if (f == null || f.Count == 0) return;
                _model.Update(f, realizedDir, realizedRet); /* update */
                /* update calibration stats if we can recompute probability */
                var pUp = _model.ScoreUpProbability(f);
                var y = realizedDir == 1 ? 1m : 0m;
                var brier = (pUp - y) * (pUp - y);
                _dirStats.Add((double)brier);
                _retStats.Add((double)((realizedRet) * (realizedRet)));
            }
        }

        public decimal CurrentBrier() { lock (_sync) return (decimal)_dirStats.Mean(); } /* brier */
        public decimal CurrentRetMSE() { lock (_sync) return (decimal)_retStats.Mean(); } /* mse */

        public decimal ReliabilityWeight()
        {
            /* convert current calibration error to a [0,1] weight, lower error => higher weight */
            var brier = CurrentBrier(); if (brier <= 0m) return 1m;
            var w = 1m / (1m + 10m * brier); /* simple */
            if (w < 0m) w = 0m; if (w > 1m) w = 1m;
            return w;
        }

        // AUDIT-0013: AI Amnesia Fix
        public string SerializeState()
        {
            lock (_sync)
            {
                return CryptoDayTraderSuite.Util.UtilCompat.JsonSerialize(_model);
            }
        }

        public void LoadState(string json)
        {
            lock (_sync)
            {
                var loaded = CryptoDayTraderSuite.Util.UtilCompat.JsonDeserialize<PredictionModel>(json);
                if (loaded != null)
                {
                    _model.CopyFrom(loaded);
                    /* Enforce config overrides */
                    _model.LearningRate = _startLr;
                    _model.Regularization = _startReg;
                }
            }
        }
    }

    public class PredictionModel
    {
        /* simple online model: logistic regression for up/down and linear for return magnitude/volatility */
        public Dictionary<string, decimal> _wDir = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase); /* weights */
        public decimal _bDir = 0m; /* bias */
        public Dictionary<string, decimal> _wRet = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase); /* weights for return */
        public decimal _bRet = 0m; /* bias for return */
        public Dictionary<string, decimal> _wVol = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase); /* weights for vol */
        public decimal _bVol = 0m; /* bias for vol */

        public void CopyFrom(PredictionModel other)
        {
            if (other == null) return;
            _wDir = new Dictionary<string, decimal>(other._wDir, StringComparer.OrdinalIgnoreCase);
            _bDir = other._bDir;
            _wRet = new Dictionary<string, decimal>(other._wRet, StringComparer.OrdinalIgnoreCase);
            _bRet = other._bRet;
            _wVol = new Dictionary<string, decimal>(other._wVol, StringComparer.OrdinalIgnoreCase);
            _bVol = other._bVol;
            /* Phase 9: Hyperparams */
            LearningRate = other.LearningRate;
            Regularization = other.Regularization;
        }

        /* AUDIT-0014: Configurable Hyperparameters with defaults */
        public decimal LearningRate = 0.05m; 
        public decimal Regularization = 0.0001m; 

        public decimal ScoreUpProbability(Dictionary<string, decimal> f)
        {
            var z = _bDir;
            foreach (var kv in f) { decimal w; _wDir.TryGetValue(kv.Key, out w); z += w * kv.Value; } /* dot */
            var p = Sigmoid(z);
            return p;
        }

        public decimal ScoreReturn(Dictionary<string, decimal> f)
        {
            var y = _bRet; foreach (var kv in f) { decimal w; _wRet.TryGetValue(kv.Key, out w); y += w * kv.Value; } return y; /* linear */
        }

        public decimal ScoreVolatility(Dictionary<string, decimal> f)
        {
            var y = _bVol; foreach (var kv in f) { decimal w; _wVol.TryGetValue(kv.Key, out w); y += w * kv.Value; } return Math.Abs(y); /* abs */
        }

        public void Update(Dictionary<string, decimal> f, int realizedDir, decimal realizedRet)
        {
            var lr = LearningRate;
            var reg = Regularization;

            /* logistic update */
            var p = ScoreUpProbability(f); var y = realizedDir == 1 ? 1m : 0m;
            var grad = (p - y); /* dL/dz for log loss */
            _bDir -= lr * (grad + reg * _bDir);
            foreach (var kv in f)
            {
                decimal w; _wDir.TryGetValue(kv.Key, out w);
                w -= lr * (grad * kv.Value + reg * w);
                _wDir[kv.Key] = w;
            }

            /* linear updates for return + volatility (mse) */
            var predRet = ScoreReturn(f); var errR = predRet - realizedRet;
            _bRet -= lr * (errR + reg * _bRet);
            foreach (var kv in f)
            {
                decimal w; _wRet.TryGetValue(kv.Key, out w);
                w -= lr * (errR * kv.Value + reg * w);
                _wRet[kv.Key] = w;
            }

            var predVol = ScoreVolatility(f); var errV = predVol - Math.Abs(realizedRet);
            _bVol -= lr * (errV + reg * _bVol);
            foreach (var kv in f)
            {
                decimal w; _wVol.TryGetValue(kv.Key, out w);
                w -= lr * (errV * kv.Value + reg * w);
                _wVol[kv.Key] = w;
            }
        }

        // Needed for serialization access
        public PredictionModel() { }

        private static decimal Sigmoid(decimal z)
        {
            var dz = (double)z;
            if (dz > 60d) return 1m;
            if (dz < -60d) return 0m;
            var s = 1.0 / (1.0 + Math.Exp(-dz));
            return (decimal)s;
        }
    }

    public class RollingStats
    {
        private readonly double[] _buf; /* ring buffer */
        private int _idx = 0; /* index */
        private int _count = 0; /* count */
        private double _sum = 0.0; /* sum */

        public RollingStats(int window)
        {
            _buf = new double[Math.Max(1, window)]; /* init */
        }

        public void Add(double x)
        {
            if (_count < _buf.Length)
            {
                _buf[_idx] = x; _sum += x; _idx++; _count++;
            }
            else
            {
                _idx %= _buf.Length;
                _sum -= _buf[_idx];
                _buf[_idx] = x;
                _sum += x;
                _idx++;
            }
        }

        public double Mean()
        {
            if (_count == 0) return 0.0;
            int n = Math.Min(_count, _buf.Length);
            return _sum / n;
        }
    }
}