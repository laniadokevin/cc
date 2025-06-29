// ===== SHARED FILTERS SYSTEM =====
// This system manages shared filters across all views with caching and state persistence

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
const FILTER_API_BASE = CatchCornerStatsConfig?.api?.baseUrl || 'https://localhost:7254/api';

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
                console.log('🔧 [SharedFilters] ✅ Cache cargado de localStorage:', {
                    sports: SharedFilters.cache.sports?.length || 0,
                    cities: SharedFilters.cache.cities?.length || 0,
                    rinkSizes: SharedFilters.cache.rinkSizes?.length || 0,
                    facilities: SharedFilters.cache.facilities?.length || 0,
                    lastLoadTime: new Date(SharedFilters.cache.lastLoadTime).toLocaleString()
                });
                return true;
            }
        }
    } catch (error) {
        console.warn('🔧 [SharedFilters] Error cargando cache de localStorage:', error);
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
        console.log('🔧 [SharedFilters] ✅ Cache guardado en localStorage');
    } catch (error) {
        console.warn('🔧 [SharedFilters] Error guardando cache en localStorage:', error);
    }
}

/**
 * Initialize the shared filters system (optimized for parallel loading)
 */
async function initializeSharedFilters() {
    console.log('🔧 [SharedFilters] ===== INICIANDO SISTEMA DE FILTROS COMPARTIDOS =====');
    
    // If already initialized, return immediately
    if (SharedFilters.isInitialized) {
        console.log('🔧 [SharedFilters] ✅ Ya inicializado, retornando...');
        return;
    }
    
    // If already loading, wait for existing promise
    if (SharedFilters.loadPromise) {
        console.log('🔧 [SharedFilters] ⏳ Ya hay una carga en progreso, esperando...');
        return SharedFilters.loadPromise;
    }
    
    try {
        // Load cached selections from localStorage (synchronous)
        loadFilterSelectionsFromStorage();
        console.log('🔧 [SharedFilters] Selecciones cargadas de localStorage:', SharedFilters.selections);
        
        // Load filter cache from localStorage first
        loadFilterCacheFromStorage();
        
        // Load filter options if not cached (this is the async part)
        await loadFilterOptionsIfNeeded();
        console.log('🔧 [SharedFilters] Opciones de filtros cargadas:', {
            sports: SharedFilters.cache.sports?.length || 0,
            cities: SharedFilters.cache.cities?.length || 0,
            rinkSizes: SharedFilters.cache.rinkSizes?.length || 0,
            facilities: SharedFilters.cache.facilities?.length || 0
        });
        
        // Mark as initialized
        SharedFilters.isInitialized = true;
        
        // Notify all ready callbacks
        SharedFilters.readyCallbacks.forEach(callback => {
            try {
                callback();
            } catch (error) {
                console.error('🔧 [SharedFilters] Error en callback de ready:', error);
            }
        });
        SharedFilters.readyCallbacks = [];
        
        console.log('🔧 [SharedFilters] ===== SISTEMA INICIALIZADO COMPLETAMENTE =====');
        
    } catch (error) {
        console.error('🔧 [SharedFilters] ❌ Error en inicialización:', error);
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
    console.log('🔧 [SharedFilters] Verificando cache de opciones...');
    
    // Check if cache is valid
    if (isFilterCacheValid()) {
        console.log('🔧 [SharedFilters] ✅ Cache válido, usando opciones cacheadas');
        return SharedFilters.cache;
    }
    
    console.log('🔧 [SharedFilters] ❌ Cache expirado o no existe, cargando desde API...');
    
    // If already loading, wait for existing promise
    if (SharedFilters.loadPromise) {
        console.log('🔧 [SharedFilters] ⏳ Ya hay una carga en progreso, esperando...');
        return SharedFilters.loadPromise;
    }
    
    // Start loading
    SharedFilters.isLoading = true;
    SharedFilters.loadPromise = new Promise(async (resolve, reject) => {
        try {
            console.log('🔧 [SharedFilters] 🌐 Iniciando carga de opciones desde API...');
            
            // Use optimized API utilities if available
            let filterData;
            if (typeof API_UTILS !== 'undefined' && API_UTILS.loadAllFilterOptions) {
                filterData = await API_UTILS.loadAllFilterOptions();
            } else {
                // Fallback to manual loading with timeout and retry
                filterData = await loadFiltersWithRetry();
            }
            
            console.log('🔧 [SharedFilters] 📊 Datos recibidos de API:', {
                sports: filterData.sports?.length || 0,
                cities: filterData.cities?.length || 0,
                rinkSizes: filterData.rinkSizes?.length || 0,
                facilities: filterData.facilities?.length || 0
            });
            
            // Cache the results
            SharedFilters.cache.sports = filterData.sports || [];
            SharedFilters.cache.cities = filterData.cities || [];
            SharedFilters.cache.rinkSizes = filterData.rinkSizes || [];
            SharedFilters.cache.facilities = filterData.facilities || [];
            SharedFilters.cache.lastLoadTime = Date.now();
            
            // Save to localStorage for persistence
            saveFilterCacheToStorage();
            
            console.log('🔧 [SharedFilters] ✅ Opciones cacheadas exitosamente');
            resolve(SharedFilters.cache);
            
        } catch (error) {
            console.error('🔧 [SharedFilters] ❌ Error cargando opciones:', error);
            
            // Use fallback data if API fails
            console.log('🔧 [SharedFilters] 🔄 Usando datos de respaldo...');
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
            console.log(`🔧 [SharedFilters] Intento ${attempt}/${maxRetries} de carga de filtros...`);
            
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
            console.error(`🔧 [SharedFilters] Error en intento ${attempt}:`, error);
            
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
        console.log('🔧 [SharedFilters] Cache no existe (lastLoadTime es null)');
        return false;
    }
    const isValid = (Date.now() - SharedFilters.cache.lastLoadTime) < SharedFilters.cache.cacheExpiry;
    console.log('🔧 [SharedFilters] Cache válido:', isValid, `(${Date.now() - SharedFilters.cache.lastLoadTime}ms < ${SharedFilters.cache.cacheExpiry}ms)`);
    return isValid;
}

/**
 * Initialize filter UI with cached options and selections
 */
function initializeFilterUI() {
    console.log('🔧 [SharedFilters] ===== INICIANDO UI DE FILTROS =====');
    console.log('🔧 [SharedFilters] Estado actual de selecciones:', SharedFilters.selections);
    
    // Primero restaurar las selecciones del localStorage
    loadFilterSelectionsFromStorage();
    console.log('🔧 [SharedFilters] Selecciones restauradas:', SharedFilters.selections);
    
    // Luego poblar los dropdowns con las opciones cacheadas
    populateFilterDropdowns();
    
    // Finalmente configurar los event listeners
    setupFilterEventListeners();
    
    console.log('🔧 [SharedFilters] ===== UI DE FILTROS INICIALIZADA =====');
}

/**
 * Populate filter dropdowns with cached options
 */
function populateFilterDropdowns() {
    console.log('🔧 [SharedFilters] Poblando dropdowns de filtros...');
    
    const filterMappings = [
        { containerId: 'sportsOptions', type: 'sports', labelId: 'sportsDropdownLabel', defaultLabel: 'All Sports' },
        { containerId: 'citiesOptions', type: 'cities', labelId: 'citiesDropdownLabel', defaultLabel: 'All Cities' },
        { containerId: 'rinkSizesOptions', type: 'rinkSizes', labelId: 'rinkSizesDropdownLabel', defaultLabel: 'All Sizes' },
        { containerId: 'facilitiesOptions', type: 'facilities', labelId: 'facilitiesDropdownLabel', defaultLabel: 'All Facilities' }
    ];
    
    filterMappings.forEach(mapping => {
        console.log(`🔧 [SharedFilters] Poblando ${mapping.type}...`);
        populateSingleDropdown(mapping);
    });
}

/**
 * Populate a single dropdown with options
 */
function populateSingleDropdown(mapping) {
    const container = document.getElementById(mapping.containerId);
    const label = document.getElementById(mapping.labelId);
    
    if (!container) {
        console.warn(`🔧 [SharedFilters] ❌ Container ${mapping.containerId} no encontrado`);
        return;
    }
    
    console.log(`🔧 [SharedFilters] Poblando ${mapping.containerId}...`);
    
    container.innerHTML = '';
    
    const options = SharedFilters.cache[mapping.type] || [];
    const selectedValues = SharedFilters.selections[mapping.type] || [];
    
    console.log(`🔧 [SharedFilters] ${mapping.type}:`, {
        opciones: options.length,
        seleccionadas: selectedValues,
        valores: selectedValues
    });
    
    if (!options.length) {
        container.innerHTML = '<div class="text-muted small">No options available</div>';
        if (label) label.textContent = mapping.defaultLabel;
        return;
    }

    // Helper para IDs únicos
    function btoaUtf8(str) {
        return btoa(unescape(encodeURIComponent(str))).replace(/[^a-zA-Z0-9]/g, '');
    }

    let checkedCount = 0;
    options.forEach(opt => {
        const value = typeof opt === 'string' ? opt : opt.value;
        if (value && value !== '') {
            const id = mapping.containerId + '_' + btoaUtf8(value);
            const isChecked = selectedValues.includes(value);
            if (isChecked) checkedCount++;
            
            container.innerHTML += `
                <div class='form-check'>
                    <input class='form-check-input' type='checkbox' value="${value}" id="${id}" ${isChecked ? 'checked' : ''} onchange="onFilterChange('${mapping.type}', '${mapping.containerId}', '${mapping.defaultLabel}', '${mapping.labelId}')">
                    <label class='form-check-label' for="${id}">${value}</label>
                </div>
            `;
        }
    });

    console.log(`🔧 [SharedFilters] ${mapping.type} renderizado: ${checkedCount} seleccionadas de ${options.length} opciones`);

    // Actualiza el label
    updateDropdownLabel(mapping.containerId, mapping.defaultLabel, mapping.labelId);
}

/**
 * Load filter selections from localStorage
 */
function loadFilterSelectionsFromStorage() {
    console.log('🔧 [SharedFilters] ===== CARGANDO FILTROS DE LOCALSTORAGE =====');
    
    try {
        const stored = localStorage.getItem('catchCornerStats_filters');
        console.log('🔧 [SharedFilters] Datos raw de localStorage:', stored);
        
        if (stored) {
            const parsed = JSON.parse(stored);
            console.log('🔧 [SharedFilters] Datos parseados:', parsed);
            
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
            console.log('🔧 [SharedFilters] ✅ Filtros cargados de localStorage:', SharedFilters.selections);
        } else {
            console.log('🔧 [SharedFilters] ℹ️ No hay filtros guardados en localStorage');
        }
    } catch (error) {
        console.warn('🔧 [SharedFilters] ❌ Error cargando filtros de localStorage:', error);
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
    console.log('🔧 [SharedFilters] ===== RESTAURANDO SELECCIONES DE FILTROS =====');
    
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
            console.log(`🔧 [SharedFilters] ✅ Restaurado ${input.id}: ${SharedFilters.selections[input.key]}`);
        } else {
            console.log(`🔧 [SharedFilters] ℹ️ No se pudo restaurar ${input.id}:`, {
                elementoExiste: !!element,
                valor: SharedFilters.selections[input.key]
            });
        }
    });
}

/**
 * Set up event listeners for filter changes
 */
function setupFilterEventListeners() {
    console.log('🔧 [SharedFilters] Configurando event listeners...');
    
    // Date input listeners
    const dateInputs = ['createdDateFrom', 'createdDateTo', 'happeningDateFrom', 'happeningDateTo'];
    dateInputs.forEach(inputId => {
        const element = document.getElementById(inputId);
        if (element) {
            element.addEventListener('change', (e) => {
                console.log(`🔧 [SharedFilters] 📅 Cambio en ${inputId}: ${e.target.value}`);
                onDateFilterChange(inputId, e.target.value);
            });
            console.log(`🔧 [SharedFilters] ✅ Event listener configurado para ${inputId}`);
        } else {
            console.warn(`🔧 [SharedFilters] ❌ Elemento ${inputId} no encontrado para event listener`);
        }
    });
}

/**
 * Handle filter checkbox changes
 */
function onFilterChange(filterType, containerId, defaultLabel, labelId) {
    console.log(`🔧 [SharedFilters] ===== CAMBIO DE FILTRO: ${filterType} =====`);
    
    // Update selections
    const newSelections = getCheckedValues(containerId);
    SharedFilters.selections[filterType] = newSelections;
    
    console.log(`🔧 [SharedFilters] Nuevas selecciones para ${filterType}:`, newSelections);
    
    // Update dropdown label
    updateDropdownLabel(containerId, defaultLabel, labelId);
    
    // Save to localStorage
    saveFilterSelectionsToStorage();
    
    // Notify listeners
    notifyFilterChangeListeners();
}

/**
 * Handle date filter changes
 */
function onDateFilterChange(inputId, value) {
    console.log(`🔧 [SharedFilters] ===== CAMBIO DE FECHA: ${inputId} = ${value} =====`);
    
    const key = inputId;
    SharedFilters.selections[key] = value;
    
    console.log(`🔧 [SharedFilters] Nueva fecha para ${key}: ${value}`);
    
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
        console.warn(`🔧 [SharedFilters] ❌ Container ${containerId} no encontrado para obtener valores`);
        return [];
    }
    
    const checkedValues = Array.from(container.querySelectorAll('input[type=checkbox]:checked')).map(cb => cb.value);
    console.log(`🔧 [SharedFilters] Valores seleccionados en ${containerId}:`, checkedValues);
    return checkedValues;
}

