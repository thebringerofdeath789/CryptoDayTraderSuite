[← Back to Documentation Index](docs/index.md)
# Documentation Roadmap

This roadmap defines the plan to bring the repository's documentation up to a professional standard. This is a living document�update it as new modules are discovered.

## Phase 1: Discovery & Foundation (Current)
- [x] Analyze codebase entry points and architecture.
- [x] Create documentation file structure (`docs/`).
- [x] Write `README.md` and basic "What is this" overview.
- [x] Document configuration and secrets handling.
- [x] Map the "Startup Flow".

## Phase 2: Feature Deep Dives
- [x] **Strategy Documentation**: Detail the filtering logic of ORB, VWAP, and Donchian strategies (`docs/features/trading/Strategies.md`).
- [x] **AI Architecture**: Document the "Chrome Sidecar" CDP approach (`docs/architecture/AI_Integration_Plan.md`).
- [x] **ML Engine**: Explain how `PredictionEngine` learns and what the input features are.
- [x] **Exchange Connectivity**: Document how to add a new Broker adapter.
- [x] **Governance**: Document the trade planner blacklisting and losing streak rules.

## Phase 3: Developer Guides
- [x] **Exchange Client Details**: Deep dive into the Public vs Private client implementations (`docs/features/trading/Exchanges.md`).

## Phase 5: AI Integration (Complete)
- [x] **Transport Layer**: Implement `ChromeSidecar` for CDP/WebSocket communication.
- [x] **Governance**: Implement `AIGovernor` service and `MarketBias` logic.
- [x] **Integration**: Wire `ChromeSidecar` into `AutoPlanner` for trade review.
- [x] **Documentation**: Create User Guide for Chrome Sidecar workflow.

## Phase 8: Remediation & Hardening (Feb 2026)
- [x] **Backtester Allocations**: Fix O(N^2) memory bug in `Backtester.cs` using `int index` lookups.
- [x] **Architecture Cleanup**: Deprecate Static `AutoPlanner` and migrate logic to `AutoPlannerService` (DI).
- [x] **Dependency Repair**: Fix wiring in `AutoModeControl.cs` to use injected services.
- [x] **Security/Perf**: Implement Key/Client caching in `ExchangeProvider` to reduce decryption CPU usage.
- [x] **Resilience**: Add Retry Policies to `ExchangeProvider` for network stability.

## Phase 9: Modernization & De-Statification (In Progress)
- [x] **ML Configuration**: Externalize hardcoded learning rate/regularization in `PredictionEngine` (Fix AUDIT-0014).
- [x] **Service Migration**: Refactor static `AccountRegistry` to `AccountService` (DI).
- [x] **Service Migration**: Refactor static `KeyRegistry` to `KeyService` (DI).
- [x] **Service Migration**: Refactor static `ProfileStore` to `ProfileService` (DI).
- [x] **UI Wiring**: Update `MainForm` and Dialogs to use injected state services.

## Phase 10: Operational & Feature Expansion (Active)
- [x] **AI-Driven Strategy Enforcement**: Ensure `StrategyEngine` respects `AIGovernor` bias.
- [x] **Execution Optimization**: Implement `Smart-Limit` logic in `AutoPlanner`.
- [x] **Dashboard Enhancements**: Add Real-time PnL chart using `HistoryService`.

## Phase 11: Refinement & Tuning (Active)
- [x] **Theme Unification**: Refactor `DashboardControl` and others to use `Themes/Theme.cs` for consistent dark/light mode support.
- [x] **Logging Enhancements**: Improve `Util/Log.cs` to include full stack traces and structured context.
- [x] **Code Analysis**: Identify and remove unused methods and unify coding style.
- [x] **Resilience Verification**: Verify `ResilientExchangeClient` usage across all brokers.
- [x] **Strategy Optimization**: Implement "Chop-Block" (Choppiness Index Filter) for `VWAPTrendStrategy`.

## Phase 12: UI Refactor & Modernization (Complete)
- [x] **Backend Events**: Implement `StateChanged` events in `AIGovernor` and `ChromeSidecar`.
- [x] **Theme Enchancement**: Update `Theme.cs` with "Dark Graphite" palette (`#151717`, `#1E2026`).
- [x] **Component**: Build `SidebarControl` with expanding/collapsing logic.
- [x] **Component**: Build `GovernorWidget` for dashboard visualization.
- [x] **Feature**: Implement `ProfilesControl` for managing user profiles.
- [x] **Feature**: Implement `StrategyConfigDialog` using PropertyGrid.
- [x] **Shell**: Refactor `MainForm` to replace tabs with Sidebar + Content Panel architecture.

## Phase 13: AI Core Integration (Complete)
- [x] **Chrome Sidecar**: Implement WebSocket CDP client (`Services/ChromeSidecar.cs`) for Gemini/ChatGPT.
- [x] **AI Governor**: Create `AIGovernor` service to poll market bias every 15m.
- [x] **Trade Logic**: Integrate `GlobalBias` into `StrategyEngine` to block contradictory trades.
- [x] **Smart Limits**: Update `AutoPlannerService` to parse `SuggestedLimit` from AI response.

## Phase 14: Deep Clean & Quality Assurance (Complete)
- [x] **Data Binding**: Convert Models from Fields to Properties for WinForms compatibility.
- [x] **Architecture Fix**: Remove static `HistoryStore` and replace with `IHistoryService`.
- [x] **UI Policy**: Refactor `PlannerControl`, `AutoModeControl`, `TradeEditDialog` to use Designer files (`.Designer.cs`).
- [x] **Bug Fix**: Resolve `RateRouter` crash (Ticker.Price -> Ticker.Last).
- [x] **Code Audit**: Verify `AutoPlannerService` DI usage.

