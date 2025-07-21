namespace ToolsSharing.Core.Entities;

public class Review : BaseEntity
{
    public Guid? ToolId { get; set; }
    public Guid? RentalId { get; set; }
    public Guid? BundleId { get; set; }
    public Guid? BundleRentalId { get; set; }
    public string ReviewerId { get; set; } = string.Empty;
    public string RevieweeId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public ReviewType Type { get; set; }
    
    // Navigation properties
    public Tool? Tool { get; set; }
    public Rental? Rental { get; set; }
    public Bundle? Bundle { get; set; }
    public BundleRental? BundleRental { get; set; }
    public User Reviewer { get; set; } = null!;
    public User Reviewee { get; set; } = null!;
}

public enum ReviewType
{
    ToolReview,
    UserReview,
    BundleReview
}