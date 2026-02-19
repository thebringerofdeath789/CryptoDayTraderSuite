using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Models.AI;
using CryptoDayTraderSuite.Util;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Strategy;

namespace CryptoDayTraderSuite.Services
{
    public class AutoPlannerService
    {
        private const decimal MinRiskDistance = 0.00000001m;
        private const int ReviewPromptRecentCandles = 48;
        private const int ProposerPromptRecentCandles = 48;
        private const string JsonStartMarker = "CDTS_JSON_START";
        private const string JsonEndMarker = "CDTS_JSON_END";

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

        private readonly IExchangeClient _client;
        private readonly IEnumerable<IStrategy> _strategies;
        private readonly ChromeSidecar _sidecar;
        private readonly StrategyEngine _engine;
        private readonly MultiVenueQuoteService _multiVenueQuoteService;
        private readonly VenueHealthService _venueHealthService;
        private readonly SpreadDivergenceDetector _spreadDivergenceDetector;
        private readonly SmartOrderRouter _smartOrderRouter;
        private readonly FundingCarryDetector _fundingCarryDetector;
        private readonly StrategyExchangePolicyService _strategyExchangePolicyService;
        private readonly ExecutionCostModelService _executionCostModelService;
        private readonly string[] _routingVenues = new[] { "Coinbase", "Kraken", "Bitstamp", "Binance", "Bybit", "OKX" };
        private const decimal DefaultMinNetEdgeR = 0.02m;
        private const decimal DefaultSlippageBudgetBps = 6m;
        private const decimal DefaultRoutingExpectedGrossEdgeBps = 12m;
        private const decimal DefaultFundingMinCarryBps = 2m;
        private const decimal DefaultFundingMinBasisStability = 0.55m;
        private const decimal DefaultFundingMaxAgeMinutes = 20m;
        private const decimal DefaultAiProposerMinConfidence = 0.55m;
        private const decimal DefaultAiProposerMinRMultiple = 1.50m;
        private const decimal ProposalPriceMatchTolerance = 0.00000001m;
        private const decimal ProposalRMultipleTolerance = 0.000001m;
        private const int ProposalPriceScale = 8;
        private const int ProposalRScale = 6;

        public sealed class ProposalDiagnostics
        {
            public List<TradePlan> Plans = new List<TradePlan>();
            public string ReasonCode;
            public string ReasonMessage;
        }

        public AutoPlannerService(
            IExchangeClient client,
            IEnumerable<IStrategy> strategies,
            ChromeSidecar sidecar = null,
            StrategyEngine engine = null,
            MultiVenueQuoteService multiVenueQuoteService = null,
            VenueHealthService venueHealthService = null,
            SpreadDivergenceDetector spreadDivergenceDetector = null,
            SmartOrderRouter smartOrderRouter = null,
            FundingCarryDetector fundingCarryDetector = null,
            StrategyExchangePolicyService strategyExchangePolicyService = null,
            ExecutionCostModelService executionCostModelService = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
            _sidecar = sidecar;
            _engine = engine;
            _multiVenueQuoteService = multiVenueQuoteService;
            _venueHealthService = venueHealthService;
            _spreadDivergenceDetector = spreadDivergenceDetector;
            _smartOrderRouter = smartOrderRouter;
            _fundingCarryDetector = fundingCarryDetector;
            _strategyExchangePolicyService = strategyExchangePolicyService;
            _executionCostModelService = executionCostModelService;
        }

        public async Task<List<ProjectionRow>> ProjectAsync(string productId, int granMinutes, int lookbackMinutes, decimal takerRate, decimal makerRate)
        {
            var end = DateTime.UtcNow;
            var start = end.AddMinutes(-Math.Max(lookbackMinutes, 60));

            var candles = await GetCandlesSafeAsync(productId, granMinutes, start, end, "Project") ?? new List<Candle>();

            return await Task.Run(() =>
            {
                var outRows = new List<ProjectionRow>();
                if (candles.Count < 40) return outRows;

                foreach (var strategy in _strategies)
                {
                    int wins = 0, total = 0;
                    decimal rsum = 0m;
                    decimal rWinSum = 0m;
                    decimal rLossSum = 0m;

                    /* simulation loop: walk forward */
                    for (int i = 20; i < candles.Count - 1; i++)
                    {
                        var result = strategy.GetSignal(candles, i);

                        if (result.IsSignal)
                        {
                            total++;
                            var entry = result.EntryPrice;
                            var stop = result.StopLoss;
                            var next = candles[i + 1].Close;

                            /* calculate R multiple realized on next bar close (simplified approximation) */
                            /* in reality we would check low/high against stop, but for projection close-to-close is distinct */
                            decimal r = 0m;
                            var risk = Math.Abs(entry - stop);
                            
                            if (risk > 0.00000001m)
                            {
                                if (result.Side == OrderSide.Buy) r = (next - entry) / risk;
                                else r = (entry - next) / risk;
                            }

                            /* naive win check */
                            if (r > 0) 
                            {
                                wins++;
                                rWinSum += r;
                            }
                            else
                            {
                                rLossSum += r;
                            }
                            
                            rsum += r;
                        }
                    }

                    if (total > 0)
                    {
                        var losses = total - wins;
                        outRows.Add(new ProjectionRow
                        {
                            Strategy = strategy.Name,
                            Symbol = productId,
                            GranMinutes = granMinutes,
                            Expectancy = (double)((rsum / total) - (takerRate + makerRate)),
                            WinRate = (double)(100m * wins / total),
                            AvgWin = wins > 0 ? (double)(rWinSum / wins) : 0,
                            AvgLoss = losses > 0 ? (double)(rLossSum / losses) : 0,
                            SharpeApprox = 0, /* Need variance for Sharpe, skip for now */
                            Samples = total
                        });
                    }
                }

                return outRows.OrderByDescending(r => r.Expectancy).ToList();
            });
        }

        public async Task<List<TradePlan>> ProposeAsync(string accountId, string productId, int granMinutes, decimal equityUsd, decimal riskPct, List<ProjectionRow> rows)
        {
            var diag = await ProposeWithDiagnosticsAsync(accountId, productId, granMinutes, equityUsd, riskPct, rows);
            return diag != null ? (diag.Plans ?? new List<TradePlan>()) : new List<TradePlan>();
        }

