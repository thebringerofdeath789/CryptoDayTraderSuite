param(
    [int]$DurationSeconds = 390,
    [switch]$RunStrict,
    [switch]$SkipAutoRunPrecheck,
    [int]$AutoModeMaxSymbols = 4,
    [switch]$UseFastProfileOverride,
    [switch]$AutoRepairBindings,
    [switch]$RunProviderProbeBeforeCert,
    [switch]$AllowPartialExitCodeZero,
    [int]$ExternalRetryCount = 2,
    [int]$ExternalRetryDelaySeconds = 3,
    [int]$RuntimeCaptureAttempts = 2,
    [int]$RuntimeRetryDelaySeconds = 5,
    [string]$RepoRoot = "c:\Users\admin\Documents\Visual Studio 2022\Projects\CryptoDayTraderSuite"
)

$ErrorActionPreference = 'Stop'

$CaptureCiVersion = '1'
$CaptureCiFields = 'result|decision|exit|effective_exit|original_exit|override|strict|cycle_fresh|log_fresh|observed|default_exit|strict_exit|report|precheck_autorun_known|precheck_autorun_enabled|precheck_runnable_profiles|precheck_fresh_cycle|precheck_cycle_age_min'
$FreshnessSkewSeconds = 2
$PostRunEvidenceSettleSeconds = 12
$EscalationDurationStepSeconds = 90
$EscalationMaxDurationSeconds = 900
$EscalationMaxSymbolsCap = 12

trap {
    $message = if ($null -ne $_ -and $null -ne $_.Exception) { $_.Exception.Message } else { "unknown error" }
    Write-Output ('UNHANDLED_ERROR=' + $message)
    Emit-CaptureContract -Result 'PARTIAL' -Decision 'partial-unhandled-error' -ExitCode 6 -CycleFresh -1 -LogFresh -1 -Observed 'none' -DefaultExit -1 -StrictExit -1 -ReportPath 'none'
    Complete-Result -Message 'RESULT:PARTIAL unhandled script error during reject evidence capture.' -Code 6 -IsPartial
}

if ($DurationSeconds -lt 60) {
    throw "DurationSeconds must be at least 60."
}

if ($AutoModeMaxSymbols -lt 1) {
    throw "AutoModeMaxSymbols must be at least 1."
}

if ($ExternalRetryCount -lt 1) {
    throw "ExternalRetryCount must be at least 1."
}

if ($ExternalRetryDelaySeconds -lt 0) {
    throw "ExternalRetryDelaySeconds must be non-negative."
}

if ($RuntimeCaptureAttempts -lt 1) {
    throw "RuntimeCaptureAttempts must be at least 1."
}

if ($RuntimeRetryDelaySeconds -lt 0) {
    throw "RuntimeRetryDelaySeconds must be non-negative."
}

$categories = @("fees-kill", "slippage-kill", "routing-unavailable", "no-signal", "ai-veto", "bias-blocked")

function Get-LatestFile {
    param(
        [string]$Pattern,
        [datetime]$AfterTime,
        [string]$Folder
    )

    if (-not (Test-Path $Folder)) {
        return $null
    }

    $latest = Get-ChildItem -Path $Folder -Filter $Pattern -File -ErrorAction SilentlyContinue |
        Where-Object { $_.LastWriteTimeUtc -ge $AfterTime.ToUniversalTime().AddSeconds(-1 * $FreshnessSkewSeconds) } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -ne $latest) {
        return $latest
    }

    return Get-ChildItem -Path $Folder -Filter $Pattern -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

function Read-KeyValueFromOutput {
    param(
        [string]$Output,
        [string]$Key,
        [string]$DefaultValue = ''
    )

    if ([string]::IsNullOrWhiteSpace($Output) -or [string]::IsNullOrWhiteSpace($Key)) {
        return $DefaultValue
    }

    $match = ($Output -split "`r?`n" | Where-Object { $_ -like ($Key + '=*') } | Select-Object -Last 1)
    if ([string]::IsNullOrWhiteSpace($match)) {
        return $DefaultValue
    }

    return $match.Substring($Key.Length + 1)
}

function Get-ZeroedCounts {
    $d = @{}
    foreach ($c in $categories) {
        $d[$c] = 0
    }
    return $d
}

function Invoke-ScriptWithRetry {
    param(
        [string]$Name,
        [string]$FilePath,
        [string[]]$Arguments,
        [int]$MaxAttempts,
        [int]$DelaySeconds,
        [scriptblock]$BeforeRetry
    )

    if ([string]::IsNullOrWhiteSpace($FilePath) -or -not (Test-Path $FilePath)) {
        return [pscustomobject]@{
            Output = ""
            ExitCode = 9001
            AttemptCount = 0
            Success = $false
            Error = "script not found"
        }
    }

    $attempt = 0
    while ($attempt -lt $MaxAttempts) {
        $attempt++
        $invokeArgs = @('-NoLogo', '-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', $FilePath)
        if ($null -ne $Arguments -and $Arguments.Count -gt 0) {
            $invokeArgs += $Arguments
        }

        $output = & powershell @invokeArgs 2>&1 | Out-String
        $exitCode = $LASTEXITCODE
        if ($exitCode -eq 0) {
            return [pscustomobject]@{
                Output = $output
                ExitCode = $exitCode
                AttemptCount = $attempt
                Success = $true
                Error = ""
            }
        }

        Write-Output ($Name + "_RETRY=" + $attempt + "/" + $MaxAttempts + " exit=" + $exitCode)
        if ($attempt -lt $MaxAttempts) {
            if ($null -ne $BeforeRetry) {
                try {
                    & $BeforeRetry
                }
                catch {
                    Write-Output ($Name + "_RETRY_HOOK_ERROR=" + $_.Exception.Message)
                }
            }

            if ($DelaySeconds -gt 0) {
                Start-Sleep -Seconds $DelaySeconds
            }
        }
        else {
            return [pscustomobject]@{
                Output = $output
                ExitCode = $exitCode
                AttemptCount = $attempt
                Success = $false
                Error = "non-zero exit after retries"
            }
        }
    }
}

