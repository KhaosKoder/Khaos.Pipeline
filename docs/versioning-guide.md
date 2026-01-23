# Khaos.Pipeline – Versioning Guide

This document describes how versions are managed for the Pipeline package.

## Version Strategy

This package uses **MinVer** for automatic semantic versioning based on Git tags.

### Configuration

In `Directory.Build.props`:

```xml
<MinVerTagPrefix>Khaos.Pipeline/v</MinVerTagPrefix>
<MinVerDefaultPreReleaseIdentifiers>alpha.0</MinVerDefaultPreReleaseIdentifiers>
```

### Tag Format

Tags follow the pattern: `Khaos.Pipeline/vX.Y.Z`

Examples:
- `Khaos.Pipeline/v1.0.0` → Version 1.0.0
- `Khaos.Pipeline/v1.1.0` → Version 1.1.0

## Semantic Versioning

### Major Version (X.0.0)
- Breaking changes to public API
- Removing public types or members
- Changing behavior in incompatible ways

### Minor Version (0.X.0)
- New features (step types, adapters, builder methods)
- Performance improvements
- New context methods

### Patch Version (0.0.X)
- Bug fixes
- Documentation updates
- Internal refactoring

## Release Workflow

### 1. Check Current Version

```powershell
cd Khaos.Pipeline
.\scripts\Get-Version.ps1
```

### 2. Ensure Tests Pass

```powershell
.\scripts\Test.ps1
# Or with coverage
.\scripts\Test-Coverage.ps1
```

### 3. Create Release Tag

```powershell
git tag Khaos.Pipeline/v1.0.0
git push origin Khaos.Pipeline/v1.0.0
```

### 4. Build and Pack

```powershell
.\scripts\Build.ps1
.\scripts\Pack.ps1
```

### 5. Publish

```powershell
dotnet nuget push artifacts/*.nupkg --source nuget.org --api-key YOUR_KEY
```

## Dependency Coordination

This package depends on:
- `KhaosCode.Pipeline.Abstractions`
- `KhaosCode.Flow.Abstractions`

When updating:
1. Update abstractions first if needed.
2. Update this package's dependency references.
3. Tag and release this package.
4. Update consumers (e.g., `KhaosCode.Processing.Pipelines`).

## Pre-release Versions

Between tags, MinVer generates:
- After `v1.0.0`: `1.0.1-alpha.0.{commits}`

## Guidelines

1. **Never manually set Version** in project files
2. **Keep abstraction versions aligned** where possible
3. **Test with consumers** before major releases
4. **Document breaking changes** in release notes
5. **Run full test suite** including coverage before releases
