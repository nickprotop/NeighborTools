/**
 * Google Maps Provider Implementation
 * 
 * Implements the IMapProvider interface using Google Maps JavaScript API.
 * This is a template implementation showing how to integrate Google Maps
 * with the unified map provider interface.
 * 
 * Prerequisites:
 * - Google Maps JavaScript API key
 * - Google Maps JavaScript API loaded in the page
 */

/**
 * Google Maps provider implementation
 */
class GoogleMapsProvider extends IMapProvider {
    constructor(config) {
        super(config);
        this.markers = new Map(); // Container ID -> Map of markers
        this.circles = new Map();  // Container ID -> Map of circles
        this.infoWindows = new Map(); // Container ID -> Map of info windows
        this.apiKey = config.apiKey || config.googleMapsApiKey || '';
        
        if (!this.apiKey) {
            console.warn('Google Maps API key not provided. Some features may not work.');
        }
    }

    /**
     * Initialize the provider (load Google Maps API if needed)
     */
    async initialize() {
        if (this.isInitialized) {
            return true;
        }

        // Check if Google Maps is available
        if (typeof google !== 'undefined' && google.maps) {
            this.isInitialized = true;
            console.log('GoogleMapsProvider initialized successfully');
            return true;
        }

        // Load Google Maps API if not available
        if (this.apiKey) {
            try {
                await this._loadGoogleMapsApi();
                this.isInitialized = true;
                console.log('GoogleMapsProvider initialized successfully');
                return true;
            } catch (error) {
                console.error('Failed to load Google Maps API:', error);
                return false;
            }
        } else {
            console.error('Google Maps API is not loaded and no API key provided');
            return false;
        }
    }

    /**
     * Load Google Maps API dynamically
     */
    async _loadGoogleMapsApi() {
        return new Promise((resolve, reject) => {
            if (typeof google !== 'undefined' && google.maps) {
                resolve();
                return;
            }

            const script = document.createElement('script');
            script.src = `https://maps.googleapis.com/maps/api/js?key=${this.apiKey}&libraries=geometry`;
            script.async = true;
            script.defer = true;
            
            script.onload = () => resolve();
            script.onerror = () => reject(new Error('Failed to load Google Maps API'));
            
            document.head.appendChild(script);
        });
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
                center: new google.maps.LatLng(
                    options.center?.lat || options.center?.Lat || 39.8283,
                    options.center?.lng || options.center?.Lng || -98.5795
                ),
                zoom: options.zoom || 4,
                minZoom: options.minZoom || 1,
                maxZoom: options.maxZoom || 18,
                zoomControl: options.showLocationControls !== false,
                mapTypeControl: false,
                streetViewControl: false,
                fullscreenControl: options.enableFullscreen !== false,
                gestureHandling: 'cooperative',
                styles: this._getMapStyles(options.mapStyle || 'default')
            };

            // Create the map
            const map = new google.maps.Map(container, mapOptions);

            // Initialize storage for this container
            this.markers.set(containerId, new Map());
            this.circles.set(containerId, new Map());
            this.infoWindows.set(containerId, new Map());

            // Store the map instance
            this.maps.set(containerId, map);

            // Add event listeners
            this._addMapEventListeners(containerId, map);

