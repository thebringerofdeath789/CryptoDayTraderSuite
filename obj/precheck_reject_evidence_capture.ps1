param(
    [switch]$RequireAutoRunEnabled,
    [switch]$RequireRunnableProfile,
    [int]$FreshCycleWindowMinutes = 20,
    [string]$CycleDir = "$env:LOCALAPPDATA\CryptoDayTraderSuite\automode\cycle_reports",
    [string]$ProfileStorePath = "$env:LOCALAPPDATA\CryptoDayTraderSuite\automode_profiles.json",
    [string]$AccountsStorePath = "$env:LOCALAPPDATA\CryptoDayTraderSuite\accounts.json"
)

$ErrorActionPreference = 'Stop'

$CiVersion = '1'
$CiFields = 'result|decision|exit|autorun_known|autorun_enabled|runnable_profiles|fresh_cycle'

function Complete-Result {
    param(
        [string]$Result,
        [string]$Decision,
        [int]$ExitCode,
        [string]$Message
    )

    $resultText = if ([string]::IsNullOrWhiteSpace($Result)) { 'FAIL' } else { $Result.Trim().ToUpperInvariant() }
    $decisionText = if ([string]::IsNullOrWhiteSpace($Decision)) { 'unknown' } else { $Decision.Trim().ToLowerInvariant() }

    Write-Output ('CAPTURE_RESULT=' + $resultText)
    Write-Output ('CAPTURE_DECISION=' + $decisionText)
    Write-Output ('CI_VERSION=' + $CiVersion)
    Write-Output ('CI_FIELDS=' + $CiFields)
    Write-Output ('CI_SUMMARY=version=' + $CiVersion + ';result=' + $resultText + ';decision=' + $decisionText + ';exit=' + $ExitCode + ';autorun_known=' + ([int]$autoRunKnown) + ';autorun_enabled=' + $(if ($autoRunKnown) { [int]$autoRunEnabled } else { -1 }) + ';runnable_profiles=' + $runnableProfiles.Count + ';fresh_cycle=' + ([int]$hasFreshCycle))

    if (-not [string]::IsNullOrWhiteSpace($Message)) {
        Write-Output $Message
    }

    Write-Output ('RESULT_EXIT_CODE=' + $ExitCode)
    exit $ExitCode
}

function Find-AutoRunSettingConfig {
    $candidates = Get-ChildItem -Path $env:LOCALAPPDATA -Recurse -Filter user.config -ErrorAction SilentlyContinue
    foreach ($file in $candidates) {
        try {
            if (
                (Select-String -Path $file.FullName -Pattern 'CryptoDayTraderSuite.Properties.Settings' -SimpleMatch -Quiet -ErrorAction SilentlyContinue) -and
                (Select-String -Path $file.FullName -Pattern 'AutoModeAutoRunEnabled' -SimpleMatch -Quiet -ErrorAction SilentlyContinue)
            ) {
                return $file
            }
        }
        catch {
        }
    }

    return $null
}

function Read-AutoRunSettingValue {
    param([string]$ConfigPath)

    if ([string]::IsNullOrWhiteSpace($ConfigPath) -or -not (Test-Path $ConfigPath)) {
        return $null
    }

    try {
        [xml]$xml = Get-Content -Path $ConfigPath -Raw
        $node = $xml.SelectSingleNode("//userSettings/CryptoDayTraderSuite.Properties.Settings/setting[@name='AutoModeAutoRunEnabled']/value")
        if ($null -eq $node -or [string]::IsNullOrWhiteSpace($node.InnerText)) {
            return $null
        }

        $parsed = $false
        if ([bool]::TryParse($node.InnerText.Trim(), [ref]$parsed)) {
            return $parsed
        }

        return $null
    }
    catch {
        return $null
    }
}

function Read-JsonObject {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path $Path)) {
        return $null
    }

    try {
        return (Get-Content -Path $Path -Raw | ConvertFrom-Json)
    }
    catch {
        return $null
    }
}

$config = Find-AutoRunSettingConfig
$configPath = if ($null -ne $config) { $config.FullName } else { "" }
$autoRunEnabled = Read-AutoRunSettingValue -ConfigPath $configPath
$autoRunKnown = $null -ne $autoRunEnabled

$profileStore = Read-JsonObject -Path $ProfileStorePath
$profiles = @()
if ($null -ne $profileStore -and $null -ne $profileStore.Profiles) {
    $profiles = @($profileStore.Profiles)
    if ($profiles.Count -eq 1 -and $profiles[0] -and ($profiles[0].PSObject.Properties.Name -contains 'ProfileId')) {
        $profiles = @($profiles[0])
    }
}

