using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Configuration;
using ToolsSharing.Infrastructure.Services;

namespace ToolsSharing.Infrastructure.Middleware;

public class SessionSecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionSecurityMiddleware> _logger;
    private readonly SessionSecurityOptions _options;

    public SessionSecurityMiddleware(
        RequestDelegate next,
        ILogger<SessionSecurityMiddleware> logger,
        IOptions<SessionSecurityOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, ITokenBlacklistService tokenBlacklistService, IAuthenticationEventLogger eventLogger, IGeolocationService geolocationService, ILocationService locationService)
    {
        if (!_options.EnableSessionSecurity)
        {
            await _next(context);
            return;
        }

        try
        {
            // Skip security checks for certain paths
            if (ShouldSkipSecurityChecks(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Extract JWT token
            var token = ExtractTokenFromRequest(context.Request);
            if (string.IsNullOrEmpty(token))
            {
                await _next(context);
                return;
            }

            // Check if token is blacklisted
            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(token))
            {
                await HandleInvalidToken(context, eventLogger, "Invalid JWT format");
                return;
            }

            var jwtToken = tokenHandler.ReadJwtToken(token);
            var tokenId = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

            if (!string.IsNullOrEmpty(tokenId))
            {
                var isBlacklisted = await tokenBlacklistService.IsTokenBlacklistedAsync(tokenId);
                if (isBlacklisted)
                {
                    await HandleBlacklistedToken(context, eventLogger, tokenId);
                    return;
                }
            }

            // Validate session security if user is authenticated
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var securityValidation = await ValidateSessionSecurity(context, jwtToken, eventLogger);
                if (!securityValidation.IsValid)
                {
                    await HandleSessionSecurityViolation(context, eventLogger, securityValidation.Reason, tokenId);
                    return;
                }
            }

            // Check for sensitive operations requiring re-authentication
            if (_options.RequireReauthForSensitiveOps && IsSensitiveOperation(context.Request.Path))
            {
                var reauthValidation = await ValidateReauthentication(context, jwtToken);
                if (!reauthValidation.IsValid)
                {
                    await HandleReauthenticationRequired(context, reauthValidation.Reason);
                    return;
                }
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SessionSecurityMiddleware");
            await _next(context);
        }
    }

    private static bool ShouldSkipSecurityChecks(PathString path)
    {
        var skipPaths = new[]
        {
            "/api/auth/login",
            "/api/auth/register",
            "/api/auth/refresh-token",
            "/health",
            "/swagger"
        };

        return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ExtractTokenFromRequest(HttpRequest request)
    {
        var authHeader = request.Headers["Authorization"].FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
        {
            return authHeader["Bearer ".Length..].Trim();
        }

        return null;
    }

    private async Task<SessionSecurityValidation> ValidateSessionSecurity(HttpContext context, JwtSecurityToken jwtToken, IAuthenticationEventLogger eventLogger)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var currentIp = GetClientIpAddress(context);
        var userAgent = context.Request.Headers["User-Agent"].ToString();

        // Check for suspicious user agents
        if (_options.SuspiciousUserAgents.Any(ua => userAgent.Contains(ua, StringComparison.OrdinalIgnoreCase)))
        {
            await eventLogger.LogSuspiciousActivityAsync(
                "SuspiciousUserAgent",
                userId,
                context.User.FindFirst(ClaimTypes.Email)?.Value,
                currentIp,
                70m,
                $"Suspicious user agent detected: {userAgent}",
                userAgent
            );

            return new SessionSecurityValidation
            {
                IsValid = false,
                Reason = "Suspicious user agent detected"
            };
        }

        // Check device fingerprint if enabled
        if (_options.EnableDeviceFingerprinting)
        {
            var deviceValidation = ValidateDeviceFingerprint(jwtToken, context);
            if (!deviceValidation.IsValid)
            {
                return deviceValidation;
            }
        }

        // Check geographic validation if enabled
        if (_options.EnableGeographicValidation)
        {
            var geoValidation = await ValidateGeographicLocation(context, jwtToken, currentIp, userId, eventLogger);
            if (!geoValidation.IsValid)
            {
                return geoValidation;
            }
        }

        // Check for session hijacking indicators
        if (_options.EnableHijackingDetection)
        {
            var hijackingValidation = await ValidateSessionHijacking(jwtToken, currentIp, userAgent, userId, eventLogger);
            if (!hijackingValidation.IsValid)
            {
                return hijackingValidation;
            }
        }

        return new SessionSecurityValidation { IsValid = true };
    }

    private SessionSecurityValidation ValidateDeviceFingerprint(JwtSecurityToken jwtToken, HttpContext context)
    {
        var tokenFingerprint = jwtToken.Claims.FirstOrDefault(c => c.Type == "device_fingerprint")?.Value;
        if (string.IsNullOrEmpty(tokenFingerprint))
        {
            return new SessionSecurityValidation { IsValid = true }; // No fingerprint in token, skip validation
        }

        var currentFingerprint = GenerateDeviceFingerprint(context);
        if (tokenFingerprint != currentFingerprint)
        {
            if (_options.TerminateOnDeviceChange)
            {
                return new SessionSecurityValidation
                {
                    IsValid = false,
                    Reason = "Device fingerprint mismatch"
                };
            }
            else
            {
                _logger.LogWarning("Device fingerprint mismatch for user {UserId} but not terminating session", 
                    context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            }
        }

        return new SessionSecurityValidation { IsValid = true };
    }

    private async Task<SessionSecurityValidation> ValidateGeographicLocation(HttpContext context, JwtSecurityToken jwtToken, string currentIp, string? userId, IAuthenticationEventLogger eventLogger)
    {
        var tokenLatClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "geo_lat")?.Value;
        var tokenLngClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "geo_lng")?.Value;
        var tokenLoginTime = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Iat)?.Value;

        if (string.IsNullOrEmpty(tokenLatClaim) || string.IsNullOrEmpty(tokenLngClaim) || string.IsNullOrEmpty(tokenLoginTime))
        {
            return new SessionSecurityValidation { IsValid = true }; // No location data, skip validation
        }

        // Check if IP is in exempt ranges
        if (IsIpExempt(currentIp))
        {
            return new SessionSecurityValidation { IsValid = true };
        }

        var loginTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(tokenLoginTime)).UtcDateTime;
        var timeSinceLogin = DateTime.UtcNow - loginTime;

        // If login was recent, allow some tolerance
        if (timeSinceLogin.TotalMinutes < _options.MinLocationChangeIntervalMinutes)
        {
            return new SessionSecurityValidation { IsValid = true };
        }

        try
        {
            // Get services from DI container
            var geolocationService = context.RequestServices.GetRequiredService<IGeolocationService>();
            var locationService = context.RequestServices.GetRequiredService<ILocationService>();
            
            // Get current location from IP
            var currentLocation = await geolocationService.GetLocationAsync(currentIp);
            
            if (currentLocation?.Latitude == null || currentLocation?.Longitude == null)
            {
                _logger.LogWarning("Could not determine location for IP {IP}, skipping geographic validation", currentIp);
                return new SessionSecurityValidation { IsValid = true };
            }

            // Parse token coordinates
            if (!decimal.TryParse(tokenLatClaim, out var tokenLat) || !decimal.TryParse(tokenLngClaim, out var tokenLng))
            {
                _logger.LogWarning("Invalid coordinates in token for user {UserId}", userId);
                return new SessionSecurityValidation { IsValid = true };
            }

            // Calculate distance between login location and current location
            var distance = locationService.CalculateDistance(
                tokenLat, tokenLng,
                (decimal)currentLocation.Latitude.Value, (decimal)currentLocation.Longitude.Value
            );

            // Calculate travel speed (distance / time)
            var travelTimeHours = timeSinceLogin.TotalHours;
            var travelSpeedKmh = travelTimeHours > 0 ? distance / (decimal)travelTimeHours : 0;

            _logger.LogInformation("Geographic validation: Distance={Distance}km, Time={Time}h, Speed={Speed}km/h, MaxSpeed={MaxSpeed}km/h", 
                distance, travelTimeHours, travelSpeedKmh, _options.MaxTravelSpeedKmh);

            // Check if travel speed exceeds maximum possible speed
            if (travelSpeedKmh > (decimal)_options.MaxTravelSpeedKmh)
            {
                if (_options.TerminateOnImpossibleTravel)
                {
                    await eventLogger.LogGeographicAnomalyAsync(
                        userId,
                        null,
                        currentIp,
                        $"{currentLocation.City}, {currentLocation.CountryName}",
                        $"Token location: {tokenLat}, {tokenLng}",
                        (int)distance,
                        (int)travelSpeedKmh
                    );

                    return new SessionSecurityValidation
                    {
                        IsValid = false,
                        Reason = $"Impossible travel detected: {travelSpeedKmh:F1} km/h over {distance:F1} km in {travelTimeHours:F1} hours"
                    };
                }
                else
                {
                    // Just log the suspicious activity but don't terminate
                    await eventLogger.LogSuspiciousActivityAsync(
                        "SuspiciousTravel",
                        userId,
                        null,
                        currentIp,
                        75m, // High risk score for suspicious travel
                        $"Fast travel detected: {travelSpeedKmh:F1} km/h over {distance:F1} km",
                        context.Request.Headers["User-Agent"].ToString()
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during geographic validation for user {UserId} from IP {IP}", userId, currentIp);
            // On error, allow access but log the issue
            return new SessionSecurityValidation { IsValid = true };
        }

        return new SessionSecurityValidation { IsValid = true };
    }

    private async Task<SessionSecurityValidation> ValidateSessionHijacking(JwtSecurityToken jwtToken, string currentIp, string userAgent, string? userId, IAuthenticationEventLogger eventLogger)
    {
        var tokenIp = jwtToken.Claims.FirstOrDefault(c => c.Type == "ip_address")?.Value;
        var tokenUserAgent = jwtToken.Claims.FirstOrDefault(c => c.Type == "user_agent")?.Value;

        var riskScore = 0m;
        var suspiciousActivity = new List<string>();

        // Check IP address changes
        if (!string.IsNullOrEmpty(tokenIp) && tokenIp != currentIp)
        {
            riskScore += 30m;
            suspiciousActivity.Add("IP address changed");
        }

        // Check User-Agent changes
        if (!string.IsNullOrEmpty(tokenUserAgent) && tokenUserAgent != userAgent)
        {
            riskScore += 20m;
            suspiciousActivity.Add("User agent changed");
        }

        // Check for too many IP changes (would require session storage)
        // This is a simplified version
        if (riskScore >= _options.SuspiciousSessionThreshold)
        {
            await eventLogger.LogSuspiciousActivityAsync(
                "SessionHijackingAttempt",
                userId,
                null,
                currentIp,
                riskScore,
                $"Potential session hijacking: {string.Join(", ", suspiciousActivity)}",
                userAgent
            );

            if (_options.TerminateOnSuspectedHijacking && riskScore >= _options.TerminateSessionThreshold)
            {
                return new SessionSecurityValidation
                {
                    IsValid = false,
                    Reason = $"Session hijacking detected: {string.Join(", ", suspiciousActivity)}"
                };
            }
        }

        return new SessionSecurityValidation { IsValid = true };
    }

    private async Task<ReauthValidation> ValidateReauthentication(HttpContext context, JwtSecurityToken jwtToken)
    {
        var lastReauth = jwtToken.Claims.FirstOrDefault(c => c.Type == "last_reauth")?.Value;
        if (string.IsNullOrEmpty(lastReauth))
        {
            return new ReauthValidation
            {
                IsValid = false,
                Reason = "Re-authentication required for sensitive operation"
            };
        }

        var lastReauthTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(lastReauth)).UtcDateTime;
        var timeSinceReauth = DateTime.UtcNow - lastReauthTime;

        if (timeSinceReauth.TotalMinutes > _options.ReauthTimeoutMinutes)
        {
            return new ReauthValidation
            {
                IsValid = false,
                Reason = "Re-authentication timeout expired"
            };
        }

        return new ReauthValidation { IsValid = true };
    }

    private bool IsSensitiveOperation(PathString path)
    {
        return _options.SensitiveEndpoints.Any(endpoint => 
            path.StartsWithSegments(endpoint, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsIpExempt(string ipAddress)
    {
        // Simple IP range checking - in production, use proper CIDR matching
        return _options.ExemptIpRanges.Contains(ipAddress) ||
               ipAddress.StartsWith("127.") ||
               ipAddress.StartsWith("192.168.") ||
               ipAddress.StartsWith("10.");
    }


    private static string GenerateDeviceFingerprint(HttpContext context)
    {
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        var acceptLanguage = context.Request.Headers["Accept-Language"].ToString();
        var acceptEncoding = context.Request.Headers["Accept-Encoding"].ToString();

        var fingerprint = $"{userAgent}|{acceptLanguage}|{acceptEncoding}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(fingerprint));
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private async Task HandleInvalidToken(HttpContext context, IAuthenticationEventLogger eventLogger, string reason)
    {
        var ipAddress = GetClientIpAddress(context);
        await eventLogger.LogSuspiciousActivityAsync(
            "InvalidToken",
            null,
            null,
            ipAddress,
            60m,
            reason,
            context.Request.Headers["User-Agent"].ToString()
        );

        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Invalid authentication token");
    }

    private async Task HandleBlacklistedToken(HttpContext context, IAuthenticationEventLogger eventLogger, string tokenId)
    {
        var ipAddress = GetClientIpAddress(context);
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        await eventLogger.LogSuspiciousActivityAsync(
            "BlacklistedTokenUsage",
            userId,
            null,
            ipAddress,
            90m,
            $"Attempt to use blacklisted token: {tokenId}",
            context.Request.Headers["User-Agent"].ToString()
        );

        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Authentication token has been revoked");
    }

    private async Task HandleSessionSecurityViolation(HttpContext context, IAuthenticationEventLogger eventLogger, string reason, string? tokenId)
    {
        var ipAddress = GetClientIpAddress(context);
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        await eventLogger.LogSuspiciousActivityAsync(
            "SessionSecurityViolation",
            userId,
            null,
            ipAddress,
            85m,
            reason,
            context.Request.Headers["User-Agent"].ToString(),
            new Dictionary<string, object> { ["TokenId"] = tokenId ?? "unknown" }
        );

        context.Response.StatusCode = 401;
        await context.Response.WriteAsync($"Session security violation: {reason}");
    }

    private static async Task HandleReauthenticationRequired(HttpContext context, string reason)
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsync($"Re-authentication required: {reason}");
    }
}

public class SessionSecurityValidation
{
    public bool IsValid { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ReauthValidation
{
    public bool IsValid { get; set; }
    public string Reason { get; set; } = string.Empty;
}