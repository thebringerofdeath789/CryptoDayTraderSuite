# Code Audit Tracker (Independent)

- Scope: Full code and docs audit for Crypto Day Trader Suite
- This tracker is separate from `PROGRESS_TRACKER.md` and does not replace it.
- Audit started: 2026-02-17

## Audit Phases

- Phase A: Documentation & Copilot instructions compliance
- Phase B: Docs to code consistency audit
- Phase C: Lifecycle and worker audit
- Phase D: Static correctness and bug scan
- Phase E: Orphan, stub, and synthetic path cleanup
- Phase F: Final completeness and execution readiness check

## Findings Legend

- SEV-1: Critical bug, can break execution or money flow
- SEV-2: Serious bug or functional gap, but with obvious workaround
- SEV-3: Minor bug, inconsistency, or maintainability problem
- INFO: Observation, possible cleanup, or documentation gap

---

## Phase A: Documentation & Copilot Instructions Compliance

### A.1 Normative Instruction Sources Located

- `.github/copilot-instructions.md` (normative behavioral and implementation constraints)
- `docs/architecture/SystemMap.md` (architecture layer boundaries)
- `ROADMAP.md` (phase/workstream planning source)
- `PROGRESS_TRACKER.md` (historical and current execution narrative)

### A.2 Required Document Coverage (Requested Set)

| File | Main runtime promises | Freshness | Contradictions / Notes |
|---|---|---|---|
| `ROADMAP.md` | Defines planned/active phases (18/19), exchange expansion, certification, guardrails, and completion gates. | Partially stale | Contains many `[x]` completions that conflict with still-open remediation/checklist items in same file and in `PROGRESS_TRACKER.md`. |
| `PROGRESS_TRACKER.md` | Claims detailed audit/remediation completion status and ongoing operations validation. | Partially stale | Marks numerous `AUDIT-*` items as fixed while roadmap still tracks same areas as open/planned. |
| `.github/copilot-instructions.md` | Architecture-first, source-of-truth docs policy, DI/no-stub/no-synthetic behavior, designer-first UI, changelog/index upkeep. | Current | Normative and internally consistent; adopted for this audit. |
| `docs/features/trading/Strategies.md` | Strategy logic definitions + risk guard behavior. | Current/Partially stale | High-level strategy descriptions appear plausible; runtime-level parameter defaults may have drift risk and require code cross-check in Phase B. |
| `docs/features/trading/Exchanges.md` | Describes Coinbase public/private clients and auth model. | Stale | Private auth section states legacy HMAC `CB-ACCESS-*` flow for `CoinbaseExchangeClient`; recent codebase narrative indicates Advanced JWT/Bearer auth path. |
| `docs/features/trading/MultiExchange_Execution_Checklist.md` | Owner/file/acceptance checklist for Phase 19 execution. | Current/Partially stale | Mixed check state (some completed slices, many open) aligns with ongoing work; needs code verification in later phases. |
| `docs/architecture/AI_Integration_Plan.md` | Sidecar governor every 15m, trade review hook behavior, fail-mode semantics. | Stale | States governor keeps prior bias on failure and planner fail-open if AI offline; other docs/work logs describe fail-safe neutral and stronger gating paths. |
| `docs/ops/AutoMode_Runtime_Snapshot.md` | Scripted runtime snapshot verification behavior and strict gate semantics. | Current | Operationally specific and coherent; should be verified against script behavior in Phase B/C. |
| `docs/ops/AutoMode_Matrix_Validation.md` | Scripted matrix validation for Track B evidence. | Current | Coherent with telemetry artifact model; requires code/tool parity validation later. |
| `docs/ui/MultiExchange_UI_Execution_Plan.md` | Designer-first UI delivery plan and passive-UI boundary. | Current/Target-state | Primarily target-state plan; does not itself prove implementation completion. |
| `docs/index.md` | Canonical documentation map and “read first” architecture pointers. | Current/Partially stale | Index is comprehensive; freshness depends on linked doc accuracy (some linked docs are stale). |

### A.3 Additional Roadmap-Critical Docs Reviewed

