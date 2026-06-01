#Requires -Version 5.1
<#
.SYNOPSIS
    Guided migration: prod monolith schema (tbapi_db) -> modular schema (dancedance)
    Runs 01_migrate_data.sql then 02_verify_migration.sql.

.USAGE
    .\run-migration.ps1
    .\run-migration.ps1 -PsqlPath "D:\pgsql\bin\psql.exe" -DbHost localhost -Port 5432 -User postgres
#>
param(
    [string] $PsqlPath = "D:\pgsql\bin\psql.exe",
    [string] $DbHost   = "localhost",
    [int]    $Port     = 5432,
    [string] $User     = "postgres",
    [string] $TargetDb = "dancedance",
    [string] $SourceDb = "tbapi_db"
)

$ScriptDir = $PSScriptRoot

function Write-Step([string]$msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }
function Write-Ok([string]$msg)   { Write-Host "    OK: $msg" -ForegroundColor Green }
function Write-Fail([string]$msg) { Write-Host "    FAIL: $msg" -ForegroundColor Red }

# ─── 1. Prerequisite: psql ───────────────────────────────────────────────────
Write-Step "Checking psql..."
if (-not (Test-Path $PsqlPath)) {
    Write-Fail "psql not found at: $PsqlPath"
    Write-Host "    Provide the path via -PsqlPath" -ForegroundColor Yellow
    exit 1
}
$psqlVersion = & $PsqlPath --version
Write-Ok $psqlVersion

# ─── 2. Password ─────────────────────────────────────────────────────────────
Write-Step "Enter PostgreSQL password (user: $User)"
$securePwd = Read-Host "Password" -AsSecureString
$bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePwd)
$Password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
[System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)

$env:PGPASSWORD = $Password

function Invoke-Psql([string]$Database, [string]$Sql, [hashtable]$Vars = @{}) {
    $varArgs = $Vars.Keys | ForEach-Object { "-v"; "$_=$($Vars[$_])" }
    & $PsqlPath -h $DbHost -p $Port -U $User -d $Database @varArgs -c $Sql
}

function Invoke-PsqlFile([string]$Database, [string]$File, [hashtable]$Vars = @{}) {
    $varArgs = $Vars.Keys | ForEach-Object { "-v"; "$_=$($Vars[$_])" }
    & $PsqlPath -h $DbHost -p $Port -U $User -d $Database @varArgs -v ON_ERROR_STOP=1 -f $File
}

# ─── 3. Test connections ─────────────────────────────────────────────────────
Write-Step "Testing connection to $TargetDb (modular target)..."
$r = Invoke-Psql $TargetDb "SELECT current_database();"
if ($LASTEXITCODE -ne 0) { Write-Fail "Cannot connect to $TargetDb`n$r"; exit 1 }
Write-Ok "Connected to $TargetDb"

Write-Step "Testing connection to $SourceDb (old prod restore)..."
$r = Invoke-Psql $SourceDb "SELECT current_database();"
if ($LASTEXITCODE -ne 0) { Write-Fail "Cannot connect to $SourceDb`n$r"; exit 1 }
Write-Ok "Connected to $SourceDb"

# ─── 4. State BEFORE ─────────────────────────────────────────────────────────
Write-Step "State BEFORE migration ($TargetDb):"
$before = Invoke-Psql $TargetDb @"
SELECT 'Users' AS entity, COUNT(*)::text AS count FROM access."Users"
UNION ALL SELECT 'Videos', COUNT(*)::text FROM video."Videos"
UNION ALL SELECT 'SharedWith', COUNT(*)::text FROM sharing."SharedWith"
UNION ALL SELECT 'Comments', COUNT(*)::text FROM comments."Comments";
"@
$before | ForEach-Object { Write-Host "    $_" }

# ─── 5. Confirmation ─────────────────────────────────────────────────────────
Write-Host "`nThis migration will, in $TargetDb :" -ForegroundColor Yellow
Write-Host "  - TRUNCATE all data tables (EF history tables are left untouched)"
Write-Host "  - Reload all app data from $SourceDb, applying the schema renames/moves"
$confirm = Read-Host "`nContinue? [y/N]"
if ($confirm -notmatch "^[YyTt]") {
    Write-Host "Aborted." -ForegroundColor Yellow
    $env:PGPASSWORD = ""
    exit 0
}

$sourceConn = "host=$DbHost port=$Port dbname=$SourceDb user=$User password=$Password"

# ─── 6. Step 1: copy data ────────────────────────────────────────────────────
Write-Step "Step 1/2: Copying data (01_migrate_data.sql)..."
$file1 = Join-Path $ScriptDir "01_migrate_data.sql"
Invoke-PsqlFile $TargetDb $file1 @{ source_conn = $sourceConn }
if ($LASTEXITCODE -ne 0) { Write-Fail "Script failed. Migration rolled back."; $env:PGPASSWORD = ""; exit 1 }
Write-Ok "01_migrate_data.sql finished"

# ─── 7. Step 2: verify ───────────────────────────────────────────────────────
Write-Step "Step 2/2: Verification (02_verify_migration.sql)..."
$file2 = Join-Path $ScriptDir "02_verify_migration.sql"
$out2 = Invoke-PsqlFile $TargetDb $file2 @{ source_conn = $sourceConn }
$out2 | ForEach-Object { Write-Host "    $_" }
if ($LASTEXITCODE -ne 0) { Write-Fail "Verification script errored."; $env:PGPASSWORD = ""; exit 1 }

if ($out2 -match "MISMATCH") {
    Write-Fail "Row-count MISMATCH detected — review the verification output above."
    $env:PGPASSWORD = ""
    exit 1
}

# ─── 8. Summary ──────────────────────────────────────────────────────────────
Write-Host "`n==========================================" -ForegroundColor Cyan
Write-Host "  Migration completed successfully!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "All target tables match source row counts. Next steps:" -ForegroundColor Yellow
Write-Host "  1. Point the local API at $TargetDb and sanity-check a list view"
Write-Host "  2. The source DB ($SourceDb) was not modified"
Write-Host ""

$env:PGPASSWORD = ""
