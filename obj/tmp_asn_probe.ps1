$asm = Join-Path (Get-Location) "bin\Debug_Verify_Probe\CryptoDayTraderSuite.exe"
[void][System.Reflection.Assembly]::LoadFrom($asm)
$ks = New-Object CryptoDayTraderSuite.Services.KeyService
$id = $ks.GetActiveId('coinbase-advanced')
$k = $ks.Get($id)
function U([string]$v){ if([string]::IsNullOrWhiteSpace($v)){ return ''}; $u=$ks.Unprotect($v); if([string]::IsNullOrWhiteSpace($u)){ return $v}; return $u }
$pem = U($k.ECPrivateKeyPem)
$norm = [CryptoDayTraderSuite.Util.CoinbaseCredentialNormalizer]::NormalizePem($pem)
$startMarker = '-----BEGIN EC PRIVATE KEY-----'
$endMarker = '-----END EC PRIVATE KEY-----'
$s = $norm.IndexOf($startMarker,[System.StringComparison]::OrdinalIgnoreCase)
$e = $norm.IndexOf($endMarker,[System.StringComparison]::OrdinalIgnoreCase)
$b64 = $norm.Substring($s + $startMarker.Length, $e - ($s + $startMarker.Length)).Replace("`n","").Replace("`r","").Trim()
$der = [Convert]::FromBase64String($b64)
function ReadLen([byte[]]$d,[ref]$o){
  $f = $d[$o.Value]; $o.Value++
  if (($f -band 0x80) -eq 0){ return [int]$f }
  $c = $f -band 0x7F
  $len = 0
  for($i=0;$i -lt $c;$i++){ $len = ($len -shl 8) -bor $d[$o.Value]; $o.Value++ }
  return $len
}
$o = 0
$tag = $der[$o]; $o++
$seqLen = ReadLen $der ([ref]$o)
Write-Output ('__SEQ_TAG=0x' + $tag.ToString('X2') + ' LEN=' + $seqLen)
$end = $o + $seqLen
while($o -lt $end){
  $t = $der[$o]; $o++
  $l = ReadLen $der ([ref]$o)
  Write-Output ('__FIELD_TAG=0x' + $t.ToString('X2') + ' LEN=' + $l + ' OFF=' + $o)
  $o += $l
}
Write-Output ('__DER_TOTAL=' + $der.Length)
