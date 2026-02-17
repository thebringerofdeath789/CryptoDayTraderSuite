using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Exchanges
{
	public class BitstampClient : IExchangeClient
	{
		private string _apiKey;

		private string _apiSecret;

		private string _customerId;

		private const string Rest = "https://www.bitstamp.net/api";

		private static readonly HttpClient _http = new HttpClient();

		public string Name => "Bitstamp";

		public BitstampClient(string apiKey, string apiSecret, string customerId)
		{
			_apiKey = apiKey;
			_apiSecret = apiSecret;
			_customerId = customerId;
		}

		public void SetCredentials(string apiKey, string apiSecret, string passphrase = null)
		{
			_apiKey = apiKey;
			_apiSecret = apiSecret;
			_customerId = passphrase;
		}

		public string NormalizeProduct(string uiSymbol)
		{
			return uiSymbol.Replace("/", "").Replace("-", "").ToLowerInvariant();
		}

		public string DenormalizeProduct(string apiSymbol)
		{
			apiSymbol = apiSymbol.ToUpperInvariant();
			if (apiSymbol.Length == 6)
			{
				return apiSymbol.Substring(0, 3) + "/" + apiSymbol.Substring(3);
			}
			return apiSymbol;
		}

		private async Task<string> PrivatePostAsync(string path, Dictionary<string, string> form)
		{
			return await HttpUtil.RetryAsync(async delegate
			{
				string nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
				string signature = SecurityUtil.ComputeHmacSha256Hex(message: nonce + _customerId + _apiKey, secret: _apiSecret);
				Dictionary<string, string> dict = new Dictionary<string, string>(form ?? new Dictionary<string, string>())
				{
					["key"] = _apiKey,
					["signature"] = signature,
					["nonce"] = nonce
				};
				string url = "https://www.bitstamp.net/api" + path;
				FormUrlEncodedContent content = new FormUrlEncodedContent((IEnumerable<KeyValuePair<string, string>>)dict);
				HttpResponseMessage resp = await _http.PostAsync(url, (HttpContent)(object)content);
				try
				{
					string body = await resp.Content.ReadAsStringAsync();
					if (!resp.IsSuccessStatusCode)
					{
						throw new Exception(body);
					}
					return body;
				}
				finally
				{
					((IDisposable)resp)?.Dispose();
				}
			});
		}

		public async Task<List<string>> ListProductsAsync()
		{
			return await HttpUtil.RetryAsync(async delegate
			{
				List<Dictionary<string, object>> arr = UtilCompat.JsonDeserialize<List<Dictionary<string, object>>>(await HttpUtil.GetAsync("https://www.bitstamp.net/api/v2/trading-pairs-info/"));
				List<string> res = new List<string>();
				foreach (Dictionary<string, object> p in arr)
				{
					if (p.ContainsKey("name"))
					{
						res.Add(p["name"].ToString().Replace(" ", ""));
					}
				}
				return res;
			});
		}

		public async Task<List<Candle>> GetCandlesAsync(string productId, int minutes, DateTime startUtc, DateTime endUtc)
		{
			return await HttpUtil.RetryAsync(async delegate
			{
				string pair = NormalizeProduct(productId);
				int step = minutes * 60;
				if (step < 60)
				{
					step = 60;
				}
				string url = "https://www.bitstamp.net/api/v2/ohlc/" + pair + "/?step=" + step + "&limit=1000";
				Dictionary<string, object> root = UtilCompat.JsonDeserialize<Dictionary<string, object>>(await HttpUtil.GetAsync(url));
				List<Candle> res = new List<Candle>();
				if (root.ContainsKey("data"))
				{
					Dictionary<string, object> data = (Dictionary<string, object>)root["data"];
					if (data["ohlc"] is object[] ohlc)
					{
						object[] array = ohlc;
						foreach (object o in array)
						{
							Dictionary<string, object> row = (Dictionary<string, object>)o;
							DateTime ts = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Convert.ToInt64(row["timestamp"]));
							if (!(ts < startUtc) && !(ts > endUtc))
							{
								res.Add(new Candle
								{
									Time = ts,
									Open = Convert.ToDecimal(row["open"]),
									High = Convert.ToDecimal(row["high"]),
									Low = Convert.ToDecimal(row["low"]),
									Close = Convert.ToDecimal(row["close"]),
									Volume = Convert.ToDecimal(row["volume"])
								});
							}
						}
					}
				}
				res.Sort((Candle a, Candle b) => a.Time.CompareTo(b.Time));
				return res;
			});
		}

		public async Task<Ticker> GetTickerAsync(string productId)
		{
			return await HttpUtil.RetryAsync(async delegate
			{
				string pair = NormalizeProduct(productId);
				string url = "https://www.bitstamp.net/api/v2/ticker/" + pair + "/";
				Dictionary<string, object> obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(await HttpUtil.GetAsync(url));
				return new Ticker
				{
					Bid = Convert.ToDecimal(obj["bid"]),
					Ask = Convert.ToDecimal(obj["ask"]),
					Last = Convert.ToDecimal(obj["last"]),
					Time = DateTime.UtcNow
				};
			});
		}

		public async Task<FeeSchedule> GetFeesAsync()
		{
			Dictionary<string, object> obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(await PrivatePostAsync("/v2/balance/", new Dictionary<string, string>()));
			decimal taker = 0.0040m;
			decimal maker = 0.0030m;
			foreach (string k in obj.Keys)
			{
				if (k.EndsWith("_fee"))
				{
					string val = obj[k].ToString();
					if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var pct))
					{
						taker = pct / 100m;
						maker = taker * 0.75m;
						break;
					}
				}
			}
			return new FeeSchedule
			{
				MakerRate = maker,
				TakerRate = taker,
				Notes = "from balance fees"
			};
		}

		public async Task<Dictionary<string, decimal>> GetBalancesAsync()
		{
			Dictionary<string, object> obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(await PrivatePostAsync("/v2/balance/", new Dictionary<string, string>()));
			Dictionary<string, decimal> d = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
			foreach (string k in obj.Keys)
			{
				if (k.EndsWith("_balance"))
				{
					string sym = k.Substring(0, k.Length - "_balance".Length).ToUpperInvariant();
					if (decimal.TryParse(obj[k].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var bal))
					{
						d[sym] = bal;
					}
				}
			}
			return d;
		}

		public async Task<OrderResult> PlaceOrderAsync(OrderRequest req)
		{
			string pair = NormalizeProduct(req.ProductId);
			string path = ((req.Side == OrderSide.Buy) ? ("/v2/buy/" + pair + "/") : ("/v2/sell/" + pair + "/"));
			Dictionary<string, string> form = new Dictionary<string, string>();
			if (req.Type == OrderType.Market)
			{
				form["amount"] = req.Quantity.ToString(CultureInfo.InvariantCulture);
			}
			else
			{
				form["amount"] = req.Quantity.ToString(CultureInfo.InvariantCulture);
				form["price"] = req.Price.Value.ToString(CultureInfo.InvariantCulture);
			}
			Dictionary<string, object> obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(await PrivatePostAsync(path, form));
			bool accepted = obj.ContainsKey("id");
			string id = (accepted ? obj["id"].ToString() : "");
			return new OrderResult
			{
				OrderId = id,
				Accepted = accepted,
				Filled = false,
				FilledQty = 0m,
				AvgFillPrice = 0m,
				Message = (accepted ? "accepted" : "error")
			};
		}

		public async Task<bool> CancelOrderAsync(string orderId)
		{
			return (await PrivatePostAsync("/v2/cancel_order/", new Dictionary<string, string> { ["id"] = orderId })).Contains("true");
		}

		public async Task<List<Dictionary<string, object>>> GetOpenOrdersAsync()
		{
			return UtilCompat.JsonDeserialize<List<Dictionary<string, object>>>(await PrivatePostAsync("/v2/open_orders/all/", new Dictionary<string, string>()));
		}
	}
}
