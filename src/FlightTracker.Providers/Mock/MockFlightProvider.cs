using FlightTracker.Core.Interfaces;
using FlightTracker.Core.Models;

namespace FlightTracker.Providers.Mock;

/// <summary>
/// Mock flight provider that returns fake data for testing.
/// Useful for development without needing real API keys.
/// </summary>
public class MockFlightProvider : IFlightProvider
{
    private readonly Random _random = new();

    public Task<FlightSearchResult> SearchFlightsAsync(
        string originAirportCode,
        string destinationAirportCode,
        DateTime outboundDate,
        DateTime returnDate,
        CancellationToken cancellationToken = default)
    {
        // Simulate API delay
        Task.Delay(500, cancellationToken).Wait(cancellationToken);

        // Generate mock flight options
        var flights = GenerateMockFlights(
            originAirportCode,
            destinationAirportCode,
            outboundDate,
            returnDate);

        var result = new FlightSearchResult
        {
            Success = true,
            Flights = flights,
            Origin = originAirportCode,
            Destination = destinationAirportCode,
            OutboundDate = outboundDate,
            ReturnDate = returnDate
        };

        return Task.FromResult(result);
    }

    private IEnumerable<FlightOption> GenerateMockFlights(
        string origin,
        string destination,
        DateTime outboundDate,
        DateTime returnDate)
    {
        var flights = new List<FlightOption>();

        // Generate 3-5 flight options with varying prices and characteristics
        var flightCount = _random.Next(3, 6);

        for (int i = 0; i < flightCount; i++)
        {
            var basePrice = GetBasePriceForRoute(origin, destination);
            var priceVariation = _random.Next(-30, 50);
            var price = basePrice + priceVariation;

            var departureHour = _random.Next(6, 21); // 6 AM to 9 PM
            var departureMinute = _random.Next(0, 60);
            var flightDuration = GetFlightDuration(origin, destination);
            var stops = GetStops(i, flightCount);

            var departureTime = outboundDate.Date
                .AddHours(departureHour)
                .AddMinutes(departureMinute);
            var arrivalTime = departureTime.AddMinutes(flightDuration);

            flights.Add(new FlightOption
            {
                Price = price,
                Currency = "EUR",
                DepartureTime = departureTime,
                ArrivalTime = arrivalTime,
                Airline = GetRandomAirline(),
                Stops = stops,
                BookingUrl = GenerateBookingUrl(origin, destination, outboundDate, returnDate)
            });
        }

        // Return sorted by price (cheapest first)
        return flights.OrderBy(f => f.Price).ToList();
    }

    private decimal GetBasePriceForRoute(string origin, string destination)
    {
        // Base prices for different destinations from Frankfurt
        return destination switch
        {
            "PMI" => 89m,   // Mallorca - popular, competitive
            "ARN" => 142m,  // Stockholm - longer distance
            "TFS" => 156m,  // Tenerife - far south
            "LPA" => 149m,  // Gran Canaria - far south
            _ => 120m       // Default
        };
    }

    private int GetFlightDuration(string origin, string destination)
    {
        // Approximate flight durations in minutes
        return destination switch
        {
            "PMI" => 135,  // ~2h 15m
            "ARN" => 150,  // ~2h 30m
            "TFS" => 270,  // ~4h 30m
            "LPA" => 270,  // ~4h 30m
            _ => 180       // Default 3h
        };
    }

    private int GetStops(int index, int totalFlights)
    {
        // First 2-3 flights are direct, rest have stops
        if (index < 2)
            return 0;
        
        if (index < totalFlights - 1)
            return 1;
        
        return _random.Next(0, 2);
    }

    private string GetRandomAirline()
    {
        var airlines = new[]
        {
            "Lufthansa",
            "Ryanair",
            "Eurowings",
            "easyJet",
            "Vueling",
            "Air Europa",
            "Condor"
        };

        return airlines[_random.Next(airlines.Length)];
    }

    private string GenerateBookingUrl(
        string origin,
        string destination,
        DateTime outbound,
        DateTime returnDate)
    {
        // Generate a fake booking URL
        var outboundStr = outbound.ToString("yyyy-MM-dd");
        var returnStr = returnDate.ToString("yyyy-MM-dd");
        
        return $"https://www.skyscanner.com/transport/flights/{origin}/{destination}/{outboundStr}/{returnStr}/";
    }
}
