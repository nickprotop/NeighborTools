/**
 * OpenStreetMap Provider Implementation
 * 
 * Implements the IMapProvider interface using Leaflet.js for OpenStreetMap integration.
 * This is a refactored version of the original openstreetmap-leaflet.js to follow
 * the new provider interface pattern.
 */

/**
 * OpenStreetMap provider implementation using Leaflet.js
 */
class OpenStreetMapProvider extends IMapProvider {
    constructor(config) {
        super(config);
        this.markers = new Map(); // Container ID -> Map of markers
        this.circles = new Map();  // Container ID -> Map of circles
        this.defaultTileUrl = 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png';
        this.defaultAttribution = '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors';
    }

    /**
     * Initialize the provider (load required libraries)
     */
    async initialize() {
        if (this.isInitialized) {
            return true;
        }

        // Check if Leaflet is available
        if (typeof L === 'undefined') {
            console.error('Leaflet.js is not loaded. Please include Leaflet.js before using OpenStreetMapProvider.');
            return false;
        }

        this.isInitialized = true;
        console.log('OpenStreetMapProvider initialized successfully');
        return true;
    }

    /**
     * Create a new map instance in the specified container
     */
    async createMap(containerId, options = {}) {
        if (!this.isInitialized) {
            await this.initialize();
        }

        try {
            const container = document.getElementById(containerId);
            if (!container) {
                throw new Error(`Container element with ID '${containerId}' not found`);
            }

            // Clear any existing content
            container.innerHTML = '';

            // Prepare tile layer options
            const tileUrl = options.tileUrl || this.config.tileUrl || this.defaultTileUrl;
            const attribution = options.attribution || this.config.attribution || this.defaultAttribution;

            // Create the map
            const map = L.map(containerId, {
                center: [options.center?.lat || options.center?.Lat || 39.8283, 
                        options.center?.lng || options.center?.Lng || -98.5795],
                zoom: options.zoom || 4,
                minZoom: options.minZoom || 1,
                maxZoom: options.maxZoom || 18,
                zoomControl: options.showLocationControls !== false,
                attributionControl: false // We'll add custom attribution
            });

            // Add tile layer
            L.tileLayer(tileUrl, {
                maxZoom: options.maxZoom || 18,
                attribution: attribution,
                className: 'map-tiles'
            }).addTo(map);

            // Add custom attribution control if needed
            if (attribution) {
                L.control.attribution({
                    position: 'bottomright',
                    prefix: false
                }).addAttribution(attribution).addTo(map);
            }

            // Add scale control
            L.control.scale({
                position: 'bottomleft',
                imperial: true,
                metric: true
            }).addTo(map);

            // Initialize marker and circle storage for this container
            this.markers.set(containerId, new Map());
            this.circles.set(containerId, new Map());

            // Store the map instance
            this.maps.set(containerId, map);

            // Add event listeners
            this._addMapEventListeners(containerId, map);

            console.log(`OpenStreetMap created successfully in container: ${containerId}`);
            return true;

        } catch (error) {
            console.error(`Failed to create OpenStreetMap in container '${containerId}':`, error);
            return false;
        }
    }

    /**
     * Update map location with optional marker and panning
     */
    async updateLocation(containerId, lat, lng, zoom, options = {}) {
        const map = this.maps.get(containerId);
        if (!map) {
            throw new Error(`Map not found for container: ${containerId}`);
        }

        try {
            // Update map view if requested
            if (options.panTo !== false) {
                map.setView([lat, lng], zoom || map.getZoom(), {
                    animate: true,
                    duration: 0.5
                });
            }

            // Add or update marker if requested
            if (options.markerId) {
                await this.addMarker(containerId, lat, lng, {
                    markerId: options.markerId,
                    markerType: options.markerType || 'default',
                    showPopup: options.showPopup || false,
                    popupContent: options.popupContent || '',
                    replace: true
                });
            }

            return true;
        } catch (error) {
            console.error(`Failed to update location for container '${containerId}':`, error);
            throw error;
        }
    }

