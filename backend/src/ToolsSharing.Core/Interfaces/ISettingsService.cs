using ToolsSharing.Core.Features.Settings;

namespace ToolsSharing.Core.Interfaces;

public interface ISettingsService
{
    Task<UserSettingsDto?> GetUserSettingsAsync(string userId);
    Task<UserSettingsDto> UpdateUserSettingsAsync(UpdateUserSettingsCommand command);
    Task<UserSettingsDto> CreateDefaultSettingsAsync(string userId);
    Task<bool> ChangePasswordAsync(ChangePasswordCommand command);
    Task<bool> DeleteUserSettingsAsync(string userId);
    Task<bool> ResetSettingsToDefaultAsync(string userId);
}