/* File: MainForm_PredictionHook.cs */
/* Author: Gregory King */
/* Date: 2025-08-10 */
/* Description: hooks for running predictions, planning trades, and showing planner form */
/* Functions: AnalyzeAndPlanAsync, btnPlanner_Click */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Strategy;
using CryptoDayTraderSuite.Services;

namespace CryptoDayTraderSuite
{
    public partial class MainForm
    {
        private readonly PredictionEngine _predict = new PredictionEngine(new PredictionConfig()); /* prediction */
        private readonly TradePlanner _planner = new TradePlanner(); /* planner */
        private UI.PlannerForm _plannerForm = null; /* window */

        private Task AnalyzeAndPlanAsync(string product, List<Candle> candles)
        {
            if (candles == null || candles.Count < 30 || string.IsNullOrWhiteSpace(product)) return Task.CompletedTask;

            /* compute features */
            Dictionary<string, decimal> f;
            if (!FeatureExtractor.TryComputeFeatures(candles, out f)) return Task.CompletedTask;

            /* predict next 5 min move */
            var dir = _predict.PredictDirection(product, f, 5m); /* direction */
            var mag = _predict.PredictMagnitude(product, f, 5m); /* magnitude */

            /* save prediction */
            var rec = new PredictionRecord
            {
                ProductId = product,
                AtUtc = DateTime.UtcNow,
                HorizonMinutes = 5m,
                Direction = (int)dir.Direction,
                Probability = dir.Probability,
                ExpectedReturn = mag.ExpectedReturn,
                ExpectedVol = mag.ExpectedVol,
                RealizedKnown = false,
                RealizedDirection = 0,
                RealizedReturn = 0m
            };
            if (_historyService != null) _historyService.SavePrediction(rec); /* persist */

            /* build trade candidates from our strategies using expected edge = prob-adjusted return minus friction */
            _planner.Clear(); /* reset */

            var fees = _fees; /* from main form */
            var friction = fees.MakerRate + fees.TakerRate + 0.0005m; /* est spread */

            /* candidate: ORB continuation */
            if (cmbStrategy.Items.Contains("ORB"))
            {
                var edge = mag.ExpectedReturn * dir.Probability - friction; /* naive edge */
                var side = edge >= 0m ? "buy" : "sell";
                _planner.AddCandidate(new TradeRecord
                {
                    Exchange = _client != null ? _client.Name : "n/a",
                    ProductId = product,
                    AtUtc = DateTime.UtcNow,
                    Strategy = "ORB",
                    Side = side,
                    Quantity = 0.0m, /* size decided later */
                    Price = candles[candles.Count - 1].Close,
                    EstEdge = Math.Abs(edge),
                    Executed = false,
                    Notes = "auto from prediction"
                });
            }

            /* candidate: VWAP reversion */
            if (cmbStrategy.Items.Contains("VWAPTrend"))
            {
                var edge = (-mag.ExpectedReturn) * (1m - dir.Probability) - friction;
                var side = edge >= 0m ? "sell" : "buy";
                _planner.AddCandidate(new TradeRecord
                {
                    Exchange = _client != null ? _client.Name : "n/a",
                    ProductId = product,
                    AtUtc = DateTime.UtcNow,
                    Strategy = "VWAPTrend",
                    Side = side,
                    Quantity = 0.0m,
                    Price = candles[candles.Count - 1].Close,
                    EstEdge = Math.Abs(edge),
                    Executed = false,
                    Notes = "auto from prediction"
                });
            }

            /* open planner window if requested */
            if (_plannerForm != null && !_plannerForm.IsDisposed)
            {
                _planner.ReapplyAll(); /* reapply rules */
                _plannerForm.SetData(_planner.Planned.ToList(), _historyService != null ? _historyService.LoadPredictions() : new List<PredictionRecord>());
            }

            return Task.CompletedTask;
        }

        private void LearnFromCandles(string product, List<Candle> candles)
        {
            if (candles == null || candles.Count < 40 || string.IsNullOrWhiteSpace(product)) return;

            int start = 20;
            int end = candles.Count - 2;
            for (int i = start; i <= end; i++)
            {
                try
                {
                    var window = candles.GetRange(0, i + 1);
                    Dictionary<string, decimal> f;
                    if (!FeatureExtractor.TryComputeFeatures(window, out f)) continue;

                    var curr = candles[i].Close;
                    var next = candles[i + 1].Close;
                    if (curr <= 0m) continue;

                    var ret = (next - curr) / curr;
                    int dir = ret > 0m ? 1 : -1;
                    _predict.Learn(product, f, dir, ret);
                }
                catch
                {
                    continue;
                }
            }
        }

        public void LoadAI()
        {
            try
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CryptoDayTraderSuite");
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                var path = System.IO.Path.Combine(dir, "prediction_model.json");
                if (System.IO.File.Exists(path))
                {
                    _predict.LoadState(System.IO.File.ReadAllText(path));
                    Log("AI model loaded.");
                }
            }
            catch (Exception ex) { Log("AI load failed: " + ex.Message); }
        }

        public void SaveAI()
        {
            try
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CryptoDayTraderSuite");
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                var path = System.IO.Path.Combine(dir, "prediction_model.json");
                System.IO.File.WriteAllText(path, _predict.SerializeState());
                Log("AI model saved.");
            }
            catch (Exception ex) { Log("AI save failed: " + ex.Message); }
        }

        private void btnPlanner_Click(object sender, EventArgs e)
        {
            NavigateTo("Planner");
        }

        public void OpenPlanner()
        {
            NavigateTo("Planner");
        }
    }
}