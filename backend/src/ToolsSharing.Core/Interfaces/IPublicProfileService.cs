using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Features.Users;

namespace ToolsSharing.Core.Interfaces;

public interface IPublicProfileService
{
    Task<ApiResponse<PublicUserProfileDto>> GetPublicUserProfileAsync(string userId);
    Task<ApiResponse<List<PublicUserToolDto>>> GetUserToolsAsync(string userId, int page = 1, int pageSize = 20);
    Task<ApiResponse<List<PublicUserReviewDto>>> GetUserReviewsAsync(string userId, int page = 1, int pageSize = 20);
}