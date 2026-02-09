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
    /// </summary>
    public async Task<int> SearchAllRoutesAsync(
        string originAirportCode,
        CancellationToken cancellationToken = default)
    {
        var successCount = 0;

        // Get all upcoming target dates
        var targetDates = await _targetDateRepository.GetUpcomingAsync(cancellationToken);

        // Get all destinations
        var destinations = await _destinationRepository.GetAllAsync(cancellationToken);

        foreach (var targetDate in targetDates)
        {
            foreach (var destination in destinations)
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
}
