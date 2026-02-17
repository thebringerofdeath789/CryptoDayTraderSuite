[← Back to Documentation Index](../index.md)

# AutoMode Runtime Snapshot Verifier

## Purpose
Generate a deterministic, one-command runtime snapshot from recent log files for AutoMode stability checks.

## Script
Use:

- `obj/verify_runtime_snapshot.ps1`

The script reports per-log and aggregate metrics:

- completed cycles,
- paper fills,
- HTTP `429` count,
- no-signal informational count,
- bias-block count,
- AI veto count,
- All-scope limit-application count.

## Usage
From repository root:

```powershell
# Snapshot latest 4 logs (no strict pass/fail gates)
powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\obj\verify_runtime_snapshot.ps1 -Count 4

# Strict validation gate (fail if any sampled log has 429, no cycle, or no fill)
powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\obj\verify_runtime_snapshot.ps1 -Count 4 -RequireNo429 -RequireCycles -RequireFills

# Strict validation gate, but ignore startup-only logs (recommended)
powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\obj\verify_runtime_snapshot.ps1 -Count 3 -RequireNo429 -RequireCycles -IgnoreStartupOnly

# Strict validation gate with fill requirement (only for execution-focused runs)
powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\obj\verify_runtime_snapshot.ps1 -Count 3 -RequireNo429 -RequireCycles -RequireFills

# Narrow sample to latest 3 logs (useful to avoid very recent startup-only log)
powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\obj\verify_runtime_snapshot.ps1 -Count 3 -RequireNo429 -RequireCycles -RequireFills
```

## Output Format
- Per log:
  - `SNAPSHOT|log_xxx.txt|cycles=...|fills=...|429=...|...`
- Aggregate:
  - `SUMMARY|files=...|cycles=...|fills=...|429=...|...`
- Result:
  - `RESULT:PASS ...` or `RESULT:FAIL ...`

## Exit Codes
- `0`: success (requirements met or no strict requirements requested).
- `1`: strict requirements failed.
- `2`: script usage/discovery error (invalid count, missing log root, no logs found).

## Notes
- Immediately after app startup/shutdown, the newest log can contain setup lines only; in strict mode this may fail `RequireCycles`/`RequireFills` by design.
- Use `-IgnoreStartupOnly` to skip strict checks on startup-only logs (`cycles=0`, `fills=0`, and no signal/decision lines).
- Use `-RequireFills` only when you explicitly expect executed trades in the sampled window (it can fail valid no-fill cycles caused by veto/no-signal guardrails).
- `RequireCycles` and `RequireFills` evaluate the sampled window in aggregate (not per-file), while `RequireNo429` remains strict per log file.
- Use `-Count 3` or run for a longer interval before sampling to avoid startup-only artifacts.
