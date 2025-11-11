#!/usr/bin/env pwsh
# Release script for AstroImages
# Usage: .\release.ps1 [major|minor|patch]
# Example: .\release.ps1 patch    -> 1.0.2 -> 1.0.3

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('major', 'minor', 'patch')]
    [string]$BumpType = 'patch'
)

$ErrorActionPreference = "Stop"

# Read current version from .csproj
$csprojPath = "AstroImages.Wpf\AstroImages.Wpf.csproj"
[xml]$csproj = Get-Content $csprojPath

$currentVersion = $csproj.Project.PropertyGroup.Version
Write-Host "Current version: $currentVersion" -ForegroundColor Cyan

# Parse version
$versionParts = $currentVersion.Split('.')
$major = [int]$versionParts[0]
$minor = [int]$versionParts[1]
$patch = [int]$versionParts[2]

# Bump version based on type
switch ($BumpType) {
    'major' { 
        $major++
        $minor = 0
        $patch = 0
    }
    'minor' { 
        $minor++
        $patch = 0
    }
    'patch' { 
        $patch++
    }
}

$newVersion = "$major.$minor.$patch"
$newVersionFull = "$major.$minor.$patch.0"
$tagName = "v$newVersion"

Write-Host "New version: $newVersion" -ForegroundColor Green
Write-Host "Tag: $tagName" -ForegroundColor Green

# Ask for confirmation
$confirm = Read-Host "`nContinue with release? (y/n)"
if ($confirm -ne 'y' -and $confirm -ne 'Y') {
    Write-Host "Release cancelled." -ForegroundColor Yellow
    exit 0
}

# Update version in .csproj
Write-Host "`nUpdating version in $csprojPath..." -ForegroundColor Cyan
$csproj.Project.PropertyGroup.Version = $newVersion
$csproj.Project.PropertyGroup.AssemblyVersion = $newVersionFull
$csproj.Project.PropertyGroup.FileVersion = $newVersionFull
$csproj.Save($csprojPath)

Write-Host "✓ Version updated" -ForegroundColor Green

# Git operations
Write-Host "`nGit operations..." -ForegroundColor Cyan

# Check for uncommitted changes (excluding the .csproj we just changed)
$status = git status --porcelain
$otherChanges = $status | Where-Object { $_ -notmatch 'AstroImages.Wpf.csproj' }

if ($otherChanges) {
    Write-Host "Warning: You have uncommitted changes:" -ForegroundColor Yellow
    Write-Host $otherChanges
    $confirmCommit = Read-Host "`nCommit all changes? (y/n)"
    if ($confirmCommit -eq 'y' -or $confirmCommit -eq 'Y') {
        git add -A
        git commit -m "Release $newVersion"
    } else {
        Write-Host "Please commit or stash changes first." -ForegroundColor Red
        exit 1
    }
} else {
    # Only commit the .csproj change
    git add $csprojPath
    git commit -m "Bump version to $newVersion"
}

Write-Host "✓ Changes committed" -ForegroundColor Green

# Create and push tag
Write-Host "`nCreating tag $tagName..." -ForegroundColor Cyan
git tag -a $tagName -m "Release $newVersion"
Write-Host "✓ Tag created" -ForegroundColor Green

Write-Host "`nPushing to GitHub..." -ForegroundColor Cyan
git push origin main
git push origin $tagName
Write-Host "✓ Pushed to GitHub" -ForegroundColor Green

Write-Host "`n🎉 Release $newVersion initiated!" -ForegroundColor Green
Write-Host "Check GitHub Actions: https://github.com/kfaubel/AstroImages/actions" -ForegroundColor Cyan
