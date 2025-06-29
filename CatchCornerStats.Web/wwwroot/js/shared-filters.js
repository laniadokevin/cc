// ===== SHARED FILTERS SYSTEM =====
// This system manages shared filters across all views with caching and state persistence

console.log('Shared Filters System loading...');

// Global shared filter state
const SharedFilters = {
    // Cache for filter options
    cache: {
        sports: null,
        cities: null,
        rinkSizes: null,
        facilities: null,
        lastLoadTime: null,
        cacheExpiry: 30 * 60 * 1000 // 30 minutes (aumentado de 10 a 30)
    },
    
    // Current filter selections (persisted in localStorage)
    selections: {
        sports: [],
        cities: [],
        rinkSizes: [],
        facilities: [],
        createdDateFrom: '',
        createdDateTo: '',
        happeningDateFrom: '',
        happeningDateTo: ''
    },
    
    // Loading state
    isLoading: false,
    loadPromise: null,
    isInitialized: false,
    
    // Event listeners for filter changes
    listeners: [],
    
    // Callbacks for when filters are ready
    readyCallbacks: [],
    
    // Fallback data for offline/error scenarios
    fallbackData: {
        sports: ['Hockey', 'Soccer', 'Basketball', 'Tennis', 'Volleyball', 'Baseball'],
        cities: ['Toronto', 'Vancouver', 'Montreal', 'Calgary', 'Edmonton', 'Ottawa'],
        rinkSizes: ['Full Size', 'Half Size', 'Quarter Size', 'Mini Rink'],
        facilities: ['Sports Complex A', 'Community Center B', 'Arena C', 'Recreation Center D']
    }
};

// API configuration (use centralized config if available)
const FILTER_API_BASE = CatchCornerStatsConfig?.api?.baseUrl;

/**
 * Load filter cache from localStorage
 */
function loadFilterCacheFromStorage() {
    try {
        const cached = localStorage.getItem('catchCornerStats_filterCache');
        if (cached) {
            const parsed = JSON.parse(cached);
            if (parsed && parsed.lastLoadTime) {
                SharedFilters.cache = {
                    ...SharedFilters.cache,
                    ...parsed
                };
                return true;
            }
        }
    } catch (error) {
        // No logs for this function
    }
    return false;
}

/**
 * Save filter cache to localStorage
 */
function saveFilterCacheToStorage() {
    try {
        const cacheData = {
            sports: SharedFilters.cache.sports,
            cities: SharedFilters.cache.cities,
            rinkSizes: SharedFilters.cache.rinkSizes,
            facilities: SharedFilters.cache.facilities,
            lastLoadTime: SharedFilters.cache.lastLoadTime
        };
        localStorage.setItem('catchCornerStats_filterCache', JSON.stringify(cacheData));
    } catch (error) {
        // No logs for this function
    }
}

/**
 * Initialize the shared filters system (optimized for parallel loading)
 */
async function initializeSharedFilters() {
    // Esperar a que la precarga global termine si existe
    if (window.catchCornerStatsFiltersLoading && typeof window.catchCornerStatsFiltersLoading.then === 'function') {
        await window.catchCornerStatsFiltersLoading;
    }
    // If already initialized, return immediately
    if (SharedFilters.isInitialized) {
        return;
    }
    
    // If already loading, wait for existing promise
    if (SharedFilters.loadPromise) {
        return SharedFilters.loadPromise;
    }
    
    try {
        // Load cached selections from localStorage (synchronous)
        loadFilterSelectionsFromStorage();
        
        // Load filter cache from localStorage first
        loadFilterCacheFromStorage();
        
        // Load filter options if not cached or expired (this is the async part)
        await loadFilterOptionsIfNeeded();
        
        // Initialize the UI with the loaded data
        initializeFilterUI();
        
        // Mark as initialized
        SharedFilters.isInitialized = true;
        
        // Notify all ready callbacks
        SharedFilters.readyCallbacks.forEach(callback => {
            try {
                callback();
            } catch (error) {
                // No logs for this function
            }
        });
        SharedFilters.readyCallbacks = [];
        
    } catch (error) {
        throw error;
    }
    
    return SharedFilters.loadPromise;
}

/**
 * Register callback for when filters are ready
 */
function onFiltersReady(callback) {
    if (SharedFilters.isInitialized) {
        // If already ready, execute immediately
        callback();
    } else {
        // If not ready, add to queue
        SharedFilters.readyCallbacks.push(callback);
    }
}

