/**
 * NeighborTools Map Initialization Script
 * 
 * This script initializes the map provider system and sets up the
 * appropriate provider based on configuration. It acts as the bootstrap
 * for the entire map system.
 */

(function() {
    'use strict';

    // Configuration detection and initialization
    let mapConfig = null;
    let initializationPromise = null;

    /**
     * Get map configuration from various sources
     */
    function getMapConfiguration() {
        // Try to get config from global variables set by Blazor
        if (window.blazorMapConfig) {
            return window.blazorMapConfig;
        }

        // Try to get config from meta tags
        const configMeta = document.querySelector('meta[name="map-config"]');
        if (configMeta) {
            try {
                return JSON.parse(configMeta.getAttribute('content'));
            } catch (e) {
                console.warn('Failed to parse map config from meta tag:', e);
            }
        }

        // Try to get config from data attributes on the body
        const body = document.body;
        if (body.hasAttribute('data-map-provider')) {
            return {
                provider: body.getAttribute('data-map-provider'),
                apiKey: body.getAttribute('data-map-api-key') || '',
                defaultCenter: {
                    lat: parseFloat(body.getAttribute('data-map-default-lat')) || 39.8283,
                    lng: parseFloat(body.getAttribute('data-map-default-lng')) || -98.5795
                },
                defaultZoom: parseInt(body.getAttribute('data-map-default-zoom')) || 4
            };
        }

        // Default configuration
        return {
            provider: 'openstreetmap',
            apiKey: '',
            defaultCenter: { lat: 39.8283, lng: -98.5795 },
            defaultZoom: 4,
            minZoom: 1,
            maxZoom: 18,
            enableGeolocation: true,
            locationTimeout: 10000,
            maxLocationAge: 300000,
            showLocationControls: true,
            enableFullscreen: true,
            privacyMode: true,
            tileUrl: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
            attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        };
    }

    /**
     * Initialize the map system
     */
    async function initializeMapSystem() {
        if (initializationPromise) {
            return initializationPromise;
        }

        initializationPromise = (async () => {
            try {
                console.log('Initializing NeighborTools Map System...');

                // Get configuration
                mapConfig = getMapConfiguration();
                console.log('Map configuration:', mapConfig);

                // Wait for required dependencies
                await waitForDependencies();

                // Initialize the map manager
                const success = await window.NeighborToolsMapManager.initialize(mapConfig);
                
                if (success) {
                    console.log('NeighborTools Map System initialized successfully');
                    
                    // Emit initialization event
                    window.dispatchEvent(new CustomEvent('neighbortools:map:initialized', {
                        detail: { config: mapConfig }
                    }));
                    
                    return true;
                } else {
                    throw new Error('Failed to initialize map manager');
                }
            } catch (error) {
                console.error('Failed to initialize NeighborTools Map System:', error);
                
                // Emit error event
                window.dispatchEvent(new CustomEvent('neighbortools:map:error', {
                    detail: { error: error.message }
                }));
                
                throw error;
            }
        })();

        return initializationPromise;
    }

    /**
     * Wait for required dependencies to be available
     */
    async function waitForDependencies() {
        const maxWaitTime = 30000; // 30 seconds
        const checkInterval = 100; // 100ms
        let elapsed = 0;

        return new Promise((resolve, reject) => {
            const checkDependencies = () => {
                // Check for core interface
                if (typeof window.NeighborToolsMapManager === 'undefined') {
                    elapsed += checkInterval;
                    if (elapsed >= maxWaitTime) {
                        reject(new Error('Map provider interface not loaded within timeout'));
                        return;
                    }
                    setTimeout(checkDependencies, checkInterval);
                    return;
                }

                // Check for provider-specific dependencies
                const provider = mapConfig.provider?.toLowerCase() || 'openstreetmap';
                
                switch (provider) {
                    case 'openstreetmap':
                    case 'leaflet':
                        if (typeof L === 'undefined') {
                            elapsed += checkInterval;
                            if (elapsed >= maxWaitTime) {
                                reject(new Error('Leaflet.js not loaded within timeout'));
                                return;
                            }
                            setTimeout(checkDependencies, checkInterval);
                            return;
                        }
                        break;
                        
                    case 'googlemaps':
                    case 'google':
                        if (typeof google === 'undefined' || !google.maps) {
                            // Google Maps can be loaded dynamically, so we don't fail here
                            console.log('Google Maps API will be loaded dynamically');
                        }
                        break;
                        
                    case 'mapbox':
                        if (typeof mapboxgl === 'undefined') {
                            elapsed += checkInterval;
                            if (elapsed >= maxWaitTime) {
                                reject(new Error('Mapbox GL JS not loaded within timeout'));
                                return;
                            }
                            setTimeout(checkDependencies, checkInterval);
                            return;
                        }
                        break;
                }

                resolve();
            };

            checkDependencies();
        });
    }

    /**
     * Update configuration (called from Blazor)
     */
    window.updateMapConfiguration = function(newConfig) {
        console.log('Updating map configuration:', newConfig);
        mapConfig = { ...mapConfig, ...newConfig };
        
        // Re-initialize if needed
        if (window.NeighborToolsMapManager.isInitialized) {
            return window.NeighborToolsMapManager.switchProvider(
                newConfig.provider || mapConfig.provider,
                mapConfig
            );
        }
        
        return Promise.resolve(true);
    };

    /**
     * Get current configuration
     */
    window.getMapConfiguration = function() {
        return mapConfig;
    };

    /**
     * Initialize when DOM is ready
     */
    function onDOMReady() {
        console.log('DOM ready, initializing map system...');
        
        // Start initialization
        initializeMapSystem().catch(error => {
            console.error('Map system initialization failed:', error);
        });
    }

    /**
     * Auto-initialize based on document ready state
     */
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', onDOMReady);
    } else {
        // DOM is already ready
        setTimeout(onDOMReady, 0);
    }

    // Manual initialization function
    window.initializeNeighborToolsMaps = initializeMapSystem;

    // Provider loading helpers
    window.loadMapProvider = async function(providerName) {
        const providers = {
            openstreetmap: '/js/providers/openstreetmap-provider.js',
            googlemaps: '/js/providers/googlemaps-provider.js',
            mapbox: '/js/providers/mapbox-provider.js',
            azure: '/js/providers/azure-provider.js',
            here: '/js/providers/here-provider.js'
        };

        const scriptUrl = providers[providerName.toLowerCase()];
        if (!scriptUrl) {
            throw new Error(`Unknown provider: ${providerName}`);
        }

        return new Promise((resolve, reject) => {
            // Check if already loaded
            if (document.querySelector(`script[src="${scriptUrl}"]`)) {
                resolve();
                return;
            }

            const script = document.createElement('script');
            script.src = scriptUrl;
            script.async = true;
            script.onload = () => resolve();
            script.onerror = () => reject(new Error(`Failed to load provider: ${providerName}`));
            
            document.head.appendChild(script);
        });
    };

    // Debug helper
    window.debugMapSystem = function() {
        console.log('=== NeighborTools Map System Debug ===');
        console.log('Configuration:', mapConfig);
        console.log('Manager Info:', window.NeighborToolsMapManager?.getProviderInfo());
        console.log('Debug Info:', window.NeighborToolsMapManager?.getDebugInfo());
        console.log('Available Providers:', MapProviderFactory?.getAvailableProviders());
        console.log('======================================');
    };

    // Expose configuration getter globally
    window.getNeighborToolsMapConfig = () => mapConfig;

    console.log('NeighborTools Map Initialization Script loaded');
})();