function Read-RejectCountsFromCycle {
    param([string]$CyclePath)

    $counts = Get-ZeroedCounts
    if ([string]::IsNullOrWhiteSpace($CyclePath) -or -not (Test-Path $CyclePath)) {
        return $counts
    }

    try {
        $cycle = Get-Content -Path $CyclePath -Raw | ConvertFrom-Json
    }
    catch {
        return $counts
    }

    if ($null -eq $cycle) {
        return $counts
    }

    if (-not ($cycle.PSObject.Properties.Name -contains "RejectReasonCounts")) {
        return $counts
    }

    $source = $cycle.RejectReasonCounts
    if ($null -eq $source) {
        return $counts
    }

    foreach ($c in $categories) {
        $v = 0
        if ($source -is [System.Collections.IDictionary]) {
            if ($source.Contains($c) -or $source.ContainsKey($c)) {
                $v = [int]$source[$c]
            }
        }
        else {
            $p = $source.PSObject.Properties | Where-Object { [string]::Equals($_.Name, $c, [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
            if ($null -ne $p -and $null -ne $p.Value) {
                $v = [int]$p.Value
            }
        }

        $counts[$c] = [Math]::Max(0, $v)
    }

    return $counts
}

function Read-RejectCountsFromLog {
    param([string]$LogPath)

    $counts = Get-ZeroedCounts
    if ([string]::IsNullOrWhiteSpace($LogPath) -or -not (Test-Path $LogPath)) {
        return $counts
    }

    foreach ($c in $categories) {
        $pattern = [regex]::Escape($c)
        $counts[$c] = (Select-String -Path $LogPath -Pattern $pattern -CaseSensitive:$false | Measure-Object).Count
    }

    return $counts
}

function Format-ObservedCounts {
    param([hashtable]$Counts)

    if ($null -eq $Counts) {
        return ""
    }

    $parts = New-Object System.Collections.Generic.List[string]
    foreach ($c in $categories) {
        if ($Counts.ContainsKey($c) -and [int]$Counts[$c] -gt 0) {
            $parts.Add($c + "=" + [int]$Counts[$c]) | Out-Null
        }
    }

    return ($parts -join ", ")
}

function Complete-Result {
    param(
        [string]$Message,
        [int]$Code,
        [switch]$IsPartial
    )

    if (-not [string]::IsNullOrWhiteSpace($Message)) {
        Write-Output $Message
    }

    Write-Output ('RESULT_EXIT_CODE=' + $Code)

    if ($IsPartial.IsPresent -and $Code -ne 0 -and $AllowPartialExitCodeZero.IsPresent) {
        Write-Output ('RESULT_EXIT_ORIGINAL=' + $Code)
        Write-Output 'RESULT_EXIT_OVERRIDDEN=0 mode=allow-partial-exit-zero'
        exit 0
    }

    exit $Code
}

function Get-FileAgeMinutes {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path $Path)) {
        return -1
    }

    try {
        $item = Get-Item -LiteralPath $Path -ErrorAction Stop
        return [int][Math]::Round(([DateTime]::Now - $item.LastWriteTime).TotalMinutes, 0)
    }
    catch {
        return -1
    }
}

function Emit-CaptureContract {
    param(
        [string]$Result,
        [string]$Decision,
        [int]$ExitCode,
        [int]$CycleFresh,
        [int]$LogFresh,
        [string]$Observed,
        [int]$DefaultExit,
        [int]$StrictExit,
        [string]$ReportPath
    )

    $resultText = if ([string]::IsNullOrWhiteSpace($Result)) { 'PARTIAL' } else { $Result.ToUpperInvariant() }
    $decisionText = if ([string]::IsNullOrWhiteSpace($Decision)) { 'unknown' } else { $Decision.Trim().ToLowerInvariant() }
    $observedText = if ([string]::IsNullOrWhiteSpace($Observed)) { 'none' } else { $Observed }
    $reportText = if ([string]::IsNullOrWhiteSpace($ReportPath)) { 'none' } else { $ReportPath }
    $isPartial = [string]::Equals($resultText, 'PARTIAL', [System.StringComparison]::OrdinalIgnoreCase)
    $effectiveExit = $ExitCode
    $overrideText = '0'
    if ($isPartial -and $ExitCode -ne 0 -and $AllowPartialExitCodeZero.IsPresent) {
        $effectiveExit = 0
        $overrideText = '1'
    }

    Write-Output ('CAPTURE_RESULT=' + $resultText)
    Write-Output ('CAPTURE_DECISION=' + $decisionText)
    Write-Output ('CI_VERSION=' + $CaptureCiVersion)
    Write-Output ('CI_FIELDS=' + $CaptureCiFields)
    Write-Output ('CI_SUMMARY=version=' + $CaptureCiVersion + ';result=' + $resultText + ';decision=' + $decisionText + ';exit=' + $effectiveExit + ';effective_exit=' + $effectiveExit + ';original_exit=' + $ExitCode + ';override=' + $overrideText + ';strict=' + ([int]$RunStrict.IsPresent) + ';cycle=' + $CycleFresh + ';log=' + $LogFresh + ';observed=' + $observedText + ';default_exit=' + $DefaultExit + ';strict_exit=' + $StrictExit + ';report=' + $reportText + ';precheck_autorun_known=' + $precheckAutoRunKnown + ';precheck_autorun_enabled=' + $precheckAutoRunEnabled + ';precheck_runnable_profiles=' + $precheckRunnableProfiles + ';precheck_fresh_cycle=' + $precheckHasFreshCycle + ';precheck_cycle_age_min=' + $precheckCycleAgeMin)
}

Set-Location $RepoRoot

$precheckAutoRunKnown = -1
$precheckAutoRunEnabled = -1
$precheckRunnableProfiles = -1
$precheckHasFreshCycle = -1
$precheckCycleAgeMin = -1

if (-not $SkipAutoRunPrecheck.IsPresent) {
    $precheckScript = Join-Path $RepoRoot "obj\precheck_reject_evidence_capture.ps1"
    if (Test-Path $precheckScript) {
        $precheckArgs = @('-NoLogo', '-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', $precheckScript, '-RequireAutoRunEnabled', '-RequireRunnableProfile')
        $precheckOutput = & powershell @precheckArgs 2>&1 | Out-String
        $precheckExit = $LASTEXITCODE
        if (-not [string]::IsNullOrWhiteSpace($precheckOutput)) {
            $precheckOutput -split "`r?`n" | ForEach-Object {
                if (-not [string]::IsNullOrWhiteSpace($_)) {
                    Write-Output $_
                }
            }
            $precheckAutoRunKnown = [int](Read-KeyValueFromOutput -Output $precheckOutput -Key 'PRECHECK_AUTORUN_KNOWN' -DefaultValue '-1')
            $precheckAutoRunEnabled = [int](Read-KeyValueFromOutput -Output $precheckOutput -Key 'PRECHECK_AUTORUN_ENABLED' -DefaultValue '-1')
            $precheckRunnableProfiles = [int](Read-KeyValueFromOutput -Output $precheckOutput -Key 'PRECHECK_RUNNABLE_PROFILE_COUNT' -DefaultValue '-1')
            $precheckHasFreshCycle = [int](Read-KeyValueFromOutput -Output $precheckOutput -Key 'PRECHECK_HAS_FRESH_CYCLE' -DefaultValue '-1')
            $precheckCycleAgeMin = [int](Read-KeyValueFromOutput -Output $precheckOutput -Key 'PRECHECK_LATEST_CYCLE_AGE_MIN' -DefaultValue '-1')
        }

        if ($precheckExit -ne 0 -and $AutoRepairBindings.IsPresent) {
            $repairScript = Join-Path $RepoRoot "obj\repair_profile_account_bindings.ps1"
            if (Test-Path $repairScript) {
                Write-Output 'AUTO_REPAIR_BINDINGS=attempt'
                $repairOutput = & powershell -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $repairScript -Apply 2>&1 | Out-String
                if (-not [string]::IsNullOrWhiteSpace($repairOutput)) {
                    $repairOutput -split "`r?`n" | ForEach-Object {
                        if (-not [string]::IsNullOrWhiteSpace($_)) {
                            Write-Output $_
                        }
                    }
                }

                $precheckOutput = & powershell @precheckArgs 2>&1 | Out-String
                $precheckExit = $LASTEXITCODE
                if (-not [string]::IsNullOrWhiteSpace($precheckOutput)) {
                    $precheckOutput -split "`r?`n" | ForEach-Object {
                        if (-not [string]::IsNullOrWhiteSpace($_)) {
                            Write-Output $_
                        }
                    }
                    $precheckAutoRunKnown = [int](Read-KeyValueFromOutput -Output $precheckOutput -Key 'PRECHECK_AUTORUN_KNOWN' -DefaultValue '-1')
                    $precheckAutoRunEnabled = [int](Read-KeyValueFromOutput -Output $precheckOutput -Key 'PRECHECK_AUTORUN_ENABLED' -DefaultValue '-1')
                    $precheckRunnableProfiles = [int](Read-KeyValueFromOutput -Output $precheckOutput -Key 'PRECHECK_RUNNABLE_PROFILE_COUNT' -DefaultValue '-1')
                    $precheckHasFreshCycle = [int](Read-KeyValueFromOutput -Output $precheckOutput -Key 'PRECHECK_HAS_FRESH_CYCLE' -DefaultValue '-1')
                    $precheckCycleAgeMin = [int](Read-KeyValueFromOutput -Output $precheckOutput -Key 'PRECHECK_LATEST_CYCLE_AGE_MIN' -DefaultValue '-1')
                }
            }
        }

        if ($precheckExit -ne 0) {
            Emit-CaptureContract -Result 'PARTIAL' -Decision 'partial-precheck-failed' -ExitCode 5 -CycleFresh -1 -LogFresh -1 -Observed 'none' -DefaultExit -1 -StrictExit -1 -ReportPath 'none'
            Complete-Result -Message 'RESULT:PARTIAL Auto Run precheck failed; aborting capture.' -Code 5 -IsPartial
        }
    }
}

$cycleDir = Join-Path $env:LOCALAPPDATA "CryptoDayTraderSuite\automode\cycle_reports"
$logDir = Join-Path $env:LOCALAPPDATA "CryptoDayTraderSuite\logs"
if (-not (Test-Path $cycleDir)) { New-Item -ItemType Directory -Path $cycleDir | Out-Null }
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir | Out-Null }

