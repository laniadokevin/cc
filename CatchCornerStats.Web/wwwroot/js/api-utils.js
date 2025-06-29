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

    // Make API call with error handling
    async fetchApi(endpoint, options = {}) {
        const url = buildApiUrl(endpoint, options.params);
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
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status} - ${response.statusText}`);
            }
            
            return await response.json();
        } catch (error) {
            console.error('API call failed:', error);
            throw error;
        }
    },

    // Make API call with retry logic
    async fetchApiWithRetry(endpoint, options = {}, maxRetries = 3) {
        for (let attempt = 1; attempt <= maxRetries; attempt++) {
            try {
                return await this.fetchApi(endpoint, options);
            } catch (error) {
                if (attempt === maxRetries) {
                    throw error;
                }
                
                console.warn(`API call failed (attempt ${attempt}/${maxRetries}), retrying...`, error);
                await new Promise(resolve => setTimeout(resolve, 1000 * attempt)); // Exponential backoff
            }
        }
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

    // Handle common API errors
    handleApiError(error, context = '') {
        console.error(`API Error in ${context}:`, error);
        
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
    }
};

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = API_UTILS;
} 