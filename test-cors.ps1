# ===== CATCHCORNERSTATS - CORS TEST SCRIPT =====

Write-Host "Testing CatchCornerStats CORS Configuration..." -ForegroundColor Green
Write-Host ""

$presentationPort = 7254
$webPort = 7032

# Function to test if a URL is accessible
function Test-Url {
    param([string]$Url, [string]$Description)
    
    try {
        $response = Invoke-WebRequest -Uri $Url -Method GET -TimeoutSec 10 -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            Write-Host "✓ $Description - Status: $($response.StatusCode)" -ForegroundColor Green
            return $true
        } else {
            Write-Host "✗ $Description - Status: $($response.StatusCode)" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "✗ $Description - Error: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to test CORS headers
function Test-CorsHeaders {
    param([string]$ApiUrl, [string]$Origin)
    
    try {
        $headers = @{
            'Origin' = $Origin
            'Access-Control-Request-Method' = 'GET'
            'Access-Control-Request-Headers' = 'Content-Type'
        }
        
        $response = Invoke-WebRequest -Uri $ApiUrl -Method OPTIONS -Headers $headers -TimeoutSec 10 -UseBasicParsing
        
        $corsHeaders = @(
            'Access-Control-Allow-Origin',
            'Access-Control-Allow-Methods',
            'Access-Control-Allow-Headers'
        )
        
        $missingHeaders = @()
        foreach ($header in $corsHeaders) {
            if (-not $response.Headers.ContainsKey($header)) {
                $missingHeaders += $header
            }
        }
        
        if ($missingHeaders.Count -eq 0) {
            Write-Host "✓ CORS headers present for origin: $Origin" -ForegroundColor Green
            return $true
        } else {
            Write-Host "✗ Missing CORS headers for origin: $Origin - Missing: $($missingHeaders -join ', ')" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "✗ CORS test failed for origin: $Origin - Error: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Test basic connectivity
Write-Host "Testing basic connectivity..." -ForegroundColor Yellow

$backendUrl = "https://localhost:$presentationPort"
$swaggerUrl = "https://localhost:$presentationPort/swagger"
$apiUrl = "https://localhost:$presentationPort/api/Stats/GetAllStatsRaw"
$frontendUrl = "https://localhost:$webPort"

$backendAccessible = Test-Url $backendUrl "Backend API"
$swaggerAccessible = Test-Url $swaggerUrl "Swagger UI"
$frontendAccessible = Test-Url $frontendUrl "Frontend"

Write-Host ""

# Test CORS configuration
Write-Host "Testing CORS configuration..." -ForegroundColor Yellow

$origins = @(
    "https://localhost:$webPort",
    "http://localhost:5069",
    "http://localhost:45332",
    "https://localhost:44347"
)

$corsTests = @()
foreach ($origin in $origins) {
    $corsTests += Test-CorsHeaders $apiUrl $origin
}

Write-Host ""

# Summary
Write-Host "=== TEST SUMMARY ===" -ForegroundColor Cyan

if ($backendAccessible) {
    Write-Host "✓ Backend is accessible" -ForegroundColor Green
} else {
    Write-Host "✗ Backend is not accessible - Make sure the Presentation project is running" -ForegroundColor Red
}

if ($swaggerAccessible) {
    Write-Host "✓ Swagger UI is accessible" -ForegroundColor Green
} else {
    Write-Host "✗ Swagger UI is not accessible" -ForegroundColor Red
}

if ($frontendAccessible) {
    Write-Host "✓ Frontend is accessible" -ForegroundColor Green
} else {
    Write-Host "✗ Frontend is not accessible - Make sure the Web project is running" -ForegroundColor Red
}

$corsSuccessCount = ($corsTests | Where-Object { $_ -eq $true }).Count
$corsTotalCount = $corsTests.Count

if ($corsSuccessCount -eq $corsTotalCount) {
    Write-Host "✓ CORS is properly configured for all origins" -ForegroundColor Green
} else {
    Write-Host "✗ CORS configuration issues detected ($corsSuccessCount/$corsTotalCount origins working)" -ForegroundColor Red
}

Write-Host ""

# Recommendations
if (-not $backendAccessible) {
    Write-Host "RECOMMENDATIONS:" -ForegroundColor Yellow
    Write-Host "1. Start the Presentation project: cd CatchCornerStats.Presentation && dotnet run" -ForegroundColor White
    Write-Host "2. Check if port $presentationPort is available" -ForegroundColor White
    Write-Host "3. Verify the database connection string in appsettings.json" -ForegroundColor White
}

if (-not $frontendAccessible) {
    Write-Host "RECOMMENDATIONS:" -ForegroundColor Yellow
    Write-Host "1. Start the Web project: cd CatchCornerStats.Web && dotnet run" -ForegroundColor White
    Write-Host "2. Check if port $webPort is available" -ForegroundColor White
}

if ($corsSuccessCount -lt $corsTotalCount) {
    Write-Host "RECOMMENDATIONS:" -ForegroundColor Yellow
    Write-Host "1. Check the CORS configuration in CatchCornerStats.Presentation/Program.cs" -ForegroundColor White
    Write-Host "2. Ensure all required origins are included in the CORS policy" -ForegroundColor White
    Write-Host "3. Restart the Presentation project after making changes" -ForegroundColor White
}

Write-Host ""
Write-Host "Test completed!" -ForegroundColor Green 