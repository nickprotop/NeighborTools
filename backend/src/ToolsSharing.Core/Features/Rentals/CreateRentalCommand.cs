
using ToolsSharing.Core.Common.Models;

namespace ToolsSharing.Core.Features.Rentals;

public record CreateRentalCommand(
    Guid ToolId,
    string RenterId,
    DateTime StartDate,
    DateTime EndDate,
    string? Notes = null
);

public record RentalDto(
    Guid Id,
    Guid ToolId,
    string ToolName,
    string RenterId,
    string RenterName,
    string OwnerId,
    string OwnerName,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalCost,
    decimal DepositAmount,
    string Status,
    string? Notes,
    DateTime? PickupDate,
    DateTime? ReturnDate
);