using FlightTracker.Providers.Mock;
using FluentAssertions;

namespace FlightTracker.Tests.Providers;

public class MockFlightProviderTests
{
    private readonly MockFlightProvider _provider;

    public MockFlightProviderTests()
    {
        _provider = new MockFlightProvider();
    }

    [Fact]
    public async Task SearchFlightsAsync_ReturnsSuccessfulResult()
    {
        // Arrange
        var origin = "FRA";
        var destination = "PMI";
        var outbound = DateTime.Now.AddDays(30);
        var returnDate = DateTime.Now.AddDays(34);

        // Act
        var result = await _provider.SearchFlightsAsync(origin, destination, outbound, returnDate);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Origin.Should().Be(origin);
        result.Destination.Should().Be(destination);
    }

    [Fact]
    public async Task SearchFlightsAsync_ReturnsMultipleFlights()
    {
        // Arrange
        var origin = "FRA";
        var destination = "ARN";
        var outbound = DateTime.Now.AddDays(30);
        var returnDate = DateTime.Now.AddDays(34);

        // Act
        var result = await _provider.SearchFlightsAsync(origin, destination, outbound, returnDate);

        // Assert
        result.Flights.Should().NotBeEmpty();
        result.Flights.Count().Should().BeGreaterThanOrEqualTo(3);
        result.Flights.Count().Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task SearchFlightsAsync_ReturnsCheapestFlightFirst()
    {
        // Arrange
        var origin = "FRA";
        var destination = "TFS";
        var outbound = DateTime.Now.AddDays(30);
        var returnDate = DateTime.Now.AddDays(34);

        // Act
        var result = await _provider.SearchFlightsAsync(origin, destination, outbound, returnDate);

        // Assert
        var flights = result.Flights.ToList();
        flights.Should().NotBeEmpty();
        
        // Check that flights are sorted by price (cheapest first)
        for (int i = 0; i < flights.Count - 1; i++)
        {
            flights[i].Price.Should().BeLessThanOrEqualTo(flights[i + 1].Price);
        }
    }

    [Fact]
    public async Task SearchFlightsAsync_IncludesFlightDetails()
    {
        // Arrange
        var origin = "FRA";
        var destination = "LPA";
        var outbound = DateTime.Now.AddDays(30);
        var returnDate = DateTime.Now.AddDays(34);

        // Act
        var result = await _provider.SearchFlightsAsync(origin, destination, outbound, returnDate);

        // Assert
        var firstFlight = result.Flights.First();
        
        firstFlight.Price.Should().BeGreaterThan(0);
        firstFlight.Currency.Should().Be("EUR");
        firstFlight.Airline.Should().NotBeNullOrEmpty();
        firstFlight.DepartureTime.Should().BeAfter(DateTime.Now);
        firstFlight.ArrivalTime.Should().BeAfter(firstFlight.DepartureTime);
        firstFlight.Stops.Should().BeGreaterThanOrEqualTo(0);
        firstFlight.BookingUrl.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("PMI")] // Mallorca
    [InlineData("ARN")] // Stockholm
    [InlineData("TFS")] // Tenerife
    [InlineData("LPA")] // Gran Canaria
    public async Task SearchFlightsAsync_WorksForAllConfiguredDestinations(string destination)
    {
        // Arrange
        var origin = "FRA";
        var outbound = DateTime.Now.AddDays(30);
        var returnDate = DateTime.Now.AddDays(34);

        // Act
        var result = await _provider.SearchFlightsAsync(origin, destination, outbound, returnDate);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Flights.Should().NotBeEmpty();
    }
}
