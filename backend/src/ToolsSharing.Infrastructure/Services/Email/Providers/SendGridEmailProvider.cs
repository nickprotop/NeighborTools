using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;

namespace ToolsSharing.Infrastructure.Services.Email.Providers;

public class SendGridEmailProvider : IEmailProvider
{
    private readonly ILogger<SendGridEmailProvider> _logger;
    private readonly EmailSettings _settings;
    private readonly ISendGridClient _client;

    public SendGridEmailProvider(
        ILogger<SendGridEmailProvider> logger,
        IOptions<EmailSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
        _client = new SendGridClient(_settings.ApiKey);
    }

    public EmailProvider ProviderType => EmailProvider.SendGrid;

    public bool IsConfigured => !string.IsNullOrEmpty(_settings.ApiKey);

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("SendGrid provider is not configured");
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = "SendGrid provider is not configured",
                Provider = ProviderType
            };
        }

        try
        {
            var msg = new SendGridMessage
            {
                From = new EmailAddress(message.From, message.FromName),
                Subject = message.Subject,
                HtmlContent = message.HtmlBody,
                PlainTextContent = message.PlainTextBody
            };

            // To
            msg.AddTo(new EmailAddress(message.To, message.ToName));

            // Reply-To
            if (!string.IsNullOrEmpty(message.ReplyTo))
            {
                msg.ReplyTo = new EmailAddress(message.ReplyTo, message.ReplyToName);
            }

            // CC
            foreach (var cc in message.Cc)
            {
                msg.AddCc(cc);
            }

            // BCC
            foreach (var bcc in message.Bcc)
            {
                msg.AddBcc(bcc);
            }

            // Headers
            foreach (var header in message.Headers)
            {
                msg.AddHeader(header.Key, header.Value);
            }

            // Custom tracking settings
            msg.SetClickTracking(true, true);
            msg.SetOpenTracking(true);
            msg.SetGoogleAnalytics(false);
            msg.SetSubscriptionTracking(true);

            // Categories for analytics
            msg.AddCategory(message.Metadata.GetValueOrDefault("NotificationType", "general").ToString());

            // Attachments
            foreach (var attachment in message.Attachments)
            {
                msg.AddAttachment(
                    attachment.FileName,
                    Convert.ToBase64String(attachment.Content),
                    attachment.ContentType,
                    attachment.IsInline ? "inline" : "attachment",
                    attachment.ContentId
                );
            }

            var response = await _client.SendEmailAsync(msg, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var headers = response.Headers;
                var messageId = headers.GetValues("X-Message-Id").FirstOrDefault() ?? Guid.NewGuid().ToString();
                
                _logger.LogInformation("Email sent via SendGrid: {Subject} to {To}", message.Subject, message.To);

                return new EmailSendResult
                {
                    Success = true,
                    MessageId = messageId,
                    Provider = ProviderType,
                    ProviderResponse = new Dictionary<string, object>
                    {
                        ["StatusCode"] = response.StatusCode,
                        ["Headers"] = response.Headers.ToString()
                    }
                };
            }
            else
            {
                var body = await response.Body.ReadAsStringAsync(cancellationToken);
                _logger.LogError("SendGrid error: {StatusCode} - {Body}", response.StatusCode, body);

                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = $"SendGrid error: {response.StatusCode} - {body}",
                    Provider = ProviderType,
                    ProviderResponse = new Dictionary<string, object>
                    {
                        ["StatusCode"] = response.StatusCode,
                        ["Body"] = body
                    }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via SendGrid: {Subject} to {To}", message.Subject, message.To);
            
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
        var personalizations = new List<Personalization>();
        
        // SendGrid supports batch sending with personalizations
        var msg = new SendGridMessage();
        
        foreach (var message in messages.Take(1000)) // SendGrid limit
        {
            var personalization = new Personalization
            {
                Tos = new List<EmailAddress> { new EmailAddress(message.To, message.ToName) },
                Subject = message.Subject
            };
            
            // Add custom headers per recipient
            foreach (var header in message.Headers)
            {
                personalization.Headers.Add(header.Key, header.Value);
            }
            
            personalizations.Add(personalization);
        }
        
        // Set common properties
        var firstMessage = messages.First();
        msg.From = new EmailAddress(firstMessage.From, firstMessage.FromName);
        msg.HtmlContent = firstMessage.HtmlBody;
        msg.PlainTextContent = firstMessage.PlainTextBody;
        msg.Personalizations = personalizations;
        
        var response = await _client.SendEmailAsync(msg, cancellationToken);
        
        // Create results for each message
        foreach (var message in messages)
        {
            results.Add(new EmailSendResult
            {
                Success = response.IsSuccessStatusCode,
                ErrorMessage = response.IsSuccessStatusCode ? null : $"SendGrid batch error: {response.StatusCode}",
                Provider = ProviderType
            });
        }
        
        return results;
    }
}