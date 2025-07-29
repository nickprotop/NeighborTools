using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToolsSharing.Core.Features.Users;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Core.Common.Models;

namespace ToolsSharing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new ApiResponse<UserProfileDto>
            {
                Success = false,
                Message = "User not found"
            });
        }

        var profile = await _userService.GetUserProfileAsync(userId);
        if (profile == null)
        {
            return NotFound(new ApiResponse<UserProfileDto>
            {
                Success = false,
                Message = "User profile not found"
            });
        }

        return Ok(new ApiResponse<UserProfileDto>
        {
            Success = true,
            Data = profile
        });
    }

    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile(UpdateUserProfileCommand command)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new ApiResponse<UserProfileDto>
            {
                Success = false,
                Message = "User not found"
            });
        }

        var updatedCommand = command with { UserId = userId };
        var updatedProfile = await _userService.UpdateUserProfileAsync(updatedCommand);
        
        if (updatedProfile == null)
        {
            return NotFound(new ApiResponse<UserProfileDto>
            {
                Success = false,
                Message = "User profile not found"
            });
        }

        return Ok(new ApiResponse<UserProfileDto>
        {
            Success = true,
            Data = updatedProfile,
            Message = "Profile updated successfully"
        });
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<UserStatisticsDto>>> GetStatistics()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new ApiResponse<UserStatisticsDto>
            {
                Success = false,
                Message = "User not found"
            });
        }

        var statistics = await _userService.GetUserStatisticsAsync(userId);
        
        return Ok(new ApiResponse<UserStatisticsDto>
        {
            Success = true,
            Data = statistics
        });
    }

    [HttpGet("reviews")]
    public async Task<ActionResult<ApiResponse<PagedResult<UserReviewDto>>>> GetReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new ApiResponse<PagedResult<UserReviewDto>>
            {
                Success = false,
                Message = "User not found"
            });
        }

        var query = new GetUserReviewsQuery(userId, page, pageSize);
        var reviews = await _userService.GetUserReviewsAsync(query);
        
        return Ok(new ApiResponse<PagedResult<UserReviewDto>>
        {
            Success = true,
            Data = reviews
        });
    }

    [HttpPost("profile-picture")]
    public async Task<ActionResult<ApiResponse<string>>> UploadProfilePicture(IFormFile file)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = "User not found"
            });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = "No file uploaded"
            });
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
        if (!allowedTypes.Contains(file.ContentType))
        {
            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = "Invalid file type. Only JPEG, PNG, and GIF files are allowed."
            });
        }

        // Validate file size (5MB limit)
        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = "File size must be less than 5MB"
            });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var imageUrl = await _userService.UploadProfilePictureAsync(userId, stream, file.FileName);
            
            if (imageUrl == null)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Failed to upload profile picture"
                });
            }

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Data = imageUrl,
                Message = "Profile picture uploaded successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred while uploading the file",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpDelete("profile-picture")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveProfilePicture()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new ApiResponse<bool>
            {
                Success = false,
                Message = "User not found"
            });
        }

        var result = await _userService.RemoveProfilePictureAsync(userId);
        
        return Ok(new ApiResponse<bool>
        {
            Success = result,
            Data = result,
            Message = result ? "Profile picture removed successfully" : "Failed to remove profile picture"
        });
    }

    [HttpGet("profile/{userId}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetPublicProfile(string userId)
    {
        var profile = await _userService.GetUserProfileAsync(userId);
        if (profile == null)
        {
            return NotFound(new ApiResponse<UserProfileDto>
            {
                Success = false,
                Message = "User profile not found"
            });
        }

        // Remove sensitive information for public profile
        var publicProfile = new UserProfileDto
        {
            Id = profile.Id,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            ProfilePictureUrl = profile.ProfilePictureUrl,
            City = profile.PublicLocation,
            Country = profile.Country,
            CreatedAt = profile.CreatedAt,
            IsVerified = profile.IsVerified,
            AverageRating = profile.AverageRating,
            ReviewCount = profile.ReviewCount
        };

        return Ok(new ApiResponse<UserProfileDto>
        {
            Success = true,
            Data = publicProfile
        });
    }
}