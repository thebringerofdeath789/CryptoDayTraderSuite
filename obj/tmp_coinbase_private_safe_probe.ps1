$asm = Join-Path (Get-Location) "bin\Debug_Verify_Probe4\CryptoDayTraderSuite.exe"
[void][System.Reflection.Assembly]::LoadFrom($asm)
$ks = New-Object CryptoDayTraderSuite.Services.KeyService
$id = $ks.GetActiveId('coinbase-advanced')
if ([string]::IsNullOrWhiteSpace($id)) { $id = $ks.GetActiveId('coinbase-exchange') }
if ([string]::IsNullOrWhiteSpace($id)) { Write-Output '__PRIVATE_SAFE=FAIL'; Write-Output '__ERROR=No active Coinbase key'; exit 1 }
$key = $ks.Get($id)
if ($null -eq $key) { Write-Output '__PRIVATE_SAFE=FAIL'; Write-Output '__ERROR=Active key not found'; exit 1 }
function U([string]$v){ if([string]::IsNullOrWhiteSpace($v)){ return ''}; $u=$ks.Unprotect($v); if([string]::IsNullOrWhiteSpace($u)){ return $v}; return $u }
$apiKey = U($key.ApiKey)
$apiSecret = U($key.ApiSecretBase64)
if ([string]::IsNullOrWhiteSpace($apiSecret)) { $apiSecret = U($key.Secret) }
$passphrase = U($key.Passphrase)
$keyName = U($key.ApiKeyName)
$pem = U($key.ECPrivateKeyPem)
if ($key.Data -and $key.Data.ContainsKey('ApiKeyName') -and [string]::IsNullOrWhiteSpace($keyName)) { $keyName = U([string]$key.Data['ApiKeyName']) }
if ($key.Data -and $key.Data.ContainsKey('ECPrivateKeyPem') -and [string]::IsNullOrWhiteSpace($pem)) { $pem = U([string]$key.Data['ECPrivateKeyPem']) }
[CryptoDayTraderSuite.Util.CoinbaseCredentialNormalizer]::NormalizeCoinbaseAdvancedInputs([ref]$apiKey,[ref]$apiSecret,[ref]$keyName,[ref]$pem)
$cli = New-Object CryptoDayTraderSuite.Exchanges.CoinbaseExchangeClient($keyName,$pem,$passphrase)
try {
  $open = $cli.GetOpenOrdersAsync().GetAwaiter().GetResult()
  $fills = $cli.GetRecentFillsAsync(10).GetAwaiter().GetResult()
  $fakeId = [Guid]::NewGuid().ToString()
  $cancel = $cli.CancelOrderAsync($fakeId).GetAwaiter().GetResult()
  Write-Output '__PRIVATE_SAFE=PASS'
  Write-Output ('__KEY_ID=' + $id)
  Write-Output ('__OPEN_ORDERS_COUNT=' + $open.Count)
  Write-Output ('__RECENT_FILLS_COUNT=' + $fills.Count)
  Write-Output ('__FAKE_CANCEL_RESULT=' + $cancel)
} catch {
  Write-Output '__PRIVATE_SAFE=FAIL'
  Write-Output ('__KEY_ID=' + $id)
  Write-Output ('__ERROR=' + $_.Exception.Message)
  exit 1
}
