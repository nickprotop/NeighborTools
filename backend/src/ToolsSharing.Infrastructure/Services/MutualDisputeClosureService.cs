using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MapsterMapper;
using ToolsSharing.Core.DTOs.Dispute;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services;

/// <summary>
/// Service implementing mutual dispute closure with industry best practices and comprehensive safeguards
/// </summary>
public class MutualDisputeClosureService : IMutualDisputeClosureService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<MutualDisputeClosureService> _logger;
    private readonly IMutualClosureConfigurationService _config;
    private readonly IMutualClosureNotificationService _notifications;
    private readonly IPaymentService _paymentService;

    public MutualDisputeClosureService(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<MutualDisputeClosureService> logger,
        IMutualClosureConfigurationService config,
        IMutualClosureNotificationService notifications,
        IPaymentService paymentService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _config = config;
        _notifications = notifications;
        _paymentService = paymentService;
    }

    public async Task<CreateMutualClosureResult> CreateMutualClosureRequestAsync(CreateMutualClosureRequest request, string initiatingUserId)
    {
        try
        {
            _logger.LogInformation("Creating mutual closure request for dispute {DisputeId} by user {UserId}", 
                request.DisputeId, initiatingUserId);

            // Validate user eligibility
            if (!await CanUserCreateMutualClosureAsync(request.DisputeId, initiatingUserId))
            {
                return CreateMutualClosureResult.CreateFailure("User is not eligible to create mutual closure for this dispute");
            }

            // Validate business rules
            var validationErrors = await ValidateBusinessRulesAsync(request.DisputeId, request);
            if (validationErrors.Any())
            {
                return CreateMutualClosureResult.CreateFailure($"Validation failed: {string.Join(", ", validationErrors)}");
            }

            // Get dispute and validate
            var dispute = await _context.Disputes
                .Include(d => d.Rental)
                    .ThenInclude(r => r.Tool)
                .Include(d => d.Rental)
                    .ThenInclude(r => r.Renter)
                .FirstOrDefaultAsync(d => d.Id == request.DisputeId);

            if (dispute == null)
            {
                return CreateMutualClosureResult.CreateFailure("Dispute not found");
            }

            // Determine the other party
            var otherPartyUserId = initiatingUserId == dispute.Rental.RenterId 
                ? dispute.Rental.Tool.OwnerId 
                : dispute.Rental.RenterId;

            // Check for existing active mutual closure
            var existingClosure = await _context.Set<MutualDisputeClosure>()
                .FirstOrDefaultAsync(mc => mc.DisputeId == request.DisputeId && 
                                         mc.Status == MutualClosureStatus.Pending);

            if (existingClosure != null)
            {
                return CreateMutualClosureResult.CreateFailure("There is already an active mutual closure request for this dispute");
            }

            // Create mutual closure request
            var mutualClosure = new MutualDisputeClosure
            {
                Id = Guid.NewGuid(),
                DisputeId = request.DisputeId,
                InitiatedByUserId = initiatingUserId,
                ResponseRequiredFromUserId = otherPartyUserId,
                ProposedResolution = request.ProposedResolution,
                ResolutionDetails = request.ResolutionDetails,
                AgreedRefundAmount = request.AgreedRefundAmount,
                RefundRecipient = request.RefundRecipient,
                RequiresPaymentAction = request.RequiresPaymentAction,
                ExpiresAt = DateTime.UtcNow.AddHours(request.ExpirationHours),
                Status = _config.RequiresAdminReviewForAmount(request.AgreedRefundAmount ?? 0) ||
                        _config.RequiresAdminReviewForDisputeType(dispute.Type)
                    ? MutualClosureStatus.UnderAdminReview
                    : MutualClosureStatus.Pending
            };

            _context.Set<MutualDisputeClosure>().Add(mutualClosure);

            // Add audit log
            await AddAuditLogAsync(mutualClosure.Id, initiatingUserId, "Created", 
                "Mutual closure request created", GetUserContext());

            await _context.SaveChangesAsync();

            // Send notifications
            var mutualClosureDto = await GetMutualClosureAsync(mutualClosure.Id, initiatingUserId);
            if (mutualClosureDto != null)
            {
                await _notifications.SendMutualClosureRequestNotificationAsync(mutualClosureDto);
                
                // Notify admin if high-value or requires review
                if (mutualClosure.Status == MutualClosureStatus.UnderAdminReview)
                {
                    await _notifications.NotifyAdminOfHighValueMutualClosureAsync(mutualClosureDto);
                }
            }

            _logger.LogInformation("Mutual closure request {MutualClosureId} created successfully", mutualClosure.Id);

            return CreateMutualClosureResult.CreateSuccess(mutualClosureDto!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating mutual closure request for dispute {DisputeId}", request.DisputeId);
            return CreateMutualClosureResult.CreateFailure("An error occurred while creating the mutual closure request");
        }
    }

    public async Task<RespondToMutualClosureResult> RespondToMutualClosureAsync(RespondToMutualClosureRequest request, string respondingUserId)
    {
        try
        {
            _logger.LogInformation("User {UserId} responding to mutual closure {MutualClosureId} with {Response}", 
                respondingUserId, request.MutualClosureId, request.Accept ? "ACCEPT" : "REJECT");

            // Validate user can respond
            if (!await CanUserRespondToMutualClosureAsync(request.MutualClosureId, respondingUserId))
            {
                return RespondToMutualClosureResult.CreateFailure("User is not authorized to respond to this mutual closure request");
            }

            var mutualClosure = await _context.Set<MutualDisputeClosure>()
                .Include(mc => mc.Dispute)
                    .ThenInclude(d => d.Rental)
                        .ThenInclude(r => r.Tool)
                .FirstOrDefaultAsync(mc => mc.Id == request.MutualClosureId);

            if (mutualClosure == null)
            {
                return RespondToMutualClosureResult.CreateFailure("Mutual closure request not found");
            }

            if (mutualClosure.Status != MutualClosureStatus.Pending)
            {
                return RespondToMutualClosureResult.CreateFailure("This mutual closure request is no longer pending");
            }

            if (mutualClosure.IsExpired)
            {
                mutualClosure.Status = MutualClosureStatus.Expired;
                await AddAuditLogAsync(mutualClosure.Id, respondingUserId, "Expired", 
                    "Mutual closure request expired before response", GetUserContext());
                await _context.SaveChangesAsync();
                return RespondToMutualClosureResult.CreateFailure("This mutual closure request has expired");
            }

            // Update mutual closure with response
            mutualClosure.RespondedAt = DateTime.UtcNow;
            mutualClosure.ResponseMessage = request.ResponseMessage;

            if (request.Accept)
            {
                // Accept the mutual closure
                mutualClosure.Status = MutualClosureStatus.Accepted;
                await AddAuditLogAsync(mutualClosure.Id, respondingUserId, "Accepted", 
                    "Mutual closure request accepted", GetUserContext());

                // Process refund if required
                string? refundTransactionId = null;
                if (mutualClosure.RequiresPaymentAction && mutualClosure.AgreedRefundAmount.HasValue)
                {
                    refundTransactionId = await ProcessRefundAsync(mutualClosure);
                    mutualClosure.RefundTransactionId = refundTransactionId;
                }

                // Update dispute status to resolved (avoiding circular dependency)
                mutualClosure.Dispute.Status = DisputeStatus.Resolved;
                mutualClosure.Dispute.Resolution = DisputeResolution.MutualAgreement;
                mutualClosure.Dispute.ResolvedAt = DateTime.UtcNow;
                mutualClosure.Dispute.ResolutionNotes = $"Dispute resolved through mutual agreement. Resolution: {mutualClosure.ProposedResolution}";
                mutualClosure.Dispute.RefundAmount = mutualClosure.AgreedRefundAmount;
                mutualClosure.Dispute.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Dispute {DisputeId} resolved through mutual closure {MutualClosureId}", 
                    mutualClosure.DisputeId, mutualClosure.Id);

                await _notifications.SendMutualClosureResponseNotificationAsync(
                    _mapper.Map<MutualClosureDto>(mutualClosure), true);

                _logger.LogInformation("Mutual closure {MutualClosureId} accepted and dispute {DisputeId} resolved", 
                    mutualClosure.Id, mutualClosure.DisputeId);

                await _context.SaveChangesAsync();

                return RespondToMutualClosureResult.CreateSuccess(
                    _mapper.Map<MutualClosureDto>(mutualClosure), 
                    disputeClosed: true, 
                    refundTransactionId: refundTransactionId);
            }
            else
            {
                // Reject the mutual closure
                mutualClosure.Status = MutualClosureStatus.Rejected;
                mutualClosure.RejectionReason = request.RejectionReason;
                await AddAuditLogAsync(mutualClosure.Id, respondingUserId, "Rejected", 
                    $"Mutual closure request rejected: {request.RejectionReason}", GetUserContext());

                await _notifications.SendMutualClosureResponseNotificationAsync(
                    _mapper.Map<MutualClosureDto>(mutualClosure), false);

                _logger.LogInformation("Mutual closure {MutualClosureId} rejected by user {UserId}", 
                    mutualClosure.Id, respondingUserId);

                await _context.SaveChangesAsync();

                return RespondToMutualClosureResult.CreateSuccess(_mapper.Map<MutualClosureDto>(mutualClosure));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error responding to mutual closure {MutualClosureId}", request.MutualClosureId);
            return RespondToMutualClosureResult.CreateFailure("An error occurred while processing the response");
        }
    }

    public async Task<RespondToMutualClosureResult> CancelMutualClosureAsync(CancelMutualClosureRequest request, string cancellingUserId)
    {
        try
        {
            var mutualClosure = await _context.Set<MutualDisputeClosure>()
                .FirstOrDefaultAsync(mc => mc.Id == request.MutualClosureId);

            if (mutualClosure == null)
            {
                return RespondToMutualClosureResult.CreateFailure("Mutual closure request not found");
            }

            if (mutualClosure.InitiatedByUserId != cancellingUserId)
            {
                return RespondToMutualClosureResult.CreateFailure("Only the initiator can cancel a mutual closure request");
            }

            if (mutualClosure.Status != MutualClosureStatus.Pending)
            {
                return RespondToMutualClosureResult.CreateFailure("This mutual closure request cannot be cancelled");
            }

            mutualClosure.Status = MutualClosureStatus.Cancelled;
            await AddAuditLogAsync(mutualClosure.Id, cancellingUserId, "Cancelled", 
                $"Mutual closure cancelled: {request.CancellationReason}", GetUserContext());

            await _notifications.SendMutualClosureCancelledNotificationAsync(_mapper.Map<MutualClosureDto>(mutualClosure));

            await _context.SaveChangesAsync();

            _logger.LogInformation("Mutual closure {MutualClosureId} cancelled by user {UserId}", 
                request.MutualClosureId, cancellingUserId);

            return RespondToMutualClosureResult.CreateSuccess(_mapper.Map<MutualClosureDto>(mutualClosure));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling mutual closure {MutualClosureId}", request.MutualClosureId);
            return RespondToMutualClosureResult.CreateFailure("An error occurred while cancelling the mutual closure request");
        }
    }

    public async Task<MutualClosureDto?> GetMutualClosureAsync(Guid mutualClosureId, string userId)
    {
        try
        {
            var mutualClosure = await _context.Set<MutualDisputeClosure>()
                .Include(mc => mc.Dispute)
                .Include(mc => mc.InitiatedByUser)
                .Include(mc => mc.ResponseRequiredFromUser)
                .Include(mc => mc.AuditLogs)
                    .ThenInclude(al => al.User)
                .FirstOrDefaultAsync(mc => mc.Id == mutualClosureId &&
                    (mc.InitiatedByUserId == userId || mc.ResponseRequiredFromUserId == userId));

            if (mutualClosure == null)
                return null;

            var dto = _mapper.Map<MutualClosureDto>(mutualClosure);
            dto.IsExpired = mutualClosure.IsExpired;
            dto.IsActionable = mutualClosure.IsActionable;
            dto.HoursUntilExpiry = mutualClosure.IsExpired ? 0 : 
                (int)Math.Max(0, (mutualClosure.ExpiresAt - DateTime.UtcNow).TotalHours);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mutual closure {MutualClosureId}", mutualClosureId);
            return null;
        }
    }

    public async Task<MutualClosureEligibilityDto> CheckMutualClosureEligibilityAsync(Guid disputeId, string userId)
    {
        try
        {
            var result = new MutualClosureEligibilityDto { IsEligible = false };

            var dispute = await _context.Disputes
                .Include(d => d.Rental)
                    .ThenInclude(r => r.Tool)
                .Include(d => d.Payment)
                .FirstOrDefaultAsync(d => d.Id == disputeId);

            if (dispute == null)
            {
                result.Reasons.Add("Dispute not found");
                return result;
            }

            // Check if user is involved in dispute
            if (userId != dispute.Rental.RenterId && userId != dispute.Rental.Tool.OwnerId)
            {
                result.Reasons.Add("User is not involved in this dispute");
                return result;
            }

            // Check dispute eligibility
            if (!await IsDisputeEligibleForMutualClosureAsync(dispute))
            {
                result.Reasons.Add("Dispute is not eligible for mutual closure");
                return result;
            }

            // Check user eligibility
            if (!_config.IsUserEligibleForMutualClosure(userId))
            {
                result.Reasons.Add("User is not eligible for mutual closure");
                return result;
            }

            // Check for existing active mutual closure
            var existingClosure = await _context.Set<MutualDisputeClosure>()
                .AnyAsync(mc => mc.DisputeId == disputeId && mc.Status == MutualClosureStatus.Pending);

            if (existingClosure)
            {
                result.Reasons.Add("There is already an active mutual closure request for this dispute");
                return result;
            }

            // Check velocity limits
            if (_config.ExceedsVelocityLimits(userId))
            {
                result.Reasons.Add("User has exceeded velocity limits for mutual closure requests");
                return result;
            }

            // All checks passed
            result.IsEligible = true;
            result.MaxRefundAmount = await GetMaxAllowedRefundAmountAsync(disputeId);
            result.RequiresAdminReview = _config.RequiresAdminReviewForDisputeType(dispute.Type);

            // Add any restrictions
            if (dispute.Type == DisputeType.PaymentDispute)
            {
                result.Restrictions.Add("Payment disputes require admin verification of refund amounts");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking mutual closure eligibility for dispute {DisputeId}", disputeId);
            return new MutualClosureEligibilityDto { IsEligible = false, Reasons = { "An error occurred while checking eligibility" } };
        }
    }

    public async Task<bool> CanUserCreateMutualClosureAsync(Guid disputeId, string userId)
    {
        var eligibility = await CheckMutualClosureEligibilityAsync(disputeId, userId);
        return eligibility.IsEligible;
    }

    public async Task<bool> CanUserRespondToMutualClosureAsync(Guid mutualClosureId, string userId)
    {
        var mutualClosure = await _context.Set<MutualDisputeClosure>()
            .FirstOrDefaultAsync(mc => mc.Id == mutualClosureId);

        return mutualClosure != null &&
               mutualClosure.ResponseRequiredFromUserId == userId &&
               mutualClosure.Status == MutualClosureStatus.Pending &&
               !mutualClosure.IsExpired;
    }

    public async Task<List<string>> ValidateBusinessRulesAsync(Guid disputeId, CreateMutualClosureRequest request)
    {
        var errors = new List<string>();

        try
        {
            // Validate refund amount
            if (request.AgreedRefundAmount.HasValue)
            {
                if (request.AgreedRefundAmount < 0)
                {
                    errors.Add("Refund amount cannot be negative");
                }

                var maxRefund = await GetMaxAllowedRefundAmountAsync(disputeId);
                if (request.AgreedRefundAmount > maxRefund)
                {
                    errors.Add($"Refund amount cannot exceed ${maxRefund:F2}");
                }

                if (request.AgreedRefundAmount > _config.GetMaxMutualClosureAmount())
                {
                    errors.Add($"Refund amount exceeds maximum allowed amount of ${_config.GetMaxMutualClosureAmount():F2}");
                }
            }

            // Validate expiration hours
            if (request.ExpirationHours < _config.GetMinExpirationHours())
            {
                errors.Add($"Expiration must be at least {_config.GetMinExpirationHours()} hours");
            }

            if (request.ExpirationHours > _config.GetMaxExpirationHours())
            {
                errors.Add($"Expiration cannot exceed {_config.GetMaxExpirationHours()} hours");
            }

            // Validate refund recipient consistency
            if (request.AgreedRefundAmount.HasValue && request.AgreedRefundAmount > 0 && 
                (!request.RefundRecipient.HasValue || request.RefundRecipient == RefundRecipient.None))
            {
                errors.Add("Refund recipient must be specified when refund amount is provided");
            }

            if (!request.AgreedRefundAmount.HasValue || request.AgreedRefundAmount == 0)
            {
                if (request.RefundRecipient.HasValue && request.RefundRecipient != RefundRecipient.None)
                {
                    errors.Add("Refund recipient should be 'None' when no refund amount is specified");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating business rules for dispute {DisputeId}", disputeId);
            errors.Add("An error occurred while validating the request");
        }

        return errors;
    }

    public async Task<bool> IsDisputeEligibleForMutualClosureAsync(Dispute dispute)
    {
        try
        {
            // Check dispute status eligibility
            if (!_config.GetEligibleDisputeStatuses().Contains(dispute.Status))
            {
                return false;
            }

            // Check dispute type eligibility
            if (!_config.GetEligibleDisputeTypes().Contains(dispute.Type))
            {
                return false;
            }

            // Check if dispute is escalated to external providers
            if (!string.IsNullOrEmpty(dispute.ExternalDisputeId))
            {
                return false;
            }

            // Check if dispute involves fraud allegations
            if (dispute.Category == DisputeCategory.Fraud)
            {
                return false;
            }

            // Check dispute age (disputes older than 30 days require admin review)
            if (DateTime.UtcNow.Subtract(dispute.CreatedAt).TotalDays > 30)
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking dispute eligibility for dispute {DisputeId}", dispute.Id);
            return false;
        }
    }

    public async Task<decimal> GetMaxAllowedRefundAmountAsync(Guid disputeId)
    {
        try
        {
            var dispute = await _context.Disputes
                .Include(d => d.Payment)
                .FirstOrDefaultAsync(d => d.Id == disputeId);

            if (dispute?.Payment == null)
            {
                return 0;
            }

            // Maximum refund is the total payment amount minus any platform fees already processed
            var maxRefund = dispute.Payment.Amount - (dispute.Payment.Amount * 0.05m); // Assuming 5% platform fee
            return Math.Min(maxRefund, _config.GetMaxMutualClosureAmount());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating max refund amount for dispute {DisputeId}", disputeId);
            return 0;
        }
    }

    public async Task<GetMutualClosuresResult> GetUserMutualClosuresAsync(string userId, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.Set<MutualDisputeClosure>()
                .Include(mc => mc.Dispute)
                .Include(mc => mc.InitiatedByUser)
                .Include(mc => mc.ResponseRequiredFromUser)
                .Where(mc => mc.InitiatedByUserId == userId || mc.ResponseRequiredFromUserId == userId);

            var totalCount = await query.CountAsync();

            var mutualClosures = await query
                .OrderByDescending(mc => mc.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = mutualClosures.Select(mc => _mapper.Map<MutualClosureSummaryDto>(mc)).ToList();

            return GetMutualClosuresResult.CreateSuccess(dtos, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mutual closures for user {UserId}", userId);
            return GetMutualClosuresResult.CreateFailure("An error occurred while retrieving mutual closures");
        }
    }

    public async Task<GetMutualClosuresResult> GetDisputeMutualClosuresAsync(Guid disputeId, string userId)
    {
        try
        {
            // Verify user has access to this dispute
            var dispute = await _context.Disputes
                .Include(d => d.Rental)
                    .ThenInclude(r => r.Tool)
                .FirstOrDefaultAsync(d => d.Id == disputeId);

            if (dispute == null || (userId != dispute.Rental.RenterId && userId != dispute.Rental.Tool.OwnerId))
            {
                return GetMutualClosuresResult.CreateFailure("Access denied to dispute mutual closures");
            }

            var mutualClosures = await _context.Set<MutualDisputeClosure>()
                .Include(mc => mc.InitiatedByUser)
                .Include(mc => mc.ResponseRequiredFromUser)
                .Where(mc => mc.DisputeId == disputeId)
                .OrderByDescending(mc => mc.CreatedAt)
                .ToListAsync();

            var dtos = mutualClosures.Select(mc => _mapper.Map<MutualClosureSummaryDto>(mc)).ToList();

            return GetMutualClosuresResult.CreateSuccess(dtos, dtos.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mutual closures for dispute {DisputeId}", disputeId);
            return GetMutualClosuresResult.CreateFailure("An error occurred while retrieving mutual closures");
        }
    }

    public async Task<GetMutualClosuresResult> GetMutualClosuresForAdminAsync(int page = 1, int pageSize = 20, MutualClosureStatus? status = null)
    {
        try
        {
            var query = _context.Set<MutualDisputeClosure>()
                .Include(mc => mc.Dispute)
                .Include(mc => mc.InitiatedByUser)
                .Include(mc => mc.ResponseRequiredFromUser)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(mc => mc.Status == status.Value);
            }

            var totalCount = await query.CountAsync();

            var mutualClosures = await query
                .OrderByDescending(mc => mc.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = mutualClosures.Select(mc => _mapper.Map<MutualClosureSummaryDto>(mc)).ToList();

            return GetMutualClosuresResult.CreateSuccess(dtos, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mutual closures for admin");
            return GetMutualClosuresResult.CreateFailure("An error occurred while retrieving mutual closures");
        }
    }

    public async Task<RespondToMutualClosureResult> AdminReviewMutualClosureAsync(AdminReviewMutualClosureRequest request, string adminUserId)
    {
        try
        {
            var mutualClosure = await _context.Set<MutualDisputeClosure>()
                .FirstOrDefaultAsync(mc => mc.Id == request.MutualClosureId);

            if (mutualClosure == null)
            {
                return RespondToMutualClosureResult.CreateFailure("Mutual closure request not found");
            }

            switch (request.Action)
            {
                case MutualClosureAdminAction.Approve:
                    mutualClosure.Status = MutualClosureStatus.Pending;
                    break;
                case MutualClosureAdminAction.Block:
                    mutualClosure.Status = MutualClosureStatus.AdminBlocked;
                    break;
                case MutualClosureAdminAction.RequireReview:
                    mutualClosure.Status = MutualClosureStatus.UnderAdminReview;
                    break;
                case MutualClosureAdminAction.Override:
                    mutualClosure.Status = MutualClosureStatus.Accepted;
                    // Process as if both parties agreed
                    break;
            }

            mutualClosure.ReviewedByAdminId = adminUserId;
            mutualClosure.AdminReviewedAt = DateTime.UtcNow;
            mutualClosure.AdminNotes = request.AdminNotes;

            await AddAuditLogAsync(mutualClosure.Id, adminUserId, $"Admin{request.Action}", 
                $"Admin {request.Action.ToString().ToLower()}: {request.Reason}", GetUserContext());

            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin {AdminId} {Action} mutual closure {MutualClosureId}", 
                adminUserId, request.Action, request.MutualClosureId);

            return RespondToMutualClosureResult.CreateSuccess(_mapper.Map<MutualClosureDto>(mutualClosure));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in admin review of mutual closure {MutualClosureId}", request.MutualClosureId);
            return RespondToMutualClosureResult.CreateFailure("An error occurred during admin review");
        }
    }

    public async Task<MutualClosureStatsDto> GetMutualClosureStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            fromDate ??= DateTime.UtcNow.AddDays(-30);
            toDate ??= DateTime.UtcNow;

            var query = _context.Set<MutualDisputeClosure>()
                .Include(mc => mc.Dispute)
                .Where(mc => mc.CreatedAt >= fromDate && mc.CreatedAt <= toDate);

            var stats = new MutualClosureStatsDto
            {
                TotalRequests = await query.CountAsync(),
                AcceptedRequests = await query.CountAsync(mc => mc.Status == MutualClosureStatus.Accepted),
                RejectedRequests = await query.CountAsync(mc => mc.Status == MutualClosureStatus.Rejected),
                ExpiredRequests = await query.CountAsync(mc => mc.Status == MutualClosureStatus.Expired),
                PendingRequests = await query.CountAsync(mc => mc.Status == MutualClosureStatus.Pending),
                TotalRefundAmount = await query.Where(mc => mc.Status == MutualClosureStatus.Accepted)
                    .SumAsync(mc => mc.AgreedRefundAmount ?? 0)
            };

            if (stats.TotalRequests > 0)
            {
                stats.AcceptanceRate = (decimal)stats.AcceptedRequests / stats.TotalRequests * 100;

                var responseTimes = await query
                    .Where(mc => mc.RespondedAt.HasValue)
                    .Select(mc => EF.Functions.DateDiffHour(mc.CreatedAt, mc.RespondedAt.Value))
                    .ToListAsync();

                if (responseTimes.Any())
                {
                    stats.AverageResponseTimeHours = (decimal)responseTimes.Average();
                }
            }

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mutual closure statistics");
            return new MutualClosureStatsDto();
        }
    }

    public async Task ProcessExpiredMutualClosuresAsync()
    {
        try
        {
            var expiredClosures = await _context.Set<MutualDisputeClosure>()
                .Where(mc => mc.Status == MutualClosureStatus.Pending && mc.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            foreach (var closure in expiredClosures)
            {
                closure.Status = MutualClosureStatus.Expired;
                await AddAuditLogAsync(closure.Id, "SYSTEM", "Expired", 
                    "Mutual closure request expired automatically", null);

                await _notifications.SendMutualClosureExpiredNotificationAsync(_mapper.Map<MutualClosureDto>(closure));
            }

            if (expiredClosures.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Processed {Count} expired mutual closure requests", expiredClosures.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expired mutual closures");
        }
    }

    public async Task SendMutualClosureRemindersAsync()
    {
        try
        {
            var reminders = await _context.Set<MutualDisputeClosure>()
                .Where(mc => mc.Status == MutualClosureStatus.Pending && 
                           mc.ExpiresAt > DateTime.UtcNow &&
                           mc.ExpiresAt <= DateTime.UtcNow.AddHours(24))
                .ToListAsync();

            foreach (var closure in reminders)
            {
                await _notifications.SendMutualClosureExpiryReminderAsync(_mapper.Map<MutualClosureDto>(closure));
            }

            _logger.LogInformation("Sent {Count} mutual closure expiry reminders", reminders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending mutual closure reminders");
        }
    }

    // Private helper methods
    private async Task<string?> ProcessRefundAsync(MutualDisputeClosure mutualClosure)
    {
        try
        {
            if (!mutualClosure.AgreedRefundAmount.HasValue || mutualClosure.AgreedRefundAmount <= 0)
                return null;

            // Process refund through payment service
            var reason = $"Mutual dispute closure agreement: {mutualClosure.ProposedResolution}";
            var result = await _paymentService.RefundRentalAsync(
                mutualClosure.Dispute.RentalId, 
                mutualClosure.AgreedRefundAmount.Value, 
                reason);

            return result.Success ? result.RefundId : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for mutual closure {MutualClosureId}", mutualClosure.Id);
            return null;
        }
    }

    private async Task AddAuditLogAsync(Guid mutualClosureId, string userId, string actionType, string description, Dictionary<string, object>? context)
    {
        var auditLog = new MutualClosureAuditLog
        {
            Id = Guid.NewGuid(),
            MutualClosureId = mutualClosureId,
            UserId = userId,
            ActionType = actionType,
            Description = description,
            Metadata = context != null ? System.Text.Json.JsonSerializer.Serialize(context) : null,
            IpAddress = GetClientIpAddress(),
            UserAgent = GetUserAgent()
        };

        _context.Set<MutualClosureAuditLog>().Add(auditLog);
    }

    private Dictionary<string, object>? GetUserContext()
    {
        // This would be populated from HTTP context in a real implementation
        return new Dictionary<string, object>
        {
            { "timestamp", DateTime.UtcNow },
            { "source", "MutualClosureService" }
        };
    }

    private string? GetClientIpAddress()
    {
        // This would be populated from HTTP context in a real implementation
        return null;
    }

    private string? GetUserAgent()
    {
        // This would be populated from HTTP context in a real implementation
        return null;
    }
}

