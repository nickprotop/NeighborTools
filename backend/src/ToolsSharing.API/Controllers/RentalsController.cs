using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Features.Rentals;

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
    public async Task<IActionResult> CreateRental([FromBody] CreateRentalCommand command)
    {
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
}