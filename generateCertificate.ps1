$AzureWebsiteName = "wcsdance.azurewebsites.net"
Write-Host "Provide password"
$CertPassword = Read-Host
if ((Test-Path 'C:\temp') -eq $false){
    New-Item "C:\temp" -ItemType 'Directory'
}
$PfxOutputLocation = "C:\temp\cert.pfx"

#### You shouldn't need to modify anything below this line.

$cert = New-SelfSignedCertificate -certstorelocation cert:\localmachine\my -dnsname $AzureWebsiteName

$pwd = ConvertTo-SecureString -String $CertPassword -Force -AsPlainText

$path = 'cert:\localMachine\my\' + $cert.thumbprint
Export-PfxCertificate -cert $path -FilePath $PfxOutputLocation -Password $pwd