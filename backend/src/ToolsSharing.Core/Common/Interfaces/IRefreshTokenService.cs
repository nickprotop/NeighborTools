using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.Common.Interfaces;

public interface IRefreshTokenService
{
    /// <summary>
    /// Generates a new random refresh token string
    /// </summary>
    string GenerateRefreshToken();
    
    /// <summary>
    /// Stores a refresh token in the database
    /// </summary>
    Task<RefreshToken> StoreRefreshTokenAsync(string userId, string token, string? ipAddress = null, string? userAgent = null);
    
    /// <summary>
    /// Validates a refresh token and returns the associated user ID if valid
    /// </summary>
    Task<string?> ValidateRefreshTokenAsync(string token);
    
    /// <summary>
    /// Revokes a specific refresh token
    /// </summary>
    Task<bool> RevokeRefreshTokenAsync(string token, string reason = "Manual revocation");
    
    /// <summary>
    /// Revokes all refresh tokens for a specific user
    /// </summary>
    Task<int> RevokeAllUserRefreshTokensAsync(string userId, string reason = "Revoke all tokens");
    
    /// <summary>
    /// Removes expired refresh tokens from the database
    /// </summary>
    Task<int> CleanupExpiredTokensAsync();
    
    /// <summary>
    /// Gets active refresh tokens for a user (for admin purposes)
    /// </summary>
    Task<List<RefreshToken>> GetActiveUserTokensAsync(string userId);
}