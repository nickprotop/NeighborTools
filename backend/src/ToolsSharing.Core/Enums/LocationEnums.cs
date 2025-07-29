namespace ToolsSharing.Core.Enums;

/// <summary>
/// Privacy levels for location display and sharing
/// </summary>
public enum PrivacyLevel
{
    /// <summary>
    /// Show neighborhood/area level (~1-2km radius)
    /// </summary>
    Neighborhood = 1,
    
    /// <summary>
    /// Show ZIP/postal code level (~5-10km radius)
    /// </summary>
    ZipCode = 2,
    
    /// <summary>
    /// Show district/city level (~10-20km radius)
    /// </summary>
    District = 3,
    
    /// <summary>
    /// Show exact or near-exact location (street level)
    /// </summary>
    Exact = 4
}

/// <summary>
/// Source of location data for tracking and validation
/// </summary>
public enum LocationSource
{
    /// <summary>
    /// User manually entered the location
    /// </summary>
    Manual = 1,
    
    /// <summary>
    /// Location was geocoded from text input
    /// </summary>
    Geocoded = 2,
    
    /// <summary>
    /// User clicked on a map to select location
    /// </summary>
    UserClick = 3,
    
    /// <summary>
    /// Location obtained from browser geolocation API
    /// </summary>
    Browser = 4
}

/// <summary>
/// Distance bands for privacy-aware distance display
/// </summary>
public enum DistanceBand
{
    /// <summary>
    /// Very close: 0-2km
    /// </summary>
    VeryClose = 1,
    
    /// <summary>
    /// Nearby: 2-10km
    /// </summary>
    Nearby = 2,
    
    /// <summary>
    /// Moderate distance: 10-25km
    /// </summary>
    Moderate = 3,
    
    /// <summary>
    /// Far: 25-50km
    /// </summary>
    Far = 4,
    
    /// <summary>
    /// Very far: 50km+
    /// </summary>
    VeryFar = 5
}

/// <summary>
/// Types of location searches for triangulation detection
/// </summary>
public enum LocationSearchType
{
    /// <summary>
    /// Searching for tools by location
    /// </summary>
    ToolSearch = 1,
    
    /// <summary>
    /// Searching for bundles by location
    /// </summary>
    BundleSearch = 2,
    
    /// <summary>
    /// Searching for users by location
    /// </summary>
    UserSearch = 3,
    
    /// <summary>
    /// General proximity search
    /// </summary>
    ProximitySearch = 4,
    
    /// <summary>
    /// Geocoding request
    /// </summary>
    Geocoding = 5
}