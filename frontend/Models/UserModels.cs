using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace frontend.Models;

public class UserProfileDto
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? PhoneNumber { get; set; }
    
    // Phase 7: Comprehensive location system
    public string? LocationDisplay { get; set; }
    public string? LocationArea { get; set; }
    public string? LocationCity { get; set; }
    public string? LocationState { get; set; }
    public string? LocationCountry { get; set; }
    public decimal? LocationLat { get; set; }
    public decimal? LocationLng { get; set; }
    public int? LocationPrecisionRadius { get; set; }
    public string? LocationSource { get; set; }
    public string? LocationPrivacyLevel { get; set; }
    public DateTime? LocationUpdatedAt { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsVerified { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public class UpdateUserProfileRequest
{
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = "";

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = "";

    [Phone]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    // Phase 7: Comprehensive location system
    [StringLength(200)]
    public string? LocationDisplay { get; set; }
    
    [StringLength(100)]
    public string? LocationArea { get; set; }
    
    [StringLength(100)]
    public string? LocationCity { get; set; }
    
    [StringLength(100)]
    public string? LocationState { get; set; }
    
    [StringLength(100)]
    public string? LocationCountry { get; set; }
    
    public decimal? LocationLat { get; set; }
    
    public decimal? LocationLng { get; set; }
    
    public int? LocationPrecisionRadius { get; set; }
    
    public string? LocationSource { get; set; }
    
    public string? LocationPrivacyLevel { get; set; }
    
    public DateTime? LocationUpdatedAt { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? ProfilePictureUrl { get; set; }
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

public class UserSearchResult
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string DisplayName => FullName;
    public string? ProfilePictureUrl { get; set; }
    public string? LocationDisplay { get; set; }
    public bool IsVerified { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}