| File | Why included in Phase A | Freshness | Notes |
|---|---|---|---|
| `docs/features/trading/MultiExchange_Profit_Architecture.md` | Referenced by roadmap/checklist as Phase 19 architecture baseline. | Current/Target-state | Clear target architecture; must be treated as target unless code parity is proven in Phase B+. |
| `docs/features/trading/MultiExchange_Implementation_MasterPlan.md` | Roadmap-linked detailed execution master plan. | Current/Target-state | Strong execution decomposition; many items remain plan-level. |
| `docs/ops/MultiExchange_Certification_Matrix.md` | Mandatory certification and gating source for Phase 19. | Partially stale | Credential matrix still lists legacy `coinbase-exchange` key/secret/passphrase path, likely divergent from Advanced-only private path direction in active code changes. |
| `docs/features/trading/AutoMode_Automation.md` | Roadmap Phase 18 DoD references this file explicitly. | Current | Strong behavioral spec; implementation parity to be audited in Phase B/C. |

### A.4 Phase A Findings (Docs / Governance)

- **DOC-CODE-MISMATCH-CBX-001**
  - Severity: **SEV-2**
  - Doc: `docs/features/trading/Exchanges.md` (Coinbase Exchange Client authentication section)
  - Code anchor for later verification: `Exchanges/CoinbaseExchangeClient.cs`
  - Issue: Documentation asserts legacy HMAC `CB-ACCESS-*` private auth flow, while current code direction indicates Advanced JWT/Bearer auth for private brokerage endpoints.
  - Recommendation: **Update doc to match code** (if JWT path is canonical), or restore legacy flow in code only if intentionally required and tested.

- **DOC-CODE-MISMATCH-AI-001**
  - Severity: **SEV-2**
  - Doc: `docs/architecture/AI_Integration_Plan.md` (failure mode and fallback semantics)
  - Related code areas for later verification: `Services/AIGovernor.cs`, `Services/AutoPlannerService.cs`, `Services/ChromeSidecar.cs`
  - Issue: Doc states governor “retain previous bias” and planner “fail open” on AI outage; repo progress narrative indicates fail-safe neutral behavior and expanded verification gates.
  - Recommendation: **Update doc to match implemented safety behavior** after confirming runtime logic in Phase B/C.

- **DOC-TRACKER-STATE-001**
  - Severity: **SEV-3**
  - Docs: `ROADMAP.md` and `PROGRESS_TRACKER.md`
  - Issue: Completion status is internally inconsistent across planning and execution trackers (same remediation families appear both fixed and still open/planned).
  - Recommendation: **Reconcile roadmap/progress statuses** using code-evidenced checkpoints; preserve roadmap as plan and progress as evidence log, but align status semantics.

- **DOC-CODE-MISMATCH-CRED-001**
  - Severity: **SEV-3**
  - Doc: `docs/ops/MultiExchange_Certification_Matrix.md` (credential requirement matrix)
  - Related code areas for later verification: `Services/ExchangeCredentialPolicy.cs`, `UI/AccountEditDialog.cs`, `UI/KeyEditDialog.cs`, `Exchanges/CoinbaseExchangeClient.cs`
  - Issue: Credential matrix includes legacy `coinbase-exchange` secret/passphrase requirements that may conflict with Advanced-only Coinbase private auth direction.
  - Recommendation: **Update credential matrix to canonical runtime service/auth policy** after Phase B verification.

### A.5 Missing Expected Documents Check

- Requested/roadmap-critical docs checked in this phase were found in workspace.
- No missing-document finding raised in Phase A.

### A.6 Phase A Exit Status

- **Status: COMPLETE**
- Normative behavior rules identified and adopted (`.github/copilot-instructions.md`).
- Roadmap-mapped documentation baseline established.
- High-confidence stale/contradictory doc findings logged for Phase B code-parity validation.

---

## Phase B: Docs to Code Consistency Audit

### B.1 Runtime Flow Cross-Check Summary

