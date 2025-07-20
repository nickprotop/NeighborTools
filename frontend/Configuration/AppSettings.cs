namespace ToolsSharing.Frontend.Configuration
{
    public class AppSettings
    {
        public ApiSettings ApiSettings { get; set; } = new();
        public string Environment { get; set; } = "Development";
        public FeatureFlags Features { get; set; } = new();
        public SiteSettings Site { get; set; } = new();
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
}