param(
    [string]$RepoRoot = ".",
    [string]$AssemblyPath,
    [string]$OutputRoot,
    [string[]]$Services = @("Coinbase", "Binance", "Bybit", "OKX", "Kraken"),
    [string]$PreferredSymbol = "BTC-USD"
)

$ErrorActionPreference = "Stop"

function Complete-Result {
    param(
        [int]$Code
    )

    Write-Host ('RESULT_EXIT_CODE=' + $Code)
    Write-Host ('PROBE_EXIT_CODE=' + $Code)
    exit $Code
}

trap {
    $message = if ($null -ne $_ -and $null -ne $_.Exception) { $_.Exception.Message } else { 'unknown error' }
    Write-Host ('PROBE_ERROR=' + $message)
    Write-Host 'PROBE_VERDICT=FAIL'
    Complete-Result -Code 1
}

function Resolve-RepoRoot {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "RepoRoot is required."
    }

    if (-not (Test-Path $Path)) {
        throw "RepoRoot path not found: $Path"
    }

    return (Resolve-Path $Path).Path
}

function Resolve-AssemblyPath {
    param(
        [string]$Repo,
        [string]$ExplicitPath
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        if (-not (Test-Path $ExplicitPath)) {
            throw "Assembly path not found: $ExplicitPath"
        }

        return (Resolve-Path $ExplicitPath).Path
    }

    $candidates = @(
        (Join-Path $Repo "bin\Debug_Verify\CryptoDayTraderSuite.exe"),
        (Join-Path $Repo "bin\Debug\CryptoDayTraderSuite.exe"),
        (Join-Path $Repo "bin\Release\CryptoDayTraderSuite.exe")
    ) | Where-Object { Test-Path $_ }

    if (-not $candidates -or $candidates.Count -eq 0) {
        throw "No CryptoDayTraderSuite assembly found in expected bin paths."
    }

    return ($candidates | Sort-Object { (Get-Item $_).LastWriteTimeUtc } -Descending | Select-Object -First 1)
}

function Get-FailureClass {
    param([string]$ErrorText)

    if ([string]::IsNullOrWhiteSpace($ErrorText)) {
        return "INTEGRATION-ERROR"
    }

    $text = $ErrorText.ToLowerInvariant()

    $envTokens = @(
        "restricted location",
        "451",
        "country",
        "cloudfront",
        "forbidden",
        "access denied",
        "geoblock",
        "geo-block",
        "not available in your region",
        "region"
    )

    foreach ($token in $envTokens) {
        if ($text.Contains($token)) {
            return "ENV-CONSTRAINT"
        }
    }

    $providerTokens = @(
        "http",
        "status",
        "timeout",
        "network",
        "socket",
        "rate limit",
        "429",
        "502",
        "503",
        "504",
        "gateway"
    )

    foreach ($token in $providerTokens) {
        if ($text.Contains($token)) {
            return "PROVIDER-ERROR"
        }
    }

    return "INTEGRATION-ERROR"
}

