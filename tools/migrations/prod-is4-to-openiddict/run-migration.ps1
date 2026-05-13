#Requires -Version 5.1
<#
.SYNOPSIS
    Guided migration: IdentityServer4 → OpenIddict
    Runs 01_migrate_users.sql, 02_migrate_applications.sql, 03_verify_migration.sql

.USAGE
    .\run-migration.ps1
    .\run-migration.ps1 -PsqlPath "D:\pgsql\bin\psql.exe" -Host localhost -Port 5432 -User postgres
#>
param(
    [string] $PsqlPath = "D:\pgsql\bin\psql.exe",
    [string] $DbHost = "localhost",
    [int]    $Port = 5432,
    [string] $User = "postgres",
    [string] $TargetDb = "tbauthwebdb",
    [string] $SourceDb = "prodoriginaldata"
)

$ScriptDir = $PSScriptRoot

function Write-Step([string]$msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }
function Write-Ok([string]$msg) { Write-Host "    OK: $msg" -ForegroundColor Green }
function Write-Fail([string]$msg) { Write-Host "    FAIL: $msg" -ForegroundColor Red }

# ─── 1. Prerequisite: psql ───────────────────────────────────────────────────
Write-Step "Sprawdzanie psql..."
if (-not (Test-Path $PsqlPath)) {
    Write-Fail "Nie znaleziono psql pod: $PsqlPath"
    Write-Host "    Podaj ścieżkę przez parametr -PsqlPath" -ForegroundColor Yellow
    exit 1
}
$psqlVersion = & $PsqlPath --version 2>&1
Write-Ok $psqlVersion

# ─── 2. Hasło ────────────────────────────────────────────────────────────────
Write-Step "Podaj hasło PostgreSQL (user: $User)"
$securePwd = Read-Host "Hasło" -AsSecureString
$bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePwd)
$Password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
[System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)

$env:PGPASSWORD = $Password

function Invoke-Psql([string]$Database, [string]$Sql, [hashtable]$Vars = @{}) {
    $varArgs = $Vars.Keys | ForEach-Object { "-v"; "$_=$($Vars[$_])" }
    $output = & $PsqlPath -h $DbHost -p $Port -U $User -d $Database @varArgs -c $Sql 2>&1
    return $output
}

function Invoke-PsqlFile([string]$Database, [string]$File, [hashtable]$Vars = @{}) {
    $varArgs = $Vars.Keys | ForEach-Object { "-v"; "$_=$($Vars[$_])" }
    $output = & $PsqlPath -h $DbHost -p $Port -U $User -d $Database @varArgs -f $File 2>&1
    return $output
}

# ─── 3. Test połączeń ────────────────────────────────────────────────────────
Write-Step "Testowanie połączenia z $TargetDb (OpenIddict)..."
$r = Invoke-Psql $TargetDb "SELECT current_database();"
if ($LASTEXITCODE -ne 0) { Write-Fail "Brak połączenia z $TargetDb`n$r"; exit 1 }
Write-Ok "Połączono z $TargetDb"

Write-Step "Testowanie połączenia z $SourceDb (IS4)..."
$r = Invoke-Psql $SourceDb "SELECT current_database();"
if ($LASTEXITCODE -ne 0) { Write-Fail "Brak połączenia z $SourceDb`n$r"; exit 1 }
Write-Ok "Połączono z $SourceDb"

# ─── 4. Podsumowanie przed migracją ─────────────────────────────────────────
Write-Step "Stan PRZED migracją (tbauthwebdb):"
$before = Invoke-Psql $TargetDb @"
SELECT 'Users' AS entity, COUNT(*)::text AS count FROM "Idp.Ident"."AspNetUsers"
UNION ALL SELECT 'UserClaims', COUNT(*)::text FROM "Idp.Ident"."AspNetUserClaims"
UNION ALL SELECT 'Applications', COUNT(*)::text FROM "Idp.Auth"."OpenIddictApplications";
"@
$before | ForEach-Object { Write-Host "    $_" }

# ─── 5. Potwierdzenie ────────────────────────────────────────────────────────
Write-Host "`nMigracja dokona następujących zmian w $TargetDb :" -ForegroundColor Yellow
Write-Host "  - Skopiuje użytkowników z $SourceDb.Idp.Ident → $TargetDb.Idp.Ident"
Write-Host "  - Zaktualizuje redirect URIs aplikacji tbdancedancefront"
Write-Host "  - Ustawi secret klienta tbdancedanceconverter"
$confirm = Read-Host "`nKontynuować? [T/n]"
if ($confirm -ne "" -and $confirm -notmatch "^[TtYy]") {
    Write-Host "Anulowano." -ForegroundColor Yellow
    exit 0
}

$sourceConn = "host=$DbHost port=$Port dbname=$SourceDb user=$User password=$Password"

# ─── 6. Krok 1: Użytkownicy ─────────────────────────────────────────────────
Write-Step "Krok 1/2: Migracja użytkowników (01_migrate_users.sql)..."
$file1 = Join-Path $ScriptDir "01_migrate_users.sql"
$out1 = Invoke-PsqlFile $TargetDb $file1 @{ source_conn = $sourceConn }
$out1 | ForEach-Object { Write-Host "    $_" }
if ($LASTEXITCODE -ne 0) { Write-Fail "Skrypt zakończył się błędem. Migracja zatrzymana."; exit 1 }
Write-Ok "Skrypt 01 zakończony"

# ─── 7. Krok 2: Aplikacje ────────────────────────────────────────────────────
Write-Step "Krok 2/2: Migracja konfiguracji aplikacji (02_migrate_applications.sql)..."
$file2 = Join-Path $ScriptDir "02_migrate_applications.sql"
$out2 = Invoke-PsqlFile $TargetDb $file2 @{ source_conn = $sourceConn }
$out2 | Where-Object { $_ -notmatch "already exists, skipping" } | ForEach-Object { Write-Host "    $_" }
if ($LASTEXITCODE -ne 0) { Write-Fail "Skrypt zakończył się błędem."; exit 1 }
Write-Ok "Skrypt 02 zakończony"

# ─── 8. Weryfikacja ──────────────────────────────────────────────────────────
Write-Step "Weryfikacja (03_verify_migration.sql)..."
$file3 = Join-Path $ScriptDir "03_verify_migration.sql"
$out3 = Invoke-PsqlFile $TargetDb $file3
$out3 | ForEach-Object { Write-Host "    $_" }

# ─── 9. Podsumowanie ─────────────────────────────────────────────────────────
Write-Host "`n==========================================" -ForegroundColor Cyan
Write-Host "  Migracja zakonczona pomyslnie!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Nastepne kroki:" -ForegroundColor Yellow
Write-Host "  1. Sprawdz redirect URIs frontendu w tbauthwebdb (wyniki powyzej)"
Write-Host "  2. Upewnij sie ze nowy auth server wskazuje na tbauthwebdb"
Write-Host "  3. Przetestuj logowanie przez OpenIddict"
Write-Host ""

$env:PGPASSWORD = ""
