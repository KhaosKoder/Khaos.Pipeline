#!/usr/bin/env pwsh
# Pack script for Khaos.Pipeline

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot | Split-Path -Parent

Write-Host "Packing Khaos.Pipeline ($Configuration)..." -ForegroundColor Cyan

dotnet pack "$repoRoot\Khaos.Pipeline.sln" -c $Configuration --no-build

if ($LASTEXITCODE -ne 0) {
    Write-Error "Pack failed."
    exit $LASTEXITCODE
}

$artifactsDir = Join-Path $repoRoot "artifacts"
$packages = Get-ChildItem -Path $artifactsDir -Filter "*.nupkg" -ErrorAction SilentlyContinue

if ($packages) {
    Write-Host "Created packages:" -ForegroundColor Green
    $packages | ForEach-Object { Write-Host "  - $($_.Name)" }
} else {
    Write-Host "No packages found in artifacts." -ForegroundColor Yellow
}
