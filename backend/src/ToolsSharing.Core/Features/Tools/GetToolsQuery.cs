
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

