using System.ComponentModel.DataAnnotations;
using ToolsSharing.Core.Enums;

namespace ToolsSharing.Core.DTOs.Location;

/// <summary>
/// Enhanced location data for entities
/// </summary>
public class LocationDto
{
    /// <summary>
    /// User-friendly display name (e.g., "Downtown Athens, GA")
    /// </summary>
    public string? LocationDisplay { get; set; }
    
    /// <summary>
    /// Neighborhood/area name (e.g., "Downtown")
    /// </summary>
    public string? LocationArea { get; set; }
    
    /// <summary>
    /// City name (e.g., "Athens")
    /// </summary>
    public string? LocationCity { get; set; }
    
    /// <summary>
    /// State/province name (e.g., "Georgia")
    /// </summary>
    public string? LocationState { get; set; }
    
    /// <summary>
    /// Country name (e.g., "USA")
    /// </summary>
    public string? LocationCountry { get; set; }
    
    /// <summary>
    /// Latitude (quantized for privacy)
    /// </summary>
    public decimal? LocationLat { get; set; }
    
    /// <summary>
    /// Longitude (quantized for privacy)
    /// </summary>
    public decimal? LocationLng { get; set; }
    
    /// <summary>
    /// Generalization radius in meters
    /// </summary>
    public int? LocationPrecisionRadius { get; set; }
    
    /// <summary>
    /// How the location was obtained
    /// </summary>
    public LocationSource? LocationSource { get; set; }
    
    /// <summary>
    /// User's privacy preference for this location
    /// </summary>
    public PrivacyLevel LocationPrivacyLevel { get; set; } = PrivacyLevel.Neighborhood;
    
    /// <summary>
    /// When the location was last updated
    /// </summary>
    public DateTime? LocationUpdatedAt { get; set; }
}

/// <summary>
/// Request DTO for updating location information
/// </summary>
public class UpdateLocationRequest
{
    [MaxLength(255)]
    public string? LocationDisplay { get; set; }
    
    [MaxLength(100)]
    public string? LocationArea { get; set; }
    
    [MaxLength(100)]
    public string? LocationCity { get; set; }
    
    [MaxLength(100)]
    public string? LocationState { get; set; }
    
    [MaxLength(100)]
    public string? LocationCountry { get; set; }
    
    [Range(-90.0, 90.0)]
    public decimal? LocationLat { get; set; }
    
    [Range(-180.0, 180.0)]
    public decimal? LocationLng { get; set; }
    
    [Range(1, 100000)]
    public int? LocationPrecisionRadius { get; set; }
    
    public LocationSource? LocationSource { get; set; }
    
    public PrivacyLevel LocationPrivacyLevel { get; set; } = PrivacyLevel.Neighborhood;
}

/// <summary>
/// Search request with location-based filtering
/// </summary>
public class LocationSearchRequest
{
    /// <summary>
    /// Text-based location query (e.g., "near Atlanta")
    /// </summary>
    public string? LocationQuery { get; set; }
    
    /// <summary>
    /// Center point latitude for proximity search
    /// </summary>
    [Range(-90.0, 90.0)]
    public decimal? CenterLat { get; set; }
    
    /// <summary>
    /// Center point longitude for proximity search
    /// </summary>
    [Range(-180.0, 180.0)]
    public decimal? CenterLng { get; set; }
    
    /// <summary>
    /// Search radius in kilometers
    /// </summary>
    [Range(1, 1000)]
    public int? RadiusKm { get; set; }
    
    /// <summary>
    /// Filter by specific areas
    /// </summary>
    public List<string>? Areas { get; set; }
    
    /// <summary>
    /// Filter by specific cities
    /// </summary>
    public List<string>? Cities { get; set; }
    
    /// <summary>
    /// Filter by specific states/provinces
    /// </summary>
    public List<string>? States { get; set; }
    
    /// <summary>
    /// Filter by specific countries
    /// </summary>
    public List<string>? Countries { get; set; }
}

/// <summary>
/// Response for proximity searches with distance information
/// </summary>
public class LocationSearchResultDto<T>
{
    /// <summary>
    /// The search result item
    /// </summary>
    public T Item { get; set; } = default!;
    
    /// <summary>
    /// Distance band for privacy protection
    /// </summary>
    public DistanceBand DistanceBand { get; set; }
    
    /// <summary>
    /// Human-readable distance description
    /// </summary>
    public string DistanceDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Location information (privacy-adjusted)
    /// </summary>
    public LocationDto Location { get; set; } = new();
}

/// <summary>
/// Location search log entry for tracking and triangulation detection
/// </summary>
public class LocationSearchLogDto
{
    public Guid Id { get; set; }
    public string? UserId { get; set; }
    public string? TargetId { get; set; }
    public LocationSearchType SearchType { get; set; }
    public decimal? SearchLat { get; set; }
    public decimal? SearchLng { get; set; }
    public decimal? SearchRadiusKm { get; set; }
    public string? SearchQuery { get; set; }
    public string? IpAddress { get; set; }
    public string? SessionId { get; set; }
    public bool IsSuspicious { get; set; }
    public string? SuspiciousReason { get; set; }
    public int ResultCount { get; set; }
    public int ResponseTimeMs { get; set; }
    public DateTime CreatedAt { get; set; }
}