using FlightTracker.Core.Entities;
using FlightTracker.Core.Interfaces;
using FlightTracker.Core.Models;
using Microsoft.Extensions.Logging;

namespace FlightTracker.Core.Services;

/// <summary>
/// Service for searching flights and storing price history.
/// </summary>
public class FlightSearchService
{
    private readonly IFlightProvider _flightProvider;
    private readonly IDestinationRepository _destinationRepository;
    private readonly ITargetDateRepository _targetDateRepository;
    private readonly IPriceCheckRepository _priceCheckRepository;
    private readonly ILogger<FlightSearchService> _logger;

    public FlightSearchService(
        IFlightProvider flightProvider,
        IDestinationRepository destinationRepository,
        ITargetDateRepository targetDateRepository,
        IPriceCheckRepository priceCheckRepository,
        ILogger<FlightSearchService> logger)
    {
        _flightProvider = flightProvider;
        _destinationRepository = destinationRepository;
        _targetDateRepository = targetDateRepository;
        _priceCheckRepository = priceCheckRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get the latest price checks for a specific target date (from database, not live search).
    /// </summary>
    public async Task<IEnumerable<PriceCheck>> GetLatestPricesAsync(
        int targetDateId,
        CancellationToken cancellationToken = default)
    {
        return await _priceCheckRepository.GetLatestForTargetDateAsync(
            targetDateId,
            cancellationToken);
    }

    /// <summary>
    /// Search for flights and save the cheapest option to database.
    /// </summary>
    public async Task<PriceCheck?> SearchAndSaveFlightAsync(
        string originAirportCode,
        string destinationAirportCode,
        DateTime outboundDate,
        DateTime returnDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Searching flights: {Origin} -> {Destination} ({Outbound} to {Return})",
                originAirportCode,
                destinationAirportCode,
                outboundDate.ToShortDateString(),
                returnDate.ToShortDateString());

            // Call flight provider API
            var searchResult = await _flightProvider.SearchFlightsAsync(
                originAirportCode,
                destinationAirportCode,
                outboundDate,
                returnDate,
                cancellationToken);

            if (!searchResult.Success || !searchResult.Flights.Any())
            {
                _logger.LogWarning(
                    "No flights found for {Origin} -> {Destination}: {Error}",
                    originAirportCode,
                    destinationAirportCode,
                    searchResult.ErrorMessage ?? "No flights available");
                return null;
            }

            // Get cheapest flight
            var cheapestFlight = searchResult.Flights
                .OrderBy(f => f.Price)
                .First();

            _logger.LogInformation(
                "Found cheapest flight: {Airline} at {Price} {Currency}",
                cheapestFlight.Airline,
                cheapestFlight.Price,
                cheapestFlight.Currency);

            // Get or create destination
            var destination = await _destinationRepository.GetByAirportCodeAsync(
                destinationAirportCode,
                cancellationToken);

            if (destination == null)
            {
                _logger.LogWarning(
                    "Destination {Code} not found in database",
                    destinationAirportCode);
                return null;
            }

            // Get or create target date
            var targetDate = await _targetDateRepository.GetByDatesAsync(
                outboundDate,
                returnDate,
                cancellationToken);

            if (targetDate == null)
            {
                _logger.LogWarning(
                    "Target date range not found in database: {Outbound} - {Return}",
                    outboundDate.ToShortDateString(),
                    returnDate.ToShortDateString());
                return null;
            }

            // Create price check record
            var priceCheck = new PriceCheck
            {
                TargetDateId = targetDate.Id,
                DestinationId = destination.Id,
                CheckTimestamp = DateTime.UtcNow,
                Price = cheapestFlight.Price,
                Currency = cheapestFlight.Currency,
                DepartureTime = TimeOnly.FromDateTime(cheapestFlight.DepartureTime),
                ArrivalTime = TimeOnly.FromDateTime(cheapestFlight.ArrivalTime),
                Airline = cheapestFlight.Airline,
                Stops = cheapestFlight.Stops,
                BookingUrl = cheapestFlight.BookingUrl
            };

            await _priceCheckRepository.AddAsync(priceCheck, cancellationToken);
            await _priceCheckRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Saved price check: {Id} for {Destination}",
                priceCheck.Id,
                destination.Name);

