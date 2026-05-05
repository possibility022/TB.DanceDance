param(
    [string]$OutputDirectory = "$env:USERPROFILE\.aspnet\https",
    [string]$OutputEnvFile = (Join-Path (Split-Path -Parent $PSScriptRoot) ".env.authserver-certs"),
    [string]$LocalAppSettingsPath = (Join-Path (Split-Path -Parent $PSScriptRoot) "src\authserver\appsettings.Local.json"),
    [string]$Password,
    [switch]$Force,
    [switch]$SkipAppSettingsUpdate
)

$ErrorActionPreference = "Stop"

if ((Test-Path $OutputDirectory) -eq $false) {
    New-Item -Path $OutputDirectory -ItemType Directory | Out-Null
}

function Set-ObjectProperty {
    param(
        [Parameter(Mandatory = $true)]$Object,
        [Parameter(Mandatory = $true)][string]$PropertyName,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$PropertyValue
    )

    if ($Object.PSObject.Properties.Name -contains $PropertyName) {
        $Object.$PropertyName = $PropertyValue
    }
    else {
        $Object | Add-Member -NotePropertyName $PropertyName -NotePropertyValue $PropertyValue
    }
}

if ([string]::IsNullOrWhiteSpace($Password)) {
    Write-Host "Provide password for all generated AuthServer certificates."
    Write-Host "This password will be written to $OutputEnvFile."
    $certPasswordSecure = Read-Host -AsSecureString
    $certPassword = [System.Net.NetworkCredential]::new("", $certPasswordSecure).Password
}
else {
    $certPassword = $Password
    $certPasswordSecure = ConvertTo-SecureString -String $Password -Force -AsPlainText
}

$certDefinitions = @(
    @{
        Name = "authserver-server-signing"
        Subject = "CN=TB DanceDance Auth Server Signing"
        KeyUsage = @("DigitalSignature")
    },
    @{
        Name = "authserver-server-encryption"
        Subject = "CN=TB DanceDance Auth Server Encryption"
        KeyUsage = @("KeyEncipherment", "DataEncipherment")
    },
    @{
        Name = "authserver-client-signing"
        Subject = "CN=TB DanceDance Auth Client Signing"
        KeyUsage = @("DigitalSignature")
    },
    @{
        Name = "authserver-client-encryption"
        Subject = "CN=TB DanceDance Auth Client Encryption"
        KeyUsage = @("KeyEncipherment", "DataEncipherment")
    }
)

$generated = @{}
foreach ($definition in $certDefinitions) {
    $filePath = Join-Path $OutputDirectory "$($definition.Name).pfx"

    if ((Test-Path $filePath) -and !$Force) {
        throw "File already exists: $filePath. Rerun with -Force to overwrite."
    }

    $cert = New-SelfSignedCertificate `
        -Subject $definition.Subject `
        -KeyAlgorithm RSA `
        -KeyLength 2048 `
        -KeyUsage $definition.KeyUsage `
        -KeyExportPolicy Exportable `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -NotAfter (Get-Date).AddYears(10)

    $storePath = "Cert:\CurrentUser\My\$($cert.Thumbprint)"
    Export-PfxCertificate -Cert $storePath -FilePath $filePath -Password $certPasswordSecure | Out-Null

    $generated[$definition.Name] = @{
        FilePath = $filePath
        Base64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes($filePath))
    }
}

$envLines = @(
    "AuthServer__ServerSigningCertificateBase64=$($generated['authserver-server-signing'].Base64)",
    "AuthServer__ServerSigningCertificatePassword=$certPassword",
    "AuthServer__ServerEncryptionCertificateBase64=$($generated['authserver-server-encryption'].Base64)",
    "AuthServer__ServerEncryptionCertificatePassword=$certPassword",
    "AuthServer__ClientSigningCertificateBase64=$($generated['authserver-client-signing'].Base64)",
    "AuthServer__ClientSigningCertificatePassword=$certPassword",
    "AuthServer__ClientEncryptionCertificateBase64=$($generated['authserver-client-encryption'].Base64)",
    "AuthServer__ClientEncryptionCertificatePassword=$certPassword"
)

Set-Content -Path $OutputEnvFile -Value $envLines -Encoding UTF8

if (!$SkipAppSettingsUpdate) {
    if (!(Test-Path $LocalAppSettingsPath)) {
        throw "appsettings.Development.json was not found at: $LocalAppSettingsPath"
    }

    $appSettingsJson = Get-Content -Path $LocalAppSettingsPath -Raw
    if ([string]::IsNullOrWhiteSpace($appSettingsJson)) {
        throw "appsettings.Development.json is empty: $LocalAppSettingsPath"
    }

    $appSettings = $appSettingsJson | ConvertFrom-Json

    if ($null -eq $appSettings.AuthServer) {
        $appSettings | Add-Member -NotePropertyName AuthServer -NotePropertyValue ([pscustomobject]@{})
    }

    Set-ObjectProperty -Object $appSettings.AuthServer -PropertyName "ServerSigningCertificateBase64" -PropertyValue $generated['authserver-server-signing'].Base64
    Set-ObjectProperty -Object $appSettings.AuthServer -PropertyName "ServerSigningCertificatePassword" -PropertyValue $certPassword
    Set-ObjectProperty -Object $appSettings.AuthServer -PropertyName "ServerEncryptionCertificateBase64" -PropertyValue $generated['authserver-server-encryption'].Base64
    Set-ObjectProperty -Object $appSettings.AuthServer -PropertyName "ServerEncryptionCertificatePassword" -PropertyValue $certPassword
    Set-ObjectProperty -Object $appSettings.AuthServer -PropertyName "ClientSigningCertificateBase64" -PropertyValue $generated['authserver-client-signing'].Base64
    Set-ObjectProperty -Object $appSettings.AuthServer -PropertyName "ClientSigningCertificatePassword" -PropertyValue $certPassword
    Set-ObjectProperty -Object $appSettings.AuthServer -PropertyName "ClientEncryptionCertificateBase64" -PropertyValue $generated['authserver-client-encryption'].Base64
    Set-ObjectProperty -Object $appSettings.AuthServer -PropertyName "ClientEncryptionCertificatePassword" -PropertyValue $certPassword

    $updatedAppSettings = $appSettings | ConvertTo-Json -Depth 20
    Set-Content -Path $LocalAppSettingsPath -Value $updatedAppSettings -Encoding UTF8
}

Write-Host ""
Write-Host "Generated certificates:"
foreach ($definition in $certDefinitions) {
    Write-Host "- $($generated[$definition.Name].FilePath)"
}
Write-Host ""
Write-Host "Wrote Docker env vars to: $OutputEnvFile"
if (!$SkipAppSettingsUpdate) {
    Write-Host "Updated appsettings: $LocalAppSettingsPath"
}
