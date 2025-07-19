using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Common.Models.EmailNotifications;
using ToolsSharing.Core.Features.Settings;
using ToolsSharing.Core.Interfaces;

namespace ToolsSharing.Infrastructure.Services;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly ISettingsService _settingsService;

    public EmailNotificationService(
        IConfiguration configuration, 
        ILogger<EmailNotificationService> logger,
        ISettingsService settingsService)
    {
        _configuration = configuration;
        _logger = logger;
        _settingsService = settingsService;
    }

    public async Task<bool> SendNotificationAsync(EmailNotification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check user preferences before sending
            if (!string.IsNullOrEmpty(notification.UserId))
            {
                if (!await CanSendToUserAsync(notification.UserId, notification.Type, cancellationToken))
                {
                    _logger.LogInformation("Email notification skipped due to user preferences. Type: {Type}, UserId: {UserId}", 
                        notification.Type, notification.UserId);
                    return true; // Return true as it's not a failure, just respecting user preferences
                }
            }

            var subject = notification.GetSubject();
            var body = GenerateEmailBody(notification);
            
            await SendEmailAsync(notification.RecipientEmail, subject, body);
            _logger.LogInformation("Email notification sent to {Email} with subject {Subject}", 
                notification.RecipientEmail, subject);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email notification to {Email}", notification.RecipientEmail);
            return false;
        }
    }

    public async Task<bool> SendNotificationAsync<T>(T notification, CancellationToken cancellationToken = default) where T : EmailNotification
    {
        return await SendNotificationAsync((EmailNotification)notification, cancellationToken);
    }

    public async Task<int> SendBatchNotificationsAsync(IEnumerable<EmailNotification> notifications, CancellationToken cancellationToken = default)
    {
        int successCount = 0;
        foreach (var notification in notifications)
        {
            // SendNotificationAsync already checks user preferences, so we don't need to check again here
            if (await SendNotificationAsync(notification, cancellationToken))
            {
                successCount++;
            }
        }
        return successCount;
    }

    private string GenerateEmailBody(EmailNotification notification)
    {
        var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5000";
        
        return notification switch
        {
            EmailVerificationNotification emailVerification => GenerateEmailVerificationTemplate(emailVerification, frontendUrl),
            PasswordResetNotification passwordReset => GeneratePasswordResetTemplate(passwordReset, frontendUrl),
            WelcomeEmailNotification welcome => GenerateWelcomeTemplate(welcome, frontendUrl),
            PasswordChangedNotification passwordChanged => GeneratePasswordChangedTemplate(passwordChanged, frontendUrl),
            RentalRequestNotification rentalRequest => GenerateRentalRequestTemplate(rentalRequest, frontendUrl),
            RentalApprovedNotification rentalApproved => GenerateRentalApprovedTemplate(rentalApproved, frontendUrl),
            RentalRejectedNotification rentalRejected => GenerateRentalRejectedTemplate(rentalRejected, frontendUrl),
            RentalReminderNotification rentalReminder => GenerateRentalReminderTemplate(rentalReminder, frontendUrl),
            LoginAlertNotification loginAlert => GenerateLoginAlertTemplate(loginAlert, frontendUrl),
            SecurityAlertNotification securityAlert => GenerateSecurityAlertTemplate(securityAlert, frontendUrl),
            DisputeCreatedNotification disputeCreated => GenerateDisputeCreatedTemplate(disputeCreated, frontendUrl),
            DisputeMessageNotification disputeMessage => GenerateDisputeMessageTemplate(disputeMessage, frontendUrl),
            DisputeStatusChangeNotification disputeStatusChange => GenerateDisputeStatusChangeTemplate(disputeStatusChange, frontendUrl),
            DisputeEscalationNotification disputeEscalation => GenerateDisputeEscalationTemplate(disputeEscalation, frontendUrl),
            DisputeResolutionNotification disputeResolution => GenerateDisputeResolutionTemplate(disputeResolution, frontendUrl),
            DisputeEvidenceNotification disputeEvidence => GenerateDisputeEvidenceTemplate(disputeEvidence, frontendUrl),
            DisputeOverdueNotification disputeOverdue => GenerateDisputeOverdueTemplate(disputeOverdue, frontendUrl),
            NewMessageNotification newMessage => GenerateNewMessageTemplate(newMessage, frontendUrl),
            MessageReplyNotification messageReply => GenerateMessageReplyTemplate(messageReply, frontendUrl),
            MessageModerationNotification messageModeration => GenerateMessageModerationTemplate(messageModeration, frontendUrl),
            ConversationDigestNotification conversationDigest => GenerateConversationDigestTemplate(conversationDigest, frontendUrl),
            _ => GenerateGenericTemplate(notification, frontendUrl)
        };
    }

    private string GenerateEmailVerificationTemplate(EmailVerificationNotification notification, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #594AE2; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ background-color: #594AE2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>NeighborTools</h1>
        </div>
        <div class=""content"">
            <h2>Email Verification Required</h2>
            <p>Hello {notification.UserName},</p>
            <p>Thank you for creating your NeighborTools account! To complete your registration, please verify your email address by clicking the button below:</p>
            <p style=""text-align: center; margin: 30px 0;"">
                <a href=""{notification.VerificationUrl}"" class=""button"">Verify Email Address</a>
            </p>
            <p><strong>This link will expire in 24 hours for security reasons.</strong></p>
            <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
            <p style=""word-break: break-all; color: #594AE2;"">{notification.VerificationUrl}</p>
            <p>If you didn't create this account, please ignore this email.</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GeneratePasswordResetTemplate(PasswordResetNotification notification, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #594AE2; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ background-color: #594AE2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>NeighborTools</h1>
        </div>
        <div class=""content"">
            <h2>Password Reset Request</h2>
            <p>Hello {notification.UserName},</p>
            <p>We received a request to reset your password for your NeighborTools account. If you didn't make this request, you can safely ignore this email.</p>
            <p>To reset your password, click the button below:</p>
            <p style=""text-align: center; margin: 30px 0;"">
                <a href=""{notification.ResetUrl}"" class=""button"">Reset Password</a>
            </p>
            <p><strong>This link will expire in 24 hours for security reasons.</strong></p>
            <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
            <p style=""word-break: break-all; color: #594AE2;"">{notification.ResetUrl}</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateWelcomeTemplate(WelcomeEmailNotification notification, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #594AE2; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ background-color: #594AE2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Welcome to NeighborTools!</h1>
        </div>
        <div class=""content"">
            <h2>Hello {notification.UserName}!</h2>
            <p>Thank you for joining NeighborTools, the community platform for sharing and borrowing tools.</p>
            <p>Here's what you can do with your new account:</p>
            <ul>
                <li><strong>Browse Tools:</strong> Find the perfect tool for your next project</li>
                <li><strong>Share Your Tools:</strong> List your tools and earn money by renting them out</li>
                <li><strong>Connect with Neighbors:</strong> Build stronger community relationships</li>
                <li><strong>Save Money:</strong> Rent instead of buying tools you rarely use</li>
            </ul>
            <p style=""text-align: center; margin: 30px 0;"">
                <a href=""{frontendUrl}/tools"" class=""button"">Start Browsing Tools</a>
            </p>
            <p>If you have any questions, don't hesitate to reach out to our support team.</p>
            <p>Happy tool sharing!</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GeneratePasswordChangedTemplate(PasswordChangedNotification notification, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #594AE2; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .alert {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>NeighborTools</h1>
        </div>
        <div class=""content"">
            <h2>Password Changed Successfully</h2>
            <p>Hello {notification.UserName},</p>
            <p>This email confirms that your NeighborTools account password was successfully changed on {notification.ChangedAt:f}.</p>
            <div class=""alert"">
                <strong>Security Information:</strong><br>
                IP Address: {notification.IpAddress}<br>
                Device: {notification.UserAgent}
            </div>
            <p>If you didn't make this change, please contact our support team immediately.</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateRentalRequestTemplate(RentalRequestNotification notification, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #594AE2; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ background-color: #594AE2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 5px; }}
        .button.secondary {{ background-color: #6c757d; }}
        .rental-details {{ background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>NeighborTools</h1>
        </div>
        <div class=""content"">
            <h2>New Rental Request</h2>
            <p>Hello {notification.OwnerName},</p>
            <p><strong>{notification.RenterName}</strong> would like to rent your tool:</p>
            <div class=""rental-details"">
                <h3>{notification.ToolName}</h3>
                <p><strong>Rental Period:</strong> {notification.StartDate:d} to {notification.EndDate:d} ({(notification.EndDate - notification.StartDate).Days} days)</p>
                <p><strong>Total Cost:</strong> ${notification.TotalCost:F2}</p>
                {(string.IsNullOrEmpty(notification.Message) ? "" : $"<p><strong>Message from {notification.RenterName}:</strong><br>\"{notification.Message}\"</p>")}
            </div>
            <p style=""text-align: center; margin: 30px 0;"">
                <a href=""{notification.ApprovalUrl}"" class=""button"">Approve Request</a>
                <a href=""{notification.RentalDetailsUrl}"" class=""button secondary"">View Details</a>
            </p>
            <p>You can review this request and approve or decline it through your NeighborTools dashboard.</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateRentalApprovedTemplate(RentalApprovedNotification notification, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ background-color: #594AE2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
        .rental-details {{ background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .contact-info {{ background-color: #e9ecef; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🎉 Rental Approved!</h1>
        </div>
        <div class=""content"">
            <h2>Great news, {notification.RenterName}!</h2>
            <p>Your rental request has been approved by <strong>{notification.OwnerName}</strong>.</p>
            <div class=""rental-details"">
                <h3>{notification.ToolName}</h3>
                <p><strong>Rental Period:</strong> {notification.StartDate:d} to {notification.EndDate:d} ({(notification.EndDate - notification.StartDate).Days} days)</p>
                <p><strong>Total Cost:</strong> ${notification.TotalCost:F2}</p>
                <p><strong>Location:</strong> {notification.ToolLocation}</p>
            </div>
            <div class=""contact-info"">
                <h4>Owner Contact Information:</h4>
                <p><strong>Name:</strong> {notification.OwnerName}</p>
                <p><strong>Email:</strong> {notification.OwnerEmail}</p>
                <p><strong>Phone:</strong> {notification.OwnerPhone}</p>
            </div>
            <p style=""text-align: center; margin: 30px 0;"">
                <a href=""{notification.RentalDetailsUrl}"" class=""button"">View Rental Details</a>
            </p>
            <p>Please coordinate with the owner for pickup arrangements. Remember to inspect the tool before taking it and return it in the same condition.</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateRentalRejectedTemplate(RentalRejectedNotification notification, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ background-color: #594AE2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
        .rental-details {{ background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Rental Request Update</h1>
        </div>
        <div class=""content"">
            <h2>Hello {notification.RenterName},</h2>
            <p>Unfortunately, your rental request for <strong>{notification.ToolName}</strong> was not approved.</p>
            <div class=""rental-details"">
                <p><strong>Requested Period:</strong> {notification.StartDate:d} to {notification.EndDate:d}</p>
                {(string.IsNullOrEmpty(notification.RejectionReason) ? "" : $"<p><strong>Reason:</strong> {notification.RejectionReason}</p>")}
            </div>
            <p>Don't worry! There are many other great tools available in your area.</p>
            <p style=""text-align: center; margin: 30px 0;"">
                <a href=""{notification.BrowseToolsUrl}"" class=""button"">Browse Other Tools</a>
            </p>
            <p>Keep exploring and you'll find the perfect tool for your project.</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateRentalReminderTemplate(RentalReminderNotification notification, string frontendUrl)
    {
        var action = notification.IsPickupReminder ? "pick up" : "return";
        var date = notification.IsPickupReminder ? notification.PickupDate : notification.ReturnDate;
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #ffc107; color: #212529; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ background-color: #594AE2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
        .reminder-details {{ background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .contact-info {{ background-color: #e9ecef; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>⏰ Rental Reminder</h1>
        </div>
        <div class=""content"">
            <h2>Hello {notification.UserName},</h2>
            <p>This is a friendly reminder that you need to <strong>{action}</strong> the following tool:</p>
            <div class=""reminder-details"">
                <h3>{notification.ToolName}</h3>
                <p><strong>{(notification.IsPickupReminder ? "Pickup" : "Return")} Date:</strong> {date:f}</p>
                <p><strong>Location:</strong> {notification.ToolLocation}</p>
            </div>
            <div class=""contact-info"">
                <h4>Owner Contact:</h4>
                <p><strong>Name:</strong> {notification.OwnerName}</p>
                <p><strong>Phone:</strong> {notification.OwnerPhone}</p>
            </div>
            <p style=""text-align: center; margin: 30px 0;"">
                <a href=""{notification.RentalDetailsUrl}"" class=""button"">View Rental Details</a>
            </p>
            <p>{(notification.IsPickupReminder ? "Please coordinate with the owner for pickup." : "Please return the tool on time and in good condition.")}</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateLoginAlertTemplate(LoginAlertNotification notification, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #17a2b8; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .alert {{ background-color: #d1ecf1; border: 1px solid #bee5eb; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>NeighborTools</h1>
        </div>
        <div class=""content"">
            <h2>New Login Detected</h2>
            <p>Hello {notification.UserName},</p>
            <p>We detected a new login to your NeighborTools account:</p>
            <div class=""alert"">
                <p><strong>Time:</strong> {notification.LoginTime:f}</p>
                <p><strong>Location:</strong> {notification.Location}</p>
                <p><strong>IP Address:</strong> {notification.IpAddress}</p>
                <p><strong>Device:</strong> {notification.Device}</p>
                <p><strong>Browser:</strong> {notification.Browser}</p>
            </div>
            <p>If this was you, no action is needed. If you don't recognize this login, please change your password immediately.</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateSecurityAlertTemplate(SecurityAlertNotification notification, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .alert {{ background-color: #f8d7da; border: 1px solid #f5c6cb; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .button {{ background-color: #594AE2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🚨 Security Alert</h1>
        </div>
        <div class=""content"">
            <h2>Hello {notification.UserName},</h2>
            <p>We detected suspicious activity on your NeighborTools account:</p>
            <div class=""alert"">
                <p><strong>Alert Type:</strong> {notification.AlertType}</p>
                <p><strong>Description:</strong> {notification.AlertMessage}</p>
                <p><strong>Time:</strong> {notification.OccurredAt:f}</p>
                <p><strong>Action Required:</strong> {notification.ActionRequired}</p>
            </div>
            <p>For your security, please review your account activity and change your password if necessary.</p>
            <p style=""text-align: center; margin: 30px 0;"">
                <a href=""{frontendUrl}/account/security"" class=""button"">Review Account Security</a>
            </p>
            <p>If you have any concerns, please contact our support team immediately.</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateGenericTemplate(EmailNotification notification, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #594AE2; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>NeighborTools</h1>
        </div>
        <div class=""content"">
            <h2>Notification</h2>
            <p>Hello {notification.RecipientName},</p>
            <p>You have received a notification from NeighborTools.</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var smtpSettings = _configuration.GetSection("EmailSettings");
        var smtpServer = smtpSettings["SmtpServer"];
        var smtpPort = int.Parse(smtpSettings["SmtpPort"] ?? "587");
        var smtpUsername = smtpSettings["SmtpUsername"];
        var smtpPassword = smtpSettings["SmtpPassword"];
        var fromEmail = smtpSettings["FromEmail"];
        var fromName = smtpSettings["FromName"] ?? "NeighborTools";

        // If SMTP is not configured, log and skip sending (for development)
        if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpUsername))
        {
            _logger.LogWarning("Email sending skipped - SMTP not configured. Subject: {Subject}, To: {Email}", subject, toEmail);
            return;
        }

        using var client = new SmtpClient(smtpServer, smtpPort);
        client.EnableSsl = true;
        client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

        using var message = new MailMessage();
        message.From = new MailAddress(fromEmail ?? "", fromName);
        message.To.Add(toEmail);
        message.Subject = subject;
        message.Body = body;
        message.IsBodyHtml = true;

        await client.SendMailAsync(message);
    }

    // Simplified implementations for the complex interface methods
    // These are stubs that can be implemented later if needed

    public Task<Guid> QueueNotificationAsync(EmailNotification notification, CancellationToken cancellationToken = default)
    {
        // For now, just send immediately and return a fake ID
        _ = Task.Run(() => SendNotificationAsync(notification, cancellationToken));
        return Task.FromResult(Guid.NewGuid());
    }

    public Task<List<Guid>> QueueBatchNotificationsAsync(IEnumerable<EmailNotification> notifications, CancellationToken cancellationToken = default)
    {
        var ids = new List<Guid>();
        foreach (var notification in notifications)
        {
            ids.Add(Guid.NewGuid());
            _ = Task.Run(() => SendNotificationAsync(notification, cancellationToken));
        }
        return Task.FromResult(ids);
    }

    public Task<bool> CancelQueuedNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        // Not implemented in simple version
        return Task.FromResult(false);
    }

    public Task<string> RenderTemplateAsync(string templateName, object data, CancellationToken cancellationToken = default)
    {
        // Not implemented in simple version
        return Task.FromResult("Template rendering not implemented in simple version");
    }

    public Task<bool> TemplateExistsAsync(string templateName, CancellationToken cancellationToken = default)
    {
        // Simple implementation - assume all templates exist
        return Task.FromResult(true);
    }

    public async Task<bool> CanSendToUserAsync(string userId, EmailNotificationType type, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get user settings
            var userSettings = await _settingsService.GetUserSettingsAsync(userId);
            if (userSettings?.Notifications == null)
            {
                // If no settings found, default to allowing the email (safe fallback)
                _logger.LogWarning("No user settings found for user {UserId}, defaulting to allow email", userId);
                return true;
            }

            // Map EmailNotificationType to user preference setting
            return type switch
            {
                // Rental notifications
                EmailNotificationType.RentalRequest => userSettings.Notifications.EmailRentalRequests,
                EmailNotificationType.RentalApproved => userSettings.Notifications.EmailRentalUpdates,
                EmailNotificationType.RentalRejected => userSettings.Notifications.EmailRentalUpdates,
                EmailNotificationType.RentalCancelled => userSettings.Notifications.EmailRentalUpdates,
                EmailNotificationType.RentalReminder => userSettings.Notifications.EmailRentalUpdates,
                EmailNotificationType.RentalOverdue => userSettings.Notifications.EmailRentalUpdates,
                EmailNotificationType.RentalReturned => userSettings.Notifications.EmailRentalUpdates,
                
                // Security notifications (always critical, respect security alerts setting)
                EmailNotificationType.LoginAlert => userSettings.Notifications.EmailSecurityAlerts,
                EmailNotificationType.SecurityAlert => userSettings.Notifications.EmailSecurityAlerts,
                EmailNotificationType.PasswordChanged => userSettings.Notifications.EmailSecurityAlerts,
                
                // Messages
                EmailNotificationType.NewMessage => userSettings.Notifications.EmailMessages,
                EmailNotificationType.MessageReceived => userSettings.Notifications.EmailMessages,
                EmailNotificationType.MessageDigest => userSettings.Notifications.EmailMessages,
                
                // Marketing
                EmailNotificationType.Newsletter => userSettings.Notifications.EmailMarketing,
                EmailNotificationType.Promotion => userSettings.Notifications.EmailMarketing,
                EmailNotificationType.ProductUpdate => userSettings.Notifications.EmailMarketing,
                
                // Account-related emails (always send for critical account functions)
                EmailNotificationType.Welcome => true,
                EmailNotificationType.EmailVerification => true,
                EmailNotificationType.PasswordReset => true,
                EmailNotificationType.AccountDeleted => true,
                
                // Two-factor and reviews (use security and messages respectively)
                EmailNotificationType.TwoFactorCode => userSettings.Notifications.EmailSecurityAlerts,
                EmailNotificationType.NewReview => userSettings.Notifications.EmailMessages,
                EmailNotificationType.ReviewResponse => userSettings.Notifications.EmailMessages,
                
                // System notifications (always send for important updates)
                EmailNotificationType.SystemMaintenance => true,
                EmailNotificationType.TermsUpdate => true,
                EmailNotificationType.PrivacyUpdate => true,
                
                // Default to true for unknown types (safe fallback)
                _ => true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user notification preferences for user {UserId}, type {Type}. Defaulting to allow.", userId, type);
            return true; // Default to allowing email if there's an error
        }
    }

    public Task<bool> IsUserUnsubscribedAsync(string email, EmailNotificationType? type = null, CancellationToken cancellationToken = default)
    {
        // Simple implementation - no unsubscribe tracking
        return Task.FromResult(false);
    }

    public Task UnsubscribeUserAsync(string email, EmailNotificationType? type = null, CancellationToken cancellationToken = default)
    {
        // Not implemented in simple version
        return Task.CompletedTask;
    }

    public Task ResubscribeUserAsync(string email, EmailNotificationType? type = null, CancellationToken cancellationToken = default)
    {
        // Not implemented in simple version
        return Task.CompletedTask;
    }

    public Task TrackEmailOpenedAsync(string messageId, CancellationToken cancellationToken = default)
    {
        // Not implemented in simple version
        return Task.CompletedTask;
    }

    public Task TrackEmailClickedAsync(string messageId, string link, CancellationToken cancellationToken = default)
    {
        // Not implemented in simple version
        return Task.CompletedTask;
    }

    public Task<EmailTracking?> GetEmailTrackingAsync(string messageId, CancellationToken cancellationToken = default)
    {
        // Not implemented in simple version
        return Task.FromResult<EmailTracking?>(null);
    }

    public Task<EmailStatistics> GetStatisticsAsync(DateTime from, DateTime to, EmailNotificationType? type = null, CancellationToken cancellationToken = default)
    {
        // Return empty stats
        return Task.FromResult(new EmailStatistics());
    }

    public Task<List<EmailQueueItem>> GetFailedEmailsAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        // Return empty list
        return Task.FromResult(new List<EmailQueueItem>());
    }

    public Task<bool> RetryFailedEmailAsync(Guid queueItemId, CancellationToken cancellationToken = default)
    {
        // Not implemented in simple version
        return Task.FromResult(false);
    }

    public Task<Dictionary<string, bool>> GetUserNotificationPreferencesAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Return default preferences (all enabled)
        var preferences = new Dictionary<string, bool>();
        foreach (EmailNotificationType type in Enum.GetValues<EmailNotificationType>())
        {
            preferences[type.ToString()] = true;
        }
        return Task.FromResult(preferences);
    }

    public Task UpdateUserNotificationPreferencesAsync(string userId, Dictionary<string, bool> preferences, CancellationToken cancellationToken = default)
    {
        // Not implemented in simple version
        return Task.CompletedTask;
    }

    public Task<bool> UnsubscribeUserAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        // Not implemented in simple version
        return Task.FromResult(false);
    }

    public Task<EmailStatistics> GetEmailStatisticsAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Return empty stats
        return Task.FromResult(new EmailStatistics());
    }

    public Task<string> PreviewEmailTemplateAsync(string templateName, object data, CancellationToken cancellationToken = default)
    {
        // Not implemented in simple version
        return Task.FromResult("Template preview not implemented in simple version");
    }

    public Task<QueueStatus> GetQueueStatusAsync(CancellationToken cancellationToken = default)
    {
        // Return empty queue status
        return Task.FromResult(new QueueStatus 
        { 
            PendingCount = 0, 
            ProcessingCount = 0, 
            FailedCount = 0, 
            IsProcessorRunning = false 
        });
    }

    public Task<int> ProcessQueueManuallyAsync(CancellationToken cancellationToken = default)
    {
        // Not implemented in simple version
        return Task.FromResult(0);
    }

    // Dispute notification template methods
    private string GenerateDisputeCreatedTemplate(DisputeCreatedNotification notification, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ background-color: #594AE2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
        .dispute-details {{ background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>⚠️ New Dispute Created</h1>
        </div>
        <div class=""content"">
            <h2>Hello {notification.RecipientName},</h2>
            <p>A new dispute has been created by <strong>{notification.InitiatorName}</strong>.</p>
            <div class=""dispute-details"">
                <h3>{notification.DisputeTitle}</h3>
                <p><strong>Tool:</strong> {notification.RentalToolName}</p>
                <p><strong>Created:</strong> {notification.DisputeCreatedAt:f}</p>
                <p><strong>Dispute ID:</strong> {notification.DisputeId}</p>
            </div>
            <p style=""text-align: center; margin: 30px 0;"">
                <a href=""{notification.DisputeUrl}"" class=""button"">View Dispute Details</a>
            </p>
            <p>Please review the dispute and respond appropriately. Our team is here to help facilitate a fair resolution.</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateDisputeMessageTemplate(DisputeMessageNotification notification, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #17a2b8; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ background-color: #594AE2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
        .message-preview {{ background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #17a2b8; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>💬 New Dispute Message</h1>
        </div>
        <div class=""content"">
            <h2>Hello {notification.RecipientName},</h2>
            <p>You have received a new message in dispute: <strong>{notification.DisputeTitle}</strong></p>
            <div class=""message-preview"">
                <p><strong>From:</strong> {notification.SenderName}</p>
                <p><strong>Message:</strong></p>
                <p style=""font-style: italic;"">""{notification.MessagePreview}""</p>
                <p><strong>Sent:</strong> {notification.MessageCreatedAt:f}</p>
            </div>
            <p style=""text-align: center; margin: 30px 0;"">
                <a href=""{notification.DisputeUrl}"" class=""button"">View Full Message</a>
            </p>
            <p>Please respond promptly to help resolve this dispute efficiently.</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateDisputeStatusChangeTemplate(DisputeStatusChangeNotification notification, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #ffc107; color: #212529; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ background-color: #594AE2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
        .status-change {{ background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>📋 Dispute Status Updated</h1>
        </div>
        <div class=""content"">
            <h2>Hello {notification.RecipientName},</h2>
            <p>The status of dispute <strong>{notification.DisputeTitle}</strong> has been updated.</p>
            <div class=""status-change"">
                <p><strong>Previous Status:</strong> {notification.OldStatus}</p>
                <p><strong>New Status:</strong> {notification.NewStatus}</p>
                <p><strong>Updated:</strong> {notification.UpdatedAt:f}</p>
                {(string.IsNullOrEmpty(notification.Notes) ? "" : $"<p><strong>Notes:</strong> {notification.Notes}</p>")}
            </div>
            <p style=""text-align: center; margin: 30px 0;"">
                <a href=""{notification.DisputeUrl}"" class=""button"">View Dispute</a>
            </p>
            <p>Thank you for your patience as we work to resolve this matter.</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateDisputeEscalationTemplate(DisputeEscalationNotification notification, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ background-color: #594AE2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
        .escalation-info {{ background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #dc3545; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🚨 Dispute Escalated</h1>
        </div>
        <div class=""content"">
            <h2>Hello {notification.RecipientName},</h2>
            <p>The dispute <strong>{notification.DisputeTitle}</strong> has been escalated to our support team.</p>
            <div class=""escalation-info"">
                <p><strong>Escalated by:</strong> {notification.EscalatedBy}</p>
                <p><strong>Escalated on:</strong> {notification.EscalatedAt:f}</p>
                {(string.IsNullOrEmpty(notification.ExternalDisputeId) ? "" : $"<p><strong>External Case ID:</strong> {notification.ExternalDisputeId}</p>")}
            </div>
            <p>Our support team will now review the case and work towards a resolution. You may be contacted for additional information.</p>
            <p style=""text-align: center; margin: 30px 0;"">
                <a href=""{notification.DisputeUrl}"" class=""button"">View Dispute</a>
            </p>
            <p>We appreciate your patience as we work to resolve this matter fairly.</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateDisputeResolutionTemplate(DisputeResolutionNotification notification, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ background-color: #594AE2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
        .resolution-details {{ background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>✅ Dispute Resolved</h1>
        </div>
        <div class=""content"">
            <h2>Hello {notification.RecipientName},</h2>
            <p>Good news! The dispute <strong>{notification.DisputeTitle}</strong> has been resolved.</p>
            <div class=""resolution-details"">
                <p><strong>Resolution:</strong> {notification.Resolution}</p>
                <p><strong>Resolved on:</strong> {notification.ResolvedAt:f}</p>
                {(notification.RefundAmount.HasValue ? $"<p><strong>Refund Amount:</strong> ${notification.RefundAmount:F2}</p>" : "")}
                {(string.IsNullOrEmpty(notification.ResolutionNotes) ? "" : $"<p><strong>Notes:</strong> {notification.ResolutionNotes}</p>")}
            </div>
            <p style=""text-align: center; margin: 30px 0;"">
                <a href=""{notification.DisputeUrl}"" class=""button"">View Final Details</a>
            </p>
            <p>Thank you for your patience throughout this process. If you have any questions about the resolution, please don't hesitate to contact us.</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateDisputeEvidenceTemplate(DisputeEvidenceNotification notification, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #6f42c1; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ background-color: #594AE2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
        .evidence-info {{ background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>📎 New Evidence Uploaded</h1>
        </div>
        <div class=""content"">
            <h2>Hello {notification.RecipientName},</h2>
            <p>New evidence has been uploaded for dispute: <strong>{notification.DisputeTitle}</strong></p>
            <div class=""evidence-info"">
                <p><strong>Uploaded by:</strong> {notification.UploadedBy}</p>
                <p><strong>Number of files:</strong> {notification.FileCount}</p>
                <p><strong>Uploaded on:</strong> {notification.UploadedAt:f}</p>
            </div>
            <p style=""text-align: center; margin: 30px 0;"">
                <a href=""{notification.DisputeUrl}"" class=""button"">Review Evidence</a>
            </p>
            <p>Please review the new evidence and provide any response if necessary.</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateDisputeOverdueTemplate(DisputeOverdueNotification notification, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #fd7e14; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ background-color: #594AE2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
        .overdue-info {{ background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #fd7e14; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>⏰ Dispute Response Overdue</h1>
        </div>
        <div class=""content"">
            <h2>Hello {notification.RecipientName},</h2>
            <p>This is a reminder that your response is overdue for dispute: <strong>{notification.DisputeTitle}</strong></p>
            <div class=""overdue-info"">
                <p><strong>Original due date:</strong> {notification.DueDate:f}</p>
                <p><strong>Days overdue:</strong> {notification.DaysOverdue}</p>
            </div>
            <p style=""text-align: center; margin: 30px 0;"">
                <a href=""{notification.DisputeUrl}"" class=""button"">Respond Now</a>
            </p>
            <p>Please respond as soon as possible to avoid further delays in resolving this dispute. If you need assistance, please contact our support team.</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }
    
    private string GenerateNewMessageTemplate(NewMessageNotification notification, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #594AE2; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .message-preview {{ background-color: white; padding: 15px; border-left: 4px solid #594AE2; margin: 20px 0; }}
        .button {{ background-color: #594AE2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
        .meta {{ color: #666; font-size: 14px; margin-bottom: 10px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>New Message Received</h1>
        </div>
        <div class=""content"">
            <h2>Hello {notification.RecipientName}!</h2>
            <p>You have received a new message from <strong>{notification.SenderName}</strong>.</p>
            
            <div class=""message-preview"">
                <div class=""meta"">
                    <strong>From:</strong> {notification.SenderName} ({notification.SenderEmail})<br>
                    <strong>Subject:</strong> {notification.MessageSubject}
                    {(notification.HasAttachments ? $"<br><strong>Attachments:</strong> {notification.AttachmentCount} file(s)" : "")}
                    {(!string.IsNullOrEmpty(notification.RentalToolName) ? $"<br><strong>Regarding:</strong> {notification.RentalToolName}" : "")}
                </div>
                <p><strong>Message Preview:</strong></p>
                <p>{notification.MessagePreview}</p>
            </div>
            
            <p style=""text-align: center; margin: 30px 0;"">
                <a href=""{notification.MessageUrl}"" class=""button"">Read Full Message</a>
                <a href=""{notification.ConversationUrl}"" class=""button"" style=""margin-left: 10px; background-color: #6c757d;"">View Conversation</a>
            </p>
            
            <p>You can reply directly from the message page or manage your notification preferences in your account settings.</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
            <p><a href=""{frontendUrl}/settings/notifications"">Unsubscribe from message notifications</a></p>
        </div>
    </div>
</body>
</html>";
    }
    
    private string GenerateMessageReplyTemplate(MessageReplyNotification notification, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #594AE2; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .reply-preview {{ background-color: white; padding: 15px; border-left: 4px solid #28a745; margin: 20px 0; }}
        .button {{ background-color: #594AE2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
        .meta {{ color: #666; font-size: 14px; margin-bottom: 10px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>New Reply Received</h1>
        </div>
        <div class=""content"">
            <h2>Hello {notification.RecipientName}!</h2>
            <p><strong>{notification.SenderName}</strong> has replied to your conversation.</p>
            
            <div class=""reply-preview"">
                <div class=""meta"">
                    <strong>Reply to:</strong> {notification.OriginalMessageSubject}
                    {(notification.HasAttachments ? "<br><strong>Includes attachments</strong>" : "")}
                    {(!string.IsNullOrEmpty(notification.RentalToolName) ? $"<br><strong>Regarding:</strong> {notification.RentalToolName}" : "")}
                </div>
                <p><strong>Reply:</strong></p>
                <p>{notification.ReplyContent}</p>
            </div>
            
            <p style=""text-align: center; margin: 30px 0;"">
                <a href=""{notification.ConversationUrl}"" class=""button"">View Conversation & Reply</a>
            </p>
            
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
            <p><a href=""{frontendUrl}/settings/notifications"">Manage notification preferences</a></p>
        </div>
    </div>
</body>
</html>";
    }
    
    private string GenerateMessageModerationTemplate(MessageModerationNotification notification, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: {(notification.IsBlocked ? "#dc3545" : "#ffc107")}; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .alert {{ background-color: {(notification.IsBlocked ? "#f8d7da" : "#fff3cd")}; border: 1px solid {(notification.IsBlocked ? "#f5c6cb" : "#ffeaa7")}; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .button {{ background-color: #594AE2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Message {(notification.IsBlocked ? "Blocked" : "Modified")}</h1>
        </div>
        <div class=""content"">
            <h2>Hello {notification.RecipientName}!</h2>
            
            <div class=""alert"">
                <h3>{(notification.IsBlocked ? "Your message was blocked" : "Your message was modified")}</h3>
                <p><strong>Subject:</strong> {notification.MessageSubject}</p>
                <p><strong>Reason:</strong> {notification.ModerationReason}</p>
            </div>
            
            {(notification.IsBlocked ? 
                "<p>Your message could not be delivered due to a violation of our community guidelines. Please review our terms of service and try sending a revised message.</p>" :
                "<p>Your message has been modified to comply with our community guidelines and has been delivered.</p>")}
            
            {(!notification.IsBlocked ? $"<p><strong>Modified content:</strong><br>{notification.ModeratedContent}</p>" : "")}
            
            <p style=""text-align: center; margin: 30px 0;"">
                <a href=""{notification.AppealUrl}"" class=""button"">Appeal This Decision</a>
                <a href=""{frontendUrl}/terms"" class=""button"" style=""margin-left: 10px; background-color: #6c757d;"">Review Guidelines</a>
            </p>
            
            <p>If you believe this was an error, you can appeal this decision using the button above.</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }
    
    private string GenerateConversationDigestTemplate(ConversationDigestNotification notification, string frontendUrl)
    {
        var messagesList = string.Join("", notification.RecentMessages.Select(m => 
            $@"<li style=""margin-bottom: 15px; padding: 10px; background-color: white; border-radius: 5px;"">
                <strong>{m.SenderName}</strong> <span style=""color: #666; font-size: 12px;"">({m.SentAt:MMM d, yyyy 'at' h:mm tt})</span>
                <br>{m.Content.Substring(0, Math.Min(100, m.Content.Length))}{(m.Content.Length > 100 ? "..." : "")}
                {(m.HasAttachments ? "<br><em style=\"color: #666; font-size: 12px;\">📎 Has attachments</em>" : "")}
            </li>"));
            
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #594AE2; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ background-color: #594AE2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
        .messages-list {{ margin: 20px 0; }}
        .messages-list ul {{ list-style: none; padding: 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Unread Messages Summary</h1>
        </div>
        <div class=""content"">
            <h2>Hello {notification.RecipientName}!</h2>
            <p>You have <strong>{notification.UnreadMessageCount} unread message{(notification.UnreadMessageCount > 1 ? "s" : "")}</strong> from <strong>{notification.OtherParticipantName}</strong>.</p>
            
            <p><strong>Most recent message:</strong><br>
            <em>""{notification.LastMessagePreview}""</em><br>
            <small>Sent on {notification.LastMessageAt:MMMM d, yyyy 'at' h:mm tt}</small></p>
            
            {(notification.RecentMessages.Any() ? $@"
            <div class=""messages-list"">
                <h3>Recent Messages:</h3>
                <ul>
                    {messagesList}
                </ul>
            </div>" : "")}
            
            <p style=""text-align: center; margin: 30px 0;"">
                <a href=""{notification.ConversationUrl}"" class=""button"">Read All Messages</a>
            </p>
            
            <p>Stay connected with your NeighborTools community!</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} NeighborTools. All rights reserved.</p>
            <p><a href=""{frontendUrl}/settings/notifications"">Change email frequency</a></p>
        </div>
    </div>
</body>
</html>";
    }
}