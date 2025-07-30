# NeighborTools Map Provider Interface

This directory contains map provider implementations for the NeighborTools location system. The provider interface allows for pluggable map implementations while maintaining a consistent API for Blazor components.

## Architecture Overview

### Core Components

1. **`map-provider-interface.js`** - Defines the base interface and factory system
2. **`map-initialization.js`** - Handles system initialization and configuration
3. **Provider Implementations** - Specific implementations for different map services

### Provider Interface (`IMapProvider`)

All map providers must extend the `IMapProvider` abstract class and implement the following methods:

```javascript
class MyMapProvider extends IMapProvider {
    async initialize()                              // Initialize provider dependencies
    async createMap(containerId, options)          // Create map in container
    async updateLocation(containerId, lat, lng, zoom, options) // Update map location
    async addMarker(containerId, lat, lng, options) // Add marker to map
    async removeMarker(containerId, markerId)       // Remove marker from map
    async addCircle(containerId, lat, lng, radius, options) // Add circle (privacy zones)
    async removeCircle(containerId, circleId)       // Remove circle from map
    async setView(containerId, lat, lng, zoom)      // Set map viewport
    async getCenter(containerId)                    // Get map center coordinates
    async getZoom(containerId)                      // Get current zoom level
    async getCurrentLocation()                      // Get user's current location
    async dispose(containerId)                      // Clean up map instance
    async toggleFullscreen(containerId)             // Toggle fullscreen mode
}
```

## Available Providers

### OpenStreetMap Provider (`openstreetmap-provider.js`)
- **Library**: Leaflet.js
- **Features**: Full feature support, custom markers, privacy circles
- **Requirements**: Leaflet.js CSS and JavaScript files
- **Status**: ✅ Complete implementation

### Google Maps Provider (`googlemaps-provider.js`)
- **Library**: Google Maps JavaScript API
- **Features**: Full feature support, custom markers, info windows
- **Requirements**: Google Maps API key
- **Status**: 🚧 Template implementation (requires API key)

### Mapbox Provider (`mapbox-provider.js`)
- **Library**: Mapbox GL JS
- **Features**: Modern vector tiles, 3D capabilities
- **Requirements**: Mapbox API key
- **Status**: 📋 Planned

### Azure Maps Provider (`azure-provider.js`)
- **Library**: Azure Maps Web SDK
- **Features**: Enterprise features, traffic data
- **Requirements**: Azure Maps subscription key
- **Status**: 📋 Planned

### HERE Maps Provider (`here-provider.js`)
- **Library**: HERE Maps JavaScript API
- **Features**: Real-time traffic, routing
- **Requirements**: HERE API key
- **Status**: 📋 Planned

## Configuration

Map providers are configured through the application settings and can be switched at runtime.

### Example Configuration

```json
{
  "MapSettings": {
    "Provider": "OpenStreetMap",
    "ApiKey": "",
    "DefaultCenter": {
      "Lat": 39.8283,
      "Lng": -98.5795
    },
    "DefaultZoom": 4,
    "MinZoom": 1,
    "MaxZoom": 18,
    "MapTileUrl": "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
    "MapAttribution": "© OpenStreetMap contributors",
    "ShowLocationControls": true,
    "EnableGeolocation": true,
    "LocationTimeout": 10000,
    "MaxLocationAge": 300000
  }
}
```

### Runtime Provider Switching

```javascript
// Switch to Google Maps
await window.switchMapProvider('googlemaps', {
    apiKey: 'your-google-maps-api-key'
});

// Switch back to OpenStreetMap
await window.switchMapProvider('openstreetmap');
```

## Implementation Guide

### Creating a New Provider

1. **Create Provider Class**:
```javascript
class MyMapProvider extends IMapProvider {
    constructor(config) {
        super(config);
        // Initialize provider-specific properties
    }
    
    async initialize() {
        // Load required libraries/APIs
        // Return true on success, false on failure
    }
    
    // Implement all required methods...
}
```

2. **Register Provider**:
```javascript
MapProviderFactory.registerProvider('myprovider', MyMapProvider);
```

3. **Add Loading Script** (optional):
```javascript
// In map-initialization.js providers object
myprovider: '/js/providers/myprovider-provider.js'
```

### Provider Implementation Guidelines

#### Consistent API Behavior
- All coordinate parameters should accept `lat, lng` in decimal degrees
- All methods should return consistent data structures
- Error handling should be consistent across providers

#### Marker Management
- Marker IDs should be unique per container
- Support marker types: `default`, `current-location`, `tool`, `bundle`, `search`
- Handle marker replacement when same ID is used

#### Privacy Circles
- Circle radius is always in meters
- Support configurable colors and opacity
- Use privacy-friendly styling (subtle, not prominent)

#### Event Handling
- Emit standard events: `mapClick`, `mapMoveEnd`, `mapZoomEnd`, `mapReady`
- Include container ID in all events
- Provide original event object when possible

#### Resource Management
- Clean up all resources in `dispose()` method
- Handle multiple maps per provider instance
- Prevent memory leaks from event listeners

## Usage in Blazor Components

### Basic Map Initialization

```razor
@inject IJSRuntime JSRuntime

<div id="@mapContainerId" style="height: 400px;"></div>

@code {
    private string mapContainerId = $"map-{Guid.NewGuid():N}";
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var success = await JSRuntime.InvokeAsync<bool>("initializeMap", 
                mapContainerId, 
                new { 
                    center = new { lat = 40.7128, lng = -74.0060 },
                    zoom = 10 
                });
        }
    }
}
```

### Advanced Features

```razor
@code {
    private async Task UpdateLocation()
    {
        await JSRuntime.InvokeVoidAsync("updateMapLocation", 
            mapContainerId, 
            40.7589, -73.9851, 12,
            new {
                markerId = "location-marker",
                markerType = "current-location",
                showPopup = true,
                popupContent = "You are here!",
                privacyLevel = 1
            });
    }
    
    private async Task AddPrivacyCircle()
    {
        await JSRuntime.InvokeVoidAsync("addPrivacyCircle", 
            mapContainerId, 
            40.7589, -73.9851, 
            1); // Privacy level 1 = Neighborhood
    }
}
```

## Debugging

### Debug Information

```javascript
// Get current system status
console.log(window.getMapDebugInfo());

// Get provider information
console.log(window.getMapProviderInfo());

// Full debug output
window.debugMapSystem();
```

### Common Issues

1. **Provider Not Loading**: Check browser console for script loading errors
2. **API Keys**: Ensure API keys are correctly configured for commercial providers
3. **CORS Issues**: Some tile servers may have CORS restrictions
4. **Memory Leaks**: Always call `disposeMap()` when components are destroyed

## Testing

### Provider Compliance Testing

```javascript
// Test all required methods exist
const provider = new MyMapProvider({});
const requiredMethods = [
    'initialize', 'createMap', 'updateLocation', 'addMarker', 
    'removeMarker', 'addCircle', 'removeCircle', 'setView',
    'getCenter', 'getZoom', 'getCurrentLocation', 'dispose',
    'toggleFullscreen'
];

requiredMethods.forEach(method => {
    if (typeof provider[method] !== 'function') {
        console.error(`Missing required method: ${method}`);
    }
});
```

### Integration Testing

Test providers against the standard test suite to ensure consistent behavior across all implementations.

## Contributing

When adding new providers:

1. Follow the established patterns in existing providers
2. Include comprehensive error handling
3. Add appropriate logging for debugging
4. Document any provider-specific configuration options
5. Test with the standard test suite
6. Update this README with provider information

## License

This provider interface is part of the NeighborTools project and follows the same licensing terms.