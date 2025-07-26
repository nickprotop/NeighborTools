namespace ToolsSharing.Infrastructure.Configuration;

public class SecurityHeadersOptions
{
    public const string SectionName = "SecurityHeaders";
    
    public bool EnableContentSecurityPolicy { get; set; } = true;
    public bool EnableHSTS { get; set; } = true;
    public bool EnablePermissionsPolicy { get; set; } = true;
    public bool EnableXSSProtection { get; set; } = true;
    public bool EnableFrameOptions { get; set; } = true;
    public bool EnableContentTypeOptions { get; set; } = true;
    
    public int HSTSMaxAge { get; set; } = 31536000; // 1 year in seconds
    public bool HSTSIncludeSubDomains { get; set; } = true;
    public bool HSTSPreload { get; set; } = true;
    
    public string ContentSecurityPolicy { get; set; } = 
        "default-src 'none'; frame-ancestors 'none';";
    
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";
    
    public string PermissionsPolicy { get; set; } = 
        "geolocation=(), microphone=(), camera=(), payment=(), usb=()";
    
    // Custom headers to add
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
}