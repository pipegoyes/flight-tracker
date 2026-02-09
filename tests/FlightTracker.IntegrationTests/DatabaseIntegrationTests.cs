using FlightTracker.Core.Entities;
using FlightTracker.Data;
using FlightTracker.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlightTracker.IntegrationTests;

public class DatabaseIntegrationTests : IDisposable
{
    private readonly FlightTrackerDbContext _context;
    private readonly DestinationRepository _destinationRepo;
    private readonly TargetDateRepository _targetDateRepo;
    private readonly PriceCheckRepository _priceCheckRepo;

    public DatabaseIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<FlightTrackerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new FlightTrackerDbContext(options);
        _destinationRepo = new DestinationRepository(_context);
        _targetDateRepo = new TargetDateRepository(_context);
        _priceCheckRepo = new PriceCheckRepository(_context);
    }

    [Fact]
    public async Task CanAddAndRetrieveDestination()
    {
        // Arrange
        var destination = new Destination
        {
            AirportCode = "PMI",
            Name = "Palma de Mallorca"
        };

        // Act
        await _destinationRepo.AddAsync(destination);
        await _destinationRepo.SaveChangesAsync();

        var retrieved = await _destinationRepo.GetByAirportCodeAsync("PMI");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("PMI", retrieved.AirportCode);
        Assert.Equal("Palma de Mallorca", retrieved.Name);
    }

    [Fact]
    public async Task CanAddAndRetrieveTargetDate()
    {
        // Arrange
        var targetDate = new TargetDate
        {
            OutboundDate = new DateTime(2026, 4, 18),
            ReturnDate = new DateTime(2026, 4, 21),
            Name = "Easter Weekend"
        };

        // Act
        await _targetDateRepo.AddAsync(targetDate);
        await _targetDateRepo.SaveChangesAsync();

        var retrieved = await _targetDateRepo.GetByDatesAsync(
            new DateTime(2026, 4, 18),
            new DateTime(2026, 4, 21));

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Easter Weekend", retrieved.Name);
    }

    [Fact]
    public async Task CanAddPriceCheckWithRelationships()
    {
        // Arrange
        var destination = new Destination
        {
            AirportCode = "ARN",
            Name = "Stockholm"
        };
        await _destinationRepo.AddAsync(destination);
        await _destinationRepo.SaveChangesAsync();

        var targetDate = new TargetDate
        {
            OutboundDate = new DateTime(2026, 6, 5),
            ReturnDate = new DateTime(2026, 6, 9),
            Name = "Pentecost"
        };
        await _targetDateRepo.AddAsync(targetDate);
        await _targetDateRepo.SaveChangesAsync();

        var priceCheck = new PriceCheck
        {
            TargetDateId = targetDate.Id,
            DestinationId = destination.Id,
            CheckTimestamp = DateTime.UtcNow,
            Price = 142.50m,
            Currency = "EUR",
            DepartureTime = new TimeOnly(14, 30),
            ArrivalTime = new TimeOnly(16, 45),
            Airline = "Ryanair",
            Stops = 0,
            BookingUrl = "https://test.com"
        };

        // Act
        await _priceCheckRepo.AddAsync(priceCheck);
        await _priceCheckRepo.SaveChangesAsync();

        var retrieved = await _priceCheckRepo.GetLatestAsync(targetDate.Id, destination.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(142.50m, retrieved.Price);
        Assert.Equal("Ryanair", retrieved.Airline);
        Assert.NotNull(retrieved.TargetDate);
        Assert.NotNull(retrieved.Destination);
        Assert.Equal("Stockholm", retrieved.Destination.Name);
    }

    [Fact]
    public async Task GetUpcomingTargetDates_ReturnsOnlyFutureDates()
    {
        // Arrange
        var pastDate = new TargetDate
        {
            OutboundDate = DateTime.Today.AddDays(-10),
            ReturnDate = DateTime.Today.AddDays(-7),
            Name = "Past Trip"
        };
        await _targetDateRepo.AddAsync(pastDate);

        var futureDate = new TargetDate
        {
            OutboundDate = DateTime.Today.AddDays(30),
            ReturnDate = DateTime.Today.AddDays(34),
            Name = "Future Trip"
        };
        await _targetDateRepo.AddAsync(futureDate);
        await _targetDateRepo.SaveChangesAsync();

        // Act
        var upcoming = await _targetDateRepo.GetUpcomingAsync();

        // Assert
        var upcomingList = upcoming.ToList();
        Assert.Single(upcomingList);
        Assert.Equal("Future Trip", upcomingList.First().Name);
    }

    [Fact]
    public async Task GetLatestForTargetDate_ReturnsOnePerDestination()
    {
        // Arrange
        var destination1 = new Destination { AirportCode = "PMI", Name = "Mallorca" };
        var destination2 = new Destination { AirportCode = "ARN", Name = "Stockholm" };
        await _destinationRepo.AddAsync(destination1);
        await _destinationRepo.AddAsync(destination2);
        await _destinationRepo.SaveChangesAsync();

        var targetDate = new TargetDate
        {
            OutboundDate = DateTime.Today.AddDays(30),
            ReturnDate = DateTime.Today.AddDays(34),
            Name = "Test Date"
        };
        await _targetDateRepo.AddAsync(targetDate);
        await _targetDateRepo.SaveChangesAsync();

        // Add multiple price checks for same destination
        await _priceCheckRepo.AddAsync(new PriceCheck
        {
            TargetDateId = targetDate.Id,
            DestinationId = destination1.Id,
            CheckTimestamp = DateTime.UtcNow.AddHours(-2),
            Price = 100m,
            Currency = "EUR",
            DepartureTime = TimeOnly.MinValue,
            ArrivalTime = TimeOnly.MinValue,
            Airline = "Test"
        });

        await _priceCheckRepo.AddAsync(new PriceCheck
        {
            TargetDateId = targetDate.Id,
            DestinationId = destination1.Id,
            CheckTimestamp = DateTime.UtcNow, // Latest
            Price = 90m,
            Currency = "EUR",
            DepartureTime = TimeOnly.MinValue,
            ArrivalTime = TimeOnly.MinValue,
            Airline = "Test"
        });

        await _priceCheckRepo.AddAsync(new PriceCheck
        {
            TargetDateId = targetDate.Id,
            DestinationId = destination2.Id,
            CheckTimestamp = DateTime.UtcNow,
            Price = 140m,
            Currency = "EUR",
            DepartureTime = TimeOnly.MinValue,
            ArrivalTime = TimeOnly.MinValue,
            Airline = "Test"
        });

        await _priceCheckRepo.SaveChangesAsync();

        // Act
        var latest = await _priceCheckRepo.GetLatestForTargetDateAsync(targetDate.Id);

        // Assert
        var latestList = latest.ToList();
        Assert.Equal(2, latestList.Count); // One per destination
        
        var mallorcaPriceCheck = latestList.First(p => p.DestinationId == destination1.Id);
        Assert.Equal(90m, mallorcaPriceCheck.Price); // Should be the latest (90, not 100)
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
