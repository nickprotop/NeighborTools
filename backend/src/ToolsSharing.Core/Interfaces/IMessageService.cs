using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.DTOs.Messaging;
using ToolsSharing.Core.Features.Messaging;

namespace ToolsSharing.Core.Interfaces;

public interface IMessageService
{
    // Message operations
    Task<ApiResponse<MessageDto>> SendMessageAsync(SendMessageCommand command);
    Task<ApiResponse<MessageDto>> ReplyToMessageAsync(ReplyToMessageCommand command);
    Task<ApiResponse<MessageDto>> GetMessageByIdAsync(GetMessageByIdQuery query);
    Task<ApiResponse<List<MessageSummaryDto>>> GetMessagesAsync(GetMessagesQuery query);
    Task<ApiResponse<List<MessageSummaryDto>>> SearchMessagesAsync(SearchMessagesQuery query);
    Task<ApiResponse<bool>> MarkMessageAsReadAsync(MarkMessageAsReadCommand command);
    Task<ApiResponse<bool>> ArchiveMessageAsync(ArchiveMessageCommand command);
    Task<ApiResponse<bool>> DeleteMessageAsync(DeleteMessageCommand command);
    Task<ApiResponse<bool>> ModerateMessageAsync(ModerateMessageCommand command);
    
    // Conversation operations
    Task<ApiResponse<ConversationDto>> CreateConversationAsync(CreateConversationCommand command);
    Task<ApiResponse<ConversationDto>> UpdateConversationAsync(UpdateConversationCommand command);
    Task<ApiResponse<List<ConversationDto>>> GetConversationsAsync(GetConversationsQuery query);
    Task<ApiResponse<ConversationDetailsDto>> GetConversationByIdAsync(GetConversationByIdQuery query);
    Task<ApiResponse<ConversationDto>> GetConversationBetweenUsersAsync(GetConversationBetweenUsersQuery query);
    Task<ApiResponse<bool>> MarkConversationAsReadAsync(MarkConversationAsReadCommand command);
    Task<ApiResponse<bool>> ArchiveConversationAsync(ArchiveConversationCommand command);
    
    // Statistics and utilities
    Task<ApiResponse<int>> GetUnreadMessageCountAsync(GetUnreadMessageCountQuery query);
    Task<ApiResponse<MessageStatisticsDto>> GetMessageStatisticsAsync(GetMessageStatisticsQuery query);
    Task<ApiResponse<byte[]>> GetMessageAttachmentAsync(GetMessageAttachmentQuery query);
    Task<ApiResponse<List<MessageSummaryDto>>> GetModeratedMessagesAsync(GetModeratedMessagesQuery query);
    
    // Legal moderation hook
    Task<ApiResponse<bool>> ValidateMessageContentAsync(string content, string senderId);
}