using System;
using System.Collections;
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
                EnsureCredentials();

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
                var res = new List<string>();
                if (arr == null)
                {
                    return res;
                }

                foreach (var p in arr)
                {
                    if (p == null || !p.ContainsKey("name") || p["name"] == null)
                    {
                        continue;
                    }

                    var normalized = p["name"].ToString().Replace(" ", "");
                    if (!string.IsNullOrWhiteSpace(normalized))
                    {
                        res.Add(normalized);
                    }
                }
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
                if (root != null && root.ContainsKey("data"))
                {
                    var data = root["data"] as Dictionary<string, object>; /* data */
                    var ohlc = ToObjectArray(data != null && data.ContainsKey("ohlc") ? data["ohlc"] : null);
                    if (ohlc != null)
                    {
                        foreach (var o in ohlc)
                        {
                            var row = o as Dictionary<string, object>; /* row */
                            if (row == null)
                            {
                                continue;
                            }

                            var seconds = ToLong(row.ContainsKey("timestamp") ? row["timestamp"] : null);
                            if (seconds <= 0)
                            {
                                continue;
                            }

                            DateTime ts;
                            if (!TryFromUnixSeconds(seconds, out ts))
                            {
                                continue;
                            }

                            if (ts < startUtc || ts > endUtc) continue; /* filter */

                            decimal open;
                            decimal high;
                            decimal low;
                            decimal close;
                            decimal volume;
                            if (!TryGetDecimal(row, "open", out open)) continue;
                            if (!TryGetDecimal(row, "high", out high)) continue;
                            if (!TryGetDecimal(row, "low", out low)) continue;
                            if (!TryGetDecimal(row, "close", out close)) continue;
                            if (!TryGetDecimal(row, "volume", out volume)) continue;

                            res.Add(new Candle { Time = ts, Open = open, High = high, Low = low, Close = close, Volume = volume }); /* add */
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
                if (obj == null)
                {
                    throw new InvalidOperationException("Bitstamp ticker payload was empty.");
                }

                var bid = ToDecimal(GetString(obj, "bid"));
                var ask = ToDecimal(GetString(obj, "ask"));
                var last = ToDecimal(GetString(obj, "last"));
                if (last <= 0m && bid > 0m && ask > 0m && ask >= bid)
                {
                    last = (bid + ask) / 2m;
                }

                if (last <= 0m)
                {
                    throw new InvalidOperationException("Bitstamp ticker payload did not provide a valid last price.");
                }

                if (bid <= 0m) bid = last;
                if (ask <= 0m) ask = last;

                return new Ticker { Bid = bid, Ask = ask, Last = last, Time = DateTime.UtcNow }; /* ticker */
            });
        }

        public async Task<FeeSchedule> GetFeesAsync()
        {
            /* fees are volume tiered; parse explicit maker/taker where present, otherwise use conservative mirrored fallback */
            var json = await PrivatePostAsync("/v2/balance/", new Dictionary<string,string>()); /* call */
            var obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json); /* parse */
            if (obj == null)
            {
                throw new InvalidOperationException("Bitstamp balance payload was empty while reading fees.");
            }
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
            if (obj == null)
            {
                return d;
            }

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
                if (!req.Price.HasValue || req.Price.Value <= 0m)
                {
                    throw new InvalidOperationException("Limit order requires positive price.");
                }
                form["price"] = req.Price.Value.ToString(CultureInfo.InvariantCulture); /* price */
            }
            var json = await PrivatePostAsync(path, form); /* post */
            var obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json); /* parse */
            if (obj == null)
            {
                return new OrderResult { OrderId = "", Accepted = false, Filled = false, FilledQty = 0m, AvgFillPrice = 0m, Message = "empty response" }; /* result */
            }

            var id = GetString(obj, "id");
            var status = GetString(obj, "status");
            var reason = GetString(obj, "reason");
            var error = GetString(obj, "error");

            var hasFailure = IsFailureText(status) || IsFailureText(reason) || IsFailureText(error);
            var accepted = !hasFailure && !string.IsNullOrWhiteSpace(id);
            var message = accepted
                ? "accepted"
                : (!string.IsNullOrWhiteSpace(error) ? error : (!string.IsNullOrWhiteSpace(reason) ? reason : "order rejected"));

            return new OrderResult { OrderId = accepted ? id : "", Accepted = accepted, Filled = false, FilledQty = 0m, AvgFillPrice = 0m, Message = message }; /* result */
        }

        public async Task<bool> CancelOrderAsync(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                return false;
            }

            var form = new Dictionary<string,string>(); form["id"] = orderId; /* id */
            var json = await PrivatePostAsync("/v2/cancel_order/", form).ConfigureAwait(false); /* post */
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            var trimmed = json.Trim();
            if (string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, "\"true\"", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, "1", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, "\"false\"", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, "0", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
            if (obj == null)
            {
                return false;
            }

            var status = GetString(obj, "status");
            var statusUpper = string.IsNullOrWhiteSpace(status) ? string.Empty : status.Trim().ToUpperInvariant();
            if (statusUpper.Contains("ERROR") || statusUpper.Contains("FAIL") || statusUpper.Contains("REJECT") || statusUpper.Contains("NOT_FOUND"))
            {
                return false;
            }

            var reason = GetString(obj, "reason");
            var reasonUpper = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim().ToUpperInvariant();
            if (reasonUpper.Contains("ERROR") || reasonUpper.Contains("FAIL") || reasonUpper.Contains("REJECT") || reasonUpper.Contains("NOT_FOUND"))
            {
                return false;
            }

            var successRaw = GetString(obj, "success");
            if (string.Equals(successRaw, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(successRaw, "1", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var okRaw = GetString(obj, "ok");
            if (string.Equals(okRaw, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(okRaw, "1", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var responseOrderId = GetString(obj, "id");
            if (!string.IsNullOrWhiteSpace(responseOrderId)
                && string.Equals(responseOrderId, orderId, StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(statusUpper))
            {
                return true;
            }

            if (statusUpper == "OK" || statusUpper == "SUCCESS" || statusUpper == "CANCELED" || statusUpper == "CANCELLED" || statusUpper == "FINISHED")
            {
                return true;
            }

            return false;
        }

        public async Task<List<OpenOrder>> GetOpenOrdersAsync(string productId = null)
        {
            var json = await PrivatePostAsync("/v2/open_orders/all/", new Dictionary<string, string>());
            var items = UtilCompat.JsonDeserialize<List<Dictionary<string, object>>>(json) ?? new List<Dictionary<string, object>>();
            var normalizedFilterProduct = string.IsNullOrWhiteSpace(productId) ? string.Empty : NormalizeProduct(productId);
            var list = new List<OpenOrder>();
            foreach (var item in items)
            {
                if (item == null) continue;
                string currencyPair = GetString(item, "currency_pair");
                string productIdRaw = string.IsNullOrWhiteSpace(currencyPair) ? "" : currencyPair.Replace("/", "-").ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(productIdRaw))
                {
                    continue;
                }
                string normalizedRowProduct = NormalizeProduct(productIdRaw);

                if (!string.IsNullOrWhiteSpace(normalizedFilterProduct)
                    && !string.Equals(normalizedRowProduct, normalizedFilterProduct, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string orderId = GetString(item, "id");
                if (string.IsNullOrWhiteSpace(orderId))
                {
                    continue;
                }
                string typeStr = GetString(item, "type");
                decimal price = ToDecimal(GetString(item, "price"));
                decimal qty = ToDecimal(GetString(item, "amount"));
                string datetimeStr = GetString(item, "datetime");
                DateTime created; 
                if (!DateTime.TryParse(datetimeStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out created))
                {
                    continue;
                }

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

        private static bool IsFailureText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var text = value.Trim().ToUpperInvariant();
            return text.Contains("ERROR")
                || text.Contains("FAIL")
                || text.Contains("REJECT")
                || text.Contains("DENIED")
                || text.Contains("INVALID")
                || text.Contains("NOT_FOUND");
        }

        private static object[] ToObjectArray(object value)
        {
            var arr = value as object[];
            if (arr != null)
            {
                return arr;
            }

            var list = value as ArrayList;
            if (list != null)
            {
                return list.ToArray();
            }

            return new object[0];
        }

        private static long ToLong(object value)
        {
            if (value == null)
            {
                return 0L;
            }

            long parsed;
            if (long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            return 0L;
        }

        private static bool TryFromUnixSeconds(long seconds, out DateTime utc)
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

        private static bool TryGetDecimal(Dictionary<string, object> map, string key, out decimal value)
        {
            value = 0m;
            if (map == null || string.IsNullOrWhiteSpace(key) || !map.ContainsKey(key) || map[key] == null)
            {
                return false;
            }

            return decimal.TryParse(Convert.ToString(map[key], CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        private void EnsureCredentials()
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_apiSecret) || string.IsNullOrWhiteSpace(_customerId))
            {
                throw new InvalidOperationException("Bitstamp credentials (key/secret/customer id) are required.");
            }
        }
    }
}
