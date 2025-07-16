using Microsoft.EntityFrameworkCore;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Common.Models.EmailNotifications;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Features.Rentals;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.API.Services;

public class RentalLifecycleService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RentalLifecycleService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15); // Check every 15 minutes

    public RentalLifecycleService(IServiceProvider serviceProvider, ILogger<RentalLifecycleService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RentalLifecycleService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRentalLifecycleAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing rental lifecycle");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task ProcessRentalLifecycleAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();
        var mobileNotificationService = scope.ServiceProvider.GetRequiredService<IMobileNotificationService>();
        var smsService = scope.ServiceProvider.GetRequiredService<ISmsNotificationService>();

        try
        {
            var currentTime = DateTime.UtcNow;
            
            // Process return reminders (2 days before, 1 day before, same day)
            await ProcessReturnRemindersAsync(context, emailService, mobileNotificationService, smsService, currentTime);
            
            // Process overdue rentals
            await ProcessOverdueRentalsAsync(context, emailService, mobileNotificationService, smsService, currentTime);
            
            // Process pickup reminders (same day as start date)
            await ProcessPickupRemindersAsync(context, emailService, mobileNotificationService, smsService, currentTime);
            
            _logger.LogInformation("Rental lifecycle processing completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in rental lifecycle processing");
        }
    }

    private async Task ProcessReturnRemindersAsync(
        ApplicationDbContext context, 
        IEmailNotificationService emailService,
        IMobileNotificationService mobileNotificationService,
        ISmsNotificationService smsService,
        DateTime currentTime)
    {
        // Get rentals that need return reminders
        var rentalsForReminder = await context.Rentals
            .Include(r => r.Tool)
                .ThenInclude(t => t.Owner)
            .Include(r => r.Renter)
            .Where(r => r.Status == RentalStatus.PickedUp || r.Status == RentalStatus.Approved)
            .Where(r => r.EndDate >= currentTime && r.EndDate <= currentTime.AddDays(2))
            .ToListAsync();

        foreach (var rental in rentalsForReminder)
        {
            var hoursUntilReturn = (rental.EndDate - currentTime).TotalHours;
            string reminderType;
            
            if (hoursUntilReturn <= 2) // Same day reminder
            {
                reminderType = "return_due_today";
            }
            else if (hoursUntilReturn <= 26) // 1 day before (with 2-hour buffer)
            {
                reminderType = "return_due_tomorrow";
            }
            else if (hoursUntilReturn <= 50) // 2 days before (with 2-hour buffer)
            {
                reminderType = "return_due_soon";
            }
            else
            {
                continue; // Skip if not within reminder window
            }

            // Check if we already sent this type of reminder
            var alreadySent = await context.RentalNotifications
                .AnyAsync(rn => rn.RentalId == rental.Id && 
                              rn.NotificationType == reminderType &&
                              rn.SentAt >= currentTime.AddDays(-1));

            if (alreadySent)
                continue;

            // Send email notification
            try
            {
                var emailNotification = new RentalReminderNotification
                {
                    RecipientEmail = rental.Renter.Email!,
                    RecipientName = $"{rental.Renter.FirstName} {rental.Renter.LastName}",
                    UserId = rental.RenterId,
                    RenterName = $"{rental.Renter.FirstName} {rental.Renter.LastName}",
                    OwnerName = $"{rental.Tool.Owner.FirstName} {rental.Tool.Owner.LastName}",
                    ToolName = rental.Tool.Name,
                    StartDate = rental.StartDate,
                    EndDate = rental.EndDate,
                    ReminderType = reminderType,
                    RentalDetailsUrl = $"/rentals/{rental.Id}",
                    Priority = reminderType == "return_due_today" ? EmailPriority.High : EmailPriority.Normal
                };

                await emailService.SendNotificationAsync(emailNotification);

                // Send mobile notification
                // TODO: Implement mobile push notification sending
                await mobileNotificationService.SendReturnReminderAsync(rental.RenterId, rental.Id, reminderType);

                // Send SMS for urgent reminders
                if (reminderType == "return_due_today")
                {
                    // TODO: Implement SMS notification sending
                    await smsService.SendReturnReminderAsync(rental.Renter.PhoneNumber, rental.Tool.Name, rental.EndDate);
                }

                // Record notification sent
                context.RentalNotifications.Add(new RentalNotification
                {
                    RentalId = rental.Id,
                    NotificationType = reminderType,
                    SentAt = currentTime,
                    RecipientId = rental.RenterId
                });

                _logger.LogInformation($"Sent {reminderType} reminder for rental {rental.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send {reminderType} reminder for rental {rental.Id}");
            }
        }

        await context.SaveChangesAsync();
    }

    private async Task ProcessOverdueRentalsAsync(
        ApplicationDbContext context, 
        IEmailNotificationService emailService,
        IMobileNotificationService mobileNotificationService,
        ISmsNotificationService smsService,
        DateTime currentTime)
    {
        // Find rentals that are overdue but still marked as picked up
        var overdueRentals = await context.Rentals
            .Include(r => r.Tool)
                .ThenInclude(t => t.Owner)
            .Include(r => r.Renter)
            .Where(r => r.Status == RentalStatus.PickedUp && r.EndDate < currentTime)
            .ToListAsync();

        foreach (var rental in overdueRentals)
        {
            // Update status to overdue
            rental.Status = RentalStatus.Overdue;
            rental.UpdatedAt = currentTime;

            var daysOverdue = (currentTime - rental.EndDate).Days;
            
            // Send overdue notifications based on escalation schedule
            string notificationType;
            bool sendSms = false;
            
            if (daysOverdue == 1)
            {
                notificationType = "overdue_day_1";
            }
            else if (daysOverdue == 3)
            {
                notificationType = "overdue_day_3";
                sendSms = true;
            }
            else if (daysOverdue == 7)
            {
                notificationType = "overdue_day_7";
                sendSms = true;
            }
            else if (daysOverdue % 7 == 0 && daysOverdue > 7) // Weekly after first week
            {
                notificationType = "overdue_weekly";
                sendSms = true;
            }
            else
            {
                continue; // Skip if not a notification day
            }

            // Check if we already sent this type of notification
            var alreadySent = await context.RentalNotifications
                .AnyAsync(rn => rn.RentalId == rental.Id && 
                              rn.NotificationType == notificationType &&
                              rn.SentAt >= currentTime.AddDays(-1));

            if (alreadySent)
                continue;

            try
            {
                // Send email to renter
                var renterNotification = new RentalReminderNotification
                {
                    RecipientEmail = rental.Renter.Email!,
                    RecipientName = $"{rental.Renter.FirstName} {rental.Renter.LastName}",
                    UserId = rental.RenterId,
                    RenterName = $"{rental.Renter.FirstName} {rental.Renter.LastName}",
                    OwnerName = $"{rental.Tool.Owner.FirstName} {rental.Tool.Owner.LastName}",
                    ToolName = rental.Tool.Name,
                    StartDate = rental.StartDate,
                    EndDate = rental.EndDate,
                    ReminderType = notificationType,
                    RentalDetailsUrl = $"/rentals/{rental.Id}",
                    Priority = EmailPriority.High
                };

                await emailService.SendNotificationAsync(renterNotification);

                // Send email to owner for escalation
                if (daysOverdue >= 3)
                {
                    var ownerNotification = new RentalReminderNotification
                    {
                        RecipientEmail = rental.Tool.Owner.Email!,
                        RecipientName = $"{rental.Tool.Owner.FirstName} {rental.Tool.Owner.LastName}",
                        UserId = rental.Tool.OwnerId,
                        RenterName = $"{rental.Renter.FirstName} {rental.Renter.LastName}",
                        OwnerName = $"{rental.Tool.Owner.FirstName} {rental.Tool.Owner.LastName}",
                        ToolName = rental.Tool.Name,
                        StartDate = rental.StartDate,
                        EndDate = rental.EndDate,
                        ReminderType = $"owner_{notificationType}",
                        RentalDetailsUrl = $"/rentals/{rental.Id}",
                        Priority = EmailPriority.High
                    };

                    await emailService.SendNotificationAsync(ownerNotification);
                }

                // Send mobile notifications
                // TODO: Implement mobile push notification sending
                await mobileNotificationService.SendOverdueNotificationAsync(rental.RenterId, rental.Id, daysOverdue);

                // Send SMS for urgent cases
                if (sendSms)
                {
                    // TODO: Implement SMS notification sending
                    await smsService.SendOverdueNotificationAsync(rental.Renter.PhoneNumber, rental.Tool.Name, daysOverdue);
                }

                // Record notification sent
                context.RentalNotifications.Add(new RentalNotification
                {
                    RentalId = rental.Id,
                    NotificationType = notificationType,
                    SentAt = currentTime,
                    RecipientId = rental.RenterId
                });

                _logger.LogInformation($"Sent {notificationType} notification for rental {rental.Id} ({daysOverdue} days overdue)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send {notificationType} notification for rental {rental.Id}");
            }
        }

        await context.SaveChangesAsync();
    }

    private async Task ProcessPickupRemindersAsync(
        ApplicationDbContext context, 
        IEmailNotificationService emailService,
        IMobileNotificationService mobileNotificationService,
        ISmsNotificationService smsService,
        DateTime currentTime)
    {
        // Get approved rentals starting today that haven't been picked up
        var rentalsForPickup = await context.Rentals
            .Include(r => r.Tool)
                .ThenInclude(t => t.Owner)
            .Include(r => r.Renter)
            .Where(r => r.Status == RentalStatus.Approved)
            .Where(r => r.StartDate.Date == currentTime.Date)
            .ToListAsync();

        foreach (var rental in rentalsForPickup)
        {
            // Check if we already sent pickup reminder today
            var alreadySent = await context.RentalNotifications
                .AnyAsync(rn => rn.RentalId == rental.Id && 
                              rn.NotificationType == "pickup_reminder" &&
                              rn.SentAt >= currentTime.Date);

            if (alreadySent)
                continue;

            try
            {
                var pickupNotification = new RentalReminderNotification
                {
                    RecipientEmail = rental.Renter.Email!,
                    RecipientName = $"{rental.Renter.FirstName} {rental.Renter.LastName}",
                    UserId = rental.RenterId,
                    RenterName = $"{rental.Renter.FirstName} {rental.Renter.LastName}",
                    OwnerName = $"{rental.Tool.Owner.FirstName} {rental.Tool.Owner.LastName}",
                    ToolName = rental.Tool.Name,
                    StartDate = rental.StartDate,
                    EndDate = rental.EndDate,
                    ReminderType = "pickup_reminder",
                    RentalDetailsUrl = $"/rentals/{rental.Id}",
                    Priority = EmailPriority.Normal
                };

                await emailService.SendNotificationAsync(pickupNotification);

                // Send mobile notification
                // TODO: Implement mobile push notification sending
                await mobileNotificationService.SendPickupReminderAsync(rental.RenterId, rental.Id);

                // Record notification sent
                context.RentalNotifications.Add(new RentalNotification
                {
                    RentalId = rental.Id,
                    NotificationType = "pickup_reminder",
                    SentAt = currentTime,
                    RecipientId = rental.RenterId
                });

                _logger.LogInformation($"Sent pickup reminder for rental {rental.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send pickup reminder for rental {rental.Id}");
            }
        }

        await context.SaveChangesAsync();
    }
}