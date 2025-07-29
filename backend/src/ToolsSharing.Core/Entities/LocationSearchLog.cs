using ToolsSharing.Core.Enums;

namespace ToolsSharing.Core.Entities;

/// <summary>
/// Entity for tracking location search patterns to detect triangulation attempts
/// </summary>
public class LocationSearchLog : BaseEntity
{
    /// <summary>
    /// ID of the user performing the search (null for anonymous users)
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// ID of the target being searched for (tool, bundle, or user ID)
    /// </summary>
    public string? TargetId { get; set; }
    
    /// <summary>
    /// Type of location search being performed
    /// </summary>
    public LocationSearchType SearchType { get; set; }
    
    /// <summary>
    /// Latitude of the search center point
    /// </summary>
    public decimal? SearchLat { get; set; }
    
    /// <summary>
    /// Longitude of the search center point
    /// </summary>
    public decimal? SearchLng { get; set; }
    
    /// <summary>
    /// Search radius in kilometers
    /// </summary>
    public decimal? SearchRadiusKm { get; set; }
    
    /// <summary>
    /// Original search query text (if applicable)
    /// </summary>
    public string? SearchQuery { get; set; }
    
    /// <summary>
    /// User agent string from the browser
    /// </summary>
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// IP address of the search request
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// Session ID for grouping related searches
    /// </summary>
    public string? SessionId { get; set; }
    
    /// <summary>
    /// Whether this search was flagged as suspicious
    /// </summary>
    public bool IsSuspicious { get; set; } = false;
    
    /// <summary>
    /// Reason for flagging as suspicious (if applicable)
    /// </summary>
    public string? SuspiciousReason { get; set; }
    
    /// <summary>
    /// Number of results returned by this search
    /// </summary>
    public int ResultCount { get; set; } = 0;
    
    /// <summary>
    /// Response time of the search in milliseconds
    /// </summary>
    public int ResponseTimeMs { get; set; }
    
    /// <summary>
    /// Navigation property to the user who performed the search
    /// </summary>
    public User? User { get; set; }
}