            console.log(`Google Maps created successfully in container: ${containerId}`);
            return true;

        } catch (error) {
            console.error(`Failed to create Google Maps in container '${containerId}':`, error);
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
            const position = new google.maps.LatLng(lat, lng);

            // Update map view if requested
            if (options.panTo !== false) {
                map.setCenter(position);
                if (zoom) {
                    map.setZoom(zoom);
                }
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
        const containerInfoWindows = this.infoWindows.get(containerId);
        
        if (!map || !containerMarkers || !containerInfoWindows) {
            throw new Error(`Map not found for container: ${containerId}`);
        }

        try {
            const markerId = options.markerId || `marker_${Date.now()}_${Math.random()}`;

            // Remove existing marker if replacing
            if (options.replace && containerMarkers.has(markerId)) {
                await this.removeMarker(containerId, markerId);
            }

            const position = new google.maps.LatLng(lat, lng);

            // Create marker options
            const markerOptions = {
                position: position,
                map: map,
                title: options.title || '',
                icon: this._getMarkerIcon(options.markerType || 'default'),
                animation: options.animated ? google.maps.Animation.DROP : null
            };

            // Create the marker
            const marker = new google.maps.Marker(markerOptions);

            // Add info window if requested
            if (options.showPopup && options.popupContent) {
                const infoWindow = new google.maps.InfoWindow({
                    content: options.popupContent
                });

                marker.addListener('click', () => {
                    // Close other info windows first
                    for (const [, window] of containerInfoWindows) {
                        window.close();
                    }
                    infoWindow.open(map, marker);
                });

                containerInfoWindows.set(markerId, infoWindow);

                // Open immediately if requested
                if (options.openPopup) {
                    infoWindow.open(map, marker);
                }
            }

            // Store the marker
            containerMarkers.set(markerId, marker);

            console.log(`Marker '${markerId}' added to Google Maps '${containerId}'`);
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
        const containerInfoWindows = this.infoWindows.get(containerId);
        
        if (!containerMarkers) {
            return false;
        }

        try {
            const marker = containerMarkers.get(markerId);
            if (marker) {
                marker.setMap(null);
                containerMarkers.delete(markerId);

                // Remove associated info window
                const infoWindow = containerInfoWindows?.get(markerId);
                if (infoWindow) {
                    infoWindow.close();
                    containerInfoWindows.delete(markerId);
                }

                console.log(`Marker '${markerId}' removed from Google Maps '${containerId}'`);
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

            const center = new google.maps.LatLng(lat, lng);

            // Create circle options
            const circleOptions = {
                strokeColor: options.strokeColor || '#594ae2',
                strokeOpacity: options.strokeOpacity || 0.3,
                strokeWeight: options.strokeWeight || 2,
                fillColor: options.fillColor || '#594ae2',
                fillOpacity: options.fillOpacity || 0.1,
                map: map,
                center: center,
                radius: radius // radius in meters
            };

            // Create the circle
            const circle = new google.maps.Circle(circleOptions);

            // Store the circle
            containerCircles.set(circleId, circle);

            console.log(`Circle '${circleId}' added to Google Maps '${containerId}' with radius ${radius}m`);
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
        const containerCircles = this.circles.get(containerId);
        
        if (!containerCircles) {
            return false;
        }

        try {
            const circle = containerCircles.get(circleId);
            if (circle) {
                circle.setMap(null);
                containerCircles.delete(circleId);
                console.log(`Circle '${circleId}' removed from Google Maps '${containerId}'`);
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

        const position = new google.maps.LatLng(lat, lng);
        map.setCenter(position);
        map.setZoom(zoom);
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
            lat: center.lat(),
            lng: center.lng()
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
                    google.maps.event.trigger(map, 'resize');
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
            // Clear all markers
            const containerMarkers = this.markers.get(containerId);
            if (containerMarkers) {
                for (const [markerId, marker] of containerMarkers) {
                    marker.setMap(null);
                }
                this.markers.delete(containerId);
            }

            // Clear all circles
            const containerCircles = this.circles.get(containerId);
            if (containerCircles) {
                for (const [circleId, circle] of containerCircles) {
                    circle.setMap(null);
                }
                this.circles.delete(containerId);
            }

            // Clear all info windows
            const containerInfoWindows = this.infoWindows.get(containerId);
            if (containerInfoWindows) {
                for (const [windowId, infoWindow] of containerInfoWindows) {
                    infoWindow.close();
                }
                this.infoWindows.delete(containerId);
            }

            // Remove the map
            this.maps.delete(containerId);

            console.log(`Google Maps disposed for container: ${containerId}`);
            return true;
        } catch (error) {
            console.error(`Failed to dispose map for container '${containerId}':`, error);
            return false;
        }
    }

    /**
     * Get marker icon based on type
     */
    _getMarkerIcon(markerType) {
        const baseUrl = 'https://maps.google.com/mapfiles/ms/icons/';
        
        switch (markerType) {
            case 'current-location':
                return {
                    url: `${baseUrl}blue-dot.png`,
                    scaledSize: new google.maps.Size(32, 32),
                    anchor: new google.maps.Point(16, 16)
                };
            case 'tool':
                return {
                    url: `${baseUrl}blue.png`,
                    scaledSize: new google.maps.Size(32, 32),
                    anchor: new google.maps.Point(16, 32)
                };
            case 'bundle':
                return {
                    url: `${baseUrl}orange.png`,
                    scaledSize: new google.maps.Size(32, 32),
                    anchor: new google.maps.Point(16, 32)
                };
            case 'search':
                return {
                    url: `${baseUrl}purple.png`,
                    scaledSize: new google.maps.Size(32, 32),
                    anchor: new google.maps.Point(16, 32)
                };
            default:
                return null; // Use default Google Maps marker
        }
    }

    /**
     * Get map styles based on style name
     */
    _getMapStyles(styleName) {
        const styles = {
            default: [],
            dark: [
                { elementType: 'geometry', stylers: [{ color: '#242f3e' }] },
                { elementType: 'labels.text.stroke', stylers: [{ color: '#242f3e' }] },
                { elementType: 'labels.text.fill', stylers: [{ color: '#746855' }] }
                // Add more dark mode styles...
            ],
            minimal: [
                { featureType: 'poi', stylers: [{ visibility: 'off' }] },
                { featureType: 'transit', stylers: [{ visibility: 'off' }] }
            ]
        };

        return styles[styleName] || styles.default;
    }

    /**
     * Add event listeners to the map
     */
    _addMapEventListeners(containerId, map) {
        map.addListener('click', (e) => {
            this.emit('mapClick', {
                containerId,
                lat: e.latLng.lat(),
                lng: e.latLng.lng(),
                originalEvent: e
            });
        });

        map.addListener('dragend', () => {
            const center = map.getCenter();
            this.emit('mapMoveEnd', {
                containerId,
                center: { lat: center.lat(), lng: center.lng() },
                zoom: map.getZoom()
            });
        });

        map.addListener('zoom_changed', () => {
            this.emit('mapZoomEnd', {
                containerId,
                zoom: map.getZoom()
            });
        });

        map.addListener('tilesloaded', () => {
            console.log(`Google Maps ready: ${containerId}`);
            this.emit('mapReady', { containerId });
        });
    }
}

// Register the Google Maps provider if the factory is available
if (typeof MapProviderFactory !== 'undefined') {
    MapProviderFactory.registerProvider('googlemaps', GoogleMapsProvider);
    MapProviderFactory.registerProvider('google', GoogleMapsProvider); // Alias
    console.log('GoogleMapsProvider registered successfully');
}

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = GoogleMapsProvider;
}