using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ToolsSharing.Core.Configuration;
using ToolsSharing.Core.DTOs.ContentModeration;
using ToolsSharing.Core.Interfaces;

namespace ToolsSharing.Infrastructure.Services;

public class SightEngineService : IContentModerationService
{
    private readonly HttpClient _httpClient;
    private readonly SightEngineConfiguration _config;
    private readonly ILogger<SightEngineService> _logger;
    private readonly string _authHeader;

    public SightEngineService(
        HttpClient httpClient,
        IOptions<SightEngineConfiguration> config,
        ILogger<SightEngineService> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;

        if (!_config.IsConfigured)
        {
            throw new InvalidOperationException("SightEngine is not properly configured. Please check ApiUser and ApiSecret settings.");
        }

        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_config.ApiUser}:{_config.ApiSecret}"));
        _authHeader = $"Basic {credentials}";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    // Legacy method for message validation
    public async Task<ContentModerationResult> ValidateContentAsync(string content, string senderId)
    {
        return await ModerateTextAsync(content, new ContentModerationOptions
        {
            Models = _config.Models.Text
        });
    }

    // Legacy method for reporting messages
    public async Task<bool> ReportMessageAsync(Guid messageId, string reporterId, string reason)
    {
        _logger.LogInformation("Message {MessageId} reported by {ReporterId} for reason: {Reason}", messageId, reporterId, reason);
        return true;
    }

