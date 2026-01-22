#!/usr/bin/env pwsh
# Test script for Khaos.Pipeline

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot | Split-Path -Parent

Write-Host "Running tests for Khaos.Pipeline ($Configuration)..." -ForegroundColor Cyan

dotnet test "$repoRoot\Khaos.Pipeline.sln" -c $Configuration --no-build --verbosity normal

if ($LASTEXITCODE -ne 0) {
    Write-Error "Tests failed."
    exit $LASTEXITCODE
}

Write-Host "All tests passed." -ForegroundColor Green
