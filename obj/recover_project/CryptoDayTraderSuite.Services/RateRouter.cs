using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Services
{
	public class RateRouter : IRateRouter
	{
		private struct CachedRate
		{
			public decimal Rate;

			public DateTime Time;
		}

		private readonly IExchangeProvider _provider;

		private readonly ConcurrentDictionary<string, CachedRate> _mid = new ConcurrentDictionary<string, CachedRate>(StringComparer.OrdinalIgnoreCase);

		private readonly TimeSpan _ttl = TimeSpan.FromSeconds(15.0);

		public RateRouter(IExchangeProvider provider)
		{
			_provider = provider ?? throw new ArgumentNullException("provider");
		}

		public decimal Mid(string baseAsset, string quoteAsset)
		{
			return MidAsync(baseAsset, quoteAsset).GetAwaiter().GetResult();
		}

		public async Task<decimal> MidAsync(string baseAsset, string quoteAsset)
		{
			string pid = baseAsset + "-" + quoteAsset;
			if (_mid.TryGetValue(pid, out var c) && DateTime.UtcNow - c.Time < _ttl)
			{
				return c.Rate;
			}
			try
			{
				IExchangeClient client = _provider.CreatePublicClient("Coinbase");
				Ticker ticker = await client.GetTickerAsync(pid).ConfigureAwait(continueOnCapturedContext: false);
				if (ticker != null && ticker.Last > 0m)
				{
					decimal rate = ticker.Last;
					_mid[pid] = new CachedRate
					{
						Rate = rate,
						Time = DateTime.UtcNow
					};
					return rate;
				}
			}
			catch
			{
			}
			return default(decimal);
		}

		public decimal Convert(string fromAsset, string toAsset, decimal amount)
		{
			return ConvertAsync(fromAsset, toAsset, amount).GetAwaiter().GetResult();
		}

		public async Task<decimal> ConvertAsync(string fromAsset, string toAsset, decimal amount)
		{
			if (string.Equals(fromAsset, toAsset, StringComparison.OrdinalIgnoreCase))
			{
				return amount;
			}
			decimal d = await MidAsync(fromAsset, toAsset).ConfigureAwait(continueOnCapturedContext: false);
			if (d > 0m)
			{
				return amount * d;
			}
			decimal r = await MidAsync(toAsset, fromAsset).ConfigureAwait(continueOnCapturedContext: false);
			if (r > 0m)
			{
				return amount / r;
			}
			string[] hubs = new string[4] { "USD", "USDC", "USDT", "BTC" };
			string[] array = hubs;
			foreach (string hub in array)
			{
				if (hub == fromAsset || hub == toAsset)
				{
					continue;
				}
				decimal rate1 = await GetRateOrInverse(fromAsset, hub).ConfigureAwait(continueOnCapturedContext: false);
				if (!(rate1 == 0m))
				{
					decimal rate2 = await GetRateOrInverse(hub, toAsset).ConfigureAwait(continueOnCapturedContext: false);
					if (!(rate2 == 0m))
					{
						return amount * rate1 * rate2;
					}
				}
			}
			return default(decimal);
		}

		private async Task<decimal> GetRateOrInverse(string a, string b)
		{
			decimal d = await MidAsync(a, b).ConfigureAwait(continueOnCapturedContext: false);
			if (d > 0m)
			{
				return d;
			}
			decimal r = await MidAsync(b, a).ConfigureAwait(continueOnCapturedContext: false);
			if (r > 0m)
			{
				return 1m / r;
			}
			return default(decimal);
		}
	}
}
