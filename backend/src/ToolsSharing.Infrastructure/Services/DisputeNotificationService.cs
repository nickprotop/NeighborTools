using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Common.Models.EmailNotifications;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Interfaces;

namespace ToolsSharing.Infrastructure.Services;

public class DisputeNotificationService : IDisputeNotificationService
{
    private readonly IEmailNotificationService _emailService;
    private readonly ILogger<DisputeNotificationService> _logger;
    private readonly IConfiguration _configuration;

    public DisputeNotificationService(
        IEmailNotificationService emailService,
        ILogger<DisputeNotificationService> logger,
        IConfiguration configuration)
    {
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendDisputeCreatedNotificationAsync(Dispute dispute, User initiator, User otherParty)
    {
        try
        {
            var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5000";
            var notification = new DisputeCreatedNotification
            {
                RecipientEmail = otherParty.Email,
                RecipientName = $"{otherParty.FirstName} {otherParty.LastName}",
                UserId = otherParty.Id,
                Type = EmailNotificationType.DisputeCreated,
                Priority = EmailPriority.High,
                DisputeId = dispute.Id.ToString(),
                DisputeTitle = dispute.Title,
                InitiatorName = $"{initiator.FirstName} {initiator.LastName}",
                RentalToolName = dispute.Rental?.Tool?.Name ?? "Unknown Tool",
                DisputeCreatedAt = dispute.CreatedAt,
                DisputeUrl = $"{frontendUrl}/disputes/{dispute.Id}"
            };

            await _emailService.SendNotificationAsync(notification);

            _logger.LogInformation("Dispute created notification sent for dispute {DisputeId} to {Email}", 
                dispute.Id, otherParty.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending dispute created notification for dispute {DisputeId}", dispute.Id);
        }
    }

    public async Task SendNewMessageNotificationAsync(Dispute dispute, DisputeMessage message, User recipient)
    {
        try
        {
            var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5000";
            var notification = new DisputeMessageNotification
            {
                RecipientEmail = recipient.Email,
                RecipientName = $"{recipient.FirstName} {recipient.LastName}",
                UserId = recipient.Id,
                Type = EmailNotificationType.NewMessage,
                Priority = EmailPriority.Normal,
                DisputeId = dispute.Id.ToString(),
                DisputeTitle = dispute.Title,
                SenderName = message.SenderName,
                MessagePreview = message.Message.Length > 100 ? 
                    message.Message.Substring(0, 100) + "..." : 
                    message.Message,
                MessageCreatedAt = message.CreatedAt,
                DisputeUrl = $"{frontendUrl}/disputes/{dispute.Id}"
            };

            await _emailService.SendNotificationAsync(notification);

            _logger.LogInformation("Dispute message notification sent for dispute {DisputeId} to {Email}", 
                dispute.Id, recipient.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending dispute message notification for dispute {DisputeId}", dispute.Id);
        }
    }

    public async Task SendStatusChangeNotificationAsync(Dispute dispute, DisputeStatus oldStatus, DisputeStatus newStatus, string? notes = null)
    {
        try
        {
            var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5000";

            // Send to all parties involved
            var parties = new List<User>();
            if (dispute.Rental?.Owner != null) parties.Add(dispute.Rental.Owner);
            if (dispute.Rental?.Renter != null) parties.Add(dispute.Rental.Renter);

            foreach (var party in parties.Distinct())
            {
                var notification = new DisputeStatusChangeNotification
                {
                    RecipientEmail = party.Email,
                    RecipientName = $"{party.FirstName} {party.LastName}",
                    UserId = party.Id,
                    Type = EmailNotificationType.DisputeResolved,
                    Priority = EmailPriority.High,
                    DisputeId = dispute.Id.ToString(),
                    DisputeTitle = dispute.Title,
                    OldStatus = oldStatus.ToString(),
                    NewStatus = newStatus.ToString(),
                    Notes = notes ?? "",
                    UpdatedAt = DateTime.UtcNow,
                    DisputeUrl = $"{frontendUrl}/disputes/{dispute.Id}"
                };

                await _emailService.SendNotificationAsync(notification);
            }

            _logger.LogInformation("Dispute status change notification sent for dispute {DisputeId} from {OldStatus} to {NewStatus}", 
                dispute.Id, oldStatus, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending dispute status change notification for dispute {DisputeId}", dispute.Id);
        }
    }

    public async Task SendEscalationNotificationAsync(Dispute dispute, string escalatedBy)
    {
        try
        {
            var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5000";

            // Send to all parties involved
            var parties = new List<User>();
            if (dispute.Rental?.Owner != null) parties.Add(dispute.Rental.Owner);
            if (dispute.Rental?.Renter != null) parties.Add(dispute.Rental.Renter);

            foreach (var party in parties.Distinct())
            {
                var notification = new DisputeEscalationNotification
                {
                    RecipientEmail = party.Email,
                    RecipientName = $"{party.FirstName} {party.LastName}",
                    UserId = party.Id,
                    Type = EmailNotificationType.DisputeEscalated,
                    Priority = EmailPriority.High,
                    DisputeId = dispute.Id.ToString(),
                    DisputeTitle = dispute.Title,
                    EscalatedBy = escalatedBy,
                    EscalatedAt = DateTime.UtcNow,
                    ExternalDisputeId = dispute.ExternalDisputeId,
                    DisputeUrl = $"{frontendUrl}/disputes/{dispute.Id}"
                };

                await _emailService.SendNotificationAsync(notification);
            }

            _logger.LogInformation("Dispute escalation notification sent for dispute {DisputeId}", dispute.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending dispute escalation notification for dispute {DisputeId}", dispute.Id);
        }
    }

    public async Task SendResolutionNotificationAsync(Dispute dispute, DisputeResolution resolution, string? notes = null)
    {
        try
        {
            var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5000";

            // Send to all parties involved
            var parties = new List<User>();
            if (dispute.Rental?.Owner != null) parties.Add(dispute.Rental.Owner);
            if (dispute.Rental?.Renter != null) parties.Add(dispute.Rental.Renter);

            foreach (var party in parties.Distinct())
            {
                var notification = new DisputeResolutionNotification
                {
                    RecipientEmail = party.Email,
                    RecipientName = $"{party.FirstName} {party.LastName}",
                    UserId = party.Id,
                    Type = EmailNotificationType.DisputeResolved,
                    Priority = EmailPriority.High,
                    DisputeId = dispute.Id.ToString(),
                    DisputeTitle = dispute.Title,
                    Resolution = resolution.ToString(),
                    ResolutionNotes = notes ?? "",
                    ResolvedAt = dispute.ResolvedAt ?? DateTime.UtcNow,
                    RefundAmount = dispute.RefundAmount,
                    DisputeUrl = $"{frontendUrl}/disputes/{dispute.Id}"
                };

                await _emailService.SendNotificationAsync(notification);
            }

            _logger.LogInformation("Dispute resolution notification sent for dispute {DisputeId} with resolution {Resolution}", 
                dispute.Id, resolution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending dispute resolution notification for dispute {DisputeId}", dispute.Id);
        }
    }

    public async Task SendEvidenceUploadedNotificationAsync(Dispute dispute, string uploadedBy, int fileCount)
    {
        try
        {
            var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5000";

            // Send to other parties (not the uploader)
            var parties = new List<User>();
            if (dispute.Rental?.Owner != null && dispute.Rental.Owner.Id != uploadedBy) 
                parties.Add(dispute.Rental.Owner);
            if (dispute.Rental?.Renter != null && dispute.Rental.Renter.Id != uploadedBy) 
                parties.Add(dispute.Rental.Renter);

            foreach (var party in parties)
            {
                var notification = new DisputeEvidenceNotification
                {
                    RecipientEmail = party.Email,
                    RecipientName = $"{party.FirstName} {party.LastName}",
                    UserId = party.Id,
                    Type = EmailNotificationType.GeneralNotification,
                    Priority = EmailPriority.Normal,
                    DisputeId = dispute.Id.ToString(),
                    DisputeTitle = dispute.Title,
                    UploadedBy = uploadedBy,
                    FileCount = fileCount,
                    UploadedAt = DateTime.UtcNow,
                    DisputeUrl = $"{frontendUrl}/disputes/{dispute.Id}"
                };

                await _emailService.SendNotificationAsync(notification);
            }

            _logger.LogInformation("Evidence upload notification sent for dispute {DisputeId}, {FileCount} files", 
                dispute.Id, fileCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending evidence upload notification for dispute {DisputeId}", dispute.Id);
        }
    }

    public async Task SendOverdueReminderNotificationAsync(Dispute dispute, User recipient)
    {
        try
        {
            var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5000";
            var notification = new DisputeOverdueNotification
            {
                RecipientEmail = recipient.Email,
                RecipientName = $"{recipient.FirstName} {recipient.LastName}",
                UserId = recipient.Id,
                Type = EmailNotificationType.GeneralNotification,
                Priority = EmailPriority.High,
                DisputeId = dispute.Id.ToString(),
                DisputeTitle = dispute.Title,
                DueDate = dispute.ResponseDueDate ?? DateTime.UtcNow,
                DaysOverdue = dispute.ResponseDueDate.HasValue ? 
                    (DateTime.UtcNow - dispute.ResponseDueDate.Value).Days : 0,
                DisputeUrl = $"{frontendUrl}/disputes/{dispute.Id}"
            };

            await _emailService.SendNotificationAsync(notification);

            _logger.LogInformation("Overdue reminder notification sent for dispute {DisputeId} to {Email}", 
                dispute.Id, recipient.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending overdue reminder notification for dispute {DisputeId}", dispute.Id);
        }
    }
}