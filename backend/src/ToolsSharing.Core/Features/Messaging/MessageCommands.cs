using ToolsSharing.Core.DTOs.Messaging;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.Features.Messaging;

public record SendMessageCommand(
    string SenderId,
    string RecipientId,
    string Subject,
    string Content,
    Guid? ConversationId = null,
    Guid? RentalId = null,
    Guid? ToolId = null,
    MessagePriority Priority = MessagePriority.Normal,
    MessageType Type = MessageType.Direct,
    List<MessageAttachmentUpload>? Attachments = null
);

public record ReplyToMessageCommand(
    string SenderId,
    Guid MessageId,
    string Content,
    List<MessageAttachmentUpload>? Attachments = null
);

public record MarkMessageAsReadCommand(
    string UserId,
    Guid MessageId
);

public record MarkConversationAsReadCommand(
    string UserId,
    Guid ConversationId
);

public record ArchiveMessageCommand(
    string UserId,
    Guid MessageId
);

public record ArchiveConversationCommand(
    string UserId,
    Guid ConversationId
);

public record DeleteMessageCommand(
    string UserId,
    Guid MessageId
);

public record ModerateMessageCommand(
    string ModeratorId,
    Guid MessageId,
    string Reason,
    string? ModifiedContent = null
);

public record CreateConversationCommand(
    string Participant1Id,
    string Participant2Id,
    string? Title = null,
    Guid? RentalId = null,
    Guid? ToolId = null
);

public record UpdateConversationCommand(
    string UserId,
    Guid ConversationId,
    string? Title = null,
    bool? IsArchived = null
);