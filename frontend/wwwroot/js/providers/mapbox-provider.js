/**
 * Mapbox GL JS Provider Implementation
 * 
 * Implements the IMapProvider interface using Mapbox GL JS.
 * This is a template implementation showing how to integrate Mapbox
 * with the unified map provider interface.
 * 
 * Prerequisites:
 * - Mapbox API access token
 * - Mapbox GL JS library loaded in the page
 */

/**
 * Mapbox GL JS provider implementation
 */
class MapboxProvider extends IMapProvider {
    constructor(config) {
        super(config);
        this.markers = new Map(); // Container ID -> Map of markers
        this.circles = new Map();  // Container ID -> Map of circles (layers)
        this.popups = new Map();   // Container ID -> Map of popups
        this.accessToken = config.apiKey || config.mapboxAccessToken || '';
        
        if (!this.accessToken) {
            console.warn('Mapbox access token not provided. Some features may not work.');
        }
    }

    /**
     * Initialize the provider (set access token and check library)
     */
    async initialize() {
        if (this.isInitialized) {
            return true;
        }

        // Check if Mapbox GL JS is available
        if (typeof mapboxgl === 'undefined') {
            console.error('Mapbox GL JS is not loaded. Please include Mapbox GL JS before using MapboxProvider.');
            return false;
        }

        // Set access token
        if (this.accessToken) {
            mapboxgl.accessToken = this.accessToken;
        } else {
            console.error('Mapbox access token is required');
            return false;
        }

        this.isInitialized = true;
        console.log('MapboxProvider initialized successfully');
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

            // Create map options
            const mapOptions = {
                container: containerId,
                style: options.mapStyle || 'mapbox://styles/mapbox/streets-v11',
                center: [
                    options.center?.lng || options.center?.Lng || -98.5795,
                    options.center?.lat || options.center?.Lat || 39.8283
                ],
                zoom: options.zoom || 4,
                minZoom: options.minZoom || 1,
                maxZoom: options.maxZoom || 18,
                attributionControl: true,
                dragRotate: false, // Disable rotation for consistent UX
                pitchWithRotate: false
            };

            // Create the map
            const map = new mapboxgl.Map(mapOptions);

            // Add navigation control if requested
            if (options.showLocationControls !== false) {
                map.addControl(new mapboxgl.NavigationControl(), 'top-right');
            }

            // Add fullscreen control if requested
            if (options.enableFullscreen !== false) {
                map.addControl(new mapboxgl.FullscreenControl(), 'top-right');
            }

            // Add geolocate control if requested
            if (options.enableGeolocation !== false) {
                const geolocateControl = new mapboxgl.GeolocateControl({
                    positionOptions: {
                        enableHighAccuracy: true
                    },
                    trackUserLocation: false,
                    showUserHeading: false
                });
                map.addControl(geolocateControl, 'top-right');
            }

            // Initialize storage for this container
            this.markers.set(containerId, new Map());
            this.circles.set(containerId, new Map());
            this.popups.set(containerId, new Map());

            // Store the map instance
            this.maps.set(containerId, map);

            // Wait for map to load before adding event listeners
            await new Promise((resolve) => {
                map.on('load', () => {
                    this._addMapEventListeners(containerId, map);
                    resolve();
                });
            });

            console.log(`Mapbox GL JS map created successfully in container: ${containerId}`);
            return true;

        } catch (error) {
            console.error(`Failed to create Mapbox map in container '${containerId}':`, error);
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
                await new Promise((resolve) => {
                    map.flyTo({
                        center: [lng, lat],
                        zoom: zoom || map.getZoom(),
                        duration: 1000,
                        essential: true
                    });
                    map.once('moveend', resolve);
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
        const containerPopups = this.popups.get(containerId);
        
        if (!map || !containerMarkers || !containerPopups) {
            throw new Error(`Map not found for container: ${containerId}`);
        }

        try {
            const markerId = options.markerId || `marker_${Date.now()}_${Math.random()}`;

            // Remove existing marker if replacing
            if (options.replace && containerMarkers.has(markerId)) {
                await this.removeMarker(containerId, markerId);
            }

            // Create marker element
            const markerElement = this._createMarkerElement(options.markerType || 'default');

            // Create the marker
            const marker = new mapboxgl.Marker(markerElement)
                .setLngLat([lng, lat])
                .addTo(map);

            // Add popup if requested
            if (options.showPopup && options.popupContent) {
                const popup = new mapboxgl.Popup({
                    offset: 25,
                    closeButton: true,
                    closeOnClick: false
                }).setHTML(options.popupContent);

                marker.setPopup(popup);
                containerPopups.set(markerId, popup);

                // Open immediately if requested
                if (options.openPopup) {
                    marker.togglePopup();
                }
            }

            // Store the marker
            containerMarkers.set(markerId, marker);

            console.log(`Marker '${markerId}' added to Mapbox map '${containerId}'`);
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
        const containerMarkers = this.markers.get(containerId);
        const containerPopups = this.popups.get(containerId);
        
        if (!containerMarkers) {
            return false;
        }

        try {
            const marker = containerMarkers.get(markerId);
            if (marker) {
                marker.remove();
                containerMarkers.delete(markerId);

                // Remove associated popup
                const popup = containerPopups?.get(markerId);
                if (popup) {
                    popup.remove();
                    containerPopups.delete(markerId);
                }

                console.log(`Marker '${markerId}' removed from Mapbox map '${containerId}'`);
                return true;
            }
            return false;
        } catch (error) {
            console.error(`Failed to remove marker '${markerId}' from container '${containerId}':`, error);
            return false;
        }
    }

    /**
     * Add a circle to the map using a layer (for privacy zones)
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

            // Create circle as a GeoJSON feature
            const circleGeoJSON = this._createCircleGeoJSON([lng, lat], radius);

            // Add source and layer for the circle
            const sourceId = `${circleId}-source`;
            const layerId = `${circleId}-layer`;

            map.addSource(sourceId, {
                type: 'geojson',
                data: circleGeoJSON
            });

            map.addLayer({
                id: layerId,
                type: 'fill',
                source: sourceId,
                paint: {
                    'fill-color': options.fillColor || '#594ae2',
                    'fill-opacity': options.fillOpacity || 0.1,
                    'fill-outline-color': options.strokeColor || '#594ae2'
                }
            });

            // Add stroke layer
            const strokeLayerId = `${circleId}-stroke`;
            map.addLayer({
                id: strokeLayerId,
                type: 'line',
                source: sourceId,
                paint: {
                    'line-color': options.strokeColor || '#594ae2',
                    'line-width': options.strokeWeight || 2,
                    'line-opacity': options.strokeOpacity || 0.3
                }
            });

            // Store the circle info
            containerCircles.set(circleId, {
                sourceId,
                layerId,
                strokeLayerId,
                center: [lng, lat],
                radius
            });

            console.log(`Circle '${circleId}' added to Mapbox map '${containerId}' with radius ${radius}m`);
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
            const circleInfo = containerCircles.get(circleId);
            if (circleInfo) {
                // Remove layers and source
                if (map.getLayer(circleInfo.layerId)) {
                    map.removeLayer(circleInfo.layerId);
                }
                if (map.getLayer(circleInfo.strokeLayerId)) {
                    map.removeLayer(circleInfo.strokeLayerId);
                }
                if (map.getSource(circleInfo.sourceId)) {
                    map.removeSource(circleInfo.sourceId);
                }

                containerCircles.delete(circleId);
                console.log(`Circle '${circleId}' removed from Mapbox map '${containerId}'`);
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

        return new Promise((resolve) => {
            map.flyTo({
                center: [lng, lat],
                zoom: zoom,
                duration: 1000,
                essential: true
            });
            map.once('moveend', resolve);
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
     * Toggle fullscreen mode (handled by Mapbox control)
     */
    async toggleFullscreen(containerId) {
        // Mapbox GL JS handles fullscreen through its control
        // We can also manually toggle if needed
        const container = document.getElementById(containerId);
        if (!container) {
            return false;
        }

        try {
            if (!document.fullscreenElement) {
                await container.requestFullscreen();
            } else {
                await document.exitFullscreen();
            }
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
            // Clear all markers
            const containerMarkers = this.markers.get(containerId);
            if (containerMarkers) {
                for (const [markerId, marker] of containerMarkers) {
                    marker.remove();
                }
                this.markers.delete(containerId);
            }

            // Clear all popups
            const containerPopups = this.popups.get(containerId);
            if (containerPopups) {
                for (const [popupId, popup] of containerPopups) {
                    popup.remove();
                }
                this.popups.delete(containerId);
            }

            // Clear all circles (layers)
            const map = this.maps.get(containerId);
            const containerCircles = this.circles.get(containerId);
            if (map && containerCircles) {
                for (const [circleId, circleInfo] of containerCircles) {
                    if (map.getLayer(circleInfo.layerId)) {
                        map.removeLayer(circleInfo.layerId);
                    }
                    if (map.getLayer(circleInfo.strokeLayerId)) {
                        map.removeLayer(circleInfo.strokeLayerId);
                    }
                    if (map.getSource(circleInfo.sourceId)) {
                        map.removeSource(circleInfo.sourceId);
                    }
                }
                this.circles.delete(containerId);
            }

            // Remove the map
            if (map) {
                map.remove();
                this.maps.delete(containerId);
            }

            console.log(`Mapbox map disposed for container: ${containerId}`);
            return true;
        } catch (error) {
            console.error(`Failed to dispose map for container '${containerId}':`, error);
            return false;
        }
    }

    /**
     * Create marker element based on type
     */
    _createMarkerElement(markerType) {
        const element = document.createElement('div');
        element.className = `mapbox-marker mapbox-marker-${markerType}`;

        switch (markerType) {
            case 'current-location':
                element.innerHTML = '<div class="location-pulse"></div><div class="location-dot"></div>';
                element.style.cssText = `
                    width: 20px; height: 20px; position: relative;
                    background: transparent; border: none;
                `;
                break;
            case 'tool':
                element.innerHTML = '<i class="material-icons">build</i>';
                element.style.cssText = `
                    width: 30px; height: 30px; background: #2196F3; color: white;
                    border-radius: 50% 50% 50% 0; transform: rotate(-45deg);
                    display: flex; align-items: center; justify-content: center;
                    box-shadow: 0 2px 8px rgba(0,0,0,0.3); border: 2px solid white;
                `;
                break;
            case 'bundle':
                element.innerHTML = '<i class="material-icons">inventory</i>';
                element.style.cssText = `
                    width: 30px; height: 30px; background: #FF9800; color: white;
                    border-radius: 50% 50% 50% 0; transform: rotate(-45deg);
                    display: flex; align-items: center; justify-content: center;
                    box-shadow: 0 2px 8px rgba(0,0,0,0.3); border: 2px solid white;
                `;
                break;
            case 'search':
                element.innerHTML = '<i class="material-icons">search</i>';
                element.style.cssText = `
                    width: 25px; height: 25px; background: #9C27B0; color: white;
                    border-radius: 50% 50% 50% 0; transform: rotate(-45deg);
                    display: flex; align-items: center; justify-content: center;
                    box-shadow: 0 2px 8px rgba(0,0,0,0.3); border: 2px solid white;
                `;
                break;
            default:
                element.style.cssText = `
                    width: 20px; height: 20px; background: #594ae2;
                    border-radius: 50% 50% 50% 0; transform: rotate(-45deg);
                    border: 2px solid white; box-shadow: 0 2px 4px rgba(0,0,0,0.3);
                `;
        }

        // Rotate icon back for non-default markers
        if (markerType !== 'default' && markerType !== 'current-location') {
            const icon = element.querySelector('i');
            if (icon) {
                icon.style.transform = 'rotate(45deg)';
            }
        }

        return element;
    }

    /**
     * Create a GeoJSON circle feature
     */
    _createCircleGeoJSON(center, radiusMeters) {
        const points = 64;
        const coordinates = [];
        const distancePerPoint = 360 / points;

        for (let i = 0; i <= points; i++) {
            const angle = i * distancePerPoint;
            const point = this._getPointAtDistance(center, radiusMeters, angle);
            coordinates.push(point);
        }

        return {
            type: 'Feature',
            geometry: {
                type: 'Polygon',
                coordinates: [coordinates]
            }
        };
    }

    /**
     * Calculate point at distance and bearing from center
     */
    _getPointAtDistance(center, distance, bearing) {
        const R = 6371000; // Earth's radius in meters
        const lat1 = center[1] * Math.PI / 180;
        const lng1 = center[0] * Math.PI / 180;
        const bearingRad = bearing * Math.PI / 180;

        const lat2 = Math.asin(
            Math.sin(lat1) * Math.cos(distance / R) +
            Math.cos(lat1) * Math.sin(distance / R) * Math.cos(bearingRad)
        );

        const lng2 = lng1 + Math.atan2(
            Math.sin(bearingRad) * Math.sin(distance / R) * Math.cos(lat1),
            Math.cos(distance / R) - Math.sin(lat1) * Math.sin(lat2)
        );

        return [lng2 * 180 / Math.PI, lat2 * 180 / Math.PI];
    }

    /**
     * Add event listeners to the map
     */
    _addMapEventListeners(containerId, map) {
        map.on('click', (e) => {
            this.emit('mapClick', {
                containerId,
                lat: e.lngLat.lat,
                lng: e.lngLat.lng,
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

        map.on('load', (e) => {
            console.log(`Mapbox map ready: ${containerId}`);
            this.emit('mapReady', { containerId, originalEvent: e });
        });
    }
}

// CSS for current location marker (injected dynamically)
if (!document.getElementById('mapbox-marker-styles')) {
    const style = document.createElement('style');
    style.id = 'mapbox-marker-styles';
    style.textContent = `
        .mapbox-marker-current-location .location-pulse {
            width: 20px; height: 20px; border: 2px solid #594ae2;
            border-radius: 50%; position: absolute; top: 0; left: 0;
            animation: locationPulse 2s infinite;
            background: rgba(89, 74, 226, 0.1);
        }
        
        .mapbox-marker-current-location .location-dot {
            width: 8px; height: 8px; background: #594ae2;
            border-radius: 50%; position: absolute; top: 6px; left: 6px;
            border: 2px solid white; box-shadow: 0 2px 4px rgba(0,0,0,0.3);
        }
        
        @keyframes locationPulse {
            0% { transform: scale(1); opacity: 1; }
            100% { transform: scale(2); opacity: 0; }
        }
    `;
    document.head.appendChild(style);
}

// Register the Mapbox provider
if (typeof MapProviderFactory !== 'undefined') {
    MapProviderFactory.registerProvider('mapbox', MapboxProvider);
    console.log('MapboxProvider registered successfully');
}

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = MapboxProvider;
}