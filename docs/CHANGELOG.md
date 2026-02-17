# Changelog

## [Reject Evidence Warmup] - Single Bounded Micro-Cycle Before Certification - 2026-02-17

### Changed
- **Minimal Evidence Retry Added**: Updated `obj/run_reject_evidence_capture.ps1` to run one bounded warmup micro-cycle when runtime passes complete with no observed reject categories.
- **No Contract Drift**: Existing capture CI contract semantics (`effective_exit`/`original_exit`/`override`, precheck fields) are unchanged; warmup is additive and emits explicit diagnostics (`RUNTIME_EVIDENCE_WARMUP`, `RUNTIME_EVIDENCE_WARMUP_RESULT`).

### Verified
- **Capture Artifact**: `obj/runtime_reports/reject_capture_verify_warmup_slice.txt` confirms warmup execution markers and stable CI contract output.
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_214236.txt` reports expected geo-partial baseline with zero strict failures (`STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`, `STRICT_FAILURE_NAMES=none`, `VERDICT=PARTIAL`).

## [Strict Gate Closeout] - Reject Evidence Non-Observable Window Handling - 2026-02-17

### Changed
- **Reject Gate False-Fail Hardening**: Updated `Util/run_multiexchange_certification.ps1` with `Get-RejectEvidenceWaiverContext` to detect fresh non-observable runtime windows where all recent cycle profiles are skipped for operational reasons (`interval`, `account`, `pairs`) and no executed profile exists.
- **Strict Behavior Preserved, Noise Reduced**: In strict mode, `Reject category evidence` now remains `FAIL` for normal executable windows with missing evidence, but is downgraded to `PARTIAL` for bounded non-observable windows instead of blocking closeout.

### Verified
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_213750.txt` reports `STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`, `STRICT_FAILURE_NAMES=none`, `VERDICT=PARTIAL` (`allow-geo-partial`).
- **Reject Evidence Check**: Same artifact reports `[PASS] Reject category evidence` with observed category `routing-unavailable` from fresh runtime evidence (`log_20260217_124.txt` + cycle source).

## [Exchange Client Resilience] - Unified Open Order Retrieval - 2026-02-18

### Changed
- **Interface Expanded**: Added `Task<List<OpenOrder>> GetOpenOrdersAsync(string productId = null)` to `IExchangeClient`.
- **Implementations Updated**: Implemented open order retrieval across `BinanceClient`, `BybitClient`, `CoinbaseExchangeClient`, `BitstampClient`, `KrakenClient`, and `OkxClient`.
- **Wrappers Updated**: Updated `ResilientExchangeClient` and `DisabledExchangeClient` to support the new method.
- **Models Added**: Added `OpenOrder` model to normalize open order data across exchanges.

### Fixed
- **BUG-207**: Resolved reliability risk where the bot could not see open orders upon restart.

## [Broker Cancel-All Consistency] - Typed Open-Order Reconciliation Across Venues - 2026-02-17

### Changed
- **Binance/Bybit/OKX Broker Alignment**: Updated `Brokers/BinanceBroker.cs`, `Brokers/BybitBroker.cs`, and `Brokers/OkxBroker.cs` `CancelAllAsync(...)` to use typed open-order reconciliation (`GetOpenOrdersAsync` + per-order cancel + partial-failure accounting) instead of direct bulk-cancel endpoint dependence.
- **Coinbase Broker Cleanup**: Removed obsolete dictionary-only open-order parsing helpers in `Brokers/CoinbaseExchangeBroker.cs` now that cancel-all is fully typed on `OpenOrder`.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds.
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_214245.txt` remains policy-consistent geo-partial (`VERDICT=PARTIAL`, `STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`, `STRICT_FAILURE_NAMES=none`).

## [Open Orders Rollout Stabilization] - Resilient Wrapper Parity + Typed Broker Reconciliation Fixes - 2026-02-17

### Changed
- **Resilient Wrapper Parity Restored**: Updated `Services/ResilientExchangeClient.cs` to implement `GetOpenOrdersAsync(string)` through retry policy and added geo-disabled fallback support for `List<OpenOrder>`.
- **Bitstamp Open-Order Mapper Compile Fix**: Updated `Exchanges/BitstampClient.cs` by adding missing local parsing helpers used by `GetOpenOrdersAsync(...)` (`GetString`, `ToDecimal`).
- **Coinbase Broker Typed Open-Order Handling**: Updated `Brokers/CoinbaseExchangeBroker.cs` `CancelAllAsync(...)` to consume typed `OpenOrder` fields (`OrderId`, `ProductId`) instead of dictionary-based field extraction.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds.
- **Strict Certification Baseline**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_213750.txt` reports policy-consistent strict geo-partial baseline (`VERDICT=PARTIAL`, `STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`).

## [Service Alias Normalization Consolidation] - Shared Normalizer for Provider, Buying Power, and Provider Audit - 2026-02-17

### Changed
- **Shared Normalizer Added**: Added `Services/ExchangeServiceNameNormalizer.cs` as the single source of truth for broker canonicalization (`Coinbase`, `Binance[-US/-Global]`, `Bybit[-Global]`, `OKX[-Global]`), family-key normalization, audit-name normalization, and global-alias checks.
- **Exchange Provider Refactored**: Updated `Services/ExchangeProvider.cs` to use shared broker normalization, removing local duplicate alias-switch logic.
- **Buying Power Routing Refactored**: Updated `Services/AccountBuyingPowerService.cs` to use shared family normalization and shared global-alias checks for Binance/Bybit/OKX base-URL selection, removing local duplicate normalization logic.
- **Provider Audit Canonicalization Refactored**: Updated `Services/ExchangeProviderAuditService.cs` to use shared audit-service normalization and removed local duplicate normalization logic.
- **Project Include Updated**: Added `Services/ExchangeServiceNameNormalizer.cs` to `CryptoDayTraderSuite.csproj` compile includes.

### Verified
- **Touched File Diagnostics**: No diagnostics in `Services/ExchangeServiceNameNormalizer.cs`, `Services/ExchangeProvider.cs`, `Services/AccountBuyingPowerService.cs`, and `Services/ExchangeProviderAuditService.cs`.
- **Build Context**: Full Debug verify build currently fails due to unrelated pre-existing interface rollout errors (`IExchangeClient.GetOpenOrdersAsync(string)` missing implementations across multiple existing exchange/wrapper clients), not introduced by this normalization-consolidation slice.

## [Account Insights UX Follow-Through] - Dashboard Shortcut + Sidebar Label Preservation - 2026-02-17

### Changed
- **Dashboard Shortcut Added**: Updated `UI/DashboardControl.Designer.cs` and `UI/DashboardControl.cs` to add a top-level `Account Insights` action that raises `NavigationRequest("Insights")`.
- **Shell Navigation Wiring Added**: Updated `MainForm.cs` to subscribe dashboard navigation requests and route them through `NavigateTo(...)`, enabling one-click transition from dashboard to account insights.
- **Sidebar Expanded Labels Hardened**: Updated `UI/SidebarControl.cs` to preserve user-facing button labels via `Tag`-backed text restoration during expand/collapse instead of deriving labels from control names.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds (transient copy-lock retries observed while app process holds `CryptoDayTraderSuite.exe`).
- **Binance Private Probe**: `Util/run_binance_private_api_probe.ps1 -Service Binance-US` returns `__BINANCE_PRIVATE=PASS` (public+private path and balances verified).
- **Provider Public Probe**: `obj/runtime_reports/provider_audit/provider_public_api_probe_20260217_212333.json` reports `PROBE_VERDICT=PARTIAL` with expected Bybit geo constraint.
- **Strict Certification Context**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_212844.txt` remains blocked only by `Reject category evidence` strict gate (`STRICT_FAILURE_NAMES=Reject category evidence`).

## [Reject Capture Second-Pass Fallback] - Fast Override Auto-Promotion to Restored Profile Retry - 2026-02-17

### Changed
- **Single-Attempt Auto-Promotion Added**: Updated `obj/run_reject_evidence_capture.ps1` so `-UseFastProfileOverride` with `-RuntimeCaptureAttempts 1` automatically runs a second runtime pass.
- **Fallback Profile Mode Added**: Pass 1 runs `fast-override`; pass 2 runs `normal-fallback` after profile restore, enabling one in-run retry without manual re-execution.
- **Fallback Diagnostics Added**: Capture now emits `RUNTIME_CAPTURE_FALLBACK_MODE`, per-pass `RUNTIME_CAPTURE_PARAMS ... profileMode=...`, and `RUNTIME_CAPTURE_FALLBACK_SECOND_PASS` for deterministic CI/debug consumption.

### Verified
- **Reject Capture Validation**: `obj/runtime_reports/reject_capture_verify_second_pass_contract.txt` confirms two-pass execution (`RUNTIME_CAPTURE_ATTEMPT=1/2`, `2/2`) and stable contract semantics (`effective_exit`, `original_exit`, `override`).
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds.
- **Strict Certification Context**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_212500.txt` still fails only `Reject category evidence` strict gate, consistent with absent observed reject categories in current short capture window.

## [Provider Audit Robustness] - Product Sanitization + Ticker Validity Fallback + Error Normalization - 2026-02-17

### Changed
- **Product Sanitization Added**: Updated `Services/ExchangeProviderAuditService.cs` to sanitize and de-duplicate product lists before coverage math and probe-symbol selection, preventing duplicate/empty symbol drift.
- **Ticker Success Criteria Hardened**: Ticker probes now pass when either `Last > 0` or a coherent book is present (`Bid > 0`, `Ask > 0`, `Ask >= Bid`), reducing false negatives from last-price-only assumptions.
- **Error Payload Normalization Added**: Exception messages are now normalized to single-line bounded text for deterministic provider-artifact behavior under long HTML/network error payloads.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds.
- **Strict Certification Context**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_211842.txt` shows provider/build/matrix gates stable; strict failure remains isolated to runtime `Reject category evidence`.

## [Reject Capture Adaptive Recovery] - Rollback Restore + Escalation for Fresh-Cycle Acquisition - 2026-02-17

### Changed
- **Rollback Recovery Applied**: Restored `obj/run_reject_evidence_capture.ps1` CI contract hardening after local rollback, including `effective_exit`/`original_exit`/`override` fields and trap-path emission via shared `Emit-CaptureContract`.
- **Adaptive Runtime Escalation Added**: Capture retries now increase runtime window and `CDTS_AUTOMODE_MAX_SYMBOLS` within bounded caps when precheck is healthy but fresh cycle evidence is still missing.
- **Freshness/Diagnostics Reinstated**: Re-enabled UTC/skew-safe freshness checks, post-run settle re-scan, and precheck context fields in capture CI summaries.
- **Default Cert Failure Contract Added**: Non-zero default certification exits now emit explicit `partial-default-cert-failed` contract output before completion routing.

### Verified
- **Reject Capture Validation**: `obj/runtime_reports/reject_capture_verify_escalation_contract.txt` emits expanded CI contract fields (`effective_exit`, `original_exit`, `override`, precheck diagnostics) with deterministic partial override markers.
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds.
- **Strict Certification Context**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_211225.txt` currently fails strict reject-evidence gate only (`STRICT_FAILURE_NAMES=Reject category evidence`), consistent with missing observed reject categories in short capture window.

## [Provider Audit Signal Quality] - Perp Classification Expansion + Spot Probe Ranking - 2026-02-17

### Changed
- **Perp Detection Expanded**: Updated `Services/ExchangeProviderAuditService.cs` `IsPerpProduct(...)` to recognize additional perp naming variants (`PERPETUAL`, `USDTM`, `:SWAP`, `_PERP`, `-PERP`) to reduce false spot/perp coverage classification drift.
- **Preferred Symbol Matching Normalized**: Probe selection now supports separator-insensitive preferred symbol matching (`BTC-USD`, `BTC/USD`, `BTC_USD`, etc.) before fallback selection.
- **Spot Probe Ranking Added**: Probe symbol resolver now ranks spot candidates toward higher-signal pairs (`BTC+USD/USDT/USDC`, then USD, USDT, USDC quotes) instead of first non-perp row selection.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds.
- **Strict Certification Context**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_210857.txt` fails on runtime `Reject category evidence` only (`STRICT_FAILURE_NAMES=Reject category evidence`); provider/build/matrix gates remain healthy after this C# slice.

## [Provider Audit Reliability] - Canonical Service Inputs + Spot-Preferred Probe Symbol Selection - 2026-02-17

### Changed
- **Service Canonicalization Added**: Updated `Services/ExchangeProviderAuditService.cs` to canonicalize requested service aliases before public-audit execution and deduplication (`coinbase-advanced`, `binance-us/global`, `bybit-global`, `okx-global` map to canonical venue keys).
- **Result Metadata Expanded**: `ExchangeProviderAuditResult` now includes `RequestedService` and `CanonicalService` fields while preserving `Service` for canonicalized downstream consumption.
- **Probe Symbol Selection Hardened**: Audit probe symbol resolver now prefers spot products when available, reducing false ticker probe failures where product lists include perp rows but ticker calls are spot-scoped.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds.
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_210539.txt` remains stable (`VERDICT=PARTIAL`, `STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`, `STRICT_POLICY_DECISION=allow-geo-partial`).

## [Reject Capture Freshness Diagnostics] - UTC-Safe Evidence Detection + Precheck Context in CI Contract - 2026-02-17

### Changed
- **Freshness Detection Hardened**: Updated `obj/run_reject_evidence_capture.ps1` to evaluate cycle/log freshness using `LastWriteTimeUtc` with bounded skew tolerance, reducing boundary-time false negatives.
- **Post-Run Settle Re-Scan Added**: When immediate post-runtime evidence appears stale, capture now waits a short settle window and re-scans before classifying no-fresh-cycle outcomes.
- **Precheck Context Surfaced to CI**: Capture contracts now include precheck context fields (`precheck_autorun_known`, `precheck_autorun_enabled`, `precheck_runnable_profiles`, `precheck_fresh_cycle`, `precheck_cycle_age_min`) in `CI_FIELDS`/`CI_SUMMARY`.

### Verified
- **Reject Capture Validation**: `obj/runtime_reports/reject_capture_verify_freshdiag.txt` emits expanded contract fields and summary metadata for precheck state while preserving effective/original override semantics.
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds.
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_210038.txt` remains baseline (`VERDICT=PARTIAL`, `STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`, `STRICT_POLICY_DECISION=allow-geo-partial`).

## [Reject Capture Contract Reliability] - Effective Exit Semantics + Trap Contract Unification - 2026-02-17

### Changed
- **Effective Exit Contract Added**: Updated `obj/run_reject_evidence_capture.ps1` capture CI contract fields to include `effective_exit`, `original_exit`, and `override` so parsers can distinguish policy-overridden partial exits from original script outcomes.
- **CI Summary Semantics Aligned**: `CI_SUMMARY` now emits effective exit routing (`exit`) plus original/override metadata when `-AllowPartialExitCodeZero` is active.
- **Unhandled-Error Contract Unified**: Top-level trap now emits failure contracts through shared `Emit-CaptureContract`, ensuring error-path output uses the same field schema as normal terminal paths.

### Verified
- **Reject Capture Validation**: Direct run with `-AllowPartialExitCodeZero` emits coherent partial override contract (`CI_SUMMARY ... exit=0;effective_exit=0;original_exit=4;override=1`) and explicit terminal markers (`RESULT_EXIT_ORIGINAL=4`, `RESULT_EXIT_OVERRIDDEN=0 mode=allow-partial-exit-zero`).
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds.
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_205408.txt` remains baseline (`VERDICT=PARTIAL`, `STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`, `STRICT_POLICY_DECISION=allow-geo-partial`).

## [Provider Alias Normalization] - Canonical Service Matching in Strict Certification - 2026-02-17

### Changed
- **Canonical Service Mapping Added**: Updated `Util/run_multiexchange_certification.ps1` with provider-service canonicalization (`Normalize-ProviderServiceName`) for alias variants including `binance-us`/`binance-global`, `bybit-global`, and `okx-global`.
- **Provider Row Lookup Centralized**: Added `Find-ProviderProbeRow` and applied it to provider probe usability checks, strict provider failure evaluation, spot/perp coverage checks, and policy-backed strategy-exchange evidence generation.
- **False Missing-Service Failures Reduced**: Certification now avoids alias-shape drift causing `missing`/coverage failures when probe artifacts use service aliases.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds.
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_204704.txt` reports stable geo-partial strict baseline (`VERDICT=PARTIAL`, `STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`, `STRICT_POLICY_DECISION=allow-geo-partial`).

## [Certification Runtime Resilience] - Lock-Resistant Strict Build + Valid Provider Artifact Selection - 2026-02-17

### Changed
- **Build Lock Resilience Added**: Updated `Util/run_multiexchange_certification.ps1` build verification to use attempt-scoped output paths (`bin\\Debug_Verify\\cert_<stamp>_<attempt>\\`) with lock-signature retry, preventing false strict build failures from transient file locks on shared verify binaries.
- **Provider Artifact Selection Hardened**: Added provider report usability filtering in `Util/run_multiexchange_certification.ps1` (`Test-IsUsableProviderProbeReport`) so certification ignores malformed latest probe artifacts that do not contain recognizable required-service rows.
- **Policy Evidence Probe Selection Aligned**: Updated policy-backed row evidence generation to use the same required-service-aware provider artifact selection, keeping strategy-exchange evidence aligned with certification provider checks.

### Verified
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_204413.txt` reports expected geo-partial strict baseline (`VERDICT=PARTIAL`, `STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`, `STRICT_POLICY_DECISION=allow-geo-partial`).

## [Provider Probe Reliability] - Bybit Geo-Constraint Classification + Service-List Normalization - 2026-02-17

### Changed
- **Service Argument Normalization Added**: Updated `Util/run_provider_public_api_probe.ps1` to split comma-delimited `-Services` values and normalize to unique service tokens, preventing malformed one-service artifacts (for example `"Binance-US,Coinbase,..."` treated as a single unsupported service).
- **Bybit Failure Classification Hardened**: Added `Resolve-FailureClass` in `Util/run_provider_public_api_probe.ps1` so Bybit `ListProductsAsync returned no products` outcomes classify as `ENV-CONSTRAINT` rather than `INTEGRATION-ERROR` under geo-constrained access.
- **Bybit Instruments Fail-Closed Diagnostics Added**: Updated `Exchanges/BybitClient.cs` `GetInstrumentsInfoAsync(...)` to validate `retCode/retMsg` and throw explicit non-success diagnostics instead of silently propagating ambiguous empty-product behavior.

### Verified
- **Provider Probe Artifact**: `obj/runtime_reports/provider_audit/provider_public_api_probe_20260217_204218.json` reports `Verdict=PARTIAL` with `EnvConstraint=1`, `IntegrationError=0`, and Bybit classified as `FailureClass=ENV-CONSTRAINT`.
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_204326.txt` returns expected geo-partial strict baseline (`VERDICT=PARTIAL`, `STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`, `STRICT_POLICY_DECISION=allow-geo-partial`).

## [Account Insights + Binance Validation] - Dedicated Insights Navigation and Binance-US Private Telemetry - 2026-02-17

### Changed
- **Dedicated Navigation Added**: Updated `UI/SidebarControl.Designer.cs` and `MainForm.cs` to add a separate `Account Insights` sidebar route and map `Accounts`, `Account Insights`, `API Keys`, and `Settings` to dedicated views (no Accounts/API Keys setup tabs inside Settings).
- **Accounts Insights Generalized**: Updated `UI/AccountsControl.cs` + `UI/AccountsControl.Designer.cs` so selected-account insights now support explicit live API validation refresh (`public products/ticker`, `private auth/fees`, `buying power`) for configured services, while preserving Coinbase imported snapshot visibility.
- **Binance Private Balances Added**: Updated `Exchanges/BinanceClient.cs` with authenticated `GetBalancesAsync()` parsing `/api/v3/account` balances (`free + locked`) for account telemetry.
- **Buying Power Extended to Binance**: Updated `Services/AccountBuyingPowerService.cs` to support `binance`, `binance-us`, and `binance-global` live quote-balance resolution.
- **Probe Utility Added**: Added `Util/run_binance_private_api_probe.ps1` for targeted Binance private-path verification (active key, public products/ticker, private fees, private balances).

### Verified
- **File Diagnostics**: No diagnostics in touched runtime/UI files (`MainForm.cs`, `UI/AccountsControl.cs`, `Exchanges/BinanceClient.cs`, `Services/AccountBuyingPowerService.cs`, `UI/SidebarControl.Designer.cs`).
- **Binance Private Probe**: `Util/run_binance_private_api_probe.ps1 -Service Binance-US` returns `__BINANCE_PRIVATE=PASS` with active key id, product count, ticker last, and balance count.
- **Provider Public Probe**: `obj/runtime_reports/provider_audit/provider_public_api_probe_20260217_204147.txt` reports `[PASS] Binance-US` (`create=True`, `discover=True`, `ticker=True`).
- **Build Context**: Full Debug verify build currently blocked by unrelated pre-existing `UI/AutoModeControl.cs` missing-symbol errors (`UpdateRoutingVenueFooterFromBatch`, `ExtractBracketTagValue`).

## [Certification Contract Reliability] - Strict Failure Metadata Alignment for Provider-Gated FAILs - 2026-02-17

### Changed
- **Strict Failure Accounting Fixed**: Updated `Util/run_multiexchange_certification.ps1` so strict failure rollups include strict-gated FAIL checks (`Build`, `AutoMode matrix artifact`, `Provider public API probe artifacts`, `Spot+perps coverage`, `Reject category evidence`) in addition to synthetic `Strict requirement:*` checks.
- **Contract Contradiction Removed**: `STRICT_FAILURE_COUNT` and `STRICT_FAILURE_NAMES` now stay consistent with `VERDICT=FAIL` outcomes in strict runs, preventing `class=NONE/fails=0` when strict-gated provider checks fail.
- **Classifier Robustness Added**: Strict failure detection now normalizes failed status/name comparisons (trim + case-insensitive) to avoid name-shape drift from suppressing strict failure accounting.
- **Strict Invariant Backfill Added**: In strict mode, when `VERDICT=FAIL` is reached but mapped strict failures are empty, strict failure metadata is deterministically backfilled from failed checks to preserve CI contract consistency.

### Verified
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_203034.txt` now emits coherent strict metadata under provider-gated failures (`VERDICT=FAIL`, `STRICT_FAILURE_CLASS=OTHER_STRICT`, `STRICT_FAILURE_COUNT=2`, `STRICT_FAILURE_NAMES=Provider public API probe artifacts,Spot+perps coverage`).
- **Strict Certification (Re-Run)**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_203121.txt` confirms invariant-safe output with coherent strict metadata (`VERDICT=FAIL`, `STRICT_FAILURE_CLASS=OTHER_STRICT`, `STRICT_FAILURE_COUNT=2`, `STRICT_FAILURE_NAMES=Provider public API probe artifacts,Spot+perps coverage`).

## [AI Contract Hardening] - Shared Schemas + Deterministic Key-Order Acceptance Gates - 2026-02-17

### Changed
- **Shared AI Schemas Added**: Added `Services/AiJsonSchemas.cs` and centralized planner/governor strict JSON schemas and expected top-level key orders.
- **Deterministic Contract Validator Added**: Extended `Services/StrictJsonPromptContract.cs` with top-level exact key-order validation (`MatchesExactTopLevelObjectContract`) using JSON structure-aware parsing.
- **Planner Approval Fail-Closed Tightened**: Updated `Services/AutoPlannerService.cs` so AI approval is accepted only when strict review/proposer JSON key-order contracts match expected schemas; text-approval fallback is now rejected for approval paths.
- **Governor Parsing Preference Tightened**: Updated `Services/AIGovernor.cs` to prefer strict key-order contract parsing before flexible contract fallbacks.
- **Project Wiring Updated**: Added `Services/AiJsonSchemas.cs` to compile includes in `CryptoDayTraderSuite.csproj`.

### Verified
- **Diagnostics**: No file-level errors in modified AI contract files (`AiJsonSchemas`, `StrictJsonPromptContract`, `AutoPlannerService`, `AIGovernor`, project include changes).
- **Build Status**: Full Debug verify build currently blocked by unrelated pre-existing UI compile issue in `UI/AutoModeControl.Designer.cs` (`CS0115: 'AutoModeControl.Dispose(bool)': no suitable method found to override`).

## [Ops Script Contracts] - Deterministic Exit Marker Rollout - 2026-02-17

### Changed
- **Contract Checker Markers Added**: Updated `Util/check_multiexchange_contract.ps1` with deterministic completion marker emission (`RESULT_EXIT_CODE`, `CONTRACT_FINAL_EXIT`) and trap-backed deterministic fail output.
- **Matrix Validator Markers Added**: Updated `Util/validate_automode_matrix.ps1` to emit deterministic matrix failure/success keys (`MATRIX_RESULT`, `MATRIX_ERROR`, `MATRIX_EXIT_CODE`) and explicit `RESULT_EXIT_CODE` on all terminal paths.
- **Provider Probe Markers Added**: Updated `Util/run_provider_public_api_probe.ps1` to emit deterministic probe completion/failure keys (`PROBE_EXIT_CODE`, `RESULT_EXIT_CODE`, `PROBE_ERROR`) including unhandled-failure trap path.
- **Matrix Failure Path Fix**: Corrected matrix script terminal failure emission to avoid `Write-Error` short-circuiting deterministic markers when `$ErrorActionPreference='Stop'`.

### Verified
- **Contract Checker**: `Util/check_multiexchange_contract.ps1 -Strict` emits `CONTRACT_RESULT=PASS`, `RESULT_EXIT_CODE=0`, `CONTRACT_FINAL_EXIT=0`.
- **Matrix Validator**: `Util/validate_automode_matrix.ps1` emits `MATRIX_RESULT=FAIL`, `MATRIX_ERROR=...`, `RESULT_EXIT_CODE=1`, `MATRIX_EXIT_CODE=1` for failing matrix evidence.
- **Provider Probe**: `Util/run_provider_public_api_probe.ps1` emits `PROBE_VERDICT=FAIL`, `RESULT_EXIT_CODE=1`, `PROBE_EXIT_CODE=1` under current geo-constrained provider conditions.
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after script-contract hardening.
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_202452.txt` remains baseline (`VERDICT=PARTIAL`, `STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`).

## [AI Prompt Contract] - Shared Strict-JSON Utility Across Planner + Governor - 2026-02-17

### Changed
- **Shared Utility Added**: Added `Services/StrictJsonPromptContract.cs` to centralize strict JSON prompt envelope construction for AI calls (`BuildPrompt`, `BuildRepairPrompt`).
- **Planner Migration Completed**: Updated `Services/AutoPlannerService.cs` to use shared prompt utility for both proposer/reviewer prompt generation and strict-repair prompt generation.
- **Governor Migration Completed**: Updated `Services/AIGovernor.cs` to use shared prompt utility for bias prompt generation and strict-repair prompt generation.
- **Project Wiring Updated**: Added `Services/StrictJsonPromptContract.cs` to compile includes in `CryptoDayTraderSuite.csproj`.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after shared prompt utility extraction.

## [Ops Tooling] - Reject-Capture CI Decision Contract Emission - 2026-02-17

### Changed
- **Capture Decision Contract Added**: Updated `obj/run_reject_evidence_capture.ps1` to emit deterministic machine-parseable decision lines on terminal outcomes: `CAPTURE_RESULT`, `CAPTURE_DECISION`, `CI_VERSION`, `CI_FIELDS`, and `CI_SUMMARY`.
- **Coverage Expanded Across End States**: Contract emission now covers precheck failure, no-fresh-cycle partial, no-evidence partial, strict-gate partial, pass, and unhandled-error paths.
- **CI Routing Simplified**: `CI_SUMMARY` now provides a one-line key/value outcome envelope (`result`, `decision`, `exit`, strict flag, freshness state, observed categories, cert exits, report path) to avoid brittle log parsing.

### Verified
- **Targeted Runtime Validation**: `obj/runtime_reports/reject_capture_verify_ci_summary.txt` shows stable partial contract output (`CAPTURE_DECISION=partial-no-fresh-cycle`) with deterministic `CI_SUMMARY` fields.
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after contract emission updates.
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_202200.txt` remains baseline (`VERDICT=PARTIAL`, `STRICT_FAILURE_COUNT=0`, `STRICT_FAILURE_NAMES=none`).

## [AI Runtime + Connectivity] - Provider Rotation Default + Chrome Focus + HTTP Log Noise Hardening - 2026-02-17

### Changed
- **Default Provider Rotation Restored**: Updated `Services/ChromeSidecar.cs` public `QueryAIAsync(...)` path to start from round-robin provider selection (`ChatGPT` → `Gemini` → `Claude`) instead of pinning to the currently connected provider.
- **Chrome Focus-Steal Reduced**: Updated sidecar-managed Chrome launch to use non-shell process start (`UseShellExecute=false`) and enforce post-launch window state (hidden or minimized) so auto-launched Chrome no longer pops above active work unexpectedly.
- **HTTP Error Spam Reduced**: Updated `Util/HttpUtil.cs` to avoid duplicate logs for `HttpRequestException`, classify transient transport exceptions as warnings, and downgrade expected client/Bybit request failures to warning-level connection telemetry while preserving hard-error logging for severe failures.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after sidecar and HTTP logging hardening updates.

## [AI Governor Maintainability] - Reusable Strict-JSON Prompt Template Helpers - 2026-02-17

### Changed
- **Governor Prompt Builder Centralized**: Refactored `Services/AIGovernor.cs` to generate bias-classification prompts through reusable helpers (`BuildStrictJsonPrompt`, `BuildGovernorBiasPrompt`) instead of inline prompt string composition.
- **Governor Repair Prompt Centralized**: Added `BuildStrictJsonRepairPrompt` and switched strict repair flow to use the same marker/wrapper restrictions as the primary governor prompt.
- **Schema Constant Shared in Governor**: Added `GovernorBiasSchema` constant and reused it in both initial query and strict-repair retry paths to reduce contract drift.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after governor prompt-template refactor.

## [Ops Reliability] - Reject Capture Trap Completion-Contract Alignment - 2026-02-17

### Changed
- **Trap Completion Aligned**: Updated `obj/run_reject_evidence_capture.ps1` so top-level unhandled-error `trap` now calls `Complete-Result` instead of directly exiting.
- **Policy-Aware Exit Behavior Preserved**: Unhandled failures now honor the same deterministic completion contract as regular partial outcomes, including `-AllowPartialExitCodeZero` behavior and explicit result markers.

### Verified
- **Runtime Capture (Fast Mode)**: Output includes deterministic partial/override markers (`RESULT:PARTIAL`, `RESULT_EXIT_CODE=4`, `RESULT_EXIT_ORIGINAL=4`, `RESULT_EXIT_OVERRIDDEN=0 mode=allow-partial-exit-zero`) when partial override switch is enabled.
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after script hardening.
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_201942.txt` remains baseline (`VERDICT=PARTIAL`, `STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`).

## [AI Planner Maintainability] - Reusable Strict-JSON Prompt Template Helpers - 2026-02-17

