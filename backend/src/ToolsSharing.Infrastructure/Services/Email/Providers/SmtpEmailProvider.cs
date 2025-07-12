using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;

namespace ToolsSharing.Infrastructure.Services.Email.Providers;

public class SmtpEmailProvider : IEmailProvider
{
    private readonly ILogger<SmtpEmailProvider> _logger;
    private readonly EmailSettings _settings;

    public SmtpEmailProvider(
        ILogger<SmtpEmailProvider> logger,
        IOptions<EmailSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public EmailProvider ProviderType => EmailProvider.Smtp;

    public bool IsConfigured => !string.IsNullOrEmpty(_settings.SmtpServer) && 
                               !string.IsNullOrEmpty(_settings.SmtpUsername);

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("SMTP provider is not configured");
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = "SMTP provider is not configured",
                Provider = ProviderType
            };
        }

        try
        {
            using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword),
                Timeout = _settings.TimeoutSeconds * 1000
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(message.From, message.FromName),
                Subject = message.Subject,
                Body = message.HtmlBody,
                IsBodyHtml = true,
                Priority = ConvertPriority(message.Priority)
            };

            // To
            mailMessage.To.Add(new MailAddress(message.To, message.ToName));

            // Reply-To
            if (!string.IsNullOrEmpty(message.ReplyTo))
            {
                mailMessage.ReplyToList.Add(new MailAddress(message.ReplyTo, message.ReplyToName ?? ""));
            }

            // CC
            foreach (var cc in message.Cc)
            {
                mailMessage.CC.Add(cc);
            }

            // BCC
            foreach (var bcc in message.Bcc)
            {
                mailMessage.Bcc.Add(bcc);
            }

            // Headers
            foreach (var header in message.Headers)
            {
                mailMessage.Headers.Add(header.Key, header.Value);
            }

            // Plain text alternative
            if (!string.IsNullOrEmpty(message.PlainTextBody))
            {
                var plainTextView = AlternateView.CreateAlternateViewFromString(
                    message.PlainTextBody, null, "text/plain");
                mailMessage.AlternateViews.Add(plainTextView);

                var htmlView = AlternateView.CreateAlternateViewFromString(
                    message.HtmlBody, null, "text/html");
                mailMessage.AlternateViews.Add(htmlView);
            }

            // Attachments
            foreach (var attachment in message.Attachments)
            {
                var mailAttachment = new Attachment(
                    new MemoryStream(attachment.Content),
                    attachment.FileName,
                    attachment.ContentType);

                if (attachment.IsInline && !string.IsNullOrEmpty(attachment.ContentId))
                {
                    mailAttachment.ContentId = attachment.ContentId;
                    mailAttachment.ContentDisposition.Inline = true;
                }

                mailMessage.Attachments.Add(mailAttachment);
            }

            await client.SendMailAsync(mailMessage, cancellationToken);

            var messageId = mailMessage.Headers["Message-ID"] ?? Guid.NewGuid().ToString();
            
            _logger.LogInformation("Email sent via SMTP: {Subject} to {To}", message.Subject, message.To);

            return new EmailSendResult
            {
                Success = true,
                MessageId = messageId,
                Provider = ProviderType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via SMTP: {Subject} to {To}", message.Subject, message.To);
            
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Provider = ProviderType
            };
        }
    }

    public async Task<List<EmailSendResult>> SendBatchAsync(IEnumerable<EmailMessage> messages, CancellationToken cancellationToken = default)
    {
        var results = new List<EmailSendResult>();
        
        // SMTP doesn't support true batch sending, so we send individually
        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
                
            results.Add(await SendAsync(message, cancellationToken));
            
            // Small delay to avoid overwhelming the server
            await Task.Delay(100, cancellationToken);
        }
        
        return results;
    }

    private static MailPriority ConvertPriority(EmailPriority priority)
    {
        return priority switch
        {
            EmailPriority.Low => MailPriority.Low,
            EmailPriority.High or EmailPriority.Critical => MailPriority.High,
            _ => MailPriority.Normal
        };
    }
}