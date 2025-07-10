using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToolsSharing.Core.Entities.GDPR;
using ToolsSharing.Core.Interfaces.GDPR;

namespace ToolsSharing.API.Controllers.GDPR;

[ApiController]
[Route("api/gdpr/consent")]
public class ConsentController : ControllerBase
{
    private readonly IConsentService _consentService;
    private readonly IDataProcessingLogger _dataLogger;
    private readonly ILogger<ConsentController> _logger;

    public ConsentController(
        IConsentService consentService,
        IDataProcessingLogger dataLogger,
        ILogger<ConsentController> logger)
    {
        _consentService = consentService;
        _dataLogger = dataLogger;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> RecordConsent([FromBody] ConsentRequest request)
    {
        try
        {
            var consent = new UserConsent
            {
                UserId = request.UserId,
                ConsentType = request.ConsentType,
                ConsentGiven = request.ConsentGiven,
                ConsentDate = DateTime.UtcNow,
                ConsentSource = request.Source,
                ConsentVersion = await _consentService.GetCurrentPrivacyVersionAsync(),
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                UserAgent = Request.Headers["User-Agent"].ToString()
            };

            await _consentService.RecordConsentAsync(consent);

            // Log data processing activity
            await _dataLogger.LogDataProcessingAsync(new DataProcessingActivity
            {
                UserId = request.UserId,
                ActivityType = "consent_update",
                DataCategories = new[] { "consent_data" }.ToList(),
                ProcessingPurpose = "Record user consent preferences for GDPR compliance",
                LegalBasis = LegalBasis.LegalObligation,
                DataSources = new[] { request.Source }.ToList(),
                RetentionPeriod = "7 years",
                IPAddress = consent.IPAddress,
                UserAgent = consent.UserAgent
            });

            return Ok(new { Success = true, ConsentId = consent.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record consent for user {UserId}", request.UserId);
            return StatusCode(500, new { Error = "Failed to record consent" });
        }
    }

    [HttpGet("{userId}")]
    [Authorize]
    public async Task<IActionResult> GetUserConsents(int userId)
    {
        try
        {
            var consents = await _consentService.GetUserConsentsAsync(userId);
            var response = consents.Select(c => new ConsentResponse
            {
                ConsentType = c.ConsentType,
                ConsentGiven = c.ConsentGiven,
                ConsentDate = c.ConsentDate,
                ConsentVersion = c.ConsentVersion,
                WithdrawnDate = c.WithdrawnDate
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get consents for user {UserId}", userId);
            return StatusCode(500, new { Error = "Failed to retrieve consents" });
        }
    }

    [HttpPost("withdraw")]
    [Authorize]
    public async Task<IActionResult> WithdrawConsent([FromBody] WithdrawConsentRequest request)
    {
        try
        {
            await _consentService.WithdrawConsentAsync(
                request.UserId,
                request.ConsentType,
                request.Reason);

            // Log withdrawal
            await _dataLogger.LogDataProcessingAsync(new DataProcessingActivity
            {
                UserId = request.UserId,
                ActivityType = "consent_withdrawal",
                DataCategories = new[] { "consent_data" }.ToList(),
                ProcessingPurpose = "Record user consent withdrawal for GDPR compliance",
                LegalBasis = LegalBasis.LegalObligation,
                DataSources = new[] { "user_request" }.ToList(),
                RetentionPeriod = "7 years",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
            });

            return Ok(new { Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to withdraw consent for user {UserId}", request.UserId);
            return StatusCode(500, new { Error = "Failed to withdraw consent" });
        }
    }
}

// DTOs
public class ConsentRequest
{
    public int UserId { get; set; }
    public ConsentType ConsentType { get; set; }
    public bool ConsentGiven { get; set; }
    public string Source { get; set; } = string.Empty;
}

public class ConsentResponse
{
    public ConsentType ConsentType { get; set; }
    public bool ConsentGiven { get; set; }
    public DateTime ConsentDate { get; set; }
    public string ConsentVersion { get; set; } = string.Empty;
    public DateTime? WithdrawnDate { get; set; }
}

public class WithdrawConsentRequest
{
    public int UserId { get; set; }
    public ConsentType ConsentType { get; set; }
    public string Reason { get; set; } = string.Empty;
}