#!/usr/bin/env pwsh
# Build script for Khaos.Pipeline

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot | Split-Path -Parent

Write-Host "Building Khaos.Pipeline ($Configuration)..." -ForegroundColor Cyan

dotnet build "$repoRoot\Khaos.Pipeline.sln" -c $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit $LASTEXITCODE
}

Write-Host "Build succeeded." -ForegroundColor Green
