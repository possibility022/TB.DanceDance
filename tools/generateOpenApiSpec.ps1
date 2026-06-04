<#
.SYNOPSIS
    Generates the OpenAPI (Swagger) JSON spec and the frontend TypeScript model types for the TB.DanceDance API.

.DESCRIPTION
    Runs the API host with FastEndpoints export switches (the app does not stay running and needs no database):
      1. --exportswaggerjson true : builds the spec and writes "<OutputDirectory>\<DocumentName>.json".
      2. --generateclients true    : builds a single TypeScript file of all request/response model
                                      interfaces (no HTTP client) at "<TypesOutputDirectory>\<TypesFileName>.ts".
    Each switch runs in its own 'dotnet run' because the export exits the process when done.

.EXAMPLE
    tools/generateOpenApiSpec.ps1
    tools/generateOpenApiSpec.ps1 -SkipSpec
    tools/generateOpenApiSpec.ps1 -TypesOutputDirectory src/frontend/src/types/ApiModels/dancedance
#>
param(
    [string]$OutputDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) "artifacts"),
    [string]$TypesOutputDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) "src\frontend\src\types\ApiModels\dancedance"),
    [string]$TypesFileName = "apiModels",
    [string]$DocumentName = "v1",
    [string]$Environment = "Development",
    [switch]$SkipSpec,
    [switch]$SkipTypes
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$apiProject = Join-Path $repoRoot "src\backend\TB.DanceDance.API"

if (-not $SkipSpec) {
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
    Write-Host "OpenAPI spec written to: $specFile"
    Write-Host ""
}

if (-not $SkipTypes) {
    if (-not (Test-Path $TypesOutputDirectory)) {
        New-Item -Path $TypesOutputDirectory -ItemType Directory | Out-Null
    }
    $TypesOutputDirectory = (Resolve-Path $TypesOutputDirectory).Path

    Write-Host "Generating TypeScript model types into: $TypesOutputDirectory"
    dotnet run --project $apiProject --environment $Environment `
        --generateclients true `
        --clientsOutputPath $TypesOutputDirectory `
        --clientsFileName $TypesFileName
    if ($LASTEXITCODE -ne 0) {
        throw "TypeScript model generation failed (dotnet run exited with $LASTEXITCODE)."
    }

    $typesFile = Join-Path $TypesOutputDirectory "$TypesFileName.ts"
    if (-not (Test-Path $typesFile)) {
        throw "Expected TypeScript file was not produced: $typesFile"
    }
    Write-Host "TypeScript models written to: $typesFile"
}
