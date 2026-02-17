param(
    [int]$DurationSeconds = 390,
    [switch]$RunStrict,
    [switch]$SkipAutoRunPrecheck,
    [int]$AutoModeMaxSymbols = 4,
    [switch]$UseFastProfileOverride,
    [switch]$AutoRepairBindings,
    [switch]$RunProviderProbeBeforeCert,
    [int]$ExternalRetryCount = 2,
    [int]$ExternalRetryDelaySeconds = 3,
    [int]$RuntimeCaptureAttempts = 2,
    [int]$RuntimeRetryDelaySeconds = 5,
    [string]$RepoRoot = "c:\Users\admin\Documents\Visual Studio 2022\Projects\CryptoDayTraderSuite"
)

$ErrorActionPreference = 'Stop'

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
        Where-Object { $_.LastWriteTime -gt $AfterTime } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -ne $latest) {
        return $latest
    }

    return Get-ChildItem -Path $Folder -Filter $Pattern -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
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

Set-Location $RepoRoot

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
                }
            }
        }

        if ($precheckExit -ne 0) {
            Write-Output 'RESULT:PARTIAL Auto Run precheck failed; aborting capture.'
            exit 5
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

for ($runtimeAttempt = 1; $runtimeAttempt -le $RuntimeCaptureAttempts; $runtimeAttempt++) {
    $runtimeAttemptUsed = $runtimeAttempt
    $before = Get-Date

    Get-Process CryptoDayTraderSuite -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 2

    $app = Start-Process -FilePath $exePath -PassThru
    if ($profileOverrideApplied -and (Test-Path $profileStoreBackupPath)) {
        Start-Sleep -Seconds 8
        try {
            Move-Item -Path $profileStoreBackupPath -Destination $profileStorePath -Force
            Write-Output 'FAST_PROFILE_OVERRIDE_RESTORED=1'
        }
        catch {
            Write-Output ('FAST_PROFILE_OVERRIDE_RESTORED=0 error=' + $_.Exception.Message)
        }
    }
    Start-Sleep -Seconds $DurationSeconds

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
        $cycleIsFresh = $newCycle.LastWriteTime -gt $before
    }

    $logIsFresh = $false
    if ($null -ne $newLog) {
        $logIsFresh = $newLog.LastWriteTime -gt $before
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

    Write-Output ('RUNTIME_CAPTURE_ATTEMPT=' + $runtimeAttempt + '/' + $RuntimeCaptureAttempts + ' cycleFresh=' + [int]$cycleIsFresh + ' logFresh=' + [int]$logIsFresh + ' observed=' + $(if ([string]::IsNullOrWhiteSpace($mergedObserved)) { 'none' } else { $mergedObserved }))

    $hasFreshEvidence = $cycleIsFresh -or $logIsFresh
    $hasObservedRejects = -not [string]::IsNullOrWhiteSpace($mergedObserved)
    $hasSufficientEvidence = $cycleIsFresh -or $hasObservedRejects
    if ($hasSufficientEvidence -or $runtimeAttempt -eq $RuntimeCaptureAttempts) {
        break
    }

    if ($RuntimeRetryDelaySeconds -gt 0) {
        Start-Sleep -Seconds $RuntimeRetryDelaySeconds
    }
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
Write-Output ('RUNTIME_CAPTURE_ATTEMPTS_USED=' + $runtimeAttemptUsed)
Write-Output ('RUNTIME_CAPTURE_ATTEMPTS_MAX=' + $RuntimeCaptureAttempts)
Write-Output ('AUTOMODE_MAX_SYMBOLS=' + $AutoModeMaxSymbols)
Write-Output ('REJECT_OBSERVED_CYCLE=' + $(if ([string]::IsNullOrWhiteSpace($cycleObserved)) { 'none' } else { $cycleObserved }))
Write-Output ('REJECT_OBSERVED_LOG=' + $(if ([string]::IsNullOrWhiteSpace($logObserved)) { 'none' } else { $logObserved }))
Write-Output ('REJECT_OBSERVED_MERGED=' + $(if ([string]::IsNullOrWhiteSpace($mergedObserved)) { 'none' } else { $mergedObserved }))

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

if ([string]::IsNullOrWhiteSpace($mergedObserved)) {
    if (-not $cycleIsFresh) {
        Write-Output 'RESULT:PARTIAL no fresh cycle artifact detected; ensure Auto Run is enabled before capture.'
        exit 4
    }
    Write-Output 'RESULT:PARTIAL no reject categories observed in fresh evidence window.'
    exit 2
}

if ($RunStrict.IsPresent -and $strictCertExit -ne 0) {
    Write-Output 'RESULT:PARTIAL reject evidence observed, strict certification still failing on other gates.'
    exit 3
}

Write-Output 'RESULT:PASS reject evidence observed and certification executed.'
exit 0