    // Legacy method for statistics
    public async Task<ModerationStatisticsDto> GetModerationStatisticsAsync()
    {
        var stats = await GetUsageStatsAsync(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
        return new ModerationStatisticsDto
        {
            TotalMessagesProcessed = stats.TotalRequests,
            ApprovedMessages = stats.SuccessfulRequests,
            ModeratedMessages = stats.FailedRequests,
            PendingReview = 0,
            ViolationsBySeverity = new Dictionary<ModerationSeverity, int>(),
            CommonViolations = new List<string>()
        };
    }

    public async Task<ContentModerationResult> ModerateImageAsync(byte[] imageData, string fileName = "", ContentModerationOptions? options = null)
    {
        try
        {
            options ??= new ContentModerationOptions { Models = _config.Models.Image };

            using var content = new MultipartFormDataContent();
            using var imageContent = new ByteArrayContent(imageData);
            imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse(GetMimeType(fileName));
            content.Add(imageContent, "media", fileName);

            var models = string.Join(",", options.Models);
            content.Add(new StringContent(models), "models");

            if (options.Language != null)
                content.Add(new StringContent(options.Language), "lang");

            foreach (var param in options.CustomParameters)
                content.Add(new StringContent(param.Value.ToString() ?? ""), param.Key);

            var response = await _httpClient.PostAsync("check.json", content);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (_config.EnableLogging)
            {
                _logger.LogInformation("SightEngine image moderation response: {Response}", jsonResponse);
            }

            return ParseModerationResponse(jsonResponse, "image");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during image moderation");
            return CreateErrorResult(ex);
        }
    }

    public async Task<ContentModerationResult> ModerateImageAsync(string imageUrl, ContentModerationOptions? options = null)
    {
        try
        {
            options ??= new ContentModerationOptions { Models = _config.Models.Image };

            var parameters = new List<KeyValuePair<string, string>>
            {
                new("url", imageUrl),
                new("models", string.Join(",", options.Models))
            };

            if (options.Language != null)
                parameters.Add(new KeyValuePair<string, string>("lang", options.Language));

            foreach (var param in options.CustomParameters)
                parameters.Add(new KeyValuePair<string, string>(param.Key, param.Value.ToString() ?? ""));

            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync("check.json", content);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (_config.EnableLogging)
            {
                _logger.LogInformation("SightEngine image URL moderation response: {Response}", jsonResponse);
            }

            return ParseModerationResponse(jsonResponse, "image");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during image URL moderation");
            return CreateErrorResult(ex);
        }
    }

    public async Task<ContentModerationResult> ModerateTextAsync(string text, ContentModerationOptions? options = null)
    {
        try
        {
            options ??= new ContentModerationOptions { Models = _config.Models.Text };

            var parameters = new List<KeyValuePair<string, string>>
            {
                new("text", text),
                new("models", string.Join(",", options.Models))
            };

            if (options.Language != null)
                parameters.Add(new KeyValuePair<string, string>("lang", options.Language));

            foreach (var param in options.CustomParameters)
                parameters.Add(new KeyValuePair<string, string>(param.Key, param.Value.ToString() ?? ""));

            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync("text/check.json", content);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (_config.EnableLogging)
            {
                _logger.LogInformation("SightEngine text moderation response: {Response}", jsonResponse);
            }

            return ParseModerationResponse(jsonResponse, "text");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during text moderation");
            return CreateErrorResult(ex);
        }
    }

    public async Task<ContentModerationResult> ModerateVideoAsync(byte[] videoData, string fileName = "", ContentModerationOptions? options = null)
    {
        try
        {
            options ??= new ContentModerationOptions { Models = _config.Models.Video };

            using var content = new MultipartFormDataContent();
            using var videoContent = new ByteArrayContent(videoData);
            videoContent.Headers.ContentType = MediaTypeHeaderValue.Parse(GetVideoMimeType(fileName));
            content.Add(videoContent, "media", fileName);

            var models = string.Join(",", options.Models);
            content.Add(new StringContent(models), "models");

            if (options.Language != null)
                content.Add(new StringContent(options.Language), "lang");

            foreach (var param in options.CustomParameters)
                content.Add(new StringContent(param.Value.ToString() ?? ""), param.Key);

            var response = await _httpClient.PostAsync("video/check-sync.json", content);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (_config.EnableLogging)
            {
                _logger.LogInformation("SightEngine video moderation response: {Response}", jsonResponse);
            }

            return ParseModerationResponse(jsonResponse, "video");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during video moderation");
            return CreateErrorResult(ex);
        }
    }

    public async Task<ContentModerationResult> ModerateVideoAsync(string videoUrl, ContentModerationOptions? options = null)
    {
        try
        {
            options ??= new ContentModerationOptions { Models = _config.Models.Video };

            var parameters = new List<KeyValuePair<string, string>>
            {
                new("url", videoUrl),
                new("models", string.Join(",", options.Models))
            };

            if (options.Language != null)
                parameters.Add(new KeyValuePair<string, string>("lang", options.Language));

            foreach (var param in options.CustomParameters)
                parameters.Add(new KeyValuePair<string, string>(param.Key, param.Value.ToString() ?? ""));

            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync("video/check-sync.json", content);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (_config.EnableLogging)
            {
                _logger.LogInformation("SightEngine video URL moderation response: {Response}", jsonResponse);
            }

            return ParseModerationResponse(jsonResponse, "video");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during video URL moderation");
            return CreateErrorResult(ex);
        }
    }

    public async Task<WorkflowModerationResult> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object> parameters)
    {
        try
        {
            var paramList = new List<KeyValuePair<string, string>>
            {
                new("workflow", workflowId)
            };

            foreach (var param in parameters)
                paramList.Add(new KeyValuePair<string, string>(param.Key, param.Value.ToString() ?? ""));

            var content = new FormUrlEncodedContent(paramList);
            var response = await _httpClient.PostAsync("workflow.json", content);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (_config.EnableLogging)
            {
                _logger.LogInformation("SightEngine workflow execution response: {Response}", jsonResponse);
            }

            return ParseWorkflowResponse(jsonResponse, workflowId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing workflow {WorkflowId}", workflowId);
            return new WorkflowModerationResult
            {
                Success = false,
                WorkflowId = workflowId,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ServiceHealthResult> CheckServiceHealthAsync()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await _httpClient.GetAsync("account.json");
            stopwatch.Stop();

            var isHealthy = response.IsSuccessStatusCode;
            var responseContent = await response.Content.ReadAsStringAsync();

            return new ServiceHealthResult
            {
                IsHealthy = isHealthy,
                Status = isHealthy ? "Healthy" : "Unhealthy",
                ResponseTime = stopwatch.Elapsed,
                ServiceInfo = isHealthy ? JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent) ?? new() : new()
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "SightEngine health check failed");
            return new ServiceHealthResult
            {
                IsHealthy = false,
                Status = "Error",
                ResponseTime = stopwatch.Elapsed,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<UsageStatsResult> GetUsageStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var parameters = new List<KeyValuePair<string, string>>();

            if (startDate.HasValue)
                parameters.Add(new KeyValuePair<string, string>("from", startDate.Value.ToString("yyyy-MM-dd")));

            if (endDate.HasValue)
                parameters.Add(new KeyValuePair<string, string>("to", endDate.Value.ToString("yyyy-MM-dd")));

            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync("usage.json", content);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (_config.EnableLogging)
            {
                _logger.LogInformation("SightEngine usage stats response: {Response}", jsonResponse);
            }

            return ParseUsageStatsResponse(jsonResponse, startDate, endDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving usage statistics");
            return new UsageStatsResult
            {
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                EndDate = endDate ?? DateTime.UtcNow
            };
        }
    }

    private ContentModerationResult ParseModerationResponse(string jsonResponse, string contentType)
    {
        try
        {
            var response = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse);
            if (response == null) return CreateErrorResult(new Exception("Failed to parse response"));

            var result = new ContentModerationResult
            {
                Provider = "SightEngine",
                ProcessedAt = DateTime.UtcNow,
                RawResponseJson = JsonSerializer.Serialize(response)
            };

            var violations = new List<string>();
            var detections = new List<Detection>();
            var maxConfidence = 0.0;

            // Parse different model responses
            if (response.ContainsKey("nudity"))
            {
                var nudity = JsonSerializer.Deserialize<JsonElement>(response["nudity"].ToString() ?? "{}");
                if (nudity.TryGetProperty("sexual_activity", out var sexualActivity))
                {
                    var confidence = sexualActivity.GetDouble();
                    if (confidence > _config.Thresholds.NudityThreshold)
                    {
                        violations.Add("Nudity/Sexual Content");
                        detections.Add(new Detection
                        {
                            Type = "nudity",
                            Category = "sexual_activity",
                            Confidence = confidence,
                            Description = "Sexual activity detected"
                        });
                        maxConfidence = Math.Max(maxConfidence, confidence);
                    }
                }
            }

            if (response.ContainsKey("weapon"))
            {
                var weapon = JsonSerializer.Deserialize<JsonElement>(response["weapon"].ToString() ?? "{}");
                if (weapon.TryGetProperty("prob", out var weaponProb))
                {
                    var confidence = weaponProb.GetDouble();
                    if (confidence > _config.Thresholds.WeaponThreshold)
                    {
                        violations.Add("Weapons");
                        detections.Add(new Detection
                        {
                            Type = "weapon",
                            Category = "weapon",
                            Confidence = confidence,
                            Description = "Weapon detected"
                        });
                        maxConfidence = Math.Max(maxConfidence, confidence);
                    }
                }
            }

            if (response.ContainsKey("alcohol"))
            {
                var alcohol = JsonSerializer.Deserialize<JsonElement>(response["alcohol"].ToString() ?? "{}");
                if (alcohol.TryGetProperty("prob", out var alcoholProb))
                {
                    var confidence = alcoholProb.GetDouble();
                    if (confidence > _config.Thresholds.AlcoholThreshold)
                    {
                        violations.Add("Alcohol");
                        detections.Add(new Detection
                        {
                            Type = "alcohol",
                            Category = "alcohol",
                            Confidence = confidence,
                            Description = "Alcohol detected"
                        });
                        maxConfidence = Math.Max(maxConfidence, confidence);
                    }
                }
            }

            if (response.ContainsKey("drugs"))
            {
                var drugs = JsonSerializer.Deserialize<JsonElement>(response["drugs"].ToString() ?? "{}");
                if (drugs.TryGetProperty("prob", out var drugsProb))
                {
                    var confidence = drugsProb.GetDouble();
                    if (confidence > _config.Thresholds.DrugThreshold)
                    {
                        violations.Add("Drugs");
                        detections.Add(new Detection
                        {
                            Type = "drugs",
                            Category = "drugs",
                            Confidence = confidence,
                            Description = "Drug content detected"
                        });
                        maxConfidence = Math.Max(maxConfidence, confidence);
                    }
                }
            }

            if (response.ContainsKey("offensive"))
            {
                var offensive = JsonSerializer.Deserialize<JsonElement>(response["offensive"].ToString() ?? "{}");
                if (offensive.TryGetProperty("prob", out var offensiveProb))
                {
                    var confidence = offensiveProb.GetDouble();
                    if (confidence > _config.Thresholds.OffensiveThreshold)
                    {
                        violations.Add("Offensive Content");
                        detections.Add(new Detection
                        {
                            Type = "offensive",
                            Category = "offensive",
                            Confidence = confidence,
                            Description = "Offensive content detected"
                        });
                        maxConfidence = Math.Max(maxConfidence, confidence);
                    }
                }
            }

            if (response.ContainsKey("profanity"))
            {
                var profanity = JsonSerializer.Deserialize<JsonElement>(response["profanity"].ToString() ?? "{}");
                if (profanity.TryGetProperty("prob", out var profanityProb))
                {
                    var confidence = profanityProb.GetDouble();
                    if (confidence > _config.Thresholds.ProfanityThreshold)
                    {
                        violations.Add("Profanity");
                        detections.Add(new Detection
                        {
                            Type = "profanity",
                            Category = "profanity",
                            Confidence = confidence,
                            Description = "Profanity detected"
                        });
                        maxConfidence = Math.Max(maxConfidence, confidence);
                    }
                }
            }

            // Parse text-classification model results (ML-based)
            if (response.ContainsKey("classes"))
            {
                var classes = JsonSerializer.Deserialize<JsonElement>(response["classes"].ToString() ?? "{}");
                
                // Sexual content detection
                if (classes.TryGetProperty("sexual", out var sexual))
                {
                    var confidence = sexual.GetDouble();
                    if (confidence > _config.Thresholds.SexualThreshold)
                    {
                        violations.Add("Sexual Content");
                        detections.Add(new Detection
                        {
                            Type = "text-classification",
                            Category = "sexual",
                            Confidence = confidence,
                            Description = "Sexual content detected"
                        });
                        maxConfidence = Math.Max(maxConfidence, confidence);
                    }
                }

                // Discriminatory content detection
                if (classes.TryGetProperty("discriminatory", out var discriminatory))
                {
                    var confidence = discriminatory.GetDouble();
                    if (confidence > _config.Thresholds.DiscriminatoryThreshold)
                    {
                        violations.Add("Discriminatory Content");
                        detections.Add(new Detection
                        {
                            Type = "text-classification",
                            Category = "discriminatory",
                            Confidence = confidence,
                            Description = "Discriminatory/hate speech detected"
                        });
                        maxConfidence = Math.Max(maxConfidence, confidence);
                    }
                }

                // Insulting content detection
                if (classes.TryGetProperty("insulting", out var insulting))
                {
                    var confidence = insulting.GetDouble();
                    if (confidence > _config.Thresholds.InsultingThreshold)
                    {
                        violations.Add("Insulting Content");
                        detections.Add(new Detection
                        {
                            Type = "text-classification",
                            Category = "insulting",
                            Confidence = confidence,
                            Description = "Insulting content detected"
                        });
                        maxConfidence = Math.Max(maxConfidence, confidence);
                    }
                }

                // Violent content detection
                if (classes.TryGetProperty("violent", out var violent))
                {
                    var confidence = violent.GetDouble();
                    if (confidence > _config.Thresholds.ViolentThreshold)
                    {
                        violations.Add("Violent Content");
                        detections.Add(new Detection
                        {
                            Type = "text-classification",
                            Category = "violent",
                            Confidence = confidence,
                            Description = "Violent/threatening content detected"
                        });
                        maxConfidence = Math.Max(maxConfidence, confidence);
                    }
                }

                // Toxic content detection
                if (classes.TryGetProperty("toxic", out var toxic))
                {
                    var confidence = toxic.GetDouble();
                    if (confidence > _config.Thresholds.ToxicThreshold)
                    {
                        violations.Add("Toxic Content");
                        detections.Add(new Detection
                        {
                            Type = "text-classification",
                            Category = "toxic",
                            Confidence = confidence,
                            Description = "Toxic content detected"
                        });
                        maxConfidence = Math.Max(maxConfidence, confidence);
                    }
                }

                // Self-harm content detection
                if (classes.TryGetProperty("self-harm", out var selfHarm))
                {
                    var confidence = selfHarm.GetDouble();
                    if (confidence > _config.Thresholds.SelfHarmThreshold)
                    {
                        violations.Add("Self-Harm Content");
                        detections.Add(new Detection
                        {
                            Type = "text-classification",
                            Category = "self-harm",
                            Confidence = confidence,
                            Description = "Self-harm content detected"
                        });
                        maxConfidence = Math.Max(maxConfidence, confidence);
                    }
                }
            }

            // Parse text-content model results (rule-based comprehensive)
            if (response.ContainsKey("text"))
            {
                var textAnalysis = JsonSerializer.Deserialize<JsonElement>(response["text"].ToString() ?? "{}");
                
                // Personal Information detection
                if (textAnalysis.TryGetProperty("has_pii", out var hasPii) && hasPii.GetBoolean())
                {
                    violations.Add("Personal Information Detected");
                    detections.Add(new Detection
                    {
                        Type = "text-content",
                        Category = "personal-info",
                        Confidence = 1.0, // Rule-based detection is binary
                        Description = "Personal information detected (email, phone, SSN, etc.)"
                    });
                    maxConfidence = Math.Max(maxConfidence, 1.0);
                }

                // Links detection
                if (textAnalysis.TryGetProperty("has_links", out var hasLinks) && hasLinks.GetBoolean())
                {
                    detections.Add(new Detection
                    {
                        Type = "text-content",
                        Category = "links",
                        Confidence = 1.0,
                        Description = "Links detected in content"
                    });
                    // Note: Links might not always be violations, depends on context
                }

                // Spam detection
                if (textAnalysis.TryGetProperty("is_spam", out var isSpam) && isSpam.GetBoolean())
                {
                    violations.Add("Spam Content");
                    detections.Add(new Detection
                    {
                        Type = "text-content",
                        Category = "spam",
                        Confidence = 1.0,
                        Description = "Spam content detected"
                    });
                    maxConfidence = Math.Max(maxConfidence, 1.0);
                }

                // Extremism detection
                if (textAnalysis.TryGetProperty("has_extremism", out var hasExtremism) && hasExtremism.GetBoolean())
                {
                    violations.Add("Extremist Content");
                    detections.Add(new Detection
                    {
                        Type = "text-content",
                        Category = "extremism",
                        Confidence = 1.0,
                        Description = "Extremist content detected"
                    });
                    maxConfidence = Math.Max(maxConfidence, 1.0);
                }

                // Drug content detection
                if (textAnalysis.TryGetProperty("has_drug_content", out var hasDrugContent) && hasDrugContent.GetBoolean())
                {
                    violations.Add("Drug-Related Content");
                    detections.Add(new Detection
                    {
                        Type = "text-content",
                        Category = "drug",
                        Confidence = 1.0,
                        Description = "Drug-related content detected"
                    });
                    maxConfidence = Math.Max(maxConfidence, 1.0);
                }
            }

            result.Violations = violations;
            result.Detections = detections;
            result.ConfidenceScore = maxConfidence;
            result.IsApproved = violations.Count == 0;
            result.Severity = CalculateSeverity(maxConfidence, violations.Count);
            result.RequiresManualReview = maxConfidence > 0.8 || violations.Count > 2;

            if (!result.IsApproved)
            {
                result.ModerationReason = $"Content violates community guidelines: {string.Join(", ", violations)}";
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing SightEngine response: {Response}", jsonResponse);
            return CreateErrorResult(ex);
        }
    }

    private WorkflowModerationResult ParseWorkflowResponse(string jsonResponse, string workflowId)
    {
        try
        {
            var response = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse);
            if (response == null)
            {
                return new WorkflowModerationResult
                {
                    Success = false,
                    WorkflowId = workflowId,
                    ErrorMessage = "Failed to parse workflow response"
                };
            }

            return new WorkflowModerationResult
            {
                Success = true,
                WorkflowId = workflowId,
                Results = response,
                Steps = new List<WorkflowStep>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing workflow response");
            return new WorkflowModerationResult
            {
                Success = false,
                WorkflowId = workflowId,
                ErrorMessage = ex.Message
            };
        }
    }

    private UsageStatsResult ParseUsageStatsResponse(string jsonResponse, DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var response = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse);
            if (response == null) return new UsageStatsResult();

            var result = new UsageStatsResult
            {
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                EndDate = endDate ?? DateTime.UtcNow
            };

            if (response.ContainsKey("total"))
            {
                if (int.TryParse(response["total"].ToString(), out var total))
                    result.TotalRequests = total;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing usage stats response");
            return new UsageStatsResult();
        }
    }

    private ModerationSeverity CalculateSeverity(double confidence, int violationCount)
    {
        if (violationCount == 0) return ModerationSeverity.Clean;
        if (confidence < 0.3) return ModerationSeverity.Minor;
        if (confidence < 0.6) return ModerationSeverity.Moderate;
        if (confidence < 0.8) return ModerationSeverity.Severe;
        return ModerationSeverity.Critical;
    }

    private ContentModerationResult CreateErrorResult(Exception ex)
    {
        return new ContentModerationResult
        {
            IsApproved = false,
            ModerationReason = "Error during content moderation",
            Severity = ModerationSeverity.Critical,
            Violations = new List<string> { "Processing Error" },
            RequiresManualReview = true,
            Provider = "SightEngine",
            ProcessedAt = DateTime.UtcNow,
            RawResponseJson = JsonSerializer.Serialize(new Dictionary<string, object> { ["error"] = ex.Message })
        };
    }

    private static string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => "image/jpeg"
        };
    }

    private static string GetVideoMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".mp4" => "video/mp4",
            ".avi" => "video/avi",
            ".mov" => "video/quicktime",
            ".webm" => "video/webm",
            ".mkv" => "video/x-matroska",
            _ => "video/mp4"
        };
    }
}