using System.Text.Json;
using frontend.Models;
using ToolsSharing.Core.Models.SecurityAnalytics;

namespace ToolsSharing.Frontend.Services;

public class SecurityAnalyticsService : ISecurityAnalyticsService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public SecurityAnalyticsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<SecurityDashboardData> GetSecurityDashboardAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = BuildQueryString(new Dictionary<string, object?>
        {
            ["startDate"] = startDate?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ["endDate"] = endDate?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        });

        var response = await _httpClient.GetAsync($"/api/securityanalytics/dashboard{query}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<SecurityDashboardData>>(content, _jsonOptions);

        if (apiResponse?.Success == true && apiResponse.Data != null)
        {
            return apiResponse.Data;
        }

        throw new InvalidOperationException($"Failed to get security dashboard: {apiResponse?.Message ?? "Unknown error"}");
    }

    public async Task<SecurityMetrics> GetSecurityMetricsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = BuildQueryString(new Dictionary<string, object?>
        {
            ["startDate"] = startDate?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ["endDate"] = endDate?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        });

        var response = await _httpClient.GetAsync($"/api/securityanalytics/metrics{query}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<SecurityMetrics>>(content, _jsonOptions);

        if (apiResponse?.Success == true && apiResponse.Data != null)
        {
            return apiResponse.Data;
        }

        throw new InvalidOperationException($"Failed to get security metrics: {apiResponse?.Message ?? "Unknown error"}");
    }

    public async Task<SystemHealthStatus> GetSystemHealthAsync()
    {
        var response = await _httpClient.GetAsync("/api/securityanalytics/health");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<SystemHealthStatus>>(content, _jsonOptions);

        if (apiResponse?.Success == true && apiResponse.Data != null)
        {
            return apiResponse.Data;
        }

        throw new InvalidOperationException($"Failed to get system health: {apiResponse?.Message ?? "Unknown error"}");
    }

    public async Task<List<SecurityThreat>> GetActiveThreatsAsync(int limit = 50)
    {
        var query = BuildQueryString(new Dictionary<string, object?>
        {
            ["limit"] = limit
        });

        var response = await _httpClient.GetAsync($"/api/securityanalytics/threats{query}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<SecurityThreat>>>(content, _jsonOptions);

        if (apiResponse?.Success == true && apiResponse.Data != null)
        {
            return apiResponse.Data;
        }

        throw new InvalidOperationException($"Failed to get active threats: {apiResponse?.Message ?? "Unknown error"}");
    }

    public async Task<SecurityThreat?> AnalyzeThreatAsync(string sourceIP, string? userId = null)
    {
        var request = new { SourceIP = sourceIP, UserId = userId };
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/securityanalytics/threats/analyze", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<SecurityThreat?>>(responseContent, _jsonOptions);

        if (apiResponse?.Success == true)
        {
            return apiResponse.Data;
        }

        throw new InvalidOperationException($"Failed to analyze threat: {apiResponse?.Message ?? "Unknown error"}");
    }

    public async Task<List<SecurityAlert>> GetActiveAlertsAsync(int limit = 100)
    {
        var query = BuildQueryString(new Dictionary<string, object?>
        {
            ["limit"] = limit
        });

        var response = await _httpClient.GetAsync($"/api/securityanalytics/alerts{query}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<SecurityAlert>>>(content, _jsonOptions);

        if (apiResponse?.Success == true && apiResponse.Data != null)
        {
            return apiResponse.Data;
        }

        throw new InvalidOperationException($"Failed to get active alerts: {apiResponse?.Message ?? "Unknown error"}");
    }

    public async Task<SecurityTrendData> GetSecurityTrendsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = BuildQueryString(new Dictionary<string, object?>
        {
            ["startDate"] = startDate?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ["endDate"] = endDate?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        });

        var response = await _httpClient.GetAsync($"/api/securityanalytics/trends{query}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<SecurityTrendData>>(content, _jsonOptions);

        if (apiResponse?.Success == true && apiResponse.Data != null)
        {
            return apiResponse.Data;
        }

        throw new InvalidOperationException($"Failed to get security trends: {apiResponse?.Message ?? "Unknown error"}");
    }

    public async Task<List<AttackPatternSummary>> GetAttackPatternsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = BuildQueryString(new Dictionary<string, object?>
        {
            ["startDate"] = startDate?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ["endDate"] = endDate?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        });

        var response = await _httpClient.GetAsync($"/api/securityanalytics/attack-patterns{query}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<AttackPatternSummary>>>(content, _jsonOptions);

        if (apiResponse?.Success == true && apiResponse.Data != null)
        {
            return apiResponse.Data;
        }

        throw new InvalidOperationException($"Failed to get attack patterns: {apiResponse?.Message ?? "Unknown error"}");
    }

    public async Task<List<GeographicThreat>> GetGeographicThreatsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = BuildQueryString(new Dictionary<string, object?>
        {
            ["startDate"] = startDate?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ["endDate"] = endDate?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        });

        var response = await _httpClient.GetAsync($"/api/securityanalytics/geographic-threats{query}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<GeographicThreat>>>(content, _jsonOptions);

        if (apiResponse?.Success == true && apiResponse.Data != null)
        {
            return apiResponse.Data;
        }

        throw new InvalidOperationException($"Failed to get geographic threats: {apiResponse?.Message ?? "Unknown error"}");
    }

    public async Task<decimal> CalculateRiskScoreAsync(string sourceIP, string? userId = null, Dictionary<string, object>? factors = null)
    {
        var request = new { SourceIP = sourceIP, UserId = userId, Factors = factors };
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/securityanalytics/risk-score", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<RiskScoreResult>>(responseContent, _jsonOptions);

        if (apiResponse?.Success == true && apiResponse.Data != null)
        {
            return apiResponse.Data.RiskScore;
        }

        throw new InvalidOperationException($"Failed to calculate risk score: {apiResponse?.Message ?? "Unknown error"}");
    }

    public async Task<bool> IsIPHighRiskAsync(string ipAddress)
    {
        var response = await _httpClient.GetAsync($"/api/securityanalytics/ip-risk/{Uri.EscapeDataString(ipAddress)}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<IPRiskStatus>>(content, _jsonOptions);

        if (apiResponse?.Success == true && apiResponse.Data != null)
        {
            return apiResponse.Data.IsHighRisk;
        }

        throw new InvalidOperationException($"Failed to check IP risk: {apiResponse?.Message ?? "Unknown error"}");
    }

    private static string BuildQueryString(Dictionary<string, object?> parameters)
    {
        var queryParams = parameters
            .Where(kvp => kvp.Value != null)
            .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!.ToString()!)}")
            .ToList();

        return queryParams.Any() ? "?" + string.Join("&", queryParams) : string.Empty;
    }
}

/// <summary>
/// Risk score calculation result
/// </summary>
public class RiskScoreResult
{
    public string SourceIP { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public decimal RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; }
}

/// <summary>
/// IP risk status result
/// </summary>
public class IPRiskStatus
{
    public string IPAddress { get; set; } = string.Empty;
    public bool IsHighRisk { get; set; }
    public decimal RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
}