using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToolsSharing.Infrastructure.Configuration;
using ToolsSharing.Infrastructure.Models;

namespace ToolsSharing.Infrastructure.Services;

public interface IGeolocationService
{
    Task<GeolocationInfo> GetLocationAsync(string ipAddress);
    Task<bool> IsFromAllowedCountryAsync(string ipAddress);
}

public class GeolocationService : IGeolocationService
{
    private readonly IDistributedCache _cache;
    private readonly IPSecurityOptions _options;
    private readonly ILogger<GeolocationService> _logger;
    private readonly HttpClient _httpClient;

    public GeolocationService(
        IDistributedCache cache,
        IOptions<IPSecurityOptions> options,
        ILogger<GeolocationService> logger,
        HttpClient httpClient)
    {
        _cache = cache;
        _options = options.Value;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<GeolocationInfo> GetLocationAsync(string ipAddress)
    {
        // Handle localhost and private IPs
        if (IsLocalOrPrivateIP(ipAddress))
        {
            return new GeolocationInfo
            {
                CountryCode = "LOCAL",
                CountryName = "Local/Private Network",
                City = "Unknown",
                Region = "Unknown"
            };
        }

        // Check cache first
        var cacheKey = $"geo:{ipAddress}";
        var cachedResult = await _cache.GetStringAsync(cacheKey);
        
        if (!string.IsNullOrEmpty(cachedResult))
        {
            try
            {
                var cached = System.Text.Json.JsonSerializer.Deserialize<GeolocationInfo>(cachedResult);
                if (cached != null && cached.CachedAt > DateTime.UtcNow.AddHours(-24))
                {
                    return cached;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached geolocation for IP {IP}", ipAddress);
            }
        }

        // Fetch from geolocation service
        var geoInfo = await FetchGeolocationAsync(ipAddress);
        
        // Cache the result for 24 hours
        if (geoInfo != null)
        {
            var serialized = System.Text.Json.JsonSerializer.Serialize(geoInfo);
            await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            });
        }

        return geoInfo ?? new GeolocationInfo
        {
            CountryCode = "UNKNOWN",
            CountryName = "Unknown",
            City = "Unknown",
            Region = "Unknown"
        };
    }

    public async Task<bool> IsFromAllowedCountryAsync(string ipAddress)
    {
        var geoInfo = await GetLocationAsync(ipAddress);
        
        // If no restrictions set, allow all
        if (_options.AllowedCountries.Count == 0)
        {
            // Check if country is explicitly blocked
            return !_options.BlockedCountries.Contains(geoInfo.CountryCode);
        }
        
        // If allowed countries are specified, check if IP is from allowed country
        return _options.AllowedCountries.Contains(geoInfo.CountryCode) &&
               !_options.BlockedCountries.Contains(geoInfo.CountryCode);
    }

    private async Task<GeolocationInfo?> FetchGeolocationAsync(string ipAddress)
    {
        try
        {
            // Using ipwho.is - free geolocation service with unlimited requests and no API key required
            var response = await _httpClient.GetAsync(ipAddress);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                
                // Log the actual response for debugging rate limiting issues
                if (string.IsNullOrEmpty(json) || json.Length < 10)
                {
                    _logger.LogWarning("ipwho.is returned empty/invalid response for IP {IP}. Response: '{Response}'", 
                        ipAddress, json);
                    return null;
                }
                
                IPWhoResponse? apiResponse;
                try 
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    };
                    apiResponse = System.Text.Json.JsonSerializer.Deserialize<IPWhoResponse>(json, options);
                }
                catch (JsonException ex)
                {
                    // Check if this is likely a rate limiting or API change issue
                    if (json.Contains("rate") || json.Contains("limit") || json.Contains("quota"))
                    {
                        _logger.LogWarning("ipwho.is API rate limiting detected during JSON parsing for IP {IP}. Will return cached/fallback data.", ipAddress);
                    }
                    else if (json.Contains("asn") && ex.Message.Contains("Number"))
                    {
                        _logger.LogWarning("ipwho.is API schema change detected (ASN field type changed) for IP {IP}. Error: {Error}", 
                            ipAddress, ex.Message);
                    }
                    else
                    {
                        _logger.LogError(ex, "Failed to deserialize ipwho.is response for IP {IP}. Response: {Response}", 
                            ipAddress, json.Length > 500 ? json[..500] + "..." : json);
                    }
                    return null;
                }
                
