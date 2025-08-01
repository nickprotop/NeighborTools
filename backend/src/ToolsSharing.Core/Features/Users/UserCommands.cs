namespace ToolsSharing.Core.Features.Users;

public record GetUserProfileQuery(string UserId);

public record UpdateUserProfileCommand(
    string UserId,
    string FirstName,
    string LastName,
    string? PhoneNumber = null,
    string? Address = null,
    string? City = null,
    string? PostalCode = null,
    string? Country = null,
    string? LocationDisplay = null,
    DateTime? DateOfBirth = null,
    string? ProfilePictureUrl = null,
    // Phase 7: Enhanced location fields for inheritance system
    string? LocationArea = null,
    string? LocationCity = null,
    string? LocationState = null,
    string? LocationCountry = null,
    decimal? LocationLat = null,
    decimal? LocationLng = null,
    int? LocationPrecisionRadius = null,
    Core.Enums.LocationSource? LocationSource = null,
    Core.Enums.PrivacyLevel? LocationPrivacyLevel = null
);

public record GetUserStatisticsQuery(string UserId);

public record GetUserReviewsQuery(string UserId, int Page = 1, int PageSize = 10);

public class UserProfileDto
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? LocationDisplay { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsVerified { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    
    // Phase 7: Enhanced location fields for inheritance system
    public string? LocationArea { get; set; }
    public string? LocationCity { get; set; }
    public string? LocationState { get; set; }
    public string? LocationCountry { get; set; }
    public decimal? LocationLat { get; set; }
    public decimal? LocationLng { get; set; }
    public int? LocationPrecisionRadius { get; set; }
    public Core.Enums.LocationSource? LocationSource { get; set; }
    public Core.Enums.PrivacyLevel LocationPrivacyLevel { get; set; }
    public DateTime? LocationUpdatedAt { get; set; }
}

public class UserStatisticsDto
{
    public int ToolsShared { get; set; }
    public int SuccessfulRentals { get; set; }
    public int TotalRentals { get; set; }
    public decimal TotalEarned { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public DateTime MemberSince { get; set; }
    public int ActiveRentals { get; set; }
    public List<RecentActivityDto> RecentActivity { get; set; } = new();
}

public class RecentActivityDto
{
    public string ActivityType { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime Date { get; set; }
    public string? ToolName { get; set; }
    public string? RenterName { get; set; }
}

public class UserReviewDto
{
    public string Id { get; set; } = "";
    public string ReviewerId { get; set; } = "";
    public string ReviewerName { get; set; } = "";
    public string? ReviewerAvatarUrl { get; set; }
    public int Rating { get; set; }
    public string Title { get; set; } = "";
    public string Comment { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string? ToolName { get; set; }
    public string? ToolId { get; set; }
    public string ReviewType { get; set; } = "";
}

public class UserSearchResultDto
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string? ProfilePictureUrl { get; set; }
    public string? LocationDisplay { get; set; }
    public bool IsVerified { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}