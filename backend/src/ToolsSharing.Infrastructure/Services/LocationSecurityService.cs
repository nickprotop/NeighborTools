using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ToolsSharing.Core.Configuration;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Enums;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services;

/// <summary>
/// Service for location security operations including triangulation detection and privacy protection
/// </summary>
public class LocationSecurityService : ILocationSecurityService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LocationSecurityService> _logger;
    private readonly LocationSecurityConfiguration _config;
    private readonly Random _random = new();

    public LocationSecurityService(
        ApplicationDbContext context,
        IMemoryCache cache,
        ILogger<LocationSecurityService> logger,
        LocationSecurityConfiguration config)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
        _config = config;
    }

    public async Task<bool> IsTriangulationAttemptAsync(string? userId, string? targetId, LocationSearchType searchType,
        decimal? searchLat, decimal? searchLng, string? searchQuery)
    {
        if (!_config.EnableTriangulationDetection || string.IsNullOrEmpty(userId))
            return false;

        try
        {
            var timeWindow = DateTime.UtcNow.AddHours(-_config.TriangulationTimeWindowHours);
            
            // Get recent searches by this user for the same target
            var recentSearches = await _context.LocationSearchLogs
                .Where(log => log.UserId == userId &&
                            log.TargetId == targetId &&
                            log.SearchType == searchType &&
                            log.CreatedAt >= timeWindow &&
                            log.SearchLat.HasValue &&
                            log.SearchLng.HasValue)
                .OrderBy(log => log.CreatedAt)
                .ToListAsync();

            if (recentSearches.Count < _config.TriangulationMinSearchPoints - 1) // -1 because current search will be added
                return false;

            // Add current search point for analysis
            if (searchLat.HasValue && searchLng.HasValue)
            {
                recentSearches.Add(new LocationSearchLog
                {
                    SearchLat = searchLat,
                    SearchLng = searchLng,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Analyze geometric patterns
            return await AnalyzeGeometricPatternsAsync(recentSearches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting triangulation attempt for user {UserId}", userId);
            return false;
        }
    }

    public DistanceBand GetDistanceBand(decimal distanceKm)
    {
        return distanceKm switch
        {
            <= 0.5m => DistanceBand.VeryClose,
            <= 2.0m => DistanceBand.Nearby,
            <= 10.0m => DistanceBand.Moderate,
            <= 50.0m => DistanceBand.Far,
            _ => DistanceBand.VeryFar
        };
    }

    public decimal GetFuzzedDistance(decimal exactDistance)
    {
        // Add 10-20% random noise to distance
        var noisePercent = 0.1m + ((decimal)_random.NextDouble() * 0.1m); // 10-20%
        var noise = exactDistance * noisePercent * (_random.Next(2) == 0 ? 1 : -1); // Random positive/negative
        return Math.Max(0, exactDistance + noise);
    }

    public (decimal quantizedLat, decimal quantizedLng) QuantizeLocation(decimal lat, decimal lng, PrivacyLevel privacyLevel)
    {
        // Grid size in decimal degrees based on privacy level
        var gridSize = privacyLevel switch
        {
            PrivacyLevel.Exact => 0.0001m,      // ~11m
            PrivacyLevel.District => 0.001m,    // ~111m
            PrivacyLevel.ZipCode => 0.01m,      // ~1.1km
            PrivacyLevel.Neighborhood => 0.1m,  // ~11km
            _ => 0.01m
        };

        // Snap to grid
        var quantizedLat = Math.Floor(lat / gridSize) * gridSize + (gridSize / 2);
        var quantizedLng = Math.Floor(lng / gridSize) * gridSize + (gridSize / 2);

        return (quantizedLat, quantizedLng);
    }

    public (decimal jitteredLat, decimal jitteredLng) GetJitteredLocation(decimal lat, decimal lng, PrivacyLevel privacyLevel)
    {
        // Jitter amount based on privacy level
        var jitterAmount = privacyLevel switch
        {
            PrivacyLevel.Exact => 0.0001m,      // ~11m
            PrivacyLevel.District => 0.002m,    // ~222m
            PrivacyLevel.ZipCode => 0.005m,     // ~555m
            PrivacyLevel.Neighborhood => 0.01m, // ~1.1km
            _ => 0.002m
        };

        // Time-based seed for consistent jitter within same hour
        var hourSeed = DateTime.UtcNow.ToString("yyyyMMddHH").GetHashCode();
        var random = new Random(hourSeed + lat.GetHashCode() + lng.GetHashCode());

        var jitterLat = (decimal)(random.NextDouble() - 0.5) * 2 * jitterAmount;
        var jitterLng = (decimal)(random.NextDouble() - 0.5) * 2 * jitterAmount;

        return (lat + jitterLat, lng + jitterLng);
    }

    public async Task<LocationSearchLog> LogLocationSearchAsync(string? userId, string? targetId, LocationSearchType searchType,
        decimal? searchLat, decimal? searchLng, string? searchQuery, string? userAgent, string? ipAddress)
    {
        var searchLog = new LocationSearchLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TargetId = targetId,
            SearchType = searchType,
            SearchLat = searchLat,
            SearchLng = searchLng,
            SearchQuery = searchQuery,
            UserAgent = userAgent,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };

        // Check if this looks suspicious
        if (!string.IsNullOrEmpty(userId))
        {
            var isTriangulation = await IsTriangulationAttemptAsync(userId, targetId, searchType, searchLat, searchLng, searchQuery);
            if (isTriangulation)
            {
                searchLog.IsSuspicious = true;
                searchLog.SuspiciousReason = "Potential triangulation attempt detected";
                _logger.LogWarning("Triangulation attempt detected: User {UserId}, Target {TargetId}, Type {SearchType}", 
                    userId, targetId, searchType);
            }
        }

        try
        {
            _context.LocationSearchLogs.Add(searchLog);
            await _context.SaveChangesAsync();
            
            _logger.LogDebug("Location search logged: User {UserId}, Type {SearchType}, Suspicious: {IsSuspicious}", 
                userId, searchType, searchLog.IsSuspicious);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log location search for user {UserId}", userId);
        }

        return searchLog;
    }

    public async Task<bool> ValidateLocationSearchAsync(string? userId, string? targetId = null)
    {
        if (string.IsNullOrEmpty(userId))
            return true; // Allow anonymous searches with lower limits

        var cacheKey = $"location_search_rate_limit:{userId}:{targetId ?? "any"}";
        
        try
        {
            // Check rate limits
            var hourlySearches = await GetSearchCountAsync(userId, TimeSpan.FromHours(1));
            if (hourlySearches >= _config.MaxSearchesPerHour)
            {
                _logger.LogWarning("Hourly search limit exceeded for user {UserId}: {Count}/{Limit}", 
                    userId, hourlySearches, _config.MaxSearchesPerHour);
                return false;
            }

            // Check per-target limits if targetId is specified
            if (!string.IsNullOrEmpty(targetId))
            {
                var targetSearches = await GetTargetSearchCountAsync(userId, targetId, TimeSpan.FromHours(1));
                if (targetSearches >= _config.MaxSearchesPerTarget)
                {
                    _logger.LogWarning("Per-target search limit exceeded for user {UserId}, target {TargetId}: {Count}/{Limit}", 
                        userId, targetId, targetSearches, _config.MaxSearchesPerTarget);
                    return false;
                }
            }

            // Check minimum interval between searches
            var lastSearch = await GetLastSearchTimeAsync(userId);
            if (lastSearch.HasValue)
            {
                var timeSinceLastSearch = DateTime.UtcNow - lastSearch.Value;
                if (timeSinceLastSearch.TotalSeconds < _config.MinSearchIntervalSeconds)
                {
                    _logger.LogWarning("Minimum search interval not met for user {UserId}: {Seconds}s < {MinSeconds}s", 
                        userId, timeSinceLastSearch.TotalSeconds, _config.MinSearchIntervalSeconds);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating location search for user {UserId}", userId);
            return false; // Fail securely
        }
    }

    public string GetDistanceBandText(DistanceBand distanceBand)
    {
        return distanceBand switch
        {
            DistanceBand.VeryClose => "Very close (< 0.5 km)",
            DistanceBand.Nearby => "Nearby (< 2 km)",
            DistanceBand.Moderate => "Moderate distance (< 10 km)",
            DistanceBand.Far => "Far (< 50 km)",
            DistanceBand.VeryFar => "Very far (50+ km)",
            _ => "Unknown distance"
        };
    }

    private async Task<bool> AnalyzeGeometricPatternsAsync(List<LocationSearchLog> searches)
    {
        if (searches.Count < _config.TriangulationMinSearchPoints)
            return false;

        var points = searches
            .Where(s => s.SearchLat.HasValue && s.SearchLng.HasValue)
            .Select(s => new LocationPoint { Lat = s.SearchLat!.Value, Lng = s.SearchLng!.Value })
            .ToList();

        if (points.Count < _config.TriangulationMinSearchPoints)
            return false;

        // Check for triangular patterns (3 points forming triangles around a potential target)
        for (int i = 0; i < points.Count - 2; i++)
        {
            for (int j = i + 1; j < points.Count - 1; j++)
            {
                for (int k = j + 1; k < points.Count; k++)
                {
                    var p1 = points[i];
                    var p2 = points[j];
                    var p3 = points[k];

                    // Calculate distances between points
                    var dist12 = CalculateDistance(p1.Lat, p1.Lng, p2.Lat, p2.Lng);
                    var dist13 = CalculateDistance(p1.Lat, p1.Lng, p3.Lat, p3.Lng);
                    var dist23 = CalculateDistance(p2.Lat, p2.Lng, p3.Lat, p3.Lng);

                    // Check if points form a reasonable triangle (not collinear)
                    if (IsValidTriangle(dist12, dist13, dist23) && 
                        AllDistancesInRange(dist12, dist13, dist23))
                    {
                        _logger.LogWarning("Triangular search pattern detected with distances: {Dist12:F2}km, {Dist13:F2}km, {Dist23:F2}km", 
                            dist12, dist13, dist23);
                        return true;
                    }
                }
            }
        }

        // Check for circular patterns (points roughly equidistant from a center)
        return await CheckForCircularPatternAsync(points);
    }

    private async Task<bool> CheckForCircularPatternAsync(List<LocationPoint> points)
    {
        // For circular pattern detection, we need at least 3 points
        if (points.Count < 3)
            return false;

        // This is a simplified circular pattern detection
        // In a real implementation, you might use more sophisticated algorithms
        var avgLat = points.Average(p => p.Lat);
        var avgLng = points.Average(p => p.Lng);

        var distances = points
            .Select(p => CalculateDistance(p.Lat, p.Lng, avgLat, avgLng))
            .ToList();

        // Check if distances from center are similar (within 50% variance)
        var avgDistance = distances.Average();
        var maxVariance = avgDistance * 0.5m;

        var isCircular = distances.All(d => Math.Abs(d - avgDistance) <= maxVariance);

        if (isCircular && avgDistance >= _config.TriangulationMinDistanceKm)
        {
            _logger.LogWarning("Circular search pattern detected with average radius: {AvgDistance:F2}km", avgDistance);
            return true;
        }

        return false;
    }

    private static bool IsValidTriangle(decimal a, decimal b, decimal c)
    {
        // Triangle inequality theorem
        return a + b > c && a + c > b && b + c > a;
    }

    private bool AllDistancesInRange(decimal dist12, decimal dist13, decimal dist23)
    {
        var minDistance = _config.TriangulationMinDistanceKm;
        var maxDistance = minDistance * 20; // Max 20x the minimum distance

        return dist12 >= minDistance && dist12 <= maxDistance &&
               dist13 >= minDistance && dist13 <= maxDistance &&
               dist23 >= minDistance && dist23 <= maxDistance;
    }

    private static decimal CalculateDistance(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
    {
        // Haversine formula for calculating distance between two points
        const decimal earthRadius = 6371; // km

        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);

        var a = (decimal)(Math.Sin((double)(dLat / 2)) * Math.Sin((double)(dLat / 2)) +
                Math.Cos((double)ToRadians(lat1)) * Math.Cos((double)ToRadians(lat2)) *
                Math.Sin((double)(dLng / 2)) * Math.Sin((double)(dLng / 2)));

        var c = (decimal)(2 * Math.Atan2(Math.Sqrt((double)a), Math.Sqrt((double)(1 - a))));

        return earthRadius * c;
    }

    private static decimal ToRadians(decimal degrees)
    {
        return degrees * (decimal)Math.PI / 180;
    }

    private async Task<int> GetSearchCountAsync(string userId, TimeSpan timeWindow)
    {
        var cutoff = DateTime.UtcNow - timeWindow;
        return await _context.LocationSearchLogs
            .CountAsync(log => log.UserId == userId && log.CreatedAt >= cutoff);
    }

    private async Task<int> GetTargetSearchCountAsync(string userId, string targetId, TimeSpan timeWindow)
    {
        var cutoff = DateTime.UtcNow - timeWindow;
        return await _context.LocationSearchLogs
            .CountAsync(log => log.UserId == userId && 
                              log.TargetId == targetId && 
                              log.CreatedAt >= cutoff);
    }

    private async Task<DateTime?> GetLastSearchTimeAsync(string userId)
    {
        var lastSearch = await _context.LocationSearchLogs
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.CreatedAt)
            .Select(log => log.CreatedAt)
            .FirstOrDefaultAsync();

        return lastSearch == default ? null : lastSearch;
    }
}

/// <summary>
/// Simple location point for internal calculations
/// </summary>
internal class LocationPoint
{
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
}