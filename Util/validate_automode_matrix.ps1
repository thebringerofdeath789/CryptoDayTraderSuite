param(
    [string]$ReportPath,
    [string]$ReportsDir = (Join-Path $env:LOCALAPPDATA "CryptoDayTraderSuite\automode\cycle_reports"),
    [int]$MinProfiles = 2,
    [switch]$RequireFailureContainment,
    [switch]$RequireMixedScopes,
    [string[]]$RequireSelectedSymbolCounts,
    [switch]$RequireIndependentGuardrailConfigs,
    [switch]$RequireFailureIsolation
)

$ErrorActionPreference = "Stop"

function Resolve-ReportPath {
    param(
        [string]$ExplicitPath,
        [string]$RootDir
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        if (-not (Test-Path $ExplicitPath)) {
            throw "Report file not found: $ExplicitPath"
        }
        return (Resolve-Path $ExplicitPath).Path
    }

    if (-not (Test-Path $RootDir)) {
        throw "Reports directory not found: $RootDir"
    }

    $latest = Get-ChildItem -Path $RootDir -Filter "cycle_*.json" -File |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $latest) {
        throw "No cycle report files found in: $RootDir"
    }

    return $latest.FullName
}

function To-Bool($value) {
    if ($null -eq $value) { return $false }
    return [bool]$value
}

function Expand-IntValues {
    param([string[]]$Values)

    $expanded = New-Object System.Collections.Generic.List[int]
    if ($null -eq $Values) {
        return @()
    }

    foreach ($token in $Values) {
        if ([string]::IsNullOrWhiteSpace($token)) { continue }

        $parts = $token.Split(',')
        foreach ($part in $parts) {
            $trimmed = $part.Trim()
            if ([string]::IsNullOrWhiteSpace($trimmed)) { continue }

            $parsed = 0
            if ([int]::TryParse($trimmed, [ref]$parsed)) {
                $expanded.Add($parsed)
            }
            else {
                throw "Invalid selected symbol count '$trimmed'. Use integers only (for example: -RequireSelectedSymbolCounts 3,15)."
            }
        }
    }

    return @($expanded.ToArray())
}

