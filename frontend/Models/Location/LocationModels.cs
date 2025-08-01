namespace ToolsSharing.Frontend.Models.Location;

/// <summary>
/// Represents a location option from geocoding or database (mirrors backend exactly)
/// </summary>
public class LocationOption
{
    public string DisplayName { get; set; } = "";
    public string? Area { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public string? PostalCode { get; set; }
    public decimal? Lat { get; set; }
    public decimal? Lng { get; set; }
    public int PrecisionRadius { get; set; } = 1000;
    public LocationSource Source { get; set; } = LocationSource.Manual;
    public decimal Confidence { get; set; } = 1.0m;
    public LocationBounds? Bounds { get; set; }

    // Required for MudBlazor 8 object-based autocomplete
    public override bool Equals(object? obj)
    {
        if (obj is not LocationOption other) return false;
        return DisplayName == other.DisplayName && 
               Lat == other.Lat && 
               Lng == other.Lng &&
               Source == other.Source;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(DisplayName, Lat, Lng, Source);
    }
}

/// <summary>
/// Nearby tool search result with distance bands (mirrors backend exactly)
/// </summary>
public class NearbyToolDto
{
    public Guid Id { get; set; }
    public string OwnerId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal DailyRate { get; set; }
    public string Condition { get; set; } = "";
    public string Category { get; set; } = "";
    public string RequiredSkillLevel { get; set; } = "";
    public string EstimatedProjectDuration { get; set; } = "";
    public List<string> ImageUrls { get; set; } = new();
    public string? ImageUrl { get; set; }
    public string OwnerName { get; set; } = "";
    public string LocationDisplay { get; set; } = "";
    public DistanceBand DistanceBand { get; set; }
    public string DistanceText { get; set; } = "";
    public bool IsAvailable { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int RentalCount { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Nearby bundle search result with distance bands (mirrors backend exactly)
/// </summary>
public class NearbyBundleDto
{
    public Guid Id { get; set; }
    public string OwnerId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string RequiredSkillLevel { get; set; } = "";
    public string EstimatedProjectDuration { get; set; } = "";
    public string? ImageUrl { get; set; }
    public int ToolCount { get; set; }
    public decimal OriginalCost { get; set; }
    public decimal DiscountedCost { get; set; }
    public decimal DiscountPercentage { get; set; }
    public string OwnerName { get; set; } = "";
    public string LocationDisplay { get; set; } = "";
    public DistanceBand DistanceBand { get; set; }
    public string DistanceText { get; set; } = "";
    public bool IsAvailable { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int RentalCount { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; }
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

/// <summary>
/// Geographic bounding box (mirrors backend exactly)
/// </summary>
public class LocationBounds
{
    public decimal NorthLat { get; set; }
    public decimal SouthLat { get; set; }
    public decimal EastLng { get; set; }
    public decimal WestLng { get; set; }
}

/// <summary>
/// User location model for profile settings (mirrors enhanced location fields)
/// </summary>
public class UserLocationModel
{
    public string? LocationDisplay { get; set; }
    public string? LocationArea { get; set; }
    public string? LocationCity { get; set; }
    public string? LocationState { get; set; }
    public string? LocationCountry { get; set; }
    public decimal? LocationLat { get; set; }
    public decimal? LocationLng { get; set; }
    public int? LocationPrecisionRadius { get; set; }
    public LocationSource? LocationSource { get; set; }
    public PrivacyLevel LocationPrivacyLevel { get; set; } = PrivacyLevel.Neighborhood;
    public DateTime? LocationUpdatedAt { get; set; }
}