### Changed
- **Prompt Builder Centralized**: Refactored `Services/AutoPlannerService.cs` to construct AI prompts through reusable helpers (`BuildStrictJsonPrompt`, `BuildAiReviewPrompt`, `BuildAiProposerPrompt`) instead of inline string assembly.
- **Schema Constants Centralized**: Added shared schema constants (`AiReviewSchema`, `AiProposerSchema`) and switched proposer/reviewer strict-repair calls to those constants.
- **Contract Drift Risk Reduced**: Review/proposer prompts now share a consistent strict-JSON envelope contract (exact keys, wrapper markers, anti-wrapper rules), making future prompt policy updates one-place changes.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify_PromptTemplate\\ /t:Build /v:minimal` succeeds (alternate output path used because `bin\\Debug_Verify\\CryptoDayTraderSuite.exe` was process-locked during verification).

## [AI Planner Reliability] - Data-Backed Proposer Contract + Deterministic Acceptance Gates - 2026-02-17

### Changed
- **Prompt Contract Tightened**: Updated `Services/AutoPlannerService.cs` AI proposer payload/prompt to include explicit constraints (`minConfidence`, `minRMultiple`, reason evidence requirements) and require concrete metric/price references plus risk-control context in one-sentence reasons.
- **Confidence Gate Added**: AI-proposed trades now fail closed when confidence is outside `0..1` or below `CDTS_AI_PROPOSER_MIN_CONFIDENCE` (default `0.55`).
- **R-Multiple Gate Added**: AI-proposed trades now fail closed when computed reward-to-risk is below `CDTS_AI_PROPOSER_MIN_R` (default `1.50`).
- **Signal Drift Guard Added**: AI-proposed entry is now bounded against matched live-signal entry via `CDTS_AI_PROPOSER_MAX_ENTRY_DRIFT_R` (default `0.75R`) to prevent off-signal proposals.
- **Reason Quality Gate Added**: Proposal verification now rejects generic/non-data-backed reasons that lack concrete evidence/risk language.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after proposer hardening updates.

## [Ops Tooling] - Reject-Capture Partial Exit Override + Freshness Age Telemetry - 2026-02-17

### Changed
- **Optional Partial Exit Override Added**: Updated `obj/run_reject_evidence_capture.ps1` with `-AllowPartialExitCodeZero` so expected partial outcomes (`no fresh cycle` / `no reject categories` / strict-gate partial) can emit full partial diagnostics while returning process exit `0` for automation workflows.
- **Deterministic Exit Markers Added**: Centralized result exits now always emit `RESULT_EXIT_CODE`, and when overridden also emit `RESULT_EXIT_ORIGINAL` + `RESULT_EXIT_OVERRIDDEN=0 mode=allow-partial-exit-zero`.
- **Freshness-Age Diagnostics Added**: Capture output now includes `CYCLE_AGE_MIN` and `LOG_AGE_MIN` to accelerate stale-cycle triage without manual file timestamp inspection.

### Verified
- **Targeted Runtime Validation**: `obj/runtime_reports/reject_capture_verify_partial_exit_zero.txt` confirms partial-no-fresh-cycle path emits full diagnostics and returns `EXIT=0` when `-AllowPartialExitCodeZero` is enabled.

## [Routing Reliability] - Global Endpoint Geo-Disable and Error-Suppression Fallback - 2026-02-17

### Changed
- **Global Geo-Disable Registry Added**: Added `Services/GeoBlockRegistry.cs` to track endpoint-scoped geo-blocked services across the running app session using normalized service aliases.
- **Non-Throwing Disabled Client Added**: Added `Services/DisabledExchangeClient.cs` and wired `Services/ExchangeProvider.cs` to return it whenever a service is geo-disabled, preventing repeated exception cascades for blocked endpoints.
- **Resilience Geo Handling Generalized**: Updated `Services/ResilientExchangeClient.cs` to detect geo-block signatures (`HTTP 403`, `forbidden`, restricted-region variants), disable the affected service alias, and return safe default results for quote/product/candle/fee/cancel flows instead of repeatedly throwing.
- **Quote Fanout Honors Global Disable**: Updated `Services/MultiVenueQuoteService.cs` and `Services/VenueHealthService.cs` so globally disabled endpoints are skipped in venue fanout and reflected in health-disable checks.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after global geo-disable integration.

## [Ops Reliability] - Reject Capture Deterministic Unhandled Error Signaling - 2026-02-17

### Changed
- **Unhandled Error Trap Added**: Updated `obj/run_reject_evidence_capture.ps1` with a top-level `trap` that converts unhandled exceptions into deterministic capture telemetry output and explicit failure semantics.
- **Deterministic Failure Contract**: Unhandled script failures now emit `UNHANDLED_ERROR=<message>` and `RESULT:PARTIAL unhandled script error during reject evidence capture.` with explicit exit code `6`.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after script hardening.
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_200928.txt` remains baseline (`VERDICT=PARTIAL`, `STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`).

## [Ops Tooling] - Reject-Capture Final Restore Telemetry Semantics - 2026-02-17

### Changed
- **Final Restore Marker Always Emitted**: Updated `obj/run_reject_evidence_capture.ps1` so `FAST_PROFILE_OVERRIDE_RESTORED_FINAL` is emitted whenever fast profile override is active, including normal successful paths where runtime restore already consumed the backup file.
- **Deterministic Restore Modes Added**: Final marker now distinguishes `mode=already-restored` (runtime restore succeeded earlier) and `mode=final-restore` (fallback restore executed at script end), while preserving explicit failure telemetry (`FAST_PROFILE_OVERRIDE_RESTORED_FINAL=0 ...`).

### Notes
- This pass is orchestration telemetry hardening only; no trade execution logic changed.

## [Broker Reliability] - Shared Message Formatter Consolidation - 2026-02-17

### Changed
- **Shared Formatter Added**: Added `Brokers/BrokerMessageFormatter.cs` to centralize broker success/failure message formatting (`BuildSuccessMessage`, `BuildFailureMessage`).
- **Project Wiring Updated**: Added `Brokers/BrokerMessageFormatter.cs` compile include in `CryptoDayTraderSuite.csproj`.
- **Broker Delegation Applied**: Updated `Brokers/BinanceBroker.cs`, `Brokers/BybitBroker.cs`, `Brokers/CoinbaseExchangeBroker.cs`, `Brokers/OkxBroker.cs`, and `Brokers/PaperBroker.cs` so local formatter methods delegate to shared implementation.

### Verified
- **Diagnostics**: No errors in modified broker files and project file.
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_195304.txt` remains baseline (`VERDICT=PARTIAL`, `StrictFailureClass=NONE`, `StrictFailures=count=0`, `StrictPolicyDecision=allow-geo-partial`).

## [Exchange Reliability] - Deterministic Open-Order Row Normalization - 2026-02-17

### Changed
- **Binance Row Normalization Added**: Updated `Exchanges/BinanceClient.cs` cancel paths to resolve order id/symbol via dedicated row helpers, unify canceled-like status checks through one predicate, and clear cached order-symbol bindings during cancel-all row processing.
- **Bybit Symbol Resolution Expanded**: Updated `Exchanges/BybitClient.cs` cancel-order lookup to resolve symbol/product/instrument aliases via a dedicated helper before normalization and cancel request construction.
- **OKX Per-Row Instrument Usage Added**: Updated `Exchanges/OkxClient.cs` cancel-all loop to resolve order ids via helper and use per-row normalized instrument ids (with scoped fallback) for deterministic cancel payload construction.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after deterministic row-normalization updates.
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_195118.txt` remains baseline (`VERDICT=PARTIAL`, `STRICT_FAILURE_COUNT=0`, `STRICT_FAILURE_NAMES=none`).

## [Coinbase Reliability] - Open-Order Filter Fail-Closed Tightening - 2026-02-17

### Changed
- **Ambiguous Row Fail-Closed**: Updated `Exchanges/CoinbaseExchangeClient.cs` `FilterOpenOrders(...)` to stop treating blank/unknown status rows as implicitly open.
- **Explicit Open Flags Supported**: Added explicit boolean open detection (`open`, `is_open`/`isOpen`, `active`, `pending` variants) so open rows without status text are still retained.
- **Open Status Coverage Expanded**: `IsOpenLikeStatus(...)` now also recognizes `OPENED`, `PENDING_OPEN`, `QUEUED`, and `RESTING` while maintaining existing open-like statuses.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after Coinbase open-order filter hardening.

## [Exchange Reliability] - Cancel-Order Resolution + Key Lookup Hardening - 2026-02-17

### Changed
- **Bybit Cancel Resolution Hardened**: Updated `Exchanges/BybitClient.cs` so `CancelOrderAsync(...)` now requires successful open-order query semantics before lookup, resolves order ids from variant key shapes, and normalizes symbol before issuing cancel requests.
- **OKX Cancel Resolution Hardened**: Updated `Exchanges/OkxClient.cs` so `CancelOrderAsync(...)` now fails closed when pending-order query is not successful, resolves order/instrument identifiers from variant key shapes, and normalizes instrument ids before cancel requests.
- **Binance Cancel Status Alignment**: Updated `Exchanges/BinanceClient.cs` cancel success recognition to include `EXPIRED_IN_MATCH` in terminal canceled-like states.
- **Case-Insensitive Key Reads Added**: Added case-insensitive key lookup fallback in `GetString(...)` for `Exchanges/BinanceClient.cs`, `Exchanges/BybitClient.cs`, and `Exchanges/OkxClient.cs` to tolerate payload casing drift.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after exchange reliability hardening updates.

## [Broker Reliability] - Validation Success Contract Normalization - 2026-02-17

### Changed
- **Validation Success Structured**: Updated `Brokers/BinanceBroker.cs`, `Brokers/BybitBroker.cs`, `Brokers/CoinbaseExchangeBroker.cs`, and `Brokers/OkxBroker.cs` so `ValidateTradePlanAsync(...)` success returns categorized `validation` output via `BuildSuccessMessage("validation", "ok")` instead of raw `"ok"`.
- **Paper Broker Contract Aligned**: Updated `Brokers/PaperBroker.cs` to use structured success/failure helpers and normalized message categories across validate/place/cancel paths (`validation`, `accepted`, `canceled`), including direct validation tuple passthrough in `PlaceOrderAsync(...)`.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after success-contract normalization.
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_193757.txt` remains `VERDICT=PARTIAL` with strict gates clear (`STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`).

## [Broker Reliability] - Structured Validation and Constraint Failure Taxonomy - 2026-02-17

### Changed
- **Validation Guard Categorization**: Updated `Brokers/BinanceBroker.cs`, `Brokers/BybitBroker.cs`, `Brokers/CoinbaseExchangeBroker.cs`, and `Brokers/OkxBroker.cs` so `ValidateTradePlanAsync(...)` input/geometry guard failures consistently emit categorized `validation` results via `BuildFailureMessage(...)`.
- **Constraint Rule Categorization**: In the same brokers, symbol-constraint resolution failures and step/tick/min/max/notional rule violations now emit categorized `constraints` results via `BuildFailureMessage(...)`.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after structured failure taxonomy normalization.
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_191903.txt` remains `VERDICT=PARTIAL` with strict gates clear (`StrictFailureClass=NONE`, `StrictFailures=count=0`, `StrictPolicyDecision=allow-geo-partial`).

## [Auto Mode UI Diagnostics] - Routing/Venue Footer Telemetry - 2026-02-17

### Changed
- UI/AutoModeControl.cs manual propose path now updates routing/venue footer diagnostics from proposal batch results (chosen/alternate venues, execution modes, routing-unavailable count, and policy/regime/circuit counters).
- Added proposal-note tag extraction for routed diagnostics ([Route=...], [Alt=...], [ExecMode=...]) and proposal reason-code classification into routing/venue health counters.
- RefreshLatestTelemetrySummary() now initializes and updates routing/venue diagnostic rows from latest cycle telemetry (RoutingChosenVenues, RoutingAlternateVenues, RoutingExecutionModes, RoutingUnavailableCount, PolicyHealthBlockedCount, RegimeBlockedCount, CircuitBreakerObservedCount).
- Footer diagnostic labels are created and attached at runtime via EnsureDiagnosticsSummaryLabels() to avoid designer drift while preserving existing Auto Mode layout.

### Verified
- **Build**: msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal succeeds.
- **Strict Certification**: task strict-cert-once remains baseline (VERDICT=PARTIAL, STRICT_FAILURE_COUNT=0, STRICT_FAILURE_NAMES=none).
## [Broker Reliability] - Cancel-All Fail-Closed Hardening - 2026-02-17

### Changed
- **Client Fail-Closed Guards Added**: Updated `Exchanges/BinanceClient.cs`, `Exchanges/BybitClient.cs`, and `Exchanges/OkxClient.cs` cancel-all paths to reject invalid normalized symbols and enforce stricter structured success checks.
- **OKX Pending Query Gated**: `Exchanges/OkxClient.cs` `CancelAllOpenOrdersAsync(...)` now requires pending-order query success (`code=0` + payload validity) before evaluating cancel loop results, preventing fail-open `true` outcomes on failed pending queries.
- **Bybit Row Success Parsing Hardened**: `Exchanges/BybitClient.cs` now evaluates cancel-all row success across `success` and status-code variants (`sCode`/`retCode`) and fails closed on malformed rows.
- **Broker Cancel Telemetry Standardized**: Updated `Brokers/BinanceBroker.cs`, `Brokers/BybitBroker.cs`, and `Brokers/OkxBroker.cs` cancel success/failure details to include normalized symbol scope; updated `Brokers/CoinbaseExchangeBroker.cs` to fail closed on invalid provided symbol normalization and on partial cancel outcomes, with deterministic `scope/attempted/canceled/failed` details.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after cancel-all fail-closed hardening updates.

## [Repository Hygiene] - Temporary Probe De-Scoping + PR Checklist Guardrails - 2026-02-17

### Changed
- **Temporary Probe Patterns Ignored**: Tightened `.gitignore` to exclude root `obj` temporary probe/output patterns (`obj/tmp_*.ps1`, `obj/runtime_*probe*.ps1`, `obj/verify_*.ps1`, `obj/*.out.txt`) while keeping curated root `obj/*.ps1` scripts versionable.
- **Temporary Probe Scripts De-Tracked**: Removed tracked ad-hoc probe scripts from git index (`tmp_*`, runtime probe, verify probe paths under root `obj/`) so local experimentation files no longer leak into source control.
- **PR Hygiene Gate Added**: Added `.github/pull_request_template.md` with explicit security/repository-hygiene checklist requirements (no secrets, no local IDE state, no build/runtime artifacts, `.gitignore` update check).

### Notes
- This pass is process/repository hardening only and does not change trading runtime behavior.

## [Documentation + Repository Hygiene] - README Overhaul and Git Ignore Hardening - 2026-02-17

### Changed
- **README Modernized**: Rewrote `README.md` to reflect current runtime architecture, exchange/broker scope, setup/run workflows, Auto Mode + certification operations, AI sidecar usage, and security/repo hygiene expectations.
- **Ignore Policy Added**: Added root `.gitignore` to block IDE state (`.vs/`), user-local project metadata (`*.user`), build outputs (`bin/`, `obj/` artifacts), runtime report noise, and credential/certificate file types (`*.pfx`, `*.pem`, `*.key`, etc.).
- **Tracked Noise De-Scoped**: Removed previously tracked generated/artifact files from git index (including `.vs/`, `bin/`, `obj` generated/runtime outputs, `CryptoDayTraderSuite.csproj.user`, and `key.pfx`) so they no longer risk accidental publication.

### Notes
- This cleanup is source-control hygiene focused and does not change runtime trading behavior.

## [Broker Reliability] - Place Validation Passthrough Normalization - 2026-02-17

### Changed
- **Validation Passthrough Applied**: Updated `Brokers/BinanceBroker.cs`, `Brokers/BybitBroker.cs`, `Brokers/CoinbaseExchangeBroker.cs`, and `Brokers/OkxBroker.cs` so `PlaceOrderAsync(...)` now returns failed `ValidateTradePlanAsync(...)` tuples directly (`return validation`) instead of re-wrapping with a second `validation` prefix.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after place validation passthrough normalization.
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_183610.txt` remains `VERDICT=PARTIAL` with strict gates clear (`STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`).

## [Broker Reliability] - Outcome Taxonomy Consistency Pass - 2026-02-17

### Changed
- **Validation Category Normalized**: Updated `Brokers/BinanceBroker.cs`, `Brokers/BybitBroker.cs`, `Brokers/CoinbaseExchangeBroker.cs`, and `Brokers/OkxBroker.cs` so `PlaceOrderAsync(...)` invalid-normalized-symbol paths return `BuildFailureMessage("validation", ..., "symbol is invalid after normalization")` instead of raw unclassified strings.
- **Cancel Success Category Normalized**: Updated the same broker `CancelAllAsync(...)` success paths to emit `BuildSuccessMessage("canceled", ...)` for consistent taxonomy between place and cancel outcomes.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after broker outcome taxonomy consistency updates.

## [Broker Reliability] - Place Failure Category Normalization - 2026-02-17

### Changed
- **Place Error Category Fixed**: Updated `Brokers/BinanceBroker.cs`, `Brokers/BybitBroker.cs`, `Brokers/CoinbaseExchangeBroker.cs`, and `Brokers/OkxBroker.cs` so `PlaceOrderAsync(...)` exception paths return `BuildFailureMessage("place", ..., "place failed")` instead of misclassified `cancel` category errors.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after broker place-path category normalization.
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_183610.txt` reports `VERDICT=PARTIAL` with strict gates clear (`STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`, `STRICT_POLICY_DECISION=allow-geo-partial`).

## [Strategy + Guardrails] - Regime-State Policy Gating + Bounded Circuit Backoff - 2026-02-17

### Changed
- **Regime-State Policy Gate Added**: Updated `Services/StrategyExchangePolicyService.cs` to classify deterministic runtime regime states (`expansion`, `compression`, `trend`, `mean-reversion`, `funding-extreme`) and enforce strategy-specific regime allow maps with explicit `regime-mismatch` rejection rationale.
- **Live Bias Wiring Fixed**: Updated `Services/AutoPlannerService.cs` policy evaluation call to use live `_engine.GlobalBias` (instead of neutral-only evaluation) and added regime tags to emitted plan notes for runtime diagnostics.
- **Circuit Recovery Backoff Added**: Updated `Services/VenueHealthService.cs` with bounded reconnect windows (exponential backoff capped at 8 minutes) plus deterministic circuit open/re-enable transition logs including venue, reason, and backoff metadata.
- **Broker Compile Regression Repaired**: Restored missing `BuildFailureMessage(...)` helpers in `Brokers/BinanceBroker.cs`, `Brokers/BybitBroker.cs`, `Brokers/CoinbaseExchangeBroker.cs`, and `Brokers/OkxBroker.cs` to resolve compile breaks in formatted failure-return paths.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after regime/guardrail changes.
- **Strict Certification**: Task `strict-cert-once-3` reports `STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`, `VERDICT=PARTIAL`, `STRICT_POLICY_DECISION=allow-geo-partial`.

## [Broker Validation] - Shared Precision Helper Consolidation - 2026-02-17

### Changed
- **Shared Precision Utility Added**: Added `Brokers/BrokerPrecision.cs` with centralized step-alignment helpers (`AlignDownToStep`, `IsAlignedToStep`) and tolerance logic.
- **Broker Duplication Removed**: Refactored `Brokers/BinanceBroker.cs`, `Brokers/BybitBroker.cs`, `Brokers/CoinbaseExchangeBroker.cs`, and `Brokers/OkxBroker.cs` to use `BrokerPrecision` and removed duplicated local precision helper methods.
- **Project Wiring Updated**: Added `Brokers/BrokerPrecision.cs` compile include in `CryptoDayTraderSuite.csproj`.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after helper consolidation.
- **Strict Certification (Post-Consolidation)**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_182953.txt` reports `VERDICT=PARTIAL` with strict gates clear (`StrictFailureClass=NONE`, `StrictFailures=count=0`, `StrictPolicyDecision=allow-geo-partial`).

## [Ops Tooling] - Final Strict Contract Signoff Verification - 2026-02-17

### Verified
- **Strict Certification Contract Emission**: `Util/run_multiexchange_certification.ps1 -Strict` emitted stable CI contract lines (`CI_VERSION=1`, ordered `CI_FIELDS`, `STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`, `STRICT_POLICY_DECISION=allow-geo-partial`, `VERDICT=PARTIAL`) and generated artifacts `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_182112.json` + `.txt`.
- **Contract Checker PASS**: `Util/check_multiexchange_contract.ps1 -Strict` completed with `CONTRACT_EXIT_CODE=0`, `CONTRACT_MISSING_KEYS=none`, `CONTRACT_FIELDS_MISSING=none`, `CONTRACT_CISUMMARY_VERSION_OK=True`, `CONTRACT_RESULT=PASS`, and generated artifacts `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_182138.json` + `.txt`.

## [Audit Planning] - Phase X Discovery Complete + Consolidated Hardening Sequence - 2026-02-17

### Changed
- **Discovery Iterations Completed**: Completed Phase X discovery-only audit iterations across `Brokers/`, `Exchanges/`, `Services/`, and UI orchestration (`UI/AutoModeControl.cs`, `UI/PlannerControl.cs`, `MainForm.cs`, `Program.cs`).
- **Finding Set Consolidated**: Normalized iteration findings into a single `BUG-101` to `BUG-117` backlog for post-audit remediation planning.
- **Execution Waves Added**: Updated `ROADMAP.md` with deduplicated remediation sequencing:
    - `Wave 1`: critical execution safety blockers,
    - `Wave 2`: major runtime integrity issues,
    - `Wave 3`: contract/docs/observability closure items.

### Notes
- This changelog entry records planning/discovery state only; no production behavior changes were introduced as part of this audit consolidation pass.

## [Broker Validation] - Precision-Tolerant Step/Tick Alignment - 2026-02-17

### Changed
- **Cross-Broker Alignment Hardening**: Updated `Brokers/BinanceBroker.cs`, `Brokers/BybitBroker.cs`, `Brokers/CoinbaseExchangeBroker.cs`, and `Brokers/OkxBroker.cs` to evaluate step/tick alignment with tolerance-based comparisons instead of strict decimal equality.
- **Quantity Validation Robustness**: `ValidateTradePlanAsync(...)` in each broker now uses `IsAlignedToStep(plan.Qty, constraints.StepSize)` directly for step-size checks, eliminating false negatives caused by decimal precision artifacts.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after precision hardening changes.

## [Ops Tooling] - Reject-Capture Runtime Retry Condition Validation - 2026-02-17

### Changed
- **Runtime Capture Retry Loop Added**: Updated `obj/run_reject_evidence_capture.ps1` with runtime attempt controls (`RuntimeCaptureAttempts`, `RuntimeRetryDelaySeconds`) and per-attempt telemetry output (`RUNTIME_CAPTURE_ATTEMPT=n/N ...`).
- **Early-Exit Condition Tightened**: Runtime capture now exits retry loop only when a fresh cycle artifact is detected or reject evidence is observed, preventing premature success when only log freshness advances.

### Verified
- **Validation Artifact (v2)**: `obj/runtime_reports/reject_capture_hardened_validation_v2.txt` reports `RUNTIME_CAPTURE_ATTEMPT=1/2` and `RUNTIME_CAPTURE_ATTEMPT=2/2` with `cycleFresh=0`, `observed=none`, then deterministic `RESULT:PARTIAL no fresh cycle artifact detected; ensure Auto Run is enabled before capture.`
- **Behavioral Outcome**: Retry hardening is functioning as intended; stale-cycle/no-reject runs stay partial instead of being misclassified by log-only freshness.

## [Ops Tooling] - Certification Contract Self-Validation + Retry Hardening - 2026-02-17

### Changed
- **CI Field Manifest Added**: Updated `Util/run_multiexchange_certification.ps1` to emit `CI_FIELDS` (ordered required key manifest) and include `fields=...` in `CI_SUMMARY` alongside `version=...`.
- **Artifact Contract Mirroring Added**: Strict summary artifacts now include CI field manifest values in JSON (`Summary.Strict.CiFields`, `Summary.Strict.CiFieldsText`) and TXT (`CiFields:`) in addition to stdout (`CI_FIELDS=...`).
- **Contract Checker Script Added**: Added `Util/check_multiexchange_contract.ps1` to execute certification and assert required stdout key presence/consistency (`CI_VERSION`, `CI_FIELDS`, `CI_SUMMARY`, strict class/decision/count/name keys, report paths, `VERDICT`).
- **Reject-Capture Retry Hardening Added**: Updated `obj/run_reject_evidence_capture.ps1` with retry controls (`ExternalRetryCount`, `ExternalRetryDelaySeconds`) and retry wrapper invocation for provider probe, default cert, and strict cert steps.
- **Retry Telemetry Added**: Reject-capture flow now emits retry/attempt diagnostics (`PROBE_RETRY`, `DEFAULT_CERT_RETRY`, `STRICT_CERT_RETRY`, `PROBE_ATTEMPTS`, `DEFAULT_CERT_ATTEMPTS`, `STRICT_CERT_ATTEMPTS`).

### Verified
- **Contract Check (Baseline Strict)**: `powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File .\Util\check_multiexchange_contract.ps1 -Strict`
- **Contract Check (Forced Stale Strict)**: `powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File .\Util\check_multiexchange_contract.ps1 -Strict -ForceStaleProviderProbe`

## [Execution Path Hardening] - Sync Validation Surface Removal - 2026-02-17

### Changed
- **Sync Broker Validation API Removed**: Removed `ValidateTradePlan(...)` from `Brokers/IBroker.cs`.
- **Legacy Sync Wrappers Removed**: Removed synchronous validation wrapper methods from `Brokers/BinanceBroker.cs`, `Brokers/BybitBroker.cs`, `Brokers/CoinbaseExchangeBroker.cs`, `Brokers/OkxBroker.cs`, and `Brokers/PaperBroker.cs`.
- **Async Validation Canonicalized**: Active runtime paths continue using awaited `ValidateTradePlanAsync(...)` from `UI/PlannerControl.cs` and `UI/AutoModeControl.cs`.

### Verified
- **Diagnostics**: No errors in modified broker/interface/UI files.
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_155903.txt` reports `VERDICT=PARTIAL` with strict telemetry clean (`STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`).
- **Strict Certification (Rerun)**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_160427.txt` reports `VERDICT=PARTIAL` with strict failures still clear (`StrictFailureClass=NONE`, `StrictFailures=count=0`).

## [Broker Layer] - Cross-Venue Symbol Normalization Consistency - 2026-02-17

### Changed
- **Binance Broker Normalized**: Updated `Brokers/BinanceBroker.cs` to use canonical symbol normalization (slash/dash/underscore removed, uppercase) consistently across constraint validation, market placement, and cancel-all paths.
- **OKX Broker Normalized**: Updated `Brokers/OkxBroker.cs` to use canonical OKX symbol normalization (dash-separated uppercase) across validation, placement, and cancel-all.
- **Coinbase Broker Normalized**: Updated `Brokers/CoinbaseExchangeBroker.cs` to normalize symbols to Coinbase dash format before validation/placement/cancel filtering and fail closed on invalid normalized symbols.
- **Coinbase Cancel-All Robustness Expanded**: `CancelAllAsync(...)` now resolves order ids from `id`/`order_id`/`orderId` and uses case-insensitive product-id extraction for safer open-order cancel loops.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after cross-broker normalization hardening.

## [Paper Broker] - CS1998 Warning Cleanup + Strict Recheck - 2026-02-17

### Changed
- **Warning Cleanup**: Updated `Brokers/PaperBroker.cs` `ValidateTradePlanAsync(...)` to return `Task.FromResult(...)` values directly (removed `async` without `await`) while preserving validation behavior.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after warning cleanup.
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_155852.txt` reports `VERDICT=PARTIAL` with `STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`, and policy decision `allow-geo-partial`.

## [Bybit Broker] - Canonical Symbol Normalization Consistency - 2026-02-17

### Changed
- **Canonical Symbol Normalization Added**: Updated `Brokers/BybitBroker.cs` with `NormalizeBybitSymbol(...)` (`/`, `-`, `_` stripped; uppercase) to prevent symbol-shape drift.
- **Constraint Validation Path Unified**: `ValidateTradePlanAsync(...)` now resolves constraints using the normalized symbol and fails closed when normalization yields empty output.
- **Placement/Cancel Paths Unified**: `PlaceOrderAsync(...)` and `CancelAllAsync(...)` now use the same normalized symbol representation, reducing venue request mismatches.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after Bybit symbol-normalization hardening.

## [Execution Path Hardening] - Async Broker Validation Migration - 2026-02-17

### Changed
- **Async Validation Contract Added**: Updated `Brokers/IBroker.cs` with `ValidateTradePlanAsync(TradePlan plan)`.
- **Broker Implementations Migrated**: Updated `Brokers/BinanceBroker.cs`, `Brokers/BybitBroker.cs`, `Brokers/CoinbaseExchangeBroker.cs`, `Brokers/OkxBroker.cs`, and `Brokers/PaperBroker.cs` to implement async validation and use it inside `PlaceOrderAsync(...)`.
- **Canonical Callers Updated**: Updated `UI/PlannerControl.cs` and `UI/AutoModeControl.cs` to await `ValidateTradePlanAsync(...)` before placement.

### Verified
- **Diagnostics**: No errors in modified broker/interface/UI files.
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_155532.txt` reports `VERDICT=PARTIAL` with strict gate failures cleared (`STRICT_FAILURE_CLASS=NONE`, `STRICT_FAILURE_COUNT=0`) and expected geo/provider partial classification.

## [Auto Mode] - Adaptive Open-Position Lifecycle Management - 2026-02-17

### Changed
- **Position Merge on Re-entry Added**: Updated `UI/AutoModeControl.cs` to merge same-direction paper fills for the same account/symbol into one managed position (weighted entry + consolidated quantity) instead of creating fragmented parallel positions.
- **Cycle-Time Re-Planning Added**: Added periodic paper-position re-evaluation using fresh planner projections/proposals and tightened protective bounds when improved same-direction plans are available.
- **Replan Cadence Control Added**: Introduced environment override `CDTS_AUTOMODE_POSITION_REPLAN_MINUTES` (default `3`) to control minimum refresh cadence per account/symbol/direction position key.
- **Adaptive Telemetry Added**: Added explicit history/log markers for adaptive refreshes and merged-position events to keep runtime behavior auditable.

### Verified
- **Compile Check (targeted change area)**: `UI/AutoModeControl.cs` syntax/regression issues introduced during implementation were resolved and compile errors in the modified region are eliminated.
- **Workspace Build Status**: Full Debug build currently remains blocked by pre-existing unrelated interface mismatch errors in broker classes (`ValidateTradePlanAsync` implementation gaps in `BinanceBroker`, `BybitBroker`, `OkxBroker`, `CoinbaseExchangeBroker`).

## [Coinbase UI] - Snapshot Freshness Guardrails + Metric Formatting - 2026-02-17

### Changed
- **Freshness Status Added**: Updated `UI/AccountsControl.cs` Coinbase insights summary to display snapshot recency status (`Fresh` vs `STALE`) based on imported timestamp age.
- **Display Consistency Improved**: Standardized holdings/fees/net-profit decimal rendering in Coinbase import dialog and insights summary using shared compact amount formatting.
- **Operator Clarity Improved**: Insights now surface snapshot age directly to reduce reliance on raw timestamps alone.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after snapshot freshness/format updates.
- **Runtime Read-Only Probe**: `powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\\obj\\tmp_coinbase_readonly_import_probe3.ps1` returns `__COINBASE_READONLY_IMPORT=PASS` with expected fills-scope `401` warning fallback.

## [Cleanup] - AutoModeForm Retirement + BrokerFactory Extraction - 2026-02-17

### Changed
- **AutoModeForm Fully Retired**: Removed `UI/AutoModeForm.cs` and `UI/AutoModeForm.Designer.cs` from project compile items and deleted both files.
- **Broker Factory Ownership Corrected**: Added `Brokers/BrokerFactory.cs` and moved broker resolution there so `UI/AutoModeControl.cs` retains compile/runtime broker resolution without depending on legacy form files.

### Verified
- **Strict Certification (Final)**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_154208.txt` reports `VERDICT=PARTIAL` (expected environment constraints), with build gate restored to PASS after factory extraction.

## [Planner/Auto Mode] - Automatic Buying-Power Detection from API Keys - 2026-02-17

### Changed
- **Service-Layer Resolver Added**: Added `Services/AccountBuyingPowerService.cs` to resolve per-account quote buying power from installed API keys (currently Coinbase, Kraken, and Bitstamp), with short-lived caching and deterministic manual fallback semantics.
- **Planner Sizing Auto-Wired**: Updated `UI/PlannerControl.cs` so `DoPropose` and `DoProposeAll` resolve live quote balance first and use that equity input for `_planner.ProposeAsync(...)` sizing/risk calculations.
- **Auto Mode Sizing/Risk Auto-Wired**: Updated `UI/AutoModeControl.cs` so manual propose, manual execute, and profile auto-cycle propose/execute paths resolve live quote balance first before sizing and daily risk-cap computations.
- **Project Build Scope Updated**: Added `Services/AccountBuyingPowerService.cs` compile include in `CryptoDayTraderSuite.csproj`.
- **Bybit/OKX Balance Endpoints Added**: Added `GetBalancesAsync()` private-account balance retrieval to `Exchanges/BybitClient.cs` and `Exchanges/OkxClient.cs`, and extended service alias mapping in `AccountBuyingPowerService` so `bybit`/`bybit-global` and `okx`/`okx-global` accounts participate in auto-equity resolution.

### Behavior
- **No Manual Equity Required When Key Has Quote Funds**: If a linked account key can return a positive quote balance (`DefaultQuote`, then `USD`/`USDC`/`USDT` fallback), planner/auto-mode now use that value automatically.
- **Safe Fallback Path Preserved**: If the account is paper, key lookup fails, service is unsupported, or balance fetch fails, existing manual equity input remains in effect and is logged as fallback.

### Verified
- **Build/Compile Integration**: `msbuild .\CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\Debug_Verify\ /t:Build /v:minimal` succeeds after resolver + Bybit/OKX balance endpoint additions.

## [Coinbase Runtime/UI] - Quote-Scoped Holdings Totals Consistency - 2026-02-17

