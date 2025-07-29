using System.Text.Json.Serialization;
using ToolsSharing.Core.Enums;

namespace ToolsSharing.Core.DTOs.Location;

/// <summary>
/// Response model for HERE Maps Geocoding API
/// </summary>
public class HereGeocodingResponse
{
    [JsonPropertyName("items")]
    public List<HereGeocodingResult> Items { get; set; } = new();
}

/// <summary>
/// Individual geocoding result from HERE Maps API
/// </summary>
public class HereGeocodingResult
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("resultType")]
    public string? ResultType { get; set; }

    [JsonPropertyName("houseNumberType")]
    public string? HouseNumberType { get; set; }

    [JsonPropertyName("address")]
    public HereAddress? Address { get; set; }

    [JsonPropertyName("position")]
    public HerePosition? Position { get; set; }

    [JsonPropertyName("access")]
    public List<HerePosition>? Access { get; set; }

    [JsonPropertyName("distance")]
    public int? Distance { get; set; }

    [JsonPropertyName("mapView")]
    public HereMapView? MapView { get; set; }

    [JsonPropertyName("scoring")]
    public HereScoring? Scoring { get; set; }

    [JsonPropertyName("categories")]
    public List<HereCategory>? Categories { get; set; }

    [JsonPropertyName("contacts")]
    public List<HereContact>? Contacts { get; set; }

    /// <summary>
    /// Convert to LocationOption for internal use
    /// </summary>
    public LocationOption ToLocationOption()
    {
        var locationOption = new LocationOption
        {
            DisplayName = Title ?? "",
            Source = LocationSource.Geocoded,
            Confidence = Scoring?.QueryScore ?? 1.0m
        };

        // Set coordinates
        if (Position != null)
        {
            locationOption.Lat = Position.Lat;
            locationOption.Lng = Position.Lng;
        }

        // Extract address components
        if (Address != null)
        {
            locationOption.Area = Address.District ?? Address.Subdistrict;
            locationOption.City = Address.City ?? Address.County;
            locationOption.State = Address.State ?? Address.StateCode;
            locationOption.Country = Address.CountryName;
            locationOption.CountryCode = Address.CountryCode;
            locationOption.PostalCode = Address.PostalCode;
        }

        // Set precision radius based on result type
        locationOption.PrecisionRadius = ResultType switch
        {
            "country" => 50000,
            "administrativeArea" => 25000,
            "locality" => 10000,
            "district" => 5000,
            "street" => 1000,
            "houseNumber" => 100,
            "place" => 500,
            _ => 1000
        };

        // Set bounding box from map view
        if (MapView != null)
        {
            locationOption.Bounds = new LocationBounds
            {
                WestLng = MapView.West,
                SouthLat = MapView.South,
                EastLng = MapView.East,
                NorthLat = MapView.North
            };
        }

        return locationOption;
    }
}

/// <summary>
/// Address details from HERE Maps API
/// </summary>
public class HereAddress
{
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("countryName")]
    public string? CountryName { get; set; }

    [JsonPropertyName("stateCode")]
    public string? StateCode { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("county")]
    public string? County { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("district")]
    public string? District { get; set; }

    [JsonPropertyName("subdistrict")]
    public string? Subdistrict { get; set; }

    [JsonPropertyName("street")]
    public string? Street { get; set; }

    [JsonPropertyName("block")]
    public string? Block { get; set; }

    [JsonPropertyName("subblock")]
    public string? Subblock { get; set; }

    [JsonPropertyName("houseNumber")]
    public string? HouseNumber { get; set; }

    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Get the best city name from available options
    /// </summary>
    public string? GetBestCityName()
    {
        return City ?? County ?? District;
    }

    /// <summary>
    /// Get the best area/neighborhood name
    /// </summary>
    public string? GetBestAreaName()
    {
        return District ?? Subdistrict ?? Block;
    }
}

/// <summary>
/// Geographic position from HERE Maps API
/// </summary>
public class HerePosition
{
    [JsonPropertyName("lat")]
    public decimal Lat { get; set; }

    [JsonPropertyName("lng")]
    public decimal Lng { get; set; }
}

/// <summary>
/// Map view bounds from HERE Maps API
/// </summary>
public class HereMapView
{
    [JsonPropertyName("west")]
    public decimal West { get; set; }

    [JsonPropertyName("south")]
    public decimal South { get; set; }

    [JsonPropertyName("east")]
    public decimal East { get; set; }

    [JsonPropertyName("north")]
    public decimal North { get; set; }
}

/// <summary>
/// Scoring information from HERE Maps API
/// </summary>
public class HereScoring
{
    [JsonPropertyName("queryScore")]
    public decimal QueryScore { get; set; }

    [JsonPropertyName("fieldScore")]
    public HereFieldScore? FieldScore { get; set; }
}

/// <summary>
/// Field-level scoring from HERE Maps API
/// </summary>
public class HereFieldScore
{
    [JsonPropertyName("country")]
    public decimal Country { get; set; }

    [JsonPropertyName("city")]
    public decimal City { get; set; }

    [JsonPropertyName("streets")]
    public List<decimal>? Streets { get; set; }

    [JsonPropertyName("houseNumber")]
    public decimal HouseNumber { get; set; }
}

/// <summary>
/// Category information for places
/// </summary>
public class HereCategory
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("primary")]
    public bool Primary { get; set; }
}

/// <summary>
/// Contact information for places
/// </summary>
public class HereContact
{
    [JsonPropertyName("phone")]
    public List<HerePhone>? Phone { get; set; }

    [JsonPropertyName("www")]
    public List<HereWebsite>? Www { get; set; }

    [JsonPropertyName("email")]
    public List<HereEmail>? Email { get; set; }
}

/// <summary>
/// Phone contact information
/// </summary>
public class HerePhone
{
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("categories")]
    public List<HereCategory>? Categories { get; set; }
}

/// <summary>
/// Website contact information
/// </summary>
public class HereWebsite
{
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("categories")]
    public List<HereCategory>? Categories { get; set; }
}

/// <summary>
/// Email contact information
/// </summary>
public class HereEmail
{
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("categories")]
    public List<HereCategory>? Categories { get; set; }
}

/// <summary>
/// HERE Maps API error response
/// </summary>
public class HereError
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    [JsonPropertyName("code")]
    public int? Code { get; set; }

    [JsonPropertyName("cause")]
    public string? Cause { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }
}

/// <summary>
/// HERE Maps API usage statistics for monitoring
/// </summary>
public class HereUsageStats
{
    public int RequestsToday { get; set; }
    public int RequestsThisHour { get; set; }
    public DateTime LastRequest { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public int ErrorCount { get; set; }
    public int CacheHitCount { get; set; }
    public int CacheMissCount { get; set; }
    public decimal ApiCostToday { get; set; }
    public int QuotaRemaining { get; set; }
}