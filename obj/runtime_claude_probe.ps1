$ErrorActionPreference = 'Stop'

$repo = 'c:/Users/admin/Documents/Visual Studio 2022/Projects/CryptoDayTraderSuite'
$logRoot = Join-Path $env:LOCALAPPDATA 'CryptoDayTraderSuite/logs'

Get-Process CryptoDayTraderSuite -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process chrome -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

$chromeCandidates = @(
    'C:/Program Files/Google/Chrome/Application/chrome.exe',
    'C:/Program Files (x86)/Google/Chrome/Application/chrome.exe',
    (Join-Path $env:LOCALAPPDATA 'Google/Chrome/Application/chrome.exe')
)
$chromePath = $chromeCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $chromePath) {
    throw 'Chrome not found in standard locations.'
}

$userData = Join-Path $env:LOCALAPPDATA 'CryptoSidecar'
Start-Process -FilePath $chromePath -ArgumentList @('--remote-debugging-port=9222', "--user-data-dir=$userData", '--disable-session-crashed-bubble', '--no-first-run', '--no-default-browser-check', 'https://claude.ai/new') | Out-Null
Start-Sleep -Seconds 6

Add-Type -AssemblyName System.Core
$assemblyCandidates = @(
    (Join-Path $repo 'bin/Debug_PostFix/CryptoDayTraderSuite.exe'),
    (Join-Path $repo 'bin/Debug_Verify_Proof/CryptoDayTraderSuite.exe'),
    (Join-Path $repo 'bin/Debug_Verify/CryptoDayTraderSuite.exe'),
    (Join-Path $repo 'bin/Debug/CryptoDayTraderSuite.exe')
)
$assemblyPath = $assemblyCandidates | Where-Object { Test-Path $_ } | Sort-Object { (Get-Item $_).LastWriteTimeUtc } -Descending | Select-Object -First 1
if (-not $assemblyPath) {
    throw 'No CryptoDayTraderSuite.exe found in expected bin paths.'
}
[void][System.Reflection.Assembly]::LoadFrom((Resolve-Path $assemblyPath))
Write-Output ('ASSEMBLY_PATH=' + $assemblyPath)
Write-Output ('ASSEMBLY_UTC=' + (Get-Item $assemblyPath).LastWriteTimeUtc.ToString('o'))
$sidecar = New-Object CryptoDayTraderSuite.Services.ChromeSidecar

$connected = $sidecar.ConnectAsync('Claude').GetAwaiter().GetResult()
Write-Output ('CONNECTED=' + $connected)
if (-not $connected) {
    $sidecar.Dispose()
    exit 0
}

$allPassed = $true
for ($i = 1; $i -le 3; $i++) {
    $token = ('CDTS_RT_' + [Guid]::NewGuid().ToString('N').Substring(0, 8).ToUpperInvariant())
    $prompt = ('Return ONLY this exact token: ' + $token)
    $resp = $sidecar.QueryAIAsync($prompt).GetAwaiter().GetResult()

    $isEmpty = [string]::IsNullOrWhiteSpace($resp)
    $hasToken = $false
    if (-not $isEmpty) {
        $hasToken = $resp.Contains($token)
    }

    if ($isEmpty -or -not $hasToken) {
        $allPassed = $false
    }

    $preview = ''
    if (-not $isEmpty) {
        $preview = $resp.Substring(0, [Math]::Min(160, $resp.Length)).Replace("`r", ' ').Replace("`n", ' ')
    }

    Write-Output ('ATTEMPT_' + $i + '_TOKEN=' + $token)
    Write-Output ('ATTEMPT_' + $i + '_EMPTY=' + ($(if ($isEmpty) { 1 } else { 0 })))
    Write-Output ('ATTEMPT_' + $i + '_HAS_TOKEN=' + $hasToken)
    Write-Output ('ATTEMPT_' + $i + '_RESP_LEN=' + ($(if ($isEmpty) { 0 } else { $resp.Length })))
    Write-Output ('ATTEMPT_' + $i + '_PREVIEW=' + $preview)
}

$sidecar.Dispose()
Write-Output ('RUNTIME_MULTI_TOKEN_PASS=' + $allPassed)

