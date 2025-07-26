namespace ToolsSharing.Infrastructure.Configuration;

public class RequestSecurityOptions
{
    public const string SectionName = "RequestSecurity";
    
    public long DefaultMaxRequestBodySize { get; set; } = 10_000_000; // 10MB default
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    public Dictionary<string, long> EndpointLimits { get; set; } = new()
    {
        // File upload endpoints - larger limits
        ["/api/files/upload"] = 100_000_000, // 100MB
        ["/api/tools/upload-images"] = 50_000_000, // 50MB
        ["/api/bundles/upload-image"] = 50_000_000, // 50MB
        
        // Authentication endpoints - small limits
        ["/api/auth/login"] = 1_000, // 1KB
        ["/api/auth/register"] = 5_000, // 5KB
        ["/api/auth/forgot-password"] = 1_000, // 1KB
        ["/api/auth/reset-password"] = 2_000, // 2KB
        
        // API endpoints - medium limits  
        ["/api/tools"] = 500_000, // 500KB
        ["/api/bundles"] = 500_000, // 500KB
        ["/api/rentals"] = 100_000, // 100KB
        ["/api/payments"] = 50_000, // 50KB
        ["/api/messages"] = 100_000, // 100KB
    };
}