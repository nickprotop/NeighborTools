using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ToolsSharing.Core.DTOs.Dispute;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Interfaces;
using GetDisputesRequest = ToolsSharing.Core.DTOs.Dispute.GetDisputesRequest;
using UpdateDisputeStatusRequest = ToolsSharing.Core.DTOs.Dispute.UpdateDisputeStatusRequest;
using CloseDisputeRequest = ToolsSharing.Core.DTOs.Dispute.CloseDisputeRequest;
using ResolveDisputeRequest = ToolsSharing.Core.Interfaces.ResolveDisputeRequest;
using PayPalDisputeWebhook = ToolsSharing.Core.Interfaces.PayPalDisputeWebhook;
using EvidenceFile = ToolsSharing.Core.Interfaces.EvidenceFile;

namespace ToolsSharing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DisputesController : ControllerBase
{
    private readonly IDisputeService _disputeService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<DisputesController> _logger;

    public DisputesController(
        IDisputeService disputeService,
        UserManager<User> userManager,
        ILogger<DisputesController> logger)
    {
        _disputeService = disputeService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetDisputes([FromQuery] GetDisputesRequest request)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            request.UserId = userId;
            var result = await _disputeService.GetDisputesAsync(request);

            return Ok(new
            {
                success = result.Success,
                data = result.Data,
                message = result.Message,
                totalCount = result.TotalCount,
                pageNumber = request.PageNumber,
                pageSize = request.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving disputes for user {UserId}", _userManager.GetUserId(User));
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving disputes"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDispute(Guid id)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _disputeService.GetDisputeAsync(id, userId);

            if (!result.Success)
            {
                return NotFound(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }

            return Ok(new
            {
                success = true,
                data = result.Dispute,
                message = "Dispute retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dispute {DisputeId}", id);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving the dispute"
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateDispute([FromBody] CreateDisputeRequest request)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            request.InitiatedBy = userId;
            var result = await _disputeService.CreateDisputeAsync(request);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }

            return CreatedAtAction(nameof(GetDispute), 
                new { id = result.DisputeId }, 
                new
                {
                    success = true,
                    data = result.Dispute,
                    message = "Dispute created successfully"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating dispute for rental {RentalId}", request.RentalId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while creating the dispute"
            });
        }
    }

    [HttpGet("{id}/messages")]
    public async Task<IActionResult> GetDisputeMessages(Guid id)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _disputeService.GetDisputeMessagesAsync(id, userId);

            if (!result.Success)
            {
                return NotFound(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Ok(new
            {
                success = true,
                data = result.Data,
                message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages for dispute {DisputeId}", id);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving dispute messages"
            });
        }
    }

    [HttpPost("{id}/messages")]
    public async Task<IActionResult> AddDisputeMessage(Guid id, [FromBody] AddDisputeMessageRequest request)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            request.DisputeId = id.ToString();
            request.SenderId = userId;
            request.SenderName = $"{user.FirstName} {user.LastName}";
            request.SenderRole = GetUserRole(User);

            var result = await _disputeService.AddDisputeMessageAsync(request);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }

            return Ok(new
            {
                success = true,
                data = result.Message,
                message = "Message added successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding message to dispute {DisputeId}", id);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while adding the message"
            });
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateDisputeStatus(Guid id, [FromBody] UpdateDisputeStatusRequest request)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            request.DisputeId = id;
            request.UpdatedBy = userId;

