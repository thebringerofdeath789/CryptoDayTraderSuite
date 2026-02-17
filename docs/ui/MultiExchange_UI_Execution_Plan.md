# Multi-Exchange UI Execution Plan (Designer-First)

[← Back to Documentation Index](../index.md)

## Objective

Define exactly how the UI must change to support Phase 19 multi-exchange execution while preserving architecture boundaries:

- UI layer remains passive (state display + operator input only).
- Domain/routing/risk decisions remain in Services/Strategy layers.
- All surface changes remain Visual Studio Designer editable.

## Scope

This plan covers required updates to:

- Auto Mode surface
- Planner surface
- Dashboard visibility
- Account/Key dialogs
- Shared status semantics and telemetry presentation

Primary implementation surfaces:

- `UI/AutoModeControl.Designer.cs`
- `UI/AutoModeControl.cs`
- `UI/PlannerControl.Designer.cs`
- `UI/PlannerControl.cs`
- `UI/DashboardControl.Designer.cs`
- `UI/DashboardControl.cs`
- `UI/AccountEditDialog.Designer.cs`
- `UI/AccountEditDialog.cs`
- `UI/KeyEditDialog.Designer.cs`
- `UI/KeyEditDialog.cs`

## Non-Negotiables

- No business logic in UI (no venue scoring, no regime decisions, no direct risk math).
- No dynamic ad-hoc layout construction for static controls.
- No modal spam for normal operations; use inline status + logs.
- No new external UI dependencies.
- Preserve existing theme primitives from `Themes/Theme.cs`.

## UI Change Model

## A. Auto Mode (Primary Operational Surface)

### A1. Header (Always Visible)

Add/retain a fixed header row with:

- Account selector
- Profile selector
- Profile enabled
- Profile interval
- Auto run
- Auto cycle interval
- Live arm
- Kill switch
- Primary status text

Required additions for multi-exchange:

- Venue role summary labels:
  - Anchor venue
  - Derivatives venue
  - Divergence venue
- Failover state label (`normal`, `degraded`, `failover-active`, `halted`)
- Last cycle timestamp + duration

### A2. Left Rail (Configuration Cards)

Split existing dense controls into designer cards:

1. **Profile Card**
   - profile CRUD
   - enabled toggle
   - profile cadence
2. **Venue Stack Card**
   - anchor venue selector
   - derivatives venue selector
   - divergence venue selector
   - per-venue enable/disable toggles
3. **Routing Policy Card**
   - maker-preferred toggle
   - max slippage bps
   - max quote age ms
   - auto failover toggle
4. **Funding Capture Card**
   - funding threshold
   - basis stability threshold
   - max hold window
5. **Risk Guardrails Card**
   - max trades per cycle
   - cooldown minutes
   - daily risk stop

### A3. Work Area

- Keep sticky action bar with `Scan`, `Propose`, `Execute`.
- Keep results grid as the central work surface.
- Add read-only routing columns:
  - chosen venue
  - expected cost basis
  - route confidence
  - reject reason

### A4. Footer / Telemetry Band

Keep always-visible summary labels and add:

- degraded venue count
- circuit breaker state
- executed/skipped counts by reason
- telemetry completeness status (trade-level gate)

## B. Planner Surface (Decision Transparency)

### B1. Header Context

Retain account/symbol/granularity/lookback/equity controls and add:

- current venue policy summary
- last scan/propose timestamp

### B2. Left Rail

Keep filter controls and add deterministic read-only diagnostics:

- regime state
- venue health summary
- selected profile risk mode (`Unified` vs `Per-venue + Global cap`)

### B3. Work Tabs

`Planned Trades` tab additions:

- chosen venue
- alternate venues (compact text)
- expected net edge
- fee drag
- slippage budget
- reject reason category

`Predictions` tab additions:

- regime tag
- data freshness flag

## C. Dashboard (Runtime Health at a Glance)

Add compact cards for:

- venue health summary (5 mandatory venues)
- spread opportunity count
- funding opportunity count
- routing outcome summary (maker/taker ratio, failovers)
- expected vs realized slippage

Every dashboard card must display a last-updated timestamp.

## D. Account/Key Dialogs (Venue Readiness)

### D1. Account Dialog

Add venue-aware configuration sections:

- exchange/service selection for Binance/Coinbase Advanced/Bybit/OKX/Kraken
- product scope flags (spot/perps where relevant)
- routing eligibility toggle

### D2. Key Dialog

Add/confirm templates per venue with canonical mapping:

- api key
- secret variant (`Secret` / `ApiSecretBase64` compatibility)
- passphrase where required
- key health test result label

## E. Shared Status Semantics

Standardize status text across Auto/Planner/Dashboard:

- `success`: completed action with count summary
- `warn`: blocked/skipped with deterministic reason
- `neutral`: idle/awaiting input
- `error`: actionable failure requiring intervention

Status grammar pattern:

`<action>: <result>; reason=<code>; t=<timestamp>`

## Implementation Sequence

### UI-1 Structural Refactor (Designer)

- Break Auto/Planner dense rows into cards and fixed bands.
- Keep all control creation in `.Designer.cs`.

### UI-2 Data Binding Contract Expansion

- Extend view models/grid bindings for route diagnostics and health fields.
- Keep bindings read-only where values come from service decisions.

### UI-3 Runtime Visibility

- Add routing/health/failover summaries to Auto/Planner/Dashboard.
- Add freshness timestamps everywhere operator decisions depend on data recency.

### UI-4 Dialog Expansion

- Add venue templates to Account/Key dialogs.
- Add validation and inline warnings for missing venue coverage.

### UI-5 Interaction Hardening

- Remove non-critical modal prompts.
- Ensure kill switch and live-arm remain obvious and always visible.

## Acceptance Criteria

- Operator can identify run state, venue state, and block reason within 3 seconds on Auto Mode.
- Planner shows deterministic route rationale and reject categories for each candidate.
- Dashboard shows venue health and freshness without opening additional dialogs.
- Account/Key dialogs support all mandatory Phase 1 venues without manual JSON editing.
- No UI file adds strategy/routing/risk decision logic.
- All changed forms open correctly in Visual Studio Designer.

## Dependencies (UI Inputs Required From Services)

UI implementation depends on service outputs being available:

- route decision DTO (`chosen`, `alternates`, `confidence`, `cost breakdown`)
- venue health DTO (`score`, `staleness`, `latency`, `breaker`)
- regime DTO (`state`, `reasons`)
- reject reason taxonomy (canonical string codes)
- telemetry completeness flag per trade decision

If any DTO is missing, UI should display `unavailable` and remain non-blocking.

## Rollout and Risk

- Ship in feature-flagged increments by surface (Auto first, Planner second, Dashboard third).
- Keep existing controls functional until replacement controls are fully wired.
- Validate each surface at minimum width 900 and typical desktop resolutions.
