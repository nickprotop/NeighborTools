using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Features.Rentals;
using ToolsSharing.API.Models;

namespace ToolsSharing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RentalsController : ControllerBase
{
    private readonly IRentalsService _rentalsService;

    public RentalsController(IRentalsService rentalsService)
    {
        _rentalsService = rentalsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRentals([FromQuery] GetRentalsQuery query)
    {
        // If no specific UserId is provided, use the current user's ID
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(query.UserId) && !string.IsNullOrEmpty(userId))
        {
            query = query with { UserId = userId };
        }
        
        var result = await _rentalsService.GetRentalsAsync(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRental(Guid id)
    {
        var query = new GetRentalByIdQuery(id);
        var result = await _rentalsService.GetRentalByIdAsync(query);
        
        if (!result.Success)
            return NotFound(result);
            
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRental([FromBody] CreateRentalDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Convert string ToolId to Guid
        if (!Guid.TryParse(dto.ToolId, out var toolId))
            return BadRequest("Invalid ToolId format");

        var command = new CreateRentalCommand(
            toolId,
            userId,
            dto.StartDate,
            dto.EndDate,
            dto.Notes
        );

        var result = await _rentalsService.CreateRentalAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return CreatedAtAction(nameof(GetRental), new { id = result.Data!.Id }, result);
    }

    [HttpPatch("{id}/approve")]
    public async Task<IActionResult> ApproveRental(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var command = new ApproveRentalCommand(id, userId);
        var result = await _rentalsService.ApproveRentalAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    [HttpPatch("{id}/reject")]
    public async Task<IActionResult> RejectRental(Guid id, [FromBody] string reason)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var command = new RejectRentalCommand(id, userId, reason);
        var result = await _rentalsService.RejectRentalAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> CancelRental(Guid id, [FromBody] string? reason = null)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var command = new CancelRentalCommand(id, userId, reason);
        var result = await _rentalsService.CancelRentalAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    [HttpPatch("{id}/pickup")]
    public async Task<IActionResult> MarkRentalPickedUp(Guid id, [FromBody] string? notes = null)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var command = new MarkRentalPickedUpCommand(id, userId, notes);
        var result = await _rentalsService.MarkRentalPickedUpAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    [HttpPatch("{id}/return")]
    public async Task<IActionResult> MarkRentalReturned(Guid id, [FromBody] MarkRentalReturnedRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var command = new MarkRentalReturnedCommand(id, userId, request.Notes, request.ConditionNotes);
        var result = await _rentalsService.MarkRentalReturnedAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    [HttpPatch("{id}/extend")]
    public async Task<IActionResult> ExtendRental(Guid id, [FromBody] ExtendRentalRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var command = new ExtendRentalCommand(id, userId, request.NewEndDate, request.Notes);
        var result = await _rentalsService.ExtendRentalAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdueRentals([FromQuery] GetOverdueRentalsQuery query)
    {
        var result = await _rentalsService.GetOverdueRentalsAsync(query);
        return Ok(result);
    }

    [HttpPost("check-overdue")]
    public async Task<IActionResult> CheckAndUpdateOverdueRentals()
    {
        var result = await _rentalsService.CheckAndUpdateOverdueRentalsAsync();
        return Ok(result);
    }
}