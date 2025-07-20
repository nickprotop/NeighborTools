using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToolsSharing.Core.Configuration;
using ToolsSharing.Core.DTOs.ContentModeration;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Features.Messaging;

namespace ToolsSharing.Infrastructure.Services;

/// <summary>
/// Two-stage content moderation service that uses basic validation first, 
/// then SightEngine for advanced analysis only if content passes basic checks.
/// This approach reduces API costs by filtering obvious violations locally.
/// </summary>
public class CascadedContentModerationService : IContentModerationService
{
    private readonly ContentModerationService _basicModerationService;
    private readonly SightEngineService? _sightEngineService;
    private readonly CascadedModerationConfiguration _config;
    private readonly ILogger<CascadedContentModerationService> _logger;

    public CascadedContentModerationService(
        ContentModerationService basicModerationService,
        SightEngineService? sightEngineService,
        IOptions<CascadedModerationConfiguration> config,
        ILogger<CascadedContentModerationService> logger)
    {
        _basicModerationService = basicModerationService;
        _sightEngineService = sightEngineService;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<ContentModerationResult> ValidateContentAsync(string content, string senderId)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Stage 1: Basic moderation (always runs)
            _logger.LogDebug("Stage 1: Running basic content moderation for user {UserId}", senderId);
            var basicResult = await _basicModerationService.ValidateContentAsync(content, senderId);

            // If basic moderation fails, return immediately (no SightEngine call)
            if (!basicResult.IsApproved || basicResult.Severity >= _config.SightEngineThreshold)
            {
                _logger.LogInformation(
                    "Content blocked by basic moderation. Severity: {Severity}, Violations: {Violations}. SightEngine not called.",
                    basicResult.Severity, string.Join(", ", basicResult.Violations));

                // Enhance result with cascaded information
                basicResult.Provider = "Basic (Stage 1)";
                basicResult.ProcessedAt = DateTime.UtcNow;
                basicResult.RawResponseJson = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["stage"] = "basic_only",
                    ["reason"] = "failed_basic_validation",
                    ["processing_time_ms"] = (DateTime.UtcNow - startTime).TotalMilliseconds
                });

