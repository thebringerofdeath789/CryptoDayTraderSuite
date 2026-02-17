using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Models.AI;
using CryptoDayTraderSuite.Services.Messaging;
using CryptoDayTraderSuite.Services.Messaging.Events;
using CryptoDayTraderSuite.Strategy;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Services
{
	public class AIGovernor
	{
		private sealed class ParsedBiasResult
		{
			public MarketBias Bias { get; set; }

			public string SourceLabel { get; set; }
		}

		private const int GovernorPromptRecentBars = 16;

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
			if (_sidecar != null)
			{
				_sidecar.StatusChanged += OnSidecarStatus;
			}
		}

		private void OnSidecarStatus(SidecarStatus status)
		{
			if (_running)
			{
				this.StatusChanged?.Invoke(status.ToString());
			}
		}

		public void Start()
		{
			if (!_running)
			{
				_running = true;
				Log("AI Governor Started.");
				this.StatusChanged?.Invoke("Starting...");
				Task.Run((Func<Task>)LoopAsync);
			}
		}

		public void Stop()
		{
			_running = false;
			Log("AI Governor Stopped.");
			this.StatusChanged?.Invoke("Stopped");
		}

		private async Task LoopAsync()
		{
			await Task.Delay(5000);
			while (_running)
			{
				try
				{
					if (_sidecar != null && _sidecar.IsConnected)
					{
						this.StatusChanged?.Invoke("Analyzing...");
						await RunCycleAsync();
						this.StatusChanged?.Invoke("Idle (Next: 15m)");
					}
					else
					{
						this.StatusChanged?.Invoke("Offline (Waiting for Browser)");
						bool stillOffline = _sidecar == null || !_sidecar.IsConnected;
						if (_sidecar != null && DateTime.UtcNow - _lastReconnectAttemptUtc >= TimeSpan.FromSeconds(30.0))
						{
							_lastReconnectAttemptUtc = DateTime.UtcNow;
							this.StatusChanged?.Invoke("Reconnecting...");
							if (await _sidecar.ConnectAsync())
							{
								this.StatusChanged?.Invoke("Connected");
								stillOffline = false;
							}
						}
						else
						{
							stillOffline = _sidecar == null || !_sidecar.IsConnected;
						}
						if (stillOffline && _engine.GlobalBias != MarketBias.Neutral)
						{
							_engine.GlobalBias = MarketBias.Neutral;
							Log("AI Governor Disconnected - Reverting Market Bias to NEUTRAL (Fail Safe)");
							this.BiasUpdated?.Invoke(MarketBias.Neutral, "AI Offline");
						}
					}
				}
				catch (Exception ex)
				{
					Log("AI Governor Error: " + ex.Message);
					this.StatusChanged?.Invoke("Error");
				}
				int waitSeconds = ((_sidecar != null && _sidecar.IsConnected) ? 900 : 30);
				for (int i = 0; i < waitSeconds; i++)
				{
					if (!_running)
					{
						break;
					}
					await Task.Delay(1000);
				}
			}
		}

		private async Task RunCycleAsync()
		{
			Log("Starting analysis cycle...");
			IExchangeClient client = _provider.CreatePublicClient("Coinbase");
			DateTime end = DateTime.UtcNow;
			DateTime start = end.AddHours(-4.0);
			List<Candle> candles = await client.GetCandlesAsync("BTC-USD", 15, start, end);
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
			List<Candle> recentBars = candles.Skip(Math.Max(0, candles.Count - 16)).ToList();
			Candle latest = candles.Last();
			Candle firstInWindow = candles.First();
			decimal range = ((firstInWindow.Close != 0m) ? ((latest.Close - firstInWindow.Close) / firstInWindow.Close * 100m) : 0m);
			decimal rsi = Indicators.RSI(candles, 14);
			decimal atr = Indicators.ATR(candles, 14);
			decimal vwap = Indicators.VWAP(candles);
			var windowSummaries = new int[3] { 4, 8, 16 }.Where((int w) => candles.Count >= w).Select(delegate(int w)
			{
				List<Candle> list = candles.Skip(candles.Count - w).ToList();
				Candle candle = list.First();
				Candle candle2 = list.Last();
				decimal changePct = ((candle.Close != 0m) ? ((candle2.Close - candle.Close) / candle.Close * 100m) : 0m);
				return new
				{
					bars = w,
					fromUtc = candle.Time.ToString("o"),
					toUtc = candle2.Time.ToString("o"),
					changePct = changePct,
					high = list.Max((Candle c) => c.High),
					low = list.Min((Candle c) => c.Low),
					avgVolume = ((list.Count > 0) ? list.Average((Candle c) => c.Volume) : 0m),
					rsi14 = Indicators.RSI(list, 14),
					atr14 = Indicators.ATR(list, 14),
					vwap = Indicators.VWAP(list)
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
				recentStructure = recentBars.Select((Candle c) => new
				{
					timeUtc = c.Time.ToString("o"),
					open = c.Open,
					high = c.High,
					low = c.Low,
					close = c.Close,
					volume = c.Volume
				}).ToList()
			};
			string json = UtilCompat.JsonSerialize(ctx);
			string prompt = "You are a deterministic crypto regime classifier. Analyze the BTC market snapshot below and classify directional bias. Use only the provided data. Return ONLY valid JSON (no markdown, no prose, no code fences). Schema: {\"bias\":\"Bullish\"|\"Bearish\"|\"Neutral\",\"reason\":\"one short sentence\",\"confidence\":0.0-1.0}. If evidence conflicts or is weak, set bias to Neutral with confidence <= 0.55. JSON Data: " + json;
			List<ChromeSidecar.SidecarAiResponse> responses = await _sidecar.QueryAcrossAvailableServicesAsync(prompt);
			if (responses == null || responses.Count == 0)
			{
				string singleRaw = await _sidecar.QueryAIAsync(prompt);
				if (!string.IsNullOrWhiteSpace(singleRaw))
				{
					responses = new List<ChromeSidecar.SidecarAiResponse>
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
			List<ParsedBiasResult> parsed = new List<ParsedBiasResult>();
			foreach (ChromeSidecar.SidecarAiResponse response in responses)
			{
				string raw = ((response == null) ? string.Empty : (response.Raw ?? string.Empty));
				string clean = raw.Replace("```json", "").Replace("```", "").Trim();
				if (!string.IsNullOrWhiteSpace(clean))
				{
					MarketBias? bias = ParseBiasFromResponse(clean);
					if (!bias.HasValue)
					{
						Log("AI response parse failed from " + ((response == null) ? "Unknown" : response.SourceLabel));
						continue;
					}
					parsed.Add(new ParsedBiasResult
					{
						Bias = bias.Value,
						SourceLabel = ((response == null) ? "Unknown" : response.SourceLabel)
					});
				}
			}
			if (parsed.Count == 0)
			{
				Log("No parseable bias returned by available AI services.");
				return;
			}
			MarketBias finalBias = ResolveConsensus(parsed);
			string reason = BuildSourceSummary(parsed, finalBias);
			if (_engine.GlobalBias != finalBias)
			{
				MarketBias old = _engine.GlobalBias;
				_engine.GlobalBias = finalBias;
				Log($"Market Bias changed from {old} to {finalBias}. {reason}");
				this.BiasUpdated?.Invoke(finalBias, reason);
			}
			else
			{
				Log($"Market Bias remains {finalBias}. {reason}");
				this.BiasUpdated?.Invoke(finalBias, reason);
			}
		}

		private MarketBias? ParseBiasFromResponse(string clean)
		{
			MarketBias? fromInlineJson = ParseBiasFromInlineJson(clean);
			if (fromInlineJson.HasValue)
			{
				return fromInlineJson;
			}
			AIResponse resp = null;
			try
			{
				resp = UtilCompat.JsonDeserialize<AIResponse>(clean);
			}
			catch
			{
				resp = null;
			}
			if (resp != null && Enum.TryParse<MarketBias>(resp.Bias, ignoreCase: true, out var parsedJson))
			{
				return parsedJson;
			}
			return ParseBiasFromLabeledText(clean) ?? ParseBiasFromText(clean);
		}

		private MarketBias? ParseBiasFromInlineJson(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			int start = text.IndexOf('{');
			if (start < 0)
			{
				return null;
			}
			int depth = 0;
			bool inString = false;
			bool escape = false;
			for (int i = start; i < text.Length; i++)
			{
				char ch = text[i];
				if (escape)
				{
					escape = false;
					continue;
				}
				switch (ch)
				{
				case '\\':
					escape = true;
					continue;
				case '"':
					inString = !inString;
					continue;
				}
				if (inString)
				{
					continue;
				}
				if (ch == '{')
				{
					depth++;
				}
				if (ch != '}')
				{
					continue;
				}
				depth--;
				if (depth != 0)
				{
					continue;
				}
				string candidate = text.Substring(start, i - start + 1);
				try
				{
					AIResponse parsed = UtilCompat.JsonDeserialize<AIResponse>(candidate);
					if (parsed != null && Enum.TryParse<MarketBias>(parsed.Bias, ignoreCase: true, out var bias))
					{
						return bias;
					}
				}
				catch
				{
				}
				start = text.IndexOf('{', start + 1);
				if (start < 0)
				{
					return null;
				}
				i = start - 1;
				depth = 0;
				inString = false;
				escape = false;
			}
			return null;
		}

		private MarketBias? ParseBiasFromLabeledText(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			string t = text.ToLowerInvariant();
			int labelIdx = t.IndexOf("bias");
			if (labelIdx < 0)
			{
				return null;
			}
			string probe = t.Substring(labelIdx, Math.Min(80, t.Length - labelIdx));
			if (probe.Contains("bullish"))
			{
				return MarketBias.Bullish;
			}
			if (probe.Contains("bearish"))
			{
				return MarketBias.Bearish;
			}
			if (probe.Contains("neutral"))
			{
				return MarketBias.Neutral;
			}
			return null;
		}

		private MarketBias ResolveConsensus(List<ParsedBiasResult> results)
		{
			int bullish = results.Count((ParsedBiasResult r) => r.Bias == MarketBias.Bullish);
			int bearish = results.Count((ParsedBiasResult r) => r.Bias == MarketBias.Bearish);
			int neutral = results.Count((ParsedBiasResult r) => r.Bias == MarketBias.Neutral);
			int max = Math.Max(bullish, Math.Max(bearish, neutral));
			int leaders = 0;
			if (bullish == max)
			{
				leaders++;
			}
			if (bearish == max)
			{
				leaders++;
			}
			if (neutral == max)
			{
				leaders++;
			}
			if (leaders > 1)
			{
				return MarketBias.Neutral;
			}
			if (bullish == max)
			{
				return MarketBias.Bullish;
			}
			if (bearish == max)
			{
				return MarketBias.Bearish;
			}
			return MarketBias.Neutral;
		}

		private string BuildSourceSummary(List<ParsedBiasResult> results, MarketBias finalBias)
		{
			string labels = string.Join("; ", results.Select((ParsedBiasResult r) => r.SourceLabel + "=" + r.Bias));
			if (results.Count == 1)
			{
				return "Source: " + results[0].SourceLabel;
			}
			int voteCount = results.Count((ParsedBiasResult r) => r.Bias == finalBias);
			return "Sources: " + labels + " | Consensus: " + finalBias.ToString() + " (" + voteCount + "/" + results.Count + ")";
		}

		private MarketBias? ParseBiasFromText(string text)
		{
			string t = (text ?? string.Empty).ToLowerInvariant();
			if (IsClarificationOrAmbiguousResponse(t))
			{
				return null;
			}
			bool bearish = t.Contains("bearish");
			bool bullish = t.Contains("bullish");
			bool neutral = t.Contains("neutral");
			int hits = 0;
			if (bearish)
			{
				hits++;
			}
			if (bullish)
			{
				hits++;
			}
			if (neutral)
			{
				hits++;
			}
			if (hits == 0)
			{
				return null;
			}
			if (hits > 1)
			{
				return null;
			}
			if (bearish)
			{
				return MarketBias.Bearish;
			}
			if (bullish)
			{
				return MarketBias.Bullish;
			}
			if (neutral)
			{
				return MarketBias.Neutral;
			}
			return null;
		}

		private bool IsClarificationOrAmbiguousResponse(string t)
		{
			if (string.IsNullOrWhiteSpace(t))
			{
				return true;
			}
			bool hasQuestionMark = t.Contains("?");
			if (t.Contains("which response") || t.Contains("which one") || t.Contains("which is preferred") || t.Contains("preferred response") || t.Contains("do you prefer") || t.Contains("please choose") || t.Contains("can you clarify") || t.Contains("clarify") || t.Contains("need more context"))
			{
				return true;
			}
			if (hasQuestionMark && (t.Contains("bullish") || t.Contains("bearish") || t.Contains("neutral")))
			{
				return true;
			}
			bool containsBiasLabel = t.Contains("\"bias\"") || t.Contains("bias:") || t.Contains("market bias");
			bool bearish = t.Contains("bearish");
			bool bullish = t.Contains("bullish");
			bool neutral = t.Contains("neutral");
			int hits = (bearish ? 1 : 0) + (bullish ? 1 : 0) + (neutral ? 1 : 0);
			if (hits > 1 && !containsBiasLabel)
			{
				return true;
			}
			return false;
		}

		private void Log(string msg)
		{
			CryptoDayTraderSuite.Util.Log.Info("[AIGovernor] " + msg, "Log", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AIGovernor.cs", 500);
			_bus?.Publish(new LogEvent(msg));
		}
	}
}
