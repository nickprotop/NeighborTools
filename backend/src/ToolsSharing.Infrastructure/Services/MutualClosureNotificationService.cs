using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ToolsSharing.Core.DTOs.Dispute;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models.EmailNotifications;
using ToolsSharing.Core.Entities;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services;

/// <summary>
/// Notification service for mutual dispute closure workflow
/// Extends the email notification service with mutual closure specific notifications
/// </summary>
public class MutualClosureNotificationService : IMutualClosureNotificationService
{
    private readonly IEmailNotificationService _emailService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<MutualClosureNotificationService> _logger;

    public MutualClosureNotificationService(
        IEmailNotificationService emailService,
        ApplicationDbContext context,
        UserManager<User> userManager,
        ILogger<MutualClosureNotificationService> logger)
    {
        _emailService = emailService;
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task SendMutualClosureRequestNotificationAsync(MutualClosureDto mutualClosure)
    {
        try
        {
            _logger.LogInformation("Sending mutual closure request notification for closure {MutualClosureId}", mutualClosure.Id);
            
            // Get recipient user details
            var responseRequiredUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == mutualClosure.ResponseRequiredFromUserId);
                
            if (responseRequiredUser?.Email == null)
            {
                _logger.LogWarning("Cannot send notification - user not found or has no email: {UserId}", 
                    mutualClosure.ResponseRequiredFromUserId);
                return;
            }

            var notification = new MutualClosureRequestNotification
            {
                RecipientEmail = responseRequiredUser.Email,
                RecipientName = $"{responseRequiredUser.FirstName} {responseRequiredUser.LastName}",
                InitiatedByUserName = mutualClosure.InitiatedByUserName,
                ResponseRequiredFromUserName = mutualClosure.ResponseRequiredFromUserName,
                DisputeTitle = mutualClosure.DisputeTitle,
                ProposedResolution = mutualClosure.ProposedResolution,
                AgreedRefundAmount = mutualClosure.AgreedRefundAmount,
                ExpiresAt = mutualClosure.ExpiresAt,
                HoursToRespond = mutualClosure.HoursUntilExpiry,
                DisputeUrl = $"https://localhost:5001/disputes/{mutualClosure.DisputeId}"
            };

            await _emailService.SendNotificationAsync(notification);
            _logger.LogInformation("Mutual closure request notification sent successfully to {Email}", responseRequiredUser.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending mutual closure request notification for closure {MutualClosureId}", mutualClosure.Id);
        }
    }

    public async Task SendMutualClosureResponseNotificationAsync(MutualClosureDto mutualClosure, bool accepted)
    {
        try
        {
            _logger.LogInformation("Sending mutual closure response notification for closure {MutualClosureId}, accepted: {Accepted}", 
                mutualClosure.Id, accepted);
            
            // Get initiator user details
            var initiatorUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == mutualClosure.InitiatedByUserId);
                
            if (initiatorUser?.Email == null)
            {
                _logger.LogWarning("Cannot send notification - user not found or has no email: {UserId}", 
                    mutualClosure.InitiatedByUserId);
                return;
            }

            var notification = new MutualClosureResponseNotification
            {
                RecipientEmail = initiatorUser.Email,
                RecipientName = $"{initiatorUser.FirstName} {initiatorUser.LastName}",
                RespondingUserName = mutualClosure.ResponseRequiredFromUserName,
                InitiatedByUserName = mutualClosure.InitiatedByUserName,
                DisputeTitle = mutualClosure.DisputeTitle,
                ProposedResolution = mutualClosure.ProposedResolution,
                WasAccepted = accepted,
                ResponseMessage = mutualClosure.ResponseMessage ?? string.Empty,
                RejectionReason = mutualClosure.RejectionReason ?? string.Empty,
                RefundAmount = mutualClosure.AgreedRefundAmount,
                DisputeUrl = $"https://localhost:5001/disputes/{mutualClosure.DisputeId}"
            };

            await _emailService.SendNotificationAsync(notification);
            _logger.LogInformation("Mutual closure response notification sent successfully to {Email}", initiatorUser.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending mutual closure response notification for closure {MutualClosureId}", mutualClosure.Id);
        }
    }

    public async Task SendMutualClosureExpiryReminderAsync(MutualClosureDto mutualClosure)
    {
        try
        {
            _logger.LogInformation("Sending mutual closure expiry reminder for closure {MutualClosureId}", mutualClosure.Id);
            
            // Get recipient user details
            var responseRequiredUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == mutualClosure.ResponseRequiredFromUserId);
                
            if (responseRequiredUser?.Email == null)
            {
                _logger.LogWarning("Cannot send expiry reminder - user not found or has no email: {UserId}", 
                    mutualClosure.ResponseRequiredFromUserId);
                return;
            }

            var notification = new MutualClosureExpiryReminderNotification
            {
                RecipientEmail = responseRequiredUser.Email,
                RecipientName = $"{responseRequiredUser.FirstName} {responseRequiredUser.LastName}",
                InitiatedByUserName = mutualClosure.InitiatedByUserName,
                ResponseRequiredFromUserName = mutualClosure.ResponseRequiredFromUserName,
                DisputeTitle = mutualClosure.DisputeTitle,
                ProposedResolution = mutualClosure.ProposedResolution,
                ExpiresAt = mutualClosure.ExpiresAt,
                HoursRemaining = mutualClosure.HoursUntilExpiry,
                DisputeUrl = $"https://localhost:5001/disputes/{mutualClosure.DisputeId}"
            };

            await _emailService.SendNotificationAsync(notification);
            _logger.LogInformation("Mutual closure expiry reminder sent successfully to {Email}", responseRequiredUser.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending mutual closure expiry reminder for closure {MutualClosureId}", mutualClosure.Id);
        }
    }

    public async Task SendMutualClosureExpiredNotificationAsync(MutualClosureDto mutualClosure)
    {
        try
        {
            _logger.LogInformation("Sending mutual closure expired notification for closure {MutualClosureId}", mutualClosure.Id);
            
            // Send notification to both parties that the request has expired
            var initiatorUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == mutualClosure.InitiatedByUserId);
            var responseRequiredUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == mutualClosure.ResponseRequiredFromUserId);
                
            // Notify initiator
            if (initiatorUser?.Email != null)
            {
                var initiatorNotification = new MutualClosureExpiredNotification
                {
                    RecipientEmail = initiatorUser.Email,
                    RecipientName = $"{initiatorUser.FirstName} {initiatorUser.LastName}",
                    InitiatedByUserName = mutualClosure.InitiatedByUserName,
                    ResponseRequiredFromUserName = mutualClosure.ResponseRequiredFromUserName,
                    DisputeTitle = mutualClosure.DisputeTitle,
                    ProposedResolution = mutualClosure.ProposedResolution,
                    ExpiredAt = mutualClosure.ExpiresAt,
                    DisputeUrl = $"https://localhost:5001/disputes/{mutualClosure.DisputeId}"
                };
                await _emailService.SendNotificationAsync(initiatorNotification);
            }
            
            // Notify response required user
            if (responseRequiredUser?.Email != null)
            {
                var responseRequiredNotification = new MutualClosureExpiredNotification
                {
                    RecipientEmail = responseRequiredUser.Email,
                    RecipientName = $"{responseRequiredUser.FirstName} {responseRequiredUser.LastName}",
                    InitiatedByUserName = mutualClosure.InitiatedByUserName,
                    ResponseRequiredFromUserName = mutualClosure.ResponseRequiredFromUserName,
                    DisputeTitle = mutualClosure.DisputeTitle,
                    ProposedResolution = mutualClosure.ProposedResolution,
                    ExpiredAt = mutualClosure.ExpiresAt,
                    DisputeUrl = $"https://localhost:5001/disputes/{mutualClosure.DisputeId}"
                };
                await _emailService.SendNotificationAsync(responseRequiredNotification);
            }

            _logger.LogInformation("Mutual closure expired notifications sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending mutual closure expired notification for closure {MutualClosureId}", mutualClosure.Id);
        }
    }

    public async Task SendMutualClosureCancelledNotificationAsync(MutualClosureDto mutualClosure)
    {
        try
        {
            _logger.LogInformation("Sending mutual closure cancelled notification for closure {MutualClosureId}", mutualClosure.Id);
            
            // Get response required user details (the person who was supposed to respond)
            var responseRequiredUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == mutualClosure.ResponseRequiredFromUserId);
                
            if (responseRequiredUser?.Email == null)
            {
                _logger.LogWarning("Cannot send cancellation notification - user not found or has no email: {UserId}", 
                    mutualClosure.ResponseRequiredFromUserId);
                return;
            }

            var notification = new MutualClosureCancelledNotification
            {
                RecipientEmail = responseRequiredUser.Email,
                RecipientName = $"{responseRequiredUser.FirstName} {responseRequiredUser.LastName}",
                InitiatedByUserName = mutualClosure.InitiatedByUserName,
                ResponseRequiredFromUserName = mutualClosure.ResponseRequiredFromUserName,
                DisputeTitle = mutualClosure.DisputeTitle,
                ProposedResolution = mutualClosure.ProposedResolution,
                CancellationReason = "Cancelled by administrator",
                DisputeUrl = $"https://localhost:5001/disputes/{mutualClosure.DisputeId}"
            };

            await _emailService.SendNotificationAsync(notification);
            _logger.LogInformation("Mutual closure cancellation notification sent successfully to {Email}", responseRequiredUser.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending mutual closure cancelled notification for closure {MutualClosureId}", mutualClosure.Id);
        }
    }

    public async Task SendAdminReviewRequiredNotificationAsync(MutualClosureDto mutualClosure)
    {
        try
        {
            _logger.LogInformation("Sending admin review required notification for closure {MutualClosureId}", mutualClosure.Id);
            
            // Get all admin users
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");

            if (!adminUsers.Any())
            {
                _logger.LogWarning("No admin users found to notify for mutual closure review: {MutualClosureId}", mutualClosure.Id);
                return;
            }

            foreach (var admin in adminUsers.Where(a => !string.IsNullOrEmpty(a.Email)))
            {
                var notification = new MutualClosureAdminReviewNotification
                {
                    RecipientEmail = admin.Email,
                    RecipientName = $"{admin.FirstName} {admin.LastName}",
                    InitiatedByUserName = mutualClosure.InitiatedByUserName,
                    ResponseRequiredFromUserName = mutualClosure.ResponseRequiredFromUserName,
                    DisputeTitle = mutualClosure.DisputeTitle,
                    ProposedResolution = mutualClosure.ProposedResolution,
                    AgreedRefundAmount = mutualClosure.AgreedRefundAmount,
                    ReviewReason = "Mutual closure requires admin review due to business rules or risk assessment",
                    CreatedAt = mutualClosure.CreatedAt,
                    AdminPanelUrl = "https://localhost:5001/admin/disputes"
                };
                
                await _emailService.SendNotificationAsync(notification);
            }

            _logger.LogInformation("Admin review required notifications sent to {AdminCount} administrators", adminUsers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending admin review required notification for closure {MutualClosureId}", mutualClosure.Id);
        }
    }

    public async Task NotifyAdminOfHighValueMutualClosureAsync(MutualClosureDto mutualClosure)
    {
        try
        {
            _logger.LogInformation("Notifying admin of high value mutual closure {MutualClosureId}", mutualClosure.Id);
            
            // Only notify if there's a significant refund amount
            if (!mutualClosure.AgreedRefundAmount.HasValue || mutualClosure.AgreedRefundAmount.Value < 100m)
            {
                _logger.LogDebug("Mutual closure {MutualClosureId} does not meet high-value criteria, skipping admin notification", mutualClosure.Id);
                return;
            }
            
            // Get all admin users
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");

            if (!adminUsers.Any())
            {
                _logger.LogWarning("No admin users found to notify for high-value mutual closure: {MutualClosureId}", mutualClosure.Id);
                return;
            }

            foreach (var admin in adminUsers.Where(a => !string.IsNullOrEmpty(a.Email)))
            {
                var notification = new MutualClosureHighValueAlertNotification
                {
                    RecipientEmail = admin.Email,
                    RecipientName = $"{admin.FirstName} {admin.LastName}",
                    InitiatedByUserName = mutualClosure.InitiatedByUserName,
                    ResponseRequiredFromUserName = mutualClosure.ResponseRequiredFromUserName,
                    DisputeTitle = mutualClosure.DisputeTitle,
                    RefundAmount = mutualClosure.AgreedRefundAmount.Value,
                    ProposedResolution = mutualClosure.ProposedResolution,
                    CreatedAt = mutualClosure.CreatedAt,
                    AdminPanelUrl = "https://localhost:5001/admin/disputes"
                };
                
                await _emailService.SendNotificationAsync(notification);
            }

            _logger.LogInformation("High-value mutual closure alerts sent to {AdminCount} administrators for amount {Amount:C}", 
                adminUsers.Count, mutualClosure.AgreedRefundAmount.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying admin of high value mutual closure {MutualClosureId}", mutualClosure.Id);
        }
    }

    public async Task NotifyAdminOfSuspiciousActivityAsync(string userId, string reason)
    {
        try
        {
            _logger.LogInformation("Notifying admin of suspicious activity by user {UserId}: {Reason}", userId, reason);
            
            // Get user details
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                _logger.LogWarning("Cannot notify admin of suspicious activity - user not found: {UserId}", userId);
                return;
            }
            
            // Get recent mutual closure activity count for this user
            var recentRequestCount = await _context.MutualDisputeClosures
                .Where(mc => (mc.InitiatedByUserId == userId || mc.ResponseRequiredFromUserId == userId) 
                            && mc.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                .CountAsync();
            
            // Get all admin users
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");

            if (!adminUsers.Any())
            {
                _logger.LogWarning("No admin users found to notify for suspicious activity by user: {UserId}", userId);
                return;
            }

            foreach (var admin in adminUsers.Where(a => !string.IsNullOrEmpty(a.Email)))
            {
                var notification = new MutualClosureSuspiciousActivityNotification
                {
                    RecipientEmail = admin.Email,
                    RecipientName = $"{admin.FirstName} {admin.LastName}",
                    UserId = userId,
                    UserName = $"{user.FirstName} {user.LastName}",
                    SuspiciousActivity = reason,
                    ActivityDetails = $"User has {recentRequestCount} mutual closure requests in the past 7 days",
                    DetectedAt = DateTime.UtcNow,
                    RecentRequestCount = recentRequestCount,
                    AdminPanelUrl = "https://localhost:5001/admin/users"
                };
                
                await _emailService.SendNotificationAsync(notification);
            }

            _logger.LogInformation("Suspicious activity alerts sent to {AdminCount} administrators for user {UserId}", 
                adminUsers.Count, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying admin of suspicious activity by user {UserId}", userId);
        }
    }
}