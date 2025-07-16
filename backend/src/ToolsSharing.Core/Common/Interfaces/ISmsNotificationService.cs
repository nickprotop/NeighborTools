namespace ToolsSharing.Core.Common.Interfaces;

public interface ISmsNotificationService
{
    Task SendReturnReminderAsync(string? phoneNumber, string toolName, DateTime returnDate);
    Task SendOverdueNotificationAsync(string? phoneNumber, string toolName, int daysOverdue);
    Task SendPickupReminderAsync(string? phoneNumber, string toolName, DateTime pickupDate);
    Task SendRentalApprovedAsync(string? phoneNumber, string toolName, DateTime startDate);
    Task SendRentalRejectedAsync(string? phoneNumber, string toolName, string reason);
    Task SendPaymentConfirmationAsync(string? phoneNumber, string toolName, decimal amount);
    Task SendTwoFactorCodeAsync(string? phoneNumber, string code);
    Task SendSecurityAlertAsync(string? phoneNumber, string alertType);
    Task ValidatePhoneNumberAsync(string? phoneNumber);
}