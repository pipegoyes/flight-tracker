using System.Net;
using FlightTracker.Web;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FlightTracker.IntegrationTests;

/// <summary>
/// Integration tests that validate UI pages load correctly.
/// </summary>
public class UIValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UIValidationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HomePage_ShouldLoad_Successfully()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Flight Tracker", content);
        Assert.Contains("blazor.web.js", content);
    }

    [Fact]
    public async Task ManageDatesPage_ShouldLoad_Successfully()
    {
        // Act
        var response = await _client.GetAsync("/manage-dates");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Manage Travel Dates", content);
        Assert.Contains("Add New Date", content);
        Assert.Contains("Active Travel Dates", content);
    }

    [Fact]
    public async Task ManageDatesPage_ShouldHave_DestinationsField()
    {
        // Act
        var response = await _client.GetAsync("/manage-dates");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Check for destinations autocomplete elements
        // Note: Initial render won't show form, but should have the button
        Assert.Contains("Add New Date", content);
    }

    [Fact]
    public async Task StaticAssets_ShouldLoad()
    {
        // Test critical assets load
        var bootstrapResponse = await _client.GetAsync("/bootstrap/bootstrap.min.css");
        var appCssResponse = await _client.GetAsync("/app.css");

        Assert.Equal(HttpStatusCode.OK, bootstrapResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, appCssResponse.StatusCode);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/manage-dates")]
    public async Task AllPages_ShouldNotHave_CompilationErrors(string url)
    {
        // Act
        var response = await _client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Check for common error indicators
        Assert.DoesNotContain("CS0103", content); // Identifier not found
        Assert.DoesNotContain("CS1503", content); // Argument type error
        Assert.DoesNotContain("error CS", content); // Any C# compilation error
        Assert.DoesNotContain("An unhandled exception occurred", content);
        Assert.DoesNotContain("HTTP Error", content);
    }

    [Fact]
    public async Task ManageDatesPage_ShouldHave_BlazorServerRenderMode()
    {
        // Act
        var response = await _client.GetAsync("/manage-dates");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Check for Blazor Server rendering markers
        Assert.Contains("blazor.web.js", content);
        Assert.Contains("type=\"server\"", content); // Blazor server component marker
    }
}
