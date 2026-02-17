using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Exchanges;

namespace CryptoDayTraderSuite.Services
{
    public sealed class ExchangeProviderAuditResult
    {
        public string Service { get; set; }
        public bool PublicClientCreated { get; set; }
        public bool ProductDiscoverySucceeded { get; set; }
        public bool TickerSucceeded { get; set; }
        public bool SpotCoverage { get; set; }
        public bool PerpCoverage { get; set; }
        public string ProbeSymbol { get; set; }
        public int ProductCount { get; set; }
        public int SpotProductCount { get; set; }
        public int PerpProductCount { get; set; }
        public long DiscoveryLatencyMs { get; set; }
        public long TickerLatencyMs { get; set; }
        public string Error { get; set; }
    }

    public interface IExchangeProviderAuditService
    {
        Task<IReadOnlyList<ExchangeProviderAuditResult>> ValidatePublicApisAsync(IEnumerable<string> services, string preferredSymbol = null);
    }

    public sealed class ExchangeProviderAuditService : IExchangeProviderAuditService
    {
        private readonly IExchangeProvider _exchangeProvider;

        public ExchangeProviderAuditService(IExchangeProvider exchangeProvider)
        {
            _exchangeProvider = exchangeProvider ?? throw new ArgumentNullException(nameof(exchangeProvider));
        }

        public async Task<IReadOnlyList<ExchangeProviderAuditResult>> ValidatePublicApisAsync(IEnumerable<string> services, string preferredSymbol = null)
        {
            var list = services == null
                ? new List<string>()
                : services.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var results = new List<ExchangeProviderAuditResult>(list.Count);
            foreach (var service in list)
            {
                results.Add(await ValidateSingleServiceAsync(service, preferredSymbol).ConfigureAwait(false));
            }

            return results;
        }

        private async Task<ExchangeProviderAuditResult> ValidateSingleServiceAsync(string service, string preferredSymbol)
        {
            var result = new ExchangeProviderAuditResult { Service = service };
            try
            {
                var client = _exchangeProvider.CreatePublicClient(service);
                result.PublicClientCreated = client != null;
                if (client == null)
                {
                    result.Error = "CreatePublicClient returned null.";
                    return result;
                }

                var discoveryWatch = Stopwatch.StartNew();
                var products = await client.ListProductsAsync().ConfigureAwait(false);
                discoveryWatch.Stop();

                result.ProductDiscoverySucceeded = products != null && products.Count > 0;
                result.ProductCount = products != null ? products.Count : 0;
                result.DiscoveryLatencyMs = discoveryWatch.ElapsedMilliseconds;

                var spotCount = CountProducts(products, false);
                var perpCount = CountProducts(products, true);
                result.SpotProductCount = spotCount;
                result.PerpProductCount = perpCount;
                result.SpotCoverage = spotCount > 0;
                result.PerpCoverage = perpCount > 0;

                if (!result.ProductDiscoverySucceeded)
                {
                    result.Error = "ListProductsAsync returned no products.";
                    return result;
                }

                var symbol = ResolveProbeSymbol(products, preferredSymbol);
                result.ProbeSymbol = symbol;

                var tickerWatch = Stopwatch.StartNew();
                var ticker = await client.GetTickerAsync(symbol).ConfigureAwait(false);
                tickerWatch.Stop();

                result.TickerLatencyMs = tickerWatch.ElapsedMilliseconds;
                result.TickerSucceeded = ticker != null && ticker.Last > 0m;

                if (!result.TickerSucceeded)
                {
                    result.Error = "GetTickerAsync returned empty/invalid ticker payload.";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                return result;
            }
        }

        private static int CountProducts(IList<string> products, bool perp)
        {
            if (products == null || products.Count == 0)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < products.Count; i++)
            {
                var symbol = products[i];
                if (string.IsNullOrWhiteSpace(symbol)) continue;

                var isPerp = IsPerpProduct(symbol);
                if (perp == isPerp)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsPerpProduct(string symbol)
        {
            var normalized = (symbol ?? string.Empty).Trim().ToUpperInvariant();
            if (normalized.Length == 0)
            {
                return false;
            }

            return normalized.Contains("PERP")
                || normalized.Contains("SWAP")
                || normalized.Contains("-PI_")
                || normalized.Contains(":PERP")
                || normalized.EndsWith("USDTPERP", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("USDT-PERP", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("USDT:USDT", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("USD:USD", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveProbeSymbol(IList<string> products, string preferredSymbol)
        {
            if (products == null || products.Count == 0) return preferredSymbol ?? "BTC-USD";

            if (!string.IsNullOrWhiteSpace(preferredSymbol))
            {
                for (var i = 0; i < products.Count; i++)
                {
                    if (string.Equals(products[i], preferredSymbol, StringComparison.OrdinalIgnoreCase))
                    {
                        return products[i];
                    }
                }
            }

            return products[0];
        }
    }
}
