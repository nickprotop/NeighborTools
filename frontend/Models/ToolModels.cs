namespace frontend.Models;

using ToolsSharing.Frontend.Models.Location;

public class Tool
{
    public string Id { get; set; } = string.Empty;
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
    
    // Phase 7: Location inheritance system
    public LocationInheritanceOption LocationInheritanceOption { get; set; } = LocationInheritanceOption.InheritFromProfile;
    
    public bool IsAvailable { get; set; }
    public int? LeadTimeHours { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = new();
    
    // New feature properties
    public string Tags { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Approval/Moderation properties
    public bool IsApproved { get; set; }
    public bool HasPendingApproval { get; set; }
    public string? RejectionReason { get; set; }
    
    // Computed properties for convenience
    public List<string> TagList => Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
    public double? Rating => AverageRating > 0 ? (double)AverageRating : null;
}

public class ToolRequestBase
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
    
    // Phase 7: Location inheritance system - align with backend DTOs
    public LocationInheritanceOption LocationSource { get; set; } = LocationInheritanceOption.InheritFromProfile;
    
    // Custom location object for inheritance system (only used when LocationSource = CustomLocation)
    public UserLocationModel? CustomLocation { get; set; }
    
    public int? LeadTimeHours { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public string? Tags { get; set; }
}

public class CreateToolRequest : ToolRequestBase
{
}

public class UpdateToolRequest : ToolRequestBase
{
    public bool IsAvailable { get; set; } = true;
    
    // Phase 7: UpdateToolRequest has different field name in backend for custom location
    public UserLocationModel? EnhancedLocation { get; set; }
    
    // Legacy field names for backward compatibility with existing code
    public string Location 
    { 
        get => EnhancedLocation?.LocationDisplay ?? ""; 
        set => EnhancedLocation = string.IsNullOrEmpty(value) ? null : new UserLocationModel { LocationDisplay = value }; 
    }
    
    public LocationInheritanceOption LocationInheritanceOption 
    { 
        get => LocationSource; 
        set => LocationSource = value; 
    }
    
    // Note: Backend UpdateToolRequest doesn't have LocationSource field - using inheritance from base
}

public class ToolRentalPreferences
{
    public int LeadTimeHours { get; set; } = 24;
    public bool AutoApprovalEnabled { get; set; } = false;
    public bool RequireDeposit { get; set; } = true;
    public decimal DefaultDepositPercentage { get; set; } = 0.20m;
}

public class Rental
{
    public string Id { get; set; } = string.Empty;
    public string ToolId { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public string RenterId { get; set; } = string.Empty;
    public string RenterName { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalCost { get; set; }
    public decimal DepositAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? PickupDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string? ReturnConditionNotes { get; set; }
    public string? ReturnedByUserId { get; set; }
    public DateTime? DisputeDeadline { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Tool? Tool { get; set; }
    public bool IsPaid { get; set; }
}

public class CreateRentalRequest
{
    public string ToolId { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Notes { get; set; }
}

public class RentalApprovalRequest
{
    public bool IsApproved { get; set; }
    public string? Reason { get; set; }
}

// Favorites models
public class FavoriteDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? ToolId { get; set; }
    public string? BundleId { get; set; }
    public string FavoriteType { get; set; } = string.Empty; // "Tool" or "Bundle"
    public DateTime CreatedAt { get; set; }
    
    // Tool information (for tool favorites)
    public string ToolName { get; set; } = string.Empty;
    public string ToolDescription { get; set; } = string.Empty;
    public string ToolCategory { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }
    public string ToolCondition { get; set; } = string.Empty;
    public string ToolLocation { get; set; } = string.Empty;
    public List<string> ToolImageUrls { get; set; } = new();
    public bool IsToolAvailable { get; set; }
    
    // Bundle information (for bundle favorites)
    public string BundleName { get; set; } = string.Empty;
    public string BundleDescription { get; set; } = string.Empty;
    public string BundleCategory { get; set; } = string.Empty;
    public string BundleLocation { get; set; } = string.Empty;
    public decimal BundleDiscountedCost { get; set; }
    public string BundleImageUrl { get; set; } = string.Empty;
    public bool IsBundleAvailable { get; set; }
    public int BundleToolCount { get; set; }
    
    // Owner information (common for both)
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
}

public class AddToFavoritesRequest
{
    public string ToolId { get; set; } = string.Empty;
}

public class AddBundleToFavoritesRequest
{
    public string BundleId { get; set; } = string.Empty;
}

public class RemoveFromFavoritesRequest
{
    public string ToolId { get; set; } = string.Empty;
}

public class CheckFavoriteStatusRequest
{
    public string ToolId { get; set; } = string.Empty;
}

public class FavoriteStatusDto
{
    public bool IsFavorited { get; set; }
    public string? FavoriteId { get; set; }
}

// Tool Review models
public class ToolReview
{
    public string Id { get; set; } = string.Empty;
    public string ToolId { get; set; } = string.Empty;
    public string ReviewerId { get; set; } = string.Empty;
    public string ReviewerName { get; set; } = string.Empty;
    public string ReviewerAvatar { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateToolReviewRequest
{
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
}

public class ToolReviewSummaryDto
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int FiveStarCount { get; set; }
    public int FourStarCount { get; set; }
    public int ThreeStarCount { get; set; }
    public int TwoStarCount { get; set; }
    public int OneStarCount { get; set; }
    public List<ToolReview> LatestReviews { get; set; } = new();
}

// Tag models
public class TagDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

// Request approval models
public class RequestApprovalRequest
{
    public string? Message { get; set; }
}

// Search models
public class ToolSearchRequest
{
    public string? Query { get; set; }
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Location { get; set; }
    public bool? IsAvailable { get; set; }
    public bool? IsFeatured { get; set; }
    public decimal? MinRating { get; set; }
    public string? SortBy { get; set; } = "featured";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

