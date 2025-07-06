using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Features.Tools;

namespace ToolsSharing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ToolsController : ControllerBase
{
    private readonly IToolsService _toolsService;

    public ToolsController(IToolsService toolsService)
    {
        _toolsService = toolsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTools([FromQuery] GetToolsQuery query)
    {
        var result = await _toolsService.GetToolsAsync(query);
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