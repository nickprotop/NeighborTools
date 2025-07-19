using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Core.Features.Messaging;

namespace ToolsSharing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IMessageService _messageService;

    public ConversationsController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    /// <summary>
    /// Get conversations for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetConversations([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? isArchived = null, [FromQuery] string? searchTerm = null)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var query = new GetConversationsQuery(userId, page, pageSize, isArchived, searchTerm);
        var result = await _messageService.GetConversationsAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific conversation with messages
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetConversation(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var query = new GetConversationByIdQuery(id, userId, page, pageSize);
        var result = await _messageService.GetConversationByIdAsync(query);
        
        if (!result.Success)
            return NotFound(result);
            
        return Ok(result);
    }

    /// <summary>
    /// Create a new conversation
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new CreateConversationCommand(
            userId,
            request.ParticipantId,
            request.Title,
            request.RentalId,
            request.ToolId
        );

        var result = await _messageService.CreateConversationAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return CreatedAtAction(nameof(GetConversation), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// Update conversation details
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateConversation(Guid id, [FromBody] UpdateConversationRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new UpdateConversationCommand(userId, id, request.Title, request.IsArchived);
        var result = await _messageService.UpdateConversationAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    /// <summary>
    /// Mark all messages in a conversation as read
    /// </summary>
    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkConversationAsRead(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new MarkConversationAsReadCommand(userId, id);
        var result = await _messageService.MarkConversationAsReadAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    /// <summary>
    /// Archive a conversation
    /// </summary>
    [HttpPatch("{id}/archive")]
    public async Task<IActionResult> ArchiveConversation(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new ArchiveConversationCommand(userId, id);
        var result = await _messageService.ArchiveConversationAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    /// <summary>
    /// Get conversation between two specific users
    /// </summary>
    [HttpGet("between/{userId}")]
    public async Task<IActionResult> GetConversationBetweenUsers(string userId, [FromQuery] Guid? rentalId = null, [FromQuery] Guid? toolId = null)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var query = new GetConversationBetweenUsersQuery(currentUserId, userId, rentalId, toolId);
        var result = await _messageService.GetConversationBetweenUsersAsync(query);
        
        if (!result.Success)
            return NotFound(result);
            
        return Ok(result);
    }
}

/// <summary>
/// Request models for conversation actions
/// </summary>
public class CreateConversationRequest
{
    public string ParticipantId { get; set; } = string.Empty;
    public string? Title { get; set; }
    public Guid? RentalId { get; set; }
    public Guid? ToolId { get; set; }
}

public class UpdateConversationRequest
{
    public string? Title { get; set; }
    public bool? IsArchived { get; set; }
}