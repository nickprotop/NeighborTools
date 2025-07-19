using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Features.Users;
using ToolsSharing.Core.Interfaces;

namespace ToolsSharing.API.Controllers;

[ApiController]
[Route("api/users")]
public class PublicProfileController : ControllerBase
{
    private readonly IPublicProfileService _publicProfileService;
    private readonly IUserService _userService;
    private readonly ILogger<PublicProfileController> _logger;

    public PublicProfileController(
        IPublicProfileService publicProfileService,
        IUserService userService,
        ILogger<PublicProfileController> logger)
    {
        _publicProfileService = publicProfileService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get public profile for a user (respects privacy settings)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Public profile information</returns>
    [HttpGet("public/{userId}")]
    public async Task<ActionResult<ApiResponse<PublicUserProfileDto>>> GetPublicProfile(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(ApiResponse<PublicUserProfileDto>.CreateFailure("User ID is required"));
        }

        try
        {
            var result = await _publicProfileService.GetPublicUserProfileAsync(userId);
            
            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public profile for user {UserId}", userId);
            return StatusCode(500, ApiResponse<PublicUserProfileDto>.CreateFailure("Internal server error"));
        }
    }

    /// <summary>
    /// Get public tools for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <returns>List of user's tools</returns>
    [HttpGet("public/{userId}/tools")]
    public async Task<ActionResult<ApiResponse<List<PublicUserToolDto>>>> GetUserTools(
        string userId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(ApiResponse<List<PublicUserToolDto>>.CreateFailure("User ID is required"));
        }

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        try
        {
            var result = await _publicProfileService.GetUserToolsAsync(userId, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tools for user {UserId}", userId);
            return StatusCode(500, ApiResponse<List<PublicUserToolDto>>.CreateFailure("Internal server error"));
        }
    }

    /// <summary>
    /// Get public reviews for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <returns>List of user's reviews</returns>
    [HttpGet("public/{userId}/reviews")]
    public async Task<ActionResult<ApiResponse<List<PublicUserReviewDto>>>> GetUserReviews(
        string userId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(ApiResponse<List<PublicUserReviewDto>>.CreateFailure("User ID is required"));
        }

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        try
        {
            var result = await _publicProfileService.GetUserReviewsAsync(userId, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reviews for user {UserId}", userId);
            return StatusCode(500, ApiResponse<List<PublicUserReviewDto>>.CreateFailure("Internal server error"));
        }
    }

    /// <summary>
    /// Search users by name or email
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="limit">Maximum number of results (default: 10, max: 50)</param>
    /// <returns>List of matching users</returns>
    [HttpGet("search")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<UserSearchResultDto>>>> SearchUsers(
        [FromQuery] string query,
        [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(ApiResponse<List<UserSearchResultDto>>.CreateFailure("Search query is required"));
        }

        if (limit < 1 || limit > 50)
        {
            limit = 10;
        }

        try
        {
            var results = await _userService.SearchUsersAsync(query, limit);
            return Ok(ApiResponse<List<UserSearchResultDto>>.CreateSuccess(results));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users with query {Query}", query);
            return StatusCode(500, ApiResponse<List<UserSearchResultDto>>.CreateFailure("Internal server error"));
        }
    }
}