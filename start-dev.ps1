# ===== CATCHCORNERSTATS - DEVELOPMENT STARTUP SCRIPT =====

Write-Host "Starting CatchCornerStats Development Environment..." -ForegroundColor Green
Write-Host ""

# Check if .NET is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ .NET is not installed or not in PATH" -ForegroundColor Red
    exit 1
}

# Function to check if a port is in use
function Test-Port {
    param([int]$Port)
    $connection = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
    return $connection -ne $null
}

# Check if ports are available
$presentationPort = 7254
$webPort = 7032

Write-Host "Checking port availability..." -ForegroundColor Yellow

if (Test-Port $presentationPort) {
    Write-Host "⚠ Port $presentationPort (Presentation) is already in use" -ForegroundColor Yellow
} else {
    Write-Host "✓ Port $presentationPort (Presentation) is available" -ForegroundColor Green
}

if (Test-Port $webPort) {
    Write-Host "⚠ Port $webPort (Web) is already in use" -ForegroundColor Yellow
} else {
    Write-Host "✓ Port $webPort (Web) is available" -ForegroundColor Green
}

Write-Host ""

# Start Presentation project (Backend)
Write-Host "Starting Presentation project (Backend)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd 'CatchCornerStats.Presentation'; dotnet run" -WindowStyle Normal

# Wait a moment for the backend to start
Start-Sleep -Seconds 3

# Start Web project (Frontend)
Write-Host "Starting Web project (Frontend)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd 'CatchCornerStats.Web'; dotnet run" -WindowStyle Normal

Write-Host ""
Write-Host "Development environment is starting..." -ForegroundColor Green
Write-Host ""
Write-Host "URLs:" -ForegroundColor Yellow
Write-Host "  Backend API: https://localhost:$presentationPort" -ForegroundColor White
Write-Host "  Swagger UI:  https://localhost:$presentationPort/swagger" -ForegroundColor White
Write-Host "  Frontend:    https://localhost:$webPort" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to open the applications in your browser..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Open applications in browser
Start-Process "https://localhost:$presentationPort/swagger"
Start-Sleep -Seconds 2
Start-Process "https://localhost:$webPort"

Write-Host ""
Write-Host "✓ Development environment started successfully!" -ForegroundColor Green
Write-Host "  Both projects are now running in separate PowerShell windows." -ForegroundColor White
Write-Host "  Close those windows to stop the development servers." -ForegroundColor White 