$profileStorePath = Join-Path $env:LOCALAPPDATA "CryptoDayTraderSuite\automode_profiles.json"
$profileStoreBackupPath = $profileStorePath + ".capture.bak"
$profileOverrideApplied = $false
$profileOverrideRestored = $false

if ($UseFastProfileOverride.IsPresent -and (Test-Path $profileStorePath)) {
    try {
        $rawStore = Get-Content -Path $profileStorePath -Raw
        $store = $rawStore | ConvertFrom-Json
        if ($null -ne $store -and $null -ne $store.Profiles) {
            $candidates = @($store.Profiles | Where-Object {
                $pairScope = if ($null -ne $_.PairScope) { $_.PairScope.ToString() } else { "" }
                $_.Enabled -eq $true -and
                [string]::Equals($pairScope, "Selected", [System.StringComparison]::OrdinalIgnoreCase) -and
                $null -ne $_.SelectedPairs -and
                $_.SelectedPairs.Count -gt 0
            } | Sort-Object { $_.SelectedPairs.Count })

            if ($candidates.Count -gt 0) {
                $target = $candidates[0]
                foreach ($profile in $store.Profiles) {
                    $profileId = if ($null -ne $profile.ProfileId) { $profile.ProfileId.ToString() } else { "" }
                    $targetId = if ($null -ne $target.ProfileId) { $target.ProfileId.ToString() } else { "" }
                    $profile.Enabled = [string]::Equals($profileId, $targetId, [System.StringComparison]::OrdinalIgnoreCase)
                }

                if ($null -ne $target.SelectedPairs -and $target.SelectedPairs.Count -gt 3) {
                    $target.SelectedPairs = @($target.SelectedPairs | Select-Object -First 3)
                }

                Set-Content -Path $profileStoreBackupPath -Value $rawStore -Encoding UTF8
                $updated = $store | ConvertTo-Json -Depth 16 -Compress
                Set-Content -Path $profileStorePath -Value $updated -Encoding UTF8
                $profileOverrideApplied = $true
                $targetName = if ($null -ne $target.Name -and -not [string]::IsNullOrWhiteSpace($target.Name.ToString())) {
                    $target.Name.ToString()
                }
                elseif ($null -ne $target.ProfileId -and -not [string]::IsNullOrWhiteSpace($target.ProfileId.ToString())) {
                    $target.ProfileId.ToString()
                }
                else {
                    '(unknown)'
                }
                Write-Output ('FAST_PROFILE_OVERRIDE=1 profile=' + $targetName)
            }
            else {
                Write-Output 'FAST_PROFILE_OVERRIDE=0 no enabled selected profile candidate found.'
            }
        }
    }
    catch {
        Write-Output ('FAST_PROFILE_OVERRIDE=0 error=' + $_.Exception.Message)
    }
}

