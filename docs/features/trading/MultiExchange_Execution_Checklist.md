# Multi-Exchange Execution Checklist (Owner + File + Acceptance)

[← Back to Documentation Index](../../index.md)

## Purpose

Provide a strict, implementation-ready checklist for Phase 19 that maps each work package to:

- Owner role
- Primary file surface
- Concrete implementation tasks
- Explicit acceptance criteria and evidence

This checklist operationalizes the architecture and master plan artifacts:

- `docs/features/trading/MultiExchange_Profit_Architecture.md`
- `docs/features/trading/MultiExchange_Implementation_MasterPlan.md`
- `docs/ops/MultiExchange_Certification_Matrix.md`
- `ROADMAP.md` (Phase 19, B19.*, P19.*)

## Decision Locks (Must Hold)

- Mandatory venues: Binance, Coinbase Advanced, Bybit, OKX, Kraken.
- Product scope: Spot + Perps (sequenced rollout allowed; both required for completion).
- Default risk mode: Per-venue + Global cap.
- Data completeness gate: missing/stale required telemetry => NO-TRADE.
- Execution policy default: maker-preferred with opportunity-aware taker fallback.
- Failover default: Auto Mode automatic failover on; Manual Mode approval required.
- Promotion gate: minimum 28-day live-proof window before unattended scale-up.

## Work Packages

## P19.1 Contracts + Data Layer

### C19-01 Normalized Quote Contract
- **Owner**: Services
- **Primary files**: `Models/MarketData.cs`, `Services/RateRouter.cs`, `Services/MultiVenueQuoteService.cs` (new)
- **Tasks**:
  - [ ] Define normalized quote/book snapshot contract (`symbol`, `venue`, `bid`, `ask`, depth, timestamp, quote age, RTT, provenance, confidence).
  - [ ] Add service composition path for parallel venue quote aggregation.
  - [ ] Add staleness thresholds and stale-quote tagging.
- **Acceptance criteria**:
  - [ ] Runtime emits deterministic quote snapshot records per venue.
  - [ ] Stale quotes are tagged and excluded from tradable route candidates.

### C19-02 Venue Health Scoring
- **Owner**: Services
- **Primary files**: `Services/VenueHealthService.cs` (new), `Services/ResilientExchangeClient.cs`
- **Tasks**:
  - [ ] Implement per-venue health model (freshness, error rate, timeout rate, RTT percentile, reconnect status).
  - [ ] Publish health state for routing/risk veto.
- **Acceptance criteria**:
  - [ ] Venue health score updates every cycle.
  - [ ] Any unhealthy venue is vetoed by routing when below threshold.

## P19.2 Adapters + Provider Expansion

### C19-03 Mandatory Venue Clients/Brokers
- **Owner**: Exchanges/Brokers
- **Primary files**: `Exchanges/BinanceClient.cs` (new), `Exchanges/BybitClient.cs` (new), `Exchanges/OkxClient.cs` (new), `Brokers/BinanceBroker.cs` (new), `Brokers/BybitBroker.cs` (new), `Brokers/OkxBroker.cs` (new)
- **Tasks**:
  - [ ] Implement required market-data and execution methods per contract.
  - [ ] Enforce precision/min-notional/step-size checks before submit.
  - [ ] Ensure unsupported semantics fail closed with diagnostic reason.
- **Acceptance criteria**:
  - [ ] Adapter certification PASS for auth, discovery, ticker/candles, submit/cancel/idempotency, partial fills, rate-limit handling.
  - [ ] No entry path can proceed without protective-exit viability check.

### C19-04 Exchange Provider Capability Wiring
- **Owner**: Services
- **Primary files**: `Services/ExchangeProvider.cs`, `Program.cs`
- **Tasks**:
  - [ ] Expand provider factory to include all mandatory venues.
  - [ ] Add capability descriptors consumed by planner/router/UI diagnostics.
  - [ ] Keep resilience wrapping for all external calls.
- **Acceptance criteria**:
  - [ ] Public/auth clients can be created for each mandatory venue.
  - [ ] Capability diagnostics visible in runtime logs and planner gating.

