/**
 * NeighborTools Map Provider Interface
 * 
 * This file defines the common interface and abstraction layer for multiple map providers.
 * Supports OpenStreetMap (Leaflet), Google Maps, Mapbox, Azure Maps, and HERE Maps.
 * 
 * Design Principles:
 * - Provider-agnostic API for Blazor components
 * - Runtime provider selection based on configuration
 * - Consistent feature set across all providers
 * - Graceful fallback mechanisms
 * - Privacy-first design with configurable data collection
 */

// =============================================================================
// CORE INTERFACE DEFINITIONS
// =============================================================================

/**
 * Base map provider interface that all implementations must follow
 */
class IMapProvider {
    constructor(config) {
        if (this.constructor === IMapProvider) {
            throw new Error("Cannot instantiate abstract class IMapProvider");
        }
        this.config = config;
        this.isInitialized = false;
        this.maps = new Map(); // Container ID -> Map Instance
        this.eventHandlers = new Map(); // Event name -> Handler functions
    }

    // Abstract methods that must be implemented by providers
    async initialize(containerId, options) { throw new Error("Must implement initialize()"); }
    async createMap(containerId, options) { throw new Error("Must implement createMap()"); }
    async updateLocation(containerId, lat, lng, zoom, options) { throw new Error("Must implement updateLocation()"); }
    async addMarker(containerId, lat, lng, options) { throw new Error("Must implement addMarker()"); }
    async removeMarker(containerId, markerId) { throw new Error("Must implement removeMarker()"); }
    async addCircle(containerId, lat, lng, radius, options) { throw new Error("Must implement addCircle()"); }
    async removeCircle(containerId, circleId) { throw new Error("Must implement removeCircle()"); }
    async setView(containerId, lat, lng, zoom) { throw new Error("Must implement setView()"); }
    async getCenter(containerId) { throw new Error("Must implement getCenter()"); }
    async getZoom(containerId) { throw new Error("Must implement getZoom()"); }
    async getCurrentLocation() { throw new Error("Must implement getCurrentLocation()"); }
    async dispose(containerId) { throw new Error("Must implement dispose()"); }
    async toggleFullscreen(containerId) { throw new Error("Must implement toggleFullscreen()"); }

    // Common utility methods (can be overridden)
    isMapInitialized(containerId) {
        return this.maps.has(containerId);
    }

    addEventListener(event, handler) {
        if (!this.eventHandlers.has(event)) {
            this.eventHandlers.set(event, []);
        }
        this.eventHandlers.get(event).push(handler);
    }

    removeEventListener(event, handler) {
        if (this.eventHandlers.has(event)) {
            const handlers = this.eventHandlers.get(event);
            const index = handlers.indexOf(handler);
            if (index > -1) {
                handlers.splice(index, 1);
            }
        }
    }

    emit(event, data) {
        if (this.eventHandlers.has(event)) {
            this.eventHandlers.get(event).forEach(handler => {
                try {
                    handler(data);
                } catch (error) {
                    console.error(`Error in event handler for ${event}:`, error);
                }
            });
        }
    }
}

// =============================================================================
// MAP PROVIDER FACTORY
// =============================================================================

/**
 * Factory class for creating map provider instances
 */
class MapProviderFactory {
    static providers = new Map();
    static defaultProvider = 'openstreetmap';

    /**
     * Register a map provider implementation
     */
    static registerProvider(name, providerClass) {
        if (!providerClass.prototype instanceof IMapProvider) {
            throw new Error(`Provider ${name} must extend IMapProvider`);
        }
        this.providers.set(name.toLowerCase(), providerClass);
        console.log(`Map provider registered: ${name}`);
    }

    /**
     * Create a map provider instance
     */
    static createProvider(providerName, config) {
        const name = (providerName || this.defaultProvider).toLowerCase();
        
        if (!this.providers.has(name)) {
            console.warn(`Map provider '${name}' not found, falling back to '${this.defaultProvider}'`);
            if (!this.providers.has(this.defaultProvider)) {
                throw new Error(`Default map provider '${this.defaultProvider}' not available`);
            }
            return new (this.providers.get(this.defaultProvider))(config);
        }

        const ProviderClass = this.providers.get(name);
        return new ProviderClass(config);
    }

    /**
     * Get list of available providers
     */
    static getAvailableProviders() {
        return Array.from(this.providers.keys());
    }

    /**
     * Check if a provider is available
     */
    static isProviderAvailable(providerName) {
        return this.providers.has(providerName.toLowerCase());
    }
}

