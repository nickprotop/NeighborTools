using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Features.Rentals;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Features.Rentals;

public class RentalsService : IRentalsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public RentalsService(IUnitOfWork unitOfWork, IMapper mapper, ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _context = context;
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

            // Calculate rental costs
            var rentalDays = (command.EndDate - command.StartDate).Days + 1;
            var totalCost = CalculateRentalCost(tool, command.StartDate, command.EndDate);
            var depositAmount = tool.DepositRequired;

            var rental = new Rental
            {
                Id = Guid.NewGuid(),
                ToolId = command.ToolId,
                RenterId = command.RenterId,
                StartDate = command.StartDate,
                EndDate = command.EndDate,
                TotalCost = totalCost,
                DepositAmount = depositAmount,
                Status = RentalStatus.Pending,
                Notes = command.Notes,
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

            var rentalDto = _mapper.Map<RentalDto>(createdRental);
            return ApiResponse<RentalDto>.CreateSuccess(rentalDto, "Rental request created successfully");
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