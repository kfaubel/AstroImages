#!/usr/bin/env pwsh#!/usr/bin/env pwsh

# Release script for AstroImages# Release script for AstroImages

# Usage: .\release.ps1 [major|minor|patch]# Usage: .\release.ps1 [major|minor|patch]

# Example: .\release.ps1 patch    -> 1.0.7 -> 1.0.8# Example: .\release.ps1 patch    -> 1.0.2 -> 1.0.3



param(param(

    [Parameter(Mandatory=$false)]    [Parameter(Mandatory=$false)]

    [ValidateSet('major', 'minor', 'patch')]    [ValidateSet('major', 'minor', 'patch')]

    [string]$BumpType = 'patch'    [string]$BumpType = 'patch'

))



$ErrorActionPreference = "Stop"$ErrorActionPreference = "Stop"



# Read current version from .csproj# Read current version from .csproj

$csprojPath = "AstroImages.Wpf\AstroImages.Wpf.csproj"$csprojPath = "AstroImages.Wpf\AstroImages.Wpf.csproj"

$csprojContent = Get-Content $csprojPath -Raw$csprojContent = Get-Content $csprojPath -Raw



# Extract current version using regex# Extract current version using regex

if ($csprojContent -match '<Version>(\d+\.\d+\.\d+)</Version>') {if ($csprojContent -match '<Version>(\d+\.\d+\.\d+)</Version>') {

    $currentVersion = $Matches[1]    $currentVersion = $Matches[1]

} else {} else {

    Write-Host "Error: Could not find version in $csprojPath" -ForegroundColor Red    Write-Host "Error: Could not find version in $csprojPath" -ForegroundColor Red

    exit 1    exit 1

}}



Write-Host "Current version: $currentVersion" -ForegroundColor CyanWrite-Host "Current version: $currentVersion" -ForegroundColor Cyan



# Parse version# Parse version

$versionParts = $currentVersion.Split('.')$versionParts = $currentVersion.Split('.')

$major = [int]$versionParts[0]$major = [int]$versionParts[0]

$minor = [int]$versionParts[1]$minor = [int]$versionParts[1]

$patch = [int]$versionParts[2]$patch = [int]$versionParts[2]



# Bump version based on type# Bump version based on type

switch ($BumpType) {switch ($BumpType) {

    'major' {     'major' { 

        $major++        $major++

        $minor = 0        $minor = 0

        $patch = 0        $patch = 0

    }    }

    'minor' {     'minor' { 

        $minor++        $minor++

        $patch = 0        $patch = 0

    }    }

    'patch' {     'patch' { 

        $patch++        $patch++

    }    }

}}



$newVersion = "$major.$minor.$patch"$newVersion = "$major.$minor.$patch"

$newVersionFull = "$major.$minor.$patch.0"$newVersionFull = "$major.$minor.$patch.0"

$tagName = "v$newVersion"$tagName = "v$newVersion"



Write-Host "New version: $newVersion" -ForegroundColor GreenWrite-Host "New version: $newVersion" -ForegroundColor Green

Write-Host "Tag: $tagName" -ForegroundColor GreenWrite-Host "Tag: $tagName" -ForegroundColor Green



# Ask for confirmation# Ask for confirmation

$confirm = Read-Host "`nContinue with release? (y/n)"$confirm = Read-Host "`nContinue with release? (y/n)"

if ($confirm -ne 'y' -and $confirm -ne 'Y') {if ($confirm -ne 'y' -and $confirm -ne 'Y') {

    Write-Host "Release cancelled." -ForegroundColor Yellow    Write-Host "Release cancelled." -ForegroundColor Yellow

    exit 0    exit 0

}}



# Update version in .csproj# Update version in .csproj

Write-Host "`nUpdating version in $csprojPath..." -ForegroundColor CyanWrite-Host "`nUpdating version in $csprojPath..." -ForegroundColor Cyan



