using ToolsSharing.Core.DTOs.Location;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Enums;

namespace ToolsSharing.Core.Interfaces;

/// <summary>
/// Comprehensive location service combining geocoding, security, and proximity search operations
/// </summary>
public interface ILocationService
{
    #region Geocoding Operations

    /// <summary>
    /// Search for locations by text query with geocoding
    /// </summary>
    /// <param name="query">Location search query (e.g., "Athens, GA")</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="countryCode">Optional country code filter (e.g., "US")</param>
    /// <param name="userId">User performing the search for security logging</param>
    /// <returns>List of location options with coordinates</returns>
    Task<List<LocationOption>> SearchLocationsAsync(string query, int limit = 5, string? countryCode = null, string? userId = null);

    /// <summary>
    /// Reverse geocode coordinates to location names
    /// </summary>
    /// <param name="lat">Latitude</param>
    /// <param name="lng">Longitude</param>
    /// <param name="userId">User performing the search for security logging</param>
    /// <returns>Location option with address details or null if not found</returns>
    Task<LocationOption?> ReverseGeocodeAsync(decimal lat, decimal lng, string? userId = null);

    #endregion

    #region Database Operations

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
    /// <param name="userId">User performing the search for security logging</param>
    /// <returns>Combined suggestions from database and geocoding</returns>
    Task<List<LocationOption>> GetLocationSuggestionsAsync(string query, int limit = 5, string? userId = null);

    #endregion

    #region Location Processing

    /// <summary>
    /// Parse and validate location input with fallback logic
    /// </summary>
    /// <param name="locationInput">Raw location input (coordinates, address, or place name)</param>
    /// <param name="fallbackLocation">Fallback location if parsing fails</param>
    /// <returns>Parsed and validated location option</returns>
    Task<LocationOption?> ProcessLocationInputAsync(string? locationInput, string? fallbackLocation = null);

    /// <summary>
    /// Parse coordinate string to decimal values
    /// </summary>
    /// <param name="coordinateString">Coordinate string in various formats</param>
    /// <returns>Parsed latitude and longitude or null if invalid</returns>
    (decimal lat, decimal lng)? ParseCoordinates(string coordinateString);

    /// <summary>
    /// Validate coordinate values are within valid ranges
    /// </summary>
    /// <param name="lat">Latitude to validate</param>
    /// <param name="lng">Longitude to validate</param>
    /// <returns>True if coordinates are valid</returns>
    bool ValidateCoordinates(decimal lat, decimal lng);

    #endregion

    #region Proximity Search

    /// <summary>
    /// Find nearby tools within specified radius with security-aware distance bands
    /// </summary>
    /// <param name="centerLat">Search center latitude</param>
    /// <param name="centerLng">Search center longitude</param>
    /// <param name="radiusKm">Search radius in kilometers</param>
    /// <param name="userId">User performing the search</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="userAgent">User agent for security logging</param>
    /// <param name="ipAddress">IP address for security logging</param>
    /// <returns>List of nearby tools with distance bands</returns>
    Task<List<NearbyToolDto>> FindNearbyToolsAsync(decimal centerLat, decimal centerLng, decimal radiusKm, 
        string? userId = null, int limit = 50, string? userAgent = null, string? ipAddress = null);

    /// <summary>
    /// Find nearby bundles within specified radius with security-aware distance bands
    /// </summary>
    /// <param name="centerLat">Search center latitude</param>
    /// <param name="centerLng">Search center longitude</param>
    /// <param name="radiusKm">Search radius in kilometers</param>
    /// <param name="userId">User performing the search</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="userAgent">User agent for security logging</param>
    /// <param name="ipAddress">IP address for security logging</param>
    /// <returns>List of nearby bundles with distance bands</returns>
    Task<List<NearbyBundleDto>> FindNearbyBundlesAsync(decimal centerLat, decimal centerLng, decimal radiusKm, 
        string? userId = null, int limit = 50, string? userAgent = null, string? ipAddress = null);

    /// <summary>
    /// Find nearby users within specified radius with security-aware distance bands
    /// </summary>
    /// <param name="centerLat">Search center latitude</param>
    /// <param name="centerLng">Search center longitude</param>
    /// <param name="radiusKm">Search radius in kilometers</param>
    /// <param name="userId">User performing the search</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="userAgent">User agent for security logging</param>
    /// <param name="ipAddress">IP address for security logging</param>
    /// <returns>List of nearby users with distance bands</returns>
    Task<List<NearbyUserDto>> FindNearbyUsersAsync(decimal centerLat, decimal centerLng, decimal radiusKm, 
        string? userId = null, int limit = 50, string? userAgent = null, string? ipAddress = null);

    #endregion

    #region Security Integration

    /// <summary>
    /// Validate if user can perform location search (rate limiting and triangulation detection)
    /// </summary>
    /// <param name="userId">User attempting search</param>
    /// <param name="targetId">Target being searched (optional)</param>
    /// <param name="searchType">Type of search being performed</param>
    /// <param name="searchLat">Search center latitude</param>
    /// <param name="searchLng">Search center longitude</param>
    /// <param name="searchQuery">Text search query</param>
    /// <param name="userAgent">User agent for security logging</param>
    /// <param name="ipAddress">IP address for security logging</param>
    /// <returns>True if search is allowed, throws exception if blocked</returns>
    Task<bool> ValidateLocationSearchAsync(string? userId, string? targetId, LocationSearchType searchType,
        decimal? searchLat = null, decimal? searchLng = null, string? searchQuery = null, 
        string? userAgent = null, string? ipAddress = null);

    #endregion

    #region Distance Calculations

    /// <summary>
    /// Calculate distance between two points using Haversine formula
    /// </summary>
    /// <param name="lat1">First point latitude</param>
    /// <param name="lng1">First point longitude</param>
    /// <param name="lat2">Second point latitude</param>  
    /// <param name="lng2">Second point longitude</param>
    /// <returns>Distance in kilometers</returns>
    decimal CalculateDistance(decimal lat1, decimal lng1, decimal lat2, decimal lng2);

    /// <summary>
    /// Convert exact distance to privacy-aware distance band
    /// </summary>
    /// <param name="distanceKm">Exact distance in kilometers</param>
    /// <returns>Distance band for display</returns>
    DistanceBand GetDistanceBand(decimal distanceKm);

    /// <summary>
    /// Get user-friendly text for distance band
    /// </summary>
    /// <param name="distanceBand">Distance band enum</param>
    /// <returns>Human-readable text</returns>
    string GetDistanceBandText(DistanceBand distanceBand);

    #endregion

    #region Geographic Clustering

    /// <summary>
    /// Analyze geographic clustering of search results
    /// </summary>
    /// <param name="locations">List of locations to analyze</param>
    /// <param name="clusterRadius">Radius for clustering in kilometers</param>
    /// <returns>List of location clusters</returns>
    Task<List<LocationCluster>> AnalyzeGeographicClustersAsync(List<LocationOption> locations, decimal clusterRadius = 5.0m);

    #endregion
}