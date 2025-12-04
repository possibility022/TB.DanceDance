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

$increaseVersion = Read-Host -Prompt "Chcesz podnieść wersję? y/N"

if ($increaseVersion -eq "Y" -or $increaseVersion -eq "y"){
    $mobileSolution = Join-Path $mobileDir 'TB.DanceDance.Mobile.csproj'
    $csProjContent = Get-Content $mobileSolution -Raw
    # Extract all version numbers between <ApplicationVersion> tags
    $versions = [regex]::Matches($csProjContent, "<ApplicationVersion>(\d+)</ApplicationVersion>") | ForEach-Object {
        [int]$_.Groups[1].Value
    }

    # Find the highest version
    $maxVersion = ($versions | Measure-Object -Maximum).Maximum
    $toReplace = "<ApplicationVersion>" + $maxVersion + "</ApplicationVersion>"
    $newVersion = [int]$maxVersion + 1;
    $toSet = "<ApplicationVersion>" + $newVersion + "</ApplicationVersion>"
    $csProjContent = $csProjContent.Replace($toReplace, $toSet)

    Write-Output "Zmiana wersji z: " + $toReplace + "na: " $toSet

    Out-File $mobileSolution -Encoding utf8 -InputObject $csProjContent -NoNewline
}

$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($password)
$UnsecurePassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
Set-Content -Path $passwordPath -Value $UnsecurePassword -NoNewline

Set-Location $mobileDir
dotnet publish -f net10.0-android -c Release -p:AndroidKeyStore=true -p:AndroidSigningKeyStore=$keystorePath -p:AndroidSigningKeyAlias=$alias -p:AndroidSigningKeyPass=file:$passwordPath -p:AndroidSigningStorePass=file:$passwordPath
Set-Location $originalLocation

Remove-Item $passwordPath -ErrorAction SilentlyContinue