    /**
     * Add a marker to the map
     */
    async addMarker(containerId, lat, lng, options = {}) {
        const map = this.maps.get(containerId);
        const containerMarkers = this.markers.get(containerId);
        
        if (!map || !containerMarkers) {
            throw new Error(`Map not found for container: ${containerId}`);
        }

        try {
            const markerId = options.markerId || `marker_${Date.now()}_${Math.random()}`;

            // Remove existing marker if replacing
            if (options.replace && containerMarkers.has(markerId)) {
                await this.removeMarker(containerId, markerId);
            }

            // Create marker icon based on type
            let icon;
            switch (options.markerType) {
                case 'current-location':
                    icon = L.divIcon({
                        className: 'current-location-marker',
                        html: '<div class="location-pulse"></div><div class="location-dot"></div>',
                        iconSize: [20, 20],
                        iconAnchor: [10, 10]
                    });
                    break;
                case 'tool':
                    icon = L.divIcon({
                        className: 'tool-marker',
                        html: '<i class="material-icons">build</i>',
                        iconSize: [30, 30],
                        iconAnchor: [15, 30]
                    });
                    break;
                case 'bundle':
                    icon = L.divIcon({
                        className: 'bundle-marker',
                        html: '<i class="material-icons">inventory</i>',
                        iconSize: [30, 30],
                        iconAnchor: [15, 30]
                    });
                    break;
                case 'search':
                    icon = L.divIcon({
                        className: 'search-marker',
                        html: '<i class="material-icons">search</i>',
                        iconSize: [25, 25],
                        iconAnchor: [12, 25]
                    });
                    break;
                default:
                    icon = L.marker([lat, lng]).options.icon; // Default Leaflet icon
            }

            // Create the marker
            const marker = L.marker([lat, lng], { icon }).addTo(map);

            // Add popup if requested
            if (options.showPopup && options.popupContent) {
                marker.bindPopup(options.popupContent).openPopup();
            }

            // Store the marker
            containerMarkers.set(markerId, marker);

            console.log(`Marker '${markerId}' added to map '${containerId}'`);
            return markerId;

        } catch (error) {
            console.error(`Failed to add marker to container '${containerId}':`, error);
            throw error;
        }
    }

    /**
     * Remove a marker from the map
     */
    async removeMarker(containerId, markerId) {
        const map = this.maps.get(containerId);
        const containerMarkers = this.markers.get(containerId);
        
        if (!map || !containerMarkers) {
            return false;
        }

        try {
            const marker = containerMarkers.get(markerId);
            if (marker) {
                map.removeLayer(marker);
                containerMarkers.delete(markerId);
                console.log(`Marker '${markerId}' removed from map '${containerId}'`);
                return true;
            }
            return false;
        } catch (error) {
            console.error(`Failed to remove marker '${markerId}' from container '${containerId}':`, error);
            return false;
        }
    }

    /**
     * Add a circle to the map (used for privacy zones)
     */
    async addCircle(containerId, lat, lng, radius, options = {}) {
        const map = this.maps.get(containerId);
        const containerCircles = this.circles.get(containerId);
        
        if (!map || !containerCircles) {
            throw new Error(`Map not found for container: ${containerId}`);
        }

        try {
            const circleId = options.circleId || `circle_${Date.now()}_${Math.random()}`;

            // Remove existing circle if replacing
            if (containerCircles.has(circleId)) {
                await this.removeCircle(containerId, circleId);
            }

            // Create circle options
            const circleOptions = {
                color: options.strokeColor || '#594ae2',
                weight: options.strokeWeight || 2,
                opacity: options.strokeOpacity || 0.3,
                fillColor: options.fillColor || '#594ae2',
                fillOpacity: options.fillOpacity || 0.1,
                radius: radius
            };

            // Create the circle
            const circle = L.circle([lat, lng], circleOptions).addTo(map);

            // Store the circle
            containerCircles.set(circleId, circle);

            console.log(`Circle '${circleId}' added to map '${containerId}' with radius ${radius}m`);
            return circleId;

        } catch (error) {
            console.error(`Failed to add circle to container '${containerId}':`, error);
            throw error;
        }
    }

