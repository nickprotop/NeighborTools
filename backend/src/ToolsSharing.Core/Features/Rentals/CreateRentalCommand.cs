
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Features.Tools;

namespace ToolsSharing.Core.Features.Rentals;

public record CreateRentalCommand(
    Guid ToolId,
    string RenterId,
    DateTime StartDate,
    DateTime EndDate,
    string? Notes = null
);

// DTO for frontend requests
public class CreateRentalDto
{
    public string ToolId { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Notes { get; set; }
}

public class RentalDto
{
    public string Id { get; set; } = "";  // Changed to string to match frontend
    public string ToolId { get; set; } = "";  // Changed to string to match frontend
    public string ToolName { get; set; } = "";
    public string RenterId { get; set; } = "";
    public string RenterName { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalCost { get; set; }
    public decimal DepositAmount { get; set; }
    public string Status { get; set; } = "";
    public string? Notes { get; set; }
    public DateTime? PickupDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string? ReturnConditionNotes { get; set; }  // Added to match frontend
    public string? ReturnedByUserId { get; set; }  // Added to match frontend
    public DateTime? DisputeDeadline { get; set; }  // Added to match frontend
    public DateTime? ApprovedAt { get; set; }  // Added to match frontend
    public DateTime? CancelledAt { get; set; }  // Added to match frontend
    public string? CancellationReason { get; set; }  // Added to match frontend
    public DateTime CreatedAt { get; set; }  // Added to match frontend
    public DateTime UpdatedAt { get; set; }  // Added to match frontend
    public bool IsPaid { get; set; }
    public DTOs.Tools.ToolDto? Tool { get; set; }
}