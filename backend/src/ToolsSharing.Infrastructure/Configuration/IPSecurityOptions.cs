namespace ToolsSharing.Infrastructure.Configuration;

public class IPSecurityOptions
{
    public const string SectionName = "IPSecurity";
    
    public bool EnableGeolocation { get; set; } = true;
    public bool EnableIPBlocking { get; set; } = true;
    
    public List<string> BlockedCountries { get; set; } = new();
    public List<string> AllowedCountries { get; set; } = new(); // Empty means no restrictions
    
    public TimeSpan DefaultBlockDuration { get; set; } = TimeSpan.FromDays(1);
    public TimeSpan TemporaryBlockDuration { get; set; } = TimeSpan.FromHours(4);
    
    // Known malicious IP ranges (CIDR notation supported)
    public List<string> KnownMaliciousIPs { get; set; } = new();
    
    // Trusted proxy IPs (for X-Forwarded-For header validation)
    public List<string> TrustedProxies { get; set; } = new()
    {
        "127.0.0.1", // localhost
        "::1" // IPv6 localhost
    };
    
    // Rate limiting for IP checking (prevent abuse of geolocation service)
    public int MaxGeoLocationChecksPerMinute { get; set; } = 60;
}