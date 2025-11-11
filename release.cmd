@echo off
REM Quick release script - bumps patch version
REM For more control, use: .\release.ps1 [major|minor|patch]

echo Running automated release...
powershell -ExecutionPolicy Bypass -File release.ps1 patch
