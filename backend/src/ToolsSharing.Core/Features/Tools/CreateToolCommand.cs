using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.DTOs.Location;
using ToolsSharing.Core.Enums;

namespace ToolsSharing.Core.Features.Tools;

public record CreateToolCommand(
    string Name,
    string Description,
    string Category,
    string Brand,
    string Model,
    decimal DailyRate,
    decimal WeeklyRate,
    decimal MonthlyRate,
    decimal DepositRequired,
    string Condition,
    LocationInheritanceOption LocationSource,
    UpdateLocationRequest? CustomLocation,
    string OwnerId,
    int? LeadTimeHours = null,
    List<string>? ImageUrls = null,
    string? Tags = null
);