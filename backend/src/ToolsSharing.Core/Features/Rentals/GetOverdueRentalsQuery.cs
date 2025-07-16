namespace ToolsSharing.Core.Features.Rentals;

public record GetOverdueRentalsQuery(
    DateTime? AsOfDate = null,
    int PageNumber = 1,
    int PageSize = 50,
    string? SortBy = null
);