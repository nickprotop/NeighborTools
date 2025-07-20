using System.Text.Json;
using System.Text;
using frontend.Models;

namespace frontend.Services;

/// <summary>
/// Admin-specific service for mutual closure oversight and management
/// Extends the regular MutualClosureService with admin-only operations
/// </summary>
public class AdminMutualClosureService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AdminMutualClosureService> _logger;

    public AdminMutualClosureService(HttpClient httpClient, ILogger<AdminMutualClosureService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Get all mutual closures with admin privileges (no filtering by user)
    /// </summary>
    public async Task<ApiResponse<IEnumerable<MutualClosureSummaryDto>>?> GetAllMutualClosuresAsync()
    {
        try
        {
            _logger.LogInformation("Fetching all mutual closures for admin oversight");
            
            var response = await _httpClient.GetAsync("api/admin/mutual-closures");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<IEnumerable<MutualClosureSummaryDto>>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                _logger.LogInformation("Successfully fetched {Count} mutual closures", result?.Data?.Count() ?? 0);
                return result;
            }
            else
            {
                _logger.LogWarning("Failed to fetch mutual closures: {StatusCode} - {Content}", response.StatusCode, content);
                return new ApiResponse<IEnumerable<MutualClosureSummaryDto>>
                {
                    Success = false,
                    Message = $"Failed to fetch mutual closures: {response.StatusCode}",
                    Data = Enumerable.Empty<MutualClosureSummaryDto>()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all mutual closures");
            return new ApiResponse<IEnumerable<MutualClosureSummaryDto>>
            {
                Success = false,
                Message = "An error occurred while fetching mutual closures",
                Data = Enumerable.Empty<MutualClosureSummaryDto>()
            };
        }
    }

    /// <summary>
    /// Get mutual closure statistics for admin dashboard
    /// </summary>
    public async Task<ApiResponse<MutualClosureStatisticsDto>?> GetMutualClosureStatisticsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching mutual closure statistics");
            
            var response = await _httpClient.GetAsync("api/admin/mutual-closures/statistics");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<MutualClosureStatisticsDto>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                _logger.LogInformation("Successfully fetched mutual closure statistics");
                return result;
            }
            else
            {
                _logger.LogWarning("Failed to fetch statistics: {StatusCode} - {Content}", response.StatusCode, content);
                return new ApiResponse<MutualClosureStatisticsDto>
                {
                    Success = false,
                    Message = $"Failed to fetch statistics: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching mutual closure statistics");
            return new ApiResponse<MutualClosureStatisticsDto>
            {
                Success = false,
                Message = "An error occurred while fetching statistics"
            };
        }
    }

    /// <summary>
    /// Force a mutual closure request into admin review status
    /// </summary>
    public async Task<ApiResponse<bool>?> ForceAdminReviewAsync(Guid mutualClosureId, string reason)
    {
        try
        {
            _logger.LogInformation("Forcing admin review for mutual closure {MutualClosureId}", mutualClosureId);
            
            var request = new ForceAdminReviewRequest
            {
                MutualClosureId = mutualClosureId,
                Reason = reason,
                AdminAction = "Force Review"
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"api/admin/mutual-closures/{mutualClosureId}/force-review", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<bool>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                _logger.LogInformation("Successfully forced admin review for mutual closure {MutualClosureId}", mutualClosureId);
                return result;
            }
            else
            {
                _logger.LogWarning("Failed to force admin review: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Failed to force admin review: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forcing admin review for mutual closure {MutualClosureId}", mutualClosureId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "An error occurred while forcing admin review"
            };
        }
    }

    /// <summary>
    /// Admin approval of a mutual closure request
    /// </summary>
    public async Task<ApiResponse<bool>?> ApproveMultualClosureAsync(Guid mutualClosureId, string adminNotes = "")
    {
        try
        {
            _logger.LogInformation("Admin approving mutual closure {MutualClosureId}", mutualClosureId);
            
            var request = new AdminMutualClosureDecisionRequest
            {
                MutualClosureId = mutualClosureId,
                Decision = "Approve",
                AdminNotes = adminNotes,
                ProcessRefund = true
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"api/admin/mutual-closures/{mutualClosureId}/approve", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<bool>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                _logger.LogInformation("Successfully approved mutual closure {MutualClosureId}", mutualClosureId);
                return result;
            }
            else
            {
                _logger.LogWarning("Failed to approve mutual closure: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Failed to approve mutual closure: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving mutual closure {MutualClosureId}", mutualClosureId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "An error occurred while approving mutual closure"
            };
        }
    }

    /// <summary>
    /// Admin rejection of a mutual closure request
    /// </summary>
    public async Task<ApiResponse<bool>?> RejectMutualClosureAsync(Guid mutualClosureId, string reason, string adminNotes = "")
    {
        try
        {
            _logger.LogInformation("Admin rejecting mutual closure {MutualClosureId}", mutualClosureId);
            
            var request = new AdminMutualClosureDecisionRequest
            {
                MutualClosureId = mutualClosureId,
                Decision = "Reject",
                RejectionReason = reason,
                AdminNotes = adminNotes,
                ProcessRefund = false
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"api/admin/mutual-closures/{mutualClosureId}/reject", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<bool>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                _logger.LogInformation("Successfully rejected mutual closure {MutualClosureId}", mutualClosureId);
                return result;
            }
            else
            {
                _logger.LogWarning("Failed to reject mutual closure: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Failed to reject mutual closure: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting mutual closure {MutualClosureId}", mutualClosureId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "An error occurred while rejecting mutual closure"
            };
        }
    }

    /// <summary>
    /// Get admin audit trail for a specific mutual closure
    /// </summary>
    public async Task<ApiResponse<IEnumerable<MutualClosureAuditLogDto>>?> GetMutualClosureAuditTrailAsync(Guid mutualClosureId)
    {
        try
        {
            _logger.LogInformation("Fetching audit trail for mutual closure {MutualClosureId}", mutualClosureId);
            
            var response = await _httpClient.GetAsync($"api/admin/mutual-closures/{mutualClosureId}/audit-trail");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<IEnumerable<MutualClosureAuditLogDto>>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                _logger.LogInformation("Successfully fetched audit trail for mutual closure {MutualClosureId}", mutualClosureId);
                return result;
            }
            else
            {
                _logger.LogWarning("Failed to fetch audit trail: {StatusCode} - {Content}", response.StatusCode, content);
                return new ApiResponse<IEnumerable<MutualClosureAuditLogDto>>
                {
                    Success = false,
                    Message = $"Failed to fetch audit trail: {response.StatusCode}",
                    Data = Enumerable.Empty<MutualClosureAuditLogDto>()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching audit trail for mutual closure {MutualClosureId}", mutualClosureId);
            return new ApiResponse<IEnumerable<MutualClosureAuditLogDto>>
            {
                Success = false,
                Message = "An error occurred while fetching audit trail",
                Data = Enumerable.Empty<MutualClosureAuditLogDto>()
            };
        }
    }

    /// <summary>
    /// Export mutual closure data for reporting/compliance
    /// </summary>
    public async Task<ApiResponse<string>?> ExportMutualClosureDataAsync(Guid mutualClosureId, string format = "json")
    {
        try
        {
            _logger.LogInformation("Exporting data for mutual closure {MutualClosureId} in format {Format}", mutualClosureId, format);
            
            var response = await _httpClient.GetAsync($"api/admin/mutual-closures/{mutualClosureId}/export?format={format}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<string>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                _logger.LogInformation("Successfully exported data for mutual closure {MutualClosureId}", mutualClosureId);
                return result;
            }
            else
            {
                _logger.LogWarning("Failed to export data: {StatusCode} - {Content}", response.StatusCode, content);
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Failed to export data: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data for mutual closure {MutualClosureId}", mutualClosureId);
            return new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred while exporting data"
            };
        }
    }
}

// Admin-specific DTOs
public class MutualClosureStatisticsDto
{
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int AcceptedCount { get; set; }
    public int RejectedCount { get; set; }
    public int ExpiredCount { get; set; }
    public int CancelledCount { get; set; }
    public int UnderAdminReviewCount { get; set; }
    public decimal TotalRefundAmount { get; set; }
    public decimal AverageRefundAmount { get; set; }
    public double AverageResolutionTimeHours { get; set; }
    public int HighValueCount { get; set; } // Count of requests > $100
}

public class ForceAdminReviewRequest
{
    public Guid MutualClosureId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string AdminAction { get; set; } = string.Empty;
}

public class AdminMutualClosureDecisionRequest
{
    public Guid MutualClosureId { get; set; }
    public string Decision { get; set; } = string.Empty; // "Approve" or "Reject"
    public string RejectionReason { get; set; } = string.Empty;
    public string AdminNotes { get; set; } = string.Empty;
    public bool ProcessRefund { get; set; }
}

public class MutualClosureAuditLogDto
{
    public Guid Id { get; set; }
    public Guid MutualClosureId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public string PerformedByUserId { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}