$env:CDTS_LOG_LEVEL = 'debug'
$env:CDTS_AI_PROVIDER = 'auto'
$env:CDTS_AI_MODEL = 'auto'
$env:CDTS_AI_PROPOSER_MODE = 'enabled'
$env:CDTS_AUTOMODE_MAX_SYMBOLS = $AutoModeMaxSymbols.ToString()

Write-Output ('RETRY_CONFIG=externalAttempts=' + $ExternalRetryCount + ',externalDelaySec=' + $ExternalRetryDelaySeconds + ',runtimeAttempts=' + $RuntimeCaptureAttempts + ',runtimeDelaySec=' + $RuntimeRetryDelaySeconds)

$exePath = '.\bin\Debug_Verify\CryptoDayTraderSuite.exe'
if (-not (Test-Path $exePath)) {
    $exePath = '.\bin\Debug\CryptoDayTraderSuite.exe'
}
if (-not (Test-Path $exePath)) {
    throw "CryptoDayTraderSuite executable not found in bin\\Debug_Verify or bin\\Debug."
}

$newCycle = $null
$newLog = $null
$cycleIsFresh = $false
$logIsFresh = $false
$cyclePath = ""
$logPath = ""
$cycleCounts = Get-ZeroedCounts
$logCounts = Get-ZeroedCounts
$merged = Get-ZeroedCounts
$cycleObserved = ""
$logObserved = ""
$mergedObserved = ""
$runtimeAttemptUsed = 0
$effectiveDurationSeconds = $DurationSeconds
$effectiveAutoModeMaxSymbols = $AutoModeMaxSymbols
$effectiveRuntimeCaptureAttempts = $RuntimeCaptureAttempts
$fallbackSecondPassEnabled = $false