                return basicResult;
            }

            // Stage 2: SightEngine moderation (only if SightEngine is available and content passed basic checks)
            if (_sightEngineService == null)
            {
                _logger.LogDebug("SightEngine not available, returning basic moderation result");
                basicResult.Provider = "Basic (SightEngine Unavailable)";
                basicResult.ProcessedAt = DateTime.UtcNow;
                return basicResult;
            }

            _logger.LogDebug("Stage 2: Running SightEngine moderation for user {UserId}", senderId);
            var sightEngineResult = await _sightEngineService.ModerateTextAsync(content, new ContentModerationOptions
            {
                Models = _config.SightEngineModels
            });

            // Combine results - use the most restrictive outcome
            var finalResult = CombineResults(basicResult, sightEngineResult, startTime);

            _logger.LogInformation(
                "Cascaded moderation completed. Final decision: {IsApproved}, Severity: {Severity}, Provider: {Provider}, Processing time: {ProcessingTime}ms",
                finalResult.IsApproved, finalResult.Severity, finalResult.Provider, 
                (DateTime.UtcNow - startTime).TotalMilliseconds);

            return finalResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in cascaded content moderation for user {UserId}", senderId);
            
            // Fallback to basic result if there was an error with SightEngine
            var fallbackResult = await _basicModerationService.ValidateContentAsync(content, senderId);
            fallbackResult.Provider = "Basic (SightEngine Error)";
            fallbackResult.ProcessedAt = DateTime.UtcNow;
            fallbackResult.RawResponseJson = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["stage"] = "basic_fallback",
                ["error"] = ex.Message,
                ["processing_time_ms"] = (DateTime.UtcNow - startTime).TotalMilliseconds
            });
            
            return fallbackResult;
        }
    }

    public async Task<ContentModerationResult> ModerateImageAsync(byte[] imageData, string fileName = "", ContentModerationOptions? options = null)
    {
        // For images, we can't do basic validation, so go directly to SightEngine if available
        if (_sightEngineService == null)
        {
            _logger.LogWarning("Image moderation requested but SightEngine not available");
            return new ContentModerationResult
            {
                IsApproved = _config.AllowImagesWhenSightEngineUnavailable,
                ModerationReason = _config.AllowImagesWhenSightEngineUnavailable 
                    ? "Image allowed (SightEngine unavailable)" 
                    : "Image blocked (SightEngine unavailable)",
                Severity = _config.AllowImagesWhenSightEngineUnavailable ? ModerationSeverity.Clean : ModerationSeverity.Moderate,
                Provider = "Cascaded (No Image Analysis)",
                ProcessedAt = DateTime.UtcNow
            };
        }

        return await _sightEngineService.ModerateImageAsync(imageData, fileName, options);
    }

    public async Task<ContentModerationResult> ModerateImageAsync(string imageUrl, ContentModerationOptions? options = null)
    {
        if (_sightEngineService == null)
        {
            _logger.LogWarning("Image URL moderation requested but SightEngine not available");
            return new ContentModerationResult
            {
                IsApproved = _config.AllowImagesWhenSightEngineUnavailable,
                ModerationReason = _config.AllowImagesWhenSightEngineUnavailable 
                    ? "Image allowed (SightEngine unavailable)" 
                    : "Image blocked (SightEngine unavailable)",
                Severity = _config.AllowImagesWhenSightEngineUnavailable ? ModerationSeverity.Clean : ModerationSeverity.Moderate,
                Provider = "Cascaded (No Image Analysis)",
                ProcessedAt = DateTime.UtcNow
            };
        }

        return await _sightEngineService.ModerateImageAsync(imageUrl, options);
    }

    public async Task<ContentModerationResult> ModerateTextAsync(string text, ContentModerationOptions? options = null)
    {
        return await ValidateContentAsync(text, "system");
    }

    public async Task<ContentModerationResult> ModerateVideoAsync(byte[] videoData, string fileName = "", ContentModerationOptions? options = null)
    {
        if (_sightEngineService == null)
        {
            _logger.LogWarning("Video moderation requested but SightEngine not available");
            return new ContentModerationResult
            {
                IsApproved = _config.AllowVideosWhenSightEngineUnavailable,
                ModerationReason = _config.AllowVideosWhenSightEngineUnavailable 
                    ? "Video allowed (SightEngine unavailable)" 
                    : "Video blocked (SightEngine unavailable)",
                Severity = _config.AllowVideosWhenSightEngineUnavailable ? ModerationSeverity.Clean : ModerationSeverity.Moderate,
                Provider = "Cascaded (No Video Analysis)",
                ProcessedAt = DateTime.UtcNow
            };
        }

        return await _sightEngineService.ModerateVideoAsync(videoData, fileName, options);
    }

    public async Task<ContentModerationResult> ModerateVideoAsync(string videoUrl, ContentModerationOptions? options = null)
    {
        if (_sightEngineService == null)
        {
            _logger.LogWarning("Video URL moderation requested but SightEngine not available");
            return new ContentModerationResult
            {
                IsApproved = _config.AllowVideosWhenSightEngineUnavailable,
                ModerationReason = _config.AllowVideosWhenSightEngineUnavailable 
                    ? "Video allowed (SightEngine unavailable)" 
                    : "Video blocked (SightEngine unavailable)",
                Severity = _config.AllowVideosWhenSightEngineUnavailable ? ModerationSeverity.Clean : ModerationSeverity.Moderate,
                Provider = "Cascaded (No Video Analysis)",
                ProcessedAt = DateTime.UtcNow
            };
        }

        return await _sightEngineService.ModerateVideoAsync(videoUrl, options);
    }

    public async Task<WorkflowModerationResult> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object> parameters)
    {
        if (_sightEngineService == null)
        {
            return new WorkflowModerationResult
            {
                Success = false,
                WorkflowId = workflowId,
                ErrorMessage = "SightEngine not available for workflow execution"
            };
        }

        return await _sightEngineService.ExecuteWorkflowAsync(workflowId, parameters);
    }

    public async Task<ServiceHealthResult> CheckServiceHealthAsync()
    {
        var basicHealth = new ServiceHealthResult
        {
            IsHealthy = true,
            Status = "Healthy",
            ResponseTime = TimeSpan.FromMilliseconds(1)
        };

        if (_sightEngineService == null)
        {
            return new ServiceHealthResult
            {
                IsHealthy = true,
                Status = "Basic Only",
                ServiceInfo = new Dictionary<string, object>
                {
                    ["basic_service"] = "healthy",
                    ["sightengine_service"] = "not_configured"
                }
            };
        }

        var sightEngineHealth = await _sightEngineService.CheckServiceHealthAsync();
        
        return new ServiceHealthResult
        {
            IsHealthy = basicHealth.IsHealthy && sightEngineHealth.IsHealthy,
            Status = sightEngineHealth.IsHealthy ? "Both Services Healthy" : "SightEngine Issues",
            ResponseTime = sightEngineHealth.ResponseTime,
            ServiceInfo = new Dictionary<string, object>
            {
                ["basic_service"] = "healthy",
                ["sightengine_service"] = sightEngineHealth.Status,
                ["sightengine_response_time"] = sightEngineHealth.ResponseTime.TotalMilliseconds
            }
        };
    }

    public async Task<UsageStatsResult> GetUsageStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        if (_sightEngineService == null)
        {
            return new UsageStatsResult
            {
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                EndDate = endDate ?? DateTime.UtcNow,
                TotalRequests = 0,
                RequestsByType = new Dictionary<string, int> { ["basic_only"] = 0 }
            };
        }

        return await _sightEngineService.GetUsageStatsAsync(startDate, endDate);
    }

    public async Task<bool> ReportMessageAsync(Guid messageId, string reporterId, string reason)
    {
        return await _basicModerationService.ReportMessageAsync(messageId, reporterId, reason);
    }

    public async Task<ModerationStatisticsDto> GetModerationStatisticsAsync()
    {
        return await _basicModerationService.GetModerationStatisticsAsync();
    }

    private ContentModerationResult CombineResults(ContentModerationResult basicResult, ContentModerationResult sightEngineResult, DateTime startTime)
    {
        // Use the most restrictive result
        var isApproved = basicResult.IsApproved && sightEngineResult.IsApproved;
        var severity = (ModerationSeverity)Math.Max((int)basicResult.Severity, (int)sightEngineResult.Severity);
        
        // Combine violations
        var combinedViolations = new List<string>();
        combinedViolations.AddRange(basicResult.Violations);
        combinedViolations.AddRange(sightEngineResult.Violations);

        // Combine moderation reasons
        var reasons = new List<string>();
        if (!string.IsNullOrEmpty(basicResult.ModerationReason))
            reasons.Add($"Basic: {basicResult.ModerationReason}");
        if (!string.IsNullOrEmpty(sightEngineResult.ModerationReason))
            reasons.Add($"SightEngine: {sightEngineResult.ModerationReason}");

        return new ContentModerationResult
        {
            IsApproved = isApproved,
            Severity = severity,
            Violations = combinedViolations.Distinct().ToList(),
            ModerationReason = reasons.Any() ? string.Join(" | ", reasons) : null,
            ModifiedContent = basicResult.ModifiedContent ?? sightEngineResult.ModifiedContent,
            RequiresManualReview = basicResult.RequiresManualReview || sightEngineResult.RequiresManualReview,
            Provider = "Cascaded (Basic + SightEngine)",
            ConfidenceScore = sightEngineResult.ConfidenceScore,
            Detections = sightEngineResult.Detections,
            ProcessedAt = DateTime.UtcNow,
            RawResponseJson = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["stage"] = "cascaded_complete",
                ["basic_result"] = new
                {
                    approved = basicResult.IsApproved,
                    severity = basicResult.Severity.ToString(),
                    violations = basicResult.Violations
                },
                ["sightengine_result"] = new
                {
                    approved = sightEngineResult.IsApproved,
                    severity = sightEngineResult.Severity.ToString(),
                    violations = sightEngineResult.Violations,
                    confidence = sightEngineResult.ConfidenceScore
                },
                ["final_decision"] = new
                {
                    approved = isApproved,
                    severity = severity.ToString(),
                    processing_time_ms = (DateTime.UtcNow - startTime).TotalMilliseconds
                }
            })
        };
    }
}