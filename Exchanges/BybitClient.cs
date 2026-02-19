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
    public class BybitClient : IExchangeClient
    {
        private string _apiKey;
        private string _apiSecret;
        private readonly string _restBaseUrl;
        private const string RecvWindow = "5000";
        private static readonly HttpClient _http = new HttpClient();
        private static readonly SemaphoreSlim _constraintsLock = new SemaphoreSlim(1, 1);
        private static DateTime _constraintsFetchedUtc = DateTime.MinValue;
        private static Dictionary<string, SymbolConstraints> _symbolConstraintsBySymbol = new Dictionary<string, SymbolConstraints>(StringComparer.OrdinalIgnoreCase);

        public BybitClient(string apiKey, string apiSecret, string restBaseUrl = null)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _restBaseUrl = ResolveRestBaseUrl(restBaseUrl);
        }

        public string Name => "Bybit";

        public void SetCredentials(string apiKey, string apiSecret, string passphrase = null)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
        }

        public async Task<List<string>> ListProductsAsync()
        {
            return await HttpUtil.RetryAsync(async () =>
            {
                var root = await GetInstrumentsInfoAsync().ConfigureAwait(false);
                var output = new List<string>();

                var result = GetMap(root, "result");
                var list = ToObjectArray(result != null && result.ContainsKey("list") ? result["list"] : null);
                foreach (var rowObj in list)
                {
                    var row = rowObj as Dictionary<string, object>;
                    if (row == null) continue;
                    var status = GetString(row, "status");
                    if (!string.Equals(status, "Trading", StringComparison.OrdinalIgnoreCase)) continue;

                    var baseCoin = GetString(row, "baseCoin");
                    var quoteCoin = GetString(row, "quoteCoin");
                    if (string.IsNullOrWhiteSpace(baseCoin) || string.IsNullOrWhiteSpace(quoteCoin)) continue;
                    output.Add(baseCoin.ToUpperInvariant() + "/" + quoteCoin.ToUpperInvariant());
                }

                output.Sort(StringComparer.OrdinalIgnoreCase);
                return output;
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
                var normalizedGranularity = NormalizeGranularityMinutes(granularity);
                var interval = NormalizeInterval(normalizedGranularity);
                var intervalMs = (long)normalizedGranularity * 60L * 1000L;
                var startMs = ToUnixMs(startUtc);
                var endMs = ToUnixMs(endUtc);

                if (endMs < startMs)
                {
                    return new List<Candle>();
                }

                var outRows = new List<Candle>();
                var cursorMs = startMs;

                while (cursorMs <= endMs)
                {
                    var url = _restBaseUrl + "/v5/market/kline?category=spot&symbol=" + Uri.EscapeDataString(symbol)
                        + "&interval=" + Uri.EscapeDataString(interval)
                        + "&start=" + cursorMs
                        + "&end=" + endMs
                        + "&limit=1000";

                    var json = await HttpUtil.GetAsync(url).ConfigureAwait(false);
                    var root = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
                    if (!IsBybitSuccess(root))
                    {
                        var code = GetString(root, "retCode");
                        var message = GetString(root, "retMsg");
                        throw new InvalidOperationException("Bybit kline request failed (retCode=" + code + "): " + message);
                    }

                    var result = GetMap(root, "result");
                    var list = ToObjectArray(result != null && result.ContainsKey("list") ? result["list"] : null);
                    if (list.Length == 0)
                    {
                        break;
                    }

                    long maxOpenTimeMs = 0L;
                    foreach (var rowObj in list)
                    {
                        var row = ToObjectArray(rowObj);
                        if (row.Length < 6) continue;

                        var openTimeMs = ToLong(row[0]);
                        if (openTimeMs > maxOpenTimeMs) maxOpenTimeMs = openTimeMs;

                        DateTime ts;
                        if (!TryFromUnixMilliseconds(openTimeMs, out ts))
                        {
                            continue;
                        }

                        if (ts < startUtc || ts > endUtc) continue;

                        outRows.Add(new Candle
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
                    if (list.Length < 1000)
                    {
                        break;
                    }
                }

                var dedupByTime = new Dictionary<DateTime, Candle>();
                foreach (var candle in outRows)
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
                var json = await HttpUtil.GetAsync(_restBaseUrl + "/v5/market/tickers?category=spot&symbol=" + Uri.EscapeDataString(symbol)).ConfigureAwait(false);
                var root = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
                if (!IsBybitSuccess(root))
                {
                    var code = GetString(root, "retCode");
                    var message = GetString(root, "retMsg");
                    throw new InvalidOperationException("Bybit ticker request failed (retCode=" + code + "): " + message);
                }

                var result = GetMap(root, "result");
                var list = ToObjectArray(result != null && result.ContainsKey("list") ? result["list"] : null);
                if (list.Length == 0)
                {
                    throw new InvalidOperationException("Bybit ticker request returned no rows.");
                }

                var row = list[0] as Dictionary<string, object>;
                if (row == null)
                {
                    throw new InvalidOperationException("Bybit ticker payload had invalid row shape.");
                }

                var bid = ToDecimal(GetString(row, "bid1Price"));
                var ask = ToDecimal(GetString(row, "ask1Price"));
                var last = ToDecimal(GetString(row, "lastPrice"));
                if (last <= 0m && bid > 0m && ask > 0m && ask >= bid)
                {
                    last = (bid + ask) / 2m;
                }

                if (last <= 0m)
                {
                    throw new InvalidOperationException("Bybit ticker payload did not provide a valid last price.");
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
                return new FeeSchedule { MakerRate = 0.0010m, TakerRate = 0.0010m, Notes = "bybit default spot fees" };
            }

            try
            {
                var root = await QueryFeeRateAsync("category=spot").ConfigureAwait(false);
                var fee = BuildWorstCaseFeeSchedule(root, "bybit fee-rate");
                if (fee != null)
                {
                    return fee;
                }

                root = await QueryFeeRateAsync("category=spot&symbol=BTCUSDT").ConfigureAwait(false);
                fee = BuildWorstCaseFeeSchedule(root, "bybit fee-rate (BTCUSDT fallback)");
                if (fee != null)
                {
                    return fee;
                }
            }
            catch (Exception ex)
            {
                Log.Warn("[BybitClient] GetFees fallback: " + ex.Message);
            }

            return new FeeSchedule { MakerRate = 0.0010m, TakerRate = 0.0010m, Notes = "bybit fallback fees" };
        }

        public async Task<Dictionary<string, decimal>> GetBalancesAsync()
        {
            EnsureCredentials();

            var req = BuildSignedRequest(HttpMethod.Get, "/v5/account/wallet-balance", "accountType=UNIFIED", string.Empty);
            var json = await HttpUtil.SendAsync(req).ConfigureAwait(false);
            var root = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
            if (!IsBybitSuccess(root))
            {
                throw new InvalidOperationException("bybit wallet-balance request failed");
            }

            var balances = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var result = GetMap(root, "result");
            var accounts = ToObjectArray(result != null && result.ContainsKey("list") ? result["list"] : null);
            foreach (var accountObj in accounts)
            {
                var account = accountObj as Dictionary<string, object>;
                if (account == null) continue;

                var coins = ToObjectArray(account.ContainsKey("coin") ? account["coin"] : null);
                foreach (var coinObj in coins)
                {
                    var coin = coinObj as Dictionary<string, object>;
                    if (coin == null) continue;

                    var currency = GetString(coin, "coin");
                    if (string.IsNullOrWhiteSpace(currency)) continue;

                    /* 
                       Standardize on Total Equity (Wallet Balance) to match Binance/Coinbase semantics. 
                       Original logic returned 'transferBalance' (Available) which caused sizing to collapse 
                       when positions were open.
                    */
                    var total = ToDecimal(GetString(coin, "walletBalance"));
                    if (total <= 0m)
                    {
                        total = ToDecimal(GetString(coin, "equity"));
                    }
                    if (total <= 0m)
                    {
                        /* Fallback to transferBalance only if total is missing/zero (e.g. Funding account) */
                        total = ToDecimal(GetString(coin, "transferBalance"));
                    }

                    decimal existing;
                    if (balances.TryGetValue(currency, out existing))
                    {
                        balances[currency] = existing + total;
                    }
                    else
                    {
                        balances[currency] = total;
                    }
                }
            }

            return balances;
        }

        public async Task<List<OpenOrder>> GetOpenOrdersAsync(string productId = null)
        {
            EnsureCredentials();

            var query = "category=spot";
            if (!string.IsNullOrWhiteSpace(productId))
            {
                query += "&symbol=" + NormalizeProduct(productId);
            }

            var req = BuildSignedRequest(HttpMethod.Get, "/v5/order/realtime", query, string.Empty);
            var json = await HttpUtil.SendAsync(req).ConfigureAwait(false);
            var result = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();

            var list = new List<OpenOrder>();
            var data = GetMap(result, "result");
            if (data != null && data.ContainsKey("list"))
            {
                var orders = ToObjectArray(data["list"]);
                foreach (var item in orders)
                {
                    var map = item as Dictionary<string, object>;
                    if (map == null) continue;

                    var orderId = GetString(map, "orderId");
                    var symbol = GetString(map, "symbol");
                    if (string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(symbol))
                    {
                        continue;
                    }
                    var side = GetString(map, "side");
                    if (!string.Equals(side, "BUY", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(side, "SELL", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(side, "Buy", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(side, "Sell", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    var type = GetString(map, "orderType");
                    var price = ToDecimal(GetString(map, "price"));
                    var qty = ToDecimal(GetString(map, "qty"));
                    var filled = ToDecimal(GetString(map, "cumExecQty"));
                    var status = NormalizeOpenOrderStatus(GetString(map, "orderStatus"));
                    if (string.IsNullOrWhiteSpace(status))
                    {
                        continue;
                    }
                    var created = ToDecimal(GetString(map, "createdTime"));
                    DateTime createdUtc;
                    if (!TryFromUnixMilliseconds((long)created, out createdUtc))
                    {
                        continue;
                    }

                    list.Add(new OpenOrder
                    {
                        OrderId = orderId,
                        ProductId = symbol,
                        Side = "BUY".Equals(side, StringComparison.OrdinalIgnoreCase) ? OrderSide.Buy : OrderSide.Sell,
                        Type = "MARKET".Equals(type, StringComparison.OrdinalIgnoreCase) ? OrderType.Market : OrderType.Limit,
                        Price = price,
                        Quantity = qty,
                        FilledQty = filled,
                        Status = status,
                        CreatedUtc = createdUtc
                    });
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

            if (status == "NEW" || status == "OPEN" || status == "ACTIVE" || status == "WORKING" || status == "PENDING" || status == "RESTING" || status == "LIVE" || status == "UNTRIGGERED")
            {
                return "OPEN";
            }

            if (status == "PARTIALLY_FILLED")
            {
                return "PARTIALLY_FILLED";
            }

            return string.Empty;
        }

        public async Task<OrderResult> PlaceOrderAsync(OrderRequest order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            EnsureCredentials();

            var bodyMap = new Dictionary<string, object>
            {
                { "category", "spot" },
                { "symbol", NormalizeProduct(order.ProductId) },
                { "side", order.Side == OrderSide.Buy ? "Buy" : "Sell" },
                { "orderType", order.Type == OrderType.Market ? "Market" : "Limit" },
                { "qty", order.Quantity.ToString(CultureInfo.InvariantCulture) },
                { "timeInForce", order.Tif == TimeInForce.IOC ? "IOC" : (order.Tif == TimeInForce.FOK ? "FOK" : "GTC") }
            };

            if (order.Type == OrderType.Limit)
            {
                if (!order.Price.HasValue || order.Price.Value <= 0m)
                    throw new InvalidOperationException("Limit order requires positive price.");
                bodyMap["price"] = order.Price.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrWhiteSpace(order.ClientOrderId))
            {
                bodyMap["orderLinkId"] = order.ClientOrderId;
            }

            var body = UtilCompat.JsonSerialize(bodyMap);
            var req = BuildSignedRequest(HttpMethod.Post, "/v5/order/create", string.Empty, body);
            var json = await HttpUtil.SendAsync(req).ConfigureAwait(false);
            var root = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
            var result = GetMap(root, "result");

            var orderId = GetString(result, "orderId");
            var success = IsBybitSuccess(root);
            var message = success
                ? (!string.IsNullOrWhiteSpace(orderId) ? "accepted" : "accepted-no-order-id")
                : GetString(root, "retMsg");
            if (string.IsNullOrWhiteSpace(message))
            {
                message = success ? "accepted" : "error";
            }

            return new OrderResult
            {
                OrderId = orderId,
                Accepted = success && !string.IsNullOrWhiteSpace(orderId),
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

            var openReq = BuildSignedRequest(HttpMethod.Get, "/v5/order/realtime", "category=spot&openOnly=0", string.Empty);
            var openJson = await HttpUtil.SendAsync(openReq).ConfigureAwait(false);
            var openRoot = UtilCompat.JsonDeserialize<Dictionary<string, object>>(openJson);
            if (!IsBybitSuccess(openRoot))
            {
                return false;
            }

            var openResult = GetMap(openRoot, "result");
            var list = ToObjectArray(openResult != null && openResult.ContainsKey("list") ? openResult["list"] : null);

            string symbol = string.Empty;
            foreach (var rowObj in list)
            {
                var row = rowObj as Dictionary<string, object>;
                if (row == null) continue;
                if (!string.Equals(ReadBybitOrderId(row), orderId, StringComparison.OrdinalIgnoreCase)) continue;
                symbol = NormalizeProduct(ReadBybitSymbol(row));
                break;
            }

            if (string.IsNullOrWhiteSpace(symbol)) return false;

            var body = UtilCompat.JsonSerialize(new Dictionary<string, object>
            {
                { "category", "spot" },
                { "symbol", symbol },
                { "orderId", orderId }
            });

            var cancelReq = BuildSignedRequest(HttpMethod.Post, "/v5/order/cancel", string.Empty, body);
            var cancelJson = await HttpUtil.SendAsync(cancelReq).ConfigureAwait(false);
            var cancelRoot = UtilCompat.JsonDeserialize<Dictionary<string, object>>(cancelJson);
            if (!IsBybitSuccess(cancelRoot))
            {
                return false;
            }

            return IsBybitCancelOrderResponseForOrder(cancelRoot, orderId);
        }

        public async Task<bool> CancelAllOpenOrdersAsync(string productId)
        {
            EnsureCredentials();
            var normalizedProduct = NormalizeProduct(productId);
            if (string.IsNullOrWhiteSpace(normalizedProduct))
            {
                return false;
            }

            var body = UtilCompat.JsonSerialize(new Dictionary<string, object>
            {
                { "category", "spot" },
                { "symbol", normalizedProduct }
            });
            var req = BuildSignedRequest(HttpMethod.Post, "/v5/order/cancel-all", string.Empty, body);
            var json = await HttpUtil.SendAsync(req).ConfigureAwait(false);
            var root = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
            if (!IsBybitSuccess(root))
            {
                return false;
            }

            var result = GetMap(root, "result");
            var list = ToObjectArray(result != null && result.ContainsKey("list") ? result["list"] : null);
            if (list.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < list.Length; i++)
            {
                var row = list[i] as Dictionary<string, object>;
                if (row == null)
                {
                    return false;
                }

                if (!IsBybitCancelAllRowSuccess(row))
                {
                    return false;
                }
            }

            return true;
        }

        private string ReadBybitOrderId(Dictionary<string, object> row)
        {
            var value = GetString(row, "orderId");
            if (string.IsNullOrWhiteSpace(value)) value = GetString(row, "order_id");
            if (string.IsNullOrWhiteSpace(value)) value = GetString(row, "orderID");
            return value;
        }

        private string ReadBybitSymbol(Dictionary<string, object> row)
        {
            var value = GetString(row, "symbol");
            if (string.IsNullOrWhiteSpace(value)) value = GetString(row, "product_id");
            if (string.IsNullOrWhiteSpace(value)) value = GetString(row, "productId");
            if (string.IsNullOrWhiteSpace(value)) value = GetString(row, "instrument_id");
            if (string.IsNullOrWhiteSpace(value)) value = GetString(row, "instrumentId");
            return value;
        }

        private bool IsBybitCancelAllRowSuccess(Dictionary<string, object> row)
        {
            if (row == null)
            {
                return false;
            }

            var success = GetString(row, "success");
            if (!string.IsNullOrWhiteSpace(success))
            {
                if (string.Equals(success, "1", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(success, "true", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(success, "ok", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(success, "success", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return false;
            }

            var sCode = GetString(row, "sCode");
            if (!string.IsNullOrWhiteSpace(sCode))
            {
                return string.Equals(sCode, "0", StringComparison.OrdinalIgnoreCase);
            }

            var retCode = GetString(row, "retCode");
            if (!string.IsNullOrWhiteSpace(retCode))
            {
                return string.Equals(retCode, "0", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private bool IsBybitCancelOrderResponseForOrder(Dictionary<string, object> root, string orderId)
        {
            if (root == null || string.IsNullOrWhiteSpace(orderId))
            {
                return false;
            }

            var result = GetMap(root, "result");
            var list = ToObjectArray(result != null && result.ContainsKey("list") ? result["list"] : null);
            if (list.Length == 0)
            {
                list = ToObjectArray(result != null && result.ContainsKey("data") ? result["data"] : null);
            }

            if (list.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < list.Length; i++)
            {
                var row = list[i] as Dictionary<string, object>;
                if (row == null)
                {
                    continue;
                }

                var rowOrderId = ReadBybitOrderId(row);
                if (!string.IsNullOrWhiteSpace(rowOrderId)
                    && !string.Equals(rowOrderId, orderId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return IsBybitCancelAllRowSuccess(row);
            }

            return false;
        }

        private async Task<Dictionary<string, object>> QueryFeeRateAsync(string query)
        {
            var req = BuildSignedRequest(HttpMethod.Get, "/v5/account/fee-rate", query, string.Empty);
            var json = await HttpUtil.SendAsync(req).ConfigureAwait(false);
            return UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
        }

        private FeeSchedule BuildWorstCaseFeeSchedule(Dictionary<string, object> root, string notePrefix)
        {
            var result = GetMap(root, "result");
            var list = ToObjectArray(result != null && result.ContainsKey("list") ? result["list"] : null);
            if (list.Length == 0)
            {
                return null;
            }

            decimal maxMaker = 0m;
            decimal maxTaker = 0m;
            int validRows = 0;

            foreach (var rowObj in list)
            {
                var row = rowObj as Dictionary<string, object>;
                if (row == null) continue;

                var maker = ToDecimal(GetString(row, "makerFeeRate"));
                var taker = ToDecimal(GetString(row, "takerFeeRate"));
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

        private bool IsBybitSuccess(Dictionary<string, object> root)
        {
            var codeText = GetString(root, "retCode");
            long code;
            if (!long.TryParse(codeText, NumberStyles.Any, CultureInfo.InvariantCulture, out code))
            {
                return false;
            }

            return code == 0L;
        }

        private HttpRequestMessage BuildSignedRequest(HttpMethod method, string path, string query, string body)
        {
            EnsureCredentials();
            var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
            var payload = ts + _apiKey + RecvWindow + (method == HttpMethod.Get ? (query ?? string.Empty) : (body ?? string.Empty));
            var sign = ComputeHmacSha256HexLower(_apiSecret, payload);
            var url = _restBaseUrl + path + (string.IsNullOrWhiteSpace(query) ? string.Empty : "?" + query);

            var req = new HttpRequestMessage(method, url);
            req.Headers.TryAddWithoutValidation("X-BAPI-API-KEY", _apiKey);
            req.Headers.TryAddWithoutValidation("X-BAPI-TIMESTAMP", ts);
            req.Headers.TryAddWithoutValidation("X-BAPI-RECV-WINDOW", RecvWindow);
            req.Headers.TryAddWithoutValidation("X-BAPI-SIGN", sign);

            if (method != HttpMethod.Get)
            {
                req.Content = new StringContent(body ?? "{}", Encoding.UTF8, "application/json");
            }

            return req;
        }

        private async Task<Dictionary<string, object>> GetInstrumentsInfoAsync(string category = "spot")
        {
            var normalizedCategory = string.IsNullOrWhiteSpace(category) ? "spot" : category.Trim().ToLowerInvariant();
            var url = _restBaseUrl + "/v5/market/instruments-info?category=" + Uri.EscapeDataString(normalizedCategory) + "&limit=1000";
            var json = await HttpUtil.GetAsync(url).ConfigureAwait(false);
            var root = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
            if (root == null)
            {
                throw new InvalidOperationException("Bybit instruments-info returned empty response payload.");
            }

            if (!IsBybitSuccess(root))
            {
                var code = GetString(root, "retCode");
                var message = GetString(root, "retMsg");
                throw new InvalidOperationException("Bybit instruments-info failed (category=" + normalizedCategory + ", retCode=" + code + "): " + message);
            }

            return root;
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

                var root = await GetInstrumentsInfoAsync().ConfigureAwait(false);
                var rebuilt = new Dictionary<string, SymbolConstraints>(StringComparer.OrdinalIgnoreCase);
                var result = GetMap(root, "result");
                var list = ToObjectArray(result != null && result.ContainsKey("list") ? result["list"] : null);
                foreach (var rowObj in list)
                {
                    var row = rowObj as Dictionary<string, object>;
                    if (row == null) continue;
                    var status = GetString(row, "status");
                    if (!string.Equals(status, "Trading", StringComparison.OrdinalIgnoreCase)) continue;

                    var symbol = GetString(row, "symbol");
                    if (string.IsNullOrWhiteSpace(symbol)) continue;

                    var constraints = new SymbolConstraints
                    {
                        Symbol = symbol,
                        Source = "bybit.instruments-info"
                    };

                    var lotSize = GetMap(row, "lotSizeFilter");
                    if (lotSize != null)
                    {
                        constraints.MinQty = ToDecimal(GetString(lotSize, "minOrderQty"));
                        constraints.MaxQty = ToDecimal(GetString(lotSize, "maxOrderQty"));
                        constraints.StepSize = ToDecimal(GetString(lotSize, "qtyStep"));

                        var minOrderAmt = ToDecimal(GetString(lotSize, "minOrderAmt"));
                        if (minOrderAmt > 0m)
                        {
                            constraints.MinNotional = minOrderAmt;
                        }
                    }

                    var priceFilter = GetMap(row, "priceFilter");
                    if (priceFilter != null)
                    {
                        constraints.PriceTickSize = ToDecimal(GetString(priceFilter, "tickSize"));
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

        private string NormalizeProduct(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return string.Empty;
            return symbol.Replace("/", "").Replace("-", "").ToUpperInvariant();
        }

        private string NormalizeInterval(int granularityMinutes)
        {
            switch (granularityMinutes)
            {
                case 1: return "1";
                case 3: return "3";
                case 5: return "5";
                case 15: return "15";
                case 30: return "30";
                case 60: return "60";
                case 120: return "120";
                case 240: return "240";
                case 360: return "360";
                case 720: return "720";
                case 1440: return "D";
                default: return granularityMinutes <= 1 ? "1" : "5";
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

        private Dictionary<string, object> GetMap(Dictionary<string, object> root, string key)
        {
            if (root == null || string.IsNullOrWhiteSpace(key) || !root.ContainsKey(key)) return null;
            return root[key] as Dictionary<string, object>;
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
            long v;
            if (long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v;
            return 0;
        }

        private bool TryFromUnixMilliseconds(long ms, out DateTime utc)
        {
            utc = DateTime.MinValue;
            if (ms <= 0)
            {
                return false;
            }

            try
            {
                utc = DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
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
                throw new InvalidOperationException("Bybit credentials are required.");
        }

        private static string ResolveRestBaseUrl(string overrideBaseUrl)
        {
            if (!string.IsNullOrWhiteSpace(overrideBaseUrl))
            {
                return overrideBaseUrl.Trim().TrimEnd('/');
            }

            var configured = Environment.GetEnvironmentVariable("CDTS_BYBIT_BASE_URL");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured.Trim().TrimEnd('/');
            }

            return "https://api.bybit.com";
        }
    }
}
