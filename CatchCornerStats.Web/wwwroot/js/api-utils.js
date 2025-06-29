// ===== CATCHCORNERSTATS - API UTILITIES =====

// API Utility Functions
const API_UTILS = {
    // Default fetch options
    defaultOptions: {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        },
        mode: 'cors',
        credentials: 'omit'
    },

    // Make API call with error handling and performance logging
    async fetchApi(endpoint, options = {}) {
        const startTime = performance.now();
        const url = buildApiUrl(endpoint, options.params);
        
        ConfigHelpers?.log(`🌐 API Request: ${options.method || 'GET'} ${url}`);
        
        const fetchOptions = {
            ...this.defaultOptions,
            ...options,
            headers: {
                ...this.defaultOptions.headers,
                ...options.headers
            }
        };

        try {
            const response = await fetch(url, fetchOptions);
            const responseTime = performance.now() - startTime;
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status} - ${response.statusText}`);
            }
            
            const data = await response.json();
            
            ConfigHelpers?.log(`✅ API Response (${responseTime.toFixed(2)}ms):`, {
                url,
                status: response.status,
                dataSize: JSON.stringify(data).length
            });
            
            return data;
        } catch (error) {
            const responseTime = performance.now() - startTime;
            ConfigHelpers?.logError(`❌ API Error (${responseTime.toFixed(2)}ms):`, {
                url,
                error: error.message
            });
            throw error;
        }
    },

    // Make API call with retry logic and exponential backoff
    async fetchApiWithRetry(endpoint, options = {}, maxRetries = 3) {
        for (let attempt = 1; attempt <= maxRetries; attempt++) {
            try {
                return await this.fetchApi(endpoint, options);
            } catch (error) {
                if (attempt === maxRetries) {
                    throw error;
                }
                
                const delay = 1000 * Math.pow(2, attempt - 1); // Exponential backoff
                ConfigHelpers?.log(`⚠️ API call failed (attempt ${attempt}/${maxRetries}), retrying in ${delay}ms...`);
                await new Promise(resolve => setTimeout(resolve, delay));
            }
        }
    },

    // Load stats data using centralized config
    async loadStatsData(statsEndpoint, params = null) {
        const endpoint = CatchCornerStatsConfig?.api?.stats?.[statsEndpoint];
        if (!endpoint) {
            throw new Error(`Stats endpoint not found: ${statsEndpoint}`);
        }
        
        const options = {
            params: params
        };
        
        return await this.fetchApi(endpoint, options);
    },

    // Load filter options using centralized config
    async loadFilterOptions(filterType) {
        const endpoint = CatchCornerStatsConfig?.api?.filters?.[filterType];
        if (!endpoint) {
            throw new Error(`Filter endpoint not found: ${filterType}`);
        }
        
        return await this.fetchApi(endpoint);
    },

    // Load all filter options in parallel (optimized)
    async loadAllFilterOptions() {
        const filterTypes = ['sports', 'cities', 'rinkSizes', 'facilities'];
        
        ConfigHelpers?.log('🔄 Loading all filter options in parallel...');
        
        // OPTIMIZACIÓN: Intentar usar el endpoint combinado primero
        try {
            const allFiltersEndpoint = CatchCornerStatsConfig?.api?.filters?.allFilters;
            if (allFiltersEndpoint) {
                ConfigHelpers?.log('🚀 Using combined filters endpoint...');
                const combinedData = await this.fetchApi(allFiltersEndpoint);
                
                // Convertir el formato de respuesta
                const filterData = {
                    sports: combinedData.sports?.map(x => x.value) || [],
                    cities: combinedData.cities?.map(x => x.value) || [],
                    rinkSizes: combinedData.rinkSizes?.map(x => x.value) || [],
                    facilities: combinedData.facilities?.map(x => x.value) || []
                };
                
                ConfigHelpers?.log('✅ All filter options loaded via combined endpoint:', {
                    sports: filterData.sports?.length || 0,
                    cities: filterData.cities?.length || 0,
                    rinkSizes: filterData.rinkSizes?.length || 0,
                    facilities: filterData.facilities?.length || 0
                });
                
                return filterData;
            }
        } catch (error) {
            ConfigHelpers?.logError('Combined endpoint failed, falling back to individual endpoints:', error);
        }
        
        // Fallback: cargar individualmente
        const promises = filterTypes.map(async (type) => {
            try {
                const data = await this.loadFilterOptions(type);
                return { type, data, success: true };
            } catch (error) {
                ConfigHelpers?.logError(`Error loading ${type} options:`, error);
                return { type, data: [], success: false, error };
            }
        });
        
        const results = await Promise.all(promises);
        
        // Convert to object format
        const filterData = {};
        results.forEach(result => {
            filterData[result.type] = result.data;
        });
        
        ConfigHelpers?.log('✅ All filter options loaded via individual endpoints:', {
            sports: filterData.sports?.length || 0,
            cities: filterData.cities?.length || 0,
            rinkSizes: filterData.rinkSizes?.length || 0,
            facilities: filterData.facilities?.length || 0
        });
        
        return filterData;
    },

    // Build URL parameters from object
    buildParams(params) {
        if (!params) return null;
        
        const urlParams = new URLSearchParams();
        Object.entries(params).forEach(([key, value]) => {
            if (value !== null && value !== undefined && value !== '') {
                if (Array.isArray(value)) {
                    value.forEach(v => urlParams.append(key, v));
                } else {
                    urlParams.append(key, value);
                }
            }
        });
        
        return urlParams;
    },

    // Handle common API errors with user-friendly messages
    handleApiError(error, context = '') {
        ConfigHelpers?.logError(`API Error in ${context}:`, error);
        
        let userMessage = 'An error occurred while loading data.';
        
        if (error.message.includes('Failed to fetch')) {
            userMessage = 'Unable to connect to the server. Please check your internet connection.';
        } else if (error.message.includes('HTTP error! status: 404')) {
            userMessage = 'The requested data was not found.';
        } else if (error.message.includes('HTTP error! status: 500')) {
            userMessage = 'Server error occurred. Please try again later.';
        } else if (error.message.includes('HTTP error! status: 403')) {
            userMessage = 'Access denied. Please check your permissions.';
        }
        
        return userMessage;
    },

    // Cache management
    cache: {
        data: new Map(),
        defaultTTL: CatchCornerStatsConfig?.cache?.filterExpiry || 10 * 60 * 1000, // 10 minutes
        
        set(key, value, ttl = this.defaultTTL) {
            const expiry = Date.now() + ttl;
            this.data.set(key, { value, expiry });
        },
        
        get(key) {
            const item = this.data.get(key);
            if (!item) return null;
            
            if (Date.now() > item.expiry) {
                this.data.delete(key);
                return null;
            }
            
            return item.value;
        },
        
        clear() {
            this.data.clear();
        },
        
        clearExpired() {
            const now = Date.now();
            for (const [key, item] of this.data.entries()) {
                if (now > item.expiry) {
                    this.data.delete(key);
                }
            }
        }
    },

    // Cached API fetch
    async fetchApiCached(endpoint, options = {}, ttl = this.cache.defaultTTL) {
        const cacheKey = `${endpoint}_${JSON.stringify(options)}`;
        
        // Check cache first
        const cached = this.cache.get(cacheKey);
        if (cached) {
            ConfigHelpers?.log(`📦 Cache hit: ${endpoint}`);
            return cached;
        }
        
        // Fetch from API
        const data = await this.fetchApi(endpoint, options);
        
        // Cache the result
        this.cache.set(cacheKey, data, ttl);
        
        return data;
    },

    // Debounce function for API calls
    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    },

    // Throttle function for API calls
    throttle(func, limit) {
        let inThrottle;
        return function() {
            const args = arguments;
            const context = this;
            if (!inThrottle) {
                func.apply(context, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        }
    }
};

// Clean up expired cache entries periodically
setInterval(() => {
    API_UTILS.cache.clearExpired();
}, 60000); // Every minute

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = API_UTILS;
}

// Make available globally
window.API_UTILS = API_UTILS;

console.log('[ApiUtils] API utilities loaded with optimizations'); 