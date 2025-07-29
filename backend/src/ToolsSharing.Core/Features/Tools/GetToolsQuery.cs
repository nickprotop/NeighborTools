
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.DTOs.Location;

namespace ToolsSharing.Core.Features.Tools;

public record GetToolsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? Category = null,
    LocationSearchRequest? LocationSearch = null,
    decimal? MaxDailyRate = null,
    bool AvailableOnly = true,
    string? SearchTerm = null,
    string? SortBy = null,
    string? Tags = null
);

