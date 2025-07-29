using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using ToolsSharing.Core.Configuration;
using ToolsSharing.Core.DTOs.Location;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services;

/// <summary>
/// OpenStreetMap Nominatim geocoding service implementation
/// </summary>
public class OpenStreetMapGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OpenStreetMapGeocodingService> _logger;
    private readonly OpenStreetMapConfiguration _config;
    private readonly SemaphoreSlim _rateLimiter;
    private DateTime _lastRequestTime = DateTime.MinValue;

    public string ProviderName => "OpenStreetMap";

    public OpenStreetMapGeocodingService(
        HttpClient httpClient,
        ApplicationDbContext context,
        IMemoryCache cache,
        ILogger<OpenStreetMapGeocodingService> logger,
        OpenStreetMapConfiguration config)
    {
        _httpClient = httpClient;
        _context = context;
        _cache = cache;
        _logger = logger;
        _config = config;
        _rateLimiter = new SemaphoreSlim(1, 1);

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", _config.UserAgent);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
    }

    public async Task<List<LocationOption>> SearchLocationsAsync(string query, int limit = 5, string? countryCode = null)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<LocationOption>();

        var cacheKey = $"osm_search:{query}:{limit}:{countryCode}";
        
        // Check cache first
        if (_cache.TryGetValue(cacheKey, out List<LocationOption>? cachedResults))
        {
            _logger.LogDebug("Returning cached results for query: {Query}", query);
            return cachedResults!;
        }

        try
        {
            await EnforceRateLimit();

            var queryParams = new StringBuilder();
            queryParams.Append($"q={Uri.EscapeDataString(query)}");
            queryParams.Append("&format=json");
            queryParams.Append("&addressdetails=1");
            queryParams.Append("&extratags=1");
            queryParams.Append("&namedetails=1");
            queryParams.Append($"&limit={Math.Min(limit, 50)}"); // Cap at 50
            queryParams.Append($"&accept-language={_config.DefaultLanguage}");

            if (!string.IsNullOrEmpty(countryCode))
                queryParams.Append($"&countrycodes={countryCode.ToLowerInvariant()}");

            var requestUrl = $"/search?{queryParams}";
            _logger.LogDebug("Making Nominatim request: {Url}", requestUrl);

            var response = await _httpClient.GetAsync(requestUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Nominatim API error: {StatusCode} - {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
                return new List<LocationOption>();
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var nominatimResults = JsonSerializer.Deserialize<List<NominatimResult>>(jsonContent);

            if (nominatimResults == null || !nominatimResults.Any())
            {
                _logger.LogDebug("No results found for query: {Query}", query);
                return new List<LocationOption>();
            }

            // Convert to LocationOptions and apply disambiguation
            var locationOptions = nominatimResults
                .Select(result => result.ToLocationOption())
                .Where(option => !string.IsNullOrEmpty(option.DisplayName))
                .ToList();

            // Apply location disambiguation
            locationOptions = await ApplyLocationDisambiguationAsync(locationOptions, null);

            // Cache the results
            var cacheExpiry = TimeSpan.FromHours(_config.CacheDurationHours);
            _cache.Set(cacheKey, locationOptions, cacheExpiry);

            _logger.LogDebug("Found {Count} locations for query: {Query}", locationOptions.Count, query);
            return locationOptions;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Nominatim API for query: {Query}", query);
            return new List<LocationOption>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error from Nominatim API for query: {Query}", query);
            return new List<LocationOption>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Nominatim search for query: {Query}", query);
            return new List<LocationOption>();
        }
    }

    public async Task<LocationOption?> ReverseGeocodeAsync(decimal lat, decimal lng)
    {
        var cacheKey = $"osm_reverse:{lat:F6}:{lng:F6}";
        
        // Check cache first
        if (_cache.TryGetValue(cacheKey, out LocationOption? cachedResult))
        {
            _logger.LogDebug("Returning cached reverse geocoding result for: {Lat}, {Lng}", lat, lng);
            return cachedResult;
        }

        try
        {
            await EnforceRateLimit();

            var queryParams = new StringBuilder();
            queryParams.Append($"lat={lat}");
            queryParams.Append($"&lon={lng}");
            queryParams.Append("&format=json");
            queryParams.Append("&addressdetails=1");
            queryParams.Append("&extratags=1");
            queryParams.Append("&namedetails=1");
            queryParams.Append("&zoom=18"); // High detail level
            queryParams.Append($"&accept-language={_config.DefaultLanguage}");

            var requestUrl = $"/reverse?{queryParams}";
            _logger.LogDebug("Making Nominatim reverse request: {Url}", requestUrl);

            var response = await _httpClient.GetAsync(requestUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Nominatim reverse API error: {StatusCode} - {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
                return null;
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var nominatimResult = JsonSerializer.Deserialize<NominatimResult>(jsonContent);

            if (nominatimResult == null)
            {
                _logger.LogDebug("No reverse geocoding result found for: {Lat}, {Lng}", lat, lng);
                return null;
            }

            var locationOption = nominatimResult.ToLocationOption();

            // Cache the result
            var cacheExpiry = TimeSpan.FromHours(_config.CacheDurationHours);
            _cache.Set(cacheKey, locationOption, cacheExpiry);

            _logger.LogDebug("Reverse geocoded {Lat},{Lng} to: {DisplayName}", lat, lng, locationOption.DisplayName);
            return locationOption;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error in reverse geocoding for: {Lat}, {Lng}", lat, lng);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error in reverse geocoding for: {Lat}, {Lng}", lat, lng);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in reverse geocoding for: {Lat}, {Lng}", lat, lng);
            return null;
        }
    }

    public async Task<List<LocationOption>> GetPopularLocationsAsync(int limit = 10)
    {
        var cacheKey = $"osm_popular:{limit}";
        
        // Check cache first
        if (_cache.TryGetValue(cacheKey, out List<LocationOption>? cachedResults))
        {
            return cachedResults!;
        }

        try
        {
            // Get popular locations from database based on usage frequency
            var popularLocations = await _context.LocationSearchLogs
                .Where(log => !string.IsNullOrEmpty(log.SearchQuery))
                .GroupBy(log => log.SearchQuery!.ToLower())
                .Select(group => new { Query = group.Key, Count = group.Count() })
                .OrderByDescending(item => item.Count)
                .Take(limit * 2) // Get more to filter out invalid ones
                .ToListAsync();

            var locationOptions = new List<LocationOption>();

            foreach (var popular in popularLocations)
            {
                if (locationOptions.Count >= limit)
                    break;

                // Try to geocode the popular search term
                var results = await SearchLocationsAsync(popular.Query, 1);
                if (results.Any())
                {
                    var location = results.First();
                    location.Confidence = Math.Min(1.0m, location.Confidence + (popular.Count * 0.01m)); // Boost confidence
                    locationOptions.Add(location);
                }
            }

            // Cache for a longer time since popular locations change slowly
            var cacheExpiry = TimeSpan.FromHours(_config.CacheDurationHours * 7); // 1 week
            _cache.Set(cacheKey, locationOptions, cacheExpiry);

            _logger.LogDebug("Retrieved {Count} popular locations", locationOptions.Count);
            return locationOptions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving popular locations");
            return new List<LocationOption>();
        }
    }

    public async Task<List<LocationOption>> GetLocationSuggestionsAsync(string query, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new List<LocationOption>();

        try
        {
            // Combine database suggestions with geocoding results
            var tasks = new List<Task<List<LocationOption>>>
            {
                GetDatabaseSuggestionsAsync(query, limit / 2),
                SearchLocationsAsync(query, limit / 2)
            };

            var results = await Task.WhenAll(tasks);
            var allSuggestions = results.SelectMany(r => r).ToList();

            // Remove duplicates based on display name and coordinates
            var uniqueSuggestions = allSuggestions
                .GroupBy(loc => new { 
                    DisplayName = loc.DisplayName.ToLowerInvariant(),
                    Lat = loc.Lat?.ToString("F3"),
                    Lng = loc.Lng?.ToString("F3")
                })
                .Select(group => group.OrderByDescending(loc => loc.Confidence).First())
                .OrderByDescending(loc => loc.Confidence)
                .Take(limit)
                .ToList();

            _logger.LogDebug("Generated {Count} location suggestions for query: {Query}", uniqueSuggestions.Count, query);
            return uniqueSuggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location suggestions for query: {Query}", query);
            return new List<LocationOption>();
        }
    }

    private async Task<List<LocationOption>> GetDatabaseSuggestionsAsync(string query, int limit)
    {
        try
        {
            var lowerQuery = query.ToLowerInvariant();
            
            // Search in recent successful searches
            var dbSuggestions = await _context.LocationSearchLogs
                .Where(log => !string.IsNullOrEmpty(log.SearchQuery) &&
                            log.SearchQuery.ToLower().Contains(lowerQuery))
                .GroupBy(log => log.SearchQuery!.ToLower())
                .Select(group => new { Query = group.Key, Count = group.Count() })
                .OrderByDescending(item => item.Count)
                .Take(limit)
                .ToListAsync();

            var locationOptions = new List<LocationOption>();

            foreach (var suggestion in dbSuggestions)
            {
                locationOptions.Add(new LocationOption
                {
                    DisplayName = suggestion.Query,
                    Source = Core.Enums.LocationSource.Manual,
                    Confidence = Math.Min(1.0m, suggestion.Count * 0.1m)
                });
            }

            return locationOptions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database suggestions for query: {Query}", query);
            return new List<LocationOption>();
        }
    }

    private async Task<List<LocationOption>> ApplyLocationDisambiguationAsync(List<LocationOption> locations, string? userLocation)
    {
        if (locations.Count <= 1)
            return locations;

        try
        {
            // Group locations by similar names to identify ambiguous cases
            var ambiguousGroups = locations
                .GroupBy(loc => GetCityName(loc))
                .Where(group => group.Count() > 1)
                .ToList();

            foreach (var group in ambiguousGroups)
            {
                var groupLocations = group.ToList();
                
                // Enhance display names to disambiguate
                foreach (var location in groupLocations)
                {
                    var parts = new List<string>();
                    
                    if (!string.IsNullOrEmpty(location.City))
                        parts.Add(location.City);
                    
                    if (!string.IsNullOrEmpty(location.State))
                        parts.Add(location.State);
                    
                    if (!string.IsNullOrEmpty(location.Country))
                        parts.Add(location.Country);

                    location.DisplayName = string.Join(", ", parts);
                }

                // If user location is provided, boost nearby results
                if (!string.IsNullOrEmpty(userLocation))
                {
                    // This is a simplified implementation
                    // In production, you'd want more sophisticated proximity calculations
                    foreach (var location in groupLocations)
                    {
                        if (location.State?.ToLowerInvariant().Contains(userLocation.ToLowerInvariant()) == true ||
                            location.Country?.ToLowerInvariant().Contains(userLocation.ToLowerInvariant()) == true)
                        {
                            location.Confidence += 0.2m; // Boost local results
                        }
                    }
                }
            }

            // Re-sort by confidence after disambiguation
            return locations.OrderByDescending(loc => loc.Confidence).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying location disambiguation");
            return locations; // Return original list if disambiguation fails
        }
    }

    private static string GetCityName(LocationOption location)
    {
        return (location.City ?? location.Area ?? location.DisplayName.Split(',')[0]).ToLowerInvariant();
    }

    private async Task EnforceRateLimit()
    {
        await _rateLimiter.WaitAsync();
        
        try
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            var minInterval = TimeSpan.FromSeconds(1.0 / _config.RequestsPerSecond);
            
            if (timeSinceLastRequest < minInterval)
            {
                var delay = minInterval - timeSinceLastRequest;
                _logger.LogDebug("Rate limiting: waiting {DelayMs}ms", delay.TotalMilliseconds);
                await Task.Delay(delay);
            }
            
            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    public void Dispose()
    {
        _rateLimiter?.Dispose();
        _httpClient?.Dispose();
    }
}