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
    /// Text-based location query (e.g., "near Atlanta") - used when Lat/Lng are null
    /// </summary>
    public string? LocationQuery { get; set; }
    
    /// <summary>
    /// Direct latitude coordinate for proximity search - takes priority over LocationQuery
    /// </summary>
    [Range(-90.0, 90.0)]
    public decimal? Lat { get; set; }
    
    /// <summary>
    /// Direct longitude coordinate for proximity search - takes priority over LocationQuery
    /// </summary>
    [Range(-180.0, 180.0)]
    public decimal? Lng { get; set; }
    
    /// <summary>
    /// Center point latitude for proximity search (legacy field, use Lat instead)
    /// </summary>
    [Range(-90.0, 90.0)]
    public decimal? CenterLat { get; set; }
    
    /// <summary>
    /// Center point longitude for proximity search (legacy field, use Lng instead)
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
    
    /// <summary>
    /// Include items that don't have location data (Phase 8 Enhancement)
    /// </summary>
    public bool IncludeItemsWithoutLocation { get; set; } = true;
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

/// <summary>
/// A location option with address details and coordinates for geocoding
/// </summary>
public class LocationOption
{
    /// <summary>
    /// Display name for UI (e.g., "Athens, GA, USA")
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Area/neighborhood name
    /// </summary>
    public string? Area { get; set; }

    /// <summary>
    /// City name
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// State/province name
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Country name
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Country code (e.g., "US", "GR")
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Postal/ZIP code
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Latitude coordinate
    /// </summary>
    public decimal? Lat { get; set; }

    /// <summary>
    /// Longitude coordinate
    /// </summary>
    public decimal? Lng { get; set; }

    /// <summary>
    /// Precision radius in meters for privacy
    /// </summary>
    public int PrecisionRadius { get; set; } = 1000;

    /// <summary>
    /// Source of the location data
    /// </summary>
    public LocationSource Source { get; set; } = LocationSource.Manual;

    /// <summary>
    /// Confidence score (0.0 to 1.0)
    /// </summary>
    public decimal Confidence { get; set; } = 1.0m;

    /// <summary>
    /// Bounding box for the location (optional)
    /// </summary>
    public LocationBounds? Bounds { get; set; }
}

/// <summary>
/// Geographic bounding box
/// </summary>
public class LocationBounds
{
    public decimal NorthLat { get; set; }
    public decimal SouthLat { get; set; }
    public decimal EastLng { get; set; }
    public decimal WestLng { get; set; }
}

/// <summary>
/// Response for location search operations
/// </summary>
public class LocationSearchResponse
{
    /// <summary>
    /// List of location options
    /// </summary>
    public List<LocationOption> Results { get; set; } = new();

    /// <summary>
    /// Total number of results found
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Search query that was executed
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Provider used for the search
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Time taken for the search in milliseconds
    /// </summary>
    public int TimeTakenMs { get; set; }

    /// <summary>
    /// Whether results were served from cache
    /// </summary>
    public bool FromCache { get; set; }
}

/// <summary>
/// Request for reverse geocoding operations
/// </summary>
public class ReverseGeocodeRequest
{
    /// <summary>
    /// Latitude coordinate
    /// </summary>
    public decimal Lat { get; set; }

    /// <summary>
    /// Longitude coordinate
    /// </summary>
    public decimal Lng { get; set; }

    /// <summary>
    /// Language preference for results
    /// </summary>
    public string? Language { get; set; } = "en";

    /// <summary>
    /// Zoom level for detail (1-18, higher = more detail)
    /// </summary>
    public int Zoom { get; set; } = 10;
}

/// <summary>
/// Tool with distance information for proximity searches
/// </summary>
public class NearbyToolDto
{
    /// <summary>
    /// Tool ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Owner ID
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Tool name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tool description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Daily rental rate
    /// </summary>
    public decimal DailyRate { get; set; }

    /// <summary>
    /// Tool condition
    /// </summary>
    public string Condition { get; set; } = string.Empty;

    /// <summary>
    /// Tool category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Required skill level
    /// </summary>
    public string RequiredSkillLevel { get; set; } = string.Empty;

    /// <summary>
    /// Estimated project duration
    /// </summary>
    public string EstimatedProjectDuration { get; set; } = string.Empty;

    /// <summary>
    /// Tool image URLs
    /// </summary>
    public List<string> ImageUrls { get; set; } = new();

    /// <summary>
    /// Owner name
    /// </summary>
    public string OwnerName { get; set; } = string.Empty;

    /// <summary>
    /// Location display text
    /// </summary>
    public string LocationDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Distance band for privacy
    /// </summary>
    public DistanceBand DistanceBand { get; set; }

    /// <summary>
    /// Human-readable distance text
    /// </summary>
    public string DistanceText { get; set; } = string.Empty;

    /// <summary>
    /// Whether the tool is currently available
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Average rating
    /// </summary>
    public decimal AverageRating { get; set; }

    /// <summary>
    /// Number of reviews
    /// </summary>
    public int ReviewCount { get; set; }

    /// <summary>
    /// Number of times rented
    /// </summary>
    public int RentalCount { get; set; }

