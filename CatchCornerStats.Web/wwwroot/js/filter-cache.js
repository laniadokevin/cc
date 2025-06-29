// ===== FILTER CACHE SYSTEM =====
// This system loads filter options once and caches them for all views

// Global filter cache
const FilterCache = {
    sports: null,
    cities: null,
    rinkSizes: null,
    facilities: null,
    isLoading: false,
    loadPromise: null,
    lastLoadTime: null,
    cacheExpiry: 5 * 60 * 1000 // 5 minutes
};

// API base URL for filter endpoints
const FILTER_API_BASE = 'https://localhost:7254/api/Stats';

/**
 * Check if cache is valid (not expired)
 */
function isCacheValid() {
    if (!FilterCache.lastLoadTime) return false;
    return (Date.now() - FilterCache.lastLoadTime) < FilterCache.cacheExpiry;
}

/**
 * Load all filter options from API
 */
async function loadFilterOptions() {
    // If already loading, return existing promise
    if (FilterCache.loadPromise) {
        return FilterCache.loadPromise;
    }

    // If cache is valid, return cached data
    if (isCacheValid()) {
        return {
            sports: FilterCache.sports,
            cities: FilterCache.cities,
            rinkSizes: FilterCache.rinkSizes,
            facilities: FilterCache.facilities
        };
    }

    // Start loading
    FilterCache.isLoading = true;
    FilterCache.loadPromise = new Promise(async (resolve, reject) => {
        try {
            console.log('Loading filter options from API...');
            
            // Load all filter options in parallel
            const [sports, cities, rinkSizes, facilities] = await Promise.all([
                fetch(`${FILTER_API_BASE}/GetSports?v=${Date.now()}`).then(r => r.json()),
                fetch(`${FILTER_API_BASE}/GetCities?v=${Date.now()}`).then(r => r.json()),
                fetch(`${FILTER_API_BASE}/GetRinkSizes?v=${Date.now()}`).then(r => r.json()),
                fetch(`${FILTER_API_BASE}/GetFacilities?v=${Date.now()}`).then(r => r.json())
            ]);

            // Cache the results
            FilterCache.sports = sports;
            FilterCache.cities = cities;
            FilterCache.rinkSizes = rinkSizes;
            FilterCache.facilities = facilities;
            FilterCache.lastLoadTime = Date.now();

            console.log('Filter options loaded and cached successfully');
            
            resolve({
                sports: sports,
                cities: cities,
                rinkSizes: rinkSizes,
                facilities: facilities
            });
        } catch (error) {
            console.error('Failed to load filter options:', error);
            reject(error);
        } finally {
            FilterCache.isLoading = false;
            FilterCache.loadPromise = null;
        }
    });

    return FilterCache.loadPromise;
}

/**
 * Get cached filter options
 */
function getCachedFilterOptions() {
    return {
        sports: FilterCache.sports || [],
        cities: FilterCache.cities || [],
        rinkSizes: FilterCache.rinkSizes || [],
        facilities: FilterCache.facilities || []
    };
}

/**
 * Populate a checkbox dropdown using cached data
 */
function populateCheckboxDropdownFromCache(containerId, filterType, allLabel, labelId) {
    const container = document.getElementById(containerId);
    const label = document.getElementById(labelId);
    
    if (!container) {
        console.warn(`Container ${containerId} not found`);
        return;
    }

    container.innerHTML = '';
    
    const options = getCachedFilterOptions()[filterType] || [];
    
    if (!options.length) {
        container.innerHTML = '<div class="text-muted small">No options available</div>';
        if (label) label.textContent = allLabel;
        return;
    }

    // Helper function to create safe ID
    function btoaUtf8(str) {
        return btoa(unescape(encodeURIComponent(str))).replace(/[^a-zA-Z0-9]/g, '');
    }

    options.forEach(item => {
        if (item.value && item.value !== '') {
            const id = containerId + '_' + btoaUtf8(item.value);
            container.innerHTML += `
                <div class='form-check'>
                    <input class='form-check-input' type='checkbox' value="${item.value}" id="${id}" onchange="updateDropdownLabel('${containerId}','${allLabel}','${labelId}')">
                    <label class='form-check-label' for="${id}">${item.value}</label>
                </div>
            `;
        }
    });

    if (label) {
        updateDropdownLabel(containerId, allLabel, labelId);
    }
}

/**
 * Update dropdown label based on selected checkboxes
 */
function updateDropdownLabel(containerId, allLabel, labelId) {
    const container = document.getElementById(containerId);
    const label = document.getElementById(labelId);
    
    if (!container || !label) return;
    
    const checked = Array.from(container.querySelectorAll('input[type=checkbox]:checked')).map(cb => cb.value);
    
    if (checked.length === 0) {
        label.textContent = allLabel;
    } else if (checked.length === 1) {
        label.textContent = checked[0];
    } else {
        label.textContent = `${checked.length} selected`;
    }
}

/**
 * Get checked values from a container
 */
function getCheckedValues(containerId) {
    const container = document.getElementById(containerId);
    if (!container) return [];
    return Array.from(container.querySelectorAll('input[type=checkbox]:checked')).map(cb => cb.value);
}

/**
 * Filter dropdown options based on search input
 */
