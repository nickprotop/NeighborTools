using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Features.Settings;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Data;
using Mapster;

namespace ToolsSharing.Infrastructure.Features.Settings;

public class SettingsService : ISettingsService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public SettingsService(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<UserSettingsDto?> GetUserSettingsAsync(string userId)
    {
        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            // Create default settings if they don't exist
            settings = await CreateDefaultSettingsEntityAsync(userId);
        }

        return MapToDto(settings);
    }

    public async Task<UserSettingsDto> UpdateUserSettingsAsync(UpdateUserSettingsCommand command)
    {
        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == command.UserId);

        if (settings == null)
        {
            settings = await CreateDefaultSettingsEntityAsync(command.UserId);
        }

        // Update only the provided sections
        if (command.Privacy != null)
        {
            settings.ShowProfilePicture = command.Privacy.ShowProfilePicture;
            settings.ShowRealName = command.Privacy.ShowRealName;
            settings.ShowLocation = command.Privacy.ShowLocation;
            settings.ShowPhoneNumber = command.Privacy.ShowPhoneNumber;
            settings.ShowEmail = command.Privacy.ShowEmail;
            settings.ShowStatistics = command.Privacy.ShowStatistics;
        }

        if (command.Notifications != null)
        {
            settings.EmailRentalRequests = command.Notifications.EmailRentalRequests;
            settings.EmailRentalUpdates = command.Notifications.EmailRentalUpdates;
            settings.EmailMessages = command.Notifications.EmailMessages;
            settings.EmailMarketing = command.Notifications.EmailMarketing;
            settings.EmailSecurityAlerts = command.Notifications.EmailSecurityAlerts;
            settings.PushMessages = command.Notifications.PushMessages;
            settings.PushReminders = command.Notifications.PushReminders;
            settings.PushRentalRequests = command.Notifications.PushRentalRequests;
            settings.PushRentalUpdates = command.Notifications.PushRentalUpdates;
        }

        if (command.Display != null)
        {
            settings.Theme = command.Display.Theme;
            settings.Language = command.Display.Language;
            settings.Currency = command.Display.Currency;
            settings.TimeZone = command.Display.TimeZone;
        }

        if (command.Rental != null)
        {
            settings.AutoApproveRentals = command.Rental.AutoApproveRentals;
            settings.RentalLeadTime = command.Rental.RentalLeadTime;
            settings.RequireDeposit = command.Rental.RequireDeposit;
            settings.DefaultDepositPercentage = command.Rental.DefaultDepositPercentage;
        }

        if (command.Security != null)
        {
            settings.TwoFactorEnabled = command.Security.TwoFactorEnabled;
            settings.LoginAlertsEnabled = command.Security.LoginAlertsEnabled;
            settings.SessionTimeoutMinutes = command.Security.SessionTimeoutMinutes;
        }

        if (command.Communication != null)
        {
            settings.AllowDirectMessages = command.Communication.AllowDirectMessages;
            settings.AllowRentalInquiries = command.Communication.AllowRentalInquiries;
            settings.ShowOnlineStatus = command.Communication.ShowOnlineStatus;
        }

        settings.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDto(settings);
    }

    public async Task<UserSettingsDto> CreateDefaultSettingsAsync(string userId)
    {
        var settings = await CreateDefaultSettingsEntityAsync(userId);
        return MapToDto(settings);
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordCommand command)
    {
        var user = await _userManager.FindByIdAsync(command.UserId);
        if (user == null)
            return false;

        var result = await _userManager.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword);
        return result.Succeeded;
    }

    public async Task<bool> DeleteUserSettingsAsync(string userId)
    {
        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
            return false;

        _context.UserSettings.Remove(settings);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResetSettingsToDefaultAsync(string userId)
    {
        // Delete existing settings
        await DeleteUserSettingsAsync(userId);
        
        // Create new default settings
        await CreateDefaultSettingsEntityAsync(userId);
        
        return true;
    }

    private async Task<UserSettings> CreateDefaultSettingsEntityAsync(string userId)
    {
        var settings = new UserSettings
        {
            UserId = userId,
            // Privacy defaults
            ShowProfilePicture = true,
            ShowRealName = true,
            ShowLocation = true,
            ShowPhoneNumber = false,
            ShowEmail = false,
            ShowStatistics = true,
            
            // Notification defaults
            EmailRentalRequests = true,
            EmailRentalUpdates = true,
            EmailMessages = true,
            EmailMarketing = false,
            EmailSecurityAlerts = true,
            PushMessages = true,
            PushReminders = true,
            PushRentalRequests = true,
            PushRentalUpdates = true,
            
            // Display defaults
            Theme = "system",
            Language = "en",
            Currency = "USD",
            TimeZone = "UTC",
            
            // Rental defaults
            AutoApproveRentals = false,
            RentalLeadTime = 24,
            RequireDeposit = true,
            DefaultDepositPercentage = 0.20m,
            
            // Security defaults
            TwoFactorEnabled = false,
            LoginAlertsEnabled = true,
            SessionTimeoutMinutes = 480,
            
            // Communication defaults
            AllowDirectMessages = true,
            AllowRentalInquiries = true,
            ShowOnlineStatus = true
        };

        _context.UserSettings.Add(settings);
        await _context.SaveChangesAsync();
        
        return settings;
    }

    private static UserSettingsDto MapToDto(UserSettings settings)
    {
        return new UserSettingsDto
        {
            Id = settings.Id.ToString(),
            UserId = settings.UserId,
            Privacy = new PrivacySettingsDto
            {
                ShowProfilePicture = settings.ShowProfilePicture,
                ShowRealName = settings.ShowRealName,
                ShowLocation = settings.ShowLocation,
                ShowPhoneNumber = settings.ShowPhoneNumber,
                ShowEmail = settings.ShowEmail,
                ShowStatistics = settings.ShowStatistics
            },
            Notifications = new NotificationSettingsDto
            {
                EmailRentalRequests = settings.EmailRentalRequests,
                EmailRentalUpdates = settings.EmailRentalUpdates,
                EmailMessages = settings.EmailMessages,
                EmailMarketing = settings.EmailMarketing,
                EmailSecurityAlerts = settings.EmailSecurityAlerts,
                PushMessages = settings.PushMessages,
                PushReminders = settings.PushReminders,
                PushRentalRequests = settings.PushRentalRequests,
                PushRentalUpdates = settings.PushRentalUpdates
            },
            Display = new DisplaySettingsDto
            {
                Theme = settings.Theme,
                Language = settings.Language,
                Currency = settings.Currency,
                TimeZone = settings.TimeZone
            },
            Rental = new RentalSettingsDto
            {
                AutoApproveRentals = settings.AutoApproveRentals,
                RentalLeadTime = settings.RentalLeadTime,
                RequireDeposit = settings.RequireDeposit,
                DefaultDepositPercentage = settings.DefaultDepositPercentage
            },
            Security = new SecuritySettingsDto
            {
                TwoFactorEnabled = settings.TwoFactorEnabled,
                LoginAlertsEnabled = settings.LoginAlertsEnabled,
                SessionTimeoutMinutes = settings.SessionTimeoutMinutes
            },
            Communication = new CommunicationSettingsDto
            {
                AllowDirectMessages = settings.AllowDirectMessages,
                AllowRentalInquiries = settings.AllowRentalInquiries,
                ShowOnlineStatus = settings.ShowOnlineStatus
            },
            CreatedAt = settings.CreatedAt,
            UpdatedAt = settings.UpdatedAt
        };
    }
}