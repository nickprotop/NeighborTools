using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Features.Tools;
using ToolsSharing.Core.Interfaces;

namespace ToolsSharing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ToolsController : ControllerBase
{
    private readonly IToolsService _toolsService;
    private readonly ISettingsService _settingsService;

    public ToolsController(IToolsService toolsService, ISettingsService settingsService)
    {
        _toolsService = toolsService;
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTools([FromQuery] GetToolsQuery query)
    {
        var result = await _toolsService.GetToolsAsync(query);
        return Ok(result);
    }

    [HttpGet("my-tools")]
    [Authorize]
    public async Task<IActionResult> GetMyTools([FromQuery] GetToolsQuery query)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var result = await _toolsService.GetUserToolsAsync(query, userId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTool(Guid id)
    {
        var query = new GetToolByIdQuery(id);
        var result = await _toolsService.GetToolByIdAsync(query);
        
        if (!result.Success)
            return NotFound(result);
            
        return Ok(result);
    }

    /// <summary>
    /// Get public rental preferences for a tool owner
    /// </summary>
    [HttpGet("{toolId}/rental-preferences")]
    public async Task<IActionResult> GetToolRentalPreferences(Guid toolId)
    {
        try
        {
            // First get the tool to find the owner
            var toolQuery = new GetToolByIdQuery(toolId);
            var toolResult = await _toolsService.GetToolByIdAsync(toolQuery);
            
            if (!toolResult.Success || toolResult.Data == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Tool not found"
                });
            }

            // Get owner's settings
            var ownerSettings = await _settingsService.GetUserSettingsAsync(toolResult.Data.OwnerId);
            
            // Calculate lead time: tool-specific > owner default > system default (24)
            var leadTimeHours = toolResult.Data.LeadTimeHours ?? 
                               ownerSettings?.Rental?.RentalLeadTime ?? 
                               24;
            
            // Use owner settings for other preferences, with defaults
            var autoApprovalEnabled = ownerSettings?.Rental?.AutoApproveRentals ?? false;
            var requireDeposit = ownerSettings?.Rental?.RequireDeposit ?? true;
            var defaultDepositPercentage = ownerSettings?.Rental?.DefaultDepositPercentage ?? 0.20m;

            // Return public rental preferences (no sensitive data)
            var publicPreferences = new
            {
                LeadTimeHours = leadTimeHours,
                AutoApprovalEnabled = autoApprovalEnabled,
                RequireDeposit = requireDeposit,
                DefaultDepositPercentage = defaultDepositPercentage
            };

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = publicPreferences,
                Message = "Rental preferences retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving rental preferences",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateTool([FromBody] CreateToolCommand command)
    {
        var result = await _toolsService.CreateToolAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return CreatedAtAction(nameof(GetTool), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateTool(Guid id, [FromBody] UpdateToolCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");
            
        var result = await _toolsService.UpdateToolAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteTool(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var command = new DeleteToolCommand(id, userId);
        var result = await _toolsService.DeleteToolAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return NoContent();
    }
}