/**
 * Check if filters are ready
 */
function areFiltersReady() {
    return SharedFilters.isInitialized;
}

/**
 * Load filter options from API if not cached or expired (optimized)
 */
async function loadFilterOptionsIfNeeded() {
    // Check if cache is valid
    if (isFilterCacheValid()) {
        return SharedFilters.cache;
    }
    // Intentar cargar de localStorage (precarga global)
    try {
        const raw = localStorage.getItem('catchCornerStats_filterCache');
        if (raw) {
            const parsed = JSON.parse(raw);
            if (parsed && parsed.lastLoadTime && (Date.now() - parsed.lastLoadTime) < SharedFilters.cache.cacheExpiry) {
                SharedFilters.cache = {
                    ...SharedFilters.cache,
                    ...parsed
                };
                return SharedFilters.cache;
            }
        }
    } catch (e) {
        // No logs for this function
    }
    // Si no hay cache válido, cargar desde la API
    
    // If already loading, wait for existing promise
    if (SharedFilters.loadPromise) {
        return SharedFilters.loadPromise;
    }
    
    // Start loading
    SharedFilters.isLoading = true;
    SharedFilters.loadPromise = new Promise(async (resolve, reject) => {
        try {
            // Use optimized API utilities if available
            let filterData;
            if (typeof API_UTILS !== 'undefined' && API_UTILS.loadAllFilterOptions) {
                filterData = await API_UTILS.loadAllFilterOptions();
            } else {
                // Fallback to manual loading with timeout and retry
                filterData = await loadFiltersWithRetry();
            }
            
            // Cache the results
            SharedFilters.cache.sports = filterData.sports || [];
            SharedFilters.cache.cities = filterData.cities || [];
            SharedFilters.cache.rinkSizes = filterData.rinkSizes || [];
            SharedFilters.cache.facilities = filterData.facilities || [];
            SharedFilters.cache.lastLoadTime = Date.now();
            
            // Save to localStorage for persistence
            saveFilterCacheToStorage();
            
            resolve(SharedFilters.cache);
            
        } catch (error) {
            // Use fallback data if API fails
            SharedFilters.cache.sports = SharedFilters.fallbackData.sports;
            SharedFilters.cache.cities = SharedFilters.fallbackData.cities;
            SharedFilters.cache.rinkSizes = SharedFilters.fallbackData.rinkSizes;
            SharedFilters.cache.facilities = SharedFilters.fallbackData.facilities;
            SharedFilters.cache.lastLoadTime = Date.now();
            
            // Save fallback data to localStorage too
            saveFilterCacheToStorage();
            
            resolve(SharedFilters.cache);
        } finally {
            SharedFilters.isLoading = false;
            SharedFilters.loadPromise = null;
        }
    });
    
    return SharedFilters.loadPromise;
}

/**
 * Load filters with retry logic and timeout
 */
async function loadFiltersWithRetry(maxRetries = 2, timeout = 5000) {
    const filterEndpoints = {
        sports: CatchCornerStatsConfig?.api?.filters?.sports || '/Stats/GetSports',
        cities: CatchCornerStatsConfig?.api?.filters?.cities || '/Stats/GetCities',
        rinkSizes: CatchCornerStatsConfig?.api?.filters?.rinkSizes || '/Stats/GetRinkSizes',
        facilities: CatchCornerStatsConfig?.api?.filters?.facilities || '/Stats/GetFacilities'
    };
    
    for (let attempt = 1; attempt <= maxRetries; attempt++) {
        try {
            // Load all filter options in parallel with timeout
            const promises = Object.entries(filterEndpoints).map(async ([key, endpoint]) => {
                const controller = new AbortController();
                const timeoutId = setTimeout(() => controller.abort(), timeout);
                
                try {
                    const response = await fetch(`${FILTER_API_BASE}${endpoint}?v=${Date.now()}`, {
                        signal: controller.signal
                    });
                    clearTimeout(timeoutId);
                    
                    if (!response.ok) {
                        throw new Error(`HTTP ${response.status} para ${key}`);
                    }
                    
                    const data = await response.json();
                    return { key, data: data.map(x => x.value || x), success: true };
                } catch (error) {
                    clearTimeout(timeoutId);
                    throw error;
                }
            });
            
            const results = await Promise.all(promises);
            
            // Convert to object format
            const filterData = {};
            results.forEach(result => {
                filterData[result.key] = result.data;
            });
            
            return filterData;
            
        } catch (error) {
            if (attempt === maxRetries) {
                throw error;
            }
            
            // Wait before retry
            await new Promise(resolve => setTimeout(resolve, 1000 * attempt));
        }
    }
}

