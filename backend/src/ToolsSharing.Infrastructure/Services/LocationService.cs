using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;
using ToolsSharing.Core.DTOs.Location;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Enums;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services;

/// <summary>
/// Comprehensive location service combining geocoding, security, and proximity search operations
/// </summary>
public class LocationService : ILocationService
{
    private readonly IGeocodingService _geocodingService;
    private readonly ILocationSecurityService _securityService;
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LocationService> _logger;

    // Cache keys and durations
    private const string POPULAR_LOCATIONS_CACHE_KEY = "popular_locations";
    private const string SUGGESTIONS_CACHE_KEY_PREFIX = "location_suggestions_";
    private static readonly TimeSpan PopularLocationsCacheDuration = TimeSpan.FromHours(1); // Reduced from 6 hours
    private static readonly TimeSpan SuggestionsCacheDuration = TimeSpan.FromMinutes(30);

    // Earth radius in kilometers for Haversine calculations
    private const decimal EARTH_RADIUS_KM = 6371.0m;

    // Coordinate parsing regex patterns
    private static readonly Regex DecimalDegreesRegex = new(@"^(-?\d+\.?\d*),?\s*(-?\d+\.?\d*)$", RegexOptions.Compiled);
    private static readonly Regex DegreesMinutesSecondsRegex = new(@"^(-?\d+)[°\s]+(-?\d+)['\s]+(-?\d+\.?\d*)[""'\s]*,?\s*(-?\d+)[°\s]+(-?\d+)['\s]+(-?\d+\.?\d*)[""'\s]*$", RegexOptions.Compiled);