if ($UseFastProfileOverride.IsPresent -and $RuntimeCaptureAttempts -lt 2) {
    $effectiveRuntimeCaptureAttempts = 2
    $fallbackSecondPassEnabled = $true
    Write-Output ('RUNTIME_CAPTURE_FALLBACK_MODE=enabled;reason=fast-profile-single-attempt;attempts=' + $RuntimeCaptureAttempts + '->' + $effectiveRuntimeCaptureAttempts)
}

for ($runtimeAttempt = 1; $runtimeAttempt -le $effectiveRuntimeCaptureAttempts; $runtimeAttempt++) {
    $runtimeAttemptUsed = $runtimeAttempt
    $before = [DateTime]::UtcNow

    Get-Process CryptoDayTraderSuite -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 2

    $app = Start-Process -FilePath $exePath -PassThru
    if ($profileOverrideApplied -and (Test-Path $profileStoreBackupPath)) {
        Start-Sleep -Seconds 8
        try {
            Move-Item -Path $profileStoreBackupPath -Destination $profileStorePath -Force
            $profileOverrideRestored = $true
            Write-Output 'FAST_PROFILE_OVERRIDE_RESTORED=1'
        }
        catch {
            Write-Output ('FAST_PROFILE_OVERRIDE_RESTORED=0 error=' + $_.Exception.Message)
        }
    }
    $profileMode = if ($fallbackSecondPassEnabled -and $runtimeAttempt -ge 2) { 'normal-fallback' } elseif ($UseFastProfileOverride.IsPresent) { 'fast-override' } else { 'normal' }
    Write-Output ('RUNTIME_CAPTURE_PARAMS=attempt=' + $runtimeAttempt + ';durationSec=' + $effectiveDurationSeconds + ';maxSymbols=' + $effectiveAutoModeMaxSymbols + ';profileMode=' + $profileMode)
    $env:CDTS_AUTOMODE_MAX_SYMBOLS = $effectiveAutoModeMaxSymbols.ToString()
    Start-Sleep -Seconds $effectiveDurationSeconds

    if ($app -and -not $app.HasExited) {
        try { $null = $app.CloseMainWindow() } catch { }
        Start-Sleep -Seconds 4
    }
    if ($app -and -not $app.HasExited) {
        Stop-Process -Id $app.Id -Force
    }
    Start-Sleep -Seconds 2
    Get-Process CryptoDayTraderSuite -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 2

    $newCycle = Get-LatestFile -Folder $cycleDir -Pattern 'cycle_*.json' -AfterTime $before
    $newLog = Get-LatestFile -Folder $logDir -Pattern 'log_*.txt' -AfterTime $before

    $cycleIsFresh = $false
    if ($null -ne $newCycle) {
        $cycleIsFresh = $newCycle.LastWriteTimeUtc -ge $before.AddSeconds(-1 * $FreshnessSkewSeconds)
    }

    $logIsFresh = $false
    if ($null -ne $newLog) {
        $logIsFresh = $newLog.LastWriteTimeUtc -ge $before.AddSeconds(-1 * $FreshnessSkewSeconds)
    }

    if (-not $cycleIsFresh -and -not $logIsFresh -and $PostRunEvidenceSettleSeconds -gt 0) {
        Start-Sleep -Seconds $PostRunEvidenceSettleSeconds
        $newCycle = Get-LatestFile -Folder $cycleDir -Pattern 'cycle_*.json' -AfterTime $before
        $newLog = Get-LatestFile -Folder $logDir -Pattern 'log_*.txt' -AfterTime $before
        if ($null -ne $newCycle) {
            $cycleIsFresh = $newCycle.LastWriteTimeUtc -ge $before.AddSeconds(-1 * $FreshnessSkewSeconds)
        }
        if ($null -ne $newLog) {
            $logIsFresh = $newLog.LastWriteTimeUtc -ge $before.AddSeconds(-1 * $FreshnessSkewSeconds)
        }
    }

    $cyclePath = if ($null -ne $newCycle) { $newCycle.FullName } else { "" }
    $logPath = if ($null -ne $newLog) { $newLog.FullName } else { "" }

    $cycleCounts = Read-RejectCountsFromCycle -CyclePath $cyclePath
    $logCounts = Read-RejectCountsFromLog -LogPath $logPath

    $merged = Get-ZeroedCounts
    foreach ($c in $categories) {
        $merged[$c] = [Math]::Max([int]$cycleCounts[$c], [int]$logCounts[$c])
    }

    $cycleObserved = Format-ObservedCounts -Counts $cycleCounts
    $logObserved = Format-ObservedCounts -Counts $logCounts
    $mergedObserved = Format-ObservedCounts -Counts $merged

    Write-Output ('RUNTIME_CAPTURE_ATTEMPT=' + $runtimeAttempt + '/' + $effectiveRuntimeCaptureAttempts + ' cycleFresh=' + [int]$cycleIsFresh + ' logFresh=' + [int]$logIsFresh + ' observed=' + $(if ([string]::IsNullOrWhiteSpace($mergedObserved)) { 'none' } else { $mergedObserved }))

    $hasFreshEvidence = $cycleIsFresh -or $logIsFresh
    $hasObservedRejects = -not [string]::IsNullOrWhiteSpace($mergedObserved)
    $hasSufficientEvidence = $cycleIsFresh -or $hasObservedRejects
    if ($hasSufficientEvidence -or $runtimeAttempt -eq $effectiveRuntimeCaptureAttempts) {
        break
    }

    $precheckHealthy = ($precheckAutoRunKnown -eq 1 -and $precheckAutoRunEnabled -eq 1 -and $precheckRunnableProfiles -gt 0)
    if ($precheckHealthy -and -not $cycleIsFresh) {
        $newDuration = [Math]::Min($EscalationMaxDurationSeconds, ($effectiveDurationSeconds + $EscalationDurationStepSeconds))
        $newMaxSymbols = [Math]::Min($EscalationMaxSymbolsCap, ($effectiveAutoModeMaxSymbols + 1))
        if ($newDuration -ne $effectiveDurationSeconds -or $newMaxSymbols -ne $effectiveAutoModeMaxSymbols) {
            Write-Output ('RUNTIME_CAPTURE_ESCALATION=attempt=' + $runtimeAttempt + ';reason=precheck-healthy-no-fresh-cycle;durationSec=' + $effectiveDurationSeconds + '->' + $newDuration + ';maxSymbols=' + $effectiveAutoModeMaxSymbols + '->' + $newMaxSymbols)
            $effectiveDurationSeconds = $newDuration
            $effectiveAutoModeMaxSymbols = $newMaxSymbols
        }
    }

    if ($RuntimeRetryDelaySeconds -gt 0) {
        Start-Sleep -Seconds $RuntimeRetryDelaySeconds
    }
}

