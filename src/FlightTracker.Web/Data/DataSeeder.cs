using FlightTracker.Core.Entities;
using FlightTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace FlightTracker.Web.Data;

public static class DataSeeder
{
    /// <summary>
    /// Seed all airports from AirportSeedData.
    /// </summary>
    public static async Task SeedAirportsAsync(FlightTrackerDbContext context)
    {
        // Check if airports are already seeded
        var existingCount = await context.Destinations.CountAsync();
        if (existingCount > 10) // If we have more than 10, assume it's seeded
        {
            return;
        }

        var airports = AirportSeedData.GetAirports();
        
        foreach (var airport in airports)
        {
            // Check if already exists
            var exists = await context.Destinations
                .AnyAsync(d => d.AirportCode == airport.AirportCode);
            
            if (!exists)
            {
                context.Destinations.Add(airport);
            }
        }

        await context.SaveChangesAsync();
    }

    public static async Task SeedHistoricalPriceDataAsync(FlightTrackerDbContext context, bool enabled = true)
    {
        if (!enabled)
        {
            Console.WriteLine("[SEED] Historical price seeding is disabled");
            return;
        }

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
        var random = new Random(42); // Fixed seed for consistent test data
        
        // Prefer popular destinations if available
        var preferredCodes = new[] { "PMI", "BCN", "AGP", "ALC", "TFS", "LPA", "FAO", "OPO", "LIS", "VLC" };
        var preferredDestinations = destinations
            .Where(d => preferredCodes.Contains(d.AirportCode))
            .ToList();
        
        var fallbackDestinations = destinations
            .Where(d => !preferredCodes.Contains(d.AirportCode))
            .ToList();

        foreach (var targetDate in targetDates)
        {
            // Check existing associations
            var existingCount = await context.TargetDateDestinations
                .CountAsync(tdd => tdd.TargetDateId == targetDate.Id);
            
            Console.WriteLine($"[SEED] TargetDate '{targetDate.Name}' has {existingCount} existing destinations");
            
            if (existingCount >= 2)
            {
                Console.WriteLine($"[SEED] Skipping '{targetDate.Name}' - already has {existingCount} destinations");
                continue; // Already has associations
            }

            // Select 2 random destinations (prefer popular ones)
            var pool = preferredDestinations.Any() ? preferredDestinations : fallbackDestinations;
            var selectedDestinations = pool
                .OrderBy(_ => random.Next())
                .Take(2)
                .ToList();

            Console.WriteLine($"[SEED] Selected for '{targetDate.Name}': {string.Join(", ", selectedDestinations.Select(d => d.AirportCode))}");

            foreach (var destination in selectedDestinations)
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
                    Console.WriteLine($"[SEED] Added {destination.AirportCode} to '{targetDate.Name}'");
                }
            }
        }

        Console.WriteLine($"[SEED] Saving {context.ChangeTracker.Entries<TargetDateDestination>().Count()} destination associations...");
        await context.SaveChangesAsync();
        Console.WriteLine("[SEED] Destination associations saved successfully");
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
            // Get only the destinations associated with this target date
            var associatedDestinationIds = await context.TargetDateDestinations
                .Where(tdd => tdd.TargetDateId == targetDate.Id)
                .Select(tdd => tdd.DestinationId)
                .ToListAsync();
            
            var associatedDestinations = destinations
                .Where(d => associatedDestinationIds.Contains(d.Id))
                .ToList();

            foreach (var destination in associatedDestinations)
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
