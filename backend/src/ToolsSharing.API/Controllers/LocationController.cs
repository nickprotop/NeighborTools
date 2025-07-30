using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.DTOs.Location;
using ToolsSharing.Core.Enums;
using ToolsSharing.Core.Interfaces;
using System.Security.Claims;

namespace ToolsSharing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LocationController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly ILogger<LocationController> _logger;

    public LocationController(ILocationService locationService, ILogger<LocationController> logger)
    {
        _locationService = locationService;
        _logger = logger;
    }

    /// <summary>
    /// Search for locations using geocoding with privacy levels
    /// </summary>
    /// <param name="query">Location search query (e.g., "Athens, GA")</param>
    /// <param name="maxResults">Maximum number of results (1-20, default 5)</param>
    /// <param name="countryCode">Optional country code filter (e.g., "us")</param>
    /// <returns>List of location options with privacy-aware information</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<List<LocationOption>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<LocationOption>>>> SearchLocations(
        [FromQuery] string query,
        [FromQuery] int maxResults = 5,
        [FromQuery] string? countryCode = null)
    {
        try
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Search query is required",
                    Errors = new List<string> { "Query parameter cannot be empty" }
                });
            }

            if (maxResults < 1 || maxResults > 20)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid maxResults parameter",
                    Errors = new List<string> { "maxResults must be between 1 and 20" }
                });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User authentication required",
                    Errors = new List<string> { "Valid user authentication is required for location searches" }
                });
            }

            // Perform search with security logging
            var results = await _locationService.SearchLocationsAsync(query, maxResults, countryCode, userId);

            _logger.LogInformation("Location search completed for user {UserId}: query='{Query}', results={ResultCount}",
                userId, query, results.Count);

            return Ok(new ApiResponse<List<LocationOption>>
            {
                Success = true,
                Message = $"Found {results.Count} location(s)",
                Data = results
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Rate limit exceeded"))
        {
            _logger.LogWarning("Rate limit exceeded for location search: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status429TooManyRequests, new ApiResponse<object>
            {
                Success = false,
                Message = "Rate limit exceeded for location searches",
                Errors = new List<string> { "Please wait before making more location searches" }
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Suspicious search pattern"))
        {
            _logger.LogWarning("Suspicious search pattern detected: {Message}", ex.Message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid search request",
                Errors = new List<string> { "Search request cannot be processed" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during location search for query: {Query}", query);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while searching for locations",
                Errors = new List<string> { "Please try again later" }
            });
        }
    }

    /// <summary>
    /// Reverse geocode coordinates to location name with privacy generalization
    /// </summary>
    /// <param name="lat">Latitude (-90 to 90)</param>
    /// <param name="lng">Longitude (-180 to 180)</param>
    /// <returns>Location information for the coordinates</returns>
    [HttpGet("reverse")]
    [ProducesResponseType(typeof(ApiResponse<LocationOption>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<LocationOption>>> ReverseGeocode(
        [FromQuery] decimal lat,
        [FromQuery] decimal lng)
    {
        try
        {
            // Coordinate validation
            if (!_locationService.ValidateCoordinates(lat, lng))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid coordinates",
                    Errors = new List<string> 
                    { 
                        "Latitude must be between -90 and 90",
                        "Longitude must be between -180 and 180"
                    }
                });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User authentication required",
                    Errors = new List<string> { "Valid user authentication is required for reverse geocoding" }
                });
            }

            // Perform reverse geocoding with security logging
            var result = await _locationService.ReverseGeocodeAsync(lat, lng, userId);

            if (result == null)
            {
                return Ok(new ApiResponse<LocationOption>
                {
                    Success = true,
                    Message = "No location found for the specified coordinates",
                    Data = null
                });
            }

            _logger.LogInformation("Reverse geocoding completed for user {UserId}: lat={Lat}, lng={Lng}",
                userId, lat, lng);

            return Ok(new ApiResponse<LocationOption>
            {
                Success = true,
                Message = "Location found",
                Data = result
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Rate limit exceeded"))
        {
            _logger.LogWarning("Rate limit exceeded for reverse geocoding: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status429TooManyRequests, new ApiResponse<object>
            {
                Success = false,
                Message = "Rate limit exceeded for location requests",
                Errors = new List<string> { "Please wait before making more location requests" }
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Suspicious search pattern"))
        {
            _logger.LogWarning("Suspicious search pattern detected in reverse geocoding: {Message}", ex.Message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid location request",
                Errors = new List<string> { "Location request cannot be processed" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during reverse geocoding for coordinates: {Lat}, {Lng}", lat, lng);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while processing the location request",
                Errors = new List<string> { "Please try again later" }
            });
        }
    }

    /// <summary>
    /// Get popular locations from database frequency analysis
    /// </summary>
    /// <param name="maxResults">Maximum number of results (1-50, default 10)</param>
    /// <returns>List of popular locations based on database usage</returns>
    [HttpGet("popular")]
    [ProducesResponseType(typeof(ApiResponse<List<LocationOption>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<LocationOption>>>> GetPopularLocations(
        [FromQuery] int maxResults = 10)
    {
        try
        {
            // Input validation
            if (maxResults < 1 || maxResults > 50)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid maxResults parameter",
                    Errors = new List<string> { "maxResults must be between 1 and 50" }
                });
            }

            // Get popular locations from database
            var results = await _locationService.GetPopularLocationsAsync(maxResults);

            _logger.LogInformation("Popular locations request completed: returned {ResultCount} locations", results.Count);

            return Ok(new ApiResponse<List<LocationOption>>
            {
                Success = true,
                Message = $"Found {results.Count} popular location(s)",
                Data = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching popular locations");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while fetching popular locations",
                Errors = new List<string> { "Please try again later" }
            });
        }
    }

    /// <summary>
    /// Get location suggestions combining database and geocoding results
    /// </summary>
    /// <param name="query">Partial location query for suggestions</param>
    /// <param name="maxResults">Maximum number of results (1-20, default 8)</param>
    /// <returns>Hybrid suggestions from database and geocoding service</returns>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(ApiResponse<List<LocationOption>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<LocationOption>>>> GetLocationSuggestions(
        [FromQuery] string query,
        [FromQuery] int maxResults = 8)
    {
        try
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Search query is required",
                    Errors = new List<string> { "Query parameter cannot be empty" }
                });
            }

            if (maxResults < 1 || maxResults > 20)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid maxResults parameter",
                    Errors = new List<string> { "maxResults must be between 1 and 20" }
                });
            }

            // Get hybrid suggestions
            var results = await _locationService.GetLocationSuggestionsAsync(query, maxResults);

            _logger.LogInformation("Location suggestions request completed for query '{Query}': returned {ResultCount} suggestions", 
                query, results.Count);

            return Ok(new ApiResponse<List<LocationOption>>
            {
                Success = true,
                Message = $"Found {results.Count} suggestion(s)",
                Data = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching location suggestions for query: {Query}", query);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while fetching location suggestions",
                Errors = new List<string> { "Please try again later" }
            });
        }
    }

    /// <summary>
    /// Find nearby tools with triangulation protection
    /// </summary>
    /// <param name="lat">Center latitude (-90 to 90)</param>
    /// <param name="lng">Center longitude (-180 to 180)</param>
    /// <param name="radiusKm">Search radius in kilometers (1-100)</param>
    /// <param name="maxResults">Maximum number of results (1-100, default 20)</param>
    /// <returns>Nearby tools with distance bands for privacy</returns>
    [HttpGet("nearby/tools")]
    [ProducesResponseType(typeof(ApiResponse<List<NearbyToolDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<NearbyToolDto>>>> FindNearbyTools(
        [FromQuery] decimal lat,
        [FromQuery] decimal lng,
        [FromQuery] decimal radiusKm,
        [FromQuery] int maxResults = 20)
    {
        try
        {
            // Coordinate validation
            if (!_locationService.ValidateCoordinates(lat, lng))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid coordinates",
                    Errors = new List<string> 
                    { 
                        "Latitude must be between -90 and 90",
                        "Longitude must be between -180 and 180"
                    }
                });
            }

            // Radius validation
            if (radiusKm < 1 || radiusKm > 100)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid radius",
                    Errors = new List<string> { "Radius must be between 1 and 100 kilometers" }
                });
            }

            // Results limit validation
            if (maxResults < 1 || maxResults > 100)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid maxResults parameter",
                    Errors = new List<string> { "maxResults must be between 1 and 100" }
                });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User authentication required",
                    Errors = new List<string> { "Valid user authentication is required for proximity searches" }
                });
            }

            // Perform proximity search with security validation
            var results = await _locationService.FindNearbyToolsAsync(lat, lng, radiusKm, userId, maxResults);

            _logger.LogInformation("Nearby tools search completed for user {UserId}: lat={Lat}, lng={Lng}, radius={Radius}km, results={ResultCount}",
                userId, lat, lng, radiusKm, results.Count);

            return Ok(new ApiResponse<List<NearbyToolDto>>
            {
                Success = true,
                Message = $"Found {results.Count} nearby tool(s) within {radiusKm}km",
                Data = results
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid arguments for nearby tools search: {Message}", ex.Message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid search parameters",
                Errors = new List<string> { ex.Message }
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Rate limit exceeded"))
        {
            _logger.LogWarning("Rate limit exceeded for nearby tools search: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status429TooManyRequests, new ApiResponse<object>
            {
                Success = false,
                Message = "Rate limit exceeded for proximity searches",
                Errors = new List<string> { "Please wait before making more proximity searches" }
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Suspicious search pattern"))
        {
            _logger.LogWarning("Suspicious search pattern detected in nearby tools search: {Message}", ex.Message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid search request",
                Errors = new List<string> { "Search request cannot be processed" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during nearby tools search for coordinates: {Lat}, {Lng}", lat, lng);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while searching for nearby tools",
                Errors = new List<string> { "Please try again later" }
            });
        }
    }

    /// <summary>
    /// Find nearby bundles with triangulation protection
    /// </summary>
    /// <param name="lat">Center latitude (-90 to 90)</param>
    /// <param name="lng">Center longitude (-180 to 180)</param>
    /// <param name="radiusKm">Search radius in kilometers (1-100)</param>
    /// <param name="maxResults">Maximum number of results (1-100, default 20)</param>
    /// <returns>Nearby bundles with distance bands for privacy</returns>
    [HttpGet("nearby/bundles")]
    [ProducesResponseType(typeof(ApiResponse<List<NearbyBundleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<NearbyBundleDto>>>> FindNearbyBundles(
        [FromQuery] decimal lat,
        [FromQuery] decimal lng,
        [FromQuery] decimal radiusKm,
        [FromQuery] int maxResults = 20)
    {
        try
        {
            // Coordinate validation  
            if (!_locationService.ValidateCoordinates(lat, lng))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid coordinates",
                    Errors = new List<string> 
                    { 
                        "Latitude must be between -90 and 90",
                        "Longitude must be between -180 and 180"
                    }
                });
            }

            // Radius validation
            if (radiusKm < 1 || radiusKm > 100)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid radius",
                    Errors = new List<string> { "Radius must be between 1 and 100 kilometers" }
                });
            }

            // Results limit validation
            if (maxResults < 1 || maxResults > 100)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid maxResults parameter",
                    Errors = new List<string> { "maxResults must be between 1 and 100" }
                });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User authentication required",
                    Errors = new List<string> { "Valid user authentication is required for proximity searches" }
                });
            }

            // Perform proximity search with security validation
            var results = await _locationService.FindNearbyBundlesAsync(lat, lng, radiusKm, userId, maxResults);

            _logger.LogInformation("Nearby bundles search completed for user {UserId}: lat={Lat}, lng={Lng}, radius={Radius}km, results={ResultCount}",
                userId, lat, lng, radiusKm, results.Count);

            return Ok(new ApiResponse<List<NearbyBundleDto>>
            {
                Success = true,
                Message = $"Found {results.Count} nearby bundle(s) within {radiusKm}km",
                Data = results
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid arguments for nearby bundles search: {Message}", ex.Message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid search parameters",
                Errors = new List<string> { ex.Message }
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Rate limit exceeded"))
        {
            _logger.LogWarning("Rate limit exceeded for nearby bundles search: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status429TooManyRequests, new ApiResponse<object>
            {
                Success = false,
                Message = "Rate limit exceeded for proximity searches",
                Errors = new List<string> { "Please wait before making more proximity searches" }
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Suspicious search pattern"))
        {
            _logger.LogWarning("Suspicious search pattern detected in nearby bundles search: {Message}", ex.Message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid search request",
                Errors = new List<string> { "Search request cannot be processed" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during nearby bundles search for coordinates: {Lat}, {Lng}", lat, lng);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while searching for nearby bundles",
                Errors = new List<string> { "Please try again later" }
            });
        }
    }
}