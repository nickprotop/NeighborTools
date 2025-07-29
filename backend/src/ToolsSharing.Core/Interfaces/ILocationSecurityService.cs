using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Enums;

namespace ToolsSharing.Core.Interfaces;

/// <summary>
/// Service for location security operations including triangulation detection and privacy protection
/// </summary>
public interface ILocationSecurityService
{
    /// <summary>
    /// Detects if the search pattern indicates a triangulation attempt
    /// </summary>
    /// <param name="userId">User performing the search</param>
    /// <param name="targetId">Target entity being searched</param>
    /// <param name="searchType">Type of search (Tool, Bundle, User)</param>
    /// <param name="searchLat">Search center latitude</param>
    /// <param name="searchLng">Search center longitude</param>
    /// <param name="searchQuery">Text search query</param>
    /// <returns>True if triangulation attempt detected</returns>
    Task<bool> IsTriangulationAttemptAsync(string? userId, string? targetId, LocationSearchType searchType, 
        decimal? searchLat, decimal? searchLng, string? searchQuery);

    /// <summary>
    /// Converts exact distance to privacy-aware distance band
    /// </summary>
    /// <param name="distanceKm">Exact distance in kilometers</param>
    /// <returns>Distance band for display</returns>
    DistanceBand GetDistanceBand(decimal distanceKm);

    /// <summary>
    /// Adds random noise to distance for privacy protection
    /// </summary>
    /// <param name="exactDistance">Exact distance in kilometers</param>
    /// <returns>Fuzzed distance with noise</returns>
    decimal GetFuzzedDistance(decimal exactDistance);

    /// <summary>
    /// Quantizes coordinates to a grid system for privacy
    /// </summary>
    /// <param name="lat">Original latitude</param>
    /// <param name="lng">Original longitude</param>
    /// <param name="privacyLevel">Privacy level determining grid size</param>
    /// <returns>Quantized coordinates</returns>
    (decimal quantizedLat, decimal quantizedLng) QuantizeLocation(decimal lat, decimal lng, PrivacyLevel privacyLevel);

    /// <summary>
    /// Applies time-based jitter to coordinates
    /// </summary>
    /// <param name="lat">Original latitude</param>
    /// <param name="lng">Original longitude</param>
    /// <param name="privacyLevel">Privacy level determining jitter amount</param>
    /// <returns>Jittered coordinates</returns>
    (decimal jitteredLat, decimal jitteredLng) GetJitteredLocation(decimal lat, decimal lng, PrivacyLevel privacyLevel);

    /// <summary>
    /// Logs location search for security analysis
    /// </summary>
    /// <param name="userId">User performing the search</param>
    /// <param name="targetId">Target entity being searched</param>
    /// <param name="searchType">Type of search</param>
    /// <param name="searchLat">Search center latitude</param>
    /// <param name="searchLng">Search center longitude</param>
    /// <param name="searchQuery">Text search query</param>
    /// <param name="userAgent">User agent string</param>
    /// <param name="ipAddress">User IP address</param>
    /// <returns>Location search log entry</returns>
    Task<LocationSearchLog> LogLocationSearchAsync(string? userId, string? targetId, LocationSearchType searchType,
        decimal? searchLat, decimal? searchLng, string? searchQuery, string? userAgent, string? ipAddress);

    /// <summary>
    /// Validates if user can perform location search (rate limiting)
    /// </summary>
    /// <param name="userId">User attempting search</param>
    /// <param name="targetId">Target being searched (optional)</param>
    /// <returns>True if search is allowed</returns>
    Task<bool> ValidateLocationSearchAsync(string? userId, string? targetId = null);

    /// <summary>
    /// Gets user-friendly text for distance band
    /// </summary>
    /// <param name="distanceBand">Distance band enum</param>
    /// <returns>Human-readable text</returns>
    string GetDistanceBandText(DistanceBand distanceBand);
}