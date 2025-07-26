using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToolsSharing.Infrastructure.Configuration;
using ToolsSharing.Infrastructure.Models;

namespace ToolsSharing.Infrastructure.Services;

public interface IIPSecurityService
{
    Task<IPBlockResult> IsIPBlockedAsync(string ipAddress);
    Task<GeolocationResult> CheckGeolocationAsync(string ipAddress);
    Task BlockIPAsync(string ipAddress, string reason, TimeSpan? duration = null, string blockedBy = "System");
    Task UnblockIPAsync(string ipAddress);
    Task IncrementOffenseCountAsync(string ipAddress, string reason);
}

public class IPSecurityService : IIPSecurityService
{
    private readonly IDistributedCache _cache;
    private readonly IGeolocationService _geolocation;
    private readonly IPSecurityOptions _options;
    private readonly ILogger<IPSecurityService> _logger;

    // Known malicious IP ranges and suspicious networks
    private readonly HashSet<string> _knownMaliciousIPs;

    public IPSecurityService(
        IDistributedCache cache,
        IGeolocationService geolocation,
        IOptions<IPSecurityOptions> options,
        ILogger<IPSecurityService> logger)
    {
        _cache = cache;
        _geolocation = geolocation;
        _options = options.Value;
        _logger = logger;
        
        // Initialize known malicious IPs from configuration
        _knownMaliciousIPs = new HashSet<string>(_options.KnownMaliciousIPs);
    }

