using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToolsSharing.Core.DTOs.Dispute;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Interfaces;
using CreateDisputeRequest = ToolsSharing.Core.Interfaces.CreateDisputeRequest;
using CreateDisputeResult = ToolsSharing.Core.Interfaces.CreateDisputeResult;
using DisputeDetailsResult = ToolsSharing.Core.Interfaces.DisputeDetailsResult;
using AddMessageResult = ToolsSharing.Core.Interfaces.AddMessageResult;
using ResolveDisputeRequest = ToolsSharing.Core.Interfaces.ResolveDisputeRequest;
using ResolveDisputeResult = ToolsSharing.Core.Interfaces.ResolveDisputeResult;
using EscalateDisputeResult = ToolsSharing.Core.Interfaces.EscalateDisputeResult;
using SyncPayPalDisputeResult = ToolsSharing.Core.Interfaces.SyncPayPalDisputeResult;
using PayPalDisputeWebhook = ToolsSharing.Core.Interfaces.PayPalDisputeWebhook;
using AssignDisputeResult = ToolsSharing.Core.Interfaces.AssignDisputeResult;
using UploadEvidenceResult = ToolsSharing.Core.Interfaces.UploadEvidenceResult;
using EvidenceFile = ToolsSharing.Core.Interfaces.EvidenceFile;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models.EmailNotifications;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services;

public class DisputeService : IDisputeService
{
    private readonly ApplicationDbContext _context;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IEmailNotificationService _emailService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IDisputeNotificationService _notificationService;
    private readonly ILogger<DisputeService> _logger;

    public DisputeService(
        ApplicationDbContext context,
        IPaymentProvider paymentProvider,
        IEmailNotificationService emailService,
        IFileStorageService fileStorageService,
        IDisputeNotificationService notificationService,
        ILogger<DisputeService> logger)
    {
        _context = context;
        _paymentProvider = paymentProvider;
        _emailService = emailService;
        _fileStorageService = fileStorageService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<CreateDisputeResult> CreateDisputeAsync(CreateDisputeRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var rental = await _context.Rentals
                .Include(r => r.Tool)
                .Include(r => r.Owner)
                .Include(r => r.Renter)
                .FirstOrDefaultAsync(r => r.Id == request.RentalId);

            if (rental == null)
            {
                return new CreateDisputeResult
                {
                    Success = false,
                    ErrorMessage = "Rental not found"
                };
            }

            // Verify user can create dispute
            if (request.InitiatedBy != rental.OwnerId && request.InitiatedBy != rental.RenterId)
            {
                return new CreateDisputeResult
                {
                    Success = false,
                    ErrorMessage = "You are not authorized to create a dispute for this rental"
                };
            }

            // Check if dispute already exists
            var existingDispute = await _context.Disputes
                .FirstOrDefaultAsync(d => d.RentalId == request.RentalId && 
                                         d.Status != DisputeStatus.Closed);

            if (existingDispute != null)
            {
                return new CreateDisputeResult
                {
                    Success = false,
                    ErrorMessage = "A dispute already exists for this rental"
                };
            }

            var dispute = new Dispute
            {
                RentalId = request.RentalId,
                PaymentId = request.PaymentId,
                InitiatedBy = request.InitiatedBy,
                Type = request.Type,
                Category = request.Category,
                Status = DisputeStatus.Open,
                Title = request.Title,
                Description = request.Description,
                DisputeAmount = request.DisputeAmount,
                Evidence = request.Evidence?.Any() == true ? JsonSerializer.Serialize(request.Evidence) : null,
                ResponseDueDate = DateTime.UtcNow.AddDays(7), // 7 days to respond
                LastActionAt = DateTime.UtcNow
            };

            _context.Disputes.Add(dispute);
            await _context.SaveChangesAsync();

            // Create initial system message
            var initialMessage = new DisputeMessage
            {
                DisputeId = dispute.Id,
                FromUserId = "SYSTEM",
                Message = $"Dispute created: {dispute.Title}",
                IsSystemGenerated = true,
                IsFromAdmin = false,
                IsInternal = false
            };

            _context.DisputeMessages.Add(initialMessage);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Dispute {DisputeId} created for rental {RentalId} by user {UserId}", 
                dispute.Id, request.RentalId, request.InitiatedBy);

            // Send notifications
            await SendDisputeCreatedNotificationsAsync(dispute, rental);

            var disputeDto = await MapToDisputeDetailsAsync(dispute);
            
            return new CreateDisputeResult
            {
                Success = true,
                DisputeId = dispute.Id,
                Dispute = disputeDto
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating dispute for rental {RentalId}", request.RentalId);
            
            return new CreateDisputeResult
            {
                Success = false,
                ErrorMessage = "An error occurred while creating the dispute"
            };
        }
    }