    /// <summary>
    /// Whether the tool is featured
    /// </summary>
    public bool IsFeatured { get; set; }

    /// <summary>
    /// When the tool was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Bundle with distance information for proximity searches
/// </summary>
public class NearbyBundleDto
{
    /// <summary>
    /// Bundle ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Owner ID
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Bundle name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Bundle description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Bundle category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Required skill level
    /// </summary>
    public string RequiredSkillLevel { get; set; } = string.Empty;

    /// <summary>
    /// Estimated project duration
    /// </summary>
    public string EstimatedProjectDuration { get; set; } = string.Empty;

    /// <summary>
    /// Bundle image URL
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Number of tools in bundle
    /// </summary>
    public int ToolCount { get; set; }

    /// <summary>
    /// Original total cost
    /// </summary>
    public decimal OriginalCost { get; set; }

    /// <summary>
    /// Discounted cost
    /// </summary>
    public decimal DiscountedCost { get; set; }

    /// <summary>
    /// Discount percentage
    /// </summary>
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Owner name
    /// </summary>
    public string OwnerName { get; set; } = string.Empty;

    /// <summary>
    /// Location display text
    /// </summary>
    public string LocationDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Distance band for privacy
    /// </summary>
    public DistanceBand DistanceBand { get; set; }

    /// <summary>
    /// Human-readable distance text
    /// </summary>
    public string DistanceText { get; set; } = string.Empty;

    /// <summary>
    /// Whether the bundle is currently available
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Average rating
    /// </summary>
    public decimal AverageRating { get; set; }

    /// <summary>
    /// Number of reviews
    /// </summary>
    public int ReviewCount { get; set; }

    /// <summary>
    /// Number of times rented
    /// </summary>
    public int RentalCount { get; set; }

    /// <summary>
    /// Whether the bundle is featured
    /// </summary>
    public bool IsFeatured { get; set; }

    /// <summary>
    /// When the bundle was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request for nearby search operations
/// </summary>
public class NearbySearchRequest
{
    /// <summary>
    /// Search center latitude
    /// </summary>
    public decimal? Lat { get; set; }

    /// <summary>
    /// Search center longitude
    /// </summary>
    public decimal? Lng { get; set; }

    /// <summary>
    /// Search radius in kilometers
    /// </summary>
    public decimal RadiusKm { get; set; } = 10;

    /// <summary>
    /// Category filter (optional)
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Maximum daily rate filter (optional)
    /// </summary>
    public decimal? MaxDailyRate { get; set; }

    /// <summary>
    /// Minimum rating filter (optional)
    /// </summary>
    public decimal? MinRating { get; set; }

    /// <summary>
    /// Only available items
    /// </summary>
    public bool AvailableOnly { get; set; } = true;

    /// <summary>
    /// Maximum number of results
    /// </summary>
    public int Limit { get; set; } = 20;

    /// <summary>
    /// Skip this many results (for pagination)
    /// </summary>
    public int Skip { get; set; } = 0;
}

/// <summary>
/// User with distance information for proximity searches
/// </summary>
public class NearbyUserDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// User bio/description
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// Avatar image URL
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Location display text
    /// </summary>
    public string LocationDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Distance band for privacy
    /// </summary>
    public DistanceBand DistanceBand { get; set; }

    /// <summary>
    /// Human-readable distance text
    /// </summary>
    public string DistanceText { get; set; } = string.Empty;

    /// <summary>
    /// Number of tools owned
    /// </summary>
    public int ToolCount { get; set; }

    /// <summary>
    /// Number of bundles owned
    /// </summary>
    public int BundleCount { get; set; }

    /// <summary>
    /// Average rating as tool owner
    /// </summary>
    public decimal AverageRating { get; set; }

    /// <summary>
    /// Number of reviews received
    /// </summary>
    public int ReviewCount { get; set; }

    /// <summary>
    /// Whether the user is currently active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When the user was last seen
    /// </summary>
    public DateTime? LastSeen { get; set; }
}

/// <summary>
/// Geographic cluster of locations for analysis
/// </summary>
public class LocationCluster
{
    /// <summary>
    /// Center latitude of the cluster
    /// </summary>
    public decimal CenterLat { get; set; }

    /// <summary>
    /// Center longitude of the cluster
    /// </summary>
    public decimal CenterLng { get; set; }

    /// <summary>
    /// Radius of the cluster in kilometers
    /// </summary>
    public decimal RadiusKm { get; set; }

    /// <summary>
    /// Number of locations in this cluster
    /// </summary>
    public int LocationCount { get; set; }

    /// <summary>
    /// Locations belonging to this cluster
    /// </summary>
    public List<LocationOption> Locations { get; set; } = new();

    /// <summary>
    /// Representative location name for the cluster
    /// </summary>
    public string ClusterName { get; set; } = string.Empty;

    /// <summary>
    /// Density score (locations per square km)
    /// </summary>
    public decimal DensityScore { get; set; }

    /// <summary>
    /// Bounding box of the cluster
    /// </summary>
    public LocationBounds? Bounds { get; set; }
}