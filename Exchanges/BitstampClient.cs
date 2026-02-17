using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Exchanges
{
    public class BitstampClient : IExchangeClient
    {
        private string _apiKey; /* api key */
        private string _apiSecret; /* secret raw */
        private string _customerId; /* customer id */
        private const string Rest = "https://www.bitstamp.net/api"; /* base */
        private static readonly HttpClient _http = new HttpClient(); /* shared client */

        public BitstampClient(string apiKey, string apiSecret, string customerId) { _apiKey = apiKey; _apiSecret = apiSecret; _customerId = customerId; } /* ctor */

        public void SetCredentials(string apiKey, string apiSecret, string passphrase = null)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _customerId = passphrase; // Bitstamp uses the 'passphrase' field for Customer ID
        }

        public string Name { get { return "Bitstamp"; } } /* name */

        public string NormalizeProduct(string uiSymbol) { return uiSymbol.Replace("/", "").Replace("-", "").ToLowerInvariant(); } /* e.g. btcusd */
        public string DenormalizeProduct(string apiSymbol) { apiSymbol = apiSymbol.ToUpperInvariant(); if (apiSymbol.Length == 6) return apiSymbol.Substring(0,3) + "/" + apiSymbol.Substring(3); return apiSymbol; } /* to ui */

        private async Task<string> PrivatePostAsync(string path, Dictionary<string,string> form)
        {
            return await HttpUtil.RetryAsync(async () =>
            {
                /* legacy v2 auth: signature = HMAC_SHA256(secret, nonce + customer_id + api_key).upper */
                var nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(); /* nonce */
                var message = nonce + _customerId + _apiKey; /* concat */
                var signature = SecurityUtil.ComputeHmacSha256Hex(_apiSecret, message); /* sign */
                var dict = new Dictionary<string,string>(form ?? new Dictionary<string,string>()); /* copy */
                dict["key"] = _apiKey; dict["signature"] = signature; dict["nonce"] = nonce; /* auth */

                var url = Rest + path; 
                var content = new FormUrlEncodedContent(dict); /* form */
            
                using (var resp = await _http.PostAsync(url, content))
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    if (!resp.IsSuccessStatusCode) throw new HttpRequestException(body);
                    return body; /* post */
                }
            });
        }

        public async Task<List<string>> ListProductsAsync()
        {
            return await HttpUtil.RetryAsync(async () =>
            {
                var json = await HttpUtil.GetAsync(Rest + "/v2/trading-pairs-info/"); /* list */
                var arr = UtilCompat.JsonDeserialize<List<Dictionary<string, object>>>(json); /* parse */
                var res = new List<string>(); foreach (var p in arr) if (p.ContainsKey("name")) res.Add(p["name"].ToString().Replace(" ", "")); /* add like BTC/USD */
                return res; /* return */
            });
        }

        public async Task<List<Candle>> GetCandlesAsync(string productId, int minutes, DateTime startUtc, DateTime endUtc)
        {
            return await HttpUtil.RetryAsync(async () =>
            {
                var pair = NormalizeProduct(productId); /* pair */
                var step = minutes * 60; if (step < 60) step = 60; /* seconds */
                var url = Rest + "/v2/ohlc/" + pair + "/?step=" + step + "&limit=1000"; /* url */
                var json = await HttpUtil.GetAsync(url); /* get */
                var root = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json); /* parse */
                var res = new List<Candle>(); /* list */
                if (root.ContainsKey("data"))
                {
                    var data = (Dictionary<string, object>)root["data"]; /* data */
                    var ohlc = data["ohlc"] as object[]; if (ohlc != null)
                    {
                        foreach (var o in ohlc)
                        {
                            var row = (Dictionary<string, object>)o; /* row */
                            var ts = new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddSeconds(Convert.ToInt64(row["timestamp"])); /* ts */
                            if (ts < startUtc || ts > endUtc) continue; /* filter */
                            res.Add(new Candle { Time = ts, Open = Convert.ToDecimal(row["open"]), High = Convert.ToDecimal(row["high"]), Low = Convert.ToDecimal(row["low"]), Close = Convert.ToDecimal(row["close"]), Volume = Convert.ToDecimal(row["volume"]) }); /* add */
                        }
                    }
                }
                res.Sort((a,b)=>a.Time.CompareTo(b.Time)); /* sort */
                return res; /* return */
            });
        }

        public async Task<Ticker> GetTickerAsync(string productId)
        {
            return await HttpUtil.RetryAsync(async () =>
            {
                var pair = NormalizeProduct(productId); /* pair */
                var url = Rest + "/v2/ticker/" + pair + "/"; /* url */
                var json = await HttpUtil.GetAsync(url); /* get */
                var obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json); /* parse */
                return new Ticker { Bid = Convert.ToDecimal(obj["bid"]), Ask = Convert.ToDecimal(obj["ask"]), Last = Convert.ToDecimal(obj["last"]), Time = DateTime.UtcNow }; /* ticker */
            });
        }

        public async Task<FeeSchedule> GetFeesAsync()
        {
            /* fees are volume tiered; parse explicit maker/taker where present, otherwise use conservative mirrored fallback */
            var json = await PrivatePostAsync("/v2/balance/", new Dictionary<string,string>()); /* call */
            var obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json); /* parse */
            decimal taker = 0m;
            decimal maker = 0m;
            foreach (var k in obj.Keys)
            {
                if (k.EndsWith("_maker_fee", StringComparison.OrdinalIgnoreCase))
                {
                    var val = obj[k].ToString();
                    decimal pct;
                    if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out pct))
                    {
                        maker = pct / 100m;
                    }
                    continue;
                }

                if (k.EndsWith("_taker_fee", StringComparison.OrdinalIgnoreCase))
                {
                    var val = obj[k].ToString();
                    decimal pct;
                    if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out pct))
                    {
                        taker = pct / 100m;
                    }
                    continue;
                }

                if (k.EndsWith("_fee", StringComparison.OrdinalIgnoreCase) && taker <= 0m)
                {
                    var val = obj[k].ToString();
                    decimal pct;
                    if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out pct))
                    {
                        taker = pct / 100m;
                    }
                }
            }

            if (taker <= 0m)
            {
                throw new InvalidOperationException("Bitstamp balance payload did not provide a valid taker fee rate.");
            }

            if (maker <= 0m)
            {
                maker = taker;
            }

            return new FeeSchedule { MakerRate = maker, TakerRate = taker, Notes = "from balance fees" }; /* return */
        }

        public async Task<Dictionary<string, decimal>> GetBalancesAsync()
        {
            var json = await PrivatePostAsync("/v2/balance/", new Dictionary<string,string>()); /* call */
            var obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json); /* parse */
            var d = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase); /* dict */
            foreach (var k in obj.Keys)
            {
                if (k.EndsWith("_balance"))
                {
                    var sym = k.Substring(0, k.Length - "_balance".Length).ToUpperInvariant(); /* sym */
                    if (decimal.TryParse(obj[k].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var bal)) d[sym] = bal; /* set */
                }
            }
            return d; /* return */
        }

        public async Task<OrderResult> PlaceOrderAsync(OrderRequest req)
        {
            var pair = NormalizeProduct(req.ProductId); /* pair */
            var path = req.Side == OrderSide.Buy ? "/v2/buy/" + pair + "/" : "/v2/sell/" + pair + "/"; /* endpoint */
            var form = new Dictionary<string,string>(); /* form */
            if (req.Type == OrderType.Market)
            {
                form["amount"] = req.Quantity.ToString(CultureInfo.InvariantCulture); /* qty */
            }
            else
            {
                form["amount"] = req.Quantity.ToString(CultureInfo.InvariantCulture); /* qty */
                form["price"] = req.Price.Value.ToString(CultureInfo.InvariantCulture); /* price */
            }
            var json = await PrivatePostAsync(path, form); /* post */
            var obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json); /* parse */
            var accepted = obj.ContainsKey("id"); var id = accepted ? obj["id"].ToString() : ""; /* id */
            return new OrderResult { OrderId = id, Accepted = accepted, Filled = false, FilledQty = 0m, AvgFillPrice = 0m, Message = accepted ? "accepted" : "error" }; /* result */
        }

        public async Task<bool> CancelOrderAsync(string orderId)
        {
            var form = new Dictionary<string,string>(); form["id"] = orderId; /* id */
            var json = await PrivatePostAsync("/v2/cancel_order/", form); /* post */
            return json.Contains("true"); /* naive */
        }

        public async Task<List<OpenOrder>> GetOpenOrdersAsync(string productId = null)
        {
            var json = await PrivatePostAsync("/v2/open_orders/all/", new Dictionary<string, string>());
            var items = UtilCompat.JsonDeserialize<List<Dictionary<string, object>>>(json) ?? new List<Dictionary<string, object>>();
            var list = new List<OpenOrder>();
            foreach (var item in items)
            {
                if (item == null) continue;
                string currencyPair = GetString(item, "currency_pair");
                string productIdRaw = string.IsNullOrWhiteSpace(currencyPair) ? "" : currencyPair.Replace("/", "-").ToUpperInvariant();

                if (!string.IsNullOrWhiteSpace(productId) && !string.Equals(productIdRaw, productId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string orderId = GetString(item, "id");
                string typeStr = GetString(item, "type");
                decimal price = ToDecimal(GetString(item, "price"));
                decimal qty = ToDecimal(GetString(item, "amount"));
                string datetimeStr = GetString(item, "datetime");
                DateTime created; 
                if (!DateTime.TryParse(datetimeStr, out created)) created = DateTime.UtcNow;

                list.Add(new OpenOrder
                {
                    OrderId = orderId,
                    ProductId = productIdRaw,
                    Side = (typeStr == "0") ? OrderSide.Buy : OrderSide.Sell,
                    Type = OrderType.Limit, // Bitstamp mostly limit
                    Price = price,
                    Quantity = qty,
                    FilledQty = 0m, // Not returned in list usually
                    CreatedUtc = created,
                    Status = "OPEN"
                });
            }
            return list;
        }

        private static string GetString(Dictionary<string, object> item, string key)
        {
            if (item == null || string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            object value;
            if (!item.TryGetValue(key, out value) || value == null)
            {
                return string.Empty;
            }

            return value.ToString();
        }

        private static decimal ToDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0m;
            }

            decimal parsed;
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            return 0m;
        }
    }
}