| Flow | Doc baseline | Code anchors checked | Status |
|---|---|---|---|
| App startup and wiring | `docs/architecture/StartupFlow.md`, `docs/architecture/SystemMap.md` | `Program.cs` (`Main`), `MainForm.cs` (`InitializeDependencies`, `BuildModernLayout`, `EnsureAutoModeControlInitialized`) | **Diverged** |
| Manual planner scan/propose/execute | `docs/features/trading/AutoPlanner.md`, `docs/features/trading/Governance.md` | `UI/PlannerControl.cs` (`DoScan`, `DoPropose`, `DoProposeAll`, `DoExecute`), `Services/AutoPlannerService.cs` (`ProjectAsync`, `ProposeWithDiagnosticsAsync`) | **Diverged** |
| Auto Mode execution loop | `docs/features/trading/AutoMode_Automation.md` | `UI/AutoModeControl.cs` (`InitializeAutoTimer`, `RunAutoCycleAsync`, `ExecutePlansForAccountAsync`, telemetry writers) | **Mostly aligned** |
| AI governance and sidecar lifecycle | `docs/architecture/AI_Integration_Plan.md` | `Services/AIGovernor.cs` (`Start`, `LoopAsync`, `RunCycleAsync`), `Services/ChromeSidecar.cs` (connect/reconnect/query pipeline) | **Diverged** |
| Backtesting behavior | `docs/features/trading/Backtesting.md` | `Services/BacktestService.cs` (`RunBacktestAsync`), `Backtest/Backtester.cs` (`Run`) | **Diverged** |
| Rate routing/dataflow | `docs/architecture/DataFlow.md` | `Services/RateRouter.cs`, `Program.cs` (EventBus wiring) | **Diverged** |

### B.2 Phase B Findings (Doc ↔ Code Mismatches)

- **DOC-CODE-MISMATCH-STARTUP-001**
  - Severity: **SEV-2**
  - Doc: `docs/architecture/StartupFlow.md`
  - Code: `Program.cs` (`Main`), `MainForm.cs` (`InitializeDependencies`)
  - Divergence: Doc states legacy static `KeyRegistry` startup and “no background threads until user action”. Code uses DI services (`KeyService`, `AccountService`, etc.) and starts `AIGovernor` at app bootstrap (`aiGovernor.Start()`) before UI loop.
  - Recommendation: **Update startup doc to DI + immediate governor worker lifecycle** and remove static-registry narrative.

- **DOC-CODE-MISMATCH-DATAFLOW-001**
  - Severity: **SEV-2**
  - Doc: `docs/architecture/DataFlow.md`
  - Code: `Program.cs` (creates and publishes via `EventBus`), `MainForm.cs` (`OnLogEvent` subscription)
  - Divergence: Doc claims no global event bus and synchronous-only feedback paths; code has active event bus usage for log/event propagation.
  - Recommendation: **Update dataflow doc to current event-bus architecture** and remove obsolete “future impl” language.

- **DOC-CODE-MISMATCH-AUTOPLANNER-001**
  - Severity: **SEV-2**
  - Doc: `docs/features/trading/AutoPlanner.md`
  - Code: `Services/AutoPlannerService.cs` (service in use), `UI/PlannerControl.cs` and `UI/AutoModeControl.cs` (callers)
  - Divergence: Doc references `Strategy/AutoPlanner.cs` and `AutoPlanner` class as canonical runtime path; file/class do not exist in current code, and runtime uses `AutoPlannerService`.
  - Recommendation: **Rewrite AutoPlanner doc to service-based runtime path** and remove references to non-existent class/file.

- **DOC-CODE-MISMATCH-AUTOPLANNER-002**
  - Severity: **SEV-3**
  - Doc: `docs/features/trading/AutoPlanner.md`
  - Code: `Program.cs` (registered strategies), `Services/AutoPlannerService.cs` (`ProjectAsync`)
  - Divergence: Doc describes planner signal set as Donchian/VWAP Pullback/Range Reversion; code projects over injected runtime strategies (`ORB`, `VWAPTrend`, `RSIReversion`, `Donchian`) and uses strategy-specific live signal checks.
  - Recommendation: **Update strategy set documentation to actual injected strategies and ranking semantics.**