$accountsStore = Read-JsonObject -Path $AccountsStorePath
$accounts = @()
if ($null -ne $accountsStore) {
    if ($accountsStore -is [System.Array]) {
        $accounts = @($accountsStore)
    }
    elseif ($accountsStore.PSObject.Properties.Name -contains 'Items') {
        $accounts = @($accountsStore.Items)
    }
    elseif ($accountsStore.PSObject.Properties.Name -contains 'Id') {
        $accounts = @($accountsStore)
    }
}

$enabledProfiles = @($profiles | Where-Object { $_.Enabled -eq $true -and -not [string]::IsNullOrWhiteSpace($_.AccountId) })
$enabledAccounts = @($accounts | Where-Object { $_.Enabled -eq $true -and -not [string]::IsNullOrWhiteSpace($_.Id) })
$enabledAccountIds = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
foreach ($acc in $enabledAccounts) {
    [void]$enabledAccountIds.Add($acc.Id.ToString())
}

$runnableProfiles = @($enabledProfiles | Where-Object { $enabledAccountIds.Contains($_.AccountId.ToString()) })
$unmatchedProfiles = @($enabledProfiles | Where-Object { -not $enabledAccountIds.Contains($_.AccountId.ToString()) })

$latestCycle = $null
if (Test-Path $CycleDir) {
    $latestCycle = Get-ChildItem -Path $CycleDir -Filter 'cycle_*.json' -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

$hasFreshCycle = $false
$cycleAgeMinutes = -1
if ($null -ne $latestCycle) {
    $cycleAgeMinutes = [int]([DateTime]::UtcNow - $latestCycle.LastWriteTimeUtc).TotalMinutes
    $hasFreshCycle = $cycleAgeMinutes -le $FreshCycleWindowMinutes
}

Write-Output ('PRECHECK_CONFIG_PATH=' + $configPath)
Write-Output ('PRECHECK_AUTORUN_KNOWN=' + ([int]$autoRunKnown))
Write-Output ('PRECHECK_AUTORUN_ENABLED=' + $(if ($autoRunKnown) { [int]$autoRunEnabled } else { -1 }))
Write-Output ('PRECHECK_PROFILE_STORE=' + $ProfileStorePath)
Write-Output ('PRECHECK_ACCOUNTS_STORE=' + $AccountsStorePath)
Write-Output ('PRECHECK_ENABLED_PROFILE_COUNT=' + $enabledProfiles.Count)
Write-Output ('PRECHECK_ENABLED_ACCOUNT_COUNT=' + $enabledAccounts.Count)
Write-Output ('PRECHECK_RUNNABLE_PROFILE_COUNT=' + $runnableProfiles.Count)
Write-Output ('PRECHECK_UNMATCHED_PROFILE_COUNT=' + $unmatchedProfiles.Count)
if ($unmatchedProfiles.Count -gt 0) {
    $unmatchedIds = @($unmatchedProfiles | ForEach-Object { $_.AccountId } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
    Write-Output ('PRECHECK_UNMATCHED_ACCOUNT_IDS=' + ($unmatchedIds -join ','))
}
Write-Output ('PRECHECK_CYCLE_DIR=' + $CycleDir)
Write-Output ('PRECHECK_LATEST_CYCLE=' + $(if ($null -ne $latestCycle) { $latestCycle.FullName } else { '' }))
Write-Output ('PRECHECK_LATEST_CYCLE_AGE_MIN=' + $cycleAgeMinutes)
Write-Output ('PRECHECK_HAS_FRESH_CYCLE=' + ([int]$hasFreshCycle))

if ($RequireAutoRunEnabled.IsPresent -and (-not $autoRunKnown -or -not $autoRunEnabled)) {
    Complete-Result -Result 'FAIL' -Decision 'fail-autorun-not-enabled' -ExitCode 2 -Message 'PRECHECK_RESULT=FAIL Auto Run persistence flag is not enabled.'
}

if ($RequireRunnableProfile.IsPresent -and $runnableProfiles.Count -lt 1) {
    Complete-Result -Result 'FAIL' -Decision 'fail-no-runnable-profile' -ExitCode 3 -Message 'PRECHECK_RESULT=FAIL No enabled Auto profile is bound to an enabled account.'
}

Complete-Result -Result 'PASS' -Decision 'pass' -ExitCode 0 -Message 'PRECHECK_RESULT=PASS'
