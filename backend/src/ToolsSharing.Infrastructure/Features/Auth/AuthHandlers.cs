using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Features.Auth;
using ToolsSharing.Core.Common.Constants;
using ToolsSharing.Core.Interfaces.GDPR;
using ToolsSharing.Core.Interfaces;

namespace ToolsSharing.Infrastructure.Features.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly IConsentService _consentService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<User> userManager, 
        IJwtTokenService jwtTokenService, 
        IEmailService emailService,
        IConsentService consentService,
        ISettingsService settingsService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _consentService = consentService;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<ApiResponse<AuthResult>> RegisterAsync(RegisterCommand command)
    {
        try
        {
            // Validate required terms acceptance
            if (!command.AcceptTerms)
            {
                return ApiResponse<AuthResult>.CreateFailure("You must accept the Terms of Service to create an account");
            }

            if (!command.AcceptPrivacyPolicy)
            {
                return ApiResponse<AuthResult>.CreateFailure("You must accept the Privacy Policy to create an account");
            }

            if (!command.AcceptDataProcessing)
            {
                return ApiResponse<AuthResult>.CreateFailure("You must consent to data processing to create an account");
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(command.Email);
            if (existingUser != null)
            {
                return ApiResponse<AuthResult>.CreateFailure("User with this email already exists");
            }

            var currentTime = DateTime.UtcNow;
            
            // Create new user
            var user = new User
            {
                UserName = command.Email,
                Email = command.Email,
                FirstName = command.FirstName,
                LastName = command.LastName,
                PhoneNumber = command.PhoneNumber,
                DateOfBirth = command.DateOfBirth,
                Address = command.Address,
                City = command.City,
                PostalCode = command.PostalCode,
                Country = command.Country,
                CreatedAt = currentTime,
                UpdatedAt = currentTime,
                
                // GDPR and Terms acceptance
                TermsOfServiceAccepted = command.AcceptTerms,
                TermsAcceptedDate = currentTime,
                TermsVersion = VersionConstants.GetCurrentTermsVersion(),
                DataProcessingConsent = command.AcceptDataProcessing,
                MarketingConsent = command.AcceptMarketing,
                LastConsentUpdate = currentTime
            };

            var result = await _userManager.CreateAsync(user, command.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ApiResponse<AuthResult>.CreateFailure($"Failed to create user: {errors}");
            }

            // Record initial consents in UserConsents table for GDPR audit trail
            await _consentService.RecordInitialConsentsAsync(
                user.Id,
                command.AcceptDataProcessing,
                command.AcceptMarketing,
                "registration",
                null, // IP address would be captured by the controller
                null  // User agent would be captured by the controller
            );

            // Generate tokens
            var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            var authResult = new AuthResult
            {
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60) // Should match JWT expiration
            };

            return ApiResponse<AuthResult>.CreateSuccess(authResult, "User registered successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<AuthResult>.CreateFailure($"Registration failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<AuthResult>> LoginAsync(LoginCommand command)
    {
        try
        {
            // Find user by email
            var user = await _userManager.FindByEmailAsync(command.Email);
            if (user == null)
            {
                return ApiResponse<AuthResult>.CreateFailure("Invalid email or password");
            }

            // Check password
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, command.Password);
            if (!isPasswordValid)
            {
                return ApiResponse<AuthResult>.CreateFailure("Invalid email or password");
            }

            // Sync consents for existing users who might not have UserConsent records
            await _consentService.SyncAllUserConsentsFromEntityAsync(user.Id);

            // Get user's session timeout preference
            int sessionTimeoutMinutes = 60; // Default fallback
            try
            {
                var userSettings = await _settingsService.GetUserSettingsAsync(user.Id);
                if (userSettings != null)
                {
                    sessionTimeoutMinutes = userSettings.Security.SessionTimeoutMinutes;
                    _logger.LogInformation("Using user's session timeout preference: {TimeoutMinutes} minutes for user {UserId}", 
                        sessionTimeoutMinutes, user.Id);
                }
                else
                {
                    _logger.LogInformation("No user settings found, using default session timeout: {TimeoutMinutes} minutes for user {UserId}", 
                        sessionTimeoutMinutes, user.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load user settings for session timeout, using default: {TimeoutMinutes} minutes for user {UserId}", 
                    sessionTimeoutMinutes, user.Id);
            }

            // Generate tokens with user's preferred timeout
            var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user, sessionTimeoutMinutes);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            var authResult = new AuthResult
            {
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(sessionTimeoutMinutes) // Match JWT expiration with user preference
            };

            return ApiResponse<AuthResult>.CreateSuccess(authResult, "Login successful");
        }
        catch (Exception ex)
        {
            return ApiResponse<AuthResult>.CreateFailure($"Login failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<AuthResult>> RefreshTokenAsync(RefreshTokenCommand command)
    {
        try
        {
            // Validate refresh token
            var principal = _jwtTokenService.GetPrincipalFromExpiredToken(command.RefreshToken);
            if (principal == null)
            {
                return ApiResponse<AuthResult>.CreateFailure("Invalid refresh token");
            }

            var userId = principal.FindFirst("sub")?.Value ?? principal.FindFirst("nameidentifier")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return ApiResponse<AuthResult>.CreateFailure("Invalid token claims");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<AuthResult>.CreateFailure("User not found");
            }

            // Get user's session timeout preference for refresh token
            int sessionTimeoutMinutes = 60; // Default fallback
            try
            {
                var userSettings = await _settingsService.GetUserSettingsAsync(user.Id);
                if (userSettings != null)
                {
                    sessionTimeoutMinutes = userSettings.Security.SessionTimeoutMinutes;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load user settings for token refresh, using default timeout: {TimeoutMinutes} minutes for user {UserId}", 
                    sessionTimeoutMinutes, user.Id);
            }

            // Generate new tokens with user's preferred timeout
            var newAccessToken = await _jwtTokenService.GenerateAccessTokenAsync(user, sessionTimeoutMinutes);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

            var authResult = new AuthResult
            {
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(sessionTimeoutMinutes)
            };

            return ApiResponse<AuthResult>.CreateSuccess(authResult, "Token refreshed successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<AuthResult>.CreateFailure($"Token refresh failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> ForgotPasswordAsync(ForgotPasswordCommand command)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(command.Email);
            if (user == null)
            {
                // For security, always return success even if user doesn't exist
                return ApiResponse<bool>.CreateSuccess(true, "If the email exists, a reset link has been sent");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            
            // Send email with reset token
            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, token, $"{user.FirstName} {user.LastName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
                // Continue execution - don't fail the request if email fails
            }
            
            return ApiResponse<bool>.CreateSuccess(true, "If the email exists, a reset link has been sent");
        }
        catch (Exception)
        {
            return ApiResponse<bool>.CreateSuccess(true, "If the email exists, a reset link has been sent");
        }
    }

    public async Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordCommand command)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(command.Email);
            if (user == null)
            {
                return ApiResponse<bool>.CreateFailure("Invalid reset request");
            }

            var result = await _userManager.ResetPasswordAsync(user, command.Token, command.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ApiResponse<bool>.CreateFailure($"Password reset failed: {errors}");
            }

            return ApiResponse<bool>.CreateSuccess(true, "Password reset successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.CreateFailure($"Password reset failed: {ex.Message}");
        }
    }
}