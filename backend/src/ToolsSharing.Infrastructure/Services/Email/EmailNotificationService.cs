using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services.Email;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IEmailTemplateEngine _templateEngine;
    private readonly IEmailProvider _emailProvider;
    private readonly ISettingsService _settingsService;
    private readonly UserManager<User> _userManager;
    private readonly EmailSettings _emailSettings;

    public EmailNotificationService(
        ILogger<EmailNotificationService> logger,
        ApplicationDbContext dbContext,
        IEmailTemplateEngine templateEngine,
        IEmailProvider emailProvider,
        ISettingsService settingsService,
        UserManager<User> userManager,
        IOptions<EmailSettings> emailSettings)
    {
        _logger = logger;
        _dbContext = dbContext;
        _templateEngine = templateEngine;
        _emailProvider = emailProvider;
        _settingsService = settingsService;
        _userManager = userManager;
        _emailSettings = emailSettings.Value;
    }

    public async Task<bool> SendNotificationAsync(EmailNotification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if we can send to user
            if (!string.IsNullOrEmpty(notification.UserId))
            {
                var canSend = await CanSendToUserAsync(notification.UserId, notification.Type, cancellationToken);
                if (!canSend)
                {
                    _logger.LogInformation("Email notification blocked by user preferences: {Type} to {UserId}", 
                        notification.Type, notification.UserId);
                    return false;
                }
            }

            // Check if user is unsubscribed
            if (await IsUserUnsubscribedAsync(notification.RecipientEmail, notification.Type, cancellationToken))
            {
                _logger.LogInformation("Email notification blocked - user unsubscribed: {Type} to {Email}", 
                    notification.Type, notification.RecipientEmail);
                return false;
            }

            // Test mode - redirect to test recipient
            if (_emailSettings.TestMode && !string.IsNullOrEmpty(_emailSettings.TestEmailRecipient))
            {
                _logger.LogWarning("Test mode enabled - redirecting email from {Original} to {Test}", 
                    notification.RecipientEmail, _emailSettings.TestEmailRecipient);
                notification.RecipientEmail = _emailSettings.TestEmailRecipient;
            }

            // Render template
            var templateData = notification.GetTemplateData();
            var html = await _templateEngine.RenderAsync(notification.GetTemplateName(), templateData, cancellationToken);
            var plainText = await _templateEngine.RenderAsync($"{notification.GetTemplateName()}.txt", templateData, cancellationToken)
                .ContinueWith(t => t.IsCompletedSuccessfully ? t.Result : null, cancellationToken);

            // Create email message
            var message = new EmailMessage
            {
                To = notification.RecipientEmail,
                ToName = notification.RecipientName,
                From = _emailSettings.FromEmail,
                FromName = _emailSettings.FromName,
                ReplyTo = _emailSettings.ReplyToEmail,
                ReplyToName = _emailSettings.ReplyToName,
                Subject = notification.GetSubject(),
                HtmlBody = html,
                PlainTextBody = plainText,
                Priority = notification.Priority,
                Metadata = notification.Metadata
            };

            // Add unsubscribe link
            if (!string.IsNullOrEmpty(_emailSettings.UnsubscribeUrl))
            {
                var unsubscribeUrl = $"{_emailSettings.UnsubscribeUrl}?email={Uri.EscapeDataString(notification.RecipientEmail)}&type={notification.Type}";
                message.Headers["List-Unsubscribe"] = $"<{unsubscribeUrl}>";
                message.Headers["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click";
            }

            // Add tracking headers
            message.Headers["X-NeighborTools-NotificationType"] = notification.Type.ToString();
            message.Headers["X-NeighborTools-NotificationId"] = notification.Id.ToString();
            
            if (!string.IsNullOrEmpty(notification.CorrelationId))
            {
                message.Headers["X-NeighborTools-CorrelationId"] = notification.CorrelationId;
            }

            // Send immediately or queue
            if (_emailSettings.EnableQueue)
            {
                await QueueNotificationAsync(notification, cancellationToken);
                return true;
            }
            else
            {
                var result = await _emailProvider.SendAsync(message, cancellationToken);
                
                if (result.Success)
                {
                    await TrackEmailSentAsync(notification, result.MessageId, cancellationToken);
                }
                
                return result.Success;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email notification: {Type} to {Email}", 
                notification.Type, notification.RecipientEmail);
            return false;
        }
    }

    public Task<bool> SendNotificationAsync<T>(T notification, CancellationToken cancellationToken = default) where T : EmailNotification
    {
        return SendNotificationAsync((EmailNotification)notification, cancellationToken);
    }

    public async Task<int> SendBatchNotificationsAsync(IEnumerable<EmailNotification> notifications, CancellationToken cancellationToken = default)
    {
        var successCount = 0;
        
        foreach (var notification in notifications)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
                
            if (await SendNotificationAsync(notification, cancellationToken))
            {
                successCount++;
            }
        }
        
        return successCount;
    }

    public async Task<Guid> QueueNotificationAsync(EmailNotification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            // Render template
            var templateData = notification.GetTemplateData();
            var html = await _templateEngine.RenderAsync(notification.GetTemplateName(), templateData, cancellationToken);
            var plainText = await _templateEngine.RenderAsync($"{notification.GetTemplateName()}.txt", templateData, cancellationToken)
                .ContinueWith(t => t.IsCompletedSuccessfully ? t.Result : null, cancellationToken);

            // Create queue item
            var queueItem = new EmailQueueItem
            {
                RecipientEmail = notification.RecipientEmail,
                RecipientName = notification.RecipientName,
                Subject = notification.GetSubject(),
                Body = html,
                PlainTextBody = plainText,
                NotificationType = notification.Type,
                Priority = notification.Priority,
                Status = EmailStatus.Pending,
                ScheduledFor = notification.ScheduledFor,
                UserId = notification.UserId,
                CorrelationId = notification.CorrelationId,
                Metadata = notification.Metadata
            };

            // Add headers
            queueItem.Headers["X-NeighborTools-NotificationType"] = notification.Type.ToString();
            queueItem.Headers["X-NeighborTools-NotificationId"] = notification.Id.ToString();

            _dbContext.Set<EmailQueueItem>().Add(queueItem);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Email queued: {Type} to {Email}", notification.Type, notification.RecipientEmail);
            
            return queueItem.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queueing email notification: {Type} to {Email}", 
                notification.Type, notification.RecipientEmail);
            throw;
        }
    }

    public async Task<List<Guid>> QueueBatchNotificationsAsync(IEnumerable<EmailNotification> notifications, CancellationToken cancellationToken = default)
    {
        var queueIds = new List<Guid>();
        
        foreach (var notification in notifications)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
                
            queueIds.Add(await QueueNotificationAsync(notification, cancellationToken));
        }
        
        return queueIds;
    }

    public async Task<bool> CancelQueuedNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var queueItem = await _dbContext.Set<EmailQueueItem>()
            .FirstOrDefaultAsync(e => e.Id == notificationId && e.Status == EmailStatus.Pending, cancellationToken);
            
        if (queueItem == null)
            return false;
            
        queueItem.Status = EmailStatus.Cancelled;
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        return true;
    }

    public Task<string> RenderTemplateAsync(string templateName, object data, CancellationToken cancellationToken = default)
    {
        return _templateEngine.RenderAsync(templateName, data, cancellationToken);
    }

    public Task<bool> TemplateExistsAsync(string templateName, CancellationToken cancellationToken = default)
    {
        return _templateEngine.TemplateExistsAsync(templateName, cancellationToken);
    }

    public async Task<bool> CanSendToUserAsync(string userId, EmailNotificationType type, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetUserSettingsAsync(userId);
        if (settings?.Notifications == null)
            return true; // Default to allow if no settings

        return type switch
        {
            EmailNotificationType.RentalRequest or 
            EmailNotificationType.RentalApproved or 
            EmailNotificationType.RentalRejected or
            EmailNotificationType.RentalCancelled => settings.Notifications.EmailRentalRequests,
            
            EmailNotificationType.RentalReminder or
            EmailNotificationType.RentalOverdue or
            EmailNotificationType.RentalReturned => settings.Notifications.EmailRentalUpdates,
            
            EmailNotificationType.NewMessage or
            EmailNotificationType.MessageDigest => settings.Notifications.EmailMessages,
            
            EmailNotificationType.Newsletter or
            EmailNotificationType.Promotion or
            EmailNotificationType.ProductUpdate => settings.Notifications.EmailMarketing,
            
            EmailNotificationType.LoginAlert or
            EmailNotificationType.TwoFactorCode or
            EmailNotificationType.SecurityAlert or
            EmailNotificationType.PasswordReset or
            EmailNotificationType.PasswordChanged => settings.Notifications.EmailSecurityAlerts,
            
            _ => true
        };
    }

    public async Task<bool> IsUserUnsubscribedAsync(string email, EmailNotificationType? type = null, CancellationToken cancellationToken = default)
    {
        // Check global unsubscribe
        var query = _dbContext.Set<EmailUnsubscribe>()
            .Where(u => u.Email == email);
            
        if (type.HasValue)
        {
            query = query.Where(u => u.NotificationType == null || u.NotificationType == type);
        }
        else
        {
            query = query.Where(u => u.NotificationType == null);
        }
        
        return await query.AnyAsync(cancellationToken);
    }

    public async Task UnsubscribeUserAsync(string email, EmailNotificationType? type = null, CancellationToken cancellationToken = default)
    {
        var unsubscribe = new EmailUnsubscribe
        {
            Email = email,
            NotificationType = type,
            UnsubscribedAt = DateTime.UtcNow
        };
        
        _dbContext.Set<EmailUnsubscribe>().Add(unsubscribe);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("User unsubscribed: {Email} from {Type}", email, type?.ToString() ?? "all");
    }

    public async Task ResubscribeUserAsync(string email, EmailNotificationType? type = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<EmailUnsubscribe>()
            .Where(u => u.Email == email);
            
        if (type.HasValue)
        {
            query = query.Where(u => u.NotificationType == type);
        }
        
        _dbContext.Set<EmailUnsubscribe>().RemoveRange(query);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("User resubscribed: {Email} to {Type}", email, type?.ToString() ?? "all");
    }

    public async Task TrackEmailOpenedAsync(string messageId, CancellationToken cancellationToken = default)
    {
        var tracking = await _dbContext.Set<EmailTracking>()
            .FirstOrDefaultAsync(t => t.MessageId == messageId, cancellationToken);
            
        if (tracking != null)
        {
            tracking.OpenCount++;
            tracking.OpenedAt ??= DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task TrackEmailClickedAsync(string messageId, string link, CancellationToken cancellationToken = default)
    {
        var tracking = await _dbContext.Set<EmailTracking>()
            .FirstOrDefaultAsync(t => t.MessageId == messageId, cancellationToken);
            
        if (tracking != null)
        {
            tracking.ClickCount++;
            tracking.ClickedAt ??= DateTime.UtcNow;
            tracking.Metadata[$"click_{tracking.ClickCount}"] = link;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public Task<EmailTracking?> GetEmailTrackingAsync(string messageId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<EmailTracking>()
            .FirstOrDefaultAsync(t => t.MessageId == messageId, cancellationToken);
    }

    public async Task<EmailStatistics> GetStatisticsAsync(DateTime from, DateTime to, EmailNotificationType? type = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<EmailTracking>()
            .Where(t => t.SentAt >= from && t.SentAt <= to);
            
        if (type.HasValue)
        {
            query = query.Where(t => t.NotificationType == type);
        }
        
        var trackings = await query.ToListAsync(cancellationToken);
        
        var stats = new EmailStatistics
        {
            TotalSent = trackings.Count,
            TotalOpened = trackings.Count(t => t.OpenedAt.HasValue),
            TotalClicked = trackings.Count(t => t.ClickedAt.HasValue),
            TotalUnsubscribed = trackings.Count(t => t.Unsubscribed),
            TotalBounced = trackings.Count(t => t.Bounced),
            TotalSpam = trackings.Count(t => t.MarkedAsSpam)
        };
        
        if (stats.TotalSent > 0)
        {
            stats.OpenRate = (double)stats.TotalOpened / stats.TotalSent * 100;
            stats.ClickRate = (double)stats.TotalClicked / stats.TotalSent * 100;
            stats.BounceRate = (double)stats.TotalBounced / stats.TotalSent * 100;
        }
        
        // Group by type
        stats.ByType = trackings
            .GroupBy(t => t.NotificationType)
            .ToDictionary(g => g.Key, g => g.Count());
            
        // Group by day
        stats.ByDay = trackings
            .GroupBy(t => t.SentAt.Date.ToString("yyyy-MM-dd"))
            .ToDictionary(g => g.Key, g => g.Count());
        
        // Add failed count from queue
        var failedCount = await _dbContext.Set<EmailQueueItem>()
            .CountAsync(e => e.Status == EmailStatus.Failed && 
                           e.CreatedAt >= from && 
                           e.CreatedAt <= to &&
                           (type == null || e.NotificationType == type), cancellationToken);
                           
        stats.TotalFailed = failedCount;
        
        return stats;
    }

    public async Task<List<EmailQueueItem>> GetFailedEmailsAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<EmailQueueItem>()
            .Where(e => e.Status == EmailStatus.Failed)
            .OrderByDescending(e => e.LastAttemptAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RetryFailedEmailAsync(Guid queueItemId, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.Set<EmailQueueItem>()
            .FirstOrDefaultAsync(e => e.Id == queueItemId && e.Status == EmailStatus.Failed, cancellationToken);
            
        if (item == null)
            return false;
            
        item.Status = EmailStatus.Pending;
        item.RetryCount = 0;
        item.ScheduledFor = DateTime.UtcNow;
        item.ErrorMessage = null;
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Retrying failed email: {Subject} to {Email}", item.Subject, item.RecipientEmail);
        
        return true;
    }

    private async Task TrackEmailSentAsync(EmailNotification notification, string? messageId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(messageId))
            return;
            
        var tracking = new EmailTracking
        {
            MessageId = messageId,
            RecipientEmail = notification.RecipientEmail,
            NotificationType = notification.Type,
            SentAt = DateTime.UtcNow,
            UserId = notification.UserId,
            Metadata = notification.Metadata
        };
        
        _dbContext.Set<EmailTracking>().Add(tracking);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<Dictionary<string, bool>> GetUserNotificationPreferencesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.Settings == null)
        {
            return GetDefaultNotificationPreferences();
        }
        
        return new Dictionary<string, bool>
        {
            ["EmailNotifications"] = user.Settings.EmailNotifications,
            ["RentalRequests"] = user.Settings.NotifyOnRentalRequests,
            ["RentalUpdates"] = user.Settings.NotifyOnRentalUpdates,
            ["ToolAvailability"] = user.Settings.NotifyOnToolAvailability,
            ["Reminders"] = user.Settings.NotifyOnReminders,
            ["Marketing"] = user.Settings.NotifyOnMarketing
        };
    }
    
    public async Task UpdateUserNotificationPreferencesAsync(string userId, Dictionary<string, bool> preferences, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.Settings == null)
        {
            throw new InvalidOperationException("User or settings not found");
        }
        
        if (preferences.TryGetValue("EmailNotifications", out var emailNotifications))
            user.Settings.EmailNotifications = emailNotifications;
        if (preferences.TryGetValue("RentalRequests", out var rentalRequests))
            user.Settings.NotifyOnRentalRequests = rentalRequests;
        if (preferences.TryGetValue("RentalUpdates", out var rentalUpdates))
            user.Settings.NotifyOnRentalUpdates = rentalUpdates;
        if (preferences.TryGetValue("ToolAvailability", out var toolAvailability))
            user.Settings.NotifyOnToolAvailability = toolAvailability;
        if (preferences.TryGetValue("Reminders", out var reminders))
            user.Settings.NotifyOnReminders = reminders;
        if (preferences.TryGetValue("Marketing", out var marketing))
            user.Settings.NotifyOnMarketing = marketing;
            
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<bool> UnsubscribeUserAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        // Validate token (simple implementation - in production, use proper token validation)
        var expectedToken = GenerateUnsubscribeToken(email);
        if (token != expectedToken)
        {
            return false;
        }
        
        await UnsubscribeUserAsync(email, null, cancellationToken);
        return true;
    }
    
    public async Task<EmailStatistics> GetEmailStatisticsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        
        var userTrackings = await _dbContext.Set<EmailTracking>()
            .Where(t => t.UserId == userId && t.SentAt >= thirtyDaysAgo)
            .ToListAsync(cancellationToken);
            
        var stats = new EmailStatistics
        {
            TotalSent = userTrackings.Count,
            TotalOpened = userTrackings.Count(t => t.OpenCount > 0),
            TotalClicked = userTrackings.Count(t => t.ClickCount > 0),
            OpenRate = userTrackings.Count > 0 ? (double)userTrackings.Count(t => t.OpenCount > 0) / userTrackings.Count * 100 : 0,
            ClickRate = userTrackings.Count > 0 ? (double)userTrackings.Count(t => t.ClickCount > 0) / userTrackings.Count * 100 : 0
        };
        
        // Group by notification type
        stats.ByType = userTrackings
            .GroupBy(t => t.NotificationType)
            .ToDictionary(g => g.Key, g => g.Count());
            
        return stats;
    }
    
    public async Task<string> PreviewEmailTemplateAsync(string templateName, object data, CancellationToken cancellationToken = default)
    {
        return await _templateEngine.RenderAsync(templateName, data, cancellationToken);
    }
    
    public async Task<QueueStatus> GetQueueStatusAsync(CancellationToken cancellationToken = default)
    {
        var queue = _dbContext.Set<EmailQueueItem>();
        
        var status = new QueueStatus
        {
            PendingCount = await queue.CountAsync(q => q.Status == EmailQueueStatus.Pending, cancellationToken),
            ProcessingCount = await queue.CountAsync(q => q.Status == EmailQueueStatus.Processing, cancellationToken),
            FailedCount = await queue.CountAsync(q => q.Status == EmailQueueStatus.Failed && q.RetryCount < _emailSettings.MaxRetries, cancellationToken),
            IsProcessorRunning = _queueProcessor != null,
            LastProcessedDate = await queue
                .Where(q => q.Status == EmailQueueStatus.Sent)
                .OrderByDescending(q => q.ProcessedAt)
                .Select(q => q.ProcessedAt)
                .FirstOrDefaultAsync(cancellationToken)
        };
        
        return status;
    }
    
    public async Task<int> ProcessQueueManuallyAsync(CancellationToken cancellationToken = default)
    {
        if (_queueProcessor == null)
        {
            throw new InvalidOperationException("Queue processor is not available");
        }
        
        await _queueProcessor.ProcessQueueAsync(cancellationToken);
        
        // Return the number of processed items
        var processedCount = await _dbContext.Set<EmailQueueItem>()
            .CountAsync(q => q.Status == EmailQueueStatus.Sent && 
                           q.ProcessedAt >= DateTime.UtcNow.AddMinutes(-1), cancellationToken);
                           
        return processedCount;
    }
    
    private Dictionary<string, bool> GetDefaultNotificationPreferences()
    {
        return new Dictionary<string, bool>
        {
            ["EmailNotifications"] = true,
            ["RentalRequests"] = true,
            ["RentalUpdates"] = true,
            ["ToolAvailability"] = true,
            ["Reminders"] = true,
            ["Marketing"] = false
        };
    }
    
    private string GenerateUnsubscribeToken(string email)
    {
        // Simple token generation - in production, use proper cryptographic methods
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"{email}:{_emailSettings.FromEmail}"));
        return Convert.ToBase64String(bytes).Replace("/", "-").Replace("+", "_").Substring(0, 16);
    }
}

// Supporting entity for unsubscribes
public class EmailUnsubscribe : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public EmailNotificationType? NotificationType { get; set; }
    public DateTime UnsubscribedAt { get; set; }
    public string? Reason { get; set; }
}