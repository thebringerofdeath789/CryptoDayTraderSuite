param(
    [string]$RepoRoot = ".",
    [switch]$Strict,
    [switch]$ForceStale,
    [switch]$RequireZeroExit
)

$ErrorActionPreference = 'Stop'

function Complete-Result {
    param(
        [int]$Code
    )

    Write-Output ('RESULT_EXIT_CODE=' + $Code)
    Write-Output ('CONTRACT_FINAL_EXIT=' + $Code)
    exit $Code
}

trap {
    $message = if ($null -ne $_ -and $null -ne $_.Exception) { $_.Exception.Message } else { 'unknown error' }
    Write-Output ('CONTRACT_ERROR=' + $message)
    Write-Output 'CONTRACT_RESULT=FAIL'
    Complete-Result -Code 1
}

$resolvedRepo = (Resolve-Path $RepoRoot).Path
Set-Location $resolvedRepo

$runnerPath = Join-Path $resolvedRepo "Util\run_multiexchange_certification.ps1"
if (-not (Test-Path $runnerPath)) {
    throw "Certification runner not found: $runnerPath"
}

$args = @('-NoLogo', '-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', $runnerPath)
if ($Strict.IsPresent) {
    $args += '-Strict'
}
if ($ForceStale.IsPresent) {
    $args += @('-MaxMatrixArtifactAgeHours', '0', '-MaxProviderProbeAgeHours', '0', '-MaxRejectEvidenceAgeHours', '0')
}

$output = & powershell @args 2>&1 | Out-String
$exitCode = $LASTEXITCODE

$lines = @($output -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

$keyMap = @{}
foreach ($line in $lines) {
    if ($line -match '^([A-Z_]+)=(.*)$') {
        $keyMap[$matches[1]] = $matches[2]
    }
}

$requiredKeys = @(
    'CI_VERSION',
    'CI_FIELDS',
    'REPORT_JSON',
    'REPORT_TXT',
    'STRICT_REQUESTED',
    'STRICT_SWITCH',
    'STRICT_GATES',
    'STRICT_FAILURE_CLASS',
    'STRICT_POLICY_DECISION',
    'CI_SUMMARY',
    'STRICT_FAILURE_COUNT',
    'STRICT_FAILURE_NAMES',
    'VERDICT'
)

$missing = @($requiredKeys | Where-Object { -not $keyMap.ContainsKey($_) })
$fieldList = @()
if ($keyMap.ContainsKey('CI_FIELDS')) {
    $fieldList = @($keyMap['CI_FIELDS'].Split(',') | ForEach-Object { $_.Trim() } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

$missingInCiFields = @($requiredKeys | Where-Object { $fieldList -notcontains $_ })
$ciVersion = if ($keyMap.ContainsKey('CI_VERSION')) { [string]$keyMap['CI_VERSION'] } else { '' }
$ciSummary = if ($keyMap.ContainsKey('CI_SUMMARY')) { [string]$keyMap['CI_SUMMARY'] } else { '' }
$ciSummaryVersionExpected = 'version=' + $ciVersion
$ciSummaryVersionOk = -not [string]::IsNullOrWhiteSpace($ciVersion) -and $ciSummary.Contains($ciSummaryVersionExpected)

if (-not [string]::IsNullOrWhiteSpace($output)) {
    $lines | ForEach-Object { Write-Output $_ }
}

Write-Output ('CONTRACT_EXIT_CODE=' + $exitCode)
Write-Output ('CONTRACT_REQUIRED_KEYS=' + ($requiredKeys -join ','))
Write-Output ('CONTRACT_MISSING_KEYS=' + ($(if ($missing.Count -gt 0) { $missing -join ',' } else { 'none' })))
Write-Output ('CONTRACT_FIELDS_MISSING=' + ($(if ($missingInCiFields.Count -gt 0) { $missingInCiFields -join ',' } else { 'none' })))
Write-Output ('CONTRACT_CISUMMARY_VERSION_OK=' + [string]$ciSummaryVersionOk)

$pass = $true
if ($missing.Count -gt 0) { $pass = $false }
if ($missingInCiFields.Count -gt 0) { $pass = $false }
if (-not $ciSummaryVersionOk) { $pass = $false }
if ($RequireZeroExit.IsPresent -and $exitCode -ne 0) { $pass = $false }

if ($pass) {
    Write-Output 'CONTRACT_RESULT=PASS'
    Complete-Result -Code 0
}

Write-Output 'CONTRACT_RESULT=FAIL'
Complete-Result -Code 1
