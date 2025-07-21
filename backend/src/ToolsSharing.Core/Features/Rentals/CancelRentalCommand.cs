using ToolsSharing.Core.Common.Models;

namespace ToolsSharing.Core.Features.Rentals;

public record CancelRentalCommand(Guid RentalId, string RenterId, string? Reason = null);