using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Features.Tools;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Core.DTOs.Tools;

namespace ToolsSharing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ToolsController : ControllerBase
{
    private readonly IToolsService _toolsService;
    private readonly ISettingsService _settingsService;
    private readonly IFileStorageService _fileStorageService;

    public ToolsController(IToolsService toolsService, ISettingsService settingsService, IFileStorageService fileStorageService)
    {
        _toolsService = toolsService;
        _settingsService = settingsService;
        _fileStorageService = fileStorageService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTools([FromQuery] GetToolsQuery query)
    {
        var result = await _toolsService.GetToolsAsync(query);
        return Ok(result);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetToolsPaged([FromQuery] GetToolsQuery query)
    {
        // Set default page size to 24 if not specified
        if (query.PageSize <= 0)
        {
            query = query with { PageSize = 24 };
        }
        
        var result = await _toolsService.GetToolsPagedAsync(query);
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

    [HttpGet("featured")]
    public async Task<IActionResult> GetFeaturedTools([FromQuery] int count = 6)
    {
        var result = await _toolsService.GetFeaturedToolsAsync(count);
        return Ok(result);
    }
    
    [HttpGet("popular")]
    public async Task<IActionResult> GetPopularTools([FromQuery] int count = 6)
    {
        var result = await _toolsService.GetPopularToolsAsync(count);
        return Ok(result);
    }
    
    [HttpGet("tags")]
    public async Task<IActionResult> GetPopularTags([FromQuery] int count = 20)
    {
        var result = await _toolsService.GetPopularTagsAsync(count);
        return Ok(result);
    }
    
    [HttpGet("search")]
    public async Task<IActionResult> SearchTools([FromQuery] SearchToolsQuery query)
    {
        var result = await _toolsService.SearchToolsAsync(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTool(Guid id)
    {
        var query = new GetToolByIdQuery(id);
        var result = await _toolsService.GetToolByIdAsync(query);
        
        if (!result.Success)
            return NotFound(result);
            
        // Increment view count asynchronously but properly sequenced to avoid race conditions
        try 
        {
            await _toolsService.IncrementViewCountAsync(id);
        } 
        catch 
        {
            // Log but don't fail the request
        }
            
        return Ok(result);
    }
    
    [HttpPost("{id}/views")]
    public async Task<IActionResult> IncrementViewCount(Guid id)
    {
        var result = await _toolsService.IncrementViewCountAsync(id);
        if (!result.Success)
            return NotFound(result);
            
        return Ok(result);
    }
    
    [HttpGet("{id}/reviews")]
    public async Task<IActionResult> GetToolReviews(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _toolsService.GetToolReviewsAsync(id, page, pageSize);
        return Ok(result);
    }
    
    [HttpGet("{id}/reviews/summary")]
    public async Task<IActionResult> GetToolReviewSummary(Guid id)
    {
        var result = await _toolsService.GetToolReviewSummaryAsync(id);
        return Ok(result);
    }
    
    [HttpPost("{id}/reviews")]
    [Authorize]
    public async Task<IActionResult> CreateToolReview(Guid id, [FromBody] CreateToolReviewRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var result = await _toolsService.CreateToolReviewAsync(id, userId, request);
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }
    
    [HttpGet("{id}/reviews/can-review")]
    [Authorize]
    public async Task<IActionResult> CanUserReviewTool(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var result = await _toolsService.CanUserReviewToolAsync(id, userId);
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
    public async Task<IActionResult> CreateTool([FromBody] CreateToolRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new CreateToolCommand(
            request.Name,
            request.Description,
            request.Category,
            request.Brand ?? string.Empty,
            request.Model ?? string.Empty,
            request.DailyRate,
            request.WeeklyRate ?? 0,
            request.MonthlyRate ?? 0,
            request.DepositRequired,
            request.Condition,
            request.EnhancedLocation,
            userId,
            request.LeadTimeHours,
            request.ImageUrls,
            request.Tags
        );
        
        var result = await _toolsService.CreateToolAsync(command);
        
        if (!result.Success)
            return BadRequest(result);
            
        return CreatedAtAction(nameof(GetTool), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateTool(Guid id, [FromBody] UpdateToolRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new UpdateToolCommand(
            id,
            request.Name,
            request.Description,
            request.Category,
            request.Brand ?? string.Empty,
            request.Model ?? string.Empty,
            request.DailyRate,
            request.WeeklyRate ?? 0,
            request.MonthlyRate ?? 0,
            request.DepositRequired,
            request.Condition,
            request.EnhancedLocation,
            request.IsAvailable,
            request.LeadTimeHours,
            userId,
            request.ImageUrls ?? new List<string>(),
            request.Tags
        );
            
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

    [HttpPost("upload-images")]
    [Authorize]
    public async Task<IActionResult> UploadImages(List<IFormFile> files)
    {
        try
        {
            if (files == null || !files.Any())
            {
                return BadRequest(new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "No files provided",
                    Errors = new List<string> { "At least one file must be provided" }
                });
            }

            if (files.Count > 5)
            {
                return BadRequest(new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "Too many files",
                    Errors = new List<string> { "Maximum 5 images allowed" }
                });
            }

            var uploadedUrls = new List<string>();
            var allowedTypes = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            foreach (var file in files)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedTypes.Contains(extension))
                {
                    return BadRequest(new ApiResponse<List<string>>
                    {
                        Success = false,
                        Message = $"Invalid file type: {file.FileName}",
                        Errors = new List<string> { "Only JPG, JPEG, PNG, GIF, and WebP files are allowed" }
                    });
                }

                if (file.Length > 5 * 1024 * 1024) // 5MB
                {
                    return BadRequest(new ApiResponse<List<string>>
                    {
                        Success = false,
                        Message = $"File too large: {file.FileName}",
                        Errors = new List<string> { "Maximum file size is 5MB" }
                    });
                }

                using var stream = file.OpenReadStream();
                var fileName = $"{Guid.NewGuid()}{extension}";
                var storagePath = await _fileStorageService.UploadFileAsync(stream, fileName, file.ContentType, "images");
                var fileUrl = await _fileStorageService.GetFileUrlAsync(storagePath);
                uploadedUrls.Add(fileUrl);
            }

            return Ok(new ApiResponse<List<string>>
            {
                Success = true,
                Data = uploadedUrls,
                Message = $"Successfully uploaded {uploadedUrls.Count} image(s)"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<List<string>>
            {
                Success = false,
                Message = "An error occurred while uploading images",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpPost("{id}/request-approval")]
    [Authorize]
    public async Task<IActionResult> RequestApproval(Guid id, [FromBody] RequestApprovalRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var result = await _toolsService.RequestApprovalAsync(id, userId, request);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }
}