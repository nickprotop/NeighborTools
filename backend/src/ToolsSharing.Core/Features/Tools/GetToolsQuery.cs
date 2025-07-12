
using ToolsSharing.Core.Common.Models;

namespace ToolsSharing.Core.Features.Tools;

public record GetToolsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? Category = null,
    string? Location = null,
    decimal? MaxDailyRate = null,
    bool AvailableOnly = true,
    string? SearchTerm = null,
    string? SortBy = null
);

public class ToolDto
{
    public Guid Id { get; set; } = default;
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Model { get; set; } = "";
    public decimal DailyRate { get; set; } = 0;
    public decimal? WeeklyRate { get; set; } = null;
    public decimal? MonthlyRate { get; set; } = null;
    public decimal DepositRequired { get; set; } = 0;
    public string Condition { get; set; } = "";
    public string Location { get; set; } = "";
    public bool IsAvailable { get; set; } = true;
    public string OwnerId { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public List<string>? ImageUrls { get; set; } = null;
}