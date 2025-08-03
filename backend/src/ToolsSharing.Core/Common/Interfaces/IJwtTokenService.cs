using System.Security.Claims;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.Common.Interfaces;

public interface IJwtTokenService
{
    Task<string> GenerateAccessTokenAsync(User user);
    Task<string> GenerateAccessTokenAsync(User user, int timeoutMinutes);
    Task<string> GenerateAccessTokenWithReauthAsync(User user, int timeoutMinutes);
    string GenerateRefreshToken();
    bool ValidateToken(string token);
    string GetUserIdFromToken(string token);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}