    public async Task<DisputeDetailsResult> GetDisputeAsync(Guid disputeId, string userId)
    {
        var dispute = await _context.Disputes
            .Include(d => d.Rental)
                .ThenInclude(r => r.Tool)
            .Include(d => d.Rental)
                .ThenInclude(r => r.Owner)
            .Include(d => d.Rental)
                .ThenInclude(r => r.Renter)
            .Include(d => d.Messages.OrderByDescending(m => m.CreatedAt).Take(10))
                .ThenInclude(m => m.FromUser)
            .FirstOrDefaultAsync(d => d.Id == disputeId);

        if (dispute == null)
        {
            return new DisputeDetailsResult
            {
                Success = false,
                ErrorMessage = "Dispute not found"
            };
        }

        // Verify user access
        if (dispute.InitiatedBy != userId && 
            dispute.Rental!.OwnerId != userId && 
            dispute.Rental.RenterId != userId)
        {
            return new DisputeDetailsResult
            {
                Success = false,
                ErrorMessage = "Access denied"
            };
        }

        var disputeDto = await MapToDisputeDetailsAsync(dispute);
        
        return new DisputeDetailsResult
        {
            Success = true,
            Dispute = disputeDto
        };
    }

    public async Task<List<DisputeSummaryDto>> GetUserDisputesAsync(string userId)
    {
        var disputes = await _context.Disputes
            .Include(d => d.Rental)
                .ThenInclude(r => r.Tool)
            .Include(d => d.Initiator)
            .Include(d => d.Messages)
            .Where(d => d.InitiatedBy == userId || 
                       d.Rental!.OwnerId == userId || 
                       d.Rental.RenterId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .Take(50)
            .ToListAsync();

        return disputes.Select(d => MapToDisputeSummary(d, userId)).ToList();
    }

    public async Task<List<DisputeSummaryDto>> GetRentalDisputesAsync(Guid rentalId)
    {
        var disputes = await _context.Disputes
            .Include(d => d.Rental)
                .ThenInclude(r => r.Tool)
            .Include(d => d.Initiator)
            .Include(d => d.Messages)
            .Where(d => d.RentalId == rentalId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return disputes.Select(d => MapToDisputeSummary(d, "")).ToList();
    }

    public async Task<GetDisputesResult> GetDisputesAsync(GetDisputesRequest request)
    {
        try
        {
            var query = _context.Disputes
                .Include(d => d.Rental)
                    .ThenInclude(r => r.Tool)
                .Include(d => d.Messages)
                .AsQueryable();

            // Apply user filter
            if (!string.IsNullOrEmpty(request.UserId))
            {
                query = query.Where(d => d.InitiatedBy == request.UserId || 
                                        d.Rental.RenterId == request.UserId ||
                                        d.Rental.Tool.OwnerId == request.UserId);
            }

            // Apply filters
            if (request.Status.HasValue)
                query = query.Where(d => d.Status == request.Status);

            if (request.Type.HasValue)
                query = query.Where(d => d.Type == request.Type);

            if (request.StartDate.HasValue)
                query = query.Where(d => d.CreatedAt >= request.StartDate);

            if (request.EndDate.HasValue)
                query = query.Where(d => d.CreatedAt <= request.EndDate);

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "createdat" => request.SortDescending ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt),
                "status" => request.SortDescending ? query.OrderByDescending(d => d.Status) : query.OrderBy(d => d.Status),
                "type" => request.SortDescending ? query.OrderByDescending(d => d.Type) : query.OrderBy(d => d.Type),
                _ => query.OrderByDescending(d => d.CreatedAt)
            };

            // Apply pagination
            var disputes = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            // Convert to DTOs
            var disputeDtos = disputes.Select(d => new DisputeDto
            {
                Id = d.Id.ToString(),
                RentalId = d.RentalId,
                Subject = d.Title, // Use Title for Subject
                Description = d.Description,
                Type = d.Type,
                Status = d.Status,
                Category = d.Category,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                DisputedAmount = d.DisputeAmount, // Use DisputeAmount for DisputedAmount
                InitiatedBy = d.InitiatedBy,
                InitiatedByName = d.InitiatedByName,
                ExternalDisputeId = d.ExternalDisputeId,
                Resolution = d.Resolution,
                ResolvedAt = d.ResolvedAt,
                ResolutionNotes = d.ResolutionNotes,
                Rental = d.Rental != null ? new RentalSummaryDto
                {
                    Id = d.Rental.Id,
                    ToolName = d.Rental.Tool?.Name ?? "Unknown Tool",
                    StartDate = d.Rental.StartDate,
                    EndDate = d.Rental.EndDate,
                    TotalCost = d.Rental.TotalCost
                } : null
            }).ToList();

            return GetDisputesResult.CreateSuccess(disputeDtos, totalCount, request.PageNumber, request.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving disputes for user {UserId}", request.UserId);
            return GetDisputesResult.CreateFailure($"Failed to retrieve disputes: {ex.Message}");
        }
    }

    public async Task<AddMessageResult> AddDisputeMessageAsync(AddDisputeMessageRequest request)
    {
        try
        {
            var disputeId = Guid.Parse(request.DisputeId);
            var dispute = await _context.Disputes
                .Include(d => d.Rental)
                .FirstOrDefaultAsync(d => d.Id == disputeId);

            if (dispute == null)
            {
                return new AddMessageResult
                {
                    Success = false,
                    ErrorMessage = "Dispute not found"
                };
            }

            // Create new message
            var message = new DisputeMessage
            {
                Id = Guid.NewGuid(),
                DisputeId = disputeId,
                FromUserId = request.SenderId,
                SenderId = request.SenderId,
                SenderName = request.SenderName,
                SenderRole = request.SenderRole,
                Message = request.Message,
                CreatedAt = DateTime.UtcNow,
                IsInternal = request.IsInternal,
                IsFromAdmin = request.SenderRole == "Admin",
                IsSystemGenerated = false,
                Attachments = string.Join(";", request.Attachments)
            };

            _context.DisputeMessages.Add(message);
            await _context.SaveChangesAsync();

            // Create message DTO
            var messageDto = new DisputeMessageDto
            {
                Id = message.Id.ToString(),
                DisputeId = message.DisputeId.ToString(),
                SenderId = message.SenderId,
                SenderName = message.SenderName,
                SenderRole = message.SenderRole,
                Message = message.Message,
                CreatedAt = message.CreatedAt,
                IsInternal = message.IsInternal,
                IsRead = false,
                Attachments = string.IsNullOrEmpty(message.Attachments) ? new List<string>() : message.Attachments.Split(';').ToList()
            };

            return new AddMessageResult
            {
                Success = true,
                Message = messageDto,
                ErrorMessage = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding message to dispute {DisputeId}", request.DisputeId);
            return new AddMessageResult
            {
                Success = false,
                ErrorMessage = $"Failed to add message: {ex.Message}"
            };
        }
    }

    public async Task<GetMessagesResult> GetDisputeMessagesAsync(Guid disputeId, string userId)
    {
        try
        {
            // Verify user has access to this dispute
            var dispute = await _context.Disputes
                .Include(d => d.Rental)
                    .ThenInclude(r => r.Tool)
                .FirstOrDefaultAsync(d => d.Id == disputeId);

            if (dispute == null)
            {
                return GetMessagesResult.CreateFailure("Dispute not found");
            }

            // Check access permissions
            bool hasAccess = dispute.InitiatedBy == userId ||
                           dispute.Rental.RenterId == userId ||
                           dispute.Rental.Tool.OwnerId == userId;

            if (!hasAccess)
            {
                return GetMessagesResult.CreateFailure("Access denied");
            }

            var messages = await _context.DisputeMessages
                .Where(m => m.DisputeId == disputeId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var messageDtos = messages.Select(m => new DisputeMessageDto
            {
                Id = m.Id.ToString(),
                DisputeId = m.DisputeId.ToString(),
                SenderId = m.SenderId,
                SenderName = m.SenderName,
                SenderRole = m.SenderRole,
                Message = m.Message,
                CreatedAt = m.CreatedAt,
                IsInternal = m.IsInternal,
                IsRead = m.IsRead,
                Attachments = string.IsNullOrEmpty(m.Attachments) ? new List<string>() : m.Attachments.Split(';').ToList()
            }).ToList();

            return GetMessagesResult.CreateSuccess(messageDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages for dispute {DisputeId}", disputeId);
            return GetMessagesResult.CreateFailure($"Failed to retrieve messages: {ex.Message}");
        }
    }

    public async Task<UpdateStatusResult> UpdateDisputeStatusAsync(UpdateDisputeStatusRequest request)
    {
        try
        {
            var dispute = await _context.Disputes
                .Include(d => d.Rental)
                    .ThenInclude(r => r.Tool)
                .FirstOrDefaultAsync(d => d.Id == request.DisputeId);

            if (dispute == null)
            {
                return UpdateStatusResult.CreateFailure("Dispute not found");
            }

            var oldStatus = dispute.Status;
            dispute.Status = request.Status;
            dispute.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(request.Notes))
            {
                dispute.ResolutionNotes = request.Notes;
            }

            if (request.Status == DisputeStatus.Resolved)
            {
                dispute.ResolvedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Send notification for status change
            await _notificationService.SendStatusChangeNotificationAsync(dispute, oldStatus, request.Status, request.Notes);

            var disputeDto = new DisputeDto
            {
                Id = dispute.Id.ToString(),
                Status = dispute.Status,
                UpdatedAt = dispute.UpdatedAt,
                ResolvedAt = dispute.ResolvedAt,
                ResolutionNotes = dispute.ResolutionNotes
            };

            return UpdateStatusResult.CreateSuccess(disputeDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dispute status for {DisputeId}", request.DisputeId);
            return UpdateStatusResult.CreateFailure($"Failed to update status: {ex.Message}");
        }
    }

    public async Task<CloseDisputeResult> CloseDisputeAsync(CloseDisputeRequest request)
    {
        try
        {
            var dispute = await _context.Disputes
                .Include(d => d.Rental)
                    .ThenInclude(r => r.Tool)
                .FirstOrDefaultAsync(d => d.Id == request.DisputeId);

            if (dispute == null)
            {
                return CloseDisputeResult.CreateFailure("Dispute not found");
            }

            dispute.Status = DisputeStatus.Closed;
            dispute.UpdatedAt = DateTime.UtcNow;
            dispute.ResolutionNotes = request.Reason;

            await _context.SaveChangesAsync();

            var disputeDto = new DisputeDto
            {
                Id = dispute.Id.ToString(),
                Status = dispute.Status,
                UpdatedAt = dispute.UpdatedAt,
                ResolutionNotes = dispute.ResolutionNotes
            };

            return CloseDisputeResult.CreateSuccess(disputeDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing dispute {DisputeId}", request.DisputeId);
            return CloseDisputeResult.CreateFailure($"Failed to close dispute: {ex.Message}");
        }
    }

    public async Task<GetTimelineResult> GetDisputeTimelineAsync(Guid disputeId, string userId)
    {
        try
        {
            // Verify user has access to this dispute
            var dispute = await _context.Disputes
                .Include(d => d.Rental)
                    .ThenInclude(r => r.Tool)
                .Include(d => d.Messages)
                .FirstOrDefaultAsync(d => d.Id == disputeId);

            if (dispute == null)
            {
                return GetTimelineResult.CreateFailure("Dispute not found");
            }

            // Check access permissions
            bool hasAccess = dispute.InitiatedBy == userId ||
                           dispute.Rental.RenterId == userId ||
                           dispute.Rental.Tool.OwnerId == userId;

            if (!hasAccess)
            {
                return GetTimelineResult.CreateFailure("Access denied");
            }

            var timelineEvents = new List<DisputeTimelineEventDto>();

            // Add dispute creation event
            timelineEvents.Add(new DisputeTimelineEventDto
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Dispute Created",
                Description = $"Dispute opened regarding: {dispute.Title}",
                Timestamp = dispute.CreatedAt,
                EventType = "Created",
                ActorName = dispute.InitiatedByName,
                ActorId = dispute.InitiatedBy
            });

            // Add message events
            foreach (var message in dispute.Messages.OrderBy(m => m.CreatedAt))
            {
                timelineEvents.Add(new DisputeTimelineEventDto
                {
                    Id = message.Id.ToString(),
                    Title = $"Message from {message.SenderName}",
                    Description = message.Message.Length > 100 ? 
                        message.Message.Substring(0, 100) + "..." : 
                        message.Message,
                    Details = message.Message,
                    Timestamp = message.CreatedAt,
                    EventType = "MessageAdded",
                    ActorName = message.SenderName,
                    ActorId = message.SenderId,
                    ActorRole = message.SenderRole,
                    Attachments = string.IsNullOrEmpty(message.Attachments) ? 
                        new List<string>() : 
                        message.Attachments.Split(';').ToList()
                });
            }

            // Add status change events (simplified - in production you'd track these in a separate table)
            if (dispute.Status == DisputeStatus.Resolved && dispute.ResolvedAt.HasValue)
            {
                timelineEvents.Add(new DisputeTimelineEventDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Dispute Resolved",
                    Description = "Dispute has been resolved",
                    Details = dispute.ResolutionNotes,
                    Timestamp = dispute.ResolvedAt.Value,
                    EventType = "Resolved"
                });
            }

            return GetTimelineResult.CreateSuccess(timelineEvents.OrderBy(e => e.Timestamp).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving timeline for dispute {DisputeId}", disputeId);
            return GetTimelineResult.CreateFailure($"Failed to retrieve timeline: {ex.Message}");
        }
    }

    public async Task<GetStatsResult> GetDisputeStatsAsync()
    {
        try
        {
            var disputes = await _context.Disputes.ToListAsync();
            
            var stats = new DisputeStatsDto
            {
                TotalDisputes = disputes.Count,
                OpenDisputes = disputes.Count(d => d.Status == DisputeStatus.Open),
                InProgressDisputes = disputes.Count(d => d.Status == DisputeStatus.InProgress),
                ResolvedDisputes = disputes.Count(d => d.Status == DisputeStatus.Resolved),
                EscalatedDisputes = disputes.Count(d => d.Status == DisputeStatus.EscalatedToPayPal),
                RefundedAmount = disputes.Where(d => d.DisputedAmount.HasValue && d.Status == DisputeStatus.Resolved)
                                       .Sum(d => d.DisputedAmount.Value),
                DisputesByCategory = disputes.GroupBy(d => d.Category.ToString())
                                           .ToDictionary(g => g.Key, g => g.Count()),
                DisputesByMonth = disputes.Where(d => d.CreatedAt >= DateTime.UtcNow.AddMonths(-12))
                                        .GroupBy(d => d.CreatedAt.ToString("yyyy-MM"))
                                        .ToDictionary(g => g.Key, g => g.Count())
            };

            // Calculate average resolution time
            var resolvedDisputes = disputes.Where(d => d.ResolvedAt.HasValue).ToList();
            if (resolvedDisputes.Any())
            {
                var totalHours = resolvedDisputes.Sum(d => (d.ResolvedAt!.Value - d.CreatedAt).TotalHours);
                stats.AverageResolutionTime = (decimal)(totalHours / resolvedDisputes.Count);
            }

            return GetStatsResult.CreateSuccess(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dispute statistics");
            return GetStatsResult.CreateFailure($"Failed to retrieve statistics: {ex.Message}");
        }
    }

    // Original AddMessageResult method with old signature - need to update signature
    public async Task<AddMessageResult> AddDisputeMessageAsync(Guid disputeId, string userId, string message, List<string>? attachments = null)
    {
        // Convert to new request format
        var request = new AddDisputeMessageRequest
        {
            DisputeId = disputeId.ToString(),
            SenderId = userId,
            SenderName = "User", // This will need to be looked up from user context
            SenderRole = "User", // This will need to be determined from context
            Message = message,
            Attachments = attachments ?? new List<string>(),
            IsInternal = false
        };
        
        return await AddDisputeMessageAsync(request);
    }

    // Helper mapping methods
    private async Task<DisputeDetailsDto> MapToDisputeDetailsAsync(Dispute dispute)
    {
        return new DisputeDetailsDto
        {
            Id = dispute.Id,
            RentalId = dispute.RentalId,
            PaymentId = dispute.PaymentId,
            RentalToolName = dispute.Rental?.Tool?.Name ?? "Unknown Tool",
            RentalToolImage = dispute.Rental?.Tool?.Images?.FirstOrDefault()?.ImageUrl ?? "",
            OwnerName = dispute.Rental?.Owner?.FirstName + " " + dispute.Rental?.Owner?.LastName ?? "Unknown Owner",
            RenterName = dispute.Rental?.Renter?.FirstName + " " + dispute.Rental?.Renter?.LastName ?? "Unknown Renter",
            RentalStartDate = dispute.Rental?.StartDate ?? DateTime.MinValue,
            RentalEndDate = dispute.Rental?.EndDate ?? DateTime.MinValue,
            RentalAmount = dispute.Rental?.TotalCost ?? 0,
            InitiatorName = dispute.InitiatedByName,
            InitiatorId = dispute.InitiatedBy,
            Type = dispute.Type,
            Status = dispute.Status,
            Category = dispute.Category,
            Title = dispute.Title,
            Description = dispute.Description,
            DisputeAmount = dispute.DisputeAmount,
            CreatedAt = dispute.CreatedAt,
            EscalatedAt = dispute.EscalatedAt,
            ResolvedAt = dispute.ResolvedAt,
            ResponseDueDate = dispute.ResponseDueDate,
            LastActionAt = dispute.LastActionAt,
            ResolutionNotes = dispute.ResolutionNotes,
            ResolvedByName = dispute.ResolvedBy,
            Resolution = dispute.Resolution,
            RefundAmount = dispute.RefundAmount,
            ExternalDisputeId = dispute.ExternalDisputeId,
            ExternalCaseId = dispute.ExternalCaseId,
            PayPalReason = dispute.PayPalReason,
            Evidence = new List<EvidenceFileDto>(), // TODO: Implement evidence mapping
            RecentMessages = dispute.Messages?.Take(5).Select(MapToMessageDto).ToList() ?? new List<DisputeMessageDto>(),
            UnreadMessageCount = dispute.Messages?.Count(m => !m.IsRead) ?? 0,
            CanUserRespond = dispute.Status == DisputeStatus.Open || dispute.Status == DisputeStatus.InProgress,
            IsOverdue = dispute.ResponseDueDate.HasValue && dispute.ResponseDueDate < DateTime.UtcNow,
            RequiresAttention = false, // TODO: Implement business logic
            AvailableActions = new List<string>() // TODO: Implement based on user role and dispute status
        };
    }

    private DisputeSummaryDto MapToDisputeSummary(Dispute dispute, string userId)
    {
        return new DisputeSummaryDto
        {
            Id = dispute.Id,
            RentalId = dispute.RentalId,
            RentalToolName = dispute.Rental?.Tool?.Name ?? "Unknown Tool",
            InitiatorName = dispute.InitiatedByName,
            Type = dispute.Type,
            Status = dispute.Status,
            Category = dispute.Category,
            Title = dispute.Title,
            DisputeAmount = dispute.DisputeAmount,
            CreatedAt = dispute.CreatedAt,
            LastActionAt = dispute.LastActionAt,
            HasUnreadMessages = dispute.Messages?.Any(m => !m.IsRead && m.FromUserId != userId) ?? false,
            MessageCount = dispute.Messages?.Count ?? 0,
            ResponseDueDate = dispute.ResponseDueDate,
            IsOverdue = dispute.ResponseDueDate.HasValue && dispute.ResponseDueDate < DateTime.UtcNow
        };
    }

    private DisputeMessageDto MapToMessageDto(DisputeMessage message)
    {
        return new DisputeMessageDto
        {
            Id = message.Id.ToString(),
            DisputeId = message.DisputeId.ToString(),
            FromUserId = message.FromUserId,
            FromUserName = message.FromUser?.FirstName + " " + message.FromUser?.LastName ?? message.SenderName,
            SenderId = message.SenderId,
            SenderName = message.SenderName,
            SenderRole = message.SenderRole,
            Message = message.Message,
            Attachments = string.IsNullOrEmpty(message.Attachments) ? new List<string>() : message.Attachments.Split(';').ToList(),
            IsFromAdmin = message.IsFromAdmin,
            IsInternal = message.IsInternal,
            IsSystemGenerated = message.IsSystemGenerated,
            IsRead = message.IsRead,
            CreatedAt = message.CreatedAt,
            ReadAt = message.ReadAt
        };
    }

    private async Task SendDisputeCreatedNotificationsAsync(Dispute dispute, Rental rental)
    {
        try
        {
            var initiator = await _context.Users.FindAsync(dispute.InitiatedBy);
            User? otherParty = null;

            // Determine the other party
            if (dispute.InitiatedBy == rental.OwnerId)
            {
                otherParty = rental.Renter;
            }
            else if (dispute.InitiatedBy == rental.RenterId)
            {
                otherParty = rental.Owner;
            }

            if (initiator != null && otherParty != null)
            {
                await _notificationService.SendDisputeCreatedNotificationAsync(dispute, initiator, otherParty);
            }

            _logger.LogInformation("Dispute created notifications sent for dispute {DisputeId}", dispute.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending dispute created notifications for dispute {DisputeId}", dispute.Id);
        }
    }

    // Stub implementations for missing interface methods
    public async Task MarkMessagesAsReadAsync(Guid disputeId, string userId)
    {
        var messages = await _context.DisputeMessages
            .Where(m => m.DisputeId == disputeId && m.FromUserId != userId && !m.IsRead)
            .ToListAsync();

        foreach (var message in messages)
        {
            message.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<ResolveDisputeResult> ResolveDisputeAsync(ResolveDisputeRequest request)
    {
        try
        {
            var dispute = await _context.Disputes.FindAsync(request.DisputeId);
            if (dispute == null)
            {
                return new ResolveDisputeResult { Success = false, ErrorMessage = "Dispute not found" };
            }

            dispute.Status = DisputeStatus.Resolved;
            dispute.Resolution = request.Resolution;
            dispute.ResolvedAt = DateTime.UtcNow;
            dispute.ResolutionNotes = request.ResolutionNotes;

            await _context.SaveChangesAsync();

            return new ResolveDisputeResult { Success = true, ErrorMessage = null };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving dispute {DisputeId}", request.DisputeId);
            return new ResolveDisputeResult { Success = false, ErrorMessage = "Failed to resolve dispute" };
        }
    }

    public async Task<EscalateDisputeResult> EscalateToPayPalAsync(Guid disputeId, string adminUserId)
    {
        try
        {
            var dispute = await _context.Disputes.FindAsync(disputeId);
            if (dispute == null)
            {
                return new EscalateDisputeResult { Success = false, ErrorMessage = "Dispute not found" };
            }

            dispute.Status = DisputeStatus.EscalatedToPayPal;
            dispute.ExternalDisputeId = $"PP-D-{Guid.NewGuid()}"; // Placeholder
            dispute.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new EscalateDisputeResult { Success = true, ExternalDisputeId = dispute.ExternalDisputeId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating dispute {DisputeId}", disputeId);
            return new EscalateDisputeResult { Success = false, ErrorMessage = "Failed to escalate dispute" };
        }
    }

    public async Task<SyncPayPalDisputeResult> SyncPayPalDisputeAsync(string externalDisputeId)
    {
        // Placeholder implementation
        return new SyncPayPalDisputeResult { Success = true, ErrorMessage = null };
    }

    public async Task HandlePayPalDisputeWebhookAsync(PayPalDisputeWebhook webhook)
    {
        // Placeholder implementation
        _logger.LogInformation("PayPal dispute webhook received: {EventType}", webhook.EventType);
    }

    public async Task<List<PayPalDisputeDto>> GetPayPalDisputesAsync()
    {
        // Placeholder implementation
        return new List<PayPalDisputeDto>();
    }

    public async Task<List<DisputeSummaryDto>> GetPendingDisputesAsync()
    {
        var disputes = await _context.Disputes
            .Where(d => d.Status == DisputeStatus.Open || d.Status == DisputeStatus.InProgress)
            .Include(d => d.Rental)
                .ThenInclude(r => r.Tool)
            .ToListAsync();

        return disputes.Select(d => new DisputeSummaryDto
        {
            Id = d.Id,
            RentalId = d.RentalId,
            RentalToolName = d.Rental?.Tool?.Name ?? "Unknown Tool",
            InitiatorName = d.InitiatedByName,
            Type = d.Type,
            Status = d.Status,
            Category = d.Category,
            Title = d.Subject,
            DisputeAmount = d.DisputedAmount,
            CreatedAt = d.CreatedAt,
            LastActionAt = d.UpdatedAt
        }).ToList();
    }

    public async Task<DisputeStatisticsDto> GetDisputeStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.Disputes.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(d => d.CreatedAt >= fromDate);

        if (toDate.HasValue)
            query = query.Where(d => d.CreatedAt <= toDate);

        var disputes = await query.ToListAsync();

        return new DisputeStatisticsDto
        {
            TotalDisputes = disputes.Count,
            OpenDisputes = disputes.Count(d => d.Status == DisputeStatus.Open),
            ResolvedDisputes = disputes.Count(d => d.Status == DisputeStatus.Resolved),
            EscalatedDisputes = disputes.Count(d => d.Status == DisputeStatus.EscalatedToPayPal)
        };
    }

    public async Task<AssignDisputeResult> AssignDisputeToAdminAsync(Guid disputeId, string adminUserId)
    {
        // Placeholder implementation
        return new AssignDisputeResult { Success = true, ErrorMessage = null };
    }

    public async Task<UploadEvidenceResult> UploadEvidenceAsync(Guid disputeId, string userId, List<EvidenceFile> files)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Verify dispute exists and user has access
            var dispute = await _context.Disputes
                .Include(d => d.Rental)
                    .ThenInclude(r => r.Tool)
                .FirstOrDefaultAsync(d => d.Id == disputeId);

            if (dispute == null)
            {
                return new UploadEvidenceResult { Success = false, ErrorMessage = "Dispute not found" };
            }

            // Check access permissions
            bool hasAccess = dispute.InitiatedBy == userId ||
                           dispute.Rental.RenterId == userId ||
                           dispute.Rental.Tool.OwnerId == userId;

            if (!hasAccess)
            {
                return new UploadEvidenceResult { Success = false, ErrorMessage = "Access denied" };
            }

            var uploadedFiles = new List<EvidenceFileDto>();

            foreach (var file in files)
            {
                // Upload file to storage
                using var fileStream = new MemoryStream(file.Content);
                var storagePath = await _fileStorageService.UploadFileAsync(
                    fileStream, 
                    file.FileName, 
                    file.ContentType, 
                    $"disputes/{disputeId}");

                // Create evidence record
                var evidence = new DisputeEvidence
                {
                    DisputeId = disputeId,
                    FileName = Path.GetFileName(storagePath),
                    OriginalFileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSize = file.Size,
                    StoragePath = storagePath,
                    Description = file.Description ?? "",
                    UploadedBy = userId,
                    UploadedAt = DateTime.UtcNow,
                    IsPublic = true // Evidence is visible to all parties by default
                };

                _context.Set<DisputeEvidence>().Add(evidence);
                await _context.SaveChangesAsync();

                // Get file URL for response
                var fileUrl = await _fileStorageService.GetFileUrlAsync(storagePath);

                uploadedFiles.Add(new EvidenceFileDto
                {
                    Id = evidence.Id.ToString(),
                    FileName = evidence.OriginalFileName,
                    Url = fileUrl,
                    Size = evidence.FileSize,
                    UploadedAt = evidence.UploadedAt,
                    UploadedBy = evidence.UploadedBy
                });
            }

            await transaction.CommitAsync();

            _logger.LogInformation("Evidence uploaded for dispute {DisputeId}: {FileCount} files", disputeId, files.Count);

            // Send notification about evidence upload
            var uploader = await _context.Users.FindAsync(userId);
            var uploaderName = uploader != null ? $"{uploader.FirstName} {uploader.LastName}" : "Unknown User";
            await _notificationService.SendEvidenceUploadedNotificationAsync(dispute, uploaderName, files.Count);

            return new UploadEvidenceResult 
            { 
                Success = true, 
                ErrorMessage = null,
                UploadedFiles = uploadedFiles 
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error uploading evidence for dispute {DisputeId}", disputeId);
            
            // Clean up any partially uploaded files
            foreach (var file in files)
            {
                try
                {
                    var storagePath = $"disputes/{disputeId}/{file.FileName}";
                    await _fileStorageService.DeleteFileAsync(storagePath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            return new UploadEvidenceResult 
            { 
                Success = false, 
                ErrorMessage = "Failed to upload evidence files" 
            };
        }
    }

    public async Task<List<EvidenceFileDto>> GetDisputeEvidenceAsync(Guid disputeId, string userId)
    {
        try
        {
            // Verify dispute exists and user has access
            var dispute = await _context.Disputes
                .Include(d => d.Rental)
                    .ThenInclude(r => r.Tool)
                .FirstOrDefaultAsync(d => d.Id == disputeId);

            if (dispute == null)
            {
                return new List<EvidenceFileDto>();
            }

            // Check access permissions
            bool hasAccess = dispute.InitiatedBy == userId ||
                           dispute.Rental.RenterId == userId ||
                           dispute.Rental.Tool.OwnerId == userId;

            if (!hasAccess)
            {
                return new List<EvidenceFileDto>();
            }

            // Get evidence files
            var evidenceFiles = await _context.Set<DisputeEvidence>()
                .Where(e => e.DisputeId == disputeId && (e.IsPublic || e.UploadedBy == userId))
                .Include(e => e.UploadedByUser)
                .OrderByDescending(e => e.UploadedAt)
                .ToListAsync();

            var result = new List<EvidenceFileDto>();

            foreach (var evidence in evidenceFiles)
            {
                var fileUrl = await _fileStorageService.GetFileUrlAsync(evidence.StoragePath);
                
                result.Add(new EvidenceFileDto
                {
                    Id = evidence.Id.ToString(),
                    FileName = evidence.OriginalFileName,
                    Url = fileUrl,
                    Size = evidence.FileSize,
                    UploadedAt = evidence.UploadedAt,
                    UploadedBy = evidence.UploadedBy,
                    ContentType = evidence.ContentType,
                    Description = evidence.Description,
                    UploadedByName = evidence.UploadedByUser != null 
                        ? $"{evidence.UploadedByUser.FirstName} {evidence.UploadedByUser.LastName}"
                        : "Unknown User"
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving evidence for dispute {DisputeId}", disputeId);
            return new List<EvidenceFileDto>();
        }
    }
}