- **DOC-CODE-MISMATCH-GOVERNANCE-001**
  - Severity: **SEV-2**
  - Doc: `docs/features/trading/Governance.md`
  - Code: `UI/PlannerControl.cs`, `UI/AutoModeControl.cs`, `Strategy/TradePlanner.cs`
  - Divergence: Governance doc positions `TradePlanner` as final gatekeeper; active execution paths gate through planner/autopilot service diagnostics + broker capability/validation + runtime guardrails. `TradePlanner` exists but is not the main execution gate in current UI flows.
  - Recommendation: **Update governance doc to reflect actual guardrail chain** (proposal diagnostics, policy matrix, broker validation, cooldown/open-cap/risk-stop), and classify `TradePlanner` role accurately.

- **DOC-CODE-MISMATCH-AI-002**
  - Severity: **SEV-2**
  - Doc: `docs/architecture/AI_Integration_Plan.md`
  - Code: `Services/AIGovernor.cs` (`LoopAsync`), `Services/AutoPlannerService.cs` (`ProposeWithDiagnosticsAsync`)
  - Divergence: Doc says governor retains previous bias on AI failure and planner fails open when AI offline. Code reverts to `Neutral` bias on offline/disconnect and uses fail-closed behavior for invalid AI review contract (explicit `ai-veto` path).
  - Recommendation: **Update AI integration doc to current fail-safe/fail-closed behavior and reconnect loop.**

- **DOC-CODE-MISMATCH-RATEROUTER-001**
  - Severity: **SEV-3**
  - Doc: `docs/architecture/DataFlow.md`
  - Code: `Services/RateRouter.cs`
  - Divergence: Doc states no cache invalidation and Coinbase-only ticker source. Code uses a 15-second TTL cache plus multi-venue composite quote service with Coinbase fallback.
  - Recommendation: **Update dataflow doc to composite quote + TTL cache behavior.**

- **DOC-CODE-MISMATCH-BACKTEST-001**
  - Severity: **SEV-3**
  - Doc: `docs/features/trading/Backtesting.md`
  - Code: `Backtest/Backtester.cs` (`Run`)
  - Divergence: Doc describes close-only/flip-style exits and simplified assumptions; code now executes stop/target intrabar checks (`bar.Low`/`bar.High`) with counter-signal fallback and end-of-series forced close.
  - Recommendation: **Update backtesting doc to current exit model and assumptions.**

### B.3 Phase B Exit Status

- **Status: COMPLETE**
- Core documented runtime flows were mapped to active code paths and mismatch findings were recorded with file/method anchors.
- Auto Mode dual-track behavior appears materially implemented in code and mostly aligned with its dedicated automation spec; older architecture/feature docs remain the main stale sources.

---

## Phase C: Lifecycle and Worker Audit

### C.1 Lifecycle Trace Summary

- Startup path validated: `Program.Main` composes services, starts `AIGovernor`, builds `MainForm`, and enters `Application.Run`.
- Auto worker loop validated: `UI/AutoModeControl` owns `_autoTimer`, enforces `_autoCycleRunning` reentry guard, supports kill switch (`StopAutoRun`), and persists auto-run preference.
- Sidecar worker lifecycle validated: `Services/ChromeSidecar` starts receive pump via `CancellationTokenSource` and stops pump in `Dispose`/reconnect transitions.
- Shutdown path partially validated: `Program.Main` `finally` calls `aiGovernor.Stop()` and `chromeSidecar.Dispose()`.

### C.2 Phase C Findings

- **LIFE-WORKER-LOGGER-001**
  - Severity: **SEV-2**
  - File/Method: `Util/Log.cs` (`Init`, `Shutdown`, `WriterLoop`), `Program.cs` (`Main` shutdown path)
  - Condition: Logger worker (`Task.Factory.StartNew(... LongRunning)`) is started in `Log.Init` but `Log.Shutdown` is never invoked in app shutdown path.
  - Impact: queued log entries can be dropped during process teardown; writer cancellation/flush semantics are not deterministically executed.
  - Recommended fix: invoke `Log.Shutdown()` in `Program.Main` `finally` after service stop/dispose sequence.

