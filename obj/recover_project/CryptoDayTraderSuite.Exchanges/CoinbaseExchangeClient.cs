using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Exchanges
{
	public class CoinbaseExchangeClient : IExchangeClient
	{
		private string _key;

		private string _secretBase64;

		private string _passphrase;

		private const string Rest = "https://api.exchange.coinbase.com";

		private static readonly HttpClient _http = new HttpClient();

		public string Name => "Coinbase";

		public CoinbaseExchangeClient(string key, string secretBase64, string passphrase)
		{
			_key = key;
			_secretBase64 = secretBase64;
			_passphrase = passphrase;
		}

		public void SetCredentials(string apiKey, string apiSecret, string passphrase = null)
		{
			_key = apiKey;
			_secretBase64 = apiSecret;
			_passphrase = passphrase;
		}

		public string NormalizeProduct(string uiSymbol)
		{
			return uiSymbol.Replace("/", "-");
		}

		public string DenormalizeProduct(string apiSymbol)
		{
			return apiSymbol.Replace("-", "/");
		}

		private async Task<string> PrivateRequestAsync(string method, string path, string body = "")
		{
			return await HttpUtil.RetryAsync(async delegate
			{
				string ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
				string sig = SecurityUtil.ComputeHmacSha256Base64(message: ts + method.ToUpperInvariant() + path + body, secretBase64: _secretBase64);
				string url = "https://api.exchange.coinbase.com" + path;
				HttpRequestMessage req = new HttpRequestMessage(new HttpMethod(method.ToUpperInvariant()), url);
				try
				{
					((HttpHeaders)req.Headers).Add("CB-ACCESS-KEY", _key);
					((HttpHeaders)req.Headers).Add("CB-ACCESS-SIGN", sig);
					((HttpHeaders)req.Headers).Add("CB-ACCESS-TIMESTAMP", ts);
					((HttpHeaders)req.Headers).Add("CB-ACCESS-PASSPHRASE", _passphrase);
					((HttpHeaders)req.Headers).Add("User-Agent", "CryptoDayTraderSuite");
					if (!string.IsNullOrEmpty(body))
					{
						req.Content = (HttpContent)new StringContent(body, Encoding.UTF8, "application/json");
					}
					HttpResponseMessage resp = await _http.SendAsync(req);
					try
					{
						string respBody = await resp.Content.ReadAsStringAsync();
						if (!resp.IsSuccessStatusCode)
						{
							throw new Exception(respBody);
						}
						return respBody;
					}
					finally
					{
						((IDisposable)resp)?.Dispose();
					}
				}
				finally
				{
					((IDisposable)req)?.Dispose();
				}
			});
		}

		public async Task<List<string>> ListProductsAsync()
		{
			return await HttpUtil.RetryAsync(async delegate
			{
				List<Dictionary<string, object>> arr = UtilCompat.JsonDeserialize<List<Dictionary<string, object>>>(await HttpUtil.GetAsync("https://api.exchange.coinbase.com/products"));
				List<string> res = new List<string>();
				foreach (Dictionary<string, object> p in arr)
				{
					if (p.ContainsKey("id"))
					{
						res.Add(p["id"].ToString());
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
				int normalizedMinutes = NormalizeGranularityMinutes(minutes);
				if (normalizedMinutes != Math.Max(1, minutes))
				{
					Log.Warn($"[Coinbase] Unsupported granularity '{minutes}m' normalized to '{normalizedMinutes}m'.", "GetCandlesAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Exchanges\\CoinbaseExchangeClient.cs", 92);
				}
				int gran = normalizedMinutes * 60;
				TimeSpan maxSpan = TimeSpan.FromSeconds(gran * 280);
				List<Candle> res = new List<Candle>();
				DateTime cursor = startUtc;
				while (cursor < endUtc)
				{
					DateTime chunkEnd = cursor.Add(maxSpan);
					if (chunkEnd > endUtc)
					{
						chunkEnd = endUtc;
					}
					string url = "https://api.exchange.coinbase.com/products/" + pair + "/candles?granularity=" + gran + "&start=" + cursor.ToString("o") + "&end=" + chunkEnd.ToString("o");
					object[] arr = UtilCompat.JsonDeserialize<object[]>(await HttpUtil.GetAsync(url));
					if (arr != null)
					{
						object[] array = arr;
						foreach (object o in array)
						{
							object[] row = o as object[];
							if (row != null && row.Length >= 6)
							{
								res.Add(new Candle
								{
									Time = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Convert.ToInt64(row[0])),
									Low = Convert.ToDecimal(row[1]),
									High = Convert.ToDecimal(row[2]),
									Open = Convert.ToDecimal(row[3]),
									Close = Convert.ToDecimal(row[4]),
									Volume = Convert.ToDecimal(row[5])
								});
							}
						}
					}
					cursor = chunkEnd.AddSeconds(gran);
				}
				Dictionary<DateTime, Candle> dedup = new Dictionary<DateTime, Candle>();
				foreach (Candle candle in res)
				{
					dedup[candle.Time] = candle;
				}
				List<Candle> merged = new List<Candle>(dedup.Values);
				merged.Sort((Candle a, Candle b) => a.Time.CompareTo(b.Time));
				return merged;
			});
		}

		private int NormalizeGranularityMinutes(int minutes)
		{
			int requested = Math.Max(1, minutes);
			int[] supported = new int[6] { 1, 5, 15, 60, 360, 1440 };
			if (Array.IndexOf(supported, requested) >= 0)
			{
				return requested;
			}
			int closest = supported[0];
			int bestDiff = Math.Abs(requested - closest);
			for (int i = 1; i < supported.Length; i++)
			{
				int diff = Math.Abs(requested - supported[i]);
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
			return await HttpUtil.RetryAsync(async delegate
			{
				string pair = NormalizeProduct(productId);
				string url = "https://api.exchange.coinbase.com/products/" + pair + "/ticker";
				Dictionary<string, object> obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(await HttpUtil.GetAsync(url));
				return new Ticker
				{
					Bid = Convert.ToDecimal(obj["bid"]),
					Ask = Convert.ToDecimal(obj["ask"]),
					Last = Convert.ToDecimal(obj["price"]),
					Time = DateTime.Parse(obj["time"].ToString(), null, DateTimeStyles.RoundtripKind)
				};
			});
		}

		public async Task<FeeSchedule> GetFeesAsync()
		{
			Dictionary<string, object> obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(await PrivateRequestAsync("GET", "/fees"));
			return new FeeSchedule
			{
				MakerRate = Convert.ToDecimal(obj["maker_fee_rate"]),
				TakerRate = Convert.ToDecimal(obj["taker_fee_rate"]),
				Notes = "from fees endpoint"
			};
		}

		public async Task<Dictionary<string, decimal>> GetBalancesAsync()
		{
			List<Dictionary<string, object>> arr = UtilCompat.JsonDeserialize<List<Dictionary<string, object>>>(await PrivateRequestAsync("GET", "/accounts"));
			Dictionary<string, decimal> d = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
			foreach (Dictionary<string, object> a in arr)
			{
				d[a["currency"].ToString()] = Convert.ToDecimal(a["balance"]);
			}
			return d;
		}

		public async Task<OrderResult> PlaceOrderAsync(OrderRequest req)
		{
			Dictionary<string, object> body = new Dictionary<string, object>
			{
				["product_id"] = NormalizeProduct(req.ProductId),
				["side"] = ((req.Side == OrderSide.Buy) ? "buy" : "sell"),
				["type"] = ((req.Type == OrderType.Market) ? "market" : "limit")
			};
			if (req.Type == OrderType.Limit)
			{
				body["price"] = req.Price.Value.ToString(CultureInfo.InvariantCulture);
				body["size"] = req.Quantity.ToString(CultureInfo.InvariantCulture);
			}
			else
			{
				body["size"] = req.Quantity.ToString(CultureInfo.InvariantCulture);
			}
			Dictionary<string, object> obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(await PrivateRequestAsync("POST", "/orders", UtilCompat.JsonSerialize(body)));
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
			return (await PrivateRequestAsync("DELETE", "/orders/" + orderId)).Contains(orderId);
		}

		public async Task<List<Dictionary<string, object>>> GetOpenOrdersAsync()
		{
			return UtilCompat.JsonDeserialize<List<Dictionary<string, object>>>(await PrivateRequestAsync("GET", "/orders"));
		}
	}
}
