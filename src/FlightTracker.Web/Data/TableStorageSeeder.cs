using FlightTracker.Core.Interfaces;
using FlightTracker.Data.TableStorage;

namespace FlightTracker.Web.Data;

/// <summary>
/// Seeds initial data into Azure Table Storage.
/// </summary>
public static class TableStorageSeeder
{
    /// <summary>
    /// Seed airports into Table Storage.
    /// </summary>
    public static async Task SeedAirportsAsync(IDestinationRepository destinationRepo, ILogger logger)
    {
        var airports = AirportSeedData.GetAirports();
        var seededCount = 0;

        foreach (var airport in airports)
        {
            try
            {
                var existing = await destinationRepo.GetByAirportCodeAsync(airport.AirportCode);
                if (existing == null)
                {
                    await destinationRepo.AddAsync(airport);
                    seededCount++;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to seed airport {Code}", airport.AirportCode);
            }
        }

        if (seededCount > 0)
        {
            logger.LogInformation("Seeded {Count} airports into Table Storage", seededCount);
        }
        else
        {
            logger.LogInformation("No new airports to seed (already exists or empty list)");
        }
    }
}
