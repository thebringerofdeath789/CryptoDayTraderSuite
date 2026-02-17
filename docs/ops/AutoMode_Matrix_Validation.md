[← Back to Documentation Index](../index.md)

# AutoMode Matrix Validation

## Purpose
Validate Track B matrix evidence from the latest AutoMode cycle telemetry JSON without manually inspecting raw files.

## Report Source
AutoMode writes one report per cycle to:

- `%LocalAppData%\CryptoDayTraderSuite\automode\cycle_reports\cycle_*.json`

## Validator Script
Use:

- `Util/validate_automode_matrix.ps1`

The script validates:

- minimum processed profile count,
- pair-configuration consistency (`MatrixPairConfigurationConsistent`),
- independent guardrail observation (`MatrixIndependentGuardrailsObserved`),
- guardrail scope isolation (`MatrixGuardrailScopesIsolated`),
- failure non-halting behavior (`MatrixFailureDoesNotHaltCycle`),
- optional strict failure containment evidence (`MatrixFailureContainmentObserved`),
- overall matrix state (`MatrixStatus=PASS`).

Optional strict scenario checks:

- mixed pair scopes in one cycle (`Selected` + `All`),
- required selected profile symbol counts (for scenarios like `3` and `15`),
- independent per-profile guardrail configuration evidence (distinct scope keys + guardrail tuples + risk telemetry),
- failure isolation evidence (at least one failed/blocked profile and at least one unrelated profile that still completes).

## Usage
From the repository root:

```powershell
# Validate latest report with baseline checks
.\Util\validate_automode_matrix.ps1

# Validate latest report and require an observed failure-containment scenario
.\Util\validate_automode_matrix.ps1 -RequireFailureContainment

# Validate a mixed-scope scenario (`Selected` + `All`) with required selected counts (example: 3 and 15)
.\Util\validate_automode_matrix.ps1 -RequireMixedScopes -RequireSelectedSymbolCounts 3,15

# Validate independent per-profile guardrail configurations and failure isolation evidence
.\Util\validate_automode_matrix.ps1 -RequireIndependentGuardrailConfigs -RequireFailureIsolation

# Validate a specific report file with explicit minimum profile count
.\Util\validate_automode_matrix.ps1 -ReportPath "$env:LOCALAPPDATA\CryptoDayTraderSuite\automode\cycle_reports\cycle_20260216_145501123_abcd1234.json" -MinProfiles 3
```

## Exit Codes
- `0`: validation passed.
- `1`: one or more checks failed, or report discovery/parsing failed.

## Notes
- Use `-RequireFailureContainment` only when your test run intentionally includes at least one blocked/error profile in the same cycle.
- Use `-RequireMixedScopes -RequireSelectedSymbolCounts 3,15` to validate the common Track B example scenario in one run.
- Use `-RequireIndependentGuardrailConfigs` when profiles intentionally differ in max trades/cooldown/daily-risk settings.
- Use `-RequireFailureIsolation` only when your run intentionally includes one failing/blocked profile while others continue.
- If no cycle report exists yet, run one full AutoMode cycle first.
