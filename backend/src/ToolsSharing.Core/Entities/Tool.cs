using ToolsSharing.Core.Enums;

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
    public bool IsAvailable { get; set; } = true;
    public int? LeadTimeHours { get; set; } // Nullable - falls back to owner's default if not set
    
    // Enhanced location fields (Phase 1 - Comprehensive Location System)
    public string? LocationDisplay { get; set; } // User-friendly display name
    public string? LocationArea { get; set; } // Neighborhood/area name
    public string? LocationCity { get; set; } // City name
    public string? LocationState { get; set; } // State/province name
    public string? LocationCountry { get; set; } // Country name
    public decimal? LocationLat { get; set; } // Latitude (quantized for privacy)
    public decimal? LocationLng { get; set; } // Longitude (quantized for privacy)
    public int? LocationPrecisionRadius { get; set; } // Generalization radius in meters
    public LocationSource? LocationSource { get; set; } // How location was obtained
    public PrivacyLevel LocationPrivacyLevel { get; set; } = PrivacyLevel.Neighborhood; // User's privacy preference
    public DateTime? LocationUpdatedAt { get; set; } // Track location changes
    
    // Phase 7 - Location Inheritance System
    public LocationInheritanceOption LocationInheritanceOption { get; set; } = LocationInheritanceOption.InheritFromProfile;
    public string OwnerId { get; set; } = string.Empty;
    
    // New feature fields
    public string Tags { get; set; } = string.Empty; // Comma-separated tags
    public int ViewCount { get; set; } = 0;
    public decimal AverageRating { get; set; } = 0.00m;
    public int ReviewCount { get; set; } = 0;
    public bool IsFeatured { get; set; } = false;
    
    // Approval/Moderation fields
    public bool IsApproved { get; set; } = false;
    public bool PendingApproval { get; set; } = true;
    public string? RejectionReason { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedById { get; set; }
    
    // Navigation properties
    public User Owner { get; set; } = null!;
    public ICollection<ToolImage> Images { get; set; } = new List<ToolImage>();
    public ICollection<Rental> Rentals { get; set; } = new List<Rental>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Favorite> FavoritedBy { get; set; } = new List<Favorite>();
}