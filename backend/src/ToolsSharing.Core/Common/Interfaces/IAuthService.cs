using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Features.Auth;

namespace ToolsSharing.Core.Common.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResult>> RegisterAsync(RegisterCommand command);
    Task<ApiResponse<AuthResult>> LoginAsync(LoginCommand command);
    Task<ApiResponse<AuthResult>> RefreshTokenAsync(RefreshTokenCommand command);
    Task<ApiResponse<AuthResult>> RefreshCurrentSessionAsync(string userId);
    Task<ApiResponse<bool>> ForgotPasswordAsync(ForgotPasswordCommand command);
    Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordCommand command);
    Task<ApiResponse<bool>> ConfirmEmailAsync(ConfirmEmailCommand command);
    Task<ApiResponse<bool>> ResendEmailVerificationAsync(ResendEmailVerificationCommand command);
}