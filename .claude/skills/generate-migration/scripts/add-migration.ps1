<#
.SYNOPSIS
    Generate a new EF Core migration for DanceDbContext against a throwaway PostgreSQL container.

.DESCRIPTION
    One command does everything: starts a disposable postgres container (matching the
    design-time connection string), runs `dotnet ef migrations add`, applies it to the
    fresh database to prove it applies cleanly, then removes the container - always, even
    if a step fails.

    Migrations land in src/backend/Infrastructure/Data/Migrations (the project's existing
    non-default output dir). The local stack must NOT be running, because it also binds
    port 5432.

.PARAMETER Name
    The migration name in PascalCase, e.g. AddCompetitions.

.PARAMETER NoApply
    Generate the migration only; skip applying it to the throwaway database.

.EXAMPLE
    ./add-migration.ps1 -Name AddCompetitions

.EXAMPLE
    ./add-migration.ps1 AddCompetitions -NoApply
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Name,
    [switch]$NoApply
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_common.ps1')

$infra = Get-InfrastructureProject
Assert-DotnetEf

try {
    Start-MigrationDb

    Write-Host "Adding migration '$Name'..." -ForegroundColor Cyan
    dotnet ef migrations add $Name `
        --project $infra `
        --startup-project $infra `
        --output-dir Data/Migrations
    if ($LASTEXITCODE -ne 0) { throw "dotnet ef migrations add failed." }

    if (-not $NoApply) {
        Write-Host "Applying migration to the throwaway DB to validate it..." -ForegroundColor Cyan
        dotnet ef database update --project $infra --startup-project $infra
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet ef database update failed - the migration does not apply cleanly. Review it (or run remove-migration.ps1)."
        }
    }

    Write-Host "Migration '$Name' generated under src/backend/Infrastructure/Data/Migrations." -ForegroundColor Green
}
finally {
    Stop-MigrationDb
}
