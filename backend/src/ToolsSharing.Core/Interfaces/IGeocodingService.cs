using ToolsSharing.Core.DTOs.Location;

namespace ToolsSharing.Core.Interfaces;

/// <summary>
/// Service for geocoding operations with support for multiple providers
/// </summary>
public interface IGeocodingService
{
    /// <summary>
    /// Search for locations by text query with geocoding
    /// </summary>
    /// <param name="query">Location search query (e.g., "Athens, GA")</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="countryCode">Optional country code filter (e.g., "US")</param>
    /// <returns>List of location options with coordinates</returns>
    Task<List<LocationOption>> SearchLocationsAsync(string query, int limit = 5, string? countryCode = null);

    /// <summary>
    /// Reverse geocode coordinates to location names
    /// </summary>
    /// <param name="lat">Latitude</param>
    /// <param name="lng">Longitude</param>
    /// <returns>Location option with address details or null if not found</returns>
    Task<LocationOption?> ReverseGeocodeAsync(decimal lat, decimal lng);

    /// <summary>
    /// Get popular locations from database based on usage frequency
    /// </summary>
    /// <param name="limit">Maximum number of results</param>
    /// <returns>List of popular locations</returns>
    Task<List<LocationOption>> GetPopularLocationsAsync(int limit = 10);

    /// <summary>
    /// Get hybrid location suggestions combining database and geocoding
    /// </summary>
    /// <param name="query">Partial location query</param>
    /// <param name="limit">Maximum number of results</param>
    /// <returns>Combined suggestions from database and geocoding</returns>
    Task<List<LocationOption>> GetLocationSuggestionsAsync(string query, int limit = 5);

    /// <summary>
    /// Gets the provider name for logging and debugging
    /// </summary>
    string ProviderName { get; }
}