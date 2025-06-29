// ===== CATCHCORNERSTATS - MODERN JAVASCRIPT =====

// Global configuration
const CONFIG = {
    API_BASE_URL: 'http://localhost:7254/api',
    CHART_COLORS: [
        '#0d6efd', '#198754', '#ffc107', '#dc3545', 
        '#0dcaf0', '#6f42c1', '#fd7e14', '#20c997'
    ],
    ANIMATION_DURATION: 300,
    REFRESH_INTERVAL: 30000 // 30 seconds
};

// Global state
const AppState = {
    charts: {},
    filters: {},
    isLoading: false,
    lastRefresh: null
};

// ===== UTILITY FUNCTIONS =====

/**
 * Show loading overlay
 */
function showLoading(message = 'Loading...') {
    const overlay = document.getElementById('loadingOverlay');
    if (overlay) {
        overlay.querySelector('.loading-text').textContent = message;
        overlay.classList.remove('d-none');
    }
}

/**
 * Hide loading overlay
 */
function hideLoading() {
    const overlay = document.getElementById('loadingOverlay');
    if (overlay) {
        overlay.classList.add('d-none');
    }
}

/**
 * Show notification
 */
function showNotification(message, type = 'info', duration = 5000) {
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
    alertDiv.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
    
    alertDiv.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    document.body.appendChild(alertDiv);
    
    // Auto remove after duration
    setTimeout(() => {
        if (alertDiv.parentNode) {
            alertDiv.remove();
        }
    }, duration);
}

/**
 * Format number with commas
 */
function formatNumber(num) {
    return new Intl.NumberFormat().format(num);
}

/**
 * Format currency
 */
function formatCurrency(amount, currency = 'USD') {
    return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: currency
    }).format(amount);
}

/**
 * Format date
 */
function formatDate(date, options = {}) {
    const defaultOptions = {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    };
    return new Date(date).toLocaleDateString('en-US', { ...defaultOptions, ...options });
}

/**
 * Format time
 */
function formatTime(time) {
    return new Date(`2000-01-01T${time}`).toLocaleTimeString('en-US', {
        hour: '2-digit',
        minute: '2-digit'
    });
}

/**
 * Get unique values from array
 */
function getUniqueValues(array, key) {
    if (!Array.isArray(array)) return [];
    return [...new Set(array.map(item => item[key]).filter(Boolean))];
}

/**
 * Debounce function
 */
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// ===== API FUNCTIONS =====

/**
 * Make API request
 */
async function apiRequest(endpoint, options = {}) {
    const url = `${CONFIG.API_BASE_URL}/${endpoint}`;
    const defaultOptions = {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
        },
        ...options
    };

    try {
        const response = await fetch(url, defaultOptions);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        return await response.json();
    } catch (error) {
        console.error('API request failed:', error);
        showNotification(`API Error: ${error.message}`, 'danger');
        throw error;
    }
}

/**
 * Get all stats raw data
 */
async function getAllStatsRaw(filters = {}) {
    const queryString = new URLSearchParams(filters).toString();
    return await apiRequest(`Stats/GetAllStatsRaw${queryString ? '?' + queryString : ''}`);
}

/**
 * Get average lead time
 */
async function getAverageLeadTime(filters = {}) {
    const queryString = new URLSearchParams(filters).toString();
    return await apiRequest(`Stats/GetAverageLeadTime${queryString ? '?' + queryString : ''}`);
}

/**
 * Get lead time breakdown
 */
async function getLeadTimeBreakdown(filters = {}) {
    const queryString = new URLSearchParams(filters).toString();
    return await apiRequest(`Stats/GetLeadTimeBreakdown${queryString ? '?' + queryString : ''}`);
}

/**
 * Get bookings by day
 */
async function getBookingsByDay(filters = {}) {
    const queryString = new URLSearchParams(filters).toString();
    return await apiRequest(`Stats/GetBookingsByDay${queryString ? '?' + queryString : ''}`);
}

/**
 * Get bookings by start time
 */
async function getBookingsByStartTime(filters = {}) {
    const queryString = new URLSearchParams(filters).toString();
    return await apiRequest(`Stats/GetBookingsByStartTime${queryString ? '?' + queryString : ''}`);
}

/**
 * Get booking duration breakdown
 */
async function getBookingDurationBreakdown(filters = {}) {
    const queryString = new URLSearchParams(filters).toString();
    return await apiRequest(`Stats/GetBookingDurationBreakdown${queryString ? '?' + queryString : ''}`);
}

