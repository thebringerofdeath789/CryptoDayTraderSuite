# Multi-Exchange Profit Architecture Blueprint

[← Back to Documentation Index](../../index.md)

## Purpose

Define a concrete implementation path for moving from single-venue strategy execution to a multi-venue edge engine focused on:

- Cross-exchange spread divergence capture
- Funding-rate carry capture
- Liquidity/latency-aware routing
- Fee- and slippage-aware expectancy preservation

This blueprint translates the strategy thesis into architecture and service responsibilities that align with the existing system map in `docs/architecture/SystemMap.md`.

## Profit Thesis (What Drives PnL)

The exchange brand is not the edge by itself. Durable edge comes from structure:

- Liquidity depth quality
- Effective fee model (taker + maker/rebates)
- Funding-rate dislocations
- Slippage and queue-position outcomes
- API/market-data reliability
- Product coverage (spot/perps/options)

Expected value remains constrained by fee and slippage drag:

$$
E = (W \cdot \overline{G}) - (L \cdot \overline{R}) - F - S
$$

Where:

- $W$ = win rate, $\overline{G}$ = average gain
- $L$ = loss rate, $\overline{R}$ = average loss
- $F$ = fees (including funding impact)
- $S$ = slippage + adverse selection

## Target Venue Stack

Minimum profitable multi-venue profile:

1. **Primary liquidity anchor**: Binance or Coinbase Advanced
2. **Derivatives-heavy venue**: Bybit or OKX
3. **Divergence/slow venue**: Kraken or Coinbase Advanced

Operational interpretation:

- Use anchor venue as principal quote/depth reference.
- Use derivatives venue for perp momentum and funding carry.
- Use slower venue as divergence detector leg and mean-reversion counterparty.

## Strategy-to-Venue Matrix

### 1) HFT/Scalping (sub-5m)

- Primary venues: Binance, Bybit
- Enable when: ATR expansion, rising depth imbalance, stable funding
- Disable when: spread widens, volatility compresses, news shock slippage

### 2) VWAP Trend

- Primary venues: Binance, Coinbase Advanced, Bybit (leverage variants)
- Enable when: 1h VWAP slope and volume expansion confirm trend
- Disable when: repeated VWAP reclaim/chop

### 3) ORB (Opening Range Breakout)

- Primary venues: Bybit, Binance, OKX
- Enable when: ATR and opening volume are expanding
- Disable when: inside-day compression / failed delta follow-through

### 4) Funding-Rate Arbitrage

- Primary venues: Binance, Bybit, OKX
- Structure: spot-perp basis hedge; direction by funding sign
- Enable when: funding is extreme and basis remains stable

### 5) Cross-Exchange Arbitrage

- Primary pairs: Binance↔Coinbase, Binance↔Kraken, Binance↔Bybit
- Trigger: spread divergence exceeds total cost stack (fees + slippage + latency risk)
- Require: near-simultaneous execution and deterministic unwind policy

## Architecture Mapping to Current Codebase

This section defines where implementation belongs under current layer boundaries.

### Market Data Layer (Services + Exchanges)

- Extend `Services/RateRouter.cs` to maintain normalized top-of-book snapshots per venue and symbol.
- Add per-venue feed health and staleness metrics in `Services/ResilientExchangeClient.cs`.
- Keep exchange API specifics in `Exchanges/*Client.cs` implementations behind `Exchanges/IExchangeClient.cs`.

### Regime Engine (Strategy)

- Add regime-state composition in `Strategy/StrategyEngine.cs` using existing ATR/VWAP/choppiness features from `Strategy/Indicators.cs` and `Strategy/FeatureExtractor.cs`.
- Introduce funding/basis regime inputs as normalized model fields (not UI logic).

### Opportunity & Routing Layer (Services)

- Add a service-level venue scorer that ranks execution venue by expected net edge:
  - expected fill quality
  - fee tier impact
  - funding carry impact
  - reliability penalty
- Candidate home: `Services/AutoPlannerService.cs` orchestration path, with scoring isolated in a dedicated service (recommended: `Services/ExecutionVenueScorer.cs`).

### Execution Layer (Brokers + Services)

- Keep broker-specific placement in `Brokers/*Broker.cs`.
- Add smart-routing policy in a service (recommended: `Services/SmartOrderRouter.cs`) that selects venue/account before broker submit.
- Preserve fail-closed behavior when protective exits cannot be guaranteed.

### Risk Layer (Strategy + Services)

- Extend `Strategy/RiskGuards.cs` with multi-venue constraints:
  - per-venue exposure cap
  - cross-venue net exposure cap
  - latency/quote-staleness rejection
  - max divergence-age guard
- Surface hard stops in Auto Mode cycle orchestration (`UI/AutoModeControl.cs`) through existing guardrail flow.

## Implementation Roadmap (Execution Order)

### Milestone M1: Multi-Venue Data Foundation

- Add normalized quote snapshot contract (`symbol`, `venue`, `bid`, `ask`, `depth`, `ts`).
- Emit spread table for configured venue pairs.
- Track quote age and round-trip latency per venue.

### Milestone M2: Opportunity Detection

- Add cross-venue divergence detector with configurable net-edge threshold.
- Add funding capture detector using periodic funding snapshots and basis spread checks.
- Add telemetry outputs for candidate/opportunity counts and reject reasons.

### Milestone M3: Smart Routing

- Introduce venue scoring and route-selection policy for each executable plan.
- Route to lowest expected slippage venue that still satisfies risk and reliability gates.
- Add fallback venue switching when primary venue health degrades.

### Milestone M4: Regime-Aware Strategy Activation

- Gate strategy families by regime class (trend/expansion/compression/funding-extreme).
- Prevent always-on strategy execution across all conditions.
- Emit explicit "strategy disabled by regime" diagnostics.

### Milestone M5: Validation & Ops

- Add deterministic runtime report fields for:
  - venue-level spread opportunities
  - venue routing decisions and expected cost basis
  - funding capture events and realized carry
- Extend ops scripts to assert no-regression on cycle completion and error isolation.

## Guardrails (Non-Negotiable)

- No UI-layer trading logic (`UI/*` remains passive control surface).
- No static global state for orchestrators; use constructor-injected services.
- No placeholder execution branches; unsupported paths fail closed with diagnostics.
- No strategy enablement without fee/slippage-adjusted expectancy checks.

## KPIs for Profitability Tracking

Track these at cycle and daily horizons:

- Net expectancy after fees/slippage/funding
- Maker vs taker execution ratio
- Realized vs expected slippage by venue
- Spread-opportunity hit rate and capture rate
- Funding carry PnL contribution
- Venue reliability impact (timeouts, stale quote rejects, failovers)

## Practical Starting Configuration

For current suite evolution, start with:

- Anchor: Coinbase Advanced (compliance + API reliability)
- Derivatives: Bybit (perp/funding behavior)
- Divergence leg: Kraken (profile differentiation)

Then expand with Binance/OKX paths as region/compliance allows.

This sequencing gives immediate multi-venue signal quality gains without over-expanding integration surface in one iteration.