// =============================================================================
// UNIFIED MAP MANAGER
// =============================================================================

/**
 * Main map manager that handles provider selection and provides unified API
 */
class NeighborToolsMapManager {
    constructor() {
        this.currentProvider = null;
        this.config = null;
        this.isInitialized = false;
        this.pendingOperations = new Map(); // For operations called before initialization
    }

    /**
     * Initialize the map manager with configuration
     */
    async initialize(config) {
        try {
            this.config = {
                // Default configuration
                provider: 'openstreetmap',
                apiKey: '',
                defaultCenter: { lat: 39.8283, lng: -98.5795 }, // Center of USA
                defaultZoom: 4,
                minZoom: 1,
                maxZoom: 18,
                enableGeolocation: true,
                locationTimeout: 10000,
                maxLocationAge: 300000,
                showLocationControls: true,
                enableFullscreen: true,
                enableSearch: true,
                tileUrl: '',
                attribution: '',
                privacyMode: true,
                // Override with provided config
                ...config
            };

            // Create the appropriate provider
            this.currentProvider = MapProviderFactory.createProvider(
                this.config.provider, 
                this.config
            );

            this.isInitialized = true;
            console.log(`Map manager initialized with provider: ${this.config.provider}`);

            // Process any pending operations
            await this._processPendingOperations();

            return true;
        } catch (error) {
            console.error('Failed to initialize map manager:', error);
            throw error;
        }
    }

    /**
     * Process operations that were called before initialization
     */
    async _processPendingOperations() {
        for (const [operationId, operation] of this.pendingOperations) {
            try {
                await operation();
                this.pendingOperations.delete(operationId);
            } catch (error) {
                console.error(`Failed to process pending operation ${operationId}:`, error);
            }
        }
    }

    /**
     * Queue operation if not initialized yet
     */
    _queueOrExecute(operationName, operation) {
        if (!this.isInitialized) {
            const operationId = `${operationName}_${Date.now()}_${Math.random()}`;
            this.pendingOperations.set(operationId, operation);
            console.log(`Queued operation: ${operationName}`);
            return Promise.resolve();
        }
        return operation();
    }

    // =============================================================================
    // PUBLIC API METHODS (Blazer-callable)
    // =============================================================================

    /**
     * Initialize a map in the specified container
     */
    async initializeMap(containerId, options = {}) {
        return this._queueOrExecute('initializeMap', async () => {
            if (!this.currentProvider) {
                throw new Error('Map manager not initialized');
            }

            const mapOptions = {
                center: options.defaultCenter || this.config.defaultCenter,
                zoom: options.defaultZoom || this.config.defaultZoom,
                minZoom: options.minZoom || this.config.minZoom,
                maxZoom: options.maxZoom || this.config.maxZoom,
                showLocationControls: options.showLocationControls ?? this.config.showLocationControls,
                enableGeolocation: options.enableGeolocation ?? this.config.enableGeolocation,
                tileUrl: options.mapTileUrl || this.config.tileUrl,
                attribution: options.mapAttribution || this.config.attribution,
                ...options
            };

            const success = await this.currentProvider.createMap(containerId, mapOptions);
            
            if (success) {
                console.log(`Map initialized in container: ${containerId}`);
                this.currentProvider.emit('mapInitialized', { containerId, options: mapOptions });
            }
            
            return success;
        });
    }

    /**
     * Update map location with marker and optional privacy circle
     */
    async updateMapLocation(containerId, lat, lng, zoom, options = {}) {
        return this._queueOrExecute('updateMapLocation', async () => {
            if (!this.currentProvider?.isMapInitialized(containerId)) {
                console.warn(`Map not initialized for container: ${containerId}`);
                return false;
            }

            await this.currentProvider.updateLocation(containerId, lat, lng, zoom, options);
            
            // Add privacy circle if privacy level is specified
            if (options.privacyLevel !== undefined) {
                await this.addPrivacyCircle(containerId, lat, lng, options.privacyLevel);
            }

            this.currentProvider.emit('locationUpdated', { containerId, lat, lng, zoom, options });
            return true;
        });
    }