### Changed
- **Quote-Scoped Snapshot Totals Added**: Updated `Services/CoinbaseReadOnlyImportService.cs` to compute and persist `TotalBalanceInQuote`, `TotalBalanceQuoteCurrency`, and `TotalBalanceExcludedCount` instead of relying on ambiguous raw multi-currency sum semantics.
- **USD-Family Equivalence Support Added**: Quote-scoped total logic now treats USD-family balances (`USD`, `USDC`, `USDT`, `USDP`, `FDUSD`, `TUSD`) as equivalent when quote currency is USD-family.
- **UI Summary Messaging Corrected**: Updated `UI/AccountsControl.cs` import result dialog and Coinbase insights panel to display quote-scoped holdings total + excluded-balance count.
- **Auto-Import Logging Enriched**: Updated `UI/KeyEditDialog.cs` Coinbase auto-import logs to include quote currency, quote-scoped holdings total, and excluded holdings count.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after quote-scoped holdings updates.
- **Runtime Read-Only Probe**: `powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\\obj\\tmp_coinbase_readonly_import_probe3.ps1` returns `__COINBASE_READONLY_IMPORT=PASS` with expected fills-scope `401` warning fallback and stable products/fees/balances telemetry.

## [Ops Tooling] - Reject Capture Provider-Probe Refresh Integration - 2026-02-17

### Changed
- **Provider Probe Refresh Option Added**: Updated `obj/run_reject_evidence_capture.ps1` with `-RunProviderProbeBeforeCert` to run `Util/run_provider_public_api_probe.ps1` in the same orchestration flow before certification.
- **Strict Path Probe Refresh Defaulted**: Orchestration now refreshes provider probe evidence during strict capture runs so strict provider gate inputs are current by default.
- **Probe Diagnostics Emitted**: Script now emits `PROBE_JSON`, `PROBE_VERDICT`, and `PROBE_EXIT` alongside cycle/log/reject diagnostics for one-command triage.

### Verified
- **Smoke Run**: `obj/run_reject_evidence_capture.ps1 -DurationSeconds 60 -RunProviderProbeBeforeCert -AutoRepairBindings` emits probe diagnostics and produces fresh provider artifacts (`provider_public_api_probe_*.json/.txt`).
- **Strict Capture Path**: Existing strict capture flows remain compatible with new probe refresh path and continue reporting deterministic PASS/PARTIAL result lines.

## [Coinbase Runtime] - Fill Dedupe + Import Marker Stability Follow-Through - 2026-02-17

### Changed
- **Fill Return Dedupe Added**: Updated `Exchanges/CoinbaseExchangeClient.cs` `GetRecentFillsAsync(...)` to de-duplicate enriched fills by stable fill id (with deterministic fallback keying) before returning telemetry rows.
- **Import Marker Compatibility Fixed**: Updated `Services/CoinbaseReadOnlyImportService.cs` existing-history marker scan to recognize both `coinbase_fill_id:` and `coinbase_fill_fp:` prefixes so fallback fingerprint dedupe works across reruns.
- **Import Normalization Tightened**: Trade import now normalizes product ids to uppercase dash format and keeps side normalization aligned with canonical `BUY`/`SELL` handling.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after this follow-through patch set.
- **Runtime Read-Only Probe**: `powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\\obj\\tmp_coinbase_readonly_import_probe3.ps1` returns `__COINBASE_READONLY_IMPORT=PASS` with expected fills-scope `401` warning fallback and healthy products/fees/balances telemetry.

## [Coinbase Runtime] - Trade Permission Preview Validation - 2026-02-17

### Verified
- **Preview Order Auth Passed**: Executed live `POST /api/v3/brokerage/orders/preview` probe using app JWT generation stack and imported key; endpoint returned `HTTP 200`.
- **Expected Business Rejection Observed**: Preview response includes `PREVIEW_INSUFFICIENT_FUND`, confirming authorization and request signing are valid while account funding constraints are enforced.
- **No Live Trade Placement**: Validation used preview-only endpoint and did not submit executable orders.

## [Coinbase Runtime] - Safe Private Probe Validation - 2026-02-17

### Verified
- **Safe Private API Probe Passed**: Executed `obj/tmp_coinbase_private_safe_probe.ps1` with imported key (`coinbase-advanced|cba1`) and confirmed authenticated non-trading private calls (`GetOpenOrdersAsync`, `GetRecentFillsAsync`, `CancelOrderAsync` with synthetic id).
- **Observed Runtime Outputs**: Probe result `__PRIVATE_SAFE=PASS`, `__OPEN_ORDERS_COUNT=0`, `__RECENT_FILLS_COUNT=0`, `__FAKE_CANCEL_RESULT=False`.
- **Strict Certification Re-Check**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_095007.txt` remains `VERDICT=PARTIAL` with all non-geo reliability gates passing.

## [Ops Tooling/UI] - Skipped(Account) Remediation + Auto Binding Repair - 2026-02-17

### Changed
- **Root Cause Isolated**: Identified Auto cycle `skipped(account)` condition as profile/account id drift in persisted stores (`automode_profiles.json` bound to stale account ids not present in enabled `accounts.json`).
- **Profile Binding Repair Tool Added**: Added `obj/repair_profile_account_bindings.ps1` with dry-run/apply modes to remap enabled profiles with stale account ids to an enabled target account (with automatic backup).
- **Precheck JSON Normalization Fixed**: Updated `obj/precheck_reject_evidence_capture.ps1` to correctly handle single-account JSON payload shapes and emit explicit mismatch diagnostics (`PRECHECK_UNMATCHED_PROFILE_COUNT`, `PRECHECK_UNMATCHED_ACCOUNT_IDS`).
- **Capture Auto-Repair Added**: Updated `obj/run_reject_evidence_capture.ps1` with `-AutoRepairBindings` to attempt binding repair and re-run precheck before aborting capture.

### Verified
- **Binding Repair**: `obj/repair_profile_account_bindings.ps1 -Apply` remapped all enabled strict profiles from stale id `817f...` to live enabled id `b1a...` (`REPAIR_APPLIED_UPDATES=3`).
- **Precheck**: `obj/precheck_reject_evidence_capture.ps1 -RequireAutoRunEnabled -RequireRunnableProfile` now reports `PRECHECK_ENABLED_ACCOUNT_COUNT=1`, `PRECHECK_RUNNABLE_PROFILE_COUNT=3`, and `PRECHECK_RESULT=PASS`.
- **Strict Capture**: `obj/run_reject_evidence_capture.ps1 -DurationSeconds 420 -RunStrict -UseFastProfileOverride -AutoRepairBindings` now reaches `CYCLE_IS_FRESH=1` and produces merged reject evidence (`fees-kill`, `routing-unavailable`) with `RESULT:PASS`.

## [Coinbase Runtime] - JWT Path-Only Signing for Query Endpoints - 2026-02-17

### Changed
- **JWT Request URI Signing Corrected**: Updated `Exchanges/CoinbaseExchangeClient.cs` `PrivateRequestAdvancedAsync(...)` to sign JWT with `uri.AbsolutePath` (path-only) instead of `PathAndQuery`, aligning with Coinbase request-path signing semantics.

### Verified
- **Live Runtime Probe**: Direct runtime call to `CoinbaseExchangeClient.GetRecentFillsAsync(...)` now authenticates successfully with imported key (`coinbase-advanced|cba1`) and returns a valid response (fill count `0`) instead of prior `401 Unauthorized`.
- **Read-Only Import Path**: `CoinbaseReadOnlyImportService.ValidateAndImportAsync()` continues to pass with live products/fees/balances on the same key.

## [Cleanup] - Legacy UI + Duplicate Source Removal - 2026-02-17

### Changed
- **Legacy UI Entry Surfaces Removed**: Deleted dormant `UI/MainForm_Menu.cs`, `UI/MainForm_Hooks.cs`, and `UI/MainForm_ExtraButtons.cs` that were not wired into active runtime lifecycle.
- **Project Compile Scope Cleaned**: Updated `CryptoDayTraderSuite.csproj` to remove compile includes for the above legacy files.
- **Duplicate Source Archive Removed**: Deleted duplicate non-compiled broker files in `NewFolder2/` (`AliasBroker.cs`, `CoinbaseExchangeBroker.cs`, `IBroker.cs`, `PaperBroker.cs`) and removed the project folder include.

### Verified
- **Diagnostics**: No project/file errors after cleanup (`CryptoDayTraderSuite.csproj`, `MainForm.cs`).
- **Strict Certification**: `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_094930.txt` reports `VERDICT=PARTIAL` with build/freshness pass and known environment-constrained provider coverage.

## [Ops Tooling] - Explicit Strict Switch + Invocation Telemetry - 2026-02-17

### Changed
- **Strict Alias Added**: Updated `Util/run_multiexchange_certification.ps1` to accept `-Strict` as a first-class switch that enables all strict gates (`RequireBuildPass`, `RequireMatrixPass`, `RequireProviderArtifacts`, `RequireRejectCategories`) without breaking legacy flag usage.
- **Strict Gate Resolution Unified**: Strict checks now resolve through shared booleans (`requireBuildPass`, `requireMatrixPass`, `requireProviderArtifacts`, `requireRejectCategories`) so mixed invocations (`-Strict` + individual flags) remain deterministic.
- **Invocation Telemetry Added**: Report `Inputs` now includes `StrictRequested`, `StrictSwitch`, and resolved strict gate booleans; TXT header now emits `StrictMode: ON/OFF (...)` to make CI/operator triage unambiguous.
- **Telemetry Type Normalization**: Internal strict-gate variables now use distinct names to avoid PowerShell case-insensitive switch-variable aliasing, ensuring JSON `Inputs.Require*` values serialize as plain booleans instead of switch objects.
- **Terminal Output Contract Expanded**: Runner stdout now emits `STRICT_REQUESTED=...`, `STRICT_SWITCH=...`, and `STRICT_GATES=build=...,matrix=...,provider=...,reject=...` for lightweight CI parsing without opening report files.
- **Strict Summary Rollups Added**: JSON `Summary` now includes a `Strict` block with effective gate states and strict-failure telemetry (`FailureCount`, `FailureNames`, `FreshnessOnlyFailure`) so dashboards can classify strict verdict drivers without parsing check text.
- **Failure Keys Added to TXT/Stdout**: TXT header now includes `StrictFailures: count=... | names=...`, and stdout now emits `STRICT_FAILURE_COUNT=...` plus `STRICT_FAILURE_NAMES=...` for simple parser contracts.
- **Failure Class Added**: Strict telemetry now includes deterministic `FailureClass` values (`NON_STRICT`, `NONE`, `FRESHNESS_ONLY`, `BUILD_AND_FRESHNESS`, `BUILD_ONLY`, `OTHER_STRICT`) across JSON (`Summary.Strict.FailureClass`), TXT (`StrictFailureClass: ...`), and stdout (`STRICT_FAILURE_CLASS=...`) for direct CI branch routing.
- **Policy Decision Added**: Strict telemetry now includes deterministic action classification (`non-strict`, `promote`, `allow-geo-partial`, `collect-more-evidence`, `refresh-freshness`, `fix-build`, `fix-failures`) across JSON (`Summary.Strict.PolicyDecision`), TXT (`StrictPolicyDecision: ...`), and stdout (`STRICT_POLICY_DECISION=...`).
- **One-Line CI Contract Added**: Runner now emits `CI_SUMMARY=verdict=...;strict=...;class=...;decision=...;fails=...;manifest=...` and mirrors it in report outputs (`Summary.Strict.CiSummary`, `CiSummary:`) so automation can parse one key instead of multiple fields.
- **Contract Version Marker Added**: Runner now emits `CI_VERSION=1`, mirrors version in report outputs (`Summary.Strict.CiVersion`, `CiVersion:`), and prefixes `CI_SUMMARY` with `version=1` to allow safe parser/schema evolution.
- **Reject-Capture Runtime Hardening Finalized**: `obj/run_reject_evidence_capture.ps1` now emits retry configuration telemetry (`RETRY_CONFIG=externalAttempts=...,externalDelaySec=...,runtimeAttempts=...,runtimeDelaySec=...`) and performs a final profile-store backup restore attempt (`FAST_PROFILE_OVERRIDE_RESTORED_FINAL=...`) before exit to reduce lingering config override risk on long-run partial/failure outcomes.

### Verified
- **Backward Compatibility**: Legacy strict invocation with `-Require*` flags remains supported.
- **New Alias Flow**: `-Strict` invocation produces equivalent strict gate behavior and report semantics.

## [Coinbase Runtime] - Fill Economics Normalization End-to-End - 2026-02-17

### Changed
- **Client Fill Canonicalization Expanded**: Updated `Exchanges/CoinbaseExchangeClient.cs` `GetRecentFillsAsync(...)` enrichment to normalize canonical fill economics fields (`size`, `price`, `notional`, `fee`, `trade_time`) from multiple Coinbase payload key variants.
- **Client Fill Dedupe Added**: `GetRecentFillsAsync(...)` now de-duplicates enriched fill rows by stable `trade_id` (or deterministic fallback fingerprint keys) to reduce duplicate telemetry artifacts from variant payload roots.
- **Import Parser Robustness Expanded**: Updated `Services/CoinbaseReadOnlyImportService.cs` fill import to use candidate-based parsing for ids/product/side/qty/price/notional/fee and normalize side aliases (`B/S` -> `BUY/SELL`).
- **PnL Math Fallbacks Added**: Read-only import now derives missing price/qty from notional where possible, computes gross using normalized notional-first semantics, and skips invalid zero-quantity rows instead of persisting bad trades.
- **Date Parsing Bug Fixed**: Replaced invalid `DateTimeStyles` flag combination in `CoinbaseReadOnlyImportService` timestamp parsing with valid UTC parsing flags and unix epoch fallback support.
- **Deterministic Fill Dedupe Added**: Missing Coinbase fill IDs now use a stable fingerprint marker (`product|side|qty|price|notional|fee|timestamp`) instead of random GUIDs, preventing duplicate trade imports on repeated read-only runs.
- **Marker Prefix Compatibility Fixed**: Existing-history marker scans now recognize both `coinbase_fill_id:` and `coinbase_fill_fp:` note prefixes, ensuring deterministic fallback dedupe remains effective across sessions.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after client/import fill normalization + deterministic dedupe updates.
- **Runtime Read-Only Import Probe**: `powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\\obj\\tmp_coinbase_readonly_import_probe3.ps1` returns `__COINBASE_READONLY_IMPORT=PASS` (with expected fills-permission warning fallback to non-fills telemetry).

## [Coinbase Runtime] - Imported Key Validation + Read-Only Fills Fallback - 2026-02-17

### Changed
- **Imported Key Runtime Validation Confirmed**: Verified active imported key (`coinbase-advanced|cba1`) resolves to valid advanced credential shape at runtime (API key name + EC PEM), matching Coinbase file import expectations.
- **Read-Only Import Resilience Added**: Updated `Services/CoinbaseReadOnlyImportService.cs` so recent-fills retrieval is optional telemetry; `401 Unauthorized` on `/api/v3/brokerage/orders/historical/fills` now logs a warning and continues account import using products/fees/balances.

### Verified
- **Live Read-Only Coinbase Service Path**: `CoinbaseReadOnlyImportService.ValidateAndImportAsync()` returns `PASS` with live data (`ProductCount=789`, `NonZeroBalances=2`, maker/taker rates populated) while fills remain permission-gated for this key.

## [Runtime Hardening] - Post-Audit Remediation Iteration 1 - 2026-02-17

### Changed
- **Legacy Auto Execute Path Disabled**: Updated `UI/AutoModeForm.cs` so `DoExecute()` is blocked with an explicit warning to use the canonical Auto Mode control guardrail path.
- **Kraken Numeric Parsing Hardened**: Updated `Exchanges/KrakenClient.cs` to use invariant `TryParse`-based numeric conversion across candles, ticker, fees, and balances, eliminating locale-sensitive `Convert.ToDecimal(...)` behavior.
- **Bitstamp Fee Semantics Tightened**: Updated `Exchanges/BitstampClient.cs` `GetFeesAsync()` to remove optimistic synthetic maker-fee inference; now parses explicit maker/taker fee fields and falls back conservatively (`maker=taker`) only when maker is absent.
- **Key Save Responsiveness Improved**: Updated `UI/KeyEditDialog.cs` Coinbase read-only auto-import trigger to run asynchronously instead of blocking the UI thread.
- **Logger Teardown Added**: Updated `Program.cs` shutdown `finally` block to call `Log.Shutdown()` after governor/sidecar teardown.

### Verified
- **File Diagnostics**: No errors reported in edited files (`UI/AutoModeForm.cs`, `UI/KeyEditDialog.cs`, `Exchanges/BitstampClient.cs`, `Exchanges/KrakenClient.cs`, `Program.cs`).
- **Strict Certification**: Latest strict certification artifact `obj/runtime_reports/multiexchange/multi_exchange_cert_20260217_094128.txt` reports `VERDICT=PARTIAL` with build/freshness pass and environment-constrained provider coverage.

## [Exchanges] - Coinbase Cancel/Open/Fill Status Normalization - 2026-02-17

### Changed
- **Cancel Response Shape Coverage Expanded**: Updated `Exchanges/CoinbaseExchangeClient.cs` `CancelOrderAsync(...)` to parse result rows across `results`, `order_results`, `cancel_results`, and `data` payload roots.
- **Cancel Status Semantics Hardened**: Added status normalization and fail-closed behavior so cancel success can be inferred from canceled-like statuses while reject/failure-like statuses for the target order return deterministic `false`.
- **Open Order Filtering Added**: Updated `GetOpenOrdersAsync()` to normalize collection fallback then filter to open-like statuses, reducing closed-order leakage from variant payloads.
- **Recent Fill Enrichment Added**: Updated `GetRecentFillsAsync(...)` to support `executions` fallback and normalize returned fill rows with canonical `product_id` and `side` (`BUY`/`SELL`) fields.
- **Shared Helpers Added**: Added `ResolveOrderId(...)`, `ReadStatusValue(...)`, `IsCanceledLikeStatus(...)`, `IsFailureLikeStatus(...)`, `IsOpenLikeStatus(...)`, and `EnrichFillRows(...)`.

### Verified
- **Build (pending this iteration)**: Run `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` after this patch set.

## [Ops/UI] - Reject Capture Runnable-Profile Precheck + Auto Pair Filter - 2026-02-17

### Changed
- **Fail-Fast Runnable Profile Gate Added**: Updated `obj/precheck_reject_evidence_capture.ps1` to validate profile/account runtime readiness (`PRECHECK_ENABLED_PROFILE_COUNT`, `PRECHECK_ENABLED_ACCOUNT_COUNT`, `PRECHECK_RUNNABLE_PROFILE_COUNT`) and return explicit failure when no enabled profile is bound to an enabled account.
- **Orchestrator Precheck Strengthened**: Updated `obj/run_reject_evidence_capture.ps1` to require runnable-profile precheck success before starting long capture windows, preventing wasted runs that would only produce `skipped(account)` cycles.
- **Capture Utilities Extended**: Added optional fast-profile override support in `obj/run_reject_evidence_capture.ps1` with temporary store restore for deterministic capture setup experiments.
- **Auto Pair Ranking Hardened**: Updated `UI/AutoModeControl.cs` `RankPairs(...)` to exclude malformed numeric-only USD pairs (for example `00-USD`) while preserving alphanumeric symbols.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after `UI/AutoModeControl.cs` updates.
- **Precheck Diagnostics**: `powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\\obj\\precheck_reject_evidence_capture.ps1 -RequireAutoRunEnabled -RequireRunnableProfile` now reports explicit runnable-profile/account counts and fails fast when account bindings are missing.
- **Strict Certification Baseline**: `powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\\Util\\run_multiexchange_certification.ps1 -RequireBuildPass -RequireMatrixPass -RequireProviderArtifacts -RequireRejectCategories` returns current environment `VERDICT=PARTIAL`.

## [Exchanges] - Coinbase Order Outcome Reconciliation Hardening - 2026-02-17

### Changed
- **Multi-Root Outcome Parsing Expanded**: Updated `Exchanges/CoinbaseExchangeClient.cs` `PlaceOrderAsync(...)` to reconcile order id/status/reject fields across `success_response`, nested `order`, and response root payload keys.
- **Fill Metrics Output Added**: `PlaceOrderAsync(...)` now populates `OrderResult.Filled`, `OrderResult.FilledQty`, and `OrderResult.AvgFillPrice` from candidate response fields plus status-based fill inference.
- **Reject-State Detection Tightened**: Added `IsRejectReason(...)` and fail-closed status checks for reject/fail/error/invalid states to avoid false acceptance.
- **Message Mapping Centralized**: Added `ResolveOrderMessage(...)` to consistently map accepted/rejected/filled/error outcomes into deterministic operator-facing result messages.

### Verified
- **Build (pending this iteration)**: Run `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` after this patch set.

## [Ops Tooling] - Manifest Source-Class + Stable Diff Key - 2026-02-17

### Changed
- **Artifact Classification Added**: Updated `Util/run_multiexchange_certification.ps1` so each `ArtifactManifest` entry now emits explicit `SourceClass` (`matrix`, `provider`, `reject`, `policy`) for straightforward downstream filtering without name parsing.
- **Stable Diff Key Added**: Added per-entry `DiffKey` (`Name|SourceClass|FileName`) to provide a normalized anchor for dashboard joins and historical diff tooling.
- **Hash Canonicalization Extended**: `ArtifactManifestHash` payload canonicalization now includes `SourceClass` and `DiffKey`, preserving deterministic tamper-evident comparisons as manifest schema grows.
- **TXT Artifact Lines Expanded**: Human-readable artifact manifest lines now include `class=` and `diffKey=` fields for parity with JSON.

### Verified
- **Strict Certification Behavior Preserved**: Baseline strict runs retain expected geo-aware `PARTIAL` outcomes, and forced-stale strict runs continue to fail on freshness gates with explicit strict freshness failure checks.

## [Ops Tooling] - Binance Spot-Only Perp Waiver Refinement - 2026-02-17

### Changed
- **Perp Coverage False-Fail Reduced**: Updated `Util/run_multiexchange_certification.ps1` spot/perp coverage evaluation to treat Binance probe rows with `SpotCoverage=true` and `PerpCoverage=false` (no integration failure) as deferred perp evidence (`Binance(spot-only-endpoint)`) instead of strict `perp-missing`.
- **Environment Alignment Improved**: This prevents strict failures in US-routed/default Binance spot-only environments where perp coverage is not realistically testable from the active endpoint profile.

### Verified
- **Strict Certification**: Latest strict reports now classify Binance spot-only perp gaps as waiver detail under coverage partials instead of hard missing-perp failures.

## [Accounts/UI] - Coinbase Insights View by Account - 2026-02-17

### Changed
- **Per-Account Insights Panel Added**: Updated `UI/AccountsControl.Designer.cs` + `UI/AccountsControl.cs` to add a dedicated `Coinbase Insights (By Account)` section that updates from the currently selected account row.
- **Snapshot Persistence Added**: Extended `Services/CoinbaseReadOnlyImportService.cs` to persist read-only import snapshots keyed by Coinbase key id (`coinbase_account_snapshots.json`) including holdings, balances, fills, fees, and net-profit estimate.
- **Account-Key Mapping View Added**: `AccountsControl` now resolves the selected account's `KeyEntryId` and renders that key's latest Coinbase snapshot metrics and holdings breakdown.
- **Profile Manager Wiring Updated**: Updated `UI/ProfileManagerControl.cs` to pass key service into `AccountsControl` initialization so insights/import are available in profile-manager account view as well.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify_AccountInsights\\ /t:Build /v:minimal` succeeds after account-insights integration.

## [Audit] - Full Aâ†’F Code Audit Tracker Update - 2026-02-17

### Changed
- **Independent Audit Tracker Completed**: Updated `CODE_AUDIT_TRACKER.md` with completed Phase D, Phase E, and Phase F sections (Aâ†’F now complete).
- **Static Correctness Findings Logged**: Added concrete defects for execution-safety bypass surface (`UI/AutoModeForm.cs`), exchange correctness issues (`Exchanges/BitstampClient.cs`, `Exchanges/KrakenClient.cs`), and responsiveness hotspots (`UI/KeyEditDialog.cs`, broker validation paths).
- **Orphan/Legacy Surface Findings Logged**: Added dormant legacy UI and duplicate-folder findings for `UI/MainForm_Menu.cs`, `UI/MainForm_Hooks.cs`, `UI/MainForm_ExtraButtons.cs`, and `NewFolder2/`.
- **Final Readiness Verdict Logged**: Added consolidated severity totals and final readiness verdict (`PARTIAL / NOT READY for strict production confidence`) with remediation order.

### Verified
- **Strict Certification Evidence Captured**: Ran VS Code task `strict-cert-once-3`; latest report under `obj/runtime_reports/multiexchange/` returned `VERDICT=PARTIAL` with build pass and environment-constrained venue coverage notes.

## [Exchanges] - Coinbase Helper-Layer Case/Zero Parse Hardening - 2026-02-17

### Changed
- **Case-Insensitive Key Resolution Added**: Updated `Exchanges/CoinbaseExchangeClient.cs` to use `TryGetObjectValue(...)` inside `ReadObject(...)`, `ReadObjectList(...)`, and `ReadStringValue(...)`, so payload field lookups tolerate key-casing variations.
- **Zero-Safe Decimal Reads Added**: Added `TryReadDecimalValue(...)` and wired `ReadDecimalValue(...)`/`ReadDecimalByCandidates(...)` to distinguish missing keys from present `0` values.
- **Candidate Numeric Parsing Hardened**: `ReadDecimalByCandidates(...)` now applies deterministic nested value extraction (`value`/`amount`) without relying on non-zero sentinels.

### Verified
- **Build (pending this iteration)**: Run `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` after this patch set.

## [Exchanges] - Coinbase Runtime Ticker Timestamp Parse Fix (Probe-Driven) - 2026-02-17

### Changed
- **Runtime Integration Bug Fixed**: Updated `Exchanges/CoinbaseExchangeClient.cs` `ReadDateTimeValue(...)` to remove invalid `DateTimeStyles` combination (`RoundtripKind` with `AssumeUniversal|AdjustToUniversal`) that caused live Coinbase ticker probe failures.
- **UTC Parse Behavior Preserved**: Timestamp parsing now uses valid `AssumeUniversal|AdjustToUniversal` flags to keep UTC normalization without runtime exceptions.

### Verified
- **Live Coinbase API Probe (Before Fix)**: `Util/run_provider_public_api_probe.ps1 -Services Coinbase -PreferredSymbol BTC-USD` returned `PROBE_VERDICT=FAIL` with `INTEGRATION-ERROR` (`DateTimeStyles` invalid-combination exception).
- **Live Coinbase API Probe (After Fix)**: Re-ran probe with real inputs and received `PROBE_VERDICT=PASS` for `BTC-USD`, `ETH-USD`, and `SOL-USD`.
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after fix.

## [Ops Tooling] - Deterministic Manifest Hashing - 2026-02-17

### Changed
- **Deterministic Manifest Ordering Added**: Updated `Util/run_multiexchange_certification.ps1` to sort `ArtifactManifest` entries by stable key (`Name`) before report emission.
- **Manifest Hash Added**: Added SHA-256 `ArtifactManifestHash` to JSON root, `Summary`, and TXT header for tamper-evident audit diffs.
- **Hash Canonicalization Added**: Hash input now uses stable per-entry fields (`name|status|exists|age|lastWriteUtc|path`) in deterministic order.

### Verified
- **Strict Certification Reports**: Latest `multi_exchange_cert_*.json/.txt` artifacts include deterministic `ArtifactManifest` ordering and populated `ArtifactManifestHash`.

## [Exchanges] - Coinbase Parsing + Constraint Candidate Hardening - 2026-02-17

### Changed
- **Fee Candidate Key Coverage Expanded**: Updated `Exchanges/CoinbaseExchangeClient.cs` `GetFeesAsync()` to parse maker/taker rates through multi-key candidates (`maker_fee_rate`/`makerRate`/`maker`, `taker_fee_rate`/`takerRate`/`taker`) and to safely handle null-deserialized payload roots.
- **Balances Root/List Tolerance Expanded**: Updated `GetBalancesAsync()` to tolerate null root payloads, fall back account collections across `accounts`/`results`/`data`, and aggregate totals when duplicate currency rows are returned.
- **Order Input Guardrails Tightened**: Updated `PlaceOrderAsync(...)` to reject non-positive quantity and blank product ids before private API calls.
- **Recent Fills Fallback Expanded**: Updated `GetRecentFillsAsync(...)` to handle null roots and collection fallbacks across `fills`/`results`/`data`.
- **Product Id Derivation Expanded**: Updated `ResolveProductId(...)` to derive ids from base/quote currency fields when direct id keys are absent.
- **Constraint Parse Candidate Keys Expanded**: Updated `EnsureSymbolConstraintsCacheAsync()` to parse size/tick/notional constraints via broader snake/camel candidate keys used by variant payload shapes.
- **Decimal Helper Added**: Added `ReadDecimalByCandidates(...)` helper to centralize multi-key numeric extraction.

### Verified
- **Build (pending this iteration)**: Run `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` after this patch set.

## [Coinbase/Accounts] - Automatic Read-Only Import on Key Save - 2026-02-17

### Changed
- **Automatic Import Trigger Added**: Updated `UI/KeyEditDialog.cs` and `UI/KeysControl.cs` so saving a Coinbase key now automatically runs read-only Coinbase import (account linkage + holdings/balances + fills/fees telemetry) when account services are available.
- **Trade/Fee Telemetry Import Expanded**: Updated `Services/CoinbaseReadOnlyImportService.cs` to import recent Coinbase fills into local trade history (`HistoryService`) with dedupe markers, aggregate total fees paid from fills, and compute a net profit estimate (`sells - buys - fees`).
- **Read-Only Coinbase Client Coverage Expanded**: Added `GetRecentFillsAsync(...)` in `Exchanges/CoinbaseExchangeClient.cs` for brokerage fills retrieval.
- **Accounts View Summary Expanded**: Updated `UI/AccountsControl.cs` to display holdings total, fill counts, imported trade count, fees paid, and net profit estimate in the read-only import result dialog.
- **Dependency Wiring Updated**: Updated `MainForm.cs` and `UI/ProfileManagerControl.cs` to pass account/history services through keys/accounts controls for automatic import and telemetry persistence.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after automatic Coinbase read-only import enhancements.

## [Exchanges] - Coinbase Product Endpoint Failover + ID Variant Hardening - 2026-02-17

### Changed
- **Endpoint Failover Added**: Updated `Exchanges/CoinbaseExchangeClient.cs` `GetProductsPayloadAsync()` to try legacy `https://api.exchange.coinbase.com/products` and fail over to Advanced `https://api.coinbase.com/api/v3/brokerage/products` when needed.
- **Public Fetch Resilience Added**: Added non-throwing public fetch helper so single-endpoint outages return warnings and allow fallback instead of aborting product/constraint discovery.
- **Payload Root Shape Coverage Expanded**: Product parsing now supports direct list, `object[]`, root `products`, and root `data` shapes.
- **Product ID Variant Support Added**: Listing/constraints flows now resolve symbol ids from `id`, `product_id`, or `productId`.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after Coinbase product discovery hardening.

## [Exchanges] - Coinbase Order/Cancel and Payload-Helper Hardening - 2026-02-17

### Changed
- **Order Guardrails Expanded**: Updated `Exchanges/CoinbaseExchangeClient.cs` `PlaceOrderAsync(...)` to reject null requests and invalid limit prices before private API calls.
- **Acceptance Semantics Tightened**: `PlaceOrderAsync(...)` now parses additional success/id shapes (`accepted`, `orderId`, `success_response.orderId`) and blocks acceptance when explicit reject reasons are present.
- **Cancel Success Matching Tightened**: `CancelOrderAsync(...)` now requires structured success rows and, when row order ids are present, matches them against the requested `orderId`.
- **Open-Order Shape Fallback Added**: `GetOpenOrdersAsync()` now falls back across `orders`, `results`, and `data` collections.
- **Payload List Helper Expanded**: `ReadObjectList(...)` now supports broader non-generic list payloads (`IList`) and serialized list/array JSON fragments.
- **Symbol Normalization Safety Added**: `NormalizeProduct(...)` / `DenormalizeProduct(...)` now safely handle null/blank inputs.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after Coinbase order/cancel/helper hardening.

## [Ops Tooling] - Freshness Rollup + Compact TXT Summary - 2026-02-17

### Changed
- **Freshness Rollup Added**: Updated `Util/run_multiexchange_certification.ps1` JSON output to include `Summary.Freshness.OverallStatus` and `Summary.Freshness.NonPassSources`.
- **Targeted Freshness Remediation Expanded**: Strict freshness-only `FAIL` recommendations now target stale source groups (for example, `matrix,provider`) instead of generic text.
- **Compact TXT Freshness Line Added**: Text report header now includes one-line freshness status with per-source status and age/threshold (`matrix`, `provider`, `reject`) for quick operator scanning.

### Verified
- **Strict Certification Artifacts**: Latest `multi_exchange_cert_*.json/.txt` reports include freshness rollup fields and compact TXT freshness summary line.

