$ErrorActionPreference = 'Stop'

$logDir = Join-Path $env:LOCALAPPDATA 'CryptoDayTraderSuite\logs'
$logs = Get-ChildItem $logDir -Filter 'log_*.txt' | Sort-Object LastWriteTime -Descending | Select-Object -First 2
if (-not $logs -or $logs.Count -eq 0) {
    Write-Output 'NO_LOG'
    exit 1
}

Write-Output ('LOG_COUNT=' + $logs.Count)
$patterns = @(
    'Connected via CDP',
    'Prompt injection result: ok',
    'Model response captured',
    'AI proposer parse failed',
    'AI response parse failed from',
    'AI response parse failed (json and text fallback)',
    'AI review returned invalid contract; trade vetoed (fail-closed).',
    'AI proposer returned invalid contract; proposal rejected.',
    'AI response invalid contract from',
    'AI strict-json retry failed',
    'No AI tab found',
    'Connection failed',
    'CONNECT_TIMEOUT',
    'QUERY_TIMEOUT'
)

foreach ($log in $logs) {
    Write-Output ('LOG=' + $log.FullName)
    foreach ($p in $patterns) {
        $c = (Select-String -Path $log.FullName -Pattern $p -SimpleMatch | Measure-Object).Count
        Write-Output ($p + '=' + $c)
    }
}
