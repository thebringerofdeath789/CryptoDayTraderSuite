$p = Join-Path $PSScriptRoot 'runtime_claude_probe.ps1'
$raw = Get-Content $p -Raw
try {
    [void][ScriptBlock]::Create($raw)
    Write-Output 'PARSE_OK'
}
catch {
    Write-Output ('PARSE_ERR=' + $_.Exception.Message)
    exit 1
}
