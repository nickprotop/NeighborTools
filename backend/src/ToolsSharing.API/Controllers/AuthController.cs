using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Features.Auth;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Common.Constants;

namespace ToolsSharing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly UserManager<User> _userManager;

    public AuthController(IAuthService authService, UserManager<User> userManager)
    {
        _authService = authService;
        _userManager = userManager;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _authService.RegisterAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _authService.LoginAsync(command);
        
        if (!result.Success)
            return Unauthorized(result);
            
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
    {
        var result = await _authService.RefreshTokenAsync(command);
        
        if (!result.Success)
            return Unauthorized(result);
            
        return Ok(result);
    }

    [HttpPost("refresh-current-session")]
    [Authorize]
    public async Task<IActionResult> RefreshCurrentSession()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _authService.RefreshCurrentSessionAsync(userId);
            
            if (!result.Success)
                return BadRequest(result);
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = "Failed to refresh current session" });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        var result = await _authService.ForgotPasswordAsync(command);
        return Ok(result); // Always return OK for security
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await _authService.ResetPasswordAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailCommand command)
    {
        var result = await _authService.ConfirmEmailAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendEmailVerification([FromBody] ResendEmailVerificationCommand command)
    {
        var result = await _authService.ResendEmailVerificationAsync(command);
        return Ok(result); // Always return OK for security
    }

    [HttpGet("verification-status/{email}")]
    public async Task<IActionResult> GetVerificationStatus(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound(new { Error = "User not found" });
            }

            return Ok(new { 
                EmailConfirmed = user.EmailConfirmed,
                Email = user.Email,
                UserId = user.Id
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Error = "Failed to check verification status" });
        }
    }

    [HttpGet("user/{userId}")]
    [Authorize]
    public async Task<IActionResult> GetUser(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Error = "User not found" });
            }

            var userInfo = new UserInfoDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PublicLocation = user.PublicLocation,
                TermsOfServiceAccepted = user.TermsOfServiceAccepted,
                TermsVersion = user.TermsVersion,
                TermsAcceptedDate = user.TermsAcceptedDate,
                DataProcessingConsent = user.DataProcessingConsent,
                MarketingConsent = user.MarketingConsent
            };

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = "Failed to retrieve user information" });
        }
    }

    [HttpPut("user/{userId}/terms")]
    [Authorize]
    public async Task<IActionResult> UpdateUserTermsAcceptance(string userId, [FromBody] UpdateTermsRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Error = "User not found" });
            }

            // Update terms acceptance
            user.TermsOfServiceAccepted = request.TermsOfServiceAccepted;
            user.TermsVersion = request.TermsVersion;
            user.TermsAcceptedDate = request.TermsAcceptedDate;
            user.DataProcessingConsent = request.DataProcessingConsent;
            user.LastConsentUpdate = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { Error = "Failed to update user terms acceptance" });
            }

            return Ok(new { Success = true, Message = "Terms acceptance updated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = "Failed to update terms acceptance" });
        }
    }

    [HttpGet("terms-version")]
    public IActionResult GetCurrentTermsVersion()
    {
        return Ok(new
        {
            TermsVersion = VersionConstants.GetCurrentTermsVersion(),
            PrivacyVersion = VersionConstants.GetCurrentPrivacyVersion(),
            ConsentVersion = VersionConstants.GetCurrentConsentVersion()
        });
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
               ?? User.FindFirst("sub")?.Value 
               ?? User.FindFirst("UserId")?.Value 
               ?? string.Empty;
    }
}

// DTOs
public class UserInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PublicLocation { get; set; }
    public bool TermsOfServiceAccepted { get; set; }
    public string? TermsVersion { get; set; }
    public DateTime? TermsAcceptedDate { get; set; }
    public bool DataProcessingConsent { get; set; }
    public bool MarketingConsent { get; set; }
}

public class UpdateTermsRequest
{
    public bool TermsOfServiceAccepted { get; set; }
    public string TermsVersion { get; set; } = string.Empty;
    public DateTime TermsAcceptedDate { get; set; }
    public bool DataProcessingConsent { get; set; }
}