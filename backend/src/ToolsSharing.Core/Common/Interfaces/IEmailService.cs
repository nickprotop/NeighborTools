namespace ToolsSharing.Core.Common.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetToken, string userName);
    Task SendRentalNotificationAsync(string email, string userName, string toolName, string message);
    Task SendWelcomeEmailAsync(string email, string userName);
}