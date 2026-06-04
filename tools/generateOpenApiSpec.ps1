<#
.SYNOPSIS
    Generates the OpenAPI (Swagger) JSON spec for the TB.DanceDance API.

.DESCRIPTION
    Runs the API host with the FastEndpoints export switch (--exportswaggerjson true), which
    builds the spec from the endpoints and writes "<OutputDirectory>\<DocumentName>.json", then exits.
    The app does not stay running and does not need a database.

.EXAMPLE
    tools/generateOpenApiSpec.ps1
    tools/generateOpenApiSpec.ps1 -OutputDirectory src/frontend/openapi
#>
param(
    [string]$OutputDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) "artifacts"),
    [string]$DocumentName = "v1",
    [string]$Environment = "Development"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$apiProject = Join-Path $repoRoot "src\backend\TB.DanceDance.API"

if (-not (Test-Path $OutputDirectory)) {
    New-Item -Path $OutputDirectory -ItemType Directory | Out-Null
}
$OutputDirectory = (Resolve-Path $OutputDirectory).Path

Write-Host "Generating OpenAPI spec ('$DocumentName') into: $OutputDirectory"

dotnet run --project $apiProject --environment $Environment `
    --exportswaggerjson true `
    --swaggerOutputPath $OutputDirectory

if ($LASTEXITCODE -ne 0) {
    throw "Spec generation failed (dotnet run exited with $LASTEXITCODE)."
}

$specFile = Join-Path $OutputDirectory "$($DocumentName.ToLowerInvariant() -replace ' ', '-').json"
if (-not (Test-Path $specFile)) {
    throw "Expected spec file was not produced: $specFile"
}

Write-Host ""
Write-Host "OpenAPI spec written to: $specFile"
