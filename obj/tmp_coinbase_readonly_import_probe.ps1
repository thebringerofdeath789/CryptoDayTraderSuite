$asm = Join-Path (Get-Location) "bin\Debug_Verify_Probe\CryptoDayTraderSuite.exe"
[void][System.Reflection.Assembly]::LoadFrom($asm)
$ks = New-Object CryptoDayTraderSuite.Services.KeyService
$acc = New-Object CryptoDayTraderSuite.Services.AccountService
$hist = New-Object CryptoDayTraderSuite.Services.HistoryService
$svc = New-Object CryptoDayTraderSuite.Services.CoinbaseReadOnlyImportService($ks,$acc,$hist)
try {
  $r = $svc.ValidateAndImportAsync().GetAwaiter().GetResult()
  Write-Output '__COINBASE_READONLY_IMPORT=PASS'
  Write-Output ('__KEY_ID=' + $r.KeyId)
  Write-Output ('__PRODUCT_COUNT=' + $r.ProductCount)
  Write-Output ('__NONZERO_BALANCES=' + $r.NonZeroBalanceCount)
  Write-Output ('__MAKER=' + $r.MakerRate)
  Write-Output ('__TAKER=' + $r.TakerRate)
  Write-Output ('__TOTAL_FILLS=' + $r.TotalFillCount)
  Write-Output ('__IMPORTED_TRADES=' + $r.ImportedTradeCount)
  Write-Output ('__TOTAL_FEES=' + $r.TotalFeesPaid)
  Write-Output ('__NET_PROFIT_EST=' + $r.NetProfitEstimate)
} catch {
  Write-Output '__COINBASE_READONLY_IMPORT=FAIL'
  Write-Output ('__ERROR=' + $_.Exception.Message)
}