                if (apiResponse?.Success == true)
                {
                    return new GeolocationInfo
                    {
                        CountryCode = apiResponse.CountryCode ?? "UNKNOWN",
                        CountryName = apiResponse.Country ?? "Unknown",
                        City = apiResponse.City ?? "Unknown",
                        Region = apiResponse.Region ?? "Unknown",
                        Latitude = apiResponse.Latitude,
                        Longitude = apiResponse.Longitude,
                        TimeZone = apiResponse.Timezone?.Id ?? "Unknown",
                        ISP = apiResponse.Connection?.Isp ?? "Unknown",
                        IsFromProxy = apiResponse.Security?.IsProxy == true,
                        IsFromVPN = apiResponse.Security?.IsVpn == true,
                        CachedAt = DateTime.UtcNow
                    };
                }
                else
                {
                    // Check if this might be a rate limiting response
                    if (json.Contains("rate") || json.Contains("limit") || json.Contains("quota"))
                    {
                        _logger.LogWarning("ipwho.is API rate limiting detected for IP {IP}. Response: '{ResponsePreview}'", 
                            ipAddress, json.Length > 300 ? json.Substring(0, 300) + "..." : json);
                    }
                    else
                    {
                        // Log both the message and a portion of the raw response for debugging
                        _logger.LogWarning("ipwho.is API returned error for IP {IP}. Message: '{Message}', Response preview: '{ResponsePreview}'", 
                            ipAddress, apiResponse?.Message ?? "null", 
                            json.Length > 200 ? json.Substring(0, 200) + "..." : json);
                    }
                }
            }
            else
            {
                _logger.LogWarning("ipwho.is API request failed for IP {IP}: {StatusCode}", 
                    ipAddress, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching geolocation for IP {IP}", ipAddress);
        }

        return null;
    }

    private static bool IsLocalOrPrivateIP(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress) || ipAddress == "unknown")
            return true;

        if (!IPAddress.TryParse(ipAddress, out var ip))
            return true;

        // Check for localhost
        if (IPAddress.IsLoopback(ip))
            return true;

        // Check for private IP ranges
        var bytes = ip.GetAddressBytes();
        
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            // IPv4 private ranges
            return (bytes[0] == 10) ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 169 && bytes[1] == 254); // Link-local
        }
        
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            // IPv6 private ranges
            return ip.ToString().StartsWith("fc00:") ||  // Unique local
                   ip.ToString().StartsWith("fd00:") ||  // Unique local
                   ip.ToString().StartsWith("fe80:");    // Link-local
        }

        return false;
    }

    // Data model for ipwho.is response
    private class IPWhoResponse
    {
        [JsonPropertyName("ip")]
        public string? Ip { get; set; }
        
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        
        [JsonPropertyName("continent")]
        public string? Continent { get; set; }
        
        [JsonPropertyName("continent_code")]
        public string? ContinentCode { get; set; }
        
        [JsonPropertyName("country")]
        public string? Country { get; set; }
        
        [JsonPropertyName("country_code")]
        public string? CountryCode { get; set; }
        
        [JsonPropertyName("region")]
        public string? Region { get; set; }
        
        [JsonPropertyName("region_code")]
        public string? RegionCode { get; set; }
        
        [JsonPropertyName("city")]
        public string? City { get; set; }
        
        [JsonPropertyName("latitude")]
        public double? Latitude { get; set; }
        
        [JsonPropertyName("longitude")]
        public double? Longitude { get; set; }
        
        [JsonPropertyName("is_eu")]
        public bool IsEu { get; set; }
        
        [JsonPropertyName("postal")]
        public string? PostalCode { get; set; }
        
        [JsonPropertyName("calling_code")]
        public string? CallingCode { get; set; }
        
        [JsonPropertyName("capital")]
        public string? Capital { get; set; }
        
        [JsonPropertyName("borders")]
        public string? Borders { get; set; }
        [JsonPropertyName("flag")]
        public IPWhoFlag? Flag { get; set; }
        
        [JsonPropertyName("connection")]
        public IPWhoConnection? Connection { get; set; }
        
        [JsonPropertyName("timezone")]
        public IPWhoTimezone? Timezone { get; set; }
        
        [JsonPropertyName("security")]
        public IPWhoSecurity? Security { get; set; }
        
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    private class IPWhoFlag
    {
        [JsonPropertyName("img")]
        public string? Img { get; set; }
        
        [JsonPropertyName("emoji")]
        public string? Emoji { get; set; }
        
        [JsonPropertyName("emoji_unicode")]
        public string? EmojiUnicode { get; set; }
    }

    private class IPWhoConnection
    {
        [JsonPropertyName("asn")]
        public object? Asn { get; set; } // Can be string or number
        
        [JsonPropertyName("org")]
        public string? Org { get; set; }
        
        [JsonPropertyName("isp")]
        public string? Isp { get; set; }
        
        [JsonPropertyName("domain")]
        public string? Domain { get; set; }
    }

    private class IPWhoTimezone
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [JsonPropertyName("abbr")]
        public string? Abbr { get; set; }
        
        [JsonPropertyName("is_dst")]
        public bool IsDst { get; set; }
        
        [JsonPropertyName("offset")]
        public int Offset { get; set; }
        
        [JsonPropertyName("utc")]
        public string? Utc { get; set; }
        
        [JsonPropertyName("current_time")]
        public string? CurrentTime { get; set; }
    }

    private class IPWhoSecurity
    {
        [JsonPropertyName("is_proxy")]
        public bool IsProxy { get; set; }
        
        [JsonPropertyName("proxy_type")]
        public string? ProxyType { get; set; }
        
        [JsonPropertyName("is_vpn")]
        public bool IsVpn { get; set; }
        
        [JsonPropertyName("is_tor")]
        public bool IsTor { get; set; }
        
        [JsonPropertyName("is_threat")]
        public bool IsThreat { get; set; }
        
        [JsonPropertyName("is_anonymous")]
        public bool IsAnonymous { get; set; }
    }
}