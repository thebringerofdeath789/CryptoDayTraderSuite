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
                EnsureCredentials();

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

                                if (row == null || row.Length < 7) continue;

                                double epochSeconds;
                                if (!TryParseDoubleInvariant(row[0], out epochSeconds)) continue;
                                DateTime ts;
                                if (!TryFromUnixSeconds((long)epochSeconds, out ts)) continue;
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

                if (t.Last <= 0m)
                {
                    if (t.Bid > 0m && t.Ask > 0m && t.Ask >= t.Bid)
                    {
                        t.Last = (t.Bid + t.Ask) / 2m;
                    }
                }

                if (t.Last <= 0m)
                {
                    throw new InvalidOperationException("Kraken ticker payload did not provide a valid last price.");
                }

                if (t.Bid <= 0m) t.Bid = t.Last;
                if (t.Ask <= 0m) t.Ask = t.Last;

                return t;
            });
        }

        public async Task<FeeSchedule> GetFeesAsync()
        {
            var body = new Dictionary<string, string> { { "fee-info", "true" } };
            // PrivatePostAsync already wraps RetryAsync
            var json = await PrivatePostAsync("/0/private/TradeVolume", body);
            var obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);

            EnsureNoKrakenErrors(obj, "trade volume");

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
            catch (Exception ex)
            {
                throw new InvalidOperationException("Kraken fee parsing failed.", ex);
            }
            return new FeeSchedule { MakerRate = maker, TakerRate = taker, Notes = "from TradeVolume" };
        }

        public async Task<Dictionary<string, decimal>> GetBalancesAsync()
        {
            var json = await PrivatePostAsync("/0/private/Balance", new Dictionary<string, string>());
            var obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
            EnsureNoKrakenErrors(obj, "balances");
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

        public async Task<List<OpenOrder>> GetOpenOrdersAsync(string productId = null)
        {
            var normalizedFilterProduct = string.IsNullOrWhiteSpace(productId) ? string.Empty : NormalizeProduct(productId);
            var json = await PrivatePostAsync("/0/private/OpenOrders", new Dictionary<string, string>());
            var obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
            EnsureNoKrakenErrors(obj, "open orders");
            var list = new List<OpenOrder>();
            
            if (obj != null && obj.ContainsKey("result"))
            {
                var result = obj["result"] as Dictionary<string, object>;
                if (result != null && result.ContainsKey("open"))
                {
                    var open = result["open"] as Dictionary<string, object>;
                    if (open != null)
                    {
                        foreach (var key in open.Keys)
                        {
                            var orderMap = open[key] as Dictionary<string, object>;
                            if (orderMap == null) continue;
                            if (string.IsNullOrWhiteSpace(key)) continue;

                            var desc = orderMap.ContainsKey("descr") ? orderMap["descr"] as Dictionary<string, object> : null;
                            var symbol = desc != null && desc.ContainsKey("pair") ? desc["pair"].ToString() : "";
                            if (string.IsNullOrWhiteSpace(symbol))
                            {
                                continue;
                            }
                            var normalizedSymbol = string.IsNullOrWhiteSpace(symbol) ? string.Empty : NormalizeProduct(symbol);
                            if (!string.IsNullOrWhiteSpace(normalizedFilterProduct)
                                && !string.Equals(normalizedSymbol, normalizedFilterProduct, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            var sideStr = desc != null && desc.ContainsKey("type") ? desc["type"].ToString() : "";
                            if (!string.Equals(sideStr, "buy", StringComparison.OrdinalIgnoreCase)
                                && !string.Equals(sideStr, "sell", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            var typeStr = desc != null && desc.ContainsKey("ordertype") ? desc["ordertype"].ToString() : "";
                            var priceStr = desc != null && desc.ContainsKey("price") ? desc["price"].ToString() : "0";
                            var volStr = orderMap.ContainsKey("vol") ? orderMap["vol"].ToString() : "0";
                            var filledStr = orderMap.ContainsKey("vol_exec") ? orderMap["vol_exec"].ToString() : "0";
                            var statusStr = NormalizeOpenOrderStatus(orderMap.ContainsKey("status") ? orderMap["status"].ToString() : "");
                            if (string.IsNullOrWhiteSpace(statusStr))
                            {
                                continue;
                            }
                            double createdTime;
                            if (!orderMap.ContainsKey("opentm") || !TryParseDoubleInvariant(orderMap["opentm"], out createdTime))
                            {
                                continue;
                            }

                            DateTime createdUtc;
                            if (!TryFromUnixSeconds((long)createdTime, out createdUtc))
                            {
                                continue;
                            }

                            decimal price, vol, filled;
                            decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out price);
                            decimal.TryParse(volStr, NumberStyles.Any, CultureInfo.InvariantCulture, out vol);
                            decimal.TryParse(filledStr, NumberStyles.Any, CultureInfo.InvariantCulture, out filled);

                            list.Add(new OpenOrder
                            {
                                OrderId = key,
                                ProductId = symbol,
                                Side = string.Equals(sideStr, "buy", StringComparison.OrdinalIgnoreCase) ? OrderSide.Buy : OrderSide.Sell,
                                Type = string.Equals(typeStr, "market", StringComparison.OrdinalIgnoreCase) ? OrderType.Market : OrderType.Limit,
                                Price = price,
                                Quantity = vol,
                                FilledQty = filled,
                                Status = statusStr,
                                CreatedUtc = createdUtc
                            });
                        }
                    }
                }
            }
            return list;
        }

        private static string NormalizeOpenOrderStatus(string rawStatus)
        {
            var status = string.IsNullOrWhiteSpace(rawStatus)
                ? string.Empty
                : rawStatus.Trim().ToUpperInvariant().Replace("-", "_").Replace(" ", "_");

            if (string.IsNullOrWhiteSpace(status))
            {
                return string.Empty;
            }

            if (status.Contains("PARTIAL") && status.Contains("FILL"))
            {
                return "PARTIALLY_FILLED";
            }

            if (status == "NEW" || status == "OPEN" || status == "ACTIVE" || status == "WORKING" || status == "PENDING" || status == "RESTING" || status == "LIVE")
            {
                return "OPEN";
            }

            if (status == "PARTIALLY_FILLED")
            {
                return "PARTIALLY_FILLED";
            }

            return string.Empty;
        }

        public async Task<OrderResult> PlaceOrderAsync(OrderRequest req)
        {
            var form = new Dictionary<string, string>();
            form["pair"] = NormalizeProduct(req.ProductId);
            form["type"] = req.Side == OrderSide.Buy ? "buy" : "sell";
            form["ordertype"] = req.Type == OrderType.Market ? "market" : "limit";
            form["volume"] = req.Quantity.ToString(CultureInfo.InvariantCulture);
            if (req.Type == OrderType.Limit)
            {
                if (!req.Price.HasValue || req.Price.Value <= 0m)
                {
                    throw new InvalidOperationException("Limit order requires positive price.");
                }

                form["price"] = req.Price.Value.ToString(CultureInfo.InvariantCulture);
            }

            var json = await PrivatePostAsync("/0/private/AddOrder", form);
            var obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);

            if (obj != null)
            {
                var precheckErrors = ReadObjectArray(obj, "error");
                for (int i = 0; i < precheckErrors.Length; i++)
                {
                    var text = precheckErrors[i] == null ? string.Empty : precheckErrors[i].ToString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return new OrderResult
                        {
                            OrderId = string.Empty,
                            Accepted = false,
                            Filled = false,
                            FilledQty = 0m,
                            AvgFillPrice = 0m,
                            Message = text
                        };
                    }
                }
            }

            if (obj == null)
            {
                return new OrderResult
                {
                    OrderId = string.Empty,
                    Accepted = false,
                    Filled = false,
                    FilledQty = 0m,
                    AvgFillPrice = 0m,
                    Message = "empty response"
                };
            }

            var id = string.Empty;
            if (obj.ContainsKey("result"))
            {
                var res = obj["result"] as Dictionary<string, object>;
                if (res != null)
                {
                    var tx = ReadObjectArray(res, "txid");
                    if (tx != null && tx.Length > 0) id = tx[0].ToString();
                }
            }

            var errors = ReadObjectArray(obj, "error");
            var firstError = string.Empty;
            for (int i = 0; i < errors.Length; i++)
            {
                var text = errors[i] == null ? string.Empty : errors[i].ToString();
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                firstError = text;
                break;
            }

            var hasError = !string.IsNullOrWhiteSpace(firstError);
            var accepted = !hasError && !string.IsNullOrWhiteSpace(id);

            return new OrderResult
            {
                OrderId = id,
                Accepted = accepted,
                Filled = false,
                FilledQty = 0m,
                AvgFillPrice = 0m,
                Message = accepted ? "accepted" : (string.IsNullOrWhiteSpace(firstError) ? "order rejected" : firstError)
            };
        }

        public async Task<bool> CancelOrderAsync(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                return false;
            }

            var form = new Dictionary<string, string> { { "txid", orderId } };
            var json = await PrivatePostAsync("/0/private/CancelOrder", form).ConfigureAwait(false);
            var root = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
            if (root == null)
            {
                return false;
            }

            var errors = ReadObjectArray(root, "error");
            if (errors.Length > 0)
            {
                for (int i = 0; i < errors.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(errors[i] == null ? string.Empty : errors[i].ToString()))
                    {
                        return false;
                    }
                }
            }

            var result = root.ContainsKey("result") ? root["result"] as Dictionary<string, object> : null;
            if (result == null)
            {
                return false;
            }

            decimal count;
            if (result.ContainsKey("count") && TryParseDecimalInvariant(result["count"], out count))
            {
                return count > 0m;
            }

            var pending = result.ContainsKey("pending") ? result["pending"] : null;
            var pendingText = pending == null ? string.Empty : pending.ToString();
            if (string.Equals(pendingText, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(pendingText, "1", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private void EnsureNoKrakenErrors(Dictionary<string, object> root, string operation)
        {
            if (root == null)
            {
                throw new InvalidOperationException("Kraken " + operation + " response was empty.");
            }

            var errors = ReadObjectArray(root, "error");
            for (int i = 0; i < errors.Length; i++)
            {
                var text = errors[i] == null ? string.Empty : errors[i].ToString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    throw new InvalidOperationException("Kraken " + operation + " failed: " + text);
                }
            }
        }

        private static object[] ReadObjectArray(Dictionary<string, object> map, string key)
        {
            if (map == null || string.IsNullOrWhiteSpace(key) || !map.ContainsKey(key) || map[key] == null)
            {
                return new object[0];
            }

            var typed = map[key] as object[];
            if (typed != null)
            {
                return typed;
            }

            var list = map[key] as System.Collections.ArrayList;
            if (list != null)
            {
                return list.ToArray();
            }

            return new object[0];
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

        private bool TryFromUnixSeconds(long seconds, out DateTime utc)
        {
            utc = DateTime.MinValue;
            if (seconds <= 0)
            {
                return false;
            }

            try
            {
                utc = DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        private void EnsureCredentials()
        {
            if (string.IsNullOrWhiteSpace(_key) || string.IsNullOrWhiteSpace(_secretBase64))
            {
                throw new InvalidOperationException("Kraken credentials are required.");
            }
        }
    }
}
