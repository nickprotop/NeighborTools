using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Interfaces;

namespace ToolsSharing.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiresInMinutes;

    public JwtTokenService(IConfiguration configuration, IServiceProvider serviceProvider, IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _httpContextAccessor = httpContextAccessor;
        _secretKey = _configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        _issuer = _configuration["JwtSettings:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured");
        _audience = _configuration["JwtSettings:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured");
        _expiresInMinutes = int.Parse(_configuration["JwtSettings:ExpiresInMinutes"] ?? "60");
    }

    public async Task<string> GenerateAccessTokenAsync(User user)
    {
        return await GenerateAccessTokenAsync(user, _expiresInMinutes);
    }

    public async Task<string> GenerateAccessTokenAsync(User user, int timeoutMinutes)
    {
        return await GenerateAccessTokenAsync(user, timeoutMinutes, updateReauth: false);
    }

    public async Task<string> GenerateAccessTokenWithReauthAsync(User user, int timeoutMinutes)
    {
        return await GenerateAccessTokenAsync(user, timeoutMinutes, updateReauth: true);
    }

    private async Task<string> GenerateAccessTokenAsync(User user, int timeoutMinutes, bool updateReauth)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new("UserId", user.Id),
            new(JwtRegisteredClaimNames.Iat, currentTime.ToString(), ClaimValueTypes.Integer64)
        };

        // Add last_reauth claim - either current time for new auth, or preserve from existing token
        if (updateReauth)
        {
            claims.Add(new("last_reauth", currentTime.ToString(), ClaimValueTypes.Integer64));
        }
        else
        {
            // For regular token refresh, check if we can preserve existing last_reauth from current token
            var existingReauth = await GetExistingReauthFromContextAsync();
            var reauthTime = existingReauth ?? currentTime; // Use current time for new logins
            claims.Add(new("last_reauth", reauthTime.ToString(), ClaimValueTypes.Integer64));
        }

        // Add security-related claims from HTTP context
        await AddSecurityClaimsAsync(claims);

        // Get user roles from UserManager
        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roles = await userManager.GetRolesAsync(user);
        
        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        
        // Add IsAdmin claim for easier frontend checking
        if (roles.Contains("Admin"))
        {
            claims.Add(new Claim("IsAdmin", "true"));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(timeoutMinutes),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public bool ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.FromMinutes(5)
            }, out SecurityToken validatedToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GetUserIdFromToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            throw new ArgumentException("Token cannot be null or empty");

        var tokenHandler = new JwtSecurityTokenHandler();
        var jsonToken = tokenHandler.ReadJwtToken(token);

        var userIdClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == "UserId");
        return userIdClaim?.Value ?? throw new InvalidOperationException("UserId claim not found in token");
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false, // Don't validate lifetime for refresh tokens
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.FromMinutes(5)
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch
        {
            return null;
        }
    }

    private async Task AddSecurityClaimsAsync(List<Claim> claims)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return;

        try
        {
            // Add IP address
            var ipAddress = GetClientIpAddress(context);
            if (!string.IsNullOrEmpty(ipAddress))
            {
                claims.Add(new Claim("ip_address", ipAddress));
            }

            // Add User Agent
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            if (!string.IsNullOrEmpty(userAgent))
            {
                claims.Add(new Claim("user_agent", userAgent));
            }

            // Add Device Fingerprint
            var deviceFingerprint = GenerateDeviceFingerprint(context);
            if (!string.IsNullOrEmpty(deviceFingerprint))
            {
                claims.Add(new Claim("device_fingerprint", deviceFingerprint));
            }

            // Add Geographic Location
            await AddGeographicClaimsAsync(claims, ipAddress);
        }
        catch (Exception ex)
        {
            // Log error but don't fail token generation
            using var scope = _serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetService<ILogger<JwtTokenService>>();
            logger?.LogWarning(ex, "Failed to add security claims to JWT token");
        }
    }

    private async Task AddGeographicClaimsAsync(List<Claim> claims, string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress) || IsLocalOrPrivateIP(ipAddress)) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var geolocationService = scope.ServiceProvider.GetService<IGeolocationService>();
            
            if (geolocationService != null)
            {
                var location = await geolocationService.GetLocationAsync(ipAddress);
                if (location?.Latitude != null && location?.Longitude != null)
                {
                    claims.Add(new Claim("geo_lat", location.Latitude.Value.ToString()));
                    claims.Add(new Claim("geo_lng", location.Longitude.Value.ToString()));
                }
            }
        }
        catch
        {
            // Silently fail for geographic claims to not block login
        }
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check X-Forwarded-For header (for load balancers/proxies)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check X-Real-IP header (for nginx/Apache)
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static string GenerateDeviceFingerprint(HttpContext context)
    {
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        var acceptLanguage = context.Request.Headers["Accept-Language"].ToString();
        var acceptEncoding = context.Request.Headers["Accept-Encoding"].ToString();

        var fingerprint = $"{userAgent}|{acceptLanguage}|{acceptEncoding}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(fingerprint));
    }

    private static bool IsLocalOrPrivateIP(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress) || ipAddress == "unknown")
            return true;

        if (!System.Net.IPAddress.TryParse(ipAddress, out var ip))
            return true;

        // Check for localhost
        if (System.Net.IPAddress.IsLoopback(ip))
            return true;

        // Check for private IP ranges
        var bytes = ip.GetAddressBytes();
        
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            // IPv4 private ranges
            return (bytes[0] == 10) ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 169 && bytes[1] == 254); // Link-local
        }
        
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            // IPv6 private ranges
            return ip.ToString().StartsWith("fc00:") ||  // Unique local
                   ip.ToString().StartsWith("fd00:") ||  // Unique local
                   ip.ToString().StartsWith("fe80:");    // Link-local
        }

        return false;
    }

    private async Task<long?> GetExistingReauthFromContextAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.User?.Identity?.IsAuthenticated == true)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
            {
                var token = authHeader["Bearer ".Length..].Trim();
                var tokenHandler = new JwtSecurityTokenHandler();
                
                try
                {
                    if (tokenHandler.CanReadToken(token))
                    {
                        var jwtToken = tokenHandler.ReadJwtToken(token);
                        var lastReauthClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "last_reauth")?.Value;
                        
                        if (!string.IsNullOrEmpty(lastReauthClaim) && long.TryParse(lastReauthClaim, out var timestamp))
                        {
                            return timestamp;
                        }
                    }
                }
                catch
                {
                    // If token parsing fails, return null to use current time
                }
            }
        }
        
        return null;
    }
}