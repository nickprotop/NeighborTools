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

    public UserService(
        ApplicationDbContext context,
        UserManager<User> userManager,
        IRepository<User> userRepository,
        IRepository<Tool> toolRepository,
        IRepository<Rental> rentalRepository,
        IRepository<Review> reviewRepository)
    {
        _context = context;
        _userManager = userManager;
        _userRepository = userRepository;
        _toolRepository = toolRepository;
        _rentalRepository = rentalRepository;
        _reviewRepository = reviewRepository;
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
        user.Address = command.Address;
        user.City = command.City;
        user.PostalCode = command.PostalCode;
        user.Country = command.Country;
        user.PublicLocation = command.PublicLocation;
        user.ProfilePictureUrl = command.ProfilePictureUrl;
        user.UpdatedAt = DateTime.UtcNow;

        if (command.DateOfBirth.HasValue)
            user.DateOfBirth = command.DateOfBirth.Value;

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

        // For now, we'll just simulate the upload and return a placeholder URL
        // In a real implementation, you would upload to a cloud storage service
        var imageUrl = $"/images/profiles/{userId}_{fileName}";
        
        user.ProfilePictureUrl = imageUrl;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return imageUrl;
    }

    public async Task<bool> RemoveProfilePictureAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return false;

        user.ProfilePictureUrl = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return true;
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
}