function Get-ProviderVerdict {
    param(
        [System.Collections.IList]$Results,
        [string[]]$RequiredServices
    )

    $normalizedRequired = @($RequiredServices | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim() })
    if ($normalizedRequired.Count -eq 0) { return "PARTIAL" }

    $missing = 0
    $hasIntegrationFailure = $false
    $hasNonPassFailure = $false

    foreach ($svc in $normalizedRequired) {
        $match = $Results | Where-Object { [string]::Equals([string]$_.Service, $svc, [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
        if ($null -eq $match) {
            $missing++
            continue
        }

        $ok = [bool]$match.PublicClientCreated -and [bool]$match.ProductDiscoverySucceeded -and [bool]$match.TickerSucceeded
        if (-not $ok) {
            $hasNonPassFailure = $true
            $failureClass = [string]$match.FailureClass
            if ([string]::IsNullOrWhiteSpace($failureClass) -or [string]::Equals($failureClass, "PASS", [System.StringComparison]::OrdinalIgnoreCase)) {
                $failureClass = Get-FailureClass -ErrorText ([string]$match.Error)
            }

            if ([string]::Equals($failureClass, "INTEGRATION-ERROR", [System.StringComparison]::OrdinalIgnoreCase)) {
                $hasIntegrationFailure = $true
            }
        }
    }

    if ($missing -gt 0) { return "PARTIAL" }
    if ($hasIntegrationFailure) { return "FAIL" }
    if ($hasNonPassFailure) { return "PARTIAL" }
    return "PASS"
}

$repo = Resolve-RepoRoot -Path $RepoRoot
Push-Location $repo
try {
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        $OutputRoot = Join-Path $repo "obj\runtime_reports\provider_audit"
    }

    if (-not (Test-Path $OutputRoot)) {
        New-Item -Path $OutputRoot -ItemType Directory -Force | Out-Null
    }

    $assemblyResolved = Resolve-AssemblyPath -Repo $repo -ExplicitPath $AssemblyPath
    [void][System.Reflection.Assembly]::LoadFrom($assemblyResolved)

    $keyService = New-Object CryptoDayTraderSuite.Services.KeyService
    $provider = New-Object CryptoDayTraderSuite.Services.ExchangeProvider($keyService)
    $auditService = New-Object CryptoDayTraderSuite.Services.ExchangeProviderAuditService($provider)

    $serviceList = @($Services | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim() })
    if ($serviceList.Count -eq 0) {
        throw "At least one service is required."
    }

    $typedServices = [string[]]$serviceList
    $results = $auditService.ValidatePublicApisAsync($typedServices, [string]$PreferredSymbol).GetAwaiter().GetResult()
    $resultRows = @($results)

    $timestampUtc = (Get-Date).ToUniversalTime()
    $stamp = $timestampUtc.ToString("yyyyMMdd_HHmmss")
    $verdict = Get-ProviderVerdict -Results $resultRows -RequiredServices $serviceList

    $summaryRows = @()
    foreach ($row in $resultRows) {
        $pass = [bool]$row.PublicClientCreated -and [bool]$row.ProductDiscoverySucceeded -and [bool]$row.TickerSucceeded
        $failureClass = if ($pass) { "PASS" } else { Get-FailureClass -ErrorText ([string]$row.Error) }
        $failureReason = if ($pass) { "" } else { [string]$row.Error }
        $summaryRows += [pscustomobject]@{
            Service = [string]$row.Service
            Status = if ($pass) { "PASS" } else { "FAIL" }
            FailureClass = $failureClass
            FailureReason = $failureReason
            PublicClientCreated = [bool]$row.PublicClientCreated
            ProductDiscoverySucceeded = [bool]$row.ProductDiscoverySucceeded
            TickerSucceeded = [bool]$row.TickerSucceeded
            SpotCoverage = [bool]$row.SpotCoverage
            PerpCoverage = [bool]$row.PerpCoverage
            ProbeSymbol = [string]$row.ProbeSymbol
            ProductCount = [int]$row.ProductCount
            SpotProductCount = [int]$row.SpotProductCount
            PerpProductCount = [int]$row.PerpProductCount
            DiscoveryLatencyMs = [int64]$row.DiscoveryLatencyMs
            TickerLatencyMs = [int64]$row.TickerLatencyMs
            Error = [string]$row.Error
        }
    }

    $normalizedRequired = @($serviceList | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim() })
    $requiredRows = @()
    $missingRequired = 0
    foreach ($svc in $normalizedRequired) {
        $match = $summaryRows | Where-Object { [string]::Equals([string]$_.Service, $svc, [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
        if ($null -eq $match) {
            $missingRequired++
        }
        else {
            $requiredRows += $match
        }
    }

    $passedCount = @($requiredRows | Where-Object { [string]::Equals([string]$_.FailureClass, "PASS", [System.StringComparison]::OrdinalIgnoreCase) }).Count
    $envConstraintCount = @($requiredRows | Where-Object { [string]::Equals([string]$_.FailureClass, "ENV-CONSTRAINT", [System.StringComparison]::OrdinalIgnoreCase) }).Count
    $providerErrorCount = @($requiredRows | Where-Object { [string]::Equals([string]$_.FailureClass, "PROVIDER-ERROR", [System.StringComparison]::OrdinalIgnoreCase) }).Count
    $integrationErrorCount = @($requiredRows | Where-Object { [string]::Equals([string]$_.FailureClass, "INTEGRATION-ERROR", [System.StringComparison]::OrdinalIgnoreCase) }).Count
    $spotCoverageCount = @($requiredRows | Where-Object { [bool]$_.SpotCoverage }).Count
    $perpCoverageCount = @($requiredRows | Where-Object { [bool]$_.PerpCoverage }).Count

    $report = [pscustomobject]@{
        ReportType = "ProviderPublicApiAudit"
        GeneratedUtc = $timestampUtc.ToString("o")
        Verdict = $verdict
        AssemblyPath = $assemblyResolved
        PreferredSymbol = $PreferredSymbol
        RequiredServices = $serviceList
        Summary = [pscustomobject]@{
            Passed = $passedCount
            EnvConstraint = $envConstraintCount
            ProviderError = $providerErrorCount
            IntegrationError = $integrationErrorCount
            SpotCoverage = $spotCoverageCount
            PerpCoverage = $perpCoverageCount
            MissingRequired = $missingRequired
            TotalRequired = $normalizedRequired.Count
        }
        Results = $summaryRows
    }

    $jsonPath = Join-Path $OutputRoot ("provider_public_api_probe_" + $stamp + ".json")
    $txtPath = Join-Path $OutputRoot ("provider_public_api_probe_" + $stamp + ".txt")

    $report | ConvertTo-Json -Depth 8 | Set-Content -Path $jsonPath -Encoding UTF8

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("Provider Public API Probe") | Out-Null
    $lines.Add("GeneratedUtc: " + $timestampUtc.ToString("o")) | Out-Null
    $lines.Add("Verdict: " + $verdict) | Out-Null
    $lines.Add("Assembly: " + $assemblyResolved) | Out-Null
    $lines.Add("") | Out-Null

    foreach ($row in $summaryRows) {
        $rowError = if ($null -eq $row.Error) { "" } else { [string]$row.Error }
        $lines.Add("- [" + $row.Status + "] " + $row.Service + " | create=" + $row.PublicClientCreated + " discover=" + $row.ProductDiscoverySucceeded + " ticker=" + $row.TickerSucceeded + " spot=" + $row.SpotCoverage + " perp=" + $row.PerpCoverage + " symbol=" + $row.ProbeSymbol + " products=" + $row.ProductCount + " (spot=" + $row.SpotProductCount + ",perp=" + $row.PerpProductCount + ") lat(ms)=" + $row.DiscoveryLatencyMs + "/" + $row.TickerLatencyMs + " error=" + $rowError + " failureClass=" + [string]$row.FailureClass) | Out-Null
    }

    $lines | Set-Content -Path $txtPath -Encoding UTF8

    Write-Host ("PROBE_JSON=" + $jsonPath)
    Write-Host ("PROBE_TXT=" + $txtPath)
    Write-Host ("PROBE_VERDICT=" + $verdict)

    if ($verdict -eq "FAIL") {
        Complete-Result -Code 1
    }

    Complete-Result -Code 0
}
finally {
    Pop-Location
}