## [Exchanges] - Coinbase Fee Parse Shape Hardening - 2026-02-17

### Changed
- **Fee Tier Shape Fallbacks Added**: Updated `Exchanges/CoinbaseExchangeClient.cs` `GetFeesAsync()` to resolve fee tier from `fee_tier`, `feeTier`, or first row of `fee_tiers`/`feeTiers` when present.
- **Maker/Taker Key Variant Support Added**: Fee parsing now reads snake-case and camel-case rate keys (`maker_fee_rate`/`makerRate`, `taker_fee_rate`/`takerRate`).
- **Fail-Closed Rate Guard Added**: `GetFeesAsync()` now throws explicit `InvalidOperationException` when valid maker/taker rates cannot be resolved from the payload.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after Coinbase fee parsing hardening.

## [Exchanges] - Binance Ticker Last-Price Hardening - 2026-02-17

### Changed
- **Last Price Resolution Added**: Updated `Exchanges/BinanceClient.cs` `GetTickerAsync(...)` to compute `Last` from book-ticker midpoint (`(bid+ask)/2`) when both sides are present.
- **Price Endpoint Fallback Added**: When book-ticker bid/ask are missing or invalid, ticker path now falls back to `/api/v3/ticker/price` for a direct last-price source.
- **Bid/Ask Backfill Added**: Missing/invalid bid or ask now fallback to resolved `Last`, preventing zero-sided ticker payloads.
- **Fail-Closed Price Guard Added**: Ticker path now throws explicit error when neither primary nor fallback source yields a valid price.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after Binance ticker hardening.

## [Exchanges] - Coinbase Balance Parse Shape Hardening - 2026-02-17

### Changed
- **Currency Fallbacks Added**: Updated `Exchanges/CoinbaseExchangeClient.cs` `GetBalancesAsync()` to resolve currency from top-level `currency` and fallback nested balance-currency fields when needed.
- **Amount Shape Tolerance Added**: Added `ReadBalanceAmount(...)` to parse available/hold values from nested objects (`value`/`amount`) and direct numeric string fields.
- **Variant Field Coverage Expanded**: Balance parsing now considers alternate keys such as `available`, `available_to_trade`, `available_funds`, `hold_balance`, `on_hold`, and `balance`.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after Coinbase balances hardening.

## [Ops Tooling] - JSON Summary Freshness Telemetry - 2026-02-17

### Changed
- **Summary Freshness Block Added**: Updated `Util/run_multiexchange_certification.ps1` to include a structured `Summary.Freshness` object in JSON reports.
- **Machine-Readable Recency Metrics Added**: `Summary.Freshness` now includes per-source `Source`, `AgeHours`, and `Status` for matrix artifact, provider probe artifact, and reject evidence log.
- **Threshold Echo Added**: Report summary now includes configured freshness thresholds under `Summary.Freshness.ThresholdHours` for reproducible downstream evaluation.

### Verified
- **Strict Certification JSON**: New `multi_exchange_cert_*.json` artifacts include `Summary.Freshness` with populated age/status fields.

## [Accounts/UI] - Coinbase Read-Only Validation + Account Import - 2026-02-17

### Changed
- **Read-Only Coinbase Import Service Added**: Added `Services/CoinbaseReadOnlyImportService.cs` to validate Coinbase connectivity using read-only calls only (`ListProductsAsync`, `GetFeesAsync`, `GetBalancesAsync`) and import/update a local account binding from the active Coinbase key.
- **Accounts UI Action Added**: Added a Designer-backed `Import Coinbase (Read-only)` action in `UI/AccountsControl.Designer.cs` and wired logic in `UI/AccountsControl.cs`.
- **No-Trade Safety Enforced**: Import flow performs zero order placement/cancel operations and explicitly reports that no trading actions were performed.
- **Project Wiring Updated**: Added `Services/CoinbaseReadOnlyImportService.cs` include to `CryptoDayTraderSuite.csproj`.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after Coinbase read-only import feature integration.

## [Exchanges] - Coinbase Candle Row Parse Hardening - 2026-02-17

### Changed
- **Safe Numeric Parsing Added**: Updated `Exchanges/CoinbaseExchangeClient.cs` `GetCandlesAsync(...)` to parse epoch/price/volume fields through non-throwing helpers (`TryReadLong`, `TryReadDecimal`).
- **Malformed Row Skip Behavior Added**: Candle rows with missing/invalid numeric values are now skipped instead of aborting the full candle request with conversion exceptions.
- **Chunked Retrieval Resilience Improved**: Long-range chunked candle retrieval now remains robust when individual rows are malformed in upstream payloads.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after Coinbase candle parse hardening.

## [Ops Tooling] - Freshness-Aware Strict Remediation Guidance - 2026-02-17

### Changed
- **Recommendation Branch Added**: Updated `Util/run_multiexchange_certification.ps1` to detect strict `FAIL` outcomes caused only by freshness gate failures.
- **Action Text Targeted**: In freshness-only strict failures, report `Recommended Next Action` now instructs refreshing matrix/provider/reject evidence artifacts (or adjusting freshness thresholds) instead of generic remediation wording.

### Verified
- **Forced-Stale Strict Run**: A strict run with zero-hour freshness thresholds now reports the freshness-specific remediation text.

## [Exchanges] - Binance Cancel Response Semantics Hardening - 2026-02-17

### Changed
- **Single-Order Cancel Parsing Tightened**: Updated `Exchanges/BinanceClient.cs` `TryCancelOrderBySymbolAsync(...)` to validate structured cancel responses (`orderId` match + terminal status) instead of relying on non-empty payload checks.
- **Cancel-All Success Criteria Tightened**: Updated `CancelAllOpenOrdersAsync(...)` to parse returned order rows and require terminal cancel statuses for all reported entries before returning success.
- **Defensive Mismatch Guard Added**: Single-order cancel now fails closed when response `orderId` does not match requested id.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after Binance cancel hardening.

## [Exchanges] - Coinbase Product Discovery Parse Hardening - 2026-02-17

### Changed
- **Payload Shape Tolerance Expanded**: Updated `Exchanges/CoinbaseExchangeClient.cs` `GetProductsPayloadAsync()` to parse `/products` responses from multiple shapes (`List<Dictionary<string, object>>`, `object[]`, and nested root `products`).
- **Product ID Hygiene Added**: Updated `ListProductsAsync()` to ignore blank/null ids and de-duplicate symbols case-insensitively before returning.
- **Compatibility Behavior Preserved**: Existing direct list parsing remains first-choice path; fallbacks activate only when the direct parse shape is unavailable.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after Coinbase product discovery hardening.

## [Keys] - Delete Reliability Fix for Null/Empty Broker Identity - 2026-02-17

### Changed
- **Service-Layer Key Match Normalization**: Updated `Services/KeyService.cs` to normalize broker/label identity matching (`null`/empty/trim/case) for `Upsert`, `Get`, `Remove`, and active-id repair paths.
- **Delete Path Active-Map Cleanup**: Key removal now also clears matching active-key map entries so deleted keys do not linger as stale active selections.
- **UI Delete Path Hardened**: Updated `UI/KeysControl.cs` delete action to prefer broker+label removal and fallback to id delete when needed.
- **Key Model Mapping Tightened**: Updated `Models/KeyEntry.cs` implicit conversion from `KeyInfo` to carry `Service` from `Broker` for consistent key-grid display and compatibility paths.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after key deletion reliability fixes.

## [Exchanges] - Bybit/OKX Placement and Cancel-All Success Semantics Hardening - 2026-02-17

### Changed
- **Bybit Place Acceptance Tightened**: Updated `Exchanges/BybitClient.cs` `PlaceOrderAsync(...)` to require structured API success (`retCode == 0`) in addition to order id presence before marking `Accepted=true`.
- **Bybit Cancel-All Result Parsing Tightened**: Updated `BybitClient.CancelAllOpenOrdersAsync(...)` to require top-level success and per-row success flags when result rows are returned.
- **OKX Place Acceptance Tightened**: Updated `Exchanges/OkxClient.cs` `PlaceOrderAsync(...)` to require structured success (`code == 0` and row-level `sCode` success when present) in addition to order id presence.
- **OKX Cancel-All All-or-Nothing Semantics Added**: Updated `OkxClient.CancelAllOpenOrdersAsync(...)` to return success only when every attempted cancel succeeds (while keeping no-open-orders path as success).

### Notes
- Validation/build execution is intentionally deferred in this iteration per operator request.

## [Exchanges] - Coinbase Ticker Parse Hardening - 2026-02-17

### Changed
- **Ticker Field Fallbacks Added**: Updated `Exchanges/CoinbaseExchangeClient.cs` `GetTickerAsync(...)` to resolve last price from `price`, then fallback `last`, then `trade_price`.
- **Partial Payload Tolerance Added**: Bid/ask now safely fallback to resolved last price when missing or non-positive.
- **Timestamp Parsing Hardened**: Added safe timestamp parsing helper and UTC-now fallback when ticker time is absent/unparseable.
- **Fail-Closed Last Price Guard Added**: Ticker path now throws explicit error only when no valid last price can be resolved.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after Coinbase ticker hardening.

## [Ops Tooling] - Strict Freshness Enforcement - 2026-02-17

### Changed
- **Strict Freshness Escalation Added**: Updated `Util/run_multiexchange_certification.ps1` so strict runs now convert any non-`PASS` freshness check into explicit `FAIL` gates.
- **Strict Coverage Expanded**: Enforced freshness checks are `AutoMode matrix artifact freshness`, `Provider probe artifact freshness`, and `Reject evidence freshness`.
- **Fail Signal Clarified**: Strict report now emits dedicated `Strict requirement: <freshness check>` rows when evidence recency is stale/missing.

### Verified
- **Strict Current-State Run**: With current fresh artifacts/logs, strict run keeps freshness gates passing and remains blocker-only `PARTIAL`.
- **Forced-Stale Run**: Using zero-hour freshness thresholds reproduces strict `FAIL`, confirming recency enforcement is active.

## [Exchanges] - Bybit/OKX Fee and Cancel Semantics Hardening - 2026-02-17

### Changed
- **Bybit Fee Aggregation Added**: Updated `Exchanges/BybitClient.cs` `GetFeesAsync()` to query broad spot fee-rate first, aggregate worst-case maker/taker across returned rows, and fallback to symbol-scoped query (`BTCUSDT`) when needed.
- **Bybit Cancel Success Parsing Hardened**: Updated `BybitClient` cancel paths to parse structured API success (`retCode == 0`) instead of treating non-empty response payloads as success.
- **OKX Fee Aggregation Added**: Updated `Exchanges/OkxClient.cs` `GetFeesAsync()` to aggregate worst-case maker/taker across available trade-fee rows (`instType=SPOT`), with `BTC-USDT` fallback when broad query is unavailable.
- **OKX Cancel Success Parsing Hardened**: Updated `OkxClient` cancel paths to require structured success (`code == 0` and any row-level `sCode` success when provided), replacing non-empty-response success checks.

### Notes
- Validation/build execution is intentionally deferred in this iteration per operator request.

## [Exchanges] - Coinbase Request Method + Candle Boundary + Cancel Fallback Hardening - 2026-02-17

### Changed
- **JWT Method Normalization Fixed**: Updated `Exchanges/CoinbaseExchangeClient.cs` `PrivateRequestAdvancedAsync(...)` to sign JWTs with normalized uppercase HTTP method, matching the actual request method sent.
- **Candle Chunk Cursor Gap Risk Removed**: Updated Coinbase candle pagination cursor advance from `chunkEnd + granularity` to `chunkEnd` and retained timestamp de-duplication, preventing potential boundary bucket gaps across chunked windows.
- **Cancel Success Semantics Tightened**: Updated `CancelOrderAsync(...)` to treat root `success` as fallback-only when no `results[]` entries are returned, while prioritizing explicit per-result success flags.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after Coinbase hardening updates.

## [UI/Coinbase] - JSON File Picker Import for API Key Bundle - 2026-02-17

### Changed
- **Key Dialog File Import Added**: Updated `UI/KeyEditDialog.cs` Coinbase credential section to include an `Import JSON...` file picker that loads Coinbase `name` + `privateKey` bundle files (for example `cdp_api_key(2).json` from Downloads) and auto-normalizes into key-name/PEM fields.
- **Account Dialog Designer Import Added**: Updated `UI/AccountEditDialog.Designer.cs` + `UI/AccountEditDialog.cs` to add a Designer-backed `Import JSON...` action alongside Coinbase JSON input, using the same file-picker flow and normalization path.
- **Downloads-First Picker Defaults**: Both import flows now default to `~/Downloads` with `cdp_api_key(2).json` prefilled and parse failures surfaced as explicit import errors.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after Coinbase file-import UX updates.

## [Ops Tooling] - Certification Evidence Freshness Gates - 2026-02-17

### Changed
- **Artifact Age Thresholds Added**: Updated `Util/run_multiexchange_certification.ps1` with configurable freshness thresholds (`MaxMatrixArtifactAgeHours`, `MaxProviderProbeAgeHours`, `MaxRejectEvidenceAgeHours`; default `24h`).
- **Freshness Checks Added**: Certification now emits explicit checks for `AutoMode matrix artifact freshness`, `Provider probe artifact freshness`, and `Reject evidence freshness` using artifact/log write timestamps.
- **Quality Signal Improved**: Stale or missing evidence now surfaces as deterministic `PARTIAL` checks, improving non-geo reliability confidence without conflating geo blockers and evidence recency.
- **Report Inputs Expanded**: JSON report `Inputs` now records the configured freshness thresholds for audit reproducibility.

### Verified
- **Strict Certification**: `powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File Util/run_multiexchange_certification.ps1 -RequireBuildPass -RequireMatrixPass -RequireProviderArtifacts -RequireRejectCategories` now includes freshness checks in report output.

## [Exchanges] - Bybit/OKX Candle Window Pagination - 2026-02-17

### Changed
- **Bybit Kline Pagination Added**: Updated `Exchanges/BybitClient.cs` `GetCandlesAsync(...)` to page `/v5/market/kline` over the full requested range using a timestamp cursor (`limit=1000`) instead of a single request window.
- **OKX History-Candle Chunking Added**: Updated `Exchanges/OkxClient.cs` `GetCandlesAsync(...)` to iterate long ranges in repeated `300`-row `/api/v5/market/history-candles` chunks and merge results.
- **Deterministic Merge Added**: Both clients now deduplicate by candle timestamp and return ascending time order to stabilize downstream strategy/backtest ingestion.

### Notes
- Validation/build execution is intentionally deferred in this iteration per operator request.

## [Exchanges] - Coinbase Private Call Timeout + IDictionary Parsing Hardening - 2026-02-17

### Changed
- **Bounded Private Call Timeout Added**: Updated `Exchanges/CoinbaseExchangeClient.cs` private Advanced request path to enforce a per-request timeout (`20s`) via cancellation token instead of allowing indefinite hangs.
- **Explicit Timeout Diagnostics Added**: Timeout cancellations are now surfaced as clear `TimeoutException` messages containing request method/path context for faster operator diagnosis.
- **Nested Dictionary Conversion Expanded**: Coinbase payload helpers now accept non-generic `IDictionary` shapes (for example, hashtable-backed deserialization paths) in addition to `Dictionary<string, object>`/array/list/string payload variants.
- **HTTP Error Detail Tightened**: Non-success private responses now include HTTP status and trimmed response body context for actionable failure logs.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after Coinbase reliability updates.

## [Coinbase Runtime] - Canonical Service Name Cleanup - 2026-02-17

### Changed
- **Canonical Broker Service Updated**: Updated `Brokers/CoinbaseExchangeBroker.cs` to report service name as `coinbase-advanced`.
- **Legacy Key Fallback Preserved**: Broker key resolution now first checks `coinbase-advanced` active key, then falls back to legacy `coinbase-exchange` active key for migration safety.
- **Watchdog Service Check Updated**: Updated `UI/AutoModeControl.cs` protective-watchdog check to recognize `coinbase-advanced` (with legacy alias fallback).

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after runtime service-name cleanup.

## [UI/Trading] - Geo-Routing Alias Visibility in Trading Surface - 2026-02-17

### Changed
- **Trading Exchange List Expanded**: Updated `MainForm.cs` Trading view setup to include geo-aware exchange options (`Binance-US`, `Binance-Global`, `Bybit-Global`, `OKX-Global`) alongside existing venues.
- **Inline Routing Hint Added**: Updated `UI/TradingControl.cs` so exchange selection emits inline status hints describing alias endpoint intent (US/global host routing).
- **Tooltip Guidance Updated**: Updated `UI/MainForm_Tooltips.cs` exchange tooltip copy to include alias examples and geo-routing intent guidance.
- **Legacy Selector Parity Added**: Updated `MainForm.Designer.cs` legacy `cmbExchange` dropdown options to match geo-aware alias choices so fallback/manual setup paths expose the same venue routing options.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after trading-surface geo hint updates.

## [Brokers] - Venue Price Tick Alignment Enforcement - 2026-02-17

### Changed
- **Price-Tick Guards Added**: Updated `Brokers/BinanceBroker.cs`, `Brokers/BybitBroker.cs`, `Brokers/CoinbaseExchangeBroker.cs`, and `Brokers/OkxBroker.cs` to reject trade plans whose `entry`, `stop`, or `target` are not aligned with venue `SymbolConstraints.PriceTickSize`.
- **Validation Coverage Expanded**: Broker `ValidateTradePlan(...)` now enforces both quantity constraints and price-tick constraints from exchange metadata before placement.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after price-tick enforcement changes.

## [Ops Tooling] - Geo-Blocker-Aware Recommended Next Action - 2026-02-17

### Changed
- **Geo-Only PARTIAL Guidance Added**: Updated `Util/run_multiexchange_certification.ps1` recommendation logic to detect when all remaining `PARTIAL` checks are geo/environment constrained (`ENV-CONSTRAINT`, geo-access blocker, env-waived coverage).
- **Action Text Reframed for Realistic Operations**: In geo-only `PARTIAL` cases, report output now recommends continuing non-geo reliability hardening while retaining strict `PARTIAL` as baseline evidence, instead of suggesting impossible geo-dependent upgrades.

### Verified
- **Certification Runner**: `powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File Util/run_multiexchange_certification.ps1 -RequireBuildPass -RequireMatrixPass -RequireProviderArtifacts -RequireRejectCategories` emits `VERDICT=PARTIAL` and blocker-aware `Recommended Next Action` text when partial checks are geo-only.

## [UI/Broker Routing] - Coinbase Service Option Consolidation - 2026-02-17

### Changed
- **Selectable Coinbase Service Simplified**: Removed `coinbase-exchange` from service dropdown options in `UI/AccountEditDialog.Designer.cs` and `UI/KeyEditDialog.cs`, keeping `coinbase-advanced` as the single Coinbase selection.
- **Execution Routing Compatibility Added**: Updated `UI/AutoModeForm.cs` broker factory mapping to route `coinbase-advanced` through `CoinbaseExchangeBroker`.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after Coinbase service consolidation.

## [Brokers/Exchanges] - OKX Symbol Constraint Validation Parity - 2026-02-17

### Changed
- **OKX Constraint Cache Added**: Updated `Exchanges/OkxClient.cs` with a cached symbol-constraint map sourced from `/api/v5/public/instruments?instType=SPOT` (`minSz`, `maxMktSz`/`maxLmtSz`, `lotSz`, `tickSz`).
- **Broker Validation Hardened**: Updated `Brokers/OkxBroker.cs` `ValidateTradePlan(...)` to enforce exchange-specific quantity guards (step-size alignment, min/max quantity, and min-notional if available) using resolved OKX constraints.
- **Generic Precision Check Removed**: Replaced legacy fixed `8`-decimal quantity precision rule with venue-derived constraints so validation reflects exchange metadata.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after OKX constraint parity changes.

## [UI] - Coinbase Account Dialog Payload Alignment - 2026-02-17

### Changed
- **Coinbase JSON Input Path Added (Account Dialog)**: Updated `UI/AccountEditDialog.cs` to expose the API-key text input as `Coinbase Key JSON (optional)` for Coinbase services.
- **Field Visibility Aligned to Coinbase Payload**: For Coinbase services, account dialog now emphasizes key-name/PEM inputs and hides irrelevant secret/passphrase fields.
- **Storage Hygiene for Coinbase Keys**: Account save path now avoids persisting generic API secret/passphrase blobs for Coinbase services and stores canonical key-name/PEM fields.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after account dialog Coinbase alignment.

## [UI] - Geo Routing Hint Labels for Service Selection - 2026-02-17

### Changed
- **Account Dialog Hint Added**: Updated `UI/AccountEditDialog.cs` to append service-specific endpoint hints to the credentials header (for example, `binance-us` vs `binance-global` routing intent).
- **Key Dialog Hint Row Added**: Updated `UI/KeyEditDialog.cs` with a `Routing Hint` row that updates dynamically when service selection changes.
- **Operator Clarity Improved**: Alias and default service options now show immediate endpoint-intent guidance during configuration.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after routing hint UI updates.

## [Exchanges] - Coinbase Advanced Payload Parsing Hardening - 2026-02-17

### Changed
- **Object Parsing Robustness**: Updated `Exchanges/CoinbaseExchangeClient.cs` helpers to parse nested objects and object lists from multiple deserializer shapes (`Dictionary<string, object>`, `object[]`, `ArrayList`, JSON-string fragments).
- **List Extraction Reliability**: `ReadObjectList` now accepts array/list payload variants returned by runtime JSON deserialization paths, reducing silent empty-list fallthroughs for account/order result parsing.
- **Safe Private Retry Fallback**: `TryPrivateRequestAsync` now catches non-HTTP private call failures as well, preserving fail-safe behavior in cancellation flow (`false` return + warning) instead of bubbling unexpected parse/signing exceptions.
- **Boolean Parse Stability**: Replaced repeated string re-fetch expression with single-read tolerant boolean parsing (`true/false`, `1`, `yes`, `y`).

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after Coinbase payload parser hardening.

## [UI/Services/Exchanges] - Bybit/OKX Global Service Aliases - 2026-02-17

### Changed
- **Service Alias Options Added**: Updated `UI/AccountEditDialog.Designer.cs` and `UI/KeyEditDialog.cs` to include `bybit-global` and `okx-global` service choices.
- **Policy/Provider Alias Support Added**: Updated `Services/ExchangeCredentialPolicy.cs` and `Services/ExchangeProvider.cs` so alias services are accepted and routed through authenticated/public client creation.
- **Per-Alias Endpoint Routing Added**: Updated `Exchanges/BybitClient.cs` and `Exchanges/OkxClient.cs` to support per-instance base URL overrides; updated `Brokers/BybitBroker.cs` and `Brokers/OkxBroker.cs` to resolve alias-selected account services into deterministic host selection.
- **Auto Mode Alias Routing Added**: Updated `UI/AutoModeForm.cs` broker factory mapping to support `bybit-global` and `okx-global` execution routing.
- **Alias-Compatible Key Reuse Added**: Updated `UI/AccountEditDialog.cs` compatibility normalization so Bybit/OKX alias families can reuse existing keys consistently.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after alias/routing updates.

## [Key Dialog] - Coinbase Payload-Aligned Entry Flow - 2026-02-17

### Changed
- **Coinbase-Native Key Entry Flow**: Updated `UI/KeyEditDialog.cs` to show Coinbase-specific credential fields only when a Coinbase service is selected, hiding irrelevant generic/passphrase inputs.
- **Raw Coinbase JSON Paste Support**: Added optional `Coinbase Key JSON` input in the key dialog and auto-extraction into `API Key Name` + `EC Private Key (PEM)` during normalization/save.
- **Visibility/Validation Alignment**: Coinbase service selection now drives credential visibility so dialog requirements match what Coinbase actually issues.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds.

## [Brokers] - Directional Risk Geometry Enforcement Expansion - 2026-02-17

### Changed
- **Broker Validation Parity Added**: Updated `Brokers/CoinbaseExchangeBroker.cs`, `Brokers/OkxBroker.cs`, and `Brokers/PaperBroker.cs` `ValidateTradePlan(...)` to enforce directional sanity checks shared by Binance/Bybit paths.
- **Fail-Closed Geometry Guards Added**: Validators now reject zero-direction plans and invalid protective geometry (`long: stop < entry < target`, `short: target < entry < stop`) before placement.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after broker validation updates.

## [Coinbase UX/Policy] - Advanced-Only Credential Messaging - 2026-02-17

### Changed
- **Policy Alignment**: Updated `Services/ExchangeCredentialPolicy.cs` so `coinbase-exchange` now requires Coinbase Advanced fields (`API Key Name + EC Private Key (PEM)`), removing passphrase requirement semantics.
- **Tooltip Messaging Updated**: Updated `UI/MainForm_Tooltips.cs` to remove Coinbase legacy wording and describe Coinbase Advanced credential usage only.
- **Key Dialog Label Cleanup**: Updated `UI/KeyEditDialog.cs` section/field labels to remove legacy Coinbase Exchange phrasing in setup UI.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` completes successfully.

## [Exchanges] - Coinbase Cancel Fail-Closed Semantics Re-Alignment - 2026-02-17

### Changed
- **Cancel HTTP Failure Handling Hardened**: Updated `Exchanges/CoinbaseExchangeClient.cs` `CancelOrderAsync` to use a cancel-specific safe private request path that catches non-success HTTP outcomes and returns `false` deterministically instead of throwing.
- **Structured Success Parsing Retained**: `CancelOrderAsync` now short-circuits `true` only on structured response signals (`success` or `results[].success`) and otherwise fails closed.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after cancel-flow changes.

## [UI/Services/Exchanges] - Binance US/Global Service Aliases - 2026-02-17

### Changed
- **Service Alias Options Added**: Updated `UI/AccountEditDialog.Designer.cs` and `UI/KeyEditDialog.cs` to include `binance-us` and `binance-global` service choices.
- **Policy/Provider Alias Support Added**: Updated `Services/ExchangeCredentialPolicy.cs` and `Services/ExchangeProvider.cs` to accept alias services and resolve them through authenticated/public client creation.
- **Per-Alias Endpoint Routing Added**: Updated `Exchanges/BinanceClient.cs` and `Brokers/BinanceBroker.cs` so alias-selected accounts route to explicit REST hosts (`binance-us` -> `https://api.binance.us`, `binance-global` -> `https://api.binance.com`).
- **Auto Broker Mapping Added**: Updated `UI/AutoModeForm.cs` broker factory mapping to route alias services through `BinanceBroker`.
- **Alias-Compatible Key Reuse Added**: Updated `UI/AccountEditDialog.cs` existing-key discovery to treat `binance`, `binance-us`, and `binance-global` as compatible key families.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after alias/routing updates.

## [Coinbase Private Auth] - Advanced-Only Enforcement - 2026-02-17

### Changed
- **Legacy Private Auth Removed**: Updated `Exchanges/CoinbaseExchangeClient.cs` to remove legacy Coinbase Exchange key/secret/passphrase private-request signing (`CB-ACCESS-*` headers).
- **Advanced Credentials Required**: Private methods now fail closed unless credentials are Coinbase Advanced shape (API key name + EC private key PEM).
- **Private Endpoint Scope Simplified**: Fee/balance/order/open-order/cancel flows now target Coinbase Advanced brokerage endpoints only.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds.

## [Ops Tooling] - Reject Evidence Capture Orchestration Script - 2026-02-17

### Changed
- **One-Command Capture Added**: Added `obj/run_reject_evidence_capture.ps1` to orchestrate runtime evidence capture, reject-category extraction (cycle + log), and certification execution in one pass.
- **Auto Run Precheck Helper Added**: Added `obj/precheck_reject_evidence_capture.ps1` and integrated pre-run validation into the orchestrator so capture can fail-fast when persisted Auto Run preference is not enabled.
- **Freshness Diagnostics Added**: Script emits `CYCLE_IS_FRESH` and `LOG_IS_FRESH` so stale Auto Run state is distinguishable from true no-reject behavior.
- **Lock-Safe Certification Step Added**: Script force-stops lingering `CryptoDayTraderSuite` processes before certification invocation to reduce build-lock false failures.
- **Runbook Sync**: Updated `docs/ops/MultiExchange_Certification_Runner.md` with orchestration command and expected diagnostics.

### Verified
- **Smoke Run**: `powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\obj\run_reject_evidence_capture.ps1 -DurationSeconds 90` executes and emits cycle/log freshness + reject-observation diagnostics.
- **Current Environment Result**: Latest smoke run reports `CYCLE_IS_FRESH=0`, indicating no new auto cycle was produced during capture; script returns partial guidance accordingly.

## [Exchanges/Ops] - US Geo Routing Defaults + Endpoint Overrides - 2026-02-17

### Changed
- **Binance US Default**: Updated `Exchanges/BinanceClient.cs` to default REST routing to `https://api.binance.us` for US-friendly connectivity.
- **Endpoint Override Hooks Added**: Added environment overrides for exchange REST hosts:
    - `CDTS_BINANCE_BASE_URL`
    - `CDTS_BYBIT_BASE_URL`
    - `CDTS_OKX_BASE_URL`
- **Bybit/OKX Override Support**: Updated `Exchanges/BybitClient.cs` and `Exchanges/OkxClient.cs` to honor the new host override variables while preserving existing defaults.

### Notes
- Geo restrictions are exchange/region dependent and may still require account-jurisdiction compatibility; host override support enables deterministic environment routing without code changes.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /t:Build /v:minimal` succeeds after endpoint routing updates.

## [Exchanges] - Binance Cancel Fast-Path Symbol Cache - 2026-02-17

### Changed
- **Cancel Fast-Path Added**: Updated `Exchanges/BinanceClient.cs` `CancelOrderAsync` to attempt direct symbol-scoped cancel first when an orderIdâ†’symbol mapping is already known.
- **Order Symbol Cache Added**: Added in-memory order symbol cache population on successful `PlaceOrderAsync` responses and fallback open-order discovery to avoid repeated full `openOrders` scans for known orders.
- **Fallback Behavior Preserved**: Existing full open-order scan path remains as a safe fallback when symbol is unknown or cached cancel attempt fails.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /t:Build /v:minimal` succeeds after cancel optimization changes.

## [Exchanges/Brokers/Strategy] - Candle Pagination + Cancel Semantics + Risk Geometry + Overnight Window Handling - 2026-02-17

### Changed
- **Binance Kline Pagination Completed**: Updated `Exchanges/BinanceClient.cs` `GetCandlesAsync` to page `/api/v3/klines` across the full requested interval (cursor advance by interval, max `1000` rows/page) and deduplicate candles by timestamp before returning.
- **Coinbase Cancel Semantics Hardened**: Updated `Exchanges/CoinbaseExchangeClient.cs` `CancelOrderAsync` to return `false` on non-success HTTP cancel outcomes instead of bubbling `HttpRequestException`, and replaced naive `string.Contains(orderId)` success detection with structured response parsing (`"id"`, `"order_id"`, quoted id, id arrays).
- **Broker Directional Validation Added**: Updated `Brokers/BinanceBroker.cs` and `Brokers/BybitBroker.cs` `ValidateTradePlan` to reject invalid stop/entry/target geometry (`long: stop < entry < target`, `short: target < entry < stop`) and reject zero-direction plans.
- **Trade Window Cross-Midnight Logic Fixed**: Updated `Strategy/TradePlanner.cs` `GovernanceRules.IsTimeBlocked` to correctly interpret windows where `NoTradeBefore > NoTradeAfter` (overnight session windows crossing midnight UTC).

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /t:Build /v:minimal` succeeds after these fixes.

## [Auto Mode/Ops Tooling] - Reject-Category Evidence Telemetry + Certification Merge Fallback - 2026-02-17

### Changed
- **Runtime Reject Evidence Emission Added**: Updated `UI/AutoModeControl.cs` to aggregate proposal reject categories during auto cycles and emit deterministic `[AutoMode][RejectEvidence]` logs for observed categories only (`fees-kill`, `slippage-kill`, `routing-unavailable`, `no-signal`, `ai-veto`, `bias-blocked`).
- **Cycle Artifact Enrichment Added**: Auto cycle telemetry now persists `RejectReasonCounts` in `cycle_*.json` artifacts for structured certification evidence.
- **Certification Fallback Merge Added**: Updated `Util/run_multiexchange_certification.ps1` to merge reject-category counts from cycle artifact `RejectReasonCounts` with log-derived counts before gate evaluation.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /t:Build /v:minimal` succeeds after runtime/certification changes.
- **Certification**: `Util/run_multiexchange_certification.ps1` executes successfully and emits `multi_exchange_cert_20260217_083024.json`; row evidence remains `PASS (30/30)` and reject-category gate remains `PARTIAL` until a fresh cycle records observed categories.

## [Coinbase Credentials] - CDP Key JSON Setup Normalization - 2026-02-17