        public async Task<ProposalDiagnostics> ProposeWithDiagnosticsAsync(string accountId, string productId, int granMinutes, decimal equityUsd, decimal riskPct, List<ProjectionRow> rows)
        {
            var candidateRows = (rows ?? new List<ProjectionRow>())
                .OrderByDescending(r => r.Expectancy)
                .ToList();

            if (candidateRows.Count == 0)
            {
                return new ProposalDiagnostics { ReasonCode = "no-best-row", ReasonMessage = "No projection rows available." };
            }

            var end = DateTime.UtcNow;
            var start = end.Subtract(TimeSpan.FromMinutes(granMinutes * 100)); /* need enough lookback */

            var candlesList = await GetCandlesSafeAsync(productId, granMinutes, start, end, "Propose");
            if (candlesList == null || candlesList.Count == 0)
            {
                return new ProposalDiagnostics { ReasonCode = "no-candles", ReasonMessage = "No candles returned for proposal." };
            }

            var fundingStrategyEnabled = candidateRows.Any(r => IsFundingCarryStrategyName(r != null ? r.Strategy : string.Empty));
            var fundingCarryDiagnostics = fundingStrategyEnabled ? BuildFundingCarryDiagnostics(productId) : null;
            if (fundingStrategyEnabled && (fundingCarryDiagnostics == null || !fundingCarryDiagnostics.InputsReady))
            {
                var message = fundingCarryDiagnostics != null
                    ? fundingCarryDiagnostics.FailReason
                    : "Funding carry enabled but detector/input path unavailable.";
                Util.Log.Warn("[AutoPlanner] Funding carry fail-closed: " + message);
                return new ProposalDiagnostics
                {
                    ReasonCode = "funding-input-unavailable",
                    ReasonMessage = message
                };
            }

            if (IsAiProposerEnabled() && _sidecar != null && _sidecar.IsConnected)
            {
                var aiPlan = await TryBuildVerifiedAiProposedPlanAsync(accountId, productId, granMinutes, equityUsd, riskPct, rows, candlesList);
                if (aiPlan != null)
                {
                    return new ProposalDiagnostics
                    {
                        Plans = new List<TradePlan> { aiPlan },
                        ReasonCode = "ok",
                        ReasonMessage = "AI proposer returned verified plan."
                    };
                }

                Util.Log.Info("[AutoPlanner] AI proposer did not return a verified trade. Falling back to strategy-first proposal flow.");
            }

            var routingDiagnostics = await BuildRoutingDiagnosticsAsync(productId).ConfigureAwait(false);
            if (routingDiagnostics != null && !routingDiagnostics.IsRoutable)
            {
                Util.Log.Warn("[AutoPlanner] Multi-venue routing unavailable: " + routingDiagnostics.Reason);
                return new ProposalDiagnostics
                {
                    ReasonCode = "routing-unavailable",
                    ReasonMessage = "Routing unavailable: " + routingDiagnostics.Reason
                };
            }

            var policyHealthSnapshots = _venueHealthService != null
                ? _venueHealthService.BuildSnapshots()
                : new List<VenueHealthSnapshot>();

            ProjectionRow selectedRow = null;
            IStrategy selectedStrategy = null;
            StrategyResult result = null;
            StrategyExchangePolicyDecision selectedPolicyDecision = null;
            var strategyNotFoundCount = 0;
            var noSignalCount = 0;
            var biasBlockedCount = 0;
            var policyBlockedCount = 0;
            var lastPolicyRejectCode = string.Empty;
            var lastPolicyRejectReason = string.Empty;

            foreach (var candidate in candidateRows)
            {
                if (candidate == null || string.IsNullOrWhiteSpace(candidate.Strategy))
                {
                    continue;
                }

                var strategy = _strategies.FirstOrDefault(s => string.Equals(s.Name, candidate.Strategy, StringComparison.OrdinalIgnoreCase));
                if (strategy == null)
                {
                    strategyNotFoundCount++;
                    continue;
                }

                var candidateSignal = strategy.GetSignal(candlesList);
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

                if (_strategyExchangePolicyService != null)
                {
                    var policyDecision = _strategyExchangePolicyService.Evaluate(
                        strategy.Name,
                        routingDiagnostics != null ? routingDiagnostics.ChosenVenue : string.Empty,
                        candidateSignal.Side,
                        _engine != null ? _engine.GlobalBias : MarketBias.Neutral,
                        policyHealthSnapshots);

                    if (policyDecision != null && !policyDecision.IsAllowed)
                    {
                        policyBlockedCount++;
                        lastPolicyRejectCode = policyDecision.RejectCode;
                        lastPolicyRejectReason = policyDecision.RejectReason;
                        continue;
                    }

                    selectedPolicyDecision = policyDecision;
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
                    return new ProposalDiagnostics { ReasonCode = "strategy-not-found", ReasonMessage = "No matching strategy instances found for projection rows." };
                }

                if (biasBlockedCount > 0)
                {
                    Util.Log.Info("[AutoPlanner] All live signals were blocked by global bias.");
                    return new ProposalDiagnostics
                    {
                        ReasonCode = "bias-blocked",
                        ReasonMessage = string.Format("Bias blocked live signals (blocked={0}, noSignal={1}).", biasBlockedCount, noSignalCount)
                    };
                }

                if (policyBlockedCount > 0)
                {
                    var policyCode = string.IsNullOrWhiteSpace(lastPolicyRejectCode) ? "policy-matrix-blocked" : lastPolicyRejectCode;
                    var policyReason = string.IsNullOrWhiteSpace(lastPolicyRejectReason)
                        ? string.Format("Policy blocked candidate strategies on routed venue (blocked={0}).", policyBlockedCount)
                        : lastPolicyRejectReason;

                    Util.Log.Info("[AutoPlanner] Strategy-policy block: " + policyReason + " code=" + policyCode);
                    return new ProposalDiagnostics
                    {
                        ReasonCode = policyCode,
                        ReasonMessage = policyReason
                    };
                }

                Util.Log.Info(string.Format("[AutoPlanner] No live strategy signal available (checked={0}, noSignal={1}, strategyMissing={2}).", candidateRows.Count, noSignalCount, strategyNotFoundCount));
                return new ProposalDiagnostics
                {
                    ReasonCode = "no-signal",
                    ReasonMessage = string.Format("No live signal from ranked strategies (checked={0}, noSignal={1}).", candidateRows.Count, noSignalCount)
                };
            }

            var strategyName = selectedStrategy.Name;
            var selectedExpectancy = selectedRow.Expectancy;
            var selectedWinRate = selectedRow.WinRate;
            var selectedSamples = selectedRow.Samples;
            var riskDistance = Math.Abs(result.EntryPrice - result.StopLoss);

            var costModel = _executionCostModelService ?? new ExecutionCostModelService();
            var modeledCosts = costModel.Build(routingDiagnostics != null ? routingDiagnostics.ChosenVenue : string.Empty, null);

            if (riskDistance <= MinRiskDistance)
            {
                return new ProposalDiagnostics
                {
                    ReasonCode = "risk-veto",
                    ReasonMessage = "Risk geometry invalid: entry/stop distance is too small."
                };
            }

            var winRate01 = Clamp01(ToDecimal(selectedWinRate) / 100m);
            var avgWinR = Math.Max(0m, ToDecimal(selectedRow.AvgWin));
            var avgLossR = Math.Abs(ToDecimal(selectedRow.AvgLoss));
            var grossEdgeR = (winRate01 * avgWinR) - ((1m - winRate01) * avgLossR);
            var projectedNetEdgeR = ToDecimal(selectedExpectancy);
            var feeDragR = Math.Max(0m, grossEdgeR - projectedNetEdgeR);
            var slippageBudgetBps = GetEnvDecimal("CDTS_EXPECTANCY_SLIPPAGE_BPS", DefaultSlippageBudgetBps);
            var modeledFeeR = ConvertBpsToRiskMultiple(modeledCosts.RoundTripFeeBps, result.EntryPrice, riskDistance);
            var modeledSlippageR = ConvertBpsToRiskMultiple(modeledCosts.SlippageBps, result.EntryPrice, riskDistance);
            feeDragR = Math.Max(feeDragR, modeledFeeR);
            var slippageBudgetR = Math.Max(ConvertBpsToRiskMultiple(slippageBudgetBps, result.EntryPrice, riskDistance), modeledSlippageR);

            var edgeBreakdown = RiskGuards.ComputeExpectancyBreakdown(
                winRate01,
                avgWinR,
                avgLossR,
                feeDragR,
                slippageBudgetR);

            var minNetEdgeR = GetEnvDecimal("CDTS_EXPECTANCY_MIN_NET_EDGE_R", DefaultMinNetEdgeR);
            if (!RiskGuards.NetEdgeIsViable(edgeBreakdown, minNetEdgeR))
            {
                var preSlippageEdgeR = edgeBreakdown.GrossEdgeR - edgeBreakdown.FeeDragR;
                var reasonCode = preSlippageEdgeR <= minNetEdgeR ? "fees-kill" : "slippage-kill";
                var reason = "Expectancy gate blocked: gross=" + edgeBreakdown.GrossEdgeR.ToString("0.0000")
                    + " feeDrag=" + edgeBreakdown.FeeDragR.ToString("0.0000")
                    + " slippage=" + edgeBreakdown.SlippageBudgetR.ToString("0.0000")
                    + " net=" + edgeBreakdown.NetEdgeR.ToString("0.0000")
                    + " minNet=" + minNetEdgeR.ToString("0.0000");

                Util.Log.Info("[AutoPlanner] " + reason + " reason=" + reasonCode);
                return new ProposalDiagnostics
                {
                    ReasonCode = reasonCode,
                    ReasonMessage = reason
                };
            }

            /* Calculate Position Size */
            var riskDollars = equityUsd * (riskPct / 100m);
            var distance = riskDistance;
            
            var qty = 0m;
            if (distance > 0)
            {
                qty = Math.Round(riskDollars / distance, 6);
            }

            var plan = new TradePlan {
                AccountId = accountId ?? "sim-acct",
                Symbol = productId.Replace("-", "/"),
                Strategy = strategyName,
                Direction = (int)result.Side,
                Entry = result.EntryPrice,
                Stop = result.StopLoss,
                Target = result.TakeProfit,
                Qty = qty,
                Note = $"AutoPlanner {strategyName} exp={selectedExpectancy:0.00} wr={selectedWinRate:0.0}%"
					+ $" [Confidence={result.ConfidenceScore:0.####}]"
                    + $" [GrossEdge={edgeBreakdown.GrossEdgeR:0.0000}]"
                    + $" [FeeDrag={edgeBreakdown.FeeDragR:0.0000}]"
                    + $" [Slip={edgeBreakdown.SlippageBudgetR:0.0000}]"
                    + $" [NetEdge={edgeBreakdown.NetEdgeR:0.0000}]"
            };

            if (routingDiagnostics != null)
            {
                if (!string.IsNullOrWhiteSpace(routingDiagnostics.ChosenVenue))
                {
                    plan.Note += " [Route=" + routingDiagnostics.ChosenVenue + "]";
                }

                if (!string.IsNullOrWhiteSpace(routingDiagnostics.FallbackVenue))
                {
                    plan.Note += " [Alt=" + routingDiagnostics.FallbackVenue + "]";
                }

                if (routingDiagnostics.BestNetEdgeBps > 0m)
                {
                    plan.Note += " [NetEdge=" + routingDiagnostics.BestNetEdgeBps.ToString("0.####") + "bps]";
                }

                if (!string.IsNullOrWhiteSpace(routingDiagnostics.ExecutionMode))
                {
                    plan.Note += " [ExecMode=" + routingDiagnostics.ExecutionMode + "]";
                }

                if (routingDiagnostics.FeeBpsUsed > 0m)
                {
                    plan.Note += " [FeeBps=" + routingDiagnostics.FeeBpsUsed.ToString("0.####") + "]";
                }

                if (routingDiagnostics.SlippageBpsUsed > 0m)
                {
                    plan.Note += " [SlipBps=" + routingDiagnostics.SlippageBpsUsed.ToString("0.####") + "]";
                }
            }

            if (selectedPolicyDecision != null)
            {
                if (!string.IsNullOrWhiteSpace(selectedPolicyDecision.PolicyId))
                {
                    plan.Note += " [Policy=" + selectedPolicyDecision.PolicyId + "]";
                }

                if (!string.IsNullOrWhiteSpace(selectedPolicyDecision.PolicyRationale))
                {
                    plan.Note += " [PolicyReason=" + selectedPolicyDecision.PolicyRationale + "]";
                }

                if (!string.IsNullOrWhiteSpace(selectedPolicyDecision.RegimeState))
                {
                    plan.Note += " [Regime=" + selectedPolicyDecision.RegimeState + "]";
                }
            }

            if (fundingCarryDiagnostics != null && fundingCarryDiagnostics.Opportunity != null)
            {
                var carry = fundingCarryDiagnostics.Opportunity;
                plan.Note += " [FundingCarry=" + carry.ExpectedCarryBps.ToString("0.####") + "bps]"
                    + " [FundingLong=" + (carry.LongVenue ?? string.Empty) + "]"
                    + " [FundingShort=" + (carry.ShortVenue ?? string.Empty) + "]"
                    + " [FundingBasis=" + carry.BasisStabilityScore.ToString("0.####") + "]"
                    + " [FundingSource=" + (fundingCarryDiagnostics.Source ?? string.Empty) + "]";
            }

            /* AI Review Hook */
            if (_sidecar != null && _sidecar.IsConnected)
            {
                try
                {
                    Util.Log.Info("[AutoPlanner] Starting AI review for generated trade.");

                    var preview = new TradePreview
                    {
                        Symbol = productId,
                        Strategy = strategyName,
                        Side = result.Side.ToString(),
                        Entry = result.EntryPrice,
                        Stop = result.StopLoss,
                        Target = result.TakeProfit,
                        Rationale = $"Expectancy {selectedExpectancy:0.00}, recent signals count {selectedSamples}"
                    };

                    var latest = candlesList.Last();
                    var first = candlesList.First();
                    var windowRangePct = first.Close != 0m ? ((latest.Close - first.Close) / first.Close) * 100m : 0m;
                    var reviewRecent = candlesList.Skip(Math.Max(0, candlesList.Count - ReviewPromptRecentCandles)).ToList();
                    var reviewWindowSummaries = new[] { 12, 24, 48 }
                        .Where(w => candlesList.Count >= w)
                        .Select(w =>
                        {
                            var slice = candlesList.Skip(candlesList.Count - w).ToList();
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
                            recentCandles = reviewRecent.Select(c => new
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
                            rr = distance > 0m ? Math.Abs(result.TakeProfit - result.EntryPrice) / distance : 0m,
                            expectedEdge = selectedExpectancy,
                            expectedGrossEdge = edgeBreakdown.GrossEdgeR,
                            expectedFeeDrag = edgeBreakdown.FeeDragR,
                            expectedSlippageBudget = edgeBreakdown.SlippageBudgetR,
                            expectedNetEdge = edgeBreakdown.NetEdgeR,
                            winRate = selectedWinRate,
                            samples = selectedSamples,
                            globalBias = _engine != null ? _engine.GlobalBias.ToString() : "Unknown",
                            executionCostModel = new
                            {
                                mode = modeledCosts.ExecutionMode,
                                roundTripFeeBps = modeledCosts.RoundTripFeeBps,
                                slippageBps = modeledCosts.SlippageBps,
                                roundTripTotalBps = modeledCosts.RoundTripTotalBps,
                                feeTierAdjustmentBps = modeledCosts.FeeTierAdjustmentBps,
                                rebateBps = modeledCosts.RebateBps
                            },
                            policyMatrix = selectedPolicyDecision == null ? null : new
                            {
                                policyId = selectedPolicyDecision.PolicyId,
                                rationale = selectedPolicyDecision.PolicyRationale,
                                venue = routingDiagnostics != null ? routingDiagnostics.ChosenVenue : string.Empty
                            },
                            fundingCarry = fundingCarryDiagnostics == null ? null : new
                            {
                                enabled = fundingStrategyEnabled,
                                inputsReady = fundingCarryDiagnostics.InputsReady,
                                source = fundingCarryDiagnostics.Source,
                                snapshotCount = fundingCarryDiagnostics.SnapshotCount,
                                latestSnapshotUtc = fundingCarryDiagnostics.LatestSnapshotUtc.HasValue ? fundingCarryDiagnostics.LatestSnapshotUtc.Value.ToString("o") : string.Empty,
                                failReason = fundingCarryDiagnostics.FailReason,
                                candidate = fundingCarryDiagnostics.Opportunity == null ? null : new
                                {
                                    symbol = fundingCarryDiagnostics.Opportunity.Symbol,
                                    longVenue = fundingCarryDiagnostics.Opportunity.LongVenue,
                                    shortVenue = fundingCarryDiagnostics.Opportunity.ShortVenue,
                                    expectedCarryBps = fundingCarryDiagnostics.Opportunity.ExpectedCarryBps,
                                    basisStabilityScore = fundingCarryDiagnostics.Opportunity.BasisStabilityScore,
                                    rejectReason = fundingCarryDiagnostics.Opportunity.RejectReason,
                                    isExecutable = fundingCarryDiagnostics.Opportunity.IsExecutable
                                }
                            }
                        }
                    };

                    var json = UtilCompat.JsonSerialize(reviewPayload);
                    var prompt = BuildAiReviewPrompt(json);
                    
                    var aiRaw = await _sidecar.QueryAIAsync(prompt);
                    if (string.IsNullOrWhiteSpace(aiRaw))
                    {
                        Util.Log.Warn("[AutoPlanner] AI returned empty response.");
                        plan.Note += " [AI Empty Response]";
                        return new ProposalDiagnostics
                        {
                            Plans = new List<TradePlan> { plan },
                            ReasonCode = "ok",
                            ReasonMessage = "AI review empty response; proceeding with base plan."
                        };
                    }
                    
                    var cleanJson = NormalizeAiResponseText(aiRaw);
                    var aiResp = ParseAiResponse(cleanJson);
                    if (aiResp == null)
                    {
                        var retryRaw = await QueryStrictJsonRepairAsync(AiJsonSchemas.PlannerReviewSchema, cleanJson);
                        if (!string.IsNullOrWhiteSpace(retryRaw))
                        {
                            cleanJson = NormalizeAiResponseText(retryRaw);
                            aiResp = ParseAiResponse(cleanJson);
                        }
                    }
                    
                    if (aiResp != null)
                    {
                        if (!aiResp.Approve)
                        {
                            Util.Log.Info("[AutoPlanner] AI vetoed trade.");
                            return new ProposalDiagnostics { ReasonCode = "ai-veto", ReasonMessage = "AI vetoed trade (json)." };
                        }

                        if (!StrictJsonPromptContract.MatchesExactTopLevelObjectContract(cleanJson, AiJsonSchemas.PlannerReviewKeys))
                        {
                            Util.Log.Warn("[AutoPlanner] AI review approve rejected: strict key-order contract mismatch.");
                            return new ProposalDiagnostics { ReasonCode = "ai-veto", ReasonMessage = "AI review invalid strict contract." };
                        }

                        Util.Log.Info("[AutoPlanner] AI approved trade.");
                        plan.Note += $" [AI Approved: {aiResp.Reason}]";
                        
                        if (aiResp.SuggestedLimit.HasValue && aiResp.SuggestedLimit.Value > 0)
                        {
                            plan.Entry = aiResp.SuggestedLimit.Value;
                            plan.Note += $" [SmartLimit: {plan.Entry}]";
                        }
                    }
                    else
                    {
                        var lower = (cleanJson ?? string.Empty).ToLowerInvariant();
                        if (IsClarificationOrAmbiguousResponse(lower))
                        {
                            Util.Log.Warn("[AutoPlanner] AI returned clarification/ambiguous response; skipping approval parse.");
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
                            Util.Log.Info("[AutoPlanner] AI vetoed trade (text parse fallback).");
                            return new ProposalDiagnostics { ReasonCode = "ai-veto", ReasonMessage = "AI vetoed trade (text fallback)." };
                        }

                        if (approved.HasValue && approved.Value)
                        {
                            Util.Log.Warn("[AutoPlanner] AI text-approve fallback rejected: strict JSON contract required for approval.");
                            return new ProposalDiagnostics { ReasonCode = "ai-veto", ReasonMessage = "AI review strict JSON contract required for approval." };
                        }
                        else
                        {
                            Util.Log.Warn("[AutoPlanner] AI review returned invalid contract; trade vetoed (fail-closed).");
                            return new ProposalDiagnostics { ReasonCode = "ai-veto", ReasonMessage = "AI review invalid contract." };
                        }
                    }
                }
                catch (Exception ex)
                {
                    Util.Log.Error("[AutoPlanner] AI review failed.", ex);
                    plan.Note += $" [AI Failed: {ex.Message}]";
                }
            }

            return new ProposalDiagnostics
            {
                Plans = new List<TradePlan> { plan },
                ReasonCode = "ok",
                ReasonMessage = "Plan proposed."
            };
        }

        private sealed class RoutingDiagnostics
        {
            public bool IsRoutable { get; set; }
            public string ChosenVenue { get; set; }
            public string FallbackVenue { get; set; }
            public decimal BestNetEdgeBps { get; set; }
            public string Reason { get; set; }
            public decimal FeeBpsUsed { get; set; }
            public decimal SlippageBpsUsed { get; set; }
            public string ExecutionMode { get; set; }
        }

        private sealed class FundingCarryDiagnostics
        {
            public bool InputsReady { get; set; }
            public string FailReason { get; set; }
            public string Source { get; set; }
            public int SnapshotCount { get; set; }
            public DateTime? LatestSnapshotUtc { get; set; }
            public FundingCarryOpportunity Opportunity { get; set; }
        }

        private FundingCarryDiagnostics BuildFundingCarryDiagnostics(string productId)
        {
            if (_fundingCarryDetector == null)
            {
                return new FundingCarryDiagnostics
                {
                    InputsReady = false,
                    FailReason = "Funding carry detector is not configured.",
                    Source = "detector-missing"
                };
            }

            string source;
            string loadError;
            var snapshots = LoadFundingSnapshots(productId, out source, out loadError);
            if (snapshots == null || snapshots.Count == 0)
            {
                return new FundingCarryDiagnostics
                {
                    InputsReady = false,
                    FailReason = "No funding snapshots available" + (string.IsNullOrWhiteSpace(loadError) ? "." : (": " + loadError)),
                    Source = source
                };
            }

            var nowUtc = DateTime.UtcNow;
            var maxAge = TimeSpan.FromMinutes((double)GetEnvDecimal("CDTS_FUNDING_MAX_AGE_MINUTES", DefaultFundingMaxAgeMinutes));
            var symbolSnapshots = snapshots
                .Where(s => s != null && FundingSymbolMatchesProduct(s.Symbol, productId))
                .Where(s => nowUtc - s.TimestampUtc <= maxAge)
                .ToList();

            if (symbolSnapshots.Count < 2)
            {
                return new FundingCarryDiagnostics
                {
                    InputsReady = false,
                    FailReason = "Funding snapshots missing/stale for product within freshness window.",
                    Source = source,
                    SnapshotCount = symbolSnapshots.Count,
                    LatestSnapshotUtc = snapshots.Where(s => s != null).Select(s => (DateTime?)s.TimestampUtc).OrderByDescending(t => t).FirstOrDefault()
                };
            }

            var minCarryBps = GetEnvDecimal("CDTS_FUNDING_MIN_CARRY_BPS", DefaultFundingMinCarryBps);
            var minBasisStability = GetEnvDecimal("CDTS_FUNDING_MIN_BASIS_STABILITY", DefaultFundingMinBasisStability);
            var opportunities = _fundingCarryDetector.Detect(symbolSnapshots, minCarryBps, minBasisStability);
            var best = opportunities
                .Where(o => o != null)
                .OrderByDescending(o => o.IsExecutable)
                .ThenByDescending(o => o.ExpectedCarryBps)
                .FirstOrDefault();

            if (best == null)
            {
                return new FundingCarryDiagnostics
                {
                    InputsReady = false,
                    FailReason = "Funding detector did not produce a candidate for symbol.",
                    Source = source,
                    SnapshotCount = symbolSnapshots.Count,
                    LatestSnapshotUtc = symbolSnapshots.Select(s => (DateTime?)s.TimestampUtc).OrderByDescending(t => t).FirstOrDefault()
                };
            }

            var diagnostics = new FundingCarryDiagnostics
            {
                InputsReady = best.IsExecutable,
                FailReason = best.IsExecutable ? string.Empty : ("Funding candidate rejected: " + (best.RejectReason ?? "unknown")),
                Source = source,
                SnapshotCount = symbolSnapshots.Count,
                LatestSnapshotUtc = symbolSnapshots.Select(s => (DateTime?)s.TimestampUtc).OrderByDescending(t => t).FirstOrDefault(),
                Opportunity = best
            };

            if (best.IsExecutable)
            {
                Util.Log.Info("[AutoPlanner] Funding carry candidate "
                    + (best.Symbol ?? string.Empty)
                    + " long=" + (best.LongVenue ?? string.Empty)
                    + " short=" + (best.ShortVenue ?? string.Empty)
                    + " carryBps=" + best.ExpectedCarryBps.ToString("0.####")
                    + " basis=" + best.BasisStabilityScore.ToString("0.####")
                    + " source=" + (source ?? string.Empty));
            }

            return diagnostics;
        }

        private List<FundingRateSnapshot> LoadFundingSnapshots(string productId, out string source, out string loadError)
        {
            source = string.Empty;
            loadError = string.Empty;

            var envJson = Environment.GetEnvironmentVariable("CDTS_FUNDING_SNAPSHOTS_JSON");
            if (!string.IsNullOrWhiteSpace(envJson))
            {
                var parsedFromEnv = ParseFundingSnapshotsFromJson(envJson);
                if (parsedFromEnv.Count > 0)
                {
                    source = "env:CDTS_FUNDING_SNAPSHOTS_JSON";
                    return parsedFromEnv;
                }
            }

            var candidatePaths = new List<string>();
            var envPath = Environment.GetEnvironmentVariable("CDTS_FUNDING_SNAPSHOTS_PATH");
            if (!string.IsNullOrWhiteSpace(envPath))
            {
                candidatePaths.Add(envPath);
            }

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            candidatePaths.Add(Path.Combine(localAppData, "CryptoDayTraderSuite", "funding", "latest_funding_snapshots.json"));
            candidatePaths.Add(Path.Combine(Environment.CurrentDirectory, "obj", "runtime_reports", "funding", "latest_funding_snapshots.json"));

            foreach (var path in candidatePaths.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    if (!File.Exists(path)) continue;

                    var raw = File.ReadAllText(path);
                    var parsed = ParseFundingSnapshotsFromJson(raw);
                    if (parsed.Count > 0)
                    {
                        source = path;
                        return parsed;
                    }
                }
                catch (Exception ex)
                {
                    loadError = ex.Message;
                }
            }

            source = candidatePaths.FirstOrDefault() ?? "none";
            if (string.IsNullOrWhiteSpace(loadError))
            {
                loadError = "No funding snapshot JSON source produced valid rows for " + (productId ?? string.Empty) + ".";
            }

            return new List<FundingRateSnapshot>();
        }

