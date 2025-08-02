using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Features.Tools;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Core.DTOs.Tools;
using ToolsSharing.Core.DTOs.Location;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Features.Tools;

public class ToolsService : IToolsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IGeocodingService _geocodingService;
    private readonly ILogger<ToolsService> _logger;

    public ToolsService(
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        IGeocodingService geocodingService,
        ILogger<ToolsService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _context = context;
        _fileStorageService = fileStorageService;
        _geocodingService = geocodingService;
        _logger = logger;
    }

    public async Task<ApiResponse<List<ToolDto>>> GetToolsAsync(GetToolsQuery query)
    {
        try
        {
            var toolsQuery = _context.Tools
                .Include(t => t.Owner)
                .Include(t => t.Images)
                .Where(t => !t.IsDeleted && t.IsApproved);

            // Apply common filters
            toolsQuery = await ApplyCommonFiltersAsync(toolsQuery, query);

            // Apply sorting
            toolsQuery = query.SortBy?.ToLower() switch
            {
                "name" => toolsQuery.OrderBy(t => t.Name),
                "price" => toolsQuery.OrderBy(t => t.DailyRate),
                "created" => toolsQuery.OrderByDescending(t => t.CreatedAt),
                _ => toolsQuery.OrderBy(t => t.Name)
            };

            // Apply pagination
            if (query.PageSize > 0)
            {
                toolsQuery = toolsQuery
                    .Skip((query.PageNumber - 1) * query.PageSize)
                    .Take(query.PageSize);
            }

            var tools = await toolsQuery.ToListAsync();
            var toolDtos = MapToolsToDto(tools);

            return ApiResponse<List<ToolDto>>.CreateSuccess(toolDtos, "Tools retrieved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<List<ToolDto>>.CreateFailure($"Error retrieving tools: {ex.Message}. Inner: {ex.InnerException?.Message}");
        }
    }

    public async Task<ApiResponse<PagedResult<ToolDto>>> GetToolsPagedAsync(GetToolsQuery query)
    {
        try
        {
            var toolsQuery = _context.Tools
                .Include(t => t.Owner)
                .Include(t => t.Images)
                .Where(t => !t.IsDeleted && t.IsApproved);

            // Apply common filters
            toolsQuery = await ApplyCommonFiltersAsync(toolsQuery, query);

            // Get total count before applying pagination
            var totalCount = await toolsQuery.CountAsync();

            // Apply sorting
            toolsQuery = query.SortBy?.ToLower() switch
            {
                "name" => toolsQuery.OrderBy(t => t.Name),
                "price-low" => toolsQuery.OrderBy(t => t.DailyRate),
                "price-high" => toolsQuery.OrderByDescending(t => t.DailyRate),
                "rating" => toolsQuery.OrderByDescending(t => t.AverageRating),
                "newest" => toolsQuery.OrderByDescending(t => t.CreatedAt),
                _ => toolsQuery.OrderBy(t => t.Name)
            };

            // Apply pagination
            var tools = await toolsQuery
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var toolDtos = MapToolsToDto(tools);

            var pagedResult = new PagedResult<ToolDto>
            {
                Items = toolDtos,
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };

            return ApiResponse<PagedResult<ToolDto>>.CreateSuccess(pagedResult, "Tools retrieved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<PagedResult<ToolDto>>.CreateFailure($"Error retrieving tools: {ex.Message}. Inner: {ex.InnerException?.Message}");
        }
    }

    public async Task<ApiResponse<List<ToolDto>>> GetUserToolsAsync(GetToolsQuery query, string userId)
    {
        try
        {
            var toolsQuery = _context.Tools
                .Include(t => t.Owner)
                .Include(t => t.Images)
                .Where(t => !t.IsDeleted && t.OwnerId == userId);

            // Simple sorting only (like BundleService.GetUserBundlesAsync)
            toolsQuery = query.SortBy?.ToLower() switch
            {
                "price" => toolsQuery.OrderBy(t => t.DailyRate),
                "created" => toolsQuery.OrderByDescending(t => t.CreatedAt),
                _ => toolsQuery.OrderByDescending(t => t.IsAvailable)
                    .ThenByDescending(t => t.UpdatedAt)
            };

            // Apply pagination
            if (query.PageSize > 0)
            {
                toolsQuery = toolsQuery
                    .Skip((query.PageNumber - 1) * query.PageSize)
                    .Take(query.PageSize);
            }

            var tools = await toolsQuery.ToListAsync();
            var toolDtos = MapToolsToDto(tools);

            return ApiResponse<List<ToolDto>>.CreateSuccess(toolDtos, "User tools retrieved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<List<ToolDto>>.CreateFailure($"Error retrieving user tools: {ex.Message}. Inner: {ex.InnerException?.Message}");
        }
    }

    public async Task<ApiResponse<ToolDto>> GetToolByIdAsync(GetToolByIdQuery query)
    {
        try
        {
            var tool = await _context.Tools
                .Include(t => t.Owner)
                .Include(t => t.Images)
                .Include(t => t.Rentals)
                .FirstOrDefaultAsync(t => t.Id == query.Id && !t.IsDeleted);

            if (tool == null)
            {
                return ApiResponse<ToolDto>.CreateFailure("Tool not found");
            }

            var toolDto = MapToolToDto(tool);
            return ApiResponse<ToolDto>.CreateSuccess(toolDto, "Tool retrieved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<ToolDto>.CreateFailure($"Error retrieving tool: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ToolDto>> CreateToolAsync(CreateToolCommand command)
    {
        try
        {
            // Get the owner from the context (should be set by the controller from JWT claims)
            var owner = await _context.Users.FindAsync(command.OwnerId);
            if (owner == null)
            {
                return ApiResponse<ToolDto>.CreateFailure("Owner not found");
            }

            var tool = new Tool
            {
                Id = Guid.NewGuid(),
                Name = command.Name,
                Description = command.Description,
                Category = command.Category,
                Brand = command.Brand,
                Model = command.Model,
                DailyRate = command.DailyRate,
                WeeklyRate = command.WeeklyRate,
                MonthlyRate = command.MonthlyRate,
                DepositRequired = command.DepositRequired,
                Condition = command.Condition,
                IsAvailable = true,
                LeadTimeHours = command.LeadTimeHours,
                OwnerId = command.OwnerId,
                Tags = command.Tags ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false,
                // Approval fields - new tools require approval
                IsApproved = false,
                PendingApproval = true
            };

            // Handle location inheritance (Phase 7 - TRUE INHERITANCE)
            // Store the inheritance choice, location will be resolved at query time
            tool.LocationInheritanceOption = command.LocationSource;
            
            if (command.LocationSource == Core.Enums.LocationInheritanceOption.CustomLocation && command.CustomLocation != null)
            {
                // Only store location data for custom locations
                tool.LocationDisplay = command.CustomLocation.LocationDisplay;
                tool.LocationArea = command.CustomLocation.LocationArea;
                tool.LocationCity = command.CustomLocation.LocationCity;
                tool.LocationState = command.CustomLocation.LocationState;
                tool.LocationCountry = command.CustomLocation.LocationCountry;
                tool.LocationLat = command.CustomLocation.LocationLat;
                tool.LocationLng = command.CustomLocation.LocationLng;
                tool.LocationPrecisionRadius = command.CustomLocation.LocationPrecisionRadius;
                tool.LocationSource = command.CustomLocation.LocationSource ?? Core.Enums.LocationSource.Manual;
                tool.LocationPrivacyLevel = command.CustomLocation.LocationPrivacyLevel;
                tool.LocationUpdatedAt = DateTime.UtcNow;
            }
            // For InheritFromProfile: leave location fields null/empty, will be resolved at query time from User profile

            // Add images if provided
            if (command.ImageUrls != null && command.ImageUrls.Any())
            {
                tool.Images = command.ImageUrls.Select(url => new ToolImage
                {
                    Id = Guid.NewGuid(),
                    ToolId = tool.Id,
                    ImageUrl = url,
                    CreatedAt = DateTime.UtcNow
                }).ToList();
            }

            _context.Tools.Add(tool);
            await _context.SaveChangesAsync();

            // Reload with includes to get complete data for mapping
            var createdTool = await _context.Tools
                .Include(t => t.Owner)
                .Include(t => t.Images)
                .FirstAsync(t => t.Id == tool.Id);

            var toolDto = MapToolToDto(createdTool);
            return ApiResponse<ToolDto>.CreateSuccess(toolDto, "Tool created successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<ToolDto>.CreateFailure($"Error creating tool: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ToolDto>> UpdateToolAsync(UpdateToolCommand command)
    {
        try
        {
            var tool = await _context.Tools
                .Include(t => t.Owner)
                .Include(t => t.Images)
                .FirstOrDefaultAsync(t => t.Id == command.Id && !t.IsDeleted);

            if (tool == null)
            {
                return ApiResponse<ToolDto>.CreateFailure("Tool not found");
            }

            // Check if the user is the owner
            if (tool.OwnerId != command.OwnerId)
            {
                return ApiResponse<ToolDto>.CreateFailure("You can only update your own tools");
            }

            // Update tool properties
            tool.Name = command.Name;
            tool.Description = command.Description;
            tool.Category = command.Category;
            tool.Brand = command.Brand;
            tool.Model = command.Model;
            tool.DailyRate = command.DailyRate;
            tool.WeeklyRate = command.WeeklyRate;
            tool.MonthlyRate = command.MonthlyRate;
            tool.DepositRequired = command.DepositRequired;
            tool.Condition = command.Condition;
            tool.LocationDisplay = command.EnhancedLocation?.LocationDisplay;
            tool.IsAvailable = command.IsAvailable;
            tool.LeadTimeHours = command.LeadTimeHours;
            tool.Tags = command.Tags ?? string.Empty;
            tool.UpdatedAt = DateTime.UtcNow;

            // Explicitly mark the tool entity as modified to ensure EF tracks changes
            _context.Entry(tool).State = EntityState.Modified;

            // Handle images separately to avoid change tracking issues
            if (command.ImageUrls != null)
            {
                // Collect old image URLs for cleanup before removing from database
                var oldImageUrls = new List<string>();
                if (tool.Images != null && tool.Images.Any())
                {
                    oldImageUrls.AddRange(tool.Images.Select(img => img.ImageUrl));
                    // Remove images explicitly
                    foreach (var image in tool.Images.ToList())
                    {
                        _context.ToolImages.Remove(image);
                    }
                    tool.Images.Clear();
                }

                // Add new images explicitly
                var newImages = new List<ToolImage>();
                foreach (var url in command.ImageUrls)
                {
                    var newImage = new ToolImage
                    {
                        Id = Guid.NewGuid(),
                        ToolId = tool.Id,
                        ImageUrl = url,
                        CreatedAt = DateTime.UtcNow
                    };
                    newImages.Add(newImage);
                    _context.ToolImages.Add(newImage);
                    tool.Images.Add(newImage);
                }

                // Save all changes in one transaction
                await _context.SaveChangesAsync();

                // Clean up old images from storage (best effort - don't fail the operation if cleanup fails)
                await CleanupOldImagesAsync(oldImageUrls, command.ImageUrls);
            }
            else
            {
                // Save changes for tool properties only
                await _context.SaveChangesAsync();
            }

            var toolDto = MapToolToDto(tool);
            return ApiResponse<ToolDto>.CreateSuccess(toolDto, "Tool updated successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<ToolDto>.CreateFailure($"Error updating tool: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> DeleteToolAsync(DeleteToolCommand command)
    {
        try
        {
            var tool = await _context.Tools
                .FirstOrDefaultAsync(t => t.Id == command.Id && !t.IsDeleted);

            if (tool == null)
            {
                return ApiResponse<bool>.CreateFailure("Tool not found");
            }

            // Check if the user is the owner
            if (tool.OwnerId != command.OwnerId)
            {
                return ApiResponse<bool>.CreateFailure("You can only delete your own tools");
            }

            // Check if tool has active rentals
            var hasActiveRentals = await _context.Rentals
                .AnyAsync(r => r.ToolId == command.Id && 
                          r.Status != RentalStatus.Returned && 
                          r.Status != RentalStatus.Cancelled);

            if (hasActiveRentals)
            {
                return ApiResponse<bool>.CreateFailure("Cannot delete tool with active rentals");
            }

            // Soft delete
            tool.IsDeleted = true;
            tool.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.CreateSuccess(true, "Tool deleted successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.CreateFailure($"Error deleting tool: {ex.Message}");
        }
    }

    /// <summary>
    /// Cleans up old image files from storage when tool images are updated.
    /// This is best-effort cleanup - failures are logged but don't fail the main operation.
    /// </summary>
    private async Task CleanupOldImagesAsync(List<string> oldImageUrls, List<string> newImageUrls)
    {
        if (!oldImageUrls.Any()) return;

        try
        {
            // Find images that are being removed (in old but not in new)
            var imagesToDelete = oldImageUrls.Except(newImageUrls).ToList();
            
            foreach (var imageUrl in imagesToDelete)
            {
                try
                {
                    var storagePath = ExtractStoragePathFromUrl(imageUrl);
                    if (!string.IsNullOrEmpty(storagePath))
                    {
                        var deleted = await _fileStorageService.DeleteFileAsync(storagePath);
                        if (deleted)
                        {
                            _logger.LogInformation("Cleaned up old tool image: {ImageUrl}", imageUrl);
                        }
                        else
                        {
                            _logger.LogWarning("Old tool image not found or already deleted: {ImageUrl}", imageUrl);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete old tool image: {ImageUrl}", imageUrl);
                    // Continue with other images - don't let one failure stop cleanup
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during tool image cleanup process");
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

    public async Task<ApiResponse<bool>> IncrementViewCountAsync(Guid toolId)
    {
        try
        {
            var tool = await _context.Tools.FirstOrDefaultAsync(t => t.Id == toolId && !t.IsDeleted);
            if (tool == null)
                return ApiResponse<bool>.CreateFailure("Tool not found");

            tool.ViewCount++;
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.CreateSuccess(true, "View count incremented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing view count for tool {ToolId}", toolId);
            return ApiResponse<bool>.CreateFailure("Error incrementing view count");
        }
    }

    public async Task<ApiResponse<PagedResult<ToolReviewDto>>> GetToolReviewsAsync(Guid toolId, int page, int pageSize)
    {
        try
        {
            var query = _context.Reviews
                .Include(r => r.Reviewer)
                .Where(r => r.ToolId == toolId && r.Type == ReviewType.ToolReview)
                .OrderByDescending(r => r.CreatedAt);

            var totalCount = await query.CountAsync();
            var reviews = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ToolReviewDto
                {
                    Id = r.Id,
                    ToolId = r.ToolId.Value,
                    ReviewerId = r.ReviewerId,
                    ReviewerName = $"{r.Reviewer.FirstName} {r.Reviewer.LastName}".Trim(),
                    ReviewerAvatar = r.Reviewer.ProfilePictureUrl ?? "",
                    Rating = r.Rating,
                    Title = r.Title,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            var result = new PagedResult<ToolReviewDto>
            {
                Items = reviews,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return ApiResponse<PagedResult<ToolReviewDto>>.CreateSuccess(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tool reviews for {ToolId}", toolId);
            return ApiResponse<PagedResult<ToolReviewDto>>.CreateFailure("Error retrieving tool reviews");
        }
    }

    public async Task<ApiResponse<ToolReviewDto>> CreateToolReviewAsync(Guid toolId, string userId, CreateToolReviewRequest request)
    {
        try
        {
            // Check if tool exists
            var tool = await _context.Tools.FirstOrDefaultAsync(t => t.Id == toolId && !t.IsDeleted);
            if (tool == null)
                return ApiResponse<ToolReviewDto>.CreateFailure("Tool not found");

            // Check if user has already reviewed this tool
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ToolId == toolId && r.ReviewerId == userId && r.Type == ReviewType.ToolReview);
            if (existingReview != null)
                return ApiResponse<ToolReviewDto>.CreateFailure("You have already reviewed this tool");

            // Check if user has rented this tool (optional business rule)
            var hasRented = await _context.Rentals
                .AnyAsync(r => r.ToolId == toolId && r.RenterId == userId && r.Status == RentalStatus.Returned);

            if (!hasRented)
                return ApiResponse<ToolReviewDto>.CreateFailure("You can only review tools you have rented and returned");

            var review = new Review
            {
                Id = Guid.NewGuid(),
                ToolId = toolId,
                ReviewerId = userId,
                RevieweeId = tool.OwnerId,
                Rating = request.Rating,
                Title = request.Title,
                Comment = request.Comment,
                Type = ReviewType.ToolReview,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Update tool statistics
            await UpdateToolStatisticsAsync(toolId);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var reviewDto = new ToolReviewDto
            {
                Id = review.Id,
                ToolId = toolId,
                ReviewerId = userId,
                ReviewerName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "Unknown",
                Rating = review.Rating,
                Title = review.Title,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt
            };

            return ApiResponse<ToolReviewDto>.CreateSuccess(reviewDto, "Review created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tool review for {ToolId}", toolId);
            return ApiResponse<ToolReviewDto>.CreateFailure("Error creating review");
        }
    }

    public async Task<ApiResponse<ToolReviewSummaryDto>> GetToolReviewSummaryAsync(Guid toolId)
    {
        try
        {
            var reviews = await _context.Reviews
                .Where(r => r.ToolId == toolId && r.Type == ReviewType.ToolReview)
                .Include(r => r.Reviewer)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var summary = new ToolReviewSummaryDto
            {
                TotalReviews = reviews.Count,
                AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
                FiveStarCount = reviews.Count(r => r.Rating == 5),
                FourStarCount = reviews.Count(r => r.Rating == 4),
                ThreeStarCount = reviews.Count(r => r.Rating == 3),
                TwoStarCount = reviews.Count(r => r.Rating == 2),
                OneStarCount = reviews.Count(r => r.Rating == 1),
                LatestReviews = reviews.Take(3).Select(r => new ToolReviewDto
                {
                    Id = r.Id,
                    ToolId = r.ToolId!.Value,
                    ReviewerId = r.ReviewerId,
                    ReviewerName = r.Reviewer.FirstName + " " + r.Reviewer.LastName,
                    ReviewerAvatar = r.Reviewer.ProfilePictureUrl ?? "",
                    Rating = r.Rating,
                    Title = r.Title,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                }).ToList()
            };

            return ApiResponse<ToolReviewSummaryDto>.CreateSuccess(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tool review summary for {ToolId}", toolId);
            return ApiResponse<ToolReviewSummaryDto>.CreateFailure("Error retrieving review summary");
        }
    }

    public async Task<ApiResponse<List<ToolDto>>> GetFeaturedToolsAsync(int count)
    {
        try
        {
            var tools = await _context.Tools
                .Include(t => t.Owner)
                .Include(t => t.Images)
                .Where(t => t.IsFeatured && t.IsApproved && !t.IsDeleted)
                .OrderByDescending(t => t.AverageRating)
                .ThenByDescending(t => t.ViewCount)
                .Take(count)
                .ToListAsync();

            var toolDtos = MapToolsToDto(tools);
            return ApiResponse<List<ToolDto>>.CreateSuccess(toolDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting featured tools");
            return ApiResponse<List<ToolDto>>.CreateFailure("Error retrieving featured tools");
        }
    }

    public async Task<ApiResponse<List<ToolDto>>> GetPopularToolsAsync(int count)
    {
        try
        {
            var tools = await _context.Tools
                .Include(t => t.Owner)
                .Include(t => t.Images)
                .Where(t => t.IsApproved && !t.IsDeleted)
                .OrderByDescending(t => t.ViewCount)
                .ThenByDescending(t => t.AverageRating)
                .ThenByDescending(t => t.ReviewCount)
                .Take(count)
                .ToListAsync();

            var toolDtos = MapToolsToDto(tools);
            return ApiResponse<List<ToolDto>>.CreateSuccess(toolDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular tools");
            return ApiResponse<List<ToolDto>>.CreateFailure("Error retrieving popular tools");
        }
    }

    public async Task<ApiResponse<List<TagDto>>> GetPopularTagsAsync(int count)
    {
        try
        {
            var tags = await _context.Tools
                .Where(t => t.IsApproved && !t.IsDeleted && !string.IsNullOrEmpty(t.Tags))
                .Select(t => t.Tags)
                .ToListAsync();

            var tagCounts = new Dictionary<string, int>();
            foreach (var tagString in tags)
            {
                var individualTags = tagString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var tag in individualTags)
                {
                    var cleanTag = tag.Trim().ToLower();
                    if (!string.IsNullOrEmpty(cleanTag))
                    {
                        tagCounts[cleanTag] = tagCounts.GetValueOrDefault(cleanTag, 0) + 1;
                    }
                }
            }

            var popularTags = tagCounts
                .OrderByDescending(kvp => kvp.Value)
                .Take(count)
                .Select(kvp => new TagDto { Name = kvp.Key, Count = kvp.Value })
                .ToList();

            return ApiResponse<List<TagDto>>.CreateSuccess(popularTags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular tags");
            return ApiResponse<List<TagDto>>.CreateFailure("Error retrieving popular tags");
        }
    }

    public async Task<ApiResponse<PagedResult<ToolDto>>> SearchToolsAsync(SearchToolsQuery query)
    {
        try
        {
            var toolsQuery = _context.Tools
                .Include(t => t.Owner)
                .Include(t => t.Images)
                .Where(t => !t.IsDeleted && t.IsApproved);

            // Apply filters
            if (!string.IsNullOrEmpty(query.Query))
            {
                var searchTerm = query.Query.ToLower();
                toolsQuery = toolsQuery.Where(t =>
                    t.Name.ToLower().Contains(searchTerm) ||
                    t.Description.ToLower().Contains(searchTerm) ||
                    t.Brand.ToLower().Contains(searchTerm) ||
                    t.Tags.ToLower().Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(query.Category))
                toolsQuery = toolsQuery.Where(t => t.Category.ToLower() == query.Category.ToLower());

            if (!string.IsNullOrEmpty(query.Tags))
            {
                var tagList = query.Tags.ToLower().Split(',').Select(t => t.Trim()).ToList();
                toolsQuery = toolsQuery.Where(t => tagList.Any(tag => t.Tags.ToLower().Contains(tag)));
            }

            if (query.MinPrice.HasValue)
                toolsQuery = toolsQuery.Where(t => t.DailyRate >= query.MinPrice.Value);

            if (query.MaxPrice.HasValue)
                toolsQuery = toolsQuery.Where(t => t.DailyRate <= query.MaxPrice.Value);

            if (!string.IsNullOrEmpty(query.LocationSearch?.LocationQuery))
                toolsQuery = toolsQuery.Where(t => t.LocationDisplay.ToLower().Contains(query.LocationSearch.LocationQuery.ToLower()));

            if (query.IsAvailable.HasValue)
                toolsQuery = toolsQuery.Where(t => t.IsAvailable == query.IsAvailable.Value);

            if (query.IsFeatured.HasValue)
                toolsQuery = toolsQuery.Where(t => t.IsFeatured == query.IsFeatured.Value);

            if (query.MinRating.HasValue)
                toolsQuery = toolsQuery.Where(t => t.AverageRating >= query.MinRating.Value);

            // Apply sorting
            toolsQuery = query.SortBy?.ToLower() switch
            {
                "price_low" => toolsQuery.OrderBy(t => t.DailyRate),
                "price_high" => toolsQuery.OrderByDescending(t => t.DailyRate),
                "rating" => toolsQuery.OrderByDescending(t => t.AverageRating).ThenByDescending(t => t.ReviewCount),
                "newest" => toolsQuery.OrderByDescending(t => t.CreatedAt),
                "popular" => toolsQuery.OrderByDescending(t => t.ViewCount).ThenByDescending(t => t.AverageRating),
                _ => toolsQuery.OrderByDescending(t => t.IsFeatured).ThenByDescending(t => t.AverageRating)
            };

            var totalCount = await toolsQuery.CountAsync();
            var tools = await toolsQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var toolDtos = MapToolsToDto(tools);

            var result = new PagedResult<ToolDto>
            {
                Items = toolDtos,
                TotalCount = totalCount,
                PageNumber = query.Page,
                PageSize = query.PageSize
            };

            return ApiResponse<PagedResult<ToolDto>>.CreateSuccess(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching tools");
            return ApiResponse<PagedResult<ToolDto>>.CreateFailure("Error searching tools");
        }
    }

    public async Task<ApiResponse<bool>> RequestApprovalAsync(Guid toolId, string userId, RequestApprovalRequest request)
    {
        try
        {
            var tool = await _context.Tools
                .FirstOrDefaultAsync(t => t.Id == toolId && t.OwnerId == userId && !t.IsDeleted);

            if (tool == null)
            {
                return ApiResponse<bool>.CreateFailure("Tool not found or you don't have permission to request approval.");
            }

            if (tool.IsApproved)
            {
                return ApiResponse<bool>.CreateFailure("Tool is already approved.");
            }

            if (tool.PendingApproval && string.IsNullOrEmpty(tool.RejectionReason))
            {
                return ApiResponse<bool>.CreateFailure("Tool is already pending approval.");
            }

            // Reset approval status
            tool.PendingApproval = true;
            tool.RejectionReason = null;
            tool.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Tool {ToolId} approval requested by user {UserId}", toolId, userId);
            return ApiResponse<bool>.CreateSuccess(true, "Approval requested successfully. Your tool will be reviewed by our team.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting approval for tool {ToolId} by user {UserId}", toolId, userId);
            return ApiResponse<bool>.CreateFailure("An error occurred while requesting approval.");
        }
    }

    private async Task UpdateToolStatisticsAsync(Guid toolId)
    {
        try
        {
            var tool = await _context.Tools.FirstOrDefaultAsync(t => t.Id == toolId);
            if (tool != null)
            {
                var reviews = await _context.Reviews
                    .Where(r => r.ToolId == toolId && r.Type == ReviewType.ToolReview)
                    .ToListAsync();

                if (reviews.Any())
                {
                    tool.AverageRating = (decimal)reviews.Average(r => r.Rating);
                    tool.ReviewCount = reviews.Count;
                }

                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tool statistics for {ToolId}", toolId);
        }
    }

    public async Task<ApiResponse<bool>> CanUserReviewToolAsync(Guid toolId, string userId)
    {
        try
        {
            // Check if tool exists
            var tool = await _context.Tools.FirstOrDefaultAsync(t => t.Id == toolId && !t.IsDeleted);
            if (tool == null)
            {
                return ApiResponse<bool>.CreateFailure("Tool not found");
            }

            // Users cannot review their own tools
            if (tool.OwnerId == userId)
            {
                return ApiResponse<bool>.CreateFailure("You cannot review your own tool");
            }

            // Check if user has already reviewed this tool
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ToolId == toolId && r.ReviewerId == userId && r.Type == ReviewType.ToolReview);
            if (existingReview != null)
            {
                return ApiResponse<bool>.CreateFailure("You have already reviewed this tool");
            }

            // Check if user has rented and returned this tool
            var hasRented = await _context.Rentals
                .AnyAsync(r => r.ToolId == toolId && r.RenterId == userId && r.Status == RentalStatus.Returned);

            if (!hasRented)
            {
                return ApiResponse<bool>.CreateFailure("You can only review tools you have rented and returned");
            }

            return ApiResponse<bool>.CreateSuccess(true, "User can review this tool");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user can review tool {ToolId} for user {UserId}", toolId, userId);
            return ApiResponse<bool>.CreateFailure("Error checking review eligibility");
        }
    }

    private ToolDto MapToolToDto(Tool tool)
    {
        var toolDto = _mapper.Map<ToolDto>(tool);
        
        // Location is now handled through EnhancedLocation field populated from tool.LocationDisplay with owner fallback
        
        return toolDto;
    }

    private List<ToolDto> MapToolsToDto(List<Tool> tools)
    {
        return tools.Select(MapToolToDto).ToList();
    }

    private async Task<IQueryable<Tool>> ApplyLocationFilterAsync(IQueryable<Tool> toolsQuery, LocationSearchRequest locationSearch)
    {
        try
        {
            _logger.LogDebug("🔍 DEBUG: Starting ApplyLocationFilterAsync for Tools");
            
            // Early return if no location search criteria
            if (!locationSearch.Lat.HasValue && !locationSearch.Lng.HasValue && 
                string.IsNullOrEmpty(locationSearch.LocationQuery))
            {
                _logger.LogDebug("🔍 DEBUG: No location criteria provided, returning original query");
                return toolsQuery; // No location filtering
            }
            
            _logger.LogDebug("🔍 DEBUG: Location criteria found - Lat: {Lat}, Lng: {Lng}, Query: '{Query}'", 
                locationSearch.Lat, locationSearch.Lng, locationSearch.LocationQuery);

            // If only coordinates provided, reverse geocode them first to get location string
            if ((locationSearch.Lat.HasValue && locationSearch.Lng.HasValue) && 
                string.IsNullOrEmpty(locationSearch.LocationQuery))
            {
                try 
                {
                    _logger.LogDebug("Starting reverse geocoding for coordinates ({Lat}, {Lng})", 
                        locationSearch.Lat, locationSearch.Lng);
                    
                    var reverseGeocodedLocation = await _geocodingService.ReverseGeocodeAsync(
                        locationSearch.Lat.Value, locationSearch.Lng.Value);
                    
                    if (reverseGeocodedLocation != null && !string.IsNullOrEmpty(reverseGeocodedLocation.DisplayName))
                    {
                        locationSearch.LocationQuery = reverseGeocodedLocation.DisplayName;
                        _logger.LogDebug("Reverse geocoding successful: '{LocationName}'", 
                            reverseGeocodedLocation.DisplayName);
                    }
                    else
                    {
                        _logger.LogDebug("Reverse geocoding returned no results for coordinates ({Lat}, {Lng})", 
                            locationSearch.Lat, locationSearch.Lng);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to reverse geocode coordinates ({Lat}, {Lng})", 
                        locationSearch.Lat, locationSearch.Lng);
                    // Continue with proximity search if reverse geocoding fails
                }
            }

            // Set default radius if coordinates provided but no radius specified
            if ((locationSearch.Lat.HasValue && locationSearch.Lng.HasValue) && !locationSearch.RadiusKm.HasValue)
            {
                locationSearch.RadiusKm = 5; // Default 5km radius
                _logger.LogDebug("Applied default radius of 5km for coordinate search");
            }

            // Determine search input type (after potential reverse geocoding)
            bool queryHasCoords = locationSearch.Lat.HasValue && locationSearch.Lng.HasValue;
            bool queryHasLocation = !string.IsNullOrEmpty(locationSearch.LocationQuery);

            _logger.LogDebug("Location search: QueryHasCoords={QueryHasCoords}, QueryHasLocation={QueryHasLocation}, Radius={Radius}km", 
                queryHasCoords, queryHasLocation, locationSearch.RadiusKm);

            // Step 3: Combined location search - proximity + text search for items without coordinates
            var combinedLocationToolIds = new List<Guid>();

            // Step 3a: Proximity search for items with coordinates
            if (queryHasCoords)
            {
                _logger.LogDebug("🔍 DEBUG: Starting proximity search for coordinates");
                var centerLat = locationSearch.Lat.Value;
                var centerLng = locationSearch.Lng.Value;
                var radiusKm = locationSearch.RadiusKm.Value;

                // Proximity search for tools with coordinates
                _logger.LogDebug("🔍 DEBUG: About to execute proximity search database query");
                var proximityToolIds = (from t in _context.Tools
                                       join u in _context.Users on t.OwnerId equals u.Id into userJoin
                                       from user in userJoin.DefaultIfEmpty()
                                       where 
                                           // Direct tool coordinates within radius
                                           (t.LocationLat.HasValue && t.LocationLng.HasValue &&
                                            (6371 * Math.Acos(
                                                Math.Cos(Math.PI * (double)centerLat / 180.0) *
                                                Math.Cos(Math.PI * (double)t.LocationLat.Value / 180.0) *
                                                Math.Cos(Math.PI * ((double)t.LocationLng.Value - (double)centerLng) / 180.0) +
                                                Math.Sin(Math.PI * (double)centerLat / 180.0) *
                                                Math.Sin(Math.PI * (double)t.LocationLat.Value / 180.0)
                                            )) <= radiusKm) ||
                                           // OR inherited coordinates from owner within radius
                                           (t.LocationInheritanceOption == Core.Enums.LocationInheritanceOption.InheritFromProfile &&
                                            user != null && user.LocationLat.HasValue && user.LocationLng.HasValue &&
                                            (6371 * Math.Acos(
                                                Math.Cos(Math.PI * (double)centerLat / 180.0) *
                                                Math.Cos(Math.PI * (double)user.LocationLat.Value / 180.0) *
                                                Math.Cos(Math.PI * ((double)user.LocationLng.Value - (double)centerLng) / 180.0) +
                                                Math.Sin(Math.PI * (double)centerLat / 180.0) *
                                                Math.Sin(Math.PI * (double)user.LocationLat.Value / 180.0)
                                            )) <= radiusKm)
                                       select t.Id).Distinct();

                _logger.LogDebug("🔍 DEBUG: Executing ToList() on proximity search query");
                var proximityResults = proximityToolIds.ToList();
                _logger.LogDebug("🔍 DEBUG: Proximity search completed, found {Count} results", proximityResults.Count);
                
                if (proximityResults.Any())
                {
                    _logger.LogDebug("Proximity search found {Count} tools within {Radius}km", proximityResults.Count, radiusKm);
                    combinedLocationToolIds.AddRange(proximityResults);
                }
                else
                {
                    _logger.LogDebug("No tools found within {Radius}km of coordinates", radiusKm);
                }
            }

            // Step 3b: Text-based search for ALL items with matching location text (with or without coordinates)
            if (queryHasLocation)
            {
                var locationQuery = locationSearch.LocationQuery.ToLower();
                _logger.LogDebug("Applying text-based tool location filter for ALL items with matching text: '{Query}'", locationQuery);

                var textMatchingToolIds = (from t in _context.Tools
                                          join u in _context.Users on t.OwnerId equals u.Id into userJoin
                                          from user in userJoin.DefaultIfEmpty()
                                          where 
                                              // All items with location text that matches (regardless of coordinates)
                                              // Enhanced partial matching: split query into words and match any word
                                              (
                                                  // Direct tool has location text that matches
                                                  locationQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(word =>
                                                      (t.LocationDisplay != null && t.LocationDisplay.ToLower().Contains(word)) ||
                                                      (t.LocationCity != null && t.LocationCity.ToLower().Contains(word)) ||
                                                      (t.LocationState != null && t.LocationState.ToLower().Contains(word)) ||
                                                      (t.LocationCountry != null && t.LocationCountry.ToLower().Contains(word)) ||
                                                      (t.LocationArea != null && t.LocationArea.ToLower().Contains(word))
                                                  )
                                              ) ||
                                              // OR inherited location: tool inherits from user with matching location text
                                              (
                                                  t.LocationInheritanceOption == Core.Enums.LocationInheritanceOption.InheritFromProfile &&
                                                  user != null &&
                                                  locationQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(word =>
                                                      (user.LocationDisplay != null && user.LocationDisplay.ToLower().Contains(word)) ||
                                                      (user.LocationCity != null && user.LocationCity.ToLower().Contains(word)) ||
                                                      (user.LocationState != null && user.LocationState.ToLower().Contains(word)) ||
                                                      (user.LocationCountry != null && user.LocationCountry.ToLower().Contains(word)) ||
                                                      (user.LocationArea != null && user.LocationArea.ToLower().Contains(word))
                                                  )
                                              )
                                          select t.Id).Distinct();

                var textResults = textMatchingToolIds.ToList();
                
                if (textResults.Any())
                {
                    _logger.LogDebug("Text search found {Count} tools with matching location text '{Query}'", textResults.Count, locationQuery);
                    combinedLocationToolIds.AddRange(textResults);
                }
                else
                {
                    _logger.LogDebug("No tools found matching text query: '{Query}'", locationQuery);
                }
            }

            // Step 3c: Apply combined location filtering if we have any location-based results
            if (combinedLocationToolIds.Any())
            {
                var uniqueLocationToolIds = combinedLocationToolIds.Distinct().ToList();
                _logger.LogDebug("Combined location search found {Count} total tools (proximity + text matching)", uniqueLocationToolIds.Count);
                toolsQuery = toolsQuery.Where(t => uniqueLocationToolIds.Contains(t.Id));
            }
            else if (queryHasCoords || queryHasLocation)
            {
                _logger.LogDebug("No tools found matching location criteria - returning empty result set");
                // If location search was requested but no results found, return empty set
                toolsQuery = toolsQuery.Where(t => false);
            }

            // Step 4: Apply structured location filters if provided using explicit joins
            if (locationSearch.Cities?.Any() == true)
            {
                var cities = locationSearch.Cities.Select(c => c.ToLower()).ToList();
                
                var toolIdsWithDirectCity = _context.Tools
                    .Where(t => t.LocationCity != null && cities.Contains(t.LocationCity.ToLower()))
                    .Select(t => t.Id);

                var toolIdsWithInheritedCity = (from t in _context.Tools
                                               join u in _context.Users on t.OwnerId equals u.Id
                                               where t.LocationInheritanceOption == Core.Enums.LocationInheritanceOption.InheritFromProfile
                                                     && u.LocationCity != null && cities.Contains(u.LocationCity.ToLower())
                                               select t.Id);

                var matchingCityToolIds = toolIdsWithDirectCity.Union(toolIdsWithInheritedCity);
                toolsQuery = toolsQuery.Where(t => matchingCityToolIds.Contains(t.Id));
            }

            if (locationSearch.States?.Any() == true)
            {
                var states = locationSearch.States.Select(s => s.ToLower()).ToList();
                
                var toolIdsWithDirectState = _context.Tools
                    .Where(t => t.LocationState != null && states.Contains(t.LocationState.ToLower()))
                    .Select(t => t.Id);

                var toolIdsWithInheritedState = (from t in _context.Tools
                                                join u in _context.Users on t.OwnerId equals u.Id
                                                where t.LocationInheritanceOption == Core.Enums.LocationInheritanceOption.InheritFromProfile
                                                      && u.LocationState != null && states.Contains(u.LocationState.ToLower())
                                                select t.Id);

                var matchingStateToolIds = toolIdsWithDirectState.Union(toolIdsWithInheritedState);
                toolsQuery = toolsQuery.Where(t => matchingStateToolIds.Contains(t.Id));
            }

            if (locationSearch.Countries?.Any() == true)
            {
                var countries = locationSearch.Countries.Select(c => c.ToLower()).ToList();
                
                var toolIdsWithDirectCountry = _context.Tools
                    .Where(t => t.LocationCountry != null && countries.Contains(t.LocationCountry.ToLower()))
                    .Select(t => t.Id);

                var toolIdsWithInheritedCountry = (from t in _context.Tools
                                                  join u in _context.Users on t.OwnerId equals u.Id
                                                  where t.LocationInheritanceOption == Core.Enums.LocationInheritanceOption.InheritFromProfile
                                                        && u.LocationCountry != null && countries.Contains(u.LocationCountry.ToLower())
                                                  select t.Id);

                var matchingCountryToolIds = toolIdsWithDirectCountry.Union(toolIdsWithInheritedCountry);
                toolsQuery = toolsQuery.Where(t => matchingCountryToolIds.Contains(t.Id));
            }

            return toolsQuery;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying location filter for query: {Query}", locationSearch.LocationQuery);
            // Fall back to simple text search if geocoding fails
            if (!string.IsNullOrEmpty(locationSearch.LocationQuery))
            {
                var locationQuery = locationSearch.LocationQuery.ToLower();
                return toolsQuery.Where(t => t.LocationDisplay != null && t.LocationDisplay.ToLower().Contains(locationQuery));
            }
            return toolsQuery;
        }
    }

    /// <summary>
    /// Applies common filtering logic shared across all GetTools methods
    /// </summary>
    private async Task<IQueryable<Tool>> ApplyCommonFiltersAsync(IQueryable<Tool> toolsQuery, GetToolsQuery query)
    {
        // Apply category filter
        if (!string.IsNullOrEmpty(query.Category))
        {
            toolsQuery = toolsQuery.Where(t => t.Category.ToLower() == query.Category.ToLower());
        }

        // Apply location-based filtering
        if (query.LocationSearch != null && 
            (!string.IsNullOrEmpty(query.LocationSearch.LocationQuery) || 
             query.LocationSearch.Lat.HasValue || 
             query.LocationSearch.Lng.HasValue))
        {
            toolsQuery = await ApplyLocationFilterAsync(toolsQuery, query.LocationSearch);
        }

        // Apply price filter
        if (query.MaxDailyRate.HasValue)
        {
            toolsQuery = toolsQuery.Where(t => t.DailyRate <= query.MaxDailyRate.Value);
        }

        // Apply availability filter
        if (query.AvailableOnly)
        {
            toolsQuery = toolsQuery.Where(t => t.IsAvailable);
        }

        // Apply search term filter
        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            toolsQuery = toolsQuery.Where(t => 
                t.Name.ToLower().Contains(searchTerm) ||
                t.Description.ToLower().Contains(searchTerm) ||
                t.Brand.ToLower().Contains(searchTerm) ||
                t.Model.ToLower().Contains(searchTerm));
        }

        // Apply tags filter
        if (!string.IsNullOrEmpty(query.Tags))
        {
            var tags = query.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(tag => tag.Trim().ToLower())
                .Where(tag => !string.IsNullOrEmpty(tag))
                .ToList();

            if (tags.Any())
            {
                toolsQuery = toolsQuery.Where(t => 
                    tags.All(tag => t.Tags.ToLower().Contains(tag)));
            }
        }

        return toolsQuery;
    }
}