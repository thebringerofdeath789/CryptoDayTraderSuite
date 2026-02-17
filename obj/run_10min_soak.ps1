$ErrorActionPreference = 'SilentlyContinue'

Set-Location "c:\Users\admin\Documents\Visual Studio 2022\Projects\CryptoDayTraderSuite"

$before = Get-ChildItem "$env:LOCALAPPDATA\CryptoDayTraderSuite\logs\log_*.txt" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

$beforePath = if ($before) { $before.FullName } else { '' }
$beforeTime = if ($before) { $before.LastWriteTime } else { Get-Date '2000-01-01' }

Get-Process CryptoDayTraderSuite -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

$env:CDTS_LOG_LEVEL = 'debug'
$env:CDTS_AI_PROVIDER = 'auto'
$env:CDTS_AI_MODEL = 'auto'
$env:CDTS_AI_PROPOSER_MODE = 'enabled'
$env:CDTS_AUTOMODE_MAX_SYMBOLS = '12'

$exePath = '.\bin\Debug_Verify\CryptoDayTraderSuite.exe'
if (!(Test-Path $exePath)) {
    $exePath = '.\bin\Debug\CryptoDayTraderSuite.exe'
}

$p = Start-Process $exePath -PassThru
Start-Sleep -Seconds 605

if ($p -and -not $p.HasExited) {
    try { $null = $p.CloseMainWindow() } catch { }
    Start-Sleep -Seconds 4
}

if ($p -and -not $p.HasExited) {
    Stop-Process -Id $p.Id -Force
}

Start-Sleep -Seconds 1

$after = Get-ChildItem "$env:LOCALAPPDATA\CryptoDayTraderSuite\logs\log_*.txt" -ErrorAction SilentlyContinue |
    Where-Object { $_.LastWriteTime -gt $beforeTime } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $after) {
    $after = Get-ChildItem "$env:LOCALAPPDATA\CryptoDayTraderSuite\logs\log_*.txt" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

if (-not $after) {
    Write-Output 'RESULT:NO_LOG'
    exit 0
}

$f = $after.FullName

function CountPattern([string] $pattern, [bool] $simpleMatch = $true) {
    if ($simpleMatch) {
        return (Select-String -Path $f -Pattern $pattern -SimpleMatch | Measure-Object).Count
    }

    return (Select-String -Path $f -Pattern $pattern | Measure-Object).Count
}

Write-Output ('RESULT:BEFORE_LOG=' + $beforePath)
Write-Output ('RESULT:AFTER_LOG=' + $f)
Write-Output ('RESULT:CONNECTED=' + (CountPattern '[ChromeSidecar] Connected via CDP'))
Write-Output ('RESULT:DISCONNECTED=' + (CountPattern '[ChromeSidecar] Disconnected'))
Write-Output ('RESULT:INJECT_OK=' + (CountPattern 'Prompt injection result: ok'))
Write-Output ('RESULT:INJECT_FAIL=' + (CountPattern 'Prompt injection failed'))
Write-Output ('RESULT:NO_SEND=' + (CountPattern 'Prompt injection did not submit'))
Write-Output ('RESULT:NO_AI_TAB=' + (CountPattern 'No AI tab found'))
Write-Output ('RESULT:FALLBACK_TAB=' + (CountPattern 'Opened fallback tab'))
Write-Output ('RESULT:RESP_TIMEOUT=' + (CountPattern 'Response polling timed out'))
Write-Output ('RESULT:RESP_STALLED=' + (CountPattern 'Provider response stalled'))
Write-Output ('RESULT:AI_REVIEW=' + (CountPattern 'Starting AI review'))
Write-Output ('RESULT:AI_VETO=' + (CountPattern 'AI vetoed'))
Write-Output ('RESULT:GOV_CYCLE=' + (CountPattern '[AIGovernor] Starting analysis cycle'))
Write-Output ('RESULT:CYCLE_COMPLETE=' + (CountPattern 'Auto cycle complete'))
Write-Output ('RESULT:PAPER_FILL=' + (CountPattern 'paper fill'))
Write-Output ('RESULT:HTTP_429=' + (CountPattern '429' $false))

Select-String -Path $f -Pattern 'ChromeSidecar|AIGovernor|Starting AI review|AI vetoed|Auto cycle complete|paper fill|Prompt injection result|Prompt injection failed|Prompt injection did not submit|Response polling timed out|Provider response stalled|No AI tab found|Opened fallback tab|429' |
    Select-Object -Last 120 |
    ForEach-Object { $_.Line }