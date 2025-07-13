using System;
using System.Collections.Generic;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.DTOs.Payment;

public enum ReceiptType
{
    Payment,
    Payout,
    TransactionSummary,
    Refund,
    Commission
}

public class PaymentReceiptDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public ReceiptType Type { get; set; }
    
    // Payment information
    public Guid PaymentId { get; set; }
    public string ExternalPaymentId { get; set; } = string.Empty;
    public PaymentType PaymentType { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentProvider Provider { get; set; }
    
    // Customer information
    public CustomerInfoDto Customer { get; set; } = new();
    public CustomerInfoDto? Recipient { get; set; }
    
    // Rental information
    public RentalReceiptInfoDto Rental { get; set; } = new();
    
    // Financial breakdown
    public ReceiptFinancialBreakdownDto Financial { get; set; } = new();
    
    // Company information
    public CompanyInfoDto Company { get; set; } = new();
    
    // Receipt metadata
    public string? Notes { get; set; }
    public List<ReceiptLineItemDto> LineItems { get; set; } = new();
    public List<ReceiptTaxDto> Taxes { get; set; } = new();
    public string? InvoiceUrl { get; set; }
    public string? PdfUrl { get; set; }
}

public class PayoutReceiptDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public ReceiptType Type { get; set; }
    
    // Payout information
    public Guid PayoutId { get; set; }
    public string? ExternalPayoutId { get; set; }
    public PayoutStatus PayoutStatus { get; set; }
    public DateTime PayoutDate { get; set; }
    public PaymentProvider PayoutMethod { get; set; }
    
    // Payee information
    public CustomerInfoDto Payee { get; set; } = new();
    
    // Rental information
    public RentalReceiptInfoDto Rental { get; set; } = new();
    
    // Financial breakdown
    public PayoutFinancialBreakdownDto Financial { get; set; } = new();
    
    // Company information
    public CompanyInfoDto Company { get; set; } = new();
    
    // Receipt metadata
    public string? Notes { get; set; }
    public List<ReceiptLineItemDto> LineItems { get; set; } = new();
    public List<ReceiptTaxDto> Taxes { get; set; } = new();
    public string? StatementUrl { get; set; }
    public string? PdfUrl { get; set; }
}

public class TransactionSummaryReceiptDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public ReceiptType Type { get; set; }
    
    // Transaction information
    public Guid TransactionId { get; set; }
    public TransactionStatus TransactionStatus { get; set; }
    public DateTime TransactionDate { get; set; }
    
    // Parties
    public CustomerInfoDto Renter { get; set; } = new();
    public CustomerInfoDto Owner { get; set; } = new();
    
    // Rental information
    public RentalReceiptInfoDto Rental { get; set; } = new();
    
    // Complete financial breakdown
    public TransactionFinancialSummaryDto Financial { get; set; } = new();
    
    // All related payments
    public List<PaymentSummaryDto> Payments { get; set; } = new();
    public List<PayoutSummaryDto> Payouts { get; set; } = new();
    
    // Company information
    public CompanyInfoDto Company { get; set; } = new();
    
    // Receipt metadata
    public string? Notes { get; set; }
    public string? PdfUrl { get; set; }
}

public class CustomerInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public AddressDto? Address { get; set; }
    public string? TaxId { get; set; }
    public string? BusinessName { get; set; }
}

public class AddressDto
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public class RentalReceiptInfoDto
{
    public Guid Id { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string ToolDescription { get; set; } = string.Empty;
    public string? ToolImage { get; set; }
    public string ToolCategory { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int RentalDays { get; set; }
    public decimal DailyRate { get; set; }
    public decimal? WeeklyRate { get; set; }
    public decimal? MonthlyRate { get; set; }
    public string RateUsed { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
}

public class ReceiptFinancialBreakdownDto
{
    public decimal SubTotal { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? TransactionFee { get; set; }
}

public class PayoutFinancialBreakdownDto
{
    public decimal GrossAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal ProcessingFee { get; set; }
    public decimal TaxWithheld { get; set; }
    public decimal NetPayoutAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PayoutMethod { get; set; } = string.Empty;
    public string? PayoutDestination { get; set; }
}

public class TransactionFinancialSummaryDto
{
    public decimal RentalAmount { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal TotalPaidByRenter { get; set; }
    public decimal NetPayoutToOwner { get; set; }
    public decimal PlatformRevenue { get; set; }
    public decimal ProcessingFees { get; set; }
    public decimal RefundAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    
    // Tax information
    public decimal TaxAmount { get; set; }
    public List<ReceiptTaxDto> TaxBreakdown { get; set; } = new();
}

public class ReceiptLineItemDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public string? Category { get; set; }
    public bool IsTaxable { get; set; }
}

public class ReceiptTaxDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? TaxId { get; set; }
}

public class CompanyInfoDto
{
    public string Name { get; set; } = "NeighborTools";
    public string LegalName { get; set; } = "NeighborTools Inc.";
    public string? TaxId { get; set; }
    public AddressDto Address { get; set; } = new();
    public string Email { get; set; } = "support@neighbortools.com";
    public string Phone { get; set; } = string.Empty;
    public string Website { get; set; } = "https://neighbortools.com";
    public string? LogoUrl { get; set; }
}

public class ReceiptListDto
{
    public List<ReceiptSummaryDto> Receipts { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class ReceiptSummaryDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public ReceiptType Type { get; set; }
    public string TypeDescription { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? DownloadUrl { get; set; }
    public string? Status { get; set; }
}

public class ReceiptTemplateDto
{
    public ReceiptType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string HtmlTemplate { get; set; } = string.Empty;
    public string EmailTemplate { get; set; } = string.Empty;
    public Dictionary<string, object> DefaultData { get; set; } = new();
    public List<string> RequiredFields { get; set; } = new();
    public DateTime LastModified { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
}