/**
 * Check if filter cache is valid
 */
function isFilterCacheValid() {
    if (!SharedFilters.cache.lastLoadTime) {
        return false;
    }
    const isValid = (Date.now() - SharedFilters.cache.lastLoadTime) < SharedFilters.cache.cacheExpiry;
    return isValid;
}

/**
 * Initialize filter UI with cached options and selections
 */
function initializeFilterUI() {
    // Populate all dropdowns
    populateFilterDropdowns();
    
    // Restore saved selections
    restoreFilterSelections();
    
    // Setup event listeners
    setupFilterEventListeners();
    
    // Mark as ready
    filtersReady = true;
    
    // Notify that filters are ready
    if (typeof filtersReady === 'undefined') {
        window.filtersReady = true;
    }
}

/**
 * Populate filter dropdowns with cached options
 */
function populateFilterDropdowns() {
    console.log('🔄 Populating filter dropdowns...');
    console.log('SharedFilters cache:', SharedFilters.cache);
    
    const mappings = [
        { containerId: 'sportsOptions', type: 'sports', labelId: 'sportsDropdownLabel', defaultLabel: 'All Sports' },
        { containerId: 'citiesOptions', type: 'cities', labelId: 'citiesDropdownLabel', defaultLabel: 'All Cities' },
        { containerId: 'rinkSizesOptions', type: 'rinkSizes', labelId: 'rinkSizesDropdownLabel', defaultLabel: 'All Sizes' },
        { containerId: 'facilitiesOptions', type: 'facilities', labelId: 'facilitiesDropdownLabel', defaultLabel: 'All Facilities' }
    ];
    
    mappings.forEach(mapping => {
        console.log(`Populating ${mapping.type} dropdown...`);
        simpleFilterDropdown(mapping);
    });
    
    console.log('✅ Filter dropdowns populated');
}

/**
 * Load filter selections from localStorage
 */
function loadFilterSelectionsFromStorage() {
    try {
        const stored = localStorage.getItem('catchCornerStats_filters');
        
        if (stored) {
            const parsed = JSON.parse(stored);
            
            // Asegurar que todas las propiedades existan
            SharedFilters.selections = {
                sports: [],
                cities: [],
                rinkSizes: [],
                facilities: [],
                createdDateFrom: '',
                createdDateTo: '',
                happeningDateFrom: '',
                happeningDateTo: '',
                ...parsed
            };
        } else {
            // Resetear a valores por defecto si hay error
            SharedFilters.selections = {
                sports: [],
                cities: [],
                rinkSizes: [],
                facilities: [],
                createdDateFrom: '',
                createdDateTo: '',
                happeningDateFrom: '',
                happeningDateTo: ''
            };
        }
    } catch (error) {
        // Resetear a valores por defecto si hay error
        SharedFilters.selections = {
            sports: [],
            cities: [],
            rinkSizes: [],
            facilities: [],
            createdDateFrom: '',
            createdDateTo: '',
            happeningDateFrom: '',
            happeningDateTo: ''
        };
    }
}

/**
 * Restore filter selections from localStorage
 */
function restoreFilterSelections() {
    // Restore date inputs
    const dateInputs = [
        { id: 'createdDateFrom', key: 'createdDateFrom' },
        { id: 'createdDateTo', key: 'createdDateTo' },
        { id: 'happeningDateFrom', key: 'happeningDateFrom' },
        { id: 'happeningDateTo', key: 'happeningDateTo' }
    ];
    
    dateInputs.forEach(input => {
        const element = document.getElementById(input.id);
        if (element && SharedFilters.selections[input.key]) {
            element.value = SharedFilters.selections[input.key];
        }
    });
}

/**
 * Set up event listeners for filter changes
 */
function setupFilterEventListeners() {
    // Date input listeners
    const dateInputs = ['createdDateFrom', 'createdDateTo', 'happeningDateFrom', 'happeningDateTo'];
    dateInputs.forEach(inputId => {
        const element = document.getElementById(inputId);
        if (element) {
            element.addEventListener('change', (e) => {
                onDateFilterChange(inputId, e.target.value);
            });
        }
    });
}

/**
 * Handle filter checkbox changes
 */
