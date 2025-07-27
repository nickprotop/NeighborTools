using ToolsSharing.Core.DTOs.Messaging;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.Features.Messaging;

public record GetMessagesQuery(
    string UserId,
    int Page = 1,
    int PageSize = 20,
    bool? IsRead = null,
    bool? IsArchived = null,
    MessageType? Type = null,
    string? SearchTerm = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
);

public record GetMessageByIdQuery(
    Guid MessageId,
    string UserId
);

public record GetConversationsQuery(
    string UserId,
    int Page = 1,
    int PageSize = 20,
    bool? IsArchived = null,
    string? SearchTerm = null
);

public record GetConversationByIdQuery(
    Guid ConversationId,
    string UserId,
    int Page = 1,
    int PageSize = 50
);

public record GetConversationBetweenUsersQuery(
    string User1Id,
    string User2Id,
    Guid? RentalId = null,
    Guid? ToolId = null
);

public record GetUnreadMessageCountQuery(
    string UserId
);

public record GetMessageStatisticsQuery(
    DateTime? FromDate = null,
    DateTime? ToDate = null
)
{
    public string UserId { get; init; } = string.Empty;
}

public record SearchMessagesQuery(
    string UserId,
    string SearchTerm,
    int Page = 1,
    int PageSize = 20,
    MessageType? Type = null,
    bool? IsRead = null
);

public record GetMessageAttachmentQuery(
    Guid AttachmentId,
    string UserId
);

public record GetModeratedMessagesQuery(
    int Page = 1,
    int PageSize = 20,
    DateTime? FromDate = null,
    DateTime? ToDate = null
);