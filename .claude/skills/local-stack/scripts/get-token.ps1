<#
.SYNOPSIS
    Fetch a user access token from the local TB.DanceDance auth server (dev only).

.DESCRIPTION
    Uses the OAuth2 password grant, which is enabled locally because the compose file
    sets AuthServer:AllowWeakPasswords=true. Two users are seeded, both password "1234":
      1 -> testemail@email.com
      2 -> testemail2@email.com

    Uses curl.exe (-k) so it works under both Windows PowerShell 5.1 and PowerShell 7
    without -SkipCertificateCheck. Prints token metadata to stderr and the raw token to
    stdout, so `$t = ./get-token.ps1 -Raw` captures just the JWT.

.EXAMPLE
    ./get-token.ps1                 # token for user 1, default read scope
    ./get-token.ps1 -User 2         # token for the second seeded user
    ./get-token.ps1 -Raw            # print only the JWT (for piping/capture)
    ./get-token.ps1 -Convert        # converter daemon token (client_credentials)
#>
[CmdletBinding()]
param(
    [ValidateSet('1', '2')]
    [string]$User = '1',
    [string]$Scope,
    [string]$AuthUrl = 'https://localhost:7259',
    [switch]$Convert,
    [switch]$Raw
)

$ErrorActionPreference = 'Stop'

if ($Convert) {
    # Converter daemon: OAuth2 client credentials (no user).
    $effectiveScope = if ($Scope) { $Scope } else { 'tbdancedanceapi.convert' }
    $fields = @{
        grant_type    = 'client_credentials'
        client_id     = 'tbdancedanceconverter'
        client_secret = 'Other'
        scope         = $effectiveScope
    }
    $who = 'tbdancedanceconverter (client_credentials)'
}
else {
    $users = @{ '1' = 'testemail@email.com'; '2' = 'testemail2@email.com' }
    $username = $users[$User]
    $effectiveScope = if ($Scope) { $Scope } else { 'openid profile tbdancedanceapi.read' }
    $fields = @{
        grant_type = 'password'
        client_id  = 'tbdancedancehttpclient'
        username   = $username
        password   = '1234'
        scope      = $effectiveScope
    }
    $who = $username
}

# URL-encode each field value into an x-www-form-urlencoded body.
$body = ($fields.GetEnumerator() | ForEach-Object {
        "$($_.Key)=$([uri]::EscapeDataString([string]$_.Value))"
    }) -join '&'

$response = curl.exe -s -k -X POST "$AuthUrl/connect/token" `
    -H 'Content-Type: application/x-www-form-urlencoded' `
    -H 'Accept: application/json' `
    --data $body

if (-not $response) {
    Write-Error "No response from $AuthUrl/connect/token. Is the auth server (tbauthserver) running?"
    exit 1
}

try { $json = $response | ConvertFrom-Json } catch {
    Write-Error "Unexpected (non-JSON) response: $response"
    exit 1
}

if (-not $json.access_token) {
    Write-Error "Token request failed: $response"
    exit 1
}

# Metadata to stderr so stdout stays clean for capture/piping.
[Console]::Error.WriteLine("User:       $who")
[Console]::Error.WriteLine("Scope:      $effectiveScope")
[Console]::Error.WriteLine("Expires in: $($json.expires_in)s")
[Console]::Error.WriteLine('')

$json.access_token