## Phase 15: Functional Simulation (Active)
- [x] **Backtest Integration**: Verify `BacktestService` respects Global Bias override (Implemented `biasOverride`).
- [x] **Resilience Test**: Simulate Chrome disconnection and ensure `AIGovernor` fails gracefully (Implemented Fail-Safe to Neutral).
- [x] **Event Bus**: Verify `LogEvent` propagation from background services to StatusControl (Implemented Global StatusStrip).
- [x] **Strategy Tuner**: Ensure `StrategyConfigDialog` updates live strategies (Updated Strategies to use Properties).
- [x] **AI Provider Expansion**: Extend Chrome Sidecar provider support to include `claude.ai` with governor/planner fan-out.
- [x] **Provider Load Rotation + Submit Robustness**: Rotate planner/governor non-strict sidecar queries across ChatGPT/Gemini/Claude with active provider switching, and harden Claude send behavior to avoid stop/cancel misclick no-send stalls.
- [x] **Verified AI Proposer (Planner/Auto Mode)**: Add optional AI-first proposal path that can suggest side/entry/stop/target per symbol, then enforce verification gates (strategy alignment, risk geometry, and global bias) before accepting.
- [x] **Auto Mode Non-Interactive Loop**: Implement one-toggle timed `Scan -> Propose -> Execute` automation with live-arm safety and kill switch in `UI/AutoModeControl`.
- [x] **Sidecar Window Visibility Controls**: Launch sidecar-managed Chrome minimized/optional hidden by default, with Settings panel UI controls to toggle hidden launch and show/hide the managed Chrome window instance at runtime.
- [x] **Coinbase Read-Only Account Import**: Automatically import Coinbase account linkage and read-only account/trade telemetry (holdings, balances, fees, fills, net profit estimate) on Coinbase key save and expose manual re-import in `AccountsControl`.
- [x] **Automatic Buying-Power Detection**: Resolve live quote buying power from installed account API keys (Coinbase/Kraken/Bitstamp) and use it for Planner/Auto Mode sizing with manual fallback.
- [ ] **Manual Simulation**: User to perform "Chrome Sidecar" live test according to `docs/ops/SIMULATION_INSTRUCTIONS.md`.

## Phase 18: Auto Mode Productionization - Dual Track Implementation (Complete)
Goal: fully implement unattended Auto Mode behavior without placeholders or partial paths, across both single-session automation and multi-broker/account segmentation.

### Track A - Non-Interactive Auto Loop Completion (Single Session)
- [x] **A1. Stabilize Auto Loop Lifecycle (`UI/AutoModeControl`)**
	- [x] Enforce exactly one active timer cycle at a time (no overlap/reentry).
	- [x] Persist `Auto Run` user intent across restart (sticky ON/OFF preference restore).
	- [x] Ensure kill switch is immediate, idempotent, and updates visible status/log state.
- [x] **A2. Replace Modal Runtime UX with Deterministic Status Flow**
	- [x] Remove repetitive normal-path message boxes from automated cycles.
	- [x] Keep modal dialogs only for explicit manual actions and critical interactive failures.
	- [x] Surface cycle outcomes using status text + structured logs (`scanned/proposed/executed/skipped/errors`).
- [x] **A3. Enforce Hard Risk/Cadence Guardrails in Execution Path**
	- [x] Enforce max trades per cycle before broker placement.
	- [x] Enforce per-symbol cooldown consistently across cycles.
	- [x] Enforce daily risk stop budget reset at UTC day rollover.
	- [x] Enforce account max concurrent positions (with explicit source of truth).
- [x] **A4. Protective Exit Enforcement (No Stub Paths)**
	- [x] Implement concrete protective-exit behavior for supported brokers (native bracket or local watchdog path).
	- [x] If a broker cannot support safe exits, fail closed with explicit reason and no entry placement.
	- [x] Remove/replace temporary rejection-only logic once safe execution path exists.
- [x] **A5. Verification & Reliability Gates**
	- [x] Add deterministic scenario checks for no-signal, AI veto, risk veto, and successful execution paths.
	- [x] Validate no unhandled exceptions from scan/propose/execute timer cycles.
	- [x] Confirm build/test commands pass with zero new warnings from Track A changes.
- [x] **A6. Adaptive Trade Lifecycle Management (Paper Runtime)**
	- [x] Merge same-direction fills for the same account/symbol into one managed paper position.
	- [x] Re-evaluate open paper positions on cycle cadence using fresh planner proposals.
	- [x] Tighten protective stop/target bounds when refreshed plans improve risk geometry.

### Track B - Multi-Broker / Multi-Account Pair Scope Orchestration
- [x] **B1. Introduce Auto Profiles (Per Account/Broker)**
	- [x] Define persisted profile model: `ProfileId`, `AccountId`, `Enabled`, `PairScope` (`All|Selected`), `SelectedPairs`, cadence/risk overrides.
	- [x] Add versioned persistence file with migration for missing/new fields.
	- [x] Validate profile-account binding and fail closed on missing/disabled account.
- [x] **B2. Build Auto Profile Management UX (Designer-First)**
	- [x] Add profile list with add/edit/delete/enable toggles in `AutoModeControl` designer-backed UI.
	- [x] Add profile editor interactions for pair scope (`All` vs custom checked set).
	- [x] Add per-profile summary row (broker, account label, pair count, mode, risk caps).
- [x] **B3. Multi-Profile Cycle Engine**
	- [x] Execute cycles per enabled profile in a stable deterministic order.
	- [x] Resolve symbols per profile (`All` products at runtime or explicit selected list).
	- [x] Isolate queue/proposal/execution state by profile (no cross-profile bleed).
	- [x] Aggregate cycle telemetry by profile and global summary.
- [x] **B4. Broker Routing & Capability Contract**
	- [x] Route execution strictly by account service/mode through `BrokerFactory`.
	- [x] Add capability checks per broker (supports market entry, supports protective exits, precision rules).
	- [x] Block unsupported broker/profile combinations with explicit actionable diagnostics.
- [x] **B5. End-to-End Validation Matrix**
	- [x] Add deterministic telemetry export artifact for matrix verification (`cycle_reports/*.json`).
	- [x] Add automatic matrix evaluator (`PASS|PARTIAL`) inside cycle telemetry for profile-configured pair scopes/counts + isolation checks.
	- [x] Extend matrix validator tooling with strict scenario switches (`-RequireMixedScopes`, `-RequireSelectedSymbolCounts`, `-RequireIndependentGuardrailConfigs`, `-RequireFailureIsolation`).
	- [x] Add one-command B5 scenario runner (`Util/run_b5_validation_scenario.ps1`) to seed mixed-scope profiles and execute strict matrix validation.
	- [x] Validate configurable scenario in one run (example: `3 pairs on Broker A`, `All pairs on Broker B`, `15 pairs on Broker C`).
	- [x] Validate independent caps/cooldowns/risk budgets per profile.
	- [x] Validate one failing profile does not stop other profiles from completing cycle.