- **LIFE-WORKER-GOV-001**
  - Severity: **SEV-3**
  - File/Method: `Services/AIGovernor.cs` (`Start`, `Stop`, `LoopAsync`)
  - Condition: governor loop is fire-and-forget (`Task.Run(LoopAsync)`) with `_running` flag only; no retained loop task handle is awaited on stop.
  - Impact: shutdown ordering is best-effort; loop may still execute briefly against disposing dependencies, increasing noisy shutdown errors/races.
  - Recommended fix: store loop task, add cooperative cancellation token, and await bounded completion in `Stop`/shutdown.

- **LIFE-WORKER-GOV-002**
  - Severity: **SEV-3**
  - File/Method: `Services/AIGovernor.cs` (constructor subscription), `Stop`
  - Condition: `AIGovernor` subscribes to `_sidecar.StatusChanged` but does not unsubscribe on stop/dispose path.
  - Impact: if lifecycle ever changes to recreate governor/sidecar instances, stale handler retention can cause duplicate status propagation.
  - Recommended fix: unsubscribe in `Stop` or implement `IDisposable` with deterministic unhooking.

- **LIFE-AUTOCYCLE-OBS-001**
  - Severity: **INFO**
  - File/Method: `UI/AutoModeControl.cs` (`RunAutoCycleAsync`, `StopAutoRun`, `ExecutePlansForAccountAsync`)
  - Observation: auto-cycle loop includes explicit kill-switch checks, profile-level exception containment, and telemetry write paths for both normal and exceptional cycle exits.
  - Recommendation: keep this path canonical and avoid introducing alternate execution surfaces that bypass these guards.

### C.3 Phase C Exit Status

- **Status: COMPLETE**
- Critical lifecycle controls for auto mode and sidecar are present.
- Shutdown determinism gaps remain in logger and governor worker teardown semantics.

---

## Phase D: Static Correctness and Bug Scan

### D.1 Static Scan Coverage

- Broad scan completed for: exception swallowing, sync-over-async usage, placeholder/synthetic behavior markers, broker validation paths, exchange adapter correctness, and risk-governance enforcement paths.
- Runtime evidence check executed via strict certification task (`strict-cert-once-3`) with latest verdict: **PARTIAL** (environment/provider constrained coverage, not a compile failure).

### D.2 Phase D Findings

- **BUG-RISK-BYPASS-AUTOMODEFORM-001**
  - Severity: **SEV-1**
  - File/Method: `UI/AutoModeForm.cs` (`DoExecute`)
  - Condition: legacy Auto Mode form executes queued plans directly with `broker.PlaceOrderAsync` and bypasses the canonical `AutoModeControl` guardrail chain (cooldown, max-open cap, daily risk stop tracking/telemetry, profile-scoped runtime matrix checks).
  - Impact: if this surface is reachable, live execution can occur without the full runtime safety controls implemented in `UI/AutoModeControl.cs`.
  - Recommended fix: retire or hard-disable `AutoModeForm` execution path, or route all execution through the canonical `AutoModeControl` execution pipeline.

- **BUG-SYNTHETIC-FEE-BITSTAMP-001**
  - Severity: **SEV-2**
  - File/Method: `Exchanges/BitstampClient.cs` (`GetFeesAsync`)
  - Condition: maker fee is synthesized as `maker = taker * 0.75m` with comment `guess maker slightly lower`.
  - Impact: synthetic fee assumptions contaminate expectancy, cost, and rejection decisions where fee model output is treated as real.
  - Recommended fix: source both maker/taker from explicit API fields or return unknown/unsupported with a deterministic fallback policy that is clearly flagged and excluded from strict scoring.

- **BUG-LOCALE-PARSE-KRAKEN-001**
  - Severity: **SEV-2**
  - File/Method: `Exchanges/KrakenClient.cs` (`GetFeesAsync`, `GetBalancesAsync`)
  - Condition: numeric parsing uses `Convert.ToDecimal(string)` on API string payloads without invariant culture.
  - Impact: locale-dependent parsing can fail or misparse on non-`.` decimal cultures, causing incorrect balances/fees or runtime failures.
  - Recommended fix: replace with `decimal.TryParse(..., NumberStyles.Any, CultureInfo.InvariantCulture, out ...)` and hard-fail with explicit diagnostics when parse fails.

