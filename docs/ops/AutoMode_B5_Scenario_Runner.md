# AutoMode B5 Scenario Runner

This runbook provides a one-command path to generate strict Track B5 validation evidence without manually configuring profiles in the UI.

## What It Does
- Seeds deterministic AutoMode profiles into `%LocalAppData%\CryptoDayTraderSuite\automode_profiles.json`:
  - `Selected` scope with 3 symbols
  - `All` scope profile
  - `Selected` scope with 15 symbols
  - One intentional failure probe profile for failure-isolation evidence
- Launches the app and waits for a soak interval.
- Runs strict matrix validation against the newest cycle report.
- Restores your original profile file by default.

## Command

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Util\run_b5_validation_scenario.ps1
```

## Optional Parameters
- `-SoakSeconds 420` (default): how long to keep the app running before validation.
- `-MaxSymbols 12` (default): sets `CDTS_AUTOMODE_MAX_SYMBOLS` for the run.
- `-NoRestore`: keep seeded B5 profiles after the run (default behavior restores original profiles).

## Expected Output
- `RESULT:REPORT=...` path to the cycle report used for validation
- `RESULT:VALIDATION_EXIT=0` on pass, non-zero on failure
- `RESULT:PROFILE_BACKUP=...` backup path for pre-run profiles (if any)

## Notes
- The script requires at least one account in `%LocalAppData%\CryptoDayTraderSuite\accounts.json`.
- If no new cycle report appears, ensure Auto Run has been enabled at least once in AutoMode.