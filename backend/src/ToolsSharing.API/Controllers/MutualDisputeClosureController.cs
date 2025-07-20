using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.DTOs.Dispute;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Interfaces;

namespace ToolsSharing.API.Controllers;

/// <summary>
/// Controller for mutual dispute closure operations with comprehensive security and rate limiting
/// </summary>
[ApiController]
[Route("api/disputes/mutual-closure")]
[Authorize]
[EnableRateLimiting("MutualClosurePolicy")]
public class MutualDisputeClosureController : ControllerBase
{
    private readonly IMutualDisputeClosureService _mutualClosureService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<MutualDisputeClosureController> _logger;

    public MutualDisputeClosureController(
        IMutualDisputeClosureService mutualClosureService,
        UserManager<User> userManager,
        ILogger<MutualDisputeClosureController> logger)
    {
        _mutualClosureService = mutualClosureService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Check if a dispute is eligible for mutual closure
    /// </summary>
    [HttpGet("eligibility/{disputeId}")]
    public async Task<IActionResult> CheckEligibility(Guid disputeId)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.CreateFailure("User not authenticated"));
            }

            var eligibility = await _mutualClosureService.CheckMutualClosureEligibilityAsync(disputeId, userId);

            return Ok(ApiResponse<MutualClosureEligibilityDto>.CreateSuccess(eligibility, 
                eligibility.IsEligible ? "Dispute is eligible for mutual closure" : "Dispute is not eligible for mutual closure"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking mutual closure eligibility for dispute {DisputeId}", disputeId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("An error occurred while checking eligibility"));
        }
    }

    /// <summary>
    /// Create a mutual closure request for a dispute
    /// </summary>
    [HttpPost]
    [EnableRateLimiting("CreateMutualClosurePolicy")]
    public async Task<IActionResult> CreateMutualClosure([FromBody] CreateMutualClosureRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponse<object>.CreateFailure($"Validation failed: {string.Join(", ", errors)}"));
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.CreateFailure("User not authenticated"));
            }

            _logger.LogInformation("User {UserId} creating mutual closure request for dispute {DisputeId}", 
                userId, request.DisputeId);

            var result = await _mutualClosureService.CreateMutualClosureRequestAsync(request, userId);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.CreateFailure(result.ErrorMessage ?? "Failed to create mutual closure request"));
            }

            return CreatedAtAction(nameof(GetMutualClosure), 
                new { mutualClosureId = result.MutualClosureId }, 
                ApiResponse<MutualClosureDto>.CreateSuccess(result.MutualClosure!, "Mutual closure request created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating mutual closure request for dispute {DisputeId}", request.DisputeId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("An error occurred while creating the mutual closure request"));
        }
    }

    /// <summary>
    /// Respond to a mutual closure request (accept or reject)
    /// </summary>
    [HttpPost("{mutualClosureId}/respond")]
    [EnableRateLimiting("RespondMutualClosurePolicy")]
    public async Task<IActionResult> RespondToMutualClosure(Guid mutualClosureId, [FromBody] RespondToMutualClosureRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponse<object>.CreateFailure($"Validation failed: {string.Join(", ", errors)}"));
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.CreateFailure("User not authenticated"));
            }

            request.MutualClosureId = mutualClosureId;

            _logger.LogInformation("User {UserId} responding to mutual closure {MutualClosureId} with {Response}", 
                userId, mutualClosureId, request.Accept ? "ACCEPT" : "REJECT");

            var result = await _mutualClosureService.RespondToMutualClosureAsync(request, userId);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.CreateFailure(result.ErrorMessage ?? "Failed to respond to mutual closure request"));
            }

            var message = request.Accept 
                ? result.DisputeClosed ? "Mutual closure accepted and dispute closed successfully" : "Mutual closure accepted successfully"
                : "Mutual closure rejected successfully";

            var responseData = new
            {
                MutualClosure = result.MutualClosure,
                DisputeClosed = result.DisputeClosed,
                RefundTransactionId = result.RefundTransactionId
            };

            return Ok(ApiResponse<object>.CreateSuccess(responseData, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error responding to mutual closure {MutualClosureId}", mutualClosureId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("An error occurred while processing the response"));
        }
    }

    /// <summary>
    /// Cancel a mutual closure request (only by the initiator)
    /// </summary>
    [HttpPost("{mutualClosureId}/cancel")]
    public async Task<IActionResult> CancelMutualClosure(Guid mutualClosureId, [FromBody] CancelMutualClosureRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponse<object>.CreateFailure($"Validation failed: {string.Join(", ", errors)}"));
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.CreateFailure("User not authenticated"));
            }

            request.MutualClosureId = mutualClosureId;

            _logger.LogInformation("User {UserId} cancelling mutual closure {MutualClosureId}", userId, mutualClosureId);

            var result = await _mutualClosureService.CancelMutualClosureAsync(request, userId);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.CreateFailure(result.ErrorMessage ?? "Failed to cancel mutual closure request"));
            }

            return Ok(ApiResponse<MutualClosureDto>.CreateSuccess(result.MutualClosure!, "Mutual closure request cancelled successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling mutual closure {MutualClosureId}", mutualClosureId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("An error occurred while cancelling the mutual closure request"));
        }
    }

    /// <summary>
    /// Get details of a specific mutual closure request
    /// </summary>
    [HttpGet("{mutualClosureId}")]
    public async Task<IActionResult> GetMutualClosure(Guid mutualClosureId)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.CreateFailure("User not authenticated"));
            }

            var mutualClosure = await _mutualClosureService.GetMutualClosureAsync(mutualClosureId, userId);

            if (mutualClosure == null)
            {
                return NotFound(ApiResponse<object>.CreateFailure("Mutual closure request not found or access denied"));
            }

            return Ok(ApiResponse<MutualClosureDto>.CreateSuccess(mutualClosure, "Mutual closure retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mutual closure {MutualClosureId}", mutualClosureId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("An error occurred while retrieving the mutual closure"));
        }
    }

    /// <summary>
    /// Get all mutual closure requests for the current user
    /// </summary>
    [HttpGet("user")]
    public async Task<IActionResult> GetUserMutualClosures([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.CreateFailure("User not authenticated"));
            }

            if (pageSize > 50)
            {
                return BadRequest(ApiResponse<object>.CreateFailure("Page size cannot exceed 50"));
            }

            var result = await _mutualClosureService.GetUserMutualClosuresAsync(userId, page, pageSize);

            if (!result.Success)
            {
                return StatusCode(500, ApiResponse<object>.CreateFailure(result.Message));
            }

            var response = new
            {
                Items = result.Data,
                TotalCount = result.TotalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(result.TotalCount / (double)pageSize)
            };

            return Ok(ApiResponse<object>.CreateSuccess(response, result.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mutual closures for user");
            return StatusCode(500, ApiResponse<object>.CreateFailure("An error occurred while retrieving mutual closures"));
        }
    }

    /// <summary>
    /// Get mutual closure requests for a specific dispute
    /// </summary>
    [HttpGet("dispute/{disputeId}")]
    public async Task<IActionResult> GetDisputeMutualClosures(Guid disputeId)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.CreateFailure("User not authenticated"));
            }

            var result = await _mutualClosureService.GetDisputeMutualClosuresAsync(disputeId, userId);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.CreateFailure(result.Message));
            }

            return Ok(ApiResponse<List<MutualClosureSummaryDto>>.CreateSuccess(result.Data, result.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mutual closures for dispute {DisputeId}", disputeId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("An error occurred while retrieving mutual closures"));
        }
    }
}

