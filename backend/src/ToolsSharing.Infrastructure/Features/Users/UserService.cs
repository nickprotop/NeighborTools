using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Features.Users;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Infrastructure.Data;
using Mapster;

namespace ToolsSharing.Infrastructure.Features.Users;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Tool> _toolRepository;
    private readonly IRepository<Rental> _rentalRepository;
    private readonly IRepository<Review> _reviewRepository;
    private readonly IFileStorageService _fileStorageService;

    public UserService(
        ApplicationDbContext context,
        UserManager<User> userManager,
        IRepository<User> userRepository,
        IRepository<Tool> toolRepository,
        IRepository<Rental> rentalRepository,
        IRepository<Review> reviewRepository,
        IFileStorageService fileStorageService)
    {
        _context = context;
        _userManager = userManager;
        _userRepository = userRepository;
        _toolRepository = toolRepository;
        _rentalRepository = rentalRepository;
        _reviewRepository = reviewRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return null;

        var profile = user.Adapt<UserProfileDto>();
        
        // Calculate additional fields
        var reviews = await _context.Reviews
            .Where(r => r.RevieweeId == userId)
            .ToListAsync();
        profile.ReviewCount = reviews.Count();
        profile.AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
        profile.IsVerified = !string.IsNullOrEmpty(user.PhoneNumber) && user.EmailConfirmed;

        return profile;
    }

    public async Task<UserProfileDto?> UpdateUserProfileAsync(UpdateUserProfileCommand command)
    {
        var user = await _userManager.FindByIdAsync(command.UserId);
        if (user == null || user.IsDeleted)
            return null;

        // Update user properties
        user.FirstName = command.FirstName;
        user.LastName = command.LastName;
        user.PhoneNumber = command.PhoneNumber;
        user.LocationDisplay = command.LocationDisplay;
        user.ProfilePictureUrl = command.ProfilePictureUrl;
        user.UpdatedAt = DateTime.UtcNow;

        if (command.DateOfBirth.HasValue)
            user.DateOfBirth = command.DateOfBirth.Value;

        // Phase 7: Update enhanced location fields for inheritance system
        if (command.LocationArea != null)
            user.LocationArea = command.LocationArea;
        if (command.LocationCity != null)
            user.LocationCity = command.LocationCity;
        if (command.LocationState != null)
            user.LocationState = command.LocationState;
        if (command.LocationCountry != null)
            user.LocationCountry = command.LocationCountry;
        if (command.LocationLat.HasValue)
            user.LocationLat = command.LocationLat.Value;
        if (command.LocationLng.HasValue)
            user.LocationLng = command.LocationLng.Value;
        if (command.LocationPrecisionRadius.HasValue)
            user.LocationPrecisionRadius = command.LocationPrecisionRadius.Value;
        if (command.LocationSource.HasValue)
            user.LocationSource = command.LocationSource.Value;
        if (command.LocationPrivacyLevel.HasValue)
            user.LocationPrivacyLevel = command.LocationPrivacyLevel.Value;
        
        // Update location timestamp if any location field was changed
        if (command.LocationArea != null || command.LocationCity != null || 
            command.LocationState != null || command.LocationCountry != null ||
            command.LocationLat.HasValue || command.LocationLng.HasValue ||
            command.LocationPrecisionRadius.HasValue || command.LocationSource.HasValue ||
            command.LocationPrivacyLevel.HasValue || command.LocationDisplay != null)
        {
            user.LocationUpdatedAt = DateTime.UtcNow;
        }

        await _userManager.UpdateAsync(user);

        return await GetUserProfileAsync(command.UserId);
    }

    public async Task<UserStatisticsDto> GetUserStatisticsAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return new UserStatisticsDto();

        var tools = await _context.Tools
            .Where(t => t.OwnerId == userId && !t.IsDeleted)
            .ToListAsync();
        var rentalsAsOwner = await _context.Rentals
            .Where(r => r.OwnerId == userId)
            .ToListAsync();
        var rentalsAsRenter = await _context.Rentals
            .Where(r => r.RenterId == userId)
            .ToListAsync();
        var reviews = await _context.Reviews
            .Where(r => r.RevieweeId == userId)
            .ToListAsync();

        var successfulRentals = rentalsAsOwner.Count(r => r.Status == RentalStatus.Returned);
        var totalEarned = rentalsAsOwner.Where(r => r.Status == RentalStatus.Returned).Sum(r => r.TotalCost);
        var activeRentals = rentalsAsOwner.Count(r => r.Status == RentalStatus.PickedUp);

        var statistics = new UserStatisticsDto
        {
            ToolsShared = tools.Count(),
            SuccessfulRentals = successfulRentals,
            TotalRentals = rentalsAsOwner.Count(),
            TotalEarned = totalEarned,
            AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
            ReviewCount = reviews.Count(),
            MemberSince = user.CreatedAt,
            ActiveRentals = activeRentals,
            RecentActivity = await GetRecentActivityAsync(userId)
        };

        return statistics;
    }

    public async Task<PagedResult<UserReviewDto>> GetUserReviewsAsync(GetUserReviewsQuery query)
    {
        var reviewsQuery = _context.Reviews
            .Where(r => r.RevieweeId == query.UserId)
            .Include(r => r.Reviewer)
            .Include(r => r.Tool)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await reviewsQuery.CountAsync();
        var reviews = await reviewsQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var reviewDtos = reviews.Select(r => new UserReviewDto
        {
            Id = r.Id.ToString(),
            ReviewerId = r.ReviewerId,
            ReviewerName = $"{r.Reviewer.FirstName} {r.Reviewer.LastName}",
            ReviewerAvatarUrl = r.Reviewer.ProfilePictureUrl,
            Rating = r.Rating,
            Title = r.Title,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt,
            ToolName = r.Tool?.Name,
            ToolId = r.ToolId?.ToString(),
            ReviewType = r.Type.ToString()
        }).ToList();

        return new PagedResult<UserReviewDto>
        {
            Items = reviewDtos,
            TotalCount = totalCount,
            PageSize = query.PageSize
        };
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return false;

        // Soft delete
        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return true;
    }

    public async Task<string?> UploadProfilePictureAsync(string userId, Stream imageStream, string fileName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return null;

        try
        {
            // Delete old profile picture if exists
            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                await _fileStorageService.DeleteFileAsync(user.ProfilePictureUrl);
            }

            // Create unique filename to prevent conflicts
            var extension = Path.GetExtension(fileName);
            var uniqueFileName = $"{userId}_{Guid.NewGuid()}{extension}";

            // Create metadata for profile pictures (public access for display)
            var metadata = new FileAccessMetadata
            {
                AccessLevel = "public",
                FileType = "avatar",
                OwnerId = userId
            };

            // Upload to avatars folder with metadata
            var storagePath = await _fileStorageService.UploadFileAsync(
                imageStream, 
                uniqueFileName, 
                GetContentTypeFromExtension(extension), 
                "avatars", 
                metadata);

            // Update user's profile picture URL
            user.ProfilePictureUrl = storagePath;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Return the file URL for frontend consumption
            return await _fileStorageService.GetFileUrlAsync(storagePath);
        }
        catch (Exception)
        {
            // If upload fails, don't update the user record
            return null;
        }
    }

    public async Task<bool> RemoveProfilePictureAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return false;

        try
        {
            // Delete the actual file from storage
            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                await _fileStorageService.DeleteFileAsync(user.ProfilePictureUrl);
            }

            // Clear the database reference
            user.ProfilePictureUrl = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return true;
        }
        catch (Exception)
        {
            // If file deletion fails, still clear the database reference
            user.ProfilePictureUrl = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            
            return true; // Return true since we cleared the database reference
        }
    }

    private async Task<List<RecentActivityDto>> GetRecentActivityAsync(string userId)
    {
        var activities = new List<RecentActivityDto>();

        // Get recent rentals as owner
        var recentRentalsAsOwner = await _context.Rentals
            .Where(r => r.OwnerId == userId)
            .Include(r => r.Tool)
            .Include(r => r.Renter)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .ToListAsync();

        foreach (var rental in recentRentalsAsOwner)
        {
            activities.Add(new RecentActivityDto
            {
                ActivityType = "rental_created",
                Description = $"Tool '{rental.Tool.Name}' rented by {rental.Renter.FirstName} {rental.Renter.LastName}",
                Date = rental.CreatedAt,
                ToolName = rental.Tool.Name,
                RenterName = $"{rental.Renter.FirstName} {rental.Renter.LastName}"
            });
        }

        // Get recent tools added
        var recentTools = await _context.Tools
            .Where(t => t.OwnerId == userId && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .Take(3)
            .ToListAsync();

        foreach (var tool in recentTools)
        {
            activities.Add(new RecentActivityDto
            {
                ActivityType = "tool_added",
                Description = $"Added new tool '{tool.Name}'",
                Date = tool.CreatedAt,
                ToolName = tool.Name
            });
        }

        return activities.OrderByDescending(a => a.Date).Take(10).ToList();
    }

    public async Task<List<UserSearchResultDto>> SearchUsersAsync(string query, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new List<UserSearchResultDto>();

        // Normalize query for search
        var normalizedQuery = query.Trim().ToLower();

        // Search users by first name, last name, or email
        var users = await _context.Users
            .Where(u => !u.IsDeleted && u.EmailConfirmed &&
                       (u.FirstName.ToLower().Contains(normalizedQuery) ||
                        u.LastName.ToLower().Contains(normalizedQuery) ||
                        u.Email.ToLower().Contains(normalizedQuery) ||
                        (u.FirstName + " " + u.LastName).ToLower().Contains(normalizedQuery)))
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Take(limit)
            .ToListAsync();

        var results = new List<UserSearchResultDto>();
        
        foreach (var user in users)
        {
            // Calculate review statistics
            var reviews = await _context.Reviews
                .Where(r => r.RevieweeId == user.Id)
                .ToListAsync();

            var userSearchResult = new UserSearchResultDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePictureUrl = user.ProfilePictureUrl,
                LocationDisplay = user.LocationDisplay,
                IsVerified = !string.IsNullOrEmpty(user.PhoneNumber) && user.EmailConfirmed,
                AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
                ReviewCount = reviews.Count
            };
            
            results.Add(userSearchResult);
        }

        return results;
    }

    private static string GetContentTypeFromExtension(string extension)
    {
        return extension?.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}