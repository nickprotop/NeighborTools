namespace ToolsSharing.Core.Features.Rentals;

public record MarkRentalReturnedCommand(
    Guid RentalId, 
    string UserId, 
    string? Notes = null,
    string? ConditionNotes = null);