### Phase 18 Definition of Done (Mandatory)
- [x] No scaffolding methods, no placeholder comments, no `NotImplementedException`, no simplified stub behavior in production paths.
- [x] All new UI surfaces are Designer-backed and editable in Visual Studio.
- [x] All feature behavior is documented in `docs/features/trading/AutoMode_Automation.md` and indexed in `docs/index.md`.
- [x] `docs/CHANGELOG.md` and `PROGRESS_TRACKER.md` updated with implementation and verification results for each completed subtask.
- [x] Build passes (`Debug`) with no new compile errors and no regression in existing auto/planner workflows.

## Phase 16: Deployment & Maintenance (Future)
- [ ] **Release Build**: create Release configuration artifacts.
- [ ] **Installer**: create simple zip/setup.

## Phase 17: Strategy & Risk Hardening Audit Remediation (Planned)
- [x] **AUDIT-0023 Risk Guard Stub Removal (Critical)**
	- [x] Replace legacy `RiskGuards.FeesKillEdge(...)` overload that always returns `false` with real logic or delete callsites and obsolete overload.
	- [x] Add compile-time deprecation marker and route all consumers to canonical notional-based overload.
	- [x] Add unit-level validation cases for fee-kill thresholds (edge < fees, edge ~= fees, edge >> fees).
- [x] **AUDIT-0024 Strategy Null/Empty Guards (Critical)**
	- [x] Update parameterless `GetSignal(List<Candle>)` wrappers in ORB/VWAP/RSI/Donchian to safely handle `null` and `Count == 0`.
	- [x] Enforce index bounds consistently before dereferencing in all strategy entrypoints.
	- [x] Validate behavior in backtest/paper/live flows to return "no signal" instead of exceptions.
- [ ] **AUDIT-0025 Donchian Routing Gap (Major)**
	- [ ] Extend `StrategyEngine.SetStrategy` and strategy enum mapping to support `Donchian 20` aliases.
	- [ ] Ensure UI strategy names and engine names are normalized to one canonical map.
	- [ ] Verify backtest/paper/live all execute Donchian when selected.
- [ ] **AUDIT-0026 Stop-Distance Safety (Major)**
	- [ ] Remove unsafe fallback `stopDistance = entry * 0.01m` in `StrategyEngine.Evaluate`.
	- [ ] Reject invalid strategy outputs (`stop == entry`, inverted risk geometry) before sizing.
	- [ ] Add explicit diagnostic logging for rejected invalid setup.
- [ ] **AUDIT-0027 Confidence Scale Normalization (Major)**
	- [ ] Define a single confidence contract (recommended: `0..1` decimal or `0..100` double) in `StrategyResult` documentation.
	- [ ] Normalize ORB/VWAP/Donchian and RSI outputs to the same scale.
	- [ ] Update any planner ranking logic that consumes confidence to use the canonical scale.
- [ ] **AUDIT-0028 Indicator Duplication & Divergence (Major)**
	- [ ] Consolidate feature math to `Strategy/Indicators.cs` as single source of truth.
	- [ ] Remove duplicate private ATR/RSI/SMA/TR implementations in `FeatureExtractor`.
	- [ ] Align flat-market RSI behavior (no gain/no loss) to one agreed output.
- [ ] **AUDIT-0029 Choppiness Guardrails (Major)**
	- [ ] Add parameter validation in `ChoppinessIndex` (`period > 1`, finite/log-safe paths).
	- [ ] Clamp/handle invalid values to prevent NaN/Infinity propagation.
	- [ ] Add edge-case tests for constant-price and tiny-window inputs.
- [ ] **AUDIT-0030 Dead Config & Unused Path Cleanup (Minor)**
	- [ ] Remove or implement unused VWAP strategy knobs (`LookbackMinutes`, `PullbackMultipleATR`).
	- [ ] Confirm usage of `ObjectiveScorer` and remove if orphaned.
	- [ ] Resolve uncalled prediction hook path (`AnalyzeAndPlanAsync`) by wiring or decommissioning.
- [ ] **AUDIT-0031 Rolling Indicator Performance Pass (Minor)**
	- [ ] Profile `AutoPlannerService.ProjectAsync` repeated indicator recomputation.
	- [ ] Introduce rolling/cached indicator arrays for ATR/RSI/SMA where safe.
	- [ ] Validate no behavior drift against current backtest outputs.
- [ ] **AUDIT-0032 Backtester Exit Logic Realism (Major)**
	- [ ] Update `Backtester.Run` to honor strategy stop/target semantics instead of exit-only-on-opposite-signal.
	- [ ] Add deterministic intrabar fill assumptions (high/low hit ordering policy).
	- [ ] Reconcile backtest metrics with live execution assumptions to avoid expectancy inflation.
- [ ] **AUDIT-0033 Max Drawdown Computation Correctness (Major)**
	- [ ] Replace global `peak/trough` shortcut with rolling peak-to-subsequent-trough MDD calculation.
	- [ ] Add regression cases where trough occurs before a later higher peak.
- [ ] **AUDIT-0034 Online ML Lifecycle Wiring (Major)**
	- [ ] Wire prediction generation (`AnalyzeAndPlanAsync`) into a real user/workflow trigger or decommission path.
	- [ ] Implement `PredictionEngine.Learn(...)` ingestion on realized outcomes so model is not inference-only.
	- [ ] Add guardrails for feature validity before scoring/training.
- [ ] **AUDIT-0035 Feature Error Sentinel Handling (Major)**
	- [ ] Replace dictionary sentinel key (`error`) with explicit success/failure result contract.
	- [ ] Prevent invalid feature dictionaries from entering prediction scoring/training.
- [ ] **AUDIT-0036 Reflection-based Planner Invocation Cleanup (Minor)**
	- [ ] Replace menu reflection (`GetMethod("btnPlanner_Click")`) with explicit command/event wiring.
	- [ ] Remove duplicate planner menu path once unified.
