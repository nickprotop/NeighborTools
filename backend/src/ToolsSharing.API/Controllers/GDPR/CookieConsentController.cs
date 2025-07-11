using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToolsSharing.Core.Entities.GDPR;
using ToolsSharing.Infrastructure.Data;
using ToolsSharing.Core.Interfaces.GDPR;

namespace ToolsSharing.API.Controllers.GDPR;

[ApiController]
[Route("api/gdpr/cookies")]
public class CookieConsentController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDataProcessingLogger _dataLogger;
    private readonly ILogger<CookieConsentController> _logger;

    public CookieConsentController(
        ApplicationDbContext context,
        IDataProcessingLogger dataLogger,
        ILogger<CookieConsentController> logger)
    {
        _context = context;
        _dataLogger = dataLogger;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> RecordCookieConsent([FromBody] CookieConsentRequest request)
    {
        try
        {
            // Validate required fields for GDPR compliance
            if (string.IsNullOrEmpty(request.SessionId))
            {
                return BadRequest(new { Error = "SessionId is required for GDPR compliance" });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var userAgent = Request.Headers["User-Agent"].ToString();

            // For GDPR compliance, we need to handle consent withdrawal properly
            // If this is a withdrawal (ConsentGiven = false), we need to record it as a new entry
            // rather than updating existing consent
            var existingConsent = await _context.CookieConsents
                .Where(c => c.SessionId == request.SessionId && 
                           c.CookieCategory == request.CookieCategory &&
                           c.ExpiryDate > DateTime.UtcNow)
                .OrderByDescending(c => c.ConsentDate)
                .FirstOrDefaultAsync();

            var cookieConsent = new CookieConsent
            {
                SessionId = request.SessionId,
                UserId = request.UserId,
                CookieCategory = request.CookieCategory,
                ConsentGiven = request.ConsentGiven,
                ConsentDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddYears(1), // 1 year expiry as per GDPR recommendations
                IPAddress = ipAddress,
                UserAgent = userAgent
            };

            // Always add new consent record for audit trail (GDPR requirement)
            _context.CookieConsents.Add(cookieConsent);
            await _context.SaveChangesAsync();

            // Log cookie consent activity for GDPR data processing log
            var activityType = request.ConsentGiven ? "cookie_consent_granted" : "cookie_consent_withdrawn";
            var processingPurpose = request.ConsentGiven 
                ? $"Record cookie consent granted for category: {request.CookieCategory}"
                : $"Record cookie consent withdrawal for category: {request.CookieCategory}";

            await _dataLogger.LogDataProcessingAsync(new DataProcessingActivity
            {
                UserId = request.UserId, // Can be null for anonymous users
                ActivityType = activityType,
                DataCategories = new[] { "consent_data", "session_data", "technical_data" }.ToList(),
                ProcessingPurpose = processingPurpose,
                LegalBasis = LegalBasis.Consent,
                DataSources = new[] { "cookie_banner", "user_interaction" }.ToList(),
                DataRecipients = new[] { "internal_analytics", "service_providers" }.ToList(),
                RetentionPeriod = "1 year from consent date",
                IPAddress = ipAddress,
                UserAgent = userAgent
            });

            return Ok(new { 
                Success = true, 
                ConsentId = cookieConsent.Id,
                ConsentDate = cookieConsent.ConsentDate,
                ExpiryDate = cookieConsent.ExpiryDate,
                ConsentGiven = cookieConsent.ConsentGiven
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record cookie consent");
            return StatusCode(500, new { Error = "Failed to record cookie consent" });
        }
    }

    [HttpGet("session/{sessionId}")]
    public async Task<IActionResult> GetSessionConsents(string sessionId)
    {
        try
        {
            // Get the latest consent for each category for GDPR compliance
            var consents = await _context.CookieConsents
                .Where(c => c.SessionId == sessionId && c.ExpiryDate > DateTime.UtcNow)
                .GroupBy(c => c.CookieCategory)
                .Select(g => g.OrderByDescending(c => c.ConsentDate).First())
                .ToListAsync();

            var response = consents.Select(c => new CookieConsentResponse
            {
                CookieCategory = c.CookieCategory,
                ConsentGiven = c.ConsentGiven,
                ConsentDate = c.ConsentDate,
                ExpiryDate = c.ExpiryDate
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get session consents for session {SessionId}", sessionId);
            return StatusCode(500, new { Error = "Failed to retrieve session consents" });
        }
    }

    [HttpGet("session/{sessionId}/history")]
    public async Task<IActionResult> GetSessionConsentHistory(string sessionId)
    {
        try
        {
            // Get full consent history for audit purposes (GDPR requirement)
            var consents = await _context.CookieConsents
                .Where(c => c.SessionId == sessionId)
                .OrderByDescending(c => c.ConsentDate)
                .ToListAsync();

            var response = consents.Select(c => new CookieConsentHistoryResponse
            {
                Id = c.Id,
                CookieCategory = c.CookieCategory,
                ConsentGiven = c.ConsentGiven,
                ConsentDate = c.ConsentDate,
                ExpiryDate = c.ExpiryDate,
                IPAddress = c.IPAddress,
                UserAgent = c.UserAgent
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get session consent history for session {SessionId}", sessionId);
            return StatusCode(500, new { Error = "Failed to retrieve session consent history" });
        }
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserCookieConsents(string userId)
    {
        try
        {
            // Get the latest consent for each category for authenticated users
            var consents = await _context.CookieConsents
                .Where(c => c.UserId == userId && c.ExpiryDate > DateTime.UtcNow)
                .GroupBy(c => c.CookieCategory)
                .Select(g => g.OrderByDescending(c => c.ConsentDate).First())
                .ToListAsync();

            var response = consents.Select(c => new CookieConsentResponse
            {
                CookieCategory = c.CookieCategory,
                ConsentGiven = c.ConsentGiven,
                ConsentDate = c.ConsentDate,
                ExpiryDate = c.ExpiryDate
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cookie consents for user {UserId}", userId);
            return StatusCode(500, new { Error = "Failed to retrieve cookie consents" });
        }
    }

    [HttpGet("user/{userId}/history")]
    public async Task<IActionResult> GetUserConsentHistory(string userId)
    {
        try
        {
            // Get full consent history for authenticated users (GDPR right to access)
            var consents = await _context.CookieConsents
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.ConsentDate)
                .ToListAsync();

            var response = consents.Select(c => new CookieConsentHistoryResponse
            {
                Id = c.Id,
                CookieCategory = c.CookieCategory,
                ConsentGiven = c.ConsentGiven,
                ConsentDate = c.ConsentDate,
                ExpiryDate = c.ExpiryDate,
                IPAddress = c.IPAddress,
                UserAgent = c.UserAgent
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user consent history for user {UserId}", userId);
            return StatusCode(500, new { Error = "Failed to retrieve user consent history" });
        }
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateConsent([FromBody] ConsentValidationRequest request)
    {
        try
        {
            var consent = await _context.CookieConsents
                .Where(c => c.SessionId == request.SessionId && 
                           c.CookieCategory == request.CookieCategory &&
                           c.ExpiryDate > DateTime.UtcNow)
                .OrderByDescending(c => c.ConsentDate)
                .FirstOrDefaultAsync();

            var isValid = consent != null && consent.ConsentGiven;
            
            return Ok(new ConsentValidationResponse
            {
                IsValid = isValid,
                ConsentGiven = consent?.ConsentGiven ?? false,
                ConsentDate = consent?.ConsentDate,
                ExpiryDate = consent?.ExpiryDate,
                RequiresRefresh = consent?.ExpiryDate <= DateTime.UtcNow.AddDays(30) // Warn 30 days before expiry
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate consent");
            return StatusCode(500, new { Error = "Failed to validate consent" });
        }
    }

    [HttpGet("audit/{sessionId}")]
    public async Task<IActionResult> GenerateConsentAuditReport(string sessionId)
    {
        try
        {
            // Generate comprehensive audit report for GDPR compliance
            var consents = await _context.CookieConsents
                .Where(c => c.SessionId == sessionId)
                .OrderByDescending(c => c.ConsentDate)
                .ToListAsync();

            var auditReport = new ConsentAuditReport
            {
                SessionId = sessionId,
                GeneratedAt = DateTime.UtcNow,
                TotalConsentRecords = consents.Count,
                ConsentsByCategory = consents
                    .GroupBy(c => c.CookieCategory)
                    .ToDictionary(g => g.Key.ToString(), g => g.ToList().Select(c => new ConsentAuditEntry
                    {
                        ConsentGiven = c.ConsentGiven,
                        ConsentDate = c.ConsentDate,
                        IPAddress = c.IPAddress,
                        UserAgent = c.UserAgent,
                        ExpiryDate = c.ExpiryDate
                    }).ToList()),
                ActiveConsents = consents
                    .Where(c => c.ExpiryDate > DateTime.UtcNow)
                    .GroupBy(c => c.CookieCategory)
                    .Select(g => g.OrderByDescending(c => c.ConsentDate).First())
                    .ToList()
                    .Select(c => new ConsentAuditEntry
                    {
                        ConsentGiven = c.ConsentGiven,
                        ConsentDate = c.ConsentDate,
                        IPAddress = c.IPAddress,
                        UserAgent = c.UserAgent,
                        ExpiryDate = c.ExpiryDate
                    }).ToList()
            };

            return Ok(auditReport);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate consent audit report for session {SessionId}", sessionId);
            return StatusCode(500, new { Error = "Failed to generate consent audit report" });
        }
    }
}

// DTOs
public class CookieConsentRequest
{
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    public CookieCategory CookieCategory { get; set; }
    public bool ConsentGiven { get; set; }
}

public class CookieConsentResponse
{
    public CookieCategory CookieCategory { get; set; }
    public bool ConsentGiven { get; set; }
    public DateTime ConsentDate { get; set; }
    public DateTime ExpiryDate { get; set; }
}

public class CookieConsentHistoryResponse
{
    public Guid Id { get; set; }
    public CookieCategory CookieCategory { get; set; }
    public bool ConsentGiven { get; set; }
    public DateTime ConsentDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string IPAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
}

public class ConsentValidationRequest
{
    public string SessionId { get; set; } = string.Empty;
    public CookieCategory CookieCategory { get; set; }
}

public class ConsentValidationResponse
{
    public bool IsValid { get; set; }
    public bool ConsentGiven { get; set; }
    public DateTime? ConsentDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool RequiresRefresh { get; set; }
}

public class ConsentAuditReport
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public int TotalConsentRecords { get; set; }
    public Dictionary<string, List<ConsentAuditEntry>> ConsentsByCategory { get; set; } = new();
    public List<ConsentAuditEntry> ActiveConsents { get; set; } = new();
}

public class ConsentAuditEntry
{
    public bool ConsentGiven { get; set; }
    public DateTime ConsentDate { get; set; }
    public string IPAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime ExpiryDate { get; set; }
}