using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mapster;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.DTOs.Bundle;
using ToolsSharing.Core.DTOs.Location;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services
{
    public class BundleService : IBundleService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IGeocodingService _geocodingService;
        private readonly ILogger<BundleService> _logger;

        public BundleService(
            ApplicationDbContext context, 
            IPaymentService paymentService,
            IFileStorageService fileStorageService,
            IGeocodingService geocodingService,
            ILogger<BundleService> logger)
        {
            _context = context;
            _paymentService = paymentService;
            _fileStorageService = fileStorageService;
            _geocodingService = geocodingService;
            _logger = logger;
        }

        public async Task<ApiResponse<BundleDto>> CreateBundleAsync(CreateBundleRequest request, string userId)
        {
            try
            {
                // Get the user for location inheritance
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return ApiResponse<BundleDto>.CreateFailure("User not found");
                }

                // SECURITY: Validate that all tools exist and belong to the user
                var toolIds = request.Tools.Select(t => t.ToolId).ToList();
                var tools = await _context.Tools
                    .Where(t => toolIds.Contains(t.Id) && !t.IsDeleted && t.OwnerId == userId)
                    .Include(t => t.Owner)
                    .ToListAsync();

                if (tools.Count != toolIds.Count)
                {
                    return ApiResponse<BundleDto>.CreateFailure("One or more tools not found or you can only create bundles with your own tools.");
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
                    Tags = request.Tags,
                    // Approval fields - new bundles require approval
                    IsApproved = false,
                    PendingApproval = true
                };

                // Handle location inheritance (Phase 7 - TRUE INHERITANCE)
                // Store the inheritance choice, location will be resolved at query time
                bundle.LocationInheritanceOption = request.LocationSource;
                
                if (request.LocationSource == Core.Enums.LocationInheritanceOption.CustomLocation && request.CustomLocation != null)
                {
                    // Only store location data for custom locations
                    bundle.LocationDisplay = request.CustomLocation.LocationDisplay;
                    bundle.LocationArea = request.CustomLocation.LocationArea;
                    bundle.LocationCity = request.CustomLocation.LocationCity;
                    bundle.LocationState = request.CustomLocation.LocationState;
                    bundle.LocationCountry = request.CustomLocation.LocationCountry;
                    bundle.LocationLat = request.CustomLocation.LocationLat;
                    bundle.LocationLng = request.CustomLocation.LocationLng;
                    bundle.LocationPrecisionRadius = request.CustomLocation.LocationPrecisionRadius;
                    bundle.LocationSource = request.CustomLocation.LocationSource ?? Core.Enums.LocationSource.Manual;
                    bundle.LocationPrivacyLevel = request.CustomLocation.LocationPrivacyLevel;
                    bundle.LocationUpdatedAt = DateTime.UtcNow;
                }
                // For InheritFromProfile: leave location fields null/empty, will be resolved at query time from User profile

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

        public async Task<ApiResponse<PagedResult<BundleDto>>> GetBundlesAsync(int page = 1, int pageSize = 20, string? category = null, string? searchTerm = null, bool featuredOnly = false, string? tags = null, LocationSearchRequest? locationSearch = null)
        {
            try
            {
                var query = _context.Bundles
                    .Include(b => b.User)
                    .Include(b => b.BundleTools)
                        .ThenInclude(bt => bt.Tool)
                            .ThenInclude(t => t.Owner)
                    .Where(b => b.IsPublished && b.IsApproved && !b.IsDeleted)
                    // SAFEGUARD: Only show bundles where ALL tools are approved
                    .Where(b => !b.BundleTools.Any() || b.BundleTools.All(bt => bt.Tool.IsApproved));

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

                // Apply tags filter
                if (!string.IsNullOrEmpty(tags))
                {
                    var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(tag => tag.Trim().ToLower())
                        .Where(tag => !string.IsNullOrEmpty(tag))
                        .ToList();

                    if (tagList.Any())
                    {
                        query = query.Where(b => 
                            tagList.All(tag => b.Tags.ToLower().Contains(tag)));
                    }
                }

                // Apply location-based filtering
                if (locationSearch != null && 
                    (!string.IsNullOrEmpty(locationSearch.LocationQuery) || 
                     locationSearch.Lat.HasValue || 
                     locationSearch.Lng.HasValue))
                {
                    query = await ApplyBundleLocationFilterAsync(query, locationSearch);
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

        public async Task<ApiResponse<PagedResult<BundleDto>>> SearchBundlesAsync(SearchBundlesQuery query)
        {
            try
            {
                var bundleQuery = _context.Bundles
                    .Include(b => b.User)
                    .Include(b => b.BundleTools)
                        .ThenInclude(bt => bt.Tool)
                            .ThenInclude(t => t.Owner)
                    .Where(b => b.IsPublished && b.IsApproved && !b.IsDeleted)
                    // SAFEGUARD: Only show bundles where ALL tools are approved
                    .Where(b => !b.BundleTools.Any() || b.BundleTools.All(bt => bt.Tool.IsApproved));

                // Apply search filters
                if (!string.IsNullOrEmpty(query.Query))
                {
                    var lowerSearchTerm = query.Query.ToLower();
                    bundleQuery = bundleQuery.Where(b => 
                        b.Name.ToLower().Contains(lowerSearchTerm) ||
                        b.Description.ToLower().Contains(lowerSearchTerm) ||
                        b.Tags.ToLower().Contains(lowerSearchTerm));
                }

                if (!string.IsNullOrEmpty(query.Category))
                {
                    bundleQuery = bundleQuery.Where(b => b.Category == query.Category);
                }

                if (!string.IsNullOrEmpty(query.Tags))
                {
                    var tagList = query.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(tag => tag.Trim().ToLower())
                        .Where(tag => !string.IsNullOrEmpty(tag))
                        .ToList();

                    if (tagList.Any())
                    {
                        bundleQuery = bundleQuery.Where(b => 
                            tagList.Any(tag => b.Tags.ToLower().Contains(tag)));
                    }
                }

                // Apply availability filtering based on constituent tools
                if (query.IsAvailable.HasValue)
                {
                    if (query.IsAvailable.Value)
                    {
                        // Bundle is available only if ALL constituent tools are available
                        var availableBundleIds = (from b in _context.Bundles
                                                where b.BundleTools.Any() && // Only bundles with tools
                                                      b.BundleTools.All(bt => bt.Tool.IsAvailable && bt.Tool.IsApproved && !bt.Tool.IsDeleted)
                                                select b.Id);

                        bundleQuery = bundleQuery.Where(b => availableBundleIds.Contains(b.Id));
                    }
                    else
                    {
                        // Bundle is NOT available if ANY constituent tool is unavailable
                        var unavailableBundleIds = (from b in _context.Bundles
                                                   where b.BundleTools.Any() && // Only bundles with tools
                                                         b.BundleTools.Any(bt => !bt.Tool.IsAvailable || !bt.Tool.IsApproved || bt.Tool.IsDeleted)
                                                   select b.Id);

                        bundleQuery = bundleQuery.Where(b => unavailableBundleIds.Contains(b.Id));
                    }
                }
                
                if (query.IsFeatured.HasValue)
                {
                    bundleQuery = bundleQuery.Where(b => b.IsFeatured == query.IsFeatured.Value);
                }

                // Apply price filtering based on discounted bundle cost
                if (query.MinPrice.HasValue || query.MaxPrice.HasValue)
                {
                    // Calculate effective bundle price: sum of tool daily rates with bundle discount applied
                    var bundleIdsWithPriceFilter = (from b in _context.Bundles
                                                   where b.BundleTools.Any() // Only bundles with tools
                                                   let totalToolCost = b.BundleTools.Sum(bt => bt.Tool.DailyRate * bt.QuantityNeeded)
                                                   let discountAmount = totalToolCost * (b.BundleDiscount / 100m)
                                                   let finalCost = totalToolCost - discountAmount
                                                   where (!query.MinPrice.HasValue || finalCost >= query.MinPrice.Value) &&
                                                         (!query.MaxPrice.HasValue || finalCost <= query.MaxPrice.Value)
                                                   select b.Id);

                    bundleQuery = bundleQuery.Where(b => bundleIdsWithPriceFilter.Contains(b.Id));
                }

                // Apply rating filtering based on bundle reviews
                if (query.MinRating.HasValue)
                {
                    // Calculate average bundle rating from reviews
                    var bundleIdsWithRatingFilter = (from b in _context.Bundles
                                                    join r in _context.Reviews on b.Id equals r.BundleId into reviews
                                                    from review in reviews.DefaultIfEmpty()
                                                    where review == null || review.Type == Core.Entities.ReviewType.BundleReview
                                                    group review by b.Id into bundleReviews
                                                    let averageRating = bundleReviews.Where(r => r != null).Any() 
                                                        ? bundleReviews.Where(r => r != null).Average(r => r.Rating) 
                                                        : 0
                                                    where averageRating >= (double)query.MinRating.Value
                                                    select bundleReviews.Key);

                    bundleQuery = bundleQuery.Where(b => bundleIdsWithRatingFilter.Contains(b.Id));
                }

                // Apply location-based filtering
                if (query.LocationSearch != null && 
                    (!string.IsNullOrEmpty(query.LocationSearch.LocationQuery) || 
                     query.LocationSearch.Lat.HasValue || 
                     query.LocationSearch.Lng.HasValue))
                {
                    bundleQuery = await ApplyBundleLocationFilterAsync(bundleQuery, query.LocationSearch);
                }

                // Get bundle IDs list for potential sorting after materialization  
                var totalCount = await bundleQuery.CountAsync();
                var bundles = await bundleQuery
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                // Apply client-side sorting for complex calculations that can't be translated to SQL
                switch (query.SortBy?.ToLower())
                {
                    case "price_low":
                        bundles = bundles
                            .Where(b => b.BundleTools.Any())
                            .OrderBy(b => {
                                var totalToolCost = b.BundleTools.Sum(bt => bt.Tool.DailyRate * bt.QuantityNeeded);
                                var discountAmount = totalToolCost * (b.BundleDiscount / 100m);
                                return totalToolCost - discountAmount;
                            })
                            .ToList();
                        break;

                    case "price_high":
                        bundles = bundles
                            .Where(b => b.BundleTools.Any())
                            .OrderByDescending(b => {
                                var totalToolCost = b.BundleTools.Sum(bt => bt.Tool.DailyRate * bt.QuantityNeeded);
                                var discountAmount = totalToolCost * (b.BundleDiscount / 100m);
                                return totalToolCost - discountAmount;
                            })
                            .ToList();
                        break;

                    case "rating":
                        // Calculate rating on client side - this could be optimized with a computed column
                        bundles = bundles
                            .OrderByDescending(b => {
                                var reviews = _context.Reviews
                                    .Where(r => r.BundleId == b.Id && r.Type == Core.Entities.ReviewType.BundleReview)
                                    .ToList();
                                return reviews.Any() ? reviews.Average(r => r.Rating) : 0;
                            })
                            .ThenByDescending(b => {
                                var reviewCount = _context.Reviews
                                    .Count(r => r.BundleId == b.Id && r.Type == Core.Entities.ReviewType.BundleReview);
                                return reviewCount;
                            })
                            .ToList();
                        break;

                    case "newest":
                        bundles = bundles.OrderByDescending(b => b.CreatedAt).ToList();
                        break;

                    case "popular":
                        bundles = bundles.OrderByDescending(b => b.ViewCount).ToList();
                        break;

                    default:
                        bundles = bundles
                            .OrderByDescending(b => b.IsFeatured)
                            .ThenByDescending(b => b.ViewCount)
                            .ThenByDescending(b => b.CreatedAt)
                            .ToList();
                        break;
                }

                var bundleDtos = new List<BundleDto>();
                foreach (var bundle in bundles)
                {
                    bundleDtos.Add(await MapBundleToDto(bundle));
                }

                var result = new PagedResult<BundleDto>
                {
                    Items = bundleDtos,
                    TotalCount = totalCount,
                    PageNumber = query.Page,
                    PageSize = query.PageSize
                };

                return ApiResponse<PagedResult<BundleDto>>.CreateSuccess(result);
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedResult<BundleDto>>.CreateFailure($"Error searching bundles: {ex.Message}");
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

        public async Task<ApiResponse<List<BundleDto>>> GetPopularBundlesAsync(int count = 6)
        {
            try
            {
                var bundles = await _context.Bundles
                    .Include(b => b.User)
                    .Include(b => b.BundleTools)
                        .ThenInclude(bt => bt.Tool)
                            .ThenInclude(t => t.Owner)
                    .Where(b => b.IsPublished && !b.IsDeleted)
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
                return ApiResponse<List<BundleDto>>.CreateFailure($"Error retrieving popular bundles: {ex.Message}");
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

        private async Task<(bool IsAvailable, DateTime? EarliestAvailableDate)> CheckBundleAvailabilityForPeriod(Guid bundleId, DateTime startDate, DateTime endDate)
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
                    return (false, null);
                }

                var allToolsAvailable = true;
                DateTime? earliestAvailableDate = null;

                // Check availability for each required tool in the bundle
                foreach (var bundleTool in bundle.BundleTools.Where(bt => !bt.IsDeleted && !bt.IsOptional))
                {
                    var toolAvailability = await CheckToolAvailability(bundleTool.Tool, startDate, endDate, bundleTool.QuantityNeeded);
                    
                    if (!toolAvailability.IsAvailable)
                    {
                        allToolsAvailable = false;
                        if (toolAvailability.AvailableFromDate.HasValue)
                        {
                            if (!earliestAvailableDate.HasValue || toolAvailability.AvailableFromDate.Value > earliestAvailableDate.Value)
                            {
                                earliestAvailableDate = toolAvailability.AvailableFromDate.Value;
                            }
                        }
                    }
                }

                return (allToolsAvailable, earliestAvailableDate);
            }
            catch
            {
                return (false, null);
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

                // Use payment service for proper calculations with configurable rates
                // For bundles, we need to consider the bundle owner (who owns all tools in bundle)
                var bundleOwnerId = bundle.UserId;
                
                // Calculate security deposit (standard 20% for now, could be configurable later)
                var securityDeposit = finalCost * 0.2m;
                
                // Use payment service to calculate commission properly
                var financialBreakdown = _paymentService.CalculateRentalFinancials(finalCost, securityDeposit, bundleOwnerId);
                var grandTotal = financialBreakdown.TotalPayerAmount;

                var response = new BundleCostCalculationResponse
                {
                    TotalCost = totalCost,
                    BundleDiscountAmount = bundleDiscountAmount,
                    FinalCost = finalCost,
                    SecurityDeposit = financialBreakdown.SecurityDeposit,
                    PlatformFee = financialBreakdown.CommissionAmount,
                    GrandTotal = financialBreakdown.TotalPayerAmount,
                    ToolCosts = toolCosts,
                    CommissionRate = financialBreakdown.CommissionRate,
                    OwnerPayoutAmount = financialBreakdown.OwnerPayoutAmount
                };

                return ApiResponse<BundleCostCalculationResponse>.CreateSuccess(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<BundleCostCalculationResponse>.CreateFailure($"Error calculating bundle cost: {ex.Message}");
            }
        }

        private async Task<ApiResponse<BundleCostCalculationResponse>> CalculateBundleCostForSelectedToolsAsync(
            Bundle bundle, List<BundleTool> selectedTools, DateTime startDate, DateTime endDate)
        {
            try
            {
                var rentalDays = (endDate - startDate).Days + 1;
                var toolCosts = new List<ToolCostBreakdown>();
                decimal totalCost = 0;

                foreach (var bundleTool in selectedTools.Where(bt => !bt.IsDeleted))
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

                // Apply bundle discount
                var bundleDiscountAmount = totalCost * (bundle.BundleDiscount / 100m);
                var finalCost = totalCost - bundleDiscountAmount;

                // Get bundle owner for commission calculation
                var bundleOwnerId = bundle.UserId;

                // Calculate security deposit (standard 20% for now, could be configurable later)
                var securityDeposit = finalCost * 0.2m;
                
                // Use payment service to calculate commission properly
                var financialBreakdown = _paymentService.CalculateRentalFinancials(finalCost, securityDeposit, bundleOwnerId);

                var response = new BundleCostCalculationResponse
                {
                    TotalCost = totalCost,
                    BundleDiscountAmount = bundleDiscountAmount,
                    FinalCost = finalCost,
                    SecurityDeposit = financialBreakdown.SecurityDeposit,
                    PlatformFee = financialBreakdown.CommissionAmount,
                    GrandTotal = financialBreakdown.TotalPayerAmount,
                    ToolCosts = toolCosts,
                    CommissionRate = financialBreakdown.CommissionRate,
                    OwnerPayoutAmount = financialBreakdown.OwnerPayoutAmount
                };

                return ApiResponse<BundleCostCalculationResponse>.CreateSuccess(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<BundleCostCalculationResponse>.CreateFailure($"Error calculating bundle cost for selected tools: {ex.Message}");
            }
        }

        // Helper methods continue in next part...
        private async Task<BundleDto> MapBundleToDto(Bundle bundle)
        {
            // Manually map all properties to avoid Mapster casting issue with Tags
            var bundleDto = new BundleDto
            {
                Id = bundle.Id,
                Name = bundle.Name,
                Description = bundle.Description,
                Guidelines = bundle.Guidelines,
                RequiredSkillLevel = bundle.RequiredSkillLevel,
                EstimatedProjectDuration = bundle.EstimatedProjectDuration,
                ImageUrl = bundle.ImageUrl,
                UserId = bundle.UserId,
                BundleDiscount = bundle.BundleDiscount,
                IsPublished = bundle.IsPublished,
                IsFeatured = bundle.IsFeatured,
                ViewCount = bundle.ViewCount,
                Category = bundle.Category,
                IsApproved = bundle.IsApproved,
                PendingApproval = bundle.PendingApproval,
                RejectionReason = bundle.RejectionReason,
                CreatedAt = bundle.CreatedAt,
                UpdatedAt = bundle.UpdatedAt
            };
            
            // Map owner information
            bundleDto.OwnerName = bundle.User.UserName ?? bundle.User.Email ?? "";
            
            // Phase 7 - Location Inheritance System: Runtime location resolution
            LocationDto enhancedLocation;
            if (bundle.LocationInheritanceOption == Core.Enums.LocationInheritanceOption.InheritFromProfile && bundle.User != null)
            {
                // TRUE INHERITANCE: Resolve location from User profile at runtime
                enhancedLocation = new LocationDto
                {
                    LocationDisplay = bundle.User.LocationDisplay ?? "",
                    LocationArea = bundle.User.LocationArea ?? "",
                    LocationCity = bundle.User.LocationCity ?? "",
                    LocationState = bundle.User.LocationState ?? "",
                    LocationCountry = bundle.User.LocationCountry ?? "",
                    LocationLat = bundle.User.LocationLat,
                    LocationLng = bundle.User.LocationLng,
                    LocationPrecisionRadius = bundle.User.LocationPrecisionRadius,
                    LocationSource = bundle.User.LocationSource,
                    LocationPrivacyLevel = bundle.User.LocationPrivacyLevel,
                    LocationUpdatedAt = bundle.User.LocationUpdatedAt
                };
            }
            else
            {
                // CUSTOM LOCATION: Use Bundle's own location fields
                enhancedLocation = new LocationDto
                {
                    LocationDisplay = bundle.LocationDisplay ?? "",
                    LocationArea = bundle.LocationArea ?? "",
                    LocationCity = bundle.LocationCity ?? "",
                    LocationState = bundle.LocationState ?? "",
                    LocationCountry = bundle.LocationCountry ?? "",
                    LocationLat = bundle.LocationLat,
                    LocationLng = bundle.LocationLng,
                    LocationPrecisionRadius = bundle.LocationPrecisionRadius,
                    LocationSource = bundle.LocationSource,
                    LocationPrivacyLevel = bundle.LocationPrivacyLevel,
                    LocationUpdatedAt = bundle.LocationUpdatedAt
                };
            }
            
            // Set location fields in DTO
            bundleDto.OwnerLocation = enhancedLocation.LocationDisplay;
            bundleDto.EnhancedLocation = enhancedLocation;
            
            // Parse tags from string to List<string>
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

            // Get review statistics
            var reviews = await _context.Reviews
                .Where(r => r.BundleId == bundle.Id && r.Type == ReviewType.BundleReview)
                .ToListAsync();
            
            bundleDto.ReviewCount = reviews.Count;
            bundleDto.AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

            // Check availability for quick display (next 7 days)
            var availabilityCheck = await CheckBundleAvailabilityForPeriod(bundle.Id, DateTime.Today, DateTime.Today.AddDays(7));
            bundleDto.IsAvailable = availabilityCheck.IsAvailable;
            bundleDto.AvailableFromDate = availabilityCheck.EarliestAvailableDate;

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
            try
            {
                // Find the existing bundle and verify ownership
                var existingBundle = await _context.Bundles
                    .Include(b => b.BundleTools)
                    .FirstOrDefaultAsync(b => b.Id == bundleId && !b.IsDeleted);

                if (existingBundle == null)
                {
                    return ApiResponse<BundleDto>.CreateFailure("Bundle not found");
                }

                if (existingBundle.UserId != userId)
                {
                    return ApiResponse<BundleDto>.CreateFailure("You can only update your own bundles");
                }

                // SECURITY: Validate that all tools exist and belong to the user
                var toolIds = request.Tools.Select(t => t.ToolId).ToList();
                var tools = await _context.Tools
                    .Where(t => toolIds.Contains(t.Id) && !t.IsDeleted && t.OwnerId == userId)
                    .Include(t => t.Owner)
                    .ToListAsync();

                if (tools.Count != toolIds.Count)
                {
                    return ApiResponse<BundleDto>.CreateFailure("One or more tools not found or you can only create bundles with your own tools.");
                }

                // Capture old image URL for cleanup
                var oldImageUrl = existingBundle.ImageUrl;

                // Update bundle properties
                existingBundle.Name = request.Name;
                existingBundle.Description = request.Description;
                existingBundle.Guidelines = request.Guidelines;
                existingBundle.RequiredSkillLevel = request.RequiredSkillLevel;
                existingBundle.EstimatedProjectDuration = request.EstimatedProjectDuration;
                existingBundle.ImageUrl = request.ImageUrl;
                // TODO: Implement location inheritance logic for bundle updates (Phase 7)
                existingBundle.BundleDiscount = request.BundleDiscount;
                existingBundle.IsPublished = request.IsPublished;
                existingBundle.Category = request.Category;
                existingBundle.Tags = request.Tags;
                existingBundle.UpdatedAt = DateTime.UtcNow;

                // Explicitly mark the bundle entity as modified to ensure EF tracks changes
                _context.Entry(existingBundle).State = EntityState.Modified;

                // Remove existing bundle tools
                _context.BundleTools.RemoveRange(existingBundle.BundleTools);

                // Add new bundle tools
                foreach (var toolRequest in request.Tools)
                {
                    var bundleTool = new BundleTool
                    {
                        Id = Guid.NewGuid(),
                        BundleId = existingBundle.Id,
                        ToolId = toolRequest.ToolId,
                        UsageNotes = toolRequest.UsageNotes,
                        OrderInBundle = toolRequest.OrderInBundle,
                        IsOptional = toolRequest.IsOptional,
                        QuantityNeeded = toolRequest.QuantityNeeded
                    };

                    _context.BundleTools.Add(bundleTool);
                }

                await _context.SaveChangesAsync();

                // Clean up old bundle image if it was replaced (best effort - don't fail the operation if cleanup fails)
                await CleanupOldBundleImageAsync(oldImageUrl, request.ImageUrl);

                // Return the updated bundle
                var result = await GetBundleByIdAsync(existingBundle.Id);
                return result;
            }
            catch (Exception ex)
            {
                return ApiResponse<BundleDto>.CreateFailure($"Error updating bundle: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteBundleAsync(Guid bundleId, string userId)
        {
            try
            {
                // Find the bundle and verify ownership
                var bundle = await _context.Bundles
                    .Include(b => b.BundleTools)
                    .FirstOrDefaultAsync(b => b.Id == bundleId && b.UserId == userId && !b.IsDeleted);

                if (bundle == null)
                {
                    return ApiResponse<bool>.CreateFailure("Bundle not found or you don't have permission to delete it");
                }

                // Check if the bundle has active rentals
                var activeRentals = await _context.BundleRentals
                    .Where(br => br.BundleId == bundleId && 
                                br.Status == "Active" && 
                                !br.IsDeleted)
                    .CountAsync();

                if (activeRentals > 0)
                {
                    return ApiResponse<bool>.CreateFailure("Cannot delete bundle with active rentals");
                }

                // Soft delete the bundle
                bundle.IsDeleted = true;
                bundle.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return ApiResponse<bool>.CreateSuccess(true, "Bundle deleted successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.CreateFailure($"Failed to delete bundle: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PagedResult<BundleDto>>> GetUserBundlesAsync(string userId, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.Bundles
                    .Include(b => b.User)
                    .Include(b => b.BundleTools)
                        .ThenInclude(bt => bt.Tool)
                            .ThenInclude(t => t.Owner)
                    .Where(b => b.UserId == userId && !b.IsDeleted);

                var totalCount = await query.CountAsync();
                var bundles = await query
                    .OrderByDescending(b => b.IsPublished)
                    .ThenByDescending(b => b.IsFeatured)
                    .ThenByDescending(b => b.UpdatedAt)
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

                return ApiResponse<PagedResult<BundleDto>>.CreateSuccess(result, "User bundles retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedResult<BundleDto>>.CreateFailure($"Failed to retrieve user bundles: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> SetFeaturedStatusAsync(Guid bundleId, bool isFeatured, string adminUserId)
        {
            try
            {
                // Find the bundle
                var bundle = await _context.Bundles
                    .FirstOrDefaultAsync(b => b.Id == bundleId && !b.IsDeleted);

                if (bundle == null)
                {
                    return ApiResponse<bool>.CreateFailure("Bundle not found");
                }

                // Update featured status
                bundle.IsFeatured = isFeatured;
                bundle.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var action = isFeatured ? "featured" : "unfeatured";
                return ApiResponse<bool>.CreateSuccess(true, $"Bundle successfully {action}");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.CreateFailure($"Failed to update featured status: {ex.Message}");
            }
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

                // Determine which tools to include in rental
                var toolsToRent = bundle.BundleTools.ToList();
                
                // If specific tools are selected, validate and filter
                if (request.SelectedToolIds.Any())
                {
                    // Validate all selected tools exist in bundle
                    var selectedToolsExist = request.SelectedToolIds.All(id => 
                        bundle.BundleTools.Any(bt => bt.ToolId == id));
                    
                    if (!selectedToolsExist)
                    {
                        return ApiResponse<BundleRentalDto>.CreateFailure("One or more selected tools are not part of this bundle");
                    }
                    
                    // Ensure all required tools are selected
                    var requiredToolsSelected = bundle.BundleTools
                        .Where(bt => !bt.IsOptional)
                        .All(bt => request.SelectedToolIds.Contains(bt.ToolId));
                    
                    if (!requiredToolsSelected)
                    {
                        return ApiResponse<BundleRentalDto>.CreateFailure("All required tools must be selected");
                    }
                    
                    // Filter to selected tools only
                    toolsToRent = bundle.BundleTools
                        .Where(bt => request.SelectedToolIds.Contains(bt.ToolId))
                        .ToList();
                }

                // Calculate bundle rental costs based on selected tools
                var costCalculation = await CalculateBundleCostForSelectedToolsAsync(bundle, toolsToRent, request.RentalDate, request.ReturnDate);
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

                // Create individual tool rentals for each selected tool
                var individualRentals = new List<Rental>();
                foreach (var bundleTool in toolsToRent)
                {
                    var toolRental = new Rental
                    {
                        Id = Guid.NewGuid(),
                        ToolId = bundleTool.ToolId,
                        RenterId = userId,
                        OwnerId = bundleTool.Tool.OwnerId,
                        StartDate = request.RentalDate,
                        EndDate = request.ReturnDate,
                        TotalCost = costCalculation.Data.ToolCosts
                            .FirstOrDefault(tc => tc.ToolId == bundleTool.ToolId)?.TotalCost ?? 0,
                        DepositAmount = 0, // Deposit handled at bundle level
                        Status = RentalStatus.Pending,
                        Notes = $"Part of bundle rental: {bundle.Name}",
                        BundleRentalId = bundleRental.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    individualRentals.Add(toolRental);
                }

                _context.Rentals.AddRange(individualRentals);
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
            try
            {
                // Find the bundle
                var bundle = await _context.Bundles
                    .FirstOrDefaultAsync(b => b.Id == bundleId && !b.IsDeleted);

                if (bundle == null)
                {
                    return ApiResponse<bool>.CreateFailure("Bundle not found");
                }

                // Increment view count
                bundle.ViewCount++;
                bundle.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return ApiResponse<bool>.CreateSuccess(true, "View count incremented successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.CreateFailure($"Failed to increment view count: {ex.Message}");
            }
        }

        public async Task<ApiResponse<Dictionary<string, int>>> GetBundleCategoryCountsAsync()
        {
            try
            {
                // Get bundle counts by category (only published and non-deleted bundles)
                var categoryCounts = await _context.Bundles
                    .Where(b => b.IsPublished && !b.IsDeleted)
                    .GroupBy(b => b.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Category, x => x.Count);

                return ApiResponse<Dictionary<string, int>>.CreateSuccess(categoryCounts, "Bundle category counts retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<Dictionary<string, int>>.CreateFailure($"Failed to get bundle category counts: {ex.Message}");
            }
        }

        public async Task<ApiResponse<BundleReviewDto>> CreateBundleReviewAsync(CreateBundleReviewRequest request, string userId)
        {
            try
            {
                // Check if user can review this bundle
                var canReview = await CanUserReviewBundleAsync(request.BundleId, userId);
                if (!canReview.Success || !canReview.Data)
                {
                    return ApiResponse<BundleReviewDto>.CreateFailure(canReview.Message ?? "You cannot review this bundle");
                }

                // Check if user has already reviewed this bundle
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.BundleId == request.BundleId && 
                                            r.ReviewerId == userId && 
                                            r.Type == ReviewType.BundleReview);

                if (existingReview != null)
                {
                    return ApiResponse<BundleReviewDto>.CreateFailure("You have already reviewed this bundle");
                }

                // Create the review
                var review = new Review
                {
                    Id = Guid.NewGuid(),
                    BundleId = request.BundleId,
                    BundleRentalId = request.BundleRentalId,
                    ReviewerId = userId,
                    RevieweeId = "", // Bundle reviews don't have reviewees
                    Rating = request.Rating,
                    Title = request.Title,
                    Comment = request.Comment,
                    Type = ReviewType.BundleReview,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                // Return the created review with populated data
                var reviewDto = await GetBundleReviewByIdAsync(review.Id);
                return ApiResponse<BundleReviewDto>.CreateSuccess(reviewDto, "Bundle review created successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<BundleReviewDto>.CreateFailure($"Failed to create bundle review: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PagedResult<BundleReviewDto>>> GetBundleReviewsAsync(Guid bundleId, int page = 1, int pageSize = 10)
        {
            try
            {
                var query = _context.Reviews
                    .Where(r => r.BundleId == bundleId && r.Type == ReviewType.BundleReview)
                    .Include(r => r.Reviewer)
                    .Include(r => r.Bundle)
                    .Include(r => r.BundleRental)
                    .OrderByDescending(r => r.CreatedAt);

                var totalCount = await query.CountAsync();
                var reviews = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new BundleReviewDto
                    {
                        Id = r.Id,
                        BundleId = r.BundleId!.Value,
                        BundleRentalId = r.BundleRentalId,
                        ReviewerId = r.ReviewerId,
                        ReviewerName = r.Reviewer.FirstName + " " + r.Reviewer.LastName,
                        ReviewerAvatar = r.Reviewer.ProfilePictureUrl ?? "",
                        Rating = r.Rating,
                        Title = r.Title,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt,
                        BundleName = r.Bundle!.Name,
                        RentalStartDate = r.BundleRental != null ? r.BundleRental.RentalDate : null,
                        RentalEndDate = r.BundleRental != null ? r.BundleRental.ReturnDate : null,
                        RentalDuration = r.BundleRental != null ? (r.BundleRental.ReturnDate - r.BundleRental.RentalDate).Days + 1 : 0
                    })
                    .ToListAsync();

                var result = new PagedResult<BundleReviewDto>
                {
                    Items = reviews,
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                };

                return ApiResponse<PagedResult<BundleReviewDto>>.CreateSuccess(result, "Bundle reviews retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedResult<BundleReviewDto>>.CreateFailure($"Failed to get bundle reviews: {ex.Message}");
            }
        }

        public async Task<ApiResponse<BundleReviewSummaryDto>> GetBundleReviewSummaryAsync(Guid bundleId)
        {
            try
            {
                var reviews = await _context.Reviews
                    .Where(r => r.BundleId == bundleId && r.Type == ReviewType.BundleReview)
                    .Include(r => r.Reviewer)
                    .Include(r => r.Bundle)
                    .Include(r => r.BundleRental)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                var summary = new BundleReviewSummaryDto
                {
                    TotalReviews = reviews.Count,
                    AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
                    FiveStarCount = reviews.Count(r => r.Rating == 5),
                    FourStarCount = reviews.Count(r => r.Rating == 4),
                    ThreeStarCount = reviews.Count(r => r.Rating == 3),
                    TwoStarCount = reviews.Count(r => r.Rating == 2),
                    OneStarCount = reviews.Count(r => r.Rating == 1),
                    LatestReviews = reviews.Take(3).Select(r => new BundleReviewDto
                    {
                        Id = r.Id,
                        BundleId = r.BundleId!.Value,
                        BundleRentalId = r.BundleRentalId,
                        ReviewerId = r.ReviewerId,
                        ReviewerName = r.Reviewer.FirstName + " " + r.Reviewer.LastName,
                        ReviewerAvatar = r.Reviewer.ProfilePictureUrl ?? "",
                        Rating = r.Rating,
                        Title = r.Title,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt,
                        BundleName = r.Bundle!.Name,
                        RentalStartDate = r.BundleRental != null ? r.BundleRental.RentalDate : null,
                        RentalEndDate = r.BundleRental != null ? r.BundleRental.ReturnDate : null,
                        RentalDuration = r.BundleRental != null ? (r.BundleRental.ReturnDate - r.BundleRental.RentalDate).Days + 1 : 0
                    }).ToList()
                };

                return ApiResponse<BundleReviewSummaryDto>.CreateSuccess(summary, "Bundle review summary retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<BundleReviewSummaryDto>.CreateFailure($"Failed to get bundle review summary: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> CanUserReviewBundleAsync(Guid bundleId, string userId)
        {
            try
            {
                // Check if bundle exists
                var bundle = await _context.Bundles
                    .FirstOrDefaultAsync(b => b.Id == bundleId && !b.IsDeleted);

                if (bundle == null)
                {
                    return ApiResponse<bool>.CreateFailure("Bundle not found");
                }

                // Users cannot review their own bundles
                if (bundle.UserId == userId)
                {
                    return ApiResponse<bool>.CreateFailure("You cannot review your own bundle");
                }

                // Check if user has completed at least one rental of this bundle
                var hasCompletedRental = await _context.BundleRentals
                    .AnyAsync(br => br.BundleId == bundleId && 
                                   br.RenterUserId == userId && 
                                   br.Status == "Completed");

                if (!hasCompletedRental)
                {
                    return ApiResponse<bool>.CreateFailure("You can only review bundles you have rented and completed");
                }

                return ApiResponse<bool>.CreateSuccess(true, "User can review this bundle");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.CreateFailure($"Failed to check review eligibility: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteBundleReviewAsync(Guid reviewId, string userId)
        {
            try
            {
                var review = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.Id == reviewId && r.Type == ReviewType.BundleReview);

                if (review == null)
                {
                    return ApiResponse<bool>.CreateFailure("Bundle review not found");
                }

                // Only the reviewer can delete their own review
                if (review.ReviewerId != userId)
                {
                    return ApiResponse<bool>.CreateFailure("You can only delete your own reviews");
                }

                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();

                return ApiResponse<bool>.CreateSuccess(true, "Bundle review deleted successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.CreateFailure($"Failed to delete bundle review: {ex.Message}");
            }
        }

        private async Task<BundleReviewDto> GetBundleReviewByIdAsync(Guid reviewId)
        {
            var review = await _context.Reviews
                .Include(r => r.Reviewer)
                .Include(r => r.Bundle)
                .Include(r => r.BundleRental)
                .FirstAsync(r => r.Id == reviewId);

            return new BundleReviewDto
            {
                Id = review.Id,
                BundleId = review.BundleId!.Value,
                BundleRentalId = review.BundleRentalId,
                ReviewerId = review.ReviewerId,
                ReviewerName = review.Reviewer.FirstName + " " + review.Reviewer.LastName,
                ReviewerAvatar = review.Reviewer.ProfilePictureUrl ?? "",
                Rating = review.Rating,
                Title = review.Title,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                BundleName = review.Bundle!.Name,
                RentalStartDate = review.BundleRental?.RentalDate,
                RentalEndDate = review.BundleRental?.ReturnDate,
                RentalDuration = review.BundleRental != null ? (review.BundleRental.ReturnDate - review.BundleRental.RentalDate).Days + 1 : 0
            };
        }

        public async Task<ApiResponse<BundleApprovalStatusDto>> GetBundleApprovalStatusAsync(Guid bundleId, string userId)
        {
            try
            {
                var bundle = await _context.Bundles
                    .Include(b => b.BundleTools)
                        .ThenInclude(bt => bt.Tool)
                    .FirstOrDefaultAsync(b => b.Id == bundleId && b.UserId == userId && !b.IsDeleted);

                if (bundle == null)
                {
                    return ApiResponse<BundleApprovalStatusDto>.CreateFailure("Bundle not found or you don't have permission to view it");
                }

                var unapprovedTools = bundle.BundleTools
                    .Where(bt => !bt.Tool.IsApproved)
                    .Select(bt => new UnapprovedToolInfo
                    {
                        ToolId = bt.Tool.Id,
                        ToolName = bt.Tool.Name,
                        IsPending = bt.Tool.PendingApproval,
                        RejectionReason = bt.Tool.RejectionReason
                    })
                    .ToList();

                var status = new BundleApprovalStatusDto
                {
                    BundleId = bundle.Id,
                    BundleName = bundle.Name,
                    BundleIsApproved = bundle.IsApproved,
                    BundleIsPending = bundle.PendingApproval,
                    BundleRejectionReason = bundle.RejectionReason,
                    HasUnapprovedTools = unapprovedTools.Any(),
                    UnapprovedTools = unapprovedTools,
                    CanBePubliclyVisible = bundle.IsApproved && !unapprovedTools.Any(),
                    WarningMessage = unapprovedTools.Any() 
                        ? $"This bundle contains {unapprovedTools.Count} unapproved tools and will not be visible to other users until all tools are approved."
                        : null
                };

                return ApiResponse<BundleApprovalStatusDto>.CreateSuccess(status);
            }
            catch (Exception ex)
            {
                return ApiResponse<BundleApprovalStatusDto>.CreateFailure($"Error checking bundle approval status: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up old bundle image from storage when bundle image is updated.
        /// This is best-effort cleanup - failures are logged but don't fail the main operation.
        /// </summary>
        private async Task CleanupOldBundleImageAsync(string? oldImageUrl, string? newImageUrl)
        {
            if (string.IsNullOrEmpty(oldImageUrl)) return;

            // Don't delete if it's the same image
            if (!string.IsNullOrEmpty(newImageUrl) && oldImageUrl.Equals(newImageUrl, StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                var storagePath = ExtractStoragePathFromUrl(oldImageUrl);
                if (!string.IsNullOrEmpty(storagePath))
                {
                    var deleted = await _fileStorageService.DeleteFileAsync(storagePath);
                    if (deleted)
                    {
                        _logger.LogInformation("Cleaned up old bundle image: {ImageUrl}", oldImageUrl);
                    }
                    else
                    {
                        _logger.LogWarning("Old bundle image not found or already deleted: {ImageUrl}", oldImageUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete old bundle image: {ImageUrl}", oldImageUrl);
                // Don't rethrow - this is best effort cleanup
            }
        }

        public async Task<ApiResponse<bool>> RequestApprovalAsync(Guid bundleId, string userId, RequestApprovalRequest request)
        {
            try
            {
                var bundle = await _context.Bundles
                    .FirstOrDefaultAsync(b => b.Id == bundleId && b.UserId == userId);

                if (bundle == null)
                {
                    return ApiResponse<bool>.CreateFailure("Bundle not found or you don't have permission to request approval.");
                }

                if (bundle.IsApproved)
                {
                    return ApiResponse<bool>.CreateFailure("Bundle is already approved.");
                }

                if (bundle.PendingApproval && string.IsNullOrEmpty(bundle.RejectionReason))
                {
                    return ApiResponse<bool>.CreateFailure("Bundle is already pending approval.");
                }

                // Reset approval status
                bundle.PendingApproval = true;
                bundle.RejectionReason = null;
                bundle.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Bundle {BundleId} approval requested by user {UserId}", bundleId, userId);
                return ApiResponse<bool>.CreateSuccess(true, "Approval requested successfully. Your bundle will be reviewed by our team.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting approval for bundle {BundleId} by user {UserId}", bundleId, userId);
                return ApiResponse<bool>.CreateFailure("An error occurred while requesting approval.");
            }
        }

        /// <summary>
        /// Extracts the storage path from a file URL for deletion
        /// </summary>
        private string ExtractStoragePathFromUrl(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return "";

            try
            {
                // Handle relative URLs like "/uploads/images/filename.jpg"
                if (imageUrl.StartsWith("/uploads/"))
                {
                    return imageUrl.Substring("/uploads/".Length);
                }

                // Handle absolute URLs - extract the path after /uploads/
                var uploadsIndex = imageUrl.IndexOf("/uploads/", StringComparison.OrdinalIgnoreCase);
                if (uploadsIndex >= 0)
                {
                    return imageUrl.Substring(uploadsIndex + "/uploads/".Length);
                }

                // If it's just the filename/path without URL prefix, return as-is
                if (!imageUrl.StartsWith("http"))
                {
                    return imageUrl;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting storage path from URL: {ImageUrl}", imageUrl);
            }

            return "";
        }

        private async Task<IQueryable<Bundle>> ApplyBundleLocationFilterAsync(IQueryable<Bundle> bundlesQuery, LocationSearchRequest locationSearch)
        {
            try
            {
                _logger.LogDebug("🔍 DEBUG: Starting ApplyBundleLocationFilterAsync for Bundles");
                
                // Early return if no location search criteria
                if (!locationSearch.Lat.HasValue && !locationSearch.Lng.HasValue && 
                    string.IsNullOrEmpty(locationSearch.LocationQuery))
                {
                    _logger.LogDebug("🔍 DEBUG: No location criteria provided for bundles, returning original query");
                    return bundlesQuery; // No location filtering
                }
                
                _logger.LogDebug("🔍 DEBUG: Bundle location criteria found - Lat: {Lat}, Lng: {Lng}, Query: '{Query}'", 
                    locationSearch.Lat, locationSearch.Lng, locationSearch.LocationQuery);

                // If only coordinates provided, reverse geocode them first to get location string
                if ((locationSearch.Lat.HasValue && locationSearch.Lng.HasValue) && 
                    string.IsNullOrEmpty(locationSearch.LocationQuery))
                {
                    try 
                    {
                        _logger.LogDebug("Starting reverse geocoding for bundle coordinates ({Lat}, {Lng})", 
                            locationSearch.Lat, locationSearch.Lng);
                        
                        var reverseGeocodedLocation = await _geocodingService.ReverseGeocodeAsync(
                            locationSearch.Lat.Value, locationSearch.Lng.Value);
                        
                        if (reverseGeocodedLocation != null && !string.IsNullOrEmpty(reverseGeocodedLocation.DisplayName))
                        {
                            locationSearch.LocationQuery = reverseGeocodedLocation.DisplayName;
                            _logger.LogDebug("Bundle reverse geocoding successful: '{LocationName}'", 
                                reverseGeocodedLocation.DisplayName);
                        }
                        else
                        {
                            _logger.LogDebug("Bundle reverse geocoding returned no results for coordinates ({Lat}, {Lng})", 
                                locationSearch.Lat, locationSearch.Lng);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to reverse geocode bundle coordinates ({Lat}, {Lng})", 
                            locationSearch.Lat, locationSearch.Lng);
                        // Continue with proximity search if reverse geocoding fails
                    }
                }

                // Set default radius if coordinates provided but no radius specified
                if ((locationSearch.Lat.HasValue && locationSearch.Lng.HasValue) && !locationSearch.RadiusKm.HasValue)
                {
                    locationSearch.RadiusKm = 5; // Default 5km radius
                    _logger.LogDebug("Applied default radius of 5km for bundle coordinate search");
                }

                // Determine search input type (after potential reverse geocoding)
                bool queryHasCoords = locationSearch.Lat.HasValue && locationSearch.Lng.HasValue;
                bool queryHasLocation = !string.IsNullOrEmpty(locationSearch.LocationQuery);

                _logger.LogDebug("Bundle location search: QueryHasCoords={QueryHasCoords}, QueryHasLocation={QueryHasLocation}, Radius={Radius}km", 
                    queryHasCoords, queryHasLocation, locationSearch.RadiusKm);

                // Step 3: Combined location search - proximity + text search for items without coordinates
                var combinedLocationBundleIds = new List<Guid>();

                // Step 3a: Proximity search for items with coordinates
                if (queryHasCoords)
                {
                    _logger.LogDebug("🔍 DEBUG: Starting proximity search for bundles with coordinates");
                    var centerLat = locationSearch.Lat.Value;
                    var centerLng = locationSearch.Lng.Value;
                    var radiusKm = locationSearch.RadiusKm.Value;

                    // Proximity search for bundles with coordinates
                    _logger.LogDebug("🔍 DEBUG: About to execute proximity search database query for bundles");
                    var proximityBundleIds = (from b in _context.Bundles
                                             join u in _context.Users on b.UserId equals u.Id into userJoin
                                             from user in userJoin.DefaultIfEmpty()
                                             where 
                                                 // Direct bundle coordinates within radius
                                                 (b.LocationLat.HasValue && b.LocationLng.HasValue &&
                                                  (6371 * Math.Acos(
                                                      Math.Cos(Math.PI * (double)centerLat / 180.0) *
                                                      Math.Cos(Math.PI * (double)b.LocationLat.Value / 180.0) *
                                                      Math.Cos(Math.PI * ((double)b.LocationLng.Value - (double)centerLng) / 180.0) +
                                                      Math.Sin(Math.PI * (double)centerLat / 180.0) *
                                                      Math.Sin(Math.PI * (double)b.LocationLat.Value / 180.0)
                                                  )) <= radiusKm) ||
                                                 // OR inherited coordinates from owner within radius
                                                 (b.LocationInheritanceOption == Core.Enums.LocationInheritanceOption.InheritFromProfile &&
                                                  user != null && user.LocationLat.HasValue && user.LocationLng.HasValue &&
                                                  (6371 * Math.Acos(
                                                      Math.Cos(Math.PI * (double)centerLat / 180.0) *
                                                      Math.Cos(Math.PI * (double)user.LocationLat.Value / 180.0) *
                                                      Math.Cos(Math.PI * ((double)user.LocationLng.Value - (double)centerLng) / 180.0) +
                                                      Math.Sin(Math.PI * (double)centerLat / 180.0) *
                                                      Math.Sin(Math.PI * (double)user.LocationLat.Value / 180.0)
                                                  )) <= radiusKm)
                                             select b.Id).Distinct();

                    _logger.LogDebug("🔍 DEBUG: Executing ToList() on bundle proximity search query");
                    var proximityResults = proximityBundleIds.ToList();
                    _logger.LogDebug("🔍 DEBUG: Bundle proximity search completed, found {Count} results", proximityResults.Count);
                    
                    if (proximityResults.Any())
                    {
                        _logger.LogDebug("Proximity search found {Count} bundles within {Radius}km", proximityResults.Count, radiusKm);
                        combinedLocationBundleIds.AddRange(proximityResults);
                    }
                    else
                    {
                        _logger.LogDebug("No bundles found within {Radius}km of coordinates", radiusKm);
                    }
                }

                // Step 3b: Text-based search for ALL items with matching location text (with or without coordinates)
                if (queryHasLocation)
                {
                    var locationQuery = locationSearch.LocationQuery.ToLower();
                    _logger.LogDebug("Applying text-based bundle location filter for ALL items with matching text: '{Query}'", locationQuery);

                    var textMatchingBundleIds = (from b in _context.Bundles
                                               join u in _context.Users on b.UserId equals u.Id into userJoin
                                               from user in userJoin.DefaultIfEmpty()
                                               where 
                                                   // All items with location text that matches (regardless of coordinates)
                                                   // Enhanced partial matching: split query into words and match any word
                                                   (
                                                       // Direct bundle has location text that matches
                                                       locationQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(word =>
                                                           (b.LocationDisplay != null && b.LocationDisplay.ToLower().Contains(word)) ||
                                                           (b.LocationCity != null && b.LocationCity.ToLower().Contains(word)) ||
                                                           (b.LocationState != null && b.LocationState.ToLower().Contains(word)) ||
                                                           (b.LocationCountry != null && b.LocationCountry.ToLower().Contains(word)) ||
                                                           (b.LocationArea != null && b.LocationArea.ToLower().Contains(word))
                                                       )
                                                   ) ||
                                                   // OR inherited location: bundle inherits from user with matching location text
                                                   (
                                                       b.LocationInheritanceOption == Core.Enums.LocationInheritanceOption.InheritFromProfile &&
                                                       user != null &&
                                                       locationQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(word =>
                                                           (user.LocationDisplay != null && user.LocationDisplay.ToLower().Contains(word)) ||
                                                           (user.LocationCity != null && user.LocationCity.ToLower().Contains(word)) ||
                                                           (user.LocationState != null && user.LocationState.ToLower().Contains(word)) ||
                                                           (user.LocationCountry != null && user.LocationCountry.ToLower().Contains(word)) ||
                                                           (user.LocationArea != null && user.LocationArea.ToLower().Contains(word))
                                                       )
                                                   )
                                               select b.Id).Distinct();

                    var textResults = textMatchingBundleIds.ToList();
                    
                    if (textResults.Any())
                    {
                        _logger.LogDebug("Text search found {Count} bundles with matching location text '{Query}'", textResults.Count, locationQuery);
                        combinedLocationBundleIds.AddRange(textResults);
                    }
                    else
                    {
                        _logger.LogDebug("No bundles found matching text query: '{Query}'", locationQuery);
                    }
                }

                // Step 3c: Apply combined location filtering if we have any location-based results
                if (combinedLocationBundleIds.Any())
                {
                    var uniqueLocationBundleIds = combinedLocationBundleIds.Distinct().ToList();
                    _logger.LogDebug("Combined location search found {Count} total bundles (proximity + text matching)", uniqueLocationBundleIds.Count);
                    bundlesQuery = bundlesQuery.Where(b => uniqueLocationBundleIds.Contains(b.Id));
                }
                else if (queryHasCoords || queryHasLocation)
                {
                    _logger.LogDebug("No bundles found matching location criteria - returning empty result set");
                    // If location search was requested but no results found, return empty set
                    bundlesQuery = bundlesQuery.Where(b => false);
                }

                // Step 4: Apply structured location filters if provided using explicit joins
                if (locationSearch.Cities?.Any() == true)
                {
                    var cities = locationSearch.Cities.Select(c => c.ToLower()).ToList();
                    
                    var bundleIdsWithDirectCity = _context.Bundles
                        .Where(b => b.LocationCity != null && cities.Contains(b.LocationCity.ToLower()))
                        .Select(b => b.Id);

                    var bundleIdsWithInheritedCity = (from b in _context.Bundles
                                                     join u in _context.Users on b.UserId equals u.Id
                                                     where b.LocationInheritanceOption == Core.Enums.LocationInheritanceOption.InheritFromProfile
                                                           && u.LocationCity != null && cities.Contains(u.LocationCity.ToLower())
                                                     select b.Id);

                    var matchingCityBundleIds = bundleIdsWithDirectCity.Union(bundleIdsWithInheritedCity);
                    bundlesQuery = bundlesQuery.Where(b => matchingCityBundleIds.Contains(b.Id));
                }

                if (locationSearch.States?.Any() == true)
                {
                    var states = locationSearch.States.Select(s => s.ToLower()).ToList();
                    
                    var bundleIdsWithDirectState = _context.Bundles
                        .Where(b => b.LocationState != null && states.Contains(b.LocationState.ToLower()))
                        .Select(b => b.Id);

                    var bundleIdsWithInheritedState = (from b in _context.Bundles
                                                      join u in _context.Users on b.UserId equals u.Id
                                                      where b.LocationInheritanceOption == Core.Enums.LocationInheritanceOption.InheritFromProfile
                                                            && u.LocationState != null && states.Contains(u.LocationState.ToLower())
                                                      select b.Id);

                    var matchingStateBundleIds = bundleIdsWithDirectState.Union(bundleIdsWithInheritedState);
                    bundlesQuery = bundlesQuery.Where(b => matchingStateBundleIds.Contains(b.Id));
                }

                if (locationSearch.Countries?.Any() == true)
                {
                    var countries = locationSearch.Countries.Select(c => c.ToLower()).ToList();
                    
                    var bundleIdsWithDirectCountry = _context.Bundles
                        .Where(b => b.LocationCountry != null && countries.Contains(b.LocationCountry.ToLower()))
                        .Select(b => b.Id);

                    var bundleIdsWithInheritedCountry = (from b in _context.Bundles
                                                        join u in _context.Users on b.UserId equals u.Id
                                                        where b.LocationInheritanceOption == Core.Enums.LocationInheritanceOption.InheritFromProfile
                                                              && u.LocationCountry != null && countries.Contains(u.LocationCountry.ToLower())
                                                        select b.Id);

                    var matchingCountryBundleIds = bundleIdsWithDirectCountry.Union(bundleIdsWithInheritedCountry);
                    bundlesQuery = bundlesQuery.Where(b => matchingCountryBundleIds.Contains(b.Id));
                }

                return bundlesQuery;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying bundle location filter for query: {Query}", locationSearch.LocationQuery);
                // Fall back to simple text search if geocoding fails
                if (!string.IsNullOrEmpty(locationSearch.LocationQuery))
                {
                    var locationQuery = locationSearch.LocationQuery.ToLower();
                    return bundlesQuery.Where(b => b.LocationDisplay != null && b.LocationDisplay.ToLower().Contains(locationQuery));
                }
                return bundlesQuery;
            }
        }

    }
}