- [ ] **AUDIT-0037 Strategy Config Surface Alignment (Major)**
	- [ ] Add Donchian strategy to all strategy configuration UI surfaces.
	- [ ] Ensure all editable strategies in `Program.cs`/`TradingControl` are present in config dialogs.
- [ ] **AUDIT-0038 History Parsing Hardening (Major)**
	- [ ] Harden CSV parse paths in `HistoryService` against malformed rows (TryParse + row-level skip/log).
	- [ ] Preserve load continuity when one record is corrupted.
- [ ] **AUDIT-0039 Protective Exit Enforcement for Live Orders (Critical)**
	- [ ] Introduce explicit protective-exit handling (stop/target) for live/paper execution paths.
	- [ ] Ensure strategy `StopLoss`/`TakeProfit` are not discarded when converting signal to executable orders.
	- [ ] Define fallback behavior if exchange does not support native bracket orders (local watchdog + cancel/flatten policy).
- [ ] **AUDIT-0040 Sidecar Reconnect Lifecycle (Major)**
	- [ ] Add automatic reconnect attempts in governor loop when sidecar is disconnected.
	- [ ] Add backoff and bounded retry intervals to avoid hot-loop reconnect storms.
	- [ ] Surface reconnect status in UI/logs with explicit degraded-mode state.
- [ ] **AUDIT-0041 Sidecar Status State-Machine Integrity (Major)**
	- [ ] Ensure `ConnectAsync` sets terminal status (`Disconnected`/`Error`) on all early failure paths.
	- [ ] Normalize transitions to prevent sticky `Connecting` state.
- [ ] **AUDIT-0042 CDP Response Correlation Safety (Major)**
	- [ ] Replace naive `ReceiveAsync` single-read behavior with request-id correlated pending-task map.
	- [ ] Ignore unrelated event frames and parse only matching response ids.
- [ ] **AUDIT-0043 Sidecar Timeout/Cancellation Hardening (Major)**
	- [ ] Add cancellation/timeouts for `ConnectAsync`, `EvaluateJsAsync`, and receive loops.
	- [ ] Fail fast with actionable error messages rather than indefinite waits.
- [ ] **AUDIT-0044 Prompt Injection Escaping Robustness (Major)**
	- [ ] Replace ad-hoc string escaping in JS injection with safe serializer-based embedding.
	- [ ] Handle quotes, backslashes, CRLF, and unicode edge cases consistently.
- [x] **AUDIT-0045 Order Quantity Constraints & Venue Precision (Major)**
	- [x] Add per-venue min-size and increment validation before submit.
	- [x] Reject/adjust orders that violate instrument constraints and emit clear diagnostics.
- [ ] **AUDIT-0046 Auto Mode Execute Stub in Live Control (Major)**
	- [ ] Replace `AutoModeControl.DoExecute` placeholder with full broker execution flow parity.
	- [ ] Ensure sidebar Auto page behavior matches AutoModeForm behavior.
	- [ ] Add clear state transitions (queued/executed/failed) in UI.
- [ ] **AUDIT-0047 CoinbaseExchangeBroker Invocation Contract Break (Critical)**
	- [ ] Remove reflection-based constructor/signature assumptions and call `IExchangeClient` directly.
	- [ ] Fix broker to build valid `OrderRequest` instead of unsupported ad-hoc signatures.
	- [ ] Verify successful live-paper parity path for `coinbase-exchange` service.
- [ ] **AUDIT-0048 Key Active-ID Semantics Mismatch (Critical)**
	- [ ] Fix key activation to use canonical id format (`Broker|Label`) consistently.
	- [ ] Correct `KeyEditDialog` active-set call to avoid embedding UUID as label segment.
	- [ ] Add migration/repair for previously saved malformed active ids.
- [ ] **AUDIT-0049 KeyData Field Loss for API Secret Variants (Critical)**
	- [ ] Preserve `ApiSecretBase64` and advanced key fields in `KeyInfo.Data` round-trips.
	- [ ] Ensure load/save/edit paths read/write identical key fields.
	- [ ] Add compatibility mapping for legacy `Secret` vs `ApiSecretBase64` payloads.
- [ ] **AUDIT-0050 Duplicate Auto Mode Surfaces Drift (Major)**
	- [ ] Consolidate `AutoModeForm` and `AutoModeControl` into single execution surface or shared service.
	- [ ] Remove divergent logic paths to prevent future feature skew.
- [ ] **AUDIT-0051 EventBus Handler Collection Concurrency (Major)**
	- [ ] Replace mutable `List<object>` handler buckets with thread-safe immutable snapshot or concurrent collection.
	- [ ] Eliminate lock-free enumeration over mutable list instances.

## Phase 19: Multi-Exchange Profit Architecture (Planned)
Goal: move from single-venue execution to multi-venue edge capture by combining spread divergence, funding carry, and liquidity-aware routing.

- [x] **P19.0 Planning Baseline**
	- [x] Add architecture blueprint (`docs/features/trading/MultiExchange_Profit_Architecture.md`).
	- [x] Add detailed implementation master plan (`docs/features/trading/MultiExchange_Implementation_MasterPlan.md`).

- [ ] **M1. Multi-Venue Market Data Normalization**
	- [x] Add normalized multi-venue quote contracts (`VenueQuoteSnapshot`, `CompositeQuote`, `VenueHealthSnapshot`) in `Models/MarketData.cs`.
	- [x] Add `Services/MultiVenueQuoteService.cs` for composite quote aggregation across currently integrated venues (Coinbase/Kraken/Bitstamp) with staleness + RTT capture.
	- [x] Upgrade `Services/RateRouter.cs` to consume composite quotes with provenance/confidence and fallback behavior.
	- [x] Add `Services/VenueHealthService.cs` and wire quote success/stale/error sample tracking.
	- [x] Add broker-name canonicalization aliases in `Services/ExchangeProvider.cs` for Coinbase Advanced naming variants.
	- [ ] Extend service-layer quote aggregation for per-venue normalized top-of-book snapshots.
	- [ ] Add quote staleness and round-trip latency metrics for routing confidence.
	- [ ] Emit cross-venue spread table telemetry for configured venue pairs.