    /**
     * Add a marker to the map
     */
    async addMarker(containerId, lat, lng, options = {}) {
        return this._queueOrExecute('addMarker', async () => {
            if (!this.currentProvider?.isMapInitialized(containerId)) {
                console.warn(`Map not initialized for container: ${containerId}`);
                return null;
            }

            const markerOptions = {
                markerId: options.markerId || `marker_${Date.now()}`,
                markerType: options.markerType || 'default',
                showPopup: options.showPopup || false,
                popupContent: options.popupContent || '',
                icon: options.icon || null,
                ...options
            };

            const markerId = await this.currentProvider.addMarker(containerId, lat, lng, markerOptions);
            this.currentProvider.emit('markerAdded', { containerId, lat, lng, markerId, options: markerOptions });
            return markerId;
        });
    }

    /**
     * Add a privacy circle based on privacy level
     */
    async addPrivacyCircle(containerId, lat, lng, privacyLevel) {
        return this._queueOrExecute('addPrivacyCircle', async () => {
            if (!this.currentProvider?.isMapInitialized(containerId)) {
                console.warn(`Map not initialized for container: ${containerId}`);
                return null;
            }

            // Convert privacy level to radius (in meters)
            const radiusMap = {
                0: 150,    // Exact - ~300 feet
                1: 500,    // Neighborhood - ~0.3 mile
                2: 1600,   // ZipCode - ~1 mile  
                3: 4800    // District - ~3 miles
            };

            const radius = radiusMap[privacyLevel] || radiusMap[1];
            
            const circleOptions = {
                circleId: 'privacy-circle',
                fillColor: '#594ae2',
                fillOpacity: 0.1,
                strokeColor: '#594ae2',
                strokeOpacity: 0.3,
                strokeWeight: 2,
                radius: radius
            };

            const circleId = await this.currentProvider.addCircle(containerId, lat, lng, radius, circleOptions);
            this.currentProvider.emit('privacyCircleAdded', { containerId, lat, lng, radius, privacyLevel });
            return circleId;
        });
    }

    /**
     * Remove a marker from the map
     */
    async removeMarker(containerId, markerId) {
        return this._queueOrExecute('removeMarker', async () => {
            if (!this.currentProvider?.isMapInitialized(containerId)) {
                return false;
            }

            const success = await this.currentProvider.removeMarker(containerId, markerId);
            if (success) {
                this.currentProvider.emit('markerRemoved', { containerId, markerId });
            }
            return success;
        });
    }

    /**
     * Toggle fullscreen mode for the map
     */
    async toggleFullscreen(containerId) {
        return this._queueOrExecute('toggleFullscreen', async () => {
            if (!this.currentProvider?.isMapInitialized(containerId)) {
                return false;
            }

            const success = await this.currentProvider.toggleFullscreen(containerId);
            if (success) {
                this.currentProvider.emit('fullscreenToggled', { containerId });
            }
            return success;
        });
    }

    /**
     * Get current location using browser geolocation
     */
    async getCurrentLocation() {
        if (!this.currentProvider) {
            throw new Error('Map manager not initialized');
        }

        try {
            const location = await this.currentProvider.getCurrentLocation();
            this.currentProvider.emit('currentLocationObtained', location);
            return location;
        } catch (error) {
            console.error('Failed to get current location:', error);
            throw error;
        }
    }

    /**
     * Set map view to specific coordinates and zoom
     */
    async setMapView(containerId, lat, lng, zoom) {
        return this._queueOrExecute('setMapView', async () => {
            if (!this.currentProvider?.isMapInitialized(containerId)) {
                return false;
            }

            await this.currentProvider.setView(containerId, lat, lng, zoom);
            this.currentProvider.emit('viewChanged', { containerId, lat, lng, zoom });
            return true;
        });
    }

    /**
     * Get map center coordinates
     */
    async getMapCenter(containerId) {
        if (!this.currentProvider?.isMapInitialized(containerId)) {
            return null;
        }

        return await this.currentProvider.getCenter(containerId);
    }

    /**
     * Get current map zoom level
     */
    async getMapZoom(containerId) {
        if (!this.currentProvider?.isMapInitialized(containerId)) {
            return null;
        }

        return await this.currentProvider.getZoom(containerId);
    }

    /**
     * Dispose of a map instance
     */
    async disposeMap(containerId) {
        if (!this.currentProvider?.isMapInitialized(containerId)) {
            return true;
        }

        const success = await this.currentProvider.dispose(containerId);
        if (success) {
            this.currentProvider.emit('mapDisposed', { containerId });
            console.log(`Map disposed: ${containerId}`);
        }
        return success;
    }

    // =============================================================================
    // UTILITY METHODS
    // =============================================================================

