$ErrorActionPreference = 'SilentlyContinue'

Set-Location "c:\Users\admin\Documents\Visual Studio 2022\Projects\CryptoDayTraderSuite"

function Get-ChromePath {
    $candidates = @(
        'C:\Program Files\Google\Chrome\Application\chrome.exe',
        'C:\Program Files (x86)\Google\Chrome\Application\chrome.exe',
        (Join-Path $env:LOCALAPPDATA 'Google\Chrome\Application\chrome.exe')
    )

    foreach ($path in $candidates) {
        if (Test-Path $path) { return $path }
    }

    return $null
}

function CountPattern {
    param(
        [string]$Path,
        [string]$Pattern,
        [bool]$SimpleMatch = $true
    )

    if ($SimpleMatch) {
        return (Select-String -Path $Path -Pattern $Pattern -SimpleMatch | Measure-Object).Count
    }

    return (Select-String -Path $Path -Pattern $Pattern | Measure-Object).Count
}

$before = Get-ChildItem "$env:LOCALAPPDATA\CryptoDayTraderSuite\logs\log_*.txt" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

$beforePath = if ($before) { $before.FullName } else { '' }
$beforeTime = if ($before) { $before.LastWriteTime } else { Get-Date '2000-01-01' }

Get-Process CryptoDayTraderSuite -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process chrome -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

$chromePath = Get-ChromePath
if (-not $chromePath) {
    Write-Output 'RESULT:CHROME_FOUND=0'
    Write-Output 'RESULT:PRECHECK_FAILED=chrome-not-found'
    exit 1
}

$userData = Join-Path $env:LOCALAPPDATA 'CryptoSidecar'
Start-Process -FilePath $chromePath -ArgumentList @(
    '--remote-debugging-port=9222',
    "--user-data-dir=$userData",
    'https://chatgpt.com/',
    'https://gemini.google.com/app',
    'https://claude.ai/new'
) | Out-Null

$cdpOk = $false
$aiTabs = 0
for ($i = 0; $i -lt 20; $i++) {
    Start-Sleep -Milliseconds 500
    try {
        $tabs = Invoke-RestMethod -UseBasicParsing 'http://127.0.0.1:9222/json'
        if ($tabs) {
            $cdpOk = $true
            $aiTabs = @($tabs | Where-Object {
                ($_.url -match 'chatgpt.com|gemini.google.com|claude.ai') -or
                ($_.title -match 'ChatGPT|Gemini|Claude')
            }).Count
            if ($aiTabs -gt 0) { break }
        }
    }
    catch { }
}

Write-Output ('RESULT:CHROME_FOUND=1')
Write-Output ('RESULT:CDP_OK=' + ([int]$cdpOk))
Write-Output ('RESULT:PRECHECK_AI_TABS=' + $aiTabs)

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

Write-Output ('RESULT:BEFORE_LOG=' + $beforePath)
Write-Output ('RESULT:AFTER_LOG=' + $f)
Write-Output ('RESULT:CONNECTED=' + (CountPattern -Path $f -Pattern '[ChromeSidecar] Connected via CDP'))
Write-Output ('RESULT:DISCONNECTED=' + (CountPattern -Path $f -Pattern '[ChromeSidecar] Disconnected'))
Write-Output ('RESULT:INJECT_OK=' + (CountPattern -Path $f -Pattern 'Prompt injection result: ok'))
Write-Output ('RESULT:INJECT_FAIL=' + (CountPattern -Path $f -Pattern 'Prompt injection failed'))
Write-Output ('RESULT:NO_SEND=' + (CountPattern -Path $f -Pattern 'Prompt injection did not submit'))
Write-Output ('RESULT:NO_AI_TAB=' + (CountPattern -Path $f -Pattern 'No AI tab found'))
Write-Output ('RESULT:FALLBACK_TAB=' + (CountPattern -Path $f -Pattern 'Opened fallback tab'))
Write-Output ('RESULT:RESP_TIMEOUT=' + (CountPattern -Path $f -Pattern 'Response polling timed out'))
Write-Output ('RESULT:RESP_STALLED=' + (CountPattern -Path $f -Pattern 'Provider response stalled'))
Write-Output ('RESULT:AI_REVIEW=' + (CountPattern -Path $f -Pattern 'Starting AI review'))
Write-Output ('RESULT:AI_VETO=' + (CountPattern -Path $f -Pattern 'AI vetoed'))
Write-Output ('RESULT:GOV_CYCLE=' + (CountPattern -Path $f -Pattern '[AIGovernor] Starting analysis cycle'))
Write-Output ('RESULT:CYCLE_COMPLETE=' + (CountPattern -Path $f -Pattern 'Auto cycle complete'))
Write-Output ('RESULT:PAPER_FILL=' + (CountPattern -Path $f -Pattern 'paper fill'))
Write-Output ('RESULT:HTTP_429=' + (CountPattern -Path $f -Pattern '429' -SimpleMatch $false))

Select-String -Path $f -Pattern 'ChromeSidecar|AIGovernor|Starting AI review|AI vetoed|Auto cycle complete|paper fill|Prompt injection result|Prompt injection failed|Prompt injection did not submit|Response polling timed out|Provider response stalled|No AI tab found|Opened fallback tab|429' |
    Select-Object -Last 120 |
    ForEach-Object { $_.Line }