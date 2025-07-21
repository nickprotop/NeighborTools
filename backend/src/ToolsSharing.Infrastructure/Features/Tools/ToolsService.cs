using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Features.Tools;
using ToolsSharing.Core.Interfaces;
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

            if (!string.IsNullOrEmpty(query.Location))
            {
                toolsQuery = toolsQuery.Where(t => t.Location.ToLower().Contains(query.Location.ToLower()));
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
            var toolDtos = _mapper.Map<List<ToolDto>>(tools);

            return ApiResponse<List<ToolDto>>.CreateSuccess(toolDtos, "Tools retrieved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<List<ToolDto>>.CreateFailure($"Error retrieving tools: {ex.Message}. Inner: {ex.InnerException?.Message}");
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

            if (!string.IsNullOrEmpty(query.Location))
            {
                toolsQuery = toolsQuery.Where(t => t.Location.ToLower().Contains(query.Location.ToLower()));
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
            var toolDtos = _mapper.Map<List<ToolDto>>(tools);

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

            var toolDto = _mapper.Map<ToolDto>(tool);
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
                Location = command.Location,
                IsAvailable = true,
                LeadTimeHours = command.LeadTimeHours,
                OwnerId = command.OwnerId,
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

            var toolDto = _mapper.Map<ToolDto>(createdTool);
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
            tool.Location = command.Location;
            tool.IsAvailable = command.IsAvailable;
            tool.LeadTimeHours = command.LeadTimeHours;
            tool.UpdatedAt = DateTime.UtcNow;

            // Update images if provided
            if (command.ImageUrls != null)
            {
                // Collect old image URLs for cleanup before removing from database
                var oldImageUrls = new List<string>();
                if (tool.Images != null && tool.Images.Any())
                {
                    oldImageUrls.AddRange(tool.Images.Select(img => img.ImageUrl));
                    _context.ToolImages.RemoveRange(tool.Images);
                }

                // Add new images
                tool.Images = command.ImageUrls.Select(url => new ToolImage
                {
                    Id = Guid.NewGuid(),
                    ToolId = tool.Id,
                    ImageUrl = url,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                // Save changes first to ensure database consistency
                await _context.SaveChangesAsync();

                // Clean up old images from storage (best effort - don't fail the operation if cleanup fails)
                await CleanupOldImagesAsync(oldImageUrls, command.ImageUrls);
            }
            else
            {
                await _context.SaveChangesAsync();
            }

            var toolDto = _mapper.Map<ToolDto>(tool);
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
}