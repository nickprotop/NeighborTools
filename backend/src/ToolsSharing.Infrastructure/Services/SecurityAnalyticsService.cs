using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Core.Models.SecurityAnalytics;
using ToolsSharing.Core.Entities;
using ToolsSharing.Infrastructure.Data;
using ToolsSharing.Infrastructure.Configuration;

namespace ToolsSharing.Infrastructure.Services;

public class SecurityAnalyticsService : ISecurityAnalyticsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SecurityAnalyticsService> _logger;
    private readonly IDistributedCache _cache;
    private readonly IGeolocationService _geolocationService;
    private readonly AuthenticationLoggingOptions _loggingOptions;
    private readonly IPerformanceMetricsService _performanceMetrics;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

    public SecurityAnalyticsService(
        ApplicationDbContext context,
        ILogger<SecurityAnalyticsService> logger,
        IDistributedCache cache,
        IGeolocationService geolocationService,
        IOptions<AuthenticationLoggingOptions> loggingOptions,
        IPerformanceMetricsService performanceMetrics)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
        _geolocationService = geolocationService;
        _loggingOptions = loggingOptions.Value;
        _performanceMetrics = performanceMetrics;
    }

    public async Task<SecurityDashboardData> GetSecurityDashboardAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-7);
        var end = endDate ?? DateTime.UtcNow;

        var cacheKey = $"security_dashboard_{start:yyyyMMdd}_{end:yyyyMMdd}";
        var cached = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cached))
        {
            var cachedData = JsonSerializer.Deserialize<SecurityDashboardData>(cached);
            if (cachedData != null)
                return cachedData;
        }

        try
        {
            var dashboard = new SecurityDashboardData
            {
                Metrics = await GetSecurityMetricsAsync(start, end),
                ActiveThreats = await GetActiveThreatsAsync(10),
                RecentAlerts = await GetActiveAlertsAsync(20),
                TrendData = await GetSecurityTrendsAsync(start, end),
                AttackPatterns = await GetAttackPatternsAsync(start, end),
                GeographicThreats = await GetGeographicThreatsAsync(start, end),
                SystemHealth = await GetSystemHealthAsync()
            };

            // Cache the result
            var serialized = JsonSerializer.Serialize(dashboard);
            await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiry
            });

            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating security dashboard");
            throw;
        }
    }

    public async Task<SecurityMetrics> GetSecurityMetricsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-7);
        var end = endDate ?? DateTime.UtcNow;

        var events = await _context.SecurityEvents
            .Where(se => se.CreatedAt >= start && se.CreatedAt <= end)
            .ToListAsync();

        var metrics = new SecurityMetrics
        {
            TotalSecurityEvents = events.Count,
            FailedLoginAttempts = events.Count(e => e.EventType == SecurityEventTypes.LoginFailed),
            AccountLockouts = events.Count(e => e.EventType == SecurityEventTypes.AccountLockout),
            SuspiciousActivities = events.Count(e => e.EventType == SecurityEventTypes.SuspiciousActivity),
            BruteForceAttempts = events.Count(e => e.EventType == SecurityEventTypes.BruteForceAttempt),
            GeographicAnomalies = events.Count(e => e.EventType == SecurityEventTypes.GeographicAnomaly),
            SessionHijackingAttempts = events.Count(e => e.EventType == SecurityEventTypes.SessionHijackingAttempt),
            TokensBlacklisted = events.Count(e => e.EventType == SecurityEventTypes.TokenBlacklisted),
            BlockedIPs = await GetBlockedIPCountAsync(),
            PeriodStart = start,
            PeriodEnd = end,
            AnalysisPeriod = end - start
        };

        var eventsWithRisk = events.Where(e => e.RiskScore.HasValue).ToList();
        if (eventsWithRisk.Any())
        {
            metrics.AverageRiskScore = eventsWithRisk.Average(e => e.RiskScore!.Value);
            metrics.MaxRiskScore = eventsWithRisk.Max(e => e.RiskScore!.Value);
        }

        return metrics;
    }

    public async Task<SystemHealthStatus> GetSystemHealthAsync()
    {
        var health = new SystemHealthStatus
        {
            LastUpdateTime = DateTime.UtcNow,
            ServiceStatus = await GetSecurityServiceStatusAsync(),
            Performance = await GetPerformanceMetricsInternalAsync()
        };

        var healthMetrics = new List<HealthMetric>();

        // Check database connectivity
        try
        {
            await _context.Database.CanConnectAsync();
            healthMetrics.Add(new HealthMetric
            {
                Name = "Database",
                Category = "Infrastructure",
                Status = HealthStatus.Healthy,
                Value = "Connected",
                LastChecked = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            healthMetrics.Add(new HealthMetric
            {
                Name = "Database",
                Category = "Infrastructure", 
                Status = HealthStatus.Critical,
                Value = "Disconnected",
                LastChecked = DateTime.UtcNow,
                Description = ex.Message
            });
        }

        // Check cache connectivity
        try
        {
            await _cache.SetStringAsync("health_check", "ok", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
            });
            healthMetrics.Add(new HealthMetric
            {
                Name = "Cache",
                Category = "Infrastructure",
                Status = HealthStatus.Healthy,
                Value = "Connected",
                LastChecked = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            healthMetrics.Add(new HealthMetric
            {
                Name = "Cache",
                Category = "Infrastructure",
                Status = HealthStatus.Critical,
                Value = "Disconnected",
                LastChecked = DateTime.UtcNow,
                Description = ex.Message
            });
        }

        health.HealthMetrics = healthMetrics;
        health.OverallHealth = healthMetrics.Any(h => h.Status == HealthStatus.Critical) 
            ? HealthStatus.Critical 
            : healthMetrics.Any(h => h.Status == HealthStatus.Warning) 
                ? HealthStatus.Warning 
                : HealthStatus.Healthy;

        return health;
    }

    public async Task<List<SecurityThreat>> GetActiveThreatsAsync(int limit = 50)
    {
        // Get recent high-risk events grouped by IP address with simpler query
        var recentEvents = await _context.SecurityEvents
            .Where(se => se.CreatedAt >= DateTime.UtcNow.AddDays(-1) && se.RiskScore >= 70)
            .GroupBy(se => se.IPAddress)
            .Select(g => new
            {
                IPAddress = g.Key,
                EventCount = g.Count(),
                MaxRiskScore = g.Max(e => e.RiskScore ?? 0),
                TotalRiskScore = g.Sum(e => e.RiskScore ?? 0),
                FirstEvent = g.Min(e => e.CreatedAt),
                LastEvent = g.Max(e => e.CreatedAt)
            })
            .OrderByDescending(g => g.MaxRiskScore)
            .Take(limit)
            .ToListAsync();

        var threats = new List<SecurityThreat>();

        foreach (var eventGroup in recentEvents)
        {
            // Get additional details for this IP address separately
            var ipEvents = await _context.SecurityEvents
                .Where(se => se.IPAddress == eventGroup.IPAddress && 
                            se.CreatedAt >= DateTime.UtcNow.AddDays(-1) && 
                            se.RiskScore >= 70)
                .ToListAsync();

            var eventTypes = ipEvents.Select(e => e.EventType).Distinct().ToList();
            var users = ipEvents.Where(e => !string.IsNullOrEmpty(e.UserEmail))
                               .Select(e => e.UserEmail!)
                               .Distinct()
                               .ToList();
            var location = ipEvents.FirstOrDefault(e => !string.IsNullOrEmpty(e.GeographicLocation))?.GeographicLocation;

            var threat = new SecurityThreat
            {
                Id = Guid.NewGuid(),
                ThreatType = DetermineThreatType(eventTypes),
                SourceIP = eventGroup.IPAddress,
                TargetUser = users.FirstOrDefault(),
                RiskScore = eventGroup.MaxRiskScore,
                Severity = GetThreatSeverity(eventGroup.MaxRiskScore),
                Status = ThreatStatus.Active,
                FirstDetected = eventGroup.FirstEvent,
                LastActivity = eventGroup.LastEvent,
                EventCount = eventGroup.EventCount,
                Description = GenerateThreatDescription(eventTypes, eventGroup.EventCount, eventGroup.MaxRiskScore),
                GeographicLocation = location,
                IsBlocked = await IsIPBlockedAsync(eventGroup.IPAddress),
                RecommendedActions = GenerateRecommendedActions(eventTypes, eventGroup.MaxRiskScore)
            };

            threats.Add(threat);
        }

        return threats;
    }

    public async Task<List<SecurityAlert>> GetActiveAlertsAsync(int limit = 100)
    {
        // For this implementation, we'll generate alerts from recent high-risk events
        var recentHighRiskEvents = await _context.SecurityEvents
            .Where(se => se.CreatedAt >= DateTime.UtcNow.AddHours(-24) && se.RiskScore >= 80)
            .OrderByDescending(se => se.RiskScore)
            .Take(limit)
            .ToListAsync();

        var alerts = new List<SecurityAlert>();

        foreach (var securityEvent in recentHighRiskEvents)
        {
            var alert = new SecurityAlert
            {
                Id = Guid.NewGuid(),
                AlertType = securityEvent.EventType,
                Title = GenerateAlertTitle(securityEvent.EventType, securityEvent.RiskScore ?? 0),
                Message = GenerateAlertMessage(securityEvent),
                Severity = GetAlertSeverity(securityEvent.RiskScore ?? 0),
                Status = AlertStatus.New,
                CreatedAt = securityEvent.CreatedAt,
                SourceIP = securityEvent.IPAddress,
                UserId = securityEvent.UserId,
                UserEmail = securityEvent.UserEmail,
                RiskScore = securityEvent.RiskScore ?? 0,
                RequiresImmedateAction = (securityEvent.RiskScore ?? 0) >= 90,
                SuggestedActions = GenerateSuggestedActions(securityEvent.EventType, securityEvent.RiskScore ?? 0)
            };

            alerts.Add(alert);
        }

        return alerts;
    }

    public async Task<SecurityTrendData> GetSecurityTrendsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-7);
        var end = endDate ?? DateTime.UtcNow;

        var events = await _context.SecurityEvents
            .Where(se => se.CreatedAt >= start && se.CreatedAt <= end)
            .ToListAsync();

        var trendData = new SecurityTrendData
        {
            LoginAttempts = GenerateDataPoints(events, SecurityEventTypes.Login, start, end),
            FailedLogins = GenerateDataPoints(events, SecurityEventTypes.LoginFailed, start, end),
            SuspiciousActivity = GenerateDataPoints(events, SecurityEventTypes.SuspiciousActivity, start, end),
            BruteForceAttacks = GenerateDataPoints(events, SecurityEventTypes.BruteForceAttempt, start, end),
            RiskScoreTrend = GenerateRiskScoreDataPoints(events, start, end),
            CountryData = await GenerateCountryDataAsync(events),
            HourlyData = GenerateHourlyData(events),
            TopAttackers = await GenerateTopAttackersAsync(events)
        };

        return trendData;
    }

    public async Task<List<AttackPatternSummary>> GetAttackPatternsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var patterns = await _context.AttackPatterns
            .Where(ap => ap.LastDetectedAt >= start && ap.LastDetectedAt <= end)
            .OrderByDescending(ap => ap.OccurrenceCount)
            .ToListAsync();

        var summaries = new List<AttackPatternSummary>();

        foreach (var pattern in patterns)
        {
            var summary = new AttackPatternSummary
            {
                PatternType = pattern.AttackType,
                PatternName = pattern.AttackType, // Using AttackType as name since PatternName doesn't exist
                Frequency = pattern.OccurrenceCount,
                AverageRiskScore = pattern.RiskScore ?? 0,
                FirstSeen = pattern.FirstDetectedAt,
                LastSeen = pattern.LastDetectedAt,
                Description = $"{pattern.AttackType} attack pattern",
                Trend = DetermineTrend(pattern.AttackType, start, end),
                CommonSources = await GetCommonSourcesForPattern(pattern.AttackType, start, end),
                TargetedUsers = await GetTargetedUsersForPattern(pattern.AttackType, start, end)
            };

            if (!string.IsNullOrEmpty(pattern.AttackData))
            {
                try
                {
                    var attackData = JsonSerializer.Deserialize<Dictionary<string, object>>(pattern.AttackData);
                    if (attackData != null)
                    {
                        summary.Characteristics = attackData.ToDictionary(
                            kvp => kvp.Key, 
                            kvp => kvp.Value is JsonElement element && element.ValueKind == JsonValueKind.Number 
                                ? element.GetInt32() 
                                : 1
                        );
                    }
                }
                catch
                {
                    summary.Characteristics = new Dictionary<string, int>();
                }
            }

            summaries.Add(summary);
        }

        return summaries;
    }

    public async Task<List<GeographicThreat>> GetGeographicThreatsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-7);
        var end = endDate ?? DateTime.UtcNow;

        var events = await _context.SecurityEvents
            .Where(se => se.CreatedAt >= start && se.CreatedAt <= end && !string.IsNullOrEmpty(se.GeographicLocation))
            .ToListAsync();

        var geographicThreats = new List<GeographicThreat>();
        var countryGroups = new Dictionary<string, List<SecurityEvent>>();

        foreach (var securityEvent in events)
        {
            try
            {
                var locationData = JsonSerializer.Deserialize<Dictionary<string, object>>(securityEvent.GeographicLocation!);
                if (locationData != null && locationData.TryGetValue("CountryCode", out var countryCodeObj))
                {
                    var countryCode = countryCodeObj.ToString() ?? "Unknown";
                    if (!countryGroups.ContainsKey(countryCode))
                        countryGroups[countryCode] = new List<SecurityEvent>();
                    
                    countryGroups[countryCode].Add(securityEvent);
                }
            }
            catch
            {
                // Ignore JSON parsing errors
            }
        }

        foreach (var countryGroup in countryGroups)
        {
            var countryEvents = countryGroup.Value;
            var highRiskEvents = countryEvents.Where(e => e.RiskScore >= 70).ToList();
            
            if (highRiskEvents.Any())
            {
                var geoThreat = new GeographicThreat
                {
                    CountryCode = countryGroup.Key,
                    CountryName = GetCountryName(countryGroup.Key),
                    ThreatCount = countryEvents.Count,
                    AverageRiskScore = countryEvents.Where(e => e.RiskScore.HasValue).Average(e => e.RiskScore!.Value),
                    MaxRiskScore = countryEvents.Where(e => e.RiskScore.HasValue).Max(e => e.RiskScore!.Value),
                    Severity = GetThreatSeverity(countryEvents.Where(e => e.RiskScore.HasValue).Max(e => e.RiskScore!.Value)),
                    ThreatTypes = countryEvents.Select(e => e.EventType).Distinct().ToList(),
                    AttackBreakdown = countryEvents.GroupBy(e => e.EventType).ToDictionary(g => g.Key, g => g.Count()),
                    IsBlocked = false // Would need to implement country-level blocking
                };

                geographicThreats.Add(geoThreat);
            }
        }

        return geographicThreats.OrderByDescending(gt => gt.MaxRiskScore).ToList();
    }

    public async Task<SecurityThreat?> AnalyzeThreatAsync(string sourceIP, string? userId = null)
    {
        var recentEvents = await _context.SecurityEvents
            .Where(se => se.IPAddress == sourceIP && se.CreatedAt >= DateTime.UtcNow.AddDays(-1))
            .OrderByDescending(se => se.CreatedAt)
            .ToListAsync();

        if (!recentEvents.Any())
            return null;

        var maxRiskScore = recentEvents.Where(e => e.RiskScore.HasValue).Max(e => e.RiskScore!.Value);
        var eventTypes = recentEvents.Select(e => e.EventType).Distinct().ToList();

        var threat = new SecurityThreat
        {
            Id = Guid.NewGuid(),
            ThreatType = DetermineThreatType(eventTypes),
            SourceIP = sourceIP,
            TargetUser = userId,
            RiskScore = maxRiskScore,
            Severity = GetThreatSeverity(maxRiskScore),
            Status = ThreatStatus.Active,
            FirstDetected = recentEvents.Min(e => e.CreatedAt),
            LastActivity = recentEvents.Max(e => e.CreatedAt),
            EventCount = recentEvents.Count,
            Description = GenerateThreatDescription(eventTypes, recentEvents.Count, maxRiskScore),
            GeographicLocation = recentEvents.FirstOrDefault(e => !string.IsNullOrEmpty(e.GeographicLocation))?.GeographicLocation,
            IsBlocked = await IsIPBlockedAsync(sourceIP),
            RecommendedActions = GenerateRecommendedActions(eventTypes, maxRiskScore)
        };

        return threat;
    }

    public async Task<decimal> CalculateRiskScoreAsync(string sourceIP, string? userId = null, Dictionary<string, object>? factors = null)
    {
        decimal riskScore = 0;

        // Base risk from recent events
        var recentEvents = await _context.SecurityEvents
            .Where(se => se.IPAddress == sourceIP && se.CreatedAt >= DateTime.UtcNow.AddHours(-24))
            .ToListAsync();

        if (recentEvents.Any())
        {
            riskScore += recentEvents.Count * 5; // 5 points per recent event
            riskScore += recentEvents.Where(e => e.RiskScore.HasValue).Sum(e => e.RiskScore!.Value) * 0.1m; // 10% of total risk
        }

        // Geographic risk
        try
        {
            var location = await _geolocationService.GetLocationAsync(sourceIP);
            if (location != null && IsHighRiskCountry(location.CountryCode))
            {
                riskScore += 20;
            }
        }
        catch
        {
            // Ignore geolocation errors
        }

        // User-specific risk
        if (!string.IsNullOrEmpty(userId))
        {
            var userEvents = await _context.SecurityEvents
                .Where(se => se.UserId == userId && se.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                .ToListAsync();

            if (userEvents.Count > 10)
            {
                riskScore += Math.Min(userEvents.Count - 10, 30); // Up to 30 additional points
            }
        }

        // Factor in additional context
        if (factors != null)
        {
            if (factors.TryGetValue("failed_attempts", out var failedAttemptsObj) && 
                failedAttemptsObj is int failedAttempts)
            {
                riskScore += failedAttempts * 10;
            }

            if (factors.TryGetValue("velocity", out var velocityObj) && 
                velocityObj is double velocity && velocity > 5)
            {
                riskScore += (decimal)velocity * 5;
            }
        }

        return Math.Min(riskScore, 100); // Cap at 100
    }

    public async Task<bool> IsIPHighRiskAsync(string ipAddress)
    {
        var riskScore = await CalculateRiskScoreAsync(ipAddress);
        return riskScore >= 70;
    }

    // Helper methods

    private async Task<int> GetBlockedIPCountAsync()
    {
        // This would typically query a blocked IPs table or cache
        // For now, return a placeholder count
        return await Task.FromResult(0);
    }

    private async Task<SecurityServiceStatus> GetSecurityServiceStatusAsync()
    {
        return new SecurityServiceStatus
        {
            BruteForceProtectionActive = true,
            TokenBlacklistActive = true,
            SessionSecurityActive = true,
            GeolocationServiceActive = true,
            EmailNotificationsActive = _loggingOptions.EnableEmailNotifications,
            SlackNotificationsActive = _loggingOptions.EnableSlackNotifications,
            RateLimitingActive = true,
            IPBlockingActive = true,
            LastServiceCheck = DateTime.UtcNow
        };
    }

    private async Task<PerformanceMetrics> GetPerformanceMetricsInternalAsync()
    {
        return await _performanceMetrics.GetPerformanceMetricsAsync();
    }

    private async Task<bool> IsIPBlockedAsync(string ipAddress)
    {
        // Check if IP is in blocked list (implementation would depend on blocking mechanism)
        return await Task.FromResult(false);
    }

    private static string DetermineThreatType(List<string> eventTypes)
    {
        if (eventTypes.Contains(SecurityEventTypes.BruteForceAttempt))
            return "Brute Force Attack";
        if (eventTypes.Contains(SecurityEventTypes.SessionHijackingAttempt))
            return "Session Hijacking";
        if (eventTypes.Contains(SecurityEventTypes.GeographicAnomaly))
            return "Geographic Anomaly";
        if (eventTypes.Contains(SecurityEventTypes.LoginFailed))
            return "Failed Login Attempts";
        
        return "Suspicious Activity";
    }

    private static ThreatSeverity GetThreatSeverity(decimal riskScore)
    {
        return riskScore switch
        {
            >= 90 => ThreatSeverity.Critical,
            >= 70 => ThreatSeverity.High,
            >= 50 => ThreatSeverity.Medium,
            _ => ThreatSeverity.Low
        };
    }

    private static AlertSeverity GetAlertSeverity(decimal riskScore)
    {
        return riskScore switch
        {
            >= 90 => AlertSeverity.Critical,
            >= 70 => AlertSeverity.Error,
            >= 50 => AlertSeverity.Warning,
            _ => AlertSeverity.Info
        };
    }

    private static string GenerateThreatDescription(List<string> eventTypes, int eventCount, decimal riskScore)
    {
        var primaryType = DetermineThreatType(eventTypes);
        return $"{primaryType} detected with {eventCount} events and risk score of {riskScore:F1}";
    }

    private static string GenerateAlertTitle(string eventType, decimal riskScore)
    {
        var severity = GetAlertSeverity(riskScore);
        return $"{severity} Security Alert: {eventType}";
    }

    private static string GenerateAlertMessage(SecurityEvent securityEvent)
    {
        return $"Security event '{securityEvent.EventType}' detected from IP {securityEvent.IPAddress} with risk score {securityEvent.RiskScore:F1}";
    }

    private static List<string> GenerateRecommendedActions(List<string> eventTypes, decimal riskScore)
    {
        var actions = new List<string>();

        if (riskScore >= 90)
        {
            actions.Add("Immediately block IP address");
            actions.Add("Escalate to security team");
        }
        else if (riskScore >= 70)
        {
            actions.Add("Monitor closely");
            actions.Add("Consider temporary rate limiting");
        }

        if (eventTypes.Contains(SecurityEventTypes.BruteForceAttempt))
        {
            actions.Add("Enable CAPTCHA for this IP");
            actions.Add("Notify affected users");
        }

        return actions;
    }

    private static List<string> GenerateSuggestedActions(string eventType, decimal riskScore)
    {
        return GenerateRecommendedActions(new List<string> { eventType }, riskScore);
    }

    private static List<SecurityDataPoint> GenerateDataPoints(List<SecurityEvent> events, string eventType, DateTime start, DateTime end)
    {
        var relevantEvents = events.Where(e => e.EventType == eventType).ToList();
        var dataPoints = new List<SecurityDataPoint>();

        var current = start.Date;
        while (current <= end.Date)
        {
            var dayEvents = relevantEvents.Where(e => e.CreatedAt.Date == current).ToList();
            dataPoints.Add(new SecurityDataPoint
            {
                Timestamp = current,
                Count = dayEvents.Count,
                Value = dayEvents.Count,
                Label = current.ToString("MMM dd")
            });
            current = current.AddDays(1);
        }

        return dataPoints;
    }

    private static List<SecurityDataPoint> GenerateRiskScoreDataPoints(List<SecurityEvent> events, DateTime start, DateTime end)
    {
        var dataPoints = new List<SecurityDataPoint>();
        var current = start.Date;

        while (current <= end.Date)
        {
            var dayEvents = events.Where(e => e.CreatedAt.Date == current && e.RiskScore.HasValue).ToList();
            var avgRisk = dayEvents.Any() ? dayEvents.Average(e => e.RiskScore!.Value) : 0;
            
            dataPoints.Add(new SecurityDataPoint
            {
                Timestamp = current,
                Count = dayEvents.Count,
                Value = avgRisk,
                Label = current.ToString("MMM dd")
            });
            current = current.AddDays(1);
        }

        return dataPoints;
    }

    private async Task<List<CountrySecurityData>> GenerateCountryDataAsync(List<SecurityEvent> events)
    {
        var countryData = new List<CountrySecurityData>();
        var eventsWithLocation = events.Where(e => !string.IsNullOrEmpty(e.GeographicLocation)).ToList();

        var countryGroups = new Dictionary<string, List<SecurityEvent>>();

        foreach (var securityEvent in eventsWithLocation)
        {
            try
            {
                var locationData = JsonSerializer.Deserialize<Dictionary<string, object>>(securityEvent.GeographicLocation!);
                if (locationData != null && locationData.TryGetValue("CountryCode", out var countryCodeObj))
                {
                    var countryCode = countryCodeObj.ToString() ?? "Unknown";
                    if (!countryGroups.ContainsKey(countryCode))
                        countryGroups[countryCode] = new List<SecurityEvent>();
                    
                    countryGroups[countryCode].Add(securityEvent);
                }
            }
            catch
            {
                // Ignore JSON parsing errors
            }
        }

        foreach (var countryGroup in countryGroups)
        {
            var countryEvents = countryGroup.Value;
            var suspiciousEvents = countryEvents.Where(e => e.RiskScore >= 50).ToList();

            countryData.Add(new CountrySecurityData
            {
                CountryCode = countryGroup.Key,
                CountryName = GetCountryName(countryGroup.Key),
                TotalEvents = countryEvents.Count,
                SuspiciousEvents = suspiciousEvents.Count,
                RiskPercentage = countryEvents.Count > 0 ? (decimal)suspiciousEvents.Count / countryEvents.Count * 100 : 0,
                TopThreatTypes = countryEvents.GroupBy(e => e.EventType).OrderByDescending(g => g.Count()).Take(3).Select(g => g.Key).ToList()
            });
        }

        return countryData.OrderByDescending(cd => cd.RiskPercentage).ToList();
    }

    private static List<HourlySecurityData> GenerateHourlyData(List<SecurityEvent> events)
    {
        var hourlyData = new List<HourlySecurityData>();

        for (int hour = 0; hour < 24; hour++)
        {
            var hourEvents = events.Where(e => e.CreatedAt.Hour == hour).ToList();
            var suspiciousEvents = hourEvents.Where(e => e.RiskScore >= 50).ToList();

            hourlyData.Add(new HourlySecurityData
            {
                Hour = hour,
                TotalEvents = hourEvents.Count,
                SuspiciousEvents = suspiciousEvents.Count,
                FailedLogins = hourEvents.Count(e => e.EventType == SecurityEventTypes.LoginFailed),
                BruteForceAttempts = hourEvents.Count(e => e.EventType == SecurityEventTypes.BruteForceAttempt),
                AverageRiskScore = hourEvents.Where(e => e.RiskScore.HasValue).Any() ? 
                    hourEvents.Where(e => e.RiskScore.HasValue).Average(e => e.RiskScore!.Value) : 0
            });
        }

        return hourlyData;
    }

    private async Task<List<TopAttackerData>> GenerateTopAttackersAsync(List<SecurityEvent> events)
    {
        var ipGroups = events.GroupBy(e => e.IPAddress)
            .Select(g => new
            {
                IPAddress = g.Key,
                Events = g.ToList(),
                AttackCount = g.Count(),
                TotalRiskScore = g.Where(e => e.RiskScore.HasValue).Sum(e => e.RiskScore!.Value),
                AverageRiskScore = g.Where(e => e.RiskScore.HasValue).Any() ? g.Where(e => e.RiskScore.HasValue).Average(e => e.RiskScore!.Value) : 0,
                AttackTypes = g.Select(e => e.EventType).Distinct().ToList(),
                FirstSeen = g.Min(e => e.CreatedAt),
                LastSeen = g.Max(e => e.CreatedAt)
            })
            .OrderByDescending(g => g.TotalRiskScore)
            .Take(10)
            .ToList();

        var topAttackers = new List<TopAttackerData>();

        foreach (var ipGroup in ipGroups)
        {
            var attacker = new TopAttackerData
            {
                IPAddress = ipGroup.IPAddress,
                AttackCount = ipGroup.AttackCount,
                TotalRiskScore = ipGroup.TotalRiskScore,
                AverageRiskScore = ipGroup.AverageRiskScore,
                AttackTypes = ipGroup.AttackTypes,
                FirstSeen = ipGroup.FirstSeen,
                LastSeen = ipGroup.LastSeen,
                IsBlocked = await IsIPBlockedAsync(ipGroup.IPAddress),
                GeographicLocation = ipGroup.Events.FirstOrDefault(e => !string.IsNullOrEmpty(e.GeographicLocation))?.GeographicLocation
            };

            topAttackers.Add(attacker);
        }

        return topAttackers;
    }

    private AttackPatternTrend DetermineTrend(string patternType, DateTime start, DateTime end)
    {
        // Simplified trend calculation - in a real implementation, this would compare periods
        return AttackPatternTrend.Stable;
    }

    private async Task<List<string>> GetCommonSourcesForPattern(string patternType, DateTime start, DateTime end)
    {
        var events = await _context.SecurityEvents
            .Where(se => se.EventType == patternType && se.CreatedAt >= start && se.CreatedAt <= end)
            .GroupBy(se => se.IPAddress)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToListAsync();

        return events;
    }

    private async Task<List<string>> GetTargetedUsersForPattern(string patternType, DateTime start, DateTime end)
    {
        var users = await _context.SecurityEvents
            .Where(se => se.EventType == patternType && se.CreatedAt >= start && se.CreatedAt <= end && !string.IsNullOrEmpty(se.UserEmail))
            .GroupBy(se => se.UserEmail)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToListAsync();

        return users!;
    }

    private static string GetCountryName(string countryCode)
    {
        // Simplified country name mapping - in production, use a proper country database
        return countryCode switch
        {
            "US" => "United States",
            "CA" => "Canada",
            "GB" => "United Kingdom",
            "DE" => "Germany",
            "FR" => "France",
            "CN" => "China",
            "RU" => "Russia",
            "IN" => "India",
            "BR" => "Brazil",
            "AU" => "Australia",
            _ => countryCode
        };
    }

    private static bool IsHighRiskCountry(string? countryCode)
    {
        // Simplified high-risk country detection
        var highRiskCountries = new[] { "CN", "RU", "KP", "IR" };
        return !string.IsNullOrEmpty(countryCode) && highRiskCountries.Contains(countryCode);
    }

    // Placeholder implementations for remaining interface methods
    public Task<List<SecurityThreat>> GetThreatsByTypeAsync(string threatType, DateTime? startDate = null, DateTime? endDate = null)
    {
        throw new NotImplementedException();
    }

    public Task<SecurityThreat?> GetThreatByIdAsync(Guid threatId)
    {
        throw new NotImplementedException();
    }

    public Task UpdateThreatStatusAsync(Guid threatId, ThreatStatus status, string? updatedBy = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> BlockThreatAsync(Guid threatId, TimeSpan? duration = null, string? reason = null)
    {
        throw new NotImplementedException();
    }

    public Task<List<SecurityAlert>> GetAlertsByTypeAsync(string alertType, DateTime? startDate = null, DateTime? endDate = null)
    {
        throw new NotImplementedException();
    }

    public Task<SecurityAlert?> GetAlertByIdAsync(Guid alertId)
    {
        throw new NotImplementedException();
    }

    public Task AcknowledgeAlertAsync(Guid alertId, string acknowledgedBy)
    {
        throw new NotImplementedException();
    }

    public Task ResolveAlertAsync(Guid alertId, string resolvedBy)
    {
        throw new NotImplementedException();
    }

    public Task CreateSecurityAlertAsync(string alertType, string message, AlertSeverity severity, string sourceIP, string? userId = null, decimal riskScore = 0, Dictionary<string, object>? alertData = null)
    {
        throw new NotImplementedException();
    }

    public Task<List<SecurityDataPoint>> GetTrendDataAsync(string metricType, DateTime startDate, DateTime endDate, string interval = "hour")
    {
        throw new NotImplementedException();
    }

    public Task<List<TopAttackerData>> GetTopAttackersAsync(int limit = 10, DateTime? startDate = null, DateTime? endDate = null)
    {
        throw new NotImplementedException();
    }

    public Task<List<CountrySecurityData>> GetCountrySecurityDataAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        throw new NotImplementedException();
    }

    public Task<List<HourlySecurityData>> GetHourlySecurityDataAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        throw new NotImplementedException();
    }

    public Task<List<SecurityThreat>> SearchThreatsAsync(SecurityAnalyticsFilter filter)
    {
        throw new NotImplementedException();
    }

    public Task<List<SecurityAlert>> SearchAlertsAsync(SecurityAnalyticsFilter filter)
    {
        throw new NotImplementedException();
    }

    public Task<SecurityDashboardData> GetFilteredDashboardAsync(SecurityAnalyticsFilter filter)
    {
        throw new NotImplementedException();
    }

    public Task<SecurityReport> GenerateReportAsync(SecurityReportRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<List<SecurityRecommendation>> GetSecurityRecommendationsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        throw new NotImplementedException();
    }

    public Task<byte[]> ExportReportAsync(Guid reportId, SecurityReportFormat format)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ScheduleReportAsync(SecurityReportRequest request, string cronExpression, string? recipients = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> StartRealTimeMonitoringAsync()
    {
        throw new NotImplementedException();
    }

    public Task<bool> StopRealTimeMonitoringAsync()
    {
        throw new NotImplementedException();
    }

    public Task<List<SecurityAlert>> GetRealtimeAlertsAsync(DateTime since)
    {
        throw new NotImplementedException();
    }

    public Task NotifySecurityEventAsync(string eventType, string sourceIP, string? userId = null, decimal riskScore = 0, Dictionary<string, object>? eventData = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateThreatDetectionRulesAsync(Dictionary<string, object> rules)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateAlertThresholdsAsync(Dictionary<string, decimal> thresholds)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CleanupOldDataAsync(TimeSpan retentionPeriod)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RecalculateRiskScoresAsync(DateTime? startDate = null)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, int>> GetEventStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, decimal>> GetRiskStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, object>> GetPerformanceMetricsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<bool> ValidateSystemIntegrityAsync()
    {
        throw new NotImplementedException();
    }
}