$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$tempConfigFile = Join-Path $scriptDir 'keystoreConfig.tmp'

if (Test-Path $tempConfigFile) {
    $config = Get-Content $tempConfigFile -Raw | ConvertFrom-Json
    $keystorePath = $config.keystorePath
    $alias = $config.alias
} else {
    $keystorePath = Read-Host "Podaj pełną ścieżkę do pliku keystore"
    $alias = Read-Host "Podaj alias keystore"
    $config = @{ keystorePath = $keystorePath; alias = $alias }
    $config | ConvertTo-Json | Set-Content -Path $tempConfigFile
}

$passwordPath = Join-Path $scriptDir 'publishPassword.tmp'
$password = Read-Host "Podaj hasło do keystore" -AsSecureString

$originalLocation = Get-Location
$mobileDir = Join-Path $scriptDir '..\src\TB.DanceDance.Mobile'

$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($password)
$UnsecurePassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
Set-Content -Path $passwordPath -Value $UnsecurePassword -NoNewline

Set-Location $mobileDir
dotnet publish -f net9.0-android -c Release -p:AndroidKeyStore=true -p:AndroidSigningKeyStore=$keystorePath -p:AndroidSigningKeyAlias=$alias -p:AndroidSigningKeyPass=file:$passwordPath -p:AndroidSigningStorePass=file:$passwordPath
Set-Location $originalLocation

Remove-Item $passwordPath -ErrorAction SilentlyContinue