/**
 * Get monthly report
 */
async function getMonthlyReport(filters = {}) {
    const queryString = new URLSearchParams(filters).toString();
    return await apiRequest(`Stats/GetMonthlyReport${queryString ? '?' + queryString : ''}`);
}

/**
 * Get sport comparison
 */
async function getSportComparison(filters = {}) {
    const queryString = new URLSearchParams(filters).toString();
    return await apiRequest(`Stats/GetSportComparison${queryString ? '?' + queryString : ''}`);
}

// ===== CHART FUNCTIONS =====

/**
 * Create chart configuration
 */
function createChartConfig(type, data, options = {}) {
    const defaultOptions = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: {
                display: true,
                position: 'top'
            },
            tooltip: {
                enabled: true,
                mode: 'index',
                intersect: false
            }
        },
        scales: {
            y: {
                beginAtZero: true,
                grid: {
                    color: 'rgba(0,0,0,0.1)'
                }
            },
            x: {
                grid: {
                    color: 'rgba(0,0,0,0.1)'
                }
            }
        }
    };

    return {
        type: type,
        data: data,
        options: { ...defaultOptions, ...options }
    };
}

/**
 * Create bar chart
 */
function createBarChart(canvasId, labels, data, label = 'Data', color = CONFIG.CHART_COLORS[0]) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return null;

    // Destroy existing chart
    if (AppState.charts[canvasId]) {
        AppState.charts[canvasId].destroy();
    }

    const chartData = {
        labels: labels,
        datasets: [{
            label: label,
            data: data,
            backgroundColor: color,
            borderColor: color,
            borderWidth: 1
        }]
    };

    const config = createChartConfig('bar', chartData);
    AppState.charts[canvasId] = new Chart(ctx, config);
    
    return AppState.charts[canvasId];
}

/**
 * Create line chart
 */
function createLineChart(canvasId, labels, data, label = 'Data', color = CONFIG.CHART_COLORS[0]) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return null;

    // Destroy existing chart
    if (AppState.charts[canvasId]) {
        AppState.charts[canvasId].destroy();
    }

    const chartData = {
        labels: labels,
        datasets: [{
            label: label,
            data: data,
            borderColor: color,
            backgroundColor: color + '20',
            borderWidth: 2,
            fill: true,
            tension: 0.4
        }]
    };

    const config = createChartConfig('line', chartData);
    AppState.charts[canvasId] = new Chart(ctx, config);
    
    return AppState.charts[canvasId];
}

/**
 * Create pie chart
 */
function createPieChart(canvasId, labels, data) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return null;

    // Destroy existing chart
    if (AppState.charts[canvasId]) {
        AppState.charts[canvasId].destroy();
    }

    const chartData = {
        labels: labels,
        datasets: [{
            data: data,
            backgroundColor: CONFIG.CHART_COLORS.slice(0, labels.length),
            borderWidth: 2,
            borderColor: '#fff'
        }]
    };

    const config = createChartConfig('pie', chartData, {
        plugins: {
            legend: {
                position: 'bottom'
            }
        }
    });
    
    AppState.charts[canvasId] = new Chart(ctx, config);
    return AppState.charts[canvasId];
}

// ===== FILTER FUNCTIONS =====

/**
 * Get current filters
 */
function getCurrentFilters() {
    const filters = {};
    const filterElements = document.querySelectorAll('[data-filter]');
    
    filterElements.forEach(element => {
        const key = element.getAttribute('data-filter');
        const value = element.value;
        if (value && value !== '') {
            filters[key] = value;
        }
    });
    
    return filters;
}

/**
 * Apply filters
 */
async function applyFilters() {
    AppState.filters = getCurrentFilters();
    await refreshDashboard();
    showNotification('Filters applied successfully', 'success');
}

/**
 * Clear filters
 */
function clearFilters() {
    const filterElements = document.querySelectorAll('[data-filter]');
    filterElements.forEach(element => {
        element.value = '';
    });
    AppState.filters = {};
    refreshDashboard();
    showNotification('Filters cleared', 'info');
}

// ===== DASHBOARD FUNCTIONS =====

/**
 * Update stats cards
 */
