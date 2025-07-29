using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Features.Tools;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Core.DTOs.Tools;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Features.Tools;

public class ToolsService : IToolsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<ToolsService> _logger;

    public ToolsService(
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        ILogger<ToolsService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _context = context;
        _fileStorageService = fileStorageService;
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

            // Apply filters
            if (!string.IsNullOrEmpty(query.Category))
            {
                toolsQuery = toolsQuery.Where(t => t.Category.ToLower() == query.Category.ToLower());
            }

            if (!string.IsNullOrEmpty(query.LocationSearch?.LocationQuery))
            {
                toolsQuery = toolsQuery.Where(t => t.LocationDisplay.ToLower().Contains(query.LocationSearch.LocationQuery.ToLower()));
            }

            if (query.MaxDailyRate.HasValue)
            {
                toolsQuery = toolsQuery.Where(t => t.DailyRate <= query.MaxDailyRate.Value);
            }

            if (query.AvailableOnly)
            {
                toolsQuery = toolsQuery.Where(t => t.IsAvailable);
            }

            // Apply search
            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                var searchTerm = query.SearchTerm.ToLower();
                toolsQuery = toolsQuery.Where(t => 
                    t.Name.ToLower().Contains(searchTerm) ||
                    t.Description.ToLower().Contains(searchTerm) ||
                    t.Brand.ToLower().Contains(searchTerm) ||
                    t.Model.ToLower().Contains(searchTerm));
            }

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

            // Apply filters
            if (!string.IsNullOrEmpty(query.Category))
            {
                toolsQuery = toolsQuery.Where(t => t.Category.ToLower() == query.Category.ToLower());
            }

            if (!string.IsNullOrEmpty(query.LocationSearch?.LocationQuery))
            {
                toolsQuery = toolsQuery.Where(t => t.LocationDisplay.ToLower().Contains(query.LocationSearch.LocationQuery.ToLower()));
            }

            if (query.MaxDailyRate.HasValue)
            {
                toolsQuery = toolsQuery.Where(t => t.DailyRate <= query.MaxDailyRate.Value);
            }

            if (query.AvailableOnly)
            {
                toolsQuery = toolsQuery.Where(t => t.IsAvailable);
            }

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

            // Apply filters (same as GetToolsAsync but with user filter)
            if (!string.IsNullOrEmpty(query.Category))
            {
                toolsQuery = toolsQuery.Where(t => t.Category.ToLower() == query.Category.ToLower());
            }

            if (!string.IsNullOrEmpty(query.LocationSearch?.LocationQuery))
            {
                toolsQuery = toolsQuery.Where(t => t.LocationDisplay.ToLower().Contains(query.LocationSearch.LocationQuery.ToLower()));
            }

            if (query.MaxDailyRate.HasValue)
            {
                toolsQuery = toolsQuery.Where(t => t.DailyRate <= query.MaxDailyRate.Value);
            }

            if (query.AvailableOnly)
            {
                toolsQuery = toolsQuery.Where(t => t.IsAvailable);
            }

            // Apply search
            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                var searchTerm = query.SearchTerm.ToLower();
                toolsQuery = toolsQuery.Where(t => 
                    t.Name.ToLower().Contains(searchTerm) ||
                    t.Description.ToLower().Contains(searchTerm) ||
                    t.Brand.ToLower().Contains(searchTerm) ||
                    t.Model.ToLower().Contains(searchTerm));
            }

            // Apply sorting
            toolsQuery = query.SortBy?.ToLower() switch
            {
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
                LocationDisplay = command.EnhancedLocation?.LocationDisplay,
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
}