- **BUG-RESPONSIVENESS-KEYIMPORT-001**
  - Severity: **SEV-3**
  - File/Method: `UI/KeyEditDialog.cs` (`TryAutoImportCoinbaseReadOnly`)
  - Condition: key-save flow synchronously blocks UI (`GetAwaiter().GetResult()`) on network-backed import validation and trade import.
  - Impact: UI freeze potential on slow network/API degradation, creating apparent app hangs during key save.
  - Recommended fix: convert auto-import invocation to async flow with progress/error UI and cancellation-safe background execution.

- **BUG-RESPONSIVENESS-BROKERVALIDATE-001**
  - Severity: **SEV-3**
  - File/Methods: `Brokers/BinanceBroker.cs`, `Brokers/BybitBroker.cs`, `Brokers/CoinbaseExchangeBroker.cs`, `Brokers/OkxBroker.cs` (`ValidateTradePlan`)
  - Condition: synchronous constraint fetch via `GetSymbolConstraintsAsync(...).GetAwaiter().GetResult()` inside validation path.
  - Impact: proposal/execution validation can block calling thread and increase latency/jank under network stress.
  - Recommended fix: introduce async validation path and prefetch/cache symbol constraints with bounded TTL.

### D.3 Phase D Exit Status

- **Status: COMPLETE**
- Multiple concrete runtime correctness risks were confirmed, including one SEV-1 execution-safety bypass surface and exchange-adapter correctness defects.

---

## Phase E: Orphan, Stub, and Synthetic Path Audit

### E.1 Orphan / Dead-Surface Coverage

- Checked compiled legacy UI modules and wiring reachability (`MainForm_Menu`, `MainForm_ExtraButtons`, `MainForm_Hooks`, `AutoModeForm`).
- Checked duplicate/parallel source folder (`NewFolder2`) against project compile includes.
- Checked strategy/planner legacy artifacts for active references.

### E.2 Phase E Findings

- **ORPHAN-LEGACY-MENU-001**
  - Severity: **SEV-3**
  - Files: `UI/MainForm_Menu.cs`
  - Condition: `MainFormMenu` compiles but has no active attach call in current runtime.
  - Impact: stale surface can drift silently, then re-enter runtime with outdated behavior.
  - Recommended fix: remove file or explicitly wire and certify it under current architecture.

- **ORPHAN-LEGACY-HOOKS-001**
  - Severity: **SEV-3**
  - Files: `UI/MainForm_Hooks.cs`, `UI/MainForm_ExtraButtons.cs`
  - Condition: `OnShown_Hooks` is defined but not wired from active `MainForm` lifecycle; extra-buttons path appears dormant.
  - Impact: dormant UI code includes alternate auto-mode entry (`AutoModeForm`) that bypasses canonical guardrail path if reactivated.
  - Recommended fix: delete dormant hook/button path or rewire through canonical controls only.

- **ORPHAN-DUPLICATE-FOLDER-001**
  - Severity: **SEV-3**
  - Files/Project: `NewFolder2/*`, `CryptoDayTraderSuite.csproj` (`<Folder Include="NewFolder2\" />`)
  - Condition: duplicate broker sources exist in `NewFolder2` but are not compiled (folder include only).
  - Impact: high confusion risk during maintenance and audits; accidental resurrection risk.
  - Recommended fix: remove `NewFolder2` duplicates or convert to explicit archival location outside project root.

- **SYNTHETIC-LEGACY-COINBASEPUBLIC-001**
  - Severity: **SEV-3**
  - File: `Exchanges/CoinbasePublicClient.cs`
  - Condition: legacy sync wrappers (`.Result`) remain for async methods and are unused in current code paths.
  - Impact: latent deadlock/jank hazard if reintroduced from UI thread.
  - Recommended fix: remove legacy synchronous wrappers or route through `ConfigureAwait(false)` safe wrappers with clear usage constraints.

### E.3 Phase E Exit Status