    public LocationService(
        IGeocodingService geocodingService,
        ILocationSecurityService securityService,
        ApplicationDbContext context,
        IMemoryCache cache,
        ILogger<LocationService> logger)
    {
        _geocodingService = geocodingService;
        _securityService = securityService;
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    #region Geocoding Operations

    public async Task<List<LocationOption>> SearchLocationsAsync(string query, int limit = 5, string? countryCode = null, string? userId = null)
    {
        try
        {
            // Log the search for security analysis
            if (!string.IsNullOrEmpty(userId))
            {
                await _securityService.LogLocationSearchAsync(userId, null, LocationSearchType.ToolSearch, 
                    null, null, query, null, null);
            }

            // Use geocoding service - request more results to account for deduplication
            var rawResults = await _geocodingService.SearchLocationsAsync(query, limit * 2, countryCode);
            
            // Deduplicate by DisplayName with normalization
            var deduplicatedResults = rawResults
                .GroupBy(r => NormalizeLocationName(r.DisplayName))
                .Select(g => g.OrderByDescending(r => r.Confidence).First()) // Keep highest confidence
                .Take(limit)
                .ToList();
            
            _logger.LogInformation("Location search for '{Query}' returned {RawCount} raw results, {DeduplicatedCount} after deduplication", 
                query, rawResults.Count, deduplicatedResults.Count);
            
            return deduplicatedResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching locations for query: {Query}", query);
            return new List<LocationOption>();
        }
    }

    public async Task<LocationOption?> ReverseGeocodeAsync(decimal lat, decimal lng, string? userId = null)
    {
        try
        {
            // Validate coordinates
            if (!ValidateCoordinates(lat, lng))
            {
                _logger.LogWarning("Invalid coordinates provided: {Lat}, {Lng}", lat, lng);
                return null;
            }

            // Log the search for security analysis
            if (!string.IsNullOrEmpty(userId))
            {
                await _securityService.LogLocationSearchAsync(userId, null, LocationSearchType.ToolSearch, 
                    lat, lng, null, null, null);
            }

            // Use geocoding service
            var result = await _geocodingService.ReverseGeocodeAsync(lat, lng);
            
            if (result != null)
            {
                _logger.LogInformation("Reverse geocoded {Lat}, {Lng} to {DisplayName}", lat, lng, result.DisplayName);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reverse geocoding coordinates: {Lat}, {Lng}", lat, lng);
            return null;
        }
    }

    #endregion

    #region Database Operations

    public async Task<List<LocationOption>> GetPopularLocationsAsync(int limit = 10)
    {
        try
        {
            // Check cache first
            if (_cache.TryGetValue(POPULAR_LOCATIONS_CACHE_KEY, out List<LocationOption>? cached))
            {
                return cached?.Take(limit).ToList() ?? new List<LocationOption>();
            }

            List<LocationOption> results;

            // HYBRID APPROACH: Different code paths for testing vs production
            // - Testing (In-Memory DB): Uses memory-based grouping to work around EF Core in-memory provider limitations
            // - Production (Real DB): Uses database-level grouping for optimal performance and scalability
            if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                // In-memory database: Use memory-based grouping (for tests)
                _logger.LogDebug("Using in-memory grouping strategy for popular locations");
                
                var allTools = await _context.Tools
                    .Where(t => !string.IsNullOrEmpty(t.LocationDisplay) && t.LocationLat.HasValue && t.LocationLng.HasValue)
                    .ToListAsync();
                
                // Group only by normalized display name, aggregate other fields
                var popularLocations = allTools
                    .GroupBy(t => NormalizeLocationName(t.LocationDisplay))
                    .Select(g => new { 
                        NormalizedName = g.Key,
                        Count = g.Count(),
                        // Pick the most complete location data
                        BestTool = g.OrderByDescending(t => 
                            (string.IsNullOrEmpty(t.LocationCity) ? 0 : 1) +
                            (string.IsNullOrEmpty(t.LocationState) ? 0 : 1) +
                            (string.IsNullOrEmpty(t.LocationCountry) ? 0 : 1)
                        ).First()
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(limit)
                    .ToList();

                results = popularLocations.Select(p => new LocationOption
                {
                    DisplayName = p.BestTool.LocationDisplay ?? "Unknown Location",
                    City = p.BestTool.LocationCity,
                    State = p.BestTool.LocationState,
                    Country = p.BestTool.LocationCountry,
                    Lat = p.BestTool.LocationLat,
                    Lng = p.BestTool.LocationLng,
                    Source = LocationSource.Manual,
                    Confidence = Math.Min(1.0m, p.Count / 5.0m) // Adjusted confidence formula
                }).ToList();
            }
            else
            {
                // Real database: Use simplified grouping and post-process normalization
                _logger.LogDebug("Using database-level grouping strategy for popular locations");
                
                // First get all tools, then group in memory for consistency with in-memory approach
                var allTools = await _context.Tools
                    .Where(t => !string.IsNullOrEmpty(t.LocationDisplay) && t.LocationLat.HasValue && t.LocationLng.HasValue)
                    .ToListAsync();
                
                // Group only by normalized display name, aggregate other fields
                var popularLocations = allTools
                    .GroupBy(t => NormalizeLocationName(t.LocationDisplay))
                    .Select(g => new { 
                        NormalizedName = g.Key,
                        Count = g.Count(),
                        // Pick the most complete location data
                        BestTool = g.OrderByDescending(t => 
                            (string.IsNullOrEmpty(t.LocationCity) ? 0 : 1) +
                            (string.IsNullOrEmpty(t.LocationState) ? 0 : 1) +
                            (string.IsNullOrEmpty(t.LocationCountry) ? 0 : 1)
                        ).First()
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(limit)
                    .ToList();

                results = popularLocations.Select(p => new LocationOption
                {
                    DisplayName = p.BestTool.LocationDisplay ?? "Unknown Location",
                    City = p.BestTool.LocationCity,
                    State = p.BestTool.LocationState,
                    Country = p.BestTool.LocationCountry,
                    Lat = p.BestTool.LocationLat,
                    Lng = p.BestTool.LocationLng,
                    Source = LocationSource.Manual,
                    Confidence = Math.Min(1.0m, p.Count / 5.0m) // Adjusted confidence formula
                }).ToList();
            }

            // Cache the results
            _cache.Set(POPULAR_LOCATIONS_CACHE_KEY, results, PopularLocationsCacheDuration);
            
            _logger.LogInformation("Retrieved {Count} popular locations from database using {Strategy} strategy", 
                results.Count, 
                _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory" ? "in-memory" : "database-level");
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving popular locations from database");
            return new List<LocationOption>();
        }
    }

    public async Task<List<LocationOption>> GetLocationSuggestionsAsync(string query, int limit = 5, string? userId = null)
    {
        try
        {
            var cacheKey = $"{SUGGESTIONS_CACHE_KEY_PREFIX}{query.ToLowerInvariant()}_{limit}";
            
            // Check cache first
            if (_cache.TryGetValue(cacheKey, out List<LocationOption>? cached))
            {
                return cached ?? new List<LocationOption>();
            }

            var suggestions = new List<LocationOption>();

            // Get database suggestions first (faster)
            var dbSuggestions = await GetDatabaseLocationSuggestions(query, limit / 2);
            suggestions.AddRange(dbSuggestions);

            // Get geocoding suggestions if we need more
            if (suggestions.Count < limit)
            {
                var rawGeocodingSuggestions = await _geocodingService.GetLocationSuggestionsAsync(query, (limit - suggestions.Count) * 2);
                
                // Deduplicate geocoding results first
                var deduplicatedGeocoding = rawGeocodingSuggestions
                    .GroupBy(s => NormalizeLocationName(s.DisplayName))
                    .Select(g => g.OrderByDescending(s => s.Confidence).First())
                    .ToList();
                
                // Then check against existing suggestions
                foreach (var suggestion in deduplicatedGeocoding)
                {
                    if (!suggestions.Any(s => IsSimilarLocation(s, suggestion)))
                    {
                        suggestions.Add(suggestion);
                        if (suggestions.Count >= limit) break;
                    }
                }
            }

            // Take only the requested limit
            var results = suggestions.Take(limit).ToList();

            // Cache the results
            _cache.Set(cacheKey, results, SuggestionsCacheDuration);
            
            _logger.LogInformation("Retrieved {Count} location suggestions for query: {Query}", results.Count, query);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location suggestions for query: {Query}", query);
            return new List<LocationOption>();
        }
    }

    #endregion

    #region Location Processing

    public async Task<LocationOption?> ProcessLocationInputAsync(string? locationInput, string? fallbackLocation = null)
    {
        try
        {
            // Try to parse as coordinates first
            var coordinates = ParseCoordinates(locationInput ?? "");
            if (coordinates.HasValue)
            {
                // Reverse geocode the coordinates
                return await ReverseGeocodeAsync(coordinates.Value.lat, coordinates.Value.lng);
            }

            // Try to geocode as address/place name
            if (!string.IsNullOrWhiteSpace(locationInput))
            {
                var searchResults = await SearchLocationsAsync(locationInput, 1);
                if (searchResults.Any())
                {
                    return searchResults.First();
                }
            }

            // Try fallback location
            if (!string.IsNullOrWhiteSpace(fallbackLocation))
            {
                var fallbackResults = await SearchLocationsAsync(fallbackLocation, 1);
                if (fallbackResults.Any())
                {
                    return fallbackResults.First();
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing location input: {LocationInput}", locationInput);
            return null;
        }
    }

    public (decimal lat, decimal lng)? ParseCoordinates(string coordinateString)
    {
        if (string.IsNullOrWhiteSpace(coordinateString))
            return null;

        try
        {
            // Try decimal degrees format: "40.7128, -74.0060" or "40.7128 -74.0060"
            var decimalMatch = DecimalDegreesRegex.Match(coordinateString.Trim());
            if (decimalMatch.Success)
            {
                if (decimal.TryParse(decimalMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
                    decimal.TryParse(decimalMatch.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
                {
                    if (ValidateCoordinates(lat, lng))
                    {
                        return (lat, lng);
                    }
                }
            }

            // Try degrees, minutes, seconds format: "40°42'46.0"N 74°00'21.6"W"
            var dmsMatch = DegreesMinutesSecondsRegex.Match(coordinateString.Trim());
            if (dmsMatch.Success)
            {
                var latDegrees = int.Parse(dmsMatch.Groups[1].Value);
                var latMinutes = int.Parse(dmsMatch.Groups[2].Value);
                var latSeconds = decimal.Parse(dmsMatch.Groups[3].Value, CultureInfo.InvariantCulture);
                
                var lngDegrees = int.Parse(dmsMatch.Groups[4].Value);
                var lngMinutes = int.Parse(dmsMatch.Groups[5].Value);
                var lngSeconds = decimal.Parse(dmsMatch.Groups[6].Value, CultureInfo.InvariantCulture);

                var lat = Math.Abs(latDegrees) + (latMinutes / 60.0m) + (latSeconds / 3600.0m);
                if (latDegrees < 0) lat = -lat;
                
                var lng = Math.Abs(lngDegrees) + (lngMinutes / 60.0m) + (lngSeconds / 3600.0m);
                if (lngDegrees < 0) lng = -lng;

                if (ValidateCoordinates(lat, lng))
                {
                    return (lat, lng);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing coordinates: {CoordinateString}", coordinateString);
            return null;
        }
    }

    public bool ValidateCoordinates(decimal lat, decimal lng)
    {
        return lat >= -90.0m && lat <= 90.0m && lng >= -180.0m && lng <= 180.0m;
    }

    #endregion

    #region Proximity Search

    public async Task<List<NearbyToolDto>> FindNearbyToolsAsync(decimal centerLat, decimal centerLng, decimal radiusKm, 
        string? userId = null, int limit = 50, string? userAgent = null, string? ipAddress = null)
    {
        try
        {
            // Validate inputs
            if (!ValidateCoordinates(centerLat, centerLng) || radiusKm <= 0 || radiusKm > 1000)
            {
                throw new ArgumentException("Invalid search parameters");
            }

            // Security validation
            await ValidateLocationSearchAsync(userId, null, LocationSearchType.ToolSearch, 
                centerLat, centerLng, null, userAgent, ipAddress);

            // Calculate bounding box for efficient querying
            var bounds = CalculateBoundingBox(centerLat, centerLng, radiusKm);

            // Query tools within bounding box
            var tools = await _context.Tools
                .Include(t => t.Owner)
                .Where(t => t.IsApproved && t.IsAvailable && 
                           t.LocationLat.HasValue && t.LocationLng.HasValue &&
                           t.LocationLat >= bounds.SouthLat && t.LocationLat <= bounds.NorthLat &&
                           t.LocationLng >= bounds.WestLng && t.LocationLng <= bounds.EastLng)
                .ToListAsync();

            // Calculate exact distances and filter
            var nearbyTools = new List<NearbyToolDto>();
            
            foreach (var tool in tools)
            {
                var distance = CalculateDistance(centerLat, centerLng, tool.LocationLat!.Value, tool.LocationLng!.Value);
                
                if (distance <= radiusKm)
                {
                    var distanceBand = _securityService.GetDistanceBand(distance);
                    
                    nearbyTools.Add(new NearbyToolDto
                    {
                        Id = tool.Id,
                        Name = tool.Name,
                        Description = tool.Description,
                        DailyRate = tool.DailyRate,
                        Condition = tool.Condition,
                        Category = tool.Category,
                        ImageUrls = tool.Images?.Select(i => i.ImageUrl ?? "").ToList() ?? new List<string>(),
                        OwnerName = $"{tool.Owner?.FirstName} {tool.Owner?.LastName}".Trim() ?? "Unknown",
                        LocationDisplay = tool.LocationDisplay ?? tool.Owner?.LocationDisplay ?? "Location not specified",
                        DistanceBand = distanceBand,
                        DistanceText = _securityService.GetDistanceBandText(distanceBand),
                        IsAvailable = tool.IsAvailable,
                        AverageRating = 0, // TODO: Calculate from reviews
                        ReviewCount = 0    // TODO: Calculate from reviews
                    });
                }
            }

            // Sort by distance band and take limit
            var results = nearbyTools
                .OrderBy(t => t.DistanceBand)
                .ThenBy(t => t.Name)
                .Take(limit)
                .ToList();

            _logger.LogInformation("Found {Count} nearby tools within {Radius}km of {Lat}, {Lng}", 
                results.Count, radiusKm, centerLat, centerLng);
            
            return results;
        }
        catch (ArgumentException)
        {
            // Rethrow validation errors
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding nearby tools at {Lat}, {Lng} within {Radius}km", 
                centerLat, centerLng, radiusKm);
            return new List<NearbyToolDto>();
        }
    }

    public async Task<List<NearbyBundleDto>> FindNearbyBundlesAsync(decimal centerLat, decimal centerLng, decimal radiusKm, 
        string? userId = null, int limit = 50, string? userAgent = null, string? ipAddress = null)
    {
        try
        {
            // Validate inputs
            if (!ValidateCoordinates(centerLat, centerLng) || radiusKm <= 0 || radiusKm > 1000)
            {
                throw new ArgumentException("Invalid search parameters");
            }

            // Security validation
            await ValidateLocationSearchAsync(userId, null, LocationSearchType.BundleSearch, 
                centerLat, centerLng, null, userAgent, ipAddress);

            // Calculate bounding box for efficient querying
            var bounds = CalculateBoundingBox(centerLat, centerLng, radiusKm);

            // Query bundles within bounding box
            var bundles = await _context.Bundles
                .Include(b => b.User)
                .Where(b => b.IsPublished && 
                           b.LocationLat.HasValue && b.LocationLng.HasValue &&
                           b.LocationLat >= bounds.SouthLat && b.LocationLat <= bounds.NorthLat &&
                           b.LocationLng >= bounds.WestLng && b.LocationLng <= bounds.EastLng)
                .ToListAsync();

            // Calculate exact distances and filter
            var nearbyBundles = new List<NearbyBundleDto>();
            
            foreach (var bundle in bundles)
            {
                var distance = CalculateDistance(centerLat, centerLng, bundle.LocationLat!.Value, bundle.LocationLng!.Value);
                
                if (distance <= radiusKm)
                {
                    var distanceBand = _securityService.GetDistanceBand(distance);
                    
                    // Calculate bundle properties
                    var toolCount = await _context.BundleTools.CountAsync(bt => bt.BundleId == bundle.Id);
                    var tools = await _context.BundleTools
                        .Include(bt => bt.Tool)
                        .Where(bt => bt.BundleId == bundle.Id)
                        .Select(bt => bt.Tool)
                        .ToListAsync();
                    
                    var originalCost = tools.Sum(t => t.DailyRate);
                    var discountedCost = originalCost * (1 - (bundle.BundleDiscount / 100));
                    
                    nearbyBundles.Add(new NearbyBundleDto
                    {
                        Id = bundle.Id,
                        Name = bundle.Name,
                        Description = bundle.Description,
                        Category = bundle.Category,
                        ImageUrl = bundle.ImageUrl,
                        ToolCount = toolCount,
                        OriginalCost = originalCost,
                        DiscountedCost = discountedCost,
                        DiscountPercentage = bundle.BundleDiscount,
                        OwnerName = $"{bundle.User?.FirstName} {bundle.User?.LastName}".Trim() ?? "Unknown",
                        LocationDisplay = bundle.LocationDisplay ?? bundle.User?.LocationDisplay ?? "Location not specified",
                        DistanceBand = distanceBand,
                        DistanceText = _securityService.GetDistanceBandText(distanceBand),
                        IsAvailable = bundle.IsPublished,
                        AverageRating = 0, // TODO: Calculate from reviews
                        ReviewCount = 0    // TODO: Calculate from reviews
                    });
                }
            }

            // Sort by distance band and take limit
            var results = nearbyBundles
                .OrderBy(b => b.DistanceBand)
                .ThenBy(b => b.Name)
                .Take(limit)
                .ToList();

            _logger.LogInformation("Found {Count} nearby bundles within {Radius}km of {Lat}, {Lng}", 
                results.Count, radiusKm, centerLat, centerLng);
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding nearby bundles at {Lat}, {Lng} within {Radius}km", 
                centerLat, centerLng, radiusKm);
            return new List<NearbyBundleDto>();
        }
    }

    public async Task<List<NearbyUserDto>> FindNearbyUsersAsync(decimal centerLat, decimal centerLng, decimal radiusKm, 
        string? userId = null, int limit = 50, string? userAgent = null, string? ipAddress = null)
    {
        try
        {
            // Validate inputs
            if (!ValidateCoordinates(centerLat, centerLng) || radiusKm <= 0 || radiusKm > 1000)
            {
                throw new ArgumentException("Invalid search parameters");
            }

            // Security validation
            await ValidateLocationSearchAsync(userId, null, LocationSearchType.UserSearch, 
                centerLat, centerLng, null, userAgent, ipAddress);

            // Calculate bounding box for efficient querying
            var bounds = CalculateBoundingBox(centerLat, centerLng, radiusKm);

            // Query users within bounding box
            var users = await _context.Users
                .Where(u => u.LocationLat.HasValue && u.LocationLng.HasValue &&
                           u.LocationLat >= bounds.SouthLat && u.LocationLat <= bounds.NorthLat &&
                           u.LocationLng >= bounds.WestLng && u.LocationLng <= bounds.EastLng)
                .ToListAsync();

            // Calculate exact distances and filter
            var nearbyUsers = new List<NearbyUserDto>();
            
            foreach (var user in users)
            {
                // Skip the searching user
                if (user.Id == userId)
                    continue;

                var distance = CalculateDistance(centerLat, centerLng, user.LocationLat!.Value, user.LocationLng!.Value);
                
                if (distance <= radiusKm)
                {
                    var distanceBand = _securityService.GetDistanceBand(distance);
                    
                    // Get user stats
                    var toolCount = await _context.Tools.CountAsync(t => t.OwnerId == user.Id && t.IsApproved);
                    var bundleCount = await _context.Bundles.CountAsync(b => b.UserId == user.Id && b.IsPublished);
                    
                    nearbyUsers.Add(new NearbyUserDto
                    {
                        Id = user.Id,
                        Name = $"{user.FirstName} {user.LastName}".Trim(),
                        Bio = null, // User entity doesn't have Bio field
                        AvatarUrl = user.ProfilePictureUrl,
                        LocationDisplay = user.LocationDisplay ?? "Location not specified",
                        DistanceBand = distanceBand,
                        DistanceText = _securityService.GetDistanceBandText(distanceBand),
                        ToolCount = toolCount,
                        BundleCount = bundleCount,
                        AverageRating = 0, // TODO: Calculate from reviews
                        ReviewCount = 0,   // TODO: Calculate from reviews
                        IsActive = true,   // TODO: Implement user activity tracking
                        LastSeen = null    // TODO: Implement last seen tracking
                    });
                }
            }

            // Sort by distance band and take limit
            var results = nearbyUsers
                .OrderBy(u => u.DistanceBand)
                .ThenBy(u => u.Name)
                .Take(limit)
                .ToList();

            _logger.LogInformation("Found {Count} nearby users within {Radius}km of {Lat}, {Lng}", 
                results.Count, radiusKm, centerLat, centerLng);
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding nearby users at {Lat}, {Lng} within {Radius}km", 
                centerLat, centerLng, radiusKm);
            return new List<NearbyUserDto>();
        }
    }

    #endregion

    #region Security Integration

    public async Task<bool> ValidateLocationSearchAsync(string? userId, string? targetId, LocationSearchType searchType,
        decimal? searchLat = null, decimal? searchLng = null, string? searchQuery = null, 
        string? userAgent = null, string? ipAddress = null)
    {
        try
        {
            // Check rate limiting first
            var isAllowed = await _securityService.ValidateLocationSearchAsync(userId, targetId);
            if (!isAllowed)
            {
                throw new InvalidOperationException("Rate limit exceeded for location searches");
            }

            // Check for triangulation attempts
            var isTriangulation = await _securityService.IsTriangulationAttemptAsync(
                userId, targetId, searchType, searchLat, searchLng, searchQuery);
            
            if (isTriangulation)
            {
                throw new InvalidOperationException("Suspicious search pattern detected");
            }

            // Log the search
            await _securityService.LogLocationSearchAsync(userId, targetId, searchType, 
                searchLat, searchLng, searchQuery, userAgent, ipAddress);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Location search validation failed for user {UserId}", userId);
            throw;
        }
    }

    #endregion

    #region Distance Calculations

    public decimal CalculateDistance(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
    {
        // Haversine formula for calculating distance between two points on Earth
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        
        var a = Math.Sin((double)(dLat / 2)) * Math.Sin((double)(dLat / 2)) +
                Math.Cos(ToRadians((double)lat1)) * Math.Cos(ToRadians((double)lat2)) *
                Math.Sin((double)(dLng / 2)) * Math.Sin((double)(dLng / 2));
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return (decimal)(EARTH_RADIUS_KM * (decimal)c);
    }

    public DistanceBand GetDistanceBand(decimal distanceKm)
    {
        return _securityService.GetDistanceBand(distanceKm);
    }

    public string GetDistanceBandText(DistanceBand distanceBand)
    {
        return _securityService.GetDistanceBandText(distanceBand);
    }

    #endregion

    #region Geographic Clustering

    public async Task<List<LocationCluster>> AnalyzeGeographicClustersAsync(List<LocationOption> locations, decimal clusterRadius = 5.0m)
    {
        try
        {
            if (!locations.Any())
                return new List<LocationCluster>();

            var clusters = new List<LocationCluster>();
            var unprocessed = locations.Where(l => l.Lat.HasValue && l.Lng.HasValue).ToList();

            while (unprocessed.Any())
            {
                var seed = unprocessed.First();
                var clusterLocations = new List<LocationOption> { seed };
                unprocessed.Remove(seed);

                // Find all locations within cluster radius
                var toRemove = new List<LocationOption>();
                foreach (var location in unprocessed)
                {
                    var distance = CalculateDistance(seed.Lat!.Value, seed.Lng!.Value, 
                        location.Lat!.Value, location.Lng!.Value);
                    
                    if (distance <= clusterRadius)
                    {
                        clusterLocations.Add(location);
                        toRemove.Add(location);
                    }
                }

                // Remove clustered locations from unprocessed
                foreach (var location in toRemove)
                {
                    unprocessed.Remove(location);
                }

                // Calculate cluster center
                var centerLat = clusterLocations.Average(l => l.Lat!.Value);
                var centerLng = clusterLocations.Average(l => l.Lng!.Value);

                // Calculate cluster bounds
                var bounds = new LocationBounds
                {
                    NorthLat = clusterLocations.Max(l => l.Lat!.Value),
                    SouthLat = clusterLocations.Min(l => l.Lat!.Value),
                    EastLng = clusterLocations.Max(l => l.Lng!.Value),
                    WestLng = clusterLocations.Min(l => l.Lng!.Value)
                };

                // Calculate cluster area and density
                var latRange = bounds.NorthLat - bounds.SouthLat;
                var lngRange = bounds.EastLng - bounds.WestLng;
                var areaKm2 = latRange * lngRange * 111.32m * 111.32m; // Rough conversion to km²
                var density = areaKm2 > 0 ? clusterLocations.Count / areaKm2 : clusterLocations.Count;

                // Generate cluster name from most common city/area
                var clusterName = GetClusterName(clusterLocations);

                clusters.Add(new LocationCluster
                {
                    CenterLat = centerLat,
                    CenterLng = centerLng,
                    RadiusKm = clusterRadius,
                    LocationCount = clusterLocations.Count,
                    Locations = clusterLocations,
                    ClusterName = clusterName,
                    DensityScore = density,
                    Bounds = bounds
                });
            }

            _logger.LogInformation("Analyzed {LocationCount} locations into {ClusterCount} clusters", 
                locations.Count, clusters.Count);

            return clusters.OrderByDescending(c => c.LocationCount).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing geographic clusters");
            return new List<LocationCluster>();
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task<List<LocationOption>> GetDatabaseLocationSuggestions(string query, int limit)
    {
        var searchTerm = query.ToLowerInvariant();
        
        // HYBRID APPROACH: Different code paths for testing vs production
        // - Testing (In-Memory DB): Uses memory-based grouping to work around EF Core in-memory provider limitations
        // - Production (Real DB): Uses database-level grouping for optimal performance and scalability
        if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            // In-memory database: Use memory-based grouping (for tests)
            var matchingTools = await _context.Tools
                .Where(t => !string.IsNullOrEmpty(t.LocationDisplay) && 
                           (t.LocationDisplay.ToLower().Contains(searchTerm) ||
                            (t.LocationCity != null && t.LocationCity.ToLower().Contains(searchTerm)) ||
                            (t.LocationState != null && t.LocationState.ToLower().Contains(searchTerm))))
                .ToListAsync();
            
            var dbResults = matchingTools
                .GroupBy(t => new { t.LocationDisplay, t.LocationCity, t.LocationState, t.LocationCountry })
                .Select(g => new { 
                    Location = g.Key, 
                    Count = g.Count(), 
                    FirstTool = g.First() 
                })
                .OrderByDescending(x => x.Count)
                .Take(limit)
                .ToList();

            var locationOptions = new List<LocationOption>();
            
            foreach (var r in dbResults)
            {
                var locationOption = new LocationOption
                {
                    DisplayName = r.Location.LocationDisplay ?? "Unknown",
                    City = r.Location.LocationCity,
                    State = r.Location.LocationState,
                    Country = r.Location.LocationCountry,
                    Lat = r.FirstTool.LocationLat,
                    Lng = r.FirstTool.LocationLng,
                    Source = LocationSource.Manual,
                    Confidence = Math.Min(1.0m, r.Count / 5.0m)
                };
                
                // If coordinates are missing, try to geocode the location
                if (!locationOption.Lat.HasValue || !locationOption.Lng.HasValue || 
                    (locationOption.Lat == 0 && locationOption.Lng == 0))
                {
                    try
                    {
                        var geocodedResults = await _geocodingService.SearchLocationsAsync(locationOption.DisplayName, 1);
                        var geocoded = geocodedResults?.FirstOrDefault();
                        if (geocoded != null && geocoded.Lat.HasValue && geocoded.Lng.HasValue)
                        {
                            locationOption.Lat = geocoded.Lat;
                            locationOption.Lng = geocoded.Lng;
                            locationOption.Source = LocationSource.OpenStreetMap; // Updated source since it's now geocoded
                            locationOption.Confidence = Math.Max(locationOption.Confidence, geocoded.Confidence);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to geocode database location: {DisplayName}", locationOption.DisplayName);
                    }
                }
                
                // Only add locations that have valid coordinates
                if (locationOption.Lat.HasValue && locationOption.Lng.HasValue && 
                    locationOption.Lat != 0 && locationOption.Lng != 0)
                {
                    locationOptions.Add(locationOption);
                }
            }
            
            return locationOptions;
        }
        else
        {
            // Real database: Use database-level grouping (for production performance)
            var dbResults = await _context.Tools
                .Where(t => !string.IsNullOrEmpty(t.LocationDisplay) && 
                           (t.LocationDisplay.ToLower().Contains(searchTerm) ||
                            (t.LocationCity != null && t.LocationCity.ToLower().Contains(searchTerm)) ||
                            (t.LocationState != null && t.LocationState.ToLower().Contains(searchTerm))))
                .GroupBy(t => new { t.LocationDisplay, t.LocationCity, t.LocationState, t.LocationCountry })
                .Select(g => new { Location = g.Key, Count = g.Count(), FirstTool = g.First() })
                .OrderByDescending(x => x.Count)
                .Take(limit)
                .ToListAsync();

            var locationOptions = new List<LocationOption>();
            
            foreach (var r in dbResults)
            {
                var locationOption = new LocationOption
                {
                    DisplayName = r.Location.LocationDisplay ?? "Unknown",
                    City = r.Location.LocationCity,
                    State = r.Location.LocationState,
                    Country = r.Location.LocationCountry,
                    Lat = r.FirstTool.LocationLat,
                    Lng = r.FirstTool.LocationLng,
                    Source = LocationSource.Manual,
                    Confidence = Math.Min(1.0m, r.Count / 5.0m)
                };
                
                // If coordinates are missing, try to geocode the location
                if (!locationOption.Lat.HasValue || !locationOption.Lng.HasValue || 
                    (locationOption.Lat == 0 && locationOption.Lng == 0))
                {
                    try
                    {
                        var geocodedResults = await _geocodingService.SearchLocationsAsync(locationOption.DisplayName, 1);
                        var geocoded = geocodedResults?.FirstOrDefault();
                        if (geocoded != null && geocoded.Lat.HasValue && geocoded.Lng.HasValue)
                        {
                            locationOption.Lat = geocoded.Lat;
                            locationOption.Lng = geocoded.Lng;
                            locationOption.Source = LocationSource.OpenStreetMap; // Updated source since it's now geocoded
                            locationOption.Confidence = Math.Max(locationOption.Confidence, geocoded.Confidence);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to geocode database location: {DisplayName}", locationOption.DisplayName);
                    }
                }
                
                // Only add locations that have valid coordinates
                if (locationOption.Lat.HasValue && locationOption.Lng.HasValue && 
                    locationOption.Lat != 0 && locationOption.Lng != 0)
                {
                    locationOptions.Add(locationOption);
                }
            }
            
            return locationOptions;
        }
    }

    private static string NormalizeLocationName(string name)
    {
        return name?.Trim().ToLowerInvariant()
            .Replace(",", "")
            .Replace("  ", " ") ?? "";
    }

    private static decimal CalculateHaversineDistance(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
    {
        const decimal R = 6371.0m; // Earth radius in kilometers

        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);

        var a = (decimal)(Math.Sin((double)dLat / 2) * Math.Sin((double)dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin((double)dLng / 2) * Math.Sin((double)dLng / 2));

        var c = 2 * (decimal)Math.Atan2(Math.Sqrt((double)a), Math.Sqrt(1 - (double)a));

        return R * c;
    }

    private static bool IsSimilarLocation(LocationOption loc1, LocationOption loc2)
    {
        // Exact normalized match
        if (NormalizeLocationName(loc1.DisplayName) == NormalizeLocationName(loc2.DisplayName))
            return true;
        
        // Geographic proximity check (if both have coordinates)
        if (loc1.Lat.HasValue && loc1.Lng.HasValue && loc2.Lat.HasValue && loc2.Lng.HasValue)
        {
            var distance = CalculateHaversineDistance(
                loc1.Lat.Value, loc1.Lng.Value,
                loc2.Lat.Value, loc2.Lng.Value);
            
            // Same location if within 1km and similar names
            if (distance < 1.0m)
            {
                var nameSimilarity = CalculateStringSimilarity(
                    NormalizeLocationName(loc1.DisplayName), 
                    NormalizeLocationName(loc2.DisplayName));
                return nameSimilarity > 0.6; // Lower threshold for geographically close locations
            }
        }
        
        // Fallback to string similarity (keep existing logic)
        var similarity = CalculateStringSimilarity(
            NormalizeLocationName(loc1.DisplayName), 
            NormalizeLocationName(loc2.DisplayName));
        return similarity > 0.8;
    }

    private static double CalculateStringSimilarity(string s1, string s2)
    {
        if (s1 == s2) return 1.0;
        if (s1.Length == 0 || s2.Length == 0) return 0.0;

        var longer = s1.Length > s2.Length ? s1 : s2;
        var shorter = s1.Length > s2.Length ? s2 : s1;

        return (longer.Length - LevenshteinDistance(longer, shorter)) / (double)longer.Length;
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++) matrix[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++) matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1), matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[s1.Length, s2.Length];
    }

    private LocationBounds CalculateBoundingBox(decimal centerLat, decimal centerLng, decimal radiusKm)
    {
        // Approximate degree calculations (more accurate methods available for production)
        var latDelta = radiusKm / 111.32m; // ~111.32 km per degree of latitude
        var lngDelta = radiusKm / (111.32m * (decimal)Math.Cos(ToRadians((double)centerLat)));

        return new LocationBounds
        {
            NorthLat = centerLat + latDelta,
            SouthLat = centerLat - latDelta,
            EastLng = centerLng + lngDelta,
            WestLng = centerLng - lngDelta
        };
    }

    private static string GetClusterName(List<LocationOption> locations)
    {
        // Find most common city or area name
        var cityGroups = locations
            .Where(l => !string.IsNullOrEmpty(l.City))
            .GroupBy(l => l.City)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        if (cityGroups != null)
        {
            var city = cityGroups.Key;
            var states = cityGroups.Select(l => l.State).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
            if (states.Count == 1)
            {
                return $"{city}, {states.First()}";
            }
            return city!;
        }

        // Fallback to area or display name
        var areaGroups = locations
            .Where(l => !string.IsNullOrEmpty(l.Area))
            .GroupBy(l => l.Area)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        return areaGroups?.Key ?? "Location Cluster";
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    private static double ToRadians(decimal degrees)
    {
        return (double)degrees * Math.PI / 180.0;
    }

    #endregion

    #region Cache Management

    /// <summary>
    /// Invalidate location caches when new tools are added or location data changes
    /// </summary>
    public async Task InvalidateLocationCacheAsync()
    {
        try
        {
            _cache.Remove(POPULAR_LOCATIONS_CACHE_KEY);
            
            // Remove all suggestion caches
            var cacheField = _cache.GetType().GetField("_coherentState", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (cacheField?.GetValue(_cache) is IDictionary<object, object> cacheDict)
            {
                var keysToRemove = cacheDict.Keys
                    .Where(key => key.ToString()?.StartsWith(SUGGESTIONS_CACHE_KEY_PREFIX) == true)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                }
                
                _logger.LogInformation("Invalidated location cache: removed popular locations and {Count} suggestion cache entries", keysToRemove.Count);
            }
            else
            {
                _logger.LogInformation("Invalidated location cache: removed popular locations cache");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while invalidating location cache");
        }

        await Task.CompletedTask;
    }

    #endregion
}