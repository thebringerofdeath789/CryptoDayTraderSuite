param(
    [int]$Count = 4,
    [string]$LogRoot = "$env:LOCALAPPDATA\CryptoDayTraderSuite\logs",
    [switch]$RequireNo429,
    [switch]$RequireCycles,
    [switch]$RequireFills,
    [switch]$IgnoreStartupOnly
)

$ErrorActionPreference = 'Stop'

if ($Count -le 0) {
    Write-Output 'RESULT:FAIL Count must be > 0.'
    exit 2
}

if (-not (Test-Path $LogRoot)) {
    Write-Output ('RESULT:FAIL log root not found: ' + $LogRoot)
    exit 2
}

$logs = Get-ChildItem $LogRoot -Filter 'log_*.txt' -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First $Count

if (-not $logs -or $logs.Count -eq 0) {
    Write-Output 'RESULT:FAIL no log files found.'
    exit 2
}

$violations = New-Object System.Collections.Generic.List[string]
$totalCycles = 0
$totalFills = 0
$total429 = 0
$totalNoSignal = 0
$totalBiasBlocked = 0
$totalAiVeto = 0
$totalLimitApplied = 0

foreach ($log in $logs) {
    $path = $log.FullName
    $name = $log.Name

    $cycleCount = (Select-String -Path $path -Pattern 'Auto cycle complete' -SimpleMatch -ErrorAction SilentlyContinue | Measure-Object).Count
    $fillCount = (Select-String -Path $path -Pattern 'paper fill' -SimpleMatch -ErrorAction SilentlyContinue | Measure-Object).Count
    $http429Count = (Select-String -Path $path -Pattern '429' -ErrorAction SilentlyContinue | Measure-Object).Count
    $noSignalCount = (Select-String -Path $path -Pattern 'No live strategy signal available' -SimpleMatch -ErrorAction SilentlyContinue | Measure-Object).Count
    $biasBlockedCount = (Select-String -Path $path -Pattern 'Global Bias blocked|bias-blocked' -ErrorAction SilentlyContinue | Measure-Object).Count
    $aiVetoCount = (Select-String -Path $path -Pattern 'AI vetoed' -SimpleMatch -ErrorAction SilentlyContinue | Measure-Object).Count
    $limitAppliedCount = (Select-String -Path $path -Pattern 'Limiting All-scope symbols' -SimpleMatch -ErrorAction SilentlyContinue | Measure-Object).Count

    $totalCycles += $cycleCount
    $totalFills += $fillCount
    $total429 += $http429Count
    $totalNoSignal += $noSignalCount
    $totalBiasBlocked += $biasBlockedCount
    $totalAiVeto += $aiVetoCount
    $totalLimitApplied += $limitAppliedCount

    $isStartupOnly = ($cycleCount -eq 0 -and $fillCount -eq 0 -and $noSignalCount -eq 0 -and $biasBlockedCount -eq 0 -and $aiVetoCount -eq 0)

    Write-Output ("SNAPSHOT|{0}|cycles={1}|fills={2}|429={3}|noSignal={4}|biasBlock={5}|aiVeto={6}|limitApplied={7}" -f $name, $cycleCount, $fillCount, $http429Count, $noSignalCount, $biasBlockedCount, $aiVetoCount, $limitAppliedCount)

    if ($IgnoreStartupOnly -and $isStartupOnly) {
        continue
    }

    if ($RequireNo429 -and $http429Count -gt 0) {
        [void]$violations.Add($name + ': contains 429 entries')
    }
}

Write-Output ("SUMMARY|files={0}|cycles={1}|fills={2}|429={3}|noSignal={4}|biasBlock={5}|aiVeto={6}|limitApplied={7}" -f $logs.Count, $totalCycles, $totalFills, $total429, $totalNoSignal, $totalBiasBlocked, $totalAiVeto, $totalLimitApplied)

if ($RequireCycles -and $totalCycles -le 0) {
    [void]$violations.Add('aggregate: no completed cycles in sampled logs')
}

if ($RequireFills -and $totalFills -le 0) {
    [void]$violations.Add('aggregate: no fills in sampled logs')
}

if ($violations.Count -gt 0) {
    foreach ($item in $violations) {
        Write-Output ('VIOLATION|' + $item)
    }
    Write-Output 'RESULT:FAIL snapshot requirements not met.'
    exit 1
}

Write-Output 'RESULT:PASS snapshot requirements met.'
exit 0
