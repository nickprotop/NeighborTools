using ToolsSharing.Core.Common.Models;

namespace ToolsSharing.Core.Features.Tools;

public record UpdateToolCommand(
    Guid Id,
    string Name,
    string Description,
    string Category,
    string Brand,
    string Model,
    decimal DailyRate,
    decimal? WeeklyRate,
    decimal? MonthlyRate,
    decimal DepositRequired,
    string Condition,
    string Location,
    bool IsAvailable,
    int? LeadTimeHours,
    string OwnerId,
    List<string> ImageUrls,
    string? Tags = null
);