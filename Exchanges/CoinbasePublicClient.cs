using System;
using System.Collections.Generic;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Exchanges
{
    public class CoinbasePublicClient
    {
        private const string Base = "https://api.exchange.coinbase.com";

        public async System.Threading.Tasks.Task<List<string>> GetProductsAsync()
        {
            return await HttpUtil.RetryAsync(async () =>
            {
                var url = Base + "/products";
                var json = await HttpUtil.GetAsync(url).ConfigureAwait(false);
                var arr = UtilCompat.JsonDeserialize<List<Product>>(json) ?? new List<Product>();
                var res = new List<string>();
                foreach (var p in arr)
                {
                    if (!string.IsNullOrEmpty(p.id)) res.Add(p.id);
                }
                res.Sort(StringComparer.OrdinalIgnoreCase);
                return res;
            }).ConfigureAwait(false);
        }

        public async System.Threading.Tasks.Task<List<CandleRow>> GetCandlesAsync(string productId, int granSeconds, DateTime startUtc, DateTime endUtc)
        {
            var outRows = new List<CandleRow>();
            var step = TimeSpan.FromSeconds(granSeconds * 300);
            var t = startUtc;
            var tasks = new List<System.Threading.Tasks.Task<string>>();
            
            // Batching creates improved performance but we must limit concurrency to avoid 429s (10 req/sec)
            // For simplicity in this async refactor, we serialize the chunks but asyncly.
            
            while (t < endUtc)
            {
                var t2 = t + step;
                if (t2 > endUtc) t2 = endUtc;
                var url = Base + "/products/" + Uri.EscapeDataString(productId) + "/candles?granularity=" + granSeconds
                        + "&start=" + Uri.EscapeDataString(t.ToString("o"))
                        + "&end=" + Uri.EscapeDataString(t2.ToString("o"));
                
                try 
                {
                    // Delay slightly to be nice to rate limits
                    await System.Threading.Tasks.Task.Delay(50).ConfigureAwait(false);
                    var json = await HttpUtil.RetryAsync(() => HttpUtil.GetAsync(url)).ConfigureAwait(false);
                    var arr = UtilCompat.JsonDeserialize<List<List<double>>>(json) ?? new List<List<double>>();
                    foreach (var row in arr)
                    {
                        if (row.Count >= 6)
                        {
                            var ts = DateTimeOffset.FromUnixTimeSeconds((long)row[0]).UtcDateTime;
                            outRows.Add(new CandleRow { TimeUtc = ts, Low = (decimal)row[1], High = (decimal)row[2], Open = (decimal)row[3], Close = (decimal)row[4], Volume = (decimal)row[5] });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn("[CoinbasePublicClient] Candle chunk failed for " + productId + " (" + t.ToString("o") + " -> " + t2.ToString("o") + "): " + ex.Message);
                }
                
                t = t2;
            }
            outRows.Sort((a,b) => a.TimeUtc.CompareTo(b.TimeUtc));
            return outRows;
        }

        public async System.Threading.Tasks.Task<decimal> GetTickerMidAsync(string productId)
        {
            try
            {
                var url = Base + "/products/" + Uri.EscapeDataString(productId) + "/ticker";
                var json = await HttpUtil.RetryAsync(() => HttpUtil.GetAsync(url)).ConfigureAwait(false);
                var t = UtilCompat.JsonDeserialize<Ticker>(json);
                if (t == null) return 0m;
                decimal bid = 0m, ask = 0m, price = 0m;
                decimal.TryParse(t.bid ?? "0", out bid);
                decimal.TryParse(t.ask ?? "0", out ask);
                decimal.TryParse(t.price ?? "0", out price);
                if (bid > 0m && ask > 0m) return (bid + ask) / 2m;
                return price;
            }
            catch (Exception ex)
            {
                Log.Warn("[CoinbasePublicClient] Ticker fetch failed for " + productId + ": " + ex.Message);
                return 0m;
            }
        }

        // Legacy synchronous methods kept for compilation until full migration, forwarding to async-wait
        public List<string> GetProducts() => GetProductsAsync().Result;
        public List<CandleRow> GetCandles(string productId, int granSeconds, DateTime startUtc, DateTime endUtc) => GetCandlesAsync(productId, granSeconds, startUtc, endUtc).Result;
        public decimal GetTickerMid(string productId) => GetTickerMidAsync(productId).Result;


        private class Product { public string id { get; set; } }
        private class Ticker { public string price { get; set; } public string bid { get; set; } public string ask { get; set; } }

        public class CandleRow
        {
            public DateTime TimeUtc;
            public decimal Open;
            public decimal High;
            public decimal Low;
            public decimal Close;
            public decimal Volume;
        }
    }
}