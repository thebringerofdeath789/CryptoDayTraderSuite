using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Models.AI;
using CryptoDayTraderSuite.Strategy;
using CryptoDayTraderSuite.Util;
using CryptoDayTraderSuite.Services.Messaging;
using CryptoDayTraderSuite.Services.Messaging.Events;

namespace CryptoDayTraderSuite.Services
{
    public class AIGovernor
    {
        private const int GovernorPromptRecentBars = 16;
        private const string JsonStartMarker = "CDTS_JSON_START";
        private const string JsonEndMarker = "CDTS_JSON_END";

        private sealed class ParsedBiasResult
        {
            public MarketBias Bias { get; set; }
            public string SourceLabel { get; set; }
        }

        private sealed class BiasParseOutcome
        {
            public MarketBias Bias { get; set; }
            public bool ContractInvalid { get; set; }
        }

        private readonly ChromeSidecar _sidecar;
        private readonly StrategyEngine _engine;
        private readonly IExchangeProvider _provider;
        private readonly IEventBus _bus;
        private bool _running;
        private DateTime _lastReconnectAttemptUtc = DateTime.MinValue;

        public event Action<MarketBias, string> BiasUpdated;
        public event Action<string> StatusChanged;

        public AIGovernor(ChromeSidecar sidecar, StrategyEngine engine, IExchangeProvider provider, IEventBus bus)
        {
            _sidecar = sidecar;
            _engine = engine;
            _provider = provider;
            _bus = bus;

            // Subscribe to sidecar status updates
            if (_sidecar != null)
            {
                _sidecar.StatusChanged += OnSidecarStatus;
            }
        }

        private void OnSidecarStatus(SidecarStatus status)
        {
            // Only propagate sidecar status if the governor is actually trying to run
            if (_running)
            {
                StatusChanged?.Invoke(status.ToString());
            }
        }

        public void Start()
        {
            if (_running) return;
            _running = true;
            
            Log("AI Governor Started.");
            StatusChanged?.Invoke("Starting...");
            
            Task.Run(LoopAsync);
        }

        public void Stop()
        {
            _running = false;
            Log("AI Governor Stopped.");
            StatusChanged?.Invoke("Stopped");
        }