        private List<FundingRateSnapshot> ParseFundingSnapshotsFromJson(string json)
        {
            var snapshots = new List<FundingRateSnapshot>();
            if (string.IsNullOrWhiteSpace(json)) return snapshots;

            var list = UtilCompat.JsonDeserialize<List<Dictionary<string, object>>>(json);
            if (list != null)
            {
                foreach (var row in list)
                {
                    AppendFundingSnapshotRow(snapshots, row);
                }
            }

            if (snapshots.Count > 0)
            {
                return snapshots;
            }

            var wrapped = UtilCompat.JsonDeserialize<Dictionary<string, object>>(json);
            if (wrapped == null)
            {
                return snapshots;
            }

            object payload;
            if (TryGetValue(wrapped, "snapshots", out payload) || TryGetValue(wrapped, "results", out payload) || TryGetValue(wrapped, "rows", out payload))
            {
                var array = payload as ArrayList;
                if (array != null)
                {
                    foreach (var item in array)
                    {
                        var row = item as Dictionary<string, object>;
                        if (row != null)
                        {
                            AppendFundingSnapshotRow(snapshots, row);
                        }
                    }
                }
            }

            return snapshots;
        }

        private void AppendFundingSnapshotRow(List<FundingRateSnapshot> target, Dictionary<string, object> row)
        {
            if (target == null || row == null) return;

            string venue;
            string symbol;
            decimal fundingBps;
            decimal basisBps;
            DateTime timestampUtc;

            if (!TryGetString(row, out venue, "venue", "exchange")) return;
            if (!TryGetString(row, out symbol, "symbol", "product", "productId")) return;
            if (!TryGetDecimal(row, out fundingBps, "fundingRateBps", "funding_bps", "fundingRate")) return;
            if (!TryGetDecimal(row, out basisBps, "basisBps", "basis_bps", "basis")) basisBps = 0m;

            if (!TryGetString(row, out var tsRaw, "timestampUtc", "atUtc", "timestamp", "time")) return;
            if (!DateTime.TryParse(tsRaw, null, System.Globalization.DateTimeStyles.RoundtripKind, out timestampUtc)) return;
            if (timestampUtc.Kind != DateTimeKind.Utc) timestampUtc = timestampUtc.ToUniversalTime();

            target.Add(new FundingRateSnapshot
            {
                Venue = venue,
                Symbol = symbol,
                FundingRateBps = fundingBps,
                BasisBps = basisBps,
                TimestampUtc = timestampUtc
            });
        }

