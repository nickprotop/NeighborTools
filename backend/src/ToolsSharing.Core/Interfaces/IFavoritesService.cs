using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.DTOs;

namespace ToolsSharing.Core.Interfaces;

public interface IFavoritesService
{
    Task<ApiResponse<List<FavoriteDto>>> GetUserFavoritesAsync(string userId);
    Task<ApiResponse<FavoriteStatusDto>> CheckFavoriteStatusAsync(string userId, Guid toolId);
    Task<ApiResponse<FavoriteDto>> AddToFavoritesAsync(string userId, Guid toolId);
    Task<ApiResponse<bool>> RemoveFromFavoritesAsync(string userId, Guid toolId);
    Task<ApiResponse<bool>> RemoveFromFavoritesByIdAsync(string userId, Guid favoriteId);
    Task<ApiResponse<int>> GetUserFavoritesCountAsync(string userId);
}