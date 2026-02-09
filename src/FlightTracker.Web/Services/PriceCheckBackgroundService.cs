using FlightTracker.Core.Services;

namespace FlightTracker.Web.Services;

public class PriceCheckBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PriceCheckBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(12); // 8 AM and 8 PM = 12 hour interval

    public PriceCheckBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<PriceCheckBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Price Check Background Service started");

        // Wait until next scheduled time (8 AM or 8 PM CET)
        await WaitUntilNextScheduledTime(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting scheduled price check at {Time}", DateTime.UtcNow);
                await RunPriceCheck(stoppingToken);
                _logger.LogInformation("Price check completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during scheduled price check");
            }

            // Wait 12 hours until next check
            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected when service is stopping
                _logger.LogInformation("Price check service stopping");
                break;
            }
        }

        _logger.LogInformation("Price Check Background Service stopped");
    }

    private async Task RunPriceCheck(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var searchService = scope.ServiceProvider.GetRequiredService<FlightSearchService>();
        var configService = scope.ServiceProvider.GetRequiredService<ConfigurationService>();

        var originAirport = configService.OriginAirport;

        var successCount = await searchService.SearchAllRoutesAsync(
            originAirport,
            cancellationToken);

        _logger.LogInformation(
            "Price check complete: {SuccessCount} routes successfully checked",
            successCount);
    }

    private async Task WaitUntilNextScheduledTime(CancellationToken cancellationToken)
    {
        var berlinTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
        var nowBerlin = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, berlinTimeZone);

        // Scheduled times: 8 AM and 8 PM
        var scheduledHours = new[] { 8, 20 };

        // Find next scheduled time
        TimeSpan delay;
        var nextScheduledTime = scheduledHours
            .Select(hour => nowBerlin.Date.AddHours(hour))
            .Where(time => time > nowBerlin)
            .OrderBy(time => time)
            .FirstOrDefault();

        if (nextScheduledTime == default)
        {
            // No more today, use first slot tomorrow
            nextScheduledTime = nowBerlin.Date.AddDays(1).AddHours(scheduledHours[0]);
        }

        delay = nextScheduledTime - nowBerlin;

        _logger.LogInformation(
            "Waiting {Hours}h {Minutes}m until next price check at {NextTime} CET",
            (int)delay.TotalHours,
            delay.Minutes,
            nextScheduledTime);

        if (delay > TimeSpan.Zero)
        {
            try
            {
                await Task.Delay(delay, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Wait cancelled - service stopping");
            }
        }
    }
}