if ([string]::IsNullOrWhiteSpace($mergedObserved)) {
    $warmupDurationSeconds = [Math]::Min(120, [Math]::Max(45, [int][Math]::Round($effectiveDurationSeconds / 2.0)))
    $warmupMaxSymbols = [Math]::Min($EscalationMaxSymbolsCap, [Math]::Max($effectiveAutoModeMaxSymbols, 6))
    Write-Output ('RUNTIME_EVIDENCE_WARMUP=1;durationSec=' + $warmupDurationSeconds + ';maxSymbols=' + $warmupMaxSymbols)

    $beforeWarmup = [DateTime]::UtcNow
    Get-Process CryptoDayTraderSuite -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 2

    $env:CDTS_AUTOMODE_MAX_SYMBOLS = $warmupMaxSymbols.ToString()
    $warmupApp = Start-Process -FilePath $exePath -PassThru
    Start-Sleep -Seconds $warmupDurationSeconds

    if ($warmupApp -and -not $warmupApp.HasExited) {
        try { $null = $warmupApp.CloseMainWindow() } catch { }
        Start-Sleep -Seconds 4
    }
    if ($warmupApp -and -not $warmupApp.HasExited) {
        Stop-Process -Id $warmupApp.Id -Force
    }
    Start-Sleep -Seconds 2
    Get-Process CryptoDayTraderSuite -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 2

    $newCycle = Get-LatestFile -Folder $cycleDir -Pattern 'cycle_*.json' -AfterTime $beforeWarmup
    $newLog = Get-LatestFile -Folder $logDir -Pattern 'log_*.txt' -AfterTime $beforeWarmup

    if ($null -ne $newCycle) {
        $cycleIsFresh = $newCycle.LastWriteTimeUtc -ge $beforeWarmup.AddSeconds(-1 * $FreshnessSkewSeconds)
    }
    if ($null -ne $newLog) {
        $logIsFresh = $newLog.LastWriteTimeUtc -ge $beforeWarmup.AddSeconds(-1 * $FreshnessSkewSeconds)
    }

    $cyclePath = if ($null -ne $newCycle) { $newCycle.FullName } else { $cyclePath }
    $logPath = if ($null -ne $newLog) { $newLog.FullName } else { $logPath }

    $cycleCounts = Read-RejectCountsFromCycle -CyclePath $cyclePath
    $logCounts = Read-RejectCountsFromLog -LogPath $logPath
    $merged = Get-ZeroedCounts
    foreach ($c in $categories) {
        $merged[$c] = [Math]::Max([int]$cycleCounts[$c], [int]$logCounts[$c])
    }

    $cycleObserved = Format-ObservedCounts -Counts $cycleCounts
    $logObserved = Format-ObservedCounts -Counts $logCounts
    $mergedObserved = Format-ObservedCounts -Counts $merged

    Write-Output ('RUNTIME_EVIDENCE_WARMUP_RESULT=cycleFresh=' + [int]$cycleIsFresh + ';logFresh=' + [int]$logIsFresh + ';observed=' + $(if ([string]::IsNullOrWhiteSpace($mergedObserved)) { 'none' } else { $mergedObserved }))
}

