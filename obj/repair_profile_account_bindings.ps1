param(
    [switch]$Apply,
    [string]$AccountId,
    [string]$ProfileStorePath = "$env:LOCALAPPDATA\CryptoDayTraderSuite\automode_profiles.json",
    [string]$AccountsStorePath = "$env:LOCALAPPDATA\CryptoDayTraderSuite\accounts.json"
)

$ErrorActionPreference = 'Stop'

function Read-JsonAny {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path $Path)) {
        return $null
    }

    return (Get-Content -Path $Path -Raw | ConvertFrom-Json)
}

function Normalize-Accounts {
    param($AccountsRaw)

    if ($null -eq $AccountsRaw) {
        return @()
    }

    if ($AccountsRaw -is [System.Array]) {
        return @($AccountsRaw)
    }

    if ($AccountsRaw.PSObject.Properties.Name -contains 'Items') {
        return @($AccountsRaw.Items)
    }

    if ($AccountsRaw.PSObject.Properties.Name -contains 'Id') {
        return @($AccountsRaw)
    }

    return @()
}

$profileStore = Read-JsonAny -Path $ProfileStorePath
if ($null -eq $profileStore -or $null -eq $profileStore.Profiles) {
    throw "Profile store missing or invalid: $ProfileStorePath"
}

$profiles = @($profileStore.Profiles)
if ($profiles.Count -eq 1 -and $profiles[0] -and ($profiles[0].PSObject.Properties.Name -contains 'ProfileId')) {
    $profiles = @($profiles[0])
}

$accountsRaw = Read-JsonAny -Path $AccountsStorePath
$accounts = Normalize-Accounts -AccountsRaw $accountsRaw

$enabledProfiles = @($profiles | Where-Object { $_.Enabled -eq $true -and -not [string]::IsNullOrWhiteSpace($_.AccountId) })
$enabledAccounts = @($accounts | Where-Object { $_.Enabled -eq $true -and -not [string]::IsNullOrWhiteSpace($_.Id) })

Write-Output ('REPAIR_PROFILE_STORE=' + $ProfileStorePath)
Write-Output ('REPAIR_ACCOUNTS_STORE=' + $AccountsStorePath)
Write-Output ('REPAIR_ENABLED_PROFILES=' + $enabledProfiles.Count)
Write-Output ('REPAIR_ENABLED_ACCOUNTS=' + $enabledAccounts.Count)

if ($enabledProfiles.Count -lt 1) {
    Write-Output 'REPAIR_RESULT=NOOP no enabled profiles.'
    exit 0
}

if ($enabledAccounts.Count -lt 1) {
    Write-Output 'REPAIR_RESULT=FAIL no enabled accounts available.'
    exit 2
}

$targetAccountId = $AccountId
if ([string]::IsNullOrWhiteSpace($targetAccountId)) {
    if ($enabledAccounts.Count -eq 1) {
        $targetAccountId = $enabledAccounts[0].Id.ToString()
    }
    else {
        Write-Output 'REPAIR_RESULT=FAIL multiple enabled accounts detected; pass -AccountId.'
        Write-Output ('REPAIR_ENABLED_ACCOUNT_IDS=' + (($enabledAccounts | ForEach-Object { $_.Id }) -join ','))
        exit 3
    }
}

$target = $enabledAccounts | Where-Object { [string]::Equals($_.Id.ToString(), $targetAccountId, [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
if ($null -eq $target) {
    Write-Output ('REPAIR_RESULT=FAIL target account not enabled or not found: ' + $targetAccountId)
    exit 4
}

$enabledAccountIds = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
foreach ($acc in $enabledAccounts) {
    [void]$enabledAccountIds.Add($acc.Id.ToString())
}

$updates = 0
foreach ($p in $enabledProfiles) {
    $current = $p.AccountId.ToString()
    if ($enabledAccountIds.Contains($current)) {
        continue
    }

    $profileName = if ($null -ne $p.Name -and -not [string]::IsNullOrWhiteSpace($p.Name.ToString())) { $p.Name.ToString() } else { $p.ProfileId.ToString() }
    Write-Output ('REPAIR_PLAN profile=' + $profileName + ' from=' + $current + ' to=' + $targetAccountId)
    if ($Apply.IsPresent) {
        $p.AccountId = $targetAccountId
        if ($p.PSObject.Properties.Name -contains 'UpdatedUtc') {
            $p.UpdatedUtc = [DateTime]::UtcNow
        }
    }
    $updates++
}

if (-not $Apply.IsPresent) {
    Write-Output ('REPAIR_PENDING_UPDATES=' + $updates)
    Write-Output 'REPAIR_RESULT=DRYRUN'
    exit 0
}

if ($updates -gt 0) {
    $backupPath = $ProfileStorePath + '.repair.bak'
    Copy-Item -Path $ProfileStorePath -Destination $backupPath -Force
    $json = $profileStore | ConvertTo-Json -Depth 16 -Compress
    Set-Content -Path $ProfileStorePath -Value $json -Encoding UTF8
    Write-Output ('REPAIR_BACKUP=' + $backupPath)
}

Write-Output ('REPAIR_APPLIED_UPDATES=' + $updates)
Write-Output 'REPAIR_RESULT=PASS'
exit 0
