using FlightTracker.Core.Entities;
using FlightTracker.Core.Interfaces;
using FlightTracker.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlightTracker.Core.Services;

/// <summary>
/// Service for managing application configuration and syncing with database.
/// </summary>
public class ConfigurationService
{
    private readonly AppConfig _config;
    private readonly IDestinationRepository _destinationRepository;
    private readonly ITargetDateRepository _targetDateRepository;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(
        IOptions<AppConfig> config,
        IDestinationRepository destinationRepository,
        ITargetDateRepository targetDateRepository,
        ILogger<ConfigurationService> logger)
    {
        _config = config.Value;
        _destinationRepository = destinationRepository;
        _targetDateRepository = targetDateRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get origin airport code from configuration.
    /// </summary>
    public string OriginAirport => _config.Origin;

    /// <summary>
    /// Get all destinations from configuration.
    /// </summary>
    public IEnumerable<DestinationConfig> Destinations => _config.Destinations;

    /// <summary>
    /// Get all target dates from configuration.
    /// </summary>
    public IEnumerable<TargetDateConfig> TargetDates => _config.TargetDates;

    /// <summary>
    /// Initialize database with destinations from configuration.
    /// Creates missing destinations, updates existing ones.
    /// </summary>
    public async Task InitializeDestinationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing destinations from configuration...");

        foreach (var destConfig in _config.Destinations)
        {
            var existing = await _destinationRepository.GetByAirportCodeAsync(
                destConfig.Code,
                cancellationToken);

            if (existing == null)
            {
                // Create new destination
                var destination = new Destination
                {
                    AirportCode = destConfig.Code,
                    Name = destConfig.Name
                };

                await _destinationRepository.AddAsync(destination, cancellationToken);
                _logger.LogInformation(
                    "Created destination: {Code} - {Name}",
                    destination.AirportCode,
                    destination.Name);
            }
            else
            {
                // Update name if changed
                if (existing.Name != destConfig.Name)
                {
                    existing.Name = destConfig.Name;
                    await _destinationRepository.UpdateAsync(existing, cancellationToken);
                    _logger.LogInformation(
                        "Updated destination: {Code} - {Name}",
                        existing.AirportCode,
                        existing.Name);
                }
            }
        }

        await _destinationRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Destinations initialized successfully");
    }

    /// <summary>
    /// Initialize database with target dates from configuration.
    /// Creates missing date ranges, updates existing ones.
    /// </summary>
    public async Task InitializeTargetDatesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing target dates from configuration...");

        foreach (var dateConfig in _config.TargetDates)
        {
            if (!DateTime.TryParse(dateConfig.Outbound, out var outboundDate))
            {
                _logger.LogWarning(
                    "Invalid outbound date format: {Date}",
                    dateConfig.Outbound);
                continue;
            }

            if (!DateTime.TryParse(dateConfig.Return, out var returnDate))
            {
                _logger.LogWarning(
                    "Invalid return date format: {Date}",
                    dateConfig.Return);
                continue;
            }

            var existing = await _targetDateRepository.GetByDatesAsync(
                outboundDate,
                returnDate,
                cancellationToken);

            if (existing == null)
            {
                // Create new target date
                var targetDate = new TargetDate
                {
                    OutboundDate = outboundDate,
                    ReturnDate = returnDate,
                    Name = dateConfig.Name
                };

                await _targetDateRepository.AddAsync(targetDate, cancellationToken);
                _logger.LogInformation(
                    "Created target date: {Name} ({Outbound} - {Return})",
                    targetDate.Name,
                    targetDate.OutboundDate.ToShortDateString(),
                    targetDate.ReturnDate.ToShortDateString());
            }
            else
            {
                // Update name if changed
                if (existing.Name != dateConfig.Name)
                {
                    existing.Name = dateConfig.Name;
                    await _targetDateRepository.UpdateAsync(existing, cancellationToken);
                    _logger.LogInformation(
                        "Updated target date: {Name}",
                        existing.Name);
                }
            }
        }

        await _targetDateRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Target dates initialized successfully");
    }

    /// <summary>
    /// Initialize both destinations and target dates from configuration.
    /// Should be called at application startup.
    /// </summary>
    public async Task InitializeAllAsync(CancellationToken cancellationToken = default)
    {
        await InitializeDestinationsAsync(cancellationToken);
        await InitializeTargetDatesAsync(cancellationToken);
    }

    /// <summary>
    /// Get all upcoming target dates from database.
    /// </summary>
    public async Task<IEnumerable<TargetDate>> GetUpcomingTargetDatesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _targetDateRepository.GetUpcomingAsync(cancellationToken);
    }

    /// <summary>
    /// Get all destinations from database.
    /// </summary>
    public async Task<IEnumerable<Destination>> GetAllDestinationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _destinationRepository.GetAllAsync(cancellationToken);
    }
}
