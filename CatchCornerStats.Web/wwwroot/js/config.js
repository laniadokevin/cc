// ===== CATCHCORNERSTATS - API CONFIGURATION =====

// Configuración centralizada para CatchCornerStats
const CatchCornerStatsConfig = {
    // API Configuration
    api: {
        baseUrl: 'https://localhost:7254/api',
        stats: {
            averageLeadTime: '/Stats/GetAverageLeadTime',
            bookingsByDay: '/Stats/GetBookingsByDay',
            bookingsByStartTime: '/Stats/GetBookingsByStartTime',
            bookingDurationBreakdown: '/Stats/GetBookingDurationBreakdown',
            leadTimeBreakdown: '/Stats/GetLeadTimeBreakdown',
            monthlyReport: '/Stats/GetMonthlyReport',
            sportComparison: '/Stats/GetSportComparison'
        },
        filters: {
            sports: '/Stats/GetSports',
            cities: '/Stats/GetCities',
            rinkSizes: '/Stats/GetRinkSizes',
            facilities: '/Stats/GetFacilities',
            allFilters: '/Stats/GetAllFilters'
        }
    },
    
    // Cache Configuration
    cache: {
        filterExpiry: 30 * 60 * 1000, // 30 minutes
        localStoragePrefix: 'catchCornerStats_'
    },
    
    // UI Configuration
    ui: {
        loadingDelay: 300, // ms
        errorTimeout: 5000, // ms
        refreshInterval: 30000 // ms
    },
    
    // Debug Configuration
    debug: {
        enabled: true,
        logPrefix: '[CatchCornerStats]'
    }
};

// Helper functions
const ConfigHelpers = {
    // Get full API URL
    getApiUrl: function(endpoint) {
        return CatchCornerStatsConfig.api.baseUrl + endpoint;
    },
    
    // Get stats API URL
    getStatsUrl: function(statsEndpoint) {
        return this.getApiUrl(CatchCornerStatsConfig.api.stats[statsEndpoint]);
    },
    
    // Get filter API URL
    getFilterUrl: function(filterType) {
        return this.getApiUrl(CatchCornerStatsConfig.api.filters[filterType]);
    },
    
    // Log with prefix
    log: function(message, ...args) {
        if (CatchCornerStatsConfig.debug.enabled) {
            console.log(CatchCornerStatsConfig.debug.logPrefix, message, ...args);
        }
    },
    
    // Log error with prefix
    logError: function(message, ...args) {
        if (CatchCornerStatsConfig.debug.enabled) {
            console.error(CatchCornerStatsConfig.debug.logPrefix, message, ...args);
        }
    }
};

// Make config available globally
window.CatchCornerStatsConfig = CatchCornerStatsConfig;
window.ConfigHelpers = ConfigHelpers;

console.log('[Config] Configuración cargada:', CatchCornerStatsConfig);

// Helper function to build API URLs
function buildApiUrl(endpoint, params = null) {
    let url = CatchCornerStatsConfig.api.baseUrl + endpoint;
    
    if (params && params.toString()) {
        url += '?' + params.toString();
    }
    
    return url;
}

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { CatchCornerStatsConfig, buildApiUrl };
} 