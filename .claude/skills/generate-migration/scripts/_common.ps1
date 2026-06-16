# Shared helpers for the generate-migration scripts.
# Dot-sourced by add-migration.ps1 and remove-migration.ps1 — not meant to be run directly.
#
# A throwaway PostgreSQL container is used so EF can validate the migration against a real
# database. Its credentials/port match the hardcoded design-time connection string in
# src/backend/Infrastructure/Data/DesignTimeContextFactory.cs
# (Server=localhost;Port=5432;Userid=postgres;Password=...;Database=dancedance).

$script:MigrationDbContainer = 'tbdance-ef-migration'
$script:MigrationDbPassword  = 'rgFraWIuyxONqWCQ71wh'
$script:MigrationDbName      = 'dancedance'
$script:MigrationDbPort      = 5432
$script:PostgresImage        = 'postgres'

function Get-RepoRoot {
    # scripts -> generate-migration -> skills -> .claude -> repo root
    return (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
}

function Get-InfrastructureProject {
    $proj = Join-Path (Get-RepoRoot) 'src\backend\Infrastructure'
    if (-not (Test-Path $proj)) {
        throw "Infrastructure project not found at $proj"
    }
    return $proj
}

function Assert-Docker {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw "docker not found on PATH. Install Docker Desktop and ensure it is running."
    }
    docker info *> $null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker is not running. Start Docker Desktop and retry."
    }
}

function Assert-DotnetEf {
    dotnet ef --version *> $null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "dotnet-ef not available; attempting 'dotnet tool restore'..." -ForegroundColor Yellow
        dotnet tool restore *> $null
        dotnet ef --version *> $null
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet-ef is not installed. Run: dotnet tool install --global dotnet-ef"
        }
    }
}

function Start-MigrationDb {
    Assert-Docker

    # Remove any stale container left over from a previously interrupted run.
    docker rm -f $script:MigrationDbContainer *> $null

    Write-Host "Starting throwaway PostgreSQL container '$script:MigrationDbContainer' on port $script:MigrationDbPort..." -ForegroundColor Cyan
    docker run -d --name $script:MigrationDbContainer `
        -e "POSTGRES_PASSWORD=$script:MigrationDbPassword" `
        -e "POSTGRES_DB=$script:MigrationDbName" `
        -p "$($script:MigrationDbPort):5432" `
        $script:PostgresImage | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to start the postgres container. Port $script:MigrationDbPort may already be in use - stop the local stack (it also binds 5432) and retry."
    }

    # Wait until the server accepts connections.
    $deadline = (Get-Date).AddSeconds(60)
    while ((Get-Date) -lt $deadline) {
        docker exec $script:MigrationDbContainer pg_isready -U postgres -d $script:MigrationDbName *> $null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "PostgreSQL is ready." -ForegroundColor Green
            return
        }
        Start-Sleep -Milliseconds 800
    }
    throw "PostgreSQL container did not become ready within 60s."
}

function Stop-MigrationDb {
    Write-Host "Removing container '$script:MigrationDbContainer'..." -ForegroundColor Cyan
    docker rm -f $script:MigrationDbContainer *> $null
}
