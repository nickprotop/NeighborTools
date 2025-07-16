namespace ToolsSharing.Core.Features.Rentals;

public record ExtendRentalCommand(Guid RentalId, string UserId, DateTime NewEndDate, string? Notes = null);