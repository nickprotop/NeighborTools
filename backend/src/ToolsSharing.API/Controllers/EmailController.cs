using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Common.Models.EmailNotifications;
using ToolsSharing.Infrastructure.Common;
using EmailStatistics = ToolsSharing.Core.Common.Interfaces.EmailStatistics;
using QueueStatus = ToolsSharing.Core.Common.Interfaces.QueueStatus;

namespace ToolsSharing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly ILogger<EmailController> _logger;

    public EmailController(
        IEmailNotificationService emailNotificationService,
        ILogger<EmailController> logger)
    {
        _emailNotificationService = emailNotificationService;
        _logger = logger;
    }

    /// <summary>
    /// Get email notification preferences for the current user
    /// </summary>
    [HttpGet("preferences")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<Dictionary<string, bool>>>> GetEmailPreferences()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<Dictionary<string, bool>>.Failure("User not authenticated"));
            }

            var preferences = await _emailNotificationService.GetUserNotificationPreferencesAsync(userId);
            return Ok(ApiResponse<Dictionary<string, bool>>.Success(preferences));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email preferences");
            return StatusCode(500, ApiResponse<Dictionary<string, bool>>.Failure("An error occurred while getting email preferences"));
        }
    }

    /// <summary>
    /// Update email notification preferences for the current user
    /// </summary>
    [HttpPut("preferences")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateEmailPreferences([FromBody] Dictionary<string, bool> preferences)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<bool>.Failure("User not authenticated"));
            }

            await _emailNotificationService.UpdateUserNotificationPreferencesAsync(userId, preferences);
            return Ok(ApiResponse<bool>.Success(true, "Email preferences updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email preferences");
            return StatusCode(500, ApiResponse<bool>.Failure("An error occurred while updating email preferences"));
        }
    }

    /// <summary>
    /// Unsubscribe from all email notifications
    /// </summary>
    [HttpPost("unsubscribe")]
    public async Task<ActionResult<ApiResponse<bool>>> Unsubscribe([FromQuery] string email, [FromQuery] string token)
    {
        try
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                return BadRequest(ApiResponse<bool>.Failure("Email and token are required"));
            }

            var result = await _emailNotificationService.UnsubscribeUserAsync(email, token);
            if (result)
            {
                return Ok(ApiResponse<bool>.Success(true, "Successfully unsubscribed from email notifications"));
            }

            return BadRequest(ApiResponse<bool>.Failure("Invalid unsubscribe link"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing user");
            return StatusCode(500, ApiResponse<bool>.Failure("An error occurred while processing your request"));
        }
    }

    /// <summary>
    /// Get email statistics for the current user
    /// </summary>
    [HttpGet("stats")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<EmailStatistics>>> GetEmailStatistics()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<EmailStatistics>.Failure("User not authenticated"));
            }

            var stats = await _emailNotificationService.GetEmailStatisticsAsync(userId);
            return Ok(ApiResponse<EmailStatistics>.Success(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email statistics");
            return StatusCode(500, ApiResponse<EmailStatistics>.Failure("An error occurred while getting email statistics"));
        }
    }

    /// <summary>
    /// Preview an email template (admin only)
    /// </summary>
    [HttpPost("preview")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<EmailPreview>>> PreviewEmail([FromBody] EmailPreviewRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.TemplateName))
            {
                return BadRequest(ApiResponse<EmailPreview>.Failure("Template name is required"));
            }

            // Create sample data based on template
            var sampleData = GetSampleDataForTemplate(request.TemplateName);
            
            var htmlContent = await _emailNotificationService.PreviewEmailTemplateAsync(
                request.TemplateName, 
                sampleData);
                
            var plainTextContent = await _emailNotificationService.PreviewEmailTemplateAsync(
                $"PlainText/{request.TemplateName}.txt", 
                sampleData);

            var preview = new EmailPreview
            {
                Subject = GetSubjectForTemplate(request.TemplateName, sampleData),
                HtmlContent = htmlContent,
                PlainTextContent = plainTextContent,
                TemplateName = request.TemplateName
            };

            return Ok(ApiResponse<EmailPreview>.Success(preview));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing email template");
            return StatusCode(500, ApiResponse<EmailPreview>.Failure("An error occurred while previewing the email template"));
        }
    }

    /// <summary>
    /// Send a test email (admin only)
    /// </summary>
    [HttpPost("test")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> SendTestEmail([FromBody] TestEmailRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.RecipientEmail))
            {
                return BadRequest(ApiResponse<bool>.Failure("Recipient email is required"));
            }

            // Create a test notification
            var notification = new TestEmailNotification
            {
                RecipientEmail = request.RecipientEmail,
                RecipientName = request.RecipientName ?? "Test User",
                Subject = request.Subject ?? "Test Email from NeighborTools",
                Message = request.Message ?? "This is a test email to verify email configuration.",
                Priority = EmailPriority.Low
            };

            await _emailNotificationService.SendNotificationAsync(notification);
            return Ok(ApiResponse<bool>.Success(true, "Test email sent successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test email");
            return StatusCode(500, ApiResponse<bool>.Failure($"Failed to send test email: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get email queue status (admin only)
    /// </summary>
    [HttpGet("queue/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<QueueStatus>>> GetQueueStatus()
    {
        try
        {
            var status = await _emailNotificationService.GetQueueStatusAsync();
            return Ok(ApiResponse<QueueStatus>.Success(status));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue status");
            return StatusCode(500, ApiResponse<QueueStatus>.Failure("An error occurred while getting queue status"));
        }
    }

    /// <summary>
    /// Process email queue manually (admin only)
    /// </summary>
    [HttpPost("queue/process")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<int>>> ProcessQueue()
    {
        try
        {
            var processedCount = await _emailNotificationService.ProcessQueueManuallyAsync();
            return Ok(ApiResponse<int>.Success(processedCount, $"Processed {processedCount} emails"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email queue");
            return StatusCode(500, ApiResponse<int>.Failure("An error occurred while processing the email queue"));
        }
    }

    private dynamic GetSampleDataForTemplate(string templateName)
    {
        var baseData = new
        {
            BaseUrl = "http://localhost:5000",
            RecipientEmail = "user@example.com",
            Year = DateTime.Now.Year
        };

        return templateName.ToLower() switch
        {
            "welcome" => new
            {
                UserName = "John Doe",
                BaseUrl = baseData.BaseUrl,
                RecipientEmail = baseData.RecipientEmail,
                Year = baseData.Year
            },
            "passwordreset" => new
            {
                UserName = "John Doe",
                ResetUrl = $"{baseData.BaseUrl}/reset-password?token=sample-token",
                ExpiresInHours = 24,
                BaseUrl = baseData.BaseUrl,
                RecipientEmail = baseData.RecipientEmail,
                Year = baseData.Year
            },
            "emailverification" => new
            {
                UserName = "John Doe",
                VerificationUrl = $"{baseData.BaseUrl}/verify-email?token=sample-token",
                BaseUrl = baseData.BaseUrl,
                RecipientEmail = baseData.RecipientEmail,
                Year = baseData.Year
            },
            "rentalrequest" => new
            {
                OwnerName = "Jane Smith",
                RenterName = "John Doe",
                ToolName = "DeWalt Circular Saw",
                ToolImageUrl = $"{baseData.BaseUrl}/images/sample-tool.jpg",
                StartDate = DateTime.Now.AddDays(2),
                EndDate = DateTime.Now.AddDays(4),
                Duration = 3,
                TotalCost = 45.00m,
                Message = "Hi! I need this for a weekend project. I'll take great care of it!",
                ApprovalUrl = $"{baseData.BaseUrl}/rentals/approve/123",
                RentalDetailsUrl = $"{baseData.BaseUrl}/rentals/123",
                BaseUrl = baseData.BaseUrl,
                RecipientEmail = baseData.RecipientEmail,
                Year = baseData.Year
            },
            _ => baseData
        };
    }

    private string GetSubjectForTemplate(string templateName, dynamic data)
    {
        return templateName.ToLower() switch
        {
            "welcome" => $"Welcome to NeighborTools, {data.UserName}!",
            "passwordreset" => "Reset Your NeighborTools Password",
            "emailverification" => "Verify Your Email Address",
            "rentalrequest" => $"New Rental Request for {data.ToolName}",
            "rentalapproved" => "Your Rental Request Has Been Approved!",
            "rentalrejected" => "Rental Request Update",
            "rentalreminder" => $"Rental Reminder: {data.ToolName}",
            "rentalcompleted" => "Rental Completed Successfully!",
            _ => "NeighborTools Notification"
        };
    }
}

// Request/Response models
public class EmailPreviewRequest
{
    public string TemplateName { get; set; } = "";
}

public class EmailPreview
{
    public string Subject { get; set; } = "";
    public string HtmlContent { get; set; } = "";
    public string PlainTextContent { get; set; } = "";
    public string TemplateName { get; set; } = "";
}

public class TestEmailRequest
{
    public string RecipientEmail { get; set; } = "";
    public string? RecipientName { get; set; }
    public string? Subject { get; set; }
    public string? Message { get; set; }
}


// Test email notification class
public class TestEmailNotification : EmailNotification
{
    public string Subject { get; set; } = "";
    public string Message { get; set; } = "";

    public TestEmailNotification()
    {
        Type = EmailNotificationType.Other;
    }

    public override string GetSubject() => Subject;
    public override string GetTemplateName() => "Test";
    public override object GetTemplateData() => new { Message, RecipientName, Year = DateTime.Now.Year };
}