try {try {

    # Read file content    # Read file content

    $csprojContent = Get-Content $csprojPath -Raw    $csprojContent = Get-Content $csprojPath -Raw

        

    # Update all version-related tags    # Update all version-related tags

    $csprojContent = $csprojContent -replace '<Version>\d+\.\d+\.\d+</Version>', "<Version>$newVersion</Version>"    $csprojContent = $csprojContent -replace '<Version>\d+\.\d+\.\d+</Version>', "<Version>$newVersion</Version>"

    $csprojContent = $csprojContent -replace '<AssemblyVersion>\d+\.\d+\.\d+\.\d+</AssemblyVersion>', "<AssemblyVersion>$newVersionFull</AssemblyVersion>"    $csprojContent = $csprojContent -replace '<AssemblyVersion>\d+\.\d+\.\d+\.\d+</AssemblyVersion>', "<AssemblyVersion>$newVersionFull</AssemblyVersion>"

    $csprojContent = $csprojContent -replace '<FileVersion>\d+\.\d+\.\d+\.\d+</FileVersion>', "<FileVersion>$newVersionFull</FileVersion>"    $csprojContent = $csprojContent -replace '<FileVersion>\d+\.\d+\.\d+\.\d+</FileVersion>', "<FileVersion>$newVersionFull</FileVersion>"

        

    # Save the file    # Save the file

    Set-Content -Path $csprojPath -Value $csprojContent -NoNewline    Set-Content -Path $csprojPath -Value $csprojContent -NoNewline

        

    Write-Host "✓ Version updated" -ForegroundColor Green    Write-Host "✓ Version updated" -ForegroundColor Green

}}

catch {catch {

    Write-Host "Error updating version: $_" -ForegroundColor Red    Write-Host "Error updating version: $_" -ForegroundColor Red

    exit 1    exit 1

}}



# Git operations# Git operations

Write-Host "`nGit operations..." -ForegroundColor CyanWrite-Host "`nGit operations..." -ForegroundColor Cyan



# Check for uncommitted changes (excluding the .csproj we just changed)# Check for uncommitted changes (excluding the .csproj we just changed)

$status = git status --porcelain$status = git status --porcelain

$otherChanges = $status | Where-Object { $_ -notmatch 'AstroImages.Wpf.csproj' }$otherChanges = $status | Where-Object { $_ -notmatch 'AstroImages.Wpf.csproj' }



if ($otherChanges) {if ($otherChanges) {

    Write-Host "Warning: You have uncommitted changes:" -ForegroundColor Yellow    Write-Host "Warning: You have uncommitted changes:" -ForegroundColor Yellow

    Write-Host $otherChanges    Write-Host $otherChanges

    $confirmCommit = Read-Host "`nCommit all changes? (y/n)"    $confirmCommit = Read-Host "`nCommit all changes? (y/n)"

    if ($confirmCommit -eq 'y' -or $confirmCommit -eq 'Y') {    if ($confirmCommit -eq 'y' -or $confirmCommit -eq 'Y') {

        git add -A        git add -A

        git commit -m "Release $newVersion"        git commit -m "Release $newVersion"

    } else {    } else {

        Write-Host "Please commit or stash changes first." -ForegroundColor Red        Write-Host "Please commit or stash changes first." -ForegroundColor Red

        exit 1        exit 1

    }    }

} else {} else {

    # Only commit the .csproj change    # Only commit the .csproj change

    git add $csprojPath    git add $csprojPath

    git commit -m "Bump version to $newVersion"    git commit -m "Bump version to $newVersion"

}}



Write-Host "✓ Changes committed" -ForegroundColor GreenWrite-Host "✓ Changes committed" -ForegroundColor Green



# Create and push tag# Create and push tag

Write-Host "`nCreating tag $tagName..." -ForegroundColor CyanWrite-Host "`nCreating tag $tagName..." -ForegroundColor Cyan

git tag -a $tagName -m "Release $newVersion"git tag -a $tagName -m "Release $newVersion"

Write-Host "✓ Tag created" -ForegroundColor GreenWrite-Host "✓ Tag created" -ForegroundColor Green



Write-Host "`nPushing to GitHub..." -ForegroundColor CyanWrite-Host "`nPushing to GitHub..." -ForegroundColor Cyan

git push origin maingit push origin main

git push origin $tagNamegit push origin $tagName

Write-Host "✓ Pushed to GitHub" -ForegroundColor GreenWrite-Host "✓ Pushed to GitHub" -ForegroundColor Green



Write-Host "`n🎉 Release $newVersion initiated!" -ForegroundColor GreenWrite-Host "`n🎉 Release $newVersion initiated!" -ForegroundColor Green

Write-Host "Check GitHub Actions: https://github.com/kfaubel/AstroImages/actions" -ForegroundColor CyanWrite-Host "Check GitHub Actions: https://github.com/kfaubel/AstroImages/actions" -ForegroundColor Cyan

