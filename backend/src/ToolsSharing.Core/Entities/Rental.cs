namespace ToolsSharing.Core.Entities;

public class Rental : BaseEntity
{
    public Guid ToolId { get; set; }
    public string RenterId { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalCost { get; set; }
    public decimal DepositAmount { get; set; }
    public RentalStatus Status { get; set; } = RentalStatus.Pending;
    public string? Notes { get; set; }
    public DateTime? PickupDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    
    // Navigation properties
    public Tool Tool { get; set; } = null!;
    public User Renter { get; set; } = null!;
    public User Owner { get; set; } = null!;
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public Transaction? Transaction { get; set; } // Financial transaction for this rental
}

public enum RentalStatus
{
    Pending,
    Approved,
    Rejected,
    PickedUp,
    Returned,
    Cancelled,
    Overdue
}