            return priceCheck;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error searching/saving flight: {Origin} -> {Destination}",
                originAirportCode,
                destinationAirportCode);
            throw;
        }
    }

    /// <summary>
    /// Search and save flights for all configured routes.
    /// Only searches destinations that are associated with each target date.
    /// </summary>
    public async Task<int> SearchAllRoutesAsync(
        string originAirportCode,
        CancellationToken cancellationToken = default)
    {
        var successCount = 0;

        // Get all upcoming target dates
        var targetDates = await _targetDateRepository.GetUpcomingAsync(cancellationToken);

        foreach (var targetDate in targetDates)
        {
            // Get destinations associated with this specific target date
            var destinations = await _targetDateRepository.GetDestinationsAsync(
                targetDate.Id, 
                cancellationToken);

            var destinationsList = destinations.ToList();

            if (!destinationsList.Any())
            {
                _logger.LogWarning(
                    "No destinations configured for target date: {DateName}. Skipping.",
                    targetDate.Name);
                continue;
            }

            _logger.LogInformation(
                "Searching flights for {DateName}: {DestCount} destination(s)",
                targetDate.Name,
                destinationsList.Count);

            foreach (var destination in destinationsList)
            {
                try
                {
                    var result = await SearchAndSaveFlightAsync(
                        originAirportCode,
                        destination.AirportCode,
                        targetDate.OutboundDate,
                        targetDate.ReturnDate,
                        cancellationToken);

                    if (result != null)
                    {
                        successCount++;
                    }

                    // Rate limiting: small delay between API calls
                    await Task.Delay(2000, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to search route: {Origin} -> {Destination} for {Date}",
                        originAirportCode,
                        destination.AirportCode,
                        targetDate.Name);
                }
            }
        }

        _logger.LogInformation(
            "Completed searching all routes. Success: {Count}",
            successCount);

        return successCount;
    }

    /// <summary>
    /// Check prices for a specific target date on-demand.
    /// Uses cached prices if available within the maxAgeHours window, otherwise fetches fresh prices.
    /// </summary>
    /// <param name="originAirportCode">Origin airport (e.g., "FRA")</param>
    /// <param name="targetDateId">Target date ID</param>
    /// <param name="maxAgeHours">Maximum age of cached prices in hours (default: 6 hours)</param>
    /// <returns>Tuple of (cachedCount, fetchedCount, totalResults)</returns>
    public async Task<(int cachedCount, int fetchedCount, IEnumerable<PriceCheck> results)> CheckPricesForTargetDateAsync(
        string originAirportCode,
        int targetDateId,
        int maxAgeHours = 6,
        CancellationToken cancellationToken = default)
    {
        var cachedCount = 0;
        var fetchedCount = 0;
        var results = new List<PriceCheck>();

        // Get target date
        var targetDate = await _targetDateRepository.GetByIdAsync(targetDateId, cancellationToken);
        if (targetDate == null)
        {
            _logger.LogWarning("Target date {Id} not found", targetDateId);
            return (0, 0, results);
        }

        // Get destinations for this target date
        var destinations = await _targetDateRepository.GetDestinationsAsync(
            targetDateId,
            cancellationToken);

        var destinationsList = destinations.ToList();

        if (!destinationsList.Any())
        {
            _logger.LogWarning(
                "No destinations configured for target date: {DateName}",
                targetDate.Name);
            return (0, 0, results);
        }

        _logger.LogInformation(
            "Checking prices for {DateName}: {DestCount} destination(s), max age: {MaxAge}h",
            targetDate.Name,
            destinationsList.Count,
            maxAgeHours);

        foreach (var destination in destinationsList)
        {
            try
            {
                // Check if we have a recent price
                var recentPrice = await _priceCheckRepository.GetRecentPriceAsync(
                    targetDateId,
                    destination.Id,
                    maxAgeHours,
                    cancellationToken);

                if (recentPrice != null)
                {
                    // Use cached price
                    _logger.LogInformation(
                        "Using cached price for {Destination}: {Price} {Currency} (age: {Age:F1}h)",
                        destination.Name,
                        recentPrice.Price,
                        recentPrice.Currency,
                        (DateTime.UtcNow - recentPrice.CheckTimestamp).TotalHours);

                    results.Add(recentPrice);
                    cachedCount++;
                }
                else
                {
                    // Fetch fresh price
                    _logger.LogInformation(
                        "Fetching fresh price for {Destination}",
                        destination.Name);

                    var freshPrice = await SearchAndSaveFlightAsync(
                        originAirportCode,
                        destination.AirportCode,
                        targetDate.OutboundDate,
                        targetDate.ReturnDate,
                        cancellationToken);

                    if (freshPrice != null)
                    {
                        results.Add(freshPrice);
                        fetchedCount++;
                    }

                    // Rate limiting: delay between API calls
                    if (fetchedCount > 0)
                    {
                        await Task.Delay(2000, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to check price for {Origin} -> {Destination}",
                    originAirportCode,
                    destination.AirportCode);
            }
        }

        _logger.LogInformation(
            "Price check complete for {DateName}: {Cached} cached, {Fetched} fetched, {Total} total",
            targetDate.Name,
            cachedCount,
            fetchedCount,
            results.Count);

        return (cachedCount, fetchedCount, results);
    }
}
