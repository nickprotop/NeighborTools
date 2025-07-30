// OpenStreetMap Leaflet.js Integration for NeighborTools
// Provides interactive map functionality using Leaflet.js and OpenStreetMap tiles
// Part of the multi-provider map architecture

window.OpenStreetMapProvider = (function() {
    'use strict';

    // Map instances and state management
    const maps = new Map();
    const mapMarkers = new Map();
    const mapCircles = new Map();
    const highlightMarkers = new Map();

    // Default configuration
    const DEFAULT_CONFIG = {
        defaultZoom: 13,
        minZoom: 5,
        maxZoom: 18,
        mapTileUrl: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
        mapAttribution: '© OpenStreetMap contributors',
        showLocationControls: true,
        enableGeolocation: true,
        locationTimeout: 10000,
        maxLocationAge: 300000
    };

    // Privacy level configurations
    const PRIVACY_LEVELS = {
        1: { name: 'Neighborhood', radius: 500, color: '#22c55e', opacity: 0.2 },    // ~0.3 miles
        2: { name: 'ZipCode', radius: 1500, color: '#f59e0b', opacity: 0.15 },       // ~1 mile  
        3: { name: 'District', radius: 5000, color: '#f97316', opacity: 0.1 },       // ~3 miles
        4: { name: 'Exact', radius: 100, color: '#ef4444', opacity: 0.25 }           // ~300 feet
    };

    // Custom marker icons
    const MARKER_ICONS = {
        default: L.divIcon({
            className: 'custom-map-marker',
            html: '<div class="marker-pin"><div class="marker-icon">📍</div></div>',
            iconSize: [30, 42],
            iconAnchor: [15, 42],
            popupAnchor: [0, -42]
        }),
        tool: L.divIcon({
            className: 'custom-map-marker tool-marker',
            html: '<div class="marker-pin"><div class="marker-icon">🛠️</div></div>',
            iconSize: [30, 42],
            iconAnchor: [15, 42],
            popupAnchor: [0, -42]
        }),
        bundle: L.divIcon({
            className: 'custom-map-marker bundle-marker',
            html: '<div class="marker-pin"><div class="marker-icon">📦</div></div>',
            iconSize: [30, 42],
            iconAnchor: [15, 42],
            popupAnchor: [0, -42]
        }),
        user: L.divIcon({
            className: 'custom-map-marker user-marker',
            html: '<div class="marker-pin"><div class="marker-icon">👤</div></div>',
            iconSize: [30, 42],
            iconAnchor: [15, 42],
            popupAnchor: [0, -42]
        }),
        highlight: L.divIcon({
            className: 'custom-map-marker highlight-marker',
            html: '<div class="marker-pin highlight"><div class="marker-icon">⭐</div></div>',
            iconSize: [35, 49],
            iconAnchor: [17, 49],
            popupAnchor: [0, -49]
        })
    };

    /**
     * Initialize a new map instance
     * @param {string} containerId - The HTML element ID to contain the map
     * @param {object} mapSettings - Configuration object from AppSettings
     * @returns {Promise<boolean>} - Success status
     */
    async function initializeMap(containerId, mapSettings) {
        try {
            if (!containerId) {
                console.error('OpenStreetMap: Container ID is required');
                return false;
            }

            const container = document.getElementById(containerId);
            if (!container) {
                console.error(`OpenStreetMap: Container element '${containerId}' not found`);
                return false;
            }

            // Dispose existing map if present
            if (maps.has(containerId)) {
                await disposeMap(containerId);
            }

            // Merge configuration with defaults
            const config = { ...DEFAULT_CONFIG, ...mapSettings };

            // Initialize Leaflet map
            const map = L.map(containerId, {
                center: [config.defaultCenter?.lat || 40.7128, config.defaultCenter?.lng || -74.0060],
                zoom: config.defaultZoom || 13,
                minZoom: config.minZoom || 5,
                maxZoom: config.maxZoom || 18,
                zoomControl: true, // Keep default zoom controls
                attributionControl: true
            });

            // Add OpenStreetMap tile layer
            const tileLayer = L.tileLayer(config.mapTileUrl || DEFAULT_CONFIG.mapTileUrl, {
                attribution: config.mapAttribution || DEFAULT_CONFIG.mapAttribution,
                maxZoom: config.maxZoom || 18,
                subdomains: ['a', 'b', 'c']
            });
            
            tileLayer.addTo(map);

            // Add custom controls if enabled
            if (config.showLocationControls !== false) {
                addCustomControls(map, containerId, config);
            }

            // Store map instance and initialize collections
            maps.set(containerId, { map, config });
            mapMarkers.set(containerId, new Map());
            mapCircles.set(containerId, new Map());
            highlightMarkers.set(containerId, new Map());

            // Add CSS styles for custom markers
            addMarkerStyles();

            // Add map click handler for location selection
            map.on('click', function(e) {
                onMapClick(containerId, e.latlng.lat, e.latlng.lng);
            });

            console.log(`OpenStreetMap: Initialized map for container '${containerId}'`);
            return true;

        } catch (error) {
            console.error(`OpenStreetMap: Failed to initialize map for '${containerId}':`, error);
            return false;
        }
    }

    /**
     * Update map location with marker and optional privacy circle
     * @param {string} containerId - Map container ID
     * @param {number} lat - Latitude
     * @param {number} lng - Longitude
     * @param {number} zoom - Zoom level (optional)
     * @param {object} options - Additional options
     */
    function updateMapLocation(containerId, lat, lng, zoom, options = {}) {
        try {
            const mapData = maps.get(containerId);
            if (!mapData) {
                console.error(`OpenStreetMap: Map '${containerId}' not found`);
                return false;
            }

            const { map } = mapData;
            const { 
                markerId = 'main',
                markerType = 'default',
                showPopup = false,
                popupContent = '',
                privacyLevel = null,
                panTo = true
            } = options;

            // Validate coordinates
            if (!isValidCoordinates(lat, lng)) {
                console.error('OpenStreetMap: Invalid coordinates', { lat, lng });
                return false;
            }

            // Update map view
            if (panTo) {
                const targetZoom = zoom || map.getZoom();
                map.setView([lat, lng], targetZoom);
            }

            // Add or update marker
            addMarker(containerId, lat, lng, {
                markerId,
                markerType,
                showPopup,
                popupContent
            });

            // Add privacy circle if specified
            if (privacyLevel && PRIVACY_LEVELS[privacyLevel]) {
                addPrivacyCircle(containerId, lat, lng, privacyLevel);
            }

            return true;

        } catch (error) {
            console.error(`OpenStreetMap: Failed to update location for '${containerId}':`, error);
            return false;
        }
    }

    /**
     * Add a marker to the map
     * @param {string} containerId - Map container ID
     * @param {number} lat - Latitude
     * @param {number} lng - Longitude
     * @param {object} options - Marker options
     */
    function addMarker(containerId, lat, lng, options = {}) {
        try {
            const mapData = maps.get(containerId);
            if (!mapData) return false;

            const { map } = mapData;
            const { 
                markerId = 'marker_' + Date.now(),
                markerType = 'default',
                showPopup = false,
                popupContent = '',
                draggable = false,
                onClick = null
            } = options;

            // Validate coordinates
            if (!isValidCoordinates(lat, lng)) return false;

            // Remove existing marker with same ID
            const markers = mapMarkers.get(containerId);
            if (markers.has(markerId)) {
                map.removeLayer(markers.get(markerId));
            }

            // Create marker with custom icon
            const icon = MARKER_ICONS[markerType] || MARKER_ICONS.default;
            const marker = L.marker([lat, lng], {
                icon: icon,
                draggable: draggable
            });

            // Add popup if specified
            if (showPopup && popupContent) {
                marker.bindPopup(popupContent);
            }

            // Add click handler if specified
            if (onClick && typeof onClick === 'function') {
                marker.on('click', (e) => onClick(e.latlng.lat, e.latlng.lng, markerId));
            }

            // Add to map and store reference
            marker.addTo(map);
            markers.set(markerId, marker);

            return markerId;

        } catch (error) {
            console.error('OpenStreetMap: Failed to add marker:', error);
            return null;
        }
    }

    /**
     * Add a privacy circle to visualize location precision
     * @param {string} containerId - Map container ID
     * @param {number} lat - Center latitude
     * @param {number} lng - Center longitude
     * @param {number} privacyLevel - Privacy level (1-4)
     */
    function addPrivacyCircle(containerId, lat, lng, privacyLevel) {
        try {
            const mapData = maps.get(containerId);
            if (!mapData) return false;

            const { map } = mapData;
            const privacyConfig = PRIVACY_LEVELS[privacyLevel];
            
            if (!privacyConfig || !isValidCoordinates(lat, lng)) return false;

            // Remove existing privacy circle
            const circles = mapCircles.get(containerId);
            if (circles.has('privacy')) {
                map.removeLayer(circles.get('privacy'));
            }

            // Create privacy circle
            const circle = L.circle([lat, lng], {
                radius: privacyConfig.radius,
                fillColor: privacyConfig.color,
                fillOpacity: privacyConfig.opacity,
                color: privacyConfig.color,
                weight: 2,
                opacity: 0.7
            });

            // Add tooltip with privacy information
            circle.bindTooltip(
                `Privacy Level: ${privacyConfig.name}<br>` +
                `Area: ~${(privacyConfig.radius / 1000).toFixed(1)}km radius`,
                { permanent: false, direction: 'top' }
            );

            // Add to map and store reference
            circle.addTo(map);
            circles.set('privacy', circle);

            return true;

        } catch (error) {
            console.error('OpenStreetMap: Failed to add privacy circle:', error);
            return false;
        }
    }

    /**
     * Highlight a location temporarily
     * @param {string} containerId - Map container ID
     * @param {number} lat - Latitude
     * @param {number} lng - Longitude
     * @param {number} duration - Highlight duration in milliseconds
     */
    function highlightLocation(containerId, lat, lng, duration = 3000) {
        try {
            const mapData = maps.get(containerId);
            if (!mapData || !isValidCoordinates(lat, lng)) return false;

            const { map } = mapData;
            const highlightId = 'highlight_' + Date.now();

            // Create highlight marker
            const highlightMarker = L.marker([lat, lng], {
                icon: MARKER_ICONS.highlight
            });

            // Add animation class
            highlightMarker.on('add', () => {
                const element = highlightMarker.getElement();
                if (element) {
                    element.classList.add('marker-bounce');
                }
            });

            // Add to map
            highlightMarker.addTo(map);
            highlightMarkers.get(containerId).set(highlightId, highlightMarker);

            // Auto-remove after duration
            setTimeout(() => {
                if (highlightMarkers.get(containerId).has(highlightId)) {
                    map.removeLayer(highlightMarker);
                    highlightMarkers.get(containerId).delete(highlightId);
                }
            }, duration);

            return true;

        } catch (error) {
            console.error('OpenStreetMap: Failed to highlight location:', error);
            return false;
        }
    }

    /**
     * Clear all highlights from the map
     * @param {string} containerId - Map container ID
     */
    function clearHighlights(containerId) {
        try {
            const mapData = maps.get(containerId);
            if (!mapData) return false;

            const { map } = mapData;
            const highlights = highlightMarkers.get(containerId);

            highlights.forEach((marker, id) => {
                map.removeLayer(marker);
            });
            highlights.clear();

            return true;

        } catch (error) {
            console.error('OpenStreetMap: Failed to clear highlights:', error);
            return false;
        }
    }

    /**
     * Enable geolocation with callback
     * @param {string} containerId - Map container ID
     * @param {function} callback - Callback function for location result
     * @param {object} options - Geolocation options
     */
    function enableGeolocation(containerId, callback, options = {}) {
        try {
            const mapData = maps.get(containerId);
            if (!mapData) return false;

            const { config } = mapData;
            
            if (!config.enableGeolocation || !navigator.geolocation) {
                callback({ success: false, error: 'Geolocation not supported or disabled' });
                return false;
            }

            const geoOptions = {
                enableHighAccuracy: true,
                timeout: config.locationTimeout || 10000,
                maximumAge: config.maxLocationAge || 300000,
                ...options
            };

            navigator.geolocation.getCurrentPosition(
                (position) => {
                    const lat = position.coords.latitude;
                    const lng = position.coords.longitude;
                    const accuracy = position.coords.accuracy;

                    callback({
                        success: true,
                        latitude: lat,
                        longitude: lng,
                        accuracy: accuracy,
                        timestamp: position.timestamp
                    });
                },
                (error) => {
                    let errorMessage = 'Unknown geolocation error';
                    switch (error.code) {
                        case error.PERMISSION_DENIED:
                            errorMessage = 'Location access denied by user';
                            break;
                        case error.POSITION_UNAVAILABLE:
                            errorMessage = 'Location information unavailable';
                            break;
                        case error.TIMEOUT:
                            errorMessage = 'Location request timed out';
                            break;
                    }

                    callback({
                        success: false,
                        error: errorMessage,
                        code: error.code
                    });
                },
                geoOptions
            );

            return true;

        } catch (error) {
            console.error('OpenStreetMap: Geolocation failed:', error);
            callback({ success: false, error: 'Geolocation failed: ' + error.message });
            return false;
        }
    }

    /**
     * Toggle fullscreen mode for the map
     * @param {string} containerId - Map container ID
     */
    function toggleFullscreen(containerId) {
        try {
            const container = document.getElementById(containerId);
            if (!container) return false;

            if (!document.fullscreenElement) {
                container.requestFullscreen().then(() => {
                    // Invalidate map size after fullscreen
                    setTimeout(() => {
                        const mapData = maps.get(containerId);
                        if (mapData) {
                            mapData.map.invalidateSize();
                        }
                    }, 100);
                });
            } else {
                document.exitFullscreen().then(() => {
                    // Invalidate map size after exit fullscreen
                    setTimeout(() => {
                        const mapData = maps.get(containerId);
                        if (mapData) {
                            mapData.map.invalidateSize();
                        }
                    }, 100);
                });
            }

            return true;

        } catch (error) {
            console.error('OpenStreetMap: Failed to toggle fullscreen:', error);
            return false;
        }
    }

    /**
     * Dispose of a map instance and clean up resources
     * @param {string} containerId - Map container ID
     */
    async function disposeMap(containerId) {
        try {
            const mapData = maps.get(containerId);
            if (!mapData) return true;

            const { map } = mapData;

            // Clear all markers
            const markers = mapMarkers.get(containerId);
            if (markers) {
                markers.forEach(marker => map.removeLayer(marker));
                markers.clear();
            }

            // Clear all circles
            const circles = mapCircles.get(containerId);
            if (circles) {
                circles.forEach(circle => map.removeLayer(circle));
                circles.clear();
            }

            // Clear all highlights
            const highlights = highlightMarkers.get(containerId);
            if (highlights) {
                highlights.forEach(marker => map.removeLayer(marker));
                highlights.clear();
            }

            // Remove map instance
            map.remove();

            // Clean up references
            maps.delete(containerId);
            mapMarkers.delete(containerId);
            mapCircles.delete(containerId);
            highlightMarkers.delete(containerId);

            console.log(`OpenStreetMap: Disposed map '${containerId}'`);
            return true;

        } catch (error) {
            console.error(`OpenStreetMap: Failed to dispose map '${containerId}':`, error);
            return false;
        }
    }

    /**
     * Add custom controls to the map
     * @param {L.Map} map - Leaflet map instance
     * @param {string} containerId - Map container ID
     * @param {object} config - Map configuration
     */
    function addCustomControls(map, containerId, config) {
        // Geolocation control
        if (config.enableGeolocation && navigator.geolocation) {
            const LocationControl = L.Control.extend({
                onAdd: function(map) {
                    const container = L.DomUtil.create('div', 'leaflet-bar leaflet-control leaflet-control-custom');
                    container.style.backgroundColor = 'white';
                    container.style.width = '30px';
                    container.style.height = '30px';
                    container.style.cursor = 'pointer';
                    container.innerHTML = '📍';
                    container.title = 'Get my location';

                    container.onclick = function() {
                        enableGeolocation(containerId, (result) => {
                            if (result.success) {
                                updateMapLocation(containerId, result.latitude, result.longitude, 15, {
                                    markerType: 'user',
                                    showPopup: true,
                                    popupContent: 'Your location'
                                });
                            }
                        });
                    };

                    return container;
                }
            });

            new LocationControl({ position: 'topleft' }).addTo(map);
        }

        // Fullscreen control
        const FullscreenControl = L.Control.extend({
            onAdd: function(map) {
                const container = L.DomUtil.create('div', 'leaflet-bar leaflet-control leaflet-control-custom');
                container.style.backgroundColor = 'white';
                container.style.width = '30px';
                container.style.height = '30px';
                container.style.cursor = 'pointer';
                container.innerHTML = '⛶';
                container.title = 'Toggle fullscreen';

                container.onclick = function() {
                    toggleFullscreen(containerId);
                };

                return container;
            }
        });

        new FullscreenControl({ position: 'topleft' }).addTo(map);
    }

    /**
     * Add CSS styles for custom markers
     */
    function addMarkerStyles() {
        if (document.getElementById('openstreetmap-marker-styles')) return;

        const style = document.createElement('style');
        style.id = 'openstreetmap-marker-styles';
        style.textContent = `
            .custom-map-marker {
                background: none;
                border: none;
            }
            
            .marker-pin {
                width: 30px;
                height: 42px;
                border-radius: 50% 50% 50% 0;
                background: #594ae2;
                position: relative;
                transform: rotate(-45deg);
                box-shadow: 0 2px 8px rgba(0,0,0,0.3);
                transition: all 0.2s ease;
            }
            
            .marker-pin.highlight {
                background: #f59e0b;
                transform: rotate(-45deg) scale(1.2);
            }
            
            .marker-icon {
                position: absolute;
                top: 50%;
                left: 50%;
                transform: translate(-50%, -50%) rotate(45deg);
                font-size: 14px;
                color: white;
            }
            
            .tool-marker .marker-pin {
                background: #22c55e;
            }
            
            .bundle-marker .marker-pin {
                background: #f97316;
            }
            
            .user-marker .marker-pin {
                background: #3b82f6;
            }
            
            .custom-map-marker:hover .marker-pin {
                transform: rotate(-45deg) scale(1.1);
            }
            
            @keyframes marker-bounce {
                0%, 100% { transform: rotate(-45deg) scale(1.2); }
                50% { transform: rotate(-45deg) scale(1.4); }
            }
            
            .marker-bounce .marker-pin {
                animation: marker-bounce 1s infinite;
            }
        `;
        
        document.head.appendChild(style);
    }

    /**
     * Validate coordinate values
     * @param {number} lat - Latitude
     * @param {number} lng - Longitude
     * @returns {boolean} - True if valid
     */
    function isValidCoordinates(lat, lng) {
        return typeof lat === 'number' && typeof lng === 'number' &&
               lat >= -90 && lat <= 90 &&
               lng >= -180 && lng <= 180 &&
               !isNaN(lat) && !isNaN(lng);
    }

    /**
     * Handle map click event - reverse geocode and notify C# component
     * @param {string} containerId - Map container ID
     * @param {number} lat - Clicked latitude
     * @param {number} lng - Clicked longitude
     */
    function onMapClick(containerId, lat, lng) {
        try {
            console.log(`OpenStreetMap: Map clicked at ${lat}, ${lng}`);
            
            // Remove any existing temporary markers
            const markers = mapMarkers.get(containerId);
            if (markers) {
                const tempMarkers = Array.from(markers.keys()).filter(id => id.startsWith('temp-click-'));
                tempMarkers.forEach(markerId => {
                    const marker = markers.get(markerId);
                    if (marker) {
                        marker.remove();
                        markers.delete(markerId);
                    }
                });
            }
            
            // Add new temporary marker at clicked location
            const tempMarkerId = `temp-click-${Date.now()}`;
            addMarker(containerId, lat, lng, {
                markerId: tempMarkerId,
                markerType: 'default',
                showPopup: true,
                popupContent: 'Loading location...'
            });

            // Create location data immediately with coordinates
            const locationData = {
                lat: lat,
                lng: lng,
                displayName: null, // Will be set by C# after API call
                source: 'MapClick',
                containerId: containerId // Add container ID for proper routing
            };
            
            console.log('OpenStreetMap: Location selected from map click with coordinates:', locationData);
            
            // Store the selected location data for the component to retrieve (legacy support)
            window[`selectedMapLocation_${containerId}`] = locationData;
            
            // Trigger custom event that C# component can listen to
            const event = new CustomEvent('mapLocationSelected', { 
                detail: locationData 
            });
            document.dispatchEvent(event);
            
            console.log('OpenStreetMap: Event dispatched - C# will handle reverse geocoding via backend API');

        } catch (error) {
            console.error(`OpenStreetMap: Map click handler failed:`, error);
        }
    }

    /**
     * Simple reverse geocoding using OpenStreetMap Nominatim
     * @param {number} lat - Latitude
     * @param {number} lng - Longitude
     * @returns {Promise<string>} Location name
     */
    async function reverseGeocodeLocation(lat, lng) {
        try {
            console.log(`OpenStreetMap: Starting reverse geocoding for ${lat}, ${lng}`);
            
            const url = `https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&zoom=18&addressdetails=1`;
            console.log(`OpenStreetMap: Nominatim URL: ${url}`);
            
            const response = await fetch(url, {
                headers: {
                    'User-Agent': 'NeighborTools/1.0'
                }
            });
            
            console.log(`OpenStreetMap: Nominatim response status: ${response.status}`);
            
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            
            const responseText = await response.text();
            console.log('OpenStreetMap: Nominatim raw response:', responseText);
            
            if (!responseText) {
                console.log('OpenStreetMap: Empty response from Nominatim');
                return null;
            }
            
            const data = JSON.parse(responseText);
            console.log('OpenStreetMap: Nominatim parsed data:', JSON.stringify(data, null, 2));
            
            if (data && data.display_name) {
                console.log(`OpenStreetMap: Found location: ${data.display_name}`);
                return data.display_name;
            }
            
            console.log('OpenStreetMap: No display_name in response data:', data);
            return null;
        } catch (error) {
            console.error('OpenStreetMap: Reverse geocoding error:', error);
            return null;
        }
    }

    /**
     * Update marker popup content
     * @param {string} containerId - Map container ID
     * @param {string} markerId - Marker ID
     * @param {string} content - New popup content
     */
    function updateMarkerPopup(containerId, markerId, content) {
        try {
            const markers = mapMarkers.get(containerId);
            if (markers && markers.has(markerId)) {
                const marker = markers.get(markerId);
                marker.setPopupContent(content);
            }
        } catch (error) {
            console.error('Failed to update marker popup:', error);
        }
    }

    // Public API
    return {
        initializeMap,
        updateMapLocation,
        addMarker,
        addPrivacyCircle,
        highlightLocation,
        clearHighlights,
        enableGeolocation,
        toggleFullscreen,
        disposeMap,
        onMapClick,
        reverseGeocodeLocation,
        updateMarkerPopup,
        
        // Utility functions
        isValidCoordinates,
        
        // Constants
        PRIVACY_LEVELS,
        MARKER_ICONS
    };
})();

// Global convenience functions for Blazor interop
window.initializeMap = window.OpenStreetMapProvider.initializeMap;
window.updateMapLocation = window.OpenStreetMapProvider.updateMapLocation;
window.addMarker = window.OpenStreetMapProvider.addMarker;
window.addPrivacyCircle = window.OpenStreetMapProvider.addPrivacyCircle;
window.highlightLocation = window.OpenStreetMapProvider.highlightLocation;
window.clearHighlights = window.OpenStreetMapProvider.clearHighlights;
window.enableGeolocation = window.OpenStreetMapProvider.enableGeolocation;
window.toggleFullscreen = window.OpenStreetMapProvider.toggleFullscreen;
window.disposeMap = window.OpenStreetMapProvider.disposeMap;
window.onMapClick = window.OpenStreetMapProvider.onMapClick;
window.reverseGeocodeLocation = window.OpenStreetMapProvider.reverseGeocodeLocation;