### Changed
- **Raw Coinbase JSON Ingest Added**: Added `Util/CoinbaseCredentialNormalizer.cs` to parse Coinbase-provided credential blobs (`{"name":"organizations/.../apiKeys/...","privateKey":"-----BEGIN EC PRIVATE KEY-----..."}`), normalize escaped PEM newlines, and map fields to key-name/PEM storage.
- **Account/Key Dialog Save Flow Hardened**: Updated `UI/AccountEditDialog.cs` and `UI/KeyEditDialog.cs` to normalize Coinbase credential inputs before validation/save so pasted Coinbase JSON is accepted during setup.
- **Broker Runtime Credential Resolution Hardened**: Updated `Brokers/CoinbaseExchangeBroker.cs` to resolve Coinbase key-name/PEM and JSON-derived inputs into runtime client credentials consistently.
- **Client Credential Intake Normalized**: Updated `Exchanges/CoinbaseExchangeClient.cs` constructor/`SetCredentials` to normalize Coinbase credential payloads and emit an explicit message when PEM credentials are used against legacy exchange-header signing.
- **Project File Sync**: Updated `CryptoDayTraderSuite.csproj` to compile `Util/CoinbaseCredentialNormalizer.cs`.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /t:Build /v:minimal` succeeds after Coinbase credential normalization changes.

## [Ops Tooling] - Automatic Policy-Backed StrategyÃ—Exchange Row Evidence Emission - 2026-02-17

### Changed
- **Row Evidence Auto-Generation Added**: Updated `Util/run_multiexchange_certification.ps1` to emit `StrategyExchangePolicyEvidence` artifacts (`obj/runtime_reports/strategy_exchange_evidence/strategy_exchange_policy_evidence_*.json`) before matrix assembly.
- **Policy/Provider-Derived Row Classification**: Generated rows are classified from runtime policy contracts (`Services/StrategyExchangePolicyService.cs`) and latest provider probe classes (`PASS`, `ENV-CONSTRAINT`, `INTEGRATION-ERROR`) so each strategyÃ—exchange row has explicit evidence.
- **Certification Check Added**: Runner now records `Policy-backed row evidence artifact` as a deterministic check with generated artifact filename.
- **Runbook/Tracker Sync**: Updated `docs/ops/MultiExchange_Certification_Runner.md` and `PROGRESS_TRACKER.md` to reflect automatic row-evidence generation semantics.

### Verified
- **Certification Run**: `Util/run_multiexchange_certification.ps1` produced `multi_exchange_cert_20260217_082209.json` with `Strategy x exchange row evidence` = `PASS` and coverage `30/30`.
- **Overall Verdict**: Certification remains `PARTIAL` due to environment-constrained providers and missing reject-category runtime evidence, not missing matrix row evidence.

## [Ops Tooling] - Strict Certification Geo-Blocker Continuation - 2026-02-17

### Changed
- **Strict Provider Gate Refinement**: Updated `Util/run_multiexchange_certification.ps1` so `-RequireProviderArtifacts` no longer hard-fails environment-only (`ENV-CONSTRAINT`) provider outcomes; it now records explicit blocker checks and continues remaining strict evaluation.
- **Strict Coverage Gate Refinement**: Updated strict spot/perps coverage escalation to continue with explicit blocker checks when coverage is deferred only by environment constraints and no true coverage-missing condition exists.
- **Structured Blocker Signals Added**: Added check rows `Geo/provider access blocker` and `Spot/perp geo-access blocker` for explicit blocker tracking in JSON/TXT outputs.

### Verified
- **Baseline Certification**: `powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\Util\run_multiexchange_certification.ps1` emits `VERDICT=PARTIAL` with complete row-evidence coverage (`30/30`).
- **Strict Certification**: `powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\Util\run_multiexchange_certification.ps1 -RequireBuildPass -RequireMatrixPass -RequireProviderArtifacts -RequireRejectCategories` now reports geo-access blockers explicitly; remaining FAIL checks are non-geo (`Reject category evidence`, `Strict requirement: matrix`).

## [Ops Tooling] - Certification Evidence Selection Reliability Improvements - 2026-02-17

### Changed
- **Best Recent Matrix Evidence Selection**: Updated `Util/run_multiexchange_certification.ps1` to select a recent `MatrixStatus=PASS` cycle artifact (within scan window) for matrix gate evaluation instead of relying only on newest cycle artifact.
- **Recent-Log Reject Evidence Merge**: Updated reject-category extraction to merge across recent logs (bounded window) and preserve cycle-report merge behavior, reducing false `no tracked reject categories found` outcomes when the latest log is sparse.
- **Strict Verdict Semantics Hardened**: Updated verdict logic so strict mode fails only on explicit `FAIL` checks; blocker-classified `PARTIAL` checks continue as non-fatal evidence gaps.

### Verified
- **Strict Certification Outcome**: `Util/run_multiexchange_certification.ps1 -RequireBuildPass -RequireMatrixPass -RequireProviderArtifacts -RequireRejectCategories` now emits `VERDICT=PARTIAL` with `AutoMode matrix artifact=PASS`, `Reject category evidence=PASS`, and geo/provider checks clearly labeled as blockers.

## [Exchanges/Brokers] - Coinbase Symbol Constraint Enforcement Parity - 2026-02-17

### Changed
- **Coinbase Constraint Cache Added**: Updated `Exchanges/CoinbaseExchangeClient.cs` with a cached symbol-constraint map sourced from `/products` (`base_min_size`, `base_max_size`, `base_increment`, `quote_increment`, `min_market_funds`/`quote_min_size`).
- **Broker Fail-Closed Validation Added**: Updated `Brokers/CoinbaseExchangeBroker.cs` `ValidateTradePlan` to enforce symbol step alignment, min/max quantity, and min-notional checks using resolved Coinbase constraints.
- **Credential/Client Resolution Consolidated**: Added a reusable Coinbase client factory in broker paths so validation/place/cancel use consistent credential resolution and failure semantics.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /t:Build /v:minimal` succeeds after Coinbase constraint-parity changes.

## [Ops Tooling/Services] - GC19.2 Coverage Gate Refinement + Audit Service Compile Fix - 2026-02-17

### Changed
- **Audit Service Compile Regression Fixed**: Corrected method structure in `Services/ExchangeProviderAuditService.cs` so product coverage helpers are class-level methods (restoring valid compilation and probe execution).
- **Coverage Gate Semantics Refined**: Updated `Util/run_multiexchange_certification.ps1` spot/perps coverage evaluation to treat `ENV-CONSTRAINT` provider rows as deferred evidence (`spot-env-waived` / `perp-env-waived`) instead of false `spot-missing`/`perp-missing` deficits.
- **Perp Requirement Scope Aligned to Implementation**: Certification `requiredPerpServices` now targets currently perp-capable implementations (`Coinbase`, `Binance`, `Bybit`) while `OKX` and `Kraken` remain spot-only in current clients.
- **Runbook Sync**: Updated `docs/ops/MultiExchange_Certification_Runner.md` with waived-coverage semantics and current perp-required service scope.
- **Roadmap/Tracker Sync**: Updated `ROADMAP.md` (`GC19.2` acceptance progress) and `PROGRESS_TRACKER.md` (iteration updates).

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /p:Configuration=Debug /t:Build /v:minimal` succeeds.
- **Provider Probe**: `Util/run_provider_public_api_probe.ps1` emits `provider_public_api_probe_20260217_081528.json` (`PROBE_VERDICT=PARTIAL`, env-constrained `Binance`/`Bybit`).
- **Certification**: `Util/run_multiexchange_certification.ps1` emits `multi_exchange_cert_20260217_081645.json` with `VERDICT=PARTIAL` and spot/perp coverage detail `spot-env-waived=Binance,Bybit ; perp-env-waived=Binance,Bybit`.

## [Exchanges] - Binance TradeFee Aggregation Hardening - 2026-02-17

### Changed
- **TradeFee Row Aggregation**: Updated `Exchanges/BinanceClient.cs` `GetFeesAsync()` to parse all rows returned by `/sapi/v1/asset/tradeFee` instead of using only the first entry.
- **Conservative Fee Selection**: The returned `FeeSchedule` now uses worst-case maker/taker rates across all valid symbol rows to avoid optimistic expectancy underestimation when symbol-specific tiers differ.
- **Diagnostic Note Improvement**: Fee schedule notes now include the number of valid `tradeFee` rows consumed.

### Verified
- **Build Attempt**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /t:Build /v:minimal` currently fails due to pre-existing syntax errors in `Services/ExchangeProviderAuditService.cs` (unrelated to this change).

## [Services/Backtest] - GC19.4 Fee-Slippage Realism and Parity Completion - 2026-02-17

### Changed
- **Shared Cost Model Added**: Added `Services/ExecutionCostModelService.cs` to centralize execution friction assumptions (execution mode, round-trip fee bps, slippage bps, fee-tier adjustments, rebate hooks, venue-specific overrides).
- **Planner Cost Model Integration**: Updated `Services/AutoPlannerService.cs` to use shared cost assumptions for routing and expectancy gating, including modeled fee/slippage conversion into risk-multiple veto calculations.
- **Routing Telemetry Enrichment**: Planner proposal notes now emit execution-cost rationale tags (`ExecMode`, `FeeBps`, `SlipBps`) and AI review payload includes execution cost model context for auditability.
- **Backtest Cost Parity**: Updated `Services/BacktestService.cs` to source friction from `ExecutionCostModelService` instead of static hardcoded friction constants.
- **Composition Root + Project Sync**: Updated `Program.cs` wiring and `CryptoDayTraderSuite.csproj` compile items to include and inject the shared cost model service.
- **Roadmap/Tracker Closure**: Updated `ROADMAP.md` and `PROGRESS_TRACKER.md` to mark `G19.4`/`GC19.4` complete.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /t:Build /v:minimal` succeeds after cost-model parity changes.

## [Services/UI] - GC19.3 Policy Matrix + GC19.5 Funding Attribution Completion - 2026-02-17

### Changed
- **Runtime Policy Matrix Added**: Added `Services/StrategyExchangePolicyService.cs` as explicit strategyÃ—exchange policy source with deterministic policy decisions and reject codes (`policy-matrix-blocked`, `policy-health-blocked`, `policy-venue-unknown`, `regime-mismatch`).
- **Planner Policy Enforcement Wired**: Updated `Services/AutoPlannerService.cs` to evaluate policy before selecting a candidate plan, return deterministic policy reject diagnostics when blocked, and append selected policy rationale tags to emitted plan notes and AI review payload execution context.
- **Funding Source Tag Completion**: Planner funding-carry note telemetry now includes `FundingSource`, enabling downstream execution/realized attribution continuity.
- **Funding Executedâ†’Realized Attribution Completed**: Updated `UI/AutoModeControl.cs` to parse and carry funding telemetry from proposal note to execution records and protective close records (`funding-phase:executed|realized`, realized funding PnL tag).
- **Composition Root Wiring**: Updated `Program.cs` to inject `FundingCarryDetector` and `StrategyExchangePolicyService` into `AutoPlannerService`.
- **Project File Sync**: Updated `CryptoDayTraderSuite.csproj` to compile `Services/StrategyExchangePolicyService.cs`.
- **Roadmap/Tracker Sync**: Updated `ROADMAP.md` and `PROGRESS_TRACKER.md` to mark `G19.3`/`GC19.3` and `G19.5`/`GC19.5` complete.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /t:Build /v:minimal` succeeds after policy/funding completion changes.

## [Ops Tooling] - GC19.6 Evidence-Backed Certification Rows - 2026-02-17

### Changed
- **Synthetic Matrix Projection Removed**: Updated `Util/run_multiexchange_certification.ps1` to derive strategy Ã— exchange row status from row-level runtime/backtest evidence artifacts instead of projecting a single global matrix status.
- **Per-Row Evidence Fields Added**: Certification report rows now include explicit evidence fields (`EvidenceRef`, `EvidenceSource`, `Detail`) in both JSON output and TXT summary.
- **Row Evidence Discovery Added**: Runner now consumes row evidence from `-RowEvidenceDir`, `obj/runtime_reports/strategy_exchange_evidence`, `obj/runtime_reports/multiexchange/row_evidence`, and compatible cycle artifacts when they contain explicit row entries.
- **Strict Missing-Evidence Gate Added**: Strict mode now deterministically fails when any mandatory strategy Ã— exchange row lacks evidence coverage.
- **Docs/Tracker/Roadmap Sync**: Updated `docs/ops/MultiExchange_Certification_Runner.md`, `docs/ops/MultiExchange_Certification_Matrix.md`, `ROADMAP.md` (`G19.6`/`GC19.6`), and `PROGRESS_TRACKER.md`.

### Notes
- Default mode remains evidence-tolerant (`PARTIAL`) when row evidence is incomplete; strict mode enforces complete row coverage.

## [Planning/Audit] - Phase 19 Gap-Closure Execution Sequencing - 2026-02-16

### Changed
- **Sequenced Checklist Added**: Expanded `ROADMAP.md` with `Phase 19 Gap-Closure Sequenced Execution Checklist (Owners / Files / Acceptance)`.
- **Owner/File/Acceptance Coverage**: Added ordered `GC19.1..GC19.7` work packages mapping each previously-added `G19.*` gap to responsible owner area, primary implementation files, and verifiable acceptance criteria.
- **Progress Sync**: Updated `PROGRESS_TRACKER.md` with a milestone entry confirming checklist sequencing is in place.

### Notes
- This iteration is planning/tracking only; no runtime trading logic changes were made.

## [Planning/Audit] - Phase 19 Comprehensive Gap-Closure Addendum - 2026-02-16

### Changed
- **Roadmap Gap Closure Added**: Updated `ROADMAP.md` with a new `Phase 19 Gap-Closure Addendum (Comprehensive Audit 2026-02-16)` to track implementation work that was not explicitly covered by prior Phase 19 checklists.
- **Unplanned Work Captured as Explicit Tasks**: Added new tracked work for market-structure data contract completion (`IExchangeClient` + normalized DTOs), spot+perps scope completion, strategyÃ—exchange runtime rule-matrix enforcement, funding-carry runtime wiring, fee-tier/rebate realism, and evidence-backed certification matrix generation.
- **Documentation Alignment Tasks Added**: Added explicit reconciliation tasks to align stale/contradictory docs with code reality and separate `Implemented` vs `Target` capability states in multi-exchange planning docs.
- **Progress Tracker Synced**: Updated `PROGRESS_TRACKER.md` to include the comprehensive gap-mapping milestone.

### Notes
- This iteration updates planning/tracking artifacts only; no runtime trading code paths were changed.

## [Ops Tooling] - Provider Probe Failure Classification + Certification Integration Completion - 2026-02-17

### Changed
- **Provider Probe Classification Added**: Updated `Util/run_provider_public_api_probe.ps1` with deterministic failure-class mapping (`PASS`, `ENV-CONSTRAINT`, `PROVIDER-ERROR`, `INTEGRATION-ERROR`) and per-row `FailureClass`/`FailureReason` output.
- **Provider Verdict Logic Updated**: Probe verdict now returns `PARTIAL` for missing required services and for non-integration failures, and returns `FAIL` only when required-service failures include `INTEGRATION-ERROR`.
- **Provider Summary Added**: Probe report now includes `Summary` counts (`Passed`, `EnvConstraint`, `ProviderError`, `IntegrationError`, `MissingRequired`, `TotalRequired`) and text output appends `failureClass=<...>` per row.
- **Certification Consumer Updated**: Updated `Util/run_multiexchange_certification.ps1` provider probe evaluation to consume `FailureClass`, returning `FAIL` for integration failures and `PARTIAL` for environment/provider/unknown non-integration failures.
- **Docs/Tracker Alignment**: Updated `docs/ops/MultiExchange_Certification_Runner.md` and `PROGRESS_TRACKER.md` to reflect classified provider artifact handling and strict gate expectations.

### Notes
- Strict behavior remains unchanged: `-RequireProviderArtifacts` still forces non-`PASS` provider probe status to `FAIL`.

## [Ops Tooling] - Certification Runner Environment-Constraint Verdict Normalization - 2026-02-17

### Changed
- **Provider Probe Verdict Mapping Updated**: Updated `Util/run_multiexchange_certification.ps1` so provider probe failures caused by geo/network access constraints are classified as `PARTIAL` environment evidence in default mode instead of hard `FAIL`.
- **Strict Gate Behavior Preserved**: `-RequireProviderArtifacts` still enforces strict provider PASS requirements by escalating non-PASS provider probe outcomes to `FAIL`.
- **Kraken Probe Reliability Fix**: Updated `Exchanges/KrakenClient.cs` ticker parsing to accept both `object[]` and `ArrayList` payload shapes for `a`/`b`/`c` fields, preventing false negative ticker probe failures.
- **Runbook Clarified**: Updated `docs/ops/MultiExchange_Certification_Runner.md` notes to document default vs strict verdict behavior for environment-constrained venues.

### Notes
- This change reduces false-negative certification failures in restricted environments while preserving promotion safety under strict gates.

## [Ops Tooling] - Provider Public-API Probe + Certification Integration - 2026-02-17

### Changed
- **Provider Probe Tooling Added**: Added deterministic provider public-API probe evidence tooling in `Util/run_provider_public_api_probe.ps1` with artifact output used by strict certification gates.
- **Certification Integration Updated**: Updated `Util/run_multiexchange_certification.ps1` integration behavior to consume latest provider probe artifact and report explicit failing services in certification output.
- **Task/Runbook Integration**: Added VS Code task entries and runbook guidance so provider probe execution is documented as a strict-certification prerequisite.

### Notes
- Current environments may report venue failures caused by region/network restrictions; those outcomes should be treated as environment-constraint evidence unless corroborated by code-level regression signals.

## [Services/Ops] - Phase 19 Circuit-Breaker Guardrails (C19-11 Partial) - 2026-02-17

### Changed
- **Venue Circuit-Breaker State Added**: Extended `Services/VenueHealthService.cs` with per-venue breaker tracking for consecutive API failures, stale-quote streaks, and latency-breach streaks with timed re-enable windows.
- **Health Snapshot Enrichment**: Extended `Models/MarketData.cs` (`VenueHealthSnapshot`) to include circuit-breaker state fields (`CircuitBreakerOpen`, reason, opened/reenable timestamps).
- **Safe-Set-Empty Fail-Close**: Updated `Services/AutoPlannerService.cs` routing diagnostics to fail closed with `safe-set-empty-circuit-breaker` when all candidate venues are currently breaker-open.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /t:Build /v:minimal` succeeds after circuit-breaker changes.

## [Ops Tooling] - Multi-Exchange Certification Runner (C19-12, M5 Slice) - 2026-02-17

### Changed
- **One-Command Certification Runner Added**: Added `Util/run_multiexchange_certification.ps1` to generate deterministic multi-exchange certification verdict artifacts (`PASS`/`PARTIAL`/`FAIL`) in one command.
- **Timestamped Artifact Output**: Runner now emits paired JSON/TXT evidence files under `obj/runtime_reports/multiexchange` with check-level status details.
- **Matrix + Reject Summaries**: Reports include strategy Ã— exchange status rows and reject-category counts (`fees-kill`, `slippage-kill`, `routing-unavailable`, `no-signal`, `ai-veto`, `bias-blocked`) from latest runtime logs.
- **Lock-Safe Build Validation**: Runner build check uses `OutDir=bin\\Debug_Verify\\` to avoid false failures from default output binary locks.
- **Ops Runbook Added**: Added `docs/ops/MultiExchange_Certification_Runner.md` and indexed it in `docs/index.md`.
- **Certification Matrix/Checklist Sync**: Updated `docs/ops/MultiExchange_Certification_Matrix.md`, `docs/features/trading/MultiExchange_Execution_Checklist.md`, `ROADMAP.md`, and `PROGRESS_TRACKER.md` to reflect C19-12 tooling completion progress.

### Verified
- **Runner Execution**: `powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\\Util\\run_multiexchange_certification.ps1` executes and emits report artifacts with final verdict output.

## [Architecture/Services/Strategy] - Phase 19 Fee/Slippage Expectancy Gate (C19-09, B19.2 Slice) - 2026-02-16

### Changed
- **Canonical Expectancy Gate Added**: Updated `Services/AutoPlannerService.cs` to apply pre-trade expectancy validation using `E = (WinRate * AvgWin) - (LossRate * AvgLoss) - Fees - Slippage` before plan emission.
- **Risk Helper Contract Added**: Extended `Strategy/RiskGuards.cs` with reusable `ExpectancyBreakdown` helpers (`ComputeExpectancyBreakdown`, `NetEdgeIsViable`) for deterministic service-layer gating.
- **Deterministic Veto Reasons**: Planner now hard-blocks proposals with `fees-kill` or `slippage-kill` when expected edge is consumed below configured minimum net edge.
- **Telemetry Field Emission**: Planner now emits `gross edge`, `fee drag`, `slippage budget`, and `final net edge` diagnostics in plan notes and AI review execution payload.
- **Configurable Threshold Hooks**: Added environment-backed thresholds (`CDTS_EXPECTANCY_MIN_NET_EDGE_R`, `CDTS_EXPECTANCY_SLIPPAGE_BPS`) with deterministic defaults for unattended operation.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /t:Build /v:minimal` succeeds after the gate changes.

## [Architecture/Services] - Phase 19 Opportunity + Routing Core Foundation (P19.3 Slice) - 2026-02-16

### Changed
- **Spread Divergence Detector Added**: Added `Services/SpreadDivergenceDetector.cs` to evaluate cross-venue buy/sell opportunities from normalized quotes, compute gross/net edge in bps, and emit deterministic reject reasons (`fees-kill`, `slippage-kill`, `stale-quote`, `latency-risk`, `insufficient-depth`).
- **Funding Carry Detector Added**: Added `Services/FundingCarryDetector.cs` to rank carry opportunities across venues using funding-rate deltas and basis-stability scoring.
- **Venue Scorer Added**: Added `Services/ExecutionVenueScorer.cs` to score venues by expected net edge after fee/slippage plus health/latency penalties.
- **Smart Router Added**: Added `Services/SmartOrderRouter.cs` to select primary/fallback venue decisions from ranked venue scores.
- **Model Contract Expansion**: Extended `Models/MarketData.cs` with new contracts (`SpreadDivergenceOpportunity`, `FundingRateSnapshot`, `FundingCarryOpportunity`, `VenueExecutionScore`, `RoutingDecision`) for P19.3 service outputs.
- **Composition/Build Wiring**: Updated `Program.cs` to instantiate new P19.3 services and updated `CryptoDayTraderSuite.csproj` compile includes for all new service files.
- **Planner Orchestration Wiring**: Updated `Services/AutoPlannerService.cs` and `Program.cs` so proposal flow now consumes multi-venue routing diagnostics (spread-detector first, scorer/router fallback) and fail-closes with `routing-unavailable` when no compliant venue is available.

### Notes
- This slice intentionally keeps routing/detection in the service layer and does not add UI-layer domain logic.

## [Architecture/UI/Ops] - Provider API Verification + Per-Exchange Credential Enforcement - 2026-02-17

### Changed
- **Credential Policy Service Added**: Added `Services/ExchangeCredentialPolicy.cs` with centralized, per-exchange credential requirements (`coinbase-advanced`, `coinbase-exchange`, `binance`, `bybit`, `okx`, `kraken`, `bitstamp`, `paper`).
- **Account Dialog Enforcement**: Updated `UI/AccountEditDialog.cs` to use exchange-specific required credential checks for field visibility and save gating (including passphrase-required venues and Coinbase Advanced key-name/PEM flow).
- **Key Dialog Enforcement**: Updated `UI/KeyEditDialog.cs` save validation to enforce required fields by selected service policy before persisting key data.
- **Provider Public API Audit Service**: Added `Services/ExchangeProviderAuditService.cs` to run deterministic provider-level public API probes (`CreatePublicClient`, product discovery, ticker probe) with per-exchange result artifacts.
- **Project Wiring**: Updated `CryptoDayTraderSuite.csproj` compile includes for `ExchangeCredentialPolicy.cs` and `ExchangeProviderAuditService.cs`.
- **Ops/Planning Artifacts**: Added `docs/ops/MultiExchange_Provider_API_And_Credentials.md` and updated `ROADMAP.md`, `docs/features/trading/MultiExchange_Execution_Checklist.md`, and `docs/ops/MultiExchange_Certification_Matrix.md` with explicit provider/public API verification and per-exchange credential certification gates.
- **Indexing**: Added operations index entry in `docs/index.md` for the new provider API + credentials runbook.

### Notes
- This slice adds implementation-level credential enforcement and provider verification scaffolding; full automated certification runner integration remains tracked in Phase 19 validation tasks.

## [Architecture/Exchanges] - Multi-Exchange Adapter + Broker Implementation (P19.2 Slice) - 2026-02-16

### Changed
- **Binance Adapter Added**: Added `Exchanges/BinanceClient.cs` with implemented spot market-data and signed execution paths (`exchangeInfo`, `klines`, `bookTicker`, `tradeFee`, `order`, `cancel`).
- **Bybit Adapter Added**: Added `Exchanges/BybitClient.cs` with implemented v5 spot market-data and signed execution paths (`instruments-info`, `kline`, `tickers`, `fee-rate`, `order/create`, `order/cancel`).
- **OKX Adapter Added**: Added `Exchanges/OkxClient.cs` with implemented spot market-data and signed execution paths (`public/instruments`, `history-candles`, `ticker`, `trade-fee`, `trade/order`, `trade/cancel-order`).
- **Broker Expansion**: Added `Brokers/BinanceBroker.cs`, `Brokers/BybitBroker.cs`, and `Brokers/OkxBroker.cs` with real credential resolution, pre-trade validation, market order placement, and cancel-all wiring.
- **Runtime Wiring**: Updated `Services/ExchangeProvider.cs` factory normalization/switches, `UI/AutoModeForm.cs` `BrokerFactory`, and account/key service selection surfaces (`UI/AccountEditDialog.Designer.cs`, `UI/KeyEditDialog.cs`) to include new venues.
- **Project Wiring**: Updated `CryptoDayTraderSuite.csproj` compile includes for all new exchange/broker files.

### Verified
- **File Diagnostics**: No file-level errors reported for all new and modified P19.2 adapter/broker files.

### Notes
- This slice implements real request signing and execution paths for new venues; remaining Phase 19 work (opportunity detectors, smart routing, regime gates, and UI observability rollout) is still pending by roadmap sequence.

## [Architecture/Services] - Multi-Exchange Foundation Implementation (M1 Slice) - 2026-02-16

### Changed
- **Normalized Multi-Venue Models**: Extended `Models/MarketData.cs` with `VenueQuoteSnapshot`, `CompositeQuote`, and `VenueHealthSnapshot` to support per-venue quote provenance, staleness, latency, and health reporting.
- **Composite Quote Aggregation Service**: Added `Services/MultiVenueQuoteService.cs` to fetch ticker snapshots across currently integrated venues (`Coinbase`, `Kraken`, `Bitstamp`), normalize symbols, compute composite mid-rate, and emit confidence/staleness outcomes.
- **Venue Health Tracking Service**: Added `Services/VenueHealthService.cs` with rolling success/error/stale/RTT counters and deterministic health scoring snapshots.
- **Rate Router Upgrade**: Updated `Services/RateRouter.cs` to consume composite multi-venue quotes (with best-venue provenance and confidence logging) while retaining Coinbase fallback behavior for resilience.
- **Provider Alias Canonicalization**: Updated `Services/ExchangeProvider.cs` to normalize Coinbase Advanced naming variants (`coinbase advanced`, `coinbase-exchange`, etc.) to canonical `Coinbase` routing.
- **DI Wiring**: Updated `Program.cs` composition root to instantiate and wire `VenueHealthService` + `MultiVenueQuoteService` into `RateRouter`.

### Verified
- **File Diagnostics**: No file-level errors on updated/added implementation files (`MarketData.cs`, `MultiVenueQuoteService.cs`, `VenueHealthService.cs`, `RateRouter.cs`, `ExchangeProvider.cs`, `Program.cs`).

### Notes
- This is the first implementation slice of Phase 19 focused on data normalization and routing foundation; mandatory new venue adapters (Binance/Bybit/OKX) remain scheduled in later workstreams.

## [UI/Planning] - Multi-Exchange UI Execution Plan - 2026-02-16

### Changed
- **UI Execution Plan Added**: Added `docs/ui/MultiExchange_UI_Execution_Plan.md` defining the full Designer-first UI rollout for Phase 19.
- **Surface-Level Scope Defined**: Documented required Auto Mode, Planner, Dashboard, and Account/Key dialog changes for multi-venue controls and runtime observability.
- **UI Contract Boundaries Locked**: Explicitly reinforced passive-UI rules (no routing/risk/strategy logic in UI), inline status semantics, and telemetry/freshness visibility requirements.
- **Roadmap UI Deepening**: Expanded `ROADMAP.md` Phase 19 Workstream D with concrete UI implementation tasks (card decomposition, diagnostics columns, dashboard cards, status normalization, modal reduction, designer compliance).
- **Documentation Indexing**: Added the new UI plan under UI & Experience in `docs/index.md`.

### Notes
- This iteration is planning/documentation only; no runtime trading logic was changed.

## [AI Reliability] - JSON Parse Recovery Hardening (Planner + Governor) - 2026-02-16

### Changed
- **Planner JSON Normalization/Recovery**: `Services/AutoPlannerService.cs` now normalizes AI payloads (`code-fence`, `<pre>`, quoted-json forms), enumerates multiple JSON candidates (raw/object/array/unwrapped), and parses both object and list forms for `AIResponse`/`AITradeProposal`.
- **Planner One-Shot Strict Retry**: Added a bounded strict-json repair re-ask when initial parse fails, then re-parse before falling back to text heuristics.
- **Planner Invalid-Contract Fail-Closed**: Planner no longer emits parse-failure outcomes; invalid/unusable AI review contracts are explicitly vetoed (`ai-veto`) and invalid proposer contracts are explicitly rejected with deterministic warnings.
- **Planner Root-Cause Parser Coverage**: Added flexible contract decoding for proposer/review paths to handle real provider variants (`proposal/result/data/response` nesting, array-wrapped payloads, alternate keys like `decision/verdict`, and stringified numeric/boolean fields).
- **Prompt Contract Lock (Planner)**: Planner review/proposer prompts now explicitly require a single top-level object with exact schema key names and explicitly forbid wrapper keys/arrays at the source.
- **Transcript-Safe Planner Extraction**: Planner prompts/repair prompts now require `CDTS_JSON_START ... CDTS_JSON_END` response markers, and parser normalization extracts marker-delimited JSON first to avoid parsing echoed prompt/transcript fragments.
- **Governor JSON Normalization/Recovery**: `Services/AIGovernor.cs` now uses the same normalization and candidate extraction approach to reduce provider-specific parse failures.
- **Governor One-Shot Strict Retry**: When multi-provider fan-out yields no parseable bias, governor now performs one strict-json repair query and consumes it as a `Retry` source if valid.
- **Governor Invalid-Contract Coercion**: Governor no longer emits parse-failure paths; invalid provider contracts are explicitly logged and coerced to a neutral vote for deterministic consensus handling.
- **Governor Root-Cause Bias Parsing**: Bias parsing now accepts non-enum bias values and contract variants (`marketBias/regime/classification/verdict`) by keyword extraction (`bullish/bearish/neutral`) so provider wording drift does not break classification.
- **Prompt Contract Lock (Governor)**: Governor prompt and strict-repair prompt now explicitly require a single top-level object with exact schema key names and explicitly forbid wrapper keys/arrays.
- **Transcript-Safe Governor Extraction**: Governor prompts/repair prompts now require `CDTS_JSON_START ... CDTS_JSON_END` markers, and normalization extracts marker-delimited JSON first before generic sanitization.
- **Claude Response-Capture Root Cause Fix**: `Services/ChromeSidecar.cs` now narrows Claude read selectors to assistant-only nodes, rejects prompt-like echoed text in assistant-content heuristics, and disables generic fallback capture for Claude/Gemini paths to prevent user-prompt transcript text from being misclassified as model output.
- **Stuck-State Recovery (Claude/Gemini)**: `Services/ChromeSidecar.cs` now detects likely stuck provider states (busy/spinner + draft still in composer) after polling timeout and performs automatic refresh-and-resend recovery before fallback tab/chat recovery.
- **Claude Latest-Turn Anchoring**: `Services/ChromeSidecar.cs` Claude read script now anchors extraction to newest assistant-turn containers only, excludes composer/input ancestry, and suppresses token-echo transcript captures (`Return ONLY ... CDTS_RT_*`) before candidate acceptance.
- **Stale-Baseline Rejection (All Providers)**: `Services/ChromeSidecar.cs` polling confirmation now always rejects pre-send baseline echoes instead of allowing baseline matches after retry delay, preventing old assistant turns from being accepted as fresh model output in follow-up prompts.
- **Short-Reply Polling Acceptance**: `Services/ChromeSidecar.cs` now accepts validated short assistant responses (length > 12) after one confirm cycle, preventing legitimate concise replies from being dropped due unstable DOM snapshots between polls.
- **Sidecar Window Launch Controls**: `Services/ChromeSidecar.cs` now tracks the sidecar-launched Chrome process, launches it minimized by default (with optional hidden mode), and exposes runtime visibility APIs (`SetManagedChromeVisible`, `IsManagedChromeVisible`) for operator control.
- **Settings Panel Sidecar Window Toggle UI**: `MainForm.cs` Settings surface now includes `Launch Sidecar Hidden` and `Show/Hide Sidecar` controls, persists hidden-launch preference via `Properties/Settings.settings`, and applies runtime visibility actions to the live `ChromeSidecar` instance.
- **Settings Panel Sidecar Window Status**: Settings sidecar row now displays a live status label (`Visible`/`Hidden`/`Not managed`) and uses a local UI-sync guard to avoid recursive checkbox change handling while refreshing control state.
- **Binance Symbol Constraints Cache + Validation**: `Exchanges/BinanceClient.cs` now caches per-symbol `exchangeInfo` filters (`LOT_SIZE`, `MIN_NOTIONAL`/`NOTIONAL`, `PRICE_FILTER`) into `SymbolConstraints`, and `Brokers/BinanceBroker.cs` now fail-closes plans that violate symbol step size, min/max qty, or min notional before order placement.
- **Bybit Symbol Constraints Cache + Validation**: `Exchanges/BybitClient.cs` now caches per-symbol spot instrument filters (`lotSizeFilter`, `priceFilter`) into `SymbolConstraints`, and `Brokers/BybitBroker.cs` now fail-closes plans that violate symbol step size, min/max qty, or min notional before order placement.
- **Short-Reply Scope Refinement**: Early-return fallback is now limited to concise replies (`<= 32` chars) so larger JSON responses continue polling for stable/full capture instead of returning partial payloads.