function onFilterChange(filterType, containerId, defaultLabel, labelId) {
    console.log(`🔘 Filter change detected: ${filterType}`, { containerId, defaultLabel, labelId });
    
    // Update selections
    const newSelections = getCheckedValues(containerId);
    SharedFilters.selections[filterType] = newSelections;
    
    console.log(`📝 Updated ${filterType} selections:`, newSelections);
    
    // Update dropdown label
    updateDropdownLabel(containerId, defaultLabel, labelId);
    
    // Save to localStorage
    saveFilterSelectionsToStorage();
    
    // Notify listeners
    notifyFilterChangeListeners();
    
    // Recargar datos automáticamente si existe la función loadData
    if (typeof window.loadData === 'function') {
        console.log(`🔄 Auto-reloading data after ${filterType} change`);
        window.loadData();
    }
    
    console.log(`✅ Filter change processed for ${filterType}`);
}

/**
 * Handle date filter changes
 */
function onDateFilterChange(inputId, value) {
    const key = inputId;
    SharedFilters.selections[key] = value;
    
    // Save to localStorage
    saveFilterSelectionsToStorage();
    
    // Notify listeners
    notifyFilterChangeListeners();
}

/**
 * Get checked values from a container
 */
function getCheckedValues(containerId) {
    const container = document.getElementById(containerId);
    if (!container) {
        console.warn(`⚠️ Container not found: ${containerId}`);
        return [];
    }
    
    const checkedValues = Array.from(container.querySelectorAll('input[type=checkbox]:checked')).map(cb => cb.value);
    console.log(`🔍 getCheckedValues for ${containerId}:`, checkedValues);
    return checkedValues;
}

/**
 * Update dropdown label based on selected checkboxes
 */
function updateDropdownLabel(containerId, defaultLabel, labelId) {
    const container = document.getElementById(containerId);
    const label = document.getElementById(labelId);
    
    if (!container || !label) {
        return;
    }
    
    const checked = getCheckedValues(containerId);
    
    let newLabel;
    if (checked.length === 0) {
        newLabel = defaultLabel;
    } else if (checked.length === 1) {
        newLabel = checked[0];
    } else {
        newLabel = `${checked.length} selected`;
    }
    
    label.textContent = newLabel;
}

/**
 * Clear all filter selections
 */
function clearAllFilters() {
    // Clear checkbox selections
    const filterContainers = ['sportsOptions', 'citiesOptions', 'rinkSizesOptions', 'facilitiesOptions'];
    const labelMappings = {
        'sportsOptions': { labelId: 'sportsDropdownLabel', defaultLabel: 'All Sports' },
        'citiesOptions': { labelId: 'citiesDropdownLabel', defaultLabel: 'All Cities' },
        'rinkSizesOptions': { labelId: 'rinkSizesDropdownLabel', defaultLabel: 'All Sizes' },
        'facilitiesOptions': { labelId: 'facilitiesDropdownLabel', defaultLabel: 'All Facilities' }
    };
    
    filterContainers.forEach(containerId => {
        const container = document.getElementById(containerId);
        if (container) {
            const checkboxes = container.querySelectorAll('input[type=checkbox]');
            checkboxes.forEach(cb => {
                cb.checked = false;
            });
            const mapping = labelMappings[containerId];
            if (mapping) {
                updateDropdownLabel(containerId, mapping.defaultLabel, mapping.labelId);
            }
        }
    });
    
    // Clear date inputs
    const dateInputs = ['createdDateFrom', 'createdDateTo', 'happeningDateFrom', 'happeningDateTo'];
    dateInputs.forEach(inputId => {
        const element = document.getElementById(inputId);
        if (element) {
            element.value = '';
        }
    });
    
    // Clear selections
    SharedFilters.selections = {
        sports: [],
        cities: [],
        rinkSizes: [],
        facilities: [],
        createdDateFrom: '',
        createdDateTo: '',
        happeningDateFrom: '',
        happeningDateTo: ''
    };
    
    // Save to localStorage
    saveFilterSelectionsToStorage();
    
    // Notify listeners
    notifyFilterChangeListeners();
}

/**
 * Get current filter selections
 */
function getCurrentFilterSelections() {
    return { ...SharedFilters.selections };
}

/**
 * Process rink size value to extract only the number
 */
