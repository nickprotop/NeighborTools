namespace frontend.Models;

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
    public bool IsAvailable { get; set; }
    public int? LeadTimeHours { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = new();
    
    // Additional properties for frontend compatibility
    public double? Rating { get; set; }
    public int ReviewCount { get; set; }
    public DateTime CreatedAt { get; set; }
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
    public string Location { get; set; } = string.Empty;
    public int? LeadTimeHours { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}

public class CreateToolRequest : ToolRequestBase
{
}

public class UpdateToolRequest : ToolRequestBase
{
    public bool IsAvailable { get; set; } = true;
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
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public Tool? Tool { get; set; }
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