    /**
     * Remove a circle from the map
     */
    async removeCircle(containerId, circleId) {
        const map = this.maps.get(containerId);
        const containerCircles = this.circles.get(containerId);
        
        if (!map || !containerCircles) {
            return false;
        }

        try {
            const circle = containerCircles.get(circleId);
            if (circle) {
                map.removeLayer(circle);
                containerCircles.delete(circleId);
                console.log(`Circle '${circleId}' removed from map '${containerId}'`);
                return true;
            }
            return false;
        } catch (error) {
            console.error(`Failed to remove circle '${circleId}' from container '${containerId}':`, error);
            return false;
        }
    }

    /**
     * Set map view to specific coordinates and zoom
     */
    async setView(containerId, lat, lng, zoom) {
        const map = this.maps.get(containerId);
        if (!map) {
            throw new Error(`Map not found for container: ${containerId}`);
        }

        map.setView([lat, lng], zoom, {
            animate: true,
            duration: 0.5
        });
    }

    /**
     * Get map center coordinates
     */
    async getCenter(containerId) {
        const map = this.maps.get(containerId);
        if (!map) {
            return null;
        }

        const center = map.getCenter();
        return {
            lat: center.lat,
            lng: center.lng
        };
    }

    /**
     * Get current map zoom level
     */
    async getZoom(containerId) {
        const map = this.maps.get(containerId);
        if (!map) {
            return null;
        }

        return map.getZoom();
    }

    /**
     * Get current location using browser geolocation
     */
    async getCurrentLocation() {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject(new Error('Geolocation is not supported by this browser'));
                return;
            }

            const options = {
                enableHighAccuracy: true,
                timeout: this.config.locationTimeout || 10000,
                maximumAge: this.config.maxLocationAge || 300000
            };

            navigator.geolocation.getCurrentPosition(
                (position) => {
                    resolve({
                        lat: position.coords.latitude,
                        lng: position.coords.longitude,
                        accuracy: position.coords.accuracy,
                        timestamp: position.timestamp
                    });
                },
                (error) => {
                    let message;
                    switch (error.code) {
                        case error.PERMISSION_DENIED:
                            message = 'Location access denied by user';
                            break;
                        case error.POSITION_UNAVAILABLE:
                            message = 'Location information unavailable';
                            break;
                        case error.TIMEOUT:
                            message = 'Location request timed out';
                            break;
                        default:
                            message = 'An unknown error occurred while getting location';
                            break;
                    }
                    reject(new Error(message));
                },
                options
            );
        });
    }

    /**
     * Toggle fullscreen mode for the map
     */
    async toggleFullscreen(containerId) {
        const container = document.getElementById(containerId);
        if (!container) {
            return false;
        }

        try {
            if (!document.fullscreenElement) {
                await container.requestFullscreen();
                container.classList.add('map-fullscreen');
            } else {
                await document.exitFullscreen();
                container.classList.remove('map-fullscreen');
            }
            
            // Trigger map resize after fullscreen change
            setTimeout(() => {
                const map = this.maps.get(containerId);
                if (map) {
                    map.invalidateSize();
                }
            }, 100);

            return true;
        } catch (error) {
            console.error(`Failed to toggle fullscreen for container '${containerId}':`, error);
            return false;
        }
    }

    /**
     * Dispose of a map instance and clean up resources
     */
    async dispose(containerId) {
        try {
            const map = this.maps.get(containerId);
            if (map) {
                // Clear all markers
                const containerMarkers = this.markers.get(containerId);
                if (containerMarkers) {
                    for (const [markerId, marker] of containerMarkers) {
                        map.removeLayer(marker);
                    }
                    this.markers.delete(containerId);
                }

                // Clear all circles
                const containerCircles = this.circles.get(containerId);
                if (containerCircles) {
                    for (const [circleId, circle] of containerCircles) {
                        map.removeLayer(circle);
                    }
                    this.circles.delete(containerId);
                }

                // Remove the map
                map.remove();
                this.maps.delete(containerId);

                console.log(`OpenStreetMap disposed for container: ${containerId}`);
            }

            return true;
        } catch (error) {
            console.error(`Failed to dispose map for container '${containerId}':`, error);
            return false;
        }
    }

    /**
     * Add event listeners to the map
     */
    _addMapEventListeners(containerId, map) {
        map.on('click', (e) => {
            this.emit('mapClick', {
                containerId,
                lat: e.latlng.lat,
                lng: e.latlng.lng,
                originalEvent: e
            });
        });

        map.on('moveend', (e) => {
            const center = map.getCenter();
            this.emit('mapMoveEnd', {
                containerId,
                center: { lat: center.lat, lng: center.lng },
                zoom: map.getZoom(),
                originalEvent: e
            });
        });

        map.on('zoomend', (e) => {
            this.emit('mapZoomEnd', {
                containerId,
                zoom: map.getZoom(),
                originalEvent: e
            });
        });

        map.on('ready', (e) => {
            console.log(`Map ready: ${containerId}`);
            this.emit('mapReady', { containerId, originalEvent: e });
        });
    }
}