function updateStatsCards(data) {
    // Update total bookings
    const totalBookings = document.getElementById('totalBookings');
    if (totalBookings && data.totalBookings !== undefined) {
        totalBookings.textContent = formatNumber(data.totalBookings);
    }

    // Update average lead time
    const avgLeadTime = document.getElementById('avgLeadTime');
    if (avgLeadTime && data.averageLeadTime !== undefined) {
        avgLeadTime.textContent = data.averageLeadTime.toFixed(1) + ' days';
    }

    // Update total revenue
    const totalRevenue = document.getElementById('totalRevenue');
    if (totalRevenue && data.totalRevenue !== undefined) {
        totalRevenue.textContent = formatCurrency(data.totalRevenue);
    }

    // Update active facilities
    const activeFacilities = document.getElementById('activeFacilities');
    if (activeFacilities && data.activeFacilities !== undefined) {
        activeFacilities.textContent = formatNumber(data.activeFacilities);
    }
}

/**
 * Refresh dashboard
 */
async function refreshDashboard() {
    try {
        showLoading('Refreshing dashboard...');
        AppState.isLoading = true;

        // Load all data in parallel
        const [
            allStatsRaw,
            averageLeadTime,
            leadTimeBreakdown,
            bookingsByDay,
            bookingsByStartTime,
            bookingDurationBreakdown,
            monthlyReport,
            sportComparison
        ] = await Promise.all([
            getAllStatsRaw(AppState.filters),
            getAverageLeadTime(AppState.filters),
            getLeadTimeBreakdown(AppState.filters),
            getBookingsByDay(AppState.filters),
            getBookingsByStartTime(AppState.filters),
            getBookingDurationBreakdown(AppState.filters),
            getMonthlyReport(AppState.filters),
            getSportComparison(AppState.filters)
        ]);

        // Update charts
        updateCharts({
            allStatsRaw,
            averageLeadTime,
            leadTimeBreakdown,
            bookingsByDay,
            bookingsByStartTime,
            bookingDurationBreakdown,
            monthlyReport,
            sportComparison
        });

        // Update stats cards
        updateStatsCards(allStatsRaw);

        AppState.lastRefresh = new Date();
        showNotification('Dashboard refreshed successfully', 'success');

    } catch (error) {
        console.error('Failed to refresh dashboard:', error);
        showNotification('Failed to refresh dashboard', 'danger');
    } finally {
        hideLoading();
        AppState.isLoading = false;
    }
}

/**
 * Update all charts
 */
function updateCharts(data) {
    // All Stats Raw Chart
    if (data.allStatsRaw && Array.isArray(data.allStatsRaw)) {
        const labels = data.allStatsRaw.map(item => item.sport || item.facility || 'Unknown');
        const values = data.allStatsRaw.map(item => item.value || 1);
        createBarChart('allStatsRawChart', labels, values, 'All Stats Raw');
    }

    // Average Lead Time Chart
    if (data.averageLeadTime !== undefined) {
        createBarChart('averageLeadTimeChart', ['Average Lead Time'], [data.averageLeadTime], 'Days', CONFIG.CHART_COLORS[1]);
    }

    // Lead Time Breakdown Chart
    if (data.leadTimeBreakdown && typeof data.leadTimeBreakdown === 'object') {
        const labels = Object.keys(data.leadTimeBreakdown);
        const values = Object.values(data.leadTimeBreakdown);
        createPieChart('leadTimeBreakdownChart', labels, values);
    }

    // Bookings by Day Chart
    if (data.bookingsByDay && typeof data.bookingsByDay === 'object') {
        const labels = Object.keys(data.bookingsByDay);
        const values = Object.values(data.bookingsByDay);
        createBarChart('bookingsByDayChart', labels, values, 'Bookings', CONFIG.CHART_COLORS[2]);
    }

    // Bookings by Start Time Chart
    if (data.bookingsByStartTime && typeof data.bookingsByStartTime === 'object') {
        const labels = Object.keys(data.bookingsByStartTime);
        const values = Object.values(data.bookingsByStartTime);
        createBarChart('bookingsByStartTimeChart', labels, values, 'Bookings', CONFIG.CHART_COLORS[3]);
    }

    // Booking Duration Breakdown Chart
    if (data.bookingDurationBreakdown && typeof data.bookingDurationBreakdown === 'object') {
        const labels = Object.keys(data.bookingDurationBreakdown);
        const values = Object.values(data.bookingDurationBreakdown);
        createPieChart('bookingDurationBreakdownChart', labels, values);
    }

    // Monthly Report Chart
    if (data.monthlyReport && Array.isArray(data.monthlyReport)) {
        const labels = data.monthlyReport.map(item => item.month || 'Unknown');
        const values = data.monthlyReport.map(item => item.bookings || 0);
        createLineChart('monthlyReportChart', labels, values, 'Bookings', CONFIG.CHART_COLORS[4]);
    }

    // Sport Comparison Chart
    if (data.sportComparison && Array.isArray(data.sportComparison)) {
        const labels = data.sportComparison.map(item => item.sport || 'Unknown');
        const values = data.sportComparison.map(item => item.value || 0);
        createBarChart('sportComparisonChart', labels, values, 'Comparison', CONFIG.CHART_COLORS[5]);
    }
}