- [ ] **M2. Opportunity Detection Engine**
	- [ ] Add cross-exchange divergence detector with threshold based on fees + slippage budget.
	- [ ] Add funding-rate carry detector with basis stability guard.
	- [ ] Emit deterministic reject reasons (`fees-kill`, `stale-quote`, `insufficient-depth`, `latency-risk`).
- [ ] **M3. Smart Venue Routing**
	- [ ] Introduce venue scoring by expected net edge (fill quality, fee drag, funding carry, reliability).
	- [ ] Route executable plans to lowest expected-slippage compliant venue.
	- [ ] Add bounded fallback venue switching on health degradation.
- [ ] **M4. Regime-Aware Strategy Activation**
	- [ ] Gate strategy families by volatility/volume/funding regime state.
	- [ ] Disable always-on execution in mismatch regimes.
	- [ ] Surface explicit regime-block diagnostics in logs/telemetry.
- [ ] **M5. Validation & Runtime KPIs**
	- [x] Add one-command certification runner (`Util/run_multiexchange_certification.ps1`) with timestamped PASS/PARTIAL/FAIL JSON/TXT evidence artifacts.
	- [x] Add certification contract self-validation fields (`CI_VERSION`, `CI_FIELDS`, `CI_SUMMARY`) and strict decision telemetry for parser-safe automation routing.
	- [x] Add standalone contract-check harness (`Util/check_multiexchange_contract.ps1`) to assert required stdout contract keys for each run profile.
	- [x] Harden reject-evidence orchestration retries for provider probe + certification invocations in `obj/run_reject_evidence_capture.ps1`.
	- [ ] Extend runtime snapshot/report tooling with multi-venue opportunity and routing metrics.
	- [ ] Track realized vs expected slippage by venue, maker/taker ratio, and funding carry contribution.
	- [ ] Document production runbook for multi-venue verification and failover drills.

### Phase 19 Blueprint Completion Tasks (Detailed)
- [ ] **B19.1 Strategy ↔ Exchange Matching Matrix (Production Rules)**
	- [ ] Encode strategy-to-venue preference matrix for HFT Scalping, VWAP Trend, ORB, Funding Carry, and Cross-Exchange Divergence.
	- [ ] Add explicit enable/disable condition contracts per strategy family (works/fails states) in service-layer config models.
	- [ ] Add monitorable regime inputs for strategy activation (`ATR`, volume expansion, depth imbalance, funding, basis stability, latency).
	- [ ] Require per-strategy deterministic reject reason when blocked by regime or venue health.
- [ ] **B19.2 Fee/Slippage Expectancy Guardrails**
	- [x] Add canonical expectancy gate `E = (W*AvgWin) - (L*AvgLoss) - Fees - Slippage` in planner pre-trade validation.
	- [ ] Add maker/taker-aware fee model hooks per venue and enforce fee-tier aware net-edge checks.
	- [x] Block proposals when projected edge is fully consumed by fees/slippage (`fees-kill`/`slippage-kill`).
	- [x] Emit telemetry fields for expected gross edge, fee drag, slippage budget, and final net edge.
- [ ] **B19.3 Regime Classification Engine**
	- [ ] Add service-driven regime classification states (`expansion`, `compression`, `funding-extreme`, `trend`, `mean-reversion`).
	- [ ] Bind strategy families to allowed regimes and enforce no-trade outside allowed regime map.
	- [ ] Add explicit runtime diagnostics for regime state and strategy enable/disable rationale.
- [ ] **B19.4 Role-Based Venue Stack Orchestration**
	- [ ] Implement role-driven venue mapping (`anchor liquidity`, `US anchor`, `derivatives engine`, `divergence leg`) independent of exchange brand constants.
	- [ ] Support dynamic routing across mandatory Phase 1 venues (Binance, Coinbase Advanced, Bybit, OKX, Kraken) with role fallback.
	- [ ] Add configuration validation to prevent Auto Mode start when required role coverage is missing.
- [ ] **B19.5 Architecture Layer Contracts (SystemMap Alignment)**
	- [ ] Enforce market-data normalization contract (parallel feeds, unified quote/book schema, staleness/latency fields).
	- [ ] Keep regime and strategy routing logic in services/strategy layers only (no UI domain logic).
	- [ ] Enforce execution-engine maker-first preference, bounded failover, and protective-exit guarantee.
	- [ ] Enforce risk-engine controls for max daily loss, concurrent exposure caps, outage detection, and kill-switch policy.

### Phase 19 Delivery Sequence (Execution Tracking)
- [ ] **P19.1 Contracts + Data Layer**
	- [ ] Finalize normalized quote/book and venue-health contracts.
- [ ] **P19.2 Adapters + Provider Expansion**
	- [ ] Complete mandatory venue adapters and provider capability wiring.
	- [ ] Add mandatory provider/public API verification phase (per-exchange `CreatePublicClient`, product discovery, ticker probe, and pass/fail evidence artifact).
- [ ] **P19.3 Opportunity + Routing Core**
	- [ ] Complete divergence/funding detectors, scorer, and smart routing path.
- [ ] **P19.4 UI Surface + Config**
	- [ ] Complete designer-backed multi-venue controls and runtime visibility.
- [ ] **P19.5 Guardrails + Operations**
	- [x] Add venue circuit-breaker state tracking in `VenueHealthService` (error/stale/latency streak thresholds with timed re-enable windows).
	- [x] Add safe-set-empty fail-closed routing halt (`safe-set-empty-circuit-breaker`) in `AutoPlannerService` when all candidate venues are circuit-broken.
	- [ ] Complete circuit breakers, journaling, and deterministic verification tooling.
- [ ] **P19.6 Paper-to-Live Cutover**
	- [ ] Complete staged exposure ramp and 28-day live-proof promotion gate.

