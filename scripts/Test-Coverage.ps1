#!/usr/bin/env pwsh
# Test coverage script for Khaos.Pipeline

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot | Split-Path -Parent
$resultsDir = Join-Path $repoRoot "TestResults"

# Clean previous results
if (Test-Path $resultsDir) {
    Remove-Item -Recurse -Force $resultsDir
}

Write-Host "Running tests with coverage for Khaos.Pipeline ($Configuration)..." -ForegroundColor Cyan

dotnet test "$repoRoot\Khaos.Pipeline.sln" -c $Configuration `
    --collect:"XPlat Code Coverage" `
    --results-directory $resultsDir `
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

if ($LASTEXITCODE -ne 0) {
    Write-Error "Tests failed."
    exit $LASTEXITCODE
}

# Find coverage file
$coverageFile = Get-ChildItem -Path $resultsDir -Recurse -Filter "coverage.cobertura.xml" | Select-Object -First 1

if ($coverageFile) {
    Write-Host "Coverage report: $($coverageFile.FullName)" -ForegroundColor Green
} else {
    Write-Host "No coverage file generated." -ForegroundColor Yellow
}