function Test-JsonSchemaCompliance {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Sidecar,
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$Prompt,
        [Parameter(Mandatory = $true)]
        [string[]]$RequiredKeys
    )

    $raw = $Sidecar.QueryAIAsync($Prompt).GetAwaiter().GetResult()
    if ($null -eq $raw) { $raw = '' }
    $clean = $raw.Replace('```json', '').Replace('```JSON', '').Replace('```', '').Trim()

    $isArray = $clean.StartsWith('[')
    $isObject = $clean.StartsWith('{')
    $isEmpty = [string]::IsNullOrWhiteSpace($clean)
    $parseOk = $false
    $hasAllKeys = $false
    $missing = @()

    if (-not $isEmpty -and $isObject -and -not $isArray) {
        try {
            $obj = $clean | ConvertFrom-Json -ErrorAction Stop
            $parseOk = $true
            $propNames = @()
            if ($obj -ne $null) {
                $propNames = @($obj.PSObject.Properties.Name)
            }
            foreach ($k in $RequiredKeys) {
                if (-not ($propNames -contains $k)) {
                    $missing += $k
                }
            }
            $hasAllKeys = ($missing.Count -eq 0)
        }
        catch {
            $parseOk = $false
        }
    }

    Write-Output ('SCHEMA_' + $Name + '_EMPTY=' + ($(if ($isEmpty) { 1 } else { 0 })))
    Write-Output ('SCHEMA_' + $Name + '_TOPLEVEL_OBJECT=' + $isObject)
    Write-Output ('SCHEMA_' + $Name + '_TOPLEVEL_ARRAY=' + $isArray)
    Write-Output ('SCHEMA_' + $Name + '_PARSE_OK=' + $parseOk)
    Write-Output ('SCHEMA_' + $Name + '_HAS_ALL_KEYS=' + $hasAllKeys)
    Write-Output ('SCHEMA_' + $Name + '_MISSING_KEYS=' + ($missing -join ','))
    Write-Output ('SCHEMA_' + $Name + '_RESP_LEN=' + ($(if ($isEmpty) { 0 } else { $clean.Length })))
    Write-Output ('SCHEMA_' + $Name + '_PREVIEW=' + ($(if ($isEmpty) { '' } else { $clean.Substring(0, [Math]::Min(180, $clean.Length)).Replace("`r", ' ').Replace("`n", ' ') })))
}

$sidecar2 = New-Object CryptoDayTraderSuite.Services.ChromeSidecar
$connected2 = $sidecar2.ConnectAsync('Claude').GetAwaiter().GetResult()
Write-Output ('SCHEMA_CONNECTED=' + $connected2)
if ($connected2) {
    $reviewPrompt = 'Return ONLY one JSON object with EXACT keys bias, approve, reason, confidence, SuggestedLimit. No wrapper keys. No array. Wrap it exactly as CDTS_JSON_START{...}CDTS_JSON_END.'
    $governorPrompt = 'Return ONLY one JSON object with EXACT keys bias, reason, confidence. No wrapper keys. No array. Wrap it exactly as CDTS_JSON_START{...}CDTS_JSON_END.'

    Test-JsonSchemaCompliance -Sidecar $sidecar2 -Name 'REVIEW' -Prompt $reviewPrompt -RequiredKeys @('bias','approve','reason','confidence','SuggestedLimit')
    Test-JsonSchemaCompliance -Sidecar $sidecar2 -Name 'GOVERNOR' -Prompt $governorPrompt -RequiredKeys @('bias','reason','confidence')
}
$sidecar2.Dispose()

$latest = Get-ChildItem $logRoot -Filter 'log_*.txt' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($latest) {
    Write-Output ('LATEST_LOG=' + $latest.FullName)
    $patterns = @('Prompt injection result', 'Prompt injection failed', 'Prompt injection did not submit', 'ok:no-send', 'no-send', 'Response polling timed out', 'Provider response stalled')
    foreach ($pattern in $patterns) {
        $count = (Select-String -Path $latest.FullName -Pattern $pattern -SimpleMatch | Measure-Object).Count
        Write-Output ('LOG_COUNT_' + ($pattern.Replace(' ', '_').Replace(':', '_')) + '=' + $count)
    }
}
