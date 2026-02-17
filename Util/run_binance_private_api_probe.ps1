param(
    [string]$RepoRoot = ".",
    [string]$AssemblyPath,
    [string]$Service = "Binance-US",
    [string]$ProbeSymbol = "BTC-USD"
)

$ErrorActionPreference = "Stop"

function Resolve-AssemblyPath {
    param([string]$Repo, [string]$ExplicitPath)

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

$repo = (Resolve-Path $RepoRoot).Path
Push-Location $repo
try {
    $assembly = Resolve-AssemblyPath -Repo $repo -ExplicitPath $AssemblyPath
    [void][System.Reflection.Assembly]::LoadFrom($assembly)

    $keyService = New-Object CryptoDayTraderSuite.Services.KeyService
    $provider = New-Object CryptoDayTraderSuite.Services.ExchangeProvider($keyService)

    $activeId = $keyService.GetActiveId($Service)
    if ([string]::IsNullOrWhiteSpace($activeId)) {
        Write-Output "__BINANCE_PRIVATE=FAIL"
        Write-Output ("__SERVICE=" + $Service)
        Write-Output "__ERROR=No active key configured for service"
        exit 1
    }

    $authClient = $provider.CreateAuthenticatedClient($Service)
    $publicClient = $provider.CreatePublicClient($Service)

    $fees = $authClient.GetFeesAsync().GetAwaiter().GetResult()
    $products = $publicClient.ListProductsAsync().GetAwaiter().GetResult()

    $symbol = $ProbeSymbol
    if ($products -and $products.Count -gt 0) {
        $match = $products | Where-Object { $_ -eq $ProbeSymbol } | Select-Object -First 1
        if ([string]::IsNullOrWhiteSpace($match)) {
            $match = $products | Where-Object { $_ -match "(BTC[-/]USD|BTC[-/]USDT|BTC[-/]USDC)" } | Select-Object -First 1
        }
        if (-not [string]::IsNullOrWhiteSpace($match)) {
            $symbol = $match
        }
        else {
            $symbol = $products[0]
        }
    }

    $ticker = $publicClient.GetTickerAsync($symbol).GetAwaiter().GetResult()

    $clientType = $authClient.GetType().FullName
    $balancesCount = -1
    $quoteBalance = 0
    $quoteCurrency = "USD"

    if ($clientType -eq "CryptoDayTraderSuite.Services.ResilientExchangeClient") {
        $innerField = $authClient.GetType().GetField("_inner", [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance)
        if ($innerField -ne $null) {
            $authClient = $innerField.GetValue($authClient)
            $clientType = $authClient.GetType().FullName
        }
    }

    if ($clientType -eq "CryptoDayTraderSuite.Exchanges.BinanceClient") {
        $balances = $authClient.GetBalancesAsync().GetAwaiter().GetResult()
        $balancesCount = if ($balances -eq $null) { 0 } else { $balances.Count }
        if ($balances -and $balances.ContainsKey("USD")) {
            $quoteBalance = [decimal]$balances["USD"]
            $quoteCurrency = "USD"
        }
        elseif ($balances -and $balances.ContainsKey("USDT")) {
            $quoteBalance = [decimal]$balances["USDT"]
            $quoteCurrency = "USDT"
        }
        elseif ($balances -and $balances.ContainsKey("USDC")) {
            $quoteBalance = [decimal]$balances["USDC"]
            $quoteCurrency = "USDC"
        }
    }

    $productCount = 0
    if ($products) {
        $productCount = $products.Count
    }

    $tickerLast = 0
    if ($ticker) {
        $tickerLast = $ticker.Last
    }

    $makerFee = 0
    $takerFee = 0
    if ($fees) {
        $makerFee = $fees.MakerRate
        $takerFee = $fees.TakerRate
    }

    Write-Output "__BINANCE_PRIVATE=PASS"
    Write-Output ("__SERVICE=" + $Service)
    Write-Output ("__KEY_ID=" + $activeId)
    Write-Output ("__PRODUCT_COUNT=" + $productCount)
    Write-Output ("__PROBE_SYMBOL=" + $symbol)
    Write-Output ("__TICKER_LAST=" + $tickerLast)
    Write-Output ("__MAKER_FEE=" + $makerFee)
    Write-Output ("__TAKER_FEE=" + $takerFee)
    Write-Output ("__BALANCE_COUNT=" + $balancesCount)
    Write-Output ("__QUOTE_CCY=" + $quoteCurrency)
    Write-Output ("__QUOTE_BALANCE=" + $quoteBalance)
}
catch {
    Write-Output "__BINANCE_PRIVATE=FAIL"
    Write-Output ("__SERVICE=" + $Service)
    Write-Output ("__ERROR=" + $_.Exception.Message)
    exit 1
}
finally {
    Pop-Location
}
