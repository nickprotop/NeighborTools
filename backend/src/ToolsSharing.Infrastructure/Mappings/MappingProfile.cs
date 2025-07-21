using Mapster;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Features.Tools;
using ToolsSharing.Core.Features.Rentals;
using ToolsSharing.Core.DTOs.Messaging;
using ToolsSharing.Core.DTOs.Dispute;

namespace ToolsSharing.Infrastructure.Mappings;

public static class MappingConfig
{
    public static void ConfigureMappings()
    {
        // Tool mappings
        TypeAdapterConfig<Tool, ToolDto>
            .NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name ?? "")
            .Map(dest => dest.Description, src => src.Description ?? "")
            .Map(dest => dest.Category, src => src.Category ?? "")
            .Map(dest => dest.Brand, src => src.Brand ?? "")
            .Map(dest => dest.Model, src => src.Model ?? "")
            .Map(dest => dest.DailyRate, src => src.DailyRate)
            .Map(dest => dest.WeeklyRate, src => src.WeeklyRate)
            .Map(dest => dest.MonthlyRate, src => src.MonthlyRate)
            .Map(dest => dest.DepositRequired, src => src.DepositRequired)
            .Map(dest => dest.Condition, src => src.Condition ?? "")
            .Map(dest => dest.Location, src => src.Location ?? "")
            .Map(dest => dest.IsAvailable, src => src.IsAvailable)
            .Map(dest => dest.OwnerId, src => src.OwnerId ?? "")
            .Map(dest => dest.OwnerName, src => 
                src.Owner != null ? $"{src.Owner.FirstName ?? ""} {src.Owner.LastName ?? ""}".Trim() : "Unknown Owner")
            .Map(dest => dest.ImageUrls, src => 
                src.Images != null ? src.Images.Select(img => img.ImageUrl ?? "").ToList() : new List<string>());

