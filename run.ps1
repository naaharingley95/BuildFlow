# BuildFlow — Quick Start Script
# Double-click or run: powershell -ExecutionPolicy Bypass -File run.ps1

$env:PATH = "$env:USERPROFILE\.dotnet;$env:PATH"
$env:DOTNET_ROOT = "$env:USERPROFILE\.dotnet"
$env:ASPNETCORE_ENVIRONMENT = "Development"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   BuildFlow - Construction Management  " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Starting server..." -ForegroundColor Yellow
Write-Host "Database: SQLite (buildflow.db)" -ForegroundColor Gray
Write-Host ""
Write-Host "Login URL : http://localhost:5000" -ForegroundColor Green
Write-Host "Admin     : admin@buildflow.local" -ForegroundColor White
Write-Host "Password  : BuildFlow@Admin2024!" -ForegroundColor White
Write-Host ""
Write-Host "Press Ctrl+C to stop the server." -ForegroundColor DarkGray
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Set-Location $PSScriptRoot
& "$env:USERPROFILE\.dotnet\dotnet.exe" run