        private async Task LoopAsync()
        {
            // Initial grace period
            await Task.Delay(5000); 

            while (_running)
            {
                try
                {
                    if (_sidecar != null && _sidecar.IsConnected)
                    {
                        StatusChanged?.Invoke("Analyzing...");
                        await RunCycleAsync();
                        StatusChanged?.Invoke("Idle (Next: 15m)");
                    }
                    else
                    {
                        StatusChanged?.Invoke("Offline (Waiting for Browser)");

                        var stillOffline = _sidecar == null || !_sidecar.IsConnected;
                        if (_sidecar != null && (DateTime.UtcNow - _lastReconnectAttemptUtc) >= TimeSpan.FromSeconds(30))
                        {
                            _lastReconnectAttemptUtc = DateTime.UtcNow;
                            StatusChanged?.Invoke("Reconnecting...");
                            var connected = await _sidecar.ConnectAsync();
                            if (connected)
                            {
                                StatusChanged?.Invoke("Connected");
                                stillOffline = false;
                            }
                        }
                        else
                        {
                            stillOffline = _sidecar == null || !_sidecar.IsConnected;
                        }
                        
                        // Fail Safe: If AI is offline, ensure we don't stick to a stale bias.
                        if (stillOffline && _engine.GlobalBias != MarketBias.Neutral)
                        {
                            _engine.GlobalBias = MarketBias.Neutral;
                            Log("AI Governor Disconnected - Reverting Market Bias to NEUTRAL (Fail Safe)");
                            BiasUpdated?.Invoke(MarketBias.Neutral, "AI Offline");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"AI Governor Error: {ex.Message}");
                    StatusChanged?.Invoke("Error");
                }

                // Poll interval: 15 minutes when connected, 30s while offline
                int waitSeconds = (_sidecar != null && _sidecar.IsConnected) ? 900 : 30;
                for (int i = 0; i < waitSeconds; i++) 
                {
                    if (!_running) break;
                    await Task.Delay(1000);
                }
            }
        }

        private async Task RunCycleAsync()
        {
            Log("Starting analysis cycle...");
            // 1. Fetch Market Context (BTC)
            var client = _provider.CreatePublicClient("Coinbase");
            var end = DateTime.UtcNow;
            var start = end.AddHours(-4);
            var candles = await client.GetCandlesAsync("BTC-USD", 15, start, end); // 15m candles for H4 context

            if (candles == null)
            {
                Log("Market data unavailable: candles response is null.");
                return;
            }

            if (candles.Count < 10)
            {
                Log($"Market data insufficient: received {candles.Count} candles for BTC-USD 15m.");
                return;
            }

            Log($"Market data received: {candles.Count} candles.");

            // 2. Build Context
            var recentBars = candles.Skip(Math.Max(0, candles.Count - GovernorPromptRecentBars)).ToList();
            var latest = candles.Last();
            var firstInWindow = candles.First();
            var range = firstInWindow.Close != 0m ? ((latest.Close - firstInWindow.Close) / firstInWindow.Close) * 100m : 0m;
            var rsi = Indicators.RSI(candles, 14);
            var atr = Indicators.ATR(candles, 14);
            var vwap = Indicators.VWAP(candles);
            var windowSummaries = new[] { 4, 8, 16 }
                .Where(w => candles.Count >= w)
                .Select(w =>
                {
                    var slice = candles.Skip(candles.Count - w).ToList();
                    var sFirst = slice.First();
                    var sLast = slice.Last();
                    var changePct = sFirst.Close != 0m ? ((sLast.Close - sFirst.Close) / sFirst.Close) * 100m : 0m;
                    return new
                    {
                        bars = w,
                        fromUtc = sFirst.Time.ToString("o"),
                        toUtc = sLast.Time.ToString("o"),
                        changePct = changePct,
                        high = slice.Max(c => c.High),
                        low = slice.Min(c => c.Low),
                        avgVolume = slice.Count > 0 ? slice.Average(c => c.Volume) : 0m,
                        rsi14 = Indicators.RSI(slice, 14),
                        atr14 = Indicators.ATR(slice, 14),
                        vwap = Indicators.VWAP(slice)
                    };
                }).ToList();

            var ctx = new
            {
                symbol = "BTC-USD",
                interval = "15m",
                analysisWindow = "4h",
                timestampUtc = end.ToString("o"),
                currentPrice = latest.Close,
                vwap = vwap,
                rsi14 = rsi,
                atr14 = atr,
                rangePctWindow = range,
                barsCount = candles.Count,
                windowSummaries = windowSummaries,
                recentStructure = recentBars.Select(c => new
                {
                    timeUtc = c.Time.ToString("o"),
                    open = c.Open,
                    high = c.High,
                    low = c.Low,
                    close = c.Close,
                    volume = c.Volume
                }).ToList()
            };

            // 3. Prompt
            var json = UtilCompat.JsonSerialize(ctx);
            var prompt =
                "You are a deterministic crypto regime classifier. Analyze the BTC market snapshot below and classify directional bias. "
                + "Use only the provided data. Return ONLY valid JSON (no markdown, no prose, no code fences). "
                + "Schema: {\"bias\":\"Bullish\"|\"Bearish\"|\"Neutral\",\"reason\":\"one short sentence\",\"confidence\":0.0-1.0}. "
                + "Return exactly one top-level JSON object with exactly these keys: bias, reason, confidence. "
                + "Do NOT wrap in result/data/response/output/payload keys, and do NOT return an array. "
                + "If evidence conflicts or is weak, set bias to Neutral with confidence <= 0.55. "
                + "Wrap your final JSON exactly as: " + JsonStartMarker + "{...}" + JsonEndMarker + ". "
                + "JSON Data: " + json;

            // 4. Query all available providers and aggregate.
            var responses = await _sidecar.QueryAcrossAvailableServicesAsync(prompt);
            if (responses == null || responses.Count == 0)
            {
                var singleRaw = await _sidecar.QueryAIAsync(prompt);
                if (!string.IsNullOrWhiteSpace(singleRaw))
                {
                    responses = new System.Collections.Generic.List<ChromeSidecar.SidecarAiResponse>
                    {
                        new ChromeSidecar.SidecarAiResponse
                        {
                            Raw = singleRaw,
                            Service = _sidecar.CurrentServiceName,
                            Model = _sidecar.CurrentModelName,
                            SourceLabel = _sidecar.CurrentSourceLabel
                        }
                    };
                }
            }

            if (responses == null || responses.Count == 0)
            {
                Log("AI response empty.");
                return;
            }

            var parsed = new System.Collections.Generic.List<ParsedBiasResult>();
            foreach (var response in responses)
            {
                var raw = response == null ? string.Empty : (response.Raw ?? string.Empty);
                var clean = NormalizeAiResponseText(raw);
                if (string.IsNullOrWhiteSpace(clean)) continue;

                var outcome = ParseBiasFromResponse(clean);
                if (outcome.ContractInvalid)
                {
                    Log("AI response invalid contract from " + (response == null ? "Unknown" : response.SourceLabel) + "; defaulting vote to Neutral.");
                }

                parsed.Add(new ParsedBiasResult
                {
                    Bias = outcome.Bias,
                    SourceLabel = response == null ? "Unknown" : response.SourceLabel
                });
            }

            if (parsed.Count == 0)
            {
                var repairSchema = "{\"bias\":\"Bullish\"|\"Bearish\"|\"Neutral\",\"reason\":\"one short sentence\",\"confidence\":0.0-1.0}";
                var retryRaw = await QueryStrictJsonRepairAsync(repairSchema, string.Join("\n", responses.Where(r => r != null).Select(r => NormalizeAiResponseText(r.Raw ?? string.Empty))));
                if (!string.IsNullOrWhiteSpace(retryRaw))
                {
                    var retryOutcome = ParseBiasFromResponse(NormalizeAiResponseText(retryRaw));
                    parsed.Add(new ParsedBiasResult
                    {
                        Bias = retryOutcome.Bias,
                        SourceLabel = "Retry"
                    });
                }
            }

            if (parsed.Count == 0)
            {
                Log("No parseable bias returned by available AI services.");
                return;
            }

            var finalBias = ResolveConsensus(parsed);
            var reason = BuildSourceSummary(parsed, finalBias);

            if (_engine.GlobalBias != finalBias)
            {
                var old = _engine.GlobalBias;
                _engine.GlobalBias = finalBias;
                Log($"Market Bias changed from {old} to {finalBias}. {reason}");
                BiasUpdated?.Invoke(finalBias, reason);
            }
            else
            {
                Log($"Market Bias remains {finalBias}. {reason}");
                BiasUpdated?.Invoke(finalBias, reason);
            }
        }

        private BiasParseOutcome ParseBiasFromResponse(string clean)
        {
            var outcome = new BiasParseOutcome { Bias = MarketBias.Neutral, ContractInvalid = false };

            if (string.IsNullOrWhiteSpace(clean))
            {
                outcome.ContractInvalid = true;
                return outcome;
            }

            foreach (var candidate in EnumerateJsonCandidates(clean))
            {
                if (TryParseBiasFromFlexibleContract(candidate, out var flexBias))
                {
                    outcome.Bias = flexBias;
                    return outcome;
                }

                AIResponse resp = null;
                try
                {
                    resp = UtilCompat.JsonDeserialize<AIResponse>(candidate);
                }
                catch
                {
                    resp = null;
                }

                if (resp != null)
                {
                    if (TryParseBiasValue(resp.Bias, out var parsedJson))
                    {
                        outcome.Bias = parsedJson;
                        return outcome;
                    }
                }
            }

            var fromInlineJson = ParseBiasFromInlineJson(clean);
            if (fromInlineJson.HasValue)
            {
                outcome.Bias = fromInlineJson.Value;
                return outcome;
            }

            var fromLabel = ParseBiasFromLabeledText(clean);
            if (fromLabel.HasValue)
            {
                outcome.Bias = fromLabel.Value;
                return outcome;
            }

            var fromText = ParseBiasFromText(clean);
            if (fromText.HasValue)
            {
                outcome.Bias = fromText.Value;
                return outcome;
            }

            outcome.ContractInvalid = true;
            return outcome;
        }

        private bool TryParseBiasFromFlexibleContract(string candidate, out MarketBias bias)
        {
            bias = MarketBias.Neutral;
            if (string.IsNullOrWhiteSpace(candidate)) return false;

            object root;
            try
            {
                root = UtilCompat.JsonDeserialize<object>(candidate);
            }
            catch
            {
                return false;
            }

            var dict = TryExtractDictionaryCandidate(root,
                "result", "analysis", "data", "answer", "response", "output", "payload");
            if (dict == null) return false;

            if (TryGetString(dict, out var biasText, "bias", "marketBias", "regime", "direction", "classification")
                && TryParseBiasValue(biasText, out bias))
            {
                return true;
            }

            if (TryGetString(dict, out var verdictText, "verdict", "decision")
                && TryParseBiasValue(verdictText, out bias))
            {
                return true;
            }

            return false;
        }

        private bool TryParseBiasValue(string raw, out MarketBias bias)
        {
            bias = MarketBias.Neutral;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            if (Enum.TryParse(raw, true, out bias)) return true;

            var t = raw.ToLowerInvariant();
            var hasBearish = t.Contains("bearish") || t.Contains("short bias") || t.Contains("short");
            var hasBullish = t.Contains("bullish") || t.Contains("long bias") || t.Contains("long");
            var hasNeutral = t.Contains("neutral") || t.Contains("range") || t.Contains("sideways") || t.Contains("flat");

            var hits = (hasBearish ? 1 : 0) + (hasBullish ? 1 : 0) + (hasNeutral ? 1 : 0);
            if (hits != 1) return false;

            if (hasBearish) bias = MarketBias.Bearish;
            else if (hasBullish) bias = MarketBias.Bullish;
            else bias = MarketBias.Neutral;

            return true;
        }

        private Dictionary<string, object> TryExtractDictionaryCandidate(object root, params string[] nestedKeys)
        {
            if (root == null) return null;

            if (root is Dictionary<string, object> rootDict)
            {
                foreach (var key in nestedKeys)
                {
                    if (TryGetValue(rootDict, key, out var nested))
                    {
                        if (nested is Dictionary<string, object> nestedDict)
                        {
                            return nestedDict;
                        }

                        if (nested is ArrayList nestedArray && nestedArray.Count > 0 && nestedArray[0] is Dictionary<string, object> nestedArrayDict)
                        {
                            return nestedArrayDict;
                        }
                    }
                }

                return rootDict;
            }

            if (root is ArrayList array && array.Count > 0 && array[0] is Dictionary<string, object> arrDict)
            {
                return arrDict;
            }

            return null;
        }

        private bool TryGetValue(Dictionary<string, object> dict, string key, out object value)
        {
            value = null;
            if (dict == null || string.IsNullOrWhiteSpace(key)) return false;

            foreach (var kv in dict)
            {
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = kv.Value;
                    return true;
                }
            }

            return false;
        }