- **Status: COMPLETE**
- Significant dormant/legacy surfaces remain in compiled codebase and should be removed or explicitly governed to prevent regression re-entry.

---

## Phase F: Final Completeness and Execution Readiness Check

### F.1 Consolidated Severity Totals

- **SEV-1:** 1
- **SEV-2:** 9
- **SEV-3:** 14
- **INFO:** 1

### F.2 Readiness Verdict

- **Overall readiness: PARTIAL / NOT READY for strict production confidence**
- Primary blockers:
  - SEV-1 execution-safety bypass surface in legacy auto-mode form path.
  - SEV-2 exchange correctness issues (synthetic fee derivation, locale-sensitive numeric parsing).
  - Broad docs/runtime divergence across architecture, startup, planner, and governance narratives.

### F.3 Runtime Evidence Cross-Check

- Strict certification run completed with `VERDICT=PARTIAL` and environment-constrained venue coverage notes (latest artifact in `obj/runtime_reports/multiexchange/`).
- Build passed; matrix artifacts present and fresh.
- Certification evidence does not negate identified code-level correctness defects above.

### F.4 Recommended Remediation Order

1. Disable/remove `AutoModeForm` execution surface or force canonical execution path reuse.
2. Fix exchange adapter correctness defects (`BitstampClient` synthetic fee inference, `KrakenClient` invariant parsing).
3. Harden sync-over-async hotspots in key import and broker validation paths.
4. Remove dormant/orphan legacy UI and duplicate source folders.
5. Reconcile stale docs to code-truth and align roadmap/progress status semantics.

### F.5 Audit Exit Status

- **Status: COMPLETE**
- Full requested phased audit (A→F) executed and logged to this independent tracker file.

---

## Post-Audit Remediation (Iteration 1) - 2026-02-17

### R1 Changes Applied

- `UI/AutoModeForm.cs`: hard-disabled legacy `DoExecute()` path with explicit warning to use canonical Auto Mode control.
- `Exchanges/BitstampClient.cs`: removed optimistic synthetic maker fee guess (`maker=taker*0.75`); now parses explicit maker/taker fields and uses conservative maker=taker fallback only when maker is absent.
- `Exchanges/KrakenClient.cs`: replaced locale-sensitive numeric conversions with invariant `TryParse` paths for candles, ticker, fees, and balances.
- `UI/KeyEditDialog.cs`: removed UI-thread sync-over-async block for Coinbase auto-import by dispatching import asynchronously.
- `Program.cs`: added `Log.Shutdown()` to deterministic app shutdown path.

### R1 Finding Status Updates

- `BUG-RISK-BYPASS-AUTOMODEFORM-001`: **Mitigated** (legacy execute path blocked).
- `BUG-SYNTHETIC-FEE-BITSTAMP-001`: **Mitigated** (optimistic synthetic derivation removed; conservative fallback documented in code).
- `BUG-LOCALE-PARSE-KRAKEN-001`: **Mitigated** (invariant parsing adopted).
- `BUG-RESPONSIVENESS-KEYIMPORT-001`: **Mitigated** (auto-import no longer blocks UI thread).
- `LIFE-WORKER-LOGGER-001`: **Mitigated** (`Log.Shutdown()` invoked on app exit).

### R1 Verification

- Edited-file diagnostics: no errors reported for changed files.
- Strict certification artifact: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_094128.txt` => `VERDICT=PARTIAL` (geo/provider environment constraints), build and freshness checks pass.

## Post-Audit Remediation (Iteration 2) - 2026-02-17

### R2 Changes Applied

- `CryptoDayTraderSuite.csproj`: removed dormant compile includes for `UI/MainForm_Menu.cs`, `UI/MainForm_Hooks.cs`, `UI/MainForm_ExtraButtons.cs` and removed `<Folder Include="NewFolder2\" />`.
- Deleted dormant legacy UI files: `UI/MainForm_Menu.cs`, `UI/MainForm_Hooks.cs`, `UI/MainForm_ExtraButtons.cs`.
- Deleted duplicate non-compiled broker artifacts: `NewFolder2/AliasBroker.cs`, `NewFolder2/CoinbaseExchangeBroker.cs`, `NewFolder2/IBroker.cs`, `NewFolder2/PaperBroker.cs`.

