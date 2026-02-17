$ErrorActionPreference = 'Stop'
Set-Location "c:\Users\admin\Documents\Visual Studio 2022\Projects\CryptoDayTraderSuite"

$reportDir = Join-Path $env:LOCALAPPDATA 'CryptoDayTraderSuite\automode\cycle_reports'
$logDir = Join-Path $env:LOCALAPPDATA 'CryptoDayTraderSuite\logs'
if (-not (Test-Path $reportDir)) { New-Item -ItemType Directory -Path $reportDir | Out-Null }
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir | Out-Null }

$beforeReportTime = Get-Date
$beforeLogTime = Get-Date

Get-Process CryptoDayTraderSuite -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

$env:CDTS_LOG_LEVEL = 'debug'
$env:CDTS_AI_PROVIDER = 'auto'
$env:CDTS_AI_MODEL = 'auto'
$env:CDTS_AUTOMODE_MAX_SYMBOLS = '12'

$p = Start-Process '.\bin\Debug\CryptoDayTraderSuite.exe' -PassThru
Start-Sleep -Seconds 330

if ($p -and -not $p.HasExited) {
    try { $null = $p.CloseMainWindow() } catch {}
    Start-Sleep -Seconds 4
}
if ($p -and -not $p.HasExited) {
    Stop-Process -Id $p.Id -Force
}
Start-Sleep -Seconds 2

$newReport = Get-ChildItem "$reportDir\cycle_*.json" -ErrorAction SilentlyContinue |
    Where-Object { $_.LastWriteTime -gt $beforeReportTime } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if (-not $newReport) {
    $newReport = Get-ChildItem "$reportDir\cycle_*.json" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

$newLog = Get-ChildItem "$logDir\log_*.txt" -ErrorAction SilentlyContinue |
    Where-Object { $_.LastWriteTime -gt $beforeLogTime } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if (-not $newLog) {
    $newLog = Get-ChildItem "$logDir\log_*.txt" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

if ($newReport) {
    $j = Get-Content $newReport.FullName -Raw | ConvertFrom-Json
    Write-Output ('NEW_REPORT=' + $newReport.FullName)
    Write-Output ('NEW_REPORT_TIME=' + $newReport.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))
    Write-Output ('REPORT_MATRIX=' + $j.MatrixStatus)
    Write-Output ('REPORT_PROFILES=' + $j.ProcessedProfileCount + '/' + $j.EnabledProfileCount)
    Write-Output ('REPORT_HAS_COVERAGE=' + ($j.PSObject.Properties.Name -contains 'MatrixMinimumProfileCoverage'))
} else {
    Write-Output 'NEW_REPORT=NONE'
}

if ($newLog) {
    Write-Output ('NEW_LOG=' + $newLog.FullName)
    Write-Output ('NEW_LOG_TIME=' + $newLog.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))
} else {
    Write-Output 'NEW_LOG=NONE'
}
