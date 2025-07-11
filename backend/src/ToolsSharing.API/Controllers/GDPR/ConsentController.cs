using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Entities.GDPR;
using ToolsSharing.Core.Interfaces.GDPR;
using ToolsSharing.Infrastructure.Data;
using ToolsSharing.Core.Common.Constants;

namespace ToolsSharing.API.Controllers.GDPR;

[ApiController]
[Route("api/gdpr/consent")]
public class ConsentController : ControllerBase
{
    private readonly IConsentService _consentService;
    private readonly IDataProcessingLogger _dataLogger;
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ConsentController> _logger;

    public ConsentController(
        IConsentService consentService,
        IDataProcessingLogger dataLogger,
        UserManager<User> userManager,
        ApplicationDbContext context,
        ILogger<ConsentController> logger)
    {
        _consentService = consentService;
        _dataLogger = dataLogger;
        _userManager = userManager;
        _context = context;
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
    public async Task<IActionResult> GetUserConsents(string userId)
    {
        try
        {
            // Get user to access basic consents stored on User entity
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Error = "User not found" });
            }

            var response = new List<ConsentResponse>();

            // Add Terms of Service acceptance (using 100 to avoid conflicts with enum)
            response.Add(new ConsentResponse
            {
                ConsentType = 100, // Terms of Service (special type, not in GDPR ConsentType enum)
                ConsentGiven = user.TermsOfServiceAccepted,
                ConsentDate = user.TermsAcceptedDate ?? user.CreatedAt,
                ConsentVersion = user.TermsVersion ?? VersionConstants.GetCurrentTermsVersion(),
                WithdrawnDate = null
            });

            // Add basic consents from User entity (these are the main ones users see)
            response.Add(new ConsentResponse
            {
                ConsentType = (int)ConsentType.DataProcessing, // Data Processing = 3
                ConsentGiven = user.DataProcessingConsent,
                ConsentDate = user.LastConsentUpdate ?? user.CreatedAt,
                ConsentVersion = VersionConstants.GetCurrentConsentVersion(),
                WithdrawnDate = null
            });

            response.Add(new ConsentResponse
            {
                ConsentType = (int)ConsentType.Marketing, // Marketing = 1
                ConsentGiven = user.MarketingConsent,
                ConsentDate = user.LastConsentUpdate ?? user.CreatedAt,
                ConsentVersion = VersionConstants.GetCurrentConsentVersion(),
                WithdrawnDate = null
            });

            // Get additional formal consents from UserConsents table if any exist
            var formalConsents = await _context.UserConsents
                .Where(c => c.UserId == userId)
                .GroupBy(c => c.ConsentType)
                .Select(g => g.OrderByDescending(c => c.ConsentDate).First())
                .ToListAsync();

            // Add formal consents (excluding duplicates of basic consents)
            foreach (var consent in formalConsents)
            {
                // Skip if this is already covered by basic consents
                if (consent.ConsentType == ConsentType.DataProcessing || 
                    consent.ConsentType == ConsentType.Marketing)
                    continue;

                response.Add(new ConsentResponse
                {
                    ConsentType = (int)consent.ConsentType,
                    ConsentGiven = consent.ConsentGiven,
                    ConsentDate = consent.ConsentDate,
                    ConsentVersion = consent.ConsentVersion,
                    WithdrawnDate = consent.WithdrawnDate
                });
            }

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
    public string UserId { get; set; } = "";
    public ConsentType ConsentType { get; set; }
    public bool ConsentGiven { get; set; }
    public string Source { get; set; } = string.Empty;
}

public class ConsentResponse
{
    public int ConsentType { get; set; }
    public bool ConsentGiven { get; set; }
    public DateTime ConsentDate { get; set; }
    public string ConsentVersion { get; set; } = string.Empty;
    public DateTime? WithdrawnDate { get; set; }
}

public class WithdrawConsentRequest
{
    public string UserId { get; set; } = "";
    public ConsentType ConsentType { get; set; }
    public string Reason { get; set; } = string.Empty;
}