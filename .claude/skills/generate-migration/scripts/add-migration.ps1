<#
.SYNOPSIS
    Generate a new EF Core migration for DanceDbContext against a throwaway PostgreSQL container.

.DESCRIPTION
    One command does everything: starts a disposable postgres container on a random host
    port, applies existing migrations to that database, runs `dotnet ef migrations add`,
    optionally applies the new migration to prove it applies cleanly, then removes the
    container - always, even if a step fails.

    Migrations land in src/backend/Infrastructure/Data/Migrations (the project's existing
    non-default output dir).

.PARAMETER Name
    The migration name in PascalCase, e.g. AddCompetitions.

.PARAMETER NoApply
    Skip applying the newly generated migration after scaffolding. Existing migrations are
    still applied before scaffolding so the throwaway database matches the current model.

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
        try {
            Update-MigrationDb -InfrastructureProject $infra
        }
        catch {
            throw "The generated migration does not apply cleanly. Review it (or run remove-migration.ps1). Details: $_"
        }
    }

    Write-Host "Migration '$Name' generated under src/backend/Infrastructure/Data/Migrations." -ForegroundColor Green
}
finally {
    Stop-MigrationDb
}
