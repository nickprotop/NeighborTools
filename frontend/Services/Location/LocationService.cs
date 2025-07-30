using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Net;
using System.Text.Json;
using ToolsSharing.Frontend.Models.Location;
using ToolsSharing.Frontend.Utilities;

namespace ToolsSharing.Frontend.Services.Location;

/// <summary>
/// Frontend location service implementation with HttpClient, caching, and geolocation
/// </summary>
public class LocationService : ILocationService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<LocationService> _logger;

    // Cache keys and expiration times
    private const string PopularLocationsCacheKey = "popular_locations";
    private const string LocationSuggestionsCacheKeyPrefix = "location_suggestions_";
    private static readonly TimeSpan PopularLocationsCacheDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan LocationSuggestionsCacheDuration = TimeSpan.FromMinutes(10);

    // API endpoints
    private const string SearchEndpoint = "/api/location/search";
    private const string ReverseEndpoint = "/api/location/reverse";
    private const string PopularEndpoint = "/api/location/popular";
    private const string SuggestionsEndpoint = "/api/location/suggestions";
    private const string NearbyToolsEndpoint = "/api/location/nearby/tools";
    private const string NearbyBundlesEndpoint = "/api/location/nearby/bundles";

    // JSON serializer options for API calls
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public LocationService(
        HttpClient httpClient,
        IMemoryCache cache,
        IJSRuntime jsRuntime,
        ILogger<LocationService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<List<LocationOption>> SearchLocationsAsync(string query, int maxResults = 5, string? countryCode = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("Empty query provided to SearchLocationsAsync");
                return new List<LocationOption>();
            }

            // Validate maxResults range
            maxResults = Math.Max(1, Math.Min(maxResults, 20));

            // Build query parameters
            var queryParams = new List<string>
            {
                $"query={Uri.EscapeDataString(query)}",
                $"maxResults={maxResults}"
            };

            if (!string.IsNullOrEmpty(countryCode))
            {
                queryParams.Add($"countryCode={Uri.EscapeDataString(countryCode)}");
            }

            var url = $"{SearchEndpoint}?{string.Join("&", queryParams)}";

            // Make API call with retry logic
            var response = await MakeApiCallWithRetryAsync(url);
            if (response != null && response.Success && response.Data != null)
            {
                _logger.LogInformation("Location search completed: query='{Query}', results={ResultCount}", 
                    query, response.Data.Count);
                return response.Data;
            }

            _logger.LogWarning("Location search failed or returned no results: query='{Query}'", query);
            return new List<LocationOption>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during location search: query='{Query}'", query);
            return new List<LocationOption>();
        }
    }

    public async Task<LocationOption?> ReverseGeocodeAsync(decimal lat, decimal lng)
    {
        try
        {
            // Validate coordinates
            if (!LocationUtilities.ValidateCoordinates(lat, lng))
            {
                _logger.LogWarning("Invalid coordinates provided to ReverseGeocodeAsync: lat={Lat}, lng={Lng}", lat, lng);
                return null;
            }

            var url = $"{ReverseEndpoint}?lat={lat}&lng={lng}";

            // Make API call with retry logic
            var response = await MakeApiCallWithRetryAsync<LocationOption>(url);
            if (response != null && response.Success)
            {
                _logger.LogInformation("Reverse geocoding completed: lat={Lat}, lng={Lng}", lat, lng);
                return response.Data;
            }

            _logger.LogWarning("Reverse geocoding failed: lat={Lat}, lng={Lng}", lat, lng);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during reverse geocoding: lat={Lat}, lng={Lng}", lat, lng);
            return null;
        }
    }

    public async Task<List<LocationOption>> GetPopularLocationsAsync(int maxResults = 10)
    {
        try
        {
            // Check cache first
            var cacheKey = $"{PopularLocationsCacheKey}_{maxResults}";
            if (_cache.TryGetValue(cacheKey, out List<LocationOption>? cachedResults) && cachedResults != null)
            {
                _logger.LogDebug("Returning cached popular locations: count={Count}", cachedResults.Count);
                return cachedResults;
            }

            // Validate maxResults range
            maxResults = Math.Max(1, Math.Min(maxResults, 50));

            var url = $"{PopularEndpoint}?maxResults={maxResults}";

            // Make API call
            var response = await MakeApiCallWithRetryAsync(url);
            if (response != null && response.Success && response.Data != null)
            {
                // Cache the results
                _cache.Set(cacheKey, response.Data, PopularLocationsCacheDuration);
                
                _logger.LogInformation("Popular locations fetched and cached: count={Count}", response.Data.Count);
                return response.Data;
            }

            _logger.LogWarning("Failed to fetch popular locations");
            return new List<LocationOption>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching popular locations");
            return new List<LocationOption>();
        }
    }

    public async Task<List<LocationOption>> GetLocationSuggestionsAsync(string query, int maxResults = 8)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("Empty query provided to GetLocationSuggestionsAsync");
                return new List<LocationOption>();
            }

            // Check cache first
            var cacheKey = $"{LocationSuggestionsCacheKeyPrefix}{query.ToLowerInvariant()}_{maxResults}";
            if (_cache.TryGetValue(cacheKey, out List<LocationOption>? cachedResults) && cachedResults != null)
            {
                _logger.LogDebug("Returning cached location suggestions: query='{Query}', count={Count}", query, cachedResults.Count);
                return cachedResults;
            }

            // Validate maxResults range
            maxResults = Math.Max(1, Math.Min(maxResults, 20));

            var url = $"{SuggestionsEndpoint}?query={Uri.EscapeDataString(query)}&maxResults={maxResults}";

            // Make API call
            var response = await MakeApiCallWithRetryAsync(url);
            if (response != null && response.Success && response.Data != null)
            {
                // Cache the results
                _cache.Set(cacheKey, response.Data, LocationSuggestionsCacheDuration);
                
                _logger.LogInformation("Location suggestions fetched and cached: query='{Query}', count={Count}", 
                    query, response.Data.Count);
                return response.Data;
            }

            _logger.LogWarning("Failed to fetch location suggestions: query='{Query}'", query);
            return new List<LocationOption>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching location suggestions: query='{Query}'", query);
            return new List<LocationOption>();
        }
    }

    public async Task<List<NearbyToolDto>> FindNearbyToolsAsync(decimal lat, decimal lng, decimal radiusKm, int maxResults = 20)
    {
        try
        {
            // Validate coordinates
            if (!LocationUtilities.ValidateCoordinates(lat, lng))
            {
                _logger.LogWarning("Invalid coordinates provided to FindNearbyToolsAsync: lat={Lat}, lng={Lng}", lat, lng);
                return new List<NearbyToolDto>();
            }

            // Validate radius and maxResults
            radiusKm = Math.Max(1, Math.Min(radiusKm, 100));
            maxResults = Math.Max(1, Math.Min(maxResults, 100));

            var url = $"{NearbyToolsEndpoint}?lat={lat}&lng={lng}&radiusKm={radiusKm}&maxResults={maxResults}";

            // Make API call with retry logic
            var response = await MakeApiCallWithRetryAsync<List<NearbyToolDto>>(url);
            if (response != null && response.Success && response.Data != null)
            {
                _logger.LogInformation("Nearby tools search completed: lat={Lat}, lng={Lng}, radius={Radius}km, results={ResultCount}",
                    lat, lng, radiusKm, response.Data.Count);
                return response.Data;
            }

            _logger.LogWarning("Nearby tools search failed: lat={Lat}, lng={Lng}, radius={Radius}km", lat, lng, radiusKm);
            return new List<NearbyToolDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during nearby tools search: lat={Lat}, lng={Lng}, radius={Radius}km", 
                lat, lng, radiusKm);
            return new List<NearbyToolDto>();
        }
    }

    public async Task<List<NearbyBundleDto>> FindNearbyBundlesAsync(decimal lat, decimal lng, decimal radiusKm, int maxResults = 20)
    {
        try
        {
            // Validate coordinates
            if (!LocationUtilities.ValidateCoordinates(lat, lng))
            {
                _logger.LogWarning("Invalid coordinates provided to FindNearbyBundlesAsync: lat={Lat}, lng={Lng}", lat, lng);
                return new List<NearbyBundleDto>();
            }

            // Validate radius and maxResults
            radiusKm = Math.Max(1, Math.Min(radiusKm, 100));
            maxResults = Math.Max(1, Math.Min(maxResults, 100));

            var url = $"{NearbyBundlesEndpoint}?lat={lat}&lng={lng}&radiusKm={radiusKm}&maxResults={maxResults}";

            // Make API call with retry logic
            var response = await MakeApiCallWithRetryAsync<List<NearbyBundleDto>>(url);
            if (response != null && response.Success && response.Data != null)
            {
                _logger.LogInformation("Nearby bundles search completed: lat={Lat}, lng={Lng}, radius={Radius}km, results={ResultCount}",
                    lat, lng, radiusKm, response.Data.Count);
                return response.Data;
            }

            _logger.LogWarning("Nearby bundles search failed: lat={Lat}, lng={Lng}, radius={Radius}km", lat, lng, radiusKm);
            return new List<NearbyBundleDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during nearby bundles search: lat={Lat}, lng={Lng}, radius={Radius}km", 
                lat, lng, radiusKm);
            return new List<NearbyBundleDto>();
        }
    }

    public async Task<GeolocationResult> GetCurrentLocationAsync(bool highAccuracy = true, int timeoutMs = 10000)
    {
        try
        {
            // Check if geolocation is supported
            var isSupported = await IsGeolocationSupportedAsync();
            if (!isSupported)
            {
                _logger.LogWarning("Geolocation not supported by browser");
                return new GeolocationResult
                {
                    Success = false,
                    Error = GeolocationError.NotSupported,
                    ErrorMessage = LocationUtilities.GetGeolocationErrorMessage(GeolocationError.NotSupported)
                };
            }

            // Prepare options for JavaScript call
            var options = new
            {
                enableHighAccuracy = highAccuracy,
                timeout = Math.Max(1000, Math.Min(timeoutMs, 60000)), // 1s to 60s
                maximumAge = 300000 // 5 minutes
            };

            // Call JavaScript geolocation function
            var result = await _jsRuntime.InvokeAsync<dynamic>("geolocationService.getCurrentPosition", options);
            
            // Parse the result
            var jsonElement = (JsonElement)result;
            
            if (jsonElement.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
            {
                var geolocationResult = new GeolocationResult
                {
                    Success = true,
                    Latitude = jsonElement.GetProperty("latitude").GetDecimal(),
                    Longitude = jsonElement.GetProperty("longitude").GetDecimal(),
                    Accuracy = jsonElement.TryGetProperty("accuracy", out var accuracyProp) ? accuracyProp.GetDecimal() : null,
                    Timestamp = jsonElement.TryGetProperty("timestamp", out var timestampProp) ? 
                        DateTime.Parse(timestampProp.GetString()!) : DateTime.UtcNow
                };

                _logger.LogInformation("Geolocation obtained: lat={Lat}, lng={Lng}, accuracy={Accuracy}m",
                    geolocationResult.Latitude, geolocationResult.Longitude, geolocationResult.Accuracy);

                return geolocationResult;
            }
            else
            {
                var errorCode = jsonElement.GetProperty("error").GetInt32();
                var errorMessage = jsonElement.GetProperty("message").GetString() ?? "Unknown error";
                var error = (GeolocationError)errorCode;

                _logger.LogWarning("Geolocation failed: error={Error}, message={Message}", error, errorMessage);

                return new GeolocationResult
                {
                    Success = false,
                    Error = error,
                    ErrorMessage = LocationUtilities.GetGeolocationErrorMessage(error)
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during geolocation request");
            return new GeolocationResult
            {
                Success = false,
                Error = GeolocationError.PositionUnavailable,
                ErrorMessage = "An error occurred while getting your location. Please try again."
            };
        }
    }

    public void ClearCache()
    {
        try
        {
            // Remove all cached location data
            var cacheField = _cache.GetType().GetField("_coherentState", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (cacheField?.GetValue(_cache) is IDictionary<object, object> cacheDict)
            {
                var keysToRemove = cacheDict.Keys
                    .Where(key => key.ToString()?.StartsWith(PopularLocationsCacheKey) == true ||
                                  key.ToString()?.StartsWith(LocationSuggestionsCacheKeyPrefix) == true)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                }

                _logger.LogInformation("Cleared {Count} location cache entries", keysToRemove.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while clearing location cache");
            // Fallback: create new cache instance (if possible)
        }
    }

    public async Task<bool> IsGeolocationSupportedAsync()
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<bool>("geolocationService.isSupported");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking geolocation support");
            return false;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Make an API call with retry logic for rate limiting
    /// </summary>
    private async Task<ApiResponse<List<LocationOption>>?> MakeApiCallWithRetryAsync(string url, int maxRetries = 2)
    {
        return await MakeApiCallWithRetryAsync<List<LocationOption>>(url, maxRetries);
    }

    /// <summary>
    /// Make an API call with retry logic for rate limiting (generic version)
    /// </summary>
    private async Task<ApiResponse<T>?> MakeApiCallWithRetryAsync<T>(string url, int maxRetries = 2)
    {
        var retryCount = 0;
        TimeSpan retryDelay = TimeSpan.FromSeconds(1);

        while (retryCount <= maxRetries)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content, JsonOptions);
                    return apiResponse;
                }
                else if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    // Rate limited - implement exponential backoff
                    if (retryCount < maxRetries)
                    {
                        _logger.LogWarning("Rate limited on API call to {Url}, retrying in {Delay}ms (attempt {Attempt}/{MaxAttempts})",
                            url, retryDelay.TotalMilliseconds, retryCount + 1, maxRetries + 1);

                        await Task.Delay(retryDelay);
                        retryDelay = TimeSpan.FromMilliseconds(retryDelay.TotalMilliseconds * 2); // Exponential backoff
                        retryCount++;
                        continue;
                    }
                    else
                    {
                        _logger.LogError("Rate limit exceeded for API call to {Url} after {MaxAttempts} attempts", url, maxRetries + 1);
                        return new ApiResponse<T>
                        {
                            Success = false,
                            Message = "Rate limit exceeded. Please try again later.",
                            Errors = new List<string> { "Too many requests. Please wait a moment before trying again." }
                        };
                    }
                }
                else
                {
                    // Other HTTP error
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("API call failed: {StatusCode} - {Content}", response.StatusCode, errorContent);

                    // Try to parse error response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ApiResponse<T>>(errorContent, JsonOptions);
                        return errorResponse;
                    }
                    catch
                    {
                        // Return generic error response
                        return new ApiResponse<T>
                        {
                            Success = false,
                            Message = $"API call failed: {response.StatusCode}",
                            Errors = new List<string> { "An error occurred while processing your request." }
                        };
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error during API call to {Url}", url);
                return new ApiResponse<T>
                {
                    Success = false,
                    Message = "Network error occurred",
                    Errors = new List<string> { "Please check your internet connection and try again." }
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout during API call to {Url}", url);
                return new ApiResponse<T>
                {
                    Success = false,
                    Message = "Request timeout",
                    Errors = new List<string> { "The request took too long to complete. Please try again." }
                };
            }
        }

        return null;
    }

    #endregion
}