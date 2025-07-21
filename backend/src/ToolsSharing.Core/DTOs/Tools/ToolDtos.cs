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
}