### Phase 19 Workstream A: Additions (New Components)
- [ ] Add `Services/MultiVenueQuoteService.cs` for normalized quote/book snapshots.
- [ ] Add `Services/VenueHealthService.cs` for freshness/error/latency health scoring.
- [ ] Add `Services/SpreadDivergenceDetector.cs` for cross-venue net-edge opportunities.
- [ ] Add `Services/FundingCarryDetector.cs` for funding/basis opportunities.
- [ ] Add `Services/ExecutionVenueScorer.cs` and `Services/SmartOrderRouter.cs` for venue selection and failover.
- [ ] Add `Services/OrderIntentJournalService.cs` for idempotent recovery/state replay.
- [ ] Add exchange clients/brokers for Binance, Bybit, and OKX via `Exchanges/*Client.cs` and `Brokers/*Broker.cs`.
	- [x] Implement `Exchanges/BinanceClient.cs` with signed Binance spot list/ticker/candles/fees/order/cancel paths.
	- [x] Implement `Exchanges/BybitClient.cs` with signed Bybit v5 spot list/ticker/candles/fees/order/cancel paths.
	- [x] Implement `Exchanges/OkxClient.cs` with signed OKX spot list/ticker/candles/fees/order/cancel paths.
	- [x] Implement `Brokers/BinanceBroker.cs`, `Brokers/BybitBroker.cs`, and `Brokers/OkxBroker.cs` with credential resolution, plan validation, place, and cancel-all flows.
	- [x] Implement `Services/SpreadDivergenceDetector.cs` with deterministic reject reasons (`fees-kill`, `slippage-kill`, `stale-quote`, `latency-risk`, `insufficient-depth`) and net-edge filtering.
	- [x] Implement `Services/FundingCarryDetector.cs` with basis-stability gating and carry opportunity ranking.
	- [x] Implement `Services/ExecutionVenueScorer.cs` and `Services/SmartOrderRouter.cs` for expected-net-edge ranking and bounded fallback selection.

### Phase 19 Workstream B: Conversions (Existing Components)
- [ ] Convert `Program.cs` DI graph from single-public-client planner wiring to multi-venue service graph.
- [ ] Convert `Services/ExchangeProvider.cs` factory/capability handling to support Binance/Bybit/OKX.
- [ ] Add provider-level verification orchestration (`ExchangeProviderAuditService`) to validate each exchange public API contract before planner/routing certification.
- [ ] Convert `Services/RateRouter.cs` from single-source Coinbase rates to composite multi-venue rates with provenance/staleness.
- [ ] Convert `Services/AutoPlannerService.cs` orchestration from one-client assumptions to detector/router-fed opportunities.
- [ ] Convert `Strategy/RiskGuards.cs` to enforce multi-venue exposure and quote-freshness constraints.
	- [x] Extend `Services/ExchangeProvider.cs` normalization/factory to support Binance/Bybit/OKX clients.
	- [x] Extend broker factory in `UI/AutoModeForm.cs` to return Binance/Bybit/OKX brokers.
	- [x] Extend account/key service selectors to include Binance/Bybit/OKX.

### Phase 19 Workstream C: Removals / Decommissioning
- [ ] Remove hardcoded single-venue assumptions in planner/rate paths when multi-venue config is active.
- [ ] Remove duplicate ad-hoc routing logic in UI handlers once `SmartOrderRouter` is canonical.
- [ ] Remove any entry execution path that cannot guarantee protective exits.

### Phase 19 Workstream D: UI (Designer-First)
- [ ] Add Auto Mode designer sections for venue stack, routing policy, and funding-capture controls.
- [x] Add Auto Mode runtime status rows for chosen venue, degraded venues, and circuit-breaker state.
- [x] Add venue-specific credential templates in account/key dialogs.
- [x] Enforce per-exchange required credential fields in account/key save flows (research-backed matrix by venue API auth model).
- [ ] Add planner/dashboard visibility for routing rationale and venue health counters.
- [ ] Implement Auto Mode left-rail card decomposition (Profile, Venue Stack, Routing Policy, Funding, Guardrails) with fixed header/footer telemetry bands.
- [ ] Add Planner grid diagnostics columns (`chosen venue`, `alternates`, `expected net edge`, `fee drag`, `slippage budget`, `reject reason`).
- [ ] Add Dashboard compact multi-venue cards (venue health, spread/funding opportunity counts, maker/taker ratio, expected-vs-realized slippage).
- [ ] Standardize cross-surface inline status semantics (`success`, `warn`, `neutral`, `error`) and timestamped freshness labels.
- [ ] Eliminate non-critical modal prompts from Auto/Planner normal paths in favor of inline status + logs.
- [ ] Preserve designer editability for all changed forms/controls; no static control construction for fixed layout surfaces.

### Phase 19 Workstream E: Set-and-Forget Reliability
- [ ] Add per-venue and global circuit-breaker rules (API failure, stale quotes, slippage breach streak).
- [ ] Add bounded reconnect/backoff and controlled venue re-enable policy.
- [ ] Add global kill-switch automation when safe venue set is empty.
- [ ] Extend cycle telemetry with expected-vs-realized slippage and opportunity capture outcomes.

### Phase 19 Decision Locks (Confirmed)
- [ ] Enforce all mandatory exchanges in Phase 1 scope (Binance, Coinbase Advanced, Bybit, OKX, Kraken); no optional venues.
- [ ] Enforce Phase 1 product scope as Spot + Perps (sequenced rollout allowed, both required for completion).
- [ ] Add UI-selectable risk budgeting mode (`Unified` vs `Per-venue + Global cap`) with default `Per-venue + Global cap`.
- [ ] Enforce strict trade-level telemetry completeness gate (missing/stale required data => no-trade).
- [ ] Set execution policy default to maker-preferred with opportunity-aware taker fallback.
- [ ] Set failover policy defaults: Auto Mode => automatic failover on; Manual Mode => approval required.
- [ ] Implement 28-day live-proof promotion gate before unattended scale-up.

### Phase 19 Validation Matrix (Mandatory)
- [ ] Implement and run full exchange adapter certification matrix across all mandatory venues.
- [ ] Implement and run provider/public API contract verification for each mandatory exchange before adapter/live certification.
- [ ] Implement and run strategy backtesting matrix across all strategies and mandatory venues with venue-specific fee/slippage/funding models.
- [ ] Implement and run per-exchange account/key credential requirement certification (required fields, validation messages, and save-path enforcement).
- [ ] Add one-command certification runner and deterministic PASS/PARTIAL/FAIL artifact outputs.