function filterDropdownOptions(input, containerId) {
    const filter = input.value.toLowerCase();
    const container = document.getElementById(containerId);
    
    if (!container) return;
    
    Array.from(container.children).forEach(div => {
        const label = div.querySelector('label');
        if (label && label.textContent.toLowerCase().includes(filter)) {
            div.style.display = '';
        } else {
            div.style.display = 'none';
        }
    });
}

/**
 * Clear all filter selections
 */
function clearAllFilterSelections() {
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
            Array.from(container.querySelectorAll('input[type=checkbox]')).forEach(cb => cb.checked = false);
        }
        
        const mapping = labelMappings[containerId];
        if (mapping) {
            const label = document.getElementById(mapping.labelId);
            if (label) {
                label.textContent = mapping.defaultLabel;
            }
        }
    });

    // Clear date inputs
    const dateInputs = ['createdDateFrom', 'createdDateTo', 'happeningDateFrom', 'happeningDateTo'];
    dateInputs.forEach(inputId => {
        const input = document.getElementById(inputId);
        if (input) input.value = '';
    });
}

/**
 * Get filters summary for history
 */
function getFiltersSummary() {
    const sports = getCheckedValues('sportsOptions');
    const cities = getCheckedValues('citiesOptions');
    const rinkSizes = getCheckedValues('rinkSizesOptions');
    const facilities = getCheckedValues('facilitiesOptions');
    
    const createdFrom = document.getElementById('createdDateFrom')?.value;
    const createdTo = document.getElementById('createdDateTo')?.value;
    const happeningFrom = document.getElementById('happeningDateFrom')?.value;
    const happeningTo = document.getElementById('happeningDateTo')?.value;
    
    const parts = [];
    
    if (sports.length > 0) parts.push(`Sports: ${sports.join(', ')}`);
    if (cities.length > 0) parts.push(`Cities: ${cities.join(', ')}`);
    if (rinkSizes.length > 0) parts.push(`Rink Sizes: ${rinkSizes.join(', ')}`);
    if (facilities.length > 0) parts.push(`Facilities: ${facilities.join(', ')}`);
    
    if (createdFrom && createdTo) {
        parts.push(`Created: ${createdFrom} to ${createdTo}`);
    } else if (createdFrom) {
        parts.push(`Created from: ${createdFrom}`);
    } else if (createdTo) {
        parts.push(`Created to: ${createdTo}`);
    }
    
    if (happeningFrom && happeningTo) {
        parts.push(`Happening: ${happeningFrom} to ${happeningTo}`);
    } else if (happeningFrom) {
        parts.push(`Happening from: ${happeningFrom}`);
    } else if (happeningTo) {
        parts.push(`Happening to: ${happeningTo}`);
    }
    
    return parts.length ? parts.join(' | ') : 'All Data';
}

/**
 * Build API parameters from current filters
 */
function buildApiParameters() {
    const params = new URLSearchParams();
    
    const sports = getCheckedValues('sportsOptions');
    const cities = getCheckedValues('citiesOptions');
    const rinkSizes = getCheckedValues('rinkSizesOptions');
    const facilities = getCheckedValues('facilitiesOptions');
    
    if (sports.length > 0) sports.forEach(s => params.append('sports', s));
    if (cities.length > 0) cities.forEach(c => params.append('cities', c));
    if (rinkSizes.length > 0) rinkSizes.forEach(r => params.append('rinkSizes', r));
    if (facilities.length > 0) facilities.forEach(f => params.append('facilities', f));
    
    // Date filters
    const createdFrom = document.getElementById('createdDateFrom')?.value;
    const createdTo = document.getElementById('createdDateTo')?.value;
    const happeningFrom = document.getElementById('happeningDateFrom')?.value;
    const happeningTo = document.getElementById('happeningDateTo')?.value;
    
    if (createdFrom) params.append('createdDateFrom', createdFrom);
    if (createdTo) params.append('createdDateTo', createdTo);
    if (happeningFrom) params.append('happeningDateFrom', happeningFrom);
    if (happeningTo) params.append('happeningDateTo', happeningTo);
    
    return params;
}

/**
 * Initialize filter cache when page loads
 */
async function initializeFilterCache() {
    try {
        await loadFilterOptions();
        console.log('Filter cache initialized successfully');
    } catch (error) {
        console.error('Failed to initialize filter cache:', error);
    }
}

// Initialize cache when script loads
document.addEventListener('DOMContentLoaded', initializeFilterCache);

// Export functions for use in other scripts
window.FilterCache = window.FilterCache || {};
window.FilterCache.loadFilterOptions = loadFilterOptions;
window.FilterCache.getCachedFilterOptions = getCachedFilterOptions;
window.FilterCache.populateCheckboxDropdownFromCache = populateCheckboxDropdownFromCache;
window.FilterCache.updateDropdownLabel = updateDropdownLabel;
window.FilterCache.getCheckedValues = getCheckedValues;
window.FilterCache.filterDropdownOptions = filterDropdownOptions;
window.FilterCache.clearAllFilterSelections = clearAllFilterSelections;
window.FilterCache.getFiltersSummary = getFiltersSummary;
window.FilterCache.buildApiParameters = buildApiParameters;
window.FilterCache.isCacheValid = isCacheValid; 