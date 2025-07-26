namespace ToolsSharing.Infrastructure.Models;

public class IPBlockResult
{
    public bool IsBlocked { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public bool IsPermanent { get; set; }
}

public class GeolocationResult
{
    public bool IsAllowed { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsFromVPN { get; set; }
    public bool IsFromProxy { get; set; }
}

public class IPBlockInfo
{
    public string Reason { get; set; } = string.Empty;
    public DateTime BlockedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string BlockedBy { get; set; } = "System"; // System, Admin, or specific user
    public int OffenseCount { get; set; } = 1;
    public bool IsPermanent { get; set; }
}

public class GeolocationInfo
{
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string TimeZone { get; set; } = string.Empty;
    public string ISP { get; set; } = string.Empty;
    public bool IsFromVPN { get; set; }
    public bool IsFromProxy { get; set; }
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
}

public class RequestSizeValidationResult
{
    public bool IsValid { get; set; }
    public string Reason { get; set; } = string.Empty;
    public long ActualSize { get; set; }
    public long MaxAllowedSize { get; set; }
    public string Endpoint { get; set; } = string.Empty;
}