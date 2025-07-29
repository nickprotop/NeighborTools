using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mapster;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Features.Users;
using ToolsSharing.Core.Interfaces;
// RentalStatus is defined in ToolsSharing.Core.Entities namespace
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Features.Users;

public class PublicProfileService : IPublicProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<PublicProfileService> _logger;

    public PublicProfileService(
        ApplicationDbContext context,
        UserManager<User> userManager,
        ISettingsService settingsService,
        ILogger<PublicProfileService> logger)
    {
        _context = context;
        _userManager = userManager;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<ApiResponse<PublicUserProfileDto>> GetPublicUserProfileAsync(string userId)
    {
        try
        {
            // Get user
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<PublicUserProfileDto>.CreateFailure("User not found");
            }

            // Get user settings to check privacy preferences
            var userSettings = await _settingsService.GetUserSettingsAsync(userId);
            if (userSettings == null)
            {
                _logger.LogWarning("No settings found for user {UserId}, using default privacy settings", userId);
            }

            // Create public profile respecting privacy settings
            var publicProfile = new PublicUserProfileDto
            {
                Id = user.Id,
                JoinedDate = user.CreatedAt,
                IsActive = !user.IsDeleted
            };

            // Apply privacy settings
            var privacy = userSettings?.Privacy;
            if (privacy != null)
            {
                // Show real name only if allowed
                if (privacy.ShowRealName)
                {
                    publicProfile.FirstName = user.FirstName;
                    publicProfile.LastName = user.LastName;
                    publicProfile.DisplayName = $"{user.FirstName} {user.LastName}".Trim();
                }
                else
                {
                    // Use a generic display name or username
                    publicProfile.DisplayName = user.UserName ?? "NeighborTools User";
                }

                // Show profile picture only if allowed
                if (privacy.ShowProfilePicture)
                {
                    publicProfile.ProfilePictureUrl = user.ProfilePictureUrl;
                }

                // Show location only if allowed
                if (privacy.ShowLocation)
                {
                    publicProfile.Location = user.LocationDisplay;
                }

                // Show email only if allowed
                if (privacy.ShowEmail)
                {
                    publicProfile.Email = user.Email;
                }

                // Show phone only if allowed
                if (privacy.ShowPhoneNumber)
                {
                    publicProfile.PhoneNumber = user.PhoneNumber;
                }

                // Show statistics only if allowed
                if (privacy.ShowStatistics)
                {
                    publicProfile.Statistics = await GetUserStatisticsAsync(userId);
                }
            }
            else
            {
                // Default privacy-safe behavior when no settings found
                publicProfile.DisplayName = user.UserName ?? "NeighborTools User";
                publicProfile.Statistics = await GetUserStatisticsAsync(userId);
            }

            return ApiResponse<PublicUserProfileDto>.CreateSuccess(publicProfile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public profile for user {UserId}", userId);
            return ApiResponse<PublicUserProfileDto>.CreateFailure("Failed to retrieve user profile");
        }
    }

    public async Task<ApiResponse<List<PublicUserToolDto>>> GetUserToolsAsync(string userId, int page = 1, int pageSize = 20)
    {
        try
        {
            var tools = await _context.Tools
                .Where(t => t.OwnerId == userId && !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new PublicUserToolDto
                {
                    Id = t.Id.ToString(),
                    Name = t.Name,
                    Description = t.Description.Length > 150 ? t.Description.Substring(0, 150) + "..." : t.Description,
                    ImageUrl = t.Images != null && t.Images.Any() ? t.Images.First().ImageUrl : null,
                    DailyRate = t.DailyRate,
                    Category = t.Category,
                    Rating = 0, // Will be calculated separately if needed
                    ReviewCount = 0, // Will be calculated separately if needed
                    IsAvailable = t.IsAvailable
                })
                .ToListAsync();

            return ApiResponse<List<PublicUserToolDto>>.CreateSuccess(tools);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tools for user {UserId}", userId);
            return ApiResponse<List<PublicUserToolDto>>.CreateFailure("Failed to retrieve user tools");
        }
    }

    public async Task<ApiResponse<List<PublicUserReviewDto>>> GetUserReviewsAsync(string userId, int page = 1, int pageSize = 20)
    {
        try
        {
            var reviews = await _context.Reviews
                .Include(r => r.Tool)
                .Include(r => r.Reviewer)
                .Where(r => r.Tool.OwnerId == userId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new PublicUserReviewDto
                {
                    Id = r.Id.ToString(),
                    ReviewerName = r.Reviewer.FirstName + " " + r.Reviewer.LastName,
                    ReviewerAvatarUrl = r.Reviewer.ProfilePictureUrl,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    ToolName = r.Tool.Name
                })
                .ToListAsync();

            return ApiResponse<List<PublicUserReviewDto>>.CreateSuccess(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reviews for user {UserId}", userId);
            return ApiResponse<List<PublicUserReviewDto>>.CreateFailure("Failed to retrieve user reviews");
        }
    }

    private async Task<PublicUserStatisticsDto?> GetUserStatisticsAsync(string userId)
    {
        try
        {
            var statistics = new PublicUserStatisticsDto();

            // Get tools count
            var toolsCount = await _context.Tools
                .Where(t => t.OwnerId == userId && !t.IsDeleted)
                .CountAsync();
            statistics.ToolsShared = toolsCount;

            // Get successful rentals count
            var successfulRentals = await _context.Rentals
                .Where(r => r.Tool.OwnerId == userId && r.Status == RentalStatus.Returned && !r.IsDeleted)
                .CountAsync();
            statistics.SuccessfulRentals = successfulRentals;

            // Get average rating and review count
            var reviewStats = await _context.Reviews
                .Include(r => r.Tool)
                .Where(r => r.Tool.OwnerId == userId && !r.IsDeleted)
                .GroupBy(r => r.Tool.OwnerId)
                .Select(g => new { 
                    AverageRating = g.Average(r => r.Rating),
                    ReviewCount = g.Count()
                })
                .FirstOrDefaultAsync();

            if (reviewStats != null)
            {
                statistics.AverageRating = (decimal)reviewStats.AverageRating;
                statistics.ReviewCount = reviewStats.ReviewCount;
            }

            // Response time calculation can be implemented later when we have better tracking
            // For now, we'll set a reasonable default
            statistics.ResponseTime = 24; // Default 24 hours response time

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating statistics for user {UserId}", userId);
            return null;
        }
    }
}