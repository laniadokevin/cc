# CORS Configuration for CatchCornerStats

## Overview
This document explains the CORS (Cross-Origin Resource Sharing) configuration for the CatchCornerStats application and how to resolve common issues.

## Architecture
- **Web Project**: Frontend application (ports: 5069 HTTP, 7032 HTTPS)
- **Presentation Project**: API backend (port: 7254 HTTPS)

## CORS Configuration

### Presentation Project (Backend)
The CORS configuration is set up in `CatchCornerStats.Presentation/Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                // Web project ports
                "http://localhost:5069",
                "https://localhost:7032",
                "http://localhost:45332",
                "https://localhost:44347",
                // Development ports
                "http://localhost:3000",
                "http://localhost:5000",
                "https://localhost:5001"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

### Web Project (Frontend)
API calls are configured to use the correct backend URL:
- Base URL: `https://localhost:7254`
- Configuration file: `wwwroot/js/config.js`

## Common CORS Issues and Solutions

### 1. "Access to fetch at '...' from origin '...' has been blocked by CORS policy"

**Solution**: Ensure both projects are running and the CORS configuration includes the correct origin.

**Steps**:
1. Verify the Presentation project is running on port 7254
2. Verify the Web project is running on port 7032 (HTTPS) or 5069 (HTTP)
3. Check that the origin is included in the CORS policy

### 2. "Failed to fetch" error

**Solution**: Check if the backend API is accessible.

**Steps**:
1. Open `https://localhost:7254/swagger` in your browser
2. Verify the API endpoints are working
3. Check the browser's developer tools Network tab for detailed error information

### 3. SSL Certificate Issues

**Solution**: Accept the development certificate or use HTTP for development.

**Steps**:
1. Run `dotnet dev-certs https --trust` to trust the development certificate
2. Or use HTTP URLs for development (update config.js accordingly)

## Development Setup

### 1. Start the Backend (Presentation Project)
```bash
cd CatchCornerStats.Presentation
dotnet run
```

### 2. Start the Frontend (Web Project)
```bash
cd CatchCornerStats.Web
dotnet run
```

### 3. Verify Configuration
- Backend should be accessible at: `https://localhost:7254/swagger`
- Frontend should be accessible at: `https://localhost:7032`

## API Configuration

### Configuration File
The API configuration is centralized in `CatchCornerStats.Web/wwwroot/js/config.js`:

```javascript
const API_CONFIG = {
    BASE_URL: 'https://localhost:7254',
    ENDPOINTS: {
        GET_ALL_STATS_RAW: '/api/Stats/GetAllStatsRaw',
        // ... other endpoints
    }
};
```

### Using the API
```javascript
// Simple API call
const data = await API_UTILS.fetchApi(API_CONFIG.ENDPOINTS.GET_ALL_STATS_RAW);

// API call with parameters
const params = API_UTILS.buildParams({
    sport: 'Hockey',
    city: 'Toronto'
});
const data = await API_UTILS.fetchApi(API_CONFIG.ENDPOINTS.GET_ALL_STATS_RAW, { params });
```

## Troubleshooting

### Check if CORS is Working
1. Open browser developer tools (F12)
2. Go to Network tab
3. Make an API call
4. Check if the request has CORS headers in the response

### Debug CORS Issues
1. Check the browser console for CORS error messages
2. Verify the request origin matches the allowed origins in the CORS policy
3. Ensure the backend is running and accessible
4. Check if the API endpoint exists and is working

### Common Error Messages
- `Access to fetch at '...' from origin '...' has been blocked by CORS policy`: Origin not allowed in CORS policy
- `Failed to fetch`: Backend not running or network issue
- `HTTP error! status: 404`: API endpoint not found
- `HTTP error! status: 500`: Server error in the backend

## Production Considerations

For production deployment:
1. Update the CORS policy to only allow specific production domains
2. Use environment variables for API URLs
3. Implement proper authentication and authorization
4. Use HTTPS for all communications

## Files Modified for CORS Setup

1. `CatchCornerStats.Presentation/Program.cs` - CORS configuration
2. `CatchCornerStats.Web/wwwroot/js/config.js` - API configuration
3. `CatchCornerStats.Web/wwwroot/js/api-utils.js` - API utilities
4. `CatchCornerStats.Web/Views/Shared/_Layout.cshtml` - Script includes
5. All view files - Updated API URLs 