using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Exchanges
{
	public class CoinbasePublicClient
	{
		private class Product
		{
			public string id { get; set; }
		}

		private class Ticker
		{
			public string price { get; set; }

			public string bid { get; set; }

			public string ask { get; set; }
		}

		public class CandleRow
		{
			public DateTime TimeUtc;

			public decimal Open;

			public decimal High;

			public decimal Low;

			public decimal Close;

			public decimal Volume;
		}

		private const string Base = "https://api.exchange.coinbase.com";

		public async Task<List<string>> GetProductsAsync()
		{
			return await HttpUtil.RetryAsync(async delegate
			{
				string url = "https://api.exchange.coinbase.com/products";
				List<Product> arr = UtilCompat.JsonDeserialize<List<Product>>(await HttpUtil.GetAsync(url).ConfigureAwait(continueOnCapturedContext: false)) ?? new List<Product>();
				List<string> res = new List<string>();
				foreach (Product p in arr)
				{
					if (!string.IsNullOrEmpty(p.id))
					{
						res.Add(p.id);
					}
				}
				res.Sort(StringComparer.OrdinalIgnoreCase);
				return res;
			}).ConfigureAwait(continueOnCapturedContext: false);
		}

		public async Task<List<CandleRow>> GetCandlesAsync(string productId, int granSeconds, DateTime startUtc, DateTime endUtc)
		{
			List<CandleRow> outRows = new List<CandleRow>();
			TimeSpan step = TimeSpan.FromSeconds(granSeconds * 300);
			DateTime t = startUtc;
			new List<Task<string>>();
			while (t < endUtc)
			{
				DateTime t2 = t + step;
				if (t2 > endUtc)
				{
					t2 = endUtc;
				}
				string url = "https://api.exchange.coinbase.com/products/" + Uri.EscapeDataString(productId) + "/candles?granularity=" + granSeconds + "&start=" + Uri.EscapeDataString(t.ToString("o")) + "&end=" + Uri.EscapeDataString(t2.ToString("o"));
				try
				{
					await Task.Delay(50).ConfigureAwait(continueOnCapturedContext: false);
					List<List<double>> arr = UtilCompat.JsonDeserialize<List<List<double>>>(await HttpUtil.RetryAsync(() => HttpUtil.GetAsync(url)).ConfigureAwait(continueOnCapturedContext: false)) ?? new List<List<double>>();
					foreach (List<double> row in arr)
					{
						if (row.Count >= 6)
						{
							DateTime ts = DateTimeOffset.FromUnixTimeSeconds((long)row[0]).UtcDateTime;
							outRows.Add(new CandleRow
							{
								TimeUtc = ts,
								Low = (decimal)row[1],
								High = (decimal)row[2],
								Open = (decimal)row[3],
								Close = (decimal)row[4],
								Volume = (decimal)row[5]
							});
						}
					}
				}
				catch
				{
				}
				t = t2;
			}
			outRows.Sort((CandleRow a, CandleRow b) => a.TimeUtc.CompareTo(b.TimeUtc));
			return outRows;
		}

		public async Task<decimal> GetTickerMidAsync(string productId)
		{
			try
			{
				string url = "https://api.exchange.coinbase.com/products/" + Uri.EscapeDataString(productId) + "/ticker";
				Ticker t = UtilCompat.JsonDeserialize<Ticker>(await HttpUtil.RetryAsync(() => HttpUtil.GetAsync(url)).ConfigureAwait(continueOnCapturedContext: false));
				if (t == null)
				{
					return default(decimal);
				}
				decimal bid = default(decimal);
				decimal ask = default(decimal);
				decimal price = default(decimal);
				decimal.TryParse(t.bid ?? "0", out bid);
				decimal.TryParse(t.ask ?? "0", out ask);
				decimal.TryParse(t.price ?? "0", out price);
				if (bid > 0m && ask > 0m)
				{
					return (bid + ask) / 2m;
				}
				return price;
			}
			catch
			{
				return default(decimal);
			}
		}

		public List<string> GetProducts()
		{
			return GetProductsAsync().Result;
		}

		public List<CandleRow> GetCandles(string productId, int granSeconds, DateTime startUtc, DateTime endUtc)
		{
			return GetCandlesAsync(productId, granSeconds, startUtc, endUtc).Result;
		}

		public decimal GetTickerMid(string productId)
		{
			return GetTickerMidAsync(productId).Result;
		}
	}
}
