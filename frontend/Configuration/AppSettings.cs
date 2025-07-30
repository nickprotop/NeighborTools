namespace ToolsSharing.Frontend.Configuration
{
    public class AppSettings
    {
        public ApiSettings ApiSettings { get; set; } = new();
        public string Environment { get; set; } = "Development";
        public FeatureFlags Features { get; set; } = new();
        public SiteSettings Site { get; set; } = new();
        public MapSettings MapSettings { get; set; } = new();
    }

    public class ApiSettings
    {
        public string BaseUrl { get; set; } = "http://localhost:5002";
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryAttempts { get; set; } = 3;
    }

    public class FeatureFlags
    {
        public bool EnableAdvancedSearch { get; set; } = true;
        public bool EnableNotifications { get; set; } = true;
        public bool EnablePayments { get; set; } = true;
        public bool EnableDisputes { get; set; } = true;
        public bool EnableAnalytics { get; set; } = false;
    }

    public class SiteSettings
    {
        public string HomePageUrl { get; set; } = "https://neighbortools.com";
    }

    public class MapSettings
    {
        public string Provider { get; set; } = "OpenStreetMap";
        public string MapTileUrl { get; set; } = "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png";
        public string MapAttribution { get; set; } = "© OpenStreetMap contributors";
        public int DefaultZoom { get; set; } = 13;
        public int MinZoom { get; set; } = 5;
        public int MaxZoom { get; set; } = 18;
        public MapCenter DefaultCenter { get; set; } = new();
        public bool ShowLocationControls { get; set; } = true;
        public bool EnableGeolocation { get; set; } = true;
        public int LocationTimeout { get; set; } = 10000;
        public int MaxLocationAge { get; set; } = 300000;
        public OpenStreetMapSettings OpenStreetMap { get; set; } = new();
        public GoogleMapsSettings GoogleMaps { get; set; } = new();
        public MapboxSettings Mapbox { get; set; } = new();
    }

    public class OpenStreetMapSettings
    {
        public string BaseUrl { get; set; } = "https://{s}.tile.openstreetmap.org";
        public string TilePattern { get; set; } = "{z}/{x}/{y}.png";
        public string[] Subdomains { get; set; } = new[] { "a", "b", "c" };
        public string Attribution { get; set; } = "© OpenStreetMap contributors";
        public int MaxZoom { get; set; } = 19;
    }

    public class GoogleMapsSettings
    {
        public string ApiKey { get; set; } = "";
        public string MapType { get; set; } = "roadmap";
        public string Language { get; set; } = "en";
        public string Region { get; set; } = "US";
    }

    public class MapboxSettings
    {
        public string AccessToken { get; set; } = "";
        public string StyleUrl { get; set; } = "mapbox://styles/mapbox/streets-v11";
        public string Attribution { get; set; } = "© Mapbox © OpenStreetMap";
    }

    public class MapCenter
    {
        public double Lat { get; set; } = 40.7128; // New York City default
        public double Lng { get; set; } = -74.0060; // New York City default
    }
}