### Phase 19 Gap-Closure Addendum (Comprehensive Audit 2026-02-16)
- [ ] **G19.1 Data Contract Completion (Part 1/4 Inputs)**
	- [ ] Extend `Exchanges/IExchangeClient.cs` contracts to support normalized market-structure inputs required by strategy gating (depth/order-book imbalance, funding snapshot inputs, open-interest snapshot inputs, basis inputs, options IV where applicable).
	- [ ] Extend `Models/MarketData.cs` with normalized DTOs for the above inputs and explicit staleness metadata.
	- [ ] Add deterministic "input unavailable" reject reasons in planner/routing flows when required market-structure data is missing.
- [ ] **G19.2 Perps/Product Coverage Completion (Phase-1 Scope Lock)**
	- [ ] Implement and certify required perpetual-product market-data/execution paths for mandatory venues where supported.
	- [ ] Enforce spot+perps readiness gates in certification so Phase 1 is not marked complete from spot-only coverage.
- [x] **G19.3 Strategy × Exchange Rule Matrix Runtime Enforcement**
	- [x] Implement a service-layer strategy-to-exchange policy matrix with explicit enable/avoid contracts (not docs-only) and deterministic reject diagnostics.
	- [x] Encode production ranking policies by strategy family (HFT scalping, swing, funding capture, cross-exchange arb) as configurable policy, then emit selected-policy rationale in telemetry.
- [x] **G19.4 Fee/Slippage/Execution Mode Realism Upgrade**
	- [x] Extend cost modeling to include maker/taker mode selection, fee tiers/rebates, and venue-specific round-trip assumptions.
	- [x] Align backtest and runtime cost models to avoid expectancy inflation from simplified friction assumptions.
- [x] **G19.5 Funding Carry Runtime Wiring Completion**
	- [x] Wire `FundingCarryDetector` into planner/runtime orchestration with live `FundingRateSnapshot` ingestion and explicit candidate->executed->realized carry telemetry.
	- [x] Add fail-closed behavior when funding-carry strategy is enabled but funding/basis inputs are stale or unavailable.
- [x] **G19.6 Evidence-Backed Certification Matrix (No Synthetic Rows)**
	- [x] Replace synthetic global matrix row generation in `Util/run_multiexchange_certification.ps1` with per strategy×exchange evidence derived from runtime/backtest artifacts.
	- [x] Require explicit artifact references for each matrix row status (`PASS`/`PARTIAL`/`FAIL`) and fail certification when evidence is missing.
- [ ] **G19.7 Documentation Reality Alignment (Code Is Source of Truth)**
	- [ ] Reconcile stale/contradictory docs to current code (including README exchange scope, architecture decommission references, and risk doc file references).
	- [ ] Split roadmap-facing docs into explicit `Implemented` vs `Target` states so planning docs cannot be misread as completed runtime capability.

### Phase 19 Gap-Closure Sequenced Execution Checklist (Owners / Files / Acceptance)
- [ ] **GC19.1 Market-Structure Contracts (executes G19.1)**
	- **Owner**: Architecture + Exchanges
	- **Primary Files**: `Exchanges/IExchangeClient.cs`, `Models/MarketData.cs`, `Services/MultiVenueQuoteService.cs`, `Services/AutoPlannerService.cs`, `Services/SpreadDivergenceDetector.cs`
	- **Acceptance**:
		- [ ] `IExchangeClient` exposes required normalized inputs used by strategy/routing gates (depth/imbalance, funding, OI, basis, IV where supported).
		- [ ] Planner/routing emit deterministic `input-unavailable`/stale reject reasons when required contracts are missing at decision time.
		- [ ] Build passes and no single-venue fallback path bypasses required input gates.
- [ ] **GC19.2 Spot+Perps Coverage Completion (executes G19.2)**
	- **Owner**: Exchanges + Brokers
	- **Primary Files**: `Exchanges/BinanceClient.cs`, `Exchanges/BybitClient.cs`, `Exchanges/OkxClient.cs`, `Exchanges/CoinbaseExchangeClient.cs`, `Exchanges/KrakenClient.cs`, `Brokers/*Broker.cs`, `Services/ExchangeProvider.cs`
	- **Acceptance**:
		- [ ] Mandatory venues pass spot and perp capability checks where venue supports both.
		- [x] Coinbase broker/client now enforce symbol-level quantity step/min/max and min-notional constraints from `/products` metadata.
		- [x] Certification cannot report Phase 1 complete from spot-only coverage.
		- [x] Unsupported perp paths fail closed with explicit diagnostics.
- [x] **GC19.3 Strategy×Exchange Runtime Policy Matrix (executes G19.3)**
	- **Owner**: Services + Strategy
	- **Primary Files**: `Services/AutoPlannerService.cs`, `Services/StrategyExchangePolicyService.cs`, `Program.cs`
	- **Acceptance**:
		- [x] Runtime uses an explicit strategy↔exchange policy matrix (not docs-only guidance).
		- [x] Every blocked candidate includes deterministic reject code for policy/regime/health mismatch.
		- [x] Telemetry includes selected policy rationale per emitted proposal.
- [x] **GC19.4 Fee/Slippage Realism and Parity (executes G19.4)**
	- **Owner**: Services + Backtest
	- **Primary Files**: `Services/ExecutionCostModelService.cs`, `Services/ExecutionVenueScorer.cs`, `Services/AutoPlannerService.cs`, `Services/BacktestService.cs`, `Program.cs`
	- **Acceptance**:
		- [x] Maker/taker mode, tier/rebate hooks, and venue round-trip assumptions are modeled in both planning and scoring paths.
		- [x] Backtest friction model matches runtime cost-model assumptions (documented and auditable).
		- [x] Net-edge veto behavior remains deterministic under updated cost model.
- [x] **GC19.5 Funding Carry Runtime Wiring (executes G19.5)**
	- **Owner**: Services + Composition
	- **Primary Files**: `Program.cs`, `Services/FundingCarryDetector.cs`, `Services/AutoPlannerService.cs`, `UI/AutoModeControl.cs`, `Models/MarketData.cs`, `Services/HistoryService.cs`
	- **Acceptance**:
		- [x] Funding-carry opportunities are sourced from live snapshots and evaluated in planner/runtime flow.
		- [x] Candidate→executed→realized funding attribution is emitted in telemetry artifacts.
		- [x] Funding strategy fails closed when required funding/basis inputs are stale/missing.
