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
            var sessionId = HttpContext.Session.Id;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var userAgent = Request.Headers["User-Agent"].ToString();

            var cookieConsent = new CookieConsent
            {
                SessionId = sessionId,
                UserId = request.UserId,
                CookieCategory = request.CookieCategory,
                ConsentGiven = request.ConsentGiven,
                ConsentDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddYears(1), // 1 year expiry
                IPAddress = ipAddress,
                UserAgent = userAgent
            };

            _context.CookieConsents.Add(cookieConsent);
            await _context.SaveChangesAsync();

            // Log cookie consent activity
            if (request.UserId.HasValue)
            {
                await _dataLogger.LogDataProcessingAsync(new DataProcessingActivity
                {
                    UserId = request.UserId.Value,
                    ActivityType = "cookie_consent",
                    DataCategories = new[] { "consent_data", "session_data" }.ToList(),
                    ProcessingPurpose = $"Record cookie consent for category: {request.CookieCategory}",
                    LegalBasis = LegalBasis.Consent,
                    DataSources = new[] { "cookie_banner" }.ToList(),
                    RetentionPeriod = "1 year",
                    IPAddress = ipAddress,
                    UserAgent = userAgent
                });
            }

            return Ok(new { Success = true, ConsentId = cookieConsent.Id });
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
            var consents = await _context.CookieConsents
                .Where(c => c.SessionId == sessionId && c.ExpiryDate > DateTime.UtcNow)
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

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserCookieConsents(int userId)
    {
        try
        {
            var consents = await _context.CookieConsents
                .Where(c => c.UserId == userId && c.ExpiryDate > DateTime.UtcNow)
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
}

// DTOs
public class CookieConsentRequest
{
    public int? UserId { get; set; }
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