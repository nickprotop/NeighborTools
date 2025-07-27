using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Entities;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RefreshTokenService> _logger;
    private readonly int _refreshTokenExpirationDays;

    public RefreshTokenService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<RefreshTokenService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        
        // Get refresh token expiration from configuration (default: 7 days)
        _refreshTokenExpirationDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpiresInDays"] ?? "7");
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<RefreshToken> StoreRefreshTokenAsync(string userId, string token, string? ipAddress = null, string? userAgent = null)
    {
        var refreshToken = new RefreshToken
        {
            Token = token,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            UserAgent = userAgent
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Refresh token stored for user {UserId}, expires at {ExpiresAt}", 
            userId, refreshToken.ExpiresAt);

        return refreshToken;
    }

    public async Task<string?> ValidateRefreshTokenAsync(string token)
    {
        var refreshToken = await _context.RefreshTokens
            .Where(rt => rt.Token == token && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (refreshToken == null)
        {
            _logger.LogWarning("Invalid refresh token attempted: {Token}", token.Substring(0, Math.Min(10, token.Length)) + "...");
            return null;
        }

        if (refreshToken.IsExpired)
        {
            _logger.LogWarning("Expired refresh token used for user {UserId}", refreshToken.UserId);
            return null;
        }

        return refreshToken.UserId;
    }

    public async Task<bool> RevokeRefreshTokenAsync(string token, string reason = "Manual revocation")
    {
        var refreshToken = await _context.RefreshTokens
            .Where(rt => rt.Token == token)
            .FirstOrDefaultAsync();

        if (refreshToken == null)
        {
            return false;
        }

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedReason = reason;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Refresh token revoked for user {UserId}: {Reason}", 
            refreshToken.UserId, reason);

        return true;
    }

    public async Task<int> RevokeAllUserRefreshTokensAsync(string userId, string reason = "Revoke all tokens")
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        var revokedCount = 0;
        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedReason = reason;
            revokedCount++;
        }

        if (revokedCount > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Revoked {Count} refresh tokens for user {UserId}: {Reason}", 
                revokedCount, userId, reason);
        }

        return revokedCount;
    }

    public async Task<int> CleanupExpiredTokensAsync()
    {
        var expiredTokens = await _context.RefreshTokens
            .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        if (expiredTokens.Any())
        {
            _context.RefreshTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} expired refresh tokens", expiredTokens.Count);
        }

        return expiredTokens.Count;
    }

    public async Task<List<RefreshToken>> GetActiveUserTokensAsync(string userId)
    {
        return await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync();
    }
}