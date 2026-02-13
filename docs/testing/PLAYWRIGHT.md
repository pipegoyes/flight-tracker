# Playwright UI Automation Tests

## 🎭 Overview

Playwright provides **real browser automation** for the Flight Tracker app:
- **Headless Chromium** - Runs without GUI (perfect for Linux servers)
- **Real clicks & typing** - Tests actual user interactions
- **Auto-waits** - Handles async operations intelligently
- **Fast & reliable** - Modern, Microsoft-backed tool

---

## 📦 Setup

### 1. Install Chromium Browser
```bash
cd tests/FlightTracker.IntegrationTests
./setup-playwright.sh
```

Or manually:
```bash
cd tests/FlightTracker.IntegrationTests
dotnet build
node bin/Debug/net8.0/.playwright/package/cli.js install chromium
```

### 2. (Optional) Install System Dependencies
If tests fail with library errors:
```bash
sudo apt-get install libatk1.0-0 libatk-bridge2.0-0 libcups2 \
  libxcomposite1 libxdamage1 libxfixes3 libxrandr2 libgbm1 \
  libpango-1.0-0 libcairo2 libasound2 libatspi2.0-0
```

---

## 🧪 Running Tests

### Run All Playwright Tests
```bash
dotnet test --filter PlaywrightUITests
```

### Run Specific Test
```bash
dotnet test --filter "PlaywrightUITests.HomePage_ShouldLoad"
dotnet test --filter "PlaywrightUITests.DestinationAutocomplete"
```

### Verbose Output
```bash
dotnet test --filter PlaywrightUITests --logger "console;verbosity=detailed"
```

---

## 📋 Available Tests

| Test | What It Does |
|------|--------------|
| `HomePage_ShouldLoad_AndHaveTitle` | Verifies home page renders correctly |
| `ManageDatesPage_ShouldLoad_WithActiveSection` | Checks Manage Dates page loads |
| `AddButton_ShouldOpen_DestinationAutocomplete` | Tests "Add New Date" button interaction |
| `DestinationAutocomplete_ShouldFilter_OnTyping` | Verifies autocomplete search (types "ber" → Berlin) |
| `SelectDestination_ShouldShowChip` | Tests selecting airport shows chip |
| `RemoveDestinationChip_ShouldWork` | Tests removing destination chip (× button) |
| `EditTravelDate_ShouldLoad_ExistingDestinations` | Verifies edit loads existing destinations |
| `NavigationMenu_ShouldWork` | Tests navigation between pages |

---

## 🎯 Test Workflow

### Typical Test Structure:
```csharp
[Fact]
public async Task MyTest()
{
    var page = await _browser!.NewPageAsync();
    
    try
    {
        // Navigate
        await page.GotoAsync("http://localhost:8080/manage-dates");
        
        // Interact
        await page.ClickAsync("button:has-text('Add New Date')");
        await page.FillAsync("input[placeholder*='Search']", "berlin");
        
        // Assert
        Assert.True(await page.Locator(".autocomplete-item:has-text('Berlin')").IsVisibleAsync());
    }
    finally
    {
        await page.CloseAsync();
    }
}
```

---

## 🔍 Debugging

### Screenshot on Failure
Add this to tests:
```csharp
await page.ScreenshotAsync(new() { Path = "failure.png" });
```

### Slow Motion (see what's happening)
```csharp
_browser = await _playwright.Chromium.LaunchAsync(new()
{
    Headless = false,  // Show browser window
    SlowMo = 1000      // 1 second delay between actions
});
```

### Console Logs
```csharp
page.Console += (_, msg) => Console.WriteLine($"[Browser] {msg.Text}");
```

---

## ✅ vs ❌ Comparison

### Shell Script (`validate-ui.sh`)
❌ Can't click buttons  
❌ Can't test autocomplete  
❌ Can't verify interactions  
✅ Fast smoke test  
✅ No browser required  

### Playwright
✅ Tests real user workflows  
✅ Catches JavaScript errors  
✅ Verifies autocomplete filtering  
✅ Tests button clicks & form interactions  
⚠️ Requires Chromium download (~170MB)  
⚠️ Slightly slower than HTTP tests  

---

## 🚀 CI/CD Integration

### GitHub Actions
```yaml
- name: Install Playwright
  run: |
    cd tests/FlightTracker.IntegrationTests
    dotnet build
    node bin/Debug/net8.0/.playwright/package/cli.js install chromium --with-deps

- name: Run Playwright Tests
  run: dotnet test --filter PlaywrightUITests
```

### Docker
```dockerfile
# Install Playwright dependencies
RUN apt-get update && apt-get install -y \
    libatk1.0-0 libatk-bridge2.0-0 libcups2 libgbm1 \
    libpango-1.0-0 libcairo2 libasound2

# Download Chromium
RUN cd /app/tests && \
    node bin/Debug/net8.0/.playwright/package/cli.js install chromium
```

---

## 📊 When to Use Each Test Type

| Test Type | Speed | Coverage | Use When |
|-----------|-------|----------|----------|
| **Unit Tests** | ⚡⚡⚡ | Logic only | Testing services, repositories |
| **Integration Tests** | ⚡⚡ | Data + Logic | Testing database operations |
| **HTTP Tests** | ⚡⚡ | Pages load | Testing Blazor renders |
| **Playwright** | ⚡ | Full UI | Testing user workflows |

**Recommendation:** Use all layers! Each catches different bugs.

---

## 🎬 Example: Full User Journey Test

```csharp
[Fact]
public async Task CompleteUserFlow_AddTravelDate()
{
    var page = await _browser!.NewPageAsync();
    
    try
    {
        // 1. Go to Manage Dates
        await page.GotoAsync($"{BaseUrl}/manage-dates");
        
        // 2. Click Add
        await page.ClickAsync("button:has-text('Add New Date')");
        
        // 3. Fill form
        await page.FillAsync("input[placeholder*='Easter']", "Berlin Trip");
        await page.FillAsync("input[type='date']", "2026-05-01");
        
        // 4. Search destination
        await page.FillAsync("input[placeholder*='Search airport']", "berlin");
        await page.WaitForTimeoutAsync(500); // Debounce
        await page.ClickAsync(".autocomplete-item:has-text('Berlin')");
        
        // 5. Verify chip appears
        Assert.True(await page.Locator(".badge:has-text('BER')").IsVisibleAsync());
        
        // 6. Submit (if Save button works)
        await page.ClickAsync("button:has-text('Save')");
        
        // 7. Verify success
        var successCard = page.Locator(".card:has-text('Berlin Trip')");
        Assert.True(await successCard.IsVisibleAsync());
    }
    finally
    {
        await page.CloseAsync();
    }
}
```

---

## 🐛 Troubleshooting

### "Executable doesn't exist"
→ Run: `./setup-playwright.sh`

### "Cannot find module"
→ Check Node.js is installed: `node --version`

### Library errors (libatk, etc.)
→ Install system dependencies (see Setup section)

### Tests timeout
→ Increase timeout:
```csharp
await page.GotoAsync(url, new() { Timeout = 60000 });
```

---

## 📚 Resources

- [Playwright for .NET Docs](https://playwright.dev/dotnet/docs/intro)
- [Selectors Cheat Sheet](https://playwright.dev/dotnet/docs/selectors)
- [Best Practices](https://playwright.dev/dotnet/docs/best-practices)

---

**Playwright = Production-Grade UI Testing** ✅
