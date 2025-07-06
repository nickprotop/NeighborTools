using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using ToolsSharing.Core.Common.Interfaces;

namespace ToolsSharing.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetToken, string userName)
    {
        try
        {
            var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5003";
            var resetUrl = $"{frontendUrl}/reset-password?token={resetToken}&email={email}";

            var subject = "Reset Your NeighborTools Password";
            var body = $@"
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
            <p>Hello {userName},</p>
            <p>We received a request to reset your password for your NeighborTools account. If you didn't make this request, you can safely ignore this email.</p>
            <p>To reset your password, click the button below:</p>
            <p style=""text-align: center; margin: 30px 0;"">
                <a href=""{resetUrl}"" class=""button"">Reset Password</a>
            </p>
            <p><strong>This link will expire in 24 hours for security reasons.</strong></p>
            <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
            <p style=""word-break: break-all; color: #594AE2;"">{resetUrl}</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© 2024 NeighborTools. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(email, subject, body);
            _logger.LogInformation("Password reset email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
            throw;
        }
    }

    public async Task SendRentalNotificationAsync(string email, string userName, string toolName, string message)
    {
        try
        {
            var subject = $"NeighborTools: Update on your rental of {toolName}";
            var body = $@"
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
            <h2>Rental Update</h2>
            <p>Hello {userName},</p>
            <p>We have an update regarding your rental of <strong>{toolName}</strong>:</p>
            <p>{message}</p>
            <p>You can view your rental details by logging into your NeighborTools account.</p>
            <p>Best regards,<br>The NeighborTools Team</p>
        </div>
        <div class=""footer"">
            <p>© 2024 NeighborTools. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(email, subject, body);
            _logger.LogInformation("Rental notification email sent to {Email} for tool {ToolName}", email, toolName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send rental notification email to {Email}", email);
            throw;
        }
    }

    public async Task SendWelcomeEmailAsync(string email, string userName)
    {
        try
        {
            var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5003";

            var subject = "Welcome to NeighborTools!";
            var body = $@"
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
            <h2>Hello {userName}!</h2>
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
            <p>© 2024 NeighborTools. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(email, subject, body);
            _logger.LogInformation("Welcome email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", email);
            throw;
        }
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
        message.From = new MailAddress(fromEmail, fromName);
        message.To.Add(toEmail);
        message.Subject = subject;
        message.Body = body;
        message.IsBodyHtml = true;

        await client.SendMailAsync(message);
    }
}