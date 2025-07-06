using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
        
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<User>>();
            
            await SeedUsersAsync(userManager, logger);
            await SeedToolsAsync(context, logger);
            
            logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
    
    private static async Task SeedUsersAsync(UserManager<User> userManager, ILogger logger)
    {
        // Check if users already exist
        if (await userManager.Users.AnyAsync())
        {
            logger.LogInformation("Users already exist, skipping user seeding");
            return;
        }
        
        logger.LogInformation("Seeding users...");
        
        var users = new[]
        {
            new User
            {
                Id = "user1-guid-1234-5678-9012345678901",
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
                DateOfBirth = new DateTime(1985, 5, 15),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = "user2-guid-1234-5678-9012345678902",
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
                DateOfBirth = new DateTime(1990, 8, 22),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
        
        foreach (var user in users)
        {
            var result = await userManager.CreateAsync(user, "Password123!");
            if (result.Succeeded)
            {
                logger.LogInformation("Created user: {Email}", user.Email);
            }
            else
            {
                logger.LogError("Failed to create user {Email}: {Errors}", 
                    user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
    
    private static async Task SeedToolsAsync(ApplicationDbContext context, ILogger logger)
    {
        // Check if tools already exist
        if (await context.Tools.AnyAsync())
        {
            logger.LogInformation("Tools already exist, skipping tool seeding");
            return;
        }
        
        logger.LogInformation("Seeding tools...");
        
        var tools = new[]
        {
            new Tool
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
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
                Location = "San Francisco, CA",
                IsAvailable = true,
                OwnerId = "user1-guid-1234-5678-9012345678901",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Tool
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
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
                Location = "San Francisco, CA",
                IsAvailable = true,
                OwnerId = "user1-guid-1234-5678-9012345678901",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Tool
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
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
                Location = "Oakland, CA",
                IsAvailable = true,
                OwnerId = "user2-guid-1234-5678-9012345678902",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Tool
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
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
                Location = "Berkeley, CA",
                IsAvailable = true,
                OwnerId = "user2-guid-1234-5678-9012345678902",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
        
        context.Tools.AddRange(tools);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Seeded {Count} tools", tools.Length);
    }
}