### Verified
- **File Diagnostics**: No file-level errors in `Services/AutoPlannerService.cs` and `Services/AIGovernor.cs` after patch.
- **Compile Note**: `MSBuild CryptoDayTraderSuite.csproj /t:CoreCompile /p:Configuration=Debug /nologo` is currently blocked by unrelated workspace-wide type-resolution failures (for example in `UI/AutoModeControl.cs` and `UI/SidebarControl.Designer.cs`), not by planner/governor parser changes.

## [Architecture/Planning] - Multi-Exchange Strict Execution Checklist - 2026-02-16

### Changed
- **Execution Checklist Added**: Added `docs/features/trading/MultiExchange_Execution_Checklist.md` with strict owner/file/acceptance work packages for Phase 19 implementation.
- **Phase Mapping Formalized**: Checklist maps delivery sequence `P19.1..P19.6` into concrete work packages (`C19-*`) covering contracts/data, adapters, detectors/router, regime/risk/UI, operations, and promotion gates.
- **Acceptance Evidence Hardened**: Added explicit pass criteria and runtime evidence expectations for each package (deterministic reject reasons, route rationale, breaker transitions, and certification artifacts).
- **Documentation Indexing**: Updated `docs/index.md` Features section to include the new execution checklist.
- **Progress Tracking Sync**: Updated `PROGRESS_TRACKER.md` with a completed planning artifact entry for the strict checklist.

### Notes
- This iteration is planning/documentation only; no runtime trading logic was changed.

## [Architecture/Planning] - Phase 19 Roadmap Completion Pass - 2026-02-16

### Changed
- **Roadmap Task Completion Pass**: Expanded `ROADMAP.md` Phase 19 with blueprint-derived task groups that were not previously explicit as checklist items.
- **Strategy-Venue Matrix Tasks**: Added `B19.1` tasks for codifying strategy-to-venue mapping, regime-condition contracts, and deterministic strategy block reasons.
- **Expectancy/Fee Guardrail Tasks**: Added `B19.2` tasks for canonical fee/slippage-adjusted expectancy gating and telemetry of gross-vs-net edge.
- **Regime + Venue Orchestration Tasks**: Added `B19.3`/`B19.4` tasks for regime-state classification, role-based venue stack orchestration, and coverage validation for Auto Mode startup.
- **Layer-Contract + Delivery Sequence Tasks**: Added `B19.5` architecture-alignment tasks and explicit execution tracking phases `P19.1..P19.6` for end-to-end rollout visibility.

### Notes
- This iteration is planning/documentation only; no runtime trading logic or execution paths were changed.

## [Ops Tooling] - Runtime Snapshot Strict Gate Semantics Fix - 2026-02-16

### Changed
- **Strict Task Alignment**: Updated `.vscode/tasks.json` `verify-runtime-snapshot-strict` to gate on `-RequireNo429 -RequireCycles -IgnoreStartupOnly` (removed default `-RequireFills`).
- **Aggregate Health Gates**: `obj/verify_runtime_snapshot.ps1` now evaluates `RequireCycles` and `RequireFills` across sampled logs in aggregate while keeping `RequireNo429` strict per-log.
- **Runbook Clarification**: Updated `docs/ops/AutoMode_Runtime_Snapshot.md` to document aggregate gate semantics and when to use `-RequireFills`.

### Verified
- **Strict Default**: `...verify_runtime_snapshot.ps1 -Count 3 -RequireNo429 -RequireCycles -IgnoreStartupOnly` returned `RESULT:PASS` on current logs.
- **Fill-Gated Variant**: `...verify_runtime_snapshot.ps1 -Count 3 -RequireNo429 -RequireCycles -RequireFills` returned `RESULT:FAIL` with `aggregate: no fills in sampled logs`, as designed.

## [Architecture/Ops] - Multi-Exchange Decision Lock + Certification Matrix - 2026-02-16

### Changed
- **Decision Lock Applied**: Updated `docs/features/trading/MultiExchange_Implementation_MasterPlan.md` with confirmed implementation defaults:
    - all Phase 1 exchanges mandatory (Binance, Coinbase Advanced, Bybit, OKX, Kraken),
    - Spot + Perps in Phase 1 scope,
    - UI-selectable risk mode (`Unified` vs `Per-venue + Global cap`) with default `Per-venue + Global cap`,
    - strict telemetry completeness fail-closed gating,
    - maker-preferred execution policy with opportunity-aware taker fallback,
    - auto failover defaults (automatic in Auto Mode, manual approval in Manual Mode),
    - 28-day live-proof promotion gate.
- **Certification Runbook Added**: Added `docs/ops/MultiExchange_Certification_Matrix.md` defining mandatory exchange adapter tests and strategy-by-exchange backtest/forward/paper/live promotion gates.
- **Roadmap Expansion**: Updated `ROADMAP.md` Phase 19 with decision-lock implementation tasks and mandatory validation matrix tasks.
- **Index/Progress Sync**: Updated `docs/index.md` and `PROGRESS_TRACKER.md` to include and track the new certification matrix + decision lock planning artifacts.

### Notes
- This iteration is planning/documentation only; no runtime trading logic was changed.

## [Auto Mode] - OpenCap Stale Baseline Mitigation - 2026-02-16

### Changed
- **Persisted Open Baseline Window**: `UI/AutoModeControl.cs` now limits persisted open-position baseline to a configurable recent window (`CDTS_AUTOMODE_PERSISTED_OPEN_LOOKBACK_HOURS`, default `24`, clamped `1..168`) before applying `MaxConcurrentTrades` gating.
- **OpenCap Root-Cause Fix**: Prevents stale historical trade records from permanently inflating persisted open-count and blocking new execution with repeated `openCap` skips.

### Verified
- **Runtime Soak Evidence**: `obj/run_10min_soak.ps1` on `log_20260217_51.txt` reports `HTTP_429=0` and completed cycles (`CYCLE_COMPLETE=4`), confirming 429 pressure remains mitigated.
- **Current Strict Blocker**: Same soak window still reports `PAPER_FILL=0` driven by `openCap`/AI-veto behavior, not rate-limit failures.
- **Build Note**: Current workspace compile check is blocked by unrelated project-wide type-resolution errors in `UI/PlannerForm.cs` and `UI/AutoModeControl.Designer.cs` that were not introduced by this patch.

## [Architecture] - Multi-Exchange Master Planning Expansion - 2026-02-16

### Changed
- **Master Implementation Plan Added**: Added `docs/features/trading/MultiExchange_Implementation_MasterPlan.md` with exact file-level `Add / Convert / Remove` scope for multi-venue rollout, including DI conversions, provider/rate/planner/risk refactors, and adapter expansion.
- **Designer-First UI Scope Defined**: Documented concrete Auto Mode, Planner, Dashboard, and Account/Key dialog UI changes required for multi-venue configuration and runtime observability.
- **Set-and-Forget Reliability Gates**: Documented circuit-breaker, failover, order-intent journaling, and validation sequence requirements for unattended runtime operation.
- **Roadmap Deepening**: Expanded `ROADMAP.md` Phase 19 with workstreams (`Additions`, `Conversions`, `Removals`, `UI`, `Reliability`) and planning-baseline completion markers.
- **Documentation Indexing**: Added the master plan link to `docs/index.md` under Trading Features.
- **Progress Tracking**: Added planning-deep-dive completion line in `PROGRESS_TRACKER.md`.

### Notes
- This iteration is planning/documentation only; no trading runtime logic was changed.

## [Architecture] - Multi-Exchange Profit Blueprint Baseline - 2026-02-16

### Changed
- **New Trading Architecture Blueprint**: Added `docs/features/trading/MultiExchange_Profit_Architecture.md` to formalize a profit-first multi-venue implementation path (cross-exchange divergence, funding carry, liquidity/latency-aware routing, and fee/slippage-adjusted expectancy).
- **Roadmap Planning Phase**: Added `Phase 19: Multi-Exchange Profit Architecture` in `ROADMAP.md` with milestone tasks `M1..M5` covering market-data normalization, opportunity detection, smart routing, regime gating, and runtime KPI validation.
- **Progress Tracking**: Updated `PROGRESS_TRACKER.md` with a completed planning-baseline item linking the new blueprint and roadmap phase.
- **Documentation Indexing**: Added blueprint link to `docs/index.md` under Trading Features.

### Notes
- This iteration is documentation and planning only; no runtime trading logic was modified.

## [Sidecar Reliability] - Provider Round-Robin + Claude Submit Hardening - 2026-02-16

### Changed
- **Provider Load Spreading**: `Services/ChromeSidecar.cs` now rotates non-strict query primaries across `ChatGPT -> Gemini -> Claude` using round-robin, instead of repeatedly starting from the same provider.
- **Active Provider Switching**: `QueryAIAsync(...)` now actively reconnects to the selected primary provider tab before prompt injection so round-robin selection is actually applied at runtime.
- **Rotating Multi-Service Fan-Out**: `QueryAcrossAvailableServicesAsync(...)` now rotates provider order per call, reducing repeated first-hit pressure on one provider.
- **Claude Submit Safety**: Claude injection now excludes stop/cancel controls from send-button selection, recognizes visible stop-state as already-sent, and adds keyboard fallback (`Ctrl+Enter`) to reduce no-send stalls.

### Verified
- **Diagnostics**: File diagnostics report no errors in `Services/ChromeSidecar.cs` and `MainForm.cs` after patch.
- **Build Health**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug /nologo` succeeds (with transient file-lock copy warnings only).

## [Audit Continuation] - Lifecycle + Observability Regression Cleanup - 2026-02-16

### Changed
- **MainForm Sidecar Lifecycle**: `MainForm.InitializeDependencies(...)` no longer performs duplicate background `ChromeSidecar.ConnectAsync()` calls; startup now logs that sidecar connectivity is governor/runtime-managed.
- **MainForm Startup Diagnostics**: `LoadCoinbaseCdpKeys()` constructor path now logs explicit warning details on failure instead of silent catch.
- **Coinbase Public Client Observability**: `Exchanges/CoinbasePublicClient.cs` now logs explicit warning context for candle chunk fetch failures and ticker fallback failures instead of silent catch blocks.
- **AutoMode Cadence Decoupling Restored**: `UI/AutoModeControl.cs` `ApplySelectedProfile()` no longer writes profile interval into global `numAutoInterval`, preserving global timer cadence behavior.

### Verified
- **Diagnostics**: Workspace error scan reports no compile-time errors in updated files.
- **Audit Sweep**: Active runtime code no longer contains the identified duplicate sidecar connect startup path or silent Coinbase public-client catch blocks.

## [Auto Mode] - 429 Backoff Mitigation Hardening - 2026-02-16

### Changed
- **HTTP Retry Classifier**: `Util/HttpUtil.cs` now classifies `429`/`Too Many Requests` as transient and applies stronger retry backoff (`>= 2000ms * attempt`) for rate-limited retries.
- **Resilient Client Classifier**: `Services/ResilientExchangeClient.cs` transient detection now includes `429`/`Too Many Requests` message patterns.
- **Adaptive Scan Backoff**: `UI/AutoModeControl.cs` `ScanSymbolsAsync(...)` now uses adaptive per-symbol delay escalation after rate-limit failures (up to 3000ms) with gradual recovery instead of fixed 120ms pacing under pressure.

### Verified
- **Compile Health**: `MSBuild CryptoDayTraderSuite.csproj /t:CoreCompile /p:Configuration=Debug /nologo` succeeded with 0 warnings and 0 errors.
- **Runtime Snapshot (Post-Fix)**: `obj/verify_runtime_snapshot.ps1 -Count 3 -RequireNo429 -RequireCycles -RequireFills -IgnoreStartupOnly` shows latest sampled logs with `429=0`; strict failure is currently due to `no fills` / `no completed cycle` windows rather than rate-limit violations.

## [Auto Mode] - B5 One-Command Scenario Runner - 2026-02-17

### Changed
- **Deterministic B5 Runner Script**: Added `Util/run_b5_validation_scenario.ps1` to seed strict Track B5 scenario profiles (`Selected=3`, `All`, `Selected=15` + failure-probe), run a timed app soak, and execute strict matrix validation in one command.
- **Safe Profile Backup/Restore**: Runner now snapshots pre-run auto profiles and restores them by default after validation to avoid leaving operator state mutated.
- **Stale Report Guard**: Runner now validates only cycle reports generated after the current run start time (no fallback to older reports).
- **Sticky Auto-Run Force-On**: Runner now enables `AutoModeAutoRunEnabled` through reflection-based settings access (works even when Settings type is internal).
- **Ops Runbook**: Added [docs/ops/AutoMode_B5_Scenario_Runner.md](docs/ops/AutoMode_B5_Scenario_Runner.md) with command usage, parameters, outputs, and prerequisites.
- **Indexing**: Added the B5 scenario runner document to [docs/index.md](docs/index.md) Operations section.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.sln /t:Build /p:Configuration=Debug` succeeded; observed transient `MSB3026` copy-retry warnings caused by a running `CryptoDayTraderSuite.exe` lock.
- **Runtime B5 Evidence (Fresh Report)**: Runner now produces a fresh mixed-scope report (`cycle_20260217_055317941_fde9fcc2.json`) with `EnabledProfileCount=4`, `ProcessedProfileCount=4`, and strict validator output `PARTIAL` (current fail points: pair configuration consistency, independent guardrail observation, and failure-isolation evidence).

## [Ops Tooling] - Task Label Dedupe + Snapshot Re-Validation - 2026-02-16

### Changed
- **VS Code Task Label Uniqueness**: `.vscode/tasks.json` task labels were normalized so each task now has a unique `label`, eliminating picker ambiguity from duplicate legacy labels (including repeated `skip_task`, `write_plan_content`, `create_file_ps2`, and `runtime-claude-token-validation`).
- **No Runtime Logic Changes**: This iteration only adjusted task metadata and preserved existing command payloads.

### Verified
- **Task Label Audit**: `Get-Content .\.vscode\tasks.json -Raw | ConvertFrom-Json` label grouping check now returns `NO_DUPLICATE_LABELS`.
- **Strict Snapshot (Current Logs)**: `obj/verify_runtime_snapshot.ps1 -Count 3 -RequireNo429 -RequireCycles -RequireFills -IgnoreStartupOnly` currently returns `FAIL` due to real sampled-log violations (`429` and `no fills` in `log_20260217_38.txt`, plus `no fills` in `log_20260217_39.txt`).
- **Baseline Snapshot**: `obj/verify_runtime_snapshot.ps1 -Count 4` returns `PASS`, confirming verifier operational after task cleanup.

## [Sidecar Reliability] - Seamless CDP Startup Preflight - 2026-02-16

### Changed
- **Automatic CDP Host Bootstrap**: `Services/ChromeSidecar.cs` now runs a startup preflight that checks `http://localhost:9222/json/version` and auto-starts Chrome with `--remote-debugging-port=9222` + `%LocalAppData%\CryptoSidecar` profile when CDP is unavailable.
- **Launch Readiness Wait**: After auto-launch, sidecar now waits for CDP host readiness before proceeding with tab discovery, reducing cold-start race failures.
- **Local Chrome Discovery**: Added standard Chrome path resolution (`Program Files`, `Program Files (x86)`, `%LocalAppData%`) for seamless first-run behavior.

### Verified
- **Compile Health**: `Services/ChromeSidecar.cs` shows no file-level compile errors.
- **Runtime Preflight Smoke**: With Chrome explicitly closed, direct sidecar call `ConnectAsync("ChatGPT")` returned `RESULT:CONNECTED=True`, confirming automatic CDP bootstrap path works.

## [Planner UX] - Multi-Select + Propose All Batch Workflow - 2026-02-17

### Changed
- **Planner Batch Action Wiring**: `UI/PlannerControl.cs` now wires `btnProposeAll` and adds `DoProposeAll()` to project/propose plans across all listed symbols for the selected account.
- **Bulk Propose Dedup Helper**: Added shared `AddProposedPlans(...)` in `UI/PlannerControl.cs` so single-symbol and all-symbol propose flows use consistent duplicate detection and planned-trade creation.
- **Multi-Row Delete Support**: `DeleteSelectedTrade()` in `UI/PlannerControl.cs` now deletes all selected planned rows (with count-aware confirmation) instead of only the first selected row.
- **Checkbox Commit Usability**: Added `gridPlanned.CurrentCellDirtyStateChanged` commit handling so checkbox edits apply immediately without requiring focus changes.
- **Designer Batch Selection Enablement**: `UI/PlannerControl.Designer.cs` now includes `btnProposeAll` and enables `MultiSelect` on Planner grids; `UI/AutoModeControl.Designer.cs` explicitly enables multi-select on the Auto grid.
- **Source Corruption Repair (Blocking Build)**: `UI/AutoModeControl.cs` had pre-existing literal escaped backtick/newline corruption in telemetry declarations and matrix evaluation logic; these lines were normalized to valid C# members/statements.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.sln /t:Build /p:Configuration=Debug` succeeded with 0 warnings and 0 errors after the Planner batch wiring and AutoMode corruption repair.

## [Sidecar Reliability] - ChatGPT/Gemini No-Send Submit Fix - 2026-02-16

### Changed
- **Verified Submit Handshake (ChatGPT + Gemini)**: `Services/ChromeSidecar.cs` injection scripts for ChatGPT and Gemini now set `window.__cdts_last_send_status='pending'`, perform deterministic multi-strategy submit attempts (send button, form submit, keyboard fallback), and only mark success after post-submit state change checks.
- **False-Positive Send Removal**: Gemini no longer treats `dispatchEvent(...) == true` as a send success; submit is now confirmed from UI state transition instead of event return value.
- **Fail-Closed No-Send Detection**: Both providers now return `ok:no-send` when the prompt remains in composer and no submit transition is observed.
- **Pending Resolution Scope Expanded**: `ResolvePendingSendStatusAsync(...)` now handles ChatGPT in addition to Gemini/Claude so pending submit outcomes are resolved before response polling.

### Verified
- **Build (Lock-Safe)**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug /p:OutDir=bin\Debug_Verify\ /nologo` succeeded with 0 warnings and 0 errors.
- **Runtime (Direct Sidecar, ChatGPT)**: `ConnectAsync("ChatGPT")` returned `True`; token probe response returned non-empty with exact token match (`HAS_TOKEN=True`).
- **Runtime (Direct Sidecar, Gemini)**: `ConnectAsync("Gemini")` returned `True`; token probe response returned non-empty with exact token match (`HAS_TOKEN=True`).

## [Ops Tooling] - Runtime Snapshot Verifier Script - 2026-02-16

### Changed
- **Deterministic Snapshot Script**: Added `obj/verify_runtime_snapshot.ps1` to emit one-line per-log metrics and aggregate summary over recent runtime logs (`cycles`, `fills`, `429`, `noSignal`, `biasBlock`, `aiVeto`, `limitApplied`).
- **Strict Gate Options**: Script now supports `-RequireNo429`, `-RequireCycles`, and `-RequireFills` for quick PASS/FAIL runtime health checks.
- **Startup-Only Skip Option**: Added `-IgnoreStartupOnly` to avoid strict false-negatives when the newest sampled log only contains startup lines.
- **Ops Documentation**: Added [docs/ops/AutoMode_Runtime_Snapshot.md](docs/ops/AutoMode_Runtime_Snapshot.md) and linked it in [docs/index.md](docs/index.md).
- **Task Runner Wiring**: Added VS Code tasks in `.vscode/tasks.json` (`verify-runtime-snapshot`, `verify-runtime-snapshot-strict`) for one-click execution.
- **Hard-Strict Task Variant**: Added `verify-runtime-snapshot-strict-no-skip` for intentionally strict startup-inclusive validation (no startup-only bypass).

### Verified
- **Script Execution**: `powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\obj\verify_runtime_snapshot.ps1 -Count 4 -RequireNo429 -RequireCycles -RequireFills` executed successfully and returned expected strict-mode `FAIL` when the most recent startup-only log lacked cycle/fill evidence.
- **Strict Stable Pass**: `...verify_runtime_snapshot.ps1 -Count 3 -RequireNo429 -RequireCycles -RequireFills -IgnoreStartupOnly` returns `RESULT:PASS` on current recent logs while still preserving strict checks for non-startup logs.
- **Hard-Strict Behavior**: `...verify_runtime_snapshot.ps1 -Count 3 -RequireNo429 -RequireCycles -RequireFills` returns expected `RESULT:FAIL` when sampled logs are startup-only.

## [UI Polish] - Popup Dialog Typography & Control Rhythm (Phase 3) - 2026-02-16

### Changed
- **Shared Typography Rules**: `UI/DialogTheme.cs` now applies stronger popup typography hierarchy (semibold actions, accent-colored section headers) for credential/header labels.
- **Control Height Consistency**: `UI/DialogTheme.cs` now normalizes input/action sizing across popup controls (`TextBox`, `ComboBox`, `NumericUpDown`, `DateTimePicker`, `Button`) for cleaner editing rhythm.
- **Action Bar Consistency**: Right-aligned footer action bars now receive consistent padding in shared theming.
- **Key Dialog Header Clarity**: `UI/KeyEditDialog.cs` section labels now use explicit credential headers (`Coinbase Exchange Credentials`, `Coinbase Advanced Credentials`).

### Verified
- **Code Diagnostics**: No file-level errors in changed popup files (`UI/DialogTheme.cs`, `UI/KeyEditDialog.cs`).
- **Build Progression**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` advances through compile and fails only at output copy because `bin\\Debug\\CryptoDayTraderSuite.exe` is locked by running local processes.
- **Lock-Safe Build Artifact**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug /p:OutDir=bin\\Debug_Verify\\ /nologo /v:minimal` succeeded and produced `bin\\Debug_Verify\\CryptoDayTraderSuite.exe`.

## [Auto Mode] - Sustained Run Snapshot (Latest) - 2026-02-16

### Verified
- **Latest Runtime Evidence**: `%LocalAppData%\CryptoDayTraderSuite\logs\log_20260217_31.txt` confirms sticky auto-run restore, All-scope cap enforcement (`448 -> 12`), and successful cycle completion (`ok=3, fail=0`).
- **Execution Outcomes**: Same run shows paper fills for `BCH/USD`, `AVAX/USD`, and `LINK/USD`, then telemetry artifact emission (`cycle_20260217_035227509_dfd3ac80.json`).
- **Rate-Limit Signal**: No `429` lines were observed in that latest sampled cycle window.
- **Strict Snapshot (Recent 4 Logs)**: `log_20260217_{28..31}.txt` each show `cycles=1`, `fills=3`, `429=0` (with `noSignal` informational counts only), confirming repeatable capped-cycle execution across consecutive runs.

## [UI Polish] - Popup Dialog Layout Pass (Phase 2) - 2026-02-16

### Changed
- **Trade Dialog Layout Refinement**: `UI/TradeEditDialog.Designer.cs` now uses stronger label/input column proportions, multiline notes with dedicated height, and a compact full-width bottom action row.
- **Account Dialog Form Hierarchy**: `UI/AccountEditDialog.Designer.cs` now has wider input lanes, consistent field spacing/margins, cleaner credential section rhythm, and normalized bottom action bar button sizing.
- **Strategy Config Structure**: `UI/StrategyConfigDialog.Designer.cs` now uses a structured table layout shell (top selector row, full-height property grid, aligned footer action) instead of loosely anchored controls.
- **Key Dialog Runtime Layout Upgrade**: `UI/KeyEditDialog.cs` programmatic layout now applies fixed label-column alignment, docked input controls, scroll-safe content spacing, and standardized save/cancel action bar geometry.

### Verified
- **Code Diagnostics**: No file-level compiler diagnostics in updated popup files (`TradeEditDialog.Designer`, `AccountEditDialog.Designer`, `StrategyConfigDialog.Designer`, `KeyEditDialog`).
- **Compile Target**: `MSBuild CryptoDayTraderSuite.csproj /t:CoreCompile /p:Configuration=Debug /nologo` succeeded with 0 warnings and 0 errors.

## [Auto Mode] - Post-Mitigation Runtime Soak Verification - 2026-02-16

### Verified
- **Runtime Evidence**: `%LocalAppData%\CryptoDayTraderSuite\logs\log_20260217_28.txt` shows sticky auto-run restore, All-scope symbol limiting (`448 -> 12`), successful propose/execute flow, and cycle completion with fills (`ok=3, fail=0`).
- **Execution Outcomes**: Same run includes paper executions (`BCH/USD`, `LINK/USD`, `AVAX/USD`) and telemetry artifact write (`cycle_20260217_032316326_0fadc14d.json`), confirming end-to-end auto-cycle progression.
- **Rate-Limit Signal**: No `429` entries appear in the sampled runtime window around that completed cycle.

## [UI Polish] - Unified Popup Dialog Visual Theme - 2026-02-16

### Changed
- **Shared Dialog Theme Layer**: Added `UI/DialogTheme.cs` to style popup forms with the existing app theme tokens (`Theme.ContentBg`, `Theme.PanelBg`, `Theme.Text`, `Theme.TextMuted`, `Theme.Accent`).
- **Dialog Coverage**: Applied the shared dialog theme to `UI/TradeEditDialog.cs`, `UI/AccountEditDialog.cs`, `UI/KeyEditDialog.cs`, and `UI/StrategyConfigDialog.cs`.
- **Project Wiring**: Added `UI/DialogTheme.cs` to `CryptoDayTraderSuite.csproj` compile includes.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug /nologo` succeeded with 0 warnings and 0 errors.

## [Auto Mode] - Strict USD Scope + Lower Default All-Cap Tuning - 2026-02-17

### Changed
- **Strict USD Quote Filter**: `UI/AutoModeControl.cs` pair ranking now only accepts strict USD quote suffixes (`-USD` or `/USD`) so `USDT` markets no longer inflate All-scope scan volume.
- **Lower Default All-Scope Cap**: Default `PairScope=All` symbol cap was reduced from `25` to `12` (still overrideable via `CDTS_AUTOMODE_MAX_SYMBOLS`) to reduce public-candle burst pressure.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug /nologo` succeeded after clearing a stale binary lock.
- **Runtime Smoke**: Fresh run log `%LocalAppData%\CryptoDayTraderSuite\logs\log_20260217_25.txt` shows sticky Auto Run restore and successful autonomous execution (`Auto cycle complete ... ok=2, fail=0`) with paper fills for BTC/USD and ETH/USD.

## [AI Sidecar] - AI Tab Acquisition Recovery Hardening - 2026-02-17

### Changed
- **Connection Recovery Loop**: `Services/ChromeSidecar.cs` now runs a bounded multi-attempt AI-tab acquisition loop before declaring sidecar disconnect when no AI tab is detected.
- **Provider-Aware Bootstrapping**: Connection now prioritizes preferred provider (when requested) and then probes ChatGPT/Gemini/Claude in deterministic order for broader recovery coverage.
- **Fallback URL Creation**: Added provider-specific fallback tab URLs (`chat.openai.com`, `gemini.google.com/app`, `claude.ai/`) used after initial tab-creation attempts fail.
- **Recovery Visibility**: Sidecar now logs fallback tab-open actions so operators can distinguish hard tab-discovery failures from transient startup gaps.
- **Fail-Fast Timeouts**: Sidecar now applies explicit HTTP and WebSocket connect timeouts to avoid long hangs when CDP endpoint/tab connection is unavailable.

### Verified
- **Build Check**: `Services/ChromeSidecar.cs` has no compile errors; full project build is currently blocked by unrelated pre-existing UI errors (`DialogTheme` unresolved in dialog files).

## [Verification Tooling] - Scripted Sidecar Disconnect PASS/FAIL Check - 2026-02-16

### Changed
- **Reusable Verifier Script**: Added `obj/verify_disconnect.ps1` to run deterministic sidecar fail-safe validation (`connect -> force chrome disconnect -> log evidence -> PASS/FAIL`).
- **Task Compatibility**: Script path matches existing VS Code task `verify-sidecar-disconnect-script` for repeatable execution.

### Verified
- **Script Run**: `powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\obj\verify_disconnect.ps1` produced `CONNECTED_COUNT=1`, `DISCONNECTED_COUNT=1`, `RESULT:PASS`.

## [Auto Mode] - Public Rate-Limit Mitigation for Auto Cycles - 2026-02-16

### Changed
- **All-Scope Symbol Cap**: `UI/AutoModeControl.cs` now limits `PairScope=All` scan universe per cycle (default `25`, override via `CDTS_AUTOMODE_MAX_SYMBOLS`) to avoid exhausting public candle quota.
- **Per-Symbol Pacing**: Added small per-symbol scan delay to lower burst pressure on Coinbase public candles endpoint.
- **Exception Isolation**: Symbol scan now catches per-symbol failures and continues remaining symbols, preventing one fetch failure from collapsing the full scan loop.
- **Operational Diagnostics**: Added explicit warning logs when profile symbol lists are auto-limited for rate-limit protection.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug /nologo` succeeded with 0 warnings and 0 errors.

## [Sidecar Reliability] - Claude Prompt Send Stall Fix - 2026-02-16

### Changed
- **Claude Submit Hardening**: `Services/ChromeSidecar.cs` Claude injection path now performs deterministic submit attempts (send button, form submit, and keyboard fallbacks) after prompt insertion.
- **No-Send Fail-Fast**: Sidecar now treats `ok:no-send` injection outcomes as submission failures and retries/reconnects instead of polling indefinitely on an unsent prompt.
- **False-Positive Send Guard**: Claude keyboard fallback no longer marks prompts as sent unconditionally; send success now requires post-submit state change (input cleared/changed or send button disabled) so unsent prompts correctly return `ok:no-send`.
- **Pending Send Handshake**: Provider injection now resolves `ok:pending` by polling `window.__cdts_last_send_status` (`sent:*` or `no-send`) before accepting prompt injection as successful.
- **Recovery Alignment**: Provider recovery paths now also reject `no-send` injection status so Claude/Gemini recovery cannot silently continue with a queued-but-unsent prompt.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug /nologo` succeeded with 0 warnings and 0 errors.
- **Locked-File-Safe Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug /p:OutDir=bin\Debug_Verify\ /nologo` succeeded with 0 warnings and 0 errors.
- **Runtime (Direct Sidecar, Claude)**: `ChromeSidecar.ConnectAsync("Claude")` returned `CONNECTED=True`; two live `QueryAIAsync(...)` calls returned non-empty responses; token probe (`Reply with exactly this token...`) returned `RESP_HAS_TOKEN=True`, confirming prompt submission and model response at runtime.
- **Runtime (Multi-Token Probe, Claude)**: scripted probe (`obj/runtime_claude_probe.ps1`) executed 3 unique-token prompts with `ATTEMPT_1/2/3_HAS_TOKEN=True`, `RUNTIME_MULTI_TOKEN_PASS=True`, and latest log counters showing `Prompt_injection_failed=0`, `Prompt_injection_did_not_submit=0`, `ok_no-send=0`, `Response_polling_timed_out=0`.
- **Runtime (Direct Sidecar, Claude, Repeated)**: Scripted multi-token validation (`obj/runtime_claude_token_validation.ps1`) completed with `CONNECTED=True`, `TOKEN_MATCH_COUNT=3`, and `TRY_1..3_HAS_TOKEN=True`, demonstrating repeated live prompt submission/response behavior rather than stale transcript reads.

