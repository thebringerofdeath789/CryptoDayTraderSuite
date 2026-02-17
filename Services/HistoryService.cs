/* File: Services/HistoryService.cs */
/* Refactored from HistoryStore into Instance Service */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Services
{
    public class HistoryService : IHistoryService
    {
        private string Root()
        {
            var app = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(app, "CryptoDayTraderSuite");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        private string PredPath() { return Path.Combine(Root(), "predictions.csv"); }
        private string TradePath() { return Path.Combine(Root(), "trades.csv"); }
        private string PlannedTradesPath() { return Path.Combine(Root(), "planned_trades.csv"); }

        public void SavePrediction(PredictionRecord r)
        {
            bool writeHeader = !File.Exists(PredPath());
            using (var sw = new StreamWriter(PredPath(), true))
            {
                if (writeHeader) sw.WriteLine("product,at_utc,horizon_min,dir,prob,exp_ret,exp_vol,realized_known,realized_dir,realized_ret");
                sw.WriteLine(string.Join(",", new string[] {
                    r.ProductId,
                    r.AtUtc.ToString("o"),
                    r.HorizonMinutes.ToString(CultureInfo.InvariantCulture),
                    r.Direction.ToString(CultureInfo.InvariantCulture),
                    r.Probability.ToString(CultureInfo.InvariantCulture),
                    r.ExpectedReturn.ToString(CultureInfo.InvariantCulture),
                    r.ExpectedVol.ToString(CultureInfo.InvariantCulture),
                    r.RealizedKnown ? "1" : "0",
                    r.RealizedDirection.ToString(CultureInfo.InvariantCulture),
                    r.RealizedReturn.ToString(CultureInfo.InvariantCulture)
                }));
            }
        }

        public void SaveTrade(TradeRecord t)
        {
            bool writeHeader = !File.Exists(TradePath());
            using (var sw = new StreamWriter(TradePath(), true))
            {
                if (writeHeader) sw.WriteLine("exchange,product,at_utc,strategy,side,qty,price,edge,executed,fill_price,pnl,notes,enabled");
                sw.WriteLine(string.Join(",", new string[] {
                    Esc(t.Exchange), Esc(t.ProductId), t.AtUtc.ToString("o"), Esc(t.Strategy), Esc(t.Side),
                    t.Quantity.ToString(CultureInfo.InvariantCulture),
                    t.Price.ToString(CultureInfo.InvariantCulture),
                    t.EstEdge.ToString(CultureInfo.InvariantCulture),
                    t.Executed ? "1":"0",
                    t.FillPrice.HasValue ? t.FillPrice.Value.ToString(CultureInfo.InvariantCulture) : "",
                    t.PnL.HasValue ? t.PnL.Value.ToString(CultureInfo.InvariantCulture) : "",
                    Esc(t.Notes),
                    t.Enabled ? "1":"0"
                }));
            }
        }

        public List<PredictionRecord> LoadPredictions()
        {
            var list = new List<PredictionRecord>();
            var p = PredPath(); if (!File.Exists(p)) return list;
            using (var sr = new StreamReader(p))
            {
                string line = sr.ReadLine(); /* header */
                while ((line = sr.ReadLine()) != null)
                {
                    try
                    {
                        var sp = SplitCsv(line);
                        if (sp.Length < 10) continue;

                        DateTime atUtc;
                        decimal horizonMinutes;
                        int direction;
                        decimal probability;
                        decimal expectedReturn;
                        decimal expectedVol;
                        int realizedDirection = 0;
                        decimal realizedReturn = 0m;

                        if (!DateTime.TryParse(sp[1], null, DateTimeStyles.RoundtripKind, out atUtc)) continue;
                        if (!decimal.TryParse(sp[2], NumberStyles.Any, CultureInfo.InvariantCulture, out horizonMinutes)) continue;
                        if (!int.TryParse(sp[3], NumberStyles.Any, CultureInfo.InvariantCulture, out direction)) continue;
                        if (!decimal.TryParse(sp[4], NumberStyles.Any, CultureInfo.InvariantCulture, out probability)) continue;
                        if (!decimal.TryParse(sp[5], NumberStyles.Any, CultureInfo.InvariantCulture, out expectedReturn)) continue;
                        if (!decimal.TryParse(sp[6], NumberStyles.Any, CultureInfo.InvariantCulture, out expectedVol)) continue;

                        if (!string.IsNullOrEmpty(sp[8])) int.TryParse(sp[8], NumberStyles.Any, CultureInfo.InvariantCulture, out realizedDirection);
                        if (!string.IsNullOrEmpty(sp[9])) decimal.TryParse(sp[9], NumberStyles.Any, CultureInfo.InvariantCulture, out realizedReturn);

                        var r = new PredictionRecord();
                        r.ProductId = sp[0];
                        r.AtUtc = atUtc;
                        r.HorizonMinutes = horizonMinutes;
                        r.Direction = direction;
                        r.Probability = probability;
                        r.ExpectedReturn = expectedReturn;
                        r.ExpectedVol = expectedVol;
                        r.RealizedKnown = sp[7] == "1";
                        r.RealizedDirection = realizedDirection;
                        r.RealizedReturn = realizedReturn;
                        list.Add(r);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            return list;
        }

        public void SavePlannedTrades(IEnumerable<TradeRecord> trades)
        {
            using (var sw = new StreamWriter(PlannedTradesPath(), false))
            {
                sw.WriteLine("exchange,product,at_utc,strategy,side,qty,price,edge,executed,fill_price,pnl,notes,enabled");
                foreach (var t in trades)
                {
                    sw.WriteLine(string.Join(",", new string[] {
                        Esc(t.Exchange), Esc(t.ProductId), t.AtUtc.ToString("o"), Esc(t.Strategy), Esc(t.Side),
                        t.Quantity.ToString(CultureInfo.InvariantCulture),
                        t.Price.ToString(CultureInfo.InvariantCulture),
                        t.EstEdge.ToString(CultureInfo.InvariantCulture),
                        t.Executed ? "1":"0",
                        t.FillPrice.HasValue ? t.FillPrice.Value.ToString(CultureInfo.InvariantCulture) : "",
                        t.PnL.HasValue ? t.PnL.Value.ToString(CultureInfo.InvariantCulture) : "",
                        Esc(t.Notes),
                        t.Enabled ? "1":"0"
                    }));
                }
            }
        }

        public List<TradeRecord> LoadPlannedTrades()
        {
            return LoadTradesInternal(PlannedTradesPath());
        }

        public List<TradeRecord> LoadTrades()
        {
            return LoadTradesInternal(TradePath());
        }

        private List<TradeRecord> LoadTradesInternal(string path)
        {
            var list = new List<TradeRecord>();
            if (!File.Exists(path)) return list;
            using (var sr = new StreamReader(path))
            {
                string line = sr.ReadLine(); /* header */
                while ((line = sr.ReadLine()) != null)
                {
                    try
                    {
                        var sp = SplitCsv(line);
                        if (sp.Length < 13) continue;

                        DateTime atUtc;
                        decimal qty;
                        decimal price;
                        decimal estEdge;

                        if (!DateTime.TryParse(sp[2], null, DateTimeStyles.RoundtripKind, out atUtc)) continue;
                        if (!decimal.TryParse(sp[5], NumberStyles.Any, CultureInfo.InvariantCulture, out qty)) continue;
                        if (!decimal.TryParse(sp[6], NumberStyles.Any, CultureInfo.InvariantCulture, out price)) continue;
                        if (!decimal.TryParse(sp[7], NumberStyles.Any, CultureInfo.InvariantCulture, out estEdge)) continue;

                        decimal fillPriceVal;
                        decimal pnlVal;

                        var t = new TradeRecord();
                        t.Exchange = Unesc(sp[0]); t.ProductId = Unesc(sp[1]);
                        t.AtUtc = atUtc;
                        t.Strategy = Unesc(sp[3]); t.Side = Unesc(sp[4]);
                        t.Quantity = qty;
                        t.Price = price;
                        t.EstEdge = estEdge;
                        t.Executed = sp[8] == "1";
                        t.FillPrice = !string.IsNullOrEmpty(sp[9]) && decimal.TryParse(sp[9], NumberStyles.Any, CultureInfo.InvariantCulture, out fillPriceVal) ? (decimal?)fillPriceVal : null;
                        t.PnL = !string.IsNullOrEmpty(sp[10]) && decimal.TryParse(sp[10], NumberStyles.Any, CultureInfo.InvariantCulture, out pnlVal) ? (decimal?)pnlVal : null;
                        t.Notes = Unesc(sp[11]);
                        t.Enabled = sp[12] == "1";
                        list.Add(t);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            return list;
        }

        private string Esc(string s) { if (s == null) return ""; if (s.Contains(",") || s.Contains("\"")) return "\"" + s.Replace("\"","\"\"") + "\""; return s; }
        private string Unesc(string s) { s = s ?? ""; s = s.Trim(); if (s.StartsWith("\"") && s.EndsWith("\"")) s = s.Substring(1, s.Length - 2).Replace("\"\"","\""); return s; }

        private string[] SplitCsv(string line)
        {
            var list = new List<string>();
            bool inq = false; var cur = new System.Text.StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (inq)
                {
                    if (ch == '"' && i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
                    else if (ch == '"') { inq = false; }
                    else cur.Append(ch);
                }
                else
                {
                    if (ch == ',') { list.Add(cur.ToString()); cur.Clear(); }
                    else if (ch == '"') { inq = true; }
                    else cur.Append(ch);
                }
            }
            list.Add(cur.ToString());
            return list.ToArray();
        }
    }
}