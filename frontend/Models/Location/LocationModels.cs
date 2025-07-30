namespace ToolsSharing.Frontend.Models.Location;

/// <summary>
/// Represents a location option from geocoding or database (mirrors backend)
/// </summary>
public class LocationOption
{
    public string DisplayName { get; set; } = "";
    public string Area { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string Country { get; set; } = "";
    public decimal? Lat { get; set; }
    public decimal? Lng { get; set; }
    public int? PrecisionRadius { get; set; }
    public LocationSource Source { get; set; }
    public decimal? Confidence { get; set; }
}

/// <summary>
/// Nearby tool search result with distance bands (mirrors backend)
/// </summary>
public class NearbyToolDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal DailyRate { get; set; }
    public string Category { get; set; } = "";
    public string Condition { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public string LocationDisplay { get; set; } = "";
    public DistanceBand DistanceBand { get; set; }
    public string DistanceText { get; set; } = "";
    public List<string> ImageUrls { get; set; } = new();
    public bool IsAvailable { get; set; }
    public decimal? Rating { get; set; }
    public int ReviewCount { get; set; }
}

/// <summary>
/// Nearby bundle search result with distance bands (mirrors backend)
/// </summary>
public class NearbyBundleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public string LocationDisplay { get; set; } = "";
    public DistanceBand DistanceBand { get; set; }
    public string DistanceText { get; set; } = "";
    public string? ImageUrl { get; set; }
    public decimal BundleDiscount { get; set; }
    public int ToolCount { get; set; }
    public decimal EstimatedValue { get; set; }
    public bool IsPublished { get; set; }
}

/// <summary>
/// Browser geolocation API result
/// </summary>
public class GeolocationResult
{
    public bool Success { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? Accuracy { get; set; }
    public DateTime? Timestamp { get; set; }
    public GeolocationError? Error { get; set; }
    public string ErrorMessage { get; set; } = "";
}

/// <summary>
/// API response wrapper (mirrors backend)
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Location search request parameters
/// </summary>
public class LocationSearchRequest
{
    public string Query { get; set; } = "";
    public int MaxResults { get; set; } = 5;
    public string? CountryCode { get; set; }
}

/// <summary>
/// Reverse geocoding request parameters
/// </summary>
public class ReverseGeocodeRequest
{
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
}

/// <summary>
/// Nearby search request parameters
/// </summary>
public class NearbySearchRequest
{
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public decimal RadiusKm { get; set; }
    public int MaxResults { get; set; } = 20;
}

/// <summary>
/// Location suggestions request parameters
/// </summary>
public class LocationSuggestionsRequest
{
    public string Query { get; set; } = "";
    public int MaxResults { get; set; } = 8;
}