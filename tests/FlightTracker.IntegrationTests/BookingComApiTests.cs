using FlightTracker.Providers.BookingCom;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace FlightTracker.IntegrationTests;

/// <summary>
/// Integration tests for Booking.com API via RapidAPI.
/// These tests call the real API - only run when needed to verify credentials.
/// </summary>
public class BookingComApiTests
{
    private readonly ITestOutputHelper _output;
    
    // Test credentials - these should match what's configured in Azure
    private const string TestApiKey = "e4256d3703msh7da218ad93c15bep103962jsnd7a711defff2";
    private const string TestApiHost = "booking-com15.p.rapidapi.com";

    public BookingComApiTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task SearchFlights_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var httpClient = new HttpClient();
        var logger = new Mock<ILogger<BookingComProvider>>();
        
        var provider = new BookingComProvider(
            httpClient,
            TestApiKey,
            TestApiHost,
            logger.Object);

        // Use Easter weekend dates
        var outboundDate = new DateTime(2026, 4, 17);
        var returnDate = new DateTime(2026, 4, 20);

        // Act
        var result = await provider.SearchFlightsAsync(
            "FRA",
            "BCN",
            outboundDate,
            returnDate);

        // Assert
        _output.WriteLine($"Success: {result.Success}");
        _output.WriteLine($"Error: {result.ErrorMessage ?? "none"}");
        
        if (result.Success && result.Flights.Any())
        {
            var flight = result.Flights.First();
            _output.WriteLine($"Price: {flight.Price} {flight.Currency}");
            _output.WriteLine($"Departure: {flight.DepartureTime:yyyy-MM-dd}");
            _output.WriteLine($"Return: {flight.ArrivalTime:yyyy-MM-dd}");
        }

        Assert.True(result.Success, $"API call failed: {result.ErrorMessage}");
        Assert.NotEmpty(result.Flights);
        
        var firstFlight = result.Flights.First();
        Assert.True(firstFlight.Price > 0, "Price should be greater than 0");
        Assert.Equal("EUR", firstFlight.Currency);
    }

    [Fact]
    public async Task SearchFlights_FRA_To_PMI_ReturnsSuccess()
    {
        // Arrange
        var httpClient = new HttpClient();
        var logger = new Mock<ILogger<BookingComProvider>>();
        
        var provider = new BookingComProvider(
            httpClient,
            TestApiKey,
            TestApiHost,
            logger.Object);

        var outboundDate = new DateTime(2026, 4, 17);
        var returnDate = new DateTime(2026, 4, 20);

        // Act
        var result = await provider.SearchFlightsAsync(
            "FRA",
            "PMI",
            outboundDate,
            returnDate);

        // Assert
        _output.WriteLine($"FRA -> PMI: Success={result.Success}");
        if (result.Success && result.Flights.Any())
        {
            _output.WriteLine($"Price: {result.Flights.First().Price} EUR");
        }
        else
        {
            _output.WriteLine($"Error: {result.ErrorMessage}");
        }

        Assert.True(result.Success, $"API call failed: {result.ErrorMessage}");
    }

    [Fact]
    public async Task SearchFlights_WithInvalidApiKey_ReturnsForbidden()
    {
        // Arrange
        var httpClient = new HttpClient();
        var logger = new Mock<ILogger<BookingComProvider>>();
        
        var provider = new BookingComProvider(
            httpClient,
            "invalid_api_key",
            TestApiHost,
            logger.Object);

        var outboundDate = new DateTime(2026, 4, 17);
        var returnDate = new DateTime(2026, 4, 20);

        // Act
        var result = await provider.SearchFlightsAsync(
            "FRA",
            "BCN",
            outboundDate,
            returnDate);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Forbidden", result.ErrorMessage ?? "");
    }

    [Theory]
    [InlineData("FRA", "BCN")] // Frankfurt to Barcelona
    [InlineData("FRA", "PMI")] // Frankfurt to Palma
    [InlineData("FRA", "TFS")] // Frankfurt to Tenerife
    [InlineData("FRA", "LPA")] // Frankfurt to Gran Canaria
    public async Task SearchFlights_MultipleDestinations_AllReturnResults(string from, string to)
    {
        // Arrange
        var httpClient = new HttpClient();
        var logger = new Mock<ILogger<BookingComProvider>>();
        
        var provider = new BookingComProvider(
            httpClient,
            TestApiKey,
            TestApiHost,
            logger.Object);

        var outboundDate = new DateTime(2026, 4, 17);
        var returnDate = new DateTime(2026, 4, 20);

        // Act
        var result = await provider.SearchFlightsAsync(from, to, outboundDate, returnDate);

        // Assert
        _output.WriteLine($"{from} -> {to}: Success={result.Success}, Price={result.Flights.FirstOrDefault()?.Price ?? 0} EUR");
        
        Assert.True(result.Success, $"{from} -> {to} failed: {result.ErrorMessage}");
    }
}
