using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MapsterMapper;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.DTOs;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services;

public class FavoritesService : IFavoritesService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<FavoritesService> _logger;

    public FavoritesService(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<FavoritesService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<List<FavoriteDto>>> GetUserFavoritesAsync(string userId)
    {
        try
        {
            var favorites = await _context.Favorites
                .Include(f => f.Tool)
                    .ThenInclude(t => t.Images)
                .Include(f => f.Tool)
                    .ThenInclude(t => t.Owner)
                .Include(f => f.Bundle)
                    .ThenInclude(b => b.User)
                .Include(f => f.Bundle)
                    .ThenInclude(b => b.BundleTools)
                    .ThenInclude(bt => bt.Tool)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            var favoriteDtos = favorites.Select(f => new FavoriteDto
            {
                Id = f.Id,
                UserId = f.UserId,
                ToolId = f.ToolId,
                BundleId = f.BundleId,
                FavoriteType = f.FavoriteType,
                CreatedAt = f.CreatedAt,
                
                // Tool information (if it's a tool favorite)
                ToolName = f.Tool?.Name ?? string.Empty,
                ToolDescription = f.Tool?.Description ?? string.Empty,
                ToolCategory = f.Tool?.Category ?? string.Empty,
                DailyRate = f.Tool?.DailyRate ?? 0,
                ToolCondition = f.Tool?.Condition ?? string.Empty,
                ToolLocation = f.Tool?.Location ?? string.Empty,
                ToolImageUrls = f.Tool?.Images?.Select(img => img.ImageUrl).ToList() ?? new List<string>(),
                IsToolAvailable = f.Tool?.IsAvailable ?? false,
                
                // Bundle information (if it's a bundle favorite)
                BundleName = f.Bundle?.Name ?? string.Empty,
                BundleDescription = f.Bundle?.Description ?? string.Empty,
                BundleCategory = f.Bundle?.Category ?? string.Empty,
                BundleDiscountedCost = f.Bundle != null ? CalculateBundleDiscountedCost(f.Bundle) : 0,
                BundleImageUrl = f.Bundle?.ImageUrl ?? string.Empty,
                IsBundleAvailable = f.Bundle != null && CalculateBundleAvailability(f.Bundle),
                BundleToolCount = f.Bundle?.BundleTools?.Count ?? 0,
                
                // Owner information (common)
                OwnerName = f.FavoriteType == "Tool" 
                    ? (f.Tool?.Owner != null ? $"{f.Tool.Owner.FirstName} {f.Tool.Owner.LastName}".Trim() : string.Empty)
                    : (f.Bundle?.User != null ? $"{f.Bundle.User.FirstName} {f.Bundle.User.LastName}".Trim() : string.Empty),
                OwnerEmail = f.FavoriteType == "Tool" 
                    ? (f.Tool?.Owner?.Email ?? string.Empty)
                    : (f.Bundle?.User?.Email ?? string.Empty)
            }).ToList();

            return ApiResponse<List<FavoriteDto>>.SuccessResult(favoriteDtos, "Favorites retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving favorites for user {UserId}", userId);
            return ApiResponse<List<FavoriteDto>>.ErrorResult("Failed to retrieve favorites");
        }
    }

    private decimal CalculateBundleDiscountedCost(Bundle bundle)
    {
        // Calculate bundle cost based on included tools
        if (bundle.BundleTools?.Any() == true)
        {
            var totalCost = bundle.BundleTools.Sum(bt => bt.Tool?.DailyRate ?? 0);
            var discountAmount = totalCost * (bundle.BundleDiscount / 100);
            return totalCost - discountAmount;
        }
        return 0;
    }

    private bool CalculateBundleAvailability(Bundle bundle)
    {
        // A bundle is available if it's published, approved, and all its tools are available
        if (!bundle.IsPublished || !bundle.IsApproved)
            return false;

        // Check if all required tools in the bundle are available
        if (bundle.BundleTools?.Any() == true)
        {
            return bundle.BundleTools
                .Where(bt => !bt.IsOptional) // Only check required tools
                .All(bt => bt.Tool?.IsAvailable == true);
        }

        return false; // Bundle with no tools is not available
    }

    public async Task<ApiResponse<FavoriteStatusDto>> CheckFavoriteStatusAsync(string userId, Guid toolId)
    {
        try
        {
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ToolId == toolId && f.FavoriteType == "Tool");

            var status = new FavoriteStatusDto
            {
                IsFavorited = favorite != null,
                FavoriteId = favorite?.Id
            };

            return ApiResponse<FavoriteStatusDto>.SuccessResult(status, "Favorite status checked successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking favorite status for user {UserId} and tool {ToolId}", userId, toolId);
            return ApiResponse<FavoriteStatusDto>.ErrorResult("Failed to check favorite status");
        }
    }

    public async Task<ApiResponse<FavoriteDto>> AddToFavoritesAsync(string userId, Guid toolId)
    {
        try
        {
            // Check if tool exists
            var tool = await _context.Tools
                .Include(t => t.Images)
                .Include(t => t.Owner)
                .FirstOrDefaultAsync(t => t.Id == toolId);

            if (tool == null)
            {
                return ApiResponse<FavoriteDto>.ErrorResult("Tool not found");
            }

            // Check if user can't favorite their own tool
            if (tool.OwnerId == userId)
            {
                return ApiResponse<FavoriteDto>.ErrorResult("You cannot favorite your own tool");
            }

            // Check if already favorited
            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ToolId == toolId);

            if (existingFavorite != null)
            {
                // Return existing favorite
                var existingFavoriteDto = new FavoriteDto
                {
                    Id = existingFavorite.Id,
                    UserId = existingFavorite.UserId,
                    ToolId = existingFavorite.ToolId ?? Guid.Empty,
                    CreatedAt = existingFavorite.CreatedAt,
                    ToolName = tool.Name,
                    ToolDescription = tool.Description,
                    ToolCategory = tool.Category,
                    DailyRate = tool.DailyRate,
                    ToolCondition = tool.Condition,
                    ToolLocation = tool.Location,
                    ToolImageUrls = tool.Images.Select(img => img.ImageUrl).ToList(),
                    IsToolAvailable = tool.IsAvailable,
                    OwnerName = $"{tool.Owner.FirstName} {tool.Owner.LastName}".Trim(),
                    OwnerEmail = tool.Owner.Email ?? string.Empty
                };

                return ApiResponse<FavoriteDto>.SuccessResult(existingFavoriteDto, "Tool is already in favorites");
            }

            // Create new favorite
            var favorite = new Favorite
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ToolId = toolId,
                FavoriteType = "Tool"
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            var favoriteDto = new FavoriteDto
            {
                Id = favorite.Id,
                UserId = favorite.UserId,
                ToolId = favorite.ToolId ?? Guid.Empty,
                CreatedAt = favorite.CreatedAt,
                ToolName = tool.Name,
                ToolDescription = tool.Description,
                ToolCategory = tool.Category,
                DailyRate = tool.DailyRate,
                ToolCondition = tool.Condition,
                ToolLocation = tool.Location,
                ToolImageUrls = tool.Images.Select(img => img.ImageUrl).ToList(),
                IsToolAvailable = tool.IsAvailable,
                OwnerName = $"{tool.Owner.FirstName} {tool.Owner.LastName}".Trim(),
                OwnerEmail = tool.Owner.Email ?? string.Empty
            };

            return ApiResponse<FavoriteDto>.SuccessResult(favoriteDto, "Tool added to favorites successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding tool {ToolId} to favorites for user {UserId}", toolId, userId);
            return ApiResponse<FavoriteDto>.ErrorResult("Failed to add tool to favorites");
        }
    }

    public async Task<ApiResponse<bool>> RemoveFromFavoritesAsync(string userId, Guid toolId)
    {
        try
        {
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ToolId == toolId && f.FavoriteType == "Tool");

            if (favorite == null)
            {
                return ApiResponse<bool>.ErrorResult("Favorite not found");
            }

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResult(true, "Tool removed from favorites successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tool {ToolId} from favorites for user {UserId}", toolId, userId);
            return ApiResponse<bool>.ErrorResult("Failed to remove tool from favorites");
        }
    }

    public async Task<ApiResponse<bool>> RemoveFromFavoritesByIdAsync(string userId, Guid favoriteId)
    {
        try
        {
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.Id == favoriteId && f.UserId == userId);

            if (favorite == null)
            {
                return ApiResponse<bool>.ErrorResult("Favorite not found");
            }

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResult(true, "Favorite removed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing favorite {FavoriteId} for user {UserId}", favoriteId, userId);
            return ApiResponse<bool>.ErrorResult("Failed to remove favorite");
        }
    }

    public async Task<ApiResponse<int>> GetUserFavoritesCountAsync(string userId)
    {
        try
        {
            var count = await _context.Favorites
                .CountAsync(f => f.UserId == userId && f.FavoriteType == "Tool");

            return ApiResponse<int>.SuccessResult(count, "Favorites count retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting favorites count for user {UserId}", userId);
            return ApiResponse<int>.ErrorResult("Failed to get favorites count");
        }
    }

    // Bundle favorites methods
    public async Task<ApiResponse<FavoriteStatusDto>> CheckBundleFavoriteStatusAsync(string userId, Guid bundleId)
    {
        try
        {
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.BundleId == bundleId && f.FavoriteType == "Bundle");

            var status = new FavoriteStatusDto
            {
                IsFavorited = favorite != null,
                FavoriteId = favorite?.Id
            };

            return ApiResponse<FavoriteStatusDto>.SuccessResult(status, "Bundle favorite status checked successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking bundle favorite status for user {UserId} and bundle {BundleId}", userId, bundleId);
            return ApiResponse<FavoriteStatusDto>.ErrorResult("Failed to check bundle favorite status");
        }
    }

    public async Task<ApiResponse<FavoriteDto>> AddBundleToFavoritesAsync(string userId, Guid bundleId)
    {
        try
        {
            // Check if bundle exists
            var bundle = await _context.Bundles
                .Include(b => b.User)
                .Include(b => b.BundleTools)
                    .ThenInclude(bt => bt.Tool)
                .FirstOrDefaultAsync(b => b.Id == bundleId);

            if (bundle == null)
            {
                return ApiResponse<FavoriteDto>.ErrorResult("Bundle not found");
            }

            // Check if user can't favorite their own bundle
            if (bundle.UserId == userId)
            {
                return ApiResponse<FavoriteDto>.ErrorResult("You cannot favorite your own bundle");
            }

            // Check if already favorited
            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.BundleId == bundleId && f.FavoriteType == "Bundle");

            if (existingFavorite != null)
            {
                // Return existing favorite
                var existingFavoriteDto = new FavoriteDto
                {
                    Id = existingFavorite.Id,
                    UserId = existingFavorite.UserId,
                    BundleId = existingFavorite.BundleId,
                    FavoriteType = existingFavorite.FavoriteType,
                    CreatedAt = existingFavorite.CreatedAt,
                    BundleName = bundle.Name,
                    BundleDescription = bundle.Description,
                    BundleCategory = bundle.Category,
                    BundleDiscountedCost = CalculateBundleDiscountedCost(bundle),
                    BundleImageUrl = bundle.ImageUrl ?? string.Empty,
                    IsBundleAvailable = CalculateBundleAvailability(bundle),
                    BundleToolCount = bundle.BundleTools.Count,
                    OwnerName = $"{bundle.User.FirstName} {bundle.User.LastName}".Trim(),
                    OwnerEmail = bundle.User.Email ?? string.Empty
                };

                return ApiResponse<FavoriteDto>.SuccessResult(existingFavoriteDto, "Bundle is already in favorites");
            }

            // Create new favorite
            var favorite = new Favorite
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                BundleId = bundleId,
                FavoriteType = "Bundle"
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            var favoriteDto = new FavoriteDto
            {
                Id = favorite.Id,
                UserId = favorite.UserId,
                BundleId = favorite.BundleId,
                FavoriteType = favorite.FavoriteType,
                CreatedAt = favorite.CreatedAt,
                BundleName = bundle.Name,
                BundleDescription = bundle.Description,
                BundleCategory = bundle.Category,
                BundleDiscountedCost = CalculateBundleDiscountedCost(bundle),
                BundleImageUrl = bundle.ImageUrl ?? string.Empty,
                IsBundleAvailable = CalculateBundleAvailability(bundle),
                BundleToolCount = bundle.BundleTools.Count,
                OwnerName = $"{bundle.User.FirstName} {bundle.User.LastName}".Trim(),
                OwnerEmail = bundle.User.Email ?? string.Empty
            };

            return ApiResponse<FavoriteDto>.SuccessResult(favoriteDto, "Bundle added to favorites successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding bundle {BundleId} to favorites for user {UserId}", bundleId, userId);
            return ApiResponse<FavoriteDto>.ErrorResult("Failed to add bundle to favorites");
        }
    }

    public async Task<ApiResponse<bool>> RemoveBundleFromFavoritesAsync(string userId, Guid bundleId)
    {
        try
        {
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.BundleId == bundleId && f.FavoriteType == "Bundle");

            if (favorite == null)
            {
                return ApiResponse<bool>.ErrorResult("Bundle favorite not found");
            }

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResult(true, "Bundle removed from favorites successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing bundle {BundleId} from favorites for user {UserId}", bundleId, userId);
            return ApiResponse<bool>.ErrorResult("Failed to remove bundle from favorites");
        }
    }
}