### R2 Finding Status Updates

- `ORPHAN-LEGACY-MENU-001`: **Mitigated** (unused legacy menu source removed from project and deleted).
- `ORPHAN-LEGACY-HOOKS-001`: **Mitigated** (unused hooks/extra-button path removed from project and deleted).
- `ORPHAN-DUPLICATE-FOLDER-001`: **Mitigated** (duplicate folder removed from project and duplicate sources deleted).

### R2 Verification

- Project diagnostics after cleanup: no errors reported in `CryptoDayTraderSuite.csproj` / `MainForm.cs`.
- Strict certification artifact: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_094930.txt` => `VERDICT=PARTIAL` (environment constraints only), build and freshness checks pass.

## Post-Audit Remediation (Iteration 3) - 2026-02-17

### R3 Changes Applied

- Fully retired legacy `AutoModeForm` surface:
  - removed project compile includes for `UI/AutoModeForm.cs` and `UI/AutoModeForm.Designer.cs`.
  - deleted both files from repository.
- Introduced dedicated broker-layer factory file `Brokers/BrokerFactory.cs` and moved broker resolution there to preserve canonical `AutoModeControl` execution compile/runtime behavior.

### R3 Verification

- Initial strict run failed on missing `BrokerFactory` symbol after form deletion (expected transitional build break).
- Follow-up strict run after broker factory extraction succeeded to baseline `PARTIAL`:
  - `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_154208.txt` => `VERDICT=PARTIAL`
  - Build PASS, matrix PASS, provider/reject freshness PASS.

## Post-Audit Remediation (Iteration 4) - 2026-02-17

### R4 Changes Applied

- Added async broker validation contract in `Brokers/IBroker.cs`: `ValidateTradePlanAsync(TradePlan plan)`.
- Implemented async validation paths in all active brokers:
  - `Brokers/BinanceBroker.cs`
  - `Brokers/BybitBroker.cs`
  - `Brokers/CoinbaseExchangeBroker.cs`
  - `Brokers/OkxBroker.cs`
  - `Brokers/PaperBroker.cs`
- Updated execution call sites to use awaited async validation:
  - `UI/PlannerControl.cs` now awaits `broker.ValidateTradePlanAsync(plan)`.
  - `UI/AutoModeControl.cs` now awaits `broker.ValidateTradePlanAsync(p)`.

### R4 Finding Status Updates

- `BUG-RESPONSIVENESS-BROKERVALIDATE-001`: **Mitigated** for canonical execution paths (planner + auto mode now async validation).

### R4 Verification

- Diagnostics: no errors in modified broker/interface/UI files.
- Strict certification artifact: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_155532.txt` => `VERDICT=PARTIAL`.
- Strict telemetry indicates no strict gate failures (`STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`), with expected geo/provider partial classification.

## Post-Audit Remediation (Iteration 5) - 2026-02-17

### R5 Changes Applied

- Removed the remaining synchronous broker validation API from the active code path:
  - deleted `ValidateTradePlan(...)` from `Brokers/IBroker.cs`.
  - deleted sync wrapper implementations from `Brokers/BinanceBroker.cs`, `Brokers/BybitBroker.cs`, `Brokers/CoinbaseExchangeBroker.cs`, `Brokers/OkxBroker.cs`, `Brokers/PaperBroker.cs`.
- Canonical runtime now uses async-only pre-trade validation (`ValidateTradePlanAsync(...)`) in active execution call paths.

### R5 Finding Status Updates

- `BUG-RESPONSIVENESS-BROKERVALIDATE-001`: **Closed** (sync validation surface removed from interface + active implementations).

### R5 Verification

- Diagnostics: no errors in modified broker/interface/UI files.
- Strict certification artifact: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_155903.txt` => `VERDICT=PARTIAL`.
- Strict telemetry remains clean: `STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`.
- Fresh strict rerun artifact: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_160427.txt` => `VERDICT=PARTIAL`, `StrictFailureClass=NONE`, `StrictFailures=count=0`.