$providerProbeOutput = ""
$providerProbeExit = 0
$providerProbeReportLine = ""
$providerProbeVerdictLine = ""
$providerProbeAttempts = 0
$shouldRunProviderProbe = $RunProviderProbeBeforeCert.IsPresent -or $RunStrict.IsPresent
if ($shouldRunProviderProbe) {
    $probeResult = Invoke-ScriptWithRetry -Name "PROBE" -FilePath (Join-Path $RepoRoot "Util\run_provider_public_api_probe.ps1") -Arguments @() -MaxAttempts $ExternalRetryCount -DelaySeconds $ExternalRetryDelaySeconds
    $providerProbeOutput = $probeResult.Output
    $providerProbeExit = $probeResult.ExitCode
    $providerProbeAttempts = $probeResult.AttemptCount
    $providerProbeReportLine = ($providerProbeOutput -split "`r?`n" | Where-Object { $_ -like 'PROBE_JSON=*' } | Select-Object -Last 1)
    $providerProbeVerdictLine = ($providerProbeOutput -split "`r?`n" | Where-Object { $_ -like 'PROBE_VERDICT=*' } | Select-Object -Last 1)
}

$defaultCertResult = Invoke-ScriptWithRetry -Name "DEFAULT_CERT" -FilePath (Join-Path $RepoRoot "Util\run_multiexchange_certification.ps1") -Arguments @() -MaxAttempts $ExternalRetryCount -DelaySeconds $ExternalRetryDelaySeconds -BeforeRetry {
    Get-Process CryptoDayTraderSuite -ErrorAction SilentlyContinue | Stop-Process -Force
}
$defaultCertOutput = $defaultCertResult.Output
$defaultCertExit = $defaultCertResult.ExitCode
$defaultCertAttempts = $defaultCertResult.AttemptCount
$defaultReportLine = ($defaultCertOutput -split "`r?`n" | Where-Object { $_ -like 'REPORT_JSON=*' } | Select-Object -Last 1)
$defaultVerdictLine = ($defaultCertOutput -split "`r?`n" | Where-Object { $_ -like 'VERDICT=*' } | Select-Object -Last 1)
$defaultReportPath = if (-not [string]::IsNullOrWhiteSpace($defaultReportLine) -and $defaultReportLine.StartsWith('REPORT_JSON=')) { $defaultReportLine.Substring(12) } else { '' }

$strictCertOutput = ""
$strictCertExit = 0
$strictReportLine = ""
$strictVerdictLine = ""
$strictCertAttempts = 0
if ($RunStrict.IsPresent) {
    $strictCertResult = Invoke-ScriptWithRetry -Name "STRICT_CERT" -FilePath (Join-Path $RepoRoot "Util\run_multiexchange_certification.ps1") -Arguments @('-RequireBuildPass', '-RequireMatrixPass', '-RequireProviderArtifacts', '-RequireRejectCategories') -MaxAttempts $ExternalRetryCount -DelaySeconds $ExternalRetryDelaySeconds -BeforeRetry {
        Get-Process CryptoDayTraderSuite -ErrorAction SilentlyContinue | Stop-Process -Force
    }
    $strictCertOutput = $strictCertResult.Output
    $strictCertExit = $strictCertResult.ExitCode
    $strictCertAttempts = $strictCertResult.AttemptCount
    $strictReportLine = ($strictCertOutput -split "`r?`n" | Where-Object { $_ -like 'REPORT_JSON=*' } | Select-Object -Last 1)
    $strictVerdictLine = ($strictCertOutput -split "`r?`n" | Where-Object { $_ -like 'VERDICT=*' } | Select-Object -Last 1)
}

Write-Output ('CYCLE_JSON=' + $cyclePath)
Write-Output ('CYCLE_IS_FRESH=' + ([int]$cycleIsFresh))
Write-Output ('LOG_FILE=' + $logPath)
Write-Output ('LOG_IS_FRESH=' + ([int]$logIsFresh))
Write-Output ('CYCLE_AGE_MIN=' + (Get-FileAgeMinutes -Path $cyclePath))
Write-Output ('LOG_AGE_MIN=' + (Get-FileAgeMinutes -Path $logPath))
Write-Output ('RUNTIME_CAPTURE_ATTEMPTS_USED=' + $runtimeAttemptUsed)
Write-Output ('RUNTIME_CAPTURE_ATTEMPTS_MAX=' + $effectiveRuntimeCaptureAttempts)
Write-Output ('RUNTIME_CAPTURE_FALLBACK_SECOND_PASS=' + ([int]$fallbackSecondPassEnabled))
Write-Output ('AUTOMODE_MAX_SYMBOLS=' + $AutoModeMaxSymbols)
Write-Output ('REJECT_OBSERVED_CYCLE=' + $(if ([string]::IsNullOrWhiteSpace($cycleObserved)) { 'none' } else { $cycleObserved }))
Write-Output ('REJECT_OBSERVED_LOG=' + $(if ([string]::IsNullOrWhiteSpace($logObserved)) { 'none' } else { $logObserved }))
Write-Output ('REJECT_OBSERVED_MERGED=' + $(if ([string]::IsNullOrWhiteSpace($mergedObserved)) { 'none' } else { $mergedObserved }))
Write-Output ('PRECHECK_AUTORUN_KNOWN_CAPTURE=' + $precheckAutoRunKnown)
Write-Output ('PRECHECK_AUTORUN_ENABLED_CAPTURE=' + $precheckAutoRunEnabled)
Write-Output ('PRECHECK_RUNNABLE_PROFILES_CAPTURE=' + $precheckRunnableProfiles)
Write-Output ('PRECHECK_FRESH_CYCLE_CAPTURE=' + $precheckHasFreshCycle)
Write-Output ('PRECHECK_CYCLE_AGE_MIN_CAPTURE=' + $precheckCycleAgeMin)

