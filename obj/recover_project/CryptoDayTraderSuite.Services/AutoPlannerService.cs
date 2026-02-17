using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Models.AI;
using CryptoDayTraderSuite.Strategy;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Services
{
	public class AutoPlannerService
	{
		private sealed class LiveSignalEvidence
		{
			public string Strategy { get; set; }

			public string Side { get; set; }

			public decimal Entry { get; set; }

			public decimal Stop { get; set; }

			public decimal Target { get; set; }

			public double Confidence { get; set; }

			public double Expectancy { get; set; }

			public double WinRate { get; set; }

			public int Samples { get; set; }
		}

		public sealed class ProposalDiagnostics
		{
			public List<TradePlan> Plans = new List<TradePlan>();

			public string ReasonCode;

			public string ReasonMessage;
		}

		private const decimal MinRiskDistance = 0.00000001m;

		private const int ReviewPromptRecentCandles = 48;

		private const int ProposerPromptRecentCandles = 48;

		private readonly IExchangeClient _client;

		private readonly IEnumerable<IStrategy> _strategies;

		private readonly ChromeSidecar _sidecar;

		private readonly StrategyEngine _engine;

		public AutoPlannerService(IExchangeClient client, IEnumerable<IStrategy> strategies, ChromeSidecar sidecar = null, StrategyEngine engine = null)
		{
			_client = client ?? throw new ArgumentNullException("client");
			_strategies = strategies ?? throw new ArgumentNullException("strategies");
			_sidecar = sidecar;
			_engine = engine;
		}

		public async Task<List<ProjectionRow>> ProjectAsync(string productId, int granMinutes, int lookbackMinutes, decimal takerRate, decimal makerRate)
		{
			DateTime end = DateTime.UtcNow;
			List<Candle> candles = (await GetCandlesSafeAsync(startUtc: end.AddMinutes(-Math.Max(lookbackMinutes, 60)), productId: productId, granMinutes: granMinutes, endUtc: end, scope: "Project")) ?? new List<Candle>();
			return await Task.Run(delegate
			{
				List<ProjectionRow> list = new List<ProjectionRow>();
				if (candles.Count < 40)
				{
					return list;
				}
				foreach (IStrategy current in _strategies)
				{
					int num = 0;
					int num2 = 0;
					decimal num3 = default(decimal);
					decimal num4 = default(decimal);
					decimal num5 = default(decimal);
					for (int i = 20; i < candles.Count - 1; i++)
					{
						StrategyResult signal = current.GetSignal(candles, i);
						if (signal.IsSignal)
						{
							num2++;
							decimal entryPrice = signal.EntryPrice;
							decimal stopLoss = signal.StopLoss;
							decimal close = candles[i + 1].Close;
							decimal num6 = default(decimal);
							decimal num7 = Math.Abs(entryPrice - stopLoss);
							if (num7 > 0.00000001m)
							{
								num6 = ((signal.Side != OrderSide.Buy) ? ((entryPrice - close) / num7) : ((close - entryPrice) / num7));
							}
							if (num6 > 0m)
							{
								num++;
								num4 += num6;
							}
							else
							{
								num5 += num6;
							}
							num3 += num6;
						}
					}
					if (num2 > 0)
					{
						int num8 = num2 - num;
						list.Add(new ProjectionRow
						{
							Strategy = current.Name,
							Symbol = productId,
							GranMinutes = granMinutes,
							Expectancy = (double)(num3 / (decimal)num2 - (takerRate + makerRate)),
							WinRate = (double)(100m * (decimal)num / (decimal)num2),
							AvgWin = ((num > 0) ? ((double)(num4 / (decimal)num)) : 0.0),
							AvgLoss = ((num8 > 0) ? ((double)(num5 / (decimal)num8)) : 0.0),
							SharpeApprox = 0.0,
							Samples = num2
						});
					}
				}
				return list.OrderByDescending((ProjectionRow r) => r.Expectancy).ToList();
			});
		}

		public async Task<List<TradePlan>> ProposeAsync(string accountId, string productId, int granMinutes, decimal equityUsd, decimal riskPct, List<ProjectionRow> rows)
		{
			ProposalDiagnostics diag = await ProposeWithDiagnosticsAsync(accountId, productId, granMinutes, equityUsd, riskPct, rows);
			return (diag != null) ? (diag.Plans ?? new List<TradePlan>()) : new List<TradePlan>();
		}

		public async Task<ProposalDiagnostics> ProposeWithDiagnosticsAsync(string accountId, string productId, int granMinutes, decimal equityUsd, decimal riskPct, List<ProjectionRow> rows)
		{
			List<ProjectionRow> candidateRows = (rows ?? new List<ProjectionRow>()).OrderByDescending((ProjectionRow r) => r.Expectancy).ToList();
			if (candidateRows.Count == 0)
			{
				return new ProposalDiagnostics
				{
					ReasonCode = "no-best-row",
					ReasonMessage = "No projection rows available."
				};
			}
			DateTime end = DateTime.UtcNow;
			DateTime start = end.Subtract(TimeSpan.FromMinutes(granMinutes * 100));
			List<Candle> candlesList = await GetCandlesSafeAsync(productId, granMinutes, start, end, "Propose");
			if (candlesList == null || candlesList.Count == 0)
			{
				return new ProposalDiagnostics
				{
					ReasonCode = "no-candles",
					ReasonMessage = "No candles returned for proposal."
				};
			}
			if (IsAiProposerEnabled() && _sidecar != null && _sidecar.IsConnected)
			{
				TradePlan aiPlan = await TryBuildVerifiedAiProposedPlanAsync(accountId, productId, granMinutes, equityUsd, riskPct, rows, candlesList);
				if (aiPlan != null)
				{
					return new ProposalDiagnostics
					{
						Plans = new List<TradePlan> { aiPlan },
						ReasonCode = "ok",
						ReasonMessage = "AI proposer returned verified plan."
					};
				}
				Log.Info("[AutoPlanner] AI proposer did not return a verified trade. Falling back to strategy-first proposal flow.", "ProposeWithDiagnosticsAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 170);
			}
			ProjectionRow selectedRow = null;
			IStrategy selectedStrategy = null;
			StrategyResult result = null;
			int strategyNotFoundCount = 0;
			int noSignalCount = 0;
			int biasBlockedCount = 0;
			foreach (ProjectionRow candidate in candidateRows)
			{
				if (candidate == null || string.IsNullOrWhiteSpace(candidate.Strategy))
				{
					continue;
				}
				IStrategy strategy = _strategies.FirstOrDefault((IStrategy s) => string.Equals(s.Name, candidate.Strategy, StringComparison.OrdinalIgnoreCase));
				if (strategy == null)
				{
					strategyNotFoundCount++;
					continue;
				}
				StrategyResult candidateSignal = strategy.GetSignal(candlesList);
				if (candidateSignal == null || !candidateSignal.IsSignal)
				{
					noSignalCount++;
					continue;
				}
				if (_engine != null)
				{
					if (_engine.GlobalBias == MarketBias.Bearish && candidateSignal.Side == OrderSide.Buy)
					{
						biasBlockedCount++;
						continue;
					}
					if (_engine.GlobalBias == MarketBias.Bullish && candidateSignal.Side == OrderSide.Sell)
					{
						biasBlockedCount++;
						continue;
					}
				}
				selectedRow = candidate;
				selectedStrategy = strategy;
				result = candidateSignal;
				break;
			}
			if (selectedStrategy == null || selectedRow == null || result == null)
			{
				if (strategyNotFoundCount == candidateRows.Count)
				{
					return new ProposalDiagnostics
					{
						ReasonCode = "strategy-not-found",
						ReasonMessage = "No matching strategy instances found for projection rows."
					};
				}
				if (biasBlockedCount > 0)
				{
					Log.Info("[AutoPlanner] All live signals were blocked by global bias.", "ProposeWithDiagnosticsAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 231);
					return new ProposalDiagnostics
					{
						ReasonCode = "bias-blocked",
						ReasonMessage = $"Bias blocked live signals (blocked={biasBlockedCount}, noSignal={noSignalCount})."
					};
				}
				Log.Info($"[AutoPlanner] No live strategy signal available (checked={candidateRows.Count}, noSignal={noSignalCount}, strategyMissing={strategyNotFoundCount}).", "ProposeWithDiagnosticsAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 239);
				return new ProposalDiagnostics
				{
					ReasonCode = "no-signal",
					ReasonMessage = $"No live signal from ranked strategies (checked={candidateRows.Count}, noSignal={noSignalCount})."
				};
			}
			string strategyName = selectedStrategy.Name;
			double selectedExpectancy = selectedRow.Expectancy;
			double selectedWinRate = selectedRow.WinRate;
			int selectedSamples = selectedRow.Samples;
			decimal riskDollars = equityUsd * (riskPct / 100m);
			decimal distance = Math.Abs(result.EntryPrice - result.StopLoss);
			decimal qty = default(decimal);
			if (distance > 0m)
			{
				qty = Math.Round(riskDollars / distance, 6);
			}
			TradePlan plan = new TradePlan
			{
				AccountId = (accountId ?? "sim-acct"),
				Symbol = productId.Replace("-", "/"),
				Strategy = strategyName,
				Direction = (int)result.Side,
				Entry = result.EntryPrice,
				Stop = result.StopLoss,
				Target = result.TakeProfit,
				Qty = qty,
				Note = $"AutoPlanner {strategyName} exp={selectedExpectancy:0.00} wr={selectedWinRate:0.0}%"
			};
			if (_sidecar != null && _sidecar.IsConnected)
			{
				try
				{
					Log.Info("[AutoPlanner] Starting AI review for generated trade.", "ProposeWithDiagnosticsAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 279);
					TradePreview preview = new TradePreview
					{
						Symbol = productId,
						Strategy = strategyName,
						Side = result.Side.ToString(),
						Entry = result.EntryPrice,
						Stop = result.StopLoss,
						Target = result.TakeProfit,
						Rationale = $"Expectancy {selectedExpectancy:0.00}, recent signals count {selectedSamples}"
					};
					Candle latest = candlesList.Last();
					Candle first = candlesList.First();
					decimal windowRangePct = ((first.Close != 0m) ? ((latest.Close - first.Close) / first.Close * 100m) : 0m);
					List<Candle> reviewRecent = candlesList.Skip(Math.Max(0, candlesList.Count - 48)).ToList();
					var reviewWindowSummaries = new int[3] { 12, 24, 48 }.Where((int w) => candlesList.Count >= w).Select(delegate(int w)
					{
						List<Candle> list = candlesList.Skip(candlesList.Count - w).ToList();
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
					var reviewPayload = new
					{
						trade = new
						{
							strategy = preview.Strategy,
							symbol = preview.Symbol,
							side = preview.Side,
							entry = preview.Entry,
							stop = preview.Stop,
							target = preview.Target,
							rationale = preview.Rationale,
							granularityMinutes = granMinutes
						},
						market = new
						{
							timestampUtc = DateTime.UtcNow.ToString("o"),
							currentPrice = latest.Close,
							vwap = Indicators.VWAP(candlesList),
							rsi14 = Indicators.RSI(candlesList, 14),
							atr14 = Indicators.ATR(candlesList, 14),
							rangePctWindow = windowRangePct,
							barsCount = candlesList.Count,
							recentCandles = reviewRecent.Select((Candle c) => new
							{
								timeUtc = c.Time.ToString("o"),
								open = c.Open,
								high = c.High,
								low = c.Low,
								close = c.Close,
								volume = c.Volume
							}).ToList(),
							windowSummaries = reviewWindowSummaries
						},
						execution = new
						{
							accountId = accountId,
							equityUsd = equityUsd,
							riskPct = riskPct,
							riskDollars = riskDollars,
							qty = qty,
							riskDistance = distance,
							rr = ((distance > 0m) ? (Math.Abs(result.TakeProfit - result.EntryPrice) / distance) : 0m),
							expectedEdge = selectedExpectancy,
							winRate = selectedWinRate,
							samples = selectedSamples,
							globalBias = ((_engine != null) ? _engine.GlobalBias.ToString() : "Unknown")
						}
					};
					string json = UtilCompat.JsonSerialize(reviewPayload);
					string prompt = "You are a deterministic crypto risk manager. Review the trade payload and decide approve/veto. Use only provided data. Return ONLY valid JSON (no markdown, no prose, no code fences). Schema: {\"bias\":\"Bullish\"|\"Bearish\"|\"Neutral\",\"approve\":true|false,\"reason\":\"one short sentence\",\"confidence\":0.0-1.0,\"SuggestedLimit\":0.0}. If evidence is weak or conflicting, set approve=false and SuggestedLimit=0. If approve=true, SuggestedLimit must be >0 only when it improves execution; otherwise set 0. JSON Data: " + json;
					string aiRaw = await _sidecar.QueryAIAsync(prompt);
					if (string.IsNullOrWhiteSpace(aiRaw))
					{
						Log.Warn("[AutoPlanner] AI returned empty response.", "ProposeWithDiagnosticsAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 378);
						plan.Note += " [AI Empty Response]";
						return new ProposalDiagnostics
						{
							Plans = new List<TradePlan> { plan },
							ReasonCode = "ok",
							ReasonMessage = "AI review empty response; proceeding with base plan."
						};
					}
					string cleanJson = aiRaw.Replace("```json", "").Replace("```", "").Trim();
					AIResponse aiResp;
					try
					{
						aiResp = UtilCompat.JsonDeserialize<AIResponse>(cleanJson);
					}
					catch
					{
						aiResp = null;
					}
					if (aiResp != null)
					{
						if (!aiResp.Approve)
						{
							Log.Info("[AutoPlanner] AI vetoed trade.", "ProposeWithDiagnosticsAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 404);
							return new ProposalDiagnostics
							{
								ReasonCode = "ai-veto",
								ReasonMessage = "AI vetoed trade (json)."
							};
						}
						Log.Info("[AutoPlanner] AI approved trade.", "ProposeWithDiagnosticsAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 408);
						plan.Note = plan.Note + " [AI Approved: " + aiResp.Reason + "]";
						if (aiResp.SuggestedLimit.HasValue && aiResp.SuggestedLimit.Value > 0m)
						{
							plan.Entry = aiResp.SuggestedLimit.Value;
							plan.Note += $" [SmartLimit: {plan.Entry}]";
						}
					}
					else
					{
						string lower = (cleanJson ?? string.Empty).ToLowerInvariant();
						if (IsClarificationOrAmbiguousResponse(lower))
						{
							Log.Warn("[AutoPlanner] AI returned clarification/ambiguous response; skipping approval parse.", "ProposeWithDiagnosticsAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 422);
							plan.Note += " [AI Clarification Requested]";
							return new ProposalDiagnostics
							{
								Plans = new List<TradePlan> { plan },
								ReasonCode = "ok",
								ReasonMessage = "Plan proposed with AI clarification response."
							};
						}
						bool? approved = ParseApproveFromText(cleanJson);
						if (approved.HasValue && !approved.Value)
						{
							Log.Info("[AutoPlanner] AI vetoed trade (text parse fallback).", "ProposeWithDiagnosticsAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 435);
							return new ProposalDiagnostics
							{
								ReasonCode = "ai-veto",
								ReasonMessage = "AI vetoed trade (text fallback)."
							};
						}
						if (approved.HasValue && approved.Value)
						{
							Log.Info("[AutoPlanner] AI approved trade (text parse fallback).", "ProposeWithDiagnosticsAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 441);
							plan.Note += " [AI Approved: parsed from text]";
						}
						else
						{
							Log.Warn("[AutoPlanner] AI response parse failed (json and text fallback). Raw head: " + ((cleanJson.Length > 80) ? cleanJson.Substring(0, 80) : cleanJson), "ProposeWithDiagnosticsAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 446);
							plan.Note += " [AI Parse Failed]";
						}
					}
				}
				catch (Exception ex)
				{
					Exception ex2 = ex;
					Log.Error("[AutoPlanner] AI review failed.", ex2, "ProposeWithDiagnosticsAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 453);
					plan.Note = plan.Note + " [AI Failed: " + ex2.Message + "]";
				}
			}
			return new ProposalDiagnostics
			{
				Plans = new List<TradePlan> { plan },
				ReasonCode = "ok",
				ReasonMessage = "Plan proposed."
			};
		}

		private bool IsAiProposerEnabled()
		{
			string mode = (Environment.GetEnvironmentVariable("CDTS_AI_PROPOSER_MODE") ?? string.Empty).Trim();
			string enabled = (Environment.GetEnvironmentVariable("CDTS_AI_PROPOSER_ENABLED") ?? string.Empty).Trim();
			string value = (string.IsNullOrWhiteSpace(mode) ? enabled : mode);
			if (string.IsNullOrWhiteSpace(value))
			{
				return false;
			}
			string normalized = value.ToLowerInvariant();
			int result;
			switch (normalized)
			{
			default:
				result = ((normalized == "enabled") ? 1 : 0);
				break;
			case "1":
			case "true":
			case "yes":
			case "on":
				result = 1;
				break;
			}
			return (byte)result != 0;
		}

		private async Task<TradePlan> TryBuildVerifiedAiProposedPlanAsync(string accountId, string productId, int granMinutes, decimal equityUsd, decimal riskPct, List<ProjectionRow> rows, List<Candle> candles)
		{
			try
			{
				List<LiveSignalEvidence> liveSignals = new List<LiveSignalEvidence>();
				foreach (IStrategy strategy in _strategies)
				{
					StrategyResult signal = strategy.GetSignal(candles);
					if (signal != null && signal.IsSignal)
					{
						ProjectionRow row = rows?.FirstOrDefault((ProjectionRow r) => string.Equals(r.Strategy, strategy.Name, StringComparison.OrdinalIgnoreCase));
						liveSignals.Add(new LiveSignalEvidence
						{
							Strategy = strategy.Name,
							Side = ((signal.Side == OrderSide.Buy) ? "Buy" : "Sell"),
							Entry = signal.EntryPrice,
							Stop = signal.StopLoss,
							Target = signal.TakeProfit,
							Confidence = signal.ConfidenceScore,
							Expectancy = (row?.Expectancy ?? 0.0),
							WinRate = (row?.WinRate ?? 0.0),
							Samples = (row?.Samples ?? 0)
						});
					}
				}
				if (liveSignals.Count == 0)
				{
					Log.Info("[AutoPlanner] AI proposer skipped: no live strategy signals available for verification.", "TryBuildVerifiedAiProposedPlanAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 515);
					return null;
				}
				var recent = (from c in candles.Skip(Math.Max(0, candles.Count - 48))
					select new
					{
						timeUtc = c.Time.ToString("o"),
						open = c.Open,
						high = c.High,
						low = c.Low,
						close = c.Close,
						volume = c.Volume
					}).ToList();
				Candle latest = candles.Last();
				Candle first = candles.First();
				decimal currentPrice = latest.Close;
				decimal vwap = Indicators.VWAP(candles);
				decimal rsi = Indicators.RSI(candles, 14);
				decimal atr = Indicators.ATR(candles, 14);
				decimal rangePct = ((first.Close != 0m) ? ((latest.Close - first.Close) / first.Close * 100m) : 0m);
				var proposerWindowSummaries = new int[3] { 12, 24, 48 }.Where((int w) => candles.Count >= w).Select(delegate(int w)
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
				var payload = new
				{
					symbol = productId,
					granularityMinutes = granMinutes,
					timestampUtc = DateTime.UtcNow.ToString("o"),
					currentPrice = currentPrice,
					vwap = vwap,
					rsi14 = rsi,
					atr14 = atr,
					rangePctWindow = rangePct,
					recentCandles = recent,
					windowSummaries = proposerWindowSummaries,
					liveSignals = liveSignals,
					rule = "Only propose trades that align with at least one provided live strategy signal."
				};
				string contextJson = UtilCompat.JsonSerialize(payload);
				string prompt = "You are an execution-constrained crypto planner. Analyze the payload and either propose one trade or reject. Use only provided data. Return ONLY valid JSON (no markdown, no prose, no code fences). Schema: {\"approve\":true|false,\"symbol\":\"...\",\"side\":\"Buy\"|\"Sell\",\"entry\":0.0,\"stop\":0.0,\"target\":0.0,\"strategyHint\":\"...\",\"reason\":\"one short sentence\",\"confidence\":0.0-1.0}. If uncertain, set approve=false and set numeric fields to 0. When approve=true, stop/entry/target must satisfy valid risk geometry for the selected side. JSON Data: " + contextJson;
				Log.Info("[AutoPlanner] Starting AI proposer query.", "TryBuildVerifiedAiProposedPlanAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 583);
				string aiRaw = await _sidecar.QueryAIAsync(prompt);
				if (string.IsNullOrWhiteSpace(aiRaw))
				{
					Log.Warn("[AutoPlanner] AI proposer returned empty response.", "TryBuildVerifiedAiProposedPlanAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 587);
					return null;
				}
				string clean = aiRaw.Replace("```json", "").Replace("```", "").Trim();
				AITradeProposal proposal = ParseAiTradeProposal(clean);
				if (proposal == null)
				{
					Log.Warn("[AutoPlanner] AI proposer parse failed.", "TryBuildVerifiedAiProposedPlanAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 595);
					return null;
				}
				if (!proposal.Approve)
				{
					Log.Info("[AutoPlanner] AI proposer rejected trade: " + (proposal.Reason ?? "No reason provided"), "TryBuildVerifiedAiProposedPlanAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 601);
					return null;
				}
				if (!string.IsNullOrWhiteSpace(proposal.Symbol))
				{
					string proposedSymbol = (proposal.Symbol ?? string.Empty).Replace("/", "-").Trim();
					string requestedSymbol = (productId ?? string.Empty).Replace("/", "-").Trim();
					if (!string.Equals(proposedSymbol, requestedSymbol, StringComparison.OrdinalIgnoreCase))
					{
						Log.Warn("[AutoPlanner] AI proposer symbol mismatch. Proposed=" + proposedSymbol + " Requested=" + requestedSymbol, "TryBuildVerifiedAiProposedPlanAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 611);
						return null;
					}
				}
				if (!TryParseOrderSide(proposal.Side, out var proposedSide))
				{
					Log.Warn("[AutoPlanner] AI proposer side parse failed: " + (proposal.Side ?? "<null>"), "TryBuildVerifiedAiProposedPlanAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 618);
					return null;
				}
				if (!proposal.Entry.HasValue || !proposal.Stop.HasValue || !proposal.Target.HasValue)
				{
					Log.Warn("[AutoPlanner] AI proposer missing required price levels.", "TryBuildVerifiedAiProposedPlanAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 624);
					return null;
				}
				decimal entry = proposal.Entry.Value;
				decimal stop = proposal.Stop.Value;
				decimal target = proposal.Target.Value;
				if (entry <= 0m || stop <= 0m || target <= 0m)
				{
					Log.Warn("[AutoPlanner] AI proposer invalid non-positive price levels.", "TryBuildVerifiedAiProposedPlanAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 633);
					return null;
				}
				decimal distance = Math.Abs(entry - stop);
				if (distance <= 0.00000001m)
				{
					Log.Warn("[AutoPlanner] AI proposer invalid risk distance.", "TryBuildVerifiedAiProposedPlanAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 640);
					return null;
				}
				if (proposedSide == OrderSide.Buy)
				{
					if (!(stop < entry) || !(target > entry))
					{
						Log.Warn("[AutoPlanner] AI proposer failed long geometry check.", "TryBuildVerifiedAiProposedPlanAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 648);
						return null;
					}
				}
				else if (!(stop > entry) || !(target < entry))
				{
					Log.Warn("[AutoPlanner] AI proposer failed short geometry check.", "TryBuildVerifiedAiProposedPlanAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 656);
					return null;
				}
				if (_engine != null)
				{
					if (_engine.GlobalBias == MarketBias.Bearish && proposedSide == OrderSide.Buy)
					{
						Log.Info("[AutoPlanner] AI proposer blocked by Global Bias (Bearish). ", "TryBuildVerifiedAiProposedPlanAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 665);
						return null;
					}
					if (_engine.GlobalBias == MarketBias.Bullish && proposedSide == OrderSide.Sell)
					{
						Log.Info("[AutoPlanner] AI proposer blocked by Global Bias (Bullish). ", "TryBuildVerifiedAiProposedPlanAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 671);
						return null;
					}
				}
				LiveSignalEvidence matchedSignal = (from s in liveSignals
					where string.Equals(s.Side, (proposedSide == OrderSide.Buy) ? "Buy" : "Sell", StringComparison.OrdinalIgnoreCase)
					orderby s.Expectancy descending
					select s).FirstOrDefault();
				if (matchedSignal == null)
				{
					Log.Info("[AutoPlanner] AI proposer rejected: no live strategy signal aligns with proposed side.", "TryBuildVerifiedAiProposedPlanAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 682);
					return null;
				}
				decimal riskDollars = equityUsd * (riskPct / 100m);
				decimal qty = Math.Round(riskDollars / distance, 6);
				if (qty <= 0m)
				{
					Log.Warn("[AutoPlanner] AI proposer rejected: computed quantity is non-positive.", "TryBuildVerifiedAiProposedPlanAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 690);
					return null;
				}
				string normalizedSymbol = (productId ?? string.Empty).Replace("-", "/");
				string note = "AutoPlanner AIProposer verified " + matchedSignal.Strategy + " exp=" + matchedSignal.Expectancy.ToString("0.00") + " wr=" + matchedSignal.WinRate.ToString("0.0") + "% [AI Approved: " + (proposal.Reason ?? "No reason") + "]";
				if (!string.IsNullOrWhiteSpace(proposal.StrategyHint) && !string.Equals(proposal.StrategyHint, matchedSignal.Strategy, StringComparison.OrdinalIgnoreCase))
				{
					note = note + " [Hint: " + proposal.StrategyHint + "]";
				}
				TradePlan plan = new TradePlan
				{
					AccountId = (accountId ?? "sim-acct"),
					Symbol = normalizedSymbol,
					Strategy = matchedSignal.Strategy,
					Direction = (int)proposedSide,
					Entry = entry,
					Stop = stop,
					Target = target,
					Qty = qty,
					Note = note
				};
				Log.Info("[AutoPlanner] AI proposer produced verified trade.", "TryBuildVerifiedAiProposedPlanAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 719);
				return plan;
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Log.Error("[AutoPlanner] AI proposer failed.", ex2, "TryBuildVerifiedAiProposedPlanAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 724);
				return null;
			}
		}

		private AITradeProposal ParseAiTradeProposal(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			try
			{
				AITradeProposal direct = UtilCompat.JsonDeserialize<AITradeProposal>(text);
				if (direct != null)
				{
					return direct;
				}
			}
			catch
			{
			}
			string inlineJson = TryExtractFirstJsonObject(text);
			if (string.IsNullOrWhiteSpace(inlineJson))
			{
				return null;
			}
			try
			{
				return UtilCompat.JsonDeserialize<AITradeProposal>(inlineJson);
			}
			catch
			{
				return null;
			}
		}

		private string TryExtractFirstJsonObject(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return string.Empty;
			}
			int start = text.IndexOf('{');
			if (start < 0)
			{
				return string.Empty;
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

		private bool TryParseOrderSide(string side, out OrderSide parsed)
		{
			parsed = OrderSide.Buy;
			string s = (side ?? string.Empty).Trim().ToLowerInvariant();
			if (s == "buy" || s == "long")
			{
				parsed = OrderSide.Buy;
				return true;
			}
			if (s == "sell" || s == "short")
			{
				parsed = OrderSide.Sell;
				return true;
			}
			return false;
		}

		private async Task<List<Candle>> GetCandlesSafeAsync(string productId, int granMinutes, DateTime startUtc, DateTime endUtc, string scope)
		{
			try
			{
				return (await _client.GetCandlesAsync(productId, granMinutes, startUtc, endUtc)) ?? new List<Candle>();
			}
			catch (Exception ex)
			{
				if (IsUnsupportedGranularityError(ex))
				{
					int normalized = NormalizeGranularityMinutes(granMinutes);
					if (normalized != Math.Max(1, granMinutes))
					{
						try
						{
							Log.Warn($"[AutoPlanner] {scope} unsupported granularity '{granMinutes}m'. Retrying with '{normalized}m'.", "GetCandlesSafeAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 836);
							return (await _client.GetCandlesAsync(productId, normalized, startUtc, endUtc)) ?? new List<Candle>();
						}
						catch (Exception ex2)
						{
							Log.Error("[AutoPlanner] " + scope + " retry failed after granularity normalization.", ex2, "GetCandlesSafeAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 841);
							return new List<Candle>();
						}
					}
				}
				Log.Error("[AutoPlanner] " + scope + " candles fetch failed.", ex, "GetCandlesSafeAsync", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\AutoPlannerService.cs", 847);
				return new List<Candle>();
			}
		}

		private bool IsUnsupportedGranularityError(Exception ex)
		{
			for (Exception cursor = ex; cursor != null; cursor = cursor.InnerException)
			{
				string message = cursor.Message ?? string.Empty;
				if (message.IndexOf("Unsupported granularity", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}
			return false;
		}

		private int NormalizeGranularityMinutes(int minutes)
		{
			int requested = Math.Max(1, minutes);
			int[] supported = new int[6] { 1, 5, 15, 60, 360, 1440 };
			if (Array.IndexOf(supported, requested) >= 0)
			{
				return requested;
			}
			int closest = supported[0];
			int bestDiff = Math.Abs(requested - closest);
			for (int i = 1; i < supported.Length; i++)
			{
				int diff = Math.Abs(requested - supported[i]);
				if (diff < bestDiff)
				{
					bestDiff = diff;
					closest = supported[i];
				}
			}
			return closest;
		}

		private bool? ParseApproveFromText(string text)
		{
			string t = (text ?? string.Empty).ToLowerInvariant();
			if (IsClarificationOrAmbiguousResponse(t))
			{
				return null;
			}
			bool? fromInlineJson = ParseApproveFromInlineJson(text);
			if (fromInlineJson.HasValue)
			{
				return fromInlineJson;
			}
			bool? fromLabel = ParseApproveFromLabeledText(t);
			if (fromLabel.HasValue)
			{
				return fromLabel;
			}
			bool hasApproveTrue = t.Contains("\"approve\": true") || t.Contains("approve: true");
			bool hasApproveFalse = t.Contains("\"approve\": false") || t.Contains("approve: false");
			if (hasApproveTrue && !hasApproveFalse)
			{
				return true;
			}
			if (hasApproveFalse && !hasApproveTrue)
			{
				return false;
			}
			bool hasApprovedWord = t.Contains("approved") || t.Contains("approve this trade");
			bool hasRejectWord = t.Contains("reject") || t.Contains("veto") || t.Contains("do not approve");
			if (hasApprovedWord && !hasRejectWord)
			{
				return true;
			}
			if (hasRejectWord && !hasApprovedWord)
			{
				return false;
			}
			return null;
		}

		private bool? ParseApproveFromInlineJson(string text)
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
					if (parsed != null)
					{
						return parsed.Approve;
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

		private bool? ParseApproveFromLabeledText(string t)
		{
			if (string.IsNullOrWhiteSpace(t))
			{
				return null;
			}
			int labelIdx = t.IndexOf("approve");
			if (labelIdx < 0)
			{
				return null;
			}
			string probe = t.Substring(labelIdx, Math.Min(80, t.Length - labelIdx));
			if (probe.Contains("false"))
			{
				return false;
			}
			if (probe.Contains("true"))
			{
				return true;
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
			int approveSignals = 0;
			if (t.Contains("\"approve\": true") || t.Contains("approve: true") || t.Contains("approved"))
			{
				approveSignals++;
			}
			if (t.Contains("\"approve\": false") || t.Contains("approve: false") || t.Contains("reject") || t.Contains("veto"))
			{
				approveSignals++;
			}
			if (approveSignals > 1)
			{
				return true;
			}
			if (hasQuestionMark && approveSignals > 0)
			{
				return true;
			}
			return false;
		}
	}
}
