namespace ToolsSharing.Core.DTOs.Admin;

public class AdminDashboardOverviewDto
{
    public UserStatsDto Users { get; set; } = new();
    public ToolStatsDto Tools { get; set; } = new();
    public RentalStatsDto Rentals { get; set; } = new();
    public RevenueStatsDto Revenue { get; set; } = new();
    public DisputeStatsDto Disputes { get; set; } = new();
}

public class UserStatsDto
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int NewThisMonth { get; set; }
}

public class ToolStatsDto
{
    public int Total { get; set; }
    public int Available { get; set; }
    public List<CategoryCountDto> Categories { get; set; } = new();
}

public class CategoryCountDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RentalStatsDto
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Pending { get; set; }
    public int CompletedThisMonth { get; set; }
}

public class RevenueStatsDto
{
    public decimal TotalThisMonth { get; set; }
    public decimal CommissionThisMonth { get; set; }
    public decimal PendingPayouts { get; set; }
}

public class DisputeStatsDto
{
    public int Open { get; set; }
    public int InReview { get; set; }
    public int Escalated { get; set; }
    public int ResolvedThisMonth { get; set; }
}

public class ActivityItemDto
{
    public string Type { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
}

public class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public bool IsDeleted { get; set; }
    public int ToolCount { get; set; }
    public int RentalCount { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class FinancialSummaryDto
{
    public DateRangeDto Period { get; set; } = new();
    public RevenueSummaryDto Revenue { get; set; } = new();
    public PayoutSummaryDto Payouts { get; set; } = new();
    public TransactionSummaryDto Transactions { get; set; } = new();
}

public class DateRangeDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class RevenueSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal RefundedDeposits { get; set; }
}

public class PayoutSummaryDto
{
    public decimal TotalPaid { get; set; }
    public decimal PendingPayouts { get; set; }
    public int ScheduledPayouts { get; set; }
}

public class TransactionSummaryDto
{
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Pending { get; set; }
    public int Failed { get; set; }
    public int Disputed { get; set; }
}

public class FraudAlertDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string CheckType { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public bool Passed { get; set; }
    public List<string> FailureReasons { get; set; } = new();
    public DateTime CheckedAt { get; set; }
    public Guid? PaymentId { get; set; }
    public decimal? Amount { get; set; }
}

public class SuspendUserRequest
{
    public bool Suspend { get; set; }
    public string? Reason { get; set; }
}

public class SystemHealthDto
{
    public string Status { get; set; } = string.Empty;
    public PaymentSystemHealthDto PaymentSystem { get; set; } = new();
    public UserManagementHealthDto UserManagement { get; set; } = new();
    public BackgroundServicesHealthDto BackgroundServices { get; set; } = new();
}

public class PaymentSystemHealthDto
{
    public int FailedPayments { get; set; }
    public int PaymentsUnderReview { get; set; }
}

public class UserManagementHealthDto
{
    public int UnverifiedUsers { get; set; }
    public int SuspendedUsers { get; set; }
}

public class BackgroundServicesHealthDto
{
    public int FailedJobs { get; set; }
    public int EmailQueueErrors { get; set; }
}