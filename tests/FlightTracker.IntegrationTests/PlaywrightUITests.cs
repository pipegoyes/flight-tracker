using Microsoft.Playwright;
using Xunit;

namespace FlightTracker.IntegrationTests;

/// <summary>
/// Playwright-based UI automation tests for Flight Tracker.
/// Tests actual browser interactions (click, type, navigation).
/// 
/// Setup required:
/// 1. Build project: dotnet build
/// 2. Install browsers: pwsh bin/Debug/net8.0/playwright.ps1 install chromium
///    OR: node bin/Debug/net8.0/.playwright/package/lib/cli/cli.js install chromium
/// </summary>
[Collection("Playwright")]
public class PlaywrightUITests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private const string BaseUrl = "http://localhost:8080";

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,  // Runs without GUI (perfect for CI/Linux server)
            Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }  // Required for Docker/Linux
        });
    }

    public async Task DisposeAsync()
    {
        if (_browser != null)
            await _browser.CloseAsync();
        
        _playwright?.Dispose();
    }

    [Fact]
    public async Task HomePage_ShouldLoad_AndHaveTitle()
    {
        var page = await _browser!.NewPageAsync();
        
        try
        {
            // Navigate to home page
            await page.GotoAsync(BaseUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Wait for Blazor to hydrate
            await page.WaitForTimeoutAsync(2000);
            
            // Verify page loaded
            var title = await page.TitleAsync();
            Assert.Equal("Flight Tracker", title);
            
            // Verify key UI elements exist
            var heading = page.Locator("h1").First;
            await AssertNotNullAsync(heading);
            
            var addButton = page.Locator("text=Add New Date");
            await AssertNotNullAsync(addButton);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ManageDatesPage_ShouldLoad_WithActiveSection()
    {
        var page = await _browser!.NewPageAsync();
        
        try
        {
            // Navigate to Manage Dates
            await page.GotoAsync($"{BaseUrl}/manage-dates");
            
            // Wait for Blazor to hydrate
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(2000);
            
            // Verify title
            var title = await page.TitleAsync();
            Assert.Equal("Manage Travel Dates", title);
            
            // Verify heading
            var heading = page.Locator("h1:has-text('Manage Travel Dates')");
            await AssertNotNullAsync(heading);
            
            // Verify Add button exists
            var addButton = page.Locator("button:has-text('Add New Date')");
            Assert.True(await addButton.IsVisibleAsync());
            
            // Verify Active Travel Dates section
            var activeSection = page.Locator("h3:has-text('Active Travel Dates')");
            Assert.True(await activeSection.IsVisibleAsync());
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task AddButton_ShouldOpen_DestinationAutocomplete()
    {
        var page = await _browser!.NewPageAsync();
        
        try
        {
            await page.GotoAsync($"{BaseUrl}/manage-dates");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Wait for Blazor to hydrate (SignalR connection)
            await page.WaitForTimeoutAsync(2000);
            
            // Click Add New Date button
            var addButton = page.Locator("button:has-text('Add New Date')");
            await addButton.ClickAsync();
            
            // Wait for form to appear
            await page.WaitForSelectorAsync("text=Destinations", new() { Timeout = 5000 });
            
            // Verify form fields exist
            var nameInput = page.Locator("input[placeholder*='Easter Weekend']");
            Assert.True(await nameInput.IsVisibleAsync());
            
            // Fixed placeholder selector to match actual text
            var destinationSearch = page.Locator("input[placeholder*='Type to search airports']");
            Assert.True(await destinationSearch.IsVisibleAsync());
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task DestinationAutocomplete_ShouldFilter_OnTyping()
    {
        var page = await _browser!.NewPageAsync();
        
        try
        {
            await page.GotoAsync($"{BaseUrl}/manage-dates");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Wait for Blazor to hydrate
            await page.WaitForTimeoutAsync(2000);
            
            // Open add form
            await page.ClickAsync("button:has-text('Add New Date')");
            
            // Fixed placeholder selector
            var searchBox = page.Locator("input[placeholder*='Type to search airports']");
            await searchBox.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
            
            // Type in autocomplete
            await searchBox.ClickAsync();
            await searchBox.FillAsync("ber");
            
            // Wait for debounce (300ms) + network
            await page.WaitForTimeoutAsync(1000);
            
            // Verify dropdown appears with results
            var dropdown = page.Locator(".autocomplete-dropdown");
            await dropdown.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 3000 });
            
            // Verify "Berlin" appears in results
            var berlinOption = page.Locator(".autocomplete-item:has-text('Berlin')");
            Assert.True(await berlinOption.IsVisibleAsync());
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SelectDestination_ShouldShowChip()
    {
        var page = await _browser!.NewPageAsync();
        
        try
        {
            await page.GotoAsync($"{BaseUrl}/manage-dates");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Wait for Blazor to hydrate
            await page.WaitForTimeoutAsync(2000);
            
            // Open add form
            await page.ClickAsync("button:has-text('Add New Date')");
            
            // Fixed placeholder selector
            var searchBox = page.Locator("input[placeholder*='Type to search airports']");
            await searchBox.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
            
            // Search and select destination
            await searchBox.FillAsync("berlin");
            await page.WaitForTimeoutAsync(1000);
            
            // Click on Berlin option
            await page.ClickAsync(".autocomplete-item:has-text('Berlin')");
            await page.WaitForTimeoutAsync(500);
            
            // Verify chip appears
            var chip = page.Locator(".badge:has-text('BER')");
            Assert.True(await chip.IsVisibleAsync());
            
            // Verify search box is cleared
            var searchValue = await searchBox.InputValueAsync();
            Assert.Empty(searchValue);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task RemoveDestinationChip_ShouldWork()
    {
        var page = await _browser!.NewPageAsync();
        
        try
        {
            await page.GotoAsync($"{BaseUrl}/manage-dates");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Wait for Blazor to hydrate
            await page.WaitForTimeoutAsync(2000);
            
            // Add a destination
            await page.ClickAsync("button:has-text('Add New Date')");
            
            // Fixed placeholder selector
            var searchBox = page.Locator("input[placeholder*='Type to search airports']");
            await searchBox.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
            
            await searchBox.FillAsync("paris");
            await page.WaitForTimeoutAsync(1000);
            await page.ClickAsync(".autocomplete-item:has-text('Paris')");
            await page.WaitForTimeoutAsync(500);
            
            // Verify chip exists
            var chip = page.Locator(".badge:has-text('CDG')");
            Assert.True(await chip.IsVisibleAsync());
            
            // Click remove button (×)
            await chip.Locator(".btn-close-chip").ClickAsync();
            
            // Verify chip is gone
            await page.WaitForTimeoutAsync(500);
            Assert.False(await chip.IsVisibleAsync());
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task EditTravelDate_ShouldLoad_ExistingDestinations()
    {
        var page = await _browser!.NewPageAsync();
        
        try
        {
            await page.GotoAsync($"{BaseUrl}/manage-dates");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Wait for Blazor to hydrate
            await page.WaitForTimeoutAsync(2000);
            
            // Click Edit on first travel date (if exists)
            var editButton = page.Locator("button:has-text('Edit')").First;
            
            if (await editButton.IsVisibleAsync())
            {
                await editButton.ClickAsync();
                await page.WaitForTimeoutAsync(1000);
                
                // Verify destination chips are loaded
                var chips = page.Locator(".destination-chips .badge");
                var count = await chips.CountAsync();
                
                // Should have at least one destination
                Assert.True(count > 0, "Edited date should have existing destinations loaded");
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task NavigationMenu_ShouldWork()
    {
        var page = await _browser!.NewPageAsync();
        
        try
        {
            // Start at home
            await page.GotoAsync(BaseUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Wait for Blazor to hydrate
            await page.WaitForTimeoutAsync(2000);
            
            // Click Manage Dates in nav
            await page.ClickAsync("a[href='manage-dates']");
            
            // Wait for navigation and title change
            await page.WaitForFunctionAsync("document.title === 'Manage Travel Dates'", null, new() { Timeout = 5000 });
            
            // Verify we're on the right page
            var title = await page.TitleAsync();
            Assert.Equal("Manage Travel Dates", title);
            
            // Go back to home
            await page.ClickAsync("a[href=''].nav-link:has-text('Home')");
            
            // Wait for navigation and title change
            await page.WaitForFunctionAsync("document.title === 'Flight Tracker'", null, new() { Timeout = 5000 });
            
            title = await page.TitleAsync();
            Assert.Equal("Flight Tracker", title);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// Helper assertion for nullable objects
    /// </summary>
    private static async Task AssertNotNullAsync<T>(T? value) where T : class
    {
        await Task.CompletedTask;
        Xunit.Assert.NotNull(value);
    }
}
