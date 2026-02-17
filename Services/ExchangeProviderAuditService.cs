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
        public string RequestedService { get; set; }
        public string CanonicalService { get; set; }
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
                : services.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();

            var normalizedList = list
                .Select(ExchangeServiceNameNormalizer.NormalizeAuditServiceName)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var results = new List<ExchangeProviderAuditResult>(normalizedList.Count);
            foreach (var service in normalizedList)
            {
                results.Add(await ValidateSingleServiceAsync(service, preferredSymbol).ConfigureAwait(false));
            }

            return results;
        }

        private async Task<ExchangeProviderAuditResult> ValidateSingleServiceAsync(string service, string preferredSymbol)
        {
            var canonicalService = ExchangeServiceNameNormalizer.NormalizeAuditServiceName(service);
            var result = new ExchangeProviderAuditResult
            {
                Service = canonicalService,
                RequestedService = service,
                CanonicalService = canonicalService
            };
            try
            {
                var client = _exchangeProvider.CreatePublicClient(canonicalService);
                result.PublicClientCreated = client != null;
                if (client == null)
                {
                    result.Error = "CreatePublicClient returned null.";
                    return result;
                }

                var discoveryWatch = Stopwatch.StartNew();
                var products = await client.ListProductsAsync().ConfigureAwait(false);
                discoveryWatch.Stop();

                var normalizedProducts = SanitizeProducts(products);

                result.ProductDiscoverySucceeded = normalizedProducts.Count > 0;
                result.ProductCount = normalizedProducts.Count;
                result.DiscoveryLatencyMs = discoveryWatch.ElapsedMilliseconds;

                var spotCount = CountProducts(normalizedProducts, false);
                var perpCount = CountProducts(normalizedProducts, true);
                result.SpotProductCount = spotCount;
                result.PerpProductCount = perpCount;
                result.SpotCoverage = spotCount > 0;
                result.PerpCoverage = perpCount > 0;

                if (!result.ProductDiscoverySucceeded)
                {
                    result.Error = "ListProductsAsync returned no products.";
                    return result;
                }

                var symbol = ResolveProbeSymbol(normalizedProducts, preferredSymbol);
                result.ProbeSymbol = symbol;

                var tickerWatch = Stopwatch.StartNew();
                var ticker = await client.GetTickerAsync(symbol).ConfigureAwait(false);
                tickerWatch.Stop();

                result.TickerLatencyMs = tickerWatch.ElapsedMilliseconds;
                result.TickerSucceeded = IsValidTicker(ticker);

                if (!result.TickerSucceeded)
                {
                    result.Error = "GetTickerAsync returned empty/invalid ticker payload (requires last>0 or bid/ask>0).";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Error = NormalizeErrorMessage(ex);
                return result;
            }
        }

        private static List<string> SanitizeProducts(IList<string> products)
        {
            var output = new List<string>();
            if (products == null || products.Count == 0)
            {
                return output;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < products.Count; i++)
            {
                var symbol = products[i];
                if (string.IsNullOrWhiteSpace(symbol))
                {
                    continue;
                }

                var trimmed = symbol.Trim();
                var normalizedKey = NormalizeProductForComparison(trimmed);
                if (normalizedKey.Length == 0 || !seen.Add(normalizedKey))
                {
                    continue;
                }

                output.Add(trimmed);
            }

            return output;
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

        private static bool IsValidTicker(Models.Ticker ticker)
        {
            if (ticker == null)
            {
                return false;
            }

            if (ticker.Last > 0m)
            {
                return true;
            }

            var hasBid = ticker.Bid > 0m;
            var hasAsk = ticker.Ask > 0m;
            if (hasBid && hasAsk && ticker.Ask >= ticker.Bid)
            {
                return true;
            }

            return false;
        }

        private static string NormalizeErrorMessage(Exception ex)
        {
            var message = ex == null ? string.Empty : (ex.Message ?? string.Empty);
            if (message.Length == 0)
            {
                return "Unknown provider audit error.";
            }

            message = message.Replace("\r", " ").Replace("\n", " ").Trim();
            if (message.Length > 800)
            {
                message = message.Substring(0, 800);
            }

            return message;
        }

        private static bool IsPerpProduct(string symbol)
        {
            var normalized = (symbol ?? string.Empty).Trim().ToUpperInvariant();
            if (normalized.Length == 0)
            {
                return false;
            }

            return normalized.Contains("PERP")
                || normalized.Contains("PERPETUAL")
                || normalized.Contains("SWAP")
                || normalized.Contains("USDTM")
                || normalized.Contains("-PI_")
                || normalized.Contains(":PERP")
                || normalized.EndsWith("_PERP", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("-PERP", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith(":SWAP", StringComparison.OrdinalIgnoreCase)
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
                var normalizedPreferred = NormalizeProductForComparison(preferredSymbol);
                for (var i = 0; i < products.Count; i++)
                {
                    if (string.Equals(products[i], preferredSymbol, StringComparison.OrdinalIgnoreCase))
                    {
                        return products[i];
                    }

                    if (string.Equals(NormalizeProductForComparison(products[i]), normalizedPreferred, StringComparison.OrdinalIgnoreCase))
                    {
                        return products[i];
                    }
                }
            }

            var bestSpotSymbol = string.Empty;
            var bestSpotScore = int.MinValue;
            for (var i = 0; i < products.Count; i++)
            {
                var symbol = products[i];
                if (string.IsNullOrWhiteSpace(symbol)) continue;
                if (!IsPerpProduct(symbol))
                {
                    var score = GetSpotProbeScore(symbol);
                    if (score > bestSpotScore)
                    {
                        bestSpotScore = score;
                        bestSpotSymbol = symbol;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(bestSpotSymbol))
            {
                return bestSpotSymbol;
            }

            return products[0];
        }

        private static int GetSpotProbeScore(string symbol)
        {
            var normalized = (symbol ?? string.Empty).Trim().ToUpperInvariant();
            if (normalized.Length == 0)
            {
                return int.MinValue;
            }

            if (normalized.Contains("BTC") && (normalized.Contains("USD") || normalized.Contains("USDT") || normalized.Contains("USDC")))
            {
                return 100;
            }

            if (normalized.EndsWith("/USD", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("-USD", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("USD", StringComparison.OrdinalIgnoreCase))
            {
                return 90;
            }

            if (normalized.EndsWith("/USDT", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("-USDT", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("USDT", StringComparison.OrdinalIgnoreCase))
            {
                return 80;
            }

            if (normalized.EndsWith("/USDC", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("-USDC", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("USDC", StringComparison.OrdinalIgnoreCase))
            {
                return 70;
            }

            return 10;
        }

        private static string NormalizeProductForComparison(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return string.Empty;
            }

            return symbol
                .Trim()
                .ToUpperInvariant()
                .Replace("/", string.Empty)
                .Replace("-", string.Empty)
                .Replace("_", string.Empty)
                .Replace(":", string.Empty)
                .Replace(" ", string.Empty);
        }

    }
}
