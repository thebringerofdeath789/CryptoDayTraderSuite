using System;
using System.Collections;
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
	public class KrakenClient : IExchangeClient
	{
		private string _key;

		private string _secretBase64;

		private const string Rest = "https://api.kraken.com";

		private static readonly HttpClient _http = new HttpClient();

		public string Name => "Kraken";

		public KrakenClient(string key, string secretBase64)
		{
			_key = key;
			_secretBase64 = secretBase64;
		}

		public void SetCredentials(string apiKey, string apiSecret, string passphrase = null)
		{
			_key = apiKey;
			_secretBase64 = apiSecret;
		}

		public string NormalizeProduct(string uiSymbol)
		{
			string s = uiSymbol.Replace("-", "").Replace("/", "").ToUpperInvariant();
			if (s == "BTCUSD" || s == "XBTUSD")
			{
				return "XXBTZUSD";
			}
			if (s == "ETHUSD")
			{
				return "XETHZUSD";
			}
			if (s == "SOLUSD")
			{
				return "SOLUSD";
			}
			return s;
		}

		public string DenormalizeProduct(string apiSymbol)
		{
			switch (apiSymbol)
			{
			case "XXBTZUSD":
				return "BTC/USD";
			case "XETHZUSD":
				return "ETH/USD";
			case "SOLUSD":
				return "SOL/USD";
			default:
				return apiSymbol;
			}
		}

		private async Task<string> PrivatePostAsync(string path, Dictionary<string, string> form)
		{
			return await HttpUtil.RetryAsync(async delegate
			{
				string nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
				Dictionary<string, string> signForm = form ?? new Dictionary<string, string>();
				signForm["nonce"] = nonce;
				FormUrlEncodedContent post = new FormUrlEncodedContent((IEnumerable<KeyValuePair<string, string>>)signForm);
				string postStr = await ((HttpContent)post).ReadAsStringAsync();
				byte[] shaData = SecurityUtil.Sha256(Encoding.UTF8.GetBytes(nonce + postStr));
				byte[] toSign = Encoding.UTF8.GetBytes(path);
				byte[] message = new byte[toSign.Length + shaData.Length];
				Buffer.BlockCopy(toSign, 0, message, 0, toSign.Length);
				Buffer.BlockCopy(shaData, 0, message, toSign.Length, shaData.Length);
				string sig = SecurityUtil.ComputeHmacSha512Base64(_secretBase64, message);
				string url = "https://api.kraken.com" + path;
				HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, url);
				try
				{
					((HttpHeaders)req.Headers).Add("API-Key", _key);
					((HttpHeaders)req.Headers).Add("API-Sign", sig);
					((HttpHeaders)req.Headers).Add("User-Agent", "CryptoDayTraderSuite");
					req.Content = (HttpContent)(object)post;
					return await HttpUtil.SendAsync(req);
				}
				finally
				{
					((IDisposable)req)?.Dispose();
				}
			});
		}

		public Task<List<string>> ListProductsAsync()
		{
			return Task.FromResult(new List<string> { "BTC/USD", "ETH/USD", "SOL/USD" });
		}

		public async Task<List<Candle>> GetCandlesAsync(string productId, int granularity, DateTime startUtc, DateTime endUtc)
		{
			return await HttpUtil.RetryAsync(async delegate
			{
				string pair = NormalizeProduct(productId);
				int interval = granularity;
				if (interval < 1)
				{
					interval = 1;
				}
				if (interval > 1440)
				{
					interval = 1440;
				}
				string url = string.Format("{0}/0/public/OHLC?pair={1}&interval={2}", "https://api.kraken.com", pair, interval);
				Dictionary<string, object> dict = UtilCompat.JsonDeserialize<Dictionary<string, object>>(await HttpUtil.GetAsync(url));
				List<Candle> result = new List<Candle>();
				if ((dict?.ContainsKey("result") ?? false) && dict["result"] is Dictionary<string, object> res)
				{
					foreach (string k in res.Keys)
					{
						if (!(k == "last"))
						{
							object[] arr = res[k] as object[];
							if (arr == null && res[k] is ArrayList al)
							{
								arr = al.ToArray();
							}
							if (arr != null)
							{
								object[] array = arr;
								foreach (object o in array)
								{
									object[] row = o as object[];
									if (row == null && o is ArrayList rowAl)
									{
										row = rowAl.ToArray();
									}
									if (row != null && row.Length >= 6)
									{
										DateTime ts = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Convert.ToDouble(row[0]));
										if (!(ts < startUtc) && !(ts > endUtc))
										{
											result.Add(new Candle
											{
												Time = ts,
												Open = Convert.ToDecimal(row[1]),
												High = Convert.ToDecimal(row[2]),
												Low = Convert.ToDecimal(row[3]),
												Close = Convert.ToDecimal(row[4]),
												Volume = Convert.ToDecimal(row[6])
											});
										}
									}
								}
							}
						}
					}
				}
				result.Sort((Candle a, Candle b) => a.Time.CompareTo(b.Time));
				return result;
			});
		}

		public async Task<Ticker> GetTickerAsync(string productId)
		{
			return await HttpUtil.RetryAsync(async delegate
			{
				string pair = NormalizeProduct(productId);
				string url = "https://api.kraken.com/0/public/Ticker?pair=" + pair;
				Dictionary<string, object> dict = UtilCompat.JsonDeserialize<Dictionary<string, object>>(await HttpUtil.GetAsync(url));
				Ticker t = new Ticker
				{
					Time = DateTime.UtcNow
				};
				if ((dict?.ContainsKey("result") ?? false) && dict["result"] is Dictionary<string, object> res)
				{
					foreach (string k in res.Keys)
					{
						if (res[k] is Dictionary<string, object> obj)
						{
							object[] a = obj["a"] as object[];
							object[] b = obj["b"] as object[];
							object[] c = obj["c"] as object[];
							if (a != null && a.Length != 0)
							{
								t.Ask = Convert.ToDecimal(a[0]);
							}
							if (b != null && b.Length != 0)
							{
								t.Bid = Convert.ToDecimal(b[0]);
							}
							if (c != null && c.Length != 0)
							{
								t.Last = Convert.ToDecimal(c[0]);
							}
							break;
						}
					}
				}
				return t;
			});
		}

		public async Task<FeeSchedule> GetFeesAsync()
		{
			Dictionary<string, string> body = new Dictionary<string, string> { { "fee-info", "true" } };
			Dictionary<string, object> obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(await PrivatePostAsync("/0/private/TradeVolume", body));
			decimal maker = 0.0025m;
			decimal taker = 0.0040m;
			try
			{
				if ((obj?.ContainsKey("result") ?? false) && obj["result"] is Dictionary<string, object> res)
				{
					if (res.ContainsKey("fees_maker") && res["fees_maker"] is Dictionary<string, object> fm)
					{
						using (Dictionary<string, object>.KeyCollection.Enumerator enumerator = fm.Keys.GetEnumerator())
						{
							if (enumerator.MoveNext())
							{
								string key = enumerator.Current;
								Dictionary<string, object> d = fm[key] as Dictionary<string, object>;
								if (d?.ContainsKey("fee") ?? false)
								{
									maker = Convert.ToDecimal(d["fee"].ToString()) / 100m;
								}
							}
						}
					}
					if (res.ContainsKey("fees") && res["fees"] is Dictionary<string, object> ft)
					{
						using (Dictionary<string, object>.KeyCollection.Enumerator enumerator2 = ft.Keys.GetEnumerator())
						{
							if (enumerator2.MoveNext())
							{
								string key2 = enumerator2.Current;
								Dictionary<string, object> d2 = ft[key2] as Dictionary<string, object>;
								if (d2?.ContainsKey("fee") ?? false)
								{
									taker = Convert.ToDecimal(d2["fee"].ToString()) / 100m;
								}
							}
						}
					}
				}
			}
			catch
			{
			}
			return new FeeSchedule
			{
				MakerRate = maker,
				TakerRate = taker,
				Notes = "from TradeVolume"
			};
		}

		public async Task<Dictionary<string, decimal>> GetBalancesAsync()
		{
			Dictionary<string, object> obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(await PrivatePostAsync("/0/private/Balance", new Dictionary<string, string>()));
			Dictionary<string, decimal> d = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
			if ((obj?.ContainsKey("result") ?? false) && obj["result"] is Dictionary<string, object> res)
			{
				foreach (string k in res.Keys)
				{
					d[k] = Convert.ToDecimal(res[k].ToString());
				}
			}
			return d;
		}

		public async Task<OrderResult> PlaceOrderAsync(OrderRequest req)
		{
			Dictionary<string, string> form = new Dictionary<string, string>
			{
				["pair"] = NormalizeProduct(req.ProductId),
				["type"] = ((req.Side == OrderSide.Buy) ? "buy" : "sell"),
				["ordertype"] = ((req.Type == OrderType.Market) ? "market" : "limit"),
				["volume"] = req.Quantity.ToString(CultureInfo.InvariantCulture)
			};
			if (req.Type == OrderType.Limit && req.Price.HasValue)
			{
				form["price"] = req.Price.Value.ToString(CultureInfo.InvariantCulture);
			}
			Dictionary<string, object> obj = UtilCompat.JsonDeserialize<Dictionary<string, object>>(await PrivatePostAsync("/0/private/AddOrder", form));
			int num;
			if (obj.ContainsKey("error"))
			{
				object[] obj2 = obj["error"] as object[];
				num = ((obj2 == null || obj2.Length == 0) ? 1 : 0);
			}
			else
			{
				num = 1;
			}
			bool accepted = (byte)num != 0;
			string id = "";
			if (obj.ContainsKey("result") && obj["result"] is Dictionary<string, object> res)
			{
				object[] tx = res["txid"] as object[];
				if (tx != null && tx.Length != 0)
				{
					id = tx[0].ToString();
				}
			}
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
			Dictionary<string, string> form = new Dictionary<string, string> { { "txid", orderId } };
			return (await PrivatePostAsync("/0/private/CancelOrder", form)).Contains("count");
		}
	}
}
