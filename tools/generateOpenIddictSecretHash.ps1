param(
    [Parameter(Mandatory=$true)]
    [string]$Secret,

    [string]$ClientId
)

Add-Type -TypeDefinition @"
using System;
using System.Security.Cryptography;

public static class Pbkdf2Helper {
    public static string Hash(string password) {
        byte[] salt = new byte[16];
        RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        rng.Dispose();

        Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        byte[] subkey = pbkdf2.GetBytes(32);
        pbkdf2.Dispose();

        // ASP.NET Core Identity PasswordHasher v3 format (used by OpenIddict)
        // [0]      version = 0x01
        // [1..4]   PRF = HMACSHA256 = 1 (big-endian)
        // [5..8]   iterations = 100000 (big-endian)
        // [9..12]  salt length = 16 (big-endian)
        // [13..28] salt (16 bytes)
        // [29..60] subkey (32 bytes)
        byte[] output = new byte[61];
        output[0] = 0x01;
        output[1] = 0x00; output[2] = 0x00; output[3] = 0x00; output[4] = 0x01;
        output[5] = 0x00; output[6] = 0x01; output[7] = 0x86; output[8] = 0xA0;
        output[9] = 0x00; output[10] = 0x00; output[11] = 0x00; output[12] = 0x10;
        Array.Copy(salt, 0, output, 13, 16);
        Array.Copy(subkey, 0, output, 29, 32);

        return Convert.ToBase64String(output);
    }
}
"@

$hash = [Pbkdf2Helper]::Hash($Secret)

Write-Host ""
Write-Host "Secret : $Secret"
Write-Host "Hash   : $hash"
Write-Host ""

if ($ClientId) {
    Write-Host "SQL UPDATE:"
    Write-Host "UPDATE `"Idp.Auth`".`"OpenIddictApplications`" SET `"ClientSecret`" = '$hash' WHERE `"ClientId`" = '$ClientId';"
}
