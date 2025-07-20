using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToolsSharing.Core.DTOs.Admin;
using ToolsSharing.Core.Interfaces;

namespace ToolsSharing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SampleDataController : ControllerBase
{
    private readonly ISampleDataService _sampleDataService;
    private readonly ILogger<SampleDataController> _logger;

    public SampleDataController(
        ISampleDataService sampleDataService,
        ILogger<SampleDataController> logger)
    {
        _sampleDataService = sampleDataService;
        _logger = logger;
    }

    /// <summary>
    /// Get current sample data status
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<SampleDataStatusDto>> GetStatus()
    {
        try
        {
            var status = await _sampleDataService.GetStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sample data status");
            return StatusCode(500, "Internal server error while getting sample data status");
        }
    }

    /// <summary>
    /// Apply sample data to the system
    /// </summary>
    [HttpPost("apply")]
    public async Task<ActionResult<SampleDataStatusDto>> ApplySampleData([FromBody] ApplySampleDataRequest request)
    {
        try
        {
            var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminUserId))
            {
                return Unauthorized("Admin user ID not found");
            }

            _logger.LogInformation("Admin {AdminUserId} applying sample data: {DataTypes}", 
                adminUserId, string.Join(", ", request.DataTypes));

            var status = await _sampleDataService.ApplySampleDataAsync(request, adminUserId);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying sample data");
            return StatusCode(500, "Internal server error while applying sample data");
        }
    }

    /// <summary>
    /// Remove sample data from the system
    /// </summary>
    [HttpPost("remove")]
    public async Task<ActionResult<SampleDataStatusDto>> RemoveSampleData([FromBody] RemoveSampleDataRequest request)
    {
        try
        {
            var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminUserId))
            {
                return Unauthorized("Admin user ID not found");
            }

            _logger.LogInformation("Admin {AdminUserId} removing sample data: {DataTypes}", 
                adminUserId, string.Join(", ", request.DataTypes));

            var status = await _sampleDataService.RemoveSampleDataAsync(request, adminUserId);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing sample data");
            return StatusCode(500, "Internal server error while removing sample data");
        }
    }

    /// <summary>
    /// Remove ALL sample data from the system
    /// </summary>
    [HttpPost("remove-all")]
    public async Task<ActionResult<SampleDataStatusDto>> RemoveAllSampleData()
    {
        try
        {
            var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminUserId))
            {
                return Unauthorized("Admin user ID not found");
            }

            _logger.LogInformation("Admin {AdminUserId} removing ALL sample data", adminUserId);

            await _sampleDataService.RemoveAllSampleDataAsync(adminUserId);
            var status = await _sampleDataService.GetStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing all sample data");
            return StatusCode(500, "Internal server error while removing all sample data");
        }
    }

    /// <summary>
    /// Check if specific sample data type is applied
    /// </summary>
    [HttpGet("check/{dataType}")]
    public async Task<ActionResult<bool>> CheckSampleDataType(string dataType)
    {
        try
        {
            var isApplied = await _sampleDataService.IsSampleDataAppliedAsync(dataType);
            return Ok(isApplied);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking sample data type {DataType}", dataType);
            return StatusCode(500, "Internal server error while checking sample data type");
        }
    }
}