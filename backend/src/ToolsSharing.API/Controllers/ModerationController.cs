using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Core.Features.Messaging;

namespace ToolsSharing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ModerationController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly IContentModerationService _contentModerationService;

    public ModerationController(IMessageService messageService, IContentModerationService contentModerationService)
    {
        _messageService = messageService;
        _contentModerationService = contentModerationService;
    }

    /// <summary>
    /// Get all moderated messages for admin review
    /// </summary>
    [HttpGet("messages")]
    public async Task<IActionResult> GetModeratedMessages([FromQuery] GetModeratedMessagesQuery query)
    {
        var result = await _messageService.GetModeratedMessagesAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Moderate a specific message
    /// </summary>
    [HttpPost("messages/{messageId}/moderate")]
    public async Task<IActionResult> ModerateMessage(Guid messageId, [FromBody] ModerateMessageRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new ModerateMessageCommand(userId, messageId, request.Reason, request.ModifiedContent);
        var result = await _messageService.ModerateMessageAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    /// <summary>
    /// Report a message for review
    /// </summary>
    [HttpPost("messages/{messageId}/report")]
    [Authorize] // Allow any authenticated user to report
    public async Task<IActionResult> ReportMessage(Guid messageId, [FromBody] ReportMessageRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var success = await _contentModerationService.ReportMessageAsync(messageId, userId, request.Reason);
        
        if (!success)
            return BadRequest(new { Success = false, Message = "Failed to report message" });
            
        return Ok(new { Success = true, Message = "Message reported successfully" });
    }

    /// <summary>
    /// Get moderation statistics for admin dashboard
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetModerationStatistics()
    {
        var statistics = await _contentModerationService.GetModerationStatisticsAsync();
        return Ok(new { Success = true, Data = statistics, Message = "Statistics retrieved successfully" });
    }

    /// <summary>
    /// Validate content with detailed moderation analysis
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateContent([FromBody] ValidateContentRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _contentModerationService.ValidateContentAsync(request.Content, userId);
        return Ok(new { Success = true, Data = result, Message = "Content validated" });
    }
}

/// <summary>
/// Request models for moderation actions
/// </summary>
public class ModerateMessageRequest
{
    public string Reason { get; set; } = string.Empty;
    public string? ModifiedContent { get; set; }
}

public class ReportMessageRequest
{
    public string Reason { get; set; } = string.Empty;
}

