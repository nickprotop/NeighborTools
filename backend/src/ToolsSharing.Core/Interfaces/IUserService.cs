using ToolsSharing.Core.Features.Users;
using ToolsSharing.Core.Common.Models;

namespace ToolsSharing.Core.Interfaces;

public interface IUserService
{
    Task<UserProfileDto?> GetUserProfileAsync(string userId);
    Task<UserProfileDto?> UpdateUserProfileAsync(UpdateUserProfileCommand command);
    Task<UserStatisticsDto> GetUserStatisticsAsync(string userId);
    Task<PagedResult<UserReviewDto>> GetUserReviewsAsync(GetUserReviewsQuery query);
    Task<bool> DeleteUserAsync(string userId);
    Task<string?> UploadProfilePictureAsync(string userId, Stream imageStream, string fileName);
    Task<bool> RemoveProfilePictureAsync(string userId);
}