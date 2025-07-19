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
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            var favoriteDtos = favorites.Select(f => new FavoriteDto
            {
                Id = f.Id,
                UserId = f.UserId,
                ToolId = f.ToolId,
                CreatedAt = f.CreatedAt,
                ToolName = f.Tool.Name,
                ToolDescription = f.Tool.Description,
                ToolCategory = f.Tool.Category,
                DailyRate = f.Tool.DailyRate,
                ToolCondition = f.Tool.Condition,
                ToolLocation = f.Tool.Location,
                ToolImageUrls = f.Tool.Images.Select(img => img.ImageUrl).ToList(),
                IsToolAvailable = f.Tool.IsAvailable,
                OwnerName = $"{f.Tool.Owner.FirstName} {f.Tool.Owner.LastName}".Trim(),
                OwnerEmail = f.Tool.Owner.Email ?? string.Empty
            }).ToList();

            return ApiResponse<List<FavoriteDto>>.SuccessResult(favoriteDtos, "Favorites retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving favorites for user {UserId}", userId);
            return ApiResponse<List<FavoriteDto>>.ErrorResult("Failed to retrieve favorites");
        }
    }

    public async Task<ApiResponse<FavoriteStatusDto>> CheckFavoriteStatusAsync(string userId, Guid toolId)
    {
        try
        {
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ToolId == toolId);

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
                    ToolId = existingFavorite.ToolId,
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
                ToolId = toolId
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            var favoriteDto = new FavoriteDto
            {
                Id = favorite.Id,
                UserId = favorite.UserId,
                ToolId = favorite.ToolId,
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
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ToolId == toolId);

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
                .CountAsync(f => f.UserId == userId);

            return ApiResponse<int>.SuccessResult(count, "Favorites count retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting favorites count for user {UserId}", userId);
            return ApiResponse<int>.ErrorResult("Failed to get favorites count");
        }
    }
}