function processRinkSizeValue(value) {
    if (!value) return value;
    
    // Si contiene "selected", extraer solo el número
    if (value.includes('selected')) {
        const numericValue = value.replace(/\s*selected.*$/, '').trim();
        console.log(`Processing rink size: "${value}" -> "${numericValue}"`);
        return numericValue;
    }
    
    return value;
}

/**
 * Build API parameters from current selections
 */
function buildApiParameters() {
    const params = new URLSearchParams();
    
    // Add array parameters
    SharedFilters.selections.sports.forEach(sport => params.append('sports', sport));
    SharedFilters.selections.cities.forEach(city => params.append('cities', city));
    SharedFilters.selections.rinkSizes.forEach(size => params.append('rinkSizes', processRinkSizeValue(size)));
    SharedFilters.selections.facilities.forEach(facility => params.append('facilities', facility));
    
    // Add date parameters
    if (SharedFilters.selections.createdDateFrom) params.append('createdDateFrom', SharedFilters.selections.createdDateFrom);
    if (SharedFilters.selections.createdDateTo) params.append('createdDateTo', SharedFilters.selections.createdDateTo);
    if (SharedFilters.selections.happeningDateFrom) params.append('happeningDateFrom', SharedFilters.selections.happeningDateFrom);
    if (SharedFilters.selections.happeningDateTo) params.append('happeningDateTo', SharedFilters.selections.happeningDateTo);
    
    return params;
}

/**
 * Get filters summary for display
 */
function getFiltersSummary() {
    const parts = [];
    
    if (SharedFilters.selections.sports.length > 0) {
        parts.push(`Sports: ${SharedFilters.selections.sports.join(', ')}`);
    }
    
    if (SharedFilters.selections.cities.length > 0) {
        parts.push(`Cities: ${SharedFilters.selections.cities.join(', ')}`);
    }
    
    if (SharedFilters.selections.rinkSizes.length > 0) {
        parts.push(`Rink Sizes: ${SharedFilters.selections.rinkSizes.join(', ')}`);
    }
    
    if (SharedFilters.selections.facilities.length > 0) {
        parts.push(`Facilities: ${SharedFilters.selections.facilities.join(', ')}`);
    }
    
    if (SharedFilters.selections.createdDateFrom && SharedFilters.selections.createdDateTo) {
        parts.push(`Created: ${SharedFilters.selections.createdDateFrom} to ${SharedFilters.selections.createdDateTo}`);
    }
    
    if (SharedFilters.selections.happeningDateFrom && SharedFilters.selections.happeningDateTo) {
        parts.push(`Happening: ${SharedFilters.selections.happeningDateFrom} to ${SharedFilters.selections.happeningDateTo}`);
    }
    
    const summary = parts.length > 0 ? parts.join(' | ') : 'All Data';
    return summary;
}

/**
 * Save filter selections to localStorage
 */
function saveFilterSelectionsToStorage() {
    try {
        const dataToSave = JSON.stringify(SharedFilters.selections);
        localStorage.setItem('catchCornerStats_filters', dataToSave);
    } catch (error) {
        // No logs for this function
    }
}

/**
 * Add listener for filter changes
 */
function addFilterChangeListener(callback) {
    SharedFilters.listeners.push(callback);
}

/**
 * Remove listener for filter changes
 */
function removeFilterChangeListener(callback) {
    const index = SharedFilters.listeners.indexOf(callback);
    if (index > -1) {
        SharedFilters.listeners.splice(index, 1);
    }
}

/**
 * Notify all listeners of filter changes
 */
function notifyFilterChangeListeners() {
    SharedFilters.listeners.forEach((callback, index) => {
        try {
            callback(SharedFilters.selections);
        } catch (error) {
            // No logs for this function
        }
    });
}

/**
 * Force refresh of filter options
 */
async function refreshFilterOptions() {
    // Clear cache
    SharedFilters.cache.lastLoadTime = null;
    SharedFilters.loadPromise = null;
    
    // Clear localStorage cache
    try {
        localStorage.removeItem('catchCornerStats_filterCache');
    } catch (error) {
        // No logs for this function
    }
    
    // Reload options
    await loadFilterOptionsIfNeeded();
    
    // Reinitialize UI
    initializeFilterUI();
}

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        SharedFilters,
        initializeSharedFilters,
        getCurrentFilterSelections,
        buildApiParameters,
        getFiltersSummary,
        clearAllFilters,
        addFilterChangeListener,
        removeFilterChangeListener,
        refreshFilterOptions
    };
}