/**
 * Update dropdown label based on selected checkboxes
 */
function updateDropdownLabel(containerId, defaultLabel, labelId) {
    const container = document.getElementById(containerId);
    const label = document.getElementById(labelId);
    
    if (!container || !label) {
        console.warn(`🔧 [SharedFilters] ❌ No se puede actualizar label para ${containerId}`);
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
    console.log(`🔧 [SharedFilters] Label actualizado para ${containerId}: "${newLabel}"`);
}

/**
 * Filter dropdown options based on search input
 */
function filterDropdownOptions(input, containerId) {
    const filter = input.value.toLowerCase();
    const container = document.getElementById(containerId);
    
    if (!container) return;
    
    let visibleCount = 0;
    Array.from(container.children).forEach(div => {
        const label = div.querySelector('label');
        if (label && label.textContent.toLowerCase().includes(filter)) {
            div.style.display = '';
            visibleCount++;
        } else {
            div.style.display = 'none';
        }
    });
    
    console.log(`🔧 [SharedFilters] Filtrado ${containerId}: ${visibleCount} opciones visibles con "${filter}"`);
}

/**
 * Clear all filter selections
 */
function clearAllFilters() {
    console.log('🔧 [SharedFilters] ===== LIMPIANDO TODOS LOS FILTROS =====');
    
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
            console.log(`🔧 [SharedFilters] ✅ Limpiados ${checkboxes.length} checkboxes en ${containerId}`);
        }
    });
    
    // Clear date inputs
    const dateInputs = ['createdDateFrom', 'createdDateTo', 'happeningDateFrom', 'happeningDateTo'];
    dateInputs.forEach(inputId => {
        const element = document.getElementById(inputId);
        if (element) {
            element.value = '';
            console.log(`🔧 [SharedFilters] ✅ Limpiado ${inputId}`);
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
    
    console.log('🔧 [SharedFilters] Estado de selecciones limpiado:', SharedFilters.selections);
    
    // Save to localStorage
    saveFilterSelectionsToStorage();
    
    // Notify listeners
    notifyFilterChangeListeners();
}

/**
 * Get current filter selections
 */
function getCurrentFilterSelections() {
    console.log('🔧 [SharedFilters] Obteniendo selecciones actuales:', SharedFilters.selections);
    return { ...SharedFilters.selections };
}

/**
 * Build API parameters from current selections
 */
function buildApiParameters() {
    console.log('🔧 [SharedFilters] ===== CONSTRUYENDO PARÁMETROS DE API =====');
    
    const params = new URLSearchParams();
    
    // Add array parameters
    SharedFilters.selections.sports.forEach(sport => params.append('sports', sport));
    SharedFilters.selections.cities.forEach(city => params.append('cities', city));
    SharedFilters.selections.rinkSizes.forEach(size => params.append('rinkSizes', size));
    SharedFilters.selections.facilities.forEach(facility => params.append('facilities', facility));
    
    // Add date parameters
    if (SharedFilters.selections.createdDateFrom) params.append('createdDateFrom', SharedFilters.selections.createdDateFrom);
    if (SharedFilters.selections.createdDateTo) params.append('createdDateTo', SharedFilters.selections.createdDateTo);
    if (SharedFilters.selections.happeningDateFrom) params.append('happeningDateFrom', SharedFilters.selections.happeningDateFrom);
    if (SharedFilters.selections.happeningDateTo) params.append('happeningDateTo', SharedFilters.selections.happeningDateTo);
    
    console.log('🔧 [SharedFilters] Parámetros construidos:', params.toString());
    return params;
}

/**
 * Get filters summary for display
 */
function getFiltersSummary() {
    console.log('🔧 [SharedFilters] ===== GENERANDO RESUMEN DE FILTROS =====');
    
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
    console.log('🔧 [SharedFilters] Resumen generado:', summary);
    return summary;
}

/**
 * Save filter selections to localStorage
 */
function saveFilterSelectionsToStorage() {
    try {
        const dataToSave = JSON.stringify(SharedFilters.selections);
        localStorage.setItem('catchCornerStats_filters', dataToSave);
        console.log('🔧 [SharedFilters] ✅ Filtros guardados en localStorage:', dataToSave);
    } catch (error) {
        console.warn('🔧 [SharedFilters] ❌ Error guardando filtros en localStorage:', error);
    }
}

/**
 * Add listener for filter changes
 */
function addFilterChangeListener(callback) {
    SharedFilters.listeners.push(callback);
    console.log('🔧 [SharedFilters] ✅ Listener agregado. Total listeners:', SharedFilters.listeners.length);
}

/**
 * Remove listener for filter changes
 */
function removeFilterChangeListener(callback) {
    const index = SharedFilters.listeners.indexOf(callback);
    if (index > -1) {
        SharedFilters.listeners.splice(index, 1);
        console.log('🔧 [SharedFilters] ✅ Listener removido. Total listeners:', SharedFilters.listeners.length);
    }
}

/**
 * Notify all listeners of filter changes
 */
function notifyFilterChangeListeners() {
    console.log('🔧 [SharedFilters] ===== NOTIFICANDO LISTENERS =====');
    console.log('🔧 [SharedFilters] Total listeners:', SharedFilters.listeners.length);
    
    SharedFilters.listeners.forEach((callback, index) => {
        try {
            console.log(`🔧 [SharedFilters] Ejecutando listener ${index + 1}...`);
            callback(SharedFilters.selections);
            console.log(`🔧 [SharedFilters] ✅ Listener ${index + 1} ejecutado exitosamente`);
        } catch (error) {
            console.error(`🔧 [SharedFilters] ❌ Error en listener ${index + 1}:`, error);
        }
    });
}

/**
 * Force refresh of filter options
 */
async function refreshFilterOptions() {
    console.log('🔧 [SharedFilters] ===== REFRESCANDO OPCIONES DE FILTROS =====');
    
    // Clear cache
    SharedFilters.cache.lastLoadTime = null;
    SharedFilters.loadPromise = null;
    
    // Clear localStorage cache
    try {
        localStorage.removeItem('catchCornerStats_filterCache');
        console.log('🔧 [SharedFilters] Cache de localStorage limpiado');
    } catch (error) {
        console.warn('🔧 [SharedFilters] Error limpiando cache de localStorage:', error);
    }
    
    console.log('🔧 [SharedFilters] Cache limpiado');
    
    // Reload options
    await loadFilterOptionsIfNeeded();
    
    // Reinitialize UI
    initializeFilterUI();
    
    console.log('🔧 [SharedFilters] ✅ Opciones refrescadas');
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