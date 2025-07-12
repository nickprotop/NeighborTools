using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Features.Settings;
using ToolsSharing.Core.Interfaces;

namespace ToolsSharing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(ISettingsService settingsService, ILogger<SettingsController> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Get user settings
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> GetSettings()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var settings = await _settingsService.GetUserSettingsAsync(userId);
            
            return Ok(new ApiResponse<UserSettingsDto>
            {
                Success = true,
                Data = settings,
                Message = "Settings retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settings for user");
            return StatusCode(500, new ApiResponse<UserSettingsDto>
            {
                Success = false,
                Message = "An error occurred while retrieving settings",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Update user settings (partial update supported)
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> UpdateSettings([FromBody] UpdateUserSettingsRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var command = new UpdateUserSettingsCommand(
                userId,
                request.Privacy,
                request.Notifications,
                request.Display,
                request.Rental,
                request.Security,
                request.Communication
            );

            var settings = await _settingsService.UpdateUserSettingsAsync(command);
            
            return Ok(new ApiResponse<UserSettingsDto>
            {
                Success = true,
                Data = settings,
                Message = "Settings updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings for user");
            return StatusCode(500, new ApiResponse<UserSettingsDto>
            {
                Success = false,
                Message = "An error occurred while updating settings",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Update privacy settings only
    /// </summary>
    [HttpPut("privacy")]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> UpdatePrivacySettings([FromBody] PrivacySettingsDto privacy)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var command = new UpdateUserSettingsCommand(userId, Privacy: privacy);
            var settings = await _settingsService.UpdateUserSettingsAsync(command);
            
            return Ok(new ApiResponse<UserSettingsDto>
            {
                Success = true,
                Data = settings,
                Message = "Privacy settings updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating privacy settings for user");
            return StatusCode(500, new ApiResponse<UserSettingsDto>
            {
                Success = false,
                Message = "An error occurred while updating privacy settings",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Update notification settings only
    /// </summary>
    [HttpPut("notifications")]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> UpdateNotificationSettings([FromBody] NotificationSettingsDto notifications)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var command = new UpdateUserSettingsCommand(userId, Notifications: notifications);
            var settings = await _settingsService.UpdateUserSettingsAsync(command);
            
            return Ok(new ApiResponse<UserSettingsDto>
            {
                Success = true,
                Data = settings,
                Message = "Notification settings updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification settings for user");
            return StatusCode(500, new ApiResponse<UserSettingsDto>
            {
                Success = false,
                Message = "An error occurred while updating notification settings",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Change user password
    /// </summary>
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (request.NewPassword != request.ConfirmPassword)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "New password and confirmation do not match",
                    Errors = new List<string> { "Password confirmation mismatch" }
                });
            }

            var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);
            var success = await _settingsService.ChangePasswordAsync(command);
            
            if (!success)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to change password. Please check your current password.",
                    Errors = new List<string> { "Invalid current password" }
                });
            }
            
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Password changed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "An error occurred while changing password",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Reset settings to default values
    /// </summary>
    [HttpPost("reset")]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> ResetSettings()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var success = await _settingsService.ResetSettingsToDefaultAsync(userId);
            if (!success)
            {
                return BadRequest(new ApiResponse<UserSettingsDto>
                {
                    Success = false,
                    Message = "Failed to reset settings"
                });
            }

            var settings = await _settingsService.GetUserSettingsAsync(userId);
            
            return Ok(new ApiResponse<UserSettingsDto>
            {
                Success = true,
                Data = settings,
                Message = "Settings reset to default values successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting settings for user");
            return StatusCode(500, new ApiResponse<UserSettingsDto>
            {
                Success = false,
                Message = "An error occurred while resetting settings",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               User.FindFirst("sub")?.Value ?? 
               User.FindFirst("id")?.Value;
    }
}

// Request models
public class UpdateUserSettingsRequest
{
    public PrivacySettingsDto? Privacy { get; set; }
    public NotificationSettingsDto? Notifications { get; set; }
    public DisplaySettingsDto? Display { get; set; }
    public RentalSettingsDto? Rental { get; set; }
    public SecuritySettingsDto? Security { get; set; }
    public CommunicationSettingsDto? Communication { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}