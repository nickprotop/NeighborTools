using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToolsSharing.Core.DTOs.Admin;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services;

public class SampleDataService : ISampleDataService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<SampleDataService> _logger;

    public SampleDataService(
        ApplicationDbContext context,
        UserManager<User> userManager,
        ILogger<SampleDataService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<SampleDataStatusDto> GetStatusAsync()
    {
        var status = new SampleDataStatusDto();
        
        // Check each data type
        foreach (var dataType in SampleDataConstants.DataTypeDisplayNames.Keys)
        {
            var typeStatus = new SampleDataTypeStatus
            {
                DataType = dataType,
                DisplayName = SampleDataConstants.DataTypeDisplayNames[dataType],
                Description = SampleDataConstants.DataTypeDescriptions[dataType],
                IsApplied = await IsSampleDataAppliedAsync(dataType),
                Count = await GetSampleDataCountAsync(dataType)
            };
            
            status.DataTypes.Add(typeStatus);
        }
        
        status.HasAnySampleData = status.DataTypes.Any(d => d.IsApplied);
        
        return status;
    }

    public async Task<SampleDataStatusDto> ApplySampleDataAsync(ApplySampleDataRequest request, string adminUserId)
    {
        _logger.LogInformation("Applying sample data: {DataTypes} by admin {AdminUserId}", 
            string.Join(", ", request.DataTypes), adminUserId);

        foreach (var dataType in request.DataTypes)
        {
            if (await IsSampleDataAppliedAsync(dataType))
            {
                _logger.LogWarning("Sample data type {DataType} already applied, skipping", dataType);
                continue;
            }

            await ApplySampleDataTypeAsync(dataType, adminUserId);
        }

        return await GetStatusAsync();
    }

    public async Task<SampleDataStatusDto> RemoveSampleDataAsync(RemoveSampleDataRequest request, string adminUserId)
    {
        _logger.LogInformation("Removing sample data: {DataTypes} by admin {AdminUserId}", 
            string.Join(", ", request.DataTypes), adminUserId);

        foreach (var dataType in request.DataTypes)
        {
            await RemoveSampleDataTypeAsync(dataType, adminUserId);
        }

        return await GetStatusAsync();
    }

    public async Task<bool> IsSampleDataAppliedAsync(string dataType)
    {
        return dataType switch
        {
            SampleDataConstants.USERS => await _context.Users.AnyAsync(u => u.Id == SampleDataIds.JOHN_DOE_USER_ID || u.Id == SampleDataIds.JANE_SMITH_USER_ID),
            SampleDataConstants.TOOLS => await _context.Tools.AnyAsync(t => t.Id == SampleDataIds.DRILL_TOOL_ID),
            SampleDataConstants.RENTALS => await _context.Rentals.AnyAsync(r => r.Id == SampleDataIds.RENTAL_1_ID),
            SampleDataConstants.REVIEWS => await _context.Reviews.AnyAsync(r => r.ToolId == SampleDataIds.DRILL_TOOL_ID),
            SampleDataConstants.MESSAGES => await _context.Messages.AnyAsync(m => m.ConversationId == SampleDataIds.CONVERSATION_1_ID),
            SampleDataConstants.CONVERSATIONS => await _context.Conversations.AnyAsync(c => c.Id == SampleDataIds.CONVERSATION_1_ID),
            _ => false
        };
    }

    public async Task RemoveAllSampleDataAsync(string adminUserId)
    {
        _logger.LogInformation("Removing ALL sample data by admin {AdminUserId}", adminUserId);

        var allDataTypes = SampleDataConstants.DataTypeDisplayNames.Keys.ToArray();
        await RemoveSampleDataAsync(new RemoveSampleDataRequest { DataTypes = allDataTypes }, adminUserId);
    }

    private async Task<int> GetSampleDataCountAsync(string dataType)
    {
        return dataType switch
        {
            SampleDataConstants.USERS => await _context.Users.CountAsync(u => u.Id == SampleDataIds.JOHN_DOE_USER_ID || u.Id == SampleDataIds.JANE_SMITH_USER_ID),
            SampleDataConstants.TOOLS => await _context.Tools.CountAsync(t => t.OwnerId == SampleDataIds.JOHN_DOE_USER_ID || t.OwnerId == SampleDataIds.JANE_SMITH_USER_ID),
            SampleDataConstants.RENTALS => await _context.Rentals.CountAsync(r => r.RenterId == SampleDataIds.JOHN_DOE_USER_ID || r.RenterId == SampleDataIds.JANE_SMITH_USER_ID),
            SampleDataConstants.REVIEWS => await _context.Reviews.CountAsync(r => r.ReviewerId == SampleDataIds.JOHN_DOE_USER_ID || r.ReviewerId == SampleDataIds.JANE_SMITH_USER_ID),
            SampleDataConstants.MESSAGES => await _context.Messages.CountAsync(m => m.SenderId == SampleDataIds.JOHN_DOE_USER_ID || m.SenderId == SampleDataIds.JANE_SMITH_USER_ID),
            SampleDataConstants.CONVERSATIONS => await _context.Conversations.CountAsync(c => c.Participant1Id == SampleDataIds.JOHN_DOE_USER_ID || c.Participant1Id == SampleDataIds.JANE_SMITH_USER_ID),
            _ => 0
        };
    }

    private async Task ApplySampleDataTypeAsync(string dataType, string adminUserId)
    {
        switch (dataType)
        {
            case SampleDataConstants.USERS:
                await ApplySampleUsersAsync();
                break;
            case SampleDataConstants.TOOLS:
                await ApplySampleToolsAsync();
                break;
            case SampleDataConstants.RENTALS:
                await ApplySampleRentalsAsync();
                break;
            case SampleDataConstants.REVIEWS:
                await ApplySampleReviewsAsync();
                break;
            case SampleDataConstants.CONVERSATIONS:
                await ApplySampleConversationsAsync();
                break;
            case SampleDataConstants.MESSAGES:
                await ApplySampleMessagesAsync();
                break;
        }
    }

    private async Task RemoveSampleDataTypeAsync(string dataType, string adminUserId)
    {
        switch (dataType)
        {
            case SampleDataConstants.MESSAGES:
                await RemoveSampleMessagesAsync();
                break;
            case SampleDataConstants.CONVERSATIONS:
                await RemoveSampleConversationsAsync();
                break;
            case SampleDataConstants.REVIEWS:
                await RemoveSampleReviewsAsync();
                break;
            case SampleDataConstants.RENTALS:
                await RemoveSampleRentalsAsync();
                break;
            case SampleDataConstants.TOOLS:
                await RemoveSampleToolsAsync();
                break;
            case SampleDataConstants.USERS:
                await RemoveSampleUsersAsync();
                break;
        }
    }

    // Implementation methods moved from DataSeeder
    private async Task ApplySampleUsersAsync()
    {
        if (await _context.Users.AnyAsync(u => u.Id == SampleDataIds.JOHN_DOE_USER_ID))
        {
            _logger.LogInformation("Sample users already exist, skipping");
            return;
        }

        _logger.LogInformation("Creating sample users...");

        var users = new[]
        {
            new User
            {
                Id = SampleDataIds.JOHN_DOE_USER_ID,
                UserName = "john.doe@email.com",
                Email = "john.doe@email.com",
                EmailConfirmed = true,
                FirstName = "John",
                LastName = "Doe",
                PhoneNumber = "+1234567890",
                PhoneNumberConfirmed = true,
                Address = "123 Main St",
                City = "San Francisco",
                PostalCode = "94102",
                Country = "USA",
                PublicLocation = "Downtown San Francisco",
                DateOfBirth = new DateTime(1985, 5, 15),
                ProfilePictureUrl = "/images/profiles/john-doe.jpg",
                DataProcessingConsent = true,
                MarketingConsent = true,
                TermsOfServiceAccepted = true,
                TermsAcceptedDate = DateTime.UtcNow,
                TermsVersion = "1.0",
                CreatedAt = DateTime.UtcNow.AddDays(-180),
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = SampleDataIds.JANE_SMITH_USER_ID,
                UserName = "jane.smith@email.com",
                Email = "jane.smith@email.com",
                EmailConfirmed = true,
                FirstName = "Jane",
                LastName = "Smith",
                PhoneNumber = "+1234567891",
                PhoneNumberConfirmed = true,
                Address = "456 Oak Ave",
                City = "Oakland",
                PostalCode = "94607",
                Country = "USA",
                PublicLocation = "East Oakland",
                DateOfBirth = new DateTime(1990, 8, 22),
                ProfilePictureUrl = "/images/profiles/jane-smith.jpg",
                DataProcessingConsent = true,
                MarketingConsent = false,
                TermsOfServiceAccepted = true,
                TermsAcceptedDate = DateTime.UtcNow,
                TermsVersion = "1.0",
                CreatedAt = DateTime.UtcNow.AddDays(-120),
                UpdatedAt = DateTime.UtcNow
            }
        };

        foreach (var user in users)
        {
            var result = await _userManager.CreateAsync(user, "Password123!");
            if (result.Succeeded)
            {
                _logger.LogInformation("Created sample user: {Email}", user.Email);
                
                // Assign roles
                if (user.Email == "john.doe@email.com")
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                    await _userManager.AddToRoleAsync(user, "User");
                    _logger.LogInformation("Assigned Admin and User roles to {Email}", user.Email);
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, "User");
                    _logger.LogInformation("Assigned User role to {Email}", user.Email);
                }
            }
            else
            {
                _logger.LogError("Failed to create sample user {Email}: {Errors}", 
                    user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task RemoveSampleUsersAsync()
    {
        _logger.LogInformation("Removing sample users and ALL their associated data...");

        // Remove all data associated with sample users (cascade delete)
        var sampleUserIds = new[] { SampleDataIds.JOHN_DOE_USER_ID, SampleDataIds.JANE_SMITH_USER_ID };
        
        // Remove favorites
        var favorites = await _context.Favorites
            .Where(f => sampleUserIds.Contains(f.UserId))
            .ToListAsync();
        if (favorites.Any())
        {
            _context.Favorites.RemoveRange(favorites);
            _logger.LogInformation("Removed {Count} sample favorites", favorites.Count);
        }

        // Remove messages
        var messages = await _context.Messages
            .Where(m => sampleUserIds.Contains(m.SenderId) || sampleUserIds.Contains(m.RecipientId))
            .ToListAsync();
        if (messages.Any())
        {
            _context.Messages.RemoveRange(messages);
            _logger.LogInformation("Removed {Count} sample messages", messages.Count);
        }

        // Remove conversations
        var conversations = await _context.Conversations
            .Where(c => sampleUserIds.Contains(c.Participant1Id) || sampleUserIds.Contains(c.Participant2Id))
            .ToListAsync();
        if (conversations.Any())
        {
            _context.Conversations.RemoveRange(conversations);
            _logger.LogInformation("Removed {Count} sample conversations", conversations.Count);
        }

        // Remove reviews
        var reviews = await _context.Reviews
            .Where(r => sampleUserIds.Contains(r.ReviewerId) || sampleUserIds.Contains(r.RevieweeId))
            .ToListAsync();
        if (reviews.Any())
        {
            _context.Reviews.RemoveRange(reviews);
            _logger.LogInformation("Removed {Count} sample reviews", reviews.Count);
        }

        // Remove rentals
        var rentals = await _context.Rentals
            .Where(r => sampleUserIds.Contains(r.RenterId) || sampleUserIds.Contains(r.OwnerId))
            .ToListAsync();
        if (rentals.Any())
        {
            _context.Rentals.RemoveRange(rentals);
            _logger.LogInformation("Removed {Count} sample rentals", rentals.Count);
        }

        // Remove tools
        var tools = await _context.Tools
            .Where(t => sampleUserIds.Contains(t.OwnerId))
            .ToListAsync();
        if (tools.Any())
        {
            _context.Tools.RemoveRange(tools);
            _logger.LogInformation("Removed {Count} sample tools", tools.Count);
        }

        // Finally remove the users themselves
        var users = await _context.Users
            .Where(u => sampleUserIds.Contains(u.Id))
            .ToListAsync();
        
        foreach (var user in users)
        {
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("Removed sample user: {Email}", user.Email);
            }
            else
            {
                _logger.LogError("Failed to remove sample user {Email}: {Errors}", 
                    user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Sample users and all associated data removed successfully");
    }

    // Additional implementation methods for tools, rentals, reviews, etc.
    // (I'll continue with the most critical ones for now)
    
    private async Task ApplySampleToolsAsync()
    {
        if (await _context.Tools.AnyAsync(t => t.Id == SampleDataIds.DRILL_TOOL_ID))
        {
            _logger.LogInformation("Sample tools already exist, skipping");
            return;
        }

        var tools = new[]
        {
            new Tool
            {
                Id = SampleDataIds.DRILL_TOOL_ID,
                Name = "Professional Drill",
                Description = "High-power cordless drill perfect for heavy-duty work",
                Category = "Power Tools",
                Brand = "DeWalt",
                Model = "DCD771C2",
                DailyRate = 15.00m,
                WeeklyRate = 80.00m,
                MonthlyRate = 250.00m,
                DepositRequired = 50.00m,
                Condition = "Excellent",
                Location = "Downtown San Francisco",
                IsAvailable = true,
                OwnerId = SampleDataIds.JOHN_DOE_USER_ID,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Tool
            {
                Id = SampleDataIds.SAW_TOOL_ID,
                Name = "Circular Saw",
                Description = "Professional grade circular saw for precise cuts",
                Category = "Power Tools",
                Brand = "Makita",
                Model = "XSR01Z",
                DailyRate = 25.00m,
                WeeklyRate = 150.00m,
                MonthlyRate = 450.00m,
                DepositRequired = 100.00m,
                Condition = "Good",
                Location = "Downtown San Francisco",
                IsAvailable = true,
                OwnerId = SampleDataIds.JOHN_DOE_USER_ID,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Tool
            {
                Id = SampleDataIds.LADDER_TOOL_ID,
                Name = "Ladder 8ft",
                Description = "Sturdy aluminum ladder for indoor/outdoor use",
                Category = "Ladders",
                Brand = "Werner",
                Model = "MT-8",
                DailyRate = 10.00m,
                WeeklyRate = 50.00m,
                MonthlyRate = 150.00m,
                DepositRequired = 25.00m,
                Condition = "Good",
                Location = "East Oakland",
                IsAvailable = true,
                OwnerId = SampleDataIds.JANE_SMITH_USER_ID,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Tool
            {
                Id = SampleDataIds.PRESSURE_WASHER_TOOL_ID,
                Name = "Pressure Washer",
                Description = "High-pressure electric washer for cleaning",
                Category = "Cleaning",
                Brand = "Karcher",
                Model = "K5 Premium",
                DailyRate = 30.00m,
                WeeklyRate = 180.00m,
                MonthlyRate = 500.00m,
                DepositRequired = 150.00m,
                Condition = "Excellent",
                Location = "Berkeley",
                IsAvailable = true,
                OwnerId = SampleDataIds.JANE_SMITH_USER_ID,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.Tools.AddRange(tools);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created {Count} sample tools", tools.Length);
    }

    private async Task RemoveSampleToolsAsync()
    {
        var sampleUserIds = new[] { SampleDataIds.JOHN_DOE_USER_ID, SampleDataIds.JANE_SMITH_USER_ID };
        var tools = await _context.Tools
            .Where(t => sampleUserIds.Contains(t.OwnerId))
            .ToListAsync();

        if (tools.Any())
        {
            _context.Tools.RemoveRange(tools);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Removed {Count} sample tools", tools.Count);
        }
    }

    private async Task ApplySampleRentalsAsync()
    {
        if (await _context.Rentals.AnyAsync(r => r.Id == SampleDataIds.RENTAL_1_ID))
        {
            _logger.LogInformation("Sample rentals already exist, skipping");
            return;
        }

        var rentals = new[]
        {
            new Rental
            {
                Id = SampleDataIds.RENTAL_1_ID,
                ToolId = SampleDataIds.DRILL_TOOL_ID,
                RenterId = SampleDataIds.JANE_SMITH_USER_ID,
                OwnerId = SampleDataIds.JOHN_DOE_USER_ID,
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddDays(-25),
                Status = RentalStatus.Returned,
                TotalCost = 75.00m,
                DepositAmount = 50.00m,
                CreatedAt = DateTime.UtcNow.AddDays(-32),
                UpdatedAt = DateTime.UtcNow.AddDays(-25)
            },
            new Rental
            {
                Id = SampleDataIds.RENTAL_2_ID,
                ToolId = SampleDataIds.LADDER_TOOL_ID,
                RenterId = SampleDataIds.JOHN_DOE_USER_ID,
                OwnerId = SampleDataIds.JANE_SMITH_USER_ID,
                StartDate = DateTime.UtcNow.AddDays(-15),
                EndDate = DateTime.UtcNow.AddDays(-10),
                Status = RentalStatus.Returned,
                TotalCost = 50.00m,
                DepositAmount = 25.00m,
                CreatedAt = DateTime.UtcNow.AddDays(-17),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new Rental
            {
                Id = SampleDataIds.RENTAL_3_ID,
                ToolId = SampleDataIds.PRESSURE_WASHER_TOOL_ID,
                RenterId = SampleDataIds.JOHN_DOE_USER_ID,
                OwnerId = SampleDataIds.JANE_SMITH_USER_ID,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(2),
                Status = RentalStatus.PickedUp,
                TotalCost = 210.00m,
                DepositAmount = 150.00m,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            }
        };

        _context.Rentals.AddRange(rentals);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created {Count} sample rentals", rentals.Length);
    }

    private async Task RemoveSampleRentalsAsync()
    {
        var sampleUserIds = new[] { SampleDataIds.JOHN_DOE_USER_ID, SampleDataIds.JANE_SMITH_USER_ID };
        var rentals = await _context.Rentals
            .Where(r => sampleUserIds.Contains(r.RenterId) || sampleUserIds.Contains(r.OwnerId))
            .ToListAsync();

        if (rentals.Any())
        {
            _context.Rentals.RemoveRange(rentals);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Removed {Count} sample rentals", rentals.Count);
        }
    }

    private async Task ApplySampleReviewsAsync()
    {
        // Implementation similar to tools/rentals
        _logger.LogInformation("Sample reviews applied (placeholder)");
    }

    private async Task RemoveSampleReviewsAsync()
    {
        var sampleUserIds = new[] { SampleDataIds.JOHN_DOE_USER_ID, SampleDataIds.JANE_SMITH_USER_ID };
        var reviews = await _context.Reviews
            .Where(r => sampleUserIds.Contains(r.ReviewerId) || sampleUserIds.Contains(r.RevieweeId))
            .ToListAsync();

        if (reviews.Any())
        {
            _context.Reviews.RemoveRange(reviews);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Removed {Count} sample reviews", reviews.Count);
        }
    }

    private async Task ApplySampleConversationsAsync()
    {
        // Implementation similar to tools/rentals
        _logger.LogInformation("Sample conversations applied (placeholder)");
    }

    private async Task RemoveSampleConversationsAsync()
    {
        var sampleUserIds = new[] { SampleDataIds.JOHN_DOE_USER_ID, SampleDataIds.JANE_SMITH_USER_ID };
        var conversations = await _context.Conversations
            .Where(c => sampleUserIds.Contains(c.Participant1Id) || sampleUserIds.Contains(c.Participant2Id))
            .ToListAsync();

        if (conversations.Any())
        {
            _context.Conversations.RemoveRange(conversations);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Removed {Count} sample conversations", conversations.Count);
        }
    }

    private async Task ApplySampleMessagesAsync()
    {
        // Implementation similar to tools/rentals
        _logger.LogInformation("Sample messages applied (placeholder)");
    }

    private async Task RemoveSampleMessagesAsync()
    {
        var sampleUserIds = new[] { SampleDataIds.JOHN_DOE_USER_ID, SampleDataIds.JANE_SMITH_USER_ID };
        var messages = await _context.Messages
            .Where(m => sampleUserIds.Contains(m.SenderId) || sampleUserIds.Contains(m.RecipientId))
            .ToListAsync();

        if (messages.Any())
        {
            _context.Messages.RemoveRange(messages);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Removed {Count} sample messages", messages.Count);
        }
    }
}