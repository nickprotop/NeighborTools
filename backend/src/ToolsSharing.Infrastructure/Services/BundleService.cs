using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mapster;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.DTOs.Bundle;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services
{
    public class BundleService : IBundleService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentService _paymentService;

        public BundleService(ApplicationDbContext context, IPaymentService paymentService)
        {
            _context = context;
            _paymentService = paymentService;
        }

        public async Task<ApiResponse<BundleDto>> CreateBundleAsync(CreateBundleRequest request, string userId)
        {
            try
            {
                // Validate that all tools exist and belong to the user or are available for bundle creation
                var toolIds = request.Tools.Select(t => t.ToolId).ToList();
                var tools = await _context.Tools
                    .Where(t => toolIds.Contains(t.Id) && !t.IsDeleted)
                    .Include(t => t.Owner)
                    .ToListAsync();

                if (tools.Count != toolIds.Count)
                {
                    return ApiResponse<BundleDto>.CreateFailure("One or more tools not found or not available.");
                }

                // Create the bundle
                var bundle = new Bundle
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Description = request.Description,
                    Guidelines = request.Guidelines,
                    RequiredSkillLevel = request.RequiredSkillLevel,
                    EstimatedProjectDuration = request.EstimatedProjectDuration,
                    ImageUrl = request.ImageUrl,
                    UserId = userId,
                    BundleDiscount = request.BundleDiscount,
                    IsPublished = request.IsPublished,
                    Category = request.Category,
                    Tags = request.Tags
                };

                _context.Bundles.Add(bundle);

                // Add bundle tools
                foreach (var toolRequest in request.Tools)
                {
                    var bundleTool = new BundleTool
                    {
                        Id = Guid.NewGuid(),
                        BundleId = bundle.Id,
                        ToolId = toolRequest.ToolId,
                        UsageNotes = toolRequest.UsageNotes,
                        OrderInBundle = toolRequest.OrderInBundle,
                        IsOptional = toolRequest.IsOptional,
                        QuantityNeeded = toolRequest.QuantityNeeded
                    };

                    _context.BundleTools.Add(bundleTool);
                }

                await _context.SaveChangesAsync();

                // Return the created bundle
                var result = await GetBundleByIdAsync(bundle.Id);
                return result;
            }
            catch (Exception ex)
            {
                return ApiResponse<BundleDto>.CreateFailure($"Error creating bundle: {ex.Message}");
            }
        }

        public async Task<ApiResponse<BundleDto?>> GetBundleByIdAsync(Guid bundleId)
        {
            try
            {
                var bundle = await _context.Bundles
                    .Include(b => b.User)
                    .Include(b => b.BundleTools)
                        .ThenInclude(bt => bt.Tool)
                            .ThenInclude(t => t.Owner)
                    .Where(b => b.Id == bundleId && !b.IsDeleted)
                    .FirstOrDefaultAsync();

                if (bundle == null)
                {
                    return ApiResponse<BundleDto?>.CreateSuccess(null, "Bundle not found");
                }

                var bundleDto = await MapBundleToDto(bundle);
                return ApiResponse<BundleDto?>.CreateSuccess(bundleDto);
            }
            catch (Exception ex)
            {
                return ApiResponse<BundleDto?>.CreateFailure($"Error retrieving bundle: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PagedResult<BundleDto>>> GetBundlesAsync(int page = 1, int pageSize = 20, string? category = null, string? searchTerm = null, bool featuredOnly = false)
        {
            try
            {
                var query = _context.Bundles
                    .Include(b => b.User)
                    .Include(b => b.BundleTools)
                        .ThenInclude(bt => bt.Tool)
                            .ThenInclude(t => t.Owner)
                    .Where(b => b.IsPublished && !b.IsDeleted);

                if (featuredOnly)
                {
                    query = query.Where(b => b.IsFeatured);
                }

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(b => b.Category == category);
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var lowerSearchTerm = searchTerm.ToLower();
                    query = query.Where(b => 
                        b.Name.ToLower().Contains(lowerSearchTerm) ||
                        b.Description.ToLower().Contains(lowerSearchTerm) ||
                        b.Tags.ToLower().Contains(lowerSearchTerm));
                }

                var totalCount = await query.CountAsync();
                var bundles = await query
                    .OrderByDescending(b => b.IsFeatured)
                    .ThenByDescending(b => b.ViewCount)
                    .ThenByDescending(b => b.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var bundleDtos = new List<BundleDto>();
                foreach (var bundle in bundles)
                {
                    bundleDtos.Add(await MapBundleToDto(bundle));
                }

                var result = new PagedResult<BundleDto>
                {
                    Items = bundleDtos,
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                };

                return ApiResponse<PagedResult<BundleDto>>.CreateSuccess(result);
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedResult<BundleDto>>.CreateFailure($"Error retrieving bundles: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<BundleDto>>> GetFeaturedBundlesAsync(int count = 6)
        {
            try
            {
                var bundles = await _context.Bundles
                    .Include(b => b.User)
                    .Include(b => b.BundleTools)
                        .ThenInclude(bt => bt.Tool)
                            .ThenInclude(t => t.Owner)
                    .Where(b => b.IsFeatured && b.IsPublished && !b.IsDeleted)
                    .OrderByDescending(b => b.ViewCount)
                    .Take(count)
                    .ToListAsync();

                var bundleDtos = new List<BundleDto>();
                foreach (var bundle in bundles)
                {
                    bundleDtos.Add(await MapBundleToDto(bundle));
                }

                return ApiResponse<List<BundleDto>>.CreateSuccess(bundleDtos);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<BundleDto>>.CreateFailure($"Error retrieving featured bundles: {ex.Message}");
            }
        }

        public async Task<ApiResponse<BundleAvailabilityResponse>> CheckBundleAvailabilityAsync(BundleAvailabilityRequest request)
        {
            try
            {
                var bundle = await _context.Bundles
                    .Include(b => b.BundleTools)
                        .ThenInclude(bt => bt.Tool)
                    .Where(b => b.Id == request.BundleId && !b.IsDeleted)
                    .FirstOrDefaultAsync();

                if (bundle == null)
                {
                    return ApiResponse<BundleAvailabilityResponse>.CreateFailure("Bundle not found");
                }

                var response = new BundleAvailabilityResponse();
                var toolStatuses = new List<ToolAvailabilityStatus>();
                var allToolsAvailable = true;
                DateTime? earliestAvailableDate = null;

                // Check availability for each tool in the bundle
                foreach (var bundleTool in bundle.BundleTools.Where(bt => !bt.IsDeleted))
                {
                    var toolAvailability = await CheckToolAvailability(bundleTool.Tool, request.StartDate, request.EndDate, bundleTool.QuantityNeeded);
                    
                    toolStatuses.Add(new ToolAvailabilityStatus
                    {
                        ToolId = bundleTool.ToolId,
                        ToolName = bundleTool.Tool.Name,
                        IsAvailable = toolAvailability.IsAvailable,
                        AvailableFromDate = toolAvailability.AvailableFromDate,
                        UnavailabilityReason = toolAvailability.UnavailabilityReason,
                        IsOptional = bundleTool.IsOptional
                    });

                    if (!toolAvailability.IsAvailable)
                    {
                        if (!bundleTool.IsOptional)
                        {
                            allToolsAvailable = false;
                        }

                        if (toolAvailability.AvailableFromDate.HasValue)
                        {
                            if (!earliestAvailableDate.HasValue || toolAvailability.AvailableFromDate.Value > earliestAvailableDate.Value)
                            {
                                earliestAvailableDate = toolAvailability.AvailableFromDate.Value;
                            }
                        }
                    }
                }

                response.IsAvailable = allToolsAvailable;
                response.EarliestAvailableDate = earliestAvailableDate;
                response.ToolAvailability = toolStatuses;

                if (allToolsAvailable)
                {
                    response.Message = "Bundle is available for the requested dates";
                    var costResult = await CalculateBundleCostAsync(request.BundleId, request.StartDate, request.EndDate);
                    if (costResult.Success)
                    {
                        response.CostCalculation = costResult.Data;
                    }
                }
                else
                {
                    var unavailableRequired = toolStatuses.Where(t => !t.IsAvailable && !t.IsOptional).ToList();
                    if (unavailableRequired.Any())
                    {
                        response.Message = $"Bundle is not available. Required tools unavailable: {string.Join(", ", unavailableRequired.Select(t => t.ToolName))}";
                    }
                    else
                    {
                        response.Message = "Bundle is available (some optional tools may be unavailable)";
                    }
                }

                return ApiResponse<BundleAvailabilityResponse>.CreateSuccess(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<BundleAvailabilityResponse>.CreateFailure($"Error checking bundle availability: {ex.Message}");
            }
        }

        public async Task<ApiResponse<BundleCostCalculationResponse>> CalculateBundleCostAsync(Guid bundleId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var bundle = await _context.Bundles
                    .Include(b => b.BundleTools)
                        .ThenInclude(bt => bt.Tool)
                    .Where(b => b.Id == bundleId && !b.IsDeleted)
                    .FirstOrDefaultAsync();

                if (bundle == null)
                {
                    return ApiResponse<BundleCostCalculationResponse>.CreateFailure("Bundle not found");
                }

                var rentalDays = (endDate - startDate).Days + 1;
                var toolCosts = new List<ToolCostBreakdown>();
                decimal totalCost = 0;

                foreach (var bundleTool in bundle.BundleTools.Where(bt => !bt.IsDeleted))
                {
                    var toolCost = bundleTool.Tool.DailyRate * rentalDays * bundleTool.QuantityNeeded;
                    totalCost += toolCost;

                    toolCosts.Add(new ToolCostBreakdown
                    {
                        ToolId = bundleTool.ToolId,
                        ToolName = bundleTool.Tool.Name,
                        DailyRate = bundleTool.Tool.DailyRate,
                        RentalDays = rentalDays,
                        QuantityNeeded = bundleTool.QuantityNeeded,
                        TotalCost = toolCost
                    });
                }

                var bundleDiscountAmount = totalCost * (bundle.BundleDiscount / 100);
                var finalCost = totalCost - bundleDiscountAmount;

                // Calculate platform fee and security deposit using payment service
                var securityDeposit = finalCost * 0.2m; // 20% security deposit
                var platformFee = finalCost * 0.05m; // 5% platform fee
                var grandTotal = finalCost + securityDeposit + platformFee;

                var response = new BundleCostCalculationResponse
                {
                    TotalCost = totalCost,
                    BundleDiscountAmount = bundleDiscountAmount,
                    FinalCost = finalCost,
                    SecurityDeposit = securityDeposit,
                    PlatformFee = platformFee,
                    GrandTotal = grandTotal,
                    ToolCosts = toolCosts
                };

                return ApiResponse<BundleCostCalculationResponse>.CreateSuccess(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<BundleCostCalculationResponse>.CreateFailure($"Error calculating bundle cost: {ex.Message}");
            }
        }

        // Helper methods continue in next part...
        private async Task<BundleDto> MapBundleToDto(Bundle bundle)
        {
            var bundleDto = bundle.Adapt<BundleDto>();
            
            // Map owner information
            bundleDto.OwnerName = bundle.User.UserName ?? bundle.User.Email ?? "";
            bundleDto.OwnerLocation = bundle.User.City ?? "";
            
            // Parse tags
            if (!string.IsNullOrEmpty(bundle.Tags))
            {
                bundleDto.Tags = bundle.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim()).ToList();
            }

            // Map bundle tools
            bundleDto.Tools = bundle.BundleTools
                .Where(bt => !bt.IsDeleted)
                .OrderBy(bt => bt.OrderInBundle)
                .Select(bt => new BundleToolDto
                {
                    Id = bt.Id,
                    ToolId = bt.ToolId,
                    ToolName = bt.Tool.Name,
                    ToolDescription = bt.Tool.Description,
                    ToolImageUrl = bt.Tool.Images.FirstOrDefault()?.ImageUrl,
                    DailyRate = bt.Tool.DailyRate,
                    UsageNotes = bt.UsageNotes,
                    OrderInBundle = bt.OrderInBundle,
                    IsOptional = bt.IsOptional,
                    QuantityNeeded = bt.QuantityNeeded,
                    OwnerName = bt.Tool.Owner.UserName ?? bt.Tool.Owner.Email ?? "",
                    OwnerId = bt.Tool.OwnerId
                }).ToList();

            // Calculate costs
            var totalCost = bundleDto.Tools.Sum(t => t.DailyRate * t.QuantityNeeded);
            var discountAmount = totalCost * (bundle.BundleDiscount / 100);
            bundleDto.TotalCost = totalCost;
            bundleDto.DiscountedCost = totalCost - discountAmount;

            // Get rental count from bundle rentals
            bundleDto.RentalCount = await _context.BundleRentals
                .Where(br => br.BundleId == bundle.Id && !br.IsDeleted)
                .CountAsync();

            return bundleDto;
        }

        private async Task<(bool IsAvailable, DateTime? AvailableFromDate, string UnavailabilityReason)> CheckToolAvailability(
            Tool tool, DateTime startDate, DateTime endDate, int quantityNeeded)
        {
            // Check if tool is available and not deleted
            if (tool.IsDeleted || !tool.IsAvailable)
            {
                return (false, null, "Tool is not available");
            }

            // Check for overlapping rentals
            var overlappingRentals = await _context.Rentals
                .Where(r => r.ToolId == tool.Id 
                    && !r.IsDeleted
                    && r.Status != RentalStatus.Cancelled 
                    && r.Status != RentalStatus.Rejected
                    && r.Status != RentalStatus.Completed
                    && !(endDate < r.StartDate || startDate > r.EndDate))
                .CountAsync();

            if (overlappingRentals >= quantityNeeded)
            {
                // Find the next available date
                var nextAvailableDate = await GetNextAvailableDate(tool.Id, startDate, quantityNeeded);
                return (false, nextAvailableDate, "Tool is already rented for this period");
            }

            return (true, null, "");
        }

        private async Task<DateTime?> GetNextAvailableDate(Guid toolId, DateTime fromDate, int quantityNeeded)
        {
            var futureRentals = await _context.Rentals
                .Where(r => r.ToolId == toolId 
                    && !r.IsDeleted
                    && r.Status != RentalStatus.Cancelled 
                    && r.Status != RentalStatus.Rejected
                    && r.Status != RentalStatus.Completed
                    && r.EndDate >= fromDate)
                .OrderBy(r => r.EndDate)
                .ToListAsync();

            if (!futureRentals.Any())
            {
                return fromDate;
            }

            // Find the earliest date after the last rental
            return futureRentals.Last().EndDate.AddDays(1);
        }

        // Implement remaining interface methods...
        public async Task<ApiResponse<BundleDto>> UpdateBundleAsync(Guid bundleId, CreateBundleRequest request, string userId)
        {
            // Implementation for update bundle
            throw new NotImplementedException("UpdateBundleAsync not yet implemented");
        }

        public async Task<ApiResponse<bool>> DeleteBundleAsync(Guid bundleId, string userId)
        {
            // Implementation for delete bundle
            throw new NotImplementedException("DeleteBundleAsync not yet implemented");
        }

        public async Task<ApiResponse<PagedResult<BundleDto>>> GetUserBundlesAsync(string userId, int page = 1, int pageSize = 20)
        {
            // Implementation for get user bundles
            throw new NotImplementedException("GetUserBundlesAsync not yet implemented");
        }

        public async Task<ApiResponse<bool>> SetFeaturedStatusAsync(Guid bundleId, bool isFeatured, string adminUserId)
        {
            // Implementation for set featured status
            throw new NotImplementedException("SetFeaturedStatusAsync not yet implemented");
        }

        public async Task<ApiResponse<BundleRentalDto>> CreateBundleRentalAsync(CreateBundleRentalRequest request, string userId)
        {
            try
            {
                // Validate bundle exists and is published
                var bundle = await _context.Bundles
                    .Include(b => b.User)
                    .Include(b => b.BundleTools)
                        .ThenInclude(bt => bt.Tool)
                            .ThenInclude(t => t.Owner)
                    .FirstOrDefaultAsync(b => b.Id == request.BundleId && !b.IsDeleted && b.IsPublished);

                if (bundle == null)
                {
                    return ApiResponse<BundleRentalDto>.CreateFailure("Bundle not found or is not available");
                }

                // Validate user exists
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return ApiResponse<BundleRentalDto>.CreateFailure("User not found");
                }

                // Check if user is trying to rent their own bundle
                if (bundle.UserId == userId)
                {
                    return ApiResponse<BundleRentalDto>.CreateFailure("You cannot rent your own bundle");
                }

                // Validate rental dates
                if (request.RentalDate >= request.ReturnDate)
                {
                    return ApiResponse<BundleRentalDto>.CreateFailure("Return date must be after rental date");
                }

                if (request.RentalDate < DateTime.UtcNow.Date)
                {
                    return ApiResponse<BundleRentalDto>.CreateFailure("Rental date cannot be in the past");
                }

                // Check bundle availability for the requested dates
                var availabilityResult = await CheckBundleAvailabilityAsync(new BundleAvailabilityRequest
                {
                    BundleId = request.BundleId,
                    StartDate = request.RentalDate,
                    EndDate = request.ReturnDate
                });

                if (!availabilityResult.Success || availabilityResult.Data?.IsAvailable != true)
                {
                    return ApiResponse<BundleRentalDto>.CreateFailure(
                        availabilityResult.Data?.Message ?? "Bundle is not available for the selected dates");
                }

                // Calculate bundle rental costs
                var costCalculation = await CalculateBundleCostAsync(request.BundleId, request.RentalDate, request.ReturnDate);
                if (!costCalculation.Success || costCalculation.Data == null)
                {
                    return ApiResponse<BundleRentalDto>.CreateFailure("Failed to calculate bundle rental cost");
                }

                // Create bundle rental entity
                var bundleRental = new BundleRental
                {
                    Id = Guid.NewGuid(),
                    BundleId = request.BundleId,
                    RenterUserId = userId,
                    RentalDate = request.RentalDate,
                    ReturnDate = request.ReturnDate,
                    TotalCost = costCalculation.Data.TotalCost,
                    BundleDiscountAmount = costCalculation.Data.BundleDiscountAmount,
                    FinalCost = costCalculation.Data.FinalCost,
                    Status = "Pending",
                    RenterNotes = request.RenterNotes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.BundleRentals.Add(bundleRental);
                await _context.SaveChangesAsync();

                // Map to DTO
                var bundleRentalDto = new BundleRentalDto
                {
                    Id = bundleRental.Id,
                    BundleId = bundleRental.BundleId,
                    BundleName = bundle.Name,
                    RenterUserId = bundleRental.RenterUserId,
                    RenterName = $"{user.FirstName} {user.LastName}",
                    RentalDate = bundleRental.RentalDate,
                    ReturnDate = bundleRental.ReturnDate,
                    TotalCost = bundleRental.TotalCost,
                    BundleDiscountAmount = bundleRental.BundleDiscountAmount,
                    FinalCost = bundleRental.FinalCost,
                    Status = bundleRental.Status,
                    RenterNotes = bundleRental.RenterNotes,
                    OwnerNotes = bundleRental.OwnerNotes,
                    CreatedAt = bundleRental.CreatedAt,
                    UpdatedAt = bundleRental.UpdatedAt
                };

                return ApiResponse<BundleRentalDto>.CreateSuccess(bundleRentalDto, "Bundle rental request created successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<BundleRentalDto>.CreateFailure($"Failed to create bundle rental: {ex.Message}");
            }
        }

        public async Task<ApiResponse<BundleRentalDto?>> GetBundleRentalByIdAsync(Guid rentalId)
        {
            try
            {
                var bundleRental = await _context.BundleRentals
                    .Include(br => br.Bundle)
                    .Include(br => br.RenterUser)
                    .Include(br => br.ToolRentals)
                        .ThenInclude(r => r.Tool)
                            .ThenInclude(t => t.Owner)
                    .FirstOrDefaultAsync(br => br.Id == rentalId && !br.IsDeleted);

                if (bundleRental == null)
                {
                    return ApiResponse<BundleRentalDto?>.CreateSuccess(null, "Bundle rental not found");
                }

                // Map to DTO
                var bundleRentalDto = new BundleRentalDto
                {
                    Id = bundleRental.Id,
                    BundleId = bundleRental.BundleId,
                    BundleName = bundleRental.Bundle?.Name ?? "",
                    RenterUserId = bundleRental.RenterUserId,
                    RenterName = bundleRental.RenterUser != null ? $"{bundleRental.RenterUser.FirstName} {bundleRental.RenterUser.LastName}" : "",
                    RentalDate = bundleRental.RentalDate,
                    ReturnDate = bundleRental.ReturnDate,
                    TotalCost = bundleRental.TotalCost,
                    BundleDiscountAmount = bundleRental.BundleDiscountAmount,
                    FinalCost = bundleRental.FinalCost,
                    Status = bundleRental.Status,
                    RenterNotes = bundleRental.RenterNotes,
                    OwnerNotes = bundleRental.OwnerNotes,
                    CreatedAt = bundleRental.CreatedAt,
                    UpdatedAt = bundleRental.UpdatedAt,
                    ToolRentals = bundleRental.ToolRentals.Select(r => new BundleToolRentalDto
                    {
                        RentalId = r.Id,
                        ToolId = r.ToolId,
                        ToolName = r.Tool?.Name ?? "",
                        OwnerName = r.Tool?.Owner != null ? $"{r.Tool.Owner.FirstName} {r.Tool.Owner.LastName}" : "",
                        Status = r.Status.ToString(),
                        Cost = r.TotalCost
                    }).ToList()
                };

                return ApiResponse<BundleRentalDto?>.CreateSuccess(bundleRentalDto, "Bundle rental retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<BundleRentalDto?>.CreateFailure($"Failed to get bundle rental: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PagedResult<BundleRentalDto>>> GetUserBundleRentalsAsync(string userId, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.BundleRentals
                    .Include(br => br.Bundle)
                    .Include(br => br.RenterUser)
                    .Include(br => br.ToolRentals)
                        .ThenInclude(r => r.Tool)
                            .ThenInclude(t => t.Owner)
                    .Where(br => !br.IsDeleted && br.RenterUserId == userId)
                    .OrderByDescending(br => br.CreatedAt);

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var rentals = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var rentalDtos = rentals.Select(bundleRental => new BundleRentalDto
                {
                    Id = bundleRental.Id,
                    BundleId = bundleRental.BundleId,
                    BundleName = bundleRental.Bundle?.Name ?? "",
                    RenterUserId = bundleRental.RenterUserId,
                    RenterName = bundleRental.RenterUser != null ? $"{bundleRental.RenterUser.FirstName} {bundleRental.RenterUser.LastName}" : "",
                    RentalDate = bundleRental.RentalDate,
                    ReturnDate = bundleRental.ReturnDate,
                    TotalCost = bundleRental.TotalCost,
                    BundleDiscountAmount = bundleRental.BundleDiscountAmount,
                    FinalCost = bundleRental.FinalCost,
                    Status = bundleRental.Status,
                    RenterNotes = bundleRental.RenterNotes,
                    OwnerNotes = bundleRental.OwnerNotes,
                    CreatedAt = bundleRental.CreatedAt,
                    UpdatedAt = bundleRental.UpdatedAt,
                    ToolRentals = bundleRental.ToolRentals.Select(r => new BundleToolRentalDto
                    {
                        RentalId = r.Id,
                        ToolId = r.ToolId,
                        ToolName = r.Tool?.Name ?? "",
                        OwnerName = r.Tool?.Owner != null ? $"{r.Tool.Owner.FirstName} {r.Tool.Owner.LastName}" : "",
                        Status = r.Status.ToString(),
                        Cost = r.TotalCost
                    }).ToList()
                }).ToList();

                var pagedResult = new PagedResult<BundleRentalDto>
                {
                    Items = rentalDtos,
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };

                return ApiResponse<PagedResult<BundleRentalDto>>.CreateSuccess(pagedResult, "Bundle rentals retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedResult<BundleRentalDto>>.CreateFailure($"Failed to get user bundle rentals: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> ApproveBundleRentalAsync(Guid rentalId, string userId)
        {
            try
            {
                var bundleRental = await _context.BundleRentals
                    .Include(br => br.Bundle)
                        .ThenInclude(b => b.BundleTools)
                            .ThenInclude(bt => bt.Tool)
                    .FirstOrDefaultAsync(br => br.Id == rentalId && !br.IsDeleted);

                if (bundleRental == null)
                {
                    return ApiResponse<bool>.CreateFailure("Bundle rental not found");
                }

                // Verify that the user owns all tools in the bundle
                var userOwnsAllTools = bundleRental.Bundle.BundleTools.All(bt => bt.Tool.OwnerId == userId);
                if (!userOwnsAllTools)
                {
                    return ApiResponse<bool>.CreateFailure("You can only approve bundle rentals for your own tools");
                }

                if (bundleRental.Status != "Pending")
                {
                    return ApiResponse<bool>.CreateFailure("Bundle rental is not pending approval");
                }

                // Update status
                bundleRental.Status = "Approved";
                bundleRental.UpdatedAt = DateTime.UtcNow;

                _context.BundleRentals.Update(bundleRental);
                await _context.SaveChangesAsync();

                return ApiResponse<bool>.CreateSuccess(true, "Bundle rental approved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.CreateFailure($"Failed to approve bundle rental: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> RejectBundleRentalAsync(Guid rentalId, string userId, string reason)
        {
            try
            {
                var bundleRental = await _context.BundleRentals
                    .Include(br => br.Bundle)
                        .ThenInclude(b => b.BundleTools)
                            .ThenInclude(bt => bt.Tool)
                    .FirstOrDefaultAsync(br => br.Id == rentalId && !br.IsDeleted);

                if (bundleRental == null)
                {
                    return ApiResponse<bool>.CreateFailure("Bundle rental not found");
                }

                // Verify that the user owns all tools in the bundle
                var userOwnsAllTools = bundleRental.Bundle.BundleTools.All(bt => bt.Tool.OwnerId == userId);
                if (!userOwnsAllTools)
                {
                    return ApiResponse<bool>.CreateFailure("You can only reject bundle rentals for your own tools");
                }

                if (bundleRental.Status != "Pending")
                {
                    return ApiResponse<bool>.CreateFailure("Bundle rental is not pending approval");
                }

                // Update status and add reason
                bundleRental.Status = "Rejected";
                bundleRental.OwnerNotes = reason;
                bundleRental.UpdatedAt = DateTime.UtcNow;

                _context.BundleRentals.Update(bundleRental);
                await _context.SaveChangesAsync();

                return ApiResponse<bool>.CreateSuccess(true, "Bundle rental rejected successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.CreateFailure($"Failed to reject bundle rental: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> CancelBundleRentalAsync(Guid rentalId, string userId)
        {
            try
            {
                var bundleRental = await _context.BundleRentals
                    .FirstOrDefaultAsync(br => br.Id == rentalId && !br.IsDeleted);

                if (bundleRental == null)
                {
                    return ApiResponse<bool>.CreateFailure("Bundle rental not found");
                }

                // Verify that the user is the renter
                if (bundleRental.RenterUserId != userId)
                {
                    return ApiResponse<bool>.CreateFailure("You can only cancel your own bundle rentals");
                }

                if (bundleRental.Status == "Cancelled")
                {
                    return ApiResponse<bool>.CreateFailure("Bundle rental is already cancelled");
                }

                if (bundleRental.Status == "Completed")
                {
                    return ApiResponse<bool>.CreateFailure("Cannot cancel completed bundle rental");
                }

                // Update status
                bundleRental.Status = "Cancelled";
                bundleRental.UpdatedAt = DateTime.UtcNow;

                _context.BundleRentals.Update(bundleRental);
                await _context.SaveChangesAsync();

                return ApiResponse<bool>.CreateSuccess(true, "Bundle rental cancelled successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.CreateFailure($"Failed to cancel bundle rental: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> IncrementViewCountAsync(Guid bundleId)
        {
            // Implementation for increment view count
            throw new NotImplementedException("IncrementViewCountAsync not yet implemented");
        }

        public async Task<ApiResponse<Dictionary<string, int>>> GetBundleCategoryCountsAsync()
        {
            // Implementation for get bundle category counts
            throw new NotImplementedException("GetBundleCategoryCountsAsync not yet implemented");
        }
    }
}