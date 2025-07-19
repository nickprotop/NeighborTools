namespace ToolsSharing.Core.Entities;

public class Tool : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }
    public decimal? WeeklyRate { get; set; }
    public decimal? MonthlyRate { get; set; }
    public decimal DepositRequired { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public int? LeadTimeHours { get; set; } // Nullable - falls back to owner's default if not set
    public string OwnerId { get; set; } = string.Empty;
    
    // Navigation properties
    public User Owner { get; set; } = null!;
    public ICollection<ToolImage> Images { get; set; } = new List<ToolImage>();
    public ICollection<Rental> Rentals { get; set; } = new List<Rental>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Favorite> FavoritedBy { get; set; } = new List<Favorite>();
}