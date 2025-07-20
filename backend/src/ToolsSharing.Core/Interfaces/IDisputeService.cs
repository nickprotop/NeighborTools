using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ToolsSharing.Core.DTOs.Dispute;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.Interfaces;

public interface IDisputeService
{
    // Dispute creation and management
    Task<CreateDisputeResult> CreateDisputeAsync(CreateDisputeRequest request);
    Task<DisputeDetailsResult> GetDisputeAsync(Guid disputeId, string userId);
    Task<GetDisputesResult> GetDisputesAsync(GetDisputesRequest request);
    Task<List<DisputeSummaryDto>> GetUserDisputesAsync(string userId);
    Task<List<DisputeSummaryDto>> GetRentalDisputesAsync(Guid rentalId);
    
    // Dispute communication
    Task<AddMessageResult> AddDisputeMessageAsync(AddDisputeMessageRequest request);
    Task<GetMessagesResult> GetDisputeMessagesAsync(Guid disputeId, string userId);
    Task MarkMessagesAsReadAsync(Guid disputeId, string userId);
    
    // Dispute resolution
    Task<ResolveDisputeResult> ResolveDisputeAsync(ResolveDisputeRequest request);
    Task<EscalateDisputeResult> EscalateToPayPalAsync(Guid disputeId, string adminUserId);
    Task<UpdateStatusResult> UpdateDisputeStatusAsync(UpdateDisputeStatusRequest request);
    Task<CloseDisputeResult> CloseDisputeAsync(CloseDisputeRequest request);
    Task<GetTimelineResult> GetDisputeTimelineAsync(Guid disputeId, string userId);
    Task<GetStatsResult> GetDisputeStatsAsync();
    
    // PayPal integration
    Task<SyncPayPalDisputeResult> SyncPayPalDisputeAsync(string externalDisputeId);
    Task HandlePayPalDisputeWebhookAsync(PayPalDisputeWebhook webhook);
    Task<List<PayPalDisputeDto>> GetPayPalDisputesAsync();
    
    // Admin functionality
    Task<List<DisputeSummaryDto>> GetPendingDisputesAsync();
    Task<DisputeStatisticsDto> GetDisputeStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<AssignDisputeResult> AssignDisputeToAdminAsync(Guid disputeId, string adminUserId);
    Task<GetDisputesResult> GetDisputesForAdminAsync(GetDisputesRequest request);
    
    // Evidence and documentation
    Task<UploadEvidenceResult> UploadEvidenceAsync(Guid disputeId, string userId, List<EvidenceFile> files);
    Task<List<EvidenceFileDto>> GetDisputeEvidenceAsync(Guid disputeId, string userId);
    
    // Mutual Closure Integration
    Task<MutualClosureEligibilityDto> CheckDisputeMutualClosureEligibilityAsync(Guid disputeId, string userId);
    Task<CreateMutualClosureResult> InitiateMutualClosureAsync(Guid disputeId, CreateMutualClosureRequest request, string userId);
    Task<GetMutualClosuresResult> GetDisputeMutualClosuresAsync(Guid disputeId, string userId);
    Task<bool> HandleMutualClosureCompletionAsync(Guid disputeId, MutualClosureStatus status, string resolutionNotes, decimal? refundAmount = null);
}

// DTOs for dispute operations
public class CreateDisputeRequest
{
    public Guid RentalId { get; set; }
    public Guid? PaymentId { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
    public DisputeType Type { get; set; }
    public DisputeCategory Category { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? DisputeAmount { get; set; }
    public List<EvidenceFile>? Evidence { get; set; }
}

public class CreateDisputeResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? DisputeId { get; set; }
    public DisputeDetailsDto? Dispute { get; set; }
}

public class DisputeDetailsResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DisputeDetailsDto? Dispute { get; set; }
}

public class ResolveDisputeRequest
{
    public Guid DisputeId { get; set; }
    public string ResolvedBy { get; set; } = string.Empty;
    public DisputeResolution Resolution { get; set; }
    public string ResolutionNotes { get; set; } = string.Empty;
    public decimal? RefundAmount { get; set; }
}

public class ResolveDisputeResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RefundTransactionId { get; set; }
}

public class EscalateDisputeResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ExternalDisputeId { get; set; }
}

public class SyncPayPalDisputeResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DisputeDetailsDto? Dispute { get; set; }
}

public class AddMessageResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DisputeMessageDto? Message { get; set; }
}

public class UploadEvidenceResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<EvidenceFileDto>? UploadedFiles { get; set; }
}

public class AssignDisputeResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class EvidenceFile
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public long Size { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class PayPalDisputeWebhook
{
    public string DisputeId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}