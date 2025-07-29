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
            SampleDataConstants.BUNDLES => await _context.Bundles.AnyAsync(b => b.Id == SampleDataIds.WOODWORKING_BUNDLE_ID),
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
            SampleDataConstants.BUNDLES => await _context.Bundles.CountAsync(b => b.UserId == SampleDataIds.JOHN_DOE_USER_ID || b.UserId == SampleDataIds.JANE_SMITH_USER_ID),
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
            case SampleDataConstants.BUNDLES:
                await ApplySampleBundlesAsync(adminUserId);
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
            case SampleDataConstants.BUNDLES:
                await RemoveSampleBundlesAsync();
                break;
            case SampleDataConstants.TOOLS:
                await RemoveSampleToolsAsync();
                break;
            case SampleDataConstants.USERS:
                await RemoveSampleUsersAsync();
                break;
        }
    }

    // Sample data implementation methods
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
                LocationDisplay = "Downtown San Francisco",
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
                LocationDisplay = "East Oakland",
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

        // Remove bundles (must be before tools since bundles reference tools)
        var bundles = await _context.Bundles
            .Where(b => sampleUserIds.Contains(b.UserId))
            .ToListAsync();
        if (bundles.Any())
        {
            _context.Bundles.RemoveRange(bundles);
            _logger.LogInformation("Removed {Count} sample bundles", bundles.Count);
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
        if (await _context.Reviews.AnyAsync(r => r.ToolId == SampleDataIds.DRILL_TOOL_ID))
        {
            _logger.LogInformation("Sample reviews already exist, skipping");
            return;
        }

        _logger.LogInformation("Creating sample reviews...");

        var reviews = new[]
        {
            new Review
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                ToolId = SampleDataIds.DRILL_TOOL_ID,
                RentalId = SampleDataIds.RENTAL_1_ID,
                ReviewerId = SampleDataIds.JANE_SMITH_USER_ID,
                RevieweeId = SampleDataIds.JOHN_DOE_USER_ID,
                Rating = 5,
                Title = "Excellent drill, worked perfectly!",
                Comment = "John's drill was in excellent condition and worked perfectly for my home renovation project. Very reliable and powerful. Would definitely rent from John again!",
                Type = ReviewType.UserReview,
                CreatedAt = DateTime.UtcNow.AddDays(-23),
                UpdatedAt = DateTime.UtcNow.AddDays(-23)
            },
            new Review
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                ToolId = SampleDataIds.LADDER_TOOL_ID,
                RentalId = SampleDataIds.RENTAL_2_ID,
                ReviewerId = SampleDataIds.JOHN_DOE_USER_ID,
                RevieweeId = SampleDataIds.JANE_SMITH_USER_ID,
                Rating = 4,
                Title = "Good ladder, fair price",
                Comment = "The ladder was sturdy and perfect for my needs. Jane was very responsive and helpful. Only minor issue was a small dent, but it didn't affect functionality.",
                Type = ReviewType.UserReview,
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                UpdatedAt = DateTime.UtcNow.AddDays(-8)
            },
            new Review
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                ToolId = SampleDataIds.DRILL_TOOL_ID,
                ReviewerId = SampleDataIds.JOHN_DOE_USER_ID,
                RevieweeId = SampleDataIds.JANE_SMITH_USER_ID,
                Rating = 5,
                Title = "Great renter, took good care of my tools",
                Comment = "Jane was an excellent renter. She picked up and returned the drill on time, and it was in perfect condition. Very professional and trustworthy. Highly recommend!",
                Type = ReviewType.UserReview,
                CreatedAt = DateTime.UtcNow.AddDays(-22),
                UpdatedAt = DateTime.UtcNow.AddDays(-22)
            },
            new Review
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                ToolId = SampleDataIds.LADDER_TOOL_ID,
                ReviewerId = SampleDataIds.JANE_SMITH_USER_ID,
                RevieweeId = SampleDataIds.JOHN_DOE_USER_ID,
                Rating = 4,
                Title = "Reliable renter, good communication",
                Comment = "John was very communicative throughout the rental process. He returned the ladder clean and in good condition. Would rent to him again without hesitation.",
                Type = ReviewType.UserReview,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow.AddDays(-7)
            }
        };

        _context.Reviews.AddRange(reviews);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created {Count} sample reviews", reviews.Length);
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
        if (await _context.Conversations.AnyAsync(c => c.Id == SampleDataIds.CONVERSATION_1_ID))
        {
            _logger.LogInformation("Sample conversations already exist, skipping");
            return;
        }

        _logger.LogInformation("Creating sample conversations...");

        var conversations = new[]
        {
            new Conversation
            {
                Id = SampleDataIds.CONVERSATION_1_ID,
                Participant1Id = SampleDataIds.JOHN_DOE_USER_ID,
                Participant2Id = SampleDataIds.JANE_SMITH_USER_ID,
                Title = "Drill Rental Discussion",
                RentalId = SampleDataIds.RENTAL_1_ID,
                ToolId = SampleDataIds.DRILL_TOOL_ID,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-25)
            },
            new Conversation
            {
                Id = SampleDataIds.CONVERSATION_2_ID,
                Participant1Id = SampleDataIds.JANE_SMITH_USER_ID,
                Participant2Id = SampleDataIds.JOHN_DOE_USER_ID,
                Title = "Ladder Rental Questions",
                RentalId = SampleDataIds.RENTAL_2_ID,
                ToolId = SampleDataIds.LADDER_TOOL_ID,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new Conversation
            {
                Id = SampleDataIds.CONVERSATION_3_ID,
                Participant1Id = SampleDataIds.JOHN_DOE_USER_ID,
                Participant2Id = SampleDataIds.JANE_SMITH_USER_ID,
                Title = "General Chat",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        _context.Conversations.AddRange(conversations);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created {Count} sample conversations", conversations.Length);
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
        if (await _context.Messages.AnyAsync(m => m.ConversationId == SampleDataIds.CONVERSATION_1_ID))
        {
            _logger.LogInformation("Sample messages already exist, skipping");
            return;
        }

        // Ensure conversations exist first - if not, create them
        var conversationsExist = await _context.Conversations.AnyAsync(c => 
            c.Id == SampleDataIds.CONVERSATION_1_ID || 
            c.Id == SampleDataIds.CONVERSATION_2_ID || 
            c.Id == SampleDataIds.CONVERSATION_3_ID);

        if (!conversationsExist)
        {
            _logger.LogInformation("Required conversations don't exist, creating them first...");
            await ApplySampleConversationsAsync();
        }

        _logger.LogInformation("Creating sample messages...");

        var messages = new[]
        {
            // Conversation 1: Drill Rental Discussion
            new Message
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                SenderId = SampleDataIds.JANE_SMITH_USER_ID,
                RecipientId = SampleDataIds.JOHN_DOE_USER_ID,
                ConversationId = SampleDataIds.CONVERSATION_1_ID,
                RentalId = SampleDataIds.RENTAL_1_ID,
                ToolId = SampleDataIds.DRILL_TOOL_ID,
                Subject = "Question about your Professional Drill",
                Content = "Hi John! I'm interested in renting your DeWalt drill for a home improvement project. Is it available for this weekend? Also, does it come with drill bits?",
                Priority = MessagePriority.Normal,
                Type = MessageType.ToolInquiry,
                IsRead = true,
                ReadAt = DateTime.UtcNow.AddDays(-30).AddMinutes(45),
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new Message
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                SenderId = SampleDataIds.JOHN_DOE_USER_ID,
                RecipientId = SampleDataIds.JANE_SMITH_USER_ID,
                ConversationId = SampleDataIds.CONVERSATION_1_ID,
                RentalId = SampleDataIds.RENTAL_1_ID,
                ToolId = SampleDataIds.DRILL_TOOL_ID,
                Subject = "Re: Question about your Professional Drill",
                Content = "Hi Jane! Yes, the drill is available this weekend. It comes with a basic set of drill bits and driver bits. The battery is fully charged and I have a spare one too. Let me know what dates work for you!",
                Priority = MessagePriority.Normal,
                Type = MessageType.Direct,
                IsRead = true,
                ReadAt = DateTime.UtcNow.AddDays(-29).AddHours(2),
                CreatedAt = DateTime.UtcNow.AddDays(-29),
                UpdatedAt = DateTime.UtcNow.AddDays(-29)
            },
            new Message
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                SenderId = SampleDataIds.JANE_SMITH_USER_ID,
                RecipientId = SampleDataIds.JOHN_DOE_USER_ID,
                ConversationId = SampleDataIds.CONVERSATION_1_ID,
                RentalId = SampleDataIds.RENTAL_1_ID,
                ToolId = SampleDataIds.DRILL_TOOL_ID,
                Subject = "Re: Question about your Professional Drill",
                Content = "Perfect! Saturday to Monday would work great for me. I'll pick it up Saturday morning and return it Monday evening. Thanks for including the extra battery - that's very helpful!",
                Priority = MessagePriority.Normal,
                Type = MessageType.Direct,
                IsRead = true,
                ReadAt = DateTime.UtcNow.AddDays(-28).AddHours(1),
                CreatedAt = DateTime.UtcNow.AddDays(-28),
                UpdatedAt = DateTime.UtcNow.AddDays(-28)
            },
            
            // Conversation 2: Ladder Rental Questions
            new Message
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
                SenderId = SampleDataIds.JOHN_DOE_USER_ID,
                RecipientId = SampleDataIds.JANE_SMITH_USER_ID,
                ConversationId = SampleDataIds.CONVERSATION_2_ID,
                RentalId = SampleDataIds.RENTAL_2_ID,
                ToolId = SampleDataIds.LADDER_TOOL_ID,
                Subject = "Ladder rental request",
                Content = "Hi Jane! I saw your 8ft Werner ladder listing. I need it for cleaning gutters this weekend. Is it still available? Also, what's the weight limit?",
                Priority = MessagePriority.Normal,
                Type = MessageType.ToolInquiry,
                IsRead = true,
                ReadAt = DateTime.UtcNow.AddDays(-15).AddMinutes(30),
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new Message
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
                SenderId = SampleDataIds.JANE_SMITH_USER_ID,
                RecipientId = SampleDataIds.JOHN_DOE_USER_ID,
                ConversationId = SampleDataIds.CONVERSATION_2_ID,
                RentalId = SampleDataIds.RENTAL_2_ID,
                ToolId = SampleDataIds.LADDER_TOOL_ID,
                Subject = "Re: Ladder rental request",
                Content = "Hi John! Yes, the ladder is available. It's rated for 300 lbs and is perfect for gutter cleaning. I can have it ready for pickup Friday evening or Saturday morning. Safety tip: make sure you have someone spot you when using it!",
                Priority = MessagePriority.Normal,
                Type = MessageType.Direct,
                IsRead = true,
                ReadAt = DateTime.UtcNow.AddDays(-14).AddHours(3),
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-14)
            },
            
            // Conversation 3: General Chat
            new Message
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000006"),
                SenderId = SampleDataIds.JANE_SMITH_USER_ID,
                RecipientId = SampleDataIds.JOHN_DOE_USER_ID,
                ConversationId = SampleDataIds.CONVERSATION_3_ID,
                Subject = "Thanks for being a great neighbor!",
                Content = "Hey John! I just wanted to say thanks for all the tool rentals. It's been so helpful having access to quality tools without having to buy everything. You're making this neighborhood sharing economy really work!",
                Priority = MessagePriority.Normal,
                Type = MessageType.Direct,
                IsRead = true,
                ReadAt = DateTime.UtcNow.AddDays(-3).AddHours(2),
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new Message
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000007"),
                SenderId = SampleDataIds.JOHN_DOE_USER_ID,
                RecipientId = SampleDataIds.JANE_SMITH_USER_ID,
                ConversationId = SampleDataIds.CONVERSATION_3_ID,
                Subject = "Re: Thanks for being a great neighbor!",
                Content = "Aw, thank you Jane! That really means a lot. I love that we can help each other out. Your pressure washer saved me a ton of money on my deck cleaning project. Looking forward to more tool sharing adventures! 😊",
                Priority = MessagePriority.Normal,
                Type = MessageType.Direct,
                IsRead = true,
                ReadAt = DateTime.UtcNow.AddDays(-2).AddHours(1),
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new Message
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000008"),
                SenderId = SampleDataIds.JANE_SMITH_USER_ID,
                RecipientId = SampleDataIds.JOHN_DOE_USER_ID,
                ConversationId = SampleDataIds.CONVERSATION_3_ID,
                Subject = "Re: Thanks for being a great neighbor!",
                Content = "By the way, I'm thinking of organizing a neighborhood tool share meetup. Maybe we could get more neighbors involved? What do you think?",
                Priority = MessagePriority.Normal,
                Type = MessageType.Direct,
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        _context.Messages.AddRange(messages);
        await _context.SaveChangesAsync();

        // Update conversation last message info
        var conversations = await _context.Conversations
            .Where(c => c.Id == SampleDataIds.CONVERSATION_1_ID ||
                       c.Id == SampleDataIds.CONVERSATION_2_ID ||
                       c.Id == SampleDataIds.CONVERSATION_3_ID)
            .ToArrayAsync();

        foreach (var conversation in conversations)
        {
            var lastMessage = messages
                .Where(m => m.ConversationId == conversation.Id)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefault();
                
            if (lastMessage != null)
            {
                conversation.LastMessageId = lastMessage.Id;
                conversation.LastMessageAt = lastMessage.CreatedAt;
                conversation.UpdatedAt = lastMessage.CreatedAt;
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Created {MessageCount} sample messages in {ConversationCount} conversations", 
            messages.Length, conversations.Length);
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

    private async Task ApplySampleBundlesAsync(string adminUserId)
    {
        if (await _context.Bundles.AnyAsync(b => b.Id == SampleDataIds.WOODWORKING_BUNDLE_ID))
        {
            _logger.LogInformation("Sample bundles already exist, skipping");
            return;
        }

        _logger.LogInformation("Creating sample bundles...");

        var bundles = new[]
        {
            new Bundle
            {
                Id = SampleDataIds.WOODWORKING_BUNDLE_ID,
                Name = "Complete Woodworking Project Kit",
                Description = "Everything you need for basic woodworking projects like shelves, tables, or DIY furniture",
                Guidelines = "Perfect for beginners to intermediate woodworkers. This bundle includes essential power tools for cutting, drilling, and finishing wood projects. Great for making shelves, small tables, picture frames, or other home furniture pieces.",
                Category = "Woodworking",
                RequiredSkillLevel = "Intermediate",
                EstimatedProjectDuration = 8, // 8 hours
                ImageUrl = "/images/bundles/woodworking-kit.jpg",
                UserId = SampleDataIds.JOHN_DOE_USER_ID,
                BundleDiscount = 15.0m, // 15% discount
                IsPublished = true,
                IsFeatured = true,
                Tags = "woodworking,furniture,DIY,power tools,beginner friendly",
                ViewCount = 45,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-5),
                // Approval fields - this bundle is approved
                IsApproved = true,
                PendingApproval = false,
                ApprovedAt = DateTime.UtcNow.AddDays(-25),
                ApprovedById = adminUserId
            },
            new Bundle
            {
                Id = SampleDataIds.HOME_IMPROVEMENT_BUNDLE_ID,
                Name = "Home Improvement Essentials",
                Description = "Complete toolkit for home renovation and improvement projects",
                Guidelines = "Ideal for DIY enthusiasts tackling home improvement projects. This comprehensive bundle covers most common tasks like installing fixtures, painting prep, basic construction, and cleaning. Suitable for bathroom renovations, kitchen updates, or general home maintenance.",
                Category = "Home Improvement",
                RequiredSkillLevel = "Beginner",
                EstimatedProjectDuration = 16, // 16 hours (2 days)
                ImageUrl = "/images/bundles/home-improvement.jpg",
                UserId = SampleDataIds.JANE_SMITH_USER_ID,
                BundleDiscount = 10.0m, // 10% discount
                IsPublished = true,
                IsFeatured = false,
                Tags = "home improvement,renovation,DIY,maintenance,upgrade",
                ViewCount = 23,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-3),
                // Approval fields - this bundle is PENDING for testing
                IsApproved = false,
                PendingApproval = true
            },
            new Bundle
            {
                Id = SampleDataIds.GARDEN_PREP_BUNDLE_ID,
                Name = "Garden Preparation & Cleanup",
                Description = "Essential tools for spring garden prep and seasonal cleanup",
                Guidelines = "Perfect for seasonal garden preparation and maintenance. This bundle helps with cleaning, organizing, and preparing your outdoor spaces. Great for spring cleaning, fall preparation, or general yard maintenance throughout the year.",
                Category = "Gardening",
                RequiredSkillLevel = "Beginner",
                EstimatedProjectDuration = 6, // 6 hours
                ImageUrl = "/images/bundles/garden-cleanup.jpg",
                UserId = SampleDataIds.JANE_SMITH_USER_ID,
                BundleDiscount = 12.0m, // 12% discount
                IsPublished = true,
                IsFeatured = true,
                Tags = "gardening,cleanup,maintenance,outdoor,seasonal",
                ViewCount = 31,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                // Approval fields - this bundle is approved
                IsApproved = true,
                PendingApproval = false,
                ApprovedAt = DateTime.UtcNow.AddDays(-10),
                ApprovedById = adminUserId
            }
        };

        _context.Bundles.AddRange(bundles);

        // Create bundle tools (linking tools to bundles)
        var bundleTools = new[]
        {
            // Woodworking Bundle Tools
            new BundleTool
            {
                Id = Guid.NewGuid(),
                BundleId = SampleDataIds.WOODWORKING_BUNDLE_ID,
                ToolId = SampleDataIds.DRILL_TOOL_ID,
                QuantityNeeded = 1,
                IsOptional = false,
                OrderInBundle = 1,
                UsageNotes = "Primary tool for drilling pilot holes and driving screws"
            },
            new BundleTool
            {
                Id = Guid.NewGuid(),
                BundleId = SampleDataIds.WOODWORKING_BUNDLE_ID,
                ToolId = SampleDataIds.SAW_TOOL_ID,
                QuantityNeeded = 1,
                IsOptional = false,
                OrderInBundle = 2,
                UsageNotes = "Essential for cutting wood to size and making precision cuts"
            },

            // Home Improvement Bundle Tools
            new BundleTool
            {
                Id = Guid.NewGuid(),
                BundleId = SampleDataIds.HOME_IMPROVEMENT_BUNDLE_ID,
                ToolId = SampleDataIds.DRILL_TOOL_ID,
                QuantityNeeded = 1,
                IsOptional = false,
                OrderInBundle = 1,
                UsageNotes = "Versatile tool for mounting, fixture installation, and general assembly"
            },
            new BundleTool
            {
                Id = Guid.NewGuid(),
                BundleId = SampleDataIds.HOME_IMPROVEMENT_BUNDLE_ID,
                ToolId = SampleDataIds.LADDER_TOOL_ID,
                QuantityNeeded = 1,
                IsOptional = false,
                OrderInBundle = 2,
                UsageNotes = "Safe access for ceiling work, light fixtures, and high installations"
            },
            new BundleTool
            {
                Id = Guid.NewGuid(),
                BundleId = SampleDataIds.HOME_IMPROVEMENT_BUNDLE_ID,
                ToolId = SampleDataIds.PRESSURE_WASHER_TOOL_ID,
                QuantityNeeded = 1,
                IsOptional = true,
                OrderInBundle = 3,
                UsageNotes = "Optional for exterior cleaning and surface preparation"
            },

            // Garden Preparation Bundle Tools
            new BundleTool
            {
                Id = Guid.NewGuid(),
                BundleId = SampleDataIds.GARDEN_PREP_BUNDLE_ID,
                ToolId = SampleDataIds.LADDER_TOOL_ID,
                QuantityNeeded = 1,
                IsOptional = false,
                OrderInBundle = 1,
                UsageNotes = "Access for pruning trees and cleaning gutters"
            },
            new BundleTool
            {
                Id = Guid.NewGuid(),
                BundleId = SampleDataIds.GARDEN_PREP_BUNDLE_ID,
                ToolId = SampleDataIds.PRESSURE_WASHER_TOOL_ID,
                QuantityNeeded = 1,
                IsOptional = false,
                OrderInBundle = 2,
                UsageNotes = "Clean patios, decks, driveways, and outdoor furniture"
            }
        };

        _context.BundleTools.AddRange(bundleTools);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created {BundleCount} sample bundles with {ToolCount} bundle tools", bundles.Length, bundleTools.Length);
    }

    private async Task RemoveSampleBundlesAsync()
    {
        var sampleUserIds = new[] { SampleDataIds.JOHN_DOE_USER_ID, SampleDataIds.JANE_SMITH_USER_ID };
        var bundles = await _context.Bundles
            .Where(b => sampleUserIds.Contains(b.UserId))
            .ToListAsync();

        if (bundles.Any())
        {
            _context.Bundles.RemoveRange(bundles);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Removed {Count} sample bundles", bundles.Count);
        }
    }
}