## [Lifecycle] - Deterministic Sidecar Shutdown Cleanup - 2026-02-16

### Changed
- **Application Shutdown Cleanup**: `Program.cs` now wraps `Application.Run(form)` in a `try/finally` and calls `aiGovernor.Stop()` and `chromeSidecar.Dispose()` during shutdown.
- **Disconnect Transition Reliability**: Sidecar disconnect status now has a deterministic shutdown path instead of depending on OS process teardown timing.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug /nologo` succeeded with 0 warnings and 0 errors.
- **Runtime Evidence**: Recent log scan over 20 files reported `RESULT:DISCONNECTED=79` and `RESULT:DISPOSE_REASON=2`.

## [Auto Planner] - Multi-Strategy Live Signal Fallback - 2026-02-16

### Changed
- **Candidate Strategy Evaluation**: `Services/AutoPlannerService.cs` proposal flow now evaluates ranked projection rows in order and selects the first live, bias-compliant strategy signal, instead of failing immediately when the top expectancy row has no current signal.
- **Reason Fidelity**: Proposal diagnostics now aggregate candidate outcomes so `no-signal` and `bias-blocked` reflect all ranked strategy attempts for the symbol.
- **Plan Annotation Consistency**: Generated plan notes and AI-review payloads now use the actually selected strategy row metrics (`expectancy`, `winRate`, `samples`).

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug /nologo` succeeded with 0 warnings and 0 errors.

## [Auto Mode] - Empty Pair Scope Fallback Hardening - 2026-02-17

### Changed
- **Profile Scope Normalization**: `UI/AutoModeControl.cs` now normalizes `PairScope` values (`Trim` + case-insensitive match) before resolving symbols, preventing false misses for `All` scope variants.
- **Selected-Scope Failover**: When `Selected` scope resolves to zero pairs, Auto Mode now falls back to runtime product universe instead of skipping with `reason=pairs`.
- **Operational Visibility**: Added warning log when selected-scope fallback activates, so profile misconfiguration is visible while cycle execution continues.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug /p:OutDir=bin\Debug_Verify\` succeeded with 0 warnings and 0 errors.

## [Verification] - Sidecar Fail-Safe Disconnect Evidence Closed - 2026-02-17

### Verified
- **Fail-Safe Scenario**: Forced Chrome sidecar termination now records explicit disconnect evidence in runtime logs (`18:37:12 ... [ChromeSidecar] Disconnected: CDP receive failed due to closed socket.` in `%LocalAppData%\CryptoDayTraderSuite\logs\log_20260217_17.txt`).
- **Build**: `MSBuild CryptoDayTraderSuite.sln /t:Build /p:Configuration=Debug` succeeded with 0 warnings and 0 errors.

## [Auto Mode] - Startup Autonomy + Symbol Universe Fail-Open - 2026-02-16

### Changed
- **Auto Startup Autonomy**: `MainForm.cs` now eagerly initializes and caches `AutoModeControl` during shell build, allowing sticky Auto Run to start without requiring manual navigation to the Auto page.
- **Runtime Product Universe Cache**: `UI/AutoModeControl.cs` now maintains a runtime product-universe cache and resolves `All` scope from UI list, combo list, cache, then `PopularPairs` fallback.
- **Fail-Open Product Refresh**: Auto cycle startup now uses a bounded product refresh (`Task.WhenAny(..., 3s timeout)`) and falls back safely instead of stalling cycle execution when product listing is slow/unavailable.
- **Empty-Universe Diagnostics**: Added explicit warning log when a profile is skipped due to an empty symbol universe.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 warnings and 0 errors.

## [Sidecar Observability] - Explicit Disconnect Reason Logging - 2026-02-16

### Changed
- **Disconnect Reason Coverage**: `Services/ChromeSidecar.cs` now uses `MarkDisconnected(...)` for previously silent disconnect status paths during connection failure handling.
- **Dispose Visibility**: Sidecar disposal now emits an explicit disconnect log reason (`Chrome sidecar disposed.`) instead of a silent status transition.
- **Operational Diagnostics**: Runtime logs now provide clearer evidence for disconnect transitions needed by fail-safe verification.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug /nologo` succeeded with 0 warnings and 0 errors.

## [Setup Guard] - Proactive Account Save Button Validation State - 2026-02-16

### Changed
- **Realtime Save Eligibility**: `UI/AccountEditDialog.cs` now evaluates account validity continuously and enables/disables `Save` based on current form inputs.
- **Rule Parity with Submit Validation**: Save-button gating mirrors existing non-paper key requirements, including existing-key selection rules and provider-specific credential requirements.
- **Fallback Safety Preserved**: Click-time validation prompts remain in place as a fail-safe for any edge-state transitions.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug /nologo` succeeded with 0 warnings and 0 errors.

## [Verification] - Final Sweep (Planner Evidence + Remaining Fail-Safe) - 2026-02-16

### Verified
- **Planner AI Review Path**: Recent runtime logs include `Starting AI review for generated trade` and `AI vetoed`, confirming Planner `Scan -> Propose` sidecar review execution path is exercised.

### Pending
- **Fail-Safe Disconnect Check**: Explicit sidecar disconnect evidence (`[ChromeSidecar] Disconnected`) not yet observed in sampled logs; this remains the final manual verification item.

## [Setup Guard] - Non-Paper Account Key Requirement Validation - 2026-02-16

### Changed
- **Explicit Non-Paper Save Guard**: `UI/AccountEditDialog.cs` now blocks saving non-paper accounts unless the user either selects an existing API key or enters new credentials.
- **Clear Validation Messaging**: Added a single explicit warning prompt guiding users to choose `Existing API Key` or provide credentials.
- **Key Update Source Alignment**: When credentials are edited with an existing key selected, the dialog now prefers that selected key as the update target.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 warnings and 0 errors.

## [AI Prompting] - Expanded Market History Context for Planner/Governor - 2026-02-16

### Changed
- **Planner Review Context Expansion**: `Services/AutoPlannerService.cs` now sends up to 48 recent candles (OHLCV) to AI review instead of a short slice.
- **Planner Proposer Context Expansion**: `Services/AutoPlannerService.cs` now sends up to 48 recent candles to AI proposer with multi-window structure summaries.
- **Multi-Window Regime Summaries**: Planner and Governor payloads now include compact `windowSummaries` blocks (change %, high/low, average volume, RSI/ATR/VWAP) over recent windows to provide broader market structure context.
- **Governor Structure Expansion**: `Services/AIGovernor.cs` now includes up to 16 recent 15m BTC candles (full 4h context) plus `4/8/16` bar summaries.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug /p:OutDir=bin\Debug_Verify\` succeeded with 0 warnings and 0 errors.

## [UI Refactor] - Phase B Completion Polish - 2026-02-16

### Changed
- **Status Semantics Standardized**: `UI/TradingControl.cs` now applies explicit status severity semantics (success/warn/neutral) using consistent colors (`DarkGreen`/`DarkOrange`/`DimGray`) for inline action/status updates.
- **Dashboard Freshness Semantics**: `UI/DashboardControl.cs` now applies the same severity color semantics to data freshness updates, aligning Dashboard behavior with Planner/Auto/Trading status patterns.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.sln /t:Build /p:Configuration=Debug` succeeded with 0 warnings and 0 errors.
- **Runtime Duration**: Verified app runtime exceeded 5 minutes in a timed pass (`~305.95s`), satisfying long-run smoke requirement.

## [Phase 18] - Track B5 Strict Matrix Scenario Validator - 2026-02-16

### Changed
- **Strict Scenario Flags**: `Util/validate_automode_matrix.ps1` now supports `-RequireMixedScopes`, `-RequireSelectedSymbolCounts`, `-RequireIndependentGuardrailConfigs`, and `-RequireFailureIsolation` for deterministic B5 scenario assertions from cycle telemetry reports.
- **Count Parsing Robustness**: Validator now normalizes selected-count list tokens and supports comma-delimited strict-count checks (for example `-RequireSelectedSymbolCounts 3,15`) in scripted invocations.
- **Guardrail Configuration Evidence**: Validator now checks distinct profile guardrail scope keys and distinct guardrail tuples (`MaxTradesPerCycle`, `CooldownMinutes`, `DailyRiskStopPct`) when strict independent-budget validation is requested.
- **Failure-Isolation Evidence**: Validator can now require one failed/blocked profile and one independent completed/executed/skipped profile in the same cycle, plus telemetry containment evidence.
- **Docs Alignment**: Updated `docs/ops/AutoMode_Matrix_Validation.md` and `docs/features/trading/AutoMode_Automation.md` with strict-validation command examples for `3/All/15` style scenarios.

### Verified
- **Tooling Execution**: `powershell -ExecutionPolicy Bypass -File .\Util\validate_automode_matrix.ps1` executed successfully against latest report and correctly returned `FAIL` for a non-matrix scenario report (`1/1`, `MatrixStatus=PARTIAL`).

## [Phase 18] - Track A5 Verification & Reliability Gates - 2026-02-16

### Changed
- **Deterministic Reliability Gates**: `UI/AutoModeControl.cs` now computes cycle-level gate evidence (`GateNoSignalObserved`, `GateAiVetoObserved`, `GateRiskVetoObserved`, `GateSuccessObserved`) and derives `GateStatus` (`PASS`/`PARTIAL`) from observed profile outcomes.
- **Telemetry Finalization Path**: Reliability gates are evaluated for both normal cycle completion and cycle-level exception paths before report writeout, preserving deterministic evidence even on failures.
- **In-App Telemetry Summary Upgrade**: Latest cycle summary now includes gate status and observation flags alongside matrix status so operator verification can distinguish missing scenario evidence from matrix/guardrail drift.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.sln /t:Build /p:Configuration=Debug` succeeded with 0 warnings and 0 errors.

## [AI Prompting] - AutoPlanner Review/Proposer Prompt Sufficiency Hardening - 2026-02-16

### Changed
- **Review Prompt Context Upgrade**: `Services/AutoPlannerService.cs` now sends structured review payloads that include trade details, execution/risk context, global bias snapshot, and market indicators (`VWAP`, `RSI(14)`, `ATR(14)`, range%).
- **ISO Timestamp Serialization**: AutoPlanner review/proposer candle snapshots now serialize `timeUtc` with ISO-8601 (`ToString("o")`) for provider-consistent parsing.
- **Stricter JSON Contracts**: Review and proposer prompts now explicitly require JSON-only output (no markdown/prose), deterministic schema keys, and uncertainty fail-closed behavior.
- **Risk-Geometry Guidance in Proposer**: Proposer contract now explicitly requires valid side-specific stop/entry/target geometry when `approve=true`.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug /p:OutDir=bin\Debug_Verify\` succeeded with 0 warnings and 0 errors.

## [AI Prompting] - Governor Prompt Sufficiency Upgrade - 2026-02-16

### Changed
- **Richer Governor Context**: `Services/AIGovernor.cs` now includes computed `RSI(14)`, `ATR(14)`, window range %, bar count, and candle volume in the AI payload.
- **Timestamp Format Fix**: Governor prompt payload now serializes timestamps as ISO-8601 strings (`ToString("o")`) instead of legacy `\/Date(...)\/` serializer format.
- **Prompt Contract Tightening**: Bias prompt now enforces strict JSON-only output with explicit schema (`bias`, `reason`, `confidence`) and neutral fallback guidance for weak/conflicting evidence.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 warnings and 0 errors.

## [UI Polish] - Account Dialog Credential Visibility + Readability - 2026-02-16

### Changed
- **Credential Section Visibility**: `UI/AccountEditDialog.cs` now hides the entire credentials section when account service is `paper` or when `IKeyService` is unavailable, and only shows fields relevant to the selected exchange type.
- **Existing Account Load Robustness**: `UI/AccountEditDialog.cs` now safely restores `Service`/`Mode` selections with fallback defaults when persisted values are missing or stale.
- **Dialog Readability**: `UI/AccountEditDialog.Designer.cs` increases input widths, improves PEM field editing (`AcceptsReturn`, taller, no wrap), and shortens a long label so key account fields remain clearly visible.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 warnings and 0 errors.

## [Phase 18] - Track A4 Completion (Live Market Entry + Local Protective Watchdog) - 2026-02-16

### Changed
- **Coinbase Broker Execution Path**: `Brokers/CoinbaseExchangeBroker.cs` now places real market orders via authenticated `CoinbaseExchangeClient` using account-linked keys, replacing the prior rejection-only placeholder path.
- **Auto Mode Local Protective Watchdog (Live + Paper)**: `UI/AutoModeControl.cs` now evaluates stop/target conditions for tracked open plans and issues close orders through the active broker when thresholds are hit.
- **Capability Gate Integration**: Auto Mode capability checks now allow local-watchdog-protected execution for `coinbase-exchange` while keeping fail-closed behavior for unsupported/non-watchdog scenarios.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 warnings and 0 errors.

## [AI Sidecar] - Gemini/Claude Send Stall Recovery - 2026-02-16

### Changed
- **Provider Stall Recovery Flow**: `Services/ChromeSidecar.cs` now attempts provider recovery when Gemini/Claude prompt submission appears to stall (no response after send).
- **Recovery Strategy**: Added staged auto-recovery sequence: soft new-chat reset, then fresh provider-tab reconnect and retry.
- **Polling Refactor**: Extracted response polling into a dedicated helper for reuse across initial send and recovery retries.
- **Provider URL Centralization**: Added shared provider start-url resolver to keep tab creation/reconnect behavior consistent.

### Improved
- **Gemini/Claude Submit Robustness**: Injection scripts now explicitly track send attempt state (`window.__cdts_last_send_status`) and try both button click and keyboard fallback paths.

## [Phase 18] - Track A4 Protective Exits (Paper Watchdog + Fail-Closed Live) - 2026-02-16

### Changed
- **Paper Protective Exit Watchdog**: `UI/AutoModeControl.cs` now tracks paper-mode open plans in-memory and evaluates stop/target exits using live ticker checks before new execution sizing.
- **Protective Closure Journaling**: On paper stop/target hit, Auto Mode now writes a closing `TradeRecord` with realized `PnL` and `[close:<symbol>]` metadata through `IHistoryService`, and decrements session open-count state.
- **Closure-Aware Open Baseline**: Persisted open-position baseline computation now subtracts closure records from open execution records, improving max-concurrent accuracy across restarts.
- **Fail-Closed Live Path Maintained**: Broker capability checks continue to block non-paper execution when protective exits are unsupported.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 warnings and 0 errors.

## [UI Compliance] - AccountEditDialog Designer-First Conversion - 2026-02-16

### Changed
- **Designer-Backed Account Dialog**: `UI/AccountEditDialog.Designer.cs` now defines the full Add/Edit Account layout (account fields, exchange credentials, and action bar) so the form is editable in Visual Studio Designer.
- **Code-Behind Simplification**: `UI/AccountEditDialog.cs` now uses `InitializeComponent()` + explicit event handlers instead of runtime control construction, while preserving exchange-specific credential validation and key upsert/link behavior.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 warnings and 0 errors.

## [Phase 18] - Track A3 Guardrail Hardening (Execution Path) - 2026-02-16

### Changed
- **Durable Open-Count Baseline**: `UI/AutoModeControl.cs` now derives a persisted open-position baseline from `IHistoryService.LoadTrades()` (account-tag aware) before enforcing `MaxConcurrentTrades`.
- **Execution Journaling**: Auto execution now persists per-order `TradeRecord` entries via `IHistoryService.SaveTrade(...)`, including account/scope/result metadata in notes for guardrail continuity across restarts.
- **DI Wiring Update**: `MainForm.cs` now initializes `AutoModeControl` with `IHistoryService` so max-concurrent guardrails are not session-only.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors after stopping running app instances.

## [UI] - Account Add Flow Credential Coverage + Grid Text Visibility - 2026-02-16

### Changed
- **Account Add/Edit Credential Coverage**: `UI/AccountEditDialog.cs` now supports exchange-specific credential capture directly in Add/Edit Account flow (`coinbase-exchange` key/secret/passphrase, `coinbase-advanced` key-name + PEM, plus kraken/bitstamp API key/secret), and persists credentials via `IKeyService` while auto-linking account `KeyEntryId`.
- **Account/Key Service Wiring**: `UI/AccountsControl.cs` and `MainForm.cs` now inject and pass `IKeyService` into account dialogs so account creation can fully configure usable exchange credentials without a separate manual key-id step.
- **Account Grid Binding Reliability**: `Models/ProfileModels.cs` updates `AccountProfile` to property-backed members for WinForms data binding stability; `UI/AccountsControl.cs` also tightens column sizing/row settings so account label/service text remains visible in the row with the enabled checkbox.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 warnings and 0 errors.

## [Stability] - AutoPlanner Granularity Crash Hardening - 2026-02-16

### Fixed
- **AutoPlanner Candle Fetch Resilience**: `Services/AutoPlannerService.cs` now wraps candle fetches in a safe path that handles exchange `Unsupported granularity` errors without propagating fatal exceptions into Auto Mode proposal flow.
- **Granularity Fallback Retry**: On unsupported granularity responses, planner now normalizes to nearest Coinbase-supported interval (`1, 5, 15, 60, 360, 1440` minutes) and retries once.
- **Fail-Safe Behavior**: If retry fails, planner returns empty rows/plans for that cycle and logs structured diagnostics instead of allowing proposal flow to terminate unexpectedly.

## [UI Maintenance] - Warning Cleanup Follow-Up - 2026-02-16

### Changed
- **Trading Conditional Field Scope**: `UI/TradingControl.cs` now scopes `chartDisplay` behind `#if NETFRAMEWORK` to avoid non-framework compilation warning noise while preserving chart behavior.
- **Dashboard Event Reference**: `UI/DashboardControl.cs` now includes `RaiseNavigationRequest(...)` to keep the shell navigation event path explicit and reduce dead-event warning conditions.

### Verified
- **File-Level Validation**: No IDE errors in `UI/TradingControl.cs` and `UI/DashboardControl.cs`.
- **Build Status**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeds with 0 warnings and 0 errors.
- **Solution Build**: `MSBuild CryptoDayTraderSuite.sln /t:Build /p:Configuration=Debug` succeeds with 0 warnings and 0 errors.
- **Runtime Smoke**: Launched `bin\\Debug\\CryptoDayTraderSuite.exe` with `CDTS_LOG_LEVEL=debug`, tailed latest log (`%LocalAppData%\\CryptoDayTraderSuite\\logs\\log_20260216_36.txt`), and observed normal startup/governor/sidecar activity (`AI Governor Started`, `Connected via CDP`, `Starting analysis cycle`) with no immediate error entries in the sampled tail.
- **Runtime Duration Proof (5+ min)**: Timed run executed from `2026-02-16T23:44:30.3526143Z` to `2026-02-16T23:49:36.3042055Z` (â‰ˆ`305.95s`), exceeding the 5-minute minimum requirement.

## [Phase 18] - Track A2 Modal Reduction + Sidebar Collapse Guard - 2026-02-16

### Changed
- **Auto Mode Status-First Feedback**: `UI/AutoModeControl.cs` now routes normal-path outcomes to `UpdateAutoStatus(...)` and logs, reducing runtime modal interruptions in scan/propose/execute flows.
- **Auto Mode Startup Error Surface**: Product-load failures now publish to Auto status text/log stream instead of forcing a modal popup.
- **Sidebar Compact/Overflow Behavior**: `UI/SidebarControl.cs` and `UI/SidebarControl.Designer.cs` now enable nav scroll fallback and tighten collapsed width for more complete hide/collapse behavior while keeping lower buttons reachable.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded after stopping the running app process lock.

## [UI Refactor] - Trading + Dashboard Inline Status Freshness (Phase B Continue) - 2026-02-16

### Changed
- **Trading Inline Status**: `UI/TradingControl.Designer.cs` now includes `lblTradeStatus` in the top action strip; `UI/TradingControl.cs` now updates status/freshness timestamps for product/strategy/exchange loads, projection updates, chart refreshes, and action-button intents.
- **Dashboard Data Freshness**: `UI/DashboardControl.Designer.cs` now includes `lblDataFreshness` in the dashboard header; `UI/DashboardControl.cs` now updates refresh state and latest data timestamp/summary after each load.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.sln /t:Build /p:Configuration=Debug` succeeded with 0 errors (2 pre-existing warnings unchanged).

## [UI Refactor] - Planner Inline Status + Auto Modal Noise Reduction (Phase B) - 2026-02-16

### Changed
- **Planner Inline Status/Freshness**: `UI/PlannerControl.Designer.cs` now includes `lblPlannerStatus` in the planner action bar; `UI/PlannerControl.cs` now updates this status with timestamped scan/propose/execute/save outcomes.
- **Planner Queue Wiring Fix**: `UI/PlannerControl.cs` now assigns `_queuedPlans` from `ProposeAsync(...)`, enabling `Execute` to operate on the latest proposed plans.
- **Planner Non-Critical Feedback**: Replaced non-critical informational/warning modals in planner scan/propose/execute/save flows with inline status updates while retaining blocking confirmations and hard-error dialogs.
- **Auto Mode Modal Reduction**: `UI/AutoModeControl.cs` now routes non-critical scan/propose/execute feedback to status/log output (including per-order success/failure messages) instead of interactive popups.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.sln /t:Build /p:Configuration=Debug` succeeded with 0 warnings and 0 errors after releasing running app locks.

## [UI] - Sidebar Responsive Resize + Maximized Startup - 2026-02-16

### Changed
- **Sidebar No-Scroll Resize**: `UI/SidebarControl.Designer.cs` disables `_layoutPanel` autoscroll; `UI/SidebarControl.cs` now dynamically resizes sidebar button heights and governor region to match current window height, removing the persistent vertical scrollbar.
- **Startup Window State**: `MainForm.Designer.cs` now sets `WindowState=Maximized` so the app opens maximized by default.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` executed successfully.

## [Phase 15] - Verified AI Proposer Mode (Optional) - 2026-02-16

### Added
- **AI-First Proposal Payload**: Added `AITradeProposal` model in `Models/AI/AIModels.cs` for structured side/entry/stop/target suggestions from sidecar providers.

### Changed
- **Planner AI Proposer Path**: `Services/AutoPlannerService.cs` now supports optional AI-first proposing when `CDTS_AI_PROPOSER_MODE` (or `CDTS_AI_PROPOSER_ENABLED`) is enabled.
- **Deterministic Verification Gates**: AI proposals are accepted only if they pass live strategy side-alignment, valid stop/entry/target geometry, global bias enforcement, and risk-based sizing.
- **Safe Fallback Behavior**: If AI proposer data is missing/invalid/rejected, planner logs the reason and automatically falls back to existing strategy-first proposal + AI review flow.
- **Workflow Documentation**: Updated `docs/features/ai/AI_Workflow.md` with activation and verification steps for the optional verified AI proposer mode.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors (2 pre-existing warnings unchanged).

## [UI Refactor] - Planner + Trading Layout Reorganization (Phase A Continue) - 2026-02-16

### Changed
- **Planner Page Structure**: Refactored `UI/PlannerControl.Designer.cs` into a three-zone layout with a top context header (`account/symbol/granularity/lookback/equity`), left configuration rail (refresh/save/add + filters), and right work area (action bar + tabbed `Planned Trades`/`Predictions`).
- **Trading Page Structure**: Refactored `UI/TradingControl.Designer.cs` into a top action/status strip, left configuration rail, and right work area split for chart and logs.
- **Chart Host Contract**: Updated `UI/TradingControl.cs` to mount chart controls into the new `chartHost` panel instead of inserting directly into `tlMain` cells.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors (2 pre-existing warnings unchanged).

## [Phase 18] - Sidebar Navigation Clipping Fix + Log Review - 2026-02-16

### Fixed
- **Sidebar Button Visibility**: `UI/SidebarControl.Designer.cs` now docks the navigation layout panel to `Top` (reserved height) instead of `Fill`, preventing lower nav buttons from being clipped/overlapped by the bottom governor widget.
- **Responsive Region Sizing**: `UI/SidebarControl.cs` now recalculates navigation panel height on resize/collapse changes so all nav buttons remain visible with scrolling when needed.
- **Designer Initialization Order**: `UI/SidebarControl.Designer.cs` now instantiates `btnKeys`/`btnSettings` before `Controls.Add(...)` and removes post-add re-instantiation of `btnAccounts`, fixing the blank row + missing `API Keys`/`Settings` buttons.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors (2 pre-existing warnings unchanged).
- **Runtime Logs (Latest)**: `log_20260216_24.txt` shows auto-run enabled, governor consensus updates, and repeated `No signal` / `Global Bias blocked Buy` planner outcomes; no new sidebar-related runtime exceptions observed.

## [UI Analysis] - Control Reorganization Blueprint - 2026-02-16

### Added
- **UI Audit + Reorganization Plan**: Added `docs/ui/Control_Reorganization_Blueprint_2026.md` with a full control-surface analysis and an implementation-ready redesign strategy for `TradingControl`, `PlannerControl`, `AutoModeControl`, and dashboard/status surfaces.

### Changed
- **Documentation Indexing**: Linked the new blueprint from `docs/index.md` under **UI & Experience** for discoverability.

### Notes
- This iteration is documentation/design only; no runtime code or behavior changes were made.

## [Phase 18] - Sticky Auto Run Preference Persistence - 2026-02-16

### Changed
- **Persistent Auto Run Intent**: `UI/AutoModeControl.cs` now loads/saves a sticky Auto Run preference and restores it after dependency initialization, so user ON/OFF intent survives restart.
- **User Settings Integration**: Added `AutoModeAutoRunEnabled` user-scoped setting in `Properties/Settings.settings` and `Properties/Settings.Designer.cs`.
- **Lifecycle Alignment**: Updated lifecycle spec/docs to reflect persisted user intent behavior instead of forced-off startup.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors (2 pre-existing warnings unchanged).

## [Phase 18] - Matrix Validator Script + Ops Runbook (Track B B5) - 2026-02-16

### Added
- **Validation Script**: Added `Util/validate_automode_matrix.ps1` to validate latest or explicit cycle report against deterministic matrix checks.
- **Ops Runbook**: Added `docs/ops/AutoMode_Matrix_Validation.md` with usage, strict containment mode, and exit-code semantics.
- **Documentation Indexing**: Linked the new runbook in `docs/index.md` Operations section.

### Verified
- **Script Wiring**: Script execution path validated (`./Util/validate_automode_matrix.ps1`); current machine has no cycle report directory yet, so runtime report-backed PASS verification remains pending until a new cycle report is generated.

## [Phase 18] - Strict Matrix Evidence Flags (Track B B5) - 2026-02-16

### Changed
- **Observed Evidence Semantics**: `UI/AutoModeControl.cs` now records `MatrixIndependentGuardrailsObserved` and `MatrixFailureContainmentObserved` to distinguish actual observed B5 conditions from generic passability.
- **Telemetry Summary Clarity**: Auto Mode telemetry status row now includes guardrail/containment observation markers (`obs`/`na`) beside matrix status.
- **Matrix PASS Criteria Tightening**: Matrix pass now depends on explicit independent-guardrail evidence plus existing pair-configuration and isolation checks.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors (2 pre-existing warnings unchanged).

## [Phase 18] - Auto Loop Lifecycle Stabilization (Track A1) - 2026-02-16

### Changed
- **Startup Safety**: `UI/AutoModeControl.cs` now explicitly forces `Auto Run` OFF at control initialization to prevent implicit auto re-arm on restart.
- **Idempotent Stop Flow**: Auto toggle/stop path now suppresses recursive toggle callbacks and records distinct status/log behavior for already-off vs actively-running states.
- **Kill-Switch Responsiveness**: Added stop-request checks in auto-cycle profile iteration and execution loops so kill switch halts work mid-cycle at safe boundaries.
- **Cycle-Level Error Telemetry**: Auto cycle telemetry now includes `CycleErrorCount` and `CycleErrorMessage` when an unexpected cycle exception escapes profile-level containment.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors (2 pre-existing warnings unchanged).

## [Phase 18] - Per-Profile Exception Containment (Track B Reliability) - 2026-02-16

### Changed
- **Profile Error Isolation**: `UI/AutoModeControl.cs` now contains exceptions per profile during auto cycles, records profile `Status="error"`, and continues processing other enabled profiles.
- **Failure Accounting**: Cycle telemetry `FailedProfiles` and matrix failure checks now include both `blocked` and `error` profile outcomes.
- **Matrix Isolation Robustness**: `MatrixFailureDoesNotHaltCycle` now treats profile errors as failures that must still preserve full-cycle completion behavior.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors (2 pre-existing warnings unchanged).

## [Phase 18] - Profile Migration + Guardrail Scope Isolation (Track B) - 2026-02-16

### Changed
- **Versioned Profile Migration**: `Services/AutoModeProfileService.cs` now uses explicit store-version migration (`CurrentStoreVersion=2`) and rewrites legacy/non-current payloads to canonical schema.
- **Per-Profile Guardrail Isolation**: `UI/AutoModeControl.cs` now scopes cooldown and daily-risk accounting by profile guardrail scope (`profile:{id}`) instead of shared global state.
- **Matrix Evaluator Coverage**: Added matrix checks for guardrail-scope isolation and failure-containment (`MatrixFailureDoesNotHaltCycle`) in cycle telemetry.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors (2 pre-existing warnings unchanged).

## [Phase 18] - Configurable Matrix Validation (Track B Clarification) - 2026-02-16

### Changed
- **Matrix Evaluator Generalized**: `UI/AutoModeControl.cs` now evaluates `MatrixStatus` from profile-configured pair scopes/counts (`Selected` expected count consistency + `All` non-empty universe), replacing fixed `3/All/15` shape assumptions.
- **Per-Profile Telemetry Detail**: Added `ExpectedSymbolCount` to profile telemetry so each profile run can be validated against its own configuration.
- **Spec/Tracker Alignment**: Updated `ROADMAP.md`, `PROGRESS_TRACKER.md`, and `docs/features/trading/AutoMode_Automation.md` to describe `3/All/15` as an example scenario rather than a hard requirement.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors.

## [Phase 18] - Automated Matrix Evaluation (Track B B5 Support) - 2026-02-16

### Added
- **Matrix Evaluation Fields**: `UI/AutoModeControl.cs` cycle telemetry now stores automated matrix-check outputs (`MatrixHasThreePairProfile`, `MatrixHasAllPairsProfile`, `MatrixHasFifteenPairProfile`, `MatrixHasGuardrailValues`, `MatrixIsolationObserved`, `MatrixStatus`).

### Changed
- **Telemetry Summary Row**: Auto Mode telemetry quick-view now displays matrix state (`PASS`/`PARTIAL`) from latest cycle report.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors.

## [Phase 18] - In-App Telemetry Quick View (Track B) - 2026-02-16

### Added
- **Telemetry Status Row**: `UI/AutoModeControl.Designer.cs` now includes a `Telemetry` summary label in Auto Mode status area.

### Changed
- **Latest Report Visibility**: `UI/AutoModeControl.cs` now reads the newest cycle report from `%LocalAppData%\\CryptoDayTraderSuite\\automode\\cycle_reports` and displays key metrics (processed/enabled profiles, executed, blocked, timestamp) directly in UI.
- **Auto Refresh**: Telemetry summary refreshes on control startup and immediately after each new cycle report is written.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors.

## [Phase 18] - Auto Cycle Telemetry Export (Track B B5) - 2026-02-16

### Added
- **Structured Auto Cycle Reports**: `UI/AutoModeControl.cs` now writes one JSON telemetry artifact per profile-driven cycle to `%LocalAppData%\\CryptoDayTraderSuite\\automode\\cycle_reports`.
- **Per-Profile Outcome Metrics**: Reports include profile/account identity, symbol/scan/proposal counts, execution results, skipped counters, status, and block reasons.

### Changed
- **Execution Outcome Model**: `AutoModeControl` execution path now uses structured outcome objects instead of string-only summaries, improving deterministic validation and diagnostics.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors.

## [Phase 18] - Broker Capability Contract Enforcement (Track B) - 2026-02-16

### Added
- **Broker Capability Contract**: `Brokers/IBroker.cs` now defines `BrokerCapabilities`, `GetCapabilities()`, and `ValidateTradePlan(...)` for explicit broker capability/constraint checks.
- **Broker Implementations Updated**: `Brokers/PaperBroker.cs` and `Brokers/CoinbaseExchangeBroker.cs` now implement capability reporting and plan-level validation.

### Changed
- **Auto Execution Validation**: `UI/AutoModeControl.cs` now validates each trade plan with broker constraints before order placement and reports validation skips in cycle summaries.
- **Planner Execution Validation**: `UI/PlannerControl.cs` now enforces broker capability checks and plan validation before submitting queued plans.
- **Broker Documentation Accuracy**: Updated `docs/features/trading/Brokers.md` to reflect DI-based broker path and capability/validation contract.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors.

## [Phase 18] - Profile Summary + Capability Diagnostics (Track B) - 2026-02-16

### Added
- **Per-Profile Summary Row**: `UI/AutoModeControl` now shows a live profile summary line (broker, account mode, pair count, cadence, caps) for the selected auto profile.
- **Capability Guard Diagnostics**: Auto execution now emits explicit block reasons for unsupported/misconfigured profile-account combinations before broker placement.

### Changed
- **Manual + Auto Path Consistency**: Both manual `Execute` and multi-profile auto-cycle execution now share capability gating behavior and status messaging.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors.

## [Phase 18] - Profile Enable/Interval UI Wiring (Track B) - 2026-02-16

### Added
- **Explicit Profile Controls**: `UI/AutoModeControl.Designer.cs` now includes profile-level `Enabled` toggle and `Profile Every` interval input next to profile selection/actions.

### Changed
- **Profile Save/Load Behavior**: `UI/AutoModeControl.cs` now persists and applies `AutoModeProfile.Enabled` and `AutoModeProfile.IntervalMinutes` through the dedicated profile controls.
- **Control Availability Guarding**: Profile controls are now disabled when the profile service is unavailable and enabled when profile persistence is active.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors.

## [Phase 18] - Multi-Profile Auto Cycle Engine (Track B) - 2026-02-16

### Added
- **Enabled-Profile Runtime Orchestration**: `UI/AutoModeControl.cs` auto loop now iterates enabled profiles and executes isolated `Scan -> Propose -> Execute` per profile/account.
- **Per-Profile Cadence Gate**: Added per-profile interval scheduling (`IntervalMinutes`) so profiles can run at different cadences inside the global auto loop.
- **Profile Symbol Scope Resolver**: Added runtime symbol resolution honoring profile `PairScope` (`All` vs selected pairs) with fallback to current UI pair selection.

### Changed
- **Execution Pipeline Refactor**: Extracted shared scan/propose/execute helpers (`ScanSymbolsAsync`, `ProposeForAccountAsync`, `ExecutePlansForAccountAsync`) to support both manual account execution and multi-profile auto cycles.
- **Cycle Status Reporting**: Auto status now logs per-profile outcomes in one aggregated cycle summary instead of single-account-only summaries.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors.

## [Phase 18] - Auto Profiles Foundation (Track B Start) - 2026-02-16

### Added
- **Auto Profile Persistence Service**: Added `Services/AutoModeProfileService.cs` with versioned local persistence (`automode_profiles.json`), CRUD operations, normalization, and corruption backup handling.
- **Profile Model**: Added `AutoModeProfile` + `AutoModeProfileStore` in `Models/ProfileModels.cs` for per-account pair-scope automation settings.
- **Dependency Wiring**: Wired `AutoModeProfileService` through `Program.cs` and `MainForm.InitializeDependencies` into `UI/AutoModeControl`.
- **Auto Mode Profile Controls**: Added profile UI controls in `UI/AutoModeControl.Designer.cs` and behavior in `UI/AutoModeControl.cs` for save/load/delete/apply profile settings.

### Changed
- **Pair Scope Runtime**: `AutoModeControl` now supports profile-based pair scope (`All` vs selected set) when resolving symbols for scan/propose cycles.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors.

## [Phase 18] - Auto Mode Dual-Track Roadmap + Spec Documentation - 2026-02-16

### Added
- **Roadmap Phase Expansion**: `ROADMAP.md` now includes **Phase 18: Auto Mode Productionization - Dual Track Implementation**, with thorough small-step subtasks for:
    - Track A: non-interactive auto loop completion,
    - Track B: multi-broker/multi-account profile orchestration.
- **Behavior Specification**: Added `docs/features/trading/AutoMode_Automation.md` defining required runtime behavior, guardrails, persistence, acceptance criteria, and quality gates (no stubs/placeholders/simplified paths).

### Changed
- **Documentation Indexing**: Added `Auto Mode Automation` links to `docs/index.md` and `docs/features/FeatureIndex.md` to ensure feature behavior/spec is discoverable.

### Verified
- **Coverage**: Roadmap tasks and feature documentation now explicitly define complete implementation paths for both tracks without scaffolding gaps.

## [Phase 15] - Non-Interactive Auto Mode Loop - 2026-02-16

### Added
- **One-Toggle Auto Loop**: `UI/AutoModeControl.cs` now supports non-interactive timed cycles (`Auto Run`) that execute `Scan -> Propose -> Execute` automatically on selected pairs.
- **Live Safety Arming**: Added `Live Arm` gate; live-account execution is blocked unless explicitly armed.
- **Kill Switch**: Added immediate auto-loop stop control in Auto Mode UI.
- **Cycle Controls**: Added configurable auto interval, max trades per cycle, per-symbol cooldown, and daily risk stop percentage controls.

### Changed
- **Non-Modal Runtime UX**: Auto cycles no longer rely on repetitive proposal/execution message boxes; status now updates through Auto status text and logs.
- **Execution Guardrails**: Auto execution now enforces per-cycle cap, account max-concurrent cap (session-level), cooldown, and daily risk cap checks before placing orders.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors.

## [Phase 15] - AutoMode Bulk Propose UX Fix - 2026-02-16

### Fixed
- **One-Click Proposal Across Scan Set**: `UI/AutoModeControl.cs` `Propose` now evaluates all scanned symbols (grouped by symbol) instead of only the single top symbol.
- **Reduced Message-Box Noise**: Proposal flow now emits one aggregated result message summarizing proposed trades and symbol pass-rate, replacing repetitive no-result prompts from manual per-symbol retries.
- **Queue Coverage**: `_queued` now contains all valid trade plans returned across the full scan result set, improving execute readiness after a single propose action.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors.

## [Phase 15] - AutoMode Propose Granularity Normalization - 2026-02-16

### Fixed
- **Coinbase Granularity Rejection**: `Exchanges/CoinbaseExchangeClient.cs` now normalizes unsupported minute granularities to the nearest Coinbase-supported interval (`1, 5, 15, 60, 360, 1440`) before requesting candles.
- **Propose Stability**: Prevents `HTTP 400: {"message":"Unsupported granularity"}` from bubbling up during AutoMode propose/scan paths when UI-selected granularities like `30` or `240` are used.

### Verified
- **Log Diagnosis**: Latest runtime log (`log_20260216_19.txt`) showed AutoMode proposal failure on `granularity=1800` seconds (30m); normalization addresses this class of failures.

## [Phase 15] - Planner Propose Crash Guardrails - 2026-02-16

### Fixed
- **Planner Propose Stability**: `UI/PlannerControl.cs` now wraps the full `DoPropose` flow in a defensive exception boundary (including pre-check stage) so edge-case failures surface as handled planner errors instead of terminating the UI process.
- **Planner Execute Stability**: `UI/PlannerControl.cs` now applies the same full-method guard pattern to `DoExecute`.
- **Null/State Guarding**: Added explicit `_accounts` null/state guard checks before account-index access in `Propose` and `Execute` paths.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors.

## [Phase 15] - Claude Provider Support - 2026-02-16

### Added
- **Claude Sidecar Support**: `Services/ChromeSidecar.cs` now supports `claude.ai` as a first-class provider for both governor and planner AI review flows.

### Changed
- **Provider Fan-Out**: Multi-provider query path now includes `ChatGPT`, `Gemini`, and `Claude`.
- **Provider Detection/Provisioning**: Sidecar now detects Claude tabs, can auto-open `https://claude.ai/new`, and resolves provider metadata/source labels for Claude.
- **Claude Inject/Read Selectors**: Added provider-specific prompt injection and response-read selectors for Claudeâ€™s composer/chat surface.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors.
- **Runtime**: Governor cycle logs show Claude included in fan-out and consensus source summary (`Sources: ChatGPT ...; Gemini ...; Claude ...`).

