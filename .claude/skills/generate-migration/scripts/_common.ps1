# Shared helpers for the generate-migration scripts.
# Dot-sourced by add-migration.ps1 and remove-migration.ps1 — not meant to be run directly.
#
# A throwaway PostgreSQL container is used so EF can validate the migration against a real
# database. Docker assigns a random host port so this can run alongside the local stack.

$script:MigrationDbContainerPrefix = 'tbdance-ef-migration'
$script:MigrationDbContainer       = $null
$script:MigrationDbPassword        = 'rgFraWIuyxONqWCQ71wh'
$script:MigrationDbName            = 'dancedance'
$script:MigrationDbPort            = $null
$script:PostgresImage              = 'postgres'
$script:MigrationDbConnectionEnvVar = 'TBDANCEDANCE_MIGRATION_CONNECTION_STRING'
$script:PreviousMigrationDbConnectionString = [Environment]::GetEnvironmentVariable($script:MigrationDbConnectionEnvVar, 'Process')

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

function New-MigrationDbContainerName {
    $suffix = [Guid]::NewGuid().ToString('N').Substring(0, 12)
    return "$script:MigrationDbContainerPrefix-$suffix"
}

function Get-MigrationDbConnectionString {
    if (-not $script:MigrationDbPort) {
        throw "Migration database has not been started yet."
    }

    return "Server=localhost;Port=$script:MigrationDbPort;Userid=postgres;Password=$script:MigrationDbPassword;Database=$script:MigrationDbName"
}

function Set-MigrationDbPort {
    $portMapping = docker port $script:MigrationDbContainer 5432/tcp
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($portMapping)) {
        throw "Failed to inspect the random host port for '$script:MigrationDbContainer'."
    }

    $script:MigrationDbPort = ($portMapping.Trim() -split ':')[-1]
}

function Set-MigrationDbEnvironment {
    [Environment]::SetEnvironmentVariable($script:MigrationDbConnectionEnvVar, (Get-MigrationDbConnectionString), 'Process')
}

function Restore-MigrationDbEnvironment {
    [Environment]::SetEnvironmentVariable($script:MigrationDbConnectionEnvVar, $script:PreviousMigrationDbConnectionString, 'Process')
}

function Start-MigrationDb {
    Assert-Docker

    $script:MigrationDbContainer = New-MigrationDbContainerName

    Write-Host "Starting throwaway PostgreSQL container '$script:MigrationDbContainer' on a random host port..." -ForegroundColor Cyan
    docker run -d --name $script:MigrationDbContainer `
        -e "POSTGRES_PASSWORD=$script:MigrationDbPassword" `
        -e "POSTGRES_DB=$script:MigrationDbName" `
        -p "127.0.0.1::5432" `
        $script:PostgresImage | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to start the postgres container."
    }

    Set-MigrationDbPort
    Set-MigrationDbEnvironment
    Write-Host "PostgreSQL is listening on localhost:$script:MigrationDbPort." -ForegroundColor Cyan

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

function Update-MigrationDb {
    param(
        [Parameter(Mandatory = $true)]
        [string]$InfrastructureProject
    )

    $connectionString = Get-MigrationDbConnectionString

    Write-Host "Applying migrations to the throwaway DB..." -ForegroundColor Cyan
    dotnet ef database update `
        --project $InfrastructureProject `
        --startup-project $InfrastructureProject `
        --connection $connectionString
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet ef database update failed."
    }
}

function Stop-MigrationDb {
    if ($script:MigrationDbContainer) {
        Write-Host "Removing container '$script:MigrationDbContainer'..." -ForegroundColor Cyan
        docker rm -f $script:MigrationDbContainer *> $null
    }

    Restore-MigrationDbEnvironment
}
