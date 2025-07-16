namespace ToolsSharing.Core.Features.Rentals;

public record MarkRentalPickedUpCommand(Guid RentalId, string UserId, string? Notes = null);