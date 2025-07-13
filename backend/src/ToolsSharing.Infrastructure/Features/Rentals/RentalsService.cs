using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Common.Models.EmailNotifications;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Features.Rentals;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Features.Rentals;

public class RentalsService : IRentalsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISettingsService _settingsService;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly IPaymentService _paymentService;

    public RentalsService(
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ApplicationDbContext context,
        ISettingsService settingsService,
        IEmailNotificationService emailNotificationService,
        IPaymentService paymentService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _context = context;
        _settingsService = settingsService;
        _emailNotificationService = emailNotificationService;
        _paymentService = paymentService;
    }

    public async Task<ApiResponse<List<RentalDto>>> GetRentalsAsync(GetRentalsQuery query)
    {
        try
        {
            var rentalsQuery = _context.Rentals
                .Include(r => r.Tool)
                    .ThenInclude(t => t.Owner)
                .Include(r => r.Tool.Images)
                .Include(r => r.Renter)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(query.UserId))
            {
                // Filter by renter or owner
                rentalsQuery = rentalsQuery.Where(r => 
                    r.RenterId == query.UserId || 
                    r.Tool.OwnerId == query.UserId);
            }

            if (query.Status.HasValue)
            {
                rentalsQuery = rentalsQuery.Where(r => r.Status == query.Status.Value);
            }

            if (query.ToolId.HasValue)
            {
                rentalsQuery = rentalsQuery.Where(r => r.ToolId == query.ToolId.Value);
            }

            // Apply sorting
            rentalsQuery = query.SortBy?.ToLower() switch
            {
                "startdate" => rentalsQuery.OrderBy(r => r.StartDate),
                "enddate" => rentalsQuery.OrderBy(r => r.EndDate),
                "created" => rentalsQuery.OrderByDescending(r => r.CreatedAt),
                "status" => rentalsQuery.OrderBy(r => r.Status),
                _ => rentalsQuery.OrderByDescending(r => r.CreatedAt)
            };

            // Apply pagination
            if (query.PageSize > 0)
            {
                rentalsQuery = rentalsQuery
                    .Skip((query.PageNumber - 1) * query.PageSize)
                    .Take(query.PageSize);
            }

            var rentals = await rentalsQuery.ToListAsync();
            var rentalDtos = _mapper.Map<List<RentalDto>>(rentals);
            
            // Set IsPaid based on transaction status
            var rentalIds = rentals.Select(r => r.Id).ToList();
            var paidRentalIds = await _context.Transactions
                .Where(t => rentalIds.Contains(t.RentalId) && 
                           (t.Status == TransactionStatus.PaymentCompleted || 
                            t.Status == TransactionStatus.PayoutPending || 
                            t.Status == TransactionStatus.PayoutCompleted))
                .Select(t => t.RentalId)
                .ToListAsync();
            
            foreach (var rentalDto in rentalDtos)
            {
                rentalDto.IsPaid = paidRentalIds.Contains(rentalDto.Id);
            }

            return ApiResponse<List<RentalDto>>.CreateSuccess(rentalDtos, "Rentals retrieved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<List<RentalDto>>.CreateFailure($"Error retrieving rentals: {ex.Message}");
        }
    }

    public async Task<ApiResponse<RentalDto>> GetRentalByIdAsync(GetRentalByIdQuery query)
    {
        try
        {
            var rental = await _context.Rentals
                .Include(r => r.Tool)
                    .ThenInclude(t => t.Owner)
                .Include(r => r.Tool.Images)
                .Include(r => r.Renter)
                .FirstOrDefaultAsync(r => r.Id == query.Id);

            if (rental == null)
            {
                return ApiResponse<RentalDto>.CreateFailure("Rental not found");
            }

            var rentalDto = _mapper.Map<RentalDto>(rental);
            
            // Set IsPaid based on transaction status
            var isPaid = await _context.Transactions
                .AnyAsync(t => t.RentalId == rental.Id && 
                          (t.Status == TransactionStatus.PaymentCompleted || 
                           t.Status == TransactionStatus.PayoutPending || 
                           t.Status == TransactionStatus.PayoutCompleted));
            
            rentalDto.IsPaid = isPaid;
            
            return ApiResponse<RentalDto>.CreateSuccess(rentalDto, "Rental retrieved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<RentalDto>.CreateFailure($"Error retrieving rental: {ex.Message}");
        }
    }

    public async Task<ApiResponse<RentalDto>> CreateRentalAsync(CreateRentalCommand command)
    {
        try
        {
            // Validate tool exists and is available
            var tool = await _context.Tools
                .Include(t => t.Owner)
                .Include(t => t.Images)
                .FirstOrDefaultAsync(t => t.Id == command.ToolId && !t.IsDeleted);

            if (tool == null)
            {
                return ApiResponse<RentalDto>.CreateFailure("Tool not found");
            }

            if (!tool.IsAvailable)
            {
                return ApiResponse<RentalDto>.CreateFailure("Tool is not available for rent");
            }

            // Validate renter exists
            var renter = await _context.Users.FindAsync(command.RenterId);
            if (renter == null)
            {
                return ApiResponse<RentalDto>.CreateFailure("Renter not found");
            }

            // Check if owner is trying to rent their own tool
            if (tool.OwnerId == command.RenterId)
            {
                return ApiResponse<RentalDto>.CreateFailure("You cannot rent your own tool");
            }

            // SAFETY MEASURE: Validate that tool owner has payment settings configured
            // This prevents rentals when the owner can't receive payouts
            var ownerPaymentSettings = await _paymentService.GetOrCreatePaymentSettingsAsync(tool.OwnerId);
            if (string.IsNullOrEmpty(ownerPaymentSettings.PayPalEmail))
            {
                return ApiResponse<RentalDto>.CreateFailure(
                    "This tool is temporarily unavailable for rent. The owner needs to configure their payment settings to receive payouts. " +
                    "Please try again later or contact the tool owner.");
            }

            // Get owner's settings for lead time enforcement and auto-approval
            var ownerSettings = await _settingsService.GetUserSettingsAsync(tool.OwnerId);
            
            // Enforce rental lead time - use tool-specific lead time with fallback to owner's default
            var leadTimeHours = tool.LeadTimeHours ?? ownerSettings?.Rental?.RentalLeadTime ?? 24; // Default to 24 hours if nothing is set
            var minimumStartTime = DateTime.UtcNow.AddHours(leadTimeHours);
            
            if (command.StartDate < minimumStartTime)
            {
                return ApiResponse<RentalDto>.CreateFailure(
                    $"Rental requests must be made at least {leadTimeHours} hours in advance. " +
                    $"Earliest available start time: {minimumStartTime:yyyy-MM-dd HH:mm} UTC");
            }

            // Check for conflicting rentals
            var hasConflictingRental = await _context.Rentals
                .AnyAsync(r => r.ToolId == command.ToolId &&
                          r.Status != RentalStatus.Returned &&
                          r.Status != RentalStatus.Cancelled &&
                          ((command.StartDate >= r.StartDate && command.StartDate <= r.EndDate) ||
                           (command.EndDate >= r.StartDate && command.EndDate <= r.EndDate) ||
                           (command.StartDate <= r.StartDate && command.EndDate >= r.EndDate)));

            if (hasConflictingRental)
            {
                return ApiResponse<RentalDto>.CreateFailure("Tool is already booked for the selected dates");
            }

            // Calculate rental costs and apply deposit settings
            var rentalDays = (command.EndDate - command.StartDate).Days + 1;
            var totalCost = CalculateRentalCost(tool, command.StartDate, command.EndDate);
            
            // Calculate deposit based on owner's settings
            var depositAmount = tool.DepositRequired;
            if (ownerSettings?.Rental != null)
            {
                if (ownerSettings.Rental.RequireDeposit)
                {
                    // Use owner's default deposit percentage if tool doesn't specify deposit
                    if (depositAmount == 0)
                    {
                        depositAmount = totalCost * ownerSettings.Rental.DefaultDepositPercentage;
                    }
                }
                else
                {
                    // Owner doesn't require deposits
                    depositAmount = 0;
                }
            }

            // Determine initial status based on auto-approval setting
            var initialStatus = RentalStatus.Pending;
            DateTime? approvedAt = null;
            
            if (ownerSettings?.Rental?.AutoApproveRentals == true)
            {
                initialStatus = RentalStatus.Approved;
                approvedAt = DateTime.UtcNow;
            }

            var rental = new Rental
            {
                Id = Guid.NewGuid(),
                ToolId = command.ToolId,
                RenterId = command.RenterId,
                StartDate = command.StartDate,
                EndDate = command.EndDate,
                TotalCost = totalCost,
                DepositAmount = depositAmount,
                Status = initialStatus,
                Notes = command.Notes,
                ApprovedAt = approvedAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Rentals.Add(rental);
            await _context.SaveChangesAsync();

            // Reload with includes to get complete data for mapping
            var createdRental = await _context.Rentals
                .Include(r => r.Tool)
                    .ThenInclude(t => t.Owner)
                .Include(r => r.Tool.Images)
                .Include(r => r.Renter)
                .FirstAsync(r => r.Id == rental.Id);

            // Send appropriate email notifications
            try
            {
                if (initialStatus == RentalStatus.Approved)
                {
                    // Auto-approved - send approval notification to renter
                    var approvalNotification = new RentalApprovedNotification
                    {
                        RecipientEmail = createdRental.Renter.Email!,
                        RecipientName = $"{createdRental.Renter.FirstName} {createdRental.Renter.LastName}",
                        UserId = createdRental.RenterId,
                        RenterName = $"{createdRental.Renter.FirstName} {createdRental.Renter.LastName}",
                        OwnerName = $"{createdRental.Tool.Owner.FirstName} {createdRental.Tool.Owner.LastName}",
                        OwnerEmail = createdRental.Tool.Owner.Email!,
                        OwnerPhone = createdRental.Tool.Owner.PhoneNumber ?? "Not provided",
                        ToolName = createdRental.Tool.Name,
                        ToolLocation = createdRental.Tool.Location,
                        StartDate = createdRental.StartDate,
                        EndDate = createdRental.EndDate,
                        TotalCost = createdRental.TotalCost,
                        RentalDetailsUrl = $"/rentals/{createdRental.Id}",
                        Priority = EmailPriority.High
                    };
                    
                    await _emailNotificationService.SendNotificationAsync(approvalNotification);
                }
                else
                {
                    // Pending approval - send request notification to owner
                    var requestNotification = new RentalRequestNotification
                    {
                        RecipientEmail = createdRental.Tool.Owner.Email!,
                        RecipientName = $"{createdRental.Tool.Owner.FirstName} {createdRental.Tool.Owner.LastName}",
                        UserId = createdRental.Tool.OwnerId,
                        RenterName = $"{createdRental.Renter.FirstName} {createdRental.Renter.LastName}",
                        OwnerName = $"{createdRental.Tool.Owner.FirstName} {createdRental.Tool.Owner.LastName}",
                        ToolName = createdRental.Tool.Name,
                        StartDate = createdRental.StartDate,
                        EndDate = createdRental.EndDate,
                        TotalCost = createdRental.TotalCost,
                        Message = createdRental.Notes,
                        ApprovalUrl = $"/rentals/{createdRental.Id}/approve",
                        RentalDetailsUrl = $"/rentals/{createdRental.Id}",
                        Priority = EmailPriority.Normal
                    };
                    
                    await _emailNotificationService.SendNotificationAsync(requestNotification);
                }
            }
            catch (Exception emailEx)
            {
                // Log email error but don't fail the rental creation
                // Email notification failure shouldn't prevent rental creation
                Console.WriteLine($"Email notification failed: {emailEx.Message}");
            }

            var rentalDto = _mapper.Map<RentalDto>(createdRental);
            var successMessage = initialStatus == RentalStatus.Approved 
                ? "Rental request automatically approved and confirmed!" 
                : "Rental request created successfully and sent to the tool owner for approval";
                
            return ApiResponse<RentalDto>.CreateSuccess(rentalDto, successMessage);
        }
        catch (Exception ex)
        {
            return ApiResponse<RentalDto>.CreateFailure($"Error creating rental: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> ApproveRentalAsync(ApproveRentalCommand command)
    {
        try
        {
            var rental = await _context.Rentals
                .Include(r => r.Tool)
                    .ThenInclude(t => t.Owner)
                .Include(r => r.Tool.Images)
                .Include(r => r.Renter)
                .FirstOrDefaultAsync(r => r.Id == command.RentalId);

            if (rental == null)
            {
                return ApiResponse<bool>.CreateFailure("Rental not found");
            }

            // Check if the user is the tool owner
            if (rental.Tool.OwnerId != command.OwnerId)
            {
                return ApiResponse<bool>.CreateFailure("Only the tool owner can approve rentals");
            }

            // Check if rental is in pending status
            if (rental.Status != RentalStatus.Pending)
            {
                return ApiResponse<bool>.CreateFailure("Only pending rentals can be approved");
            }

            // Check for conflicting rentals (in case something changed since creation)
            var hasConflictingRental = await _context.Rentals
                .AnyAsync(r => r.ToolId == rental.ToolId &&
                          r.Id != rental.Id &&
                          r.Status == RentalStatus.Approved &&
                          ((rental.StartDate >= r.StartDate && rental.StartDate <= r.EndDate) ||
                           (rental.EndDate >= r.StartDate && rental.EndDate <= r.EndDate) ||
                           (rental.StartDate <= r.StartDate && rental.EndDate >= r.EndDate)));

            if (hasConflictingRental)
            {
                return ApiResponse<bool>.CreateFailure("Tool has conflicting approved rentals");
            }

            rental.Status = RentalStatus.Approved;
            rental.UpdatedAt = DateTime.UtcNow;
            rental.ApprovedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send approval notification to renter
            try
            {
                var approvalNotification = new RentalApprovedNotification
                {
                    RecipientEmail = rental.Renter.Email!,
                    RecipientName = $"{rental.Renter.FirstName} {rental.Renter.LastName}",
                    UserId = rental.RenterId,
                    RenterName = $"{rental.Renter.FirstName} {rental.Renter.LastName}",
                    OwnerName = $"{rental.Tool.Owner.FirstName} {rental.Tool.Owner.LastName}",
                    OwnerEmail = rental.Tool.Owner.Email!,
                    OwnerPhone = rental.Tool.Owner.PhoneNumber ?? "Not provided",
                    ToolName = rental.Tool.Name,
                    ToolLocation = rental.Tool.Location,
                    StartDate = rental.StartDate,
                    EndDate = rental.EndDate,
                    TotalCost = rental.TotalCost,
                    RentalDetailsUrl = $"/rentals/{rental.Id}",
                    Priority = EmailPriority.High
                };
                
                await _emailNotificationService.SendNotificationAsync(approvalNotification);
            }
            catch (Exception emailEx)
            {
                // Log email error but don't fail the approval
                Console.WriteLine($"Email notification failed: {emailEx.Message}");
            }

            return ApiResponse<bool>.CreateSuccess(true, "Rental approved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.CreateFailure($"Error approving rental: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> RejectRentalAsync(RejectRentalCommand command)
    {
        try
        {
            var rental = await _context.Rentals
                .Include(r => r.Tool)
                    .ThenInclude(t => t.Owner)
                .Include(r => r.Tool.Images)
                .Include(r => r.Renter)
                .FirstOrDefaultAsync(r => r.Id == command.RentalId);

            if (rental == null)
            {
                return ApiResponse<bool>.CreateFailure("Rental not found");
            }

            // Check if the user is the tool owner
            if (rental.Tool.OwnerId != command.OwnerId)
            {
                return ApiResponse<bool>.CreateFailure("Only the tool owner can reject rentals");
            }

            // Check if rental is in pending status
            if (rental.Status != RentalStatus.Pending)
            {
                return ApiResponse<bool>.CreateFailure("Only pending rentals can be rejected");
            }

            rental.Status = RentalStatus.Cancelled;
            rental.UpdatedAt = DateTime.UtcNow;
            rental.CancelledAt = DateTime.UtcNow;
            rental.CancellationReason = command.Reason;

            await _context.SaveChangesAsync();

            // Send rejection notification to renter
            try
            {
                var rejectionNotification = new RentalRejectedNotification
                {
                    RecipientEmail = rental.Renter.Email!,
                    RecipientName = $"{rental.Renter.FirstName} {rental.Renter.LastName}",
                    UserId = rental.RenterId,
                    RenterName = $"{rental.Renter.FirstName} {rental.Renter.LastName}",
                    ToolName = rental.Tool.Name,
                    StartDate = rental.StartDate,
                    EndDate = rental.EndDate,
                    RejectionReason = command.Reason,
                    BrowseToolsUrl = "/tools",
                    Priority = EmailPriority.Normal
                };
                
                await _emailNotificationService.SendNotificationAsync(rejectionNotification);
            }
            catch (Exception emailEx)
            {
                // Log email error but don't fail the rejection
                Console.WriteLine($"Email notification failed: {emailEx.Message}");
            }

            return ApiResponse<bool>.CreateSuccess(true, "Rental rejected successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.CreateFailure($"Error rejecting rental: {ex.Message}");
        }
    }

    private decimal CalculateRentalCost(Tool tool, DateTime startDate, DateTime endDate)
    {
        var totalDays = (endDate - startDate).Days + 1;
        
        // Calculate cost based on duration (prefer longer term rates)
        if (totalDays >= 30 && tool.MonthlyRate.HasValue)
        {
            var months = Math.Ceiling(totalDays / 30.0);
            return (decimal)months * tool.MonthlyRate.Value;
        }
        else if (totalDays >= 7 && tool.WeeklyRate.HasValue)
        {
            var weeks = Math.Ceiling(totalDays / 7.0);
            return (decimal)weeks * tool.WeeklyRate.Value;
        }
        else
        {
            return totalDays * tool.DailyRate;
        }
    }
}