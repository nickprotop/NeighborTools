namespace ToolsSharing.Core.Configuration;

/// <summary>
/// Configuration for geocoding services
/// </summary>
public class GeocodingConfiguration
{
    /// <summary>
    /// Default geocoding provider to use
    /// </summary>
    public string DefaultProvider { get; set; } = "OpenStreetMap";

    /// <summary>
    /// OpenStreetMap/Nominatim configuration
    /// </summary>
    public OpenStreetMapConfiguration OpenStreetMap { get; set; } = new();

    /// <summary>
    /// HERE Maps configuration
    /// </summary>
    public HereConfiguration HERE { get; set; } = new();
}

/// <summary>
/// Configuration for OpenStreetMap Nominatim API
/// </summary>
public class OpenStreetMapConfiguration
{
    /// <summary>
    /// Base URL for Nominatim API
    /// </summary>
    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";

    /// <summary>
    /// User agent string (required by Nominatim)
    /// </summary>
    public string UserAgent { get; set; } = "NeighborTools/1.0";

    /// <summary>
    /// Maximum requests per second (Nominatim limit)
    /// </summary>
    public int RequestsPerSecond { get; set; } = 1;

    /// <summary>
    /// Cache duration for responses in hours
    /// </summary>
    public int CacheDurationHours { get; set; } = 24;

    /// <summary>
    /// Timeout for HTTP requests in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Default language for results
    /// </summary>
    public string DefaultLanguage { get; set; } = "en";

    /// <summary>
    /// Email address for contact (recommended by Nominatim)
    /// </summary>
    public string? ContactEmail { get; set; }
}

/// <summary>
/// Configuration for HERE Maps API
/// </summary>
public class HereConfiguration
{
    /// <summary>
    /// Base URL for HERE Geocoding API
    /// </summary>
    public string BaseUrl { get; set; } = "https://geocode.search.hereapi.com/v1";

    /// <summary>
    /// HERE API key
    /// </summary>
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// Maximum requests per second
    /// </summary>
    public int RequestsPerSecond { get; set; } = 10;

    /// <summary>
    /// Cache duration for responses in hours
    /// </summary>
    public int CacheDurationHours { get; set; } = 24;

    /// <summary>
    /// Timeout for HTTP requests in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Default language for results
    /// </summary>
    public string DefaultLanguage { get; set; } = "en";

    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int MaxResults { get; set; } = 20;

    /// <summary>
    /// Whether to include detailed address components
    /// </summary>
    public bool IncludeAddressDetails { get; set; } = true;

    /// <summary>
    /// Whether to include map view bounds
    /// </summary>
    public bool IncludeMapView { get; set; } = true;
}

/// <summary>
/// Location security configuration
/// </summary>
public class LocationSecurityConfiguration
{
    /// <summary>
    /// Maximum searches per hour per user
    /// </summary>
    public int MaxSearchesPerHour { get; set; } = 50;

    /// <summary>
    /// Maximum searches per target per user
    /// </summary>
    public int MaxSearchesPerTarget { get; set; } = 5;

    /// <summary>
    /// Minimum time between searches in seconds
    /// </summary>
    public int MinSearchIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Enable triangulation detection
    /// </summary>
    public bool EnableTriangulationDetection { get; set; } = true;

    /// <summary>
    /// Minimum distance between search points to trigger triangulation detection (km)
    /// </summary>
    public decimal TriangulationMinDistanceKm { get; set; } = 1.0m;

    /// <summary>
    /// Maximum time window for triangulation detection (hours)
    /// </summary>
    public int TriangulationTimeWindowHours { get; set; } = 24;

    /// <summary>
    /// Number of search points needed to trigger triangulation detection
    /// </summary>
    public int TriangulationMinSearchPoints { get; set; } = 3;

    /// <summary>
    /// Whether to log all location searches
    /// </summary>
    public bool LogAllSearches { get; set; } = true;

    /// <summary>
    /// How long to keep search logs (days)
    /// </summary>
    public int SearchLogRetentionDays { get; set; } = 90;
}

/// <summary>
/// Cache configuration for geocoding responses
/// </summary>
public class GeocodingCacheConfiguration
{
    /// <summary>
    /// Enable caching
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default cache duration in hours
    /// </summary>
    public int DefaultDurationHours { get; set; } = 24;

    /// <summary>
    /// Cache duration for popular locations in hours
    /// </summary>
    public int PopularLocationDurationHours { get; set; } = 168; // 1 week

    /// <summary>
    /// Cache duration for error responses in minutes
    /// </summary>
    public int ErrorCacheDurationMinutes { get; set; } = 15;

    /// <summary>
    /// Maximum number of items to cache
    /// </summary>
    public int MaxCacheSize { get; set; } = 10000;

    /// <summary>
    /// Cache key prefix
    /// </summary>
    public string KeyPrefix { get; set; } = "geocoding:";
}