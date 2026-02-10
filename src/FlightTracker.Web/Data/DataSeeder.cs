using FlightTracker.Core.Entities;
using FlightTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace FlightTracker.Web.Data;

public static class DataSeeder
{
    public static async Task SeedHistoricalPriceDataAsync(FlightTrackerDbContext context)
    {
        // Get all destinations and target dates
        var destinations = await context.Destinations.ToListAsync();
        var targetDates = await context.TargetDates.ToListAsync();

        if (!destinations.Any() || !targetDates.Any())
        {
            return; // Nothing to seed
        }

        // Ensure all target dates have destination associations
        await SeedTargetDateDestinationsAsync(context, destinations, targetDates);

        // Seed data for the past 7 days (twice per day = 14 checks)
        var now = DateTime.UtcNow;
        var random = new Random(42); // Fixed seed for consistent test data

        for (int daysAgo = 7; daysAgo >= 0; daysAgo--)
        {
            // Morning check (6 AM UTC = 7-8 AM CET)
            await SeedPriceCheck(context, destinations, targetDates, 
                now.AddDays(-daysAgo).Date.AddHours(6), random);

            // Evening check (18 PM UTC = 19-20 PM CET)
            if (daysAgo > 0) // Don't add future evening check
            {
                await SeedPriceCheck(context, destinations, targetDates, 
                    now.AddDays(-daysAgo).Date.AddHours(18), random);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedTargetDateDestinationsAsync(
        FlightTrackerDbContext context,
        List<Destination> destinations,
        List<TargetDate> targetDates)
    {
        foreach (var targetDate in targetDates)
        {
            foreach (var destination in destinations)
            {
                // Check if association already exists
                var exists = await context.TargetDateDestinations.AnyAsync(tdd =>
                    tdd.TargetDateId == targetDate.Id &&
                    tdd.DestinationId == destination.Id);

                if (!exists)
                {
                    context.TargetDateDestinations.Add(new TargetDateDestination
                    {
                        TargetDateId = targetDate.Id,
                        DestinationId = destination.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedPriceCheck(
        FlightTrackerDbContext context,
        List<Destination> destinations,
        List<TargetDate> targetDates,
        DateTime checkTime,
        Random random)
    {
        foreach (var targetDate in targetDates)
        {
            foreach (var destination in destinations)
            {
                // Check if price check already exists
                var exists = await context.PriceChecks.AnyAsync(p =>
                    p.TargetDateId == targetDate.Id &&
                    p.DestinationId == destination.Id &&
                    p.CheckTimestamp == checkTime);

                if (exists)
                    continue;

                var basePrice = GetBasePriceForDestination(destination.AirportCode);
                
                // Add some variation over time (prices fluctuate)
                var variation = random.Next(-20, 30);
                var trendFactor = (7 - (DateTime.UtcNow - checkTime).TotalDays) * 2; // Prices tend to increase closer to date
                var price = basePrice + variation + (decimal)trendFactor;

                // Ensure positive price
                price = Math.Max(price, basePrice * 0.7m);

                var departureHour = random.Next(6, 20);
                var departureMinute = random.Next(0, 60);
                var flightDuration = GetFlightDuration(destination.AirportCode);

                var priceCheck = new PriceCheck
                {
                    TargetDateId = targetDate.Id,
                    DestinationId = destination.Id,
                    CheckTimestamp = checkTime,
                    Price = Math.Round(price, 2),
                    Currency = "EUR",
                    DepartureTime = new TimeOnly(departureHour, departureMinute),
                    ArrivalTime = new TimeOnly(departureHour, departureMinute).AddMinutes(flightDuration),
                    Airline = GetRandomAirline(random),
                    Stops = random.Next(0, 3) == 0 ? 0 : (random.Next(0, 2)),
                    BookingUrl = $"https://www.skyscanner.com/transport/flights/fra/{destination.AirportCode.ToLower()}"
                };

                context.PriceChecks.Add(priceCheck);
            }
        }
    }

    private static decimal GetBasePriceForDestination(string airportCode)
    {
        return airportCode switch
        {
            "PMI" => 95m,
            "ARN" => 145m,
            "TFS" => 160m,
            "LPA" => 155m,
            _ => 120m
        };
    }

    private static int GetFlightDuration(string airportCode)
    {
        return airportCode switch
        {
            "PMI" => 135,  // 2h 15m
            "ARN" => 150,  // 2h 30m
            "TFS" => 270,  // 4h 30m
            "LPA" => 270,  // 4h 30m
            _ => 180
        };
    }

    private static string GetRandomAirline(Random random)
    {
        var airlines = new[] { "Lufthansa", "Ryanair", "Eurowings", "easyJet", "Vueling", "Condor" };
        return airlines[random.Next(airlines.Length)];
    }
}
