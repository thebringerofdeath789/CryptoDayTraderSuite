param(
    [string]$RepoRoot = ".",
    [string]$OutputRoot,
    [string]$RowEvidenceDir,
    [string]$AutoModeReportsDir = (Join-Path $env:LOCALAPPDATA "CryptoDayTraderSuite\automode\cycle_reports"),
    [string]$LogRoot = (Join-Path $env:LOCALAPPDATA "CryptoDayTraderSuite\logs"),
    [int]$MaxMatrixArtifactAgeHours = 24,
    [int]$MaxProviderProbeAgeHours = 24,
    [int]$MaxRejectEvidenceAgeHours = 24,
    [switch]$Strict,
    [switch]$RequireBuildPass,
    [switch]$RequireMatrixPass,
    [switch]$RequireProviderArtifacts,
    [switch]$RequireRejectCategories
)

$ErrorActionPreference = "Stop"
$ciContractVersion = "1"
$ciFields = @(
    "CI_VERSION",
    "CI_FIELDS",
    "REPORT_JSON",
    "REPORT_TXT",
    "STRICT_REQUESTED",
    "STRICT_SWITCH",
    "STRICT_GATES",
    "STRICT_FAILURE_CLASS",
    "STRICT_POLICY_DECISION",
    "CI_SUMMARY",
    "STRICT_FAILURE_COUNT",
    "STRICT_FAILURE_NAMES",
    "VERDICT"
)
$ciFieldsText = ($ciFields -join ",")

function New-CheckResult {
    param(
        [string]$Name,
        [string]$Status,
        [string]$Detail
    )

    return [pscustomobject]@{
        Name = $Name
        Status = $Status
        Detail = $Detail
    }
}

function Add-CheckResult {
    param(
        [System.Collections.Generic.List[object]]$List,
        [object]$Result
    )

    $List.Add($Result) | Out-Null
}

function Get-Verdict {
    param(
        [System.Collections.Generic.List[object]]$Checks,
        [switch]$RequireStrict
    )

    $failed = @($Checks | Where-Object { $_.Status -eq "FAIL" }).Count
    $partial = @($Checks | Where-Object { $_.Status -eq "PARTIAL" }).Count

    if ($failed -gt 0) {
        return "FAIL"
    }

    if ($partial -gt 0) {
        return "PARTIAL"
    }

    return "PASS"
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

function Get-LatestCycleReport {
    param([string]$ReportsDir)

    if ([string]::IsNullOrWhiteSpace($ReportsDir) -or -not (Test-Path $ReportsDir)) {
        return $null
    }

    return Get-ChildItem -Path $ReportsDir -Filter "cycle_*.json" -File |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
}

function Get-LatestLog {
    param([string]$LogsDir)

    if ([string]::IsNullOrWhiteSpace($LogsDir) -or -not (Test-Path $LogsDir)) {
        return $null
    }

    return Get-ChildItem -Path $LogsDir -Filter "log_*.txt" -File |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
}

function Get-RecentLogs {
    param(
        [string]$LogsDir,
        [int]$MaxCount = 20
    )

    if ([string]::IsNullOrWhiteSpace($LogsDir) -or -not (Test-Path $LogsDir)) {
        return @()
    }

    return @(Get-ChildItem -Path $LogsDir -Filter "log_*.txt" -File |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First $MaxCount)
}

function Get-BestMatrixCycleReport {
    param([string]$ReportsDir)

    if ([string]::IsNullOrWhiteSpace($ReportsDir) -or -not (Test-Path $ReportsDir)) {
        return $null
    }

    $recent = @(Get-ChildItem -Path $ReportsDir -Filter "cycle_*.json" -File |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 200)

    foreach ($candidate in $recent) {
        $data = Try-ReadJson -Path $candidate.FullName
        if ($null -eq $data) { continue }
        if ([string]::Equals([string]$data.MatrixStatus, "PASS", [System.StringComparison]::OrdinalIgnoreCase)) {
            return [pscustomobject]@{ File = $candidate; Data = $data }
        }
    }

    if ($recent.Count -gt 0) {
        $fallback = $recent[0]
        return [pscustomobject]@{ File = $fallback; Data = (Try-ReadJson -Path $fallback.FullName) }
    }

    return $null
}

function Try-ReadJson {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path $Path)) {
        return $null
    }

    $raw = Get-Content -Path $Path -Raw
    if ([string]::IsNullOrWhiteSpace($raw)) {
        return $null
    }

    try {
        return $raw | ConvertFrom-Json
    }
    catch {
        return $null
    }
}

function Get-AgeHours {
    param([datetime]$UtcTimestamp)

    if ($UtcTimestamp -eq [datetime]::MinValue) {
        return [double]::PositiveInfinity
    }

    return [math]::Round(([datetime]::UtcNow - $UtcTimestamp).TotalHours, 2)
}

function Evaluate-EvidenceFreshness {
    param(
        [string]$Name,
        [string]$SourceLabel,
        [datetime]$SourceUtc,
        [int]$MaxAgeHours
    )

    if ([string]::IsNullOrWhiteSpace($SourceLabel) -or $SourceUtc -eq [datetime]::MinValue) {
        return New-CheckResult -Name $Name -Status "PARTIAL" -Detail "No evidence source found to evaluate freshness."
    }

    $ageHours = Get-AgeHours -UtcTimestamp $SourceUtc
    if ($ageHours -le [double]$MaxAgeHours) {
        return New-CheckResult -Name $Name -Status "PASS" -Detail ("Freshness OK: " + $SourceLabel + " age=" + $ageHours.ToString([System.Globalization.CultureInfo]::InvariantCulture) + "h (max=" + $MaxAgeHours.ToString([System.Globalization.CultureInfo]::InvariantCulture) + "h).")
    }

    return New-CheckResult -Name $Name -Status "PARTIAL" -Detail ("Evidence is stale: " + $SourceLabel + " age=" + $ageHours.ToString([System.Globalization.CultureInfo]::InvariantCulture) + "h exceeds max=" + $MaxAgeHours.ToString([System.Globalization.CultureInfo]::InvariantCulture) + "h.")
}

function New-ArtifactManifestEntry {
    param(
        [string]$Name,
        [string]$SourceClass,
        [string]$Path,
        [string]$Status,
        [double]$AgeHours,
        [datetime]$LastWriteTimeUtc
    )

    $hasPath = -not [string]::IsNullOrWhiteSpace($Path)
    $fileName = if ($hasPath) { [System.IO.Path]::GetFileName($Path) } else { "" }
    $exists = $hasPath -and (Test-Path $Path)
    $lastWriteUtcText = if ($LastWriteTimeUtc -eq [datetime]::MinValue) { "" } else { $LastWriteTimeUtc.ToString("o") }
    $resolvedStatus = if ([string]::IsNullOrWhiteSpace($Status)) { "UNKNOWN" } else { $Status }
    $resolvedSourceClass = if ([string]::IsNullOrWhiteSpace($SourceClass)) { "other" } else { $SourceClass }
    $diffKey = $Name + "|" + $resolvedSourceClass + "|" + $fileName

    return [pscustomobject]@{
        Name = $Name
        SourceClass = $resolvedSourceClass
        DiffKey = $diffKey
        FileName = $fileName
        Path = if ($hasPath) { $Path } else { "" }
        Exists = $exists
        Status = $resolvedStatus
        AgeHours = $AgeHours
        LastWriteUtc = $lastWriteUtcText
    }
}

function Get-ArtifactManifestHash {
    param([object[]]$Entries)

    if ($null -eq $Entries -or @($Entries).Count -eq 0) {
        return ""
    }

    $lines = New-Object System.Collections.Generic.List[string]
    foreach ($entry in @($Entries)) {
        if ($null -eq $entry) { continue }

        $name = [string]$entry.Name
        $sourceClass = [string]$entry.SourceClass
        $diffKey = [string]$entry.DiffKey
        $path = [string]$entry.Path
        $status = [string]$entry.Status
        $ageHours = [double]$entry.AgeHours
        $ageText = if ([double]::IsInfinity($ageHours)) { "inf" } else { $ageHours.ToString("0.####", [System.Globalization.CultureInfo]::InvariantCulture) }
        $lastWriteUtc = [string]$entry.LastWriteUtc
        $exists = [string]([bool]$entry.Exists)

        $lines.Add($name + "|" + $sourceClass + "|" + $diffKey + "|" + $status + "|" + $exists + "|" + $ageText + "|" + $lastWriteUtc + "|" + $path) | Out-Null
    }

    $payload = ($lines.ToArray() -join "`n")
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($payload)
        $hash = $sha.ComputeHash($bytes)
        return ([System.BitConverter]::ToString($hash)).Replace("-", "").ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
    }
}

function Get-RejectCounts {
    param([string]$LogPath)

    $categories = @("fees-kill", "slippage-kill", "routing-unavailable", "no-signal", "ai-veto", "bias-blocked")
    $result = @{}
    foreach ($category in $categories) {
        $result[$category] = 0
    }

    if ([string]::IsNullOrWhiteSpace($LogPath) -or -not (Test-Path $LogPath)) {
        return $result
    }

    foreach ($category in $categories) {
        $pattern = [regex]::Escape($category)
        $count = (Select-String -Path $LogPath -Pattern $pattern -CaseSensitive:$false | Measure-Object).Count
        $result[$category] = $count
    }

    return $result
}

