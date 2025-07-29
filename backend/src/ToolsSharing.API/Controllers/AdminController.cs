using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Infrastructure.Data;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.DTOs.Admin;
using ToolsSharing.Core.Features.Messaging;
using ToolsSharing.Core.Features.Users;
using ToolsSharing.Core.DTOs.Messaging;
using ToolsSharing.Core.DTOs.Dispute;
using ToolsSharing.Core.DTOs.Tools;
using ToolsSharing.Core.DTOs.Bundle;
using ToolsSharing.Core.Entities;
using UpdateDisputeStatusRequest = ToolsSharing.Core.DTOs.Dispute.UpdateDisputeStatusRequest;
using ResolveDisputeRequest = ToolsSharing.Core.Interfaces.ResolveDisputeRequest;
using MapsterMapper;

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
    private readonly IMessageService _messageService;
    private readonly IContentModerationService _contentModerationService;
    private readonly IToolsService _toolsService;
    private readonly IBundleService _bundleService;
    private readonly IMapper _mapper;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context,
        IDisputeService disputeService,
        IFraudDetectionService fraudDetectionService,
        IPaymentService paymentService,
        IMessageService messageService,
        IContentModerationService contentModerationService,
        IToolsService toolsService,
        IBundleService bundleService,
        IMapper mapper,
        ILogger<AdminController> logger)
    {
        _context = context;
        _disputeService = disputeService;
        _fraudDetectionService = fraudDetectionService;
        _paymentService = paymentService;
        _messageService = messageService;
        _contentModerationService = contentModerationService;
        _toolsService = toolsService;
        _bundleService = bundleService;
        _mapper = mapper;
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

    // MESSAGING ADMINISTRATION ENDPOINTS

    /// <summary>
    /// Get message statistics for admin dashboard
    /// </summary>
    [HttpGet("messaging/statistics")]
    public async Task<IActionResult> GetMessageStatistics()
    {
        try
        {
            // Get global message statistics for admin dashboard (includes ALL messages for admin oversight)
            var statistics = new MessageStatisticsDto
            {
                TotalMessages = await _context.Messages.Where(m => !m.IsDeleted).CountAsync(), // Total includes blocked for admin view
                UnreadMessages = await _context.Messages.Where(m => !m.IsRead && !m.IsDeleted && !m.IsBlocked).CountAsync(), // Unread excludes blocked (they're never delivered)
                SentMessages = await _context.Messages.Where(m => !m.IsDeleted && !m.IsBlocked).CountAsync(), // Sent excludes blocked (they're never delivered)
                ReceivedMessages = await _context.Messages.Where(m => !m.IsDeleted && !m.IsBlocked).CountAsync(), // Received excludes blocked (they're never delivered)
                ArchivedMessages = await _context.Messages.Where(m => m.IsArchived && !m.IsDeleted && !m.IsBlocked).CountAsync(), // Archived excludes blocked
                ModeratedMessages = await _context.Messages.Where(m => m.IsModerated && !m.IsDeleted).CountAsync(), // Moderated includes blocked for admin oversight
                BlockedMessages = await _context.Messages.Where(m => m.IsBlocked && !m.IsDeleted).CountAsync(), // New: Blocked message count
                ConversationCount = await _context.Conversations.CountAsync(),
                LastMessageAt = await _context.Messages
                    .Where(m => !m.IsDeleted && !m.IsBlocked) // Last message excludes blocked (they're never delivered)
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => (DateTime?)m.CreatedAt)
                    .FirstOrDefaultAsync()
            };

            return Ok(ApiResponse<MessageStatisticsDto>.CreateSuccess(statistics, "Message statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting message statistics");
            return StatusCode(500, ApiResponse<MessageStatisticsDto>.CreateFailure("Failed to load message statistics"));
        }
    }

    /// <summary>
    /// Get moderation statistics for admin dashboard
    /// </summary>
    [HttpGet("messaging/moderation-statistics")]
    public async Task<IActionResult> GetModerationStatistics()
    {
        try
        {
            var statistics = await _contentModerationService.GetModerationStatisticsAsync();
            return Ok(ApiResponse<object>.CreateSuccess(statistics, "Moderation statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting moderation statistics");
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to load moderation statistics"));
        }
    }

    /// <summary>
    /// Search users for admin purposes
    /// </summary>
    [HttpGet("users/search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string? query = null, [FromQuery] int limit = 100)
    {
        try
        {
            var usersQuery = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                usersQuery = usersQuery.Where(u => 
                    u.FirstName.Contains(query) ||
                    u.LastName.Contains(query) ||
                    u.Email.Contains(query));
            }

            var users = await usersQuery
                .Select(u => new UserSearchResultDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    ProfilePictureUrl = u.ProfilePictureUrl,
                    PublicLocation = u.PublicLocation,
                    IsVerified = u.EmailConfirmed, // Use EmailConfirmed from IdentityUser
                    AverageRating = 0, // Could calculate if needed
                    ReviewCount = 0 // Could calculate if needed
                })
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Take(limit)
                .ToListAsync();

            return Ok(ApiResponse<List<UserSearchResultDto>>.CreateSuccess(users, "Users retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users");
            return StatusCode(500, ApiResponse<List<UserSearchResultDto>>.CreateFailure("Failed to search users"));
        }
    }

    /// <summary>
    /// Search messages with admin privileges
    /// </summary>
    [HttpGet("messaging/search")]
    public async Task<IActionResult> SearchMessages(
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // Admin search should include ALL messages (not filtered by user)
            var messagesQuery = _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Recipient)
                .Include(m => m.Attachments)
                .Where(m => !m.IsDeleted); // Admin can see blocked messages for review

            // Apply user filter only if specific user is selected
            if (!string.IsNullOrEmpty(userId))
            {
                messagesQuery = messagesQuery.Where(m => m.SenderId == userId || m.RecipientId == userId);
            }

            // Apply search term filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                messagesQuery = messagesQuery.Where(m => 
                    m.Subject.Contains(searchTerm) || 
                    m.Content.Contains(searchTerm) ||
                    m.Sender.FirstName.Contains(searchTerm) ||
                    m.Sender.LastName.Contains(searchTerm) ||
                    m.Recipient.FirstName.Contains(searchTerm) ||
                    m.Recipient.LastName.Contains(searchTerm));
            }

            // Apply date filters
            if (fromDate.HasValue)
            {
                messagesQuery = messagesQuery.Where(m => m.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                messagesQuery = messagesQuery.Where(m => m.CreatedAt <= toDate.Value);
            }

            // Apply pagination and sorting
            messagesQuery = messagesQuery
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var messages = await messagesQuery.ToListAsync();
            var messageDtos = _mapper.Map<List<MessageSummaryDto>>(messages);

            return Ok(ApiResponse<List<MessageSummaryDto>>.CreateSuccess(messageDtos, "Messages retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching messages");
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to search messages"));
        }
    }

    /// <summary>
    /// Get moderated messages for admin review
    /// </summary>
    [HttpGet("messaging/moderated-messages")]
    public async Task<IActionResult> GetModeratedMessages(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var query = new GetModeratedMessagesQuery(
                Page: page,
                PageSize: pageSize,
                FromDate: fromDate,
                ToDate: toDate
            );

            var result = await _messageService.GetModeratedMessagesAsync(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting moderated messages");
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to load moderated messages"));
        }
    }

    /// <summary>
    /// Get blocked messages for admin review
    /// </summary>
    [HttpGet("messaging/blocked-messages")]
    public async Task<IActionResult> GetBlockedMessages(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var messagesQuery = _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Recipient)
                .Where(m => m.IsBlocked && !m.IsDeleted);

            if (fromDate.HasValue)
            {
                messagesQuery = messagesQuery.Where(m => m.ModeratedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                messagesQuery = messagesQuery.Where(m => m.ModeratedAt <= toDate.Value);
            }

            messagesQuery = messagesQuery
                .OrderByDescending(m => m.ModeratedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var messages = await messagesQuery.ToListAsync();
            var messageDtos = _mapper.Map<List<MessageSummaryDto>>(messages);

            return Ok(ApiResponse<List<MessageSummaryDto>>.CreateSuccess(messageDtos, "Blocked messages retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blocked messages");
            return StatusCode(500, ApiResponse<List<MessageSummaryDto>>.CreateFailure("Failed to load blocked messages"));
        }
    }

    /// <summary>
    /// Get individual message for admin review
    /// </summary>
    [HttpGet("messaging/message/{messageId}")]
    public async Task<IActionResult> GetMessageById(Guid messageId)
    {
        try
        {
            // Security: Admin access is already validated by [Authorize(Roles = "Admin")] attribute
            // Use dedicated admin method that bypasses user access checks
            var result = await _messageService.GetMessageByIdForAdminAsync(messageId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting message {MessageId}", messageId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to load message"));
        }
    }

    /// <summary>
    /// Approve a moderated message
    /// </summary>
    [HttpPost("messaging/approve/{messageId}")]
    public async Task<IActionResult> ApproveMessage(Guid messageId)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var command = new ModerateMessageCommand(
                ModeratorId: userId,
                MessageId: messageId,
                Reason: "Approved by admin",
                ModifiedContent: null
            );

            // First get the message to clear moderation flags
            var message = await _context.Messages.FindAsync(messageId);
            if (message == null)
            {
                return NotFound(ApiResponse<object>.CreateFailure("Message not found"));
            }

            // Clear moderation flags to approve
            message.IsModerated = false;
            message.ModerationReason = null;
            message.ModeratedBy = userId;
            message.ModeratedAt = DateTime.UtcNow;
            message.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.CreateSuccess(new { MessageId = messageId }, "Message approved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving message {MessageId}", messageId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to approve message"));
        }
    }

    /// <summary>
    /// Moderate a message with custom content
    /// </summary>
    [HttpPost("messaging/moderate/{messageId}")]
    public async Task<IActionResult> ModerateMessage(Guid messageId, [FromBody] ModerateMessageRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var command = new ModerateMessageCommand(
                ModeratorId: userId,
                MessageId: messageId,
                Reason: request.Reason,
                ModifiedContent: request.ModifiedContent
            );

            var result = await _messageService.ModerateMessageAsync(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moderating message {MessageId}", messageId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to moderate message"));
        }
    }

    /// <summary>
    /// Block a message completely
    /// </summary>
    [HttpPost("messaging/block/{messageId}")]
    public async Task<IActionResult> BlockMessage(Guid messageId, [FromBody] BlockMessageRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var message = await _context.Messages.FindAsync(messageId);
            if (message == null)
            {
                return NotFound(ApiResponse<object>.CreateFailure("Message not found"));
            }

            // Mark message as deleted/blocked
            message.IsDeleted = true;
            message.IsModerated = true;
            message.ModerationReason = $"Blocked by admin: {request.Reason}";
            message.ModeratedBy = userId;
            message.ModeratedAt = DateTime.UtcNow;
            message.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.CreateSuccess(new { MessageId = messageId }, "Message blocked successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blocking message {MessageId}", messageId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to block message"));
        }
    }

    /// <summary>
    /// Get disputes for admin management with filtering and pagination
    /// </summary>
    [HttpGet("disputes")]
    public async Task<IActionResult> GetAdminDisputes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DisputeStatus? status = null,
        [FromQuery] DisputeType? type = null,
        [FromQuery] DisputeCategory? category = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = true)
    {
        try
        {
            var request = new GetDisputesRequest
            {
                UserId = string.Empty, // Admin request - will be ignored by admin method
                PageNumber = page,
                PageSize = pageSize,
                Status = status,
                Type = type,
                Category = category,
                StartDate = startDate,
                EndDate = endDate,
                SortBy = sortBy,
                SortDescending = sortDescending
            };

            // Use dedicated admin method that shows all disputes
            var result = await _disputeService.GetDisputesForAdminAsync(request);
            
            return Ok(new
            {
                success = result.Success,
                data = result.Data,
                message = result.Message,
                totalCount = result.TotalCount,
                pageNumber = page,
                pageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving disputes for admin");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving disputes"
            });
        }
    }

    /// <summary>
    /// Start review process for a dispute (Admin action)
    /// </summary>
    [HttpPost("disputes/{disputeId}/start-review")]
    public async Task<IActionResult> StartDisputeReview(Guid disputeId)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var updateRequest = new UpdateDisputeStatusRequest
            {
                DisputeId = disputeId,
                Status = DisputeStatus.UnderReview,
                UpdatedBy = userId,
                Reason = "Admin started review process",
                Notes = $"Review started by admin user {userId}"
            };

            var result = await _disputeService.UpdateDisputeStatusAsync(updateRequest);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.CreateFailure(result.Message ?? "Failed to start review"));
            }

            return Ok(ApiResponse<object>.CreateSuccess(new { DisputeId = disputeId }, "Dispute review started successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting review for dispute {DisputeId}", disputeId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to start dispute review"));
        }
    }

    /// <summary>
    /// Escalate dispute to PayPal (Admin action)
    /// </summary>
    [HttpPost("disputes/{disputeId}/escalate")]
    public async Task<IActionResult> EscalateDispute(Guid disputeId)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _disputeService.EscalateToPayPalAsync(disputeId, userId);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.CreateFailure(result.ErrorMessage ?? "Failed to escalate dispute"));
            }

            return Ok(ApiResponse<object>.CreateSuccess(
                new { DisputeId = disputeId, ExternalDisputeId = result.ExternalDisputeId }, 
                "Dispute escalated to PayPal successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating dispute {DisputeId}", disputeId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to escalate dispute"));
        }
    }

    /// <summary>
    /// Resolve dispute with admin privileges
    /// </summary>
    [HttpPost("disputes/{disputeId}/resolve")]
    public async Task<IActionResult> ResolveDispute(Guid disputeId, [FromBody] ResolveDisputeRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            request.DisputeId = disputeId;
            request.ResolvedBy = userId;

            var result = await _disputeService.ResolveDisputeAsync(request);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.CreateFailure(result.ErrorMessage ?? "Failed to resolve dispute"));
            }

            return Ok(ApiResponse<object>.CreateSuccess(
                new { DisputeId = disputeId, RefundTransactionId = result.RefundTransactionId }, 
                "Dispute resolved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving dispute {DisputeId}", disputeId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to resolve dispute"));
        }
    }

    /// <summary>
    /// Sync dispute with PayPal (Admin action)
    /// </summary>
    [HttpPost("disputes/{disputeId}/sync-paypal")]
    public async Task<IActionResult> SyncPayPalDispute(Guid disputeId)
    {
        try
        {
            var dispute = await _context.Disputes.FirstOrDefaultAsync(d => d.Id == disputeId);
            if (dispute?.ExternalDisputeId == null)
            {
                return BadRequest(ApiResponse<object>.CreateFailure("Dispute has no external PayPal dispute ID"));
            }

            var result = await _disputeService.SyncPayPalDisputeAsync(dispute.ExternalDisputeId);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.CreateFailure(result.ErrorMessage ?? "Failed to sync with PayPal"));
            }

            return Ok(ApiResponse<object>.CreateSuccess(
                new { DisputeId = disputeId, Dispute = result.Dispute }, 
                "PayPal sync completed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing PayPal dispute {DisputeId}", disputeId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to sync with PayPal"));
        }
    }

    /// <summary>
    /// Get top message senders for analytics
    /// </summary>
    [HttpGet("messaging/top-senders")]
    public async Task<IActionResult> GetTopMessageSenders([FromQuery] int limit = 20)
    {
        try
        {
            var topSenders = await _context.Messages
                .Where(m => !m.IsDeleted && !m.IsBlocked)
                .GroupBy(m => m.SenderId)
                .Select(g => new
                {
                    UserId = g.Key,
                    MessageCount = g.Count(),
                    ViolationCount = g.Count(m => m.IsModerated),
                    LastMessageAt = g.Max(m => m.CreatedAt)
                })
                .OrderByDescending(s => s.MessageCount)
                .Take(limit)
                .ToListAsync();

            // Get user names
            var userIds = topSenders.Select(s => s.UserId).ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, UserName = $"{u.FirstName} {u.LastName}", u.IsDeleted })
                .ToListAsync();

            var result = topSenders.Select(s => new
            {
                s.UserId,
                UserName = users.FirstOrDefault(u => u.Id == s.UserId)?.UserName ?? "Unknown User",
                s.MessageCount,
                s.ViolationCount,
                IsActive = !users.FirstOrDefault(u => u.Id == s.UserId)?.IsDeleted ?? false,
                s.LastMessageAt
            }).ToList();

            return Ok(ApiResponse<object>.CreateSuccess(result, "Top senders retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top message senders");
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to load top senders"));
        }
    }

    /// <summary>
    /// Get individual user details for admin
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUserById(string userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.OwnedTools.Where(t => !t.IsDeleted))
                .Include(u => u.RentalsAsRenter)
                    .ThenInclude(r => r.Tool)
                .Include(u => u.RentalsAsOwner)
                    .ThenInclude(r => r.Tool)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(ApiResponse<object>.CreateFailure("User not found"));
            }

            var result = new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.EmailConfirmed,
                user.CreatedAt,
                user.UpdatedAt,
                user.City,
                user.Country,
                user.Address,
                user.PostalCode,
                user.PublicLocation,
                user.DateOfBirth,
                user.ProfilePictureUrl,
                IsDeleted = user.IsDeleted,
                IsSuspended = user.IsDeleted,
                
                // Statistics
                ToolCount = user.OwnedTools.Count,
                RentalCount = user.RentalsAsRenter.Count,
                OwnerRentalCount = user.RentalsAsOwner.Count,
                
                // Tools owned
                Tools = user.OwnedTools.Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Category,
                    t.IsAvailable,
                    t.CreatedAt
                }).ToList(),
                
                // Recent activity
                RecentRentals = user.RentalsAsRenter
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .Select(r => new
                    {
                        r.Id,
                        r.Status,
                        r.CreatedAt,
                        r.StartDate,
                        r.EndDate,
                        ToolName = r.Tool.Name
                    }).ToList()
            };

            return Ok(ApiResponse<object>.CreateSuccess(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to load user"));
        }
    }

    /// <summary>
    /// Update user details as admin
    /// </summary>
    [HttpPut("users/{userId}")]
    public async Task<IActionResult> UpdateUser(string userId, [FromBody] AdminUpdateUserRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.CreateFailure("User not found"));
            }

            // Update user properties
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.PhoneNumber;
            user.City = request.City;
            user.Country = request.Country;
            user.Address = request.Address;
            user.PostalCode = request.PostalCode;
            user.PublicLocation = request.PublicLocation;
            if (request.DateOfBirth.HasValue)
                user.DateOfBirth = request.DateOfBirth.Value;
            user.EmailConfirmed = request.EmailConfirmed;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} updated by admin", userId);

            return Ok(ApiResponse<object>.CreateSuccess(new { 
                UserId = userId,
                Message = "User updated successfully"
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to update user"));
        }
    }

    /// <summary>
    /// Verify user email
    /// </summary>
    [HttpPost("users/{userId}/verify")]
    public async Task<IActionResult> VerifyUser(string userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.CreateFailure("User not found"));
            }

            user.EmailConfirmed = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} verified by admin", userId);

            return Ok(ApiResponse<object>.CreateSuccess(new { 
                UserId = userId, 
                Verified = true,
                Message = "User verified successfully"
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying user {UserId}", userId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to verify user"));
        }
    }

    /// <summary>
    /// Get pending tools awaiting approval
    /// </summary>
    [HttpGet("tools/pending")]
    public async Task<IActionResult> GetPendingTools([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _context.Tools
                .Include(t => t.Owner)
                .Include(t => t.Images)
                .Where(t => t.PendingApproval && !t.IsDeleted);

            var totalCount = await query.CountAsync();
            var tools = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Use the same logic as ToolsService.MapToolToDto for location fallback
            var toolDtos = tools.Select(tool =>
            {
                var toolDto = _mapper.Map<ToolDto>(tool);
                // Apply location fallback logic - use tool's location if set, otherwise fall back to owner's public location
                toolDto.Location = !string.IsNullOrEmpty(tool.Location) ? tool.Location : tool.Owner?.PublicLocation;
                return toolDto;
            }).ToList();

            return Ok(ApiResponse<object>.CreateSuccess(new
            {
                Items = toolDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            }, "Pending tools retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending tools");
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to retrieve pending tools"));
        }
    }

    /// <summary>
    /// Approve a tool
    /// </summary>
    [HttpPost("tools/{toolId}/approve")]
    public async Task<IActionResult> ApproveTool(Guid toolId, [FromBody] ApprovalRequest request)
    {
        try
        {
            var tool = await _context.Tools.FindAsync(toolId);
            if (tool == null)
            {
                return NotFound(ApiResponse<object>.CreateFailure("Tool not found"));
            }

            var adminUserId = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
            
            tool.IsApproved = true;
            tool.PendingApproval = false;
            tool.ApprovedAt = DateTime.UtcNow;
            tool.ApprovedById = adminUserId;
            tool.RejectionReason = null;
            tool.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Tool {ToolId} approved by admin {AdminId}", toolId, adminUserId);

            return Ok(ApiResponse<object>.CreateSuccess(new { ToolId = toolId }, "Tool approved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving tool {ToolId}", toolId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to approve tool"));
        }
    }

    /// <summary>
    /// Reject a tool
    /// </summary>
    [HttpPost("tools/{toolId}/reject")]
    public async Task<IActionResult> RejectTool(Guid toolId, [FromBody] RejectionRequest request)
    {
        try
        {
            var tool = await _context.Tools.FindAsync(toolId);
            if (tool == null)
            {
                return NotFound(ApiResponse<object>.CreateFailure("Tool not found"));
            }

            var adminUserId = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
            
            tool.IsApproved = false;
            tool.PendingApproval = false;
            tool.ApprovedAt = null;
            tool.ApprovedById = adminUserId;
            tool.RejectionReason = request.Reason;
            tool.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Tool {ToolId} rejected by admin {AdminId} with reason: {Reason}", toolId, adminUserId, request.Reason);

            return Ok(ApiResponse<object>.CreateSuccess(new { ToolId = toolId }, "Tool rejected successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting tool {ToolId}", toolId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to reject tool"));
        }
    }

    /// <summary>
    /// Get pending bundles awaiting approval
    /// </summary>
    [HttpGet("bundles/pending")]
    public async Task<IActionResult> GetPendingBundles([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _context.Bundles
                .Include(b => b.User)
                .Include(b => b.BundleTools)
                    .ThenInclude(bt => bt.Tool)
                        .ThenInclude(t => t.Owner)
                .Where(b => b.PendingApproval && !b.IsDeleted);

            var totalCount = await query.CountAsync();
            var bundles = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Apply the same logic as BundleService for location fallback
            var bundleDtos = bundles.Select(bundle =>
            {
                var bundleDto = _mapper.Map<BundleDto>(bundle);
                // Apply location fallback logic - use bundle's location if set, otherwise fall back to owner's public location
                bundleDto.Location = !string.IsNullOrEmpty(bundle.Location) ? bundle.Location : bundle.User?.PublicLocation;
                return bundleDto;
            }).ToList();

            return Ok(ApiResponse<object>.CreateSuccess(new
            {
                Items = bundleDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            }, "Pending bundles retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending bundles");
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to retrieve pending bundles"));
        }
    }

    /// <summary>
    /// Approve a bundle
    /// </summary>
    [HttpPost("bundles/{bundleId}/approve")]
    public async Task<IActionResult> ApproveBundle(Guid bundleId, [FromBody] ApprovalRequest request)
    {
        try
        {
            var bundle = await _context.Bundles.FindAsync(bundleId);
            if (bundle == null)
            {
                return NotFound(ApiResponse<object>.CreateFailure("Bundle not found"));
            }

            var adminUserId = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
            
            bundle.IsApproved = true;
            bundle.PendingApproval = false;
            bundle.ApprovedAt = DateTime.UtcNow;
            bundle.ApprovedById = adminUserId;
            bundle.RejectionReason = null;
            bundle.IsPublished = true; // Auto-publish when approved
            bundle.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Bundle {BundleId} approved by admin {AdminId}", bundleId, adminUserId);

            return Ok(ApiResponse<object>.CreateSuccess(new { BundleId = bundleId }, "Bundle approved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving bundle {BundleId}", bundleId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to approve bundle"));
        }
    }

    /// <summary>
    /// Reject a bundle
    /// </summary>
    [HttpPost("bundles/{bundleId}/reject")]
    public async Task<IActionResult> RejectBundle(Guid bundleId, [FromBody] RejectionRequest request)
    {
        try
        {
            var bundle = await _context.Bundles.FindAsync(bundleId);
            if (bundle == null)
            {
                return NotFound(ApiResponse<object>.CreateFailure("Bundle not found"));
            }

            var adminUserId = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
            
            bundle.IsApproved = false;
            bundle.PendingApproval = false;
            bundle.ApprovedAt = null;
            bundle.ApprovedById = adminUserId;
            bundle.RejectionReason = request.Reason;
            bundle.IsPublished = false;
            bundle.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Bundle {BundleId} rejected by admin {AdminId} with reason: {Reason}", bundleId, adminUserId, request.Reason);

            return Ok(ApiResponse<object>.CreateSuccess(new { BundleId = bundleId }, "Bundle rejected successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting bundle {BundleId}", bundleId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to reject bundle"));
        }
    }
}

/// <summary>
/// Request models for messaging moderation (specific to admin actions)
/// </summary>
public class BlockMessageRequest
{
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request model for admin user updates
/// </summary>
public class AdminUpdateUserRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? PublicLocation { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public bool EmailConfirmed { get; set; }
}

/// <summary>
/// Request model for approving tools/bundles
/// </summary>
public class ApprovalRequest
{
    public string? Notes { get; set; }
}

/// <summary>
/// Request model for rejecting tools/bundles
/// </summary>
public class RejectionRequest
{
    public string Reason { get; set; } = string.Empty;
}