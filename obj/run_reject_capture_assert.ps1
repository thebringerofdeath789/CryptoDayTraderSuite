param(
    [string]$RepoRoot = ".",
    [string]$OutputPath = "obj/runtime_reports/reject_capture_assert_latest.txt",
    [int]$DurationSeconds = 60
)

$ErrorActionPreference = 'Stop'

Set-Location $RepoRoot

$captureScript = "obj/run_reject_evidence_capture.ps1"
if (-not (Test-Path $captureScript)) {
    Write-Output "ASSERT_SETUP=FAIL missing obj/run_reject_evidence_capture.ps1"
    exit 2
}

$outputDir = Split-Path -Path $OutputPath -Parent
if (-not [string]::IsNullOrWhiteSpace($outputDir) -and -not (Test-Path $outputDir)) {
    New-Item -Path $outputDir -ItemType Directory -Force | Out-Null
}

& $captureScript `
    -DurationSeconds $DurationSeconds `
    -RuntimeCaptureAttempts 1 `
    -ExternalRetryCount 1 `
    -ExternalRetryDelaySeconds 0 `
    -RuntimeRetryDelaySeconds 0 `
    -UseFastProfileOverride `
    -SkipAutoRunPrecheck `
    -AllowPartialExitCodeZero *> $OutputPath

$scriptExit = $LASTEXITCODE
$text = Get-Content -Path $OutputPath -Raw

$checks = New-Object System.Collections.Generic.List[object]
function Add-Assert([string]$Name, [bool]$Pass) {
    $checks.Add([pscustomobject]@{ Name = $Name; Pass = $Pass }) | Out-Null
}

Add-Assert 'fallback_mode' ($text -match 'RUNTIME_CAPTURE_FALLBACK_MODE=enabled')
Add-Assert 'two_attempts' ($text -match 'RUNTIME_CAPTURE_ATTEMPT=1/2' -and $text -match 'RUNTIME_CAPTURE_ATTEMPT=2/2')
Add-Assert 'fallback_flag' ($text -match 'RUNTIME_CAPTURE_FALLBACK_SECOND_PASS=1')
Add-Assert 'ci_summary_present' ($text -match 'CI_SUMMARY=')
Add-Assert 'capture_result_present' ($text -match 'CAPTURE_RESULT=(PASS|PARTIAL|FAIL|NOOP|DRYRUN)')
Add-Assert 'effective_exit_present' ($text -match 'effective_exit=')
Add-Assert 'original_exit_present' ($text -match 'original_exit=')
Add-Assert 'override_present' ($text -match 'override=')
Add-Assert 'result_exit_marker' ($text -match 'RESULT_EXIT_CODE=')

$overrideMatch = [regex]::Match($text, 'override=(\d+)')
$override = if ($overrideMatch.Success) { [int]$overrideMatch.Groups[1].Value } else { -1 }
if ($override -eq 1) {
    Add-Assert 'override_marker' ($text -match 'RESULT_EXIT_OVERRIDDEN=0')
}

$failed = @($checks | Where-Object { -not $_.Pass })

Write-Output ('TEST_ARTIFACT=' + (Resolve-Path $OutputPath))
Write-Output ('SCRIPT_EXIT=' + $scriptExit)
foreach ($c in $checks) {
    Write-Output ('ASSERT_' + $c.Name.ToUpperInvariant() + '=' + $(if ($c.Pass) { 'PASS' } else { 'FAIL' }))
}
Write-Output ('ASSERT_FAILED_COUNT=' + $failed.Count)

if ($failed.Count -gt 0) {
    exit 2
}

exit 0
