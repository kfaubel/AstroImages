param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('major', 'minor', 'patch')]
    [string]$BumpType = 'patch'
)

$ErrorActionPreference = "Stop"

$csprojPath = "AstroImages.Wpf\AstroImages.Wpf.csproj"
$csprojContent = Get-Content $csprojPath -Raw

if ($csprojContent -match '<Version>(\d+\.\d+\.\d+)</Version>') {
    $currentVersion = $Matches[1]
} else {
    Write-Host "Error: Could not find version" -ForegroundColor Red
    exit 1
}

Write-Host "Current version: $currentVersion" -ForegroundColor Cyan

$versionParts = $currentVersion.Split('.')
$major = [int]$versionParts[0]
$minor = [int]$versionParts[1]
$patch = [int]$versionParts[2]

switch ($BumpType) {
    'major' { $major++; $minor = 0; $patch = 0 }
    'minor' { $minor++; $patch = 0 }
    'patch' { $patch++ }
}

$newVersion = "$major.$minor.$patch"
$newVersionFull = "$major.$minor.$patch.0"
$tagName = "v$newVersion"

Write-Host "New version: $newVersion" -ForegroundColor Green
Write-Host "Tag: $tagName" -ForegroundColor Green

$confirm = Read-Host "`nContinue? (y/n)"
if ($confirm -ne 'y' -and $confirm -ne 'Y') {
    Write-Host "Cancelled" -ForegroundColor Yellow
    exit 0
}

Write-Host "`nUpdating $csprojPath..." -ForegroundColor Cyan
$csprojContent = Get-Content $csprojPath -Raw
$csprojContent = $csprojContent -replace '<Version>\d+\.\d+\.\d+</Version>', "<Version>$newVersion</Version>"
$csprojContent = $csprojContent -replace '<AssemblyVersion>\d+\.\d+\.\d+\.\d+</AssemblyVersion>', "<AssemblyVersion>$newVersionFull</AssemblyVersion>"
$csprojContent = $csprojContent -replace '<FileVersion>\d+\.\d+\.\d+\.\d+</FileVersion>', "<FileVersion>$newVersionFull</FileVersion>"
Set-Content -Path $csprojPath -Value $csprojContent -NoNewline
Write-Host "Version updated" -ForegroundColor Green

Write-Host "`nGit operations..." -ForegroundColor Cyan
git add $csprojPath
git commit -m "Bump version to $newVersion"
git tag -a $tagName -m "Release $newVersion"
git push origin main
git push origin $tagName

Write-Host "`nRelease $newVersion complete!" -ForegroundColor Green
Write-Host "https://github.com/kfaubel/AstroImages/actions" -ForegroundColor Cyan
