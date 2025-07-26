using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Configuration;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services;

public class AuthenticationEventLogger : IAuthenticationEventLogger
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthenticationEventLogger> _logger;
    private readonly AuthenticationLoggingOptions _options;
    private readonly IGeolocationService _geolocationService;
    private readonly IEmailNotificationService _emailService;

    public AuthenticationEventLogger(
        ApplicationDbContext context,
        ILogger<AuthenticationEventLogger> logger,
        IOptions<AuthenticationLoggingOptions> options,
        IGeolocationService geolocationService,
        IEmailNotificationService emailService)
    {
        _context = context;
        _logger = logger;
        _options = options.Value;
        _geolocationService = geolocationService;
        _emailService = emailService;
    }

    public bool IsLoggingEnabled => _options.EnableAuthenticationLogging;

    public async Task LogLoginSuccessAsync(string? userId, string? userEmail, string ipAddress, string? userAgent = null, string? sessionId = null, Dictionary<string, object>? additionalData = null)
    {
        if (!IsLoggingEnabled || !_options.LogSuccessfulLogins)
            return;

        await CreateSecurityEventAsync(SecurityEventTypes.Login, userId, userEmail, ipAddress, true, null, userAgent, sessionId, additionalData);
    }

    public async Task LogLoginFailureAsync(string? userEmail, string ipAddress, string failureReason, string? userAgent = null, string? sessionId = null, Dictionary<string, object>? additionalData = null)
    {
        if (!IsLoggingEnabled || !_options.LogFailedLogins)
            return;

        await CreateSecurityEventAsync(SecurityEventTypes.LoginFailed, null, userEmail, ipAddress, false, failureReason, userAgent, sessionId, additionalData);
    }

    public async Task LogPasswordChangeAsync(string userId, string ipAddress, bool successful, string? userAgent = null, Dictionary<string, object>? additionalData = null)
    {
        if (!IsLoggingEnabled || !_options.LogPasswordChanges)
            return;

        await CreateSecurityEventAsync(SecurityEventTypes.PasswordChange, userId, null, ipAddress, successful, 
            successful ? null : "Password change failed", userAgent, null, additionalData);
    }

    public async Task LogAccountLockoutAsync(string? userId, string? userEmail, string ipAddress, string reason, TimeSpan? lockoutDuration = null, Dictionary<string, object>? additionalData = null)
    {
        if (!IsLoggingEnabled || !_options.LogAccountLockouts)
            return;

        var data = additionalData ?? new Dictionary<string, object>();
        if (lockoutDuration.HasValue)
        {
            data["LockoutDurationMinutes"] = lockoutDuration.Value.TotalMinutes;
        }

        await CreateSecurityEventAsync(SecurityEventTypes.AccountLockout, userId, userEmail, ipAddress, true, reason, null, null, data);

        if (_options.EnableRealTimeAlerts && _options.AlertOnBruteForce)
        {
            var eventObj = await GetLatestSecurityEventAsync(SecurityEventTypes.AccountLockout, ipAddress);
            if (eventObj != null)
            {
                await SendSecurityAlertAsync(eventObj);
            }
        }
    }

    public async Task LogAccountUnlockAsync(string? userId, string? userEmail, string ipAddress, string reason, string? unlockedBy = null, Dictionary<string, object>? additionalData = null)
    {
        if (!IsLoggingEnabled || !_options.LogAccountLockouts)
            return;

        var data = additionalData ?? new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(unlockedBy))
        {
            data["UnlockedBy"] = unlockedBy;
        }

        await CreateSecurityEventAsync(SecurityEventTypes.AccountUnlock, userId, userEmail, ipAddress, true, reason, null, null, data);
    }

    public async Task LogSessionCreatedAsync(string userId, string sessionId, string ipAddress, string? userAgent = null, Dictionary<string, object>? additionalData = null)
    {
        if (!IsLoggingEnabled || !_options.LogSessionEvents)
            return;

        await CreateSecurityEventAsync(SecurityEventTypes.SessionCreated, userId, null, ipAddress, true, null, userAgent, sessionId, additionalData);
    }

    public async Task LogSessionTerminatedAsync(string userId, string sessionId, string ipAddress, string reason, Dictionary<string, object>? additionalData = null)
    {
        if (!IsLoggingEnabled || !_options.LogSessionEvents)
            return;

        await CreateSecurityEventAsync(SecurityEventTypes.SessionTerminated, userId, null, ipAddress, true, reason, null, sessionId, additionalData);
    }

    public async Task LogTokenBlacklistedAsync(string tokenId, string? userId, string ipAddress, string reason, string? blacklistedBy = null, Dictionary<string, object>? additionalData = null)
    {
        if (!IsLoggingEnabled || !_options.LogTokenEvents)
            return;

        var data = additionalData ?? new Dictionary<string, object>();
        data["TokenId"] = tokenId;
        if (!string.IsNullOrEmpty(blacklistedBy))
        {
            data["BlacklistedBy"] = blacklistedBy;
        }

        await CreateSecurityEventAsync(SecurityEventTypes.TokenBlacklisted, userId, null, ipAddress, true, reason, null, null, data);
    }

    public async Task LogSuspiciousActivityAsync(string eventType, string? userId, string? userEmail, string ipAddress, decimal riskScore, string description, string? userAgent = null, Dictionary<string, object>? additionalData = null)
    {
        if (!IsLoggingEnabled || !_options.LogSuspiciousActivity)
            return;

        var data = additionalData ?? new Dictionary<string, object>();
        data["Description"] = description;
        data["RiskScore"] = riskScore;

        await CreateSecurityEventAsync(eventType, userId, userEmail, ipAddress, false, description, userAgent, null, data, riskScore);

        if (_options.EnableRealTimeAlerts && riskScore >= 80)
        {
            var eventObj = await GetLatestSecurityEventAsync(eventType, ipAddress);
            if (eventObj != null)
            {
                await SendSecurityAlertAsync(eventObj);
            }
        }
    }

    public async Task LogBruteForceAttemptAsync(string attackType, string sourceIdentifier, string? targetIdentifier, string ipAddress, int attemptCount, decimal riskScore, Dictionary<string, object>? additionalData = null)
    {
        if (!IsLoggingEnabled || !_options.LogSuspiciousActivity)
            return;

        var data = additionalData ?? new Dictionary<string, object>();
        data["AttackType"] = attackType;
        data["SourceIdentifier"] = sourceIdentifier;
        data["AttemptCount"] = attemptCount;
        if (!string.IsNullOrEmpty(targetIdentifier))
        {
            data["TargetIdentifier"] = targetIdentifier;
        }

        await CreateSecurityEventAsync(SecurityEventTypes.BruteForceAttempt, null, targetIdentifier, ipAddress, false, 
            $"Brute force attack detected: {attackType}", null, null, data, riskScore);

        if (_options.EnableRealTimeAlerts && _options.AlertOnBruteForce)
        {
            var eventObj = await GetLatestSecurityEventAsync(SecurityEventTypes.BruteForceAttempt, ipAddress);
            if (eventObj != null)
            {
                await SendSecurityAlertAsync(eventObj);
            }
        }
    }

    public async Task LogGeographicAnomalyAsync(string? userId, string? userEmail, string ipAddress, string currentLocation, string previousLocation, double travelDistance, double travelSpeed, Dictionary<string, object>? additionalData = null)
    {
        if (!IsLoggingEnabled || !_options.LogSuspiciousActivity)
            return;

        var data = additionalData ?? new Dictionary<string, object>();
        data["CurrentLocation"] = currentLocation;
        data["PreviousLocation"] = previousLocation;
        data["TravelDistanceKm"] = travelDistance;
        data["TravelSpeedKmh"] = travelSpeed;

        var riskScore = CalculateGeographicRiskScore(travelSpeed, travelDistance);

        await CreateSecurityEventAsync(SecurityEventTypes.GeographicAnomaly, userId, userEmail, ipAddress, false,
            $"Impossible travel detected: {travelSpeed:F1} km/h over {travelDistance:F1} km", null, null, data, riskScore);

        if (_options.EnableRealTimeAlerts && _options.AlertOnGeographicAnomalies)
        {
            var eventObj = await GetLatestSecurityEventAsync(SecurityEventTypes.GeographicAnomaly, ipAddress);
            if (eventObj != null)
            {
                await SendSecurityAlertAsync(eventObj);
            }
        }
    }

    public async Task LogSessionHijackingAttemptAsync(string userId, string sessionId, string ipAddress, string reason, decimal riskScore, Dictionary<string, object>? additionalData = null)
    {
        if (!IsLoggingEnabled || !_options.LogSuspiciousActivity)
            return;

        var data = additionalData ?? new Dictionary<string, object>();
        data["Reason"] = reason;

        await CreateSecurityEventAsync(SecurityEventTypes.SessionHijackingAttempt, userId, null, ipAddress, false, reason, null, sessionId, data, riskScore);

        if (_options.EnableRealTimeAlerts && _options.AlertOnSessionHijacking)
        {
            var eventObj = await GetLatestSecurityEventAsync(SecurityEventTypes.SessionHijackingAttempt, ipAddress);
            if (eventObj != null)
            {
                await SendSecurityAlertAsync(eventObj);
            }
        }
    }

    public async Task LogConcurrentSessionViolationAsync(string userId, string ipAddress, int sessionCount, int maxAllowed, string action, Dictionary<string, object>? additionalData = null)
    {
        if (!IsLoggingEnabled || !_options.LogSessionEvents)
            return;

        var data = additionalData ?? new Dictionary<string, object>();
        data["SessionCount"] = sessionCount;
        data["MaxAllowed"] = maxAllowed;
        data["Action"] = action;

        await CreateSecurityEventAsync(SecurityEventTypes.ConcurrentSessionViolation, userId, null, ipAddress, false,
            $"Concurrent session limit exceeded: {sessionCount}/{maxAllowed}", null, null, data, 60m);
    }

    public async Task<List<SecurityEvent>> GetUserSecurityEventsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, int? limit = null)
    {
        var query = _context.SecurityEvents
            .Where(se => se.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(se => se.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(se => se.CreatedAt <= endDate.Value);

        query = query.OrderByDescending(se => se.CreatedAt);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync();
    }

    public async Task<List<SecurityEvent>> GetSecurityEventsByTypeAsync(string eventType, DateTime? startDate = null, DateTime? endDate = null, int? limit = null)
    {
        var query = _context.SecurityEvents
            .Where(se => se.EventType == eventType);

        if (startDate.HasValue)
            query = query.Where(se => se.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(se => se.CreatedAt <= endDate.Value);

        query = query.OrderByDescending(se => se.CreatedAt);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync();
    }

    public async Task<List<SecurityEvent>> GetSecurityEventsByIpAsync(string ipAddress, DateTime? startDate = null, DateTime? endDate = null, int? limit = null)
    {
        var query = _context.SecurityEvents
            .Where(se => se.IPAddress == ipAddress);

        if (startDate.HasValue)
            query = query.Where(se => se.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(se => se.CreatedAt <= endDate.Value);

        query = query.OrderByDescending(se => se.CreatedAt);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync();
    }

    public async Task<List<SecurityEvent>> GetHighRiskEventsAsync(decimal minRiskScore, DateTime? startDate = null, DateTime? endDate = null, int? limit = null)
    {
        var query = _context.SecurityEvents
            .Where(se => se.RiskScore >= minRiskScore);

        if (startDate.HasValue)
            query = query.Where(se => se.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(se => se.CreatedAt <= endDate.Value);

        query = query.OrderByDescending(se => se.RiskScore).ThenByDescending(se => se.CreatedAt);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync();
    }

    public async Task SendSecurityAlertAsync(SecurityEvent securityEvent)
    {
        if (!_options.EnableRealTimeAlerts)
            return;

        try
        {
            // Check if we've already sent too many alerts this hour
            var alertCount = await GetRecentAlertCountAsync();
            if (alertCount >= _options.MaxAlertsPerHour)
            {
                _logger.LogWarning("Alert rate limit exceeded. Skipping alert for event {EventId}", securityEvent.Id);
                return;
            }

            var alertMessage = FormatSecurityAlert(securityEvent);

            // Send email notifications
            if (_options.EnableEmailNotifications && _options.EmailRecipients.Any())
            {
                foreach (var recipient in _options.EmailRecipients)
                {
                    await _emailService.SendSecurityAlertAsync(recipient, $"Security Alert: {securityEvent.EventType}", alertMessage);
                }
            }

            // Send Slack notifications
            if (_options.EnableSlackNotifications && !string.IsNullOrEmpty(_options.SlackWebhookUrl))
            {
                await SendSlackNotificationAsync(securityEvent, alertMessage);
            }

            await RecordAlertSentAsync(securityEvent.Id);

            _logger.LogInformation("Security alert sent for event {EventId} of type {EventType}", securityEvent.Id, securityEvent.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending security alert for event {EventId}", securityEvent.Id);
        }
    }

    public async Task<SecurityEventStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var events = await _context.SecurityEvents
            .Where(se => se.CreatedAt >= start && se.CreatedAt <= end)
            .ToListAsync();

        var statistics = new SecurityEventStatistics
        {
            TotalEvents = events.Count,
            SuccessfulLogins = events.Count(e => e.EventType == SecurityEventTypes.Login && e.Success),
            FailedLogins = events.Count(e => e.EventType == SecurityEventTypes.LoginFailed),
            SuspiciousActivities = events.Count(e => e.EventType == SecurityEventTypes.SuspiciousActivity),
            BruteForceAttempts = events.Count(e => e.EventType == SecurityEventTypes.BruteForceAttempt),
            GeographicAnomalies = events.Count(e => e.EventType == SecurityEventTypes.GeographicAnomaly),
            SessionHijackingAttempts = events.Count(e => e.EventType == SecurityEventTypes.SessionHijackingAttempt),
            AccountLockouts = events.Count(e => e.EventType == SecurityEventTypes.AccountLockout),
            TokensBlacklisted = events.Count(e => e.EventType == SecurityEventTypes.TokenBlacklisted),
            PeriodStart = start,
            PeriodEnd = end
        };

        // Group by event type
        statistics.EventsByType = events
            .GroupBy(e => e.EventType)
            .ToDictionary(g => g.Key, g => g.Count());

        // Group by hour for timeline visualization
        statistics.EventsByHour = events
            .GroupBy(e => e.CreatedAt.Hour)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        // Calculate risk scores
        var eventsWithRisk = events.Where(e => e.RiskScore.HasValue).ToList();
        if (eventsWithRisk.Any())
        {
            statistics.AverageRiskScore = eventsWithRisk.Average(e => e.RiskScore!.Value);
            statistics.MaxRiskScore = eventsWithRisk.Max(e => e.RiskScore!.Value);
        }

        // Get geographic data if available
        var eventsWithLocation = events.Where(e => !string.IsNullOrEmpty(e.GeographicLocation)).ToList();
        foreach (var eventWithLocation in eventsWithLocation)
        {
            try
            {
                var locationData = JsonSerializer.Deserialize<Dictionary<string, object>>(eventWithLocation.GeographicLocation!);
                if (locationData != null && locationData.TryGetValue("CountryCode", out var countryCode))
                {
                    var country = countryCode.ToString() ?? "Unknown";
                    statistics.EventsByCountry[country] = statistics.EventsByCountry.GetValueOrDefault(country, 0) + 1;
                }
            }
            catch
            {
                // Ignore JSON parsing errors
            }
        }

        // Get top risky IPs
        statistics.TopRiskyIPs = events
            .Where(e => e.RiskScore >= 70)
            .GroupBy(e => e.IPAddress)
            .OrderByDescending(g => g.Average(e => e.RiskScore ?? 0))
            .Take(10)
            .ToDictionary(g => g.Key, g => g.Count());

        return statistics;
    }

    private async Task CreateSecurityEventAsync(string eventType, string? userId, string? userEmail, string ipAddress, bool success, string? failureReason = null, string? userAgent = null, string? sessionId = null, Dictionary<string, object>? additionalData = null, decimal? riskScore = null)
    {
        try
        {
            var geoLocation = await _geolocationService.GetLocationAsync(ipAddress);

            var securityEvent = new SecurityEvent
            {
                EventType = eventType,
                UserId = userId,
                UserEmail = userEmail,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                Success = success,
                FailureReason = failureReason,
                SessionId = sessionId,
                DeviceFingerprint = additionalData?.GetValueOrDefault("DeviceFingerprint")?.ToString(),
                RiskScore = riskScore,
                ResponseAction = additionalData?.GetValueOrDefault("ResponseAction")?.ToString(),
                GeographicLocation = geoLocation != null ? JsonSerializer.Serialize(geoLocation) : null,
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null,
                CreatedAt = DateTime.UtcNow
            };

            _context.SecurityEvents.Add(securityEvent);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Security event logged: {EventType} for user {UserEmail} from IP {IpAddress}", 
                eventType, userEmail, ipAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating security event: {EventType}", eventType);
        }
    }

    private static decimal CalculateGeographicRiskScore(double travelSpeed, double travelDistance)
    {
        // Base risk score based on speed
        decimal riskScore = 0;

        if (travelSpeed > 1000) // Faster than commercial aircraft
            riskScore = 100;
        else if (travelSpeed > 500) // Very fast travel
            riskScore = 90;
        else if (travelSpeed > 200) // Fast travel
            riskScore = 70;
        else if (travelSpeed > 100) // Normal travel
            riskScore = 40;
        else
            riskScore = 20;

        // Adjust based on distance
        if (travelDistance > 10000) // Intercontinental
            riskScore = Math.Min(100, riskScore + 20);
        else if (travelDistance > 5000) // Long distance
            riskScore = Math.Min(100, riskScore + 10);

        return riskScore;
    }

    private async Task<SecurityEvent?> GetLatestSecurityEventAsync(string eventType, string ipAddress)
    {
        return await _context.SecurityEvents
            .Where(se => se.EventType == eventType && se.IPAddress == ipAddress)
            .OrderByDescending(se => se.CreatedAt)
            .FirstOrDefaultAsync();
    }

    private static string FormatSecurityAlert(SecurityEvent securityEvent)
    {
        var message = $"Security Alert: {securityEvent.EventType}\n\n";
        message += $"Time: {securityEvent.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC\n";
        message += $"IP Address: {securityEvent.IPAddress}\n";
        
        if (!string.IsNullOrEmpty(securityEvent.UserEmail))
            message += $"User: {securityEvent.UserEmail}\n";
        
        if (securityEvent.RiskScore.HasValue)
            message += $"Risk Score: {securityEvent.RiskScore:F1}/100\n";
        
        if (!string.IsNullOrEmpty(securityEvent.FailureReason))
            message += $"Details: {securityEvent.FailureReason}\n";
        
        if (!string.IsNullOrEmpty(securityEvent.UserAgent))
            message += $"User Agent: {securityEvent.UserAgent}\n";

        return message;
    }

    private async Task<int> GetRecentAlertCountAsync()
    {
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        
        // This would typically be stored in a separate alerts table or cache
        // For now, we'll approximate by counting high-risk events
        return await _context.SecurityEvents
            .Where(se => se.CreatedAt >= oneHourAgo && se.RiskScore >= 80)
            .CountAsync();
    }

    private async Task SendSlackNotificationAsync(SecurityEvent securityEvent, string message)
    {
        try
        {
            if (string.IsNullOrEmpty(_options.SlackWebhookUrl))
                return;

            using var httpClient = new HttpClient();
            var payload = new
            {
                text = message,
                username = "NeighborTools Security",
                icon_emoji = ":warning:",
                attachments = new[]
                {
                    new
                    {
                        color = securityEvent.RiskScore >= 90 ? "danger" : securityEvent.RiskScore >= 70 ? "warning" : "good",
                        fields = new[]
                        {
                            new { title = "Event Type", value = securityEvent.EventType, @short = true },
                            new { title = "IP Address", value = securityEvent.IPAddress, @short = true },
                            new { title = "Risk Score", value = securityEvent.RiskScore?.ToString("F1") ?? "N/A", @short = true },
                            new { title = "Time", value = securityEvent.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"), @short = true }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            await httpClient.PostAsync(_options.SlackWebhookUrl, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Slack notification for security event {EventId}", securityEvent.Id);
        }
    }

    private async Task RecordAlertSentAsync(Guid eventId)
    {
        // In a production system, you might want to track sent alerts in a separate table
        // For now, we'll just log it
        _logger.LogInformation("Alert sent for security event {EventId}", eventId);
        await Task.CompletedTask;
    }
}