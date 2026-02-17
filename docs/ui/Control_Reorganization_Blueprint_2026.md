# Control Reorganization Blueprint (2026-02-16)

## Purpose
This document analyzes current WinForms control organization and defines a practical redesign that improves:
- decision speed (less control hunting),
- live-state visibility (what is happening right now),
- visual polish (clear hierarchy without adding external UI frameworks).

Scope is limited to existing architecture and theme primitives (`Themes/Theme.cs`, `Theme` class).

## Audit Summary (Current State)

### Shell and Navigation
- `MainForm` (`MainForm.cs`) uses a sidebar + content panel shell and view cache.
- `SidebarControl` (`UI/SidebarControl.Designer.cs`) is clean, but the governor widget is visually separated from the selected page context.

### Trading Surface
- `TradingControl` (`UI/TradingControl.Designer.cs`) puts exchange/product/strategy/risk/equity/actions/projections in one wrapping `FlowLayoutPanel` (`pnlTop`).
- Result: parameter entry and action controls visually compete with operational outputs (chart and log), creating weak hierarchy.

### Planner Surface
- `PlannerControl` (`UI/PlannerControl.Designer.cs`) combines three conceptual groups in one wrapping `topBar`:
  1) plan management (`Refresh/Save/Add` + filters),
  2) run context (account/symbol/granularity/lookback/equity),
  3) execution actions (`Scan/Propose/Execute`).
- Primary data (`gridPlanned`) and secondary model insight (`gridPreds`) are useful, but no pinned status/header context exists for “last run / queue state / account mode”.

### Auto Mode Surface
- `AutoModeControl` (`UI/AutoModeControl.Designer.cs`) has a very high control count in one `FlowLayoutPanel` (`topPanel`), including profiles, pair scope, cadence, risk guardrails, live safety, and actions.
- Additional pair selector controls are created dynamically in code (`UI/AutoModeControl.cs`, `BuildPairSelector`), increasing visual inconsistency and maintenance complexity.
- Good telemetry/status labels already exist (`lblAutoStatus`, `lblProfileSummary`, `lblTelemetrySummary`) but are visually crowded by setup controls.

### Dashboard and Status
- `DashboardControl` (`UI/DashboardControl.Designer.cs`) has stronger structure (summary, trades, chart, notifications).
- `StatusControl` (`UI/StatusControl.Designer.cs`) duplicates part of dashboard summary and can feel disconnected from active workflows.

### Setup Screens
- `AccountsControl` and `KeysControl` are functional but look toolbar-heavy and data-light.
- `AccountEditDialog.Designer.cs` and `KeyEditDialog.Designer.cs` are currently empty stubs (`InitializeComponent()` only), which blocks visual editing and reduces consistency.

## UX Problems to Solve
1. **Control density overload** on Planner/Auto pages.
2. **Weak “now” visibility** (run phase, freshness, queue, blocking reason).
3. **Mixed intent in one row** (configuration + execution + status in same strip).
4. **Modal interruptions** still present in interactive paths where inline status would be clearer.
5. **Inconsistent visual rhythm** (spacing/alignment/group titles).

## Reorganization Strategy

## 1) Introduce Three-Zone Page Pattern (All Operational Pages)
Apply to `TradingControl`, `PlannerControl`, and `AutoModeControl`:

- **Zone A: Context Header (top, fixed height)**
  - Active account/exchange/symbol set.
  - Live state chips/labels: connected/disconnected, governor bias, last update time, current cycle state.
  - One compact status line only.

- **Zone B: Configuration Rail (left, fixed width 280–340 px)**
  - Input controls (dropdowns, checkboxes, numeric settings).
  - Grouped into titled cards/panels: “Market”, “Risk”, “Automation”, “Profile”.
  - Scrollable when needed.

- **Zone C: Work Area (right, fill)**
  - Core grids/chart/log.
  - Sticky action bar at top of work area (`Scan`, `Propose`, `Execute`, `Refresh`) separated from setup controls.

Expected benefit: immediate reduction in cognitive load by separating **what I set** vs **what I run** vs **what I observe**.

## 2) Trading Page Reorganization
Source: `UI/TradingControl.Designer.cs`, `UI/TradingControl.cs`.

### New grouping
- **Config rail**: `cmbExchange`, `cmbProduct`, `cmbStrategy`, `numRisk`, `numEquity`, `btnLoadProducts`, `btnFees`.
- **Action bar**: `btnBacktest`, `btnPaper`, `btnLive`.
- **Work area split (vertical)**:
  - top: chart (`SetCandles` output),
  - bottom: log (`txtLog`) with optional severity prefixes.
- **Projection labels** move from parameter strip into a small “Edge Snapshot” panel near action bar.

### Data visibility upgrades
- Add “Last candles fetch” timestamp label.
- Add selected fee profile summary near actions.

