using System.Net;
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
                var apiResponse = System.Text.Json.JsonSerializer.Deserialize<IPWhoResponse>(json);
                
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
                    _logger.LogWarning("ipwho.is API returned error for IP {IP}: {Message}", 
                        ipAddress, apiResponse?.Message);
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
        public string? Ip { get; set; }
        public bool Success { get; set; }
        public string? Type { get; set; }
        public string? Continent { get; set; }
        public string? ContinentCode { get; set; }
        public string? Country { get; set; }
        public string? CountryCode { get; set; }
        public string? Region { get; set; }
        public string? RegionCode { get; set; }
        public string? City { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsEu { get; set; }
        public string? PostalCode { get; set; }
        public string? CallingCode { get; set; }
        public string? Capital { get; set; }
        public string? Borders { get; set; }
        public IPWhoFlag? Flag { get; set; }
        public IPWhoConnection? Connection { get; set; }
        public IPWhoTimezone? Timezone { get; set; }
        public IPWhoSecurity? Security { get; set; }
        public string? Message { get; set; }
    }

    private class IPWhoFlag
    {
        public string? Img { get; set; }
        public string? Emoji { get; set; }
        public string? EmojiUnicode { get; set; }
    }

    private class IPWhoConnection
    {
        public string? Asn { get; set; }
        public string? Org { get; set; }
        public string? Isp { get; set; }
        public string? Domain { get; set; }
    }

    private class IPWhoTimezone
    {
        public string? Id { get; set; }
        public string? Abbr { get; set; }
        public bool IsDst { get; set; }
        public int Offset { get; set; }
        public int Utc { get; set; }
        public string? CurrentTime { get; set; }
    }

    private class IPWhoSecurity
    {
        public bool IsProxy { get; set; }
        public string? ProxyType { get; set; }
        public bool IsVpn { get; set; }
        public bool IsTor { get; set; }
        public bool IsThreat { get; set; }
        public bool IsAnonymous { get; set; }
    }
}