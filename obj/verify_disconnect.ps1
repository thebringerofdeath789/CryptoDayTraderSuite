$ErrorActionPreference = 'Stop'
function Get-ChromePath {
    $candidates = @(
        'C:\Program Files\Google\Chrome\Application\chrome.exe',
        'C:\Program Files (x86)\Google\Chrome\Application\chrome.exe',
        (Join-Path $env:LOCALAPPDATA 'Google\Chrome\Application\chrome.exe')
    )
    foreach ($path in $candidates) { if (Test-Path $path) { return $path } }
    return $null
}
function Get-LatestLogFile {
    param([string]$LogRoot)
    Get-ChildItem $LogRoot -Filter 'log_*.txt' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
}
function Wait-ForPattern {
    param([string]$LogFile,[string]$Pattern,[int]$TimeoutSeconds)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path $LogFile) {
            $hits = Select-String -Path $LogFile -Pattern $Pattern -CaseSensitive:$false -ErrorAction SilentlyContinue
            if ($hits) { return $true }
        }
        Start-Sleep -Seconds 1
    }
    return $false
}
$repo = Split-Path -Path $PSScriptRoot -Parent
$logRoot = Join-Path $env:LOCALAPPDATA 'CryptoDayTraderSuite\logs'
if (-not (Test-Path $logRoot)) { New-Item -ItemType Directory -Path $logRoot | Out-Null }
Get-Process CryptoDayTraderSuite -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1
$chromePath = Get-ChromePath
if (-not $chromePath) { Write-Output 'RESULT:FAIL chrome.exe not found in standard install paths.'; exit 2 }
$userData = Join-Path $env:LOCALAPPDATA 'CryptoSidecar'
Get-Process chrome -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1
Start-Process -FilePath $chromePath -ArgumentList @('--remote-debugging-port=9222', "--user-data-dir=$userData", 'https://chatgpt.com') | Out-Null
Start-Sleep -Seconds 4
$env:CDTS_LOG_LEVEL = 'debug'
$appPath = Join-Path $repo 'bin\Debug\CryptoDayTraderSuite.exe'
$appProc = Start-Process -FilePath $appPath -PassThru
Start-Sleep -Seconds 2
$latestLog = Get-LatestLogFile -LogRoot $logRoot
if (-not $latestLog) {
    if ($appProc -and -not $appProc.HasExited) { Stop-Process -Id $appProc.Id -Force }
    Write-Output 'RESULT:FAIL no log file found.'
    exit 2
}
$logPath = $latestLog.FullName
$connected = Wait-ForPattern -LogFile $logPath -Pattern '\[ChromeSidecar\].*Connected via CDP' -TimeoutSeconds 45
Get-Process chrome -ErrorAction SilentlyContinue | Stop-Process -Force
$disconnected = Wait-ForPattern -LogFile $logPath -Pattern '\[ChromeSidecar\].*Disconnected' -TimeoutSeconds 45
$connectedCount = (Select-String -Path $logPath -Pattern '\[ChromeSidecar\].*Connected via CDP' -CaseSensitive:$false -ErrorAction SilentlyContinue | Measure-Object).Count
$disconnectedCount = (Select-String -Path $logPath -Pattern '\[ChromeSidecar\].*Disconnected' -CaseSensitive:$false -ErrorAction SilentlyContinue | Measure-Object).Count
Write-Output ('LOG=' + $logPath)
Write-Output ('CONNECTED_COUNT=' + $connectedCount)
Write-Output ('DISCONNECTED_COUNT=' + $disconnectedCount)
if ($appProc -and -not $appProc.HasExited) { Stop-Process -Id $appProc.Id -Force }
if ($connected -and $disconnected) { Write-Output 'RESULT:PASS sidecar connect/disconnect fail-safe evidence captured.'; exit 0 }
Write-Output ('RESULT:FAIL connected=' + $connected + ' disconnected=' + $disconnected)
exit 2