## 3) Planner Page Reorganization
Source: `UI/PlannerControl.Designer.cs`, `UI/PlannerControl.cs`.

### New grouping
- **Context header**: selected account, selected symbol, granularity/lookback, and queue count.
- **Config rail**:
  - filters (`cmbFilterProduct`, `cmbFilterStrategy`),
  - run context (`cmbAccount`, `cmbRunProduct`, `cmbGran`, `numLookback`, `numEquity`).
- **Work area tabs**:
  - Tab 1: Planned Trades (`gridPlanned`) + row actions,
  - Tab 2: Scan/Prediction Results (`gridPreds`).
- **Action bar** pinned over work area: `Refresh`, `Save`, `Add Trade`, `Scan`, `Propose`, `Execute`.

### Data visibility upgrades
- Inline status label for last operation result (“Scan complete: N rows”, “Proposed: N plans”, “Execute: ok/fail”).
- Show “execution blocked reason” inline (same text currently shown in dialogs/logs).
- Show “last updated at” for planned trades and predictions.

## 4) Auto Mode Page Reorganization (Highest Impact)
Source: `UI/AutoModeControl.Designer.cs`, `UI/AutoModeControl.cs`.

### New grouping
- **Context header**:
  - `Auto Run`, `Live Arm`, `Kill Switch`, `Auto Every`, `Status`.
- **Left rail section cards**:
  1. **Profile**: account/profile selection + save/delete + enabled + profile interval.
  2. **Pair Scope**: all/selected + pair picker + quick actions.
  3. **Risk Guardrails**: max/cycle, cooldown, daily risk %.
  4. **Run Parameters**: product fallback, granularity, lookback, equity.
- **Work area**:
  - top sticky action bar (`Scan`, `Propose`, `Execute`),
  - main results grid,
  - bottom telemetry panel (latest cycle summary and matrix status).

### Data visibility upgrades
- Keep `lblAutoStatus`, `lblProfileSummary`, `lblTelemetrySummary` always visible in header/footer bands.
- Add compact counters in header: queued plans, executed this cycle, skipped counts.
- Prefer inline result to message boxes for non-critical outcomes in interactive mode.

### Designer compliance adjustment
- Move dynamic pair selector creation (`BuildPairSelector`) into designer-backed controls to keep layout editable and visually consistent.

## 5) Dashboard/Status Rationalization
Sources: `UI/DashboardControl.Designer.cs`, `UI/StatusControl.Designer.cs`.

- Keep `DashboardControl` as single “system health” page.
- Convert `StatusControl` into a lightweight reusable summary card (or merge into Dashboard-only usage) to avoid duplicated metric surfaces.
- Add freshness labels:
  - “Trades data: updated HH:mm:ss”,
  - “Equity curve: updated HH:mm:ss”,
  - “Governor: last poll HH:mm:ss”.

## 6) Visual Language Upgrade (Without New Dependencies)
Source: `Themes/Theme.cs`.

- Keep current `Dark Graphite` palette.
- Standardize spacing: 8px internal rhythm, 16px section spacing, 24px page padding.
- Replace long flat rows with titled panels/cards (`Panel` + bold `Label`).
- Use one typography hierarchy:
  - page title,
  - section title,
  - control label,
  - muted helper/status text.
- Use accent color only for primary actions and active state; warnings/errors use existing text color variants.

## Implementation Plan (Designer-First)

### Phase A: Structural Layout
1. Refactor `TradingControl.Designer.cs` into header + left rail + work area.
2. Refactor `PlannerControl.Designer.cs` with fixed action bar and work tabs.
3. Refactor `AutoModeControl.Designer.cs` into grouped cards; migrate pair selector from runtime creation into designer controls.

### Phase B: Live State Surface
1. Add per-page status labels for last operation + timestamp.
2. Route existing log/outcome summaries into those labels.
3. Keep message boxes only for destructive or blocking confirmations.

### Phase C: Visual Consistency
1. Apply consistent card/title spacing and docking rules.
2. Normalize button sizes and alignment across pages.
3. Verify all pages at minimum width 900 and common desktop sizes.

## Prioritized Change List
1. **Auto Mode top-panel decomposition** (largest immediate usability gain).
2. **Planner action/context split** (reduces accidental misuse and improves speed).
3. **Trading config-vs-output separation** (faster manual operation).
4. **Dialog/inline feedback cleanup** (less interruption).
5. **Status/dashboard freshness indicators** (higher trust in live data).

## Validation Criteria
- User can identify active account, symbol scope, and run state in <3 seconds on Planner/Auto pages.
- No operational page has more than one horizontal wrap row of mixed control types.
- All critical actions (`Scan`, `Propose`, `Execute`, `Kill`) remain visible without scrolling.
- Latest data timestamps are visible on Dashboard and operational pages.
- All updated UI remains designer-editable and compiles on .NET Framework 4.8.