/// <summary>
/// Admin controller for mutual closure management and oversight
/// </summary>
[ApiController]
[Route("api/admin/mutual-closure")]
[Authorize(Roles = "Admin,Support")]
public class AdminMutualClosureController : ControllerBase
{
    private readonly IMutualDisputeClosureService _mutualClosureService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<AdminMutualClosureController> _logger;

    public AdminMutualClosureController(
        IMutualDisputeClosureService mutualClosureService,
        UserManager<User> userManager,
        ILogger<AdminMutualClosureController> logger)
    {
        _mutualClosureService = mutualClosureService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Get all mutual closure requests for admin oversight
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllMutualClosures(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20, 
        [FromQuery] MutualClosureStatus? status = null)
    {
        try
        {
            if (pageSize > 100)
            {
                return BadRequest(ApiResponse<object>.CreateFailure("Page size cannot exceed 100"));
            }

            var result = await _mutualClosureService.GetMutualClosuresForAdminAsync(page, pageSize, status);

            if (!result.Success)
            {
                return StatusCode(500, ApiResponse<object>.CreateFailure(result.Message));
            }

            var response = new
            {
                Items = result.Data,
                TotalCount = result.TotalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(result.TotalCount / (double)pageSize)
            };

            return Ok(ApiResponse<object>.CreateSuccess(response, result.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mutual closures for admin");
            return StatusCode(500, ApiResponse<object>.CreateFailure("An error occurred while retrieving mutual closures"));
        }
    }

    /// <summary>
    /// Admin review of a mutual closure request
    /// </summary>
    [HttpPost("{mutualClosureId}/review")]
    public async Task<IActionResult> ReviewMutualClosure(Guid mutualClosureId, [FromBody] AdminReviewMutualClosureRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponse<object>.CreateFailure($"Validation failed: {string.Join(", ", errors)}"));
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.CreateFailure("User not authenticated"));
            }

            request.MutualClosureId = mutualClosureId;

            _logger.LogInformation("Admin {AdminId} reviewing mutual closure {MutualClosureId} with action {Action}", 
                userId, mutualClosureId, request.Action);

            var result = await _mutualClosureService.AdminReviewMutualClosureAsync(request, userId);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.CreateFailure(result.ErrorMessage ?? "Failed to review mutual closure"));
            }

            return Ok(ApiResponse<MutualClosureDto>.CreateSuccess(result.MutualClosure!, 
                $"Mutual closure {request.Action.ToString().ToLower()} successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in admin review of mutual closure {MutualClosureId}", mutualClosureId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("An error occurred during admin review"));
        }
    }

    /// <summary>
    /// Get mutual closure statistics for admin dashboard
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        try
        {
            var stats = await _mutualClosureService.GetMutualClosureStatisticsAsync(fromDate, toDate);

            return Ok(ApiResponse<MutualClosureStatsDto>.CreateSuccess(stats, "Statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mutual closure statistics");
            return StatusCode(500, ApiResponse<object>.CreateFailure("An error occurred while retrieving statistics"));
        }
    }

    /// <summary>
    /// Manual trigger for processing expired mutual closures (admin utility)
    /// </summary>
    [HttpPost("process-expired")]
    public async Task<IActionResult> ProcessExpired()
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            _logger.LogInformation("Admin {AdminId} manually triggering expired mutual closure processing", userId);

            await _mutualClosureService.ProcessExpiredMutualClosuresAsync();

            return Ok(ApiResponse<object>.CreateSuccess(null, "Expired mutual closures processed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expired mutual closures");
            return StatusCode(500, ApiResponse<object>.CreateFailure("An error occurred while processing expired mutual closures"));
        }
    }

    /// <summary>
    /// Manual trigger for sending reminders (admin utility)
    /// </summary>
    [HttpPost("send-reminders")]
    public async Task<IActionResult> SendReminders()
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            _logger.LogInformation("Admin {AdminId} manually triggering mutual closure reminders", userId);

            await _mutualClosureService.SendMutualClosureRemindersAsync();

            return Ok(ApiResponse<object>.CreateSuccess(null, "Mutual closure reminders sent successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending mutual closure reminders");
            return StatusCode(500, ApiResponse<object>.CreateFailure("An error occurred while sending reminders"));
        }
    }
}