### C19-14 Provider Public API Contract Verification
- **Owner**: Services/Ops
- **Primary files**: `Services/ExchangeProviderAuditService.cs`, `Services/ExchangeProvider.cs`, `docs/ops/MultiExchange_Certification_Matrix.md`
- **Tasks**:
  - [ ] Add per-exchange provider public-API probe (`CreatePublicClient`, `ListProductsAsync`, `GetTickerAsync`).
  - [ ] Record deterministic pass/fail + latency evidence per exchange.
  - [ ] Gate planner/routing certification on provider public-API probe pass.
- **Acceptance criteria**:
  - [ ] Each mandatory exchange has a PASS/FAIL provider probe artifact.
  - [ ] No exchange enters adapter/live certification with failed provider public-API probe.

## P19.3 Opportunity + Routing Core

### C19-05 Spread Divergence Detector
- **Owner**: Services
- **Primary files**: `Services/SpreadDivergenceDetector.cs` (new), `Services/AutoPlannerService.cs`
- **Tasks**:
  - [ ] Detect cross-venue spread divergence above full cost stack (fees + slippage + latency risk).
  - [ ] Emit deterministic reject reasons (`fees-kill`, `slippage-kill`, `stale-quote`, `latency-risk`, `insufficient-depth`).
- **Acceptance criteria**:
  - [ ] Candidate opportunities and reject categories are emitted every cycle.
  - [ ] Detector output is consumed by planner flow (not UI logic).

### C19-06 Funding Carry Detector
- **Owner**: Services/Strategy
- **Primary files**: `Services/FundingCarryDetector.cs` (new), `Strategy/FeatureExtractor.cs`
- **Tasks**:
  - [ ] Detect funding/basis opportunities with stability and volatility guards.
  - [ ] Add basis/funding fields to normalized inputs used by planner/strategy gates.
- **Acceptance criteria**:
  - [ ] Funding candidate events include threshold, basis stability, and risk status.
  - [ ] Funding opportunities are blocked when stability guard fails.

### C19-07 Smart Router + Scorer
- **Owner**: Services
- **Primary files**: `Services/ExecutionVenueScorer.cs` (new), `Services/SmartOrderRouter.cs` (new), `Services/AutoPlannerService.cs`
- **Tasks**:
  - [ ] Score venues by expected net edge (fees, slippage, reliability penalty, latency).
  - [ ] Route to best compliant venue; perform bounded fallback on degradation.
  - [ ] Enforce maker-preferred behavior with explicit taker fallback rule.
- **Acceptance criteria**:
  - [ ] Every executable plan has `chosen venue`, `alternates`, `score breakdown`.
  - [ ] Fallback activates only under defined degradation and records reason.

## P19.4 Regime + Risk + UI Surface

### C19-08 Regime Classification and Strategy Gates
- **Owner**: Strategy/Services
- **Primary files**: `Strategy/StrategyEngine.cs`, `Strategy/RiskGuards.cs`, `Services/AutoPlannerService.cs`
- **Tasks**:
  - [ ] Implement regime states (`expansion`, `compression`, `trend`, `mean-reversion`, `funding-extreme`).
  - [ ] Bind strategy families to allowed regimes.
  - [ ] Block strategy execution outside allowed regime-state map.
- **Acceptance criteria**:
  - [ ] Runtime emits regime state and per-strategy block/allow rationale.
  - [ ] No strategy executes when regime disallows it.

### C19-09 Fee/Slippage Expectancy Gate
- **Owner**: Services/Strategy
- **Primary files**: `Services/AutoPlannerService.cs`, `Strategy/RiskGuards.cs`
- **Tasks**:
  - [x] Apply canonical pre-trade net-edge check:
  - [x] `E = (WinRate * AvgWin) - (LossRate * AvgLoss) - Fees - Slippage`
  - [x] Enforce no-trade when expected net edge <= 0 or below configured minimum.
- **Acceptance criteria**:
  - [x] Proposal diagnostics include gross edge, fee drag, slippage budget, final net edge.
  - [x] Plans with negative/insufficient net edge are rejected with deterministic reason.

### C19-10 Designer-First Auto/Planner/Dashboard Surface
- **Owner**: UI
- **Primary files**: `UI/AutoModeControl.Designer.cs`, `UI/AutoModeControl.cs`, `UI/PlannerControl.cs`, `UI/DashboardControl.cs`, `UI/AccountEditDialog.*`, `UI/KeyEditDialog.*`
- **Tasks**:
  - [ ] Add venue stack, routing policy, and funding controls in designer-backed surfaces.
  - [ ] Add runtime status rows for chosen venue, degraded venues, and circuit-breaker state.
  - [ ] Add routing rationale visibility (chosen venue, expected cost, confidence).
