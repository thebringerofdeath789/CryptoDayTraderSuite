param(
    [int]$SoakSeconds = 420,
    [int]$MaxSymbols = 12,
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"

function Write-Info([string]$msg) {
    Write-Host ("[B5] " + $msg)
}

function Resolve-NormalizedAccountId {
    param([object]$RawId)

    if ($null -eq $RawId) { return "" }

    $text = [string]$RawId
    if ([string]::IsNullOrWhiteSpace($text)) { return "" }

    $trimmed = $text.Trim()
    $guidRegex = [regex]'[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}'
    $match = $guidRegex.Match($trimmed)
    if ($match.Success) {
        return $match.Value.ToLowerInvariant()
    }

    return $trimmed
}

function Get-LatestCycleReport([datetime]$afterUtc) {
    $dir = Join-Path $env:LOCALAPPDATA "CryptoDayTraderSuite\automode\cycle_reports"
    if (-not (Test-Path $dir)) { return $null }

    $candidate = Get-ChildItem -Path $dir -Filter "cycle_*.json" -File -ErrorAction SilentlyContinue |
        Where-Object { $_.LastWriteTimeUtc -gt $afterUtc } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    return $candidate
}

function Wait-ForCycleReport {
    param(
        [datetime]$AfterUtc,
        [int]$TimeoutSeconds,
        [int]$PollSeconds = 5
    )

    $deadline = [DateTime]::UtcNow.AddSeconds([Math]::Max(1, $TimeoutSeconds))
    while ([DateTime]::UtcNow -lt $deadline) {
        $report = Get-LatestCycleReport -afterUtc $AfterUtc
        if ($null -ne $report) {
            return $report
        }

        Start-Sleep -Seconds ([Math]::Max(1, $PollSeconds))
    }

    return $null
}

Set-Location $ProjectRoot

$storeDir = Join-Path $env:LOCALAPPDATA "CryptoDayTraderSuite"
if (-not (Test-Path $storeDir)) {
    New-Item -Path $storeDir -ItemType Directory -Force | Out-Null
}

$accountsPath = Join-Path $storeDir "accounts.json"
$profilesPath = Join-Path $storeDir "automode_profiles.json"
$backupPath = Join-Path $storeDir ("automode_profiles.pre_b5_" + (Get-Date -Format "yyyyMMdd_HHmmss") + ".bak.json")

if (-not (Test-Path $accountsPath)) {
    throw "accounts.json not found at $accountsPath. Create at least one account before running B5 scenario."
}

$accountsRaw = Get-Content -Path $accountsPath -Raw
$accountsParsed = $accountsRaw | ConvertFrom-Json
$accounts = New-Object System.Collections.Generic.List[object]
if ($null -ne $accountsParsed) {
    if ($accountsParsed -is [System.Collections.IEnumerable] -and -not ($accountsParsed -is [string])) {
        foreach ($entry in $accountsParsed) {
            if ($null -ne $entry) {
                [void]$accounts.Add($entry)
            }
        }
    }
    else {
        [void]$accounts.Add($accountsParsed)
    }
}
if ($accounts.Count -eq 0) {
    throw "No accounts found in $accountsPath. Create at least one account before running B5 scenario."
}

$enabled = @($accounts | Where-Object { $_.Enabled -ne $false })
$paperPreferred = @($enabled | Where-Object { [string]::Equals([string]$_.Mode, "Paper", [System.StringComparison]::OrdinalIgnoreCase) -or $_.Paper -eq $true })
$primaryAccount = if ($paperPreferred.Count -gt 0) { $paperPreferred[0] } elseif ($enabled.Count -gt 0) { $enabled[0] } else { $accounts[0] }

if (-not $primaryAccount.Id) {
    throw "Selected account has no Id in accounts.json"
}

$availableAccountIds = @($accounts |
    ForEach-Object { Resolve-NormalizedAccountId -RawId $_.Id } |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

$primaryAccountId = Resolve-NormalizedAccountId -RawId $primaryAccount.Id
if ([string]::IsNullOrWhiteSpace($primaryAccountId)) {
    throw "Selected account id is empty/invalid after normalization."
}

if (-not ($availableAccountIds -contains $primaryAccountId)) {
    throw "Normalized primary account id '$primaryAccountId' does not match any account id from accounts.json."
}

if (Test-Path $profilesPath) {
    Copy-Item -Path $profilesPath -Destination $backupPath -Force
    Write-Info "Backed up existing auto profiles to $backupPath"
}

$selected3 = @("BTC-USD", "ETH-USD", "SOL-USD")
$selected15 = @(
    "BTC-USD", "ETH-USD", "SOL-USD", "XRP-USD", "DOGE-USD",
    "ADA-USD", "AVAX-USD", "LINK-USD", "LTC-USD", "BCH-USD",
    "DOT-USD", "ATOM-USD", "UNI-USD", "MATIC-USD", "AAVE-USD"
)

$minimumDeterministicSoakSeconds = 240
if ($SoakSeconds -lt $minimumDeterministicSoakSeconds) {
    Write-Info "Requested SoakSeconds=$SoakSeconds is below deterministic B5 minimum ($minimumDeterministicSoakSeconds). Bumping to $minimumDeterministicSoakSeconds seconds."
    $SoakSeconds = $minimumDeterministicSoakSeconds
}

$minimumDeterministicMaxSymbols = 15
if ($MaxSymbols -lt $minimumDeterministicMaxSymbols) {
    Write-Info "Requested MaxSymbols=$MaxSymbols is below deterministic B5 minimum ($minimumDeterministicMaxSymbols). Bumping to $minimumDeterministicMaxSymbols symbols."
    $MaxSymbols = $minimumDeterministicMaxSymbols
}

$assemblyPath = Join-Path $ProjectRoot "bin\Debug\CryptoDayTraderSuite.exe"
if (-not (Test-Path $assemblyPath)) {
    throw "Assembly not found for profile seeding: $assemblyPath"
}
[void][System.Reflection.Assembly]::LoadFrom((Resolve-Path $assemblyPath).Path)

$scenarioProfiles = New-Object 'System.Collections.Generic.List[CryptoDayTraderSuite.Models.AutoModeProfile]'

function New-B5Profile {
    param(
        [string]$ProfileId,
        [string]$Name,
        [string]$AccountId,
        [string]$PairScope,
        [string[]]$SelectedPairs,
        [int]$IntervalMinutes,
        [int]$MaxTradesPerCycle,
        [int]$CooldownMinutes,
        [decimal]$DailyRiskStopPct
    )

    $profile = New-Object CryptoDayTraderSuite.Models.AutoModeProfile
    $profile.ProfileId = $ProfileId
    $profile.Name = $Name
    $profile.AccountId = $AccountId
    $profile.Enabled = $true
    $profile.PairScope = $PairScope
    $profile.SelectedPairs = New-Object 'System.Collections.Generic.List[string]'
    foreach ($pair in ($SelectedPairs | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        [void]$profile.SelectedPairs.Add($pair)
    }
    $profile.IntervalMinutes = $IntervalMinutes
    $profile.MaxTradesPerCycle = $MaxTradesPerCycle
    $profile.CooldownMinutes = $CooldownMinutes
    $profile.DailyRiskStopPct = $DailyRiskStopPct
    $profile.CreatedUtc = [DateTime]::UtcNow
    $profile.UpdatedUtc = [DateTime]::UtcNow
    return $profile
}

[void]$scenarioProfiles.Add((New-B5Profile -ProfileId "b5_selected_3" -Name "B5 Selected 3" -AccountId $primaryAccountId -PairScope "Selected" -SelectedPairs $selected3 -IntervalMinutes 1 -MaxTradesPerCycle 1 -CooldownMinutes 15 -DailyRiskStopPct 1.5))
[void]$scenarioProfiles.Add((New-B5Profile -ProfileId "b5_all_scope" -Name "B5 All Scope" -AccountId $primaryAccountId -PairScope "All" -SelectedPairs @() -IntervalMinutes 1 -MaxTradesPerCycle 2 -CooldownMinutes 20 -DailyRiskStopPct 2.0))
[void]$scenarioProfiles.Add((New-B5Profile -ProfileId "b5_selected_15" -Name "B5 Selected 15" -AccountId $primaryAccountId -PairScope "Selected" -SelectedPairs $selected15 -IntervalMinutes 1 -MaxTradesPerCycle 3 -CooldownMinutes 30 -DailyRiskStopPct 3.0))
[void]$scenarioProfiles.Add((New-B5Profile -ProfileId "b5_failure_probe" -Name "B5 Failure Probe" -AccountId "missing-account-for-isolation" -PairScope "Selected" -SelectedPairs @("BTC-USD") -IntervalMinutes 1 -MaxTradesPerCycle 4 -CooldownMinutes 45 -DailyRiskStopPct 4.0))

$profileService = New-Object CryptoDayTraderSuite.Services.AutoModeProfileService
$profileService.ReplaceAll($scenarioProfiles)
$seededCount = @($profileService.GetAll()).Count
Write-Info "Seeded B5 profiles via AutoModeProfileService: count=$seededCount, account='$primaryAccountId'"

$settingsTypeName = "CryptoDayTraderSuite.Properties.Settings"
$settingsType = [AppDomain]::CurrentDomain.GetAssemblies() |
    ForEach-Object { $_.GetType($settingsTypeName, $false, $false) } |
    Where-Object { $_ -ne $null } |
    Select-Object -First 1

if ($null -eq $settingsType) {
    throw "Unable to resolve settings type '$settingsTypeName' from loaded assemblies."
}

$flagsStatic = [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Static
$flagsInstance = [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance

$defaultProperty = $settingsType.GetProperty("Default", $flagsStatic)
if ($null -eq $defaultProperty) {
    throw "Unable to resolve static Default property on '$settingsTypeName'."
}
$settings = $defaultProperty.GetValue($null, $null)
if ($null -eq $settings) {
    throw "Settings Default instance resolved to null for '$settingsTypeName'."
}

$autoRunProperty = $settingsType.GetProperty("AutoModeAutoRunEnabled", $flagsInstance)
if ($null -eq $autoRunProperty) {
    throw "Unable to resolve instance property AutoModeAutoRunEnabled on '$settingsTypeName'."
}
$autoRunProperty.SetValue($settings, $true, $null)

$saveMethod = $settingsType.GetMethod("Save", $flagsInstance)
if ($null -eq $saveMethod) {
    throw "Unable to resolve instance method Save() on '$settingsTypeName'."
}
[void]$saveMethod.Invoke($settings, $null)
Write-Info "Forced sticky auto run ON via project settings."

Get-Process CryptoDayTraderSuite -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

$env:CDTS_LOG_LEVEL = "debug"
$env:CDTS_AI_PROVIDER = "auto"
$env:CDTS_AI_MODEL = "auto"
$env:CDTS_AI_PROPOSER_MODE = "enabled"
$env:CDTS_AUTOMODE_MAX_SYMBOLS = [string]$MaxSymbols
$env:CDTS_ROUTING_DIAGNOSTICS_DISABLED = "1"

$exePath = $assemblyPath

$beforeUtc = (Get-Date).ToUniversalTime()
Write-Info "Launching app for soak run: $SoakSeconds seconds"
$p = Start-Process -FilePath $exePath -PassThru

$report = Wait-ForCycleReport -AfterUtc $beforeUtc -TimeoutSeconds $SoakSeconds -PollSeconds 5
if ($null -eq $report) {
    $graceSeconds = 120
    Write-Info "No cycle report observed within soak window; waiting an additional grace period ($graceSeconds s)."
    $report = Wait-ForCycleReport -AfterUtc $beforeUtc -TimeoutSeconds $graceSeconds -PollSeconds 5
}

if ($p -and -not $p.HasExited) {
    try { $null = $p.CloseMainWindow() } catch {}
    Start-Sleep -Seconds 4
}
if ($p -and -not $p.HasExited) {
    Stop-Process -Id $p.Id -Force
}

Start-Sleep -Seconds 1
if ($null -eq $report) {
    throw "No cycle report found after soak run. Ensure Auto Run is enabled at least once in UI."
}

Write-Info "Validating report: $($report.FullName)"
$validatorPath = Join-Path $ProjectRoot "Util\validate_automode_matrix.ps1"
if (-not (Test-Path $validatorPath)) {
    throw "Validator script missing: $validatorPath"
}

& powershell -NoProfile -ExecutionPolicy Bypass -File $validatorPath `
    -ReportPath $report.FullName `
    -RequireMixedScopes `
    -RequireSelectedSymbolCounts 3,15 `
    -RequireIndependentGuardrailConfigs `
    -RequireFailureIsolation
$validatorExit = $LASTEXITCODE

Write-Output ("RESULT:REPORT=" + $report.FullName)
Write-Output ("RESULT:VALIDATION_EXIT=" + $validatorExit)
Write-Output ("RESULT:PROFILE_BACKUP=" + $(if (Test-Path $backupPath) { $backupPath } else { "none" }))

if (-not $NoRestore -and (Test-Path $backupPath)) {
    Copy-Item -Path $backupPath -Destination $profilesPath -Force
    Write-Info "Restored original profiles from backup"
}

exit $validatorExit
