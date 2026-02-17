using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Exchanges
{
    public class OkxClient : IExchangeClient
    {
        private string _apiKey;
        private string _apiSecret;
        private string _passphrase;
        private readonly string _restBaseUrl;
        private static readonly SemaphoreSlim _constraintsLock = new SemaphoreSlim(1, 1);
        private static DateTime _constraintsFetchedUtc = DateTime.MinValue;
        private static Dictionary<string, SymbolConstraints> _symbolConstraintsBySymbol = new Dictionary<string, SymbolConstraints>(StringComparer.OrdinalIgnoreCase);

        public OkxClient(string apiKey, string apiSecret, string passphrase, string restBaseUrl = null)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _passphrase = passphrase;
            _restBaseUrl = ResolveRestBaseUrl(restBaseUrl);
        }

        public string Name => "OKX";

        public void SetCredentials(string apiKey, string apiSecret, string passphrase = null)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _passphrase = passphrase;
        }

        public async Task<List<string>> ListProductsAsync()
        {
            return await HttpUtil.RetryAsync(async () =>
            {
                var json = await HttpUtil.GetAsync(_restBaseUrl + "/api/v5/public/instruments?instType=SPOT").ConfigureAwait(false);
                var root = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
                var list = ToObjectArray(root != null && root.ContainsKey("data") ? root["data"] : null);
                var outList = new List<string>();
                foreach (var rowObj in list)
                {
                    var row = rowObj as Dictionary<string, object>;
                    if (row == null) continue;
                    var state = GetString(row, "state");
                    if (!string.Equals(state, "live", StringComparison.OrdinalIgnoreCase)) continue;
                    var inst = GetString(row, "instId");
                    if (string.IsNullOrWhiteSpace(inst)) continue;
                    outList.Add(inst.Replace("-", "/"));
                }
                outList.Sort(StringComparer.OrdinalIgnoreCase);
                return outList;
            }).ConfigureAwait(false);
        }

        public async Task<SymbolConstraints> GetSymbolConstraintsAsync(string productId)
        {
            var symbol = NormalizeProduct(productId);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return null;
            }

            await EnsureSymbolConstraintsCacheAsync().ConfigureAwait(false);
            SymbolConstraints constraints;
            if (_symbolConstraintsBySymbol.TryGetValue(symbol, out constraints))
            {
                return constraints;
            }

            return null;
        }

        public async Task<List<Candle>> GetCandlesAsync(string productId, int granularity, DateTime startUtc, DateTime endUtc)
        {
            return await HttpUtil.RetryAsync(async () =>
            {
                var instId = NormalizeProduct(productId);
                var normalizedGranularity = NormalizeGranularityMinutes(granularity);
                var bar = NormalizeBar(normalizedGranularity);
                var barMs = (long)normalizedGranularity * 60L * 1000L;
                var maxRowsPerRequest = 300L;
                var maxWindowMs = barMs * maxRowsPerRequest;

                var outList = new List<Candle>();
                var cursor = startUtc;

                while (cursor <= endUtc)
                {
                    var chunkEnd = cursor.AddMilliseconds(maxWindowMs);
                    if (chunkEnd > endUtc) chunkEnd = endUtc;

                    var url = _restBaseUrl + "/api/v5/market/history-candles?instId=" + Uri.EscapeDataString(instId)
                        + "&bar=" + Uri.EscapeDataString(bar)
                        + "&before=" + ToUnixMs(chunkEnd)
                        + "&after=" + ToUnixMs(cursor)
                        + "&limit=300";

                    var json = await HttpUtil.GetAsync(url).ConfigureAwait(false);
                    var root = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
                    var rows = ToObjectArray(root != null && root.ContainsKey("data") ? root["data"] : null);
                    foreach (var rowObj in rows)
                    {
                        var row = ToObjectArray(rowObj);
                        if (row.Length < 6) continue;
                        var ts = DateTimeOffset.FromUnixTimeMilliseconds(ToLong(row[0])).UtcDateTime;
                        if (ts < startUtc || ts > endUtc) continue;
                        outList.Add(new Candle
                        {
                            Time = ts,
                            Open = ToDecimal(row[1]),
                            High = ToDecimal(row[2]),
                            Low = ToDecimal(row[3]),
                            Close = ToDecimal(row[4]),
                            Volume = ToDecimal(row[5])
                        });
                    }

                    cursor = chunkEnd.AddMilliseconds(barMs);
                }

                var dedupByTime = new Dictionary<DateTime, Candle>();
                foreach (var candle in outList)
                {
                    dedupByTime[candle.Time] = candle;
                }

                var merged = new List<Candle>(dedupByTime.Values);
                merged.Sort((a, b) => a.Time.CompareTo(b.Time));
                return merged;
            }).ConfigureAwait(false);
        }

        public async Task<Ticker> GetTickerAsync(string productId)
        {
            return await HttpUtil.RetryAsync(async () =>
            {
                var instId = NormalizeProduct(productId);
                var json = await HttpUtil.GetAsync(_restBaseUrl + "/api/v5/market/ticker?instId=" + Uri.EscapeDataString(instId)).ConfigureAwait(false);
                var root = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
                var data = ToObjectArray(root != null && root.ContainsKey("data") ? root["data"] : null);
                if (data.Length == 0) return new Ticker { Time = DateTime.UtcNow };
                var row = data[0] as Dictionary<string, object>;
                if (row == null) return new Ticker { Time = DateTime.UtcNow };
                return new Ticker
                {
                    Bid = ToDecimal(GetString(row, "bidPx")),
                    Ask = ToDecimal(GetString(row, "askPx")),
                    Last = ToDecimal(GetString(row, "last")),
                    Time = DateTime.UtcNow
                };
            }).ConfigureAwait(false);
        }

        private static string ResolveRestBaseUrl(string overrideBaseUrl)
        {
            if (!string.IsNullOrWhiteSpace(overrideBaseUrl))
            {
                return overrideBaseUrl.Trim().TrimEnd('/');
            }

            var configured = Environment.GetEnvironmentVariable("CDTS_OKX_BASE_URL");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured.Trim().TrimEnd('/');
            }

            return "https://www.okx.com";
        }

        public async Task<FeeSchedule> GetFeesAsync()
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_apiSecret) || string.IsNullOrWhiteSpace(_passphrase))
            {
                return new FeeSchedule { MakerRate = 0.0008m, TakerRate = 0.0010m, Notes = "okx default spot fees" };
            }

            try
            {
                var root = await QueryTradeFeeAsync("/api/v5/account/trade-fee?instType=SPOT").ConfigureAwait(false);
                var fee = BuildWorstCaseFeeSchedule(root, "okx trade-fee");
                if (fee != null)
                {
                    return fee;
                }

                root = await QueryTradeFeeAsync("/api/v5/account/trade-fee?instType=SPOT&instId=BTC-USDT").ConfigureAwait(false);
                fee = BuildWorstCaseFeeSchedule(root, "okx trade-fee (BTC-USDT fallback)");
                if (fee != null)
                {
                    return fee;
                }
            }
            catch (Exception ex)
            {
                Log.Warn("[OkxClient] GetFees fallback: " + ex.Message);
            }

            return new FeeSchedule { MakerRate = 0.0008m, TakerRate = 0.0010m, Notes = "okx fallback fees" };
        }

        public async Task<Dictionary<string, decimal>> GetBalancesAsync()
        {
            EnsureCredentials();

            var req = BuildSignedRequest(HttpMethod.Get, "/api/v5/account/balance", string.Empty);
            var json = await HttpUtil.SendAsync(req).ConfigureAwait(false);
            var root = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
            if (!IsOkxSuccess(root))
            {
                throw new InvalidOperationException("okx account balance request failed");
            }

            var balances = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var accounts = ToObjectArray(root != null && root.ContainsKey("data") ? root["data"] : null);
            foreach (var accountObj in accounts)
            {
                var account = accountObj as Dictionary<string, object>;
                if (account == null) continue;

                var details = ToObjectArray(account.ContainsKey("details") ? account["details"] : null);
                foreach (var detailObj in details)
                {
                    var detail = detailObj as Dictionary<string, object>;
                    if (detail == null) continue;

                    var currency = GetString(detail, "ccy");
                    if (string.IsNullOrWhiteSpace(currency)) continue;

                    var available = ToDecimal(GetString(detail, "availBal"));
                    if (available <= 0m)
                    {
                        available = ToDecimal(GetString(detail, "cashBal"));
                    }
                    if (available <= 0m)
                    {
                        available = ToDecimal(GetString(detail, "eq"));
                    }

                    decimal existing;
                    if (balances.TryGetValue(currency, out existing))
                    {
                        balances[currency] = existing + available;
                    }
                    else
                    {
                        balances[currency] = available;
                    }
                }
            }

            return balances;
        }

        public async Task<OrderResult> PlaceOrderAsync(OrderRequest order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            EnsureCredentials();

            var bodyMap = new Dictionary<string, object>
            {
                { "instId", NormalizeProduct(order.ProductId) },
                { "tdMode", "cash" },
                { "side", order.Side == OrderSide.Buy ? "buy" : "sell" },
                { "ordType", order.Type == OrderType.Market ? "market" : "limit" },
                { "sz", order.Quantity.ToString(CultureInfo.InvariantCulture) }
            };

            if (order.Type == OrderType.Limit)
            {
                if (!order.Price.HasValue || order.Price.Value <= 0m)
                    throw new InvalidOperationException("Limit order requires positive price.");
                bodyMap["px"] = order.Price.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrWhiteSpace(order.ClientOrderId))
            {
                bodyMap["clOrdId"] = order.ClientOrderId;
            }

            var body = UtilCompat.JsonSerialize(bodyMap);
            var req = BuildSignedRequest(HttpMethod.Post, "/api/v5/trade/order", body);
            var json = await HttpUtil.SendAsync(req).ConfigureAwait(false);
            var root = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
            var data = ToObjectArray(root.ContainsKey("data") ? root["data"] : null);
            var ordId = string.Empty;
            if (data.Length > 0)
            {
                var row = data[0] as Dictionary<string, object>;
                if (row != null) ordId = GetString(row, "ordId");
            }

            var success = IsOkxSuccess(root);
            var message = success
                ? (!string.IsNullOrWhiteSpace(ordId) ? "accepted" : "accepted-no-order-id")
                : GetString(root, "msg");
            if (string.IsNullOrWhiteSpace(message))
            {
                message = success ? "accepted" : "error";
            }

            return new OrderResult
            {
                OrderId = ordId,
                Accepted = success && !string.IsNullOrWhiteSpace(ordId),
                Filled = false,
                FilledQty = 0m,
                AvgFillPrice = 0m,
                Message = message
            };
        }

        public Task<bool> CancelOrderAsync(string orderId)
        {
            return CancelOrderResolvedAsync(orderId);
        }

        private async Task<bool> CancelOrderResolvedAsync(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return false;
            EnsureCredentials();

            var pendingReq = BuildSignedRequest(HttpMethod.Get, "/api/v5/trade/orders-pending", string.Empty);
            var pendingJson = await HttpUtil.SendAsync(pendingReq).ConfigureAwait(false);
            var pendingRoot = UtilCompat.JsonDeserialize<Dictionary<string, object>>(pendingJson);
            var pending = ToObjectArray(pendingRoot != null && pendingRoot.ContainsKey("data") ? pendingRoot["data"] : null);

            string instId = string.Empty;
            foreach (var rowObj in pending)
            {
                var row = rowObj as Dictionary<string, object>;
                if (row == null) continue;
                if (!string.Equals(GetString(row, "ordId"), orderId, StringComparison.OrdinalIgnoreCase)) continue;
                instId = GetString(row, "instId");
                break;
            }

            if (string.IsNullOrWhiteSpace(instId)) return false;

            var body = UtilCompat.JsonSerialize(new Dictionary<string, object>
            {
                { "instId", instId },
                { "ordId", orderId }
            });

            var cancelReq = BuildSignedRequest(HttpMethod.Post, "/api/v5/trade/cancel-order", body);
            var cancelJson = await HttpUtil.SendAsync(cancelReq).ConfigureAwait(false);
            var cancelRoot = UtilCompat.JsonDeserialize<Dictionary<string, object>>(cancelJson);
            return IsOkxSuccess(cancelRoot);
        }

        public async Task<bool> CancelAllOpenOrdersAsync(string productId)
        {
            EnsureCredentials();
            var instId = NormalizeProduct(productId);

            var pendingReq = BuildSignedRequest(HttpMethod.Get, "/api/v5/trade/orders-pending?instId=" + Uri.EscapeDataString(instId), string.Empty);
            var pendingJson = await HttpUtil.SendAsync(pendingReq).ConfigureAwait(false);
            var pendingRoot = UtilCompat.JsonDeserialize<Dictionary<string, object>>(pendingJson);
            var pending = ToObjectArray(pendingRoot != null && pendingRoot.ContainsKey("data") ? pendingRoot["data"] : null);
            if (pending.Length == 0) return true;

            var canceled = 0;
            var attempted = 0;
            foreach (var rowObj in pending)
            {
                var row = rowObj as Dictionary<string, object>;
                if (row == null) continue;
                var ordId = GetString(row, "ordId");
                if (string.IsNullOrWhiteSpace(ordId)) continue;
                attempted++;

                var body = UtilCompat.JsonSerialize(new Dictionary<string, object>
                {
                    { "instId", instId },
                    { "ordId", ordId }
                });

                var cancelReq = BuildSignedRequest(HttpMethod.Post, "/api/v5/trade/cancel-order", body);
                var cancelJson = await HttpUtil.SendAsync(cancelReq).ConfigureAwait(false);
                var cancelRoot = UtilCompat.JsonDeserialize<Dictionary<string, object>>(cancelJson);
                if (IsOkxSuccess(cancelRoot)) canceled++;
            }

            return attempted > 0 && canceled == attempted;
        }

        private async Task<Dictionary<string, object>> QueryTradeFeeAsync(string pathWithQuery)
        {
            var req = BuildSignedRequest(HttpMethod.Get, pathWithQuery, string.Empty);
            var json = await HttpUtil.SendAsync(req).ConfigureAwait(false);
            return UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
        }

        private FeeSchedule BuildWorstCaseFeeSchedule(Dictionary<string, object> root, string notePrefix)
        {
            var data = ToObjectArray(root != null && root.ContainsKey("data") ? root["data"] : null);
            if (data.Length == 0)
            {
                return null;
            }

            decimal maxMaker = 0m;
            decimal maxTaker = 0m;
            int validRows = 0;

            foreach (var rowObj in data)
            {
                var row = rowObj as Dictionary<string, object>;
                if (row == null) continue;

                var maker = Math.Abs(ToDecimal(GetString(row, "maker")));
                var taker = Math.Abs(ToDecimal(GetString(row, "taker")));
                if (maker <= 0m || taker <= 0m) continue;

                validRows++;
                if (maker > maxMaker) maxMaker = maker;
                if (taker > maxTaker) maxTaker = taker;
            }

            if (validRows <= 0)
            {
                return null;
            }

            return new FeeSchedule
            {
                MakerRate = maxMaker,
                TakerRate = maxTaker,
                Notes = notePrefix + " worst-case across " + validRows.ToString(CultureInfo.InvariantCulture) + " row(s)"
            };
        }

        private bool IsOkxSuccess(Dictionary<string, object> root)
        {
            if (root == null)
            {
                return false;
            }

            if (!string.Equals(GetString(root, "code"), "0", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var data = ToObjectArray(root.ContainsKey("data") ? root["data"] : null);
            if (data.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < data.Length; i++)
            {
                var row = data[i] as Dictionary<string, object>;
                if (row == null) continue;

                var sCode = GetString(row, "sCode");
                if (string.IsNullOrWhiteSpace(sCode)) continue;
                if (!string.Equals(sCode, "0", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private HttpRequestMessage BuildSignedRequest(HttpMethod method, string pathWithQuery, string body)
        {
            EnsureCredentials();
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
            var bodyText = body ?? string.Empty;
            var prehash = timestamp + method.Method.ToUpperInvariant() + pathWithQuery + bodyText;
            var sign = ComputeHmacSha256Base64Raw(_apiSecret, prehash);

            var req = new HttpRequestMessage(method, _restBaseUrl + pathWithQuery);
            req.Headers.TryAddWithoutValidation("OK-ACCESS-KEY", _apiKey);
            req.Headers.TryAddWithoutValidation("OK-ACCESS-SIGN", sign);
            req.Headers.TryAddWithoutValidation("OK-ACCESS-TIMESTAMP", timestamp);
            req.Headers.TryAddWithoutValidation("OK-ACCESS-PASSPHRASE", _passphrase);

            if (method != HttpMethod.Get)
            {
                req.Content = new StringContent(bodyText, Encoding.UTF8, "application/json");
            }

            return req;
        }

        private string ComputeHmacSha256Base64Raw(string secret, string payload)
        {
            using (var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret ?? string.Empty)))
            {
                var sig = h.ComputeHash(Encoding.UTF8.GetBytes(payload ?? string.Empty));
                return Convert.ToBase64String(sig);
            }
        }

        private string NormalizeProduct(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return string.Empty;
            var cleaned = symbol.Replace("/", "-").Replace("--", "-").ToUpperInvariant();
            if (!cleaned.Contains("-"))
            {
                if (cleaned.EndsWith("USDT", StringComparison.OrdinalIgnoreCase))
                    return cleaned.Substring(0, cleaned.Length - 4) + "-USDT";
                if (cleaned.EndsWith("USD", StringComparison.OrdinalIgnoreCase))
                    return cleaned.Substring(0, cleaned.Length - 3) + "-USD";
            }
            return cleaned;
        }

        private string NormalizeBar(int granularityMinutes)
        {
            switch (granularityMinutes)
            {
                case 1: return "1m";
                case 3: return "3m";
                case 5: return "5m";
                case 15: return "15m";
                case 30: return "30m";
                case 60: return "1H";
                case 120: return "2H";
                case 240: return "4H";
                case 360: return "6H";
                case 720: return "12H";
                case 1440: return "1D";
                default: return granularityMinutes <= 1 ? "1m" : "5m";
            }
        }

        private int NormalizeGranularityMinutes(int granularityMinutes)
        {
            switch (granularityMinutes)
            {
                case 1:
                case 3:
                case 5:
                case 15:
                case 30:
                case 60:
                case 120:
                case 240:
                case 360:
                case 720:
                case 1440:
                    return granularityMinutes;
                default:
                    return granularityMinutes <= 1 ? 1 : 5;
            }
        }

        private long ToUnixMs(DateTime utc)
        {
            return new DateTimeOffset(utc.ToUniversalTime()).ToUnixTimeMilliseconds();
        }

        private async Task EnsureSymbolConstraintsCacheAsync()
        {
            if (_constraintsFetchedUtc != DateTime.MinValue && (DateTime.UtcNow - _constraintsFetchedUtc).TotalMinutes < 30 && _symbolConstraintsBySymbol.Count > 0)
            {
                return;
            }

            await _constraintsLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_constraintsFetchedUtc != DateTime.MinValue && (DateTime.UtcNow - _constraintsFetchedUtc).TotalMinutes < 30 && _symbolConstraintsBySymbol.Count > 0)
                {
                    return;
                }

                var json = await HttpUtil.GetAsync(_restBaseUrl + "/api/v5/public/instruments?instType=SPOT").ConfigureAwait(false);
                var root = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
                var list = ToObjectArray(root != null && root.ContainsKey("data") ? root["data"] : null);
                var rebuilt = new Dictionary<string, SymbolConstraints>(StringComparer.OrdinalIgnoreCase);

                foreach (var rowObj in list)
                {
                    var row = rowObj as Dictionary<string, object>;
                    if (row == null) continue;

                    var state = GetString(row, "state");
                    if (!string.Equals(state, "live", StringComparison.OrdinalIgnoreCase)) continue;

                    var instId = GetString(row, "instId");
                    if (string.IsNullOrWhiteSpace(instId)) continue;

                    var maxQty = ToDecimal(GetString(row, "maxMktSz"));
                    if (maxQty <= 0m)
                    {
                        maxQty = ToDecimal(GetString(row, "maxLmtSz"));
                    }

                    rebuilt[instId] = new SymbolConstraints
                    {
                        Symbol = instId,
                        MinQty = ToDecimal(GetString(row, "minSz")),
                        MaxQty = maxQty,
                        StepSize = ToDecimal(GetString(row, "lotSz")),
                        MinNotional = 0m,
                        PriceTickSize = ToDecimal(GetString(row, "tickSz")),
                        Source = "okx.public.instruments"
                    };
                }

                _symbolConstraintsBySymbol = rebuilt;
                _constraintsFetchedUtc = DateTime.UtcNow;
            }
            finally
            {
                _constraintsLock.Release();
            }
        }

        private object[] ToObjectArray(object value)
        {
            var arr = value as object[];
            if (arr != null) return arr;
            var list = value as ArrayList;
            if (list != null) return list.ToArray();
            return new object[0];
        }

        private decimal ToDecimal(object value)
        {
            if (value == null) return 0m;
            decimal d;
            if (decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return d;
            return 0m;
        }

        private long ToLong(object value)
        {
            if (value == null) return 0;
            long l;
            if (long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out l)) return l;
            return 0;
        }

        private string GetString(Dictionary<string, object> obj, string key)
        {
            if (obj == null || string.IsNullOrWhiteSpace(key) || !obj.ContainsKey(key) || obj[key] == null) return string.Empty;
            return Convert.ToString(obj[key], CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private void EnsureCredentials()
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_apiSecret) || string.IsNullOrWhiteSpace(_passphrase))
                throw new InvalidOperationException("OKX credentials (key/secret/passphrase) are required.");
        }
    }
}
