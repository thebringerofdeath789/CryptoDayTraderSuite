using System;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Brokers
{
    public class OkxBroker : IBroker
    {
        private readonly IKeyService _keyService;
        private readonly IAccountService _accountService;

        public OkxBroker(IKeyService keyService, IAccountService accountService)
        {
            _keyService = keyService ?? throw new ArgumentNullException(nameof(keyService));
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        }

        public string Service => "okx";

        public BrokerCapabilities GetCapabilities()
        {
            return new BrokerCapabilities
            {
                SupportsMarketEntry = true,
                SupportsProtectiveExits = false,
                EnforcesPrecisionRules = true,
                Notes = "OKX spot order paths enabled. Local protective watchdog required for exits."
            };
        }

        public async Task<(bool ok, string message)> ValidateTradePlanAsync(TradePlan plan)
        {
            if (plan == null) return (false, "trade plan is null");
            if (string.IsNullOrWhiteSpace(plan.Symbol)) return (false, "symbol is required");
            if (plan.Qty <= 0m) return (false, "quantity must be > 0");
            if (plan.Entry <= 0m) return (false, "entry must be > 0");
            if (plan.Stop <= 0m || plan.Target <= 0m) return (false, "protective stop/target required");
            if (plan.Direction == 0) return (false, "direction must be non-zero");

            if (plan.Direction > 0)
            {
                if (!(plan.Stop < plan.Entry && plan.Entry < plan.Target))
                {
                    return (false, "invalid long risk geometry (expected stop < entry < target)");
                }
            }
            else
            {
                if (!(plan.Target < plan.Entry && plan.Entry < plan.Stop))
                {
                    return (false, "invalid short risk geometry (expected target < entry < stop)");
                }
            }

            try
            {
                var client = CreateClient(plan.AccountId);
                var normalizedSymbol = NormalizeOkxSymbol(plan.Symbol);
                if (string.IsNullOrWhiteSpace(normalizedSymbol))
                {
                    return (false, "symbol is invalid after normalization");
                }

                var constraints = await client.GetSymbolConstraintsAsync(normalizedSymbol).ConfigureAwait(false);
                if (constraints == null)
                {
                    return (false, "unable to resolve okx symbol constraints");
                }

                if (constraints.StepSize > 0m)
                {
                    var alignedQty = AlignDownToStep(plan.Qty, constraints.StepSize);
                    if (alignedQty <= 0m)
                    {
                        return (false, "quantity rounds below minimum step size");
                    }

                    if (alignedQty != plan.Qty)
                    {
                        return (false, "quantity does not align with symbol step size");
                    }
                }

                if (constraints.MinQty > 0m && plan.Qty < constraints.MinQty)
                {
                    return (false, "quantity below symbol minimum");
                }

                if (constraints.MaxQty > 0m && plan.Qty > constraints.MaxQty)
                {
                    return (false, "quantity above symbol maximum");
                }

                if (constraints.PriceTickSize > 0m)
                {
                    if (!IsAlignedToStep(plan.Entry, constraints.PriceTickSize))
                    {
                        return (false, "entry does not align with symbol price tick");
                    }

                    if (!IsAlignedToStep(plan.Stop, constraints.PriceTickSize))
                    {
                        return (false, "stop does not align with symbol price tick");
                    }

                    if (!IsAlignedToStep(plan.Target, constraints.PriceTickSize))
                    {
                        return (false, "target does not align with symbol price tick");
                    }
                }

                var notional = plan.Entry * plan.Qty;
                if (constraints.MinNotional > 0m && notional < constraints.MinNotional)
                {
                    return (false, "order notional below symbol minimum");
                }
            }
            catch (Exception ex)
            {
                Log.Warn("[OkxBroker] Constraint validation failed: " + ex.Message);
                return (false, "unable to validate symbol constraints");
            }

            return (true, "ok");
        }

        public async Task<(bool ok, string message)> PlaceOrderAsync(TradePlan plan)
        {
            try
            {
                var validation = await ValidateTradePlanAsync(plan).ConfigureAwait(false);
                if (!validation.ok) return validation;

                var client = CreateClient(plan.AccountId);
                var normalizedSymbol = NormalizeOkxSymbol(plan.Symbol);
                if (string.IsNullOrWhiteSpace(normalizedSymbol))
                {
                    return (false, "symbol is invalid after normalization");
                }

                var order = new OrderRequest
                {
                    ProductId = normalizedSymbol,
                    Side = plan.Direction > 0 ? OrderSide.Buy : OrderSide.Sell,
                    Type = OrderType.Market,
                    Quantity = plan.Qty,
                    Tif = TimeInForce.GTC,
                    ClientOrderId = "cdts-okx-" + Guid.NewGuid().ToString("N")
                };

                var result = await client.PlaceOrderAsync(order).ConfigureAwait(false);
                if (result == null) return (false, "empty order result");
                if (!result.Accepted) return (false, string.IsNullOrWhiteSpace(result.Message) ? "order rejected" : result.Message);

                return (true, "accepted order=" + (result.OrderId ?? "(unknown)"));
            }
            catch (Exception ex)
            {
                Log.Error("okx place failed: " + ex.Message, ex);
                return (false, ex.Message);
            }
        }

        public async Task<(bool ok, string message)> CancelAllAsync(string symbol)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(symbol)) return (false, "symbol required for okx cancel-all");
                var normalizedSymbol = NormalizeOkxSymbol(symbol);
                if (string.IsNullOrWhiteSpace(normalizedSymbol)) return (false, "symbol is invalid after normalization");
                var client = CreateClient(null);
                var ok = await client.CancelAllOpenOrdersAsync(normalizedSymbol).ConfigureAwait(false);
                return ok ? (true, "canceled open orders") : (false, "cancel-all failed");
            }
            catch (Exception ex)
            {
                Log.Error("okx cancel failed: " + ex.Message, ex);
                return (false, ex.Message);
            }
        }

        private string NormalizeOkxSymbol(string symbol)
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

        private OkxClient CreateClient(string accountId)
        {
            var service = ResolveServiceForAccount(accountId, "okx");
            var key = ResolveKey(service, accountId);
            var apiKey = UnprotectOrRaw(key.ApiKey);
            var secret = UnprotectOrRaw(!string.IsNullOrWhiteSpace(key.ApiSecretBase64) ? key.ApiSecretBase64 : key.Secret);
            var passphrase = UnprotectOrRaw(key.Passphrase);
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(passphrase))
                throw new InvalidOperationException("incomplete okx credentials (key/secret/passphrase required)");

            var restBaseUrl = ResolveOkxRestBaseUrlForService(service);
            return new OkxClient(apiKey, secret, passphrase, restBaseUrl);
        }

        private string ResolveServiceForAccount(string accountId, string fallback)
        {
            if (string.IsNullOrWhiteSpace(accountId)) return fallback;
            var account = _accountService.Get(accountId);
            if (account == null || string.IsNullOrWhiteSpace(account.Service)) return fallback;
            return account.Service.Trim();
        }

        private string ResolveOkxRestBaseUrlForService(string service)
        {
            if (string.IsNullOrWhiteSpace(service)) return null;
            if (string.Equals(service, "okx-global", StringComparison.OrdinalIgnoreCase)) return "https://www.okx.com";
            return null;
        }

        private KeyInfo ResolveKey(string service, string accountId)
        {
            var account = !string.IsNullOrWhiteSpace(accountId) ? _accountService.Get(accountId) : null;
            var keyId = account != null ? account.KeyEntryId : null;
            if (string.IsNullOrWhiteSpace(keyId)) keyId = _keyService.GetActiveId(service);
            if (string.IsNullOrWhiteSpace(keyId) && string.Equals(service, "okx-global", StringComparison.OrdinalIgnoreCase)) keyId = _keyService.GetActiveId("okx");
            if (string.IsNullOrWhiteSpace(keyId) && string.Equals(service, "okx", StringComparison.OrdinalIgnoreCase)) keyId = _keyService.GetActiveId("okx-global");
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

        private decimal AlignDownToStep(decimal quantity, decimal stepSize)
        {
            if (quantity <= 0m || stepSize <= 0m)
            {
                return 0m;
            }

            var steps = Math.Floor(quantity / stepSize);
            var aligned = stepSize * steps;
            if (aligned < 0m)
            {
                return 0m;
            }

            return aligned;
        }

        private bool IsAlignedToStep(decimal value, decimal stepSize)
        {
            if (value <= 0m || stepSize <= 0m)
            {
                return false;
            }

            return AlignDownToStep(value, stepSize) == value;
        }
    }
}