    /**
     * Get current provider information
     */
    getProviderInfo() {
        return {
            name: this.config?.provider || 'none',
            isInitialized: this.isInitialized,
            availableProviders: MapProviderFactory.getAvailableProviders(),
            config: this.config
        };
    }

    /**
     * Switch to a different map provider (requires re-initialization of maps)
     */
    async switchProvider(providerName, newConfig = {}) {
        if (!MapProviderFactory.isProviderAvailable(providerName)) {
            throw new Error(`Provider '${providerName}' is not available`);
        }

        // Dispose of current provider's maps
        if (this.currentProvider) {
            for (const containerId of this.currentProvider.maps.keys()) {
                await this.disposeMap(containerId);
            }
        }

        // Update configuration and create new provider
        this.config = { ...this.config, ...newConfig, provider: providerName };
        this.currentProvider = MapProviderFactory.createProvider(providerName, this.config);
        
        console.log(`Switched to map provider: ${providerName}`);
        this.currentProvider.emit('providerSwitched', { from: this.config.provider, to: providerName });
        
        return true;
    }

    /**
     * Add event listener to the current provider
     */
    addEventListener(event, handler) {
        if (this.currentProvider) {
            this.currentProvider.addEventListener(event, handler);
        }
    }

    /**
     * Remove event listener from the current provider
     */
    removeEventListener(event, handler) {
        if (this.currentProvider) {
            this.currentProvider.removeEventListener(event, handler);
        }
    }

    /**
     * Get debugging information
     */
    getDebugInfo() {
        return {
            managerInitialized: this.isInitialized,
            currentProvider: this.config?.provider,
            availableProviders: MapProviderFactory.getAvailableProviders(),
            activeMaps: this.currentProvider ? Array.from(this.currentProvider.maps.keys()) : [],
            pendingOperations: Array.from(this.pendingOperations.keys()),
            config: this.config
        };
    }
}

// =============================================================================
// GLOBAL INSTANCE AND BLAZOR BRIDGE
// =============================================================================

// Create global instance
window.NeighborToolsMapManager = window.NeighborToolsMapManager || new NeighborToolsMapManager();

// Blazor-callable functions (maintain compatibility with existing components)
window.initializeMap = async (containerId, options) => {
    return await window.NeighborToolsMapManager.initializeMap(containerId, options);
};

window.updateMapLocation = async (containerId, lat, lng, zoom, options) => {
    return await window.NeighborToolsMapManager.updateMapLocation(containerId, lat, lng, zoom, options);
};

window.addMarker = async (containerId, lat, lng, options) => {
    return await window.NeighborToolsMapManager.addMarker(containerId, lat, lng, options);
};

window.addPrivacyCircle = async (containerId, lat, lng, privacyLevel) => {
    return await window.NeighborToolsMapManager.addPrivacyCircle(containerId, lat, lng, privacyLevel);
};

window.toggleFullscreen = async (containerId) => {
    return await window.NeighborToolsMapManager.toggleFullscreen(containerId);
};

window.disposeMap = async (containerId) => {
    return await window.NeighborToolsMapManager.disposeMap(containerId);
};

window.getCurrentLocation = async () => {
    return await window.NeighborToolsMapManager.getCurrentLocation();
};

// Advanced functions for provider management
window.getMapProviderInfo = () => {
    return window.NeighborToolsMapManager.getProviderInfo();
};

window.switchMapProvider = async (providerName, config) => {
    return await window.NeighborToolsMapManager.switchProvider(providerName, config);
};

window.getMapDebugInfo = () => {
    return window.NeighborToolsMapManager.getDebugInfo();
};

// =============================================================================
// INITIALIZATION HELPER
// =============================================================================

/**
 * Initialize the map manager when the DOM is ready
 */
document.addEventListener('DOMContentLoaded', () => {
    console.log('NeighborTools Map Provider Interface loaded');
    console.log('Available functions:', {
        'initializeMap': 'Initialize a map in a container',
        'updateMapLocation': 'Update map location with markers',
        'addMarker': 'Add a marker to the map',
        'addPrivacyCircle': 'Add privacy circle around location',
        'toggleFullscreen': 'Toggle fullscreen mode',
        'disposeMap': 'Clean up map instance',
        'getCurrentLocation': 'Get user location',
        'getMapProviderInfo': 'Get provider information',
        'switchMapProvider': 'Switch to different provider',
        'getMapDebugInfo': 'Get debugging information'
    });
});

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        IMapProvider,
        MapProviderFactory,
        NeighborToolsMapManager
    };
}