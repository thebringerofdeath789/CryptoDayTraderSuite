using System;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Brokers
{
    public class BinanceBroker : IBroker
    {
        private readonly IKeyService _keyService;
        private readonly IAccountService _accountService;

        public BinanceBroker(IKeyService keyService, IAccountService accountService)
        {
            _keyService = keyService ?? throw new ArgumentNullException(nameof(keyService));
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        }

        public string Service => "binance";

        public BrokerCapabilities GetCapabilities()
        {
            return new BrokerCapabilities
            {
                SupportsMarketEntry = true,
                SupportsProtectiveExits = false,
                EnforcesPrecisionRules = true,
                Notes = "Binance spot market/limit supported. Local protective watchdog required for exits."
            };
        }

        public async Task<(bool ok, string message)> ValidateTradePlanAsync(TradePlan plan)
        {
            if (plan == null) return (false, BuildFailureMessage("validation", null, "trade plan is null"));
            if (string.IsNullOrWhiteSpace(plan.Symbol)) return (false, BuildFailureMessage("validation", null, "symbol is required"));
            if (plan.Qty <= 0m) return (false, BuildFailureMessage("validation", null, "quantity must be > 0"));
            if (plan.Entry <= 0m) return (false, BuildFailureMessage("validation", null, "entry must be > 0"));
            if (plan.Stop <= 0m || plan.Target <= 0m) return (false, BuildFailureMessage("validation", null, "protective stop/target required"));
            if (plan.Direction == 0) return (false, BuildFailureMessage("validation", null, "direction must be non-zero"));

            if (plan.Direction > 0)
            {
                if (!(plan.Stop < plan.Entry && plan.Entry < plan.Target))
                {
                    return (false, BuildFailureMessage("validation", null, "invalid long risk geometry (expected stop < entry < target)"));
                }
            }
            else
            {
                if (!(plan.Target < plan.Entry && plan.Entry < plan.Stop))
                {
                    return (false, BuildFailureMessage("validation", null, "invalid short risk geometry (expected target < entry < stop)"));
                }
            }

            try
            {
                var client = CreateClient(plan.AccountId);
                var normalizedSymbol = NormalizeBinanceSymbol(plan.Symbol);
                if (string.IsNullOrWhiteSpace(normalizedSymbol))
                {
                    return (false, BuildFailureMessage("validation", null, "symbol is invalid after normalization"));
                }

                var constraints = await client.GetSymbolConstraintsAsync(normalizedSymbol).ConfigureAwait(false);
                if (constraints == null)
                {
                    return (false, BuildFailureMessage("constraints", null, "unable to resolve binance symbol constraints"));
                }

                if (constraints.StepSize > 0m)
                {
                    var alignedQty = BrokerPrecision.AlignDownToStep(plan.Qty, constraints.StepSize);
                    if (alignedQty <= 0m)
                    {
                        return (false, BuildFailureMessage("constraints", null, "quantity rounds below minimum step size"));
                    }

                    if (!BrokerPrecision.IsAlignedToStep(plan.Qty, constraints.StepSize))
                    {
                        return (false, BuildFailureMessage("constraints", null, "quantity does not align with symbol step size"));
                    }
                }

                if (constraints.MinQty > 0m && plan.Qty < constraints.MinQty)
                {
                    return (false, BuildFailureMessage("constraints", null, "quantity below symbol minimum"));
                }

                if (constraints.MaxQty > 0m && plan.Qty > constraints.MaxQty)
                {
                    return (false, BuildFailureMessage("constraints", null, "quantity above symbol maximum"));
                }

                if (constraints.PriceTickSize > 0m)
                {
                    if (!BrokerPrecision.IsAlignedToStep(plan.Entry, constraints.PriceTickSize))
                    {
                        return (false, BuildFailureMessage("constraints", null, "entry does not align with symbol price tick"));
                    }

                    if (!BrokerPrecision.IsAlignedToStep(plan.Stop, constraints.PriceTickSize))
                    {
                        return (false, BuildFailureMessage("constraints", null, "stop does not align with symbol price tick"));
                    }

                    if (!BrokerPrecision.IsAlignedToStep(plan.Target, constraints.PriceTickSize))
                    {
                        return (false, BuildFailureMessage("constraints", null, "target does not align with symbol price tick"));
                    }
                }

                var notional = plan.Entry * plan.Qty;
                if (constraints.MinNotional > 0m && notional < constraints.MinNotional)
                {
                    return (false, BuildFailureMessage("constraints", null, "order notional below symbol minimum"));
                }
            }
            catch (Exception ex)
            {
                Log.Warn("[BinanceBroker] Constraint validation failed: " + ex.Message);
                return (false, BuildFailureMessage("constraints", ex.Message, "unable to validate symbol constraints"));
            }

            return (true, BuildSuccessMessage("validation", "ok"));
        }

        public async Task<(bool ok, string message)> PlaceOrderAsync(TradePlan plan)
        {
            try
            {
                var validation = await ValidateTradePlanAsync(plan).ConfigureAwait(false);
                if (!validation.ok) return validation;

                var client = CreateClient(plan.AccountId);
                var normalizedSymbol = NormalizeBinanceSymbol(plan.Symbol);
                if (string.IsNullOrWhiteSpace(normalizedSymbol))
                {
                    return (false, BuildFailureMessage("validation", null, "symbol is invalid after normalization"));
                }

                var order = new OrderRequest
                {
                    ProductId = normalizedSymbol,
                    Side = plan.Direction > 0 ? OrderSide.Buy : OrderSide.Sell,
                    Type = OrderType.Market,
                    Quantity = plan.Qty,
                    Tif = TimeInForce.GTC,
                    ClientOrderId = "cdts-binance-" + Guid.NewGuid().ToString("N")
                };

                var result = await client.PlaceOrderAsync(order).ConfigureAwait(false);
                if (result == null) return (false, BuildFailureMessage("place-response", null, "empty order result"));
                if (!result.Accepted) return (false, BuildFailureMessage("place-rejected", result.Message, "order rejected"));

                return (true, BuildSuccessMessage("place-accepted", "order=" + (result.OrderId ?? "(unknown)")));
            }
            catch (Exception ex)
            {
                Log.Error("binance place failed: " + ex.Message, ex);
                return (false, BuildFailureMessage("place", ex.Message, "place failed"));
            }
        }

        public async Task<(bool ok, string message)> CancelAllAsync(string symbol)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(symbol)) return (false, BuildFailureMessage("validation", null, "symbol required for binance cancel-all"));
                var normalizedSymbol = NormalizeBinanceSymbol(symbol);
                if (string.IsNullOrWhiteSpace(normalizedSymbol)) return (false, BuildFailureMessage("validation", null, "symbol is invalid after normalization"));
                var client = CreateClient(null);
                var openOrders = await client.GetOpenOrdersAsync(normalizedSymbol).ConfigureAwait(false);
                if (openOrders == null) return (false, BuildFailureMessage("cancel", null, "failed to fetch open orders"));

                int canceled = 0;
                int attempted = 0;
                int failed = 0;
                foreach (var order in openOrders)
                {
                    if (order == null || string.IsNullOrWhiteSpace(order.OrderId))
                    {
                        continue;
                    }

                    var productId = NormalizeBinanceSymbol(order.ProductId);
                    if (!string.Equals(productId, normalizedSymbol, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    attempted++;
                    if (await client.CancelOrderAsync(order.OrderId).ConfigureAwait(false))
                    {
                        canceled++;
                    }
                    else
                    {
                        failed++;
                    }
                }

                if (failed > 0)
                {
                    return (false, BuildFailureMessage("cancel", "symbol=" + normalizedSymbol + " attempted=" + attempted + " canceled=" + canceled + " failed=" + failed, "cancel-all partial failure"));
                }

                return (true, BuildSuccessMessage("canceled", "symbol=" + normalizedSymbol + " canceled=" + canceled));
            }
            catch (Exception ex)
            {
                Log.Error("binance cancel failed: " + ex.Message, ex);
                return (false, BuildFailureMessage("cancel", ex.Message, "cancel failed"));
            }
        }
        private string BuildSuccessMessage(string category, string detail)
        {
            return BrokerMessageFormatter.BuildSuccessMessage(category, detail);
        }

        private string BuildFailureMessage(string category, string detail, string fallback)
        {
            return BrokerMessageFormatter.BuildFailureMessage(category, detail, fallback);
        }

        private string NormalizeBinanceSymbol(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return string.Empty;
            }

            return symbol
                .Trim()
                .Replace("/", string.Empty)
                .Replace("-", string.Empty)
                .Replace("_", string.Empty)
                .ToUpperInvariant();
        }

        private BinanceClient CreateClient(string accountId)
        {
            var service = ResolveServiceForAccount(accountId, "binance");
            var key = ResolveKey(service, accountId);
            var apiKey = UnprotectOrRaw(key.ApiKey);
            var secret = UnprotectOrRaw(!string.IsNullOrWhiteSpace(key.ApiSecretBase64) ? key.ApiSecretBase64 : key.Secret);
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("incomplete binance credentials");

            var restBaseUrl = ResolveBinanceRestBaseUrlForService(service);
            return new BinanceClient(apiKey, secret, restBaseUrl);
        }

        private string ResolveServiceForAccount(string accountId, string fallback)
        {
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return fallback;
            }

            var account = _accountService.Get(accountId);
            if (account == null || string.IsNullOrWhiteSpace(account.Service))
            {
                return fallback;
            }

            return account.Service.Trim();
        }

        private string ResolveBinanceRestBaseUrlForService(string service)
        {
            if (string.IsNullOrWhiteSpace(service))
            {
                return null;
            }

            if (string.Equals(service, "binance-us", StringComparison.OrdinalIgnoreCase))
            {
                return "https://api.binance.us";
            }

            if (string.Equals(service, "binance-global", StringComparison.OrdinalIgnoreCase))
            {
                return "https://api.binance.com";
            }

            return null;
        }

        private KeyInfo ResolveKey(string service, string accountId)
        {
            var account = !string.IsNullOrWhiteSpace(accountId) ? _accountService.Get(accountId) : null;
            var keyId = account != null ? account.KeyEntryId : null;
            if (string.IsNullOrWhiteSpace(keyId)) keyId = _keyService.GetActiveId(service);
            if (string.IsNullOrWhiteSpace(keyId) && string.Equals(service, "binance-global", StringComparison.OrdinalIgnoreCase)) keyId = _keyService.GetActiveId("binance");
            if (string.IsNullOrWhiteSpace(keyId) && string.Equals(service, "binance", StringComparison.OrdinalIgnoreCase)) keyId = _keyService.GetActiveId("binance-global");
            if (string.IsNullOrWhiteSpace(keyId) && string.Equals(service, "binance-us", StringComparison.OrdinalIgnoreCase)) keyId = _keyService.GetActiveId("binance");
            if (string.IsNullOrWhiteSpace(keyId) && string.Equals(service, "binance", StringComparison.OrdinalIgnoreCase)) keyId = _keyService.GetActiveId("binance-us");
            if (string.IsNullOrWhiteSpace(keyId)) throw new InvalidOperationException("no active " + service + " key selected");
            var key = _keyService.Get(keyId);
            if (key == null) throw new InvalidOperationException("active " + service + " key not found");
            return key;
        }

        private string UnprotectOrRaw(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var decrypted = _keyService.Unprotect(value);
            return string.IsNullOrWhiteSpace(decrypted) ? value : decrypted;
        }

    }
}




