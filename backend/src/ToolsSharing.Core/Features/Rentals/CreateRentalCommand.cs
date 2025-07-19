
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

public class RentalDto
{
    public Guid Id { get; set; }
    public Guid ToolId { get; set; }
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
    public bool IsPaid { get; set; }
    public ToolDto? Tool { get; set; }
}