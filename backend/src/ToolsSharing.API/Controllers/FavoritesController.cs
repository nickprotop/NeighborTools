using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToolsSharing.Core.DTOs;
using ToolsSharing.Core.Interfaces;

namespace ToolsSharing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IFavoritesService _favoritesService;
    private readonly ILogger<FavoritesController> _logger;

    public FavoritesController(
        IFavoritesService favoritesService,
        ILogger<FavoritesController> logger)
    {
        _favoritesService = favoritesService;
        _logger = logger;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException("User not authenticated");
    }

    /// <summary>
    /// Get all favorites for the current user
    /// </summary>
    /// <returns>List of user's favorite tools</returns>
    [HttpGet]
    public async Task<IActionResult> GetUserFavorites()
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _favoritesService.GetUserFavoritesAsync(userId);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user favorites");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Check if a specific tool is favorited by the current user
    /// </summary>
    /// <param name="toolId">Tool ID to check</param>
    /// <returns>Favorite status information</returns>
    [HttpGet("status/{toolId:guid}")]
    public async Task<IActionResult> CheckFavoriteStatus(Guid toolId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _favoritesService.CheckFavoriteStatusAsync(userId, toolId);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking favorite status for tool {ToolId}", toolId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Add a tool to favorites
    /// </summary>
    /// <param name="request">Add to favorites request</param>
    /// <returns>Created favorite information</returns>
    [HttpPost]
    public async Task<IActionResult> AddToFavorites([FromBody] AddToFavoritesRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _favoritesService.AddToFavoritesAsync(userId, request.ToolId);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding tool {ToolId} to favorites", request.ToolId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Remove a tool from favorites by tool ID
    /// </summary>
    /// <param name="toolId">Tool ID to remove from favorites</param>
    /// <returns>Success status</returns>
    [HttpDelete("tool/{toolId:guid}")]
    public async Task<IActionResult> RemoveFromFavorites(Guid toolId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _favoritesService.RemoveFromFavoritesAsync(userId, toolId);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tool {ToolId} from favorites", toolId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Remove a favorite by favorite ID
    /// </summary>
    /// <param name="favoriteId">Favorite ID to remove</param>
    /// <returns>Success status</returns>
    [HttpDelete("{favoriteId:guid}")]
    public async Task<IActionResult> RemoveFromFavoritesById(Guid favoriteId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _favoritesService.RemoveFromFavoritesByIdAsync(userId, favoriteId);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing favorite {FavoriteId}", favoriteId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get the count of favorites for the current user
    /// </summary>
    /// <returns>Number of favorites</returns>
    [HttpGet("count")]
    public async Task<IActionResult> GetFavoritesCount()
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _favoritesService.GetUserFavoritesCountAsync(userId);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting favorites count");
            return StatusCode(500, "Internal server error");
        }
    }
}