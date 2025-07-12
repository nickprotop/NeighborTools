using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services.Email;

public class EmailQueueProcessor : BackgroundService, IEmailQueueProcessor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailQueueProcessor> _logger;
    private readonly EmailSettings _emailSettings;
    private readonly TimeSpan _processInterval = TimeSpan.FromSeconds(30);

    public EmailQueueProcessor(
        IServiceProvider serviceProvider,
        ILogger<EmailQueueProcessor> logger,
        IOptions<EmailSettings> emailSettings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _emailSettings = emailSettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_emailSettings.EnableQueue)
        {
            _logger.LogInformation("Email queue processing is disabled");
            return;
        }

        _logger.LogInformation("Email queue processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueueAsync(stoppingToken);
                await Task.Delay(_processInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in email queue processor");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Email queue processor stopped");
    }

    public async Task ProcessQueueAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailProvider = scope.ServiceProvider.GetRequiredService<IEmailProvider>();

        // Get pending emails
        var pendingEmails = await dbContext.Set<EmailQueueItem>()
            .Where(e => e.Status == EmailStatus.Pending && 
                       (e.ScheduledFor == null || e.ScheduledFor <= DateTime.UtcNow))
            .OrderBy(e => e.Priority)
            .ThenBy(e => e.CreatedAt)
            .Take(_emailSettings.BatchSize)
            .ToListAsync(cancellationToken);

        if (!pendingEmails.Any())
        {
            return;
        }

        _logger.LogInformation("Processing {Count} pending emails", pendingEmails.Count);

        foreach (var item in pendingEmails)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await ProcessItemAsync(item, cancellationToken);
        }
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.Set<EmailQueueItem>()
            .CountAsync(e => e.Status == EmailStatus.Pending, cancellationToken);
    }

    public async Task<bool> ProcessItemAsync(EmailQueueItem item, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailProvider = scope.ServiceProvider.GetRequiredService<IEmailProvider>();

        try
        {
            item.Status = EmailStatus.Processing;
            item.LastAttemptAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            // Create email message
            var message = new EmailMessage
            {
                To = item.RecipientEmail,
                ToName = item.RecipientName,
                From = _emailSettings.FromEmail,
                FromName = _emailSettings.FromName,
                ReplyTo = _emailSettings.ReplyToEmail,
                ReplyToName = _emailSettings.ReplyToName,
                Subject = item.Subject,
                HtmlBody = item.Body,
                PlainTextBody = item.PlainTextBody,
                Priority = item.Priority,
                Headers = item.Headers,
                Metadata = item.Metadata
            };

            // Add tracking headers
            message.Headers["X-NeighborTools-MessageId"] = item.Id.ToString();
            message.Headers["X-NeighborTools-NotificationType"] = item.NotificationType.ToString();
            
            if (!string.IsNullOrEmpty(item.UserId))
            {
                message.Headers["X-NeighborTools-UserId"] = item.UserId;
            }

            // Send email
            var result = await emailProvider.SendAsync(message, cancellationToken);

            if (result.Success)
            {
                item.Status = EmailStatus.Sent;
                item.ProcessedAt = DateTime.UtcNow;
                item.MessageId = result.MessageId;
                
                _logger.LogInformation("Email sent successfully: {Subject} to {Email}", item.Subject, item.RecipientEmail);
            }
            else
            {
                item.RetryCount++;
                item.ErrorMessage = result.ErrorMessage;

                if (item.RetryCount >= _emailSettings.MaxRetries)
                {
                    item.Status = EmailStatus.Failed;
                    _logger.LogError("Email failed after {Retries} retries: {Subject} to {Email}. Error: {Error}",
                        item.RetryCount, item.Subject, item.RecipientEmail, result.ErrorMessage);
                }
                else
                {
                    item.Status = EmailStatus.Pending;
                    item.ScheduledFor = DateTime.UtcNow.AddSeconds(_emailSettings.RetryDelaySeconds * item.RetryCount);
                    _logger.LogWarning("Email failed, will retry: {Subject} to {Email}. Error: {Error}",
                        item.Subject, item.RecipientEmail, result.ErrorMessage);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email queue item {Id}", item.Id);
            
            item.Status = EmailStatus.Failed;
            item.ErrorMessage = ex.Message;
            await dbContext.SaveChangesAsync(cancellationToken);
            
            return false;
        }
    }
}