<#
.SYNOPSIS
    Revert (remove) the most recent EF Core migration for DanceDbContext.

.DESCRIPTION
    Starts a disposable postgres container on a random host port, applies all migrations to
    that database, runs `dotnet ef migrations remove --force` (deletes the last migration's
    files and reverts the model snapshot), then removes the container - always, even on
    failure. Use this to undo a migration you just generated with add-migration.ps1 but
    have not yet committed/shipped.

.EXAMPLE
    ./remove-migration.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_common.ps1')

$infra = Get-InfrastructureProject
Assert-DotnetEf

try {
    Start-MigrationDb
    Update-MigrationDb -InfrastructureProject $infra

    Write-Host "Removing the most recent migration..." -ForegroundColor Cyan
    dotnet ef migrations remove --force `
        --project $infra `
        --startup-project $infra
    if ($LASTEXITCODE -ne 0) { throw "dotnet ef migrations remove failed." }

    Write-Host "Most recent migration removed; model snapshot reverted." -ForegroundColor Green
}
finally {
    Stop-MigrationDb
}
