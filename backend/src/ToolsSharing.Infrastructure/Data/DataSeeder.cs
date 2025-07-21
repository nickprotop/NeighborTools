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
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            
            await SeedRolesAsync(roleManager, logger);
            await SeedUsersAsync(userManager, logger);
            await SeedToolsAsync(context, logger);
            await SeedRentalsAsync(context, logger);
            await SeedReviewsAsync(context, logger);
            await SeedMessagesAsync(context, logger);
            
            logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
    
    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        // Check if roles already exist
        if (await roleManager.Roles.AnyAsync())
        {
            logger.LogInformation("Roles already exist, skipping role seeding");
            return;
        }
        
        logger.LogInformation("Seeding roles...");
        
        var roles = new[]
        {
            "Admin",
            "User"
        };
        
        foreach (var roleName in roles)
        {
            var role = new IdentityRole(roleName);
            var result = await roleManager.CreateAsync(role);
            
            if (result.Succeeded)
            {
                logger.LogInformation("Created role: {RoleName}", roleName);
            }
            else
            {
                logger.LogError("Failed to create role {RoleName}: {Errors}", 
                    roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
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
            var result = await userManager.CreateAsync(user, "Password123!");
            if (result.Succeeded)
            {
                logger.LogInformation("Created user: {Email}", user.Email);
                
                // Assign roles
                if (user.Email == "john.doe@email.com")
                {
                    // Make John Doe an admin
                    await userManager.AddToRoleAsync(user, "Admin");
                    await userManager.AddToRoleAsync(user, "User");
                    logger.LogInformation("Assigned Admin and User roles to {Email}", user.Email);
                }
                else
                {
                    // All other users get User role
                    await userManager.AddToRoleAsync(user, "User");
                    logger.LogInformation("Assigned User role to {Email}", user.Email);
                }
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
                Location = "Downtown San Francisco",
                IsAvailable = true,
                OwnerId = "user1-guid-1234-5678-9012345678901",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                // Approval fields - this tool is approved
                IsApproved = true,
                PendingApproval = false,
                ApprovedAt = DateTime.UtcNow.AddMinutes(-30),
                ApprovedById = "user1-guid-1234-5678-9012345678901" // Admin John approved it
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
                Location = "Downtown San Francisco",
                IsAvailable = true,
                OwnerId = "user1-guid-1234-5678-9012345678901",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                // Approval fields - this tool is PENDING for testing
                IsApproved = false,
                PendingApproval = true
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
                Location = "East Oakland",
                IsAvailable = true,
                OwnerId = "user2-guid-1234-5678-9012345678902",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                // Approval fields - this tool is approved
                IsApproved = true,
                PendingApproval = false,
                ApprovedAt = DateTime.UtcNow.AddMinutes(-45),
                ApprovedById = "user1-guid-1234-5678-9012345678901" // Admin John approved it
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
                Location = "Berkeley",
                IsAvailable = true,
                OwnerId = "user2-guid-1234-5678-9012345678902",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                // Approval fields - this tool is approved
                IsApproved = true,
                PendingApproval = false,
                ApprovedAt = DateTime.UtcNow.AddMinutes(-20),
                ApprovedById = "user1-guid-1234-5678-9012345678901" // Admin John approved it
            }
        };
        
        context.Tools.AddRange(tools);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Seeded {Count} tools", tools.Length);
    }
    
    private static async Task SeedRentalsAsync(ApplicationDbContext context, ILogger logger)
    {
        // Check if rentals already exist
        if (await context.Rentals.AnyAsync())
        {
            logger.LogInformation("Rentals already exist, skipping rental seeding");
            return;
        }
        
        logger.LogInformation("Seeding rentals...");
        
        var rentals = new[]
        {
            new Rental
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                ToolId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                RenterId = "user2-guid-1234-5678-9012345678902",
                OwnerId = "user1-guid-1234-5678-9012345678901",
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
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                ToolId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                RenterId = "user1-guid-1234-5678-9012345678901",
                OwnerId = "user2-guid-1234-5678-9012345678902",
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
                Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                ToolId = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                RenterId = "user1-guid-1234-5678-9012345678901",
                OwnerId = "user2-guid-1234-5678-9012345678902",
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(2),
                Status = RentalStatus.PickedUp,
                TotalCost = 210.00m,
                DepositAmount = 150.00m,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            }
        };
        
        context.Rentals.AddRange(rentals);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Seeded {Count} rentals", rentals.Length);
    }
    
    private static async Task SeedReviewsAsync(ApplicationDbContext context, ILogger logger)
    {
        // Check if reviews already exist
        if (await context.Reviews.AnyAsync())
        {
            logger.LogInformation("Reviews already exist, skipping review seeding");
            return;
        }
        
        logger.LogInformation("Seeding reviews...");
        
        var reviews = new[]
        {
            new Review
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                ToolId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                RentalId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                ReviewerId = "user2-guid-1234-5678-9012345678902",
                RevieweeId = "user1-guid-1234-5678-9012345678901",
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
                ToolId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                RentalId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                ReviewerId = "user1-guid-1234-5678-9012345678901",
                RevieweeId = "user2-guid-1234-5678-9012345678902",
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
                ToolId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                ReviewerId = "user1-guid-1234-5678-9012345678901",
                RevieweeId = "user2-guid-1234-5678-9012345678902",
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
                ToolId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                ReviewerId = "user2-guid-1234-5678-9012345678902",
                RevieweeId = "user1-guid-1234-5678-9012345678901",
                Rating = 4,
                Title = "Reliable renter, good communication",
                Comment = "John was very communicative throughout the rental process. He returned the ladder clean and in good condition. Would rent to him again without hesitation.",
                Type = ReviewType.UserReview,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow.AddDays(-7)
            }
        };
        
        context.Reviews.AddRange(reviews);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Seeded {Count} reviews", reviews.Length);
    }
    
    private static async Task SeedMessagesAsync(ApplicationDbContext context, ILogger logger)
    {
        // Check if messages already exist
        if (await context.Messages.AnyAsync())
        {
            logger.LogInformation("Messages already exist, skipping message seeding");
            return;
        }
        
        // Check if seeded conversations exist - if not, create them
        var conversationsExist = await context.Conversations.AnyAsync(c => 
            c.Id == Guid.Parse("00000000-0000-0000-0000-000000000001") ||
            c.Id == Guid.Parse("00000000-0000-0000-0000-000000000002") ||
            c.Id == Guid.Parse("00000000-0000-0000-0000-000000000003"));
            
        if (conversationsExist)
        {
            logger.LogInformation("Seeded conversations already exist, only adding messages");
        }
        
        logger.LogInformation("Seeding messages...");
        
        // Create conversations first (only if they don't exist)
        var conversations = new[]
        {
            new Conversation
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Participant1Id = "user1-guid-1234-5678-9012345678901", // John
                Participant2Id = "user2-guid-1234-5678-9012345678902", // Jane
                Title = "Drill Rental Discussion",
                RentalId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                ToolId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-25)
            },
            new Conversation
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Participant1Id = "user2-guid-1234-5678-9012345678902", // Jane
                Participant2Id = "user1-guid-1234-5678-9012345678901", // John
                Title = "Ladder Rental Questions",
                RentalId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                ToolId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new Conversation
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                Participant1Id = "user1-guid-1234-5678-9012345678901", // John
                Participant2Id = "user2-guid-1234-5678-9012345678902", // Jane
                Title = "General Chat",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };
        
        // Only add conversations that don't already exist
        if (!conversationsExist)
        {
            context.Conversations.AddRange(conversations);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded {ConversationCount} conversations", conversations.Length);
        }
        else
        {
            // Load existing conversations for message relationships
            var existingConversations = await context.Conversations
                .Where(c => c.Id == Guid.Parse("00000000-0000-0000-0000-000000000001") ||
                           c.Id == Guid.Parse("00000000-0000-0000-0000-000000000002") ||
                           c.Id == Guid.Parse("00000000-0000-0000-0000-000000000003"))
                .ToArrayAsync();
            conversations = existingConversations;
        }
        
        // Now create messages
        var messages = new[]
        {
            // Conversation 1: Drill Rental Discussion
            new Message
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                SenderId = "user2-guid-1234-5678-9012345678902", // Jane
                RecipientId = "user1-guid-1234-5678-9012345678901", // John
                ConversationId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                RentalId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                ToolId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
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
                SenderId = "user1-guid-1234-5678-9012345678901", // John
                RecipientId = "user2-guid-1234-5678-9012345678902", // Jane
                ConversationId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                RentalId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                ToolId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
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
                SenderId = "user2-guid-1234-5678-9012345678902", // Jane
                RecipientId = "user1-guid-1234-5678-9012345678901", // John
                ConversationId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                RentalId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                ToolId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
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
                SenderId = "user1-guid-1234-5678-9012345678901", // John
                RecipientId = "user2-guid-1234-5678-9012345678902", // Jane
                ConversationId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                RentalId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                ToolId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
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
                SenderId = "user2-guid-1234-5678-9012345678902", // Jane
                RecipientId = "user1-guid-1234-5678-9012345678901", // John
                ConversationId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                RentalId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                ToolId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
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
                SenderId = "user2-guid-1234-5678-9012345678902", // Jane
                RecipientId = "user1-guid-1234-5678-9012345678901", // John
                ConversationId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
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
                SenderId = "user1-guid-1234-5678-9012345678901", // John
                RecipientId = "user2-guid-1234-5678-9012345678902", // Jane
                ConversationId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
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
                SenderId = "user2-guid-1234-5678-9012345678902", // Jane
                RecipientId = "user1-guid-1234-5678-9012345678901", // John
                ConversationId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                Subject = "Re: Thanks for being a great neighbor!",
                Content = "By the way, I'm thinking of organizing a neighborhood tool share meetup. Maybe we could get more neighbors involved? What do you think?",
                Priority = MessagePriority.Normal,
                Type = MessageType.Direct,
                IsRead = false, // Unread message for testing
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };
        
        context.Messages.AddRange(messages);
        await context.SaveChangesAsync();
        
        // Update conversation last message info
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
        
        await context.SaveChangesAsync();
        
        logger.LogInformation("Seeded {MessageCount} messages in {ConversationCount} conversations", 
            messages.Length, conversations.Length);
    }
}