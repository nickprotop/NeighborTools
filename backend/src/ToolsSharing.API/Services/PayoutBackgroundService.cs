using ToolsSharing.Core.Interfaces;

namespace ToolsSharing.API.Services;

public class PayoutBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PayoutBackgroundService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromMinutes(15); // Check every 15 minutes

    public PayoutBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<PayoutBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payout background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledPayouts();
                await Task.Delay(_period, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in payout background service");
                await Task.Delay(_period, stoppingToken);
            }
        }

        _logger.LogInformation("Payout background service stopped");
    }

    private async Task ProcessScheduledPayouts()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
            
            _logger.LogDebug("Processing scheduled payouts");
            await paymentService.ProcessScheduledPayoutsAsync();
            _logger.LogDebug("Scheduled payouts processing completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled payouts in background service");
        }
    }
}