using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Services
{
    public sealed class CoinbaseReadOnlyImportResult
    {
        public string KeyId;
        public int ProductCount;
        public int NonZeroBalanceCount;
        public decimal TotalBalance;
        public string TotalBalanceQuoteCurrency;
        public decimal TotalBalanceInQuote;
        public int TotalBalanceExcludedCount;
        public decimal MakerRate;
        public decimal TakerRate;
        public int AccountsImported;
        public int TotalFillCount;
        public int ImportedTradeCount;
        public decimal TotalFeesPaid;
        public decimal NetProfitEstimate;
        public List<KeyValuePair<string, decimal>> Holdings = new List<KeyValuePair<string, decimal>>();
        public DateTime ImportedUtc;
    }

    [Serializable]
    public sealed class CoinbaseHoldingSnapshot
    {
        public string Currency;
        public decimal Amount;
    }

    [Serializable]
    public sealed class CoinbaseAccountSnapshot
    {
        public string KeyId;
        public DateTime ImportedUtc;
        public int ProductCount;
        public int NonZeroBalanceCount;
        public decimal TotalBalance;
        public string TotalBalanceQuoteCurrency;
        public decimal TotalBalanceInQuote;
        public int TotalBalanceExcludedCount;
        public decimal MakerRate;
        public decimal TakerRate;
        public int TotalFillCount;
        public int ImportedTradeCount;
        public decimal TotalFeesPaid;
        public decimal NetProfitEstimate;
        public List<CoinbaseHoldingSnapshot> Holdings = new List<CoinbaseHoldingSnapshot>();
    }

    public class CoinbaseReadOnlyImportService
    {
        private readonly IKeyService _keyService;
        private readonly IAccountService _accountService;
        private readonly IHistoryService _historyService;

        public CoinbaseReadOnlyImportService(IKeyService keyService, IAccountService accountService, IHistoryService historyService = null)
        {
            _keyService = keyService;
            _accountService = accountService;
            _historyService = historyService;
        }

        public async Task<CoinbaseReadOnlyImportResult> ValidateAndImportAsync()
        {
            if (_keyService == null) throw new InvalidOperationException("Key service is required.");
            if (_accountService == null) throw new InvalidOperationException("Account service is required.");

            var key = ResolveCoinbaseKeyByPreference();
            if (key == null)
            {
                throw new InvalidOperationException("No active Coinbase key found. Set an active Coinbase key first.");
            }

            var keyId = KeyEntry.MakeId(CanonicalCoinbaseService(key.Broker ?? key.Service), key.Label ?? string.Empty);
            return await ValidateAndImportForKeyAsync(keyId).ConfigureAwait(false);
        }

        public async Task<CoinbaseReadOnlyImportResult> ValidateAndImportForKeyAsync(string keyId)
        {
            if (_keyService == null) throw new InvalidOperationException("Key service is required.");
            if (_accountService == null) throw new InvalidOperationException("Account service is required.");

            if (string.IsNullOrWhiteSpace(keyId))
            {
                throw new InvalidOperationException("A Coinbase key id is required for import.");
            }

            var key = ResolveCoinbaseKeyByIdOrFallback(keyId);
            if (key == null)
            {
                throw new InvalidOperationException("Coinbase key not found for id: " + keyId + ".");
            }
            if (!key.Enabled)
            {
                throw new InvalidOperationException("Coinbase key is disabled for id: " + keyId + ".");
            }

            if (!IsCoinbaseServiceName(key.Broker ?? key.Service))
            {
                throw new InvalidOperationException("Key id does not reference a Coinbase service: " + keyId + ".");
            }

            var service = CanonicalCoinbaseService(key.Broker ?? key.Service);
            if (string.IsNullOrWhiteSpace(service))
            {
                throw new InvalidOperationException("Unable to resolve canonical Coinbase service for key id: " + keyId + ".");
            }
            var normalizedKeyId = KeyEntry.MakeId(service, key.Label ?? string.Empty);

            var apiKey = SafeUnprotect(key.ApiKey);
            var apiSecret = SafeUnprotect(!string.IsNullOrWhiteSpace(key.ApiSecretBase64) ? key.ApiSecretBase64 : key.Secret);
            var passphrase = SafeUnprotect(key.Passphrase);

            var keyName = key.ApiKeyName ?? string.Empty;
            var pem = SafeUnprotect(key.ECPrivateKeyPem);
            CryptoDayTraderSuite.Util.CoinbaseCredentialNormalizer.NormalizeCoinbaseAdvancedInputs(ref apiKey, ref apiSecret, ref keyName, ref pem);

            var client = new CoinbaseExchangeClient(keyName, pem, passphrase);

            var products = await client.ListProductsAsync().ConfigureAwait(false);
            var fees = await client.GetFeesAsync().ConfigureAwait(false);
            var balances = await client.GetBalancesAsync().ConfigureAwait(false);
            var fills = new List<Dictionary<string, object>>();
            var fillLimit = ResolveFillImportLimit();
            try
            {
                fills = await client.GetRecentFillsAsync(fillLimit).ConfigureAwait(false);
                Log.Info("[CoinbaseReadOnlyImport] Fill fetch summary: requestedLimit=" + fillLimit
                    + " fetched=" + (fills == null ? 0 : fills.Count)
                    + " maybeTruncated=" + ((fills != null && fills.Count >= fillLimit) ? "1" : "0"));
            }
            catch (Exception ex)
            {
                Log.Warn("[CoinbaseReadOnlyImport] Recent fills unavailable (continuing without fills telemetry): " + ex.Message);
            }

            var nonZero = balances.Where(x => x.Value > 0m).ToList();
            var defaultQuote = ResolveDefaultQuote(nonZero);
            var quoteScopedTotal = ComputeQuoteScopedTotal(nonZero, defaultQuote);
            var imported = ImportOrUpdateAccount(service, normalizedKeyId, key.Label, nonZero);
            var tradeStats = ImportTradeHistory(service, fills);

            var result = new CoinbaseReadOnlyImportResult
            {
                KeyId = normalizedKeyId,
                ProductCount = products == null ? 0 : products.Count,
                NonZeroBalanceCount = nonZero.Count,
                TotalBalance = quoteScopedTotal.AmountInQuote,
                TotalBalanceQuoteCurrency = quoteScopedTotal.QuoteCurrency,
                TotalBalanceInQuote = quoteScopedTotal.AmountInQuote,
                TotalBalanceExcludedCount = quoteScopedTotal.ExcludedCount,
                MakerRate = fees == null ? 0m : fees.MakerRate,
                TakerRate = fees == null ? 0m : fees.TakerRate,
                AccountsImported = imported,
                TotalFillCount = tradeStats.TotalFills,
                ImportedTradeCount = tradeStats.ImportedFills,
                TotalFeesPaid = tradeStats.TotalFees,
                NetProfitEstimate = tradeStats.NetProfitEstimate,
                Holdings = nonZero.OrderByDescending(x => x.Value).ToList(),
                ImportedUtc = DateTime.UtcNow
            };

            SaveSnapshot(result);
            return result;
        }

        public CoinbaseAccountSnapshot GetLatestSnapshot(string keyId)
        {
            if (string.IsNullOrWhiteSpace(keyId)) return null;

            var snapshots = LoadSnapshotMap();
            CoinbaseAccountSnapshot snapshot;
            if (snapshots.TryGetValue(keyId, out snapshot))
            {
                return snapshot;
            }

            string broker;
            string label;
            KeyEntry.SplitId(keyId, out broker, out label);
            var canonicalKey = KeyEntry.MakeId("coinbase-advanced", label ?? string.Empty);
            if (snapshots.TryGetValue(canonicalKey, out snapshot))
            {
                return snapshot;
            }

            return null;
        }

        private TradeImportStats ImportTradeHistory(string service, List<Dictionary<string, object>> fills)
        {
            var stats = new TradeImportStats();
            if (_historyService == null || fills == null || fills.Count == 0)
            {
                stats.TotalFills = fills == null ? 0 : fills.Count;
                return stats;
            }

            stats.TotalFills = fills.Count;

            var existing = _historyService.LoadTrades();
            var existingMarkers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < existing.Count; i++)
            {
                var note = existing[i] == null ? string.Empty : (existing[i].Notes ?? string.Empty);
                if (IsCoinbaseFillMarker(note))
                {
                    existingMarkers.Add(note.Trim());
                }
            }

            for (int i = 0; i < fills.Count; i++)
            {
                var row = fills[i];
                if (row == null) continue;

                var fillId = ReadStringByCandidates(row, "trade_id", "tradeId", "entry_id", "entryId", "id");

                var productId = ReadStringByCandidates(row, "product_id", "productId", "product", "symbol", "market", "instrument");
                if (string.IsNullOrWhiteSpace(productId)) productId = "UNKNOWN";
                productId = NormalizeProductId(productId);

                var side = ReadStringByCandidates(row, "side", "order_side", "orderSide", "trade_side", "tradeSide");
                var sideUpper = (side ?? string.Empty).Trim().ToUpperInvariant();
                if (sideUpper == "B") sideUpper = "BUY";
                if (sideUpper == "S") sideUpper = "SELL";
                if (sideUpper != "BUY" && sideUpper != "SELL")
                {
                    continue;
                }

                var fillStatus = ReadStringByCandidates(row, "status", "trade_status", "tradeStatus", "fill_status", "fillStatus", "order_status", "orderStatus");
                if (IsRejectedOrCanceledFillStatus(fillStatus))
                {
                    continue;
                }

                var qty = ReadDecimalByCandidates(row, "size", "base_size", "filled_size", "filled_quantity", "filledQuantity", "quantity", "qty", "last_fill_size", "lastFillSize");
                var price = ReadDecimalByCandidates(row, "price", "trade_price", "tradePrice", "fill_price", "fillPrice", "average_filled_price", "averageFilledPrice", "avg_price", "avgPrice");
                var notional = ReadDecimalByCandidates(row, "notional", "quote_size", "quoteSize", "quote_volume", "quoteVolume", "filled_value", "filledValue", "executed_value", "executedValue", "value", "usd_value", "usdValue");
                var fee = ReadDecimalByCandidates(row, "fee", "fees", "commission", "total_fees", "totalFees", "fill_fees", "fillFees", "fee_amount", "feeAmount");

                if (price <= 0m && qty > 0m && notional > 0m)
                {
                    price = notional / qty;
                }
                if (qty <= 0m && price > 0m && notional > 0m)
                {
                    qty = notional / price;
                }
                if (notional <= 0m && qty > 0m && price > 0m)
                {
                    notional = qty * price;
                }

                if (qty <= 0m)
                {
                    continue;
                }

                if (price <= 0m && notional <= 0m)
                {
                    continue;
                }

                var atUtc = ReadDateTimeByCandidates(row, "trade_time", "tradeTime", "fill_time", "fillTime", "created_time", "createdTime", "time", "timestamp");
                if (atUtc == DateTime.MinValue) atUtc = DateTime.UtcNow;

                var marker = BuildFillMarker(fillId, productId, sideUpper, qty, price, notional, fee, atUtc);
                if (existingMarkers.Contains(marker))
                {
					continue;
                }

                var feePaid = fee > 0m ? fee : 0m;
                stats.TotalFees += feePaid;

                var gross = notional > 0m ? notional : (qty * price);

                if (sideUpper == "SELL")
                {
                    stats.NetProfitEstimate += gross - fee;
                }
                else if (sideUpper == "BUY")
                {
                    stats.NetProfitEstimate -= gross + fee;
                }

                var trade = new TradeRecord
                {
                    Exchange = service,
                    ProductId = productId,
                    AtUtc = atUtc,
                    Strategy = "Coinbase Import",
                    Side = sideUpper,
                    Quantity = qty,
                    Price = price,
                    EstEdge = 0m,
                    Executed = true,
                    FillPrice = price > 0m ? (decimal?)price : null,
                    PnL = null,
                    Notes = marker,
                    Enabled = true
                };

                _historyService.SaveTrade(trade);
                existingMarkers.Add(marker);
                stats.ImportedFills++;
            }

            return stats;
        }

        private int ImportOrUpdateAccount(string service, string keyId, string keyLabel, List<KeyValuePair<string, decimal>> balances)
        {
            var all = _accountService.GetAll();
            var existing = all.FirstOrDefault(a => string.Equals(a.KeyEntryId ?? string.Empty, keyId ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            var defaultQuote = ResolveDefaultQuote(balances);

            if (existing != null)
            {
                existing.Service = service;
                if (string.IsNullOrWhiteSpace(existing.Label)) existing.Label = BuildImportedLabel(keyLabel);
                existing.DefaultQuote = defaultQuote;
                existing.UpdatedUtc = DateTime.UtcNow;
                _accountService.Upsert(existing);
                return 0;
            }

            var account = new AccountInfo
            {
                Id = Guid.NewGuid().ToString(),
                Label = BuildImportedLabel(keyLabel),
                Service = service,
                Mode = AccountMode.Live,
                RiskPerTradePct = 1m,
                MaxConcurrentTrades = 3,
                KeyEntryId = keyId,
                DefaultQuote = defaultQuote,
                Enabled = true,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            };

            _accountService.Upsert(account);
            return 1;
        }

        private string ResolveDefaultQuote(List<KeyValuePair<string, decimal>> balances)
        {
            if (balances == null || balances.Count == 0) return "USD";
            var preferred = balances.FirstOrDefault(b => string.Equals(b.Key, "USD", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(preferred.Key)) return preferred.Key;

            var top = balances.OrderByDescending(b => b.Value).FirstOrDefault();
            return string.IsNullOrWhiteSpace(top.Key) ? "USD" : top.Key;
        }

        private string BuildImportedLabel(string keyLabel)
        {
            var safe = string.IsNullOrWhiteSpace(keyLabel) ? "Default" : keyLabel.Trim();
            return "Coinbase Imported - " + safe;
        }

        private QuoteScopedTotal ComputeQuoteScopedTotal(List<KeyValuePair<string, decimal>> balances, string defaultQuote)
        {
            var quote = string.IsNullOrWhiteSpace(defaultQuote) ? "USD" : defaultQuote.Trim().ToUpperInvariant();
            var result = new QuoteScopedTotal
            {
                QuoteCurrency = quote,
                AmountInQuote = 0m,
                ExcludedCount = 0
            };

            if (balances == null || balances.Count == 0)
            {
                return result;
            }

            for (int i = 0; i < balances.Count; i++)
            {
                var currency = string.IsNullOrWhiteSpace(balances[i].Key) ? string.Empty : balances[i].Key.Trim().ToUpperInvariant();
                var amount = balances[i].Value;
                if (amount <= 0m)
                {
                    continue;
                }

                if (IsQuoteEquivalentCurrency(currency, quote))
                {
                    result.AmountInQuote += amount;
                }
                else
                {
                    result.ExcludedCount++;
                }
            }

            return result;
        }

        private bool IsQuoteEquivalentCurrency(string currency, string quote)
        {
            if (string.IsNullOrWhiteSpace(currency) || string.IsNullOrWhiteSpace(quote))
            {
                return false;
            }

            if (string.Equals(currency, quote, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (IsUsdFamily(quote) && IsUsdFamily(currency))
            {
                return true;
            }

            return false;
        }

        private bool IsUsdFamily(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return false;
            }

            var normalized = symbol.Trim().ToUpperInvariant();
            return normalized == "USD"
                || normalized == "USDC"
                || normalized == "USDT"
                || normalized == "USDP"
                || normalized == "FDUSD"
                || normalized == "TUSD";
        }

        private KeyInfo ResolveCoinbaseKeyByPreference()
        {
            var ids = new List<string>
            {
                _keyService.GetActiveId("coinbase-advanced"),
                _keyService.GetActiveId("coinbase-exchange"),
                _keyService.GetActiveId("coinbase")
            };

            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                var item = _keyService.Get(id);
                if (item != null) return NormalizeKeyService(item);
            }

            var all = _keyService.GetAll();
            var fallback = all.FirstOrDefault(k =>
            {
                var service = (k.Broker ?? k.Service ?? string.Empty).Trim();
                return service.IndexOf("coinbase", StringComparison.OrdinalIgnoreCase) >= 0 && k.Enabled;
            });

            return fallback == null ? null : NormalizeKeyService(fallback);
        }

        private KeyInfo ResolveCoinbaseKeyByIdOrFallback(string keyId)
        {
            if (!string.IsNullOrWhiteSpace(keyId))
            {
                var direct = _keyService.Get(keyId);
                if (direct != null)
                {
                    if (!IsCoinbaseServiceName(direct.Broker ?? direct.Service))
                    {
                        return null;
                    }
                    return NormalizeKeyService(direct);
                }

                string broker;
                string label;
                KeyEntry.SplitId(keyId, out broker, out label);
                var canonicalId = KeyEntry.MakeId("coinbase-advanced", label ?? string.Empty);
                direct = _keyService.Get(canonicalId);
                if (direct != null)
                {
                    if (!IsCoinbaseServiceName(direct.Broker ?? direct.Service))
                    {
                        return null;
                    }
                    return NormalizeKeyService(direct);
                }

                return null;
            }

            return ResolveCoinbaseKeyByPreference();
        }

        private KeyInfo NormalizeKeyService(KeyInfo key)
        {
            if (key == null) return null;
            var normalized = CanonicalCoinbaseService(key.Broker ?? key.Service);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                key.Broker = normalized;
                key.Service = normalized;
            }
            return key;
        }

        private string CanonicalCoinbaseService(string raw)
        {
            if (IsCoinbaseServiceName(raw))
            {
                return "coinbase-advanced";
            }
            return string.Empty;
        }

        private bool IsCoinbaseServiceName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            return raw.IndexOf("coinbase", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string SafeUnprotect(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            try { return _keyService.Unprotect(value); }
            catch { return string.Empty; }
        }

        private void SaveSnapshot(CoinbaseReadOnlyImportResult result)
        {
            if (result == null || string.IsNullOrWhiteSpace(result.KeyId))
            {
                return;
            }

            var map = LoadSnapshotMap();
            map[result.KeyId] = new CoinbaseAccountSnapshot
            {
                KeyId = result.KeyId,
                ImportedUtc = result.ImportedUtc,
                ProductCount = result.ProductCount,
                NonZeroBalanceCount = result.NonZeroBalanceCount,
                TotalBalance = result.TotalBalance,
                TotalBalanceQuoteCurrency = result.TotalBalanceQuoteCurrency,
                TotalBalanceInQuote = result.TotalBalanceInQuote,
                TotalBalanceExcludedCount = result.TotalBalanceExcludedCount,
                MakerRate = result.MakerRate,
                TakerRate = result.TakerRate,
                TotalFillCount = result.TotalFillCount,
                ImportedTradeCount = result.ImportedTradeCount,
                TotalFeesPaid = result.TotalFeesPaid,
                NetProfitEstimate = result.NetProfitEstimate,
                Holdings = result.Holdings == null
                    ? new List<CoinbaseHoldingSnapshot>()
                    : result.Holdings.Select(h => new CoinbaseHoldingSnapshot
                    {
                        Currency = h.Key,
                        Amount = h.Value
                    }).ToList()
            };

            SaveSnapshotMap(map);
        }

        private Dictionary<string, CoinbaseAccountSnapshot> LoadSnapshotMap()
        {
            try
            {
                var path = SnapshotPath();
                if (!File.Exists(path))
                {
                    return new Dictionary<string, CoinbaseAccountSnapshot>(StringComparer.OrdinalIgnoreCase);
                }

                var json = File.ReadAllText(path);
                var map = UtilCompat.JsonDeserialize<Dictionary<string, CoinbaseAccountSnapshot>>(json);
                return map ?? new Dictionary<string, CoinbaseAccountSnapshot>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, CoinbaseAccountSnapshot>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private void SaveSnapshotMap(Dictionary<string, CoinbaseAccountSnapshot> map)
        {
            try
            {
                var path = SnapshotPath();
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = UtilCompat.JsonSerialize(map ?? new Dictionary<string, CoinbaseAccountSnapshot>(StringComparer.OrdinalIgnoreCase));
                File.WriteAllText(path, json);
            }
            catch
            {
            }
        }

        private string SnapshotPath()
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CryptoDayTraderSuite");
            return Path.Combine(root, "coinbase_account_snapshots.json");
        }

        private int ResolveFillImportLimit()
        {
            const int fallback = 1000;
            const int min = 100;
            const int max = 5000;

            var raw = Environment.GetEnvironmentVariable("CDTS_COINBASE_IMPORT_FILL_LIMIT");
            int parsed;
            if (!int.TryParse(raw, out parsed))
            {
                return fallback;
            }

            if (parsed < min) return min;
            if (parsed > max) return max;
            return parsed;
        }

        private decimal ReadDecimal(Dictionary<string, object> row, string key)
        {
            object raw;
            if (!TryGetObjectValue(row, key, out raw) || raw == null)
            {
                return 0m;
            }

            decimal parsed;
            return decimal.TryParse(raw.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsed)
                ? parsed
                : 0m;
        }

        private string ReadString(Dictionary<string, object> row, string key)
        {
            object raw;
            if (!TryGetObjectValue(row, key, out raw) || raw == null)
            {
                return string.Empty;
            }

            return raw.ToString();
        }

        private decimal ReadDecimalByCandidates(Dictionary<string, object> row, params string[] keys)
        {
            if (row == null || keys == null)
            {
                return 0m;
            }

            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                if (string.IsNullOrWhiteSpace(key)) continue;

                var value = ReadDecimal(row, key);
                if (value != 0m)
                {
                    return value;
                }
            }

            return 0m;
        }

        private string ReadStringByCandidates(Dictionary<string, object> row, params string[] keys)
        {
            if (row == null || keys == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                if (string.IsNullOrWhiteSpace(key)) continue;

                var value = ReadString(row, key);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private DateTime ReadDateTimeByCandidates(Dictionary<string, object> row, params string[] keys)
        {
            if (row == null || keys == null)
            {
                return DateTime.MinValue;
            }

            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                if (string.IsNullOrWhiteSpace(key)) continue;

                var parsed = ReadDateTime(row, key);
                if (parsed != DateTime.MinValue)
                {
                    return parsed;
                }
            }

            return DateTime.MinValue;
        }

        private string BuildFillMarker(string fillId, string productId, string side, decimal qty, decimal price, decimal notional, decimal fee, DateTime atUtc)
        {
            if (!string.IsNullOrWhiteSpace(fillId))
            {
                return "coinbase_fill_id:" + fillId.Trim();
            }

            var ts = atUtc == DateTime.MinValue ? "0" : atUtc.ToUniversalTime().Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var parts = string.Join("|", new[]
            {
                string.IsNullOrWhiteSpace(productId) ? "UNKNOWN" : productId.Trim(),
                string.IsNullOrWhiteSpace(side) ? "UNKNOWN" : side.Trim().ToUpperInvariant(),
                qty.ToString(System.Globalization.CultureInfo.InvariantCulture),
                price.ToString(System.Globalization.CultureInfo.InvariantCulture),
                notional.ToString(System.Globalization.CultureInfo.InvariantCulture),
                fee.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ts
            });

            return "coinbase_fill_fp:" + parts;
        }

        private bool IsCoinbaseFillMarker(string note)
        {
            if (string.IsNullOrWhiteSpace(note))
            {
                return false;
            }

            return note.StartsWith("coinbase_fill_id:", StringComparison.OrdinalIgnoreCase)
                || note.StartsWith("coinbase_fill_fp:", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsRejectedOrCanceledFillStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return false;
            }

            var normalized = status.Trim().ToUpperInvariant().Replace('-', '_').Replace(' ', '_');
            return normalized.Contains("REJECT")
                || normalized.Contains("FAIL")
                || normalized.Contains("CANCEL")
                || normalized == "INVALID"
                || normalized == "DENIED"
                || normalized == "EXPIRED";
        }

        private string NormalizeProductId(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                return "UNKNOWN";
            }

            return productId.Trim().Replace("/", "-").ToUpperInvariant();
        }

        private DateTime ReadDateTime(Dictionary<string, object> row, string key)
        {
            object rawObject;
            if (!TryGetObjectValue(row, key, out rawObject) || rawObject == null)
            {
                return DateTime.MinValue;
            }

            var raw = rawObject.ToString();
            if (string.IsNullOrWhiteSpace(raw)) return DateTime.MinValue;

            long unix;
            if (long.TryParse(raw, out unix))
            {
                if (unix >= 1000000000000L)
                {
                    try
                    {
                        return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(unix);
                    }
                    catch
                    {
                        return DateTime.MinValue;
                    }
                }
                if (unix >= 1000000000L)
                {
                    try
                    {
                        return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unix);
                    }
                    catch
                    {
                        return DateTime.MinValue;
                    }
                }
            }

            DateTime parsed;
            return DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out parsed)
                ? parsed
                : DateTime.MinValue;
        }

        private bool TryGetObjectValue(Dictionary<string, object> row, string key, out object value)
        {
            value = null;
            if (row == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            if (row.ContainsKey(key))
            {
                value = row[key];
                return value != null;
            }

            foreach (var pair in row)
            {
                if (pair.Key == null) continue;
                if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = pair.Value;
                    return value != null;
                }
            }

            return false;
        }

        private sealed class TradeImportStats
        {
            public int TotalFills;
            public int ImportedFills;
            public decimal TotalFees;
            public decimal NetProfitEstimate;
        }

        private sealed class QuoteScopedTotal
        {
            public string QuoteCurrency;
            public decimal AmountInQuote;
            public int ExcludedCount;
        }
    }
}
