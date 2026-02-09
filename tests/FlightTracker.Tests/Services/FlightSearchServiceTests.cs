using FlightTracker.Core.Entities;
using FlightTracker.Core.Interfaces;
using FlightTracker.Core.Models;
using FlightTracker.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlightTracker.Tests.Services;

public class FlightSearchServiceTests
{
    private readonly Mock<IFlightProvider> _mockFlightProvider;
    private readonly Mock<IDestinationRepository> _mockDestinationRepo;
    private readonly Mock<ITargetDateRepository> _mockTargetDateRepo;
    private readonly Mock<IPriceCheckRepository> _mockPriceCheckRepo;
    private readonly Mock<ILogger<FlightSearchService>> _mockLogger;
    private readonly FlightSearchService _service;

    public FlightSearchServiceTests()
    {
        _mockFlightProvider = new Mock<IFlightProvider>();
        _mockDestinationRepo = new Mock<IDestinationRepository>();
        _mockTargetDateRepo = new Mock<ITargetDateRepository>();
        _mockPriceCheckRepo = new Mock<IPriceCheckRepository>();
        _mockLogger = new Mock<ILogger<FlightSearchService>>();

        _service = new FlightSearchService(
            _mockFlightProvider.Object,
            _mockDestinationRepo.Object,
            _mockTargetDateRepo.Object,
            _mockPriceCheckRepo.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SearchAndSaveFlightAsync_WithValidData_SavesPriceCheck()
    {
        // Arrange
        var searchResult = new FlightSearchResult
        {
            Success = true,
            Flights = new[]
            {
                new FlightOption
                {
                    Price = 99.99m,
                    Currency = "EUR",
                    DepartureTime = DateTime.Now.AddDays(10),
                    ArrivalTime = DateTime.Now.AddDays(10).AddHours(2),
                    Airline = "TestAir",
                    Stops = 0,
                    BookingUrl = "https://test.com"
                }
            }
        };

        var destination = new Destination
        {
            Id = 1,
            AirportCode = "PMI",
            Name = "Palma de Mallorca"
        };

        var targetDate = new TargetDate
        {
            Id = 1,
            OutboundDate = DateTime.Now.AddDays(10),
            ReturnDate = DateTime.Now.AddDays(14),
            Name = "Test Weekend"
        };

        _mockFlightProvider
            .Setup(x => x.SearchFlightsAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResult);

        _mockDestinationRepo
            .Setup(x => x.GetByAirportCodeAsync("PMI", It.IsAny<CancellationToken>()))
            .ReturnsAsync(destination);

        _mockTargetDateRepo
            .Setup(x => x.GetByDatesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetDate);

        // Act
        var result = await _service.SearchAndSaveFlightAsync(
            "FRA", "PMI",
            DateTime.Now.AddDays(10),
            DateTime.Now.AddDays(14));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(99.99m, result.Price);
        Assert.Equal("TestAir", result.Airline);
        
        _mockPriceCheckRepo.Verify(
            x => x.AddAsync(It.IsAny<PriceCheck>(), It.IsAny<CancellationToken>()),
            Times.Once);
        
        _mockPriceCheckRepo.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchAndSaveFlightAsync_WhenNoFlightsFound_ReturnsNull()
    {
        // Arrange
        var searchResult = new FlightSearchResult
        {
            Success = true,
            Flights = Array.Empty<FlightOption>()
        };

        _mockFlightProvider
            .Setup(x => x.SearchFlightsAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResult);

        // Act
        var result = await _service.SearchAndSaveFlightAsync(
            "FRA", "PMI",
            DateTime.Now.AddDays(10),
            DateTime.Now.AddDays(14));

        // Assert
        Assert.Null(result);
        
        _mockPriceCheckRepo.Verify(
            x => x.AddAsync(It.IsAny<PriceCheck>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetLatestPricesAsync_CallsRepository()
    {
        // Arrange
        var targetDateId = 1;
        var expectedPrices = new List<PriceCheck>
        {
            new PriceCheck
            {
                Id = 1,
                TargetDateId = targetDateId,
                DestinationId = 1,
                Price = 100m,
                CheckTimestamp = DateTime.UtcNow
            }
        };

        _mockPriceCheckRepo
            .Setup(x => x.GetLatestForTargetDateAsync(targetDateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPrices);

        // Act
        var result = await _service.GetLatestPricesAsync(targetDateId);

        // Assert
        Assert.Single(result);
        Assert.Equal(100m, result.First().Price);
        
        _mockPriceCheckRepo.Verify(
            x => x.GetLatestForTargetDateAsync(targetDateId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
