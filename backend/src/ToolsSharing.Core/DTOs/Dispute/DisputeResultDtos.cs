using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.DTOs.Dispute;

// Request DTOs
public class GetDisputesRequest
{
    public string UserId { get; set; } = string.Empty;
    public DisputeStatus? Status { get; set; }
    public DisputeType? Type { get; set; }
    public DisputeCategory? Category { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

public class UpdateDisputeStatusRequest
{
    public Guid DisputeId { get; set; }
    public DisputeStatus Status { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}

public class CloseDisputeRequest
{
    public Guid DisputeId { get; set; }
    public string ClosedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool NotifyParties { get; set; } = true;
}

// Result DTOs
public class GetDisputesResult
{
    public bool Success { get; set; }
    public List<DisputeDto> Data { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    
    public static GetDisputesResult CreateSuccess(List<DisputeDto> data, int totalCount, int pageNumber, int pageSize, string? message = null)
    {
        return new GetDisputesResult
        {
            Success = true,
            Data = data,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Message = message ?? "Disputes retrieved successfully"
        };
    }
    
    public static GetDisputesResult CreateFailure(string message)
    {
        return new GetDisputesResult
        {
            Success = false,
            Message = message
        };
    }
}

public class GetMessagesResult
{
    public bool Success { get; set; }
    public List<DisputeMessageDto> Data { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    
    public static GetMessagesResult CreateSuccess(List<DisputeMessageDto> data, string? message = null)
    {
        return new GetMessagesResult
        {
            Success = true,
            Data = data,
            Message = message ?? "Messages retrieved successfully"
        };
    }
    
    public static GetMessagesResult CreateFailure(string message)
    {
        return new GetMessagesResult
        {
            Success = false,
            Message = message
        };
    }
}

public class UpdateStatusResult
{
    public bool Success { get; set; }
    public DisputeDto? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    
    public static UpdateStatusResult CreateSuccess(DisputeDto data, string? message = null)
    {
        return new UpdateStatusResult
        {
            Success = true,
            Data = data,
            Message = message ?? "Status updated successfully"
        };
    }
    
    public static UpdateStatusResult CreateFailure(string message)
    {
        return new UpdateStatusResult
        {
            Success = false,
            Message = message
        };
    }
}

public class CloseDisputeResult
{
    public bool Success { get; set; }
    public DisputeDto? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    
    public static CloseDisputeResult CreateSuccess(DisputeDto data, string? message = null)
    {
        return new CloseDisputeResult
        {
            Success = true,
            Data = data,
            Message = message ?? "Dispute closed successfully"
        };
    }
    
    public static CloseDisputeResult CreateFailure(string message)
    {
        return new CloseDisputeResult
        {
            Success = false,
            Message = message
        };
    }
}

public class GetTimelineResult
{
    public bool Success { get; set; }
    public List<DisputeTimelineEventDto> Data { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    
    public static GetTimelineResult CreateSuccess(List<DisputeTimelineEventDto> data, string? message = null)
    {
        return new GetTimelineResult
        {
            Success = true,
            Data = data,
            Message = message ?? "Timeline retrieved successfully"
        };
    }
    
    public static GetTimelineResult CreateFailure(string message)
    {
        return new GetTimelineResult
        {
            Success = false,
            Message = message
        };
    }
}

public class GetStatsResult
{
    public bool Success { get; set; }
    public DisputeStatsDto? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    
    public static GetStatsResult CreateSuccess(DisputeStatsDto data, string? message = null)
    {
        return new GetStatsResult
        {
            Success = true,
            Data = data,
            Message = message ?? "Statistics retrieved successfully"
        };
    }
    
    public static GetStatsResult CreateFailure(string message)
    {
        return new GetStatsResult
        {
            Success = false,
            Message = message
        };
    }
}

// Supporting DTOs
public class DisputeTimelineEventDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string? ActorName { get; set; }
    public string? ActorRole { get; set; }
    public List<string> Attachments { get; set; } = new();
    public bool ActionRequired { get; set; }
    public List<TimelineActionDto> Actions { get; set; } = new();
    public bool ShowDetails { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class TimelineActionDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class DisputeStatsDto
{
    public int TotalDisputes { get; set; }
    public int OpenDisputes { get; set; }
    public int InProgressDisputes { get; set; }
    public int ResolvedDisputes { get; set; }
    public int EscalatedDisputes { get; set; }
    public decimal AverageResolutionTime { get; set; }
    public decimal RefundedAmount { get; set; }
    public Dictionary<string, int> DisputesByCategory { get; set; } = new();
    public Dictionary<string, int> DisputesByMonth { get; set; } = new();
}

