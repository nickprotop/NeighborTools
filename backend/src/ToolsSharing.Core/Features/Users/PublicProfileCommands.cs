namespace ToolsSharing.Core.Features.Users;

// Query for getting public profile
public record GetPublicUserProfileQuery(string UserId);

// Public profile DTO that respects privacy settings
public class PublicUserProfileDto
{
    public string Id { get; set; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Location { get; set; }
    public PublicUserStatisticsDto? Statistics { get; set; }
    public DateTime JoinedDate { get; set; }
    public bool IsActive { get; set; }
}

// Public statistics that may be visible based on settings
public class PublicUserStatisticsDto
{
    public int? ToolsShared { get; set; }
    public int? SuccessfulRentals { get; set; }
    public decimal? AverageRating { get; set; }
    public int? ReviewCount { get; set; }
    public string? TotalEarned { get; set; }
    public int? ResponseTime { get; set; } // Average response time in hours
}

// Tool listing for public profile
public class PublicUserToolDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? ImageUrl { get; set; }
    public decimal DailyRate { get; set; }
    public string Category { get; set; } = "";
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsAvailable { get; set; }
}

// Review for public profile display
public class PublicUserReviewDto
{
    public string Id { get; set; } = "";
    public string ReviewerName { get; set; } = "";
    public string? ReviewerAvatarUrl { get; set; }
    public decimal Rating { get; set; }
    public string Comment { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string ToolName { get; set; } = "";
}