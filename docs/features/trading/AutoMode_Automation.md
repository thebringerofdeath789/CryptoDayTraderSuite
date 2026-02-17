# Auto Mode Automation (Dual Track Specification)

## Purpose
Define the production behavior for Auto Mode so implementation can be completed in small, deterministic steps without placeholders, stubs, or simplified production paths.

## Scope
This specification covers two implementation tracks:
1. **Track A**: Non-interactive Auto loop in a single session.
2. **Track B**: Multi-broker / multi-account profile orchestration with per-profile pair scopes.

---

## Track A: Non-Interactive Auto Loop

### User Intent
A user enables Auto Mode once, selects pairs and account settings, and the system repeatedly executes `Scan -> Propose -> Execute` on current market data without per-trade approvals.

### Required Runtime Behavior
- Auto loop runs on a configurable cadence (default 5 minutes).
- Auto loop executes only one cycle at a time (no overlap/reentry).
- Auto loop restores the user’s last `Auto Run` intent on startup (default OFF when no prior preference exists).
- Kill switch is idempotent and stops future cycle work immediately, including in-progress auto-cycle iteration boundaries.
- Live execution requires explicit `Live Arm` enabled.
- Normal automation outcomes are non-modal (status + logs), not repeated message boxes.

### Guardrails (Mandatory)
- Enforce max trades per cycle.
- Enforce per-symbol cooldown.
- Enforce account max concurrent trades.
- Enforce daily risk stop threshold and day rollover reset.
- If any critical dependency is unavailable (planner/broker/keys), fail closed and log actionable reason.

### Execution Safety
- Entry order placement is allowed only if protective exit path is guaranteed.
- If broker cannot guarantee safe protective exit behavior, entry must be blocked (fail closed) with explicit reason.

### Observability
Per cycle, log at minimum:
- selected pairs count,
- projected rows count,
- proposed plans count,
- executed count,
- skipped counts by reason (cooldown/risk/cap/unsupported),
- error count.

### Cycle Telemetry Export (B5 Validation Support)
- Auto Mode writes one JSON telemetry report per profile-driven cycle to:
  - `%LocalAppData%\\CryptoDayTraderSuite\\automode\\cycle_reports\\cycle_*.json`
- Each report includes:
  - cycle id + start/end timestamps,
  - enabled/processed profile counts,
  - per-profile metrics (`SymbolCount`, `ScanRowCount`, `ProposedCount`, `Executed`, `Failed`, skipped reason counters),
  - per-profile status (`executed`, `completed`, `skipped`, `blocked`) and reason.
- This export is the primary artifact for validating mixed-scope matrix scenarios with configurable selected-pair counts (example: `3`, `All`, `15`) without relying on modal UI output.
- Auto Mode UI also displays a latest-report quick summary row (`Telemetry: ...`) so operators can confirm profile execution health without opening files.
- Cycle telemetry includes `MatrixStatus` (`PASS` / `PARTIAL`) derived from:
  - profile pair-configuration consistency (`Selected` profiles execute with their configured selected-pair count; `All` profiles execute with non-empty runtime universes),
  - observed per-profile guardrail independence (`MatrixIndependentGuardrailsObserved`),
  - per-profile guardrail scope isolation (cooldown and daily-risk accounting keyed by profile scope),
  - failure-containment observation (`MatrixFailureContainmentObserved`) when at least one profile fails/blocks/errors,
  - profile isolation observation (blocked profile does not prevent other profiles from completing).

---

## Track B: Multi-Broker / Multi-Account Profiles

### User Intent
Users can configure different account/broker strategies concurrently, for example:
- 3 selected pairs on Broker A,
- All available pairs on Broker B,
- 15 selected pairs on Broker C.

### Profile Model (Required Fields)
- `ProfileId`
- `AccountId`
- `Enabled`
- `PairScope` (`All` or `Selected`)
- `SelectedPairs`
- `IntervalMinutes` (profile override optional)
- `MaxTradesPerCycle`
- `CooldownMinutes`
- `DailyRiskStopPct`
- `CreatedUtc`
- `UpdatedUtc`

### Profile Runtime Behavior
- Auto engine iterates enabled profiles each cycle.
- Each profile resolves its own symbol universe (`All` or selected list).
- Proposal and execution state is isolated per profile.
- Unhandled exceptions inside one profile are contained to that profile and recorded as profile-level error status.
- Failure in one profile does not block other profiles.
- Profile summaries are visible in UI and logs.

### Broker/Account Routing
- Execution route is strictly determined by profile account service + account mode.
- Unsupported broker capability combinations must be rejected with explicit diagnostics.
- No cross-profile account leakage is allowed.

---

## UI Requirements (Designer-First)
- Profile management panel in Auto Mode:
  - Add profile,
  - Edit profile,
  - Delete profile,
  - Enable/disable profile.
- Pair scope editor:
  - `All pairs` toggle,
  - checked custom list.
- Status area:
  - current auto state,
  - last cycle summary,
  - active profile count.
- All forms/controls must remain editable in Visual Studio Designer.

---

## Persistence Requirements
- Persist Auto profiles to LocalAppData store.
- Include version field in persisted payload for future migration.
- On load failure/corruption, fail closed with backup + diagnostic logging.
- Never auto-enable loop on startup without explicit user action.

---

## Acceptance Criteria

### Track A Complete When
- Auto loop runs unattended for repeated cycles without modal interruptions.
- All guardrails are enforced in execution path.
- Live mode requires explicit arm and respects fail-closed constraints.
- No unhandled exceptions from timer-driven cycle.

### Track B Complete When
- Multiple enabled profiles run in one cycle with isolated outcomes.
- Mixed broker pair scopes (any selected count and/or `All`) are processed correctly.
- Profile-level failures do not halt unrelated profiles.
- Profile settings persist and reload correctly.

### Validation Matrix Procedure (Track B B5)
1. Configure multiple enabled profiles with different pair scopes/counts across different accounts/services (example: `3`, `All`, `15`).
2. Run at least one full Auto cycle.
3. Open latest telemetry JSON in `%LocalAppData%\\CryptoDayTraderSuite\\automode\\cycle_reports`.
4. Confirm each profile row has independent counts/status and that one blocked/failing profile does not prevent others from reporting `completed`/`executed`.

Recommended automated verification (from repo root):

```powershell
.\Util\validate_automode_matrix.ps1 -RequireMixedScopes -RequireSelectedSymbolCounts 3,15 -RequireIndependentGuardrailConfigs -RequireFailureIsolation
```

This command validates the mixed-scope configurable scenario, independent profile guardrail configuration evidence, and failure isolation behavior in one report-driven check.

### Engineering Quality Gates
- No placeholder comments or stubbed branches in production flow.
- No `NotImplementedException` in runtime paths.
- No synthetic hardcoded success/failure paths.
- Build succeeds and regression checks for planner/auto mode pass.

---

## Related Components
- `UI/AutoModeControl.cs`
- `UI/AutoModeControl.Designer.cs`
- `Services/AutoPlannerService.cs`
- `Brokers/*`
- `Services/AccountService.cs`
- `Services/KeyService.cs`
- `Util/Log.cs`
