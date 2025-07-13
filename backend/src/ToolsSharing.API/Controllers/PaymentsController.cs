using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ToolsSharing.Core.DTOs.Payment;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Interfaces;

namespace ToolsSharing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentProvider _paymentProvider;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        IPaymentProvider paymentProvider,
        UserManager<User> userManager,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _paymentProvider = paymentProvider;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpPost("initiate/{rentalId}")]
    public async Task<IActionResult> InitiatePayment(Guid rentalId)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _paymentService.InitiateRentalPaymentAsync(rentalId, userId);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    paymentId = result.PaymentId,
                    approvalUrl = result.ApprovalUrl,
                    message = "Payment initiated successfully"
                });
            }

            return BadRequest(new
            {
                success = false,
                message = result.ErrorMessage ?? "Failed to initiate payment"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating payment for rental {RentalId}", rentalId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while processing your request"
            });
        }
    }

    [HttpPost("complete")]
    public async Task<IActionResult> CompletePayment([FromBody] CompletePaymentRequest request)
    {
        try
        {
            var result = await _paymentService.CompleteRentalPaymentAsync(request.PaymentId, request.PayerId);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    transactionId = result.TransactionId,
                    message = "Payment completed successfully"
                });
            }

            return BadRequest(new
            {
                success = false,
                message = result.ErrorMessage ?? "Failed to complete payment"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing payment {PaymentId}", request.PaymentId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while processing your request"
            });
        }
    }

    [HttpGet("status/{paymentId}")]
    public async Task<IActionResult> GetPaymentStatus(string paymentId)
    {
        try
        {
            var result = await _paymentService.GetPaymentStatusAsync(paymentId);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    status = result.Status,
                    amount = result.Amount,
                    currency = result.Currency,
                    payerEmail = result.PayerEmail,
                    payerName = result.PayerName,
                    createdAt = result.CreatedAt,
                    updatedAt = result.UpdatedAt
                });
            }

            return NotFound(new
            {
                success = false,
                message = result.ErrorMessage ?? "Payment not found"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status for {PaymentId}", paymentId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while processing your request"
            });
        }
    }

    [HttpPost("refund/{rentalId}")]
    public async Task<IActionResult> RefundPayment(Guid rentalId, [FromBody] RefundRequest request)
    {
        try
        {
            var result = await _paymentService.RefundRentalAsync(rentalId, request.Amount, request.Reason);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    refundId = result.RefundId,
                    refundedAmount = result.RefundedAmount,
                    message = "Refund processed successfully"
                });
            }

            return BadRequest(new
            {
                success = false,
                message = result.ErrorMessage ?? "Failed to process refund"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for rental {RentalId}", rentalId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while processing your request"
            });
        }
    }

    [HttpPost("refund-deposit/{rentalId}")]
    public async Task<IActionResult> RefundSecurityDeposit(Guid rentalId)
    {
        try
        {
            var result = await _paymentService.RefundSecurityDepositAsync(rentalId);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    refundedAmount = result.RefundedAmount,
                    message = "Security deposit refunded successfully"
                });
            }

            return BadRequest(new
            {
                success = false,
                message = result.ErrorMessage ?? "Failed to refund security deposit"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding security deposit for rental {RentalId}", rentalId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while processing your request"
            });
        }
    }

    [HttpGet("transaction/{rentalId}")]
    public async Task<IActionResult> GetTransactionDetails(Guid rentalId)
    {
        try
        {
            var transaction = await _paymentService.GetTransactionByRentalAsync(rentalId);

            return Ok(new
            {
                success = true,
                transaction = new
                {
                    id = transaction.Id,
                    rentalAmount = transaction.RentalAmount,
                    securityDeposit = transaction.SecurityDeposit,
                    commissionRate = transaction.CommissionRate,
                    commissionAmount = transaction.CommissionAmount,
                    totalAmount = transaction.TotalAmount,
                    ownerPayoutAmount = transaction.OwnerPayoutAmount,
                    status = transaction.Status.ToString(),
                    paymentCompletedAt = transaction.PaymentCompletedAt,
                    payoutScheduledAt = transaction.PayoutScheduledAt,
                    payoutCompletedAt = transaction.PayoutCompletedAt,
                    depositRefundedAt = transaction.DepositRefundedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction for rental {RentalId}", rentalId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while processing your request"
            });
        }
    }

    [HttpGet("calculate-fees")]
    [AllowAnonymous]
    public IActionResult CalculateFees([FromQuery] decimal rentalAmount, [FromQuery] decimal securityDeposit)
    {
        try
        {
            var userId = _userManager.GetUserId(User); // Will be null for anonymous users
            var breakdown = _paymentService.CalculateRentalFinancials(rentalAmount, securityDeposit, userId);

            return Ok(new
            {
                success = true,
                data = new
                {
                    rentalAmount = breakdown.RentalAmount,
                    securityDeposit = breakdown.SecurityDeposit,
                    commissionRate = breakdown.CommissionRate,
                    commissionAmount = breakdown.CommissionAmount,
                    totalPayerAmount = breakdown.TotalPayerAmount,
                    ownerPayoutAmount = breakdown.OwnerPayoutAmount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating fees");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while calculating fees"
            });
        }
    }

    [HttpGet("calculate-rental-cost")]
    [AllowAnonymous]
    public async Task<IActionResult> CalculateRentalCost([FromQuery] Guid toolId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            // Get tool from database
            var tool = await _paymentService.GetToolForCalculationAsync(toolId);
            if (tool == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Tool not found"
                });
            }

            // Validate dates
            if (startDate >= endDate)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "End date must be after start date"
                });
            }

            if (startDate < DateTime.Today)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Start date cannot be in the past"
                });
            }

            // Calculate rental cost using the same logic as backend
            var rentalDays = (endDate - startDate).Days + 1;
            decimal rentalAmount;
            string rateType;
            decimal selectedRate;

            if (rentalDays >= 30 && tool.MonthlyRate.HasValue)
            {
                var months = Math.Ceiling(rentalDays / 30.0);
                rentalAmount = (decimal)months * tool.MonthlyRate.Value;
                selectedRate = tool.MonthlyRate.Value;
                rateType = "monthly";
            }
            else if (rentalDays >= 7 && tool.WeeklyRate.HasValue)
            {
                var weeks = Math.Ceiling(rentalDays / 7.0);
                rentalAmount = (decimal)weeks * tool.WeeklyRate.Value;
                selectedRate = tool.WeeklyRate.Value;
                rateType = "weekly";
            }
            else
            {
                rentalAmount = rentalDays * tool.DailyRate;
                selectedRate = tool.DailyRate;
                rateType = "daily";
            }

            // Get security deposit
            var securityDeposit = tool.DepositRequired;

            // Calculate fees with the rental amount
            var userId = _userManager.GetUserId(User);
            var breakdown = _paymentService.CalculateRentalFinancials(rentalAmount, securityDeposit, userId);

            return Ok(new
            {
                success = true,
                data = new
                {
                    toolId = toolId,
                    startDate = startDate,
                    endDate = endDate,
                    rentalDays = rentalDays,
                    rateType = rateType,
                    selectedRate = selectedRate,
                    rentalAmount = breakdown.RentalAmount,
                    securityDeposit = breakdown.SecurityDeposit,
                    commissionRate = breakdown.CommissionRate,
                    commissionAmount = breakdown.CommissionAmount,
                    totalPayerAmount = breakdown.TotalPayerAmount,
                    ownerPayoutAmount = breakdown.OwnerPayoutAmount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating rental cost for tool {ToolId} from {StartDate} to {EndDate}", toolId, startDate, endDate);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while calculating rental cost"
            });
        }
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetPaymentSettings()
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var settings = await _paymentService.GetOrCreatePaymentSettingsAsync(userId);

            return Ok(new
            {
                success = true,
                settings = new
                {
                    payPalEmail = settings.PayPalEmail,
                    customCommissionRate = settings.CustomCommissionRate,
                    isCommissionEnabled = settings.IsCommissionEnabled,
                    payoutSchedule = settings.PayoutSchedule.ToString(),
                    minimumPayoutAmount = settings.MinimumPayoutAmount,
                    notifyOnPaymentReceived = settings.NotifyOnPaymentReceived,
                    notifyOnPayoutSent = settings.NotifyOnPayoutSent,
                    notifyOnPayoutFailed = settings.NotifyOnPayoutFailed,
                    isPayoutVerified = settings.IsPayoutVerified
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment settings");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving settings"
            });
        }
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdatePaymentSettings([FromBody] UpdatePaymentSettingsDto settings)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            await _paymentService.UpdatePaymentSettingsAsync(userId, settings);

            return Ok(new
            {
                success = true,
                message = "Payment settings updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment settings");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while updating settings"
            });
        }
    }

    [HttpPost("payout/{transactionId}")]
    public async Task<IActionResult> CreateOwnerPayout(Guid transactionId)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _paymentService.CreateOwnerPayoutAsync(transactionId);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = "Payout created successfully",
                    payoutId = result.PayoutId
                });
            }

            return BadRequest(new
            {
                success = false,
                message = result.ErrorMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating owner payout for transaction {TransactionId}", transactionId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while creating the payout"
            });
        }
    }

    [HttpPost("process-scheduled-payouts")]
    [AllowAnonymous] // For background job or admin use
    public async Task<IActionResult> ProcessScheduledPayouts()
    {
        try
        {
            await _paymentService.ProcessScheduledPayoutsAsync();
            return Ok(new
            {
                success = true,
                message = "Scheduled payouts processed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled payouts");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while processing scheduled payouts"
            });
        }
    }

    [HttpGet("payout/status/{payoutId}")]
    public async Task<IActionResult> GetPayoutStatus(string payoutId)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _paymentService.GetPayoutStatusAsync(payoutId);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        payoutId = result.PayoutId,
                        status = result.Status,
                        amount = result.Amount,
                        currency = result.Currency,
                        processedAt = result.ProcessedAt,
                        recipientEmail = result.RecipientEmail
                    }
                });
            }

            return NotFound(new
            {
                success = false,
                message = result.ErrorMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payout status for {PayoutId}", payoutId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving payout status"
            });
        }
    }

    [HttpPost("webhook/paypal")]
    [AllowAnonymous] // Security is handled by middleware
    public async Task<IActionResult> PayPalWebhook()
    {
        try
        {
            // Note: Webhook signature validation is handled by PayPalWebhookValidationMiddleware
            // If we reach this point, the webhook is already validated
            
            // Read the raw request body
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            var payload = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            _logger.LogInformation("Processing validated PayPal webhook");

            // Process webhook through payment provider
            var processResult = await _paymentProvider.ProcessWebhookAsync(payload);
            
            if (processResult.Success)
            {
                _logger.LogInformation("PayPal webhook processed successfully: {EventType}", processResult.EventType);
                return Ok(new { message = "Webhook processed successfully" });
            }

            _logger.LogError("Failed to process PayPal webhook: {Error}", processResult.ErrorMessage);
            return StatusCode(500, new { error = "Failed to process webhook" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayPal webhook");
            return StatusCode(500, new { error = "Internal server error processing webhook" });
        }
    }

    [HttpGet("can-receive-payments/{ownerId}")]
    public async Task<IActionResult> CanOwnerReceivePayments(string ownerId)
    {
        try
        {
            var canReceive = await _paymentService.CanOwnerReceivePaymentsAsync(ownerId);
            
            return Ok(new
            {
                success = true,
                data = new
                {
                    canReceivePayments = canReceive
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking payment settings for owner {OwnerId}", ownerId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while checking payment settings"
            });
        }
    }
}

// Request DTOs
public class CompletePaymentRequest
{
    public string PaymentId { get; set; } = string.Empty;
    public string PayerId { get; set; } = string.Empty;
}

public class RefundRequest
{
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
}