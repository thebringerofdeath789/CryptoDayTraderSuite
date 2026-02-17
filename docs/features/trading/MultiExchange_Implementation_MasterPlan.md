# Multi-Exchange Implementation Master Plan (Set-and-Forget Target)

[← Back to Documentation Index](../../index.md)

## Objective

Build a production-grade, mostly unattended multi-exchange engine that:

- Detects cross-venue inefficiencies in real time
- Routes orders to best expected execution venue
- Captures funding/basis dislocations when valid
- Preserves edge after fees/slippage/reliability penalties

Important constraint: no architecture can guarantee profit in all regimes. The goal is robust positive expectancy with strict loss containment and high operational uptime.

## Decision Lock (Confirmed Defaults)

These decisions are locked for Phase 1 implementation unless explicitly changed later.

- **Mandatory exchanges in Phase 1 (none optional)**:
  - Binance
  - Coinbase Advanced
  - Bybit
  - OKX
  - Kraken
- **Product scope in Phase 1**:
  - Spot and Perpetuals are both in scope.
  - Phase 1 rollout sequencing still starts with spot-first paper soak, then perp activation, but both must be implemented.
- **Risk budgeting model**:
  - Support both options in UI:
    - Unified portfolio budget
    - Per-venue budgets plus global cap
  - Default mode: **Per-venue + Global cap** (better containment for venue-specific outages/behavior drift).
- **Live proof window minimum**:
  - Default gate is **4 weeks (28 days)** before unattended scale-up.
  - Reason: captures more regime diversity than 2 weeks and reduces overfitting to short windows.
- **Kill-switch / drawdown thresholds**:
  - User-settable in UI (required).
  - Provide recommended defaults and hard upper/lower validation bounds.
- **Data health requirement**:
  - If required telemetry for a trade is incomplete/stale, block trade (fail closed).
- **Execution priority policy**:
  - Default: **maker-preferred, opportunity-aware taker fallback**.
  - Taker allowed only when expected edge decay from waiting is greater than incremental taker cost.
- **Failover behavior**:
  - Auto Mode: automatic venue failover enabled by default.
  - Manual Mode: failover requires manual approval.

## Current-State Gap Summary

Based on current code:

- `Program.cs` wires one `AutoPlannerService` to a single public client (`Coinbase`).
- `Services/ExchangeProvider.cs` supports `Coinbase`, `Kraken`, `Bitstamp` only.
- `Services/RateRouter.cs` is single-source (`Coinbase`) and returns one mid-rate cache, not multi-venue book snapshots.
- `Brokers` currently include `CoinbaseExchangeBroker` and `PaperBroker`, but no Binance/Bybit/OKX execution adapters.
- `UI/AutoModeControl.cs` already supports profile orchestration and telemetry, which is the right base for unattended operation.

## Target Operating Model

### Venue Roles

- **Anchor liquidity**: Binance or Coinbase Advanced
- **Derivatives engine**: Bybit or OKX
- **Divergence leg**: Kraken or Coinbase Advanced

### Strategy Families

- Cross-exchange spread divergence capture
- Funding carry / basis capture
- Regime-gated VWAP/ORB trend execution
- Optional maker-first market making when rebate structure supports edge

### Unattended Runtime Contract

- Auto loop continues without modal prompts
- Circuit breakers and kill switches fail closed
- Deterministic telemetry is emitted every cycle
- Reconnect/failover logic handles transient provider outages

## Exact Code Change Plan

## A) Add (New Components)

### 1. Market Data Normalization

- `Models/MarketData.cs` (extend): add normalized venue quote/book snapshot models.
- `Services/MultiVenueQuoteService.cs` (new): pull and normalize top-of-book from all configured venues.
- `Services/VenueHealthService.cs` (new): track quote freshness, API errors, RTT, and degradation score.

### 2. Opportunity Detection

- `Services/SpreadDivergenceDetector.cs` (new): find spread gaps above net-cost threshold.
- `Services/FundingCarryDetector.cs` (new): evaluate funding/basis carry opportunities with stability checks.
- `Models/AI/` or `Models/` (extend): add opportunity DTOs for deterministic planner intake.

### 3. Smart Routing

- `Services/ExecutionVenueScorer.cs` (new): expected-cost model (`fees + slippage + latency + reliability penalty`).
- `Services/SmartOrderRouter.cs` (new): route each plan to best venue/account with fallback order.
- `Services/OrderIntentJournalService.cs` (new): idempotent order-intent and execution journal for recovery.

### 4. Exchanges / Brokers

- `Exchanges/BinanceClient.cs` (new)
- `Exchanges/BybitClient.cs` (new)
- `Exchanges/OkxClient.cs` (new)
- `Brokers/BinanceBroker.cs` (new)
- `Brokers/BybitBroker.cs` (new)
- `Brokers/OkxBroker.cs` (new)

All adapters must implement existing contracts (`IExchangeClient`, `IBroker`) and fail closed on unsupported order semantics.

## B) Convert (Existing Components)

### 1. DI Composition / Runtime Wiring

- `Program.cs`
  - Convert single-public-client planner wiring into multi-venue service graph.
  - Inject new quote/detector/router services into `MainForm` dependency initialization.

### 2. Exchange Factory

