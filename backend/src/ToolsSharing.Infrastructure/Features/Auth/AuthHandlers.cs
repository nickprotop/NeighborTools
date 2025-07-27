using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Common.Models.EmailNotifications;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Features.Auth;
using ToolsSharing.Core.Common.Constants;
using ToolsSharing.Core.Interfaces.GDPR;
using ToolsSharing.Core.Interfaces;
using System.Web;

namespace ToolsSharing.Infrastructure.Features.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly IConsentService _consentService;
    private readonly ISettingsService _settingsService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<User> userManager, 
        IJwtTokenService jwtTokenService, 
        IEmailNotificationService emailNotificationService,
        IConsentService consentService,
        ISettingsService settingsService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _emailNotificationService = emailNotificationService;
        _consentService = consentService;
        _settingsService = settingsService;
        _configuration = configuration;
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
            
            // Create new user with unconfirmed email
            var user = new User
            {
                UserName = command.Email,
                Email = command.Email,
                EmailConfirmed = false, // Require email verification
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

            // Send email verification
            try
            {
                var verificationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5000";
                var verificationUrl = $"{frontendBaseUrl}/verify-email?userId={HttpUtility.UrlEncode(user.Id)}&token={HttpUtility.UrlEncode(verificationToken)}";
                
                var emailVerificationNotification = new EmailVerificationNotification
                {
                    RecipientEmail = user.Email!,
                    RecipientName = $"{user.FirstName} {user.LastName}",
                    UserId = user.Id,
                    UserName = user.FirstName,
                    VerificationToken = verificationToken,
                    VerificationUrl = verificationUrl,
                    Priority = EmailPriority.High
                };

                await _emailNotificationService.SendNotificationAsync(emailVerificationNotification);
                _logger.LogInformation("Email verification sent to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email verification to {Email}", user.Email);
                // Don't fail registration if email fails, but log the error
            }

            // Return registration result WITHOUT tokens (user must verify email first)
            var authResult = new AuthResult
            {
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AccessToken = "", // No access until email verified
                RefreshToken = "", // No refresh token until email verified
                ExpiresAt = DateTime.UtcNow, // Expired immediately
                EmailVerificationRequired = true
            };

            return ApiResponse<AuthResult>.CreateSuccess(authResult, "Registration successful. Please check your email to verify your account before logging in.");
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

            // Check if email is verified
            if (!user.EmailConfirmed)
            {
                return ApiResponse<AuthResult>.CreateFailure("Please verify your email address before logging in. Check your inbox for a verification email.");
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

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);

            var authResult = new AuthResult
            {
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(sessionTimeoutMinutes), // Match JWT expiration with user preference
                Roles = roles.ToList(),
                IsAdmin = roles.Contains("Admin")
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

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);

            var authResult = new AuthResult
            {
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(sessionTimeoutMinutes),
                Roles = roles.ToList(),
                IsAdmin = roles.Contains("Admin")
            };

            return ApiResponse<AuthResult>.CreateSuccess(authResult, "Token refreshed successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<AuthResult>.CreateFailure($"Token refresh failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<AuthResult>> RefreshCurrentSessionAsync(string userId)
    {
        try
        {
            // Find the user
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<AuthResult>.CreateFailure("User not found");
            }

            // Get user's current session timeout preference
            int sessionTimeoutMinutes = 60; // Default fallback
            try
            {
                var userSettings = await _settingsService.GetUserSettingsAsync(user.Id);
                if (userSettings != null)
                {
                    sessionTimeoutMinutes = userSettings.Security.SessionTimeoutMinutes;
                    _logger.LogInformation("Refreshing current session with updated timeout: {TimeoutMinutes} minutes for user {UserId}", 
                        sessionTimeoutMinutes, user.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load user settings for session refresh, using default timeout: {TimeoutMinutes} minutes for user {UserId}", 
                    sessionTimeoutMinutes, user.Id);
            }

            // Generate new tokens with current user settings
            var newAccessToken = await _jwtTokenService.GenerateAccessTokenAsync(user, sessionTimeoutMinutes);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);

            var authResult = new AuthResult
            {
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(sessionTimeoutMinutes),
                Roles = roles.ToList(),
                IsAdmin = roles.Contains("Admin")
            };

            return ApiResponse<AuthResult>.CreateSuccess(authResult, "Current session refreshed with updated settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh current session for user {UserId}", userId);
            return ApiResponse<AuthResult>.CreateFailure($"Session refresh failed: {ex.Message}");
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
            
            // Send password reset email using the new notification system
            try
            {
                var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5000";
                var resetUrl = $"{frontendBaseUrl}/reset-password?email={HttpUtility.UrlEncode(user.Email)}&token={HttpUtility.UrlEncode(token)}";
                
                var passwordResetNotification = new PasswordResetNotification
                {
                    RecipientEmail = user.Email!,
                    RecipientName = $"{user.FirstName} {user.LastName}",
                    UserId = user.Id,
                    UserName = user.FirstName,
                    ResetToken = token,
                    ResetUrl = resetUrl,
                    ExpiresAt = DateTime.UtcNow.AddHours(24), // Password reset tokens expire in 24 hours
                    Priority = EmailPriority.High
                };

                await _emailNotificationService.SendNotificationAsync(passwordResetNotification);
                _logger.LogInformation("Password reset email sent to {Email}", user.Email);
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

    public async Task<ApiResponse<bool>> ConfirmEmailAsync(ConfirmEmailCommand command)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(command.UserId);
            if (user == null)
            {
                return ApiResponse<bool>.CreateFailure("Invalid verification request");
            }

            if (user.EmailConfirmed)
            {
                return ApiResponse<bool>.CreateSuccess(true, "Email already verified");
            }

            var result = await _userManager.ConfirmEmailAsync(user, command.Token);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Email verification failed for user {UserId}: {Errors}", user.Id, errors);
                return ApiResponse<bool>.CreateFailure("Invalid or expired verification token");
            }

            // Send welcome email after successful verification
            try
            {
                var welcomeNotification = new WelcomeEmailNotification
                {
                    RecipientEmail = user.Email!,
                    RecipientName = $"{user.FirstName} {user.LastName}",
                    UserId = user.Id,
                    UserName = user.FirstName,
                    Priority = EmailPriority.Normal
                };

                await _emailNotificationService.SendNotificationAsync(welcomeNotification);
                _logger.LogInformation("Welcome email sent to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
                // Don't fail verification if welcome email fails
            }

            _logger.LogInformation("Email verified successfully for user {UserId}", user.Id);
            return ApiResponse<bool>.CreateSuccess(true, "Email verified successfully! You can now log in to your account.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email verification failed for user {UserId}", command.UserId);
            return ApiResponse<bool>.CreateFailure("Email verification failed");
        }
    }

    public async Task<ApiResponse<bool>> ResendEmailVerificationAsync(ResendEmailVerificationCommand command)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(command.Email);
            if (user == null)
            {
                // For security, always return success even if user doesn't exist
                return ApiResponse<bool>.CreateSuccess(true, "If the email is registered and unverified, a verification email has been sent");
            }

            if (user.EmailConfirmed)
            {
                return ApiResponse<bool>.CreateSuccess(true, "Email is already verified");
            }

            // Send verification email
            try
            {
                var verificationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5000";
                var verificationUrl = $"{frontendBaseUrl}/verify-email?userId={HttpUtility.UrlEncode(user.Id)}&token={HttpUtility.UrlEncode(verificationToken)}";
                
                var emailVerificationNotification = new EmailVerificationNotification
                {
                    RecipientEmail = user.Email!,
                    RecipientName = $"{user.FirstName} {user.LastName}",
                    UserId = user.Id,
                    UserName = user.FirstName,
                    VerificationToken = verificationToken,
                    VerificationUrl = verificationUrl,
                    Priority = EmailPriority.High
                };

                await _emailNotificationService.SendNotificationAsync(emailVerificationNotification);
                _logger.LogInformation("Email verification resent to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend email verification to {Email}", user.Email);
                return ApiResponse<bool>.CreateFailure("Failed to send verification email");
            }

            return ApiResponse<bool>.CreateSuccess(true, "Verification email sent. Please check your inbox.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend email verification for {Email}", command.Email);
            return ApiResponse<bool>.CreateSuccess(true, "If the email is registered and unverified, a verification email has been sent");
        }
    }
}