try {
    $resolvedReportPath = Resolve-ReportPath -ExplicitPath $ReportPath -RootDir $ReportsDir
    $raw = Get-Content -Path $resolvedReportPath -Raw
    if ([string]::IsNullOrWhiteSpace($raw)) {
        throw "Report is empty: $resolvedReportPath"
    }

    $report = $raw | ConvertFrom-Json
    if ($null -eq $report) {
        throw "Failed to parse report JSON: $resolvedReportPath"
    }

    $profiles = @($report.Profiles)
    $requiredSelectedCounts = Expand-IntValues -Values $RequireSelectedSymbolCounts

    $checkResults = @()

    $checkResults += [pscustomobject]@{
        Name   = "Minimum profiles"
        Passed = ($profiles.Count -ge $MinProfiles)
        Detail = "processed=$($profiles.Count), minimum=$MinProfiles"
    }

    $hasCoverageFlag = $report.PSObject.Properties.Name -contains "MatrixMinimumProfileCoverage"
    $matrixCoverageSatisfied = if ($hasCoverageFlag) { To-Bool $report.MatrixMinimumProfileCoverage } else { ($profiles.Count -ge $MinProfiles) }
    $matrixCoverageDetail = if ($hasCoverageFlag) {
        $note = if ($report.PSObject.Properties.Name -contains "MatrixCoverageNote") { [string]$report.MatrixCoverageNote } else { "" }
        if ([string]::IsNullOrWhiteSpace($note)) {
            "MatrixMinimumProfileCoverage=$($report.MatrixMinimumProfileCoverage)"
        }
        else {
            "MatrixMinimumProfileCoverage=$($report.MatrixMinimumProfileCoverage), note=$note"
        }
    }
    else {
        "MatrixMinimumProfileCoverage=legacy(not-exported)"
    }

    $checkResults += [pscustomobject]@{
        Name   = "Matrix profile coverage"
        Passed = $matrixCoverageSatisfied
        Detail = $matrixCoverageDetail
    }

    $checkResults += [pscustomobject]@{
        Name   = "Pair configuration consistency"
        Passed = (To-Bool $report.MatrixPairConfigurationConsistent)
        Detail = "MatrixPairConfigurationConsistent=$($report.MatrixPairConfigurationConsistent)"
    }

    $checkResults += [pscustomobject]@{
        Name   = "Independent guardrails observed"
        Passed = (To-Bool $report.MatrixIndependentGuardrailsObserved)
        Detail = "MatrixIndependentGuardrailsObserved=$($report.MatrixIndependentGuardrailsObserved)"
    }

    $checkResults += [pscustomobject]@{
        Name   = "Guardrail scope isolation"
        Passed = (To-Bool $report.MatrixGuardrailScopesIsolated)
        Detail = "MatrixGuardrailScopesIsolated=$($report.MatrixGuardrailScopesIsolated)"
    }

    $checkResults += [pscustomobject]@{
        Name   = "Failure does not halt cycle"
        Passed = (To-Bool $report.MatrixFailureDoesNotHaltCycle)
        Detail = "MatrixFailureDoesNotHaltCycle=$($report.MatrixFailureDoesNotHaltCycle)"
    }

    if ($RequireFailureContainment.IsPresent) {
        $checkResults += [pscustomobject]@{
            Name   = "Failure containment observed"
            Passed = (To-Bool $report.MatrixFailureContainmentObserved)
            Detail = "MatrixFailureContainmentObserved=$($report.MatrixFailureContainmentObserved)"
        }
    }

    $matrixStatusText = [string]$report.MatrixStatus

    $checkResults += [pscustomobject]@{
        Name   = "Overall matrix status"
        Passed = ([string]::Equals($matrixStatusText, "PASS", [System.StringComparison]::OrdinalIgnoreCase))
        Detail = "MatrixStatus=$matrixStatusText"
    }

    if ($RequireMixedScopes.IsPresent) {
        $hasSelected = $profiles | Where-Object { [string]::Equals([string]$_.PairScope, "Selected", [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
        $hasAll = $profiles | Where-Object { [string]::Equals([string]$_.PairScope, "All", [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1

        $checkResults += [pscustomobject]@{
            Name   = "Mixed scope scenario"
            Passed = (($null -ne $hasSelected) -and ($null -ne $hasAll))
            Detail = "requires Selected + All profiles in same cycle"
        }
    }

    if ($requiredSelectedCounts -and $requiredSelectedCounts.Count -gt 0) {
        foreach ($requiredCount in $requiredSelectedCounts) {
            $matched = $profiles |
                Where-Object {
                    [string]::Equals([string]$_.PairScope, "Selected", [System.StringComparison]::OrdinalIgnoreCase) -and
                    ([int]$_.SymbolCount -eq [int]$requiredCount)
                } |
                Select-Object -First 1

            $checkResults += [pscustomobject]@{
                Name   = "Selected symbol count $requiredCount"
                Passed = ($null -ne $matched)
                Detail = "requires a Selected profile with SymbolCount=$requiredCount"
            }
        }
    }

    if ($RequireIndependentGuardrailConfigs.IsPresent) {
        $guardrailProfiles = @($profiles | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_.ProfileId) })
        $scopeDistinct = @($guardrailProfiles | Select-Object -ExpandProperty GuardrailScopeKey | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique)
        $guardrailTupleDistinct = @($guardrailProfiles |
            ForEach-Object { "{0}|{1}|{2}" -f ([int]$_.MaxTradesPerCycle), ([int]$_.CooldownMinutes), ([decimal]$_.DailyRiskStopPct) } |
            Sort-Object -Unique)
        $riskTelemetryPresent = @($guardrailProfiles | Where-Object { $_.PSObject.Properties.Name -contains "DailyRiskUsedAfter" }).Count -eq $guardrailProfiles.Count

        $checkResults += [pscustomobject]@{
            Name   = "Independent guardrail configs"
            Passed = ($guardrailProfiles.Count -ge 2 -and $scopeDistinct.Count -ge 2 -and $guardrailTupleDistinct.Count -ge 2 -and $riskTelemetryPresent)
            Detail = "profiles=$($guardrailProfiles.Count), scopeKeys=$($scopeDistinct.Count), guardrailTuples=$($guardrailTupleDistinct.Count), riskTelemetry=$riskTelemetryPresent"
        }
    }

    if ($RequireFailureIsolation.IsPresent) {
        $hasFailedProfile = @($profiles | Where-Object {
            [string]::Equals([string]$_.Status, "blocked", [System.StringComparison]::OrdinalIgnoreCase) -or
            [string]::Equals([string]$_.Status, "error", [System.StringComparison]::OrdinalIgnoreCase)
        }).Count -gt 0

        $hasCompletedProfile = @($profiles | Where-Object {
            [string]::Equals([string]$_.Status, "completed", [System.StringComparison]::OrdinalIgnoreCase) -or
            [string]::Equals([string]$_.Status, "executed", [System.StringComparison]::OrdinalIgnoreCase) -or
            [string]::Equals([string]$_.Status, "skipped", [System.StringComparison]::OrdinalIgnoreCase)
        }).Count -gt 0

        $containmentObserved = (To-Bool $report.MatrixFailureContainmentObserved)

        $checkResults += [pscustomobject]@{
            Name   = "Failure isolation observed"
            Passed = ($hasFailedProfile -and $hasCompletedProfile -and $containmentObserved)
            Detail = "failedProfile=$hasFailedProfile, completedProfile=$hasCompletedProfile, MatrixFailureContainmentObserved=$containmentObserved"
        }
    }

    Write-Host "AutoMode matrix report: $resolvedReportPath"
    $coverageDisplay = if ($hasCoverageFlag) { $report.MatrixMinimumProfileCoverage } else { "legacy" }
    Write-Host "Cycle: $($report.CycleId) | Profiles: $($report.ProcessedProfileCount)/$($report.EnabledProfileCount) | Failed: $($report.FailedProfiles) | Matrix: $($report.MatrixStatus) | Coverage: $coverageDisplay"
    if ($report.PSObject.Properties.Name -contains "GateStatus") {
        Write-Host "Reliability Gates: $($report.GateStatus)"
    }
    Write-Host ""

    foreach ($row in $checkResults) {
        $state = if ($row.Passed) { "PASS" } else { "FAIL" }
        Write-Host ("[{0}] {1} - {2}" -f $state, $row.Name, $row.Detail)
    }

    $failures = @($checkResults | Where-Object { -not $_.Passed })
    if ($failures.Count -gt 0) {
        Write-Error ("Matrix validation failed with {0} failing check(s)." -f $failures.Count)
        exit 1
    }

    Write-Host ""
    Write-Host "Matrix validation passed."
    exit 0
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}