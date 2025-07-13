using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Infrastructure.Data;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Core.DTOs.Admin;

namespace ToolsSharing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDisputeService _disputeService;
    private readonly IFraudDetectionService _fraudDetectionService;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context,
        IDisputeService disputeService,
        IFraudDetectionService fraudDetectionService,
        IPaymentService paymentService,
        ILogger<AdminController> logger)
    {
        _context = context;
        _disputeService = disputeService;
        _fraudDetectionService = fraudDetectionService;
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Get admin dashboard overview statistics
    /// </summary>
    [HttpGet("dashboard/overview")]
    public async Task<IActionResult> GetDashboardOverview()
    {
        try
        {
            var overview = new
            {
                Users = new
                {
                    Total = await _context.Users.CountAsync(),
                    Active = await _context.Users.Where(u => !u.IsDeleted).CountAsync(),
                    NewThisMonth = await _context.Users.Where(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-30)).CountAsync()
                },
                Tools = new
                {
                    Total = await _context.Tools.CountAsync(),
                    Available = await _context.Tools.Where(t => t.IsAvailable && !t.IsDeleted).CountAsync(),
                    Categories = await _context.Tools.Where(t => !t.IsDeleted).GroupBy(t => t.Category).Select(g => new { Category = g.Key, Count = g.Count() }).ToListAsync()
                },
                Rentals = new
                {
                    Total = await _context.Rentals.CountAsync(),
                    Active = await _context.Rentals.Where(r => r.Status == Core.Entities.RentalStatus.PickedUp).CountAsync(),
                    Pending = await _context.Rentals.Where(r => r.Status == Core.Entities.RentalStatus.Pending).CountAsync(),
                    CompletedThisMonth = await _context.Rentals.Where(r => r.Status == Core.Entities.RentalStatus.Returned && r.UpdatedAt >= DateTime.UtcNow.AddDays(-30)).CountAsync()
                },
                Revenue = new
                {
                    TotalThisMonth = await _context.Transactions
                        .Where(t => t.CreatedAt >= DateTime.UtcNow.AddDays(-30) && t.Status == Core.Entities.TransactionStatus.PaymentCompleted)
                        .SumAsync(t => t.TotalAmount),
                    CommissionThisMonth = await _context.Transactions
                        .Where(t => t.CreatedAt >= DateTime.UtcNow.AddDays(-30) && t.Status == Core.Entities.TransactionStatus.PaymentCompleted)
                        .SumAsync(t => t.CommissionAmount),
                    PendingPayouts = await _context.Transactions
                        .Where(t => t.Status == Core.Entities.TransactionStatus.PaymentCompleted && t.PayoutCompletedAt == null)
                        .SumAsync(t => t.OwnerPayoutAmount)
                },
                Disputes = new
                {
                    Open = await _context.Disputes.Where(d => d.Status == Core.Entities.DisputeStatus.Open).CountAsync(),
                    UnderReview = await _context.Disputes.Where(d => d.Status == Core.Entities.DisputeStatus.UnderReview).CountAsync(),
                    Escalated = await _context.Disputes.Where(d => d.Status == Core.Entities.DisputeStatus.Escalated).CountAsync(),
                    ResolvedThisMonth = await _context.Disputes.Where(d => d.Status == Core.Entities.DisputeStatus.Resolved && d.ResolvedAt >= DateTime.UtcNow.AddDays(-30)).CountAsync()
                }
            };

            return Ok(ApiResponse<object>.CreateSuccess(overview));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard overview");
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to load dashboard overview"));
        }
    }

    /// <summary>
    /// Get recent activity feed for admin dashboard
    /// </summary>
    [HttpGet("dashboard/activity")]
    public async Task<IActionResult> GetRecentActivity([FromQuery] int limit = 20)
    {
        try
        {
            var activities = new List<object>();

            // Recent rentals
            var recentRentals = await _context.Rentals
                .Include(r => r.Tool)
                .Include(r => r.Renter)
                .OrderByDescending(r => r.CreatedAt)
                .Take(limit / 4)
                .Select(r => new
                {
                    Type = "rental",
                    Timestamp = r.CreatedAt,
                    Description = $"{r.Renter.FirstName} {r.Renter.LastName} rented {r.Tool.Name}",
                    Status = r.Status.ToString(),
                    Id = r.Id
                })
                .ToListAsync();

            activities.AddRange(recentRentals);

            // Recent disputes
            var recentDisputes = await _context.Disputes
                .Include(d => d.Rental)
                    .ThenInclude(r => r.Tool)
                .OrderByDescending(d => d.CreatedAt)
                .Take(limit / 4)
                .Select(d => new
                {
                    Type = "dispute",
                    Timestamp = d.CreatedAt,
                    Description = $"Dispute opened for {d.Rental.Tool.Name}",
                    Status = d.Status.ToString(),
                    Id = d.Id
                })
                .ToListAsync();

            activities.AddRange(recentDisputes);

            // Recent payments
            var recentPayments = await _context.Payments
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit / 4)
                .Select(p => new
                {
                    Type = "payment",
                    Timestamp = p.CreatedAt,
                    Description = $"Payment of ${p.Amount:F2} {p.Status}",
                    Status = p.Status.ToString(),
                    Id = p.Id
                })
                .ToListAsync();

            activities.AddRange(recentPayments);

            // Recent user registrations
            var recentUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(limit / 4)
                .Select(u => new
                {
                    Type = "user",
                    Timestamp = u.CreatedAt,
                    Description = $"New user registered: {u.FirstName} {u.LastName}",
                    Status = "active",
                    Id = u.Id
                })
                .ToListAsync();

            activities.AddRange(recentUsers);

            // Sort all activities by timestamp
            var sortedActivities = activities.OrderByDescending(a => ((dynamic)a).Timestamp).Take(limit).ToList();

            return Ok(ApiResponse<List<object>>.CreateSuccess(sortedActivities));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent activity");
            return StatusCode(500, ApiResponse<List<object>>.CreateFailure("Failed to load recent activity"));
        }
    }

    /// <summary>
    /// Get list of all users with pagination and filtering
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null)
    {
        try
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => 
                    u.FirstName.Contains(search) || 
                    u.LastName.Contains(search) || 
                    u.Email.Contains(search));
            }

            var totalCount = await query.CountAsync();
            
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.PhoneNumber,
                    u.EmailConfirmed,
                    u.CreatedAt,
                    u.City,
                    u.Country,
                    IsDeleted = u.IsDeleted,
                    ToolCount = u.OwnedTools.Count(t => !t.IsDeleted),
                    RentalCount = u.RentalsAsRenter.Count()
                })
                .ToListAsync();

            var result = new
            {
                Items = users,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Ok(ApiResponse<object>.CreateSuccess(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to load users"));
        }
    }

    /// <summary>
    /// Get financial summary for admin dashboard
    /// </summary>
    [HttpGet("financial/summary")]
    public async Task<IActionResult> GetFinancialSummary([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            startDate ??= DateTime.UtcNow.AddMonths(-1);
            endDate ??= DateTime.UtcNow;

            var transactions = await _context.Transactions
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .ToListAsync();

            var summary = new
            {
                Period = new { StartDate = startDate, EndDate = endDate },
                Revenue = new
                {
                    TotalRevenue = transactions.Where(t => t.Status == Core.Entities.TransactionStatus.PaymentCompleted).Sum(t => t.TotalAmount),
                    TotalCommission = transactions.Where(t => t.Status == Core.Entities.TransactionStatus.PaymentCompleted).Sum(t => t.CommissionAmount),
                    TotalDeposits = transactions.Sum(t => t.SecurityDeposit),
                    RefundedDeposits = transactions.Where(t => t.DepositRefundedAt != null).Sum(t => t.SecurityDeposit)
                },
                Payouts = new
                {
                    TotalPaid = transactions.Where(t => t.PayoutCompletedAt != null).Sum(t => t.OwnerPayoutAmount),
                    PendingPayouts = transactions.Where(t => t.Status == Core.Entities.TransactionStatus.PaymentCompleted && t.PayoutCompletedAt == null).Sum(t => t.OwnerPayoutAmount),
                    ScheduledPayouts = transactions.Where(t => t.PayoutScheduledAt != null && t.PayoutCompletedAt == null).Count()
                },
                Transactions = new
                {
                    Total = transactions.Count,
                    Completed = transactions.Count(t => t.Status == Core.Entities.TransactionStatus.PaymentCompleted),
                    Pending = transactions.Count(t => t.Status == Core.Entities.TransactionStatus.Pending),
                    Cancelled = transactions.Count(t => t.Status == Core.Entities.TransactionStatus.Cancelled),
                    Disputed = transactions.Count(t => t.HasDispute)
                }
            };

            return Ok(ApiResponse<object>.CreateSuccess(summary));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting financial summary");
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to load financial summary"));
        }
    }

    /// <summary>
    /// Get fraud alerts and suspicious activity
    /// </summary>
    [HttpGet("fraud/alerts")]
    public async Task<IActionResult> GetFraudAlerts([FromQuery] int limit = 50)
    {
        try
        {
            var recentChecks = await _context.Set<Core.Entities.FraudCheck>()
                .Where(fc => fc.RiskScore > 50 || fc.Status == Core.Entities.FraudCheckStatus.Rejected || fc.Status == Core.Entities.FraudCheckStatus.UnderReview)
                .OrderByDescending(fc => fc.CreatedAt)
                .Take(limit)
                .Include(fc => fc.User)
                .Select(fc => new
                {
                    fc.Id,
                    fc.UserId,
                    UserName = fc.User != null ? $"{fc.User.FirstName} {fc.User.LastName}" : "Unknown",
                    fc.CheckType,
                    fc.RiskScore,
                    Passed = fc.Status == Core.Entities.FraudCheckStatus.Approved,
                    Status = fc.Status,
                    CheckedAt = fc.CreatedAt,
                    fc.PaymentId,
                    fc.RiskLevel
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.CreateSuccess(new { Alerts = recentChecks, TotalCount = recentChecks.Count }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fraud alerts");
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to load fraud alerts"));
        }
    }

    /// <summary>
    /// Get system health metrics for admin dashboard
    /// </summary>
    [HttpGet("system/health")]
    public async Task<IActionResult> GetSystemHealth()
    {
        try
        {
            // Get payment system health
            var failedPayments = await _context.Payments
                .Where(p => p.Status == Core.Entities.PaymentStatus.Failed)
                .CountAsync();

            var paymentsUnderReview = await _context.Payments
                .Where(p => p.Status == Core.Entities.PaymentStatus.UnderReview)
                .CountAsync();

            // Get user management metrics
            var unverifiedUsers = await _context.Users
                .Where(u => !u.EmailConfirmed && !u.IsDeleted)
                .CountAsync();

            var suspendedUsers = await _context.Users
                .Where(u => u.IsDeleted) // Using soft delete as suspension
                .CountAsync();

            // Calculate overall system health
            string systemHealth = "Healthy";
            if (failedPayments > 5 || suspendedUsers > 0)
            {
                systemHealth = "Warning";
            }
            else if (failedPayments > 0 || paymentsUnderReview > 5 || unverifiedUsers > 10)
            {
                systemHealth = "Caution";
            }

            var healthMetrics = new SystemHealthDto
            {
                Status = systemHealth,
                PaymentSystem = new PaymentSystemHealthDto
                {
                    FailedPayments = failedPayments,
                    PaymentsUnderReview = paymentsUnderReview
                },
                UserManagement = new UserManagementHealthDto
                {
                    UnverifiedUsers = unverifiedUsers,
                    SuspendedUsers = suspendedUsers
                },
                BackgroundServices = new BackgroundServicesHealthDto
                {
                    FailedJobs = 0, // Would integrate with background job system
                    EmailQueueErrors = 0 // Would integrate with email service
                }
            };

            return Ok(ApiResponse<SystemHealthDto>.CreateSuccess(healthMetrics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system health");
            return StatusCode(500, ApiResponse<SystemHealthDto>.CreateFailure("Failed to load system health"));
        }
    }

    /// <summary>
    /// Get users with filtering and pagination for admin management
    /// </summary>
    [HttpGet("users/manage")]
    public async Task<IActionResult> GetUsersForManagement(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var query = _context.Users.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => 
                    u.FirstName.Contains(search) || 
                    u.LastName.Contains(search) || 
                    u.Email.Contains(search));
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                switch (status.ToLower())
                {
                    case "unverified":
                        query = query.Where(u => !u.EmailConfirmed);
                        break;
                    case "suspended":
                        query = query.Where(u => u.IsDeleted);
                        break;
                    case "active":
                        query = query.Where(u => u.EmailConfirmed && !u.IsDeleted);
                        break;
                }
            }

            var totalCount = await query.CountAsync();
            
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.PhoneNumber,
                    u.EmailConfirmed,
                    u.CreatedAt,
                    u.City,
                    u.Country,
                    IsDeleted = u.IsDeleted,
                    IsSuspended = u.IsDeleted,
                    ToolCount = u.OwnedTools.Count(t => !t.IsDeleted),
                    RentalCount = u.RentalsAsRenter.Count(),
                    LastActivity = u.UpdatedAt
                })
                .ToListAsync();

            var result = new
            {
                Items = users,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Ok(ApiResponse<object>.CreateSuccess(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for management");
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to load users"));
        }
    }

    /// <summary>
    /// Suspend or unsuspend a user
    /// </summary>
    [HttpPost("users/{userId}/suspend")]
    public async Task<IActionResult> ToggleUserSuspension(string userId, [FromBody] SuspendUserRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.CreateFailure("User not found"));
            }

            user.IsDeleted = request.Suspend;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} suspension status changed to {Status} by admin", 
                userId, request.Suspend ? "suspended" : "active");

            return Ok(ApiResponse<object>.CreateSuccess(new { 
                UserId = userId, 
                Suspended = request.Suspend,
                Message = request.Suspend ? "User suspended successfully" : "User unsuspended successfully"
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user suspension for {UserId}", userId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to update user status"));
        }
    }

    /// <summary>
    /// Get payments requiring admin review
    /// </summary>
    [HttpGet("payments/review")]
    public async Task<IActionResult> GetPaymentsForReview([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _context.Payments
                .Where(p => p.Status == Core.Entities.PaymentStatus.UnderReview || 
                           p.Status == Core.Entities.PaymentStatus.Failed)
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Tool)
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Renter);

            var totalCount = await query.CountAsync();

            var payments = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Amount,
                    p.Status,
                    p.CreatedAt,
                    PayPalOrderId = p.ExternalOrderId,
                    RenterName = p.Rental != null ? $"{p.Rental.Renter.FirstName} {p.Rental.Renter.LastName}" : "Unknown",
                    ToolName = p.Rental != null ? p.Rental.Tool.Name : "Unknown",
                    RentalId = p.RentalId,
                    p.FailureReason
                })
                .ToListAsync();

            var result = new
            {
                Items = payments,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Ok(ApiResponse<object>.CreateSuccess(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments for review");
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to load payments"));
        }
    }
}