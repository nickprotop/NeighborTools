using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Common.Models.EmailNotifications;
using ToolsSharing.Core.DTOs.Messaging;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Features.Messaging;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Features.Messaging;

public class MessageService : IMessageService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IContentModerationService _contentModerationService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly IMobileNotificationService _mobileNotificationService;
    private readonly ISettingsService _settingsService;

    public MessageService(
        ApplicationDbContext context,
        IMapper mapper,
        IContentModerationService contentModerationService,
        IFileStorageService fileStorageService,
        IEmailNotificationService emailNotificationService,
        IMobileNotificationService mobileNotificationService,
        ISettingsService settingsService)
    {
        _context = context;
        _mapper = mapper;
        _contentModerationService = contentModerationService;
        _fileStorageService = fileStorageService;
        _emailNotificationService = emailNotificationService;
        _mobileNotificationService = mobileNotificationService;
        _settingsService = settingsService;
    }

    public async Task<ApiResponse<MessageDto>> SendMessageAsync(SendMessageCommand command)
    {
        try
        {
            // Validate users exist
            var sender = await _context.Users.FindAsync(command.SenderId);
            var recipient = await _context.Users.FindAsync(command.RecipientId);

            if (sender == null || recipient == null)
            {
                return ApiResponse<MessageDto>.CreateFailure("Sender or recipient not found");
            }

            // Check if recipient allows direct messages
            var recipientSettings = await _settingsService.GetUserSettingsAsync(command.RecipientId);
            if (recipientSettings?.Communication?.AllowDirectMessages == false)
            {
                return ApiResponse<MessageDto>.CreateFailure("Recipient does not accept direct messages");
            }

            // Content moderation
            var moderationResult = await _contentModerationService.ValidateContentAsync(command.Content, command.SenderId);
            
            // Get or create conversation (needed for audit trail even for blocked messages)
            var conversation = await GetOrCreateConversationAsync(command.SenderId, command.RecipientId, command.ConversationId, command.RentalId, command.ToolId);

            // Determine if message should be blocked (Severe/Critical violations)
            bool isBlocked = !moderationResult.IsApproved && moderationResult.Severity >= ModerationSeverity.Severe;

            // Create message (ALWAYS save to database for audit trail)
            var message = new Message
            {
                Id = Guid.NewGuid(),
                SenderId = command.SenderId,
                RecipientId = command.RecipientId,
                Subject = command.Subject,
                Content = isBlocked ? command.Content : (moderationResult.ModifiedContent ?? command.Content), // Keep original for blocked messages
                OriginalContent = !isBlocked && moderationResult.ModifiedContent != null ? command.Content : null,
                ConversationId = conversation.Id,
                RentalId = command.RentalId,
                ToolId = command.ToolId,
                Priority = command.Priority,
                Type = command.Type,
                IsModerated = !moderationResult.IsApproved,
                IsBlocked = isBlocked,
                ModerationReason = moderationResult.ModerationReason,
                ModeratedAt = !moderationResult.IsApproved ? DateTime.UtcNow : null,
                ModeratedBy = !moderationResult.IsApproved ? "auto_moderation" : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);

            // Handle attachments
            if (command.Attachments?.Any() == true)
            {
                foreach (var attachment in command.Attachments)
                {
                    try
                    {
                        using var stream = new MemoryStream(attachment.Content);
                        var storagePath = await _fileStorageService.UploadFileAsync(
                            stream, attachment.FileName, attachment.ContentType, "messages/attachments");

                        var messageAttachment = new MessageAttachment
                        {
                            MessageId = message.Id,
                            FileName = Path.GetFileNameWithoutExtension(attachment.FileName) + "_" + Guid.NewGuid() + Path.GetExtension(attachment.FileName),
                            OriginalFileName = attachment.FileName,
                            ContentType = attachment.ContentType,
                            FileSize = attachment.FileSize,
                            StoragePath = storagePath,
                            IsScanned = true, // In a real system, you'd scan for viruses
                            IsSafe = true
                        };

                        _context.MessageAttachments.Add(messageAttachment);
                    }
                    catch (Exception ex)
                    {
                        // Log attachment error but don't fail the message
                        Console.WriteLine($"Failed to upload attachment: {ex.Message}");
                    }
                }
            }

            // Update conversation only for non-blocked messages
            if (!isBlocked)
            {
                conversation.LastMessageAt = DateTime.UtcNow;
                conversation.LastMessageId = message.Id;
                conversation.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // For blocked messages: return error to user but message is saved for audit
            if (isBlocked)
            {
                return ApiResponse<MessageDto>.CreateFailure($"Message blocked due to policy violation: {moderationResult.ModerationReason}");
            }

            // Load complete message for response (only for non-blocked messages)
            var createdMessage = await GetMessageWithIncludesAsync(message.Id);
            var messageDto = _mapper.Map<MessageDto>(createdMessage);

            // Send email notification only for non-blocked messages
            await SendMessageNotificationAsync(messageDto, recipientSettings);

            return ApiResponse<MessageDto>.CreateSuccess(messageDto, "Message sent successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<MessageDto>.CreateFailure($"Error sending message: {ex.Message}");
        }
    }

    public async Task<ApiResponse<MessageDto>> ReplyToMessageAsync(ReplyToMessageCommand command)
    {
        try
        {
            var originalMessage = await _context.Messages
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.Id == command.MessageId);

            if (originalMessage == null)
            {
                return ApiResponse<MessageDto>.CreateFailure("Original message not found");
            }

            // Determine recipient (sender of original message if current user is recipient, or vice versa)
            var recipientId = originalMessage.SenderId == command.SenderId 
                ? originalMessage.RecipientId 
                : originalMessage.SenderId;

            var sendCommand = new SendMessageCommand(
                command.SenderId,
                recipientId,
                $"Re: {originalMessage.Subject}",
                command.Content,
                originalMessage.ConversationId,
                originalMessage.RentalId,
                originalMessage.ToolId,
                MessagePriority.Normal,
                originalMessage.Type,
                command.Attachments?.ToList()
            );

            return await SendMessageAsync(sendCommand);
        }
        catch (Exception ex)
        {
            return ApiResponse<MessageDto>.CreateFailure($"Error replying to message: {ex.Message}");
        }
    }

    public async Task<ApiResponse<MessageDto>> GetMessageByIdAsync(GetMessageByIdQuery query)
    {
        try
        {
            var message = await GetMessageWithIncludesAsync(query.MessageId);

            if (message == null)
            {
                return ApiResponse<MessageDto>.CreateFailure("Message not found");
            }

            // Check if user has access to this message (sender or recipient)
            if (message.SenderId != query.UserId && message.RecipientId != query.UserId)
            {
                return ApiResponse<MessageDto>.CreateFailure("Access denied");
            }

            // Block access to blocked messages for regular users
            if (message.IsBlocked)
            {
                return ApiResponse<MessageDto>.CreateFailure("Message not found");
            }

            var messageDto = _mapper.Map<MessageDto>(message);
            return ApiResponse<MessageDto>.CreateSuccess(messageDto, "Message retrieved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<MessageDto>.CreateFailure($"Error retrieving message: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<MessageSummaryDto>>> GetMessagesAsync(GetMessagesQuery query)
    {
        try
        {
            var messagesQuery = _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Recipient)
                .Include(m => m.Attachments)
                .Where(m => (m.SenderId == query.UserId || m.RecipientId == query.UserId) && !m.IsBlocked);

            // Apply filters
            if (query.IsRead.HasValue)
            {
                if (query.IsRead.Value)
                {
                    messagesQuery = messagesQuery.Where(m => m.IsRead || m.SenderId == query.UserId);
                }
                else
                {
                    messagesQuery = messagesQuery.Where(m => !m.IsRead && m.RecipientId == query.UserId);
                }
            }

            if (query.IsArchived.HasValue)
            {
                messagesQuery = messagesQuery.Where(m => m.IsArchived == query.IsArchived.Value);
            }

            if (query.Type.HasValue)
            {
                messagesQuery = messagesQuery.Where(m => m.Type == query.Type.Value);
            }

            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                messagesQuery = messagesQuery.Where(m => 
                    m.Subject.Contains(query.SearchTerm) || 
                    m.Content.Contains(query.SearchTerm));
            }

            if (query.FromDate.HasValue)
            {
                messagesQuery = messagesQuery.Where(m => m.CreatedAt >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                messagesQuery = messagesQuery.Where(m => m.CreatedAt <= query.ToDate.Value);
            }

            // Apply pagination and sorting
            messagesQuery = messagesQuery
                .OrderByDescending(m => m.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize);

            var messages = await messagesQuery.ToListAsync();
            var messageDtos = _mapper.Map<List<MessageSummaryDto>>(messages);

            return ApiResponse<List<MessageSummaryDto>>.CreateSuccess(messageDtos, "Messages retrieved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<List<MessageSummaryDto>>.CreateFailure($"Error retrieving messages: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<MessageSummaryDto>>> SearchMessagesAsync(SearchMessagesQuery query)
    {
        try
        {
            var messagesQuery = _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Recipient)
                .Where(m => (m.SenderId == query.UserId || m.RecipientId == query.UserId) && !m.IsBlocked)
                .Where(m => m.Subject.Contains(query.SearchTerm) || 
                           m.Content.Contains(query.SearchTerm));

            if (query.Type.HasValue)
            {
                messagesQuery = messagesQuery.Where(m => m.Type == query.Type.Value);
            }

            if (query.IsRead.HasValue)
            {
                if (query.IsRead.Value)
                {
                    messagesQuery = messagesQuery.Where(m => m.IsRead || m.SenderId == query.UserId);
                }
                else
                {
                    messagesQuery = messagesQuery.Where(m => !m.IsRead && m.RecipientId == query.UserId);
                }
            }

            messagesQuery = messagesQuery
                .OrderByDescending(m => m.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize);

            var messages = await messagesQuery.ToListAsync();
            var messageDtos = _mapper.Map<List<MessageSummaryDto>>(messages);

            return ApiResponse<List<MessageSummaryDto>>.CreateSuccess(messageDtos, "Search completed successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<List<MessageSummaryDto>>.CreateFailure($"Error searching messages: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> MarkMessageAsReadAsync(MarkMessageAsReadCommand command)
    {
        try
        {
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == command.MessageId && m.RecipientId == command.UserId);

            if (message == null)
            {
                return ApiResponse<bool>.CreateFailure("Message not found or access denied");
            }

            if (!message.IsRead)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                message.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return ApiResponse<bool>.CreateSuccess(true, "Message marked as read");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.CreateFailure($"Error marking message as read: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> ArchiveMessageAsync(ArchiveMessageCommand command)
    {
        try
        {
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == command.MessageId && 
                    (m.SenderId == command.UserId || m.RecipientId == command.UserId));

            if (message == null)
            {
                return ApiResponse<bool>.CreateFailure("Message not found or access denied");
            }

            message.IsArchived = true;
            message.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.CreateSuccess(true, "Message archived successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.CreateFailure($"Error archiving message: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> DeleteMessageAsync(DeleteMessageCommand command)
    {
        try
        {
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == command.MessageId && m.SenderId == command.UserId);

            if (message == null)
            {
                return ApiResponse<bool>.CreateFailure("Message not found or you can only delete your own messages");
            }

            message.IsDeleted = true;
            message.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.CreateSuccess(true, "Message deleted successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.CreateFailure($"Error deleting message: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> ModerateMessageAsync(ModerateMessageCommand command)
    {
        try
        {
            var message = await _context.Messages.FindAsync(command.MessageId);
            if (message == null)
            {
                return ApiResponse<bool>.CreateFailure("Message not found");
            }

            message.IsModerated = true;
            message.ModerationReason = command.Reason;
            message.ModeratedAt = DateTime.UtcNow;
            message.ModeratedBy = command.ModeratorId;
            
            if (!string.IsNullOrEmpty(command.ModifiedContent))
            {
                message.OriginalContent = message.Content;
                message.Content = command.ModifiedContent;
            }

            message.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Send mobile notification to sender about moderation
            var senderSettings = await _settingsService.GetUserSettingsAsync(message.SenderId);
            if (senderSettings?.Notifications?.PushMessages == true)
            {
                bool isBlocked = !string.IsNullOrEmpty(command.ModifiedContent);
                await _mobileNotificationService.SendMessageModerationNotificationAsync(
                    message.SenderId,
                    command.MessageId,
                    command.Reason,
                    isBlocked
                );
            }

            return ApiResponse<bool>.CreateSuccess(true, "Message moderated successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.CreateFailure($"Error moderating message: {ex.Message}");
        }
    }

    // Additional methods continued in next part due to length...
    
    private async Task<Message?> GetMessageWithIncludesAsync(Guid messageId)
    {
        return await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Recipient)
            .Include(m => m.Conversation)
            .Include(m => m.Rental)
                .ThenInclude(r => r.Tool)
            .Include(m => m.Tool)
            .Include(m => m.Attachments)
            .FirstOrDefaultAsync(m => m.Id == messageId);
    }

    private async Task<Conversation> GetOrCreateConversationAsync(string participant1Id, string participant2Id, Guid? existingConversationId, Guid? rentalId, Guid? toolId)
    {
        if (existingConversationId.HasValue)
        {
            var existingConversation = await _context.Conversations.FindAsync(existingConversationId.Value);
            if (existingConversation != null)
            {
                return existingConversation;
            }
        }

        // Try to find existing conversation between these users
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => 
                (c.Participant1Id == participant1Id && c.Participant2Id == participant2Id) ||
                (c.Participant1Id == participant2Id && c.Participant2Id == participant1Id));

        if (conversation == null)
        {
            conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                Participant1Id = participant1Id,
                Participant2Id = participant2Id,
                RentalId = rentalId,
                ToolId = toolId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Conversations.Add(conversation);
        }

        return conversation;
    }

    private async Task SendMessageNotificationAsync(MessageDto message, object? recipientSettings)
    {
        try
        {
            // Create email notification
            var emailNotification = new NewMessageNotification
            {
                RecipientEmail = message.RecipientEmail,
                RecipientName = message.RecipientName,
                UserId = message.RecipientId,
                SenderName = message.SenderName,
                SenderEmail = message.SenderEmail,
                MessageSubject = message.Subject,
                MessagePreview = message.Content.Length > 150 ? message.Content.Substring(0, 150) + "..." : message.Content,
                MessageUrl = $"/messages/{message.Id}",
                ConversationUrl = message.ConversationId.HasValue ? $"/conversations/{message.ConversationId}" : $"/messages/{message.Id}",
                RentalToolName = message.RentalToolName,
                ToolName = message.ToolName,
                HasAttachments = message.Attachments?.Any() == true,
                AttachmentCount = message.Attachments?.Count ?? 0,
                Priority = EmailPriority.Normal
            };

            await _emailNotificationService.SendNotificationAsync(emailNotification);

            // Also send mobile push notification if enabled
            var recipientUserSettings = await _settingsService.GetUserSettingsAsync(message.RecipientId);
            if (recipientUserSettings?.Notifications?.PushMessages == true)
            {
                await _mobileNotificationService.SendNewMessageNotificationAsync(
                    message.RecipientId,
                    Guid.Parse(message.Id),
                    message.SenderName,
                    message.Subject
                );
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send message notification: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ConversationDto>> CreateConversationAsync(CreateConversationCommand command)
    {
        try
        {
            var participant1 = await _context.Users.FindAsync(command.Participant1Id);
            var participant2 = await _context.Users.FindAsync(command.Participant2Id);

            if (participant1 == null || participant2 == null)
            {
                return ApiResponse<ConversationDto>.CreateFailure("One or both participants not found");
            }

            // Check if conversation already exists
            var existingConversation = await _context.Conversations
                .FirstOrDefaultAsync(c => 
                    (c.Participant1Id == command.Participant1Id && c.Participant2Id == command.Participant2Id) ||
                    (c.Participant1Id == command.Participant2Id && c.Participant2Id == command.Participant1Id));

            if (existingConversation != null)
            {
                var existingDto = _mapper.Map<ConversationDto>(existingConversation);
                return ApiResponse<ConversationDto>.CreateSuccess(existingDto, "Conversation already exists");
            }

            var conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                Participant1Id = command.Participant1Id,
                Participant2Id = command.Participant2Id,
                Title = command.Title,
                RentalId = command.RentalId,
                ToolId = command.ToolId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            var conversationDto = _mapper.Map<ConversationDto>(conversation);
            return ApiResponse<ConversationDto>.CreateSuccess(conversationDto, "Conversation created successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<ConversationDto>.CreateFailure($"Error creating conversation: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<ConversationDto>>> GetConversationsAsync(GetConversationsQuery query)
    {
        try
        {
            var conversationsQuery = _context.Conversations
                .Include(c => c.Participant1)
                .Include(c => c.Participant2)
                .Include(c => c.LastMessage)
                .Where(c => c.Participant1Id == query.UserId || c.Participant2Id == query.UserId);

            if (query.IsArchived.HasValue)
            {
                conversationsQuery = conversationsQuery.Where(c => c.IsArchived == query.IsArchived.Value);
            }

            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                conversationsQuery = conversationsQuery.Where(c => 
                    c.Title!.Contains(query.SearchTerm) ||
                    c.Participant1.FirstName.Contains(query.SearchTerm) ||
                    c.Participant1.LastName.Contains(query.SearchTerm) ||
                    c.Participant2.FirstName.Contains(query.SearchTerm) ||
                    c.Participant2.LastName.Contains(query.SearchTerm));
            }

            conversationsQuery = conversationsQuery
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize);

            var conversations = await conversationsQuery.ToListAsync();
            var conversationDtos = _mapper.Map<List<ConversationDto>>(conversations);

            return ApiResponse<List<ConversationDto>>.CreateSuccess(conversationDtos, "Conversations retrieved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<List<ConversationDto>>.CreateFailure($"Error retrieving conversations: {ex.Message}");
        }
    }

    public async Task<ApiResponse<int>> GetUnreadMessageCountAsync(GetUnreadMessageCountQuery query)
    {
        try
        {
            var count = await _context.Messages
                .CountAsync(m => m.RecipientId == query.UserId && !m.IsRead && !m.IsDeleted && !m.IsBlocked);

            return ApiResponse<int>.CreateSuccess(count, "Unread message count retrieved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<int>.CreateFailure($"Error getting unread message count: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ConversationDetailsDto>> GetConversationByIdAsync(GetConversationByIdQuery query)
    {
        try
        {
            var conversation = await _context.Conversations
                .Include(c => c.Participant1)
                .Include(c => c.Participant2)
                .Include(c => c.Messages.Where(m => !m.IsBlocked).OrderByDescending(m => m.CreatedAt).Take(query.PageSize))
                    .ThenInclude(m => m.Sender)
                .Include(c => c.Messages.Where(m => !m.IsBlocked).OrderByDescending(m => m.CreatedAt).Take(query.PageSize))
                    .ThenInclude(m => m.Attachments)
                .FirstOrDefaultAsync(c => c.Id == query.ConversationId && 
                    (c.Participant1Id == query.UserId || c.Participant2Id == query.UserId));

            if (conversation == null)
            {
                return ApiResponse<ConversationDetailsDto>.CreateFailure("Conversation not found or access denied");
            }

            var conversationDto = _mapper.Map<ConversationDetailsDto>(conversation);
            return ApiResponse<ConversationDetailsDto>.CreateSuccess(conversationDto, "Conversation retrieved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<ConversationDetailsDto>.CreateFailure($"Error retrieving conversation: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ConversationDto>> GetConversationBetweenUsersAsync(GetConversationBetweenUsersQuery query)
    {
        try
        {
            var conversation = await _context.Conversations
                .Include(c => c.Participant1)
                .Include(c => c.Participant2)
                .Include(c => c.LastMessage)
                .FirstOrDefaultAsync(c => 
                    (c.Participant1Id == query.User1Id && c.Participant2Id == query.User2Id) ||
                    (c.Participant1Id == query.User2Id && c.Participant2Id == query.User1Id));

            if (conversation == null)
            {
                return ApiResponse<ConversationDto>.CreateFailure("Conversation not found");
            }

            var conversationDto = _mapper.Map<ConversationDto>(conversation);
            return ApiResponse<ConversationDto>.CreateSuccess(conversationDto, "Conversation retrieved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<ConversationDto>.CreateFailure($"Error retrieving conversation: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> MarkConversationAsReadAsync(MarkConversationAsReadCommand command)
    {
        try
        {
            var unreadMessages = await _context.Messages
                .Where(m => m.ConversationId == command.ConversationId && 
                           m.RecipientId == command.UserId && 
                           !m.IsRead)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                message.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return ApiResponse<bool>.CreateSuccess(true, $"Marked {unreadMessages.Count} messages as read");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.CreateFailure($"Error marking conversation as read: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ConversationDto>> UpdateConversationAsync(UpdateConversationCommand command)
    {
        try
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == command.ConversationId && 
                    (c.Participant1Id == command.UserId || c.Participant2Id == command.UserId));

            if (conversation == null)
            {
                return ApiResponse<ConversationDto>.CreateFailure("Conversation not found or access denied");
            }

            if (!string.IsNullOrEmpty(command.Title))
            {
                conversation.Title = command.Title;
            }

            if (command.IsArchived.HasValue)
            {
                conversation.IsArchived = command.IsArchived.Value;
            }

            conversation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var conversationDto = _mapper.Map<ConversationDto>(conversation);
            return ApiResponse<ConversationDto>.CreateSuccess(conversationDto, "Conversation updated successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<ConversationDto>.CreateFailure($"Error updating conversation: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> ArchiveConversationAsync(ArchiveConversationCommand command)
    {
        var updateCommand = new UpdateConversationCommand(command.UserId, command.ConversationId, null, true);
        var result = await UpdateConversationAsync(updateCommand);
        return new ApiResponse<bool> { Success = result.Success, Message = result.Message, Data = result.Success };
    }

    public async Task<ApiResponse<MessageStatisticsDto>> GetMessageStatisticsAsync(GetMessageStatisticsQuery query)
    {
        try
        {
            var messagesQuery = _context.Messages
                .Where(m => m.SenderId == query.UserId || m.RecipientId == query.UserId);

            if (query.FromDate.HasValue)
            {
                messagesQuery = messagesQuery.Where(m => m.CreatedAt >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                messagesQuery = messagesQuery.Where(m => m.CreatedAt <= query.ToDate.Value);
            }

            var statistics = new MessageStatisticsDto
            {
                TotalMessages = await messagesQuery.CountAsync(),
                UnreadMessages = await messagesQuery.CountAsync(m => m.RecipientId == query.UserId && !m.IsRead),
                SentMessages = await messagesQuery.CountAsync(m => m.SenderId == query.UserId),
                ReceivedMessages = await messagesQuery.CountAsync(m => m.RecipientId == query.UserId),
                ArchivedMessages = await messagesQuery.CountAsync(m => m.IsArchived),
                ModeratedMessages = await messagesQuery.CountAsync(m => m.IsModerated),
                ConversationCount = await _context.Conversations.CountAsync(c => 
                    c.Participant1Id == query.UserId || c.Participant2Id == query.UserId),
                LastMessageAt = await messagesQuery
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => (DateTime?)m.CreatedAt)
                    .FirstOrDefaultAsync()
            };

            return ApiResponse<MessageStatisticsDto>.CreateSuccess(statistics, "Statistics retrieved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<MessageStatisticsDto>.CreateFailure($"Error retrieving statistics: {ex.Message}");
        }
    }

    public async Task<ApiResponse<byte[]>> GetMessageAttachmentAsync(GetMessageAttachmentQuery query)
    {
        try
        {
            var attachment = await _context.MessageAttachments
                .Include(a => a.Message)
                .FirstOrDefaultAsync(a => a.Id == query.AttachmentId && 
                    (a.Message.SenderId == query.UserId || a.Message.RecipientId == query.UserId));

            if (attachment == null)
            {
                return ApiResponse<byte[]>.CreateFailure("Attachment not found or access denied");
            }

            if (!attachment.IsSafe)
            {
                return ApiResponse<byte[]>.CreateFailure("Attachment is not safe to download");
            }

            var fileStream = await _fileStorageService.DownloadFileAsync(attachment.StoragePath);
            if (fileStream == null)
            {
                return ApiResponse<byte[]>.CreateFailure("File not found");
            }
            
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            var fileData = memoryStream.ToArray();
            return ApiResponse<byte[]>.CreateSuccess(fileData, "Attachment retrieved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<byte[]>.CreateFailure($"Error retrieving attachment: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<MessageSummaryDto>>> GetModeratedMessagesAsync(GetModeratedMessagesQuery query)
    {
        try
        {
            var messagesQuery = _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Recipient)
                .Where(m => m.IsModerated);

            if (query.FromDate.HasValue)
            {
                messagesQuery = messagesQuery.Where(m => m.ModeratedAt >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                messagesQuery = messagesQuery.Where(m => m.ModeratedAt <= query.ToDate.Value);
            }

            messagesQuery = messagesQuery
                .OrderByDescending(m => m.ModeratedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize);

            var messages = await messagesQuery.ToListAsync();
            var messageDtos = _mapper.Map<List<MessageSummaryDto>>(messages);

            return ApiResponse<List<MessageSummaryDto>>.CreateSuccess(messageDtos, "Moderated messages retrieved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<List<MessageSummaryDto>>.CreateFailure($"Error retrieving moderated messages: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> ValidateMessageContentAsync(string content, string senderId)
    {
        try
        {
            var result = await _contentModerationService.ValidateContentAsync(content, senderId);
            return ApiResponse<bool>.CreateSuccess(result.IsApproved, result.ModerationReason ?? "Content validated");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.CreateFailure($"Error validating content: {ex.Message}");
        }
    }

    public async Task<ApiResponse<MessageDto>> GetMessageByIdForAdminAsync(Guid messageId)
    {
        try
        {
            var message = await GetMessageWithIncludesAsync(messageId);

            if (message == null)
            {
                return ApiResponse<MessageDto>.CreateFailure("Message not found");
            }

            // Admin can access any message, including blocked ones
            var messageDto = _mapper.Map<MessageDto>(message);
            return ApiResponse<MessageDto>.CreateSuccess(messageDto, "Message retrieved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<MessageDto>.CreateFailure($"Error retrieving message: {ex.Message}");
        }
    }
}