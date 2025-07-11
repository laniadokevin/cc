# 🏒 CatchCornerStats

A modern web application for sports facility booking statistics and analytics. Built with ASP.NET Core and featuring a clean, responsive interface.

## 📋 Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Usage](#usage)
- [API Documentation](#api-documentation)
- [Configuration](#configuration)
- [Contributing](#contributing)
- [License](#license)

## ✨ Features

### 🎯 Core Features
- **Real-time Dashboard**: Live statistics and metrics
- **Advanced Analytics**: Comprehensive data analysis and insights
- **Report Generation**: PDF, Excel, and CSV export capabilities
- **Filtering System**: Multi-dimensional data filtering
- **Responsive Design**: Works on desktop, tablet, and mobile devices

### 📊 Statistics & Analytics
- Total bookings and revenue tracking
- Average lead time analysis
- Booking patterns by day and time
- Sport comparison reports
- Facility utilization metrics
- Customer behavior insights

### 🎨 User Interface
- Modern, clean design with Bootstrap 5
- Interactive charts with Chart.js
- Real-time data updates
- Dark/light mode support
- Customizable dashboard widgets

## 🏗️ Architecture

The project follows a clean architecture pattern with the following structure:

```
CatchCornerStats/
├── CatchCornerStats.Web/          # Web application (MVC)
├── CatchCornerStats.Presentation/ # API layer
├── CatchCornerStats.Core/         # Business logic & entities
├── CatchCornerStats.Data/         # Data access layer
└── README.md
```

### Technology Stack
- **Backend**: ASP.NET Core 8.0
- **Frontend**: HTML5, CSS3, JavaScript (ES6+)
- **UI Framework**: Bootstrap 5.3
- **Charts**: Chart.js
- **Database**: SQL Server (with Entity Framework)
- **Icons**: Bootstrap Icons

## 🔧 Prerequisites

Before running this application, ensure you have the following installed:

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express or higher)
- [Git](https://git-scm.com/)

## 🚀 Installation

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/CatchCornerStats.git
cd CatchCornerStats
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Configure Database

1. Update the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CatchCornerStats;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

2. Run the database migrations:
```bash
cd CatchCornerStats.Data
dotnet ef database update
```

### 4. Build the Application

```bash
dotnet build
```

### 5. Run the Application

```bash
# Run the Web application
cd CatchCornerStats.Web
dotnet run

# Run the API application (in a separate terminal)
cd CatchCornerStats.Presentation
dotnet run
```

The application will be available at:
- **Web App**: https://localhost:7032
- **API**: https://localhost:7254

### 6. Quick Start Script

For easier development, you can use the provided PowerShell script:

```powershell
# Start both projects automatically
.\start-dev.ps1

# Test CORS configuration
.\test-cors.ps1
```

## 📖 Usage

### Dashboard

The main dashboard provides an overview of key metrics:

1. **Stats Cards**: View total bookings, average lead time, revenue, and active facilities
2. **Charts**: Interactive visualizations of booking patterns and trends
3. **Filters**: Apply filters by sport, city, facility, date range, etc.
4. **Auto Refresh**: Enable automatic data updates every 30 seconds

### Analytics

Access advanced analytics features:

1. **Performance Trends**: View booking and revenue trends over time
2. **Seasonal Analysis**: Analyze booking patterns by season
3. **Revenue Distribution**: Breakdown of revenue by sport type
4. **Detailed Tables**: Comprehensive data tables with sorting and filtering

### Reports

Generate and download reports:

1. **Report Types**: Choose from monthly summary, performance analysis, or customer insights
2. **Customization**: Set date ranges, formats (PDF/Excel/CSV), and include charts
3. **Scheduling**: Set up automatic report generation
4. **History**: View and download previously generated reports

### Settings

Configure application preferences:

1. **General**: Language, timezone, date format, and theme settings
2. **Dashboard**: Widget visibility, auto-refresh intervals, and default views
3. **Notifications**: Email and in-app notification preferences
4. **Security**: Session timeout, password policies, and authentication settings
5. **Integrations**: Connect with external services like Google Calendar

## 🔌 API Documentation

The application exposes RESTful APIs for data access:

### Base URL
```
https://localhost:7254/api
```

### Core Analysis Endpoints

#### Lead Time Analysis
- `GET /Stats/GetAverageLeadTime` - Get average booking lead time (OrderPlacedDate vs OrderDate)
- `GET /Stats/GetLeadTimeBreakdown` - Get lead time distribution by days in advance

#### Bookings Analysis  
- `GET /Stats/GetBookingsByDay` - Get bookings by day of week (OrderDate)
- `GET /Stats/GetBookingsByStartTime` - Get bookings by start time
- `GET /Stats/GetBookingDurationBreakdown` - Get booking duration distribution (Start time to End time)

#### Advanced Analysis
- `GET /Stats/GetMonthlyReport` - Get monthly booking reports with drop flags (50%+ decrease)
- `GET /Stats/GetSportComparison` - Get sport comparison data with ranking flags (Top 6/8, 60+ bookings)

#### Entities
- `GET /Arena` - Get all arenas
- `GET /Booking` - Get all bookings
- `GET /Organization` - Get all organizations
- `GET /Neighborhood` - Get all neighborhoods

### Query Parameters

Most endpoints support filtering parameters:
- `sport` - Filter by sport type
- `city` - Filter by city
- `rinkSize` - Filter by rink size (Full court - 5 on 5, etc.)
- `facility` - Filter by facility
- `month` - Filter by month (1-12)
- `dayOfWeek` - Filter by day of week (0-6)

### Example Requests

```bash
# Lead time analysis for hockey in Toronto
curl "https://localhost:7254/api/Stats/GetAverageLeadTime?sport=hockey&city=Toronto"

# Bookings by day for basketball
curl "https://localhost:7254/api/Stats/GetBookingsByDay?sport=basketball&city=Toronto"

# Sport comparison with flags
curl "https://localhost:7254/api/Stats/GetSportComparison?city=Toronto&month=3"
```

## ⚙️ Configuration

### Environment Variables

Set the following environment variables for production:

```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=your_production_connection_string
```

### App Settings

Key configuration options in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ApiSettings": {
    "BaseUrl": "https://localhost:7254/api",
    "Timeout": 30
  }
}
```

## 🛠️ Development

### Project Structure

```
CatchCornerStats.Web/
├── Controllers/          # MVC Controllers
│   └── HomeController.cs # Core analysis views
├── Models/              # View Models
├── Views/               # Razor Views
│   ├── Home/           # Core analysis dashboard pages
│   │   ├── Index.cshtml                    # Main dashboard
│   │   ├── GetAverageLeadTime.cshtml       # Lead time analysis
│   │   ├── GetLeadTimeBreakdown.cshtml     # Lead time distribution
│   │   ├── GetBookingsByDay.cshtml         # Bookings by day
│   │   ├── GetBookingsByStartTime.cshtml   # Bookings by start time
│   │   ├── GetBookingDurationBreakdown.cshtml # Duration analysis
│   │   ├── GetMonthlyReport.cshtml         # Monthly reports with flags
│   │   └── GetSportComparison.cshtml       # Sport comparison with flags
│   └── Shared/         # Layout and partial views
└── wwwroot/            # Static files
    ├── css/            # Stylesheets
    ├── js/             # JavaScript files
    └── lib/            # Third-party libraries
```

### Core Analysis Views

The application provides 5 main analysis categories:

1. **Lead Time Analysis** - OrderPlacedDate vs OrderDate analysis
2. **Bookings Analysis** - Day and time patterns
3. **Duration Analysis** - Booking length patterns  
4. **Monthly Reports** - Trend analysis with drop flags
5. **Sport Comparison** - Per-city ranking with performance flags

### Adding New Features

1. **New API Endpoint**:
   - Add method to appropriate controller in `CatchCornerStats.Presentation`
   - Update interface in `CatchCornerStats.Core`
   - Implement in `CatchCornerStats.Data`

2. **New Dashboard Widget**:
   - Create view component or partial view
   - Add JavaScript for interactivity
   - Update dashboard layout

3. **New Chart**:
   - Add canvas element to view
   - Create chart configuration in JavaScript
   - Connect to API endpoint

### Code Style

- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Include error handling and logging

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines

- Write unit tests for new features
- Update documentation for API changes
- Ensure responsive design works on all devices
- Test with different browsers

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

If you encounter any issues or have questions:

1. Check the [Issues](https://github.com/yourusername/CatchCornerStats/issues) page
2. Create a new issue with detailed information
3. Contact the development team

## 🙏 Acknowledgments

- [Bootstrap](https://getbootstrap.com/) for the UI framework
- [Chart.js](https://www.chartjs.org/) for data visualization
- [Bootstrap Icons](https://icons.getbootstrap.com/) for icons
- [ASP.NET Core](https://dotnet.microsoft.com/apps/aspnet) for the web framework

---

**Made with ❤️ for sports facility management**

## 🔧 CORS Configuration

This application uses a multi-project architecture where the Web frontend communicates with the Presentation API backend. CORS (Cross-Origin Resource Sharing) is configured to allow this communication.

### CORS Setup

The CORS configuration is automatically set up in the Presentation project (`Program.cs`) to allow requests from the Web project's ports:

- **Web Project Ports**: 5069 (HTTP), 7032 (HTTPS), 45332 (IIS Express), 44347 (IIS Express SSL)
- **API Project Port**: 7254 (HTTPS)

### Troubleshooting CORS Issues

If you encounter CORS errors, follow these steps:

1. **Verify Both Projects Are Running**:
   ```bash
   # Check if ports are in use
   netstat -an | findstr "7254"
   netstat -an | findstr "7032"
   ```

2. **Test API Connectivity**:
   - Open https://localhost:7254/swagger in your browser
   - Verify the API endpoints are accessible

3. **Check Browser Console**:
   - Open browser developer tools (F12)
   - Look for CORS error messages in the Console tab

4. **Run CORS Test Script**:
   ```powershell
   .\test-cors.ps1
   ```

5. **Common Solutions**:
   - Restart both projects
   - Clear browser cache
   - Accept SSL certificates: `dotnet dev-certs https --trust`
   - Check firewall settings

For detailed CORS configuration information, see [CORS_SETUP.md](CORS_SETUP.md).
