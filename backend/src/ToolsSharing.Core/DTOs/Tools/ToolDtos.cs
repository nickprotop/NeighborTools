using System.ComponentModel.DataAnnotations;

namespace ToolsSharing.Core.DTOs.Tools;

public class CreateToolRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Brand { get; set; }

    [MaxLength(50)]
    public string? Model { get; set; }

    [Required]
    [Range(0.01, 10000)]
    public decimal DailyRate { get; set; }

    [Range(0, 10000)]
    public decimal? WeeklyRate { get; set; }

    [Range(0, 10000)]
    public decimal? MonthlyRate { get; set; }

    [Required]
    [Range(0, 10000)]
    public decimal DepositRequired { get; set; }

    [Required]
    [MaxLength(20)]
    public string Condition { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [Range(1, 8760)] // 1 hour to 1 year
    public int? LeadTimeHours { get; set; }

    public List<string>? ImageUrls { get; set; }
    
    [MaxLength(500)]
    public string? Tags { get; set; }
}

public class UpdateToolRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Brand { get; set; }

    [MaxLength(50)]
    public string? Model { get; set; }

    [Required]
    [Range(0.01, 10000)]
    public decimal DailyRate { get; set; }

    [Range(0, 10000)]
    public decimal? WeeklyRate { get; set; }

    [Range(0, 10000)]
    public decimal? MonthlyRate { get; set; }

    [Required]
    [Range(0, 10000)]
    public decimal DepositRequired { get; set; }

    [Required]
    [MaxLength(20)]
    public string Condition { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [Range(1, 8760)] // 1 hour to 1 year
    public int? LeadTimeHours { get; set; }

    public List<string>? ImageUrls { get; set; }

    public bool IsAvailable { get; set; } = true;
    
    [MaxLength(500)]
    public string? Tags { get; set; }
}

public class ToolDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }
    public decimal WeeklyRate { get; set; }
    public decimal MonthlyRate { get; set; }
    public decimal DepositRequired { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public int? LeadTimeHours { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    
    // Additional properties for listing/searching
    public bool HasPendingApproval { get; set; }
    public string? RejectionReason { get; set; }
    
    // New feature properties
    public string Tags { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsFeatured { get; set; }
}

// New DTOs for reviews and search
public class CreateToolReviewRequest
{
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Comment { get; set; } = string.Empty;
}

public class ToolReviewDto
{
    public Guid Id { get; set; }
    public Guid ToolId { get; set; }
    public string ReviewerId { get; set; } = string.Empty;
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class SearchToolsQuery
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
    public string SortBy { get; set; } = "relevance"; // relevance, price_low, price_high, rating, newest, popular
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public class TagDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}