- [x] **GC19.6 Evidence-Backed Certification Rows (executes G19.6)**
	- **Owner**: Ops Tooling
	- **Primary Files**: `Util/run_multiexchange_certification.ps1`, `docs/ops/MultiExchange_Certification_Runner.md`, `docs/ops/MultiExchange_Certification_Matrix.md`
	- **Acceptance**:
		- [x] Strategy×exchange row status is derived from per-row artifacts, not global matrix status projection.
		- [x] Each row includes explicit evidence reference in JSON output; missing evidence forces `PARTIAL`/`FAIL` per strict mode.
		- [x] Strict mode fails when mandatory row evidence is missing.
- [ ] **GC19.7 Documentation Reality Alignment (executes G19.7)**
	- **Owner**: Docs + Architecture
	- **Primary Files**: `README.md`, `docs/architecture/SystemMap.md`, `docs/features/trading/RiskManagement.md`, `docs/index.md`, `docs/CHANGELOG.md`
	- **Acceptance**:
		- [ ] Stale references to decommissioned files/services are removed or corrected.
		- [ ] Multi-exchange docs clearly separate current implementation state vs target architecture state.
		- [ ] `docs/index.md` reflects updated document intent and ownership.

## Phase X: Repo wide Bug Fix and Hardening (post audit)
- [ ] **Iteration 1 - Brokers folder file-by-file audit subtasks**
	- [ ] Audit `Brokers/IBroker.cs` contract usage, async validation parity, and call-site assumptions.
	- [ ] Audit `Brokers/BrokerFactory.cs` service normalization/fallback handling and unsupported-service fail behavior.
	- [ ] Audit `Brokers/CoinbaseExchangeBroker.cs` account-key resolution, cancel scope, and error classification consistency.
	- [ ] Audit `Brokers/BinanceBroker.cs` account-scoped credential resolution and service-alias fallback parity.
	- [ ] Audit `Brokers/BybitBroker.cs` account-scoped credential resolution and service-alias fallback parity.
	- [ ] Audit `Brokers/OkxBroker.cs` account-scoped credential resolution and service-alias fallback parity.
	- [ ] Audit `Brokers/PaperBroker.cs` validation parity and simulation-path invariants.
- [ ] **Iteration 2 - Exchanges folder file-by-file audit subtasks**
	- [ ] Audit `Exchanges/IExchangeClient.cs` for contract completeness vs runtime callers.
	- [ ] Audit `Exchanges/CoinbaseExchangeClient.cs`, `Exchanges/BinanceClient.cs`, `Exchanges/BybitClient.cs`, and `Exchanges/OkxClient.cs` for auth signature handling, timeout/cancellation behavior, and parser hardening.
	- [ ] Audit `Exchanges/KrakenClient.cs`, `Exchanges/BitstampClient.cs`, and `Exchanges/CoinbasePublicClient.cs` for public/private path separation and stale-data guardrails.
- [ ] **Iteration 3 - Services execution/risk slice file-by-file audit subtasks**
	- [ ] Audit `Services/AutoPlannerService.cs` risk/guardrail enforcement ordering and deterministic reject-code emission.
	- [ ] Audit `Services/AccountBuyingPowerService.cs` live-balance fallback safety and per-account cache isolation.
	- [ ] Audit `Services/ExchangeProvider.cs`, `Services/ExecutionVenueScorer.cs`, and `Services/ExecutionCostModelService.cs` for routing realism and fail-closed behavior.
	- [ ] Audit `Services/HistoryService.cs` persistence safety and malformed-record resilience.
- [ ] **Iteration 4 - UI orchestration file-by-file audit subtasks**
	- [ ] Audit `UI/AutoModeControl.cs` for live-arm gating, execute/propose parity, and profile isolation behavior.
	- [ ] Audit `UI/PlannerControl.cs` for pre-trade gating and broker capability enforcement parity.
	- [ ] Audit `MainForm.cs` and `Program.cs` for composition-root DI integrity and duplicate execution-surface drift.

### Phase X Consolidated Backlog (BUG-101 to BUG-117)
- [ ] **Wave 1 - Critical Execution Safety (Must Fix First)**
	- [ ] **BUG-101**: Add account-scoped cancel contract and remove active-key fallback ambiguity in broker cancel-all flows.
	- [ ] **BUG-109**: Pass actual runtime/global bias into strategy-policy evaluation (remove hardcoded neutral bias in planner policy gate).
	- [ ] **BUG-114**: Retire or hard-gate legacy `MainForm` live submit path so live execution always routes through broker capability/live-arm protections.
- [ ] **Wave 2 - Major Runtime Integrity**
	- [ ] **BUG-105**: Partition symbol-constraint caches by venue base-url/context to prevent cross-region contamination.
	- [ ] **BUG-106**: Replace Kraken hardcoded product list with live discovery.
	- [ ] **BUG-107**: Replace culture-sensitive decimal conversion in Bitstamp client with invariant parse guards.
	- [ ] **BUG-110**: Convert AI review exception path to deterministic fail-closed policy (or explicit operator-ack mode).
	- [ ] **BUG-111**: Add active-key fallback path in buying-power resolution when `Account.KeyEntryId` is blank.
	- [ ] **BUG-112**: Add synchronization around `ExchangeProvider` authenticated client cache.
	- [ ] **BUG-115**: Unify Planner vs Auto protective-exit policy behavior.
	- [ ] **BUG-116**: Unify key-resolution semantics in Auto Mode pre-gates with broker/service key fallback behavior.
	- [ ] **BUG-117**: Remove hardcoded Coinbase public-client wiring for Planner/Auto initialization and route by selected account/service context.
- [ ] **Wave 3 - Contract/Docs/Observability Closure**
	- [ ] **BUG-102**: Trim/normalize broker service input in factory routing.
	- [ ] **BUG-103**: Add Binance alias active-key fallback parity (`binance`/`binance-us`/`binance-global`).
	- [ ] **BUG-104**: Update broker docs to match actual async interface/factory source.
	- [ ] **BUG-108**: Align exchange abstraction contract with runtime requirements (balances/constraints/cancel-all capability surface).
	- [ ] **BUG-113**: Add malformed-row telemetry in history parsing (retain continuity but make drops observable).


