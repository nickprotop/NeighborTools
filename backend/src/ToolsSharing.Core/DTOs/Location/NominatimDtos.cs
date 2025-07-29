using System.Text.Json.Serialization;

namespace ToolsSharing.Core.DTOs.Location;

/// <summary>
/// Response model for OpenStreetMap Nominatim API search results
/// </summary>
public class NominatimResult
{
    [JsonPropertyName("place_id")]
    public long PlaceId { get; set; }

    [JsonPropertyName("licence")]
    public string? Licence { get; set; }

    [JsonPropertyName("osm_type")]
    public string? OsmType { get; set; }

    [JsonPropertyName("osm_id")]
    public long OsmId { get; set; }

    [JsonPropertyName("lat")]
    public string? Lat { get; set; }

    [JsonPropertyName("lon")]
    public string? Lon { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("place_rank")]
    public int PlaceRank { get; set; }

    [JsonPropertyName("importance")]
    public decimal Importance { get; set; }

    [JsonPropertyName("addresstype")]
    public string? AddressType { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("address")]
    public NominatimAddress? Address { get; set; }

    [JsonPropertyName("boundingbox")]
    public List<string>? BoundingBox { get; set; }

    [JsonPropertyName("extratags")]
    public Dictionary<string, string>? ExtraTags { get; set; }

    [JsonPropertyName("namedetails")]
    public Dictionary<string, string>? NameDetails { get; set; }

    /// <summary>
    /// Convert to LocationOption for internal use
    /// </summary>
    public LocationOption ToLocationOption()
    {
        var locationOption = new LocationOption
        {
            DisplayName = DisplayName ?? Name ?? "",
            Source = Core.Enums.LocationSource.Geocoded,
            Confidence = Math.Min(Importance, 1.0m)
        };

        // Parse coordinates
        if (decimal.TryParse(Lat, out var lat))
            locationOption.Lat = lat;
        if (decimal.TryParse(Lon, out var lng))
            locationOption.Lng = lng;

        // Extract address components
        if (Address != null)
        {
            locationOption.Area = Address.Neighbourhood ?? Address.Suburb ?? Address.Village;
            locationOption.City = Address.City ?? Address.Town ?? Address.Municipality;
            locationOption.State = Address.State ?? Address.Province ?? Address.Region;
            locationOption.Country = Address.Country;
            locationOption.CountryCode = Address.CountryCode?.ToUpperInvariant();
            locationOption.PostalCode = Address.Postcode;
        }

        // Set precision radius based on place rank
        locationOption.PrecisionRadius = PlaceRank switch
        {
            <= 10 => 50000, // Country/state level
            <= 12 => 10000, // City level
            <= 16 => 1000,  // Neighborhood level
            <= 18 => 500,   // Street level
            _ => 100        // Building level
        };

        // Parse bounding box if available
        if (BoundingBox?.Count >= 4)
        {
            if (decimal.TryParse(BoundingBox[0], out var south) &&
                decimal.TryParse(BoundingBox[1], out var north) &&
                decimal.TryParse(BoundingBox[2], out var west) &&
                decimal.TryParse(BoundingBox[3], out var east))
            {
                locationOption.Bounds = new LocationBounds
                {
                    SouthLat = south,
                    NorthLat = north,
                    WestLng = west,
                    EastLng = east
                };
            }
        }

        return locationOption;
    }
}

/// <summary>
/// Address details from Nominatim API response
/// </summary>
public class NominatimAddress
{
    [JsonPropertyName("house_number")]
    public string? HouseNumber { get; set; }

    [JsonPropertyName("road")]
    public string? Road { get; set; }

    [JsonPropertyName("neighbourhood")]
    public string? Neighbourhood { get; set; }

    [JsonPropertyName("suburb")]
    public string? Suburb { get; set; }

    [JsonPropertyName("village")]
    public string? Village { get; set; }

    [JsonPropertyName("town")]
    public string? Town { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("municipality")]
    public string? Municipality { get; set; }

    [JsonPropertyName("county")]
    public string? County { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("province")]
    public string? Province { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("postcode")]
    public string? Postcode { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("continent")]
    public string? Continent { get; set; }

    /// <summary>
    /// Get the best city name from available options
    /// </summary>
    public string? GetBestCityName()
    {
        return City ?? Town ?? Municipality ?? Village ?? Suburb;
    }

    /// <summary>
    /// Get the best area/neighborhood name
    /// </summary>
    public string? GetBestAreaName()
    {
        return Neighbourhood ?? Suburb ?? Village;
    }

    /// <summary>
    /// Get the best state/province name
    /// </summary>
    public string? GetBestStateName()
    {
        return State ?? Province ?? Region ?? County;
    }

    /// <summary>
    /// Create a formatted address string
    /// </summary>
    public string ToFormattedAddress()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(HouseNumber) && !string.IsNullOrEmpty(Road))
            parts.Add($"{HouseNumber} {Road}");
        else if (!string.IsNullOrEmpty(Road))
            parts.Add(Road);

        var area = GetBestAreaName();
        if (!string.IsNullOrEmpty(area))
            parts.Add(area);

        var city = GetBestCityName();
        if (!string.IsNullOrEmpty(city))
            parts.Add(city);

        var state = GetBestStateName();
        if (!string.IsNullOrEmpty(state))
            parts.Add(state);

        if (!string.IsNullOrEmpty(Postcode))
            parts.Add(Postcode);

        if (!string.IsNullOrEmpty(Country))
            parts.Add(Country);

        return string.Join(", ", parts);
    }
}

/// <summary>
/// Nominatim API error response
/// </summary>
public class NominatimError
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_code")]
    public int? ErrorCode { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Nominatim API usage statistics for monitoring
/// </summary>
public class NominatimUsageStats
{
    public int RequestsToday { get; set; }
    public int RequestsThisHour { get; set; }
    public DateTime LastRequest { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public int ErrorCount { get; set; }
    public int CacheHitCount { get; set; }
    public int CacheMissCount { get; set; }
}