- `Services/ExchangeProvider.cs`
  - Convert `Factory(...)` switch to include Binance/Bybit/OKX.
  - Add public/authenticated capability declarations per venue.
  - Keep `ResilientExchangeClient` wrapping for all external calls.

### 3. Rate Router

- `Services/RateRouter.cs`
  - Convert from single Coinbase source to multi-venue composite mid-rate with staleness checks.
  - Add quote provenance (`best venue`, `timestamp`, `confidence`).

### 4. Planner

- `Services/AutoPlannerService.cs`
  - Convert from one `_client` model to quote/opportunity-driven model input.
  - Keep strategy logic in `Strategy/*`; planner remains orchestration.
  - Add explicit reject reasons (`fees-kill`, `slippage-kill`, `latency-risk`, `stale-quote`).

### 5. Risk Controls

- `Strategy/RiskGuards.cs`
  - Add cross-venue and per-venue exposure caps.
  - Add divergence-age and quote-freshness guards.
  - Add venue reliability veto threshold.

### 6. Auto Execution Surface

- `UI/AutoModeControl.cs`
  - Keep it as orchestration UI only.
  - Add runtime visibility for venue/routing decisions and degraded-mode status.
  - Keep all logic execution in services (no new domain logic in UI).

## C) Remove / Decommission

### 1. Remove single-venue assumptions

- Remove hardcoded single-source market-data assumptions in planner/rate paths.
- Remove any implicit “Coinbase default for all paths” behavior where multi-venue config exists.

### 2. Remove duplicate routing logic

- Decommission ad-hoc execution venue selection in UI/event handlers once `SmartOrderRouter` is active.

### 3. Remove non-deterministic execution branches

- Eliminate any path that can place entry orders without guaranteed protective-exit path.

## UI Changes (Designer-First)

All UI updates must remain `.Designer.cs` editable.

### 1. Auto Mode: Multi-Venue Ops Panel

In `UI/AutoModeControl.Designer.cs` + code-behind:

- Add `Venue Stack` section:
  - Anchor venue selector
  - Derivatives venue selector
  - Divergence venue selector
  - Enable/disable per venue toggle
- Add `Routing Policy` section:
  - Maker-first toggle
  - Max allowed slippage bps
  - Max quote age ms
  - Failover enabled toggle
- Add `Funding Capture` section:
  - Funding threshold
  - Basis stability threshold
  - Max hold time
- Add read-only runtime status rows:
  - Current best venue per symbol
  - Degraded venues count
  - Circuit breaker state

### 2. Accounts / Keys UX

- `UI/AccountEditDialog.*`:
  - Add venue-specific credential field groups for Binance/Bybit/OKX.
- `UI/KeyEditDialog.*`:
  - Add service templates for new exchanges while preserving canonical key data mapping.

### 3. Planner/Dashboard Visibility

- `UI/PlannerControl.*`: show routing rationale (`chosen venue`, `expected cost`, `confidence`).
- `UI/DashboardControl.*`: add compact venue health and spread-opportunity counters.

## Set-and-Forget Reliability Design

### Circuit Breakers

Trip conditions (per venue and global):

- repeated API failures
- stale quote ratio breach
- slippage breach streak
- unresolved order acknowledgement timeout

Actions:

- disable affected venue routing
- re-route if safe alternatives exist
- pause trading if global safe set is empty

### Recovery

- background reconnect/backoff with bounded retry
- venue re-enable only after health stabilization window
- deterministic state restore from order-intent journal

### Safety Caps

- per-strategy max loss/day
- global portfolio max drawdown/day
- max concurrent cross-venue exposure
- max open hedge carry duration

## Data, Telemetry, and Validation

### Telemetry Additions

Extend current cycle reports with:

- venue quote freshness/latency
- detected spread opportunities and capture outcomes
- routing decisions + expected vs realized slippage
- funding events and carry PnL attribution
- circuit breaker transitions and failover counts

### Validation Workflow

1. Multi-venue paper-only soak (short)
2. Multi-venue paper-only soak (long)
3. Partial live with strict notional caps
4. Full unattended window with hard kill-switch automation

Minimum promotion gate:

- Maintain passing performance/risk/reliability thresholds for a continuous 28-day live-proof window.

Success criteria:

- no uncaught exceptions
- no orphaned unprotected entries
- deterministic cycle telemetry for every run
- positive net expectancy over controlled sample after fees/slippage

## Implementation Phases (Execution Sequence)

### Phase 19.1: Contracts + Data Layer

- Add models/services for quote normalization and venue health.

### Phase 19.2: Adapters + Provider Expansion

- Add Binance/Bybit/OKX clients and brokers, integrate into `ExchangeProvider`.

### Phase 19.3: Opportunity + Routing Core

- Add divergence/funding detectors + venue scorer + smart router.

### Phase 19.4: UI Surface + Config

- Add designer-backed venue/routing/funding controls and runtime status panels.

### Phase 19.5: Guardrails + Operations

- Add circuit breakers, journaling, and expanded verification scripts.

### Phase 19.6: Paper-to-Live Cutover

- staged exposure ramp with explicit promotion gates.

## Non-Negotiables

- No runtime placeholders/stubs/`NotImplementedException`.
- No strategy/business logic in UI layer.
- No entry placement without protective-exit guarantee.
- No deployment to unattended mode without passing full validation matrix.