    public async Task<IPBlockResult> IsIPBlockedAsync(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress) || ipAddress == "unknown")
        {
            return new IPBlockResult { IsBlocked = false };
        }

        // Check cache first for blocked status
        var cacheKey = $"ip_block:{ipAddress}";
        var cachedResult = await _cache.GetStringAsync(cacheKey);
        
        if (!string.IsNullOrEmpty(cachedResult))
        {
            try
            {
                var blockInfo = System.Text.Json.JsonSerializer.Deserialize<IPBlockInfo>(cachedResult);
                if (blockInfo != null)
                {
                    // Check if block has expired (unless it's permanent)
                    if (blockInfo.IsPermanent || blockInfo.ExpiresAt > DateTime.UtcNow)
                    {
                        var geoInfo = await _geolocation.GetLocationAsync(ipAddress);
                        return new IPBlockResult
                        {
                            IsBlocked = true,
                            Reason = blockInfo.Reason,
                            ExpiresAt = blockInfo.IsPermanent ? null : blockInfo.ExpiresAt,
                            IsPermanent = blockInfo.IsPermanent,
                            CountryCode = geoInfo.CountryCode
                        };
                    }
                    else
                    {
                        // Block has expired, remove from cache
                        await _cache.RemoveAsync(cacheKey);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize IP block info for {IP}", ipAddress);
            }
        }

        // Check against known malicious IPs
        if (_knownMaliciousIPs.Contains(ipAddress))
        {
            await BlockIPAsync(ipAddress, "Known malicious IP", _options.DefaultBlockDuration);
            return new IPBlockResult
            {
                IsBlocked = true,
                Reason = "Known malicious IP",
                ExpiresAt = DateTime.UtcNow.Add(_options.DefaultBlockDuration)
            };
        }

        // Not blocked
        return new IPBlockResult { IsBlocked = false };
    }

    public async Task<GeolocationResult> CheckGeolocationAsync(string ipAddress)
    {
        if (!_options.EnableGeolocation)
        {
            return new GeolocationResult { IsAllowed = true };
        }

        try
        {
            var geoInfo = await _geolocation.GetLocationAsync(ipAddress);
            var isAllowed = await _geolocation.IsFromAllowedCountryAsync(ipAddress);

            return new GeolocationResult
            {
                IsAllowed = isAllowed,
                CountryCode = geoInfo.CountryCode,
                CountryName = geoInfo.CountryName,
                City = geoInfo.City,
                Region = geoInfo.Region,
                Latitude = geoInfo.Latitude,
                Longitude = geoInfo.Longitude,
                IsFromVPN = geoInfo.IsFromVPN,
                IsFromProxy = geoInfo.IsFromProxy
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking geolocation for IP {IP}", ipAddress);
            
            // On error, default to allowing unless we have strict geo restrictions
            return new GeolocationResult
            {
                IsAllowed = _options.AllowedCountries.Count == 0, // Allow if no restrictions
                CountryCode = "ERROR",
                CountryName = "Geolocation Error"
            };
        }
    }

    public async Task BlockIPAsync(string ipAddress, string reason, TimeSpan? duration = null, string blockedBy = "System")
    {
        var blockDuration = duration ?? _options.DefaultBlockDuration;
        var isPermanent = blockDuration.TotalDays >= 365; // Consider 1 year+ as permanent
        
        var blockInfo = new IPBlockInfo
        {
            Reason = reason,
            BlockedAt = DateTime.UtcNow,
            ExpiresAt = isPermanent ? DateTime.UtcNow.AddYears(100) : DateTime.UtcNow.Add(blockDuration),
            BlockedBy = blockedBy,
            OffenseCount = 1,
            IsPermanent = isPermanent
        };

        // Check if IP was already blocked to increment offense count
        var existingBlock = await IsIPBlockedAsync(ipAddress);
        if (existingBlock.IsBlocked)
        {
            var existingCacheKey = $"ip_block:{ipAddress}";
            var existingCached = await _cache.GetStringAsync(existingCacheKey);
            if (!string.IsNullOrEmpty(existingCached))
            {
                try
                {
                    var existingInfo = System.Text.Json.JsonSerializer.Deserialize<IPBlockInfo>(existingCached);
                    if (existingInfo != null)
                    {
                        blockInfo.OffenseCount = existingInfo.OffenseCount + 1;
                        
                        // Increase block duration for repeat offenders
                        if (blockInfo.OffenseCount >= 3 && !isPermanent)
                        {
                            blockDuration = TimeSpan.FromDays(7); // 1 week for repeat offenders
                            blockInfo.ExpiresAt = DateTime.UtcNow.Add(blockDuration);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse existing block info for IP {IP}", ipAddress);
                }
            }
        }

        var cacheKey = $"ip_block:{ipAddress}";
        var serialized = System.Text.Json.JsonSerializer.Serialize(blockInfo);
        
        var cacheOptions = new DistributedCacheEntryOptions();
        if (!isPermanent)
        {
            cacheOptions.AbsoluteExpiration = blockInfo.ExpiresAt;
        }
        else
        {
            cacheOptions.SlidingExpiration = TimeSpan.FromDays(365); // Refresh annually for permanent blocks
        }
        
        await _cache.SetStringAsync(cacheKey, serialized, cacheOptions);

        _logger.LogWarning("IP {IPAddress} blocked for {Duration} (Offense #{OffenseCount}): {Reason} - Blocked by: {BlockedBy}", 
            ipAddress, blockDuration, blockInfo.OffenseCount, reason, blockedBy);
    }

    public async Task UnblockIPAsync(string ipAddress)
    {
        var cacheKey = $"ip_block:{ipAddress}";
        await _cache.RemoveAsync(cacheKey);
        
        _logger.LogInformation("IP {IPAddress} unblocked", ipAddress);
    }

    public async Task IncrementOffenseCountAsync(string ipAddress, string reason)
    {
        // This method can be called to track suspicious activity without immediately blocking
        var offenseKey = $"offense:{ipAddress}";
        var existingOffenses = await _cache.GetStringAsync(offenseKey);
        
        int offenseCount = 1;
        if (!string.IsNullOrEmpty(existingOffenses) && int.TryParse(existingOffenses, out var parsed))
        {
            offenseCount = parsed + 1;
        }

        // Store offense count with sliding expiration
        await _cache.SetStringAsync(offenseKey, offenseCount.ToString(), new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(24) // Reset count after 24 hours of no activity
        });

        _logger.LogInformation("Offense recorded for IP {IPAddress} (Count: {Count}): {Reason}", 
            ipAddress, offenseCount, reason);

        // Auto-block after too many offenses
        if (offenseCount >= 5)
        {
            await BlockIPAsync(ipAddress, $"Multiple offenses ({offenseCount}): {reason}", _options.TemporaryBlockDuration);
        }
    }
}