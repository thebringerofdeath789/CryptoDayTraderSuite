$asm = Join-Path (Get-Location) "bin\Debug_Verify_Probe\CryptoDayTraderSuite.exe"
[void][System.Reflection.Assembly]::LoadFrom($asm)
$ks = New-Object CryptoDayTraderSuite.Services.KeyService
$k = $ks.Get($ks.GetActiveId('coinbase-advanced'))
function U([string]$v){ if([string]::IsNullOrWhiteSpace($v)){ return ''}; $u=$ks.Unprotect($v); if([string]::IsNullOrWhiteSpace($u)){ return $v}; return $u }
$norm=[CryptoDayTraderSuite.Util.CoinbaseCredentialNormalizer]::NormalizePem((U $k.ECPrivateKeyPem))
$s='-----BEGIN EC PRIVATE KEY-----'; $e='-----END EC PRIVATE KEY-----'
$b64=$norm.Substring($norm.IndexOf($s)+$s.Length, $norm.IndexOf($e)-($norm.IndexOf($s)+$s.Length)).Replace("`n","").Replace("`r","").Trim()
$der=[Convert]::FromBase64String($b64)
function ReadLen([byte[]]$d,[ref]$o){ $f=$d[$o.Value]; $o.Value++; if(($f -band 0x80)-eq 0){return [int]$f}; $c=$f -band 0x7F; $len=0; for($i=0;$i -lt $c;$i++){ $len=($len -shl 8) -bor $d[$o.Value]; $o.Value++}; return $len }
$o=0; $o++; $null=ReadLen $der ([ref]$o)
while($o -lt $der.Length){ $t=$der[$o]; $o++; $l=ReadLen $der ([ref]$o); if($t -eq 0xA1){ $innerTag=$der[$o]; Write-Output ('__A1_INNER_TAG=0x' + $innerTag.ToString('X2')); Write-Output ('__A1_INNER_HEAD=' + [BitConverter]::ToString($der, $o, [Math]::Min(16,$l))); break }; $o += $l }