if ($shouldRunProviderProbe) {
    if (-not [string]::IsNullOrWhiteSpace($providerProbeReportLine)) { Write-Output $providerProbeReportLine }
    if (-not [string]::IsNullOrWhiteSpace($providerProbeVerdictLine)) { Write-Output $providerProbeVerdictLine }
    Write-Output ('PROBE_ATTEMPTS=' + $providerProbeAttempts)
    Write-Output ('PROBE_EXIT=' + $providerProbeExit)
}

if (-not [string]::IsNullOrWhiteSpace($defaultReportLine)) { Write-Output $defaultReportLine }
if (-not [string]::IsNullOrWhiteSpace($defaultVerdictLine)) { Write-Output $defaultVerdictLine }
Write-Output ('DEFAULT_CERT_ATTEMPTS=' + $defaultCertAttempts)
Write-Output ('DEFAULT_CERT_EXIT=' + $defaultCertExit)

if ($RunStrict.IsPresent) {
    if (-not [string]::IsNullOrWhiteSpace($strictReportLine)) { Write-Output $strictReportLine }
    if (-not [string]::IsNullOrWhiteSpace($strictVerdictLine)) { Write-Output $strictVerdictLine }
    Write-Output ('STRICT_CERT_ATTEMPTS=' + $strictCertAttempts)
    Write-Output ('STRICT_CERT_EXIT=' + $strictCertExit)
}

if ($profileOverrideApplied) {
    if (Test-Path $profileStoreBackupPath) {
        try {
            Move-Item -Path $profileStoreBackupPath -Destination $profileStorePath -Force
            $profileOverrideRestored = $true
            Write-Output 'FAST_PROFILE_OVERRIDE_RESTORED_FINAL=1 mode=final-restore'
        }
        catch {
            Write-Output ('FAST_PROFILE_OVERRIDE_RESTORED_FINAL=0 error=' + $_.Exception.Message)
        }
    }
    elseif ($profileOverrideRestored) {
        Write-Output 'FAST_PROFILE_OVERRIDE_RESTORED_FINAL=1 mode=already-restored'
    }
    else {
        Write-Output 'FAST_PROFILE_OVERRIDE_RESTORED_FINAL=0 reason=backup-not-found'
    }
}

if ($defaultCertExit -ne 0) {
    Emit-CaptureContract -Result 'PARTIAL' -Decision 'partial-default-cert-failed' -ExitCode 7 -CycleFresh ([int]$cycleIsFresh) -LogFresh ([int]$logIsFresh) -Observed $(if ([string]::IsNullOrWhiteSpace($mergedObserved)) { 'none' } else { $mergedObserved }) -DefaultExit $defaultCertExit -StrictExit $strictCertExit -ReportPath $defaultReportPath
    Complete-Result -Message 'RESULT:PARTIAL default certification failed during reject evidence capture.' -Code 7 -IsPartial
}

if ([string]::IsNullOrWhiteSpace($mergedObserved)) {
    if (-not $cycleIsFresh) {
        Emit-CaptureContract -Result 'PARTIAL' -Decision 'partial-no-fresh-cycle' -ExitCode 4 -CycleFresh ([int]$cycleIsFresh) -LogFresh ([int]$logIsFresh) -Observed 'none' -DefaultExit $defaultCertExit -StrictExit $strictCertExit -ReportPath $defaultReportPath
        Complete-Result -Message 'RESULT:PARTIAL no fresh cycle artifact detected; ensure Auto Run is enabled before capture.' -Code 4 -IsPartial
    }

    Emit-CaptureContract -Result 'PARTIAL' -Decision 'partial-no-reject-evidence' -ExitCode 2 -CycleFresh ([int]$cycleIsFresh) -LogFresh ([int]$logIsFresh) -Observed 'none' -DefaultExit $defaultCertExit -StrictExit $strictCertExit -ReportPath $defaultReportPath
    Complete-Result -Message 'RESULT:PARTIAL no reject categories observed in fresh evidence window.' -Code 2 -IsPartial
}

if ($RunStrict.IsPresent -and $strictCertExit -ne 0) {
    Emit-CaptureContract -Result 'PARTIAL' -Decision 'partial-strict-gates-failed' -ExitCode 3 -CycleFresh ([int]$cycleIsFresh) -LogFresh ([int]$logIsFresh) -Observed $mergedObserved -DefaultExit $defaultCertExit -StrictExit $strictCertExit -ReportPath $defaultReportPath
    Complete-Result -Message 'RESULT:PARTIAL reject evidence observed, strict certification still failing on other gates.' -Code 3 -IsPartial
}

Emit-CaptureContract -Result 'PASS' -Decision 'pass-reject-evidence-observed' -ExitCode 0 -CycleFresh ([int]$cycleIsFresh) -LogFresh ([int]$logIsFresh) -Observed $mergedObserved -DefaultExit $defaultCertExit -StrictExit $strictCertExit -ReportPath $defaultReportPath
Complete-Result -Message 'RESULT:PASS reject evidence observed and certification executed.' -Code 0
