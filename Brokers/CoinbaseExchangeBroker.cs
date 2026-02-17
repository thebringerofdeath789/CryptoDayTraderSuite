using System;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Brokers
{
    public class CoinbaseExchangeBroker : IBroker
    {
        private readonly IKeyService _keyService;
        private readonly IAccountService _accountService;

        public CoinbaseExchangeBroker(IKeyService keyService, IAccountService accountService)
        {
            _keyService = keyService ?? throw new ArgumentNullException(nameof(keyService));
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        }

        public string Service { get { return "coinbase-advanced"; } }

        public BrokerCapabilities GetCapabilities()
        {
            return new BrokerCapabilities
            {
                SupportsMarketEntry = true,
                SupportsProtectiveExits = false,
                EnforcesPrecisionRules = true,
                Notes = "Native bracket/OCO exits are unavailable; requires local protective watchdog in Auto Mode."
            };
        }

        public async Task<(bool ok, string message)> ValidateTradePlanAsync(TradePlan plan)
        {
            if (plan == null) return (false, BuildFailureMessage("validation", null, "trade plan is null"));
            if (string.IsNullOrWhiteSpace(plan.Symbol)) return (false, BuildFailureMessage("validation", null, "symbol is required"));
            if (plan.Qty <= 0m) return (false, BuildFailureMessage("validation", null, "invalid quantity"));
            if (plan.Entry <= 0m) return (false, BuildFailureMessage("validation", null, "entry must be greater than zero"));
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
                var normalizedSymbol = NormalizeCoinbaseSymbol(plan.Symbol);
                if (string.IsNullOrWhiteSpace(normalizedSymbol))
                {
                    return (false, BuildFailureMessage("validation", null, "symbol is invalid after normalization"));
                }

                var constraints = await client.GetSymbolConstraintsAsync(normalizedSymbol).ConfigureAwait(false);
                if (constraints == null)
                {
                    return (false, BuildFailureMessage("constraints", null, "unable to resolve coinbase symbol constraints"));
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
                Log.Warn("[CoinbaseExchangeBroker] Constraint validation failed: " + ex.Message);
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
                var normalizedSymbol = NormalizeCoinbaseSymbol(plan.Symbol);
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
                    ClientOrderId = "cdts-" + Guid.NewGuid().ToString("N")
                };

                return await PlaceWithClientAsync(client, order).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error("coinbase place failed: " + ex.Message, ex);
                return (false, BuildFailureMessage("place", ex.Message, "place failed"));
            }
        }

        private async Task<(bool ok, string message)> PlaceWithClientAsync(CoinbaseExchangeClient client, OrderRequest order)
        {
            var result = await client.PlaceOrderAsync(order);
            if (result == null) return (false, BuildFailureMessage("place", null, "empty order result"));
            if (!result.Accepted) return (false, BuildFailureMessage("rejected", result.Message, "order rejected"));
            return (true, BuildSuccessMessage("accepted", "order=" + (result.OrderId ?? "(unknown)")));
        }

        private string UnprotectOrRaw(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var decrypted = _keyService.Unprotect(value);
            if (!string.IsNullOrWhiteSpace(decrypted)) return decrypted;
            return value;
        }

        private CoinbaseExchangeClient CreateClient(string accountId)
        {
            var account = !string.IsNullOrWhiteSpace(accountId)
                ? _accountService.Get(accountId)
                : null;

            var keyId = account != null ? account.KeyEntryId : null;
            if (string.IsNullOrWhiteSpace(keyId)) keyId = _keyService.GetActiveId(Service);
            if (string.IsNullOrWhiteSpace(keyId)) keyId = _keyService.GetActiveId("coinbase-exchange");
            if (string.IsNullOrWhiteSpace(keyId)) throw new InvalidOperationException("no active coinbase-advanced key selected");

            var keyEntry = _keyService.Get(keyId);
            if (keyEntry == null) throw new InvalidOperationException("coinbase-advanced key not found");

            var apiKey = UnprotectOrRaw(!string.IsNullOrEmpty(keyEntry.ApiKey) ? keyEntry.ApiKey : keyEntry.Data["ApiKey"]);
            var apiSecret = UnprotectOrRaw(!string.IsNullOrEmpty(keyEntry.ApiSecretBase64)
                ? keyEntry.ApiSecretBase64
                : (!string.IsNullOrEmpty(keyEntry.Secret) ? keyEntry.Secret : keyEntry.Data["ApiSecretBase64"]));
            var passphrase = UnprotectOrRaw(!string.IsNullOrEmpty(keyEntry.Passphrase) ? keyEntry.Passphrase : keyEntry.Data["Passphrase"]);
            var apiKeyName = !string.IsNullOrEmpty(keyEntry.ApiKeyName) ? keyEntry.ApiKeyName : keyEntry.Data["ApiKeyName"];
            var ecPrivateKeyPem = UnprotectOrRaw(!string.IsNullOrEmpty(keyEntry.ECPrivateKeyPem) ? keyEntry.ECPrivateKeyPem : keyEntry.Data["ECPrivateKeyPem"]);

            CoinbaseCredentialNormalizer.NormalizeCoinbaseAdvancedInputs(ref apiKey, ref apiSecret, ref apiKeyName, ref ecPrivateKeyPem);
            if (string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(apiKeyName)) apiKey = apiKeyName;
            if (string.IsNullOrWhiteSpace(apiSecret) && !string.IsNullOrWhiteSpace(ecPrivateKeyPem)) apiSecret = ecPrivateKeyPem;

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
                throw new InvalidOperationException("incomplete coinbase credentials");

            return new CoinbaseExchangeClient(apiKey, apiSecret, passphrase);
        }

        public async Task<(bool ok, string message)> CancelAllAsync(string symbol)
        {
            try
            {
                var cli = CreateClient(null);
                var openOrders = await cli.GetOpenOrdersAsync();
                if (openOrders == null) return (false, BuildFailureMessage("cancel", null, "failed to fetch open orders"));

                var normalizedSymbol = NormalizeCoinbaseSymbol(symbol);
                if (!string.IsNullOrWhiteSpace(symbol) && string.IsNullOrWhiteSpace(normalizedSymbol))
                {
                    return (false, BuildFailureMessage("validation", null, "symbol is invalid after normalization"));
                }

                var scope = string.IsNullOrWhiteSpace(normalizedSymbol) ? "all" : normalizedSymbol;

                int canceled = 0;
                int attempted = 0;
                int failed = 0;
                foreach (var order in openOrders)
                {
                    if (order == null) continue;

                    var orderId = order.OrderId;
                    if (string.IsNullOrWhiteSpace(orderId)) continue;

                    var productId = order.ProductId;
                    productId = NormalizeCoinbaseSymbol(productId);

                    if (!string.IsNullOrWhiteSpace(normalizedSymbol))
                    {
                        if (!string.Equals(productId, normalizedSymbol, StringComparison.OrdinalIgnoreCase)) continue;
                    }

                    attempted++;
                    if (await cli.CancelOrderAsync(orderId).ConfigureAwait(false))
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
                    return (false, BuildFailureMessage("cancel", "scope=" + scope + " attempted=" + attempted + " canceled=" + canceled + " failed=" + failed, "cancel-all partial failure"));
                }

                return (true, BuildSuccessMessage("canceled", "scope=" + scope + " canceled=" + canceled));
            }
            catch (Exception ex)
            {
                Log.Error("coinbase cancel failed: " + ex.Message, ex);
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

        private string NormalizeCoinbaseSymbol(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return string.Empty;
            }

            return symbol
                .Trim()
                .Replace("/", "-")
                .Replace("_", "-")
                .ToUpperInvariant();
        }

    }
}






