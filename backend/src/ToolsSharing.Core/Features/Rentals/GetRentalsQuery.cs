
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.Features.Rentals;

public record GetRentalsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? UserId = null,
    RentalStatus? Status = null,
    Guid? ToolId = null,
    string? SortBy = null
);