            var result = await _disputeService.UpdateDisputeStatusAsync(request);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Ok(new
            {
                success = true,
                data = result.Data,
                message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for dispute {DisputeId}", id);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while updating the dispute status"
            });
        }
    }

    [HttpPost("{id}/escalate")]
    public async Task<IActionResult> EscalateDispute(Guid id)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _disputeService.EscalateToPayPalAsync(id, userId);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }

            return Ok(new
            {
                success = true,
                data = new { externalDisputeId = result.ExternalDisputeId },
                message = "Dispute escalated to PayPal successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating dispute {DisputeId} to PayPal", id);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while escalating the dispute"
            });
        }
    }

    [HttpPost("{id}/resolve")]
    public async Task<IActionResult> ResolveDispute(Guid id, [FromBody] ResolveDisputeRequest request)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            request.DisputeId = id;
            request.ResolvedBy = userId;

            var result = await _disputeService.ResolveDisputeAsync(request);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }

            return Ok(new
            {
                success = true,
                data = new { refundTransactionId = result.RefundTransactionId },
                message = "Dispute resolved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving dispute {DisputeId}", id);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while resolving the dispute"
            });
        }
    }

    [HttpPost("{id}/close")]
    public async Task<IActionResult> CloseDispute(Guid id, [FromBody] CloseDisputeRequest request)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            request.DisputeId = id;
            request.ClosedBy = userId;

            var result = await _disputeService.CloseDisputeAsync(request);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Ok(new
            {
                success = true,
                data = result.Data,
                message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing dispute {DisputeId}", id);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while closing the dispute"
            });
        }
    }

    [HttpGet("{id}/timeline")]
    public async Task<IActionResult> GetDisputeTimeline(Guid id)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _disputeService.GetDisputeTimelineAsync(id, userId);

            if (!result.Success)
            {
                return NotFound(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Ok(new
            {
                success = true,
                data = result.Data,
                message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving timeline for dispute {DisputeId}", id);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving the dispute timeline"
            });
        }
    }

    [HttpPost("webhook/paypal")]
    [AllowAnonymous] // Security handled by PayPal webhook validation middleware
    public async Task<IActionResult> PayPalDisputeWebhook()
    {
        try
        {
            // Read the raw request body
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            var payload = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            _logger.LogInformation("Processing PayPal dispute webhook");

            // Parse webhook payload
            var webhook = System.Text.Json.JsonSerializer.Deserialize<PayPalDisputeWebhook>(payload);
            if (webhook == null)
            {
                _logger.LogError("Failed to parse PayPal dispute webhook payload");
                return BadRequest(new { error = "Invalid webhook payload" });
            }

            // Process webhook
            await _disputeService.HandlePayPalDisputeWebhookAsync(webhook);

            _logger.LogInformation("PayPal dispute webhook processed successfully: {EventType}", webhook.EventType);
            return Ok(new { message = "Webhook processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayPal dispute webhook");
            return StatusCode(500, new { error = "Internal server error processing webhook" });
        }
    }

    [HttpPost("{id}/sync-paypal")]
    [Authorize(Roles = "Admin,Support")]
    public async Task<IActionResult> SyncPayPalDispute(Guid id)
    {
        try
        {
            var result = await _disputeService.SyncPayPalDisputeAsync(id.ToString());

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }

            return Ok(new
            {
                success = true,
                data = result.Dispute,
                message = "PayPal dispute synced successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing PayPal dispute {DisputeId}", id);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while syncing the PayPal dispute"
            });
        }
    }

    [HttpGet("stats")]
    [Authorize(Roles = "Admin,Support")]
    public async Task<IActionResult> GetDisputeStats()
    {
        try
        {
            var result = await _disputeService.GetDisputeStatsAsync();

            return Ok(new
            {
                success = true,
                data = result.Data,
                message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dispute statistics");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving dispute statistics"
            });
        }
    }

    [HttpPost("{id}/evidence")]
    public async Task<IActionResult> UploadEvidence(Guid id, [FromForm] IFormFileCollection files, [FromForm] string? description = null)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (files == null || files.Count == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "No files provided"
                });
            }

            var evidenceFiles = new List<EvidenceFile>();

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream);

                    evidenceFiles.Add(new EvidenceFile
                    {
                        FileName = file.FileName,
                        ContentType = file.ContentType,
                        Content = memoryStream.ToArray(),
                        Size = file.Length,
                        Description = description ?? ""
                    });
                }
            }

            var result = await _disputeService.UploadEvidenceAsync(id, userId, evidenceFiles);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }

            return Ok(new
            {
                success = true,
                data = result.UploadedFiles,
                message = $"Uploaded {evidenceFiles.Count} evidence file(s) successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading evidence for dispute {DisputeId}", id);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while uploading evidence"
            });
        }
    }

    [HttpGet("{id}/evidence")]
    public async Task<IActionResult> GetEvidence(Guid id)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var evidenceFiles = await _disputeService.GetDisputeEvidenceAsync(id, userId);

            return Ok(new
            {
                success = true,
                data = evidenceFiles,
                message = "Evidence files retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving evidence for dispute {DisputeId}", id);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving evidence"
            });
        }
    }

    private string GetUserRole(System.Security.Claims.ClaimsPrincipal user)
    {
        if (user.IsInRole("Admin"))
            return "Admin";
        if (user.IsInRole("Support"))
            return "Support";
        
        // Determine if user is owner or renter based on context
        // This would need additional logic to determine the user's role in the specific dispute
        return "User";
    }
}

