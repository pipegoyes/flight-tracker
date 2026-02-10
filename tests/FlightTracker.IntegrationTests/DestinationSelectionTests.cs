using FlightTracker.Core.Entities;
using FlightTracker.Core.Interfaces;
using FlightTracker.Data;
using FlightTracker.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightTracker.IntegrationTests;

/// <summary>
/// Integration tests for destination selection feature.
/// </summary>
public class DestinationSelectionTests : IDisposable
{
    private readonly FlightTrackerDbContext _context;
    private readonly ITargetDateRepository _targetDateRepository;
    private readonly IDestinationRepository _destinationRepository;

    public DestinationSelectionTests()
    {
        var options = new DbContextOptionsBuilder<FlightTrackerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new FlightTrackerDbContext(options);
        _targetDateRepository = new TargetDateRepository(_context);
        _destinationRepository = new DestinationRepository(_context);

        // Seed destinations
        SeedDestinations().Wait();
    }

    private async Task SeedDestinations()
    {
        var destinations = new[]
        {
            new Destination { AirportCode = "PMI", Name = "Palma de Mallorca" },
            new Destination { AirportCode = "ARN", Name = "Stockholm Arlanda" },
            new Destination { AirportCode = "TFS", Name = "Tenerife South" },
            new Destination { AirportCode = "LPA", Name = "Gran Canaria" }
        };

        foreach (var dest in destinations)
        {
            await _destinationRepository.AddAsync(dest);
        }
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateTargetDateWithDestinations_ShouldSucceed()
    {
        // Arrange
        var targetDate = new TargetDate
        {
            Name = "Summer Trip",
            OutboundDate = new DateTime(2026, 7, 15),
            ReturnDate = new DateTime(2026, 7, 22)
        };

        var allDestinations = await _destinationRepository.GetAllAsync();
        var selectedDestIds = allDestinations.Take(2).Select(d => d.Id).ToArray(); // PMI, ARN

        // Act
        var created = await _targetDateRepository.CreateTargetDateAsync(targetDate);
        await _targetDateRepository.UpdateDestinationsAsync(created.Id, selectedDestIds);

        // Assert
        var destinations = await _targetDateRepository.GetDestinationsAsync(created.Id);
        Assert.Equal(2, destinations.Count());
        Assert.Contains(destinations, d => d.AirportCode == "PMI");
        Assert.Contains(destinations, d => d.AirportCode == "ARN");
    }

    [Fact]
    public async Task UpdateDestinations_ShouldReplaceExisting()
    {
        // Arrange
        var targetDate = new TargetDate
        {
            Name = "Weekend Getaway",
            OutboundDate = DateTime.Today.AddDays(30),
            ReturnDate = DateTime.Today.AddDays(33)
        };

        var allDestinations = (await _destinationRepository.GetAllAsync()).ToList();
        var created = await _targetDateRepository.CreateTargetDateAsync(targetDate);
        
        // Initially select PMI and ARN
        var initialDestIds = allDestinations.Take(2).Select(d => d.Id);
        await _targetDateRepository.UpdateDestinationsAsync(created.Id, initialDestIds);

        // Act: Change to TFS and LPA
        var newDestIds = allDestinations.Skip(2).Take(2).Select(d => d.Id);
        await _targetDateRepository.UpdateDestinationsAsync(created.Id, newDestIds);

        // Assert
        var destinations = (await _targetDateRepository.GetDestinationsAsync(created.Id)).ToList();
        Assert.Equal(2, destinations.Count);
        Assert.DoesNotContain(destinations, d => d.AirportCode == "PMI");
        Assert.DoesNotContain(destinations, d => d.AirportCode == "ARN");
        Assert.Contains(destinations, d => d.AirportCode == "TFS");
        Assert.Contains(destinations, d => d.AirportCode == "LPA");
    }

    [Fact]
    public async Task GetByIdWithDestinations_ShouldIncludeDestinations()
    {
        // Arrange
        var targetDate = new TargetDate
        {
            Name = "Conference Trip",
            OutboundDate = DateTime.Today.AddDays(60),
            ReturnDate = DateTime.Today.AddDays(63)
        };

        var allDestinations = await _destinationRepository.GetAllAsync();
        var created = await _targetDateRepository.CreateTargetDateAsync(targetDate);
        
        var destIds = allDestinations.Take(3).Select(d => d.Id);
        await _targetDateRepository.UpdateDestinationsAsync(created.Id, destIds);

        // Act
        var loaded = await _targetDateRepository.GetByIdWithDestinationsAsync(created.Id);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(3, loaded.TargetDateDestinations.Count);
    }

    [Fact]
    public async Task EmptyDestinationList_ShouldClearAllAssociations()
    {
        // Arrange
        var targetDate = new TargetDate
        {
            Name = "Test Trip",
            OutboundDate = DateTime.Today.AddDays(45),
            ReturnDate = DateTime.Today.AddDays(48)
        };

        var allDestinations = await _destinationRepository.GetAllAsync();
        var created = await _targetDateRepository.CreateTargetDateAsync(targetDate);
        
        var destIds = allDestinations.Select(d => d.Id);
        await _targetDateRepository.UpdateDestinationsAsync(created.Id, destIds);

        // Act: Update with empty list
        await _targetDateRepository.UpdateDestinationsAsync(created.Id, Array.Empty<int>());

        // Assert
        var destinations = await _targetDateRepository.GetDestinationsAsync(created.Id);
        Assert.Empty(destinations);
    }

    [Fact]
    public async Task MultipleTargetDates_CanHaveDifferentDestinations()
    {
        // Arrange
        var date1 = new TargetDate
        {
            Name = "Winter Trip",
            OutboundDate = new DateTime(2026, 12, 20),
            ReturnDate = new DateTime(2026, 12, 27)
        };

        var date2 = new TargetDate
        {
            Name = "Spring Trip",
            OutboundDate = new DateTime(2027, 4, 10),
            ReturnDate = new DateTime(2027, 4, 17)
        };

        var allDestinations = (await _destinationRepository.GetAllAsync()).ToList();
        
        var created1 = await _targetDateRepository.CreateTargetDateAsync(date1);
        var created2 = await _targetDateRepository.CreateTargetDateAsync(date2);

        // Act: Different destinations for each date
        await _targetDateRepository.UpdateDestinationsAsync(
            created1.Id, 
            allDestinations.Take(2).Select(d => d.Id)); // PMI, ARN
        
        await _targetDateRepository.UpdateDestinationsAsync(
            created2.Id, 
            allDestinations.Skip(1).Take(2).Select(d => d.Id)); // ARN, TFS

        // Assert
        var dest1 = (await _targetDateRepository.GetDestinationsAsync(created1.Id)).ToList();
        var dest2 = (await _targetDateRepository.GetDestinationsAsync(created2.Id)).ToList();

        Assert.Equal(2, dest1.Count);
        Assert.Equal(2, dest2.Count);
        
        Assert.Contains(dest1, d => d.AirportCode == "PMI");
        Assert.Contains(dest1, d => d.AirportCode == "ARN");
        
        Assert.Contains(dest2, d => d.AirportCode == "ARN");
        Assert.Contains(dest2, d => d.AirportCode == "TFS");
    }

    [Fact]
    public async Task DeleteTargetDate_ShouldCascadeDeleteDestinationAssociations()
    {
        // Arrange
        var targetDate = new TargetDate
        {
            Name = "Temp Trip",
            OutboundDate = DateTime.Today.AddDays(20),
            ReturnDate = DateTime.Today.AddDays(23)
        };

        var allDestinations = await _destinationRepository.GetAllAsync();
        var created = await _targetDateRepository.CreateTargetDateAsync(targetDate);
        
        var destIds = allDestinations.Select(d => d.Id);
        await _targetDateRepository.UpdateDestinationsAsync(created.Id, destIds);

        // Verify associations exist
        var destinationsBefore = await _targetDateRepository.GetDestinationsAsync(created.Id);
        Assert.NotEmpty(destinationsBefore);

        // Act: Delete target date (soft delete would be better in prod, but testing hard delete for cascade)
        var toDelete = await _targetDateRepository.GetByIdAsync(created.Id);
        if (toDelete != null)
        {
            await _targetDateRepository.DeleteAsync(toDelete);
            await _context.SaveChangesAsync();
        }

        // Assert: Associations should be gone
        var associationsCount = await _context.TargetDateDestinations
            .CountAsync(tdd => tdd.TargetDateId == created.Id);
        
        Assert.Equal(0, associationsCount);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
