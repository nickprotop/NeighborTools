using System;
using System.Collections.Generic;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.DTOs.Dispute;

public class DisputeDto
{
    public string Id { get; set; } = string.Empty;
    public Guid RentalId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DisputeType Type { get; set; }
    public DisputeStatus Status { get; set; }
    public DisputeCategory Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public decimal? DisputedAmount { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
    public string InitiatedByName { get; set; } = string.Empty;
    public string? ExternalDisputeId { get; set; }
    public DisputeResolution? Resolution { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public RentalSummaryDto? Rental { get; set; }
}

public class RentalSummaryDto
{
    public Guid Id { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalCost { get; set; }
}

public class DisputeSummaryDto
{
    public Guid Id { get; set; }
    public Guid RentalId { get; set; }
    public string RentalToolName { get; set; } = string.Empty;
    public string InitiatorName { get; set; } = string.Empty;
    public DisputeType Type { get; set; }
    public DisputeStatus Status { get; set; }
    public DisputeCategory Category { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal? DisputeAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActionAt { get; set; }
    public bool HasUnreadMessages { get; set; }
    public int MessageCount { get; set; }
    public DateTime? ResponseDueDate { get; set; }
    public bool IsOverdue { get; set; }
}

public class DisputeDetailsDto
{
    public Guid Id { get; set; }
    public Guid RentalId { get; set; }
    public Guid? PaymentId { get; set; }
    
    // Rental information
    public string RentalToolName { get; set; } = string.Empty;
    public string RentalToolImage { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string RenterName { get; set; } = string.Empty;
    public DateTime RentalStartDate { get; set; }
    public DateTime RentalEndDate { get; set; }
    public decimal RentalAmount { get; set; }
    
    // Dispute information
    public string InitiatorName { get; set; } = string.Empty;
    public string InitiatorId { get; set; } = string.Empty;
    public DisputeType Type { get; set; }
    public DisputeStatus Status { get; set; }
    public DisputeCategory Category { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? DisputeAmount { get; set; }
    
    // Timeline
    public DateTime CreatedAt { get; set; }
    public DateTime? EscalatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ResponseDueDate { get; set; }
    public DateTime? LastActionAt { get; set; }
    
    // Resolution
    public string? ResolutionNotes { get; set; }
    public string? ResolvedByName { get; set; }
    public DisputeResolution? Resolution { get; set; }
    public decimal? RefundAmount { get; set; }
    
    // PayPal integration
    public string? ExternalDisputeId { get; set; }
    public string? ExternalCaseId { get; set; }
    public DisputeReason? PayPalReason { get; set; }
    
    // Evidence and communication
    public List<EvidenceFileDto> Evidence { get; set; } = new();
    public List<DisputeMessageDto> RecentMessages { get; set; } = new();
    public int UnreadMessageCount { get; set; }
    
    // Status indicators
    public bool CanUserRespond { get; set; }
    public bool IsOverdue { get; set; }
    public bool RequiresAttention { get; set; }
    public List<string> AvailableActions { get; set; } = new();
}

public class DisputeMessageDto
{
    public string Id { get; set; } = string.Empty;
    public string DisputeId { get; set; } = string.Empty;
    public string FromUserId { get; set; } = string.Empty;
    public string FromUserName { get; set; } = string.Empty;
    public string? ToUserId { get; set; }
    public string? ToUserName { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<string> Attachments { get; set; } = new();
    public bool IsFromAdmin { get; set; }
    public bool IsInternal { get; set; }
    public bool IsSystemGenerated { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class EvidenceFileDto
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public long Size { get; set; }
}

public class DisputeStatisticsDto
{
    public int TotalDisputes { get; set; }
    public int OpenDisputes { get; set; }
    public int ResolvedDisputes { get; set; }
    public int EscalatedDisputes { get; set; }
    public decimal AverageResolutionTimeHours { get; set; }
    public decimal ResolutionRate { get; set; }
    public Dictionary<DisputeType, int> DisputesByType { get; set; } = new();
    public Dictionary<DisputeResolution, int> ResolutionsByType { get; set; } = new();
    public List<DisputeTrendDto> MonthlyTrends { get; set; } = new();
}

public class DisputeTrendDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int DisputeCount { get; set; }
    public int ResolvedCount { get; set; }
    public decimal AverageResolutionTimeHours { get; set; }
}

public class AddDisputeMessageRequest
{
    public string DisputeId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public List<string> Attachments { get; set; } = new();
}

// Result classes for service operations
public class ResolveDisputeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DisputeDto? Data { get; set; }
}

public class EscalateDisputeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? PayPalDisputeId { get; set; }
}

public class SyncPayPalDisputeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public PayPalDisputeDto? Data { get; set; }
}

public class AssignDisputeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
}

public class UploadEvidenceResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> UploadedFiles { get; set; } = new();
}

// Request classes for dispute operations
public class ResolveDisputeRequest
{
    public Guid DisputeId { get; set; }
    public DisputeResolution Resolution { get; set; }
    public string ResolutionNotes { get; set; } = string.Empty;
    public decimal? RefundAmount { get; set; }
    public string ResolvedBy { get; set; } = string.Empty;
    public bool NotifyParties { get; set; } = true;
}

public class PayPalDisputeWebhook
{
    public string Id { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventVersion { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public PayPalDisputeWebhookResource Resource { get; set; } = new();
}

public class PayPalDisputeWebhookResource
{
    public string DisputeId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal DisputeAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public string SellerTransactionId { get; set; } = string.Empty;
}

public class EvidenceFile
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public long Size { get; set; }
}


public class PayPalDisputeDto
{
    public string DisputeId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
    public DateTime? LastUpdatedTime { get; set; }
    public string? TransactionId { get; set; }
    public bool IsSynced { get; set; }
    public Guid? LocalDisputeId { get; set; }
}