namespace frontend.Models;

public class AdminDashboardOverview
{
    public UserStats Users { get; set; } = new();
    public ToolStats Tools { get; set; } = new();
    public RentalStats Rentals { get; set; } = new();
    public RevenueStats Revenue { get; set; } = new();
    public DisputeStats Disputes { get; set; } = new();
}

public class UserStats
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int NewThisMonth { get; set; }
}

public class ToolStats
{
    public int Total { get; set; }
    public int Available { get; set; }
    public List<CategoryCount> Categories { get; set; } = new();
}

public class CategoryCount
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RentalStats
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Pending { get; set; }
    public int CompletedThisMonth { get; set; }
}

public class RevenueStats
{
    public decimal TotalThisMonth { get; set; }
    public decimal CommissionThisMonth { get; set; }
    public decimal PendingPayouts { get; set; }
}

public class DisputeStats
{
    public int Open { get; set; }
    public int InReview { get; set; }
    public int Escalated { get; set; }
    public int ResolvedThisMonth { get; set; }
}

public class ActivityItem
{
    public string Type { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
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

public class SuspendUserRequest
{
    public bool Suspend { get; set; }
    public string? Reason { get; set; }
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
    public bool IsSuspended { get; set; }
    public int ToolCount { get; set; }
    public int RentalCount { get; set; }
    public DateTime? LastActivity { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class FinancialSummary
{
    public DatePeriod Period { get; set; } = new();
    public RevenueData Revenue { get; set; } = new();
    public PayoutData Payouts { get; set; } = new();
    public TransactionData Transactions { get; set; } = new();
}

public class DatePeriod
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class RevenueData
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal RefundedDeposits { get; set; }
}

public class PayoutData
{
    public decimal TotalPaid { get; set; }
    public decimal PendingPayouts { get; set; }
    public int ScheduledPayouts { get; set; }
}

public class TransactionData
{
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Pending { get; set; }
    public int Cancelled { get; set; }
    public int Disputed { get; set; }
}

public class FraudAlertsResponse
{
    public List<FraudAlert> Alerts { get; set; } = new();
    public int TotalCount { get; set; }
}


public class AdminPaymentDto
{
    public string Id { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public PaymentProvider Provider { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? PayPalOrderId { get; set; }
    public string RenterName { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public Guid RentalId { get; set; }
    public string? FailureReason { get; set; }
    public bool IsRefunded { get; set; }
}

public class SuspendUserResponse
{
    public string UserId { get; set; } = string.Empty;
    public bool Suspended { get; set; }
    public string Message { get; set; } = string.Empty;
}