// ===== EVENT LISTENERS =====

/**
 * Initialize event listeners
 */
function initializeEventListeners() {
    // Refresh button
    const refreshBtn = document.getElementById('refreshBtn');
    if (refreshBtn) {
        refreshBtn.addEventListener('click', refreshDashboard);
    }

    // Apply filters button
    const applyFiltersBtn = document.getElementById('applyFiltersBtn');
    if (applyFiltersBtn) {
        applyFiltersBtn.addEventListener('click', applyFilters);
    }

    // Clear filters button
    const clearFiltersBtn = document.getElementById('clearFiltersBtn');
    if (clearFiltersBtn) {
        clearFiltersBtn.addEventListener('click', clearFilters);
    }

    // Auto refresh toggle
    const autoRefreshToggle = document.getElementById('autoRefreshToggle');
    if (autoRefreshToggle) {
        autoRefreshToggle.addEventListener('change', function() {
            if (this.checked) {
                startAutoRefresh();
            } else {
                stopAutoRefresh();
            }
        });
    }

    // Filter inputs
    const filterInputs = document.querySelectorAll('[data-filter]');
    filterInputs.forEach(input => {
        input.addEventListener('change', debounce(applyFilters, 500));
    });
}

// ===== AUTO REFRESH =====

let autoRefreshInterval = null;

function startAutoRefresh() {
    if (autoRefreshInterval) {
        clearInterval(autoRefreshInterval);
    }
    autoRefreshInterval = setInterval(refreshDashboard, CONFIG.REFRESH_INTERVAL);
    showNotification('Auto refresh enabled', 'info');
}

function stopAutoRefresh() {
    if (autoRefreshInterval) {
        clearInterval(autoRefreshInterval);
        autoRefreshInterval = null;
    }
    showNotification('Auto refresh disabled', 'info');
}

// ===== INITIALIZATION =====

/**
 * Initialize application
 */
async function initializeApp() {
    try {
        console.log('Initializing CatchCornerStats...');
        
        // Initialize event listeners
        initializeEventListeners();
        
        // Load initial data
        await refreshDashboard();
        
        console.log('CatchCornerStats initialized successfully');
        
    } catch (error) {
        console.error('Failed to initialize app:', error);
        showNotification('Failed to initialize application', 'danger');
    }
}

// ===== SOLO INICIALIZAR DASHBOARD SI EXISTE =====
document.addEventListener('DOMContentLoaded', function() {
    if (document.getElementById('dashboard') || document.querySelector('.dashboard-main')) {
        if (typeof initializeApp === 'function') {
            initializeApp();
        }
    }
});

// ===== AVERAGE LEAD TIME VIEW ONLY =====

async function refreshAverageLeadTimeView() {
    try {
        showLoading('Loading average lead time...');
        AppState.isLoading = true;
        const filters = getCurrentFilters();
        const result = await getAverageLeadTime(filters);
        // Update cards
        const averageDays = document.getElementById('averageDays');
        if (averageDays && result.averageLeadTime !== undefined) {
            averageDays.textContent = result.averageLeadTime.toFixed(2);
        }
        const totalBookings = document.getElementById('totalBookings');
        if (totalBookings && result.totalBookings !== undefined) {
            totalBookings.textContent = formatNumber(result.totalBookings);
        }
        // Optionally update a chart if needed (not required for just a number)
        showNotification('Average lead time loaded', 'success');
    } catch (error) {
        showNotification('Failed to load average lead time', 'danger');
    } finally {
        hideLoading();
        AppState.isLoading = false;
    }
}

// Attach to the Average Lead Time view
window.refreshAverageLeadTimeView = refreshAverageLeadTimeView;

// Optionally, if you want to auto-load on page load for this view:
document.addEventListener('DOMContentLoaded', function() {
    if (document.getElementById('averageDays') && document.getElementById('totalBookings')) {
        refreshAverageLeadTimeView();
    }
});

// Export for global access
window.CatchCornerStats = {
    refreshDashboard,
    applyFilters,
    clearFilters,
    showNotification,
    formatNumber,
    formatCurrency,
    formatDate,
    formatTime
}; 