// CSS for custom markers (injected dynamically)
if (!document.getElementById('openstreetmap-marker-styles')) {
    const style = document.createElement('style');
    style.id = 'openstreetmap-marker-styles';
    style.textContent = `
        /* Current Location Marker */
        .current-location-marker {
            background: transparent;
            border: none;
            position: relative;
        }
        
        .location-pulse {
            width: 20px;
            height: 20px;
            border: 2px solid #594ae2;
            border-radius: 50%;
            position: absolute;
            top: 0;
            left: 0;
            animation: locationPulse 2s infinite;
            background: rgba(89, 74, 226, 0.1);
        }
        
        .location-dot {
            width: 8px;
            height: 8px;
            background: #594ae2;
            border-radius: 50%;
            position: absolute;
            top: 6px;
            left: 6px;
            border: 2px solid white;
            box-shadow: 0 2px 4px rgba(0,0,0,0.3);
        }
        
        @keyframes locationPulse {
            0% { transform: scale(1); opacity: 1; }
            100% { transform: scale(2); opacity: 0; }
        }
        
        /* Tool Marker */
        .tool-marker {
            background: #2196F3;
            color: white;
            border-radius: 50% 50% 50% 0;
            transform: rotate(-45deg);
            display: flex;
            align-items: center;
            justify-content: center;
            box-shadow: 0 2px 8px rgba(0,0,0,0.3);
            border: 2px solid white;
        }
        
        .tool-marker i {
            transform: rotate(45deg);
            font-size: 16px;
        }
        
        /* Bundle Marker */
        .bundle-marker {
            background: #FF9800;
            color: white;
            border-radius: 50% 50% 50% 0;
            transform: rotate(-45deg);
            display: flex;
            align-items: center;
            justify-content: center;
            box-shadow: 0 2px 8px rgba(0,0,0,0.3);
            border: 2px solid white;
        }
        
        .bundle-marker i {
            transform: rotate(45deg);
            font-size: 16px;
        }
        
        /* Search Marker */
        .search-marker {
            background: #9C27B0;
            color: white;
            border-radius: 50% 50% 50% 0;
            transform: rotate(-45deg);
            display: flex;
            align-items: center;
            justify-content: center;
            box-shadow: 0 2px 8px rgba(0,0,0,0.3);
            border: 2px solid white;
        }
        
        .search-marker i {
            transform: rotate(45deg);
            font-size: 14px;
        }
        
        /* Fullscreen Map Styling */
        .map-fullscreen {
            z-index: 9999 !important;
        }
        
        .map-fullscreen .leaflet-container {
            height: 100vh !important;
            width: 100vw !important;
        }
    `;
    document.head.appendChild(style);
}

// Register the OpenStreetMap provider
if (typeof MapProviderFactory !== 'undefined') {
    MapProviderFactory.registerProvider('openstreetmap', OpenStreetMapProvider);
    MapProviderFactory.registerProvider('leaflet', OpenStreetMapProvider); // Alias
    console.log('OpenStreetMapProvider registered successfully');
}

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = OpenStreetMapProvider;
}