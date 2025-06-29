// ===== CATCHCORNERSTATS - MAIN APPLICATION SCRIPT =====

// Global application state
const AppState = {
    isInitialized: false,
    currentView: null,
    loadingStates: new Map(),
    errorStates: new Map()
};

// Initialize the application
async function initializeApp() {
    try {
        console.log('🚀 [App] Iniciando CatchCornerStats...');
        
        // Wait for all required scripts to be loaded
        await waitForDependencies();
        
        // Initialize configuration
        if (typeof CatchCornerStatsConfig === 'undefined') {
            throw new Error('Configuration not loaded');
        }
        
        // Initialize API utilities
        if (typeof API_UTILS === 'undefined') {
            throw new Error('API utilities not loaded');
        }
        
        // Initialize shared filters system
        if (typeof initializeSharedFilters === 'undefined') {
            throw new Error('Shared filters system not loaded');
        }
        
        AppState.isInitialized = true;
        console.log('✅ [App] CatchCornerStats inicializado correctamente');
        
        // Trigger app ready event
        document.dispatchEvent(new CustomEvent('appReady'));
        
    } catch (error) {
        console.error('❌ [App] Error inicializando aplicación:', error);
        showGlobalError('Error inicializando la aplicación: ' + error.message);
    }
}

// Wait for all required dependencies to be loaded
function waitForDependencies() {
    return new Promise((resolve) => {
        const checkDependencies = () => {
            const required = [
                'CatchCornerStatsConfig',
                'ConfigHelpers',
                'API_UTILS',
                'initializeSharedFilters'
            ];
            
            const missing = required.filter(dep => typeof window[dep] === 'undefined');
            
            if (missing.length === 0) {
                resolve();
            } else {
                console.log('⏳ [App] Esperando dependencias:', missing);
                setTimeout(checkDependencies, 100);
            }
        };
        
        checkDependencies();
    });
}

// Show global error message
function showGlobalError(message) {
    // Create error container if it doesn't exist
    let errorContainer = document.getElementById('globalError');
    if (!errorContainer) {
        errorContainer = document.createElement('div');
        errorContainer.id = 'globalError';
        errorContainer.className = 'alert alert-danger alert-dismissible fade show position-fixed';
        errorContainer.style.cssText = 'top: 20px; right: 20px; z-index: 9999; max-width: 400px;';
        errorContainer.innerHTML = `
            <strong>Error:</strong> <span id="globalErrorText">${message}</span>
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        document.body.appendChild(errorContainer);
    } else {
        document.getElementById('globalErrorText').textContent = message;
        errorContainer.classList.remove('d-none');
    }
}

// Hide global error message
function hideGlobalError() {
    const errorContainer = document.getElementById('globalError');
    if (errorContainer) {
        errorContainer.classList.add('d-none');
    }
}

// View management
function setCurrentView(viewName) {
    AppState.currentView = viewName;
    console.log(`📱 [App] Vista actual: ${viewName}`);
}

// Loading state management
function setLoadingState(key, isLoading) {
    AppState.loadingStates.set(key, isLoading);
    console.log(`⏳ [App] Loading state [${key}]: ${isLoading}`);
}

function getLoadingState(key) {
    return AppState.loadingStates.get(key) || false;
}

// Error state management
function setErrorState(key, error) {
    AppState.errorStates.set(key, error);
    console.log(`❌ [App] Error state [${key}]:`, error);
}

function getErrorState(key) {
    return AppState.errorStates.get(key) || null;
}

// Performance monitoring
function measurePerformance(name, fn) {
    return async (...args) => {
        const startTime = performance.now();
        try {
            const result = await fn(...args);
            const duration = performance.now() - startTime;
            console.log(`⏱️ [Performance] ${name}: ${duration.toFixed(2)}ms`);
            return result;
        } catch (error) {
            const duration = performance.now() - startTime;
            console.error(`⏱️ [Performance] ${name} failed after ${duration.toFixed(2)}ms:`, error);
            throw error;
        }
    };
}

// Navigation handling
function handleNavigation() {
    // Add navigation event listeners
    document.addEventListener('click', (e) => {
        const link = e.target.closest('a[href]');
        if (link && link.href.includes(window.location.origin)) {
            // Internal navigation
            setLoadingState('navigation', true);
        }
    });
    
    // Handle browser back/forward
    window.addEventListener('popstate', () => {
        setLoadingState('navigation', true);
    });
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeApp);
} else {
    initializeApp();
}

// Handle app ready event
document.addEventListener('appReady', () => {
    console.log('🎉 [App] Aplicación lista para usar');
    handleNavigation();
});

// Export for global use
window.AppState = AppState;
window.initializeApp = initializeApp;
window.setCurrentView = setCurrentView;
window.setLoadingState = setLoadingState;
window.getLoadingState = getLoadingState;
window.setErrorState = setErrorState;
window.getErrorState = getErrorState;
window.measurePerformance = measurePerformance;
window.showGlobalError = showGlobalError;
window.hideGlobalError = hideGlobalError;

console.log('[App] Main application script loaded'); 