        private bool IsFundingCarryStrategyName(string strategyName)
        {
            var normalized = (strategyName ?? string.Empty).Trim().ToLowerInvariant();
            return normalized.Contains("funding") && normalized.Contains("carry");
        }

        private bool FundingSymbolMatchesProduct(string fundingSymbol, string productId)
        {
            var normalizedFunding = NormalizeSymbolForCompare(fundingSymbol);
            var normalizedProduct = NormalizeSymbolForCompare(productId);
            if (string.IsNullOrWhiteSpace(normalizedFunding) || string.IsNullOrWhiteSpace(normalizedProduct))
            {
                return false;
            }

            if (string.Equals(normalizedFunding, normalizedProduct, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (normalizedFunding.EndsWith("USDT", StringComparison.OrdinalIgnoreCase)
                && normalizedProduct.EndsWith("USD", StringComparison.OrdinalIgnoreCase))
            {
                var fBase = normalizedFunding.Substring(0, normalizedFunding.Length - 4);
                var pBase = normalizedProduct.Substring(0, normalizedProduct.Length - 3);
                return string.Equals(fBase, pBase, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private string NormalizeSymbolForCompare(string symbol)
        {
            return (symbol ?? string.Empty)
                .Trim()
                .ToUpperInvariant()
                .Replace("/", string.Empty)
                .Replace("-", string.Empty)
                .Replace("_", string.Empty)
                .Replace(" ", string.Empty);
        }

        private async Task<RoutingDiagnostics> BuildRoutingDiagnosticsAsync(string productId)
        {
            if (GetEnvFlagEnabled("CDTS_ROUTING_DIAGNOSTICS_DISABLED") || GetEnvFlagEnabled("CDTS_ROUTING_DISABLED"))
            {
                return new RoutingDiagnostics { IsRoutable = false, Reason = "routing-disabled-env" };
            }

            if (_multiVenueQuoteService == null || _smartOrderRouter == null)
            {
                return null;
            }

            var isPerp = IsPerpSymbol(productId);

            string baseAsset;
            string quoteAsset;
            if (!TrySplitSymbol(productId, out baseAsset, out quoteAsset))
            {
                return new RoutingDiagnostics { IsRoutable = false, Reason = "symbol-parse-failed" };
            }

            CompositeQuote composite;
            try
            {
                composite = await _multiVenueQuoteService.GetCompositeQuoteAsync(baseAsset, quoteAsset, _routingVenues).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return new RoutingDiagnostics { IsRoutable = false, Reason = "quote-fetch-failed: " + ex.Message };
            }

            if (composite == null || composite.Venues == null || composite.Venues.Count == 0)
            {
                return new RoutingDiagnostics { IsRoutable = false, Reason = "no-venue-quotes" };
            }

            if (_venueHealthService != null)
            {
                var venueNames = composite.Venues
                    .Where(v => v != null && !string.IsNullOrWhiteSpace(v.Venue))
                    .Select(v => v.Venue)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (! _venueHealthService.HasAnyTradableVenue(venueNames, DateTime.UtcNow))
                {
                    return new RoutingDiagnostics
                    {
                        IsRoutable = false,
                        Reason = "safe-set-empty-circuit-breaker"
                    };
                }
            }

            if (_spreadDivergenceDetector != null)
            {
                try
                {
                    var opps = await _spreadDivergenceDetector.DetectAsync(baseAsset, quoteAsset, _routingVenues, 2m).ConfigureAwait(false);
                    var bestOpp = opps != null ? opps.FirstOrDefault(o => o != null && o.IsExecutable) : null;
                    if (bestOpp != null)
                    {
                        return new RoutingDiagnostics
                        {
                            IsRoutable = true,
                            ChosenVenue = bestOpp.BuyVenue,
                            FallbackVenue = bestOpp.SellVenue,
                            BestNetEdgeBps = bestOpp.NetEdgeBps,
                            Reason = "spread-divergence"
                        };
                    }
                }
                catch (Exception ex)
                {
                    Util.Log.Warn("[AutoPlanner] Spread detector diagnostics failed: " + ex.Message);
                }
            }

            var health = _venueHealthService != null ? _venueHealthService.BuildSnapshots() : new List<VenueHealthSnapshot>();
            var costModel = _executionCostModelService ?? new ExecutionCostModelService();
            var cost = costModel.Build(string.Empty, null);
            var expectedGrossEdgeBps = GetEnvDecimal("CDTS_ROUTING_EXPECTED_GROSS_EDGE_BPS", DefaultRoutingExpectedGrossEdgeBps);
            var route = _smartOrderRouter.Route(baseAsset + "-" + quoteAsset, composite.Venues, health, expectedGrossEdgeBps, cost.RoundTripFeeBps, cost.SlippageBps);

            if (route == null || string.IsNullOrWhiteSpace(route.ChosenVenue))
            {
                return new RoutingDiagnostics
                {
                    IsRoutable = false,
                    Reason = route != null ? (route.Reason ?? "no-eligible-venue") : "no-routing-decision"
                };
            }

            if (isPerp)
            {
                if (!VenueSupportsPerp(route.ChosenVenue))
                {
                    return new RoutingDiagnostics
                    {
                        IsRoutable = false,
                        Reason = "perp-unsupported-venue"
                    };
                }

                if (!string.IsNullOrWhiteSpace(route.FallbackVenue) && !VenueSupportsPerp(route.FallbackVenue))
                {
                    return new RoutingDiagnostics
                    {
                        IsRoutable = false,
                        Reason = "perp-unsupported-fallback"
                    };
                }
            }

            return new RoutingDiagnostics
            {
                IsRoutable = true,
                ChosenVenue = route.ChosenVenue,
                FallbackVenue = route.FallbackVenue,
                BestNetEdgeBps = route.ChosenScoreBps,
                Reason = route.Reason,
                FeeBpsUsed = cost.RoundTripFeeBps,
                SlippageBpsUsed = cost.SlippageBps,
                ExecutionMode = cost.ExecutionMode
            };
        }

        private bool IsPerpSymbol(string productId)
        {
            var symbol = (productId ?? string.Empty).Trim().ToUpperInvariant();
            if (symbol.Length == 0)
            {
                return false;
            }

            return symbol.Contains("PERP")
                || symbol.Contains("SWAP")
                || symbol.Contains(":PERP")
                || symbol.EndsWith("-PERP", StringComparison.OrdinalIgnoreCase)
                || symbol.EndsWith("_PERP", StringComparison.OrdinalIgnoreCase)
                || symbol.EndsWith("USDT:USDT", StringComparison.OrdinalIgnoreCase)
                || symbol.EndsWith("USD:USD", StringComparison.OrdinalIgnoreCase);
        }

        private bool VenueSupportsPerp(string venue)
        {
            var normalized = NormalizeVenueName(venue);
            return string.Equals(normalized, "BINANCE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "BYBIT", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "OKX", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "KRAKEN", StringComparison.OrdinalIgnoreCase);
        }

        private string NormalizeVenueName(string venue)
        {
            return (venue ?? string.Empty)
                .Trim()
                .ToUpperInvariant()
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("_", string.Empty);
        }

        private bool TrySplitSymbol(string productId, out string baseAsset, out string quoteAsset)
        {
            baseAsset = string.Empty;
            quoteAsset = string.Empty;

            if (string.IsNullOrWhiteSpace(productId)) return false;

            var normalized = productId.Trim().Replace("/", "-").ToUpperInvariant();
            var parts = normalized.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                baseAsset = parts[0];
                quoteAsset = parts[1];
                return true;
            }

            if (normalized.EndsWith("USD", StringComparison.OrdinalIgnoreCase) && normalized.Length > 3)
            {
                baseAsset = normalized.Substring(0, normalized.Length - 3);
                quoteAsset = "USD";
                return !string.IsNullOrWhiteSpace(baseAsset);
            }

            return false;
        }

        private decimal GetEnvDecimal(string name, decimal fallback)
        {
            var raw = Environment.GetEnvironmentVariable(name);
            decimal parsed;
            if (!string.IsNullOrWhiteSpace(raw)
                && decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private bool GetEnvFlagEnabled(string name)
        {
            var raw = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            var normalized = raw.Trim();
            return string.Equals(normalized, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "yes", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "on", StringComparison.OrdinalIgnoreCase);
        }

        private decimal ToDecimal(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return 0m;
            return Convert.ToDecimal(value);
        }

        private decimal Clamp01(decimal value)
        {
            if (value < 0m) return 0m;
            if (value > 1m) return 1m;
            return value;
        }

        private decimal ConvertBpsToRiskMultiple(decimal bps, decimal entry, decimal riskDistance)
        {
            if (bps <= 0m || entry <= 0m || riskDistance <= 0m) return 0m;
            var movePct = bps / 10000m;
            var riskPct = riskDistance / entry;
            if (riskPct <= 0m) return 0m;
            return movePct / riskPct;
        }

        private bool IsAiProposerEnabled()
        {
            var mode = (Environment.GetEnvironmentVariable("CDTS_AI_PROPOSER_MODE") ?? string.Empty).Trim();
            var enabled = (Environment.GetEnvironmentVariable("CDTS_AI_PROPOSER_ENABLED") ?? string.Empty).Trim();
            var value = string.IsNullOrWhiteSpace(mode) ? enabled : mode;
            if (string.IsNullOrWhiteSpace(value)) return false;

            var normalized = value.ToLowerInvariant();
            return normalized == "1"
                || normalized == "true"
                || normalized == "yes"
                || normalized == "on"
                || normalized == "enabled";
        }

        private async Task<TradePlan> TryBuildVerifiedAiProposedPlanAsync(
            string accountId,
            string productId,
            int granMinutes,
            decimal equityUsd,
            decimal riskPct,
            List<ProjectionRow> rows,
            List<Candle> candles)
        {
            try
            {
                var liveSignals = new List<LiveSignalEvidence>();
                foreach (var strategy in _strategies)
                {
                    var signal = strategy.GetSignal(candles);
                    if (signal == null || !signal.IsSignal) continue;

                    var row = rows == null ? null : rows.FirstOrDefault(r => string.Equals(r.Strategy, strategy.Name, StringComparison.OrdinalIgnoreCase));
                    liveSignals.Add(new LiveSignalEvidence
                    {
                        Strategy = strategy.Name,
                        Side = signal.Side == OrderSide.Buy ? "Buy" : "Sell",
                        Entry = signal.EntryPrice,
                        Stop = signal.StopLoss,
                        Target = signal.TakeProfit,
                        Confidence = signal.ConfidenceScore,
                        Expectancy = row == null ? 0 : row.Expectancy,
                        WinRate = row == null ? 0 : row.WinRate,
                        Samples = row == null ? 0 : row.Samples
                    });
                }

                if (liveSignals.Count == 0)
                {
                    Util.Log.Info("[AutoPlanner] AI proposer skipped: no live strategy signals available for verification.");
                    return null;
                }

                var recent = candles.Skip(Math.Max(0, candles.Count - ProposerPromptRecentCandles))
                    .Select(c => new
                    {
                        timeUtc = c.Time.ToString("o"),
                        open = c.Open,
                        high = c.High,
                        low = c.Low,
                        close = c.Close,
                        volume = c.Volume
                    }).ToList();
                var latest = candles.Last();
                var first = candles.First();
                var currentPrice = latest.Close;
                var vwap = Indicators.VWAP(candles);
                var rsi = Indicators.RSI(candles, 14);
                var atr = Indicators.ATR(candles, 14);
                var rangePct = first.Close != 0m ? ((latest.Close - first.Close) / first.Close) * 100m : 0m;
                var proposerWindowSummaries = new[] { 12, 24, 48 }
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
                    constraints = new
                    {
                        minConfidence = GetEnvDecimal("CDTS_AI_PROPOSER_MIN_CONFIDENCE", DefaultAiProposerMinConfidence),
                        minRMultiple = GetEnvDecimal("CDTS_AI_PROPOSER_MIN_R", DefaultAiProposerMinRMultiple),
                        signalPriceTolerance = ProposalPriceMatchTolerance,
                        rMultipleTolerance = ProposalRMultipleTolerance,
                        priceScale = ProposalPriceScale,
                        rMultipleScale = ProposalRScale,
                        requireReasonMetricOrPriceReference = true,
                        requireReasonRiskNote = true,
                        reasonMaxChars = 220,
                        requireSignalAlignment = true
                    },
                    rule = "When approve=true, side/entry/stop/target must match one liveSignals row within constraints.signalPriceTolerance and satisfy constraints."
                };

                var contextJson = UtilCompat.JsonSerialize(payload);
                var prompt = BuildAiProposerPrompt(contextJson);

                Util.Log.Info("[AutoPlanner] Starting AI proposer query.");
                var aiRaw = await _sidecar.QueryAIAsync(prompt);
                if (string.IsNullOrWhiteSpace(aiRaw))
                {
                    Util.Log.Warn("[AutoPlanner] AI proposer returned empty response.");
                    return null;
                }

                var clean = NormalizeAiResponseText(aiRaw);
                var proposal = ParseAiTradeProposal(clean);
                if (proposal == null)
                {
                    var retryRaw = await QueryStrictJsonRepairAsync(AiJsonSchemas.PlannerProposerSchema, clean);
                    if (!string.IsNullOrWhiteSpace(retryRaw))
                    {
                        clean = NormalizeAiResponseText(retryRaw);
                        proposal = ParseAiTradeProposal(clean);
                    }

                    if (proposal == null)
                    {
                        Util.Log.Warn("[AutoPlanner] AI proposer returned invalid contract; proposal rejected.");
                        return null;
                    }
                }

                if (!proposal.Approve)
                {
                    Util.Log.Info("[AutoPlanner] AI proposer rejected trade: " + (proposal.Reason ?? "No reason provided"));
                    return null;
                }

                if (!StrictJsonPromptContract.MatchesExactTopLevelObjectContract(clean, AiJsonSchemas.PlannerProposerKeys))
                {
                    Util.Log.Warn("[AutoPlanner] AI proposer rejected: strict key-order contract mismatch.");
                    return null;
                }

                if (proposal.Confidence < 0m || proposal.Confidence > 1m)
                {
                    Util.Log.Warn("[AutoPlanner] AI proposer rejected: confidence is out of 0..1 range.");
                    return null;
                }

                var minProposalConfidence = GetEnvDecimal("CDTS_AI_PROPOSER_MIN_CONFIDENCE", DefaultAiProposerMinConfidence);
                if (proposal.Confidence < minProposalConfidence)
                {
                    Util.Log.Info("[AutoPlanner] AI proposer rejected: confidence below minimum. confidence="
                        + proposal.Confidence.ToString("0.000") + " min=" + minProposalConfidence.ToString("0.000"));
                    return null;
                }

                if (!string.IsNullOrWhiteSpace(proposal.Symbol))
                {
                    var proposedSymbol = (proposal.Symbol ?? string.Empty).Replace("/", "-").Trim();
                    var requestedSymbol = (productId ?? string.Empty).Replace("/", "-").Trim();
                    if (!string.Equals(proposedSymbol, requestedSymbol, StringComparison.OrdinalIgnoreCase))
                    {
                        Util.Log.Warn("[AutoPlanner] AI proposer symbol mismatch. Proposed=" + proposedSymbol + " Requested=" + requestedSymbol);
                        return null;
                    }
                }

                if (!TryParseOrderSide(proposal.Side, out var proposedSide))
                {
                    Util.Log.Warn("[AutoPlanner] AI proposer returned invalid side contract: " + (proposal.Side ?? "<null>"));
                    return null;
                }

                if (!proposal.Entry.HasValue || !proposal.Stop.HasValue || !proposal.Target.HasValue)
                {
                    Util.Log.Warn("[AutoPlanner] AI proposer missing required price levels.");
                    return null;
                }

                var entry = proposal.Entry.Value;
                var stop = proposal.Stop.Value;
                var target = proposal.Target.Value;
                if (entry <= 0m || stop <= 0m || target <= 0m)
                {
                    Util.Log.Warn("[AutoPlanner] AI proposer invalid non-positive price levels.");
                    return null;
                }

                entry = NormalizePriceForContract(entry);
                stop = NormalizePriceForContract(stop);
                target = NormalizePriceForContract(target);

                var distance = Math.Abs(entry - stop);
                if (distance <= MinRiskDistance)
                {
                    Util.Log.Warn("[AutoPlanner] AI proposer invalid risk distance.");
                    return null;
                }

                if (proposedSide == OrderSide.Buy)
                {
                    if (!(stop < entry && target > entry))
                    {
                        Util.Log.Warn("[AutoPlanner] AI proposer failed long geometry check.");
                        return null;
                    }
                }
                else
                {
                    if (!(stop > entry && target < entry))
                    {
                        Util.Log.Warn("[AutoPlanner] AI proposer failed short geometry check.");
                        return null;
                    }
                }

                var rewardDistance = Math.Abs(target - entry);
                if (rewardDistance <= MinRiskDistance)
                {
                    Util.Log.Warn("[AutoPlanner] AI proposer rejected: reward distance is too small.");
                    return null;
                }

                var rrMultiple = ComputeRMultiple(proposedSide, entry, stop, target);
                if (rrMultiple <= 0m)
                {
                    Util.Log.Warn("[AutoPlanner] AI proposer rejected: computed R multiple is invalid.");
                    return null;
                }

                var minRMultiple = GetEnvDecimal("CDTS_AI_PROPOSER_MIN_R", DefaultAiProposerMinRMultiple);
                if (rrMultiple + ProposalRMultipleTolerance < minRMultiple)
                {
                    Util.Log.Info("[AutoPlanner] AI proposer rejected: R multiple below minimum. rr="
                        + rrMultiple.ToString("0.000") + " min=" + minRMultiple.ToString("0.000"));
                    return null;
                }

                if (_engine != null)
                {
                    if (_engine.GlobalBias == MarketBias.Bearish && proposedSide == OrderSide.Buy)
                    {
                        Util.Log.Info("[AutoPlanner] AI proposer blocked by Global Bias (Bearish). ");
                        return null;
                    }

                    if (_engine.GlobalBias == MarketBias.Bullish && proposedSide == OrderSide.Sell)
                    {
                        Util.Log.Info("[AutoPlanner] AI proposer blocked by Global Bias (Bullish). ");
                        return null;
                    }
                }

                var matchedSignal = liveSignals
                    .Where(s => IsExactSignalMatch(s, proposedSide, entry, stop, target))
                    .OrderByDescending(s => s.Expectancy)
                    .FirstOrDefault();
                if (matchedSignal == null)
                {
                    Util.Log.Info("[AutoPlanner] AI proposer rejected: no live strategy signal exactly matches side/entry/stop/target.");
                    return null;
                }

                if (!IsReasonDataBacked(proposal.Reason, entry, stop, target, matchedSignal, rrMultiple))
                {
                    Util.Log.Info("[AutoPlanner] AI proposer rejected: reason lacks concrete evidence/risk context.");
                    return null;
                }

                var riskDollars = equityUsd * (riskPct / 100m);
                var qty = Math.Round(riskDollars / distance, 6);
                if (qty <= 0m)
                {
                    Util.Log.Warn("[AutoPlanner] AI proposer rejected: computed quantity is non-positive.");
                    return null;
                }

                var normalizedSymbol = (productId ?? string.Empty).Replace("-", "/");
                var note = "AutoPlanner AIProposer verified " + matchedSignal.Strategy
                    + " exp=" + matchedSignal.Expectancy.ToString("0.00")
                    + " wr=" + matchedSignal.WinRate.ToString("0.0") + "%"
                    + " [AI Approved: " + (proposal.Reason ?? "No reason") + "]";

                if (!string.IsNullOrWhiteSpace(proposal.StrategyHint)
                    && !string.Equals(proposal.StrategyHint, matchedSignal.Strategy, StringComparison.OrdinalIgnoreCase))
                {
                    note += " [Hint: " + proposal.StrategyHint + "]";
                }

                var plan = new TradePlan
                {
                    AccountId = accountId ?? "sim-acct",
                    Symbol = normalizedSymbol,
                    Strategy = matchedSignal.Strategy,
                    Direction = (int)proposedSide,
                    Entry = entry,
                    Stop = stop,
                    Target = target,
                    Qty = qty,
                    Note = note
                };

                Util.Log.Info("[AutoPlanner] AI proposer produced verified trade.");
                return plan;
            }
            catch (Exception ex)
            {
                Util.Log.Error("[AutoPlanner] AI proposer failed.", ex);
                return null;
            }
        }

        private AITradeProposal ParseAiTradeProposal(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            foreach (var candidate in EnumerateJsonCandidates(text))
            {
                if (TryParseAiTradeProposalFlexible(candidate, out var flexible))
                {
                    return flexible;
                }

                try
                {
                    var direct = UtilCompat.JsonDeserialize<AITradeProposal>(candidate);
                    if (direct != null && ContainsAnyKey(candidate, "approve", "approved", "isApproved")) return direct;
                }
                catch
                {
                }

                try
                {
                    var list = UtilCompat.JsonDeserialize<List<AITradeProposal>>(candidate);
                    if (list != null && list.Count > 0 && list[0] != null && ContainsAnyKey(candidate, "approve", "approved", "isApproved"))
                    {
                        return list[0];
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private AIResponse ParseAiResponse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            foreach (var candidate in EnumerateJsonCandidates(text))
            {
                if (TryParseAiResponseFlexible(candidate, out var flexible))
                {
                    return flexible;
                }

                try
                {
                    var parsed = UtilCompat.JsonDeserialize<AIResponse>(candidate);
                    if (parsed != null && ContainsAnyKey(candidate, "approve", "approved", "isApproved")) return parsed;
                }
                catch
                {
                }

                try
                {
                    var list = UtilCompat.JsonDeserialize<List<AIResponse>>(candidate);
                    if (list != null && list.Count > 0 && list[0] != null && ContainsAnyKey(candidate, "approve", "approved", "isApproved"))
                    {
                        return list[0];
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private bool TryParseAiTradeProposalFlexible(string candidate, out AITradeProposal proposal)
        {
            proposal = null;
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
                "proposal", "trade", "result", "data", "answer", "response", "output", "payload");
            if (dict == null) return false;

            var hasApprove = TryGetBool(dict, out var approve, "approve", "approved", "isApproved");
            if (!hasApprove)
            {
                if (TryGetString(dict, out var decisionText, "decision", "verdict", "action"))
                {
                    var d = decisionText.ToLowerInvariant();
                    if (d.Contains("approve") || d.Contains("accept") || d.Contains("yes") || d.Contains("true"))
                    {
                        approve = true;
                        hasApprove = true;
                    }
                    else if (d.Contains("reject") || d.Contains("deny") || d.Contains("no") || d.Contains("false") || d.Contains("veto"))
                    {
                        approve = false;
                        hasApprove = true;
                    }
                }
            }
            if (!hasApprove)
            {
                approve = false;
            }

            decimal value;
            string text;

            var parsed = new AITradeProposal
            {
                Approve = approve,
                Symbol = TryGetString(dict, out text, "symbol", "product", "productId", "pair", "instrument") ? text : null,
                Side = TryGetString(dict, out text, "side", "direction", "position", "action") ? text : null,
                Entry = TryGetDecimal(dict, out value, "entry", "entryPrice", "price", "limit", "suggestedLimit") ? (decimal?)value : null,
                Stop = TryGetDecimal(dict, out value, "stop", "stopLoss", "sl", "riskStop") ? (decimal?)value : null,
                Target = TryGetDecimal(dict, out value, "target", "takeProfit", "tp") ? (decimal?)value : null,
                StrategyHint = TryGetString(dict, out text, "strategyHint", "strategy", "strategyName") ? text : null,
                Reason = TryGetString(dict, out text, "reason", "rationale", "note", "explanation") ? text : null,
                Confidence = TryGetDecimal(dict, out value, "confidence", "probability", "score") ? value : 0m
            };

            if (!parsed.Approve)
            {
                proposal = parsed;
                return true;
            }

            if (string.IsNullOrWhiteSpace(parsed.Side) || !parsed.Entry.HasValue || !parsed.Stop.HasValue || !parsed.Target.HasValue)
            {
                return false;
            }

            proposal = parsed;
            return true;
        }

        private bool TryParseAiResponseFlexible(string candidate, out AIResponse response)
        {
            response = null;
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
                "review", "result", "data", "answer", "response", "output", "payload");
            if (dict == null) return false;

            decimal dec;
            string text;
            bool ok = TryGetBool(dict, out var approve, "approve", "approved", "isApproved");
            if (!ok)
            {
                if (TryGetString(dict, out text, "decision", "verdict"))
                {
                    var d = text.ToLowerInvariant();
                    if (d.Contains("approve") || d.Contains("accept") || d.Contains("yes") || d.Contains("true"))
                    {
                        approve = true;
                        ok = true;
                    }
                    else if (d.Contains("reject") || d.Contains("deny") || d.Contains("no") || d.Contains("false") || d.Contains("veto"))
                    {
                        approve = false;
                        ok = true;
                    }
                }
            }

            if (!ok) return false;

            response = new AIResponse
            {
                Approve = approve,
                Bias = TryGetString(dict, out text, "bias", "marketBias", "regime") ? text : null,
                Reason = TryGetString(dict, out text, "reason", "rationale", "note", "explanation") ? text : null,
                Confidence = TryGetDecimal(dict, out dec, "confidence", "probability", "score") ? dec : 0m,
                SuggestedLimit = TryGetDecimal(dict, out dec, "suggestedLimit", "limit", "entry", "entryPrice") ? (decimal?)dec : null
            };

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

        private bool TryGetBool(Dictionary<string, object> dict, out bool value, params string[] keys)
        {
            value = false;
            if (dict == null || keys == null) return false;

            foreach (var key in keys)
            {
                if (!TryGetValue(dict, key, out var raw) || raw == null) continue;

                if (raw is bool b)
                {
                    value = b;
                    return true;
                }

                var s = Convert.ToString(raw).Trim().ToLowerInvariant();
                if (s == "true" || s == "1" || s == "yes" || s == "y" || s == "approve" || s == "approved")
                {
                    value = true;
                    return true;
                }
                if (s == "false" || s == "0" || s == "no" || s == "n" || s == "reject" || s == "rejected" || s == "veto")
                {
                    value = false;
                    return true;
                }
            }

            return false;
        }

        private bool TryGetDecimal(Dictionary<string, object> dict, out decimal value, params string[] keys)
        {
            value = 0m;
            if (dict == null || keys == null) return false;

            foreach (var key in keys)
            {
                if (!TryGetValue(dict, key, out var raw) || raw == null) continue;
                if (TryConvertToDecimal(raw, out value)) return true;
            }

            return false;
        }

        private bool TryConvertToDecimal(object raw, out decimal value)
        {
            value = 0m;
            if (raw == null) return false;

            if (raw is decimal d)
            {
                value = d;
                return true;
            }
            if (raw is double db)
            {
                value = Convert.ToDecimal(db);
                return true;
            }
            if (raw is float f)
            {
                value = Convert.ToDecimal(f);
                return true;
            }
            if (raw is int i)
            {
                value = i;
                return true;
            }
            if (raw is long l)
            {
                value = l;
                return true;
            }

            var s = Convert.ToString(raw).Trim();
            if (string.IsNullOrWhiteSpace(s)) return false;

            return decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value)
                || decimal.TryParse(s, out value);
        }

        private async Task<string> QueryStrictJsonRepairAsync(string schema, string previousResponse)
        {
            if (_sidecar == null || !_sidecar.IsConnected) return string.Empty;

            var previous = (previousResponse ?? string.Empty).Trim();
            if (previous.Length > 1200) previous = previous.Substring(0, 1200);

            var prompt = StrictJsonPromptContract.BuildRepairPrompt(schema, JsonStartMarker, JsonEndMarker, previous);

            try
            {
                return await _sidecar.QueryAIAsync(prompt);
            }
            catch (Exception ex)
            {
                Util.Log.Warn("[AutoPlanner] AI strict-json retry failed: " + ex.Message);
                return string.Empty;
            }
        }

        private string BuildAiReviewPrompt(string jsonPayload)
        {
            var instructions = new[]
            {
                "If evidence is weak or conflicting, set approve=false and SuggestedLimit=0.",
                "If approve=true, SuggestedLimit must be >0 only when it improves execution; otherwise set 0."
            };

            return StrictJsonPromptContract.BuildPrompt(
                "You are a deterministic crypto risk manager. Review the trade payload and decide approve/veto.",
                AiJsonSchemas.PlannerReviewSchema,
                AiJsonSchemas.PlannerReviewKeysCsv,
                JsonStartMarker,
                JsonEndMarker,
                instructions,
                jsonPayload);
        }

        private string BuildAiProposerPrompt(string jsonPayload)
        {
            var instructions = new[]
            {
                "Return contract is wrapper + JSON payload only.",
                "If uncertain, set approve=false; numeric fields may be 0.",
                "When approve=true, side/entry/stop/target must exactly match one liveSignals item within constraints.signalPriceTolerance.",
                "When approve=true, stop/entry/target must satisfy valid risk geometry for the selected side.",
                "When approve=true, compute R as (target-entry)/(entry-stop) for Buy and (entry-target)/(stop-entry) for Sell; compare to constraints.minRMultiple using constraints.rMultipleTolerance.",
                "When approve=true, confidence must be >= constraints.minConfidence.",
                "Prices must use at most constraints.priceScale decimals.",
                "Reason must be one short sentence under constraints.reasonMaxChars with no line breaks; include (a) at least one concrete metric/price reference from payload and (b) one explicit risk-control note (stop/risk/R).",
                "When approve=false, still include a concise reason that cites at least one payload metric/price when available."
            };

            return StrictJsonPromptContract.BuildPrompt(
                "You are an execution-constrained crypto planner. Analyze the payload and either propose one trade or reject.",
                AiJsonSchemas.PlannerProposerSchema,
                AiJsonSchemas.PlannerProposerKeysCsv,
                JsonStartMarker,
                JsonEndMarker,
                instructions,
                jsonPayload);
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

        private bool ContainsAnyKey(string candidate, params string[] keys)
        {
            var text = (candidate ?? string.Empty).ToLowerInvariant();
            foreach (var key in keys)
            {
                var k = (key ?? string.Empty).ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(k)) continue;
                if (text.Contains("\"" + k + "\"")) return true;
            }

            return false;
        }

        private IEnumerable<string> EnumerateJsonCandidates(string text)
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

        private bool TryParseOrderSide(string side, out OrderSide parsed)
        {
            parsed = OrderSide.Buy;
            var s = (side ?? string.Empty).Trim().ToLowerInvariant();
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

        private decimal NormalizePriceForContract(decimal value)
        {
            return Math.Round(value, ProposalPriceScale, MidpointRounding.AwayFromZero);
        }

        private decimal ComputeRMultiple(OrderSide side, decimal entry, decimal stop, decimal target)
        {
            decimal riskDistance;
            decimal rewardDistance;

            if (side == OrderSide.Buy)
            {
                riskDistance = entry - stop;
                rewardDistance = target - entry;
            }
            else
            {
                riskDistance = stop - entry;
                rewardDistance = entry - target;
            }

            if (riskDistance <= MinRiskDistance || rewardDistance <= MinRiskDistance)
            {
                return 0m;
            }

            return Math.Round(rewardDistance / riskDistance, ProposalRScale, MidpointRounding.AwayFromZero);
        }

        private bool IsExactSignalMatch(LiveSignalEvidence signal, OrderSide side, decimal entry, decimal stop, decimal target)
        {
            if (signal == null)
            {
                return false;
            }

            var expectedSide = side == OrderSide.Buy ? "Buy" : "Sell";
            if (!string.Equals(signal.Side, expectedSide, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return ArePricesEquivalent(entry, signal.Entry)
                && ArePricesEquivalent(stop, signal.Stop)
                && ArePricesEquivalent(target, signal.Target);
        }

        private bool ArePricesEquivalent(decimal left, decimal right)
        {
            var normalizedLeft = NormalizePriceForContract(left);
            var normalizedRight = NormalizePriceForContract(right);
            return Math.Abs(normalizedLeft - normalizedRight) <= ProposalPriceMatchTolerance;
        }

        private bool IsReasonDataBacked(string reason, decimal entry, decimal stop, decimal target, LiveSignalEvidence matchedSignal, decimal rrMultiple)
        {
            var text = (reason ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (text.Length < 24 || text.Length > 220) return false;
            if (!IsSingleSentenceReason(text)) return false;

            var lower = text.ToLowerInvariant();
            var genericPhrases = new[]
            {
                "signal says",
                "math checks out",
                "looks good",
                "good enough",
                "positive expectancy",
                "higher win rate"
            };
            if (genericPhrases.Any(p => lower == p || lower.StartsWith(p + " ") || lower.EndsWith(" " + p)))
            {
                return false;
            }

            var hasNumber = lower.Any(char.IsDigit);
            var hasMetricToken = ContainsAny(lower,
                "vwap", "rsi", "atr", "expectancy", "win rate", "winrate", "sample", "price", "entry", "stop", "target", "rr", "2r", "1.5r");
            var hasRiskToken = ContainsAny(lower,
                "stop", "risk", "invalidate", "invalidates", "if broken", "r multiple", "rr", "loss");

            var referencesSignal = matchedSignal != null
                && !string.IsNullOrWhiteSpace(matchedSignal.Strategy)
                && lower.Contains(matchedSignal.Strategy.ToLowerInvariant());

            var referencesPlanPrice = ReferencesPrice(text, entry)
                || ReferencesPrice(text, stop)
                || ReferencesPrice(text, target);

            var referencesR = ReferencesRMultiple(text, rrMultiple);

            if (!(hasRiskToken && (hasNumber || hasMetricToken || referencesPlanPrice || referencesR || referencesSignal)))
            {
                return false;
            }

            return true;
        }

        private bool IsSingleSentenceReason(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (text.IndexOf('\n') >= 0 || text.IndexOf('\r') >= 0)
            {
                return false;
            }

            var trimmed = text.Trim();
            if (trimmed.Length == 0)
            {
                return false;
            }

            var sentenceTerminatorCount = trimmed.Count(ch => ch == '.' || ch == '!' || ch == '?');
            if (sentenceTerminatorCount > 1)
            {
                return false;
            }

            var semicolonCount = trimmed.Count(ch => ch == ';');
            if (semicolonCount > 1)
            {
                return false;
            }

            return true;
        }

        private bool ContainsAny(string value, params string[] terms)
        {
            if (string.IsNullOrWhiteSpace(value) || terms == null) return false;
            foreach (var term in terms)
            {
                if (string.IsNullOrWhiteSpace(term)) continue;
                if (value.Contains(term)) return true;
            }

            return false;
        }

        private bool ReferencesPrice(string reason, decimal price)
        {
            var text = (reason ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text) || price <= 0m) return false;

            var forms = new[]
            {
                price.ToString("0.##"),
                price.ToString("0.###"),
                price.ToString("0.####"),
                Math.Round(price, 0).ToString("0")
            };

            return forms.Any(f => !string.IsNullOrWhiteSpace(f) && text.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private bool ReferencesRMultiple(string reason, decimal rr)
        {
            var text = (reason ?? string.Empty).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(text) || rr <= 0m) return false;

            var rrRounded1 = Math.Round(rr, 1).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            var rrRounded2 = Math.Round(rr, 2).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            return text.Contains(rrRounded1 + "r") || text.Contains(rrRounded2 + "r") || text.Contains("rr") || text.Contains("r multiple");
        }

        private async Task<List<Candle>> GetCandlesSafeAsync(string productId, int granMinutes, DateTime startUtc, DateTime endUtc, string scope)
        {
            try
            {
                return await _client.GetCandlesAsync(productId, granMinutes, startUtc, endUtc) ?? new List<Candle>();
            }
            catch (Exception ex)
            {
                if (IsUnsupportedGranularityError(ex))
                {
                    var normalized = NormalizeGranularityMinutes(granMinutes);
                    if (normalized != Math.Max(1, granMinutes))
                    {
                        try
                        {
                            Util.Log.Warn($"[AutoPlanner] {scope} unsupported granularity '{granMinutes}m'. Retrying with '{normalized}m'.");
                            return await _client.GetCandlesAsync(productId, normalized, startUtc, endUtc) ?? new List<Candle>();
                        }
                        catch (Exception retryEx)
                        {
                            Util.Log.Error($"[AutoPlanner] {scope} retry failed after granularity normalization.", retryEx);
                            return new List<Candle>();
                        }
                    }
                }

                Util.Log.Error($"[AutoPlanner] {scope} candles fetch failed.", ex);
                return new List<Candle>();
            }
        }

        private bool IsUnsupportedGranularityError(Exception ex)
        {
            var cursor = ex;
            while (cursor != null)
            {
                var message = cursor.Message ?? string.Empty;
                if (message.IndexOf("Unsupported granularity", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                cursor = cursor.InnerException;
            }

            return false;
        }

        private int NormalizeGranularityMinutes(int minutes)
        {
            var requested = Math.Max(1, minutes);
            int[] supported = { 1, 5, 15, 60, 360, 1440 };

            if (Array.IndexOf(supported, requested) >= 0) return requested;

            var closest = supported[0];
            var bestDiff = Math.Abs(requested - closest);
            for (int i = 1; i < supported.Length; i++)
            {
                var diff = Math.Abs(requested - supported[i]);
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
            var t = NormalizeAiResponseText(text).ToLowerInvariant();

            if (IsClarificationOrAmbiguousResponse(t))
            {
                return null;
            }

            var fromInlineJson = ParseApproveFromInlineJson(text);
            if (fromInlineJson.HasValue) return fromInlineJson;

            var fromLabel = ParseApproveFromLabeledText(t);
            if (fromLabel.HasValue) return fromLabel;

            var hasApproveTrue = t.Contains("\"approve\": true") || t.Contains("approve: true");
            var hasApproveFalse = t.Contains("\"approve\": false") || t.Contains("approve: false");
            if (hasApproveTrue && !hasApproveFalse) return true;
            if (hasApproveFalse && !hasApproveTrue) return false;

            var hasApprovedWord = t.Contains("approved") || t.Contains("approve this trade");
            var hasRejectWord = t.Contains("reject") || t.Contains("veto") || t.Contains("do not approve");
            if (hasApprovedWord && !hasRejectWord) return true;
            if (hasRejectWord && !hasApprovedWord) return false;

            return null;
        }

        private bool? ParseApproveFromInlineJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            foreach (var candidate in EnumerateJsonCandidates(text))
            {
                try
                {
                    var parsed = UtilCompat.JsonDeserialize<AIResponse>(candidate);
                    if (parsed != null)
                    {
                        return parsed.Approve;
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private bool? ParseApproveFromLabeledText(string t)
        {
            if (string.IsNullOrWhiteSpace(t)) return null;
            var labelIdx = t.IndexOf("approve");
            if (labelIdx < 0) return null;

            var probe = t.Substring(labelIdx, Math.Min(80, t.Length - labelIdx));
            if (probe.Contains("false")) return false;
            if (probe.Contains("true")) return true;
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

            var approveSignals = 0;
            if (t.Contains("\"approve\": true") || t.Contains("approve: true") || t.Contains("approved")) approveSignals++;
            if (t.Contains("\"approve\": false") || t.Contains("approve: false") || t.Contains("reject") || t.Contains("veto")) approveSignals++;

            if (approveSignals > 1) return true;
            if (hasQuestionMark && approveSignals > 0) return true;

            return false;
        }
    }
}