// Debug function to test filter search functionality
function debugFilterSearch() {
    console.log('🔍 [Debug] Testing filter search functionality...');
    console.log('SharedFilters cache:', SharedFilters.cache);
    console.log('SharedFilters selections:', SharedFilters.selections);
    console.log('SharedFilters isInitialized:', SharedFilters.isInitialized);
    
    // Test if filter inputs exist
    const filterInputs = document.querySelectorAll('input[placeholder="Search..."]');
    console.log('Found filter inputs:', filterInputs.length);
    
    filterInputs.forEach((input, index) => {
        console.log(`Input ${index}:`, {
            value: input.value,
            hasHandler: input._searchHandlerAttached,
            parentDropdown: input.closest('.dropdown-menu')?.id
        });
    });
    
    // Test if dropdowns are populated
    const dropdowns = ['sportsOptions', 'citiesOptions', 'rinkSizesOptions', 'facilitiesOptions'];
    dropdowns.forEach(id => {
        const container = document.getElementById(id);
        if (container) {
            console.log(`${id}:`, {
                exists: true,
                children: container.children.length,
                innerHTML: container.innerHTML.substring(0, 100) + '...'
            });
        } else {
            console.log(`${id}:`, { exists: false });
        }
    });
}

// Make debug function available globally
window.debugFilterSearch = debugFilterSearch;

// Simple and direct filter function
function simpleFilterDropdown(mapping) {
    console.log(`🔍 simpleFilterDropdown called for ${mapping.type}:`, mapping);
    
    const container = document.getElementById(mapping.containerId);
    if (!container) {
        console.error(`❌ Container not found: ${mapping.containerId}`);
        return;
    }

    // Busca el input de búsqueda relativo al contenedor
    const filterInput = container?.parentElement?.querySelector('input[type="text"]');
    const searchTerm = filterInput ? filterInput.value.trim().toLowerCase() : '';

    const allOptions = SharedFilters.cache[mapping.type] || [];
    const selectedValues = SharedFilters.selections[mapping.type] || [];

    console.log(`${mapping.type} options:`, allOptions);
    console.log(`${mapping.type} selected:`, selectedValues);

    function getOptionText(opt) {
        if (typeof opt === 'string') return opt;
        if (opt.value) return opt.value;
        if (opt.Facility) return opt.Facility;
        if (opt.Name) return opt.Name;
        if (opt.label) return opt.label;
        if (typeof opt === 'object') return Object.values(opt).join(' ');
        return String(opt);
    }

    // DEBUG: log para ver el filtro funcionando
    // console.log('[Filtro]', { searchTerm, allOptionsLength: allOptions.length });

    const filteredOptions = allOptions.filter(opt => {
        const value = getOptionText(opt);
        return !searchTerm || value.toLowerCase().includes(searchTerm);
    });

    console.log(`${mapping.type} filtered options:`, filteredOptions);

    // console.log('[Filtro]', { filteredOptionsLength: filteredOptions.length, first: filteredOptions[0] });

    let html = '';
    filteredOptions.forEach(opt => {
        const value = getOptionText(opt);
        if (value && value !== '') {
            const id = mapping.containerId + '_' + btoa(unescape(encodeURIComponent(value))).replace(/[^a-zA-Z0-9]/g, '');
            const isChecked = selectedValues.includes(value);
            html += `
                <div class='form-check'>
                    <input class='form-check-input' type='checkbox' value="${value}" id="${id}" ${isChecked ? 'checked' : ''} onchange="onFilterChange('${mapping.type}', '${mapping.containerId}', '${mapping.defaultLabel}', '${mapping.labelId}')">
                    <label class='form-check-label' for="${id}">${value}</label>
                </div>
            `;
        }
    });

    if (filteredOptions.length === 0) {
        html = '<div class="text-muted small">No hay coincidencias</div>';
    }

    console.log(`${mapping.type} HTML generated:`, html.substring(0, 200) + '...');
    
    container.innerHTML = html;
    updateDropdownLabel(mapping.containerId, mapping.defaultLabel, mapping.labelId);
    
    console.log(`✅ ${mapping.type} dropdown populated`);
}

// Make it available globally
window.simpleFilterDropdown = simpleFilterDropdown;
window.initializeSharedFilters = initializeSharedFilters;
window.initializeFilterUI = initializeFilterUI;
window.onFilterChange = onFilterChange;
window.getSelectedDropdownValues = getSelectedDropdownValues;

console.log('Shared Filters System loaded successfully'); 