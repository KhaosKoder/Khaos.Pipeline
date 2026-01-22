#!/usr/bin/env pwsh
# Clean script for Khaos.Pipeline

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot | Split-Path -Parent

Write-Host "Cleaning Khaos.Pipeline..." -ForegroundColor Cyan

$foldersToDelete = @(
    'bin',
    'obj',
    'artifacts',
    'TestResults'
)

foreach ($folder in $foldersToDelete) {
    $paths = Get-ChildItem -Path $repoRoot -Directory -Recurse -Filter $folder -ErrorAction SilentlyContinue
    foreach ($path in $paths) {
        Write-Host "Removing $($path.FullName)"
        Remove-Item -Recurse -Force $path.FullName
    }
}

Write-Host "Clean completed." -ForegroundColor Green