function Merge-RejectCountsFromCycleReport {
    param(
        [hashtable]$Current,
        [object]$CycleReport
    )

    $categories = @("fees-kill", "slippage-kill", "routing-unavailable", "no-signal", "ai-veto", "bias-blocked")
    $result = @{}
    foreach ($category in $categories) {
        $result[$category] = 0
    }

    if ($null -ne $Current) {
        foreach ($category in $categories) {
            if ($Current.ContainsKey($category)) {
                $result[$category] = [int]$Current[$category]
            }
        }
    }

    if ($null -eq $CycleReport) {
        return [pscustomobject]@{ Counts = $result; Applied = $false }
    }

    if (-not ($CycleReport.PSObject.Properties.Name -contains "RejectReasonCounts")) {
        return [pscustomobject]@{ Counts = $result; Applied = $false }
    }

    $source = $CycleReport.RejectReasonCounts
    if ($null -eq $source) {
        return [pscustomobject]@{ Counts = $result; Applied = $false }
    }

    foreach ($category in $categories) {
        $value = 0
        if ($source -is [System.Collections.IDictionary]) {
            if ($source.Contains($category) -or $source.ContainsKey($category)) {
                $value = [int]$source[$category]
            }
        }
        else {
            $property = $source.PSObject.Properties | Where-Object { [string]::Equals($_.Name, $category, [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
            if ($null -ne $property -and $null -ne $property.Value) {
                $value = [int]$property.Value
            }
        }

        if ($value -gt $result[$category]) {
            $result[$category] = $value
        }
    }

    return [pscustomobject]@{ Counts = $result; Applied = $true }
}

function Merge-RejectCountsAcrossLogs {
    param(
        [string]$LogsDir,
        [int]$MaxLogs = 20
    )

    $categories = @("fees-kill", "slippage-kill", "routing-unavailable", "no-signal", "ai-veto", "bias-blocked")
    $counts = @{}
    foreach ($category in $categories) {
        $counts[$category] = 0
    }

    $logs = Get-RecentLogs -LogsDir $LogsDir -MaxCount $MaxLogs
    $evidenceLog = ""

    foreach ($log in $logs) {
        if ($null -eq $log) { continue }
        $local = Get-RejectCounts -LogPath $log.FullName
        $localHit = $false
        foreach ($category in $categories) {
            $value = if ($local.ContainsKey($category)) { [int]$local[$category] } else { 0 }
            if ($value -gt $counts[$category]) {
                $counts[$category] = $value
            }
            if ($value -gt 0) {
                $localHit = $true
            }
        }

        if ($localHit -and [string]::IsNullOrWhiteSpace($evidenceLog)) {
            $evidenceLog = $log.Name
        }
    }

    if ([string]::IsNullOrWhiteSpace($evidenceLog) -and $logs.Count -gt 0) {
        $evidenceLog = $logs[0].Name
    }

    return [pscustomobject]@{
        Counts = $counts
        EvidenceLogName = $evidenceLog
        ScannedLogs = $logs.Count
    }
}

function Get-LatestProviderProbeReport {
    param([string]$RepoRootPath)

    $providerDir = Join-Path $RepoRootPath "obj\runtime_reports\provider_audit"
    if (-not (Test-Path $providerDir)) { return $null }

    return Get-ChildItem -Path $providerDir -Filter "provider_public_api_probe_*.json" -File |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
}

function Test-IsEnvironmentConstraintError {
    param([string]$ErrorText)

    if ([string]::IsNullOrWhiteSpace($ErrorText)) {
        return $false
    }

    $patterns = @(
        "restricted location",
        "eligibility",
        "blocked access from your country",
        "request could not be satisfied",
        "cloudfront",
        "http\s*451",
        "http\s*403"
    )

    foreach ($pattern in $patterns) {
        if ($ErrorText -match $pattern) {
            return $true
        }
    }

    return $false
}

function Evaluate-ProviderProbeStatus {
    param(
        [object]$ProbeReport,
        [string[]]$RequiredServices
    )

    if ($null -eq $ProbeReport) {
        return [pscustomobject]@{
            Status = "PARTIAL"
            Detail = "No provider probe artifact found."
            HasEnvironmentConstraints = $false
            HasIntegrationFailures = $false
            HasMissingServices = $false
        }
    }

    $rows = @($ProbeReport.Results)
    if ($rows.Count -eq 0) {
        return [pscustomobject]@{
            Status = "PARTIAL"
            Detail = "Provider probe artifact has no result rows."
            HasEnvironmentConstraints = $false
            HasIntegrationFailures = $false
            HasMissingServices = $false
        }
    }

    $required = @($RequiredServices | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim() })
    $missing = @()
    $integrationFailures = @()
    $constraintFailures = @()

    foreach ($svc in $required) {
        $row = $rows | Where-Object { [string]::Equals([string]$_.Service, $svc, [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
        if ($null -eq $row) {
            $missing += $svc
            continue
        }

        $serviceName = [string]$row.Service
        if ([string]::IsNullOrWhiteSpace($serviceName)) {
            $serviceName = $svc
        }
        if ([string]::IsNullOrWhiteSpace($serviceName)) {
            $serviceName = "Unknown"
        }

        $pass = [bool]$row.PublicClientCreated -and [bool]$row.ProductDiscoverySucceeded -and [bool]$row.TickerSucceeded
        if ($pass) {
            continue
        }

        $failureClass = [string]$row.FailureClass
        $errorText = [string]$row.Error

        if (-not [string]::IsNullOrWhiteSpace($failureClass)) {
            if ([string]::Equals($failureClass, "ENV-CONSTRAINT", [System.StringComparison]::OrdinalIgnoreCase)) {
                $constraintFailures += ($serviceName + "(ENV-CONSTRAINT)")
                continue
            }

            if ([string]::Equals($failureClass, "INTEGRATION-ERROR", [System.StringComparison]::OrdinalIgnoreCase)) {
                $integrationFailures += ($serviceName + "(INTEGRATION-ERROR)")
                continue
            }

            $constraintFailures += ($serviceName + "(" + $failureClass + ")")
            continue
        }

        if (Test-IsEnvironmentConstraintError -ErrorText $errorText) {
            $constraintFailures += ($serviceName + "(ENV-CONSTRAINT)")
        }
        else {
            $integrationFailures += ($serviceName + "(INTEGRATION-ERROR)")
        }
    }

    if ($missing.Count -gt 0) {
        return [pscustomobject]@{
            Status = "PARTIAL"
            Detail = ("Provider probe missing required services: " + ($missing -join ", "))
            HasEnvironmentConstraints = ($constraintFailures.Count -gt 0)
            HasIntegrationFailures = ($integrationFailures.Count -gt 0)
            HasMissingServices = $true
        }
    }

    if ($integrationFailures.Count -gt 0) {
        $detail = "Provider probe integration failures: " + ($integrationFailures -join ", ")
        if ($constraintFailures.Count -gt 0) {
            $detail += "; environment/provider constraints: " + ($constraintFailures -join ", ")
        }
        return [pscustomobject]@{
            Status = "FAIL"
            Detail = $detail
            HasEnvironmentConstraints = ($constraintFailures.Count -gt 0)
            HasIntegrationFailures = $true
            HasMissingServices = $false
        }
    }

    if ($constraintFailures.Count -gt 0) {
        return [pscustomobject]@{
            Status = "PARTIAL"
            Detail = ("Provider probe environment/provider constraints: " + ($constraintFailures -join ", "))
            HasEnvironmentConstraints = $true
            HasIntegrationFailures = $false
            HasMissingServices = $false
        }
    }

    return [pscustomobject]@{
        Status = "PASS"
        Detail = "All required provider probes passed."
        HasEnvironmentConstraints = $false
        HasIntegrationFailures = $false
        HasMissingServices = $false
    }
}

function Evaluate-SpotPerpCoverageStatus {
    param(
        [object]$ProbeReport,
        [string[]]$RequiredServices,
        [string[]]$RequiredPerpServices
    )

    if ($null -eq $ProbeReport) {
        return [pscustomobject]@{
            Status = "PARTIAL"
            Detail = "No provider probe artifact found for spot/perp coverage check."
            HasEnvironmentWaivers = $false
            HasMissingCoverage = $false
        }
    }

    $rows = @($ProbeReport.Results)
    if ($rows.Count -eq 0) {
        return [pscustomobject]@{
            Status = "PARTIAL"
            Detail = "Provider probe artifact has no result rows for spot/perp coverage check."
            HasEnvironmentWaivers = $false
            HasMissingCoverage = $false
        }
    }

    $missingSpot = @()
    $missingPerp = @()
    $waivedSpot = @()
    $waivedPerp = @()

    foreach ($svc in @($RequiredServices)) {
        if ([string]::IsNullOrWhiteSpace($svc)) { continue }
        $row = $rows | Where-Object { [string]::Equals([string]$_.Service, $svc, [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
        if ($null -eq $row) {
            $missingSpot += $svc
            continue
        }

        $failureClass = [string]($row | Select-Object -ExpandProperty FailureClass -ErrorAction SilentlyContinue)
        if ([string]::Equals($failureClass, "ENV-CONSTRAINT", [System.StringComparison]::OrdinalIgnoreCase)) {
            $waivedSpot += $svc
            continue
        }

        if (-not [bool]$row.SpotCoverage) {
            $missingSpot += $svc
        }
    }

    foreach ($svc in @($RequiredPerpServices)) {
        if ([string]::IsNullOrWhiteSpace($svc)) { continue }
        $row = $rows | Where-Object { [string]::Equals([string]$_.Service, $svc, [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
        if ($null -eq $row) {
            $missingPerp += $svc
            continue
        }

        $failureClass = [string]($row | Select-Object -ExpandProperty FailureClass -ErrorAction SilentlyContinue)
        if ([string]::Equals($failureClass, "ENV-CONSTRAINT", [System.StringComparison]::OrdinalIgnoreCase)) {
            $waivedPerp += $svc
            continue
        }

        $isBinanceSpotOnly = [string]::Equals($svc, "Binance", [System.StringComparison]::OrdinalIgnoreCase) `
            -and [bool]$row.SpotCoverage `
            -and (-not [bool]$row.PerpCoverage) `
            -and ([string]::IsNullOrWhiteSpace($failureClass) -or [string]::Equals($failureClass, "PASS", [System.StringComparison]::OrdinalIgnoreCase))
        if ($isBinanceSpotOnly) {
            $waivedPerp += ($svc + "(spot-only-endpoint)")
            continue
        }

        if (-not [bool]$row.PerpCoverage) {
            $missingPerp += $svc
        }
    }

    if ($missingSpot.Count -eq 0 -and $missingPerp.Count -eq 0 -and $waivedSpot.Count -eq 0 -and $waivedPerp.Count -eq 0) {
        return [pscustomobject]@{
            Status = "PASS"
            Detail = "Spot/perp coverage checks passed for required services."
            HasEnvironmentWaivers = $false
            HasMissingCoverage = $false
        }
    }

    if ($missingSpot.Count -eq 0 -and $missingPerp.Count -eq 0) {
        $waiverParts = @()
        if ($waivedSpot.Count -gt 0) {
            $waiverParts += ("spot-env-waived=" + ($waivedSpot -join ","))
        }
        if ($waivedPerp.Count -gt 0) {
            $waiverParts += ("perp-env-waived=" + ($waivedPerp -join ","))
        }
        return [pscustomobject]@{
            Status = "PARTIAL"
            Detail = ("Spot/perp coverage deferred by environment constraints: " + ($waiverParts -join " ; "))
            HasEnvironmentWaivers = $true
            HasMissingCoverage = $false
        }
    }

    $parts = @()
    if ($missingSpot.Count -gt 0) {
        $parts += ("spot-missing=" + ($missingSpot -join ","))
    }
    if ($missingPerp.Count -gt 0) {
        $parts += ("perp-missing=" + ($missingPerp -join ","))
    }
    if ($waivedSpot.Count -gt 0) {
        $parts += ("spot-env-waived=" + ($waivedSpot -join ","))
    }
    if ($waivedPerp.Count -gt 0) {
        $parts += ("perp-env-waived=" + ($waivedPerp -join ","))
    }

    return [pscustomobject]@{
        Status = "PARTIAL"
        Detail = ("Spot/perp coverage incomplete: " + ($parts -join " ; "))
        HasEnvironmentWaivers = (($waivedSpot.Count + $waivedPerp.Count) -gt 0)
        HasMissingCoverage = (($missingSpot.Count + $missingPerp.Count) -gt 0)
    }
}

function Normalize-Label {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    $trimmed = $Value.Trim().ToLowerInvariant()
    return ($trimmed -replace "[^a-z0-9]+", "")
}

function Normalize-RowStatus {
    param([string]$Status)

    if ([string]::IsNullOrWhiteSpace($Status)) {
        return "PARTIAL"
    }

    $value = $Status.Trim().ToUpperInvariant()
    if ($value -eq "PASS" -or $value -eq "FAIL" -or $value -eq "PARTIAL") {
        return $value
    }

    if ($value -eq "NOT-RUN" -or $value -eq "NOTRUN" -or $value -eq "UNKNOWN") {
        return "PARTIAL"
    }

    return "PARTIAL"
}

function Try-GetPropertyValue {
    param(
        [object]$Object,
        [string[]]$Names
    )

    if ($null -eq $Object) {
        return $null
    }

    foreach ($name in $Names) {
        if ([string]::IsNullOrWhiteSpace($name)) {
            continue
        }

        if ($Object.PSObject.Properties.Name -contains $name) {
            return $Object.$name
        }
    }

    return $null
}

function Add-StrategyExchangeEvidenceRow {
    param(
        [System.Collections.Generic.List[object]]$Rows,
        [object]$RawRow,
        [string]$ArtifactPath
    )

    if ($null -eq $RawRow) {
        return
    }

    $strategyValue = Try-GetPropertyValue -Object $RawRow -Names @("Strategy", "StrategyName")
    $exchangeValue = Try-GetPropertyValue -Object $RawRow -Names @("Exchange", "ExchangeName", "Venue")
    $statusValue = Try-GetPropertyValue -Object $RawRow -Names @("Status", "RowStatus", "Verdict")
    $detailValue = Try-GetPropertyValue -Object $RawRow -Names @("Detail", "Notes", "Reason", "Message")
    $evidenceRefValue = Try-GetPropertyValue -Object $RawRow -Names @("EvidenceRef", "EvidencePath", "ArtifactRef", "ArtifactPath", "SourcePath", "ReportPath")

    $strategy = [string]$strategyValue
    $exchange = [string]$exchangeValue
    if ([string]::IsNullOrWhiteSpace($strategy) -or [string]::IsNullOrWhiteSpace($exchange)) {
        return
    }

    $evidenceRef = [string]$evidenceRefValue
    if ([string]::IsNullOrWhiteSpace($evidenceRef)) {
        $evidenceRef = $ArtifactPath
    }

    $Rows.Add([pscustomobject]@{
        Strategy = $strategy.Trim()
        Exchange = $exchange.Trim()
        Status = Normalize-RowStatus -Status ([string]$statusValue)
        Detail = [string]$detailValue
        EvidenceRef = $evidenceRef
        EvidenceSource = $ArtifactPath
        StrategyKey = Normalize-Label -Value $strategy
        ExchangeKey = Normalize-Label -Value $exchange
    }) | Out-Null
}

function Get-StrategyExchangeEvidenceRows {
    param(
        [string]$RepoRootPath,
        [string]$AutoModeDir,
        [string]$EvidenceDir
    )

    $artifactPaths = New-Object System.Collections.Generic.List[string]

    if (-not [string]::IsNullOrWhiteSpace($EvidenceDir) -and (Test-Path $EvidenceDir)) {
        Get-ChildItem -Path $EvidenceDir -Filter "*.json" -File |
            Sort-Object LastWriteTimeUtc -Descending |
            ForEach-Object { $artifactPaths.Add($_.FullName) | Out-Null }
    }

    $defaultDirs = @(
        (Join-Path $RepoRootPath "obj\runtime_reports\strategy_exchange_evidence"),
        (Join-Path $RepoRootPath "obj\runtime_reports\multiexchange\row_evidence")
    )

    foreach ($dir in $defaultDirs) {
        if (Test-Path $dir) {
            Get-ChildItem -Path $dir -Filter "*.json" -File |
                Sort-Object LastWriteTimeUtc -Descending |
                ForEach-Object { $artifactPaths.Add($_.FullName) | Out-Null }
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($AutoModeDir) -and (Test-Path $AutoModeDir)) {
        Get-ChildItem -Path $AutoModeDir -Filter "cycle_*.json" -File |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 40 |
            ForEach-Object { $artifactPaths.Add($_.FullName) | Out-Null }
    }

    $uniquePaths = New-Object System.Collections.Generic.List[string]
    $seen = @{}
    foreach ($path in $artifactPaths) {
        if ([string]::IsNullOrWhiteSpace($path)) {
            continue
        }

        if (-not $seen.ContainsKey($path)) {
            $seen[$path] = $true
            $uniquePaths.Add($path) | Out-Null
        }
    }

    $rows = New-Object System.Collections.Generic.List[object]
    foreach ($artifactPath in $uniquePaths) {
        $json = Try-ReadJson -Path $artifactPath
        if ($null -eq $json) {
            continue
        }

        $reportType = [string](Try-GetPropertyValue -Object $json -Names @("ReportType"))
        if ([string]::Equals($reportType, "MultiExchangeCertification", [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        Add-StrategyExchangeEvidenceRow -Rows $rows -RawRow $json -ArtifactPath $artifactPath

        $collectionNames = @("StrategyExchangeRows", "StrategyExchangeMatrix", "MatrixRows", "Rows", "Results", "Entries")
        foreach ($collectionName in $collectionNames) {
            $collection = Try-GetPropertyValue -Object $json -Names @($collectionName)
            if ($null -eq $collection) {
                continue
            }

            foreach ($rawRow in @($collection)) {
                Add-StrategyExchangeEvidenceRow -Rows $rows -RawRow $rawRow -ArtifactPath $artifactPath
            }
        }
    }

    return $rows.ToArray()
}

function Convert-ToPolicyVenue {
    param([string]$Exchange)

    if ([string]::IsNullOrWhiteSpace($Exchange)) {
        return ""
    }

    if ([string]::Equals($Exchange, "Coinbase Advanced", [System.StringComparison]::OrdinalIgnoreCase)) {
        return "Coinbase"
    }

    return $Exchange.Trim()
}

function Get-PolicyAllowLookup {
    $lookup = @{}
    $lookup["vwaptrend"] = @("Coinbase", "Kraken", "Bitstamp", "Binance", "Bybit", "OKX")
    $lookup["orb"] = @("Coinbase", "Kraken", "Bitstamp", "Binance", "Bybit", "OKX")
    $lookup["rsireversion"] = @("Coinbase", "Kraken", "Bitstamp", "Binance", "Bybit", "OKX")
    $lookup["donchian"] = @("Coinbase", "Kraken", "Bitstamp", "Binance", "Bybit", "OKX")
    $lookup["fundingcarry"] = @("Binance", "Bybit", "OKX")
    $lookup["crossexchangespreaddivergence"] = @("Coinbase", "Kraken", "Bitstamp", "Binance", "Bybit", "OKX")
    return $lookup
}

function Get-PolicyBackedRowStatus {
    param(
        [string]$Strategy,
        [string]$Exchange,
        [hashtable]$PolicyAllowLookup,
        [object]$ProviderRow,
        [string]$ProviderArtifactName
    )

    $strategyKey = Normalize-Label -Value $Strategy
    $policyVenue = Convert-ToPolicyVenue -Exchange $Exchange

    $allowedOnVenue = $false
    if ($PolicyAllowLookup.ContainsKey($strategyKey)) {
        $allowedOnVenue = @($PolicyAllowLookup[$strategyKey] | Where-Object { [string]::Equals($_, $policyVenue, [System.StringComparison]::OrdinalIgnoreCase) }).Count -gt 0
    }

    if (-not $allowedOnVenue) {
        return [pscustomobject]@{
            Status = "FAIL"
            Detail = "Runtime policy matrix blocks strategy on venue (contract evidence)."
        }
    }

    if ($null -eq $ProviderRow) {
        return [pscustomobject]@{
            Status = "PARTIAL"
            Detail = "Provider probe row unavailable for venue; policy evidence only."
        }
    }

    $failureClass = [string](Try-GetPropertyValue -Object $ProviderRow -Names @("FailureClass"))
    if ([string]::Equals($failureClass, "INTEGRATION-ERROR", [System.StringComparison]::OrdinalIgnoreCase)) {
        return [pscustomobject]@{
            Status = "FAIL"
            Detail = "Provider integration error prevents venue evidence (" + $ProviderArtifactName + ")."
        }
    }

    if ([string]::Equals($failureClass, "ENV-CONSTRAINT", [System.StringComparison]::OrdinalIgnoreCase)) {
        return [pscustomobject]@{
            Status = "PARTIAL"
            Detail = "Venue blocked by environment constraints; deferred evidence (" + $ProviderArtifactName + ")."
        }
    }

    return [pscustomobject]@{
        Status = "PARTIAL"
        Detail = "Policy + provider evidence present; live row-level runtime/backtest evidence pending."
    }
}

function Emit-PolicyBackedStrategyExchangeEvidence {
    param(
        [string]$RepoRootPath,
        [string[]]$Strategies,
        [string[]]$Exchanges,
        [string]$Stamp
    )

    if ([string]::IsNullOrWhiteSpace($RepoRootPath)) {
        return $null
    }

    $providerArtifact = Get-LatestProviderProbeReport -RepoRootPath $RepoRootPath
    $providerJson = if ($null -ne $providerArtifact) { Try-ReadJson -Path $providerArtifact.FullName } else { $null }
    $providerRows = @()
    if ($null -ne $providerJson) {
        $providerRows = @($providerJson.Results)
    }
    $providerArtifactName = if ($null -ne $providerArtifact) { $providerArtifact.Name } else { "" }
    $providerArtifactPath = if ($null -ne $providerArtifact) { $providerArtifact.FullName } else { "" }

    $policyAllowLookup = Get-PolicyAllowLookup
    $outputDir = Join-Path $RepoRootPath "obj\runtime_reports\strategy_exchange_evidence"
    if (-not (Test-Path $outputDir)) {
        New-Item -Path $outputDir -ItemType Directory -Force | Out-Null
    }

    $rows = New-Object System.Collections.Generic.List[object]
    foreach ($strategy in @($Strategies)) {
        if ([string]::IsNullOrWhiteSpace($strategy)) { continue }

        foreach ($exchange in @($Exchanges)) {
            if ([string]::IsNullOrWhiteSpace($exchange)) { continue }

            $policyVenue = Convert-ToPolicyVenue -Exchange $exchange
            $providerRow = $providerRows | Where-Object { [string]::Equals([string]$_.Service, $policyVenue, [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
            $statusResult = Get-PolicyBackedRowStatus -Strategy $strategy -Exchange $exchange -PolicyAllowLookup $policyAllowLookup -ProviderRow $providerRow -ProviderArtifactName $providerArtifactName

            $rows.Add([pscustomobject]@{
                Strategy = $strategy
                Exchange = $exchange
                Status = [string]$statusResult.Status
                Detail = [string]$statusResult.Detail
                EvidenceRef = "Services/StrategyExchangePolicyService.cs"
            }) | Out-Null
        }
    }

    $artifact = [pscustomobject]@{
        ReportType = "StrategyExchangePolicyEvidence"
        GeneratedUtc = (Get-Date).ToUniversalTime().ToString("o")
        EvidenceKind = "policy-provider-derived"
        SourceArtifacts = @(
            "Services/StrategyExchangePolicyService.cs",
            $providerArtifactPath
        )
        StrategyExchangeRows = $rows
    }

    $safeStamp = if ([string]::IsNullOrWhiteSpace($Stamp)) { (Get-Date).ToUniversalTime().ToString("yyyyMMdd_HHmmss") } else { $Stamp }
    $path = Join-Path $outputDir ("strategy_exchange_policy_evidence_" + $safeStamp + ".json")
    $artifact | ConvertTo-Json -Depth 7 | Set-Content -Path $path -Encoding UTF8
    return $path
}

function Build-EvidenceBackedStrategyExchangeMatrix {
    param(
        [string[]]$Strategies,
        [string[]]$Exchanges,
        [object[]]$EvidenceRows
    )

    $rows = New-Object System.Collections.Generic.List[object]
    $missingCount = 0
    $evidencedCount = 0

    $lookup = @{}
    foreach ($evidenceRow in @($EvidenceRows)) {
        if ($null -eq $evidenceRow) {
            continue
        }

        $strategyKey = [string](Try-GetPropertyValue -Object $evidenceRow -Names @("StrategyKey"))
        if ([string]::IsNullOrWhiteSpace($strategyKey)) {
            $strategyKey = Normalize-Label -Value ([string](Try-GetPropertyValue -Object $evidenceRow -Names @("Strategy", "StrategyName")))
        }

        $exchangeKey = [string](Try-GetPropertyValue -Object $evidenceRow -Names @("ExchangeKey"))
        if ([string]::IsNullOrWhiteSpace($exchangeKey)) {
            $exchangeKey = Normalize-Label -Value ([string](Try-GetPropertyValue -Object $evidenceRow -Names @("Exchange", "ExchangeName", "Venue")))
        }

        if ([string]::IsNullOrWhiteSpace($strategyKey) -or [string]::IsNullOrWhiteSpace($exchangeKey)) {
            continue
        }

        $lookupKey = $strategyKey + "|" + $exchangeKey
        if (-not $lookup.ContainsKey($lookupKey)) {
            $lookup[$lookupKey] = $evidenceRow
            continue
        }

        $existing = $lookup[$lookupKey]
        $existingStatus = Normalize-RowStatus -Status ([string](Try-GetPropertyValue -Object $existing -Names @("Status", "RowStatus", "Verdict")))
        $candidateStatus = Normalize-RowStatus -Status ([string](Try-GetPropertyValue -Object $evidenceRow -Names @("Status", "RowStatus", "Verdict")))

        if ($existingStatus -ne "PASS" -and $candidateStatus -eq "PASS") {
            $lookup[$lookupKey] = $evidenceRow
        }
    }

    foreach ($strategy in $Strategies) {
        foreach ($exchange in $Exchanges) {
            $strategyKey = Normalize-Label -Value $strategy
            $exchangeKey = Normalize-Label -Value $exchange
            $lookupKey = $strategyKey + "|" + $exchangeKey

            $status = "PARTIAL"
            $detail = "Missing row-level evidence artifact for this strategy x exchange pair."
            $evidenceRef = ""
            $evidenceSource = ""

            if ($lookup.ContainsKey($lookupKey)) {
                $match = $lookup[$lookupKey]
                $status = Normalize-RowStatus -Status ([string](Try-GetPropertyValue -Object $match -Names @("Status", "RowStatus", "Verdict")))
                $detail = [string](Try-GetPropertyValue -Object $match -Names @("Detail", "Notes", "Reason", "Message"))
                $evidenceRef = [string](Try-GetPropertyValue -Object $match -Names @("EvidenceRef", "EvidencePath", "ArtifactRef", "ArtifactPath", "SourcePath", "ReportPath"))
                $evidenceSource = [string](Try-GetPropertyValue -Object $match -Names @("EvidenceSource", "SourcePath", "ArtifactPath"))

                if ([string]::IsNullOrWhiteSpace($evidenceRef)) {
                    $evidenceRef = $evidenceSource
                }

                if ([string]::IsNullOrWhiteSpace($detail)) {
                    $detail = "Derived from row evidence artifact."
                }

                $evidencedCount++
            }
            else {
                $missingCount++
            }

            $rows.Add([pscustomobject]@{
                Strategy = $strategy
                Exchange = $exchange
                Status = $status
                EvidenceRef = $evidenceRef
                EvidenceSource = $evidenceSource
                Detail = $detail
            }) | Out-Null
        }
    }

    return [pscustomobject]@{
        Rows = $rows.ToArray()
        MissingRowEvidenceCount = $missingCount
        EvidencedRowCount = $evidencedCount
        TotalRequiredRows = ($Strategies.Count * $Exchanges.Count)
    }
}

$repo = Resolve-RepoRoot -Path $RepoRoot
Push-Location $repo
try {
    $checks = New-Object System.Collections.Generic.List[object]
    $strictGateBuild = $Strict.IsPresent -or $RequireBuildPass.IsPresent
    $strictGateMatrix = $Strict.IsPresent -or $RequireMatrixPass.IsPresent
    $strictGateProvider = $Strict.IsPresent -or $RequireProviderArtifacts.IsPresent
    $strictGateReject = $Strict.IsPresent -or $RequireRejectCategories.IsPresent
    $strictRequested = $strictGateBuild -or $strictGateMatrix -or $strictGateProvider -or $strictGateReject

    $exchanges = @("Binance", "Coinbase Advanced", "Bybit", "OKX", "Kraken")
    $strategies = @("VWAP Trend", "ORB", "RSI Reversion", "Donchian", "Funding Carry", "Cross-Exchange Spread Divergence")

    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        $OutputRoot = Join-Path $repo "obj\runtime_reports\multiexchange"
    }

    if (-not (Test-Path $OutputRoot)) {
        New-Item -Path $OutputRoot -ItemType Directory -Force | Out-Null
    }

    $timestampUtc = (Get-Date).ToUniversalTime()
    $stamp = $timestampUtc.ToString("yyyyMMdd_HHmmss")

    $requiredFiles = @(
        "Services\ExchangeProviderAuditService.cs",
        "Services\SpreadDivergenceDetector.cs",
        "Services\FundingCarryDetector.cs",
        "Services\ExecutionVenueScorer.cs",
        "Services\SmartOrderRouter.cs",
        "Services\ExchangeCredentialPolicy.cs",
        "Exchanges\BinanceClient.cs",
        "Exchanges\BybitClient.cs",
        "Exchanges\OkxClient.cs",
        "Brokers\BinanceBroker.cs",
        "Brokers\BybitBroker.cs",
        "Brokers\OkxBroker.cs"
    )

    $missingFiles = @()
    foreach ($relative in $requiredFiles) {
        $full = Join-Path $repo $relative
        if (-not (Test-Path $full)) {
            $missingFiles += $relative
        }
    }

    if ($missingFiles.Count -eq 0) {
        Add-CheckResult -List $checks -Result (New-CheckResult -Name "Phase19 required files" -Status "PASS" -Detail "All required service/exchange/broker files present.")
    }
    else {
        Add-CheckResult -List $checks -Result (New-CheckResult -Name "Phase19 required files" -Status "FAIL" -Detail ("Missing: " + ($missingFiles -join ", ")))
    }

    $buildOutput = ""
    $buildExit = 1
    $verifyOutDir = "bin\\Debug_Verify\\"
    try {
        $buildOutput = & msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=$verifyOutDir /t:Build /v:minimal 2>&1 | Out-String
        $buildExit = $LASTEXITCODE
    }
    catch {
        $buildOutput = $_.Exception.Message
        $buildExit = 1
    }

    if ($buildExit -eq 0) {
        Add-CheckResult -List $checks -Result (New-CheckResult -Name "Build" -Status "PASS" -Detail ("Debug build succeeded (OutDir=" + $verifyOutDir + ")."))
    }
    else {
        $detail = "Debug build failed."
        if (-not [string]::IsNullOrWhiteSpace($buildOutput)) {
            $snippet = ($buildOutput -split "`r?`n" | Select-Object -Last 3) -join " | "
            if (-not [string]::IsNullOrWhiteSpace($snippet)) {
                $detail = $detail + " " + $snippet
            }
        }
        Add-CheckResult -List $checks -Result (New-CheckResult -Name "Build" -Status "FAIL" -Detail $detail)
    }

    $matrixCyclePick = Get-BestMatrixCycleReport -ReportsDir $AutoModeReportsDir
    $latestCycle = if ($null -ne $matrixCyclePick) { $matrixCyclePick.File } else { $null }
    $cycleData = if ($null -ne $matrixCyclePick) { $matrixCyclePick.Data } else { $null }

    $hasCycle = $null -ne $cycleData
    $matrixStatus = if ($hasCycle) { [string]$cycleData.MatrixStatus } else { "" }
    $matrixPass = $hasCycle -and [string]::Equals($matrixStatus, "PASS", [System.StringComparison]::OrdinalIgnoreCase)

    if (-not $hasCycle) {
        Add-CheckResult -List $checks -Result (New-CheckResult -Name "AutoMode matrix artifact" -Status "PARTIAL" -Detail "No cycle_*.json artifact found.")
    }
    elseif ($matrixPass) {
        Add-CheckResult -List $checks -Result (New-CheckResult -Name "AutoMode matrix artifact" -Status "PASS" -Detail ("MatrixStatus=PASS from " + $latestCycle.Name))
    }
    else {
        Add-CheckResult -List $checks -Result (New-CheckResult -Name "AutoMode matrix artifact" -Status "PARTIAL" -Detail ("MatrixStatus=" + $matrixStatus + " from " + $latestCycle.Name))
    }

    if ($null -ne $latestCycle) {
        Add-CheckResult -List $checks -Result (Evaluate-EvidenceFreshness -Name "AutoMode matrix artifact freshness" -SourceLabel $latestCycle.Name -SourceUtc $latestCycle.LastWriteTimeUtc -MaxAgeHours $MaxMatrixArtifactAgeHours)
    }
    else {
        Add-CheckResult -List $checks -Result (New-CheckResult -Name "AutoMode matrix artifact freshness" -Status "PARTIAL" -Detail "No matrix artifact found for freshness evaluation.")
    }

    $generatedPolicyEvidencePath = Emit-PolicyBackedStrategyExchangeEvidence -RepoRootPath $repo -Strategies $strategies -Exchanges $exchanges -Stamp $stamp
    if (-not [string]::IsNullOrWhiteSpace($generatedPolicyEvidencePath)) {
        Add-CheckResult -List $checks -Result (New-CheckResult -Name "Policy-backed row evidence artifact" -Status "PASS" -Detail ("Generated " + [System.IO.Path]::GetFileName($generatedPolicyEvidencePath)))
    }

    $rowEvidenceRows = Get-StrategyExchangeEvidenceRows -RepoRootPath $repo -AutoModeDir $AutoModeReportsDir -EvidenceDir $RowEvidenceDir
    $matrixBuild = Build-EvidenceBackedStrategyExchangeMatrix -Strategies $strategies -Exchanges $exchanges -EvidenceRows $rowEvidenceRows
    $matrixRows = @($matrixBuild.Rows)
    $missingRowEvidenceCount = [int]$matrixBuild.MissingRowEvidenceCount
    $totalRequiredRows = [int]$matrixBuild.TotalRequiredRows
    $evidencedRowCount = [int]$matrixBuild.EvidencedRowCount

    if ($missingRowEvidenceCount -eq 0 -and $totalRequiredRows -gt 0) {
        Add-CheckResult -List $checks -Result (New-CheckResult -Name "Strategy x exchange row evidence" -Status "PASS" -Detail ("Row evidence coverage complete (" + $evidencedRowCount + "/" + $totalRequiredRows + ")."))
    }
    else {
        $sampleMissing = @($matrixRows | Where-Object { [string]::IsNullOrWhiteSpace($_.EvidenceRef) } | Select-Object -First 3)
        $sampleText = ""
        if ($sampleMissing.Count -gt 0) {
            $sampleText = " Missing examples: " + (($sampleMissing | ForEach-Object { $_.Strategy + "@" + $_.Exchange }) -join ", ")
        }

        Add-CheckResult -List $checks -Result (New-CheckResult -Name "Strategy x exchange row evidence" -Status "PARTIAL" -Detail ("Row evidence coverage incomplete (" + $evidencedRowCount + "/" + $totalRequiredRows + ")." + $sampleText))

        if ($strictRequested) {
            Add-CheckResult -List $checks -Result (New-CheckResult -Name "Strict requirement: row evidence" -Status "FAIL" -Detail ("Strict mode requires evidence for every strategy x exchange row; missing=" + $missingRowEvidenceCount + "."))
        }
    }

    $requiredServices = @("Coinbase", "Binance", "Bybit", "OKX", "Kraken")
    $requiredPerpServices = @("Coinbase", "Binance", "Bybit")

    $providerReportFile = Get-LatestProviderProbeReport -RepoRootPath $repo
    $providerReport = if ($null -ne $providerReportFile) { Try-ReadJson -Path $providerReportFile.FullName } else { $null }
    $providerEval = Evaluate-ProviderProbeStatus -ProbeReport $providerReport -RequiredServices $requiredServices

    $providerStatus = [string]$providerEval.Status
    $providerDetail = [string]$providerEval.Detail
    $providerHasEnvConstraints = [bool]($providerEval | Select-Object -ExpandProperty HasEnvironmentConstraints -ErrorAction SilentlyContinue)
    $providerHasIntegrationFailures = [bool]($providerEval | Select-Object -ExpandProperty HasIntegrationFailures -ErrorAction SilentlyContinue)
    $providerHasMissingServices = [bool]($providerEval | Select-Object -ExpandProperty HasMissingServices -ErrorAction SilentlyContinue)
    if ($null -ne $providerReportFile) {
        $providerDetail += " Source=" + $providerReportFile.Name
    }

    if ($strictGateProvider -and $providerStatus -ne "PASS") {
        $allowAsBlocker = ($providerStatus -eq "PARTIAL") -and $providerHasEnvConstraints -and (-not $providerHasIntegrationFailures) -and (-not $providerHasMissingServices)
        if ($allowAsBlocker) {
            Add-CheckResult -List $checks -Result (New-CheckResult -Name "Geo/provider access blocker" -Status "PARTIAL" -Detail "Environment-constrained venue access detected; certification continues with blocker noted.")
        }
        else {
            $providerStatus = "FAIL"
        }
    }

    Add-CheckResult -List $checks -Result (New-CheckResult -Name "Provider public API probe artifacts" -Status $providerStatus -Detail $providerDetail)
    if ($null -ne $providerReportFile) {
        Add-CheckResult -List $checks -Result (Evaluate-EvidenceFreshness -Name "Provider probe artifact freshness" -SourceLabel $providerReportFile.Name -SourceUtc $providerReportFile.LastWriteTimeUtc -MaxAgeHours $MaxProviderProbeAgeHours)
    }
    else {
        Add-CheckResult -List $checks -Result (New-CheckResult -Name "Provider probe artifact freshness" -Status "PARTIAL" -Detail "No provider probe artifact found for freshness evaluation.")
    }

    $coverageEval = Evaluate-SpotPerpCoverageStatus -ProbeReport $providerReport -RequiredServices $requiredServices -RequiredPerpServices $requiredPerpServices
    $coverageStatus = [string]$coverageEval.Status
    $coverageDetail = [string]$coverageEval.Detail
    $coverageHasEnvironmentWaivers = [bool]($coverageEval | Select-Object -ExpandProperty HasEnvironmentWaivers -ErrorAction SilentlyContinue)
    $coverageHasMissingCoverage = [bool]($coverageEval | Select-Object -ExpandProperty HasMissingCoverage -ErrorAction SilentlyContinue)
    if ($null -ne $providerReportFile) {
        $coverageDetail += " Source=" + $providerReportFile.Name
    }
    if ($strictRequested -and $coverageStatus -ne "PASS") {
        $allowAsBlocker = ($coverageStatus -eq "PARTIAL") -and $coverageHasEnvironmentWaivers -and (-not $coverageHasMissingCoverage)
        if ($allowAsBlocker) {
            Add-CheckResult -List $checks -Result (New-CheckResult -Name "Spot/perp geo-access blocker" -Status "PARTIAL" -Detail "Spot/perp coverage deferred by environment-constrained venues; certification continues with blocker noted.")
        }
        else {
            $coverageStatus = "FAIL"
        }
    }

    Add-CheckResult -List $checks -Result (New-CheckResult -Name "Spot+perps coverage" -Status $coverageStatus -Detail $coverageDetail)

    $latestLog = Get-LatestLog -LogsDir $LogRoot
    if ($latestLog -is [System.Array]) {
        $latestLog = $latestLog | Select-Object -First 1
    }

    $latestCyclePath = ""
    if ($null -ne $latestCycle) {
        $latestCyclePath = [string]($latestCycle | Select-Object -ExpandProperty FullName -First 1)
    }

    $latestLogPath = ""
    if ($null -ne $latestLog) {
        $latestLogPath = [string]($latestLog | Select-Object -ExpandProperty FullName -First 1)
    }

    $recentLogMerge = Merge-RejectCountsAcrossLogs -LogsDir $LogRoot -MaxLogs 20
    $rejectCounts = $recentLogMerge.Counts
    $cycleRejectMerge = Merge-RejectCountsFromCycleReport -Current $rejectCounts -CycleReport $cycleData
    $rejectCounts = $cycleRejectMerge.Counts
    $rejectObserved = ($rejectCounts.Keys | Where-Object { $rejectCounts[$_] -gt 0 }).Count -gt 0
    $rejectEvidenceLog = [string]$recentLogMerge.EvidenceLogName
    if ([string]::IsNullOrWhiteSpace($rejectEvidenceLog) -and $null -ne $latestLog) {
        $rejectEvidenceLog = $latestLog.Name
    }

    if ($null -eq $latestLog) {
        Add-CheckResult -List $checks -Result (New-CheckResult -Name "Reject category evidence" -Status "PARTIAL" -Detail "No runtime log found for reject-category extraction.")
    }
    elseif ($rejectObserved) {
        $rejectSourceText = "Source=" + $rejectEvidenceLog
        if ($cycleRejectMerge.Applied) {
            $rejectSourceText += " + cycle"
        }
        Add-CheckResult -List $checks -Result (New-CheckResult -Name "Reject category evidence" -Status "PASS" -Detail ($rejectSourceText + ", observed=" + (($rejectCounts.Keys | Where-Object { $rejectCounts[$_] -gt 0 }) -join ", ")))
    }
    else {
        $status = if ($strictGateReject) { "FAIL" } else { "PARTIAL" }
        Add-CheckResult -List $checks -Result (New-CheckResult -Name "Reject category evidence" -Status $status -Detail ("Source=" + $rejectEvidenceLog + ", no tracked reject categories found."))
    }

    if ($null -ne $latestLog) {
        Add-CheckResult -List $checks -Result (Evaluate-EvidenceFreshness -Name "Reject evidence freshness" -SourceLabel $latestLog.Name -SourceUtc $latestLog.LastWriteTimeUtc -MaxAgeHours $MaxRejectEvidenceAgeHours)
    }
    else {
        Add-CheckResult -List $checks -Result (New-CheckResult -Name "Reject evidence freshness" -Status "PARTIAL" -Detail "No reject evidence log found for freshness evaluation.")
    }

    $matrixFreshnessStatus = "PARTIAL"
    $matrixFreshnessCheck = $checks | Where-Object { $_.Name -eq "AutoMode matrix artifact freshness" } | Select-Object -First 1
    if ($null -ne $matrixFreshnessCheck) {
        $matrixFreshnessStatus = [string]$matrixFreshnessCheck.Status
    }

    $providerFreshnessStatus = "PARTIAL"
    $providerFreshnessCheck = $checks | Where-Object { $_.Name -eq "Provider probe artifact freshness" } | Select-Object -First 1
    if ($null -ne $providerFreshnessCheck) {
        $providerFreshnessStatus = [string]$providerFreshnessCheck.Status
    }

    $rejectFreshnessStatus = "PARTIAL"
    $rejectFreshnessCheck = $checks | Where-Object { $_.Name -eq "Reject evidence freshness" } | Select-Object -First 1
    if ($null -ne $rejectFreshnessCheck) {
        $rejectFreshnessStatus = [string]$rejectFreshnessCheck.Status
    }

    $matrixArtifactAgeHours = if ($null -ne $latestCycle) { Get-AgeHours -UtcTimestamp $latestCycle.LastWriteTimeUtc } else { [double]::PositiveInfinity }
    $providerArtifactAgeHours = if ($null -ne $providerReportFile) { Get-AgeHours -UtcTimestamp $providerReportFile.LastWriteTimeUtc } else { [double]::PositiveInfinity }
    $rejectEvidenceAgeHours = if ($null -ne $latestLog) { Get-AgeHours -UtcTimestamp $latestLog.LastWriteTimeUtc } else { [double]::PositiveInfinity }
    $matrixArtifactSource = if ($null -ne $latestCycle) { $latestCycle.Name } else { "" }
    $providerArtifactSource = if ($null -ne $providerReportFile) { $providerReportFile.Name } else { "" }
    $rejectEvidenceSource = if ($null -ne $latestLog) { $latestLog.Name } else { "" }
    $policyEvidenceAgeHours = if (-not [string]::IsNullOrWhiteSpace($generatedPolicyEvidencePath) -and (Test-Path $generatedPolicyEvidencePath)) { Get-AgeHours -UtcTimestamp (Get-Item $generatedPolicyEvidencePath).LastWriteTimeUtc } else { [double]::PositiveInfinity }
    $matrixArtifactLastWriteUtc = if ($null -ne $latestCycle) { $latestCycle.LastWriteTimeUtc } else { [datetime]::MinValue }
    $providerArtifactPath = if ($null -ne $providerReportFile) { $providerReportFile.FullName } else { "" }
    $providerArtifactLastWriteUtc = if ($null -ne $providerReportFile) { $providerReportFile.LastWriteTimeUtc } else { [datetime]::MinValue }
    $rejectArtifactLastWriteUtc = if ($null -ne $latestLog) { $latestLog.LastWriteTimeUtc } else { [datetime]::MinValue }
    $policyArtifactLastWriteUtc = if (-not [string]::IsNullOrWhiteSpace($generatedPolicyEvidencePath) -and (Test-Path $generatedPolicyEvidencePath)) { (Get-Item $generatedPolicyEvidencePath).LastWriteTimeUtc } else { [datetime]::MinValue }

    $artifactManifest = @(
        (New-ArtifactManifestEntry -Name "matrix-cycle" -SourceClass "matrix" -Path $latestCyclePath -Status $matrixFreshnessStatus -AgeHours $matrixArtifactAgeHours -LastWriteTimeUtc $matrixArtifactLastWriteUtc),
        (New-ArtifactManifestEntry -Name "provider-probe" -SourceClass "provider" -Path $providerArtifactPath -Status $providerFreshnessStatus -AgeHours $providerArtifactAgeHours -LastWriteTimeUtc $providerArtifactLastWriteUtc),
        (New-ArtifactManifestEntry -Name "reject-log" -SourceClass "reject" -Path $latestLogPath -Status $rejectFreshnessStatus -AgeHours $rejectEvidenceAgeHours -LastWriteTimeUtc $rejectArtifactLastWriteUtc),
        (New-ArtifactManifestEntry -Name "policy-row-evidence" -SourceClass "policy" -Path $generatedPolicyEvidencePath -Status "PASS" -AgeHours $policyEvidenceAgeHours -LastWriteTimeUtc $policyArtifactLastWriteUtc)
    ) | Sort-Object Name
    $artifactManifestHash = Get-ArtifactManifestHash -Entries $artifactManifest

    $freshnessEntries = @(
        [pscustomobject]@{ Name = "matrix"; Status = $matrixFreshnessStatus; AgeHours = $matrixArtifactAgeHours; MaxAgeHours = $MaxMatrixArtifactAgeHours; Source = $matrixArtifactSource },
        [pscustomobject]@{ Name = "provider"; Status = $providerFreshnessStatus; AgeHours = $providerArtifactAgeHours; MaxAgeHours = $MaxProviderProbeAgeHours; Source = $providerArtifactSource },
        [pscustomobject]@{ Name = "reject"; Status = $rejectFreshnessStatus; AgeHours = $rejectEvidenceAgeHours; MaxAgeHours = $MaxRejectEvidenceAgeHours; Source = $rejectEvidenceSource }
    )

    $freshnessNonPassEntries = @($freshnessEntries | Where-Object { [string]$_.Status -ne "PASS" })
    $freshnessOverallStatus = if ($freshnessNonPassEntries.Count -eq 0) { "PASS" } else { "PARTIAL" }
    $freshnessNonPassSources = @($freshnessNonPassEntries | ForEach-Object { [string]$_.Name })

    if ($strictGateBuild -and ($checks | Where-Object { $_.Name -eq "Build" -and $_.Status -ne "PASS" })) {
        Add-CheckResult -List $checks -Result (New-CheckResult -Name "Strict requirement: build" -Status "FAIL" -Detail "RequireBuildPass was set and build is not PASS.")
    }

    if ($strictGateMatrix -and -not $matrixPass) {
        Add-CheckResult -List $checks -Result (New-CheckResult -Name "Strict requirement: matrix" -Status "FAIL" -Detail "RequireMatrixPass was set and matrix status is not PASS.")
    }

    if ($strictRequested) {
        $freshnessChecks = @(
            "AutoMode matrix artifact freshness",
            "Provider probe artifact freshness",
            "Reject evidence freshness"
        )

        foreach ($freshnessCheckName in $freshnessChecks) {
            $freshnessCheck = $checks | Where-Object { $_.Name -eq $freshnessCheckName } | Select-Object -First 1
            if ($null -ne $freshnessCheck -and [string]$freshnessCheck.Status -ne "PASS") {
                Add-CheckResult -List $checks -Result (New-CheckResult -Name ("Strict requirement: " + $freshnessCheckName) -Status "FAIL" -Detail ("Strict mode requires fresh evidence; " + $freshnessCheckName + " is " + [string]$freshnessCheck.Status + "."))
            }
        }
    }

    $verdict = Get-Verdict -Checks $checks -RequireStrict:($strictRequested)

    $checkRows = @()
    foreach ($check in $checks) {
        $checkRows += @{
            Name = [string]$check.Name
            Status = [string]$check.Status
            Detail = [string]$check.Detail
        }
    }

    $matrixCoverage = $false
    if ($hasCycle -and $cycleData.PSObject.Properties.Name -contains "MatrixMinimumProfileCoverage") {
        $matrixCoverage = [bool]$cycleData.MatrixMinimumProfileCoverage
    }

    $recommendedNextAction = "Fix FAIL checks first; re-run certification runner after remediation."
    $failedChecks = @($checks | Where-Object { [string]::Equals(([string]$_.Status).Trim(), "FAIL", [System.StringComparison]::OrdinalIgnoreCase) })
    $strictFailureChecks = @()
    if ($strictRequested) {
        foreach ($failedCheck in $failedChecks) {
            $failedName = ([string]$failedCheck.Name).Trim()
            $isStrictFailure = $false

            if ($failedName -like "Strict requirement:*") {
                $isStrictFailure = $true
            }
            elseif ($strictGateBuild -and [string]::Equals($failedName, "Build", [System.StringComparison]::OrdinalIgnoreCase)) {
                $isStrictFailure = $true
            }
            elseif ($strictGateMatrix -and [string]::Equals($failedName, "AutoMode matrix artifact", [System.StringComparison]::OrdinalIgnoreCase)) {
                $isStrictFailure = $true
            }
            elseif ($strictGateProvider -and (
                [string]::Equals($failedName, "Provider public API probe artifacts", [System.StringComparison]::OrdinalIgnoreCase) -or
                [string]::Equals($failedName, "Spot+perps coverage", [System.StringComparison]::OrdinalIgnoreCase)
            )) {
                $isStrictFailure = $true
            }
            elseif ($strictGateReject -and [string]::Equals($failedName, "Reject category evidence", [System.StringComparison]::OrdinalIgnoreCase)) {
                $isStrictFailure = $true
            }

            if ($isStrictFailure) {
                $strictFailureChecks += ,$failedCheck
            }
        }
    }
    $strictFailureNames = @($strictFailureChecks | ForEach-Object { ([string]$_.Name).Trim() } | Select-Object -Unique)
    $strictFailureCount = $strictFailureNames.Count
    if ($strictRequested -and $verdict -eq "FAIL" -and $strictFailureCount -eq 0 -and $failedChecks.Count -gt 0) {
        $strictFailureNames = @($failedChecks | ForEach-Object { ([string]$_.Name).Trim() } | Select-Object -Unique)
        $strictFailureCount = $strictFailureNames.Count
    }
    $strictFailureNamesText = if ($strictFailureCount -gt 0) { ($strictFailureNames -join ",") } else { "none" }
    $hasBuildStrictFailure = $strictFailureNames -contains "Strict requirement: build"
    $freshnessStrictFailures = @($failedChecks | Where-Object {
        [string]$_.Name -like "Strict requirement:*freshness*"
    })
    $freshnessStrictFailureCount = $freshnessStrictFailures.Count
    $hasFreshnessStrictFailure = $freshnessStrictFailureCount -gt 0
    $nonFreshnessFailures = @($failedChecks | Where-Object {
        -not ([string]$_.Name -like "Strict requirement:*freshness*")
    })
    $isFreshnessOnlyStrictFailure = ($verdict -eq "FAIL" -and $hasFreshnessStrictFailure -and $nonFreshnessFailures.Count -eq 0)
    $strictFailureClass = "NONE"
    if (-not $strictRequested) {
        $strictFailureClass = "NON_STRICT"
    }
    elseif ($strictFailureCount -eq 0) {
        $strictFailureClass = "NONE"
    }
    elseif ($isFreshnessOnlyStrictFailure) {
        $strictFailureClass = "FRESHNESS_ONLY"
    }
    elseif ($hasBuildStrictFailure -and $hasFreshnessStrictFailure) {
        $strictFailureClass = "BUILD_AND_FRESHNESS"
    }
    elseif ($hasBuildStrictFailure) {
        $strictFailureClass = "BUILD_ONLY"
    }
    else {
        $strictFailureClass = "OTHER_STRICT"
    }
    $partialChecks = @($checks | Where-Object { $_.Status -eq "PARTIAL" })
    $isGeoOnlyPartial = $partialChecks.Count -gt 0
    foreach ($partialCheck in $partialChecks) {
        $name = [string]$partialCheck.Name
        $detail = [string]$partialCheck.Detail
        $nameGeoTagged = $name -eq "Geo/provider access blocker" -or $name -eq "Provider public API probe artifacts" -or $name -eq "Spot/perp geo-access blocker" -or $name -eq "Spot+perps coverage"
        $detailGeoTagged = ($detail -match "ENV-CONSTRAINT") -or ($detail -match "environment[-/ ]constrain") -or ($detail -match "geo-access blocker") -or ($detail -match "env-waived")
        if (-not ($nameGeoTagged -or $detailGeoTagged)) {
            $isGeoOnlyPartial = $false
            break
        }
    }

    $strictPolicyDecision = "non-strict"
    if ($strictRequested) {
        if ($verdict -eq "PASS") {
            $strictPolicyDecision = "promote"
        }
        elseif ($verdict -eq "PARTIAL" -and $isGeoOnlyPartial) {
            $strictPolicyDecision = "allow-geo-partial"
        }
        elseif ($verdict -eq "PARTIAL") {
            $strictPolicyDecision = "collect-more-evidence"
        }
        elseif ($strictFailureClass -eq "FRESHNESS_ONLY") {
            $strictPolicyDecision = "refresh-freshness"
        }
        elseif ($strictFailureClass -eq "BUILD_ONLY" -or $strictFailureClass -eq "BUILD_AND_FRESHNESS") {
            $strictPolicyDecision = "fix-build"
        }
        else {
            $strictPolicyDecision = "fix-failures"
        }
    }

    $ciSummary = "version=" + $ciContractVersion + ";fields=" + $ciFieldsText + ";verdict=" + $verdict + ";strict=" + [string]$strictRequested + ";class=" + $strictFailureClass + ";decision=" + $strictPolicyDecision + ";fails=" + $strictFailureCount + ";manifest=" + $artifactManifestHash

    if ($verdict -eq "PASS") {
        $recommendedNextAction = "Proceed to next certification stage (or promotion gate) with current evidence package."
    }
    elseif ($verdict -eq "FAIL" -and $freshnessStrictFailures.Count -gt 0 -and $nonFreshnessFailures.Count -eq 0) {
        $target = if ($freshnessNonPassSources.Count -gt 0) { ($freshnessNonPassSources -join ",") } else { "matrix,provider,reject" }
        $recommendedNextAction = "Refresh " + $target + " evidence artifacts (or relax freshness thresholds), then re-run strict certification."
    }
    elseif ($verdict -eq "PARTIAL") {
        if ($isGeoOnlyPartial) {
            $recommendedNextAction = "Geo/provider blockers are documented; continue non-geo reliability hardening and keep strict certification as PARTIAL baseline evidence."
        }
        else {
            $recommendedNextAction = "Run strict matrix + provider probe artifact capture to upgrade PARTIAL to PASS."
        }
    }

    $jsonReport = New-Object psobject
    Add-Member -InputObject $jsonReport -MemberType NoteProperty -Name ReportType -Value "MultiExchangeCertification"
    Add-Member -InputObject $jsonReport -MemberType NoteProperty -Name GeneratedUtc -Value $timestampUtc.ToString("o")
    Add-Member -InputObject $jsonReport -MemberType NoteProperty -Name Verdict -Value $verdict
    Add-Member -InputObject $jsonReport -MemberType NoteProperty -Name Repository -Value $repo
    Add-Member -InputObject $jsonReport -MemberType NoteProperty -Name Inputs -Value @{
        AutoModeReportsDir = $AutoModeReportsDir
        LogRoot = $LogRoot
        StrictRequested = $strictRequested
        StrictSwitch = $Strict.IsPresent
        RequireBuildPass = $strictGateBuild
        RequireMatrixPass = $strictGateMatrix
        RequireProviderArtifacts = $strictGateProvider
        RequireRejectCategories = $strictGateReject
        MaxMatrixArtifactAgeHours = $MaxMatrixArtifactAgeHours
        MaxProviderProbeAgeHours = $MaxProviderProbeAgeHours
        MaxRejectEvidenceAgeHours = $MaxRejectEvidenceAgeHours
        LatestCycleReport = $latestCyclePath
        RowEvidenceDir = $RowEvidenceDir
        LatestLogFile = $latestLogPath
    }
    Add-Member -InputObject $jsonReport -MemberType NoteProperty -Name Checks -Value $checkRows
    Add-Member -InputObject $jsonReport -MemberType NoteProperty -Name ArtifactManifest -Value $artifactManifest
    Add-Member -InputObject $jsonReport -MemberType NoteProperty -Name ArtifactManifestHash -Value $artifactManifestHash
    Add-Member -InputObject $jsonReport -MemberType NoteProperty -Name RejectCategories -Value $rejectCounts
    Add-Member -InputObject $jsonReport -MemberType NoteProperty -Name StrategyExchangeMatrix -Value $matrixRows
    Add-Member -InputObject $jsonReport -MemberType NoteProperty -Name Summary -Value @{
        ExchangesRequired = $exchanges
        StrategiesRequired = $strategies
        MatrixStatus = if ($hasCycle) { $matrixStatus } else { "NOT-RUN" }
        MatrixCoverage = $matrixCoverage
        RequiredSpotServices = $requiredServices
        RequiredPerpServices = $requiredPerpServices
        MatrixRowEvidenceCoverage = @{
            EvidencedRows = $evidencedRowCount
            MissingRows = $missingRowEvidenceCount
            TotalRows = $totalRequiredRows
        }
        Freshness = @{
            OverallStatus = $freshnessOverallStatus
            NonPassSources = $freshnessNonPassSources
            ThresholdHours = @{
                MatrixArtifact = $MaxMatrixArtifactAgeHours
                ProviderProbe = $MaxProviderProbeAgeHours
                RejectEvidence = $MaxRejectEvidenceAgeHours
            }
            MatrixArtifact = @{
                Source = $matrixArtifactSource
                AgeHours = $matrixArtifactAgeHours
                Status = $matrixFreshnessStatus
            }
            ProviderProbe = @{
                Source = $providerArtifactSource
                AgeHours = $providerArtifactAgeHours
                Status = $providerFreshnessStatus
            }
            RejectEvidence = @{
                Source = $rejectEvidenceSource
                AgeHours = $rejectEvidenceAgeHours
                Status = $rejectFreshnessStatus
            }
        }
        Strict = @{
            Enabled = $strictRequested
            CiVersion = $ciContractVersion
            CiFields = $ciFields
            CiFieldsText = $ciFieldsText
            StrictSwitch = $Strict.IsPresent
            EffectiveGates = @{
                Build = $strictGateBuild
                Matrix = $strictGateMatrix
                ProviderArtifacts = $strictGateProvider
                RejectCategories = $strictGateReject
            }
            FailureCount = $strictFailureCount
            FailureNames = $strictFailureNames
            FailureClass = $strictFailureClass
            FreshnessOnlyFailure = $isFreshnessOnlyStrictFailure
            PolicyDecision = $strictPolicyDecision
            CiSummary = $ciSummary
        }
        ArtifactManifestCount = @($artifactManifest).Count
        ArtifactManifestHash = $artifactManifestHash
    }
    Add-Member -InputObject $jsonReport -MemberType NoteProperty -Name RecommendedNextAction -Value $recommendedNextAction

    $jsonPath = Join-Path $OutputRoot ("multi_exchange_cert_" + $stamp + ".json")
    $txtPath = Join-Path $OutputRoot ("multi_exchange_cert_" + $stamp + ".txt")

    $jsonReport | ConvertTo-Json -Depth 8 | Set-Content -Path $jsonPath -Encoding UTF8

    $txtLines = New-Object System.Collections.Generic.List[string]
    $txtLines.Add("Multi-Exchange Certification Report") | Out-Null
    $txtLines.Add("GeneratedUtc: " + $timestampUtc.ToString("o")) | Out-Null
    $txtLines.Add("Verdict: " + $verdict) | Out-Null
    $txtLines.Add("CiVersion: " + $ciContractVersion) | Out-Null
    $txtLines.Add("CiFields: " + $ciFieldsText) | Out-Null
    $txtLines.Add("StrictMode: " + ($(if ($strictRequested) { "ON" } else { "OFF" })) + " (strictSwitch=" + [string]$Strict.IsPresent + ", build=" + [string]$strictGateBuild + ", matrix=" + [string]$strictGateMatrix + ", provider=" + [string]$strictGateProvider + ", reject=" + [string]$strictGateReject + ")") | Out-Null
    $txtLines.Add("StrictFailureClass: " + $strictFailureClass) | Out-Null
    $txtLines.Add("StrictPolicyDecision: " + $strictPolicyDecision) | Out-Null
    $txtLines.Add("CiSummary: " + $ciSummary) | Out-Null
    $txtLines.Add("StrictFailures: count=" + $strictFailureCount + " | names=" + $strictFailureNamesText) | Out-Null
    $matrixAgeText = if ([double]::IsInfinity($matrixArtifactAgeHours)) { "n/a" } else { $matrixArtifactAgeHours.ToString([System.Globalization.CultureInfo]::InvariantCulture) + "h/" + $MaxMatrixArtifactAgeHours.ToString([System.Globalization.CultureInfo]::InvariantCulture) + "h" }
    $providerAgeText = if ([double]::IsInfinity($providerArtifactAgeHours)) { "n/a" } else { $providerArtifactAgeHours.ToString([System.Globalization.CultureInfo]::InvariantCulture) + "h/" + $MaxProviderProbeAgeHours.ToString([System.Globalization.CultureInfo]::InvariantCulture) + "h" }
    $rejectAgeText = if ([double]::IsInfinity($rejectEvidenceAgeHours)) { "n/a" } else { $rejectEvidenceAgeHours.ToString([System.Globalization.CultureInfo]::InvariantCulture) + "h/" + $MaxRejectEvidenceAgeHours.ToString([System.Globalization.CultureInfo]::InvariantCulture) + "h" }
    $txtLines.Add("Freshness: overall=" + $freshnessOverallStatus + " | matrix=" + $matrixFreshnessStatus + "(" + $matrixAgeText + ")" + " | provider=" + $providerFreshnessStatus + "(" + $providerAgeText + ")" + " | reject=" + $rejectFreshnessStatus + "(" + $rejectAgeText + ")") | Out-Null
    $txtLines.Add("ArtifactManifestHash: " + $artifactManifestHash) | Out-Null
    $txtLines.Add("") | Out-Null
    $txtLines.Add("Checks:") | Out-Null
    foreach ($check in $checks) {
        $txtLines.Add("- [" + $check.Status + "] " + $check.Name + " :: " + $check.Detail) | Out-Null
    }

    $txtLines.Add("") | Out-Null
    $txtLines.Add("Artifact Manifest:") | Out-Null
    foreach ($entry in $artifactManifest) {
        $entryAge = if ([double]::IsInfinity([double]$entry.AgeHours)) { "n/a" } else { [double]$entry.AgeHours }
        $txtLines.Add("- " + $entry.Name + " | class=" + [string]$entry.SourceClass + " | diffKey=" + [string]$entry.DiffKey + " | status=" + $entry.Status + " | ageHours=" + $entryAge + " | file=" + $entry.FileName) | Out-Null
    }

    $txtLines.Add("") | Out-Null
    $txtLines.Add("Reject Categories (latest log):") | Out-Null
    foreach ($key in ($rejectCounts.Keys | Sort-Object)) {
        $txtLines.Add("- " + $key + " = " + $rejectCounts[$key]) | Out-Null
    }

    $txtLines.Add("") | Out-Null
    $txtLines.Add("Strategy x Exchange Matrix:") | Out-Null
    foreach ($row in $matrixRows) {
        $evidenceText = if ([string]::IsNullOrWhiteSpace($row.EvidenceRef)) { "<missing>" } else { $row.EvidenceRef }
        $txtLines.Add("- " + $row.Strategy + " | " + $row.Exchange + " | " + $row.Status + " | evidence=" + $evidenceText) | Out-Null
    }

    $txtLines.Add("") | Out-Null
    $txtLines.Add("Recommended Next Action: " + $jsonReport.RecommendedNextAction) | Out-Null

    $txtLines | Set-Content -Path $txtPath -Encoding UTF8

    Write-Host ("REPORT_JSON=" + $jsonPath)
    Write-Host ("REPORT_TXT=" + $txtPath)
    Write-Host ("CI_VERSION=" + $ciContractVersion)
    Write-Host ("CI_FIELDS=" + $ciFieldsText)
    Write-Host ("STRICT_REQUESTED=" + [string]$strictRequested)
    Write-Host ("STRICT_SWITCH=" + [string]$Strict.IsPresent)
    Write-Host ("STRICT_GATES=" + "build=" + [string]$strictGateBuild + ",matrix=" + [string]$strictGateMatrix + ",provider=" + [string]$strictGateProvider + ",reject=" + [string]$strictGateReject)
    Write-Host ("STRICT_FAILURE_CLASS=" + $strictFailureClass)
    Write-Host ("STRICT_POLICY_DECISION=" + $strictPolicyDecision)
    Write-Host ("CI_SUMMARY=" + $ciSummary)
    Write-Host ("STRICT_FAILURE_COUNT=" + [string]$strictFailureCount)
    Write-Host ("STRICT_FAILURE_NAMES=" + $strictFailureNamesText)
    Write-Host ("VERDICT=" + $verdict)

    if ($verdict -eq "FAIL") {
        exit 1
    }

    exit 0
}
finally {
    Pop-Location
}
