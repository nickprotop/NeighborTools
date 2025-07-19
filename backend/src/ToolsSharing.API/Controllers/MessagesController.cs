using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Core.Features.Messaging;
using ToolsSharing.Core.DTOs.Messaging;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessagesController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    /// <summary>
    /// Get messages for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMessages([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? isRead = null, [FromQuery] bool? isArchived = null, [FromQuery] MessageType? type = null, [FromQuery] string? searchTerm = null, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var query = new GetMessagesQuery(userId, page, pageSize, isRead, isArchived, type, searchTerm, fromDate, toDate);
        var result = await _messageService.GetMessagesAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific message by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetMessage(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var query = new GetMessageByIdQuery(id, userId);
        var result = await _messageService.GetMessageByIdAsync(query);
        
        if (!result.Success)
            return NotFound(result);
            
        return Ok(result);
    }

    /// <summary>
    /// Send a new message
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new SendMessageCommand(
            userId,
            request.RecipientId,
            request.Subject,
            request.Content,
            request.ConversationId,
            request.RentalId,
            request.ToolId,
            request.Priority,
            request.Type,
            request.Attachments?.ToList()
        );

        var result = await _messageService.SendMessageAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return CreatedAtAction(nameof(GetMessage), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// Reply to a message
    /// </summary>
    [HttpPost("{messageId}/reply")]
    public async Task<IActionResult> ReplyToMessage(Guid messageId, [FromBody] ReplyMessageRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new ReplyToMessageCommand(userId, messageId, request.Content, request.Attachments?.ToList());
        var result = await _messageService.ReplyToMessageAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return CreatedAtAction(nameof(GetMessage), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// Mark a message as read
    /// </summary>
    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new MarkMessageAsReadCommand(userId, id);
        var result = await _messageService.MarkMessageAsReadAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    /// <summary>
    /// Archive a message
    /// </summary>
    [HttpPatch("{id}/archive")]
    public async Task<IActionResult> ArchiveMessage(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new ArchiveMessageCommand(userId, id);
        var result = await _messageService.ArchiveMessageAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    /// <summary>
    /// Delete a message (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMessage(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new DeleteMessageCommand(userId, id);
        var result = await _messageService.DeleteMessageAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    /// <summary>
    /// Search messages
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchMessages([FromQuery] SearchMessagesQuery query)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var queryWithUser = query with { UserId = userId };
        var result = await _messageService.SearchMessagesAsync(queryWithUser);
        return Ok(result);
    }

    /// <summary>
    /// Get unread message count
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var query = new GetUnreadMessageCountQuery(userId);
        var result = await _messageService.GetUnreadMessageCountAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Get message statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] GetMessageStatisticsQuery query)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var queryWithUser = query with { UserId = userId };
        var result = await _messageService.GetMessageStatisticsAsync(queryWithUser);
        return Ok(result);
    }

    /// <summary>
    /// Download message attachment
    /// </summary>
    [HttpGet("attachments/{attachmentId}")]
    public async Task<IActionResult> DownloadAttachment(Guid attachmentId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var query = new GetMessageAttachmentQuery(attachmentId, userId);
        var result = await _messageService.GetMessageAttachmentAsync(query);
        
        if (!result.Success)
            return NotFound(result);

        return File(result.Data!, "application/octet-stream");
    }

    /// <summary>
    /// Validate message content (for client-side validation)
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateContent([FromBody] ValidateContentRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _messageService.ValidateMessageContentAsync(request.Content, userId);
        return Ok(result);
    }
}

/// <summary>
/// Request models for controller actions
/// </summary>
public class ReplyMessageRequest
{
    public string Content { get; set; } = string.Empty;
    public List<MessageAttachmentUpload>? Attachments { get; set; }
}

public class ValidateContentRequest
{
    public string Content { get; set; } = string.Empty;
}