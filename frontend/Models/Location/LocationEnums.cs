namespace ToolsSharing.Frontend.Models.Location;

/// <summary>
/// Privacy levels for location display (matches backend)
/// </summary>
public enum PrivacyLevel
{
    Neighborhood = 1,   // Show general neighborhood area (~1km radius)
    ZipCode = 2,        // Show zip code area (~5km radius)
    District = 3,       // Show district/city area (~15km radius)
    Exact = 4           // Show exact location coordinates
}

/// <summary>
/// Distance bands for privacy-aware distance display (matches backend)
/// </summary>
public enum DistanceBand
{
    VeryClose = 1,      // Under 0.5km
    Nearby = 2,         // 0.5-2km
    Moderate = 3,       // 2-10km
    Far = 4,            // 10-50km
    VeryFar = 5         // Over 50km
}

/// <summary>
/// Source of location data (matches backend)
/// </summary>
public enum LocationSource
{
    Manual = 1,         // User manually entered
    Geocoded = 2,       // From geocoding service
    UserClick = 3,      // User clicked on map
    Browser = 4         // Browser geolocation API
}

/// <summary>
/// Type of location search for security logging (matches backend)
/// </summary>
public enum LocationSearchType
{
    ToolSearch = 1,     // Searching for tools
    BundleSearch = 2,   // Searching for bundles
    UserSearch = 3      // Searching for users
}

/// <summary>
/// Geolocation API error types
/// </summary>
public enum GeolocationError
{
    PermissionDenied = 1,       // User denied geolocation permission
    PositionUnavailable = 2,    // Location information unavailable
    Timeout = 3,                // Request timed out
    NotSupported = 4            // Geolocation not supported by browser
}