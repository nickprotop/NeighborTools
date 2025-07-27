namespace ToolsSharing.Infrastructure.Configuration;

public class RequestSecurityOptions
{
    public const string SectionName = "RequestSecurity";
    
    public long DefaultMaxRequestBodySize { get; set; } = 10_000_000; // 10MB default
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    private Dictionary<string, long> _endpointLimits = GetDefaultEndpointLimits();
    
    public Dictionary<string, long> EndpointLimits 
    { 
        get => _endpointLimits;
        set 
        {
            // If configuration binding provides an empty dictionary, restore defaults
            if (value == null || value.Count == 0)
            {
                _endpointLimits = GetDefaultEndpointLimits();
            }
            else
            {
                // Merge with defaults to ensure critical endpoints are covered
                var defaults = GetDefaultEndpointLimits();
                foreach (var kvp in defaults)
                {
                    if (!value.ContainsKey(kvp.Key))
                    {
                        value[kvp.Key] = kvp.Value;
                    }
                }
                _endpointLimits = value;
            }
        }
    }
    
    private static Dictionary<string, long> GetDefaultEndpointLimits()
    {
        return new Dictionary<string, long>
        {
            // File upload endpoints - larger limits
            ["/api/files/upload"] = 100_000_000, // 100MB
            ["/api/tools/upload-images"] = 50_000_000, // 50MB
            ["/api/bundles/upload-image"] = 10_000_000, // 10MB
            ["/api/user/profile-picture"] = 10_000_000, // 10MB - Profile pictures
            ["/api/disputes/"] = 50_000_000, // 50MB - Dispute evidence uploads
            
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
            ["/api/"] = 500_000, // 500KB - General API fallback
        };
    }
}