        private bool TryGetString(Dictionary<string, object> dict, out string value, params string[] keys)
        {
            value = null;
            if (dict == null || keys == null) return false;

            foreach (var key in keys)
            {
                if (TryGetValue(dict, key, out var raw) && raw != null)
                {
                    value = Convert.ToString(raw).Trim();
                    if (!string.IsNullOrWhiteSpace(value)) return true;
                }
            }

            return false;
        }

        private MarketBias? ParseBiasFromInlineJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            foreach (var candidate in EnumerateJsonCandidates(text))
            {
                try
                {
                    var parsed = UtilCompat.JsonDeserialize<AIResponse>(candidate);
                    if (parsed != null)
                    {
                                MarketBias bias;
                                if (TryParseBiasValue(parsed.Bias, out bias))
                        {
                            return bias;
                        }
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private string NormalizeAiResponseText(string text)
        {
            var marked = TryExtractMarkedJson(text);
            if (!string.IsNullOrWhiteSpace(marked)) return marked.Trim();

            var clean = (text ?? string.Empty)
                .Replace("```json", string.Empty)
                .Replace("```JSON", string.Empty)
                .Replace("```", string.Empty)
                .Trim();

            if (clean.StartsWith("<pre", StringComparison.OrdinalIgnoreCase))
            {
                var gt = clean.IndexOf('>');
                if (gt >= 0 && gt < clean.Length - 1)
                {
                    clean = clean.Substring(gt + 1).Trim();
                }
            }

            if (clean.EndsWith("</pre>", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(0, clean.Length - "</pre>".Length).Trim();
            }

            return clean;
        }

        private string TryExtractMarkedJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var start = text.LastIndexOf(JsonStartMarker, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return string.Empty;
            start += JsonStartMarker.Length;

            var end = text.IndexOf(JsonEndMarker, start, StringComparison.OrdinalIgnoreCase);
            if (end < 0 || end <= start) return string.Empty;

            return text.Substring(start, end - start).Trim();
        }

        private System.Collections.Generic.IEnumerable<string> EnumerateJsonCandidates(string text)
        {
            var clean = NormalizeAiResponseText(text);
            if (string.IsNullOrWhiteSpace(clean)) yield break;

            yield return clean;

            var objectJson = TryExtractFirstJsonObject(clean);
            if (!string.IsNullOrWhiteSpace(objectJson) && !string.Equals(objectJson, clean, StringComparison.Ordinal))
            {
                yield return objectJson;
            }

            var arrayJson = TryExtractFirstJsonArray(clean);
            if (!string.IsNullOrWhiteSpace(arrayJson) && !string.Equals(arrayJson, clean, StringComparison.Ordinal))
            {
                yield return arrayJson;
            }

            var unwrapped = TryUnwrapQuotedJson(clean);
            if (!string.IsNullOrWhiteSpace(unwrapped) && !string.Equals(unwrapped, clean, StringComparison.Ordinal))
            {
                yield return unwrapped;

                var unwrappedObj = TryExtractFirstJsonObject(unwrapped);
                if (!string.IsNullOrWhiteSpace(unwrappedObj) && !string.Equals(unwrappedObj, unwrapped, StringComparison.Ordinal))
                {
                    yield return unwrappedObj;
                }

                var unwrappedArr = TryExtractFirstJsonArray(unwrapped);
                if (!string.IsNullOrWhiteSpace(unwrappedArr) && !string.Equals(unwrappedArr, unwrapped, StringComparison.Ordinal))
                {
                    yield return unwrappedArr;
                }
            }
        }

        private string TryExtractFirstJsonObject(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var start = text.IndexOf('{');
            if (start < 0) return string.Empty;

            var depth = 0;
            var inString = false;
            var escape = false;

            for (int i = start; i < text.Length; i++)
            {
                var ch = text[i];
                if (escape)
                {
                    escape = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escape = true;
                    continue;
                }

                if (ch == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (inString) continue;

                if (ch == '{') depth++;
                if (ch == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return text.Substring(start, i - start + 1);
                    }
                }
            }

            return string.Empty;
        }

        private string TryExtractFirstJsonArray(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var start = text.IndexOf('[');
            if (start < 0) return string.Empty;

            var depth = 0;
            var inString = false;
            var escape = false;

            for (int i = start; i < text.Length; i++)
            {
                var ch = text[i];
                if (escape)
                {
                    escape = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escape = true;
                    continue;
                }

                if (ch == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (inString) continue;

                if (ch == '[') depth++;
                if (ch == ']')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return text.Substring(start, i - start + 1);
                    }
                }
            }

            return string.Empty;
        }

        private string TryUnwrapQuotedJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var trimmed = text.Trim();
            if (trimmed.Length < 2) return string.Empty;
            if (trimmed[0] != '"' || trimmed[trimmed.Length - 1] != '"') return string.Empty;

            try
            {
                return UtilCompat.JsonDeserialize<string>(trimmed) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task<string> QueryStrictJsonRepairAsync(string schema, string previousResponse)
        {
            if (_sidecar == null || !_sidecar.IsConnected) return string.Empty;

            var previous = (previousResponse ?? string.Empty).Trim();
            if (previous.Length > 1200) previous = previous.Substring(0, 1200);

            var prompt = "Your last response was not valid for parser consumption. Return ONLY strict JSON with no markdown/prose/code fences. "
                + "Schema: " + schema + ". "
                + "Return exactly one top-level JSON object using only the schema keys. "
                + "Do NOT return arrays and do NOT wrap under result/data/response/output/payload. "
                + "Wrap your final JSON exactly as: " + JsonStartMarker + "{...}" + JsonEndMarker + ". "
                + "Do not wrap the JSON in quotes. Use plain object JSON only. "
                + "Previous response to fix: " + previous;

            try
            {
                return await _sidecar.QueryAIAsync(prompt);
            }
            catch (Exception ex)
            {
                Log("AI strict-json retry failed: " + ex.Message);
                return string.Empty;
            }
        }

        private MarketBias? ParseBiasFromLabeledText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var t = text.ToLowerInvariant();

            var labelIdx = t.IndexOf("bias");
            if (labelIdx < 0) return null;

            var probe = t.Substring(labelIdx, Math.Min(80, t.Length - labelIdx));
            if (probe.Contains("bullish")) return MarketBias.Bullish;
            if (probe.Contains("bearish")) return MarketBias.Bearish;
            if (probe.Contains("neutral")) return MarketBias.Neutral;
            return null;
        }

        private MarketBias ResolveConsensus(System.Collections.Generic.List<ParsedBiasResult> results)
        {
            var bullish = results.Count(r => r.Bias == MarketBias.Bullish);
            var bearish = results.Count(r => r.Bias == MarketBias.Bearish);
            var neutral = results.Count(r => r.Bias == MarketBias.Neutral);

            var max = Math.Max(bullish, Math.Max(bearish, neutral));
            var leaders = 0;
            if (bullish == max) leaders++;
            if (bearish == max) leaders++;
            if (neutral == max) leaders++;

            if (leaders > 1) return MarketBias.Neutral;
            if (bullish == max) return MarketBias.Bullish;
            if (bearish == max) return MarketBias.Bearish;
            return MarketBias.Neutral;
        }

        private string BuildSourceSummary(System.Collections.Generic.List<ParsedBiasResult> results, MarketBias finalBias)
        {
            var labels = string.Join("; ", results.Select(r => r.SourceLabel + "=" + r.Bias));
            if (results.Count == 1)
            {
                return "Source: " + results[0].SourceLabel;
            }

            var voteCount = results.Count(r => r.Bias == finalBias);
            return "Sources: " + labels + " | Consensus: " + finalBias + " (" + voteCount + "/" + results.Count + ")";
        }

        private MarketBias? ParseBiasFromText(string text)
        {
            var t = NormalizeAiResponseText(text).ToLowerInvariant();

            if (IsClarificationOrAmbiguousResponse(t))
            {
                return null;
            }

            var bearish = t.Contains("bearish");
            var bullish = t.Contains("bullish");
            var neutral = t.Contains("neutral");

            var hits = 0;
            if (bearish) hits++;
            if (bullish) hits++;
            if (neutral) hits++;

            if (hits == 0) return null;
            if (hits > 1) return null;

            if (bearish) return MarketBias.Bearish;
            if (bullish) return MarketBias.Bullish;
            if (neutral) return MarketBias.Neutral;
            return null;
        }

        private bool IsClarificationOrAmbiguousResponse(string t)
        {
            if (string.IsNullOrWhiteSpace(t)) return true;

            var hasQuestionMark = t.Contains("?");
            var asksPreference =
                t.Contains("which response") ||
                t.Contains("which one") ||
                t.Contains("which is preferred") ||
                t.Contains("preferred response") ||
                t.Contains("do you prefer") ||
                t.Contains("please choose") ||
                t.Contains("can you clarify") ||
                t.Contains("clarify") ||
                t.Contains("need more context");

            if (asksPreference) return true;
            if (hasQuestionMark && (t.Contains("bullish") || t.Contains("bearish") || t.Contains("neutral"))) return true;

            var containsBiasLabel = t.Contains("\"bias\"") || t.Contains("bias:") || t.Contains("market bias");
            var bearish = t.Contains("bearish");
            var bullish = t.Contains("bullish");
            var neutral = t.Contains("neutral");
            var hits = (bearish ? 1 : 0) + (bullish ? 1 : 0) + (neutral ? 1 : 0);

            if (hits > 1 && !containsBiasLabel) return true;
            return false;
        }

        private void Log(string msg)
        {
            Util.Log.Info($"[AIGovernor] {msg}");
            _bus?.Publish(new LogEvent(msg));
        }
    }
}