        TypeAdapterConfig<ToolDto, Tool>
            .NewConfig()
            .Ignore(dest => dest.Images)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.Rentals)
            .Ignore(dest => dest.OwnerId)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.IsDeleted);

        // Rental mappings
        TypeAdapterConfig<Rental, RentalDto>
            .NewConfig()
            .Map(dest => dest.Id, src => src.Id.ToString()) // Convert Guid to string
            .Map(dest => dest.ToolId, src => src.ToolId.ToString()) // Convert Guid to string
            .Map(dest => dest.ToolName, src => src.Tool.Name)
            .Map(dest => dest.RenterName, src => $"{src.Renter.FirstName} {src.Renter.LastName}")
            .Map(dest => dest.OwnerId, src => src.Tool.OwnerId)
            .Map(dest => dest.OwnerName, src => $"{src.Tool.Owner.FirstName} {src.Tool.Owner.LastName}")
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.IsPaid, src => false) // Will be set separately based on payment status
            .Map(dest => dest.Tool, src => src.Tool); // Map the full Tool object

        TypeAdapterConfig<RentalDto, Rental>
            .NewConfig()
            .Ignore(dest => dest.Tool)
            .Ignore(dest => dest.Renter)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.ApprovedAt)
            .Ignore(dest => dest.CancelledAt)
            .Ignore(dest => dest.CancellationReason);

        // Message mappings
        TypeAdapterConfig<Message, MessageDto>
            .NewConfig()
            .Map(dest => dest.Id, src => src.Id.ToString())
            .Map(dest => dest.SenderName, src => $"{src.Sender.FirstName} {src.Sender.LastName}")
            .Map(dest => dest.SenderEmail, src => src.Sender.Email)
            .Map(dest => dest.RecipientName, src => $"{src.Recipient.FirstName} {src.Recipient.LastName}")
            .Map(dest => dest.RecipientEmail, src => src.Recipient.Email)
            .Map(dest => dest.RentalToolName, src => src.Rental != null ? src.Rental.Tool.Name : null)
            .Map(dest => dest.ToolName, src => src.Tool != null ? src.Tool.Name : null);

        TypeAdapterConfig<MessageAttachment, MessageAttachmentDto>
            .NewConfig()
            .Map(dest => dest.Id, src => src.Id.ToString())
            .Map(dest => dest.DownloadUrl, src => $"/api/messages/attachments/{src.Id}");

        TypeAdapterConfig<Conversation, ConversationDto>
            .NewConfig()
            .Map(dest => dest.Id, src => src.Id.ToString())
            .Map(dest => dest.Participant1Name, src => $"{src.Participant1.FirstName} {src.Participant1.LastName}")
            .Map(dest => dest.Participant2Name, src => $"{src.Participant2.FirstName} {src.Participant2.LastName}")
            .Map(dest => dest.LastMessageContent, src => src.LastMessage != null ? 
                (src.LastMessage.Content.Length > 100 ? src.LastMessage.Content.Substring(0, 100) + "..." : src.LastMessage.Content) : null)
            .Map(dest => dest.LastMessageSenderId, src => src.LastMessage != null ? src.LastMessage.SenderId : null)
            .Map(dest => dest.RentalToolName, src => src.Rental != null ? src.Rental.Tool.Name : null)
            .Map(dest => dest.ToolName, src => src.Tool != null ? src.Tool.Name : null);

        TypeAdapterConfig<Conversation, ConversationDetailsDto>
            .NewConfig()
            .Map(dest => dest.Id, src => src.Id.ToString())
            .Map(dest => dest.Participant1Name, src => $"{src.Participant1.FirstName} {src.Participant1.LastName}")
            .Map(dest => dest.Participant2Name, src => $"{src.Participant2.FirstName} {src.Participant2.LastName}")
            .Map(dest => dest.RentalToolName, src => src.Rental != null ? src.Rental.Tool.Name : null)
            .Map(dest => dest.ToolName, src => src.Tool != null ? src.Tool.Name : null);

        TypeAdapterConfig<Message, MessageSummaryDto>
            .NewConfig()
            .Map(dest => dest.Id, src => src.Id.ToString())
            .Map(dest => dest.SenderName, src => $"{src.Sender.FirstName} {src.Sender.LastName}")
            .Map(dest => dest.RecipientName, src => $"{src.Recipient.FirstName} {src.Recipient.LastName}")
            .Map(dest => dest.PreviewContent, src => src.Content.Length > 100 ? src.Content.Substring(0, 100) + "..." : src.Content)
            .Map(dest => dest.AttachmentCount, src => src.Attachments.Count);

        // Mutual Closure mappings
        TypeAdapterConfig<MutualDisputeClosure, MutualClosureDto>
            .NewConfig()
            .Map(dest => dest.DisputeTitle, src => src.Dispute.Title ?? "")
            .Map(dest => dest.InitiatedByUserName, src => $"{src.InitiatedByUser.FirstName} {src.InitiatedByUser.LastName}")
            .Map(dest => dest.ResponseRequiredFromUserName, src => $"{src.ResponseRequiredFromUser.FirstName} {src.ResponseRequiredFromUser.LastName}")
            .Map(dest => dest.AuditLogs, src => src.AuditLogs);

        TypeAdapterConfig<MutualDisputeClosure, MutualClosureSummaryDto>
            .NewConfig()
            .Map(dest => dest.DisputeTitle, src => src.Dispute.Title ?? "")
            .Map(dest => dest.InitiatedByUserName, src => $"{src.InitiatedByUser.FirstName} {src.InitiatedByUser.LastName}")
            .Map(dest => dest.ResponseRequiredFromUserName, src => $"{src.ResponseRequiredFromUser.FirstName} {src.ResponseRequiredFromUser.LastName}")
            .Map(dest => dest.StatusDisplay, src => src.Status.ToString());

        TypeAdapterConfig<MutualClosureAuditLog, MutualClosureAuditLogDto>
            .NewConfig()
            .Map(dest => dest.UserName, src => $"{src.User.FirstName} {src.User.LastName}");
    }
}