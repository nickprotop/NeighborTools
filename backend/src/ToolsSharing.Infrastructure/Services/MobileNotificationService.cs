using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Entities;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services;

public class MobileNotificationService : IMobileNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MobileNotificationService> _logger;

    public MobileNotificationService(ApplicationDbContext context, ILogger<MobileNotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SendReturnReminderAsync(string userId, Guid rentalId, string reminderType)
    {
        // TODO: Implement mobile push notification for return reminders
        // This should integrate with Firebase Cloud Messaging (FCM) for Android
        // and Apple Push Notification Service (APNs) for iOS
        
        var deviceTokens = await GetUserDeviceTokensAsync(userId);
        
        foreach (var token in deviceTokens)
        {
            try
            {
                // TODO: Send push notification using appropriate service (FCM/APNs)
                // Example notification payload:
                // {
                //   "title": "Return Reminder",
                //   "body": "Your rental is due for return soon",
                //   "data": {
                //     "type": "return_reminder",
                //     "rental_id": rentalId,
                //     "reminder_type": reminderType
                //   }
                // }
                
                _logger.LogInformation($"TODO: Send mobile return reminder to device {token.DeviceToken} for rental {rentalId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send mobile return reminder to device {token.DeviceToken}");
            }
        }
    }

    public async Task SendOverdueNotificationAsync(string userId, Guid rentalId, int daysOverdue)
    {
        // TODO: Implement mobile push notification for overdue rentals
        var deviceTokens = await GetUserDeviceTokensAsync(userId);
        
        foreach (var token in deviceTokens)
        {
            try
            {
                // TODO: Send push notification with high priority for overdue rentals
                _logger.LogInformation($"TODO: Send mobile overdue notification to device {token.DeviceToken} for rental {rentalId} ({daysOverdue} days overdue)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send mobile overdue notification to device {token.DeviceToken}");
            }
        }
    }

    public async Task SendPickupReminderAsync(string userId, Guid rentalId)
    {
        // TODO: Implement mobile push notification for pickup reminders
        var deviceTokens = await GetUserDeviceTokensAsync(userId);
        
        foreach (var token in deviceTokens)
        {
            try
            {
                // TODO: Send push notification for pickup reminder
                _logger.LogInformation($"TODO: Send mobile pickup reminder to device {token.DeviceToken} for rental {rentalId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send mobile pickup reminder to device {token.DeviceToken}");
            }
        }
    }

    public async Task SendRentalApprovedNotificationAsync(string userId, Guid rentalId)
    {
        // TODO: Implement mobile push notification for rental approval
        var deviceTokens = await GetUserDeviceTokensAsync(userId);
        
        foreach (var token in deviceTokens)
        {
            try
            {
                // TODO: Send push notification for rental approval
                _logger.LogInformation($"TODO: Send mobile rental approved notification to device {token.DeviceToken} for rental {rentalId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send mobile rental approved notification to device {token.DeviceToken}");
            }
        }
    }

    public async Task SendRentalRejectedNotificationAsync(string userId, Guid rentalId)
    {
        // TODO: Implement mobile push notification for rental rejection
        var deviceTokens = await GetUserDeviceTokensAsync(userId);
        
        foreach (var token in deviceTokens)
        {
            try
            {
                // TODO: Send push notification for rental rejection
                _logger.LogInformation($"TODO: Send mobile rental rejected notification to device {token.DeviceToken} for rental {rentalId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send mobile rental rejected notification to device {token.DeviceToken}");
            }
        }
    }

    public async Task SendPaymentNotificationAsync(string userId, Guid rentalId, string paymentStatus)
    {
        // TODO: Implement mobile push notification for payment updates
        var deviceTokens = await GetUserDeviceTokensAsync(userId);
        
        foreach (var token in deviceTokens)
        {
            try
            {
                // TODO: Send push notification for payment status updates
                _logger.LogInformation($"TODO: Send mobile payment notification to device {token.DeviceToken} for rental {rentalId} - Status: {paymentStatus}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send mobile payment notification to device {token.DeviceToken}");
            }
        }
    }

    public async Task SendDisputeNotificationAsync(string userId, Guid disputeId, string disputeStatus)
    {
        // TODO: Implement mobile push notification for dispute updates
        var deviceTokens = await GetUserDeviceTokensAsync(userId);
        
        foreach (var token in deviceTokens)
        {
            try
            {
                // TODO: Send push notification for dispute updates
                _logger.LogInformation($"TODO: Send mobile dispute notification to device {token.DeviceToken} for dispute {disputeId} - Status: {disputeStatus}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send mobile dispute notification to device {token.DeviceToken}");
            }
        }
    }

    public async Task RegisterDeviceTokenAsync(string userId, string deviceToken, string platform)
    {
        try
        {
            // Check if device token already exists
            var existingToken = await _context.UserDeviceTokens
                .FirstOrDefaultAsync(udt => udt.UserId == userId && udt.DeviceToken == deviceToken);

            if (existingToken != null)
            {
                existingToken.UpdatedAt = DateTime.UtcNow;
                existingToken.IsActive = true;
                existingToken.Platform = platform;
            }
            else
            {
                var deviceTokenEntity = new UserDeviceToken
                {
                    UserId = userId,
                    DeviceToken = deviceToken,
                    Platform = platform,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.UserDeviceTokens.Add(deviceTokenEntity);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Registered device token for user {userId} on platform {platform}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to register device token for user {userId}");
        }
    }

    public async Task UnregisterDeviceTokenAsync(string userId, string deviceToken)
    {
        try
        {
            var existingToken = await _context.UserDeviceTokens
                .FirstOrDefaultAsync(udt => udt.UserId == userId && udt.DeviceToken == deviceToken);

            if (existingToken != null)
            {
                existingToken.IsActive = false;
                existingToken.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Unregistered device token for user {userId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to unregister device token for user {userId}");
        }
    }

    private async Task<List<UserDeviceToken>> GetUserDeviceTokensAsync(string userId)
    {
        return await _context.UserDeviceTokens
            .Where(udt => udt.UserId == userId && udt.IsActive)
            .ToListAsync();
    }
}