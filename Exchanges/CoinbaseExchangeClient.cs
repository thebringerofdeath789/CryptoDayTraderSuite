/* File: Exchanges/CoinbaseExchangeClient.cs */
/* Author: Gregory King */
/* Date: 2025-08-10 */
/* Description: Coinbase REST client (formerly GDAX) for public data and private trading */
/* Functions: ctor, Name, NormalizeProduct, DenormalizeProduct, PrivatePostAsync, ListProductsAsync, GetCandlesAsync, GetTickerAsync, GetFeesAsync, GetBalancesAsync, PlaceOrderAsync, CancelOrderAsync */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Exchanges
{
	public class CoinbaseExchangeClient : IExchangeClient
	{
		private string _key; /* api key */
		private string _secretBase64; /* secret base64 */
		private string _passphrase; /* passphrase */
		private const string Rest = "https://api.exchange.coinbase.com"; /* legacy base */
		private const string BrokerageRest = "https://api.coinbase.com"; /* advanced trade base */
		private const int PrivateRequestTimeoutSeconds = 20;
        private static readonly HttpClient _http = new HttpClient(); /* shared client */
		private static readonly SemaphoreSlim _constraintsLock = new SemaphoreSlim(1, 1);
		private static DateTime _constraintsFetchedUtc = DateTime.MinValue;
		private static Dictionary<string, SymbolConstraints> _symbolConstraintsBySymbol = new Dictionary<string, SymbolConstraints>(StringComparer.OrdinalIgnoreCase);

		public CoinbaseExchangeClient(string key, string secretBase64, string passphrase)
		{
			SetCredentials(key, secretBase64, passphrase);
		} /* ctor */

        public void SetCredentials(string apiKey, string apiSecret, string passphrase = null)
        {
			string apiKeyName = null;
			string ecPrivateKeyPem = null;
			CoinbaseCredentialNormalizer.NormalizeCoinbaseAdvancedInputs(ref apiKey, ref apiSecret, ref apiKeyName, ref ecPrivateKeyPem);

			_key = !string.IsNullOrWhiteSpace(apiKeyName) ? apiKeyName : apiKey;
			_secretBase64 = !string.IsNullOrWhiteSpace(ecPrivateKeyPem) ? ecPrivateKeyPem : apiSecret;
            _passphrase = passphrase;
        }

		public string Name { get { return "Coinbase"; } } /* name */

		public string NormalizeProduct(string uiSymbol)
		{
			if (string.IsNullOrWhiteSpace(uiSymbol)) return string.Empty;
			return uiSymbol.Replace("/", "-");
		} /* e.g. BTC-USD */
		public string DenormalizeProduct(string apiSymbol)
		{
			if (string.IsNullOrWhiteSpace(apiSymbol)) return string.Empty;
			return apiSymbol.Replace("-", "/");
		} /* to ui */

		private bool IsAdvancedCredentialAuth
		{
			get { return CoinbaseJwtUtil.IsAdvancedCredentialShape(_key, _secretBase64); }
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

		private async Task<string> PrivateRequestAsync(string method, string path, string body = "")
		{
            return await HttpUtil.RetryAsync(async () =>
            {
			    if (!IsAdvancedCredentialAuth)
			    {
				    throw new InvalidOperationException("Coinbase private API requires Advanced credentials (API key name + EC private key PEM). Legacy key/secret/passphrase auth is not supported.");
			    }

			    return await PrivateRequestAdvancedAsync(method, path, body).ConfigureAwait(false);
            });
		}

		private async Task<string> TryPrivateRequestAsync(string method, string path, string body = "")
		{
			try
			{
				return await PrivateRequestAsync(method, path, body).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Log.Warn("[Coinbase] Private request failed for " + method + " " + path + ": " + ex.Message);
				return string.Empty;
			}
		}

		private async Task<string> PrivateRequestAdvancedAsync(string method, string path, string body = "")
		{
			if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("/"))
			{
				throw new InvalidOperationException("Advanced Coinbase request path must start with '/'.");
			}

			var uri = new Uri(BrokerageRest + path);
			var methodUpper = method == null ? "GET" : method.ToUpperInvariant();
			var jwt = CoinbaseJwtUtil.CreateJwt(_key, _secretBase64, methodUpper, uri.Host, uri.AbsolutePath);

			try
			{
				using (var req = new HttpRequestMessage(new HttpMethod(methodUpper), uri))
				{
					req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
					req.Headers.Add("User-Agent", "CryptoDayTraderSuite");

					if (!string.IsNullOrWhiteSpace(body))
					{
						req.Content = new StringContent(body, Encoding.UTF8, "application/json");
					}

					using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(PrivateRequestTimeoutSeconds)))
					using (var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseContentRead, timeout.Token).ConfigureAwait(false))
					{
						var respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
						if (!resp.IsSuccessStatusCode)
						{
							throw new HttpRequestException("Coinbase private request failed: "
								+ methodUpper + " " + path + " -> "
								+ ((int)resp.StatusCode).ToString(CultureInfo.InvariantCulture) + " " + resp.ReasonPhrase
								+ " | body=" + TrimForError(respBody));
						}
						return respBody;
					}
				}
			}
			catch (TaskCanceledException ex)
			{
				throw new TimeoutException("Coinbase private request timeout after " + PrivateRequestTimeoutSeconds.ToString(CultureInfo.InvariantCulture) + "s: " + methodUpper + " " + path, ex);
			}
		}

		private string TrimForError(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return string.Empty;
			}

			const int max = 600;
			if (value.Length <= max)
			{
				return value;
			}

			return value.Substring(0, max) + "...";
		}

		public async Task<List<string>> ListProductsAsync()
		{
            return await HttpUtil.RetryAsync(async () => 
            {
			    var arr = await GetProductsPayloadAsync().ConfigureAwait(false); /* parse */
				var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			    var res = new List<string>();
				foreach (var p in arr)
				{
					var id = ResolveProductId(p);
					if (string.IsNullOrWhiteSpace(id)) continue;
					if (!seen.Add(id)) continue;
					res.Add(id);
				}
			    return res; /* return */
            });
		}

		public async Task<List<Candle>> GetCandlesAsync(string productId, int minutes, DateTime startUtc, DateTime endUtc)
		{
            return await HttpUtil.RetryAsync(async () => 
            {
			    var pair = NormalizeProduct(productId); /* pair */
			    var normalizedMinutes = NormalizeGranularityMinutes(minutes);
			    if (normalizedMinutes != Math.Max(1, minutes))
			    {
				    Log.Warn($"[Coinbase] Unsupported granularity '{minutes}m' normalized to '{normalizedMinutes}m'.");
			    }
			    var gran = normalizedMinutes * 60; /* seconds */
			    const int maxBucketsPerRequest = 280; /* keep below exchange hard limit */
			    var maxSpan = TimeSpan.FromSeconds(gran * maxBucketsPerRequest);

			    var res = new List<Candle>();
			    var cursor = startUtc;
			    while (cursor < endUtc)
			    {
				    var chunkEnd = cursor.Add(maxSpan);
				    if (chunkEnd > endUtc) chunkEnd = endUtc;

				    var url = Rest + "/products/" + pair + "/candles?granularity=" + gran + "&start=" + cursor.ToString("o") + "&end=" + chunkEnd.ToString("o"); /* url */
				    var json = await HttpUtil.GetAsync(url); /* get */
				    var arr = UtilCompat.JsonDeserialize<object[]>(json); /* parse */
				    if (arr != null) foreach (var o in arr)
				    {
					    var row = o as object[]; if (row == null || row.Length < 6) continue; /* check */

						long epochSeconds;
						decimal low;
						decimal high;
						decimal open;
						decimal close;
						decimal volume;

						if (!TryReadLong(row[0], out epochSeconds)) continue;
						if (!TryReadDecimal(row[1], out low)) continue;
						if (!TryReadDecimal(row[2], out high)) continue;
						if (!TryReadDecimal(row[3], out open)) continue;
						if (!TryReadDecimal(row[4], out close)) continue;
						if (!TryReadDecimal(row[5], out volume)) continue;

						res.Add(new Candle
						{
							Time = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epochSeconds),
							Low = low,
							High = high,
							Open = open,
							Close = close,
							Volume = volume
						}); /* add */
				    }

					cursor = chunkEnd;
			    }

			    var dedup = new Dictionary<DateTime, Candle>();
			    foreach (var candle in res) dedup[candle.Time] = candle;
			    var merged = new List<Candle>(dedup.Values);
			    merged.Sort((a, b) => a.Time.CompareTo(b.Time)); /* sort */
			    return merged; /* return */
            });
		}

		private int NormalizeGranularityMinutes(int minutes)
		{
			var requested = Math.Max(1, minutes);
			int[] supported = { 1, 5, 15, 60, 360, 1440 };

			if (Array.IndexOf(supported, requested) >= 0) return requested;

			var closest = supported[0];
			var bestDiff = Math.Abs(requested - closest);
			for (int i = 1; i < supported.Length; i++)
			{
				var diff = Math.Abs(requested - supported[i]);
				if (diff < bestDiff)
				{
					bestDiff = diff;
					closest = supported[i];
				}
			}

			return closest;
		}

		public async Task<Ticker> GetTickerAsync(string productId)
		{
            return await HttpUtil.RetryAsync(async () => 
            {
			    var pair = NormalizeProduct(productId); /* pair */
			    var url = Rest + "/products/" + pair + "/ticker"; /* url */
			    var json = await HttpUtil.GetAsync(url); /* get */
			    var obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase); /* parse */

				var bid = ReadDecimalValue(obj, "bid");
				var ask = ReadDecimalValue(obj, "ask");
				var last = ReadDecimalValue(obj, "price");
				if (last <= 0m) last = ReadDecimalValue(obj, "last");
				if (last <= 0m) last = ReadDecimalValue(obj, "trade_price");
				if (last <= 0m)
				{
					throw new InvalidOperationException("Coinbase ticker response missing valid last price for " + pair + ".");
				}

				if (bid <= 0m) bid = last;
				if (ask <= 0m) ask = last;

				var time = ReadDateTimeValue(obj, "time");
				if (time == DateTime.MinValue)
				{
					time = DateTime.UtcNow;
				}

			    return new Ticker { Bid = bid, Ask = ask, Last = last, Time = time }; /* ticker */
            });
		}

		public async Task<FeeSchedule> GetFeesAsync()
		{
			var jsonAdvanced = await PrivateRequestAsync("GET", "/api/v3/brokerage/transaction_summary").ConfigureAwait(false);
			var objAdvanced = UtilCompat.JsonDeserialize<Dictionary<string, object>>(jsonAdvanced) ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			var feeTier = ReadObject(objAdvanced, "fee_tier");
			if (feeTier == null)
			{
				feeTier = ReadObject(objAdvanced, "feeTier");
			}
			if (feeTier == null)
			{
				var tiers = ReadObjectList(objAdvanced, "fee_tiers");
				if (tiers.Count > 0) feeTier = tiers[0];
			}
			if (feeTier == null)
			{
				var tiers = ReadObjectList(objAdvanced, "feeTiers");
				if (tiers.Count > 0) feeTier = tiers[0];
			}

			var maker = ReadDecimalByCandidates(feeTier, "maker_fee_rate", "makerRate", "maker");
			var taker = ReadDecimalByCandidates(feeTier, "taker_fee_rate", "takerRate", "taker");

			if (maker <= 0m || taker <= 0m)
			{
				throw new InvalidOperationException("Coinbase transaction_summary did not provide valid maker/taker fee rates.");
			}

			return new FeeSchedule
			{
				MakerRate = maker,
				TakerRate = taker,
				Notes = "from brokerage/transaction_summary"
			};
		}

		public async Task<Dictionary<string, decimal>> GetBalancesAsync()
		{
			var jsonAdvanced = await PrivateRequestAsync("GET", "/api/v3/brokerage/accounts").ConfigureAwait(false);
			var root = UtilCompat.JsonDeserialize<Dictionary<string, object>>(jsonAdvanced) ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			var accounts = ReadObjectList(root, "accounts");
			if (accounts.Count == 0)
			{
				accounts = ReadObjectList(root, "results");
			}
			if (accounts.Count == 0)
			{
				accounts = ReadObjectList(root, "data");
			}
			var balances = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

			foreach (var account in accounts)
			{
				var currency = ReadStringValue(account, "currency");
				if (string.IsNullOrWhiteSpace(currency))
				{
					currency = ReadStringValue(ReadObject(account, "available_balance"), "currency");
				}
				if (string.IsNullOrWhiteSpace(currency))
				{
					currency = ReadStringValue(ReadObject(account, "balance"), "currency");
				}
				if (string.IsNullOrWhiteSpace(currency)) continue;

				var available = ReadBalanceAmount(
					account,
					new[] { "available_balance", "available", "balance" },
					new[] { "available_balance", "available", "available_to_trade", "available_funds", "balance" });

				var hold = ReadBalanceAmount(
					account,
					new[] { "hold", "hold_balance", "on_hold" },
					new[] { "hold", "hold_balance", "on_hold" });

				var total = available + hold;
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

			return balances;
		}

		public async Task<OrderResult> PlaceOrderAsync(OrderRequest req)
		{
			if (req == null)
			{
				return new OrderResult { OrderId = string.Empty, Accepted = false, Filled = false, FilledQty = 0m, AvgFillPrice = 0m, Message = "invalid order request" };
			}
			if (req.Quantity <= 0m)
			{
				return new OrderResult { OrderId = string.Empty, Accepted = false, Filled = false, FilledQty = 0m, AvgFillPrice = 0m, Message = "invalid quantity" };
			}

			var normalizedProduct = NormalizeProduct(req.ProductId);
			if (string.IsNullOrWhiteSpace(normalizedProduct))
			{
				return new OrderResult { OrderId = string.Empty, Accepted = false, Filled = false, FilledQty = 0m, AvgFillPrice = 0m, Message = "invalid product" };
			}

			var orderConfiguration = new Dictionary<string, object>();
			if (req.Type == OrderType.Limit)
			{
				var limit = new Dictionary<string, object>();
				limit["base_size"] = req.Quantity.ToString(CultureInfo.InvariantCulture);
				if (!req.Price.HasValue || req.Price.Value <= 0m)
				{
					return new OrderResult { OrderId = string.Empty, Accepted = false, Filled = false, FilledQty = 0m, AvgFillPrice = 0m, Message = "invalid limit price" };
				}
				limit["limit_price"] = req.Price.Value.ToString(CultureInfo.InvariantCulture);
				orderConfiguration["limit_limit_gtc"] = limit;
			}
			else
			{
				var market = new Dictionary<string, object>();
				market["base_size"] = req.Quantity.ToString(CultureInfo.InvariantCulture);
				orderConfiguration["market_market_ioc"] = market;
			}

			var bodyAdvanced = new Dictionary<string, object>();
			bodyAdvanced["client_order_id"] = string.IsNullOrWhiteSpace(req.ClientOrderId) ? ("cdts-" + Guid.NewGuid().ToString("N")) : req.ClientOrderId;
			bodyAdvanced["product_id"] = normalizedProduct;
			bodyAdvanced["side"] = req.Side == OrderSide.Buy ? "BUY" : "SELL";
			bodyAdvanced["order_configuration"] = orderConfiguration;

			var jsonAdvanced = await PrivateRequestAsync("POST", "/api/v3/brokerage/orders", UtilCompat.JsonSerialize(bodyAdvanced)).ConfigureAwait(false);
			var objAdvanced = UtilCompat.JsonDeserialize<Dictionary<string, object>>(jsonAdvanced) ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

			var success = ReadBoolValue(objAdvanced, "success");
			if (!success)
			{
				success = ReadBoolValue(objAdvanced, "accepted");
			}
			var successResponse = ReadObject(objAdvanced, "success_response");
			var orderObject = ReadObject(objAdvanced, "order");
			var orderId = ReadStringValue(successResponse, "order_id");
			if (string.IsNullOrWhiteSpace(orderId)) orderId = ReadStringValue(successResponse, "orderId");
			if (string.IsNullOrWhiteSpace(orderId)) orderId = ReadStringValue(orderObject, "order_id");
			if (string.IsNullOrWhiteSpace(orderId)) orderId = ReadStringValue(orderObject, "orderId");
			if (string.IsNullOrWhiteSpace(orderId)) orderId = ReadStringValue(objAdvanced, "order_id");
			if (string.IsNullOrWhiteSpace(orderId)) orderId = ReadStringValue(objAdvanced, "orderId");
			if (string.IsNullOrWhiteSpace(orderId)) orderId = ReadStringValue(objAdvanced, "id");
			if (string.IsNullOrWhiteSpace(orderId)) orderId = ReadStringValue(successResponse, "id");
			if (string.IsNullOrWhiteSpace(orderId)) orderId = ReadStringValue(orderObject, "id");

			var errorResponse = ReadObject(objAdvanced, "error_response");
			if (errorResponse == null)
			{
				errorResponse = ReadObject(objAdvanced, "errorResponse");
			}

			var rejectReason = ReadStringValue(objAdvanced, "reject_reason");
			if (string.IsNullOrWhiteSpace(rejectReason)) rejectReason = ReadStringValue(objAdvanced, "rejectReason");
			if (string.IsNullOrWhiteSpace(rejectReason)) rejectReason = ReadStringValue(successResponse, "reject_reason");
			if (string.IsNullOrWhiteSpace(rejectReason)) rejectReason = ReadStringValue(successResponse, "rejectReason");
			if (string.IsNullOrWhiteSpace(rejectReason)) rejectReason = ReadStringValue(errorResponse, "reject_reason");
			if (string.IsNullOrWhiteSpace(rejectReason)) rejectReason = ReadStringValue(errorResponse, "rejectReason");

			var status = ReadStringValue(successResponse, "status");
			if (string.IsNullOrWhiteSpace(status)) status = ReadStringValue(successResponse, "order_status");
			if (string.IsNullOrWhiteSpace(status)) status = ReadStringValue(successResponse, "orderStatus");
			if (string.IsNullOrWhiteSpace(status)) status = ReadStringValue(orderObject, "status");
			if (string.IsNullOrWhiteSpace(status)) status = ReadStringValue(orderObject, "order_status");
			if (string.IsNullOrWhiteSpace(status)) status = ReadStringValue(orderObject, "orderStatus");
			if (string.IsNullOrWhiteSpace(status)) status = ReadStringValue(objAdvanced, "status");
			if (string.IsNullOrWhiteSpace(status)) status = ReadStringValue(objAdvanced, "order_status");

			var hasReject = IsRejectReason(rejectReason);
			var statusUpper = string.IsNullOrWhiteSpace(status) ? string.Empty : status.Trim().ToUpperInvariant();
			if (!hasReject && (statusUpper.Contains("REJECT") || statusUpper.Contains("FAIL") || statusUpper == "ERROR" || statusUpper == "INVALID"))
			{
				hasReject = true;
			}

			var filledQty = ReadDecimalByCandidates(successResponse, "filled_size", "filledSize", "filled_quantity", "filledQuantity", "cumulative_quantity", "cumulativeQuantity", "executed_size", "executedSize", "size_filled", "sizeFilled");
			if (filledQty == 0m) filledQty = ReadDecimalByCandidates(orderObject, "filled_size", "filledSize", "filled_quantity", "filledQuantity", "cumulative_quantity", "cumulativeQuantity", "executed_size", "executedSize", "size_filled", "sizeFilled");
			if (filledQty == 0m) filledQty = ReadDecimalByCandidates(objAdvanced, "filled_size", "filledSize", "filled_quantity", "filledQuantity", "cumulative_quantity", "cumulativeQuantity", "executed_size", "executedSize", "size_filled", "sizeFilled");

			var avgFillPrice = ReadDecimalByCandidates(successResponse, "average_filled_price", "averageFilledPrice", "avg_price", "avgPrice", "filled_avg_price", "filledAvgPrice", "executed_value_price", "executedValuePrice");
			if (avgFillPrice == 0m) avgFillPrice = ReadDecimalByCandidates(orderObject, "average_filled_price", "averageFilledPrice", "avg_price", "avgPrice", "filled_avg_price", "filledAvgPrice", "executed_value_price", "executedValuePrice");
			if (avgFillPrice == 0m) avgFillPrice = ReadDecimalByCandidates(objAdvanced, "average_filled_price", "averageFilledPrice", "avg_price", "avgPrice", "filled_avg_price", "filledAvgPrice", "executed_value_price", "executedValuePrice");

			if (filledQty < 0m) filledQty = 0m;
			if (avgFillPrice < 0m) avgFillPrice = 0m;

			var filled = ReadBoolValue(successResponse, "filled")
				|| ReadBoolValue(orderObject, "filled")
				|| ReadBoolValue(objAdvanced, "filled");
			if (!filled)
			{
				filled = filledQty > 0m
					|| statusUpper == "FILLED"
					|| statusUpper == "DONE"
					|| statusUpper == "FULLY_FILLED";
			}

			var acceptedAdvanced = (success || !string.IsNullOrWhiteSpace(orderId) || filled || !string.IsNullOrWhiteSpace(status)) && !hasReject;
			var message = ResolveOrderMessage(acceptedAdvanced, filled, status, rejectReason, errorResponse, objAdvanced);

			return new OrderResult
			{
				OrderId = orderId ?? string.Empty,
				Accepted = acceptedAdvanced,
				Filled = filled,
				FilledQty = filledQty,
				AvgFillPrice = avgFillPrice,
				Message = message
			};
		}

		public async Task<bool> CancelOrderAsync(string orderId)
		{
			if (string.IsNullOrWhiteSpace(orderId))
			{
				return false;
			}

			var payload = new Dictionary<string, object>();
			payload["order_ids"] = new List<string> { orderId };
			var jsonAdvanced = await TryPrivateRequestAsync("POST", "/api/v3/brokerage/orders/batch_cancel", UtilCompat.JsonSerialize(payload)).ConfigureAwait(false);
			if (string.IsNullOrWhiteSpace(jsonAdvanced))
			{
				return false;
			}

			var root = UtilCompat.JsonDeserialize<Dictionary<string, object>>(jsonAdvanced) ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			var results = ReadObjectList(root, "results");
			if (results.Count == 0)
			{
				results = ReadObjectList(root, "order_results");
			}
			if (results.Count == 0)
			{
				results = ReadObjectList(root, "cancel_results");
			}
			if (results.Count == 0)
			{
				results = ReadObjectList(root, "data");
			}

			foreach (var row in results)
			{
				var rowOrderId = ResolveOrderId(row);
				var rowStatus = ReadStatusValue(row);
				var rowStatusUpper = string.IsNullOrWhiteSpace(rowStatus) ? string.Empty : rowStatus.ToUpperInvariant();

				var rowSuccess = ReadBoolValue(row, "success") || ReadBoolValue(row, "is_success");
				if (!rowSuccess)
				{
					rowSuccess = IsCanceledLikeStatus(rowStatusUpper);
				}

				var rowFailed = IsFailureLikeStatus(rowStatusUpper)
					|| IsRejectReason(ReadStringValue(row, "reject_reason"))
					|| IsRejectReason(ReadStringValue(row, "rejectReason"));

				var idMatches = string.IsNullOrWhiteSpace(rowOrderId)
					|| string.Equals(rowOrderId, orderId, StringComparison.OrdinalIgnoreCase);

				if (rowFailed && idMatches)
				{
					return false;
				}

				if (rowSuccess && idMatches)
				{
					return true;
				}
			}

			if (results.Count == 0 && ReadBoolValue(root, "success"))
			{
				return true;
			}

			if (results.Count == 0)
			{
				var rootStatus = ReadStatusValue(root);
				if (IsCanceledLikeStatus(string.IsNullOrWhiteSpace(rootStatus) ? string.Empty : rootStatus.ToUpperInvariant()))
				{
					return true;
				}
			}

			return false;
		}

		public async Task<List<Dictionary<string, object>>> GetOpenOrdersAsync()
		{
			var jsonAdvanced = await PrivateRequestAsync("GET", "/api/v3/brokerage/orders/historical/batch?order_status=OPEN").ConfigureAwait(false);
			var root = UtilCompat.JsonDeserialize<Dictionary<string, object>>(jsonAdvanced) ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			var orders = ReadObjectList(root, "orders");
			if (orders.Count == 0)
			{
				orders = ReadObjectList(root, "results");
			}
			if (orders.Count == 0)
			{
				orders = ReadObjectList(root, "data");
			}

			return FilterOpenOrders(orders);
		}

		public async Task<List<Dictionary<string, object>>> GetRecentFillsAsync(int limit = 250)
		{
			if (limit < 1) limit = 1;
			if (limit > 1000) limit = 1000;

			var jsonAdvanced = await PrivateRequestAsync("GET", "/api/v3/brokerage/orders/historical/fills?limit=" + limit.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
			var root = UtilCompat.JsonDeserialize<Dictionary<string, object>>(jsonAdvanced) ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			var fills = ReadObjectList(root, "fills");
			if (fills.Count > 0)
			{
				return DeduplicateFillRows(EnrichFillRows(fills));
			}

			fills = ReadObjectList(root, "results");
			if (fills.Count > 0)
			{
				return DeduplicateFillRows(EnrichFillRows(fills));
			}

			fills = ReadObjectList(root, "executions");
			if (fills.Count > 0)
			{
				return DeduplicateFillRows(EnrichFillRows(fills));
			}

			return DeduplicateFillRows(EnrichFillRows(ReadObjectList(root, "data")));
		}

		private List<Dictionary<string, object>> FilterOpenOrders(List<Dictionary<string, object>> orders)
		{
			if (orders == null || orders.Count == 0)
			{
				return new List<Dictionary<string, object>>();
			}

			var filtered = new List<Dictionary<string, object>>();
			for (int i = 0; i < orders.Count; i++)
			{
				var row = orders[i];
				var status = ReadStatusValue(row);
				if (string.IsNullOrWhiteSpace(status) || IsOpenLikeStatus(status.ToUpperInvariant()))
				{
					filtered.Add(row);
				}
			}

			return filtered;
		}

		private List<Dictionary<string, object>> EnrichFillRows(List<Dictionary<string, object>> fills)
		{
			if (fills == null || fills.Count == 0)
			{
				return new List<Dictionary<string, object>>();
			}

			for (int i = 0; i < fills.Count; i++)
			{
				var row = fills[i];
				if (row == null) continue;

				var product = ResolveProductId(row);
				if (string.IsNullOrWhiteSpace(product))
				{
					product = ReadStringValue(row, "symbol");
				}
				if (!string.IsNullOrWhiteSpace(product))
				{
					product = NormalizeProduct(product).ToUpperInvariant();
				}
				if (!string.IsNullOrWhiteSpace(product) && string.IsNullOrWhiteSpace(ReadStringValue(row, "product_id")))
				{
					row["product_id"] = product;
				}

				var side = ReadStringValue(row, "side");
				if (string.IsNullOrWhiteSpace(side)) side = ReadStringValue(row, "order_side");
				if (string.IsNullOrWhiteSpace(side)) side = ReadStringValue(row, "orderSide");
				if (string.IsNullOrWhiteSpace(side)) side = ReadStringValue(row, "trade_side");
				if (string.IsNullOrWhiteSpace(side)) side = ReadStringValue(row, "tradeSide");
				if (!string.IsNullOrWhiteSpace(side))
				{
					side = NormalizeSideValue(side);
					row["side"] = side;
				}

				var fillId = ReadStringValue(row, "trade_id");
				if (string.IsNullOrWhiteSpace(fillId)) fillId = ReadStringValue(row, "tradeId");
				if (string.IsNullOrWhiteSpace(fillId)) fillId = ReadStringValue(row, "entry_id");
				if (string.IsNullOrWhiteSpace(fillId)) fillId = ReadStringValue(row, "entryId");
				if (string.IsNullOrWhiteSpace(fillId)) fillId = ReadStringValue(row, "fill_id");
				if (string.IsNullOrWhiteSpace(fillId)) fillId = ReadStringValue(row, "fillId");
				if (!string.IsNullOrWhiteSpace(fillId))
				{
					row["trade_id"] = fillId.Trim();
				}

				var orderId = ResolveOrderId(row);
				if (!string.IsNullOrWhiteSpace(orderId))
				{
					row["order_id"] = orderId.Trim();
				}

				var size = ReadDecimalByCandidates(row, "size", "base_size", "filled_size", "filled_quantity", "filledQuantity", "last_fill_size", "lastFillSize", "quantity", "qty");
				if (size != 0m)
				{
					row["size"] = size;
				}

				var price = ReadDecimalByCandidates(row, "price", "trade_price", "tradePrice", "fill_price", "fillPrice", "average_filled_price", "averageFilledPrice", "avg_price", "avgPrice");
				var notional = ReadDecimalByCandidates(row, "notional", "quote_size", "quoteSize", "quote_volume", "quoteVolume", "filled_value", "filledValue", "executed_value", "executedValue", "value", "usd_value", "usdValue");

				if (price == 0m && size > 0m && notional > 0m)
				{
					price = notional / size;
				}
				if (notional == 0m && size > 0m && price > 0m)
				{
					notional = size * price;
				}

				if (price != 0m)
				{
					row["price"] = price;
				}
				if (notional != 0m)
				{
					row["notional"] = notional;
				}

				var fee = ReadDecimalByCandidates(row, "fee", "fees", "commission", "total_fees", "totalFees", "fill_fees", "fillFees", "fee_amount", "feeAmount");
				if (fee != 0m)
				{
					row["fee"] = fee;
				}

				var tradeTime = ReadStringValue(row, "trade_time");
				if (string.IsNullOrWhiteSpace(tradeTime)) tradeTime = ReadStringValue(row, "tradeTime");
				if (string.IsNullOrWhiteSpace(tradeTime)) tradeTime = ReadStringValue(row, "fill_time");
				if (string.IsNullOrWhiteSpace(tradeTime)) tradeTime = ReadStringValue(row, "fillTime");
				if (string.IsNullOrWhiteSpace(tradeTime)) tradeTime = ReadStringValue(row, "created_time");
				if (string.IsNullOrWhiteSpace(tradeTime)) tradeTime = ReadStringValue(row, "createdTime");
				if (string.IsNullOrWhiteSpace(tradeTime)) tradeTime = ReadStringValue(row, "time");
				if (string.IsNullOrWhiteSpace(tradeTime)) tradeTime = ReadStringValue(row, "timestamp");

				if (string.IsNullOrWhiteSpace(tradeTime))
				{
					var parsedTime = ReadDateTimeByCandidates(row, "trade_time", "tradeTime", "fill_time", "fillTime", "created_time", "createdTime", "time", "timestamp");
					if (parsedTime != DateTime.MinValue)
					{
						tradeTime = parsedTime.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
					}
				}
				if (!string.IsNullOrWhiteSpace(tradeTime))
				{
					row["trade_time"] = tradeTime;
				}
			}

			return fills;
		}

		private List<Dictionary<string, object>> DeduplicateFillRows(List<Dictionary<string, object>> fills)
		{
			if (fills == null || fills.Count == 0)
			{
				return new List<Dictionary<string, object>>();
			}

			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var output = new List<Dictionary<string, object>>();
			for (int i = 0; i < fills.Count; i++)
			{
				var row = fills[i];
				if (row == null) continue;

				var id = ReadStringValue(row, "trade_id");
				if (string.IsNullOrWhiteSpace(id)) id = ReadStringValue(row, "entry_id");
				if (string.IsNullOrWhiteSpace(id)) id = ReadStringValue(row, "id");

				var key = string.IsNullOrWhiteSpace(id)
					? string.Join("|", new[]
					{
						ReadStringValue(row, "order_id"),
						ReadStringValue(row, "product_id"),
						ReadStringValue(row, "side"),
						ReadStringValue(row, "size"),
						ReadStringValue(row, "price"),
						ReadStringValue(row, "trade_time")
					})
					: "id:" + id.Trim();

				if (!seen.Add(key))
				{
					continue;
				}

				output.Add(row);
			}

			return output;
		}

		private string NormalizeSideValue(string side)
		{
			if (string.IsNullOrWhiteSpace(side))
			{
				return string.Empty;
			}

			var normalized = side.Trim().ToUpperInvariant();
			if (normalized == "B") return "BUY";
			if (normalized == "S") return "SELL";
			return normalized;
		}

		private DateTime ReadDateTimeByCandidates(Dictionary<string, object> row, params string[] keys)
		{
			if (row == null || keys == null)
			{
				return DateTime.MinValue;
			}

			for (int i = 0; i < keys.Length; i++)
			{
				var key = keys[i];
				if (string.IsNullOrWhiteSpace(key)) continue;

				var parsed = ReadDateTimeValue(row, key);
				if (parsed != DateTime.MinValue)
				{
					return parsed;
				}
			}

			return DateTime.MinValue;
		}

		private async Task<List<Dictionary<string, object>>> GetProductsPayloadAsync()
		{
			var legacyJson = await TryGetPublicAsync(Rest + "/products").ConfigureAwait(false);
			var legacyProducts = ParseProductsPayloadJson(legacyJson);
			if (legacyProducts.Count > 0)
			{
				return legacyProducts;
			}

			var advancedJson = await TryGetPublicAsync(BrokerageRest + "/api/v3/brokerage/products").ConfigureAwait(false);
			var advancedProducts = ParseProductsPayloadJson(advancedJson);
			if (advancedProducts.Count > 0)
			{
				return advancedProducts;
			}

			return new List<Dictionary<string, object>>();
		}

		private async Task<string> TryGetPublicAsync(string url)
		{
			try
			{
				return await HttpUtil.GetAsync(url).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Log.Warn("[Coinbase] Public request failed for " + url + ": " + ex.Message);
				return string.Empty;
			}
		}

		private List<Dictionary<string, object>> ParseProductsPayloadJson(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				return new List<Dictionary<string, object>>();
			}

			var direct = UtilCompat.JsonDeserialize<List<Dictionary<string, object>>>(json);
			if (direct != null)
			{
				return direct;
			}

			var fromArray = UtilCompat.JsonDeserialize<object[]>(json);
			if (fromArray != null)
			{
				var parsed = new List<Dictionary<string, object>>();
				foreach (var item in fromArray)
				{
					var dict = ToDictionary(item);
					if (dict != null) parsed.Add(dict);
				}
				return parsed;
			}

			var root = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
			if (root != null)
			{
				var nested = ReadObjectList(root, "products");
				if (nested.Count > 0)
				{
					return nested;
				}

				nested = ReadObjectList(root, "data");
				if (nested.Count > 0)
				{
					return nested;
				}
			}

			return new List<Dictionary<string, object>>();
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

				var products = await GetProductsPayloadAsync().ConfigureAwait(false);
				var rebuilt = new Dictionary<string, SymbolConstraints>(StringComparer.OrdinalIgnoreCase);
				foreach (var row in products)
				{
					var id = ResolveProductId(row);
					if (string.IsNullOrWhiteSpace(id)) continue;

					var minQty = ReadDecimalByCandidates(row, "base_min_size", "baseMinSize", "min_size", "minSize");
					var maxQty = ReadDecimalByCandidates(row, "base_max_size", "baseMaxSize", "max_size", "maxSize");
					var stepSize = ReadDecimalByCandidates(row, "base_increment", "baseIncrement", "base_step", "baseStepSize", "step_size", "stepSize");
					var priceTick = ReadDecimalByCandidates(row, "quote_increment", "quoteIncrement", "price_increment", "priceIncrement", "tick_size", "tickSize");
					var minMarketFunds = ReadDecimalByCandidates(row, "min_market_funds", "minMarketFunds", "min_notional", "minNotional");
					var quoteMinSize = ReadDecimalByCandidates(row, "quote_min_size", "quoteMinSize", "quote_min_amount", "quoteMinAmount");

					var constraints = new SymbolConstraints
					{
						Symbol = id,
						MinQty = minQty,
						MaxQty = maxQty,
						StepSize = stepSize,
						MinNotional = minMarketFunds > quoteMinSize ? minMarketFunds : quoteMinSize,
						PriceTickSize = priceTick,
						Source = "coinbase.products"
					};

					rebuilt[id] = constraints;
				}

				_symbolConstraintsBySymbol = rebuilt;
				_constraintsFetchedUtc = DateTime.UtcNow;
			}
			finally
			{
				_constraintsLock.Release();
			}
		}

		private string ResolveProductId(Dictionary<string, object> row)
		{
			var id = ReadStringValue(row, "id");
			if (!string.IsNullOrWhiteSpace(id))
			{
				return id;
			}

			id = ReadStringValue(row, "product_id");
			if (!string.IsNullOrWhiteSpace(id))
			{
				return id;
			}

			id = ReadStringValue(row, "productId");
			if (!string.IsNullOrWhiteSpace(id))
			{
				return id;
			}

			var baseAsset = ReadStringValue(row, "base_currency");
			if (string.IsNullOrWhiteSpace(baseAsset)) baseAsset = ReadStringValue(row, "baseCurrency");
			if (string.IsNullOrWhiteSpace(baseAsset)) baseAsset = ReadStringValue(row, "base_currency_id");
			if (string.IsNullOrWhiteSpace(baseAsset)) baseAsset = ReadStringValue(row, "baseCurrencyId");

			var quoteAsset = ReadStringValue(row, "quote_currency");
			if (string.IsNullOrWhiteSpace(quoteAsset)) quoteAsset = ReadStringValue(row, "quoteCurrency");
			if (string.IsNullOrWhiteSpace(quoteAsset)) quoteAsset = ReadStringValue(row, "quote_currency_id");
			if (string.IsNullOrWhiteSpace(quoteAsset)) quoteAsset = ReadStringValue(row, "quoteCurrencyId");

			if (!string.IsNullOrWhiteSpace(baseAsset) && !string.IsNullOrWhiteSpace(quoteAsset))
			{
				return baseAsset + "-" + quoteAsset;
			}

			return string.Empty;
		}

		private decimal ParseDecimal(Dictionary<string, object> row, string key)
		{
			if (row == null || string.IsNullOrWhiteSpace(key) || !row.ContainsKey(key) || row[key] == null)
			{
				return 0m;
			}

			decimal value;
			if (decimal.TryParse(row[key].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
			{
				return value;
			}

			return 0m;
		}

		private bool TryGetObjectValue(Dictionary<string, object> row, string key, out object value)
		{
			value = null;
			if (row == null || string.IsNullOrWhiteSpace(key))
			{
				return false;
			}

			if (row.ContainsKey(key))
			{
				value = row[key];
				return value != null;
			}

			foreach (var entry in row)
			{
				if (entry.Key == null) continue;
				if (string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
				{
					value = entry.Value;
					return value != null;
				}
			}

			return false;
		}

		private bool TryReadDecimalValue(Dictionary<string, object> row, string key, out decimal parsed)
		{
			parsed = 0m;
			object raw;
			if (!TryGetObjectValue(row, key, out raw) || raw == null)
			{
				return false;
			}

			return decimal.TryParse(raw.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out parsed);
		}

		private Dictionary<string, object> ReadObject(Dictionary<string, object> row, string key)
		{
			object raw;
			if (!TryGetObjectValue(row, key, out raw))
			{
				return null;
			}

			var direct = raw as Dictionary<string, object>;
			if (direct != null)
			{
				return direct;
			}

			var table = raw as IDictionary;
			if (table != null)
			{
				return ToDictionary(table);
			}

			var serialized = raw.ToString();
			if (!string.IsNullOrWhiteSpace(serialized))
			{
				return UtilCompat.JsonDeserialize<Dictionary<string, object>>(serialized);
			}

			return null;
		}

		private List<Dictionary<string, object>> ReadObjectList(Dictionary<string, object> row, string key)
		{
			var output = new List<Dictionary<string, object>>();
			object raw;
			if (!TryGetObjectValue(row, key, out raw))
			{
				return output;
			}

			var tableList = raw as IList;
			if (tableList != null)
			{
				foreach (var item in tableList)
				{
					var dict = ToDictionary(item);
					if (dict != null) output.Add(dict);
				}
				return output;
			}

			var list = raw as List<object>;
			if (list != null)
			{
				foreach (var item in list)
				{
					var dict = ToDictionary(item);
					if (dict != null) output.Add(dict);
				}
				return output;
			}

			var arr = raw as object[];
			if (arr != null)
			{
				foreach (var item in arr)
				{
					var dict = ToDictionary(item);
					if (dict != null) output.Add(dict);
				}
				return output;
			}

			var arrayList = raw as ArrayList;
			if (arrayList != null)
			{
				foreach (var item in arrayList)
				{
					var dict = ToDictionary(item);
					if (dict != null) output.Add(dict);
				}
				return output;
			}

			var direct = raw as List<Dictionary<string, object>>;
			if (direct != null)
			{
				output.AddRange(direct);
				return output;
			}

			var serialized = raw.ToString();
			if (!string.IsNullOrWhiteSpace(serialized))
			{
				var fromSerializedArray = UtilCompat.JsonDeserialize<object[]>(serialized);
				if (fromSerializedArray != null)
				{
					foreach (var item in fromSerializedArray)
					{
						var dict = ToDictionary(item);
						if (dict != null) output.Add(dict);
					}
					return output;
				}

				var fromSerializedDirect = UtilCompat.JsonDeserialize<List<Dictionary<string, object>>>(serialized);
				if (fromSerializedDirect != null)
				{
					output.AddRange(fromSerializedDirect);
				}
			}

			return output;
		}

		private Dictionary<string, object> ToDictionary(object value)
		{
			if (value == null)
			{
				return null;
			}

			var direct = value as Dictionary<string, object>;
			if (direct != null)
			{
				return direct;
			}

			var table = value as IDictionary;
			if (table != null)
			{
				var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
				foreach (DictionaryEntry entry in table)
				{
					if (entry.Key == null) continue;
					dict[entry.Key.ToString()] = entry.Value;
				}
				return dict;
			}

			var serialized = value.ToString();
			if (string.IsNullOrWhiteSpace(serialized))
			{
				return null;
			}

			return UtilCompat.JsonDeserialize<Dictionary<string, object>>(serialized);
		}

		private string ReadStringValue(Dictionary<string, object> row, string key)
		{
			object raw;
			if (!TryGetObjectValue(row, key, out raw) || raw == null)
			{
				return string.Empty;
			}

			return raw.ToString();
		}

		private decimal ReadDecimalValue(Dictionary<string, object> row, string key)
		{
			decimal parsed;
			if (TryReadDecimalValue(row, key, out parsed))
			{
				return parsed;
			}

			return 0m;
		}

		private decimal ReadDecimalByCandidates(Dictionary<string, object> row, params string[] keys)
		{
			if (row == null || keys == null)
			{
				return 0m;
			}

			for (int i = 0; i < keys.Length; i++)
			{
				var key = keys[i];
				if (string.IsNullOrWhiteSpace(key)) continue;

				decimal value;
				if (TryReadDecimalValue(row, key, out value))
				{
					return value;
				}

				var nested = ReadObject(row, key);
				if (nested != null)
				{
					if (TryReadDecimalValue(nested, "value", out value))
					{
						return value;
					}

					if (TryReadDecimalValue(nested, "amount", out value))
					{
						return value;
					}
				}
			}

			return 0m;
		}

		private decimal ReadBalanceAmount(Dictionary<string, object> row, string[] nestedKeys, string[] directKeys)
		{
			if (row == null)
			{
				return 0m;
			}

			if (nestedKeys != null)
			{
				for (int i = 0; i < nestedKeys.Length; i++)
				{
					var nested = ReadObject(row, nestedKeys[i]);
					if (nested == null) continue;

					var nestedValue = ReadDecimalValue(nested, "value");
					if (nestedValue == 0m)
					{
						nestedValue = ReadDecimalValue(nested, "amount");
					}

					if (nestedValue != 0m)
					{
						return nestedValue;
					}
				}
			}

			if (directKeys != null)
			{
				for (int i = 0; i < directKeys.Length; i++)
				{
					var directValue = ReadDecimalValue(row, directKeys[i]);
					if (directValue != 0m)
					{
						return directValue;
					}
				}
			}

			return 0m;
		}

		private string ResolveOrderId(Dictionary<string, object> row)
		{
			if (row == null)
			{
				return string.Empty;
			}

			var orderId = ReadStringValue(row, "order_id");
			if (string.IsNullOrWhiteSpace(orderId)) orderId = ReadStringValue(row, "orderId");
			if (string.IsNullOrWhiteSpace(orderId)) orderId = ReadStringValue(row, "id");

			if (string.IsNullOrWhiteSpace(orderId))
			{
				var order = ReadObject(row, "order");
				orderId = ReadStringValue(order, "order_id");
				if (string.IsNullOrWhiteSpace(orderId)) orderId = ReadStringValue(order, "orderId");
				if (string.IsNullOrWhiteSpace(orderId)) orderId = ReadStringValue(order, "id");
			}

			return orderId;
		}

		private string ReadStatusValue(Dictionary<string, object> row)
		{
			var status = ReadStringValue(row, "status");
			if (string.IsNullOrWhiteSpace(status)) status = ReadStringValue(row, "order_status");
			if (string.IsNullOrWhiteSpace(status)) status = ReadStringValue(row, "orderStatus");
			if (string.IsNullOrWhiteSpace(status)) status = ReadStringValue(row, "cancel_status");
			if (string.IsNullOrWhiteSpace(status)) status = ReadStringValue(row, "cancelStatus");
			if (string.IsNullOrWhiteSpace(status)) status = ReadStringValue(row, "result");

			if (string.IsNullOrWhiteSpace(status))
			{
				var order = ReadObject(row, "order");
				status = ReadStringValue(order, "status");
				if (string.IsNullOrWhiteSpace(status)) status = ReadStringValue(order, "order_status");
				if (string.IsNullOrWhiteSpace(status)) status = ReadStringValue(order, "orderStatus");
			}

			return status;
		}

		private bool IsCanceledLikeStatus(string statusUpper)
		{
			if (string.IsNullOrWhiteSpace(statusUpper))
			{
				return false;
			}

			return statusUpper == "CANCELED"
				|| statusUpper == "CANCELLED"
				|| statusUpper == "PENDING_CANCEL"
				|| statusUpper == "CANCEL_QUEUED"
				|| statusUpper == "SUCCESS"
				|| statusUpper == "OK";
		}

		private bool IsFailureLikeStatus(string statusUpper)
		{
			if (string.IsNullOrWhiteSpace(statusUpper))
			{
				return false;
			}

			return statusUpper.Contains("FAIL")
				|| statusUpper.Contains("REJECT")
				|| statusUpper.Contains("ERROR")
				|| statusUpper.Contains("DENIED")
				|| statusUpper == "INVALID";
		}

		private bool IsOpenLikeStatus(string statusUpper)
		{
			if (string.IsNullOrWhiteSpace(statusUpper))
			{
				return true;
			}

			return statusUpper == "OPEN"
				|| statusUpper == "PENDING"
				|| statusUpper == "ACTIVE"
				|| statusUpper == "WORKING"
				|| statusUpper == "NEW"
				|| statusUpper == "PARTIALLY_FILLED"
				|| statusUpper == "PARTIAL_FILL";
		}

		private bool TryReadDecimal(object raw, out decimal value)
		{
			value = 0m;
			if (raw == null)
			{
				return false;
			}

			return decimal.TryParse(raw.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
		}

		private bool TryReadLong(object raw, out long value)
		{
			value = 0L;
			if (raw == null)
			{
				return false;
			}

			return long.TryParse(raw.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
		}

		private DateTime ReadDateTimeValue(Dictionary<string, object> row, string key)
		{
			var raw = ReadStringValue(row, key);
			if (string.IsNullOrWhiteSpace(raw))
			{
				return DateTime.MinValue;
			}

			DateTime parsed;
			if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed))
			{
				return parsed;
			}

			return DateTime.MinValue;
		}

		private bool IsRejectReason(string rejectReason)
		{
			if (string.IsNullOrWhiteSpace(rejectReason))
			{
				return false;
			}

			var normalized = rejectReason.Trim();
			return !string.Equals(normalized, "REJECT_REASON_UNSPECIFIED", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(normalized, "NONE", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(normalized, "UNKNOWN", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(normalized, "N/A", StringComparison.OrdinalIgnoreCase);
		}

		private string ResolveOrderMessage(bool accepted, bool filled, string status, string rejectReason, Dictionary<string, object> errorResponse, Dictionary<string, object> root)
		{
			if (filled)
			{
				return "filled";
			}

			if (IsRejectReason(rejectReason))
			{
				return rejectReason;
			}

			var errorMessage = ReadStringValue(errorResponse, "message");
			if (string.IsNullOrWhiteSpace(errorMessage)) errorMessage = ReadStringValue(errorResponse, "error");
			if (string.IsNullOrWhiteSpace(errorMessage)) errorMessage = ReadStringValue(root, "message");
			if (string.IsNullOrWhiteSpace(errorMessage)) errorMessage = ReadStringValue(root, "error");

			if (!accepted)
			{
				if (!string.IsNullOrWhiteSpace(errorMessage)) return errorMessage;
				if (!string.IsNullOrWhiteSpace(status)) return status;
				return "error";
			}

			if (!string.IsNullOrWhiteSpace(status))
			{
				return status;
			}

			if (!string.IsNullOrWhiteSpace(errorMessage))
			{
				return errorMessage;
			}

			return "accepted";
		}

		private bool ReadBoolValue(Dictionary<string, object> row, string key)
		{
			var raw = ReadStringValue(row, key);
			if (string.IsNullOrWhiteSpace(raw))
			{
				return false;
			}

			bool parsed;
			if (bool.TryParse(raw, out parsed))
			{
				return parsed;
			}

			return string.Equals(raw, "1", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(raw, "yes", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(raw, "y", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
		}

	}
}