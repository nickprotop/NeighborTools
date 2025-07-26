using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Core.Models.SecurityAnalytics;
using ToolsSharing.Core.Common.Models;

namespace ToolsSharing.API.Controllers;

/// <summary>
/// Controller for security analytics and threat monitoring
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SecurityAnalyticsController : ControllerBase
{
    private readonly ISecurityAnalyticsService _securityAnalyticsService;
    private readonly ILogger<SecurityAnalyticsController> _logger;

    public SecurityAnalyticsController(
        ISecurityAnalyticsService securityAnalyticsService,
        ILogger<SecurityAnalyticsController> logger)
    {
        _securityAnalyticsService = securityAnalyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Get comprehensive security dashboard data
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<SecurityDashboardData>>> GetSecurityDashboard(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var dashboard = await _securityAnalyticsService.GetSecurityDashboardAsync(startDate, endDate);
            return Ok(new ApiResponse<SecurityDashboardData>
            {
                Success = true,
                Data = dashboard,
                Message = "Security dashboard data retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security dashboard");
            return StatusCode(500, new ApiResponse<SecurityDashboardData>
            {
                Success = false,
                Message = "An error occurred while retrieving security dashboard data",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get security metrics overview
    /// </summary>
    [HttpGet("metrics")]
    public async Task<ActionResult<ApiResponse<SecurityMetrics>>> GetSecurityMetrics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var metrics = await _securityAnalyticsService.GetSecurityMetricsAsync(startDate, endDate);
            return Ok(new ApiResponse<SecurityMetrics>
            {
                Success = true,
                Data = metrics,
                Message = "Security metrics retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security metrics");
            return StatusCode(500, new ApiResponse<SecurityMetrics>
            {
                Success = false,
                Message = "An error occurred while retrieving security metrics",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get system health status
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<ApiResponse<SystemHealthStatus>>> GetSystemHealth()
    {
        try
        {
            var health = await _securityAnalyticsService.GetSystemHealthAsync();
            return Ok(new ApiResponse<SystemHealthStatus>
            {
                Success = true,
                Data = health,
                Message = "System health status retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system health");
            return StatusCode(500, new ApiResponse<SystemHealthStatus>
            {
                Success = false,
                Message = "An error occurred while retrieving system health",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get active security threats
    /// </summary>
    [HttpGet("threats")]
    public async Task<ActionResult<ApiResponse<List<SecurityThreat>>>> GetActiveThreats(
        [FromQuery] int limit = 50)
    {
        try
        {
            var threats = await _securityAnalyticsService.GetActiveThreatsAsync(limit);
            return Ok(new ApiResponse<List<SecurityThreat>>
            {
                Success = true,
                Data = threats,
                Message = "Active threats retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active threats");
            return StatusCode(500, new ApiResponse<List<SecurityThreat>>
            {
                Success = false,
                Message = "An error occurred while retrieving active threats",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Analyze threat for specific IP
    /// </summary>
    [HttpPost("threats/analyze")]
    public async Task<ActionResult<ApiResponse<SecurityThreat?>>> AnalyzeThreat(
        [FromBody] ThreatAnalysisRequest request)
    {
        try
        {
            var threat = await _securityAnalyticsService.AnalyzeThreatAsync(request.SourceIP, request.UserId);
            return Ok(new ApiResponse<SecurityThreat?>
            {
                Success = true,
                Data = threat,
                Message = threat != null ? "Threat analysis completed" : "No threats detected for this IP"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing threat for IP {IP}", request.SourceIP);
            return StatusCode(500, new ApiResponse<SecurityThreat?>
            {
                Success = false,
                Message = "An error occurred while analyzing threat",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get active security alerts
    /// </summary>
    [HttpGet("alerts")]
    public async Task<ActionResult<ApiResponse<List<SecurityAlert>>>> GetActiveAlerts(
        [FromQuery] int limit = 100)
    {
        try
        {
            var alerts = await _securityAnalyticsService.GetActiveAlertsAsync(limit);
            return Ok(new ApiResponse<List<SecurityAlert>>
            {
                Success = true,
                Data = alerts,
                Message = "Active alerts retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active alerts");
            return StatusCode(500, new ApiResponse<List<SecurityAlert>>
            {
                Success = false,
                Message = "An error occurred while retrieving active alerts",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get security trend data
    /// </summary>
    [HttpGet("trends")]
    public async Task<ActionResult<ApiResponse<SecurityTrendData>>> GetSecurityTrends(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var trends = await _securityAnalyticsService.GetSecurityTrendsAsync(startDate, endDate);
            return Ok(new ApiResponse<SecurityTrendData>
            {
                Success = true,
                Data = trends,
                Message = "Security trends retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security trends");
            return StatusCode(500, new ApiResponse<SecurityTrendData>
            {
                Success = false,
                Message = "An error occurred while retrieving security trends",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get attack patterns analysis
    /// </summary>
    [HttpGet("attack-patterns")]
    public async Task<ActionResult<ApiResponse<List<AttackPatternSummary>>>> GetAttackPatterns(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var patterns = await _securityAnalyticsService.GetAttackPatternsAsync(startDate, endDate);
            return Ok(new ApiResponse<List<AttackPatternSummary>>
            {
                Success = true,
                Data = patterns,
                Message = "Attack patterns retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attack patterns");
            return StatusCode(500, new ApiResponse<List<AttackPatternSummary>>
            {
                Success = false,
                Message = "An error occurred while retrieving attack patterns",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get geographic threat data
    /// </summary>
    [HttpGet("geographic-threats")]
    public async Task<ActionResult<ApiResponse<List<GeographicThreat>>>> GetGeographicThreats(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var threats = await _securityAnalyticsService.GetGeographicThreatsAsync(startDate, endDate);
            return Ok(new ApiResponse<List<GeographicThreat>>
            {
                Success = true,
                Data = threats,
                Message = "Geographic threats retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving geographic threats");
            return StatusCode(500, new ApiResponse<List<GeographicThreat>>
            {
                Success = false,
                Message = "An error occurred while retrieving geographic threats",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Calculate risk score for IP/user combination
    /// </summary>
    [HttpPost("risk-score")]
    public async Task<ActionResult<ApiResponse<RiskScoreResult>>> CalculateRiskScore(
        [FromBody] RiskScoreRequest request)
    {
        try
        {
            var riskScore = await _securityAnalyticsService.CalculateRiskScoreAsync(
                request.SourceIP, 
                request.UserId, 
                request.Factors);

            var result = new RiskScoreResult
            {
                SourceIP = request.SourceIP,
                UserId = request.UserId,
                RiskScore = riskScore,
                RiskLevel = GetRiskLevel(riskScore),
                CalculatedAt = DateTime.UtcNow
            };

            return Ok(new ApiResponse<RiskScoreResult>
            {
                Success = true,
                Data = result,
                Message = "Risk score calculated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating risk score for IP {IP}", request.SourceIP);
            return StatusCode(500, new ApiResponse<RiskScoreResult>
            {
                Success = false,
                Message = "An error occurred while calculating risk score",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Check if IP is high risk
    /// </summary>
    [HttpGet("ip-risk/{ipAddress}")]
    public async Task<ActionResult<ApiResponse<IPRiskStatus>>> CheckIPRisk(string ipAddress)
    {
        try
        {
            var isHighRisk = await _securityAnalyticsService.IsIPHighRiskAsync(ipAddress);
            var riskScore = await _securityAnalyticsService.CalculateRiskScoreAsync(ipAddress);

            var status = new IPRiskStatus
            {
                IPAddress = ipAddress,
                IsHighRisk = isHighRisk,
                RiskScore = riskScore,
                RiskLevel = GetRiskLevel(riskScore),
                CheckedAt = DateTime.UtcNow
            };

            return Ok(new ApiResponse<IPRiskStatus>
            {
                Success = true,
                Data = status,
                Message = "IP risk status checked successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking IP risk for {IP}", ipAddress);
            return StatusCode(500, new ApiResponse<IPRiskStatus>
            {
                Success = false,
                Message = "An error occurred while checking IP risk",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    // Helper methods and DTOs

    private static string GetRiskLevel(decimal riskScore)
    {
        return riskScore switch
        {
            >= 90 => "Critical",
            >= 70 => "High",
            >= 50 => "Medium",
            _ => "Low"
        };
    }
}

/// <summary>
/// Request model for threat analysis
/// </summary>
public class ThreatAnalysisRequest
{
    public string SourceIP { get; set; } = string.Empty;
    public string? UserId { get; set; }
}

/// <summary>
/// Request model for risk score calculation
/// </summary>
public class RiskScoreRequest
{
    public string SourceIP { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public Dictionary<string, object>? Factors { get; set; }
}

/// <summary>
/// Result model for risk score calculation
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
/// Status model for IP risk check
/// </summary>
public class IPRiskStatus
{
    public string IPAddress { get; set; } = string.Empty;
    public bool IsHighRisk { get; set; }
    public decimal RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
}