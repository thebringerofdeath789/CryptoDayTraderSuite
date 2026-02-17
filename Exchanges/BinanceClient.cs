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
    public class BinanceClient : IExchangeClient
    {
        private string _apiKey;
        private string _apiSecret;
        private readonly string _restBaseUrl;
        private static readonly HttpClient _http = new HttpClient();
        private static readonly object _orderSymbolCacheLock = new object();
        private static Dictionary<string, string> _orderSymbolByOrderId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly SemaphoreSlim _constraintsLock = new SemaphoreSlim(1, 1);
        private static DateTime _constraintsFetchedUtc = DateTime.MinValue;
        private static Dictionary<string, SymbolConstraints> _symbolConstraintsBySymbol = new Dictionary<string, SymbolConstraints>(StringComparer.OrdinalIgnoreCase);

        public BinanceClient(string apiKey, string apiSecret, string restBaseUrl = null)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _restBaseUrl = ResolveRestBaseUrl(restBaseUrl);
        }

        public string Name => "Binance";

        public void SetCredentials(string apiKey, string apiSecret, string passphrase = null)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
        }

        public async Task<List<string>> ListProductsAsync()
        {
            return await HttpUtil.RetryAsync(async () =>
            {
                var obj = await GetExchangeInfoAsync().ConfigureAwait(false);
                var outList = new List<string>();
                if (obj == null || !obj.ContainsKey("symbols")) return outList;

                var symbols = ToObjectArray(obj["symbols"]);
                foreach (var item in symbols)
                {
                    var row = item as Dictionary<string, object>;
                    if (row == null) continue;
                    var status = GetString(row, "status");
                    if (!string.Equals(status, "TRADING", StringComparison.OrdinalIgnoreCase)) continue;
                    var baseAsset = GetString(row, "baseAsset");
                    var quoteAsset = GetString(row, "quoteAsset");
                    if (string.IsNullOrWhiteSpace(baseAsset) || string.IsNullOrWhiteSpace(quoteAsset)) continue;
                    outList.Add(baseAsset.ToUpperInvariant() + "/" + quoteAsset.ToUpperInvariant());
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
                var symbol = NormalizeProduct(productId);
                var interval = NormalizeInterval(granularity);
                var startMs = ToUnixMs(startUtc);
                var endMs = ToUnixMs(endUtc);
                var intervalMs = GetIntervalMilliseconds(granularity);

                if (endMs < startMs)
                {
                    return new List<Candle>();
                }

                var list = new List<Candle>();
                var cursorMs = startMs;

                while (cursorMs <= endMs)
                {
                    var url = _restBaseUrl + "/api/v3/klines?symbol=" + Uri.EscapeDataString(symbol)
                        + "&interval=" + Uri.EscapeDataString(interval)
                        + "&startTime=" + cursorMs
                        + "&endTime=" + endMs
                        + "&limit=1000";

                    var json = await HttpUtil.GetAsync(url).ConfigureAwait(false);
                    var data = UtilCompat.JsonDeserialize<object[]>(json);
                    if (data == null || data.Length == 0)
                    {
                        break;
                    }

                    long maxOpenTimeMs = 0L;
                    foreach (var rowObj in data)
                    {
                        var row = ToObjectArray(rowObj);
                        if (row.Length < 6) continue;

                        var openTimeMs = ToLong(row[0]);
                        if (openTimeMs > maxOpenTimeMs) maxOpenTimeMs = openTimeMs;

                        var ts = FromUnixMs(openTimeMs);
                        if (ts < startUtc || ts > endUtc) continue;

                        list.Add(new Candle
                        {
                            Time = ts,
                            Open = ToDecimal(row[1]),
                            High = ToDecimal(row[2]),
                            Low = ToDecimal(row[3]),
                            Close = ToDecimal(row[4]),
                            Volume = ToDecimal(row[5])
                        });
                    }

                    if (maxOpenTimeMs <= 0L)
                    {
                        break;
                    }

                    var nextCursorMs = maxOpenTimeMs + intervalMs;
                    if (nextCursorMs <= cursorMs)
                    {
                        break;
                    }

                    cursorMs = nextCursorMs;
                    if (data.Length < 1000)
                    {
                        break;
                    }
                }

                var dedupByTime = new Dictionary<DateTime, Candle>();
                foreach (var candle in list)
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
                var symbol = NormalizeProduct(productId);
                var url = _restBaseUrl + "/api/v3/ticker/bookTicker?symbol=" + Uri.EscapeDataString(symbol);
                var json = await HttpUtil.GetAsync(url).ConfigureAwait(false);
                var obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
                if (obj == null)
                {
                    obj = new Dictionary<string, object>();
                }

                var bid = ToDecimal(GetString(obj, "bidPrice"));
                var ask = ToDecimal(GetString(obj, "askPrice"));
                decimal last = 0m;

                if (bid > 0m && ask > 0m)
                {
                    last = (bid + ask) / 2m;
                }
                else
                {
                    var priceJson = await HttpUtil.GetAsync(_restBaseUrl + "/api/v3/ticker/price?symbol=" + Uri.EscapeDataString(symbol)).ConfigureAwait(false);
                    var priceObj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(priceJson);
                    last = ToDecimal(GetString(priceObj, "price"));
                }

                if (last <= 0m)
                {
                    throw new InvalidOperationException("Binance ticker response missing valid price for " + symbol + ".");
                }

                if (bid <= 0m) bid = last;
                if (ask <= 0m) ask = last;

                return new Ticker
                {
                    Bid = bid,
                    Ask = ask,
                    Last = last,
                    Time = DateTime.UtcNow
                };
            }).ConfigureAwait(false);
        }

        public async Task<FeeSchedule> GetFeesAsync()
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_apiSecret))
            {
                return new FeeSchedule { MakerRate = 0.0010m, TakerRate = 0.0010m, Notes = "binance default spot fees" };
            }

            try
            {
                var query = BuildSignedQuery(new Dictionary<string, string>());
                var req = new HttpRequestMessage(HttpMethod.Get, _restBaseUrl + "/sapi/v1/asset/tradeFee?" + query);
                req.Headers.TryAddWithoutValidation("X-MBX-APIKEY", _apiKey);
                var json = await HttpUtil.SendAsync(req).ConfigureAwait(false);
                var arr = UtilCompat.JsonDeserialize<object[]>(json);
                if (arr != null && arr.Length > 0)
                {
                    decimal maxMaker = 0m;
                    decimal maxTaker = 0m;
                    int validRows = 0;

                    foreach (var rowObj in arr)
                    {
                        var row = rowObj as Dictionary<string, object>;
                        if (row == null) continue;

                        var maker = ToDecimal(GetString(row, "makerCommission"));
                        var taker = ToDecimal(GetString(row, "takerCommission"));
                        if (maker <= 0m || taker <= 0m) continue;

                        validRows++;
                        if (maker > maxMaker) maxMaker = maker;
                        if (taker > maxTaker) maxTaker = taker;
                    }

                    if (validRows > 0)
                    {
                        return new FeeSchedule
                        {
                            MakerRate = maxMaker,
                            TakerRate = maxTaker,
                            Notes = "binance tradeFee worst-case across " + validRows + " symbol rows"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn("[BinanceClient] GetFees fallback: " + ex.Message);
            }

            return new FeeSchedule { MakerRate = 0.0010m, TakerRate = 0.0010m, Notes = "binance fallback fees" };
        }

        public async Task<OrderResult> PlaceOrderAsync(OrderRequest order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            EnsureCredentials();

            var payload = new Dictionary<string, string>
            {
                { "symbol", NormalizeProduct(order.ProductId) },
                { "side", order.Side == OrderSide.Buy ? "BUY" : "SELL" },
                { "type", order.Type == OrderType.Market ? "MARKET" : "LIMIT" },
                { "quantity", order.Quantity.ToString(CultureInfo.InvariantCulture) },
                { "newClientOrderId", string.IsNullOrWhiteSpace(order.ClientOrderId) ? "cdts-" + Guid.NewGuid().ToString("N") : order.ClientOrderId },
                { "recvWindow", "5000" }
            };

            if (order.Type == OrderType.Limit)
            {
                if (!order.Price.HasValue || order.Price.Value <= 0m)
                    throw new InvalidOperationException("Limit order requires positive price.");
                payload["price"] = order.Price.Value.ToString(CultureInfo.InvariantCulture);
                payload["timeInForce"] = order.Tif == TimeInForce.IOC ? "IOC" : (order.Tif == TimeInForce.FOK ? "FOK" : "GTC");
            }

            var query = BuildSignedQuery(payload);
            var req = new HttpRequestMessage(HttpMethod.Post, _restBaseUrl + "/api/v3/order");
            req.Headers.TryAddWithoutValidation("X-MBX-APIKEY", _apiKey);
            req.Content = new StringContent(query, Encoding.UTF8, "application/x-www-form-urlencoded");

            var json = await HttpUtil.SendAsync(req).ConfigureAwait(false);
            var obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();

            var orderId = GetString(obj, "orderId");
            var status = GetString(obj, "status");
            var executedQty = ToDecimal(GetString(obj, "executedQty"));
            var cummulative = ToDecimal(GetString(obj, "cummulativeQuoteQty"));
            var avg = executedQty > 0m && cummulative > 0m ? (cummulative / executedQty) : 0m;

            if (!string.IsNullOrWhiteSpace(orderId) && payload.ContainsKey("symbol") && !string.IsNullOrWhiteSpace(payload["symbol"]))
            {
                CacheOrderSymbol(orderId, payload["symbol"]);
            }

            return new OrderResult
            {
                OrderId = orderId,
                Accepted = !string.IsNullOrWhiteSpace(orderId),
                Filled = string.Equals(status, "FILLED", StringComparison.OrdinalIgnoreCase),
                FilledQty = executedQty,
                AvgFillPrice = avg,
                Message = string.IsNullOrWhiteSpace(status) ? "accepted" : status
            };
        }

        public async Task<bool> CancelOrderAsync(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return false;
            EnsureCredentials();

            string cachedSymbol;
            if (TryGetCachedOrderSymbol(orderId, out cachedSymbol) && !string.IsNullOrWhiteSpace(cachedSymbol))
            {
                var canceledFromCache = await TryCancelOrderBySymbolAsync(cachedSymbol, orderId).ConfigureAwait(false);
                if (canceledFromCache)
                {
                    RemoveCachedOrderSymbol(orderId);
                    return true;
                }
            }

            var allQuery = BuildSignedQuery(new Dictionary<string, string> { { "recvWindow", "5000" } });
            var allReq = new HttpRequestMessage(HttpMethod.Get, _restBaseUrl + "/api/v3/openOrders?" + allQuery);
            allReq.Headers.TryAddWithoutValidation("X-MBX-APIKEY", _apiKey);
            var allJson = await HttpUtil.SendAsync(allReq).ConfigureAwait(false);
            var all = UtilCompat.JsonDeserialize<object[]>(allJson);

            string symbol = string.Empty;
            if (all != null)
            {
                foreach (var rowObj in all)
                {
                    var row = rowObj as Dictionary<string, object>;
                    if (row == null) continue;
                    if (!string.Equals(ReadBinanceOrderId(row), orderId, StringComparison.OrdinalIgnoreCase)) continue;
                    symbol = NormalizeProduct(ReadBinanceSymbol(row));
                    if (!string.IsNullOrWhiteSpace(symbol))
                    {
                        CacheOrderSymbol(orderId, symbol);
                    }
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(symbol)) return false;

            var canceled = await TryCancelOrderBySymbolAsync(symbol, orderId).ConfigureAwait(false);
            if (canceled)
            {
                RemoveCachedOrderSymbol(orderId);
            }

            return canceled;
        }

        private async Task<bool> TryCancelOrderBySymbolAsync(string symbol, string orderId)
        {
            if (string.IsNullOrWhiteSpace(symbol) || string.IsNullOrWhiteSpace(orderId))
            {
                return false;
            }

            var payload = new Dictionary<string, string>
            {
                { "symbol", symbol },
                { "orderId", orderId },
                { "recvWindow", "5000" }
            };
            var query = BuildSignedQuery(payload);
            var req = new HttpRequestMessage(HttpMethod.Delete, _restBaseUrl + "/api/v3/order?" + query);
            req.Headers.TryAddWithoutValidation("X-MBX-APIKEY", _apiKey);
            try
            {
                var json = await HttpUtil.SendAsync(req).ConfigureAwait(false);
                var obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
                var canceledOrderId = ReadBinanceOrderId(obj);
                var status = GetString(obj, "status");

                if (!string.IsNullOrWhiteSpace(canceledOrderId)
                    && !string.Equals(canceledOrderId, orderId, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    return IsBinanceCanceledLikeStatus(status);
                }

                return !string.IsNullOrWhiteSpace(canceledOrderId);
            }
            catch (HttpRequestException)
            {
                return false;
            }
        }

        private void CacheOrderSymbol(string orderId, string symbol)
        {
            if (string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(symbol))
            {
                return;
            }

            lock (_orderSymbolCacheLock)
            {
                _orderSymbolByOrderId[orderId] = symbol;
            }
        }

        private bool TryGetCachedOrderSymbol(string orderId, out string symbol)
        {
            symbol = string.Empty;
            if (string.IsNullOrWhiteSpace(orderId))
            {
                return false;
            }

            lock (_orderSymbolCacheLock)
            {
                return _orderSymbolByOrderId.TryGetValue(orderId, out symbol);
            }
        }

        private void RemoveCachedOrderSymbol(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                return;
            }

            lock (_orderSymbolCacheLock)
            {
                if (_orderSymbolByOrderId.ContainsKey(orderId))
                {
                    _orderSymbolByOrderId.Remove(orderId);
                }
            }
        }

        public async Task<bool> CancelAllOpenOrdersAsync(string productId)
        {
            EnsureCredentials();
            var normalizedProduct = NormalizeProduct(productId);
            if (string.IsNullOrWhiteSpace(normalizedProduct))
            {
                return false;
            }

            var payload = new Dictionary<string, string>
            {
                { "symbol", normalizedProduct },
                { "recvWindow", "5000" }
            };
            var query = BuildSignedQuery(payload);
            var req = new HttpRequestMessage(HttpMethod.Delete, _restBaseUrl + "/api/v3/openOrders?" + query);
            req.Headers.TryAddWithoutValidation("X-MBX-APIKEY", _apiKey);
            var json = await HttpUtil.SendAsync(req).ConfigureAwait(false);

            var arr = UtilCompat.JsonDeserialize<object[]>(json);
            if (arr != null)
            {
                if (arr.Length == 0)
                {
                    return true;
                }

                for (int i = 0; i < arr.Length; i++)
                {
                    var row = arr[i] as Dictionary<string, object>;
                    if (row == null) return false;

                    var status = GetString(row, "status");
                    if (string.IsNullOrWhiteSpace(status)) return false;

                    if (!IsBinanceCanceledLikeStatus(status))
                    {
                        return false;
                    }

                    var rowOrderId = ReadBinanceOrderId(row);
                    if (!string.IsNullOrWhiteSpace(rowOrderId))
                    {
                        RemoveCachedOrderSymbol(rowOrderId);
                    }
                }

                return true;
            }

            return false;
        }

        private string NormalizeProduct(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return string.Empty;
            return symbol.Replace("/", "").Replace("-", "").ToUpperInvariant();
        }

        private string ReadBinanceOrderId(Dictionary<string, object> row)
        {
            var value = GetString(row, "orderId");
            if (string.IsNullOrWhiteSpace(value)) value = GetString(row, "order_id");
            if (string.IsNullOrWhiteSpace(value)) value = GetString(row, "id");
            return value;
        }

        private string ReadBinanceSymbol(Dictionary<string, object> row)
        {
            var value = GetString(row, "symbol");
            if (string.IsNullOrWhiteSpace(value)) value = GetString(row, "product_id");
            if (string.IsNullOrWhiteSpace(value)) value = GetString(row, "productId");
            return value;
        }

        private bool IsBinanceCanceledLikeStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return false;
            }

            return string.Equals(status, "CANCELED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "PENDING_CANCEL", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "EXPIRED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "EXPIRED_IN_MATCH", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "REJECTED", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<Dictionary<string, object>> GetExchangeInfoAsync()
        {
            var json = await HttpUtil.GetAsync(_restBaseUrl + "/api/v3/exchangeInfo").ConfigureAwait(false);
            return UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
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

                var obj = await GetExchangeInfoAsync().ConfigureAwait(false);
                var rebuilt = new Dictionary<string, SymbolConstraints>(StringComparer.OrdinalIgnoreCase);
                var symbols = ToObjectArray(obj != null && obj.ContainsKey("symbols") ? obj["symbols"] : null);
                foreach (var item in symbols)
                {
                    var row = item as Dictionary<string, object>;
                    if (row == null) continue;
                    var status = GetString(row, "status");
                    if (!string.Equals(status, "TRADING", StringComparison.OrdinalIgnoreCase)) continue;

                    var symbol = GetString(row, "symbol");
                    if (string.IsNullOrWhiteSpace(symbol)) continue;

                    var constraints = new SymbolConstraints
                    {
                        Symbol = symbol,
                        Source = "binance.exchangeInfo"
                    };

                    var filters = ToObjectArray(row.ContainsKey("filters") ? row["filters"] : null);
                    foreach (var filterObj in filters)
                    {
                        var filter = filterObj as Dictionary<string, object>;
                        if (filter == null) continue;
                        var filterType = GetString(filter, "filterType");

                        if (string.Equals(filterType, "LOT_SIZE", StringComparison.OrdinalIgnoreCase))
                        {
                            constraints.MinQty = ToDecimal(GetString(filter, "minQty"));
                            constraints.MaxQty = ToDecimal(GetString(filter, "maxQty"));
                            constraints.StepSize = ToDecimal(GetString(filter, "stepSize"));
                        }
                        else if (string.Equals(filterType, "MIN_NOTIONAL", StringComparison.OrdinalIgnoreCase))
                        {
                            constraints.MinNotional = ToDecimal(GetString(filter, "minNotional"));
                        }
                        else if (string.Equals(filterType, "NOTIONAL", StringComparison.OrdinalIgnoreCase))
                        {
                            var minNotional = ToDecimal(GetString(filter, "minNotional"));
                            if (minNotional > 0m)
                            {
                                constraints.MinNotional = minNotional;
                            }
                        }
                        else if (string.Equals(filterType, "PRICE_FILTER", StringComparison.OrdinalIgnoreCase))
                        {
                            constraints.PriceTickSize = ToDecimal(GetString(filter, "tickSize"));
                        }
                    }

                    rebuilt[symbol] = constraints;
                }

                _symbolConstraintsBySymbol = rebuilt;
                _constraintsFetchedUtc = DateTime.UtcNow;
            }
            finally
            {
                _constraintsLock.Release();
            }
        }

        private string NormalizeInterval(int granularityMinutes)
        {
            switch (granularityMinutes)
            {
                case 1: return "1m";
                case 3: return "3m";
                case 5: return "5m";
                case 15: return "15m";
                case 30: return "30m";
                case 60: return "1h";
                case 120: return "2h";
                case 240: return "4h";
                case 360: return "6h";
                case 720: return "12h";
                case 1440: return "1d";
                default: return granularityMinutes <= 1 ? "1m" : "5m";
            }
        }

        private long GetIntervalMilliseconds(int granularityMinutes)
        {
            switch (granularityMinutes)
            {
                case 1: return 60L * 1000L;
                case 3: return 3L * 60L * 1000L;
                case 5: return 5L * 60L * 1000L;
                case 15: return 15L * 60L * 1000L;
                case 30: return 30L * 60L * 1000L;
                case 60: return 60L * 60L * 1000L;
                case 120: return 120L * 60L * 1000L;
                case 240: return 240L * 60L * 1000L;
                case 360: return 360L * 60L * 1000L;
                case 720: return 720L * 60L * 1000L;
                case 1440: return 1440L * 60L * 1000L;
                default: return granularityMinutes <= 1 ? 60L * 1000L : 5L * 60L * 1000L;
            }
        }

        private string BuildSignedQuery(Dictionary<string, string> payload)
        {
            var p = payload ?? new Dictionary<string, string>();
            p["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);

            var sb = new StringBuilder();
            bool first = true;
            foreach (var kv in p)
            {
                if (!first) sb.Append("&");
                first = false;
                sb.Append(Uri.EscapeDataString(kv.Key));
                sb.Append("=");
                sb.Append(Uri.EscapeDataString(kv.Value ?? string.Empty));
            }

            var query = sb.ToString();
            var signature = ComputeHmacSha256HexLower(_apiSecret, query);
            return query + "&signature=" + signature;
        }

        private string ComputeHmacSha256HexLower(string secret, string payload)
        {
            using (var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret ?? string.Empty)))
            {
                var sig = h.ComputeHash(Encoding.UTF8.GetBytes(payload ?? string.Empty));
                var sb = new StringBuilder(sig.Length * 2);
                for (int i = 0; i < sig.Length; i++) sb.Append(sig[i].ToString("x2"));
                return sb.ToString();
            }
        }

        private void EnsureCredentials()
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_apiSecret))
                throw new InvalidOperationException("Binance credentials are required.");
        }

        private object[] ToObjectArray(object value)
        {
            var arr = value as object[];
            if (arr != null) return arr;
            var list = value as ArrayList;
            if (list != null) return list.ToArray();
            return new object[0];
        }

        private long ToUnixMs(DateTime dt)
        {
            return new DateTimeOffset(dt.ToUniversalTime()).ToUnixTimeMilliseconds();
        }

        private DateTime FromUnixMs(long ms)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
        }

        private long ToLong(object value)
        {
            if (value == null) return 0L;
            long v;
            if (long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v;
            return 0L;
        }

        private decimal ToDecimal(object value)
        {
            if (value == null) return 0m;
            decimal d;
            if (decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return d;
            return 0m;
        }

        private string GetString(Dictionary<string, object> obj, string key)
        {
            if (obj == null || string.IsNullOrWhiteSpace(key)) return string.Empty;

            object value;
            if (obj.TryGetValue(key, out value) && value != null)
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            }

            foreach (var pair in obj)
            {
                if (pair.Key == null) continue;
                if (!string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase)) continue;
                if (pair.Value == null) return string.Empty;
                return Convert.ToString(pair.Value, CultureInfo.InvariantCulture) ?? string.Empty;
            }

            return string.Empty;
        }

        private static string ResolveRestBaseUrl(string overrideBaseUrl)
        {
            if (!string.IsNullOrWhiteSpace(overrideBaseUrl))
            {
                return overrideBaseUrl.Trim().TrimEnd('/');
            }

            var configured = Environment.GetEnvironmentVariable("CDTS_BINANCE_BASE_URL");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured.Trim().TrimEnd('/');
            }

            return "https://api.binance.us";
        }
    }
}
