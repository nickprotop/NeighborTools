using Microsoft.EntityFrameworkCore;
using ToolsSharing.Core.Entities;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.API.Services;

public class RentalCompletionService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RentalCompletionService> _logger;

    public RentalCompletionService(IServiceProvider serviceProvider, ILogger<RentalCompletionService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDisputeDeadlines();
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Check every hour
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing rental completion");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait 5 minutes before retry
            }
        }
    }

    private async Task ProcessDisputeDeadlines()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;
        
        // Find all rentals that are "Returned" and past their dispute deadline
        var completableRentals = await context.Rentals
            .Where(r => r.Status == RentalStatus.Returned && 
                       r.DisputeDeadline.HasValue && 
                       r.DisputeDeadline <= now)
            .ToListAsync();

        foreach (var rental in completableRentals)
        {
            try
            {
                // Mark rental as completed
                rental.Status = RentalStatus.Completed;
                rental.UpdatedAt = now;

                _logger.LogInformation($"Rental {rental.Id} marked as completed after dispute deadline");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error completing rental {rental.Id}");
            }
        }

        if (completableRentals.Any())
        {
            await context.SaveChangesAsync();
            _logger.LogInformation($"Completed {completableRentals.Count} rentals after dispute deadline");
        }
    }

}