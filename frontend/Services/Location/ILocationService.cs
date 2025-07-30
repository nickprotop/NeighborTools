using ToolsSharing.Frontend.Models.Location;

namespace ToolsSharing.Frontend.Services.Location;

/// <summary>
/// Frontend location service interface for geocoding, proximity search, and browser geolocation
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Search for locations using geocoding service
    /// </summary>
    /// <param name="query">Location search query (e.g., "Athens, GA")</param>
    /// <param name="maxResults">Maximum number of results (default 5)</param>
    /// <param name="countryCode">Optional country code filter</param>
    /// <returns>List of location options</returns>
    Task<List<LocationOption>> SearchLocationsAsync(string query, int maxResults = 5, string? countryCode = null);

    /// <summary>
    /// Reverse geocode coordinates to location information
    /// </summary>
    /// <param name="lat">Latitude</param>
    /// <param name="lng">Longitude</param>
    /// <returns>Location information or null if not found</returns>
    Task<LocationOption?> ReverseGeocodeAsync(decimal lat, decimal lng);

    /// <summary>
    /// Get popular locations from database
    /// </summary>
    /// <param name="maxResults">Maximum number of results (default 10)</param>
    /// <returns>List of popular locations</returns>
    Task<List<LocationOption>> GetPopularLocationsAsync(int maxResults = 10);

    /// <summary>
    /// Get location suggestions combining database and geocoding results
    /// </summary>
    /// <param name="query">Partial location query</param>
    /// <param name="maxResults">Maximum number of results (default 8)</param>
    /// <returns>Hybrid suggestions</returns>
    Task<List<LocationOption>> GetLocationSuggestionsAsync(string query, int maxResults = 8);

    /// <summary>
    /// Find nearby tools with privacy protection
    /// </summary>
    /// <param name="lat">Center latitude</param>
    /// <param name="lng">Center longitude</param>
    /// <param name="radiusKm">Search radius in kilometers</param>
    /// <param name="maxResults">Maximum number of results (default 20)</param>
    /// <returns>List of nearby tools with distance bands</returns>
    Task<List<NearbyToolDto>> FindNearbyToolsAsync(decimal lat, decimal lng, decimal radiusKm, int maxResults = 20);

    /// <summary>
    /// Find nearby bundles with privacy protection
    /// </summary>
    /// <param name="lat">Center latitude</param>
    /// <param name="lng">Center longitude</param>
    /// <param name="radiusKm">Search radius in kilometers</param>
    /// <param name="maxResults">Maximum number of results (default 20)</param>
    /// <returns>List of nearby bundles with distance bands</returns>
    Task<List<NearbyBundleDto>> FindNearbyBundlesAsync(decimal lat, decimal lng, decimal radiusKm, int maxResults = 20);

    /// <summary>
    /// Get current location from browser geolocation API
    /// </summary>
    /// <param name="highAccuracy">Request high accuracy positioning</param>
    /// <param name="timeoutMs">Timeout in milliseconds (default 10000)</param>
    /// <returns>Geolocation result with coordinates or error information</returns>
    Task<GeolocationResult> GetCurrentLocationAsync(bool highAccuracy = true, int timeoutMs = 10000);

    /// <summary>
    /// Clear all cached location data
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Check if geolocation is supported by the browser
    /// </summary>
    /// <returns>True if geolocation is supported</returns>
    Task<bool> IsGeolocationSupportedAsync();
}