using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToolsSharing.Core.DTOs.Payment;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models.EmailNotifications;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services;

public class PaymentReceiptService : IPaymentReceiptService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailNotificationService _emailService;
    private readonly ILogger<PaymentReceiptService> _logger;

    public PaymentReceiptService(
        ApplicationDbContext context,
        IEmailNotificationService emailService,
        ILogger<PaymentReceiptService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<GenerateReceiptResult> GeneratePaymentReceiptAsync(Guid paymentId)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Tool)
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Owner)
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Renter)
                .Include(p => p.Payer)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                return new GenerateReceiptResult
                {
                    Success = false,
                    ErrorMessage = "Payment not found"
                };
            }

            var receiptNumber = GenerateReceiptNumber(ReceiptType.Payment, payment.CreatedAt);
            var downloadUrl = $"/api/receipts/payment/{paymentId}/download";

            _logger.LogInformation("Generated payment receipt {ReceiptNumber} for payment {PaymentId}", 
                receiptNumber, paymentId);

            return new GenerateReceiptResult
            {
                Success = true,
                ReceiptId = paymentId,
                ReceiptNumber = receiptNumber,
                DownloadUrl = downloadUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payment receipt for payment {PaymentId}", paymentId);
            
            return new GenerateReceiptResult
            {
                Success = false,
                ErrorMessage = "An error occurred while generating the receipt"
            };
        }
    }

    public async Task<GenerateReceiptResult> GeneratePayoutReceiptAsync(Guid payoutId)
    {
        try
        {
            var payout = await _context.Payouts
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Tool)
                .Include(p => p.Recipient)
                .FirstOrDefaultAsync(p => p.Id == payoutId);

            if (payout == null)
            {
                return new GenerateReceiptResult
                {
                    Success = false,
                    ErrorMessage = "Payout not found"
                };
            }

            var receiptNumber = GenerateReceiptNumber(ReceiptType.Payout, payout.CreatedAt);
            var downloadUrl = $"/api/receipts/payout/{payoutId}/download";

            _logger.LogInformation("Generated payout receipt {ReceiptNumber} for payout {PayoutId}", 
                receiptNumber, payoutId);

            return new GenerateReceiptResult
            {
                Success = true,
                ReceiptId = payoutId,
                ReceiptNumber = receiptNumber,
                DownloadUrl = downloadUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payout receipt for payout {PayoutId}", payoutId);
            
            return new GenerateReceiptResult
            {
                Success = false,
                ErrorMessage = "An error occurred while generating the receipt"
            };
        }
    }

    public async Task<GenerateReceiptResult> GenerateTransactionSummaryAsync(Guid rentalId)
    {
        try
        {
            var rental = await _context.Rentals
                .Include(r => r.Tool)
                .Include(r => r.Owner)
                .Include(r => r.Renter)
                .FirstOrDefaultAsync(r => r.Id == rentalId);

            if (rental == null)
            {
                return new GenerateReceiptResult
                {
                    Success = false,
                    ErrorMessage = "Rental not found"
                };
            }

            var receiptNumber = GenerateReceiptNumber(ReceiptType.TransactionSummary, rental.CreatedAt);
            var downloadUrl = $"/api/receipts/transaction/{rentalId}/download";

            _logger.LogInformation("Generated transaction summary {ReceiptNumber} for rental {RentalId}", 
                receiptNumber, rentalId);

            return new GenerateReceiptResult
            {
                Success = true,
                ReceiptId = rentalId,
                ReceiptNumber = receiptNumber,
                DownloadUrl = downloadUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating transaction summary for rental {RentalId}", rentalId);
            
            return new GenerateReceiptResult
            {
                Success = false,
                ErrorMessage = "An error occurred while generating the receipt"
            };
        }
    }

    public async Task<PaymentReceiptDto> GetPaymentReceiptAsync(Guid paymentId, string userId)
    {
        var payment = await _context.Payments
            .Include(p => p.Rental)
                .ThenInclude(r => r.Tool)
            .Include(p => p.Rental)
                .ThenInclude(r => r.Owner)
            .Include(p => p.Rental)
                .ThenInclude(r => r.Renter)
            .Include(p => p.Payer)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null)
            throw new ArgumentException("Payment not found");

        // Verify user access
        if (payment.PayerId != userId && payment.Rental!.OwnerId != userId)
            throw new UnauthorizedAccessException("Access denied");

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.RentalId == payment.RentalId);

        return await MapToPaymentReceiptAsync(payment, transaction);
    }

    public async Task<PayoutReceiptDto> GetPayoutReceiptAsync(Guid payoutId, string userId)
    {
        var payout = await _context.Payouts
            .Include(p => p.Rental)
                .ThenInclude(r => r.Tool)
            .Include(p => p.Rental)
                .ThenInclude(r => r.Owner)
            .Include(p => p.Rental)
                .ThenInclude(r => r.Renter)
            .Include(p => p.Recipient)
            .FirstOrDefaultAsync(p => p.Id == payoutId);

        if (payout == null)
            throw new ArgumentException("Payout not found");

        // Verify user access
        if (payout.RecipientId != userId)
            throw new UnauthorizedAccessException("Access denied");

        return await MapToPayoutReceiptAsync(payout);
    }

    public async Task<TransactionSummaryReceiptDto> GetTransactionSummaryAsync(Guid rentalId, string userId)
    {
        var rental = await _context.Rentals
            .Include(r => r.Tool)
            .Include(r => r.Owner)
            .Include(r => r.Renter)
            .FirstOrDefaultAsync(r => r.Id == rentalId);

        if (rental == null)
            throw new ArgumentException("Rental not found");

        // Verify user access
        if (rental.OwnerId != userId && rental.RenterId != userId)
            throw new UnauthorizedAccessException("Access denied");

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.RentalId == rentalId);

        var payments = await _context.Payments
            .Where(p => p.RentalId == rentalId)
            .ToListAsync();

        var payouts = await _context.Payouts
            .Where(p => p.RentalId == rentalId)
            .ToListAsync();

        return await MapToTransactionSummaryReceiptAsync(rental, transaction, payments, payouts);
    }

    public async Task<SendReceiptResult> SendPaymentReceiptEmailAsync(Guid paymentId, string recipientEmail)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Tool)
                .Include(p => p.Payer)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                return new SendReceiptResult
                {
                    Success = false,
                    ErrorMessage = "Payment not found"
                };
            }

            var receiptNumber = GenerateReceiptNumber(ReceiptType.Payment, payment.CreatedAt);
            var downloadUrl = $"/api/receipts/payment/{paymentId}/download";

            var emailContent = GeneratePaymentReceiptEmailContent(payment, receiptNumber, downloadUrl);

            var notification = SimpleEmailNotification.Create(
                recipientEmail,
                $"Payment Receipt - {receiptNumber}",
                emailContent,
                Core.Common.Models.EmailNotificationType.PaymentProcessed);

            await _emailService.SendNotificationAsync(notification);

            _logger.LogInformation("Sent payment receipt {ReceiptNumber} to {Email}", receiptNumber, recipientEmail);

            return new SendReceiptResult
            {
                Success = true,
                MessageId = notification.Id.ToString(),
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payment receipt email for payment {PaymentId}", paymentId);
            
            return new SendReceiptResult
            {
                Success = false,
                ErrorMessage = "An error occurred while sending the receipt"
            };
        }
    }

    public async Task<SendReceiptResult> SendPayoutReceiptEmailAsync(Guid payoutId, string recipientEmail)
    {
        try
        {
            var payout = await _context.Payouts
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Tool)
                .Include(p => p.Recipient)
                .FirstOrDefaultAsync(p => p.Id == payoutId);

            if (payout == null)
            {
                return new SendReceiptResult
                {
                    Success = false,
                    ErrorMessage = "Payout not found"
                };
            }

            var receiptNumber = GenerateReceiptNumber(ReceiptType.Payout, payout.CreatedAt);
            var downloadUrl = $"/api/receipts/payout/{payoutId}/download";

            var emailContent = GeneratePayoutReceiptEmailContent(payout, receiptNumber, downloadUrl);

            var notification = SimpleEmailNotification.Create(
                recipientEmail,
                $"Payout Receipt - {receiptNumber}",
                emailContent,
                Core.Common.Models.EmailNotificationType.PayoutSent);

            await _emailService.SendNotificationAsync(notification);

            _logger.LogInformation("Sent payout receipt {ReceiptNumber} to {Email}", receiptNumber, recipientEmail);

            return new SendReceiptResult
            {
                Success = true,
                MessageId = notification.Id.ToString(),
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payout receipt email for payout {PayoutId}", payoutId);
            
            return new SendReceiptResult
            {
                Success = false,
                ErrorMessage = "An error occurred while sending the receipt"
            };
        }
    }

    public async Task<SendReceiptResult> SendTransactionSummaryEmailAsync(Guid rentalId, string recipientEmail)
    {
        try
        {
            var rental = await _context.Rentals
                .Include(r => r.Tool)
                .Include(r => r.Owner)
                .Include(r => r.Renter)
                .FirstOrDefaultAsync(r => r.Id == rentalId);

            if (rental == null)
            {
                return new SendReceiptResult
                {
                    Success = false,
                    ErrorMessage = "Rental not found"
                };
            }

            var receiptNumber = GenerateReceiptNumber(ReceiptType.TransactionSummary, rental.CreatedAt);
            var downloadUrl = $"/api/receipts/transaction/{rentalId}/download";

            var emailContent = GenerateTransactionSummaryEmailContent(rental, receiptNumber, downloadUrl);

            var notification = SimpleEmailNotification.Create(
                recipientEmail,
                $"Transaction Summary - {receiptNumber}",
                emailContent,
                Core.Common.Models.EmailNotificationType.GeneralNotification);

            await _emailService.SendNotificationAsync(notification);

            _logger.LogInformation("Sent transaction summary {ReceiptNumber} to {Email}", receiptNumber, recipientEmail);

            return new SendReceiptResult
            {
                Success = true,
                MessageId = notification.Id.ToString(),
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending transaction summary email for rental {RentalId}", rentalId);
            
            return new SendReceiptResult
            {
                Success = false,
                ErrorMessage = "An error occurred while sending the receipt"
            };
        }
    }

    public async Task<ReceiptListDto> GetUserReceiptsAsync(string userId, ReceiptFilter? filter = null)
    {
        var receipts = new List<ReceiptSummaryDto>();

        // Get payment receipts
        var paymentsQuery = _context.Payments
            .Include(p => p.Rental)
                .ThenInclude(r => r.Tool)
            .Where(p => p.PayerId == userId || p.Rental!.OwnerId == userId);

        if (filter?.Type == ReceiptType.Payment || filter?.Type == null)
        {
            var payments = await paymentsQuery.ToListAsync();
            
            foreach (var payment in payments)
            {
                if (ShouldIncludeInFilter(payment.CreatedAt, payment.Amount, filter))
                {
                    receipts.Add(new ReceiptSummaryDto
                    {
                        Id = payment.Id,
                        ReceiptNumber = GenerateReceiptNumber(ReceiptType.Payment, payment.CreatedAt),
                        Type = ReceiptType.Payment,
                        TypeDescription = GetReceiptTypeDescription(ReceiptType.Payment),
                        GeneratedAt = payment.CreatedAt,
                        Amount = payment.Amount,
                        Currency = payment.Currency,
                        Description = $"Payment for {payment.Rental!.Tool!.Name}",
                        DownloadUrl = $"/api/receipts/payment/{payment.Id}/download",
                        Status = payment.Status.ToString()
                    });
                }
            }
        }

        // Get payout receipts
        if (filter?.Type == ReceiptType.Payout || filter?.Type == null)
        {
            var payouts = await _context.Payouts
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Tool)
                .Where(p => p.OwnerId == userId)
                .ToListAsync();

            foreach (var payout in payouts)
            {
                if (ShouldIncludeInFilter(payout.CreatedAt, payout.Amount, filter))
                {
                    receipts.Add(new ReceiptSummaryDto
                    {
                        Id = payout.Id,
                        ReceiptNumber = GenerateReceiptNumber(ReceiptType.Payout, payout.CreatedAt),
                        Type = ReceiptType.Payout,
                        TypeDescription = GetReceiptTypeDescription(ReceiptType.Payout),
                        GeneratedAt = payout.CreatedAt,
                        Amount = payout.Amount,
                        Currency = payout.Currency,
                        Description = $"Payout for {payout.Rental!.Tool!.Name}",
                        DownloadUrl = $"/api/receipts/payout/{payout.Id}/download",
                        Status = payout.Status.ToString()
                    });
                }
            }
        }

        // Sort by date descending
        receipts = receipts.OrderByDescending(r => r.GeneratedAt).ToList();

        // Apply search filter if provided
        if (!string.IsNullOrEmpty(filter?.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.ToLower();
            receipts = receipts.Where(r => 
                r.Description.ToLower().Contains(searchTerm) ||
                r.ReceiptNumber.ToLower().Contains(searchTerm) ||
                r.TypeDescription.ToLower().Contains(searchTerm)
            ).ToList();
        }

        const int pageSize = 25;
        var totalCount = receipts.Count;
        var pageNumber = 1; // TODO: Add pagination parameters
        
        receipts = receipts.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return new ReceiptListDto
        {
            Receipts = receipts,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            HasNextPage = totalCount > pageNumber * pageSize,
            HasPreviousPage = pageNumber > 1
        };
    }

    public async Task<byte[]> GenerateReceiptPdfAsync(Guid receiptId, string userId)
    {
        // Placeholder for PDF generation
        // This would use a PDF library like iTextSharp or similar
        var content = "PDF receipt content would be generated here";
        return Encoding.UTF8.GetBytes(content);
    }

    public async Task<string> GetReceiptDownloadUrlAsync(Guid receiptId, string userId)
    {
        // Generate secure download URL
        // This could include time-limited tokens for security
        return $"/api/receipts/{receiptId}/download?token={Guid.NewGuid()}";
    }

    public async Task<ReceiptTemplateDto> GetReceiptTemplateAsync(ReceiptType receiptType)
    {
        return new ReceiptTemplateDto
        {
            Type = receiptType,
            Name = GetReceiptTypeDescription(receiptType),
            HtmlTemplate = GetDefaultHtmlTemplate(receiptType),
            EmailTemplate = GetDefaultEmailTemplate(receiptType),
            DefaultData = GetDefaultTemplateData(receiptType),
            RequiredFields = GetRequiredFields(receiptType),
            LastModified = DateTime.UtcNow,
            ModifiedBy = "System"
        };
    }

    public async Task UpdateReceiptTemplateAsync(ReceiptType receiptType, ReceiptTemplateDto template)
    {
        // Placeholder for template update functionality
        _logger.LogInformation("Updated receipt template for type {ReceiptType}", receiptType);
    }

    // Helper methods
    private string GenerateReceiptNumber(ReceiptType type, DateTime date)
    {
        var prefix = type switch
        {
            ReceiptType.Payment => "PAY",
            ReceiptType.Payout => "OUT",
            ReceiptType.TransactionSummary => "TXN",
            ReceiptType.Refund => "REF",
            ReceiptType.Commission => "COM",
            _ => "REC"
        };

        var datePart = date.ToString("yyyyMMdd");
        var timePart = date.ToString("HHmmss");
        var random = new Random().Next(1000, 9999);

        return $"{prefix}-{datePart}-{timePart}-{random}";
    }

    private string GetReceiptTypeDescription(ReceiptType type) => type switch
    {
        ReceiptType.Payment => "Payment Receipt",
        ReceiptType.Payout => "Payout Receipt",
        ReceiptType.TransactionSummary => "Transaction Summary",
        ReceiptType.Refund => "Refund Receipt",
        ReceiptType.Commission => "Commission Receipt",
        _ => "Receipt"
    };

    private bool ShouldIncludeInFilter(DateTime date, decimal amount, ReceiptFilter? filter)
    {
        if (filter == null) return true;

        if (filter.FromDate.HasValue && date < filter.FromDate.Value) return false;
        if (filter.ToDate.HasValue && date > filter.ToDate.Value) return false;
        if (filter.MinAmount.HasValue && amount < filter.MinAmount.Value) return false;
        if (filter.MaxAmount.HasValue && amount > filter.MaxAmount.Value) return false;

        return true;
    }

    private async Task<PaymentReceiptDto> MapToPaymentReceiptAsync(Payment payment, Transaction? transaction)
    {
        var receiptNumber = GenerateReceiptNumber(ReceiptType.Payment, payment.CreatedAt);
        var rental = payment.Rental!;
        var tool = rental.Tool!;

        return new PaymentReceiptDto
        {
            Id = payment.Id,
            ReceiptNumber = receiptNumber,
            GeneratedAt = DateTime.UtcNow,
            Type = ReceiptType.Payment,
            PaymentId = payment.Id,
            ExternalPaymentId = payment.ExternalPaymentId ?? "",
            PaymentType = payment.Type,
            PaymentStatus = payment.Status,
            PaymentDate = payment.ProcessedAt ?? payment.CreatedAt,
            Provider = payment.Provider,
            Customer = new CustomerInfoDto
            {
                Id = payment.PayerId,
                Name = payment.Payer?.UserName ?? "Unknown",
                Email = payment.Payer?.Email ?? ""
            },
            Recipient = new CustomerInfoDto
            {
                Id = rental.OwnerId,
                Name = rental.Owner?.UserName ?? "Unknown",
                Email = rental.Owner?.Email ?? ""
            },
            Rental = new RentalReceiptInfoDto
            {
                Id = rental.Id,
                ToolName = tool.Name,
                ToolDescription = tool.Description,
                ToolCategory = tool.Category,
                StartDate = rental.StartDate,
                EndDate = rental.EndDate,
                RentalDays = (rental.EndDate - rental.StartDate).Days + 1,
                DailyRate = tool.DailyRate,
                WeeklyRate = tool.WeeklyRate,
                MonthlyRate = tool.MonthlyRate,
                RateUsed = GetRateUsed(rental, tool),
                Currency = payment.Currency
            },
            Financial = new ReceiptFinancialBreakdownDto
            {
                SubTotal = rental.TotalCost - (rental.DepositAmount),
                CommissionAmount = transaction?.CommissionAmount ?? 0m,
                CommissionRate = transaction?.CommissionRate ?? 0m,
                SecurityDeposit = rental.DepositAmount,
                TaxAmount = 0, // TODO: Calculate tax
                TotalAmount = payment.Amount,
                AmountPaid = payment.Amount,
                RefundAmount = payment.RefundedAmount,
                NetAmount = payment.Amount - payment.RefundedAmount,
                Currency = payment.Currency,
                PaymentMethod = payment.Provider.ToString()
            },
            Company = GetCompanyInfo(),
            Notes = GeneratePaymentNotes(payment, rental),
            LineItems = GeneratePaymentLineItems(rental, transaction),
            Taxes = new List<ReceiptTaxDto>(), // TODO: Add tax calculations
            InvoiceUrl = $"/invoices/payment/{payment.Id}",
            PdfUrl = $"/api/receipts/payment/{payment.Id}/download"
        };
    }

    private async Task<PayoutReceiptDto> MapToPayoutReceiptAsync(Payout payout)
    {
        var receiptNumber = GenerateReceiptNumber(ReceiptType.Payout, payout.CreatedAt);
        var rental = payout.Rental!;
        var tool = rental.Tool!;

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.RentalId == payout.RentalId);

        return new PayoutReceiptDto
        {
            Id = payout.Id,
            ReceiptNumber = receiptNumber,
            GeneratedAt = DateTime.UtcNow,
            Type = ReceiptType.Payout,
            PayoutId = payout.Id,
            ExternalPayoutId = payout.ExternalTransactionId,
            PayoutStatus = payout.Status,
            PayoutDate = payout.ProcessedAt ?? payout.CreatedAt,
            PayoutMethod = payout.PaymentProvider,
            Payee = new CustomerInfoDto
            {
                Id = payout.RecipientId,
                Name = payout.Recipient?.UserName ?? "Unknown",
                Email = payout.Recipient?.Email ?? ""
            },
            Rental = new RentalReceiptInfoDto
            {
                Id = rental.Id,
                ToolName = tool.Name,
                ToolDescription = tool.Description,
                ToolCategory = tool.Category,
                StartDate = rental.StartDate,
                EndDate = rental.EndDate,
                RentalDays = (rental.EndDate - rental.StartDate).Days + 1,
                DailyRate = tool.DailyRate,
                WeeklyRate = tool.WeeklyRate,
                MonthlyRate = tool.MonthlyRate,
                RateUsed = GetRateUsed(rental, tool),
                Currency = payout.Currency
            },
            Financial = new PayoutFinancialBreakdownDto
            {
                GrossAmount = transaction?.RentalAmount ?? rental.TotalCost,
                CommissionAmount = transaction?.CommissionAmount ?? 0m,
                CommissionRate = transaction?.CommissionRate ?? 0m,
                ProcessingFee = 0, // TODO: Calculate processing fees
                TaxWithheld = 0, // TODO: Calculate tax withholding
                NetPayoutAmount = payout.Amount,
                Currency = payout.Currency,
                PayoutMethod = payout.PaymentProvider.ToString(),
                PayoutDestination = payout.PayPalEmail
            },
            Company = GetCompanyInfo(),
            Notes = GeneratePayoutNotes(payout, rental),
            LineItems = GeneratePayoutLineItems(rental, transaction),
            Taxes = new List<ReceiptTaxDto>(), // TODO: Add tax calculations
            StatementUrl = $"/statements/payout/{payout.Id}",
            PdfUrl = $"/api/receipts/payout/{payout.Id}/download"
        };
    }

    private async Task<TransactionSummaryReceiptDto> MapToTransactionSummaryReceiptAsync(
        Rental rental, Transaction? transaction, List<Payment> payments, List<Payout> payouts)
    {
        var receiptNumber = GenerateReceiptNumber(ReceiptType.TransactionSummary, rental.CreatedAt);
        var tool = rental.Tool!;

        return new TransactionSummaryReceiptDto
        {
            Id = Guid.NewGuid(),
            ReceiptNumber = receiptNumber,
            GeneratedAt = DateTime.UtcNow,
            Type = ReceiptType.TransactionSummary,
            TransactionId = transaction?.Id ?? Guid.Empty,
            TransactionStatus = transaction?.Status ?? TransactionStatus.Pending,
            TransactionDate = rental.CreatedAt,
            Renter = new CustomerInfoDto
            {
                Id = rental.RenterId,
                Name = rental.Renter?.UserName ?? "Unknown",
                Email = rental.Renter?.Email ?? ""
            },
            Owner = new CustomerInfoDto
            {
                Id = rental.OwnerId,
                Name = rental.Owner?.UserName ?? "Unknown",
                Email = rental.Owner?.Email ?? ""
            },
            Rental = new RentalReceiptInfoDto
            {
                Id = rental.Id,
                ToolName = tool.Name,
                ToolDescription = tool.Description,
                ToolCategory = tool.Category,
                StartDate = rental.StartDate,
                EndDate = rental.EndDate,
                RentalDays = (rental.EndDate - rental.StartDate).Days + 1,
                DailyRate = tool.DailyRate,
                WeeklyRate = tool.WeeklyRate,
                MonthlyRate = tool.MonthlyRate,
                RateUsed = GetRateUsed(rental, tool),
                Currency = transaction?.Currency ?? "USD"
            },
            Financial = new TransactionFinancialSummaryDto
            {
                RentalAmount = transaction?.RentalAmount ?? rental.TotalCost,
                SecurityDeposit = transaction?.SecurityDeposit ?? rental.DepositAmount,
                CommissionAmount = transaction?.CommissionAmount ?? 0m,
                CommissionRate = transaction?.CommissionRate ?? 0m,
                TotalPaidByRenter = payments.Where(p => p.Type == PaymentType.RentalPayment).Sum(p => p.Amount),
                NetPayoutToOwner = payouts.Sum(p => p.Amount),
                PlatformRevenue = transaction?.CommissionAmount ?? 0m,
                ProcessingFees = 0, // TODO: Calculate processing fees
                RefundAmount = payments.Sum(p => p.RefundedAmount),
                Currency = transaction?.Currency ?? "USD",
                TaxAmount = 0, // TODO: Calculate tax
                TaxBreakdown = new List<ReceiptTaxDto>()
            },
            Payments = payments.Select(p => new PaymentSummaryDto
            {
                Id = p.Id,
                RentalId = p.RentalId,
                RentalToolName = tool.Name,
                Type = p.Type,
                Status = p.Status,
                Amount = p.Amount,
                Currency = p.Currency,
                CreatedAt = p.CreatedAt,
                ProcessedAt = p.ProcessedAt,
                UserFriendlyStatus = p.Status.ToString(),
                StatusDescription = p.Status.ToString()
            }).ToList(),
            Payouts = payouts.Select(p => new PayoutSummaryDto
            {
                Id = p.Id,
                RentalId = p.RentalId,
                RentalToolName = tool.Name,
                Amount = p.Amount,
                Currency = p.Currency,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                ScheduledAt = p.ScheduledAt,
                ProcessedAt = p.ProcessedAt,
                StatusDescription = p.Status.ToString()
            }).ToList(),
            Company = GetCompanyInfo(),
            Notes = GenerateTransactionSummaryNotes(rental, transaction),
            PdfUrl = $"/api/receipts/transaction/{rental.Id}/download"
        };
    }

    private string GetRateUsed(Rental rental, Tool tool)
    {
        var days = (rental.EndDate - rental.StartDate).Days + 1;
        
        if (days >= 30 && tool.MonthlyRate.HasValue)
            return "Monthly Rate";
        else if (days >= 7 && tool.WeeklyRate.HasValue)
            return "Weekly Rate";
        else
            return "Daily Rate";
    }

    private CompanyInfoDto GetCompanyInfo()
    {
        return new CompanyInfoDto
        {
            Name = "NeighborTools",
            LegalName = "NeighborTools Inc.",
            Address = new AddressDto
            {
                Street = "123 Tool Street",
                City = "Tech City",
                State = "CA",
                ZipCode = "94000",
                Country = "USA"
            },
            Email = "support@neighbortools.com",
            Website = "https://neighbortools.com"
        };
    }

    private string? GeneratePaymentNotes(Payment payment, Rental rental)
    {
        return payment.Type switch
        {
            PaymentType.RentalPayment => $"Payment for rental of {rental.Tool!.Name} from {rental.StartDate:d} to {rental.EndDate:d}",
            PaymentType.SecurityDeposit => $"Security deposit for {rental.Tool!.Name}",
            PaymentType.Refund => "Refund processed",
            _ => null
        };
    }

    private string? GeneratePayoutNotes(Payout payout, Rental rental)
    {
        return $"Payout for rental of {rental.Tool!.Name} completed on {rental.EndDate:d}";
    }

    private string? GenerateTransactionSummaryNotes(Rental rental, Transaction? transaction)
    {
        return $"Complete transaction summary for rental of {rental.Tool!.Name}";
    }

    private List<ReceiptLineItemDto> GeneratePaymentLineItems(Rental rental, Transaction? transaction)
    {
        var lineItems = new List<ReceiptLineItemDto>();
        var tool = rental.Tool!;
        var days = (rental.EndDate - rental.StartDate).Days + 1;

        // Add rental line item
        lineItems.Add(new ReceiptLineItemDto
        {
            Description = $"{tool.Name} rental ({days} day{(days > 1 ? "s" : "")})",
            Quantity = days,
            UnitPrice = tool.DailyRate,
            Amount = rental.TotalCost - (rental.DepositAmount),
            Category = "Rental",
            IsTaxable = true
        });

        // Add security deposit if applicable
        if (rental.DepositAmount > 0)
        {
            lineItems.Add(new ReceiptLineItemDto
            {
                Description = "Security Deposit",
                Quantity = 1,
                UnitPrice = rental.DepositAmount,
                Amount = rental.DepositAmount,
                Category = "Deposit",
                IsTaxable = false
            });
        }

        // Add commission if visible to user
        if (transaction?.CommissionAmount > 0)
        {
            lineItems.Add(new ReceiptLineItemDto
            {
                Description = $"Platform Fee ({transaction.CommissionRate:P})",
                Quantity = 1,
                UnitPrice = transaction.CommissionAmount,
                Amount = transaction.CommissionAmount,
                Category = "Fee",
                IsTaxable = false
            });
        }

        return lineItems;
    }

    private List<ReceiptLineItemDto> GeneratePayoutLineItems(Rental rental, Transaction? transaction)
    {
        var lineItems = new List<ReceiptLineItemDto>();
        var tool = rental.Tool!;

        // Add gross rental amount
        lineItems.Add(new ReceiptLineItemDto
        {
            Description = $"Rental income for {tool.Name}",
            Quantity = 1,
            UnitPrice = transaction?.RentalAmount ?? rental.TotalCost,
            Amount = transaction?.RentalAmount ?? rental.TotalCost,
            Category = "Income",
            IsTaxable = true
        });

        // Subtract commission
        if (transaction?.CommissionAmount > 0)
        {
            lineItems.Add(new ReceiptLineItemDto
            {
                Description = $"Platform Commission ({transaction.CommissionRate:P})",
                Quantity = 1,
                UnitPrice = -transaction.CommissionAmount,
                Amount = -transaction.CommissionAmount,
                Category = "Fee",
                IsTaxable = false
            });
        }

        return lineItems;
    }

    private string GeneratePaymentReceiptEmailContent(Payment payment, string receiptNumber, string downloadUrl)
    {
        var rental = payment.Rental!;
        var tool = rental.Tool!;

        return $@"
            <h2>Payment Receipt</h2>
            <p>Thank you for your payment! Here are the details:</p>
            
            <h3>Receipt Details</h3>
            <p><strong>Receipt Number:</strong> {receiptNumber}</p>
            <p><strong>Payment Date:</strong> {payment.ProcessedAt?.ToString("F") ?? payment.CreatedAt.ToString("F")}</p>
            <p><strong>Amount:</strong> {payment.Amount:C} {payment.Currency}</p>
            
            <h3>Rental Details</h3>
            <p><strong>Tool:</strong> {tool.Name}</p>
            <p><strong>Rental Period:</strong> {rental.StartDate:d} to {rental.EndDate:d}</p>
            <p><strong>Owner:</strong> {rental.Owner?.UserName ?? "Unknown"}</p>
            
            <p><a href='{downloadUrl}'>Download Full Receipt (PDF)</a></p>
            
            <p>If you have any questions, please contact our support team.</p>
        ";
    }

    private string GeneratePayoutReceiptEmailContent(Payout payout, string receiptNumber, string downloadUrl)
    {
        var rental = payout.Rental!;
        var tool = rental.Tool!;

        return $@"
            <h2>Payout Receipt</h2>
            <p>Your payout has been processed! Here are the details:</p>
            
            <h3>Payout Details</h3>
            <p><strong>Receipt Number:</strong> {receiptNumber}</p>
            <p><strong>Payout Date:</strong> {payout.ProcessedAt?.ToString("F") ?? payout.CreatedAt.ToString("F")}</p>
            <p><strong>Amount:</strong> {payout.Amount:C} {payout.Currency}</p>
            <p><strong>Method:</strong> {payout.PaymentProvider}</p>
            
            <h3>Rental Details</h3>
            <p><strong>Tool:</strong> {tool.Name}</p>
            <p><strong>Rental Period:</strong> {rental.StartDate:d} to {rental.EndDate:d}</p>
            <p><strong>Renter:</strong> {rental.Renter?.UserName ?? "Unknown"}</p>
            
            <p><a href='{downloadUrl}'>Download Full Receipt (PDF)</a></p>
            
            <p>If you have any questions, please contact our support team.</p>
        ";
    }

    private string GenerateTransactionSummaryEmailContent(Rental rental, string receiptNumber, string downloadUrl)
    {
        var tool = rental.Tool!;

        return $@"
            <h2>Transaction Summary</h2>
            <p>Here's a complete summary of your transaction:</p>
            
            <h3>Summary Details</h3>
            <p><strong>Summary Number:</strong> {receiptNumber}</p>
            <p><strong>Generated:</strong> {DateTime.UtcNow:F}</p>
            
            <h3>Rental Details</h3>
            <p><strong>Tool:</strong> {tool.Name}</p>
            <p><strong>Rental Period:</strong> {rental.StartDate:d} to {rental.EndDate:d}</p>
            <p><strong>Total Cost:</strong> {rental.TotalCost:C}</p>
            
            <p><a href='{downloadUrl}'>Download Full Summary (PDF)</a></p>
            
            <p>If you have any questions, please contact our support team.</p>
        ";
    }

    private string GetDefaultHtmlTemplate(ReceiptType type)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>{GetReceiptTypeDescription(type)}</title>
                <style>
                    body {{ font-family: Arial, sans-serif; }}
                    .header {{ text-align: center; margin-bottom: 30px; }}
                    .details {{ margin-bottom: 20px; }}
                    .line-items {{ width: 100%; border-collapse: collapse; }}
                    .line-items th, .line-items td {{ border: 1px solid #ddd; padding: 8px; }}
                </style>
            </head>
            <body>
                <div class='header'>
                    <h1>{{{{Company.Name}}}}</h1>
                    <h2>{{{{ReceiptTitle}}}}</h2>
                </div>
                
                <div class='details'>
                    <p><strong>Receipt Number:</strong> {{{{ReceiptNumber}}}}</p>
                    <p><strong>Date:</strong> {{{{GeneratedAt}}}}</p>
                    <p><strong>Amount:</strong> {{{{Amount}}}}</p>
                </div>
                
                <!-- Additional template content would go here -->
            </body>
            </html>
        ";
    }

    private string GetDefaultEmailTemplate(ReceiptType type)
    {
        return $@"
            <h2>{{{{ReceiptTitle}}}}</h2>
            <p>{{{{IntroMessage}}}}</p>
            
            <h3>{{{{ReceiptType}}}} Details</h3>
            <p><strong>Receipt Number:</strong> {{{{ReceiptNumber}}}}</p>
            <p><strong>Amount:</strong> {{{{Amount}}}}</p>
            <p><strong>Date:</strong> {{{{Date}}}}</p>
            
            <p><a href='{{{{DownloadUrl}}}}'>Download Full Receipt (PDF)</a></p>
            
            <p>If you have any questions, please contact our support team.</p>
        ";
    }

    private Dictionary<string, object> GetDefaultTemplateData(ReceiptType type)
    {
        return new Dictionary<string, object>
        {
            { "ReceiptTitle", GetReceiptTypeDescription(type) },
            { "ReceiptType", type.ToString() },
            { "IntroMessage", GetIntroMessage(type) }
        };
    }

    private string GetIntroMessage(ReceiptType type) => type switch
    {
        ReceiptType.Payment => "Thank you for your payment!",
        ReceiptType.Payout => "Your payout has been processed!",
        ReceiptType.TransactionSummary => "Here's your complete transaction summary:",
        ReceiptType.Refund => "Your refund has been processed:",
        _ => "Here are your receipt details:"
    };

    private List<string> GetRequiredFields(ReceiptType type)
    {
        var baseFields = new List<string> { "ReceiptNumber", "Amount", "Date", "Currency" };
        
        return type switch
        {
            ReceiptType.Payment => baseFields.Concat(new[] { "PaymentMethod", "Customer", "Rental" }).ToList(),
            ReceiptType.Payout => baseFields.Concat(new[] { "PayoutMethod", "Payee", "Rental" }).ToList(),
            ReceiptType.TransactionSummary => baseFields.Concat(new[] { "Renter", "Owner", "Rental", "Financial" }).ToList(),
            _ => baseFields
        };
    }
}