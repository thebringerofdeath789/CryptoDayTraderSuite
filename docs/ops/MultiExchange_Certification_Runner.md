# Multi-Exchange Certification Runner

[← Back to Documentation Index](../index.md)

## Purpose

Run a single command that produces deterministic multi-exchange certification artifacts with a promotion verdict:

- `PASS`
- `PARTIAL`
- `FAIL`

The runner consolidates:

- build viability check
- required Phase 19 component presence
- latest matrix artifact status
- reject-category extraction from latest runtime log
- strategy × exchange status table with per-row evidence references

## Script

- `Util/run_multiexchange_certification.ps1`

## Default Output

Artifacts are written to:

- `obj/runtime_reports/multiexchange/`

Per run:

- `multi_exchange_cert_YYYYMMDD_HHMMSS.json`
- `multi_exchange_cert_YYYYMMDD_HHMMSS.txt`

## Basic Usage

From repo root:

`powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\Util\run_multiexchange_certification.ps1`

The script prints:

- `REPORT_JSON=...`
- `REPORT_TXT=...`
- `VERDICT=PASS|PARTIAL|FAIL`

## Strict Switches

Optional hard-gates:

- `-RequireBuildPass`
- `-RequireMatrixPass`
- `-RequireProviderArtifacts`
- `-RequireRejectCategories`

Strict mode behavior:

- Any strict switch enables strict mode evaluation.
- Strict mode fails when any required strategy × exchange row is missing explicit evidence.

Example:

`powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\Util\run_multiexchange_certification.ps1 -RequireBuildPass -RequireMatrixPass`

## Provider Probe Prerequisite

Before running strict certification gates, run provider public-API probe evidence generation:

`powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\Util\run_provider_public_api_probe.ps1`

Then run strict certification:

`powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\Util\run_multiexchange_certification.ps1 -RequireBuildPass -RequireMatrixPass -RequireProviderArtifacts -RequireRejectCategories`

For a combined runtime evidence window + certification flow:

`powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\obj\run_reject_evidence_capture.ps1 -DurationSeconds 390 -RunStrict`

For the same flow with automatic profile/account binding repair prechecks:

`powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\obj\run_reject_evidence_capture.ps1 -DurationSeconds 390 -RunStrict -AutoRepairBindings`

For full strict flow with fresh provider probe evidence in the same run:

`powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\obj\run_reject_evidence_capture.ps1 -DurationSeconds 390 -RunStrict -AutoRepairBindings -RunProviderProbeBeforeCert`

To validate persisted Auto Run state only:

`powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\obj\precheck_reject_evidence_capture.ps1 -RequireAutoRunEnabled`

To validate full runtime readiness (Auto Run + runnable profile/account bindings):

`powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\obj\precheck_reject_evidence_capture.ps1 -RequireAutoRunEnabled -RequireRunnableProfile`

To inspect/fix stale profile-account bindings directly:

`powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\obj\repair_profile_account_bindings.ps1`

`powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\obj\repair_profile_account_bindings.ps1 -Apply`

The orchestration script emits diagnostics to show whether evidence was actually fresh:

- `CYCLE_IS_FRESH=1` indicates a new `cycle_*.json` was produced during capture.
- `LOG_IS_FRESH=1` indicates a new runtime log file was produced.
- `REJECT_OBSERVED_*` lines summarize observed reject categories and counts.
- `PRECHECK_AUTORUN_ENABLED=1` confirms persisted Auto Run preference is enabled.
- `PRECHECK_RUNNABLE_PROFILE_COUNT>0` confirms at least one enabled profile is bound to an enabled account.
- `PRECHECK_UNMATCHED_PROFILE_COUNT`/`PRECHECK_UNMATCHED_ACCOUNT_IDS` identify stale profile account bindings that cause `skipped(account)` cycles.
- `PROBE_JSON` / `PROBE_VERDICT` / `PROBE_EXIT` indicate provider probe evidence refresh results when `-RunProviderProbeBeforeCert` is enabled (or strict mode invokes probe refresh).

## Evidence Fields Produced

Report fields include:

- check list (`Name`, `Status`, `Detail`)
- reject category counts (`fees-kill`, `slippage-kill`, `routing-unavailable`, `no-signal`, `ai-veto`, `bias-blocked`)
- strategy × exchange matrix rows (`Strategy`, `Exchange`, `Status`, `EvidenceRef`, `EvidenceSource`, `Detail`)
- final verdict and recommended next action

## Row Evidence Sources

The runner builds row-level evidence from JSON artifacts in this precedence order:

- `-RowEvidenceDir` (if provided)
- `obj/runtime_reports/strategy_exchange_evidence/*.json`
- `obj/runtime_reports/multiexchange/row_evidence/*.json`
- latest Auto Mode cycle artifacts (`%LocalAppData%\CryptoDayTraderSuite\automode\cycle_reports\cycle_*.json`) when they contain explicit row entries

Accepted row shape (single object or items in array fields such as `StrategyExchangeRows`, `MatrixRows`, `Results`):

- `Strategy` (or `StrategyName`)
- `Exchange` (or `ExchangeName` / `Venue`)
- `Status` (`PASS`/`PARTIAL`/`FAIL`)
- `EvidenceRef` (optional; falls back to artifact path)
- `Detail` (optional)

## Notes

- Build is run against a lock-safe output directory (`bin\Debug_Verify\`) to avoid false negatives from active app binary locks.
- Runner auto-emits policy/provider-backed row-evidence artifacts to `obj/runtime_reports/strategy_exchange_evidence/strategy_exchange_policy_evidence_*.json` before matrix assembly.
- Provider probe artifact failures are classified (`ENV-CONSTRAINT`, `PROVIDER-ERROR`, `INTEGRATION-ERROR`) in generated probe evidence.
- Default certification treats provider environment/provider constraints as `PARTIAL`.
- Strict certification records environment-only provider/coverage constraints as explicit blockers (`Geo/provider access blocker`, `Spot/perp geo-access blocker`) and continues evaluating remaining gates; integration failures still hard-fail strict provider checks.
- Spot/perps coverage check treats `ENV-CONSTRAINT` venues as deferred evidence (`spot-env-waived` / `perp-env-waived`) rather than hard `*-missing` failures in default mode.
- Current perp-required coverage scope is `Coinbase`, `Binance`, and `Bybit`; `OKX` and `Kraken` remain spot-only in current client implementations.
- Policy-backed row evidence captures contract-level row status (`PASS`/`PARTIAL`/`FAIL`) with explicit evidence references and is intended to prevent synthetic matrix projection while preserving strict row-evidence completeness checks.
