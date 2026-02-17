using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Exchanges
{
    public class KrakenClient : IExchangeClient
    {
        private string _key;
        private string _secretBase64;
        private const string Rest = "https://api.kraken.com";
        private static readonly HttpClient _http = new HttpClient();

        public KrakenClient(string key, string secretBase64)
        {
            _key = key;
            _secretBase64 = secretBase64;
        }

        public string Name => "Kraken";

        public void SetCredentials(string apiKey, string apiSecret, string passphrase = null)
        {
            _key = apiKey;
            _secretBase64 = apiSecret;
        }

        public string NormalizeProduct(string uiSymbol)
        {
            var s = uiSymbol.Replace("-", "").Replace("/", "").ToUpperInvariant();
            if (s == "BTCUSD" || s == "XBTUSD") return "XXBTZUSD";
            if (s == "ETHUSD") return "XETHZUSD";
            if (s == "SOLUSD") return "SOLUSD";
            return s;
        }

        public string DenormalizeProduct(string apiSymbol)
        {
            if (apiSymbol == "XXBTZUSD") return "BTC/USD";
            if (apiSymbol == "XETHZUSD") return "ETH/USD";
            if (apiSymbol == "SOLUSD") return "SOL/USD";
            return apiSymbol;
        }

        private async Task<string> PrivatePostAsync(string path, Dictionary<string, string> form)
        {
            return await HttpUtil.RetryAsync(async () =>
            {
                var nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                var signForm = form ?? new Dictionary<string, string>();
                signForm["nonce"] = nonce;

                var post = new FormUrlEncodedContent(signForm);
                var postStr = await post.ReadAsStringAsync();

                var shaData = SecurityUtil.Sha256(Encoding.UTF8.GetBytes(nonce + postStr));
                var toSign = Encoding.UTF8.GetBytes(path);

                var message = new byte[toSign.Length + shaData.Length];
                Buffer.BlockCopy(toSign, 0, message, 0, toSign.Length);
                Buffer.BlockCopy(shaData, 0, message, toSign.Length, shaData.Length);

                var sig = SecurityUtil.ComputeHmacSha512Base64(_secretBase64, message);
                var url = Rest + path;

                using (var req = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    req.Headers.Add("API-Key", _key);
                    req.Headers.Add("API-Sign", sig);
                    req.Headers.Add("User-Agent", "CryptoDayTraderSuite");
                    req.Content = post;

                    return await HttpUtil.SendAsync(req);
                }
            });
        }

        public Task<List<string>> ListProductsAsync()
        {
            return Task.FromResult(new List<string> { "BTC/USD", "ETH/USD", "SOL/USD" });
        }

        public async Task<List<Candle>> GetCandlesAsync(string productId, int granularity, DateTime startUtc, DateTime endUtc)
        {
            return await HttpUtil.RetryAsync(async () =>
            {
                var pair = NormalizeProduct(productId);
                var interval = granularity;
                if (interval < 1) interval = 1;
                if (interval > 1440) interval = 1440;

                var url = $"{Rest}/0/public/OHLC?pair={pair}&interval={interval}";
                var json = await HttpUtil.GetAsync(url);
                var dict = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
                var result = new List<Candle>();

                if (dict != null && dict.ContainsKey("result"))
                {
                    var res = dict["result"] as Dictionary<string, object>;
                    if (res != null)
                    {
                        foreach (var k in res.Keys)
                        {
                            if (k == "last") continue;
                            var arr = res[k] as object[];
                            // Handle ArrayList fallback
                            if (arr == null)
                            {
                                var al = res[k] as System.Collections.ArrayList;
                                if (al != null) arr = al.ToArray();
                            }

                            if (arr == null) continue;

                            foreach (var o in arr)
                            {
                                var row = o as object[];
                                if (row == null)
                                {
                                    var rowAl = o as System.Collections.ArrayList;
                                    if (rowAl != null) row = rowAl.ToArray();
                                }

                                if (row == null || row.Length < 6) continue;

                                double epochSeconds;
                                if (!TryParseDoubleInvariant(row[0], out epochSeconds)) continue;
                                var ts = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epochSeconds);
                                if (ts < startUtc || ts > endUtc) continue;

                                decimal open;
                                decimal high;
                                decimal low;
                                decimal close;
                                decimal volume;
                                if (!TryParseDecimalInvariant(row[1], out open)) continue;
                                if (!TryParseDecimalInvariant(row[2], out high)) continue;
                                if (!TryParseDecimalInvariant(row[3], out low)) continue;
                                if (!TryParseDecimalInvariant(row[4], out close)) continue;
                                if (!TryParseDecimalInvariant(row[6], out volume)) continue;

                                result.Add(new Candle
                                {
                                    Time = ts,
                                    Open = open,
                                    High = high,
                                    Low = low,
                                    Close = close,
                                    Volume = volume
                                });
                            }
                        }
                    }
                }
                result.Sort((a, b) => a.Time.CompareTo(b.Time));
                return result;
            });
        }

        public async Task<Ticker> GetTickerAsync(string productId)
        {
            return await HttpUtil.RetryAsync(async () =>
            {
                var pair = NormalizeProduct(productId);
                var url = $"{Rest}/0/public/Ticker?pair={pair}";
                var json = await HttpUtil.GetAsync(url);
                var dict = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
                var t = new Ticker { Time = DateTime.UtcNow };

                if (dict != null && dict.ContainsKey("result"))
                {
                    var res = dict["result"] as Dictionary<string, object>;
                    if (res != null)
                    {
                        foreach (var k in res.Keys)
                        {
                            var obj = res[k] as Dictionary<string, object>;
                            if (obj != null)
                            {
                                object[] a = null;
                                object[] b = null;
                                object[] c = null;

                                if (obj.ContainsKey("a"))
                                {
                                    a = obj["a"] as object[];
                                    if (a == null)
                                    {
                                        var aList = obj["a"] as System.Collections.ArrayList;
                                        if (aList != null) a = aList.ToArray();
                                    }
                                }

                                if (obj.ContainsKey("b"))
                                {
                                    b = obj["b"] as object[];
                                    if (b == null)
                                    {
                                        var bList = obj["b"] as System.Collections.ArrayList;
                                        if (bList != null) b = bList.ToArray();
                                    }
                                }

                                if (obj.ContainsKey("c"))
                                {
                                    c = obj["c"] as object[];
                                    if (c == null)
                                    {
                                        var cList = obj["c"] as System.Collections.ArrayList;
                                        if (cList != null) c = cList.ToArray();
                                    }
                                }

                                decimal parsed;
                                if (a != null && a.Length > 0 && TryParseDecimalInvariant(a[0], out parsed)) t.Ask = parsed;
                                if (b != null && b.Length > 0 && TryParseDecimalInvariant(b[0], out parsed)) t.Bid = parsed;
                                if (c != null && c.Length > 0 && TryParseDecimalInvariant(c[0], out parsed)) t.Last = parsed;
                                break;
                            }
                        }
                    }
                }
                return t;
            });
        }

        public async Task<FeeSchedule> GetFeesAsync()
        {
            var body = new Dictionary<string, string> { { "fee-info", "true" } };
            // PrivatePostAsync already wraps RetryAsync
            var json = await PrivatePostAsync("/0/private/TradeVolume", body);
            var obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);

            var maker = 0.0025m;
            var taker = 0.0040m;

            try
            {
                if (obj != null && obj.ContainsKey("result"))
                {
                    var res = obj["result"] as Dictionary<string, object>;
                    if (res != null)
                    {
                        if (res.ContainsKey("fees_maker"))
                        {
                            var fm = res["fees_maker"] as Dictionary<string, object>;
                            if (fm != null)
                            {
                                foreach (var key in fm.Keys)
                                {
                                    var d = fm[key] as Dictionary<string, object>;
                                    if (d != null && d.ContainsKey("fee"))
                                    {
                                        decimal parsed;
                                        if (TryParseDecimalInvariant(d["fee"], out parsed)) maker = parsed / 100m;
                                    }
                                    break;
                                }
                            }
                        }
                        if (res.ContainsKey("fees"))
                        {
                            var ft = res["fees"] as Dictionary<string, object>;
                            if (ft != null)
                            {
                                foreach (var key in ft.Keys)
                                {
                                    var d = ft[key] as Dictionary<string, object>;
                                    if (d != null && d.ContainsKey("fee"))
                                    {
                                        decimal parsed;
                                        if (TryParseDecimalInvariant(d["fee"], out parsed)) taker = parsed / 100m;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return new FeeSchedule { MakerRate = maker, TakerRate = taker, Notes = "from TradeVolume" };
        }

        public async Task<Dictionary<string, decimal>> GetBalancesAsync()
        {
            var json = await PrivatePostAsync("/0/private/Balance", new Dictionary<string, string>());
            var obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
            var d = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            if (obj != null && obj.ContainsKey("result"))
            {
                var res = obj["result"] as Dictionary<string, object>;
                if (res != null)
                {
                    foreach (var k in res.Keys)
                    {
                        decimal parsed;
                        if (TryParseDecimalInvariant(res[k], out parsed))
                        {
                            d[k] = parsed;
                        }
                        else
                        {
                            Log.Warn("[Kraken] Skipping balance value parse for asset '" + k + "'.");
                        }
                    }
                }
            }
            return d;
        }

        public async Task<OrderResult> PlaceOrderAsync(OrderRequest req)
        {
            var form = new Dictionary<string, string>();
            form["pair"] = NormalizeProduct(req.ProductId);
            form["type"] = req.Side == OrderSide.Buy ? "buy" : "sell";
            form["ordertype"] = req.Type == OrderType.Market ? "market" : "limit";
            form["volume"] = req.Quantity.ToString(CultureInfo.InvariantCulture);
            if (req.Type == OrderType.Limit && req.Price.HasValue)
                form["price"] = req.Price.Value.ToString(CultureInfo.InvariantCulture);

            var json = await PrivatePostAsync("/0/private/AddOrder", form);
            var obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);

            var accepted = !(obj.ContainsKey("error") && (obj["error"] as object[])?.Length > 0);
            var id = "";
            if (obj.ContainsKey("result"))
            {
                var res = obj["result"] as Dictionary<string, object>;
                if (res != null)
                {
                    var tx = res["txid"] as object[];
                    if (tx != null && tx.Length > 0) id = tx[0].ToString();
                }
            }

            return new OrderResult
            {
                OrderId = id,
                Accepted = accepted,
                Filled = false,
                FilledQty = 0m,
                AvgFillPrice = 0m,
                Message = accepted ? "accepted" : "error"
            };
        }

        public async Task<bool> CancelOrderAsync(string orderId)
        {
            var form = new Dictionary<string, string> { { "txid", orderId } };
            var json = await PrivatePostAsync("/0/private/CancelOrder", form);
            return json.Contains("count");
        }

        private bool TryParseDecimalInvariant(object raw, out decimal value)
        {
            value = 0m;
            if (raw == null)
            {
                return false;
            }

            return decimal.TryParse(raw.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        private bool TryParseDoubleInvariant(object raw, out double value)
        {
            value = 0d;
            if (raw == null)
            {
                return false;
            }

            return double.TryParse(raw.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }
    }
}
