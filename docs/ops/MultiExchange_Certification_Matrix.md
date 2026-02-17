# Multi-Exchange Certification Matrix

[← Back to Documentation Index](../index.md)

## Purpose

Define mandatory pass/fail validation for all Phase 1 exchanges and all strategy families before unattended production operation.

Phase 1 mandatory exchanges:

- Binance
- Coinbase Advanced
- Bybit
- OKX
- Kraken

No exchange is optional in Phase 1 scope.

## Product Scope

Phase 1 requires both:

- Spot
- Perpetual Futures

Validation may be sequenced (spot-first, then perps), but release is not complete until both pass certification where supported by venue.

## Strategy Scope

Backtest and forward/paper validation must include:

- VWAP Trend
- ORB
- RSI Reversion
- Donchian
- Funding Carry
- Cross-Exchange Spread Divergence

## Certification Stages

1. Historical backtest certification
2. Walk-forward backtest certification
3. Paper execution certification
4. Shadow live certification (signal/routing only, no order submit)
5. Capped live certification
6. 28-day live-proof gate

## Exchange Adapter Certification (Per Exchange)

Each exchange must pass all relevant checks:

- Authentication and key validation
- Product/symbol discovery
- Ticker and candles retrieval (required intervals)
- Order precision/min-size constraints
- Market order placement path
- Cancel path and idempotency
- Partial fill handling
- Retry/backoff handling for transient faults
- Disconnect/reconnect behavior
- Rate-limit behavior and graceful degradation
- Telemetry emission completeness

Minimum status for progression: **PASS on all mandatory checks**.

## Provider Public API Contract Verification (Pre-Adapter Gate)

Before adapter certification and before any authenticated test sequence, each mandatory venue must pass a provider public-API contract probe:

- Provider can create public client (`CreatePublicClient`).
- Public product discovery succeeds (`ListProductsAsync` non-empty).
- Public ticker probe succeeds (`GetTickerAsync` valid last price).
- Discovery and ticker probe latency captured in run artifacts.

Primary implementation surface:

- `Services/ExchangeProviderAuditService.cs`

Minimum status for progression: **PASS for all mandatory venues**.

## Backtest Certification Matrix (Per Strategy × Per Exchange)

For each strategy/exchange pair:

- Run base backtest set (full historical range)
- Run walk-forward windows (rolling)
- Apply venue-specific fee model
- Apply venue-specific slippage model
- Apply funding/basis carry where applicable
- Apply venue precision and min-notional constraints

Required outputs:

- Net expectancy after fees/slippage/funding
- Max drawdown
- Profit factor
- Win rate and payoff ratio
- Trade count adequacy
- Stability across windows (variance bounds)

Any strategy/exchange pair failing minimum adequacy thresholds is disabled by default in runtime routing.

## Runtime Health Gating (Trade-Level)

A trade is eligible only when all required telemetry is healthy in the same cycle:

- Market data freshness within threshold
- Spread/latency metrics available and within bounds
- Venue health score above minimum
- Risk budget availability
- Strategy signal confidence and geometry valid
- Cost model available (fees/slippage/funding)

If any required input is missing or stale, decision is **NO-TRADE**.

## Account/Key Credential Requirement Certification

Per-exchange account/key setup must enforce required credential fields using a centralized policy matrix:

- `coinbase-advanced`: API Key Name + EC Private Key (PEM)
- `coinbase-exchange`: API Key + API Secret + Passphrase
- `binance`: API Key + API Secret
- `bybit`: API Key + API Secret
- `okx`: API Key + API Secret + Passphrase
- `kraken`: API Key + API Secret
- `bitstamp`: API Key + API Secret

Certification checks:

- Required fields missing => deterministic save block with actionable validation message.
- Existing key reuse path works without forced credential re-entry.
- Field visibility and save validation align with selected service policy.

Primary implementation surfaces:

- `Services/ExchangeCredentialPolicy.cs`
- `UI/AccountEditDialog.cs`
- `UI/KeyEditDialog.cs`

## Risk Mode Certification

Both runtime risk modes must be tested:

1. Unified portfolio budget
2. Per-venue budgets + global cap (default)

For both modes, verify:

- Kill-switch trigger behavior
- Drawdown cap enforcement
- Daily loss stop enforcement
- Max concurrent exposure enforcement
- Recovery behavior after reset window

## Failover Certification

- Auto Mode: automatic venue failover enabled by default and tested.
- Manual Mode: failover requires explicit approval and tested.

Failover tests must include:

- Primary venue down/unhealthy
- Primary venue stale quotes
- Primary venue rate-limit saturation
- Mid-trade fallback prevention when protective exits cannot be guaranteed

## Live-Proof Promotion Gate

Minimum gate to scale unattended mode:

- Continuous 28-day proof window
- No critical runtime safety breach
- No unhandled exception crash in auto loop
- Positive net expectancy after full costs
- Drawdown stays below configured kill-switch threshold
- Telemetry completeness ≥ required threshold each day

## Required Deliverables Per Certification Run

- Timestamped report file (JSON/TXT)
- Strategy/exchange summary table
- Failure reasons with reject categories
- Promotion verdict (`PASS`, `PARTIAL`, `FAIL`)
- Recommended next action

## Operational Defaults (Initial)

- Risk mode default: Per-venue + global cap
- Execution mode default: maker-preferred with opportunity-aware taker fallback
- Auto failover default: enabled in Auto Mode only
- Missing telemetry policy: fail closed (no trade)

## Open Implementation Tasks

- [x] Add one-command certification runner script for this matrix (`Util/run_multiexchange_certification.ps1`).
- [x] Add deterministic report schema for strategy/exchange pass-fail outputs (`obj/runtime_reports/multiexchange/multi_exchange_cert_*.json`).
- [x] Replace synthetic matrix projection with evidence-backed per strategy×exchange rows including explicit evidence references in certification artifacts.
- [x] Add provider probe artifact script and report schema (`Util/run_provider_public_api_probe.ps1`, `obj/runtime_reports/provider_probe/provider_public_api_probe_*.json`).
- Surface certification status in Auto Mode and Dashboard runtime views.