- **Acceptance criteria**:
  - [ ] Forms remain editable in Visual Studio Designer.
  - [ ] UI shows status only; no domain decision logic is added to UI layer.

### C19-15 Per-Exchange Credential Requirements Enforcement
- **Owner**: UI/Services
- **Primary files**: `Services/ExchangeCredentialPolicy.cs`, `UI/AccountEditDialog.cs`, `UI/KeyEditDialog.cs`, `docs/ops/MultiExchange_Provider_API_And_Credentials.md`
- **Tasks**:
  - [ ] Define and maintain a research-backed credential requirement matrix per exchange.
  - [ ] Enforce required fields in account/key save flows with deterministic validation messages.
  - [ ] Keep field visibility and validation behavior aligned to selected service policy.
- **Acceptance criteria**:
  - [ ] Missing required credentials are hard-blocked for each exchange.
  - [ ] Existing key reuse remains supported without forced re-entry.
  - [ ] Credential policy is documented and indexed for ops/certification reference.

## P19.5 Guardrails + Operations

### C19-11 Circuit Breakers and Recovery
- **Owner**: Services/Ops
- **Primary files**: `Services/VenueHealthService.cs`, `Services/AutoPlannerService.cs`, `Services/OrderIntentJournalService.cs` (new), `UI/AutoModeControl.cs`
- **Tasks**:
  - [x] Add per-venue/global breaker conditions (API failure streak, stale quote ratio, slippage breach streak, ack timeout).
  - [ ] Add bounded reconnect/backoff and controlled re-enable windows.
  - [x] Add global safe-set-empty halt behavior.
- **Acceptance criteria**:
  - [ ] Breaker transitions are deterministic and fully logged.
  - [x] Auto loop halts safely when no compliant venue remains.

### C19-12 Deterministic Evidence Tooling
- **Owner**: Ops/QA
- **Primary files**: `Util/run_multiexchange_certification.ps1`, `obj/runtime_reports/multiexchange/*`, `docs/ops/MultiExchange_Certification_Matrix.md`
- **Tasks**:
  - [x] Add one-command certification runner producing PASS/PARTIAL/FAIL artifacts.
  - [x] Include strategy × exchange matrix summaries and reject reason categories.
- **Acceptance criteria**:
  - [x] Runner outputs timestamped report files and final verdict per run.
  - [ ] Reports are sufficient to gate promotion decisions without manual reconstruction.

## P19.6 Paper-to-Live Cutover

### C19-13 Promotion Gates and Runtime Proof
- **Owner**: Ops/QA + Services
- **Primary files**: `docs/ops/MultiExchange_Certification_Matrix.md`, runtime telemetry/report scripts
- **Tasks**:
  - [ ] Run sequence: historical backtest -> walk-forward -> paper short soak -> paper long soak -> shadow live -> capped live -> 28-day proof.
  - [ ] Enforce live-proof gates: no critical safety breach, no unhandled crash, positive expectancy after costs, telemetry completeness.
- **Acceptance criteria**:
  - [ ] 28 consecutive days pass defined thresholds.
  - [ ] Promotion decision is traceable to deterministic artifacts.

## Core Exit Criteria (Definition of Done)

- [ ] All mandatory venues are certified PASS on required adapter checks.
- [ ] Provider public-API contract verification passes for all mandatory venues.
- [ ] Strategy × exchange matrix has deterministic enable/disable defaults based on adequacy thresholds.
- [ ] Smart routing is canonical and ad-hoc UI routing is decommissioned.
- [ ] Per-exchange credential requirements are enforced in account/key setup and validated by certification evidence.
- [ ] Trade-level telemetry completeness gate is enforced fail-closed.
- [ ] Protective exits are guaranteed for all executable entry paths.
- [ ] Circuit breakers, failover, and kill-switch automation are verified in runtime evidence.
- [ ] 28-day live-proof promotion gate passes with full artifact set.

## Ownership Summary

- **Services**: data normalization, detectors, scorer/router, health, journaling, planner orchestration.
- **Strategy**: regime modeling, strategy eligibility, risk veto logic.
- **Exchanges/Brokers**: venue adapters, precision/constraint correctness, execution safety.
- **UI**: designer-first controls and runtime observability only.
- **Ops/QA**: certification runners, evidence artifacts, promotion gate enforcement.
