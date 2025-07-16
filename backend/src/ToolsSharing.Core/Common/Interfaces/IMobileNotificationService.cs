namespace ToolsSharing.Core.Common.Interfaces;

public interface IMobileNotificationService
{
    Task SendReturnReminderAsync(string userId, Guid rentalId, string reminderType);
    Task SendOverdueNotificationAsync(string userId, Guid rentalId, int daysOverdue);
    Task SendPickupReminderAsync(string userId, Guid rentalId);
    Task SendRentalApprovedNotificationAsync(string userId, Guid rentalId);
    Task SendRentalRejectedNotificationAsync(string userId, Guid rentalId);
    Task SendPaymentNotificationAsync(string userId, Guid rentalId, string paymentStatus);
    Task SendDisputeNotificationAsync(string userId, Guid disputeId, string disputeStatus);
    Task RegisterDeviceTokenAsync(string userId, string deviceToken, string platform);
    Task UnregisterDeviceTokenAsync(string userId, string deviceToken);
}