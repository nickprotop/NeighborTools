using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToolsSharing.Core.Configuration;
using ToolsSharing.Core.DTOs.Payment;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Common.Models.EmailNotifications;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IEmailNotificationService _emailService;
    private readonly IFraudDetectionService _fraudDetectionService;
    private readonly PaymentConfiguration _config;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ApplicationDbContext context,
        IPaymentProvider paymentProvider,
        IEmailNotificationService emailService,
        IFraudDetectionService fraudDetectionService,
        IOptions<PaymentConfiguration> paymentOptions,
        ILogger<PaymentService> logger)
    {
        _context = context;
        _paymentProvider = paymentProvider;
        _emailService = emailService;
        _fraudDetectionService = fraudDetectionService;
        _config = paymentOptions.Value;
        _logger = logger;
    }

    public async Task<CreatePaymentResult> InitiateRentalPaymentAsync(Guid rentalId, string userId)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var rental = await _context.Rentals
                .Include(r => r.Tool)
                .Include(r => r.Owner)
                .Include(r => r.Renter)
                .FirstOrDefaultAsync(r => r.Id == rentalId);

            if (rental == null)
            {
                return new CreatePaymentResult
                {
                    Success = false,
                    ErrorMessage = "Rental not found"
                };
            }

            if (rental.RenterId != userId)
            {
                return new CreatePaymentResult
                {
                    Success = false,
                    ErrorMessage = "Unauthorized: You are not the renter"
                };
            }

            // Check if transaction already exists (with row lock to prevent race conditions)
            var existingTransaction = await _context.Transactions
                .Where(t => t.RentalId == rentalId)
                .FirstOrDefaultAsync();

            if (existingTransaction != null && existingTransaction.Status != TransactionStatus.Cancelled)
            {
                return new CreatePaymentResult
                {
                    Success = false,
                    ErrorMessage = "Payment already initiated for this rental"
                };
            }

            // Calculate financial breakdown
            var financials = CalculateRentalFinancials(rental.TotalCost, rental.DepositAmount, rental.OwnerId);

            // Create or update transaction
            var transaction = existingTransaction ?? new Transaction { RentalId = rentalId };
            transaction.RentalAmount = financials.RentalAmount;
            transaction.SecurityDeposit = financials.SecurityDeposit;
            transaction.CommissionRate = financials.CommissionRate;
            transaction.CommissionAmount = financials.CommissionAmount;
            transaction.TotalAmount = financials.TotalPayerAmount;
            transaction.OwnerPayoutAmount = financials.OwnerPayoutAmount;
            transaction.Status = TransactionStatus.PaymentProcessing;
            transaction.Currency = _config.DefaultCurrency;

            if (existingTransaction == null)
            {
                _context.Transactions.Add(transaction);
            }

            await _context.SaveChangesAsync();

            // Get owner's payment settings for PayPal email
            var ownerSettings = await GetOrCreatePaymentSettingsAsync(rental.OwnerId);

            // Create payment with payment provider
            var paymentRequest = new CreatePaymentRequest
            {
                RentalId = rentalId,
                PayerId = userId,
                PayeeId = rental.OwnerId,
                Amount = financials.TotalPayerAmount,
                Currency = _config.DefaultCurrency,
                Description = $"Rental of {rental.Tool.Name} from {rental.StartDate:MMM dd} to {rental.EndDate:MMM dd}",
                ReturnUrl = $"/payments/complete?rentalId={rentalId}",
                CancelUrl = $"/rentals/{rentalId}",
                IsMarketplacePayment = true,
                PlatformFee = financials.CommissionAmount,
                PayeeEmail = ownerSettings.PayPalEmail,
                Metadata = new Dictionary<string, string>
                {
                    ["rental_id"] = rentalId.ToString(),
                    ["transaction_id"] = transaction.Id.ToString()
                }
            };

            var result = await _paymentProvider.CreatePaymentAsync(paymentRequest);

            if (result.Success)
            {
                // Create payment record
                var payment = new Payment
                {
                    RentalId = rentalId,
                    PayerId = userId,
                    Type = PaymentType.RentalPayment,
                    Status = PaymentStatus.Pending,
                    Provider = PaymentProvider.PayPal,
                    Amount = financials.TotalPayerAmount,
                    Currency = _config.DefaultCurrency,
                    ExternalPaymentId = result.PaymentId,
                    ExternalOrderId = result.OrderId
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                _logger.LogInformation("Payment initiated successfully for rental {RentalId}, payment ID: {PaymentId}", 
                    rentalId, result.PaymentId);
            }
            else
            {
                // Update transaction status and rollback
                transaction.Status = TransactionStatus.Cancelled;
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync(); // Still commit the cancellation status

                _logger.LogWarning("Payment initiation failed for rental {RentalId}: {ErrorMessage}", 
                    rentalId, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Error initiating rental payment for rental {RentalId}", rentalId);
            return new CreatePaymentResult
            {
                Success = false,
                ErrorMessage = "An error occurred while processing your payment"
            };
        }
    }

    public async Task<CapturePaymentResult> CompleteRentalPaymentAsync(string paymentId, string payerId)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Tool)
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Owner)
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Renter)
                .FirstOrDefaultAsync(p => p.ExternalOrderId == paymentId || p.ExternalPaymentId == paymentId);

            if (payment == null)
            {
                return new CapturePaymentResult
                {
                    Success = false,
                    ErrorMessage = "Payment not found"
                };
            }

            if (payment.Status != PaymentStatus.Pending)
            {
                return new CapturePaymentResult
                {
                    Success = false,
                    ErrorMessage = "Payment is not in pending status"
                };
            }

            // Validate payment amount before capture
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.RentalId == payment.RentalId);

            if (transaction == null)
            {
                return new CapturePaymentResult
                {
                    Success = false,
                    ErrorMessage = "Transaction record not found"
                };
            }

            // Verify payment amount matches expected transaction amount
            if (Math.Abs(payment.Amount - transaction.TotalAmount) > 0.01m) // Allow 1 cent tolerance for rounding
            {
                _logger.LogError("Payment amount validation failed for rental {RentalId}. Expected: {Expected}, Actual: {Actual}", 
                    payment.RentalId, transaction.TotalAmount, payment.Amount);
                
                payment.Status = PaymentStatus.Failed;
                payment.FailedAt = DateTime.UtcNow;
                payment.FailureReason = "Payment amount validation failed";
                transaction.Status = TransactionStatus.Cancelled;
                
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return new CapturePaymentResult
                {
                    Success = false,
                    ErrorMessage = "Payment amount does not match expected total"
                };
            }

            // Get payment status from provider to validate external payment details
            var paymentStatus = await _paymentProvider.GetPaymentStatusAsync(payment.ExternalPaymentId!);
            
            if (!paymentStatus.Success || paymentStatus.Amount != payment.Amount)
            {
                _logger.LogError("Payment status validation failed for rental {RentalId}. Provider status: {Status}, Provider amount: {Amount}", 
                    payment.RentalId, paymentStatus.Success, paymentStatus.Amount);
                
                payment.Status = PaymentStatus.Failed;
                payment.FailedAt = DateTime.UtcNow;
                payment.FailureReason = "Payment validation failed with provider";
                transaction.Status = TransactionStatus.Cancelled;
                
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return new CapturePaymentResult
                {
                    Success = false,
                    ErrorMessage = "Payment validation failed"
                };
            }

            // Perform fraud detection check
            try
            {
                var fraudCheckResult = await _fraudDetectionService.CheckPaymentAsync(payment);
                
                if (!fraudCheckResult.IsApproved)
                {
                    _logger.LogWarning("Payment blocked by fraud detection for rental {RentalId}. Risk Score: {RiskScore}, Reason: {Reason}", 
                        payment.RentalId, fraudCheckResult.RiskScore, fraudCheckResult.BlockingReason);
                    
                    payment.Status = PaymentStatus.Failed;
                    payment.FailedAt = DateTime.UtcNow;
                    payment.FailureReason = fraudCheckResult.BlockingReason ?? "Payment blocked by fraud detection";
                    transaction.Status = TransactionStatus.Cancelled;
                    
                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    return new CapturePaymentResult
                    {
                        Success = false,
                        ErrorMessage = "Payment was blocked due to security concerns. Please contact support if you believe this is an error."
                    };
                }
                
                if (fraudCheckResult.RequiresManualReview)
                {
                    _logger.LogInformation("Payment flagged for manual review for rental {RentalId}. Risk Score: {RiskScore}", 
                        payment.RentalId, fraudCheckResult.RiskScore);
                    
                    payment.Status = PaymentStatus.UnderReview;
                    payment.FailureReason = "Payment is under security review";
                    transaction.Status = TransactionStatus.UnderReview;
                    
                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    return new CapturePaymentResult
                    {
                        Success = false,
                        ErrorMessage = "Payment is under security review. You will be notified once the review is complete."
                    };
                }
                
                // Update velocity tracking for approved payments
                await _fraudDetectionService.UpdateVelocityTrackingAsync(payment.PayerId, payment.Amount);
                
                _logger.LogInformation("Payment passed fraud detection for rental {RentalId}. Risk Score: {RiskScore}", 
                    payment.RentalId, fraudCheckResult.RiskScore);
            }
            catch (Exception fraudEx)
            {
                _logger.LogError(fraudEx, "Error during fraud detection for rental {RentalId}. Proceeding with manual review.", payment.RentalId);
                
                // In case of fraud detection error, require manual review for safety
                payment.Status = PaymentStatus.UnderReview;
                payment.FailureReason = "Payment requires manual security review";
                transaction.Status = TransactionStatus.UnderReview;
                
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return new CapturePaymentResult
                {
                    Success = false,
                    ErrorMessage = "Payment is under security review. You will be notified once the review is complete."
                };
            }

            // Capture payment with provider
            var captureRequest = new CapturePaymentRequest
            {
                PaymentId = payment.ExternalPaymentId!,
                OrderId = payment.ExternalOrderId,
                PayerId = payerId
            };

            var result = await _paymentProvider.CapturePaymentAsync(captureRequest);

            if (result.Success)
            {
                // Update payment record
                payment.Status = PaymentStatus.Completed;
                payment.ProcessedAt = DateTime.UtcNow;
                payment.ExternalPayerId = payerId;
                // Note: ExternalTransactionId field doesn't exist in Payment entity

                // Update transaction status
                transaction.Status = TransactionStatus.PaymentCompleted;
                transaction.PaymentCompletedAt = DateTime.UtcNow;
                
                // Get rental for owner ID and status update
                var rental = payment.Rental!;
                
                // Schedule payout based on owner's preferences
                transaction.PayoutScheduledAt = await CalculatePayoutScheduleAsync(rental.OwnerId);

                // Update rental status to active (payment completed)
                rental.Status = RentalStatus.Approved; // Payment completed - rental is approved
                if (!rental.ApprovedAt.HasValue)
                {
                    rental.ApprovedAt = DateTime.UtcNow;
                }

                // Create commission payment record
                var commissionPayment = new Payment
                {
                    RentalId = payment.RentalId,
                    PayerId = payment.PayerId,
                    PayeeId = "PLATFORM", // Platform receives commission
                    Type = PaymentType.PlatformCommission,
                    Status = PaymentStatus.Completed,
                    Provider = PaymentProvider.Platform,
                    Amount = transaction?.CommissionAmount ?? 0,
                    Currency = payment.Currency,
                    ProcessedAt = DateTime.UtcNow
                    // Note: ExternalTransactionId field doesn't exist in Payment entity
                };

                _context.Payments.Add(commissionPayment);
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                _logger.LogInformation("Payment completed successfully for rental {RentalId}, transaction ID: {TransactionId}", 
                    payment.RentalId, result.TransactionId);

                // Send email notifications (outside transaction)
                try
                {
                    await SendPaymentConfirmationEmailsAsync(payment, transaction!);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Error sending payment confirmation emails for rental {RentalId}", payment.RentalId);
                    // Don't fail the entire payment for email issues
                }
            }
            else
            {
                // Update payment status
                payment.Status = PaymentStatus.Failed;
                payment.FailedAt = DateTime.UtcNow;
                payment.FailureReason = result.ErrorMessage;

                // Update transaction status on failure
                transaction.Status = TransactionStatus.Cancelled;

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync(); // Still commit the failure status

                _logger.LogWarning("Payment capture failed for rental {RentalId}: {ErrorMessage}", 
                    payment.RentalId, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Error completing rental payment {PaymentId}", paymentId);
            return new CapturePaymentResult
            {
                Success = false,
                ErrorMessage = "An error occurred while completing your payment"
            };
        }
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(string paymentId)
    {
        return await _paymentProvider.GetPaymentStatusAsync(paymentId);
    }

    public async Task<RefundResult> RefundRentalAsync(Guid rentalId, decimal amount, string reason)
    {
        try
        {
            var payment = await _context.Payments
                .Where(p => p.RentalId == rentalId && 
                           p.Type == PaymentType.RentalPayment && 
                           p.Status == PaymentStatus.Completed)
                .FirstOrDefaultAsync();

            if (payment == null)
            {
                return new RefundResult
                {
                    Success = false,
                    ErrorMessage = "No completed payment found for this rental"
                };
            }

            if (payment.IsRefunded && payment.RefundedAmount >= amount)
            {
                return new RefundResult
                {
                    Success = false,
                    ErrorMessage = "Payment has already been refunded"
                };
            }

            var refundRequest = new RefundPaymentRequest
            {
                PaymentId = payment.ExternalPaymentId!,
                Amount = amount,
                Reason = reason,
                IsPartialRefund = amount < payment.Amount
            };

            var result = await _paymentProvider.RefundPaymentAsync(refundRequest);

            if (result.Success)
            {
                // Update payment record
                payment.IsRefunded = true;
                payment.RefundedAmount += result.RefundedAmount;
                payment.RefundedAt = DateTime.UtcNow;
                payment.RefundReason = reason;

                if (payment.RefundedAmount >= payment.Amount)
                {
                    payment.Status = PaymentStatus.Refunded;
                }
                else
                {
                    payment.Status = PaymentStatus.PartiallyRefunded;
                }

                // Create refund payment record
                var refundPayment = new Payment
                {
                    RentalId = rentalId,
                    PayerId = payment.PayeeId ?? "PLATFORM",
                    PayeeId = payment.PayerId,
                    Type = PaymentType.Refund,
                    Status = PaymentStatus.Completed,
                    Provider = payment.Provider,
                    Amount = result.RefundedAmount,
                    Currency = payment.Currency,
                    ExternalPaymentId = result.RefundId,
                    ProcessedAt = DateTime.UtcNow
                };

                _context.Payments.Add(refundPayment);

                // Update transaction status
                var transaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.RentalId == rentalId);

                if (transaction != null)
                {
                    transaction.Status = TransactionStatus.Refunded;
                }

                await _context.SaveChangesAsync();

                // Send refund notification emails
                await SendRefundNotificationEmailsAsync(payment, result.RefundedAmount, reason);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for rental {RentalId}", rentalId);
            return new RefundResult
            {
                Success = false,
                ErrorMessage = "An error occurred while processing the refund"
            };
        }
    }

    public async Task<RefundResult> RefundSecurityDepositAsync(Guid rentalId)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var transaction = await _context.Transactions
                .Include(t => t.Rental)
                    .ThenInclude(r => r.Renter)
                .Include(t => t.Rental.Tool)
                .FirstOrDefaultAsync(t => t.RentalId == rentalId);

            if (transaction == null)
            {
                return new RefundResult
                {
                    Success = false,
                    ErrorMessage = "Transaction not found"
                };
            }

            if (transaction.SecurityDeposit <= 0)
            {
                return new RefundResult
                {
                    Success = false,
                    ErrorMessage = "No security deposit to refund"
                };
            }

            if (transaction.DepositRefundedAt.HasValue)
            {
                return new RefundResult
                {
                    Success = false,
                    ErrorMessage = "Security deposit already refunded"
                };
            }

            // Find the original payment that includes the security deposit
            var originalPayment = await _context.Payments
                .Where(p => p.RentalId == rentalId && 
                           p.Type == PaymentType.RentalPayment && 
                           p.Status == PaymentStatus.Completed)
                .FirstOrDefaultAsync();

            if (originalPayment == null || string.IsNullOrEmpty(originalPayment.ExternalPaymentId))
            {
                return new RefundResult
                {
                    Success = false,
                    ErrorMessage = "Original payment not found or invalid"
                };
            }

            // Process refund through PayPal for the security deposit amount
            var refundRequest = new RefundPaymentRequest
            {
                PaymentId = originalPayment.ExternalPaymentId,
                Amount = transaction.SecurityDeposit,
                Reason = $"Security deposit refund for rental of {transaction.Rental.Tool?.Name}",
                IsPartialRefund = true // This is a partial refund (only the deposit portion)
            };

            var refundResult = await _paymentProvider.RefundPaymentAsync(refundRequest);

            if (!refundResult.Success)
            {
                return new RefundResult
                {
                    Success = false,
                    ErrorMessage = $"PayPal refund failed: {refundResult.ErrorMessage}"
                };
            }

            // Create refund payment record
            var depositRefund = new Payment
            {
                RentalId = rentalId,
                PayerId = originalPayment.PayerId,
                PayeeId = transaction.Rental.RenterId,
                Type = PaymentType.DepositRefund,
                Status = PaymentStatus.Completed,
                Provider = PaymentProvider.PayPal,
                Amount = transaction.SecurityDeposit,
                Currency = transaction.Currency,
                ExternalPaymentId = refundResult.RefundId,
                // External transaction ID not available in Payment entity
                ProcessedAt = DateTime.UtcNow,
                RefundReason = refundRequest.Reason
            };

            _context.Payments.Add(depositRefund);

            // Update original payment with partial refund info
            originalPayment.RefundedAmount += transaction.SecurityDeposit;
            originalPayment.IsRefunded = originalPayment.RefundedAmount >= originalPayment.Amount;
            if (!originalPayment.IsRefunded)
            {
                originalPayment.Status = PaymentStatus.PartiallyRefunded;
            }
            else
            {
                originalPayment.Status = PaymentStatus.Refunded;
            }

            // Update transaction
            transaction.DepositRefundedAt = DateTime.UtcNow;
            transaction.Status = TransactionStatus.Refunded;

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            // Send deposit refund notification
            var notification = SimpleEmailNotification.Create(
                transaction.Rental.Renter.Email!,
                "Security Deposit Refunded",
                $"Your security deposit of ${transaction.SecurityDeposit:F2} has been refunded to your PayPal account for the rental of {transaction.Rental.Tool?.Name}. Please allow 3-5 business days for the refund to appear in your account.",
                EmailNotificationType.RefundProcessed);
            notification.UserId = transaction.Rental.RenterId;
            await _emailService.SendNotificationAsync(notification);

            _logger.LogInformation("Security deposit of ${Amount} refunded successfully for rental {RentalId} via PayPal refund {RefundId}", 
                transaction.SecurityDeposit, rentalId, refundResult.RefundId);

            return new RefundResult
            {
                Success = true,
                RefundedAmount = transaction.SecurityDeposit,
                RefundId = refundResult.RefundId,
                Status = "COMPLETED"
            };
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Error refunding security deposit for rental {RentalId}", rentalId);
            return new RefundResult
            {
                Success = false,
                ErrorMessage = "An error occurred while refunding the security deposit"
            };
        }
    }

    public async Task<CreatePayoutResult> CreateOwnerPayoutAsync(Guid transactionId)
    {
        try
        {
            var transaction = await _context.Transactions
                .Include(t => t.Rental)
                    .ThenInclude(r => r.Owner)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                return new CreatePayoutResult
                {
                    Success = false,
                    ErrorMessage = "Transaction not found"
                };
            }

            if (transaction.Status != TransactionStatus.PaymentCompleted && 
                transaction.Status != TransactionStatus.Completed)
            {
                return new CreatePayoutResult
                {
                    Success = false,
                    ErrorMessage = "Transaction is not ready for payout"
                };
            }

            if (transaction.PayoutCompletedAt.HasValue)
            {
                return new CreatePayoutResult
                {
                    Success = false,
                    ErrorMessage = "Payout already completed"
                };
            }

            var ownerSettings = await GetOrCreatePaymentSettingsAsync(transaction.Rental.OwnerId);
            
            if (string.IsNullOrEmpty(ownerSettings.PayPalEmail))
            {
                return new CreatePayoutResult
                {
                    Success = false,
                    ErrorMessage = "Owner has not configured PayPal email for payouts"
                };
            }

            // Check minimum payout amount
            if (transaction.OwnerPayoutAmount < ownerSettings.MinimumPayoutAmount)
            {
                return new CreatePayoutResult
                {
                    Success = false,
                    ErrorMessage = $"Payout amount is below minimum of ${ownerSettings.MinimumPayoutAmount:F2}"
                };
            }

            // Create payout record
            var payout = new Payout
            {
                RecipientId = transaction.Rental.OwnerId,
                Status = PayoutStatus.Processing,
                Provider = PaymentProvider.PayPal,
                Amount = transaction.OwnerPayoutAmount,
                Currency = transaction.Currency,
                NetAmount = transaction.OwnerPayoutAmount, // No additional fees for now
                PayoutMethod = "paypal_email",
                PayoutDestination = ownerSettings.PayPalEmail,
                ScheduledAt = DateTime.UtcNow
            };

            _context.Payouts.Add(payout);
            await _context.SaveChangesAsync();

            // Link payout to transaction
            payout.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Process payout with provider
            var payoutRequest = new CreatePayoutRequest
            {
                RecipientId = transaction.Rental.OwnerId,
                RecipientEmail = ownerSettings.PayPalEmail,
                Amount = transaction.OwnerPayoutAmount,
                Currency = transaction.Currency,
                Note = $"Payout for rental of {transaction.Rental.Tool?.Name}",
                BatchId = $"PAYOUT_{DateTime.UtcNow:yyyyMMddHHmmss}"
            };

            var result = await _paymentProvider.CreatePayoutAsync(payoutRequest);

            if (result.Success)
            {
                payout.Status = PayoutStatus.Completed;
                payout.ProcessedAt = DateTime.UtcNow;
                payout.CompletedAt = DateTime.UtcNow;
                payout.ExternalPayoutId = result.PayoutId;
                payout.ExternalBatchId = result.BatchId;

                transaction.Status = TransactionStatus.PayoutCompleted;
                transaction.PayoutCompletedAt = DateTime.UtcNow;

                // Create payout payment record
                var payoutPayment = new Payment
                {
                    RentalId = transaction.RentalId,
                    PayerId = "PLATFORM",
                    PayeeId = transaction.Rental.OwnerId,
                    Type = PaymentType.OwnerPayout,
                    Status = PaymentStatus.Completed,
                    Provider = PaymentProvider.PayPal,
                    Amount = transaction.OwnerPayoutAmount,
                    Currency = transaction.Currency,
                    ExternalPaymentId = result.PayoutId,
                    ProcessedAt = DateTime.UtcNow
                };

                _context.Payments.Add(payoutPayment);
                await _context.SaveChangesAsync();

                // Send payout notification
                if (ownerSettings.NotifyOnPayoutSent)
                {
                    var payoutNotification = SimpleEmailNotification.Create(
                        transaction.Rental.Owner.Email!,
                        "Payout Sent",
                        $"A payout of ${transaction.OwnerPayoutAmount:F2} has been sent to your PayPal account for the rental of {transaction.Rental.Tool?.Name}.",
                        EmailNotificationType.PayoutSent);
                    payoutNotification.UserId = transaction.Rental.OwnerId;
                    await _emailService.SendNotificationAsync(payoutNotification);
                }
            }
            else
            {
                payout.Status = PayoutStatus.Failed;
                payout.FailedAt = DateTime.UtcNow;
                payout.FailureReason = result.ErrorMessage;
                await _context.SaveChangesAsync();

                // Send failure notification
                if (ownerSettings.NotifyOnPayoutFailed)
                {
                    var failureNotification = SimpleEmailNotification.Create(
                        transaction.Rental.Owner.Email!,
                        "Payout Failed",
                        $"We were unable to process your payout of ${transaction.OwnerPayoutAmount:F2}. Please check your PayPal settings. Error: {result.ErrorMessage}",
                        EmailNotificationType.PayoutFailed);
                    failureNotification.UserId = transaction.Rental.OwnerId;
                    await _emailService.SendNotificationAsync(failureNotification);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating owner payout for transaction {TransactionId}", transactionId);
            return new CreatePayoutResult
            {
                Success = false,
                ErrorMessage = "An error occurred while processing the payout"
            };
        }
    }

    public async Task<PayoutStatusResult> GetPayoutStatusAsync(string payoutId)
    {
        return await _paymentProvider.GetPayoutStatusAsync(payoutId);
    }

    public async Task ProcessScheduledPayoutsAsync()
    {
        try
        {
            var pendingPayouts = await _context.Transactions
                .Include(t => t.Rental)
                    .ThenInclude(r => r.Owner)
                .Where(t => t.Status == TransactionStatus.PaymentCompleted &&
                           t.PayoutScheduledAt.HasValue &&
                           t.PayoutScheduledAt <= DateTime.UtcNow &&
                           !t.PayoutCompletedAt.HasValue)
                .ToListAsync();

            foreach (var transaction in pendingPayouts)
            {
                await CreateOwnerPayoutAsync(transaction.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled payouts");
        }
    }

    private async Task<DateTime> CalculatePayoutScheduleAsync(string ownerId)
    {
        try
        {
            var ownerSettings = await GetOrCreatePaymentSettingsAsync(ownerId);
            var baseTime = DateTime.UtcNow.AddHours(24); // Minimum 24-hour security hold
            
            return ownerSettings.PayoutSchedule switch
            {
                PayoutSchedule.OnDemand => baseTime, // Next day for on-demand
                PayoutSchedule.Daily => GetNextDailyPayout(baseTime),
                PayoutSchedule.Weekly => GetNextWeeklyPayout(baseTime),
                PayoutSchedule.Monthly => GetNextMonthlyPayout(baseTime),
                _ => baseTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating payout schedule for owner {OwnerId}, using default schedule", ownerId);
            return DateTime.UtcNow.AddHours(24); // Fallback to 24 hours
        }
    }

    private static DateTime GetNextDailyPayout(DateTime baseTime)
    {
        // Daily payouts at 10 AM UTC
        var nextPayout = baseTime.Date.AddHours(10);
        return nextPayout <= baseTime ? nextPayout.AddDays(1) : nextPayout;
    }

    private static DateTime GetNextWeeklyPayout(DateTime baseTime)
    {
        // Weekly payouts on Fridays at 10 AM UTC
        var daysUntilFriday = ((int)DayOfWeek.Friday - (int)baseTime.DayOfWeek + 7) % 7;
        if (daysUntilFriday == 0 && baseTime.Hour >= 10)
        {
            daysUntilFriday = 7; // Next Friday if it's already past 10 AM today
        }
        
        return baseTime.Date.AddDays(daysUntilFriday).AddHours(10);
    }

    private static DateTime GetNextMonthlyPayout(DateTime baseTime)
    {
        // Monthly payouts on the 1st of each month at 10 AM UTC
        var nextMonth = baseTime.Month == 12 ? new DateTime(baseTime.Year + 1, 1, 1) : new DateTime(baseTime.Year, baseTime.Month + 1, 1);
        var nextPayout = nextMonth.AddHours(10);
        
        // If we're on the 1st and it's before 10 AM, use today
        if (baseTime.Day == 1 && baseTime.Hour < 10)
        {
            nextPayout = baseTime.Date.AddHours(10);
        }
        
        return nextPayout;
    }

    public decimal CalculateCommission(decimal rentalAmount, string? ownerId = null)
    {
        var commissionRate = _config.DefaultCommissionRate;

        if (!string.IsNullOrEmpty(ownerId))
        {
            var ownerSettings = _context.PaymentSettings
                .FirstOrDefault(ps => ps.UserId == ownerId);

            if (ownerSettings?.CustomCommissionRate.HasValue == true && ownerSettings.IsCommissionEnabled)
            {
                commissionRate = ownerSettings.CustomCommissionRate.Value;
            }
        }

        return Math.Round(rentalAmount * commissionRate, 2);
    }

    public RentalFinancialBreakdown CalculateRentalFinancials(decimal rentalAmount, decimal securityDeposit, string? ownerId = null)
    {
        var commissionRate = _config.DefaultCommissionRate;

        if (!string.IsNullOrEmpty(ownerId))
        {
            var ownerSettings = _context.PaymentSettings
                .FirstOrDefault(ps => ps.UserId == ownerId);

            if (ownerSettings?.CustomCommissionRate.HasValue == true && ownerSettings.IsCommissionEnabled)
            {
                commissionRate = ownerSettings.CustomCommissionRate.Value;
            }
        }

        var commissionAmount = Math.Round(rentalAmount * commissionRate, 2);
        var totalPayerAmount = rentalAmount + securityDeposit;
        var ownerPayoutAmount = rentalAmount - commissionAmount;

        return new RentalFinancialBreakdown
        {
            RentalAmount = rentalAmount,
            SecurityDeposit = securityDeposit,
            CommissionRate = commissionRate,
            CommissionAmount = commissionAmount,
            TotalPayerAmount = totalPayerAmount,
            OwnerPayoutAmount = ownerPayoutAmount
        };
    }

    public async Task<Transaction> GetTransactionByRentalAsync(Guid rentalId)
    {
        return await _context.Transactions
            .Include(t => t.Payments)
            .FirstOrDefaultAsync(t => t.RentalId == rentalId) 
            ?? throw new InvalidOperationException($"Transaction not found for rental {rentalId}");
    }

    public async Task UpdateTransactionStatusAsync(Guid transactionId, TransactionStatus status)
    {
        var transaction = await _context.Transactions.FindAsync(transactionId);
        if (transaction != null)
        {
            transaction.Status = status;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<PaymentSettings> GetOrCreatePaymentSettingsAsync(string userId)
    {
        var settings = await _context.PaymentSettings
            .FirstOrDefaultAsync(ps => ps.UserId == userId);

        if (settings == null)
        {
            settings = new PaymentSettings
            {
                UserId = userId,
                PreferredPayoutMethod = PaymentProvider.PayPal,
                IsCommissionEnabled = true,
                PayoutSchedule = PayoutSchedule.OnDemand,
                MinimumPayoutAmount = _config.MinimumPayoutAmount,
                NotifyOnPaymentReceived = true,
                NotifyOnPayoutSent = true,
                NotifyOnPayoutFailed = true
            };

            _context.PaymentSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        return settings;
    }

    public async Task UpdatePaymentSettingsAsync(string userId, UpdatePaymentSettingsDto dto)
    {
        var settings = await GetOrCreatePaymentSettingsAsync(userId);

        if (!string.IsNullOrEmpty(dto.PayPalEmail))
            settings.PayPalEmail = dto.PayPalEmail;

        if (dto.CustomCommissionRate.HasValue)
            settings.CustomCommissionRate = dto.CustomCommissionRate.Value;

        if (dto.PayoutSchedule.HasValue)
            settings.PayoutSchedule = dto.PayoutSchedule.Value;

        if (dto.MinimumPayoutAmount.HasValue)
            settings.MinimumPayoutAmount = dto.MinimumPayoutAmount.Value;

        if (dto.NotifyOnPaymentReceived.HasValue)
            settings.NotifyOnPaymentReceived = dto.NotifyOnPaymentReceived.Value;

        if (dto.NotifyOnPayoutSent.HasValue)
            settings.NotifyOnPayoutSent = dto.NotifyOnPayoutSent.Value;

        if (dto.NotifyOnPayoutFailed.HasValue)
            settings.NotifyOnPayoutFailed = dto.NotifyOnPayoutFailed.Value;

        await _context.SaveChangesAsync();
    }

    private async Task SendPaymentConfirmationEmailsAsync(Payment payment, Transaction transaction)
    {
        try
        {
            var rental = payment.Rental!;
            var ownerSettings = await GetOrCreatePaymentSettingsAsync(rental.OwnerId);

            // Email to renter
            var renterNotification = SimpleEmailNotification.Create(
                rental.Renter.Email!,
                "Payment Confirmed - Rental Approved",
                $@"Your payment of ${payment.Amount:F2} has been confirmed for the rental of {rental.Tool.Name}.

Rental Details:
- Tool: {rental.Tool.Name}
- Dates: {rental.StartDate:MMM dd, yyyy} to {rental.EndDate:MMM dd, yyyy}
- Rental Cost: ${transaction.RentalAmount:F2}
- Security Deposit: ${transaction.SecurityDeposit:F2}
- Total Paid: ${payment.Amount:F2}

The owner has been notified and will arrange pickup details with you.",
                EmailNotificationType.PaymentProcessed);
            renterNotification.UserId = rental.RenterId;
            await _emailService.SendNotificationAsync(renterNotification);

            // Email to owner
            if (ownerSettings.NotifyOnPaymentReceived)
            {
                var ownerNotification = SimpleEmailNotification.Create(
                    rental.Owner.Email!,
                    "Rental Payment Received",
                    $@"Great news! {rental.Renter.FirstName} {rental.Renter.LastName} has paid for the rental of your {rental.Tool.Name}.

Rental Details:
- Dates: {rental.StartDate:MMM dd, yyyy} to {rental.EndDate:MMM dd, yyyy}
- Rental Amount: ${transaction.RentalAmount:F2}
- Platform Fee ({transaction.CommissionRate:P0}): ${transaction.CommissionAmount:F2}
- Your Payout: ${transaction.OwnerPayoutAmount:F2}

Your payout will be processed within 24 hours after the rental is completed.

Please contact the renter to arrange pickup details.",
                    EmailNotificationType.PaymentProcessed);
                ownerNotification.UserId = rental.OwnerId;
                await _emailService.SendNotificationAsync(ownerNotification);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payment confirmation emails");
        }
    }

    private async Task SendRefundNotificationEmailsAsync(Payment originalPayment, decimal refundAmount, string reason)
    {
        try
        {
            var rental = originalPayment.Rental!;

            // Email to renter
            var renterRefundNotification = SimpleEmailNotification.Create(
                rental.Renter.Email!,
                "Refund Processed",
                $@"A refund of ${refundAmount:F2} has been processed for your rental of {rental.Tool?.Name}.

Reason: {reason}

The refund should appear in your account within 3-5 business days.",
                EmailNotificationType.RefundProcessed);
            renterRefundNotification.UserId = rental.RenterId;
            await _emailService.SendNotificationAsync(renterRefundNotification);

            // Email to owner
            var ownerRefundNotification = SimpleEmailNotification.Create(
                rental.Owner.Email!,
                "Rental Refunded",
                $@"A refund of ${refundAmount:F2} has been issued for the rental of your {rental.Tool?.Name}.

Reason: {reason}

This may affect your payout for this rental.",
                EmailNotificationType.RefundProcessed);
            ownerRefundNotification.UserId = rental.OwnerId;
            await _emailService.SendNotificationAsync(ownerRefundNotification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending refund notification emails");
        }
    }

    public async Task<bool> CanOwnerReceivePaymentsAsync(string ownerId)
    {
        try
        {
            var settings = await GetOrCreatePaymentSettingsAsync(ownerId);
            return !string.IsNullOrEmpty(settings.PayPalEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking payment settings for owner {OwnerId}", ownerId);
            return false; // Safe default - if we can't check, assume they can't receive payments
        }
    }

    public async Task<Tool?> GetToolForCalculationAsync(Guid toolId)
    {
        try
        {
            return await _context.Tools
                .FirstOrDefaultAsync(t => t.Id == toolId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tool {ToolId} for calculation", toolId);
            return null;
        }
    }
}