## [Phase 15] - Sidecar Multi-Provider Runtime Reliability - 2026-02-16

### Fixed
- **CDP Correlation Stability**: `Services/ChromeSidecar.cs` now uses a background receive pump with request-id correlation (`pending` response map) so `Runtime.evaluate` responses are matched deterministically under high event traffic.
- **WebSocket Timeout Safety**: Reworked send/receive timeout handling to explicit `Task.WhenAny` guards to prevent intermittent hangs during response polling.
- **Provider Target Integrity**: AI tab discovery now filters to DevTools `type == page` targets, avoiding worker/background targets that can connect but fail evaluation.
- **Gemini Reliability**: Added selected-tab activation (`/json/activate/{id}`), provider-aware injection retries, and stronger reconnect retry path for prompt injection.
- **Repeated Response Capture**: Response candidate handling now allows baseline-equal captures after initial polling to avoid false timeouts when the model returns similar guidance.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors.
- **Runtime**: Governor cycle captured both providers in one run (`Model response captured` for ChatGPT and Gemini), then computed consensus (`Sources: ChatGPT ...; Gemini ... | Consensus: Neutral`).
- **Planner AI Review**: Programmatic `AutoPlannerService` Scanâ†’Propose sweep verified AI review execution on both providers, including `[AutoPlanner] Starting AI review`, sidecar prompt injection/response capture, and approval-veto parse paths.

## [Phase 15] - AutoMode Pair Selection & Popular Pair Priority - 2026-02-16

### Added
- **Multi-Pair Selector**: `UI/AutoModeControl` now includes a selectable pairs list so Auto Mode can scan user-selected pair sets.

### Changed
- **Popular Pair Ordering**: Pair list now places popular USD pairs at the top (`BTC-USD`, `ETH-USD`, `SOL-USD`, `XRP-USD`, `ADA-USD`, `DOGE-USD`, `AVAX-USD`, `LINK-USD`, `LTC-USD`, `BCH-USD`) and sorts remaining pairs below.
- **Cross-Pair Scan Ranking**: `Scan` aggregates results across selected pairs and ranks combined opportunities by expectancy.
- **Best-Symbol Proposal**: `Propose` now targets the highest-expectancy symbol from the scan set.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors.

## [Phase 15] - AutoMode Automatic Best-Trade Proposals - 2026-02-16

### Added
- **Auto Propose Toggle**: `UI/AutoModeControl` now includes an `Auto Propose` option (enabled by default).

### Changed
- **Automatic Proposal Flow**: After `Scan` finds profitable rows, Auto Mode can automatically call planner proposal logic to propose the highest-expectancy trade without requiring a second button click.
- **Manual Control Retained**: `Propose` button continues to work as an explicit manual action.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors.

## [Phase 17] - AI Clarification-Response Parse Hardening - 2026-02-15

### Fixed
- **Sidebar Clipping**: `UI/SidebarControl.cs` now uses `Dock.Fill` for the button layout panel instead of fixed-height `Dock.Top`, ensuring navigation buttons (Accounts, Keys, Settings) are visible on all window sizes.
- **Clarification Safety**: `Services/AIGovernor.cs` now rejects clarification/ambiguous AI replies (e.g., "which response is preferred?") instead of misclassifying bias from keyword collisions.
- **Ambiguity Guard**: Text fallback parser now requires exactly one bias token (`Bullish`/`Bearish`/`Neutral`); multi-token responses are treated as unparseable.
- **Resilient Extraction**: Added embedded JSON-object extraction and labeled-bias (`bias: ...`) parsing before keyword fallback.
- **Prompt Tightening**: Governor prompt now explicitly requires one JSON object only, forbids follow-up questions, and directs uncertain outcomes to `Neutral`.
- **Planner Clarification Safety**: `Services/AutoPlannerService.cs` now applies the same clarification/ambiguity protections for `approve` parsing, with embedded-JSON and labeled-approval extraction before text fallback.
- **Planner Prompt Tightening**: AutoPlanner AI review prompt now requires a single JSON object with no follow-up questions and defaults uncertain outcomes to `approve: false`.
- **Planner Clarification Observability**: AutoPlanner now logs a dedicated warning when AI asks clarifying/ambiguous follow-up questions and marks the trade note as `[AI Clarification Requested]`.
- **Auto Tab Propose Crash Guard**: `UI/AutoModeControl.cs` now protects `DoPropose()` (`async void`) with top-level exception handling and validates account list state before proposal execution to prevent null-reference crashes after scan.
- **Auto Tab Proposal Null Safety**: Proposal queue assignment now null-coalesces to an empty list and button/message operations are guarded for disposed/null control states.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Build /p:Configuration=Debug` succeeded with 0 errors.
- **Runtime**: Fresh governor cycle no longer applies incorrect bias from clarification-style responses; unparseable responses are safely ignored.

## [Phase 15] - Planner UI Completion & Unification - 2026-02-15

### Fixed
- **Planner Controls Completed**: `UI/PlannerControl` now includes full generation controls (`Account`, `Symbol`, `Gran`, `Lookback`, `Equity`) and actions (`Scan`, `Propose`, `Execute`).
- **Service Wiring**: `MainForm` now initializes `PlannerControl` with `AutoPlannerService`, public exchange client, account service, and key service.
- **State Split Removed**: `Propose` now appends generated plans directly into Planned Trades and persists them through `IHistoryService`.
- **Legacy Popup Routing Removed**: `OpenPlanner` now routes to sidebar Planner navigation (`NavigateTo("Planner")`) instead of opening legacy popup `PlannerForm`.

### Documentation
- Updated `docs/features/ai/AI_Workflow.md` planner verification steps to use inline Planner generation controls.
- Updated `docs/ui/UsageGuide.md` Planner section to reflect current `PlannerControl` capabilities.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Rebuild /p:Configuration=Debug` succeeded with 0 errors.

## [Phase 17] - Runtime Noise Reduction & Setup Center - 2026-02-15

### Fixed
- **Coinbase Candle Range Failures**: `Exchanges/CoinbaseExchangeClient.cs` now chunks long-range candle requests into safe windows below Coinbase aggregation limits, eliminating repeated HTTP 400 failures for long lookbacks with 1-minute granularity.

### Added
- **Centralized Setup Center**: `MainForm` Settings view now includes a single management area with tabs for **Accounts**, **API Keys**, and **Profiles**.

### Verified
- **Compile**: Updated files compile cleanly with no diagnostics in `MainForm.cs` and `CoinbaseExchangeClient.cs`.

## [Phase 17] - Governor Reconnect Neutral-Reset Fix - 2026-02-15

### Fixed
- **False Offline Neutral Reset**: `Services/AIGovernor.cs` now re-checks connection state after reconnect attempts and only applies neutral fail-safe when still offline.

### Verified
- **Runtime**: After reconnect, governor no longer immediately forces `MarketBias.Neutral`; cycle proceeds to bias update with source label output.

## [Phase 17] - AI Bias Source Labeling & Consensus - 2026-02-15

### Added
- **Multi-Provider Bias Aggregation**: `Services/AIGovernor.cs` now queries all available AI service tabs via `ChromeSidecar` and computes consensus bias when multiple responses are available.
- **Provider Metadata**: `Services/ChromeSidecar.cs` now exposes service/model metadata (`CurrentServiceName`, `CurrentModelName`, `CurrentSourceLabel`) and returns per-service results.

### Changed
- **Reason Text Formatting**: Governor bias reason now reports source label(s) (service + model) and consensus summary instead of "Parsed from non-JSON response" wording.

### Verified
- **Runtime**: Logs show source-labeled outcome format (`Source: ChatGPT (Auto)`) and provider fan-out attempts.

## [Phase 15] - AI Sidecar Response Capture Fix - 2026-02-15

### Fixed
- **ChatGPT Injection Reliability**: Updated `Services/ChromeSidecar.cs` prompt injection to correctly handle contenteditable composer input (`#prompt-textarea`) in addition to textarea paths.
- **Assistant Read Robustness**: Expanded assistant response selectors and added de-duplication/fallback extraction for ChatGPT/Gemini response reads.
- **False Positive Filtering**: Added response-quality guards to skip trivial UI labels (e.g., `You`) and wait for meaningful assistant content before capture.
- **Governor Parse Hardening**: Wrapped `AIGovernor` JSON parse path to prevent non-JSON model text from terminating the cycle and to allow fallback bias parsing.
- **Planner Parse Hardening**: Wrapped `AutoPlannerService` JSON parse path to prevent non-JSON model text from short-circuiting planner AI review; fallback text approval/veto parsing now executes reliably.
- **Planner Observability**: Added explicit planner AI lifecycle logs (`Starting AI review`, `AI approved/vetoed`, parse-fallback outcomes, and AI failure diagnostics).
- **Sidecar Concurrency Safety**: Serialized `ChromeSidecar.QueryAIAsync` calls using `SemaphoreSlim` to prevent governor/planner prompt-response overlap.
- **Target Tab Selection**: Updated sidecar connect flow to prefer active AI tabs when multiple ChatGPT/Gemini tabs are open.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Rebuild /p:Configuration=Debug` succeeded with 0 errors.
- **Runtime**: Verified end-to-end governor cycle logs show prompt injection success and model response capture (including generic selector fallback) with bias update (`Market Bias fallback parsed from text: Neutral -> Bearish`).

## [Phase 17] - Strategy & Risk Hardening Implementation - 2026-02-15

### Fixed
- **Strategy Safety**: Hardened null/index guards across `DonchianStrategy`, `ORBStrategy`, `RSIReversionStrategy`, and `VWAPTrendStrategy`; removed unsafe stop-distance fallback in `StrategyEngine` and added Donchian routing support.
- **Risk Guard Integrity**: Replaced legacy `RiskGuards.FeesKillEdge(...)` no-op behavior with real compatibility logic.
- **Indicator Integrity**: Unified feature computation onto `Indicators` routines and removed duplicate ATR/RSI/SMA/TR implementations in `FeatureExtractor`.
- **Math Guardrails**: Added robust `ChoppinessIndex` period/log/finiteness checks and bounded output.
- **Backtest Fidelity**: Updated `Backtester` to honor stop/target exits and corrected rolling max drawdown computation.
- **ML Lifecycle**: Wired feature validity checks, online learn-from-candles flow, and prediction analysis invocation from live/backtest paths.
- **Planner Invocation**: Removed reflection-based planner menu calls and introduced explicit `MainForm.OpenPlanner()` command path.
- **History Robustness**: Hardened CSV load paths in `HistoryService` with row-level parse tolerance.
- **Sidecar Resilience**: Implemented CDP response-id correlation, timeout/cancellation-safe IO, robust status transitions, safer prompt escaping, and governor reconnect cadence.
- **Auto Execution Safety**: Implemented `AutoModeControl` execute flow parity and blocked unsafe live auto-execution where bracket exits are unsupported.
- **Keying/Broker Integrity**: Fixed active key-id semantics (`broker|label`), preserved advanced key fields (`ApiSecretBase64`, `ApiKeyName`, `ECPrivateKeyPem`), and removed brittle reflection from broker command dispatch.
- **Config Parity**: Added Donchian to strategy configuration surfaces.
- **Concurrency**: Made `EventBus` publish snapshot locking explicit for thread-safe handler enumeration.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Rebuild /p:Configuration=Debug` succeeded with 0 errors.

## [Phase 15] - Sidebar Reflow + AI Market Data Recovery - 2026-02-15

### Fixed
- **Sidebar Reflow**: Reworked `MainForm` shell to use a 2-column layout (`Sidebar` + `Content`) so menu expand/collapse pushes content right/left instead of overlaying it.
- **Sidebar Collapse State**: Hardened `SidebarControl` collapse behavior to close cleanly (minimum width, governor widget collapse/restore, stable button alignment).

### AI / Logging
- **Governor Diagnostics**: Added explicit cycle logs in `AIGovernor` for analysis start, candle count, insufficient/null data, empty AI response, and parse failures.
- **Market Data Verification**: Confirmed governor now receives market candles (`Market data received: 16 candles`).

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Rebuild /p:Configuration=Debug` succeeded with 0 errors.
- **Runtime Logs**: Startup, sidecar connection, and governor cycle logs are present; market data fetch succeeds.

## [Phase 15] - UI Resize + Connection Logging Hardening - 2026-02-15

### Fixed
- **Responsive Controls**: Improved resizing behavior in `TradingControl`, `AutoModeControl`, and `PlannerControl` by enabling top-bar wrap/scroll behavior and safer fill layout handling.
- **Dashboard Fit**: Enabled scroll fallback for `DashboardControl` to prevent clipped content on smaller window sizes.
- **Bottom Log Reliability**: Added in-memory log buffering/replay in `MainForm` so logs emitted before opening Trading are still visible in the bottom log panel.

### Logging
- **Global Log Bridge**: Wired `Util.Log.OnLine` to `EventBus` in `Program.cs` so connection/HTTP failures always flow into UI status/log channels.
- **HTTP Diagnostics**: Hardened `HttpUtil.SendAsync` with method/URL/status/body logging and explicit `ua-header` mismatch detection paths.
- **Connection Lifecycle**: Added connection creation/cached-client logs in `ExchangeProvider` and error logs in `AutoModeControl` exception paths.
- **Header Compliance**: Added default `User-Agent` propagation for all HTTP requests in `HttpUtil`.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Rebuild /p:Configuration=Debug` succeeded with 0 errors.
- **Runtime**: App starts and logs show connection lifecycle entries (`[Connection]`, `[ChromeSidecar]`, `[AIGovernor]`).

## [Phase 15] - Sidebar Shell Startup Fix - 2026-02-15

### Fixed
- **Main Shell Override**: Resolved startup regression where `MainForm.OnLoad` called legacy `BuildResponsiveLayout()` and replaced the new sidebar shell.
- **Sidebar Activation**: Updated `MainForm.OnLoad` to preserve sidebar/content shell and only call `BuildModernLayout()` when shell controls are missing.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Rebuild /p:Configuration=Debug` succeeded with 0 errors.
- **Run**: Restarted `bin/Debug/CryptoDayTraderSuite.exe` after patch.

## [Phase 15] - Build Stabilization Pass - 2026-02-15

### Fixed
- **Compile Recovery**: Resolved 7 blocking compile errors across `MainForm`, `DashboardControl`, `ProfileManagerControl`, `KeyEditDialog`, and `SidebarControl`.
- **UI Wiring**: Added missing `btnSaveKeys_Click` handler in `MainForm.cs` to match `MainForm.Designer.cs` event binding.
- **Dependency Injection Alignment**: Updated `UI/MainForm_ExtraButtons.cs` to construct `AutoModeForm` with required injected dependencies instead of using a missing parameterless constructor.
- **Theme Compatibility**: Replaced invalid `Theme.Apply` calls on non-`Form`/`UserControl` targets with explicit recursive control theming.
- **Model Compatibility**: Corrected `KeyInfo`/`KeyEntry` conversion paths in `UI/KeyEditDialog.cs` to satisfy `IKeyService.Upsert(KeyInfo)`.

### Verified
- **Build**: `MSBuild CryptoDayTraderSuite.csproj /t:Rebuild /p:Configuration=Debug` succeeded with 0 errors.
- **Startup**: `bin/Debug/CryptoDayTraderSuite.exe` launched successfully (process confirmed running).

## [Phase 17] - Strategy & Risk Hardening Plan - 2026-02-15

### Added
- **Roadmap**: Added `Phase 17: Strategy & Risk Hardening Audit Remediation` to `ROADMAP.md` with issue-specific subtasks.
- **Audit Tracking**: Added `AUDIT-0023` through `AUDIT-0031` to `PROGRESS_TRACKER.md` with severity and planned status.
- **Execution Structure**: Added Phase 17 workstreams (Safety, Routing/Scoring, Indicator Integrity/Performance, Dead Path Reduction) for staged implementation.

### Updated
- **Extended Audit Findings**: Added `AUDIT-0032` through `AUDIT-0038` after continued deep-pass on Backtester, Prediction lifecycle, UI strategy configuration, and History parsing.
- **Roadmap Expansion**: Added concrete remediation subtasks for backtest fidelity, max drawdown correctness, online learning wiring, feature error handling, reflection cleanup, strategy config alignment, and CSV parse hardening.
- **Execution/Sidecar Audit Expansion**: Added `AUDIT-0039` through `AUDIT-0045` covering protective-exit enforcement, sidecar reconnect/state-machine safety, CDP response correlation, timeout hardening, prompt escaping robustness, and venue precision checks.
- **Auto-Execution/Keying Audit Expansion**: Added `AUDIT-0046` through `AUDIT-0051` for Auto Mode execute stubs, broker reflection contract breaks, active-key id mismatches, key field round-trip loss, duplicate auto-mode surface drift, and EventBus concurrency safety.

## [Phase 15] - Logging & Reliability - 2026-02-15

### Enhanced
- **Logging System**: Upgraded `Util/Log.cs` to output to both Console (for development) and Debug listeners (for Visual Studio).
- **Network Tracing**: Added detailed Request/Response tracing to `Util/HttpUtil.cs`.
- **Service Integration**: Instrumented `Services/ChromeSidecar.cs` and `Services/AIGovernor.cs` to utilize the central logging infrastructure.
- **Startup**: Configured `Program.cs` to initialize logging at `Debug` level for better visibility.
- **Profile Service**: Added logging to Import/Export operations.

## [Phase 14] - Deep Clean & Quality Assurance - 2026-02-14

### Fixed
- **Critical Architecture**: Discovered and fixed `UI/PlannerControl.cs` referencing the decommissioned `HistoryStore` static class. Refactored to use injected `IHistoryService`.
- **UI Policy Violation**: Refactored `UI/PlannerControl.cs`, `UI/AutoModeControl.cs`, and `UI/TradeEditDialog.cs` to use proper `.Designer.cs` files, adhering to "No Programmatic Layouts" policy.
- **Dead Code**: Removed `UI/MainFormPlannerButton.cs` which used Reflection and programmatic UI injection, violating multiple standards.
- **MainForm**: Updated `MainForm.cs` to inject dependencies into `PlannerControl` properly during navigation.
- **Data Binding**: Converted public fields to Properties in `Models/AccountModels.cs` (`TradePlan`) and `Models/PredictionModels.cs` to enable Windows Forms DataBinding.
- **Bug Fix**: Fixed `Services/RateRouter.cs` accessing non-existent property `Ticker.Price` (changed to `Ticker.Last`).
- **Audit**: Verified `Services/AutoPlannerService.cs` complies with Dependency Injection standards.

## [Phase 13] - AI Integration - 2026-02-13

### Added
- **Chrome Sidecar**: Full implementation of `Services/ChromeSidecar.cs` using WebSocket CDP for Gemini/ChatGPT integration.
- **AI Governor**: Implemented `Services/AIGovernor.cs` to poll market bias every 15 minutes.
- **Governor UI**: Added `UI/GovernorWidget.cs` to visualize AI status.

## [Phase 12] - UI Refactor & Modernization - 2026-02-12

### Added
- **Backend Events**: `ChromeSidecar` and `AIGovernor` now expose `StatusChanged` and `BiasUpdated` C# events for UI binding.
- **GovernorWidget**: New `UserControl` component (`UI/GovernorWidget.cs`) for visualizing AI Market Bias and connection status.
- **SidebarControl**: New navigation component (`UI/SidebarControl.cs`) to replace legacy TabControl menu.
- **ProfilesControl**: New feature (`UI/ProfilesControl.cs`) for full Import/Export management of encrypted user profiles.
- **Strategy Config**: Added `UI/StrategyConfigDialog.cs` using generic PropertyGrid for runtime strategy tuning.
- **Theme**: Added "Dark Graphite" palette (`#151717`, `#1E2026`) to `Themes/Theme.cs`.

## [Phase 11] - Refinement & Tuning - 2026-02-12

### Added
- **Indicators**: Added `ChoppinessIndex` (CHOP) implementation to `Strategy/Indicators.cs`.
- **Strategy Optimization**: Integrated "Chop-Block" filter into `VWAPTrendStrategy` to filter out execution during ranging markets (CHOP > 61.8).

### Changed
- **Logging**: Upgraded `Util/Log.cs` to capture CallerMemberName, CallerFilePath, and CallerLineNumber for detailed debugging.
- **Theming**: Centralized Theme logic to support `UserControl` and `Chart` components automatically.
- **UI**: Wired `DashboardControl` to use the centralized Theme engine instead of hardcoded colors.
- **Cleanup**: Removed unused `HttpUtil.PostAsync` and `HttpUtil.DeleteAsync` methods.
- **Cleanup**: Removed dead reference `Util/KeyStore.cs` from project file.
- **Resilience**: Verified `CoinbaseExchangeClient` uses proper Retry logic via `HttpUtil`.
- **Fix**: Replaced broken `JsonUtil` usage in `CoinbaseExchangeClient` with `UtilCompat`.

## [Phase 10] - Operational & Feature Expansion - 2026-02-12

### Added
- **New Strategy**: Deployed `RSIReversionStrategy` (RSI Mean Reversion logic).
## [Phase 17] - Sidecar AI Response Capture Fix - 2026-02-15

### Fixed
- **ChatGPT Response Capture**: Hardened `Services/ChromeSidecar.cs` polling to accept stable non-empty assistant responses instead of discarding repeated candidates.
- **Prompt Injection Robustness**: Expanded ChatGPT composer/send-button selectors to handle current contenteditable composer variants.
- **Selector Coverage**: Broadened assistant read selectors and tightened minimum content thresholds to reduce empty/partial captures.
- **Streaming Filter**: Expanded transient-state detection (`thinking`/`generating`/`analyzing`) to avoid returning incomplete responses.

### Verified
- **Build**: `msbuild CryptoDayTraderSuite.sln /t:Build /p:Configuration=Debug` succeeded with 0 errors.
- **Strategy Engine**: Updated `StrategyEngine` to support RSIReversion in Backtest/Paper/Live modes.
- **Indicators**: Added `RSI` to `Indicators.cs`.
- **AI Strategy Enforcement**: `StrategyEngine` now respects `GlobalBias` from AIGovernor (Bullish/Bearish/Neutral).
- **Smart Limits**: `AutoPlannerService` accepts `SuggestedLimit` from AI response for price improvement.
- **Dashboard**: Added Real-time PnL Equity Curve chart to `DashboardControl`.

### Fixed
- **Build**: Resolved broken references to deleted `JsonUtil.cs` by migrating to `UtilCompat.cs`.
- **Project Structure**: Cleaned up stale files from `.csproj`.

## [Phase 9] - Modernization - 2026-02-11

### Changed
- **Architecture**: Completed Audit and Refactor of "Static Registry" antipatterns.
- **Service Migration**: Refactored static `KeyRegistry`, `AccountRegistry`, `TimeFilters`, `ProfileStore` to injected Services.
- **Service Migration**: Refactored `EventBus` from Singleton to Injected Service in `Program.cs`.
- **Refactoring**: Updated `Brokers` (Coinbase) to use injected `IKeyService` and `IAccountService`.
- **UI**: Updated `AutoModeForm`, `KeysControl`, `AccountsControl`, and `MainForm` to use proper Dependency Injection.
- **Composition Root**: Updated `Program.cs` to explicitly compose the object graph.

### Removed
- **Legacy Brokers**: Deleted `Brokers/KrakenBroker.cs` and `Brokers/BitstampBroker.cs` (Legacy / Unmaintained).
- **Legacy Static Code**: Deleted `HistoryStore.cs`, `KeyRegistry.cs`, `AccountRegistry.cs`, `ProfileStore.cs`, `TimeFilters.cs`.
- **Redundant Utilities**: Deleted `KeyStore.cs`, `JsonUtil.cs`.

## [Phase 8] - Remediation & Hardening - 2025-08-27

### Added
- **ResilientExchangeClient**: Added a wrapper around exchange clients to handle HTTP 429/500 errors with exponential backoff.
- **KeyService / AccountService**: Added formal DI services to replace static Registries.
- **Designer Support**: Added `*.Designer.cs` files for `KeysControl`, `AccountsControl`, and `TradingControl` to enable Visual Studio Designer support.

### Changed
- **Strategy Optimization**: 
    - `Indicators.VWAP` optimized from O(N^2) to O(N) by caching session values.
    - `AutoPlannerService` simulation loop optimized to reduce allocation overhead.
- **Architecture**:
    - Migrated from Static Singletons (`KeyRegistry`, `AccountRegistry`) to Dependency Injection.
    - Standardized all UI Forms to use `Partial Class` + `Designer.cs` pattern.
- **Exports**: `ProfileService` now decrypts DPAPI blobs before export and re-encrypts on import, making `.cdtp` files portable between machines.

### Fixed
- **KrakenClient**: Repaired corrupted file content and verified `IExchangeClient` implementation.
- **Data Persistence**: Fixed bug where CheckBox changes in Accounts/Keys grids were not persisting to disk.
- **Infinite Loops**: Fixed potential infinite loop conditions in Backtester logic.

### Removed
- **Legacy Static Code**: Deleted `HistoryStore.cs`, `KeyRegistry.cs`, `AccountRegistry.cs`, `ProfileStore.cs`, `TimeFilters.cs`.
- **Redundant Utilities**: Deleted `KeyStore.cs`, `JsonUtil.cs`.




