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

function Get-LatestCycleReport([datetime]$afterUtc) {
    $dir = Join-Path $env:LOCALAPPDATA "CryptoDayTraderSuite\automode\cycle_reports"
    if (-not (Test-Path $dir)) { return $null }

    $candidate = Get-ChildItem -Path $dir -Filter "cycle_*.json" -File -ErrorAction SilentlyContinue |
        Where-Object { $_.LastWriteTimeUtc -gt $afterUtc } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    return $candidate
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
$accounts = @($accountsRaw | ConvertFrom-Json)
if ($accounts.Count -eq 0) {
    throw "No accounts found in $accountsPath. Create at least one account before running B5 scenario."
}

$enabled = @($accounts | Where-Object { $_.Enabled -ne $false })
$paperPreferred = @($enabled | Where-Object { [string]::Equals([string]$_.Mode, "Paper", [System.StringComparison]::OrdinalIgnoreCase) -or $_.Paper -eq $true })
$primaryAccount = if ($paperPreferred.Count -gt 0) { $paperPreferred[0] } elseif ($enabled.Count -gt 0) { $enabled[0] } else { $accounts[0] }

if (-not $primaryAccount.Id) {
    throw "Selected account has no Id in accounts.json"
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

[void]$scenarioProfiles.Add((New-B5Profile -ProfileId "b5_selected_3" -Name "B5 Selected 3" -AccountId ([string]$primaryAccount.Id) -PairScope "Selected" -SelectedPairs $selected3 -IntervalMinutes 1 -MaxTradesPerCycle 1 -CooldownMinutes 15 -DailyRiskStopPct 1.5))
[void]$scenarioProfiles.Add((New-B5Profile -ProfileId "b5_all_scope" -Name "B5 All Scope" -AccountId ([string]$primaryAccount.Id) -PairScope "All" -SelectedPairs @() -IntervalMinutes 1 -MaxTradesPerCycle 2 -CooldownMinutes 20 -DailyRiskStopPct 2.0))
[void]$scenarioProfiles.Add((New-B5Profile -ProfileId "b5_selected_15" -Name "B5 Selected 15" -AccountId ([string]$primaryAccount.Id) -PairScope "Selected" -SelectedPairs $selected15 -IntervalMinutes 1 -MaxTradesPerCycle 3 -CooldownMinutes 30 -DailyRiskStopPct 3.0))
[void]$scenarioProfiles.Add((New-B5Profile -ProfileId "b5_failure_probe" -Name "B5 Failure Probe" -AccountId "missing-account-for-isolation" -PairScope "All" -SelectedPairs @() -IntervalMinutes 1 -MaxTradesPerCycle 4 -CooldownMinutes 45 -DailyRiskStopPct 4.0))

$profileService = New-Object CryptoDayTraderSuite.Services.AutoModeProfileService
$profileService.ReplaceAll($scenarioProfiles)
$seededCount = @($profileService.GetAll()).Count
Write-Info "Seeded B5 profiles via AutoModeProfileService: count=$seededCount, account='$($primaryAccount.Id)'"

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

$exePath = $assemblyPath

$beforeUtc = (Get-Date).ToUniversalTime()
Write-Info "Launching app for soak run: $SoakSeconds seconds"
$p = Start-Process -FilePath $exePath -PassThru
Start-Sleep -Seconds $SoakSeconds

if ($p -and -not $p.HasExited) {
    try { $null = $p.CloseMainWindow() } catch {}
    Start-Sleep -Seconds 4
}
if ($p -and -not $p.HasExited) {
    Stop-Process -Id $p.Id -Force
}

Start-Sleep -Seconds 1
$report = Get-LatestCycleReport -afterUtc $beforeUtc
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
