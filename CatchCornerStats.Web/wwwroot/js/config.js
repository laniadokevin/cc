// ===== CATCHCORNERSTATS - API CONFIGURATION =====

// API Configuration
const API_CONFIG = {
    // Base URL for the Presentation API
    BASE_URL: 'https://localhost:7254',
    
    // API Endpoints
    ENDPOINTS: {
        GET_ALL_STATS_RAW: '/api/Stats/GetAllStatsRaw',
        GET_AVERAGE_LEAD_TIME: '/api/Stats/GetAverageLeadTime',
        GET_LEAD_TIME_BREAKDOWN: '/api/Stats/GetLeadTimeBreakdown',
        GET_BOOKINGS_BY_DAY: '/api/Stats/GetBookingsByDay',
        GET_BOOKINGS_BY_START_TIME: '/api/Stats/GetBookingsByStartTime',
        GET_BOOKING_DURATION_BREAKDOWN: '/api/Stats/GetBookingDurationBreakdown',
        GET_MONTHLY_REPORT: '/api/Stats/GetMonthlyReport',
        GET_BOOKING_STATS_BY_SPORT: '/api/Stats/GetBookingStatsBySport'
    }
};

// Helper function to build API URLs
function buildApiUrl(endpoint, params = null) {
    let url = API_CONFIG.BASE_URL + endpoint;
    
    if (params && params.toString()) {
        url += '?' + params.toString();
